using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIAgentSharp.Agents.Interfaces;

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
    private readonly TimeSpan _llmTimeout;

    public LlmCommunicator(
        ILlmClient llm,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager)
    {
        _llm = llm;
        _config = config;
        _logger = logger;
        _eventManager = eventManager;
        _statusManager = statusManager;
        _llmTimeout = config.LlmTimeout;
    }

    public async Task<FunctionCallResult> CallWithFunctionsAsync(IEnumerable<LlmMessage> messages, List<OpenAiFunctionSpec> functionSpecs, string agentId, int turnIndex, CancellationToken ct)
    {
        if (_llm is not IFunctionCallingLlmClient functionClient)
        {
            throw new InvalidOperationException("LLM client does not support function calling");
        }

        // Raise LLM call started event for function calling
        _eventManager.RaiseLlmCallStarted(agentId, turnIndex);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_llmTimeout);

        _logger.LogDebug($"Attempting function calling with {functionSpecs.Count} functions");
        var functionResult = await functionClient.CompleteWithFunctionsAsync(messages, functionSpecs, timeoutCts.Token);

        // Raise LLM call completed event for function call
        _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, null);

        return functionResult;
    }

    public async Task<ModelMessage?> CallLlmAndParseAsync(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, string turnId, AgentState state, CancellationToken ct)
    {
        try
        {
            var llmRaw = await CallLlmWithTimeout(messages, agentId, turnIndex, ct);
            return await ParseJsonResponse(llmRaw, turnIndex, turnId, state, ct);
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
            // Handle general LLM errors (e.g., 5xx)
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

    public ModelMessage NormalizeFunctionCallToReact(FunctionCallResult functionResult, int turnIndex)
    {
        // Parse function arguments
        Dictionary<string, object?> parameters;

        try
        {
            var rawParams = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                functionResult.FunctionArgumentsJson ?? "{}",
                JsonUtil.JsonOptions) ?? new Dictionary<string, object?>();

            // Convert JsonElement values to native types
            parameters = new Dictionary<string, object?>();

            foreach (var kvp in rawParams)
            {
                parameters[kvp.Key] = ConvertJsonElementToNativeType(kvp.Value);
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to parse function arguments: {ex.Message}");
        }

        // Generate thoughts - use assistant content if available, otherwise synthesize
        var thoughts = !string.IsNullOrWhiteSpace(functionResult.AssistantContent)
            ? functionResult.AssistantContent.Trim()
            : $"Calling {functionResult.FunctionName} to advance the plan.";

        return new ModelMessage
        {
            Thoughts = thoughts,
            Action = AgentAction.ToolCall,
            ActionRaw = "tool_call",
            ActionInput = new ActionInput
            {
                Tool = functionResult.FunctionName,
                Params = parameters,
                Summary = $"Execute {functionResult.FunctionName} and continue with the results."
            }
        };
    }

    private async Task<string> CallLlmWithTimeout(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, CancellationToken ct)
    {
        // Raise LLM call started event
        _eventManager.RaiseLlmCallStarted(agentId, turnIndex);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_llmTimeout);

            _logger.LogDebug($"Calling LLM with timeout {_llmTimeout}");
            return await _llm.CompleteAsync(messages, timeoutCts.Token);
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
            throw;
        }
        catch (Exception ex)
        {
            var err = $"LLM call failed: {ex.Message}";
            _logger.LogError(err);
            throw;
        }
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
