using System.Diagnostics;
using System.Text.Json;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using AIAgentSharp.Utils;

namespace AIAgentSharp.Agents;

/// <summary>
/// Handles communication with LLM providers with consistent event emission and error handling.
/// This is the single point of responsibility for all LLM calls in the system.
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
    /// Calls the LLM with function calling support.
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

        try
        {
            var request = new LlmRequest
            {
                Messages = messages,
                Functions = functionSpecs,
                ResponseType = LlmResponseType.FunctionCall
            };

            // Use streaming and aggregate the response
            var response = await LlmResponseAggregator.AggregateChunksAsync(_llm.StreamAsync(request, timeoutCts.Token));

            // Record metrics
            _metricsCollector.RecordTokenUsage(agentId, turnIndex, response.Usage?.InputTokens ?? 0L, response.Usage?.OutputTokens ?? 0, "function-calling");

            // Raise LLM call completed event
            _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, null);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"LLM function call failed: {ex.Message}");
            _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, null, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Calls the LLM with streaming support and emits streaming events.
    /// This is the primary method for all streaming LLM calls in the system.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="agentId">Identifier of the agent making the call.</param>
    /// <param name="turnIndex">Current turn index for event tracking.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The complete response content as a string.</returns>
    public async Task<string> CallLlmWithStreamingAsync(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, CancellationToken ct)
    {
        // Raise LLM call started event
        _eventManager.RaiseLlmCallStarted(agentId, turnIndex);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_llmTimeout);

        try
        {
            var request = new LlmRequest
            {
                Messages = messages,
                ResponseType = LlmResponseType.Text
            };

            var content = "";
            var cleaner = new StreamingContentCleaner(); // New instance per streaming session
            await foreach (var chunk in _llm.StreamAsync(request, timeoutCts.Token))
            {
             //   Console.WriteLine("CallLlmWithStreamingAsync:" + content);
                content += chunk.Content;

                // Process chunk with stateful cleaner
                var cleanedContent = cleaner.ProcessChunk(chunk.Content);
                if (!string.IsNullOrEmpty(cleanedContent))
                {
                    // Create a cleaned chunk for the event
                    var cleanedChunk = new LlmStreamingChunk
                    {
                        Content = cleanedContent,
                        IsFinal = chunk.IsFinal,
                        FinishReason = chunk.FinishReason,
                        FunctionCall = chunk.FunctionCall,
                        Usage = chunk.Usage,
                        ActualResponseType = chunk.ActualResponseType,
                        AdditionalMetadata = chunk.AdditionalMetadata
                    };

                    // Emit streaming chunk event with cleaned content
                    _eventManager.RaiseLlmChunkReceived(agentId, turnIndex, cleanedChunk);
                }
            }

            // Flush any remaining content
            var remainingContent = cleaner.Flush();
            if (!string.IsNullOrEmpty(remainingContent))
            {
                var finalChunk = new LlmStreamingChunk { Content = remainingContent, IsFinal = true };
                _eventManager.RaiseLlmChunkReceived(agentId, turnIndex, finalChunk);
            }

            // Raise LLM call completed event
            _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, null);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError($"LLM streaming call failed: {ex.Message}");
            _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, null, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Calls the LLM and parses the response into a structured ModelMessage.
    /// This method is specifically for Re/Act pattern JSON parsing.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="agentId">Identifier of the agent making the call.</param>
    /// <param name="turnIndex">Current turn index for event tracking.</param>
    /// <param name="turnId">Unique identifier for the current turn.</param>
    /// <param name="state">Current agent state for error handling.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>A parsed <see cref="ModelMessage"/> or null if parsing failed.</returns>
    public async Task<ModelMessage?> CallLlmAndParseAsync(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, string turnId, AgentState state, CancellationToken ct)
    {
        try
        {
            // Use streaming for consistent behavior
            var content = await CallLlmWithStreamingAsync(messages, agentId, turnIndex, ct);

            // Parse the content (without raising duplicate events)
            return await ParseJsonResponseInternal(content, turnIndex, turnId, state, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var err = "LLM call was cancelled by user";
            _logger.LogError(err);
            throw;
        }
        catch (OperationCanceledException)
        {
            var err = $"LLM call deadline exceeded after {_llmTimeout}";
            _logger.LogError(err);
            CreateErrorTurn(state, turnIndex, turnId, err);
            return null;
        }
        catch (Exception llmEx)
        {
            var err = $"LLM call failed: {llmEx.Message}";
            _logger.LogError(err);
            CreateErrorTurn(state, turnIndex, turnId, err);
            return null;
        }
    }

    /// <summary>
    /// Parses a JSON response from the LLM into a ModelMessage.
    /// </summary>
    /// <param name="llmRaw">The raw LLM response content.</param>
    /// <param name="turnIndex">Current turn index for event tracking.</param>
    /// <param name="turnId">Unique identifier for the current turn.</param>
    /// <param name="state">Current agent state for error handling.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A parsed <see cref="ModelMessage"/> or null if parsing failed.</returns>
    public Task<ModelMessage?> ParseJsonResponse(string llmRaw, int turnIndex, string turnId, AgentState state, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug($"Parsing LLM response: {llmRaw}");
            var modelMsg = JsonUtil.ParseStrict(llmRaw, _config);

            _logger.LogDebug($"Parsed action: {modelMsg.Action}, Tool: {modelMsg.ActionInput.Tool}, ToolCalls count: {modelMsg.ActionInput.ToolCalls?.Count ?? 0}");

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
            var err = $"Invalid LLM JSON: {llmRaw} {ex.Message}";
            _logger.LogError(err);

            // Emit status for JSON parse failure
            _statusManager.EmitStatus(state.AgentId, "Invalid model output", "JSON parsing failed", "Will retry with corrected format");

            // Raise LLM call completed event for JSON parsing error
            _eventManager.RaiseLlmCallCompleted(state.AgentId, turnIndex, null, err);

            // Store error as ToolResult
            CreateErrorTurn(state, turnIndex, turnId, err);
            return Task.FromResult<ModelMessage?>(null);
        }
    }

    /// <summary>
    /// Internal method to parse JSON response without raising duplicate events.
    /// </summary>
    private Task<ModelMessage?> ParseJsonResponseInternal(string llmRaw, int turnIndex, string turnId, AgentState state, CancellationToken ct)
    {
        try
        {
            var modelMsg = JsonUtil.ParseStrict(llmRaw, _config);

            _logger.LogDebug($"Parsed action: {modelMsg.Action}, Tool: {modelMsg.ActionInput.Tool}, ToolCalls count: {modelMsg.ActionInput.ToolCalls?.Count ?? 0}");

            // Emit status if LLM provided public fields
            if (!string.IsNullOrEmpty(modelMsg.StatusTitle))
            {
                _statusManager.EmitStatus(state.AgentId, modelMsg.StatusTitle, modelMsg.StatusDetails, modelMsg.NextStepHint, modelMsg.ProgressPct);
            }

            return Task.FromResult<ModelMessage?>(modelMsg);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(llmRaw);
            Trace.WriteLine($"Error: {ex.Message}");
            var err = $"Invalid LLM JSON: {ex.Message}";
            _logger.LogError(err);
            Environment.Exit(1);

            // Emit status for JSON parse failure
            _statusManager.EmitStatus(state.AgentId, "Invalid model output", "JSON parsing failed", "Will retry with corrected format");

            // Raise LLM call completed event for JSON parsing error
            _eventManager.RaiseLlmCallCompleted(state.AgentId, turnIndex, null, err);

            // Store error as ToolResult
            CreateErrorTurn(state, turnIndex, turnId, err);
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

        // Strip the "functions." prefix if present (some LLMs add this prefix)
        var toolName = functionResult.FunctionCall.Name;
        if (toolName.StartsWith("functions."))
        {
            toolName = toolName.Substring("functions.".Length);
        }

        // Create action input
        var actionInput = new ActionInput
        {
            Tool = toolName,
            Params = parameters
        };

        // Create normalized message
        return new ModelMessage
        {
            Thoughts = $"Calling function {toolName} with parameters: {JsonSerializer.Serialize(parameters, JsonUtil.JsonOptions)}",
            Action = AgentAction.ToolCall,
            ActionRaw = "tool_call",
            ActionInput = actionInput
        };
    }

    /// <summary>
    /// Gets the underlying LLM client for direct access when needed.
    /// This method should be used sparingly and only for testing or special cases.
    /// </summary>
    /// <returns>The underlying LLM client.</returns>
    public ILlmClient GetLlmClient()
    {
        return _llm;
    }

    /// <summary>
    /// Creates an error turn in the agent state.
    /// </summary>
    private void CreateErrorTurn(AgentState state, int turnIndex, string turnId, string error)
    {
        var errorTurn = new AgentTurn
        {
            Index = turnIndex,
            TurnId = turnId,
            LlmMessage = null,
            ToolCall = null,
            ToolResult = new ToolExecutionResult
            {
                Success = false,
                Error = error,
                TurnId = turnId,
                CreatedUtc = DateTimeOffset.UtcNow
            }
        };
        state.Turns.Add(errorTurn);
    }
}
