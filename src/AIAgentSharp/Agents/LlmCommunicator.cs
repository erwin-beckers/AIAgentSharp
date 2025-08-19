using System.Text.Json;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Agents;

/// <summary>
/// Handles communication with LLM providers, including function calling and JSON parsing.
/// </summary>
public sealed class LlmCommunicator : ILlmCommunicator
{
    private readonly ILlmClient _llm;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly IMetricsCollector _metricsCollector;
    private readonly TimeSpan _llmTimeout;

    public LlmCommunicator(
        ILlmClient llm,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager,
        IMetricsCollector metricsCollector)
    {
        _llm = llm;
        _config = config;
        _logger = logger;
        _eventManager = eventManager;
        _statusManager = statusManager;
        _metricsCollector = metricsCollector;
        _llmTimeout = config.LlmTimeout;
    }

    /// <summary>
    /// Calls the LLM with function calling capabilities using the unified streaming interface.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="functionSpecs">The available functions that the LLM can call.</param>
    /// <param name="agentId">Identifier of the agent making the call.</param>
    /// <param name="turnIndex">Current turn index for event tracking.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the function calling operation.</returns>
    public async Task<LlmResponse> CallWithFunctionsAsync(IEnumerable<LlmMessage> messages, List<FunctionSpec> functionSpecs, string agentId, int turnIndex, CancellationToken ct)
    {
        // Raise LLM call started event
        _eventManager.RaiseLlmCallStarted(agentId, turnIndex);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_llmTimeout);

        var request = new LlmRequest
        {
            Messages = messages,
            Functions = functionSpecs,
            ResponseType = LlmResponseType.FunctionCall
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Use the new streaming interface and aggregate chunks
        var response = await LlmResponseAggregator.ProcessChunksWithCallbackAsync(
            _llm.StreamAsync(request, timeoutCts.Token),
            chunk => 
            {
                // Emit real-time chunk events for UI updates
                _eventManager.RaiseLlmChunkReceived(agentId, turnIndex, chunk);
            });
        
        stopwatch.Stop();

        // Record metrics
        _metricsCollector.RecordLlmCallExecutionTime(agentId, turnIndex, stopwatch.ElapsedMilliseconds, "function-calling");
        _metricsCollector.RecordLlmCallCompletion(agentId, turnIndex, true, "function-calling");
        _metricsCollector.RecordApiCall(agentId, "LLM", "function-calling");

        // If provider reported usage, record token usage
        if (response.Usage != null)
        {
            _metricsCollector.RecordTokenUsage(agentId, turnIndex, (int)response.Usage.InputTokens, (int)response.Usage.OutputTokens, response.Usage.Model);
        }

        return response;
    }

    /// <summary>
    /// Calls the LLM and parses the response into a structured ModelMessage.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="agentId">Identifier of the agent making the call.</param>
    /// <param name="turnIndex">Current turn index for event tracking.</param>
    /// <param name="turnId">Unique identifier for the current turn.</param>
    /// <param name="state">Current agent state for error handling.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A parsed <see cref="ModelMessage"/> or null if parsing failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method handles the complete LLM communication cycle:
    /// </para>
    /// <list type="number">
    /// <item><description>Calls the LLM with timeout handling</description></item>
    /// <item><description>Parses the JSON response</description></item>
    /// <item><description>Handles parsing errors gracefully</description></item>
    /// <item><description>Emits events for monitoring</description></item>
    /// <item><description>Updates agent state with errors if needed</description></item>
    /// </list>
    /// <para>
    /// If JSON parsing fails, the method creates an error turn in the agent state
    /// and returns null, allowing the agent to continue with error recovery.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled or times out.</exception>
    public async Task<ModelMessage?> CallLlmAndParseAsync(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, string turnId, AgentState state, CancellationToken ct)
    {
        try
        {
            var response = await CallLlmWithUsage(messages, agentId, turnIndex, ct);
            // Parse content
            var model = await ParseJsonResponse(response.Content, turnIndex, turnId, state, ct);
            return model;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Handle caller cancellation
            var err = "LLM call was cancelled by user";
            _logger.LogError(err);
            throw;
        }
        catch (OperationCanceledException)
        {
            // Handle deadline exceeded
            var err = $"LLM call deadline exceeded after {_llmTimeout}";
            _logger.LogError(err);

            var errorTurn = new AgentTurn
            {
                Index = turnIndex,
                TurnId = turnId,
                LlmMessage = null,
                ToolCall = null,
                ToolResult = new ToolExecutionResult
                {
                    Success = false,
                    Error = err,
                    TurnId = turnId,
                    CreatedUtc = DateTimeOffset.UtcNow
                }
            };
            state.Turns.Add(errorTurn);

            return null;
        }
        catch (Exception llmEx)
        {
            // Handle other LLM errors
            var err = $"LLM call failed: {llmEx.Message}";
            _logger.LogError(err);

            var errorTurn = new AgentTurn
            {
                Index = turnIndex,
                TurnId = turnId,
                LlmMessage = null,
                ToolCall = null,
                ToolResult = new ToolExecutionResult
                {
                    Success = false,
                    Error = err,
                    TurnId = turnId,
                    CreatedUtc = DateTimeOffset.UtcNow
                }
            };
            state.Turns.Add(errorTurn);

            return null;
        }
    }

    public Task<ModelMessage?> ParseJsonResponse(string llmRaw, int turnIndex, string turnId, AgentState state, CancellationToken ct)
    {
        try
        {
            var modelMsg = JsonUtil.ParseStrict(llmRaw, _config);

            // Emit status if LLM provided public fields
            if (!string.IsNullOrEmpty(modelMsg.StatusTitle))
            {
                _statusManager.EmitStatus(state.AgentId, modelMsg.StatusTitle, modelMsg.StatusDetails, modelMsg.NextStepHint, modelMsg.ProgressPct);
            }

            // Raise LLM call completed event for JSON parsing
            _eventManager.RaiseLlmCallCompleted(state.AgentId, turnIndex, modelMsg);

            return Task.FromResult<ModelMessage?>(modelMsg);
        }
        catch (Exception ex)
        {
            var err = $"Invalid LLM JSON: {ex.Message}";
            _logger.LogError(err);

            // Emit status for JSON parse failure
            _statusManager.EmitStatus(state.AgentId, "Invalid model output", "JSON parsing failed", "Will retry with corrected format");

            // Raise LLM call completed event for JSON parsing error
            _eventManager.RaiseLlmCallCompleted(state.AgentId, turnIndex, null, err);

            // Store error as ToolResult instead of fake LLM turn
            var errorTurn = new AgentTurn
            {
                Index = turnIndex,
                TurnId = turnId,
                LlmMessage = null,
                ToolCall = null,
                ToolResult = new ToolExecutionResult
                {
                    Success = false,
                    Error = err,
                    TurnId = turnId,
                    CreatedUtc = DateTimeOffset.UtcNow
                }
            };
            state.Turns.Add(errorTurn);
            return Task.FromResult<ModelMessage?>(null);
        }
    }

    /// <summary>
    /// Normalizes a function call result to Re/Act format.
    /// </summary>
    /// <param name="functionResult">The function call result from the LLM.</param>
    /// <param name="turnIndex">The current turn index.</param>
    /// <returns>A normalized ModelMessage in Re/Act format.</returns>
    public ModelMessage NormalizeFunctionCallToReact(LlmResponse functionResult, int turnIndex)
    {
        if (!functionResult.HasFunctionCall || functionResult.FunctionCall == null)
        {
            throw new ArgumentException("Function result does not contain a function call.");
        }

        // Parse function arguments
        var parameters = new Dictionary<string, object?>();
        try
        {
            if (!string.IsNullOrEmpty(functionResult.FunctionCall.ArgumentsJson))
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(functionResult.FunctionCall.ArgumentsJson, JsonUtil.JsonOptions) ?? new Dictionary<string, object?>();
            }
        }
        catch
        {
            // If parsing fails, create empty parameters
            parameters = new Dictionary<string, object?>();
        }

        // Create action input
        var actionInput = new ActionInput
        {
            Tool = functionResult.FunctionCall.Name,
            Params = parameters
        };

        // Create normalized message
        return new ModelMessage
        {
            Thoughts = $"Calling function {functionResult.FunctionCall.Name} with parameters: {JsonSerializer.Serialize(parameters, JsonUtil.JsonOptions)}",
            Action = AgentAction.ToolCall,
            ActionRaw = "tool_call",
            ActionInput = actionInput
        };
    }

    /// <summary>
    /// Gets the underlying LLM client for direct access when needed.
    /// </summary>
    /// <returns>The underlying LLM client.</returns>
    public ILlmClient GetLlmClient()
    {
        return _llm;
    }

    /// <summary>
    /// Calls the LLM with the unified streaming interface and returns the response with usage metadata.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="agentId">Identifier of the agent making the call.</param>
    /// <param name="turnIndex">Current turn index for event tracking.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The LLM response with usage metadata.</returns>
    private async Task<LlmResponse> CallLlmWithUsage(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, CancellationToken ct)
    {
        // Raise LLM call started event
        _eventManager.RaiseLlmCallStarted(agentId, turnIndex);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_llmTimeout);

        var request = new LlmRequest
        {
            Messages = messages,
            ResponseType = LlmResponseType.Text
        };

        _logger.LogDebug($"Calling LLM with timeout {_llmTimeout}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Use the new streaming interface and aggregate chunks
        var response = await LlmResponseAggregator.ProcessChunksWithCallbackAsync(
            _llm.StreamAsync(request, timeoutCts.Token),
            chunk => 
            {
                // Emit real-time chunk events for UI updates
                _eventManager.RaiseLlmChunkReceived(agentId, turnIndex, chunk);
            });
        
        sw.Stop();

        // Record metrics (only with a concrete model if provided)
        if (response.Usage != null)
        {
            _metricsCollector.RecordLlmCallExecutionTime(agentId, turnIndex, sw.ElapsedMilliseconds, response.Usage.Model);
            _metricsCollector.RecordLlmCallCompletion(agentId, turnIndex, true, response.Usage.Model);
            _metricsCollector.RecordApiCall(agentId, "LLM", response.Usage.Model);
            _metricsCollector.RecordTokenUsage(agentId, turnIndex, (int)response.Usage.InputTokens, (int)response.Usage.OutputTokens, response.Usage.Model);
        }

        return response;
    }

    private static object? ConvertJsonElementToNativeType(object? value)
    {
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => ConvertJsonNumber(element),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(e => ConvertJsonElementToNativeType(e)).ToList(),
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                    prop => prop.Name,
                    prop => ConvertJsonElementToNativeType(prop.Value)),
                _ => value
            };
        }
        return value;
    }

    private static object ConvertJsonNumber(JsonElement element)
    {
        // Try to preserve precision by checking for integers first
        if (element.TryGetInt64(out var longValue))
        {
            // If it fits in int32, return int32 for compatibility
            if (longValue >= int.MinValue && longValue <= int.MaxValue)
            {
                return (int)longValue;
            }
            return longValue;
        }

        // For decimal numbers, try to preserve precision
        if (element.TryGetDecimal(out var decimalValue))
        {
            // If it's a whole number, return as long
            if (decimalValue == Math.Floor(decimalValue))
            {
                var longVal = (long)decimalValue;

                if (longVal >= int.MinValue && longVal <= int.MaxValue)
                {
                    return (int)longVal;
                }
                return longVal;
            }
            return decimalValue;
        }

        // Fallback to double
        return element.GetDouble();
    }
}
