using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// Orchestrates the execution of individual agent steps, coordinating between
/// LLM communication, tool execution, and state management.
/// </summary>
public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ILlmClient _llm;
    private readonly IAgentStateStore _store;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly ILlmCommunicator _llmCommunicator;
    private readonly IToolExecutor _toolExecutor;
    private readonly ILoopDetector _loopDetector;
    private readonly IMessageBuilder _messageBuilder;

    public AgentOrchestrator(
        ILlmClient llm,
        IAgentStateStore store,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager)
    {
        _llm = llm;
        _store = store;
        _config = config;
        _logger = logger;
        _eventManager = eventManager;
        _statusManager = statusManager;

        // Initialize specialized components
        _llmCommunicator = new LlmCommunicator(_llm, _config, _logger, _eventManager, _statusManager);
        _toolExecutor = new ToolExecutor(_config, _logger, _eventManager, _statusManager);
        _loopDetector = new LoopDetector(_config, _logger);
        _messageBuilder = new MessageBuilder(_config);
    }

    public async Task<AgentStepResult> ExecuteStepAsync(AgentState state, IDictionary<string, ITool> tools, CancellationToken ct)
    {
        var turnIndex = state.Turns.Count;
        var turnId = GenerateTurnId(turnIndex);

        var messages = _messageBuilder.BuildMessages(state, tools);
        ModelMessage? modelMsg;

        // Emit status before LLM call
        _statusManager.EmitStatus(state.AgentId, "Analyzing task", "Processing goal and history", "Preparing to make decision");

        // Try function calling if enabled and supported
        if (_config.UseFunctionCalling && _llm is IFunctionCallingLlmClient functionClient)
        {
            try
            {
                var functionSpecs = tools.Values
                    .OfType<IFunctionSchemaProvider>()
                    .Select(t => new OpenAiFunctionSpec
                    {
                        Name = t.Name,
                        Description = t.Description,
                        ParametersSchema = t.GetJsonSchema()
                    })
                    .ToList();

                if (functionSpecs.Any())
                {
                    _logger.LogDebug($"Attempting function calling with {functionSpecs.Count} functions");
                    var functionResult = await _llmCommunicator.CallWithFunctionsAsync(messages, functionSpecs, state.AgentId, turnIndex, ct);

                    if (functionResult.HasFunctionCall)
                    {
                        _logger.LogInformation($"Function call received: {functionResult.FunctionName}");

                        try
                        {
                            // Normalize function call to Re/Act format
                            modelMsg = _llmCommunicator.NormalizeFunctionCallToReact(functionResult, turnIndex);

                            // Emit status after parsing model action
                            _statusManager.EmitStatus(state.AgentId, "Tool call detected", $"Calling {functionResult.FunctionName}", "Executing tool");

                            // Continue with tool execution (same as JSON path)
                            return await ProcessToolCall(modelMsg, state, tools, turnIndex, turnId, ct);
                        }
                        catch (ArgumentException ex) when (ex.Message.Contains("Failed to parse function arguments"))
                        {
                            return await HandleFunctionArgumentError(state, functionResult, turnIndex, turnId, ex.Message);
                        }
                        catch (KeyNotFoundException ex) when (ex.Message.Contains("not found"))
                        {
                            return await HandleUnknownToolError(state, functionResult, turnIndex, turnId, ex.Message);
                        }
                    }
                    // Function calling failed, fall back to JSON parsing
                    _logger.LogDebug("Function calling returned no function call, falling back to JSON parsing");
                    var llmRaw = functionResult.RawTextFallback ?? functionResult.AssistantContent ?? "";
                    modelMsg = await _llmCommunicator.ParseJsonResponse(llmRaw, turnIndex, turnId, state, ct);

                    if (modelMsg != null)
                    {
                        return await ProcessAction(modelMsg, state, tools, turnIndex, turnId, ct);
                    }
                }
                else
                {
                    // No function schemas available, use JSON path
                    _logger.LogDebug("No function schemas available, using JSON path");
                    modelMsg = await _llmCommunicator.CallLlmAndParseAsync(messages, state.AgentId, turnIndex, turnId, state, ct);

                    if (modelMsg != null)
                    {
                        return await ProcessAction(modelMsg, state, tools, turnIndex, turnId, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Function calling failed, falling back to JSON: {ex.Message}");
                modelMsg = await _llmCommunicator.CallLlmAndParseAsync(messages, state.AgentId, turnIndex, turnId, state, ct);

                if (modelMsg != null)
                {
                    return await ProcessAction(modelMsg, state, tools, turnIndex, turnId, ct);
                }
            }
        }
        else
        {
            // Function calling not enabled or not supported, use JSON path
            modelMsg = await _llmCommunicator.CallLlmAndParseAsync(messages, state.AgentId, turnIndex, turnId, state, ct);

            if (modelMsg != null)
            {
                return await ProcessAction(modelMsg, state, tools, turnIndex, turnId, ct);
            }
        }

        // If we get here, there was an error in JSON parsing
        // Check if there's an error turn in the state
        var lastTurn = state.Turns.LastOrDefault();

        if (lastTurn?.ToolResult != null && !lastTurn.ToolResult.Success)
        {
            return new AgentStepResult
            {
                Continue = true,
                ExecutedTool = false, // JSON parsing errors are not tool executions
                ToolResult = lastTurn.ToolResult,
                Error = lastTurn.ToolResult.Error,
                State = state
            };
        }

        return new AgentStepResult { Continue = true, ExecutedTool = false, State = state };
    }

    private async Task<AgentStepResult> ProcessToolCall(ModelMessage modelMsg, AgentState state, IDictionary<string, ITool> tools, int turnIndex, string turnId, CancellationToken ct)
    {
        var toolName = modelMsg.ActionInput.Tool!.Trim();
        var prms = modelMsg.ActionInput.Params ?? new Dictionary<string, object?>();

        var dedupeId = HashToolCall(toolName, prms);

        // Check deduplication
        var allowDedupe = true;
        var stalenessThreshold = _config.DedupeStalenessThreshold;

        if (tools.TryGetValue(toolName, out var toolForDedupe))
        {
            var dedupeControl = toolForDedupe as IDedupeControl;
            allowDedupe = dedupeControl?.AllowDedupe ?? true;
            var customTtl = dedupeControl?.CustomTtl;
            stalenessThreshold = customTtl ?? _config.DedupeStalenessThreshold;
        }

        // Check for existing successful result
        if (allowDedupe)
        {
            var prior = state.Turns.LastOrDefault(t =>
                t.ToolResult?.TurnId == dedupeId &&
                t.ToolResult.Success &&
                DateTimeOffset.UtcNow - t.ToolResult.CreatedUtc <= stalenessThreshold);

            if (prior?.ToolResult != null)
            {
                _logger.LogInformation($"Reusing existing successful tool result for id {dedupeId} (age: {DateTimeOffset.UtcNow - prior.ToolResult.CreatedUtc})");
                return new AgentStepResult
                {
                    Continue = true,
                    ExecutedTool = true,
                    LlmMessage = modelMsg,
                    ToolResult = prior.ToolResult,
                    State = state
                };
            }
        }

        var turn = new AgentTurn { Index = turnIndex, TurnId = turnId, LlmMessage = modelMsg };
        turn.ToolCall = new ToolCallRequest { Tool = toolName, Params = prms, TurnId = dedupeId };

        // Execute the tool
        var execResult = await _toolExecutor.ExecuteToolAsync(toolName, prms, tools, state.AgentId, turnIndex, ct);

        // Record the tool call for loop detection
        _loopDetector.RecordToolCall(state.AgentId, toolName, prms, execResult.Success);

        turn.ToolResult = execResult;
        state.Turns.Add(turn);

        // Add retry hints and loop breaker logic
        await AddRetryHintsAndLoopBreaker(state, execResult, toolName, prms, turnIndex);

        return new AgentStepResult
        {
            Continue = true,
            ExecutedTool = true,
            LlmMessage = modelMsg,
            ToolResult = execResult,
            State = state
        };
    }

    private async Task<AgentStepResult> ProcessAction(ModelMessage modelMsg, AgentState state, IDictionary<string, ITool> tools, int turnIndex, string turnId, CancellationToken ct)
    {
        var turn = new AgentTurn { Index = turnIndex, TurnId = turnId, LlmMessage = modelMsg };

        switch (modelMsg.Action)
        {
            case AgentAction.Plan:
            {
                state.Turns.Add(turn);
                _logger.LogDebug("LLM chose to plan");
                _statusManager.EmitStatus(state.AgentId, "Planning", "Creating execution plan", "Will execute planned steps");
                return new AgentStepResult
                {
                    Continue = true,
                    ExecutedTool = false,
                    LlmMessage = modelMsg,
                    State = state
                };
            }
            case AgentAction.ToolCall:
            {
                return await ProcessToolCall(modelMsg, state, tools, turnIndex, turnId, ct);
            }
            case AgentAction.Finish:
            {
                state.Turns.Add(turn);
                _logger.LogInformation($"Agent finished with output: {modelMsg.ActionInput.Final}");
                _statusManager.EmitStatus(state.AgentId, "Finalizing", "Preparing final answer", "Task completion", 100);
                return new AgentStepResult
                {
                    Continue = false,
                    ExecutedTool = false,
                    LlmMessage = modelMsg,
                    FinalOutput = modelMsg.ActionInput.Final,
                    State = state
                };
            }
            case AgentAction.Retry:
            default:
            {
                state.Turns.Add(turn);
                _logger.LogDebug("LLM chose to retry");
                _statusManager.EmitStatus(state.AgentId, "Retrying", "Attempting previous action again", "Will retry with adjustments");
                return new AgentStepResult
                {
                    Continue = true,
                    ExecutedTool = false,
                    LlmMessage = modelMsg,
                    State = state
                };
            }
        }
    }

    private Task<AgentStepResult> HandleFunctionArgumentError(AgentState state, FunctionCallResult functionResult, int turnIndex, string turnId, string errorMessage)
    {
        _logger.LogWarning($"Function argument parsing failed: {errorMessage}");
        _statusManager.EmitStatus(state.AgentId, "Function call error", "Invalid function arguments", "Will retry with corrected parameters");

        var errorTurn = new AgentTurn
        {
            Index = turnIndex,
            TurnId = turnId,
            LlmMessage = null,
            ToolCall = new ToolCallRequest
            {
                Tool = functionResult.FunctionName ?? "unknown",
                Params = new Dictionary<string, object?>(),
                TurnId = turnId
            },
            ToolResult = new ToolExecutionResult
            {
                Success = false,
                Error = errorMessage,
                Tool = functionResult.FunctionName ?? "unknown",
                Params = new Dictionary<string, object?>(),
                TurnId = turnId,
                CreatedUtc = DateTimeOffset.UtcNow
            }
        };
        state.Turns.Add(errorTurn);

        return Task.FromResult(new AgentStepResult
        {
            Continue = true,
            ExecutedTool = true, // Function argument parsing errors are tool execution attempts
            ToolResult = errorTurn.ToolResult,
            State = state
        });
    }

    private Task<AgentStepResult> HandleUnknownToolError(AgentState state, FunctionCallResult functionResult, int turnIndex, string turnId, string errorMessage)
    {
        _logger.LogWarning($"Unknown tool in function call: {errorMessage}");
        _statusManager.EmitStatus(state.AgentId, "Tool not found", errorMessage, "Will try different approach");

        var errorTurn = new AgentTurn
        {
            Index = turnIndex,
            TurnId = turnId,
            LlmMessage = null,
            ToolCall = new ToolCallRequest
            {
                Tool = functionResult.FunctionName ?? "unknown",
                Params = new Dictionary<string, object?>(),
                TurnId = turnId
            },
            ToolResult = new ToolExecutionResult
            {
                Success = false,
                Error = errorMessage,
                Tool = functionResult.FunctionName ?? "unknown",
                Params = new Dictionary<string, object?>(),
                TurnId = turnId,
                CreatedUtc = DateTimeOffset.UtcNow
            }
        };
        state.Turns.Add(errorTurn);

        return Task.FromResult(new AgentStepResult
        {
            Continue = true,
            ExecutedTool = true, // Unknown tool errors are tool execution attempts
            ToolResult = errorTurn.ToolResult,
            State = state
        });
    }

    private Task AddRetryHintsAndLoopBreaker(AgentState state, ToolExecutionResult execResult, string toolName, Dictionary<string, object?> prms, int turnIndex)
    {
        // Add a controller retry hint after any tool failure
        if (execResult.Success == false)
        {
            _statusManager.EmitStatus(state.AgentId, "Adding retry hint", "Tool failed, providing guidance", "Will retry with corrected parameters");
            state.Turns.Add(new AgentTurn
            {
                Index = turnIndex + 1,
                LlmMessage = new ModelMessage
                {
                    Thoughts = "Controller: The last tool call failed. Use the TOOL CATALOG and retry with required params.",
                    Action = AgentAction.Retry,
                    ActionInput = new ActionInput
                    {
                        Summary = $"Retry {toolName} including all required params."
                    }
                }
            });
        }

        // Loop-breaker: Check for repeated failures and insert controller turn
        if (execResult.Success == false && _loopDetector.DetectRepeatedFailures(state.AgentId, toolName, prms))
        {
            _logger.LogWarning($"Loop-breaker triggered for {toolName} with repeated failures");
            _statusManager.EmitStatus(state.AgentId, "Loop breaker triggered", "Repeated failures detected", "Will try different approach");
            state.Turns.Add(new AgentTurn
            {
                Index = turnIndex + 2,
                LlmMessage = new ModelMessage
                {
                    Thoughts = "Controller: You're repeating the same failing call. Read the validation_error.missing and adjust parameters or try a different tool.",
                    Action = AgentAction.Retry,
                    ActionInput = new ActionInput
                    {
                        Summary = $"Stop repeating the same failing call to {toolName}. Check validation_error details and try different parameters or a different tool."
                    }
                }
            });
        }
        
        return Task.CompletedTask;
    }

    private static string GenerateTurnId(int turnIndex)
    {
        return $"turn_{turnIndex}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    public static string HashToolCall(string tool, Dictionary<string, object?> prms)
    {
        var canon = CanonicalizeJson(prms);
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{tool}|{canon}"));
        return Convert.ToHexString(bytes); // stable per (tool,params)
    }

    private static string CanonicalizeJson(object? obj)
    {
        if (obj == null)
        {
            return "null";
        }

        if (obj is JsonElement element)
        {
            return CanonicalizeJsonElement(element);
        }

        // For other types, serialize to JSON first, then canonicalize
        var json = JsonSerializer.Serialize(obj, JsonUtil.JsonOptions);
        using var doc = JsonDocument.Parse(json);
        return CanonicalizeJsonElement(doc.RootElement);
    }

    private static string CanonicalizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var sortedProps = element.EnumerateObject()
                    .OrderBy(p => p.Name)
                    .Select(p => $"\"{p.Name}\":{CanonicalizeJsonElement(p.Value)}");
                return "{" + string.Join(",", sortedProps) + "}";

            case JsonValueKind.Array:
                var sortedArray = element.EnumerateArray()
                    .Select(CanonicalizeJsonElement);
                return "[" + string.Join(",", sortedArray) + "]";

            case JsonValueKind.String:
                return $"\"{element.GetString()}\"";

            case JsonValueKind.Number:
                // Preserve exact number representation
                return element.GetRawText();

            case JsonValueKind.True:
                return "true";

            case JsonValueKind.False:
                return "false";

            case JsonValueKind.Null:
                return "null";

            default:
                return "null";
        }
    }
}
