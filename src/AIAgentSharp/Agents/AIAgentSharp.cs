using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIAgentSharp;

/// <summary>
///     A stateful agent implementation that can run tasks, execute tools, and maintain conversation history.
///     This is the main implementation of the IAgent interface, providing a complete agent framework
///     with tool calling, state persistence, event monitoring, and public status updates.
/// </summary>
public sealed class AIAgentSharp : IAgent
{
    private const int MaxAgentHistory = 100; // Maximum number of agents to track
    private const int AgentHistoryTtlHours = 24; // TTL for agent history in hours
    private readonly Dictionary<string, DateTimeOffset> _agentLastActivity = new();
    private readonly AgentConfiguration _config;
    private readonly ILlmClient _llm;
    private readonly TimeSpan _llmTimeout;
    private readonly ILogger _logger;
    private readonly int _maxTurns;
    private readonly IAgentStateStore _store;

    // Loop-breaker heuristic: track last K tool calls per agentId
    private readonly Dictionary<string, Queue<ToolCallRecord>> _toolCallHistory = new();
    private readonly TimeSpan _toolTimeout;
    private readonly bool _useFunctionCalling;

    // Current agent state for status updates
    private string? _agentId;
    private AgentState? _state;

    public AIAgentSharp(
        ILlmClient llmClient,
        IAgentStateStore stateStore,
        ILogger? logger = null,
        AgentConfiguration? config = null)
    {
        _llm = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _store = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? new ConsoleLogger();
        _config = config ?? new AgentConfiguration();
        _maxTurns = Math.Max(1, _config.MaxTurns);
        _llmTimeout = _config.LlmTimeout;
        _toolTimeout = _config.ToolTimeout;
        _useFunctionCalling = _config.UseFunctionCalling;
    }

    public async Task<AgentResult> RunAsync(string agentId, string goal, IEnumerable<ITool> tools, CancellationToken ct = default)
    {
        _logger.LogInformation($"Starting agent run for {agentId} with goal: {goal}");

        // Set current agent state for status updates
        _agentId = agentId;

        // Raise run started event
        RunStarted?.Invoke(this, new AgentRunStartedEventArgs
        {
            AgentId = agentId,
            Goal = goal
        });

        // Emit initial status
        EmitStatus("Starting agent run", $"Goal: {goal}", "Initializing tools and state", 0);

        var state = await EnsureState(agentId, goal, ct);
        _state = state; // Set current state for status updates
        var registry = tools.ToRegistry();

        for (var i = 0; i < _maxTurns; i++)
        {
            _logger.LogDebug($"Executing turn {i + 1}/{_maxTurns}");

            // Emit status before step with improved progress calculation
            var baseProgress = Math.Min(100, i * 100 / _maxTurns);
            var progressPct = Math.Max(0, Math.Min(100, baseProgress));
            EmitStatus("Processing step", $"Turn {i + 1} of {_maxTurns}", "Analyzing goal and history", progressPct);

            // Raise step started event with actual turn index
            StepStarted?.Invoke(this, new AgentStepStartedEventArgs
            {
                AgentId = agentId,
                TurnIndex = state.Turns.Count
            });

            var step = await StepInternalAsync(state, registry, ct);
            await _store.SaveAsync(agentId, state, ct);

            // Raise step completed event
            StepCompleted?.Invoke(this, new AgentStepCompletedEventArgs
            {
                AgentId = agentId,
                TurnIndex = state.Turns.Count,
                Continue = step.Continue,
                ExecutedTool = step.ExecutedTool,
                FinalOutput = step.FinalOutput,
                Error = step.Error
            });

            if (!string.IsNullOrWhiteSpace(step.Error))
            {
                _logger.LogWarning($"Step error: {step.Error}");
                EmitStatus("Step encountered error", step.Error, "Will attempt to recover", progressPct);
                // Continue loop allowing LLM to recover if possible
            }

            if (!step.Continue)
            {
                var result = new AgentResult
                {
                    Succeeded = step.FinalOutput != null,
                    FinalOutput = step.FinalOutput,
                    State = state,
                    Error = step.FinalOutput == null ? step.Error ?? "Stopped without final output" : null
                };

                _logger.LogInformation($"Agent run completed. Success: {result.Succeeded}");

                // Emit completion status
                var finalStatus = result.Succeeded ? "Task completed successfully" : "Task failed";
                EmitStatus(finalStatus, result.FinalOutput ?? result.Error, null, 100);

                // Raise run completed event
                RunCompleted?.Invoke(this, new AgentRunCompletedEventArgs
                {
                    AgentId = agentId,
                    Succeeded = result.Succeeded,
                    FinalOutput = result.FinalOutput,
                    Error = result.Error,
                    TotalTurns = i + 1
                });

                return result;
            }
        }

        _logger.LogWarning($"Max turns {_maxTurns} reached without completion");

        var maxTurnsResult = new AgentResult
        {
            Succeeded = false,
            FinalOutput = null,
            State = state,
            Error = $"Max turns {_maxTurns} reached without finish."
        };

        // Raise run completed event for max turns reached
        RunCompleted?.Invoke(this, new AgentRunCompletedEventArgs
        {
            AgentId = agentId,
            Succeeded = false,
            FinalOutput = null,
            Error = $"Max turns {_maxTurns} reached without finish.",
            TotalTurns = _maxTurns
        });

        return maxTurnsResult;
    }

    public async Task<AgentStepResult> StepAsync(string agentId, string goal, IEnumerable<ITool> tools, CancellationToken ct = default)
    {
        _logger.LogInformation($"Executing single step for agent {agentId}");

        // Set current agent state for status updates
        _agentId = agentId;

        var state = await EnsureState(agentId, goal, ct);
        _state = state; // Set current state for status updates

        // Use state.Turns.Count for accurate turn indexing
        var turnIndex = state.Turns.Count;

        // Raise step started event
        StepStarted?.Invoke(this, new AgentStepStartedEventArgs
        {
            AgentId = agentId,
            TurnIndex = turnIndex
        });

        var registry = tools.ToRegistry();
        var step = await StepInternalAsync(state, registry, ct);
        await _store.SaveAsync(agentId, state, ct);

        // Raise step completed event
        StepCompleted?.Invoke(this, new AgentStepCompletedEventArgs
        {
            AgentId = agentId,
            TurnIndex = turnIndex,
            Continue = step.Continue,
            ExecutedTool = step.ExecutedTool,
            FinalOutput = step.FinalOutput,
            Error = step.Error
        });

        return new AgentStepResult
        {
            Continue = step.Continue,
            ExecutedTool = step.ExecutedTool,
            FinalOutput = step.FinalOutput,
            LlmMessage = step.LlmMessage,
            ToolResult = step.ToolResult,
            State = state,
            Error = step.Error
        };
    }

    // Events for real-time monitoring
    public event EventHandler<AgentRunStartedEventArgs>? RunStarted;
    public event EventHandler<AgentStepStartedEventArgs>? StepStarted;
    public event EventHandler<AgentLlmCallStartedEventArgs>? LlmCallStarted;
    public event EventHandler<AgentLlmCallCompletedEventArgs>? LlmCallCompleted;
    public event EventHandler<AgentToolCallStartedEventArgs>? ToolCallStarted;
    public event EventHandler<AgentToolCallCompletedEventArgs>? ToolCallCompleted;
    public event EventHandler<AgentStepCompletedEventArgs>? StepCompleted;
    public event EventHandler<AgentRunCompletedEventArgs>? RunCompleted;

    /// <summary>
    ///     Event raised when the agent emits public status updates for UI consumption.
    ///     These status updates provide real-time information about the agent's progress
    ///     without exposing internal reasoning or chain-of-thought.
    /// </summary>
    /// <remarks>
    ///     Status updates can come from two sources:
    ///     1. LLM-provided public fields in the JSON response (status_title, status_details, etc.)
    ///     2. Engine-synthesized statuses at key lifecycle points
    ///     The event is only raised when EmitPublicStatus is enabled in the configuration.
    ///     Event handlers should not throw exceptions as they are caught and logged.
    /// </remarks>
    public event EventHandler<AgentStatusEventArgs>? StatusUpdate;

    /// <summary>
    ///     Emits a status update event with exception safety.
    ///     Never throws exceptions from event emission.
    /// </summary>
    private void EmitStatus(string statusTitle, string? statusDetails = null, string? nextStepHint = null, int? progressPct = null)
    {
        if (!_config.EmitPublicStatus || StatusUpdate == null)
        {
            return;
        }

        try
        {
            StatusUpdate.Invoke(this, new AgentStatusEventArgs
            {
                AgentId = _agentId ?? string.Empty,
                TurnIndex = _state?.Turns.Count ?? 0,
                StatusTitle = statusTitle,
                StatusDetails = statusDetails,
                NextStepHint = nextStepHint,
                ProgressPct = progressPct
            });
        }
        catch (Exception ex)
        {
            // Never throw from event emission
            _logger.LogWarning($"Status update event handler threw exception: {ex.Message}");
        }
    }

    private async Task<AgentState> EnsureState(string agentId, string goal, CancellationToken ct)
    {
        var state = await _store.LoadAsync(agentId, ct) ?? new AgentState
        {
            AgentId = agentId,
            Goal = goal,
            Turns = new List<AgentTurn>()
        };
        state.Goal = string.IsNullOrWhiteSpace(state.Goal) ? goal : state.Goal;
        return state;
    }

    private async Task<AgentStepResult> StepInternalAsync(AgentState state, IDictionary<string, ITool> tools, CancellationToken ct)
    {
        var turnIndex = state.Turns.Count;
        var turnId = GenerateTurnId(turnIndex);

        var messages = BuildMessages(state, tools, _config);
        ModelMessage? modelMsg;

        // Emit status before LLM call
        EmitStatus("Analyzing task", "Processing goal and history", "Preparing to make decision");

        // Try function calling if enabled and supported
        if (_useFunctionCalling && _llm is IFunctionCallingLlmClient functionClient)
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
                    // Raise LLM call started event for function calling
                    LlmCallStarted?.Invoke(this, new AgentLlmCallStartedEventArgs
                    {
                        AgentId = state.AgentId,
                        TurnIndex = turnIndex
                    });

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(_llmTimeout);

                    _logger.LogDebug($"Attempting function calling with {functionSpecs.Count} functions");
                    var functionResult = await functionClient.CompleteWithFunctionsAsync(messages, functionSpecs, timeoutCts.Token);

                    if (functionResult.HasFunctionCall)
                    {
                        _logger.LogInformation($"Function call received: {functionResult.FunctionName}");

                        try
                        {
                            // Normalize function call to Re/Act format
                            modelMsg = NormalizeFunctionCallToReact(functionResult, turnIndex);

                            // Emit status after parsing model action
                            EmitStatus("Tool call detected", $"Calling {functionResult.FunctionName}", "Executing tool");

                            // Raise LLM call completed event for function call
                            LlmCallCompleted?.Invoke(this, new AgentLlmCallCompletedEventArgs
                            {
                                AgentId = state.AgentId,
                                TurnIndex = turnIndex,
                                LlmMessage = modelMsg
                            });

                            // Continue with tool execution (same as JSON path)
                            return await ProcessToolCall(modelMsg, state, tools, turnIndex, turnId, ct);
                        }
                        catch (ArgumentException ex) when (ex.Message.Contains("Failed to parse function arguments"))
                        {
                            // Handle malformed function arguments by creating an error result
                            _logger.LogWarning($"Function argument parsing failed: {ex.Message}");
                            EmitStatus("Function call error", "Invalid function arguments", "Will retry with corrected parameters");

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
                                    Error = ex.Message,
                                    Tool = functionResult.FunctionName ?? "unknown",
                                    Params = new Dictionary<string, object?>(),
                                    TurnId = turnId,
                                    CreatedUtc = DateTimeOffset.UtcNow
                                }
                            };
                            state.Turns.Add(errorTurn);

                            return new AgentStepResult
                            {
                                Continue = true,
                                ExecutedTool = true, // Function argument parsing errors are tool execution attempts
                                ToolResult = errorTurn.ToolResult,
                                State = state
                            };
                        }
                        catch (KeyNotFoundException ex) when (ex.Message.Contains("not found"))
                        {
                            // Handle unknown tool by creating an error result
                            _logger.LogWarning($"Unknown tool in function call: {ex.Message}");
                            EmitStatus("Tool not found", ex.Message, "Will try different approach");

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
                                    Error = ex.Message,
                                    Tool = functionResult.FunctionName ?? "unknown",
                                    Params = new Dictionary<string, object?>(),
                                    TurnId = turnId,
                                    CreatedUtc = DateTimeOffset.UtcNow
                                }
                            };
                            state.Turns.Add(errorTurn);

                            return new AgentStepResult
                            {
                                Continue = true,
                                ExecutedTool = true, // Unknown tool errors are tool execution attempts
                                ToolResult = errorTurn.ToolResult,
                                State = state
                            };
                        }
                    }
                    // Function calling failed, fall back to JSON parsing
                    _logger.LogDebug("Function calling returned no function call, falling back to JSON parsing");
                    var llmRaw = functionResult.RawTextFallback ?? functionResult.AssistantContent ?? "";
                    modelMsg = await ParseJsonResponse(llmRaw, turnIndex, turnId, state, ct);

                    if (modelMsg != null)
                    {
                        return await ProcessAction(modelMsg, state, tools, turnIndex, turnId, ct);
                    }
                }
                else
                {
                    // No function schemas available, use JSON path
                    _logger.LogDebug("No function schemas available, using JSON path");

                    try
                    {
                        var llmRaw = await CallLlmWithTimeout(messages, state.AgentId, turnIndex, ct);
                        modelMsg = await ParseJsonResponse(llmRaw, turnIndex, turnId, state, ct);

                        if (modelMsg != null)
                        {
                            return await ProcessAction(modelMsg, state, tools, turnIndex, turnId, ct);
                        }
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

                        return new AgentStepResult
                        {
                            Continue = true,
                            ExecutedTool = false, // Timeout is not a tool execution
                            ToolResult = errorTurn.ToolResult,
                            Error = err,
                            State = state
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Function calling failed, falling back to JSON: {ex.Message}");

                try
                {
                    var llmRaw = await CallLlmWithTimeout(messages, state.AgentId, turnIndex, ct);
                    modelMsg = await ParseJsonResponse(llmRaw, turnIndex, turnId, state, ct);

                    if (modelMsg != null)
                    {
                        return await ProcessAction(modelMsg, state, tools, turnIndex, turnId, ct);
                    }
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

                    return new AgentStepResult
                    {
                        Continue = true,
                        ExecutedTool = false, // Timeout is not a tool execution
                        ToolResult = errorTurn.ToolResult,
                        Error = err,
                        State = state
                    };
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

                    return new AgentStepResult
                    {
                        Continue = true,
                        ExecutedTool = false, // LLM errors are not tool executions
                        ToolResult = errorTurn.ToolResult,
                        Error = err,
                        State = state
                    };
                }
            }
        }
        else
        {
            // Function calling not enabled or not supported, use JSON path
            try
            {
                var llmRaw = await CallLlmWithTimeout(messages, state.AgentId, turnIndex, ct);
                modelMsg = await ParseJsonResponse(llmRaw, turnIndex, turnId, state, ct);

                if (modelMsg != null)
                {
                    return await ProcessAction(modelMsg, state, tools, turnIndex, turnId, ct);
                }
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

                return new AgentStepResult
                {
                    Continue = true,
                    ExecutedTool = false, // Timeout is not a tool execution
                    ToolResult = errorTurn.ToolResult,
                    Error = err,
                    State = state
                };
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
                        TurnId = turnId
                    }
                };
                state.Turns.Add(errorTurn);

                return new AgentStepResult
                {
                    Continue = true,
                    ExecutedTool = false, // LLM errors are not tool executions
                    ToolResult = errorTurn.ToolResult,
                    Error = err,
                    State = state
                };
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

    // Loop-breaker heuristic methods
    private void RecordToolCall(string agentId, string toolName, Dictionary<string, object?> parameters, bool success)
    {
        lock (_toolCallHistory)
        {
            // Clean up old agent history
            CleanupAgentHistory();

            if (!_toolCallHistory.TryGetValue(agentId, out var history))
            {
                history = new Queue<ToolCallRecord>();
                _toolCallHistory[agentId] = history;
            }

            var record = new ToolCallRecord
            {
                ToolName = toolName,
                ParametersHash = HashToolCall(toolName, parameters),
                Success = success,
                Timestamp = DateTimeOffset.UtcNow
            };

            history.Enqueue(record);

            // Keep only the last MaxToolCallHistory records
            while (history.Count > _config.MaxToolCallHistory)
            {
                history.Dequeue();
            }

            // Update last activity timestamp
            _agentLastActivity[agentId] = DateTimeOffset.UtcNow;
        }
    }

    private void CleanupAgentHistory()
    {
        var now = DateTimeOffset.UtcNow;
        var cutoffTime = now.AddHours(-AgentHistoryTtlHours);

        // Remove agents that haven't been active recently
        var inactiveAgents = _agentLastActivity
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var agentId in inactiveAgents)
        {
            _toolCallHistory.Remove(agentId);
            _agentLastActivity.Remove(agentId);
        }

        // If we still have too many agents, remove the oldest ones
        if (_toolCallHistory.Count > MaxAgentHistory)
        {
            var oldestAgents = _agentLastActivity
                .OrderBy(kvp => kvp.Value)
                .Take(_toolCallHistory.Count - MaxAgentHistory)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var agentId in oldestAgents)
            {
                _toolCallHistory.Remove(agentId);
                _agentLastActivity.Remove(agentId);
            }
        }
    }

    private bool DetectRepeatedFailures(string agentId, string toolName, Dictionary<string, object?> parameters)
    {
        lock (_toolCallHistory)
        {
            if (!_toolCallHistory.TryGetValue(agentId, out var history))
            {
                return false;
            }

            var currentHash = HashToolCall(toolName, parameters);

            // Convert Queue to array for indexing, then iterate from the end
            var historyArray = history.ToArray();
            var failureCount = 0;

            // Scan recent K records and count failures for the same (tool,hash)
            // Reset counter when we find any successful call for the same tool
            for (var i = historyArray.Length - 1; i >= 0; i--)
            {
                var call = historyArray[i];

                if (call.ToolName == toolName)
                {
                    if (call.ParametersHash == currentHash)
                    {
                        if (!call.Success)
                        {
                            failureCount++;

                            if (failureCount >= _config.ConsecutiveFailureThreshold)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            // Found a successful call for the same tool/hash, stop counting
                            break;
                        }
                    }
                    else if (call.Success)
                    {
                        // Found a successful call for the same tool (different params), reset counter
                        break;
                    }
                }
                // Continue scanning even if we hit different tools to catch interleaved failures
            }

            return false;
        }
    }

    private async Task<string> CallLlmWithTimeout(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, CancellationToken ct)
    {
        // Raise LLM call started event
        LlmCallStarted?.Invoke(this, new AgentLlmCallStartedEventArgs
        {
            AgentId = agentId,
            TurnIndex = turnIndex
        });

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

    private Task<ModelMessage?> ParseJsonResponse(string llmRaw, int turnIndex, string turnId, AgentState state, CancellationToken ct)
    {
        try
        {
            var modelMsg = JsonUtil.ParseStrict(llmRaw, _config);

            // Emit status if LLM provided public fields
            if (!string.IsNullOrEmpty(modelMsg.StatusTitle))
            {
                EmitStatus(modelMsg.StatusTitle, modelMsg.StatusDetails, modelMsg.NextStepHint, modelMsg.ProgressPct);
            }

            // Raise LLM call completed event for JSON parsing
            LlmCallCompleted?.Invoke(this, new AgentLlmCallCompletedEventArgs
            {
                AgentId = state.AgentId,
                TurnIndex = turnIndex,
                LlmMessage = modelMsg
            });

            return Task.FromResult<ModelMessage?>(modelMsg);
        }
        catch (Exception ex)
        {
            var err = $"Invalid LLM JSON: {ex.Message}";
            _logger.LogError(err);

            // Emit status for JSON parse failure
            EmitStatus("Invalid model output", "JSON parsing failed", "Will retry with corrected format");

            // Raise LLM call completed event for JSON parsing error
            LlmCallCompleted?.Invoke(this, new AgentLlmCallCompletedEventArgs
            {
                AgentId = state.AgentId,
                TurnIndex = turnIndex,
                Error = err
            });

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

    private async Task<AgentStepResult> ProcessToolCall(ModelMessage modelMsg, AgentState state, IDictionary<string, ITool> tools, int turnIndex, string turnId, CancellationToken ct)
    {
        var toolName = modelMsg.ActionInput.Tool!.Trim();
        var prms = modelMsg.ActionInput.Params ?? new Dictionary<string, object?>();

        var dedupeId = HashToolCall(toolName, prms);

        // Check if tool allows deduplication (but don't fail if tool not found yet)
        var allowDedupe = true;
        var stalenessThreshold = _config.DedupeStalenessThreshold;

        // Try to get the tool to check dedupe settings, but don't fail if not found
        if (tools.TryGetValue(toolName, out var toolForDedupe))
        {
            var dedupeControl = toolForDedupe as IDedupeControl;
            allowDedupe = dedupeControl?.AllowDedupe ?? true;
            var customTtl = dedupeControl?.CustomTtl;
            stalenessThreshold = customTtl ?? _config.DedupeStalenessThreshold;
        }

        // If we already executed this exact call successfully and recently, reuse it.
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

        // Raise tool call started event
        ToolCallStarted?.Invoke(this, new AgentToolCallStartedEventArgs
        {
            AgentId = state.AgentId,
            TurnIndex = turnIndex,
            ToolName = toolName,
            Parameters = prms
        });

        // Emit status on tool start
        EmitStatus("Executing tool", $"Running {toolName}", "Processing tool result");

        ToolExecutionResult execResult;

        try
        {
            var tool = tools.RequireTool(toolName);

            using var toolTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            toolTimeoutCts.CancelAfter(_toolTimeout);

            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug($"Invoking tool {toolName} with timeout {_toolTimeout}");

            var output = await tool.InvokeAsync(prms, toolTimeoutCts.Token);

            stopwatch.Stop();
            execResult = new ToolExecutionResult
            {
                Success = true,
                Output = output,
                Tool = toolName,
                Params = prms,
                TurnId = dedupeId,
                ExecutionTime = stopwatch.Elapsed,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            _logger.LogInformation($"Tool {toolName} executed successfully in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");

            // Emit status on tool success
            EmitStatus("Tool completed", $"{toolName} executed successfully", "Analyzing result");

            // Raise tool call completed event for success
            ToolCallCompleted?.Invoke(this, new AgentToolCallCompletedEventArgs
            {
                AgentId = state.AgentId,
                TurnIndex = turnIndex,
                ToolName = toolName,
                Success = true,
                Output = output,
                ExecutionTime = stopwatch.Elapsed
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var err = $"Tool {toolName} call was cancelled by user";
            _logger.LogError(err);
            EmitStatus("Tool cancelled", err, "Will retry or try different approach");
            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = err,
                Tool = toolName,
                Params = prms,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            // Raise tool call completed event for cancellation
            ToolCallCompleted?.Invoke(this, new AgentToolCallCompletedEventArgs
            {
                AgentId = state.AgentId,
                TurnIndex = turnIndex,
                ToolName = toolName,
                Success = false,
                Error = err
            });
        }
        catch (OperationCanceledException)
        {
            var err = $"Tool {toolName} call deadline exceeded after {_toolTimeout}";
            _logger.LogError(err);
            EmitStatus("Tool timeout", err, "Will retry with different approach");
            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = err,
                Tool = toolName,
                Params = prms,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow,
                // Add a compact machine-readable payload:
                Output = new
                {
                    type = "timeout"
                }
            };

            // Raise tool call completed event for timeout
            ToolCallCompleted?.Invoke(this, new AgentToolCallCompletedEventArgs
            {
                AgentId = state.AgentId,
                TurnIndex = turnIndex,
                ToolName = toolName,
                Success = false,
                Error = err
            });
        }
        catch (ToolValidationException ex)
        {
            var err = $"Tool {toolName} validation failed: {ex.Message}";
            _logger.LogError(err);
            EmitStatus("Validation error", ex.Message, "Will retry with corrected parameters");

            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                Tool = toolName,
                Params = prms,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow,
                // Add a compact machine-readable payload:
                Output = new
                {
                    type = "validation_error",
                    missing = ex.Missing.Count > 0 ? ex.Missing : null,
                    errors = ex.FieldErrors.Count > 0 ? ex.FieldErrors.Select(e => e.Message).ToList() : null
                }
            };

            // Raise tool call completed event for validation error
            ToolCallCompleted?.Invoke(this, new AgentToolCallCompletedEventArgs
            {
                AgentId = state.AgentId,
                TurnIndex = turnIndex,
                ToolName = toolName,
                Success = false,
                Error = err
            });
        }
        catch (KeyNotFoundException ex) when (ex.Message.Contains("not found"))
        {
            var err = $"Tool {toolName} not found: {ex.Message}";
            _logger.LogError(err);
            EmitStatus("Tool not found", err, "Will try different approach");

            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                Tool = toolName,
                Params = prms,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            // Raise tool call completed event for unknown tool error
            ToolCallCompleted?.Invoke(this, new AgentToolCallCompletedEventArgs
            {
                AgentId = state.AgentId,
                TurnIndex = turnIndex,
                ToolName = toolName,
                Success = false,
                Error = err
            });
        }
        catch (Exception ex)
        {
            var err = $"Tool {toolName} execution failed: {ex.Message}";
            _logger.LogError(err);
            EmitStatus("Tool execution failed", err, "Will retry or try different approach");

            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                Tool = toolName,
                Params = prms,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow,
                // Add a compact machine-readable payload:
                Output = new
                {
                    type = "tool_error"
                }
            };

            // Raise tool call completed event for execution error
            ToolCallCompleted?.Invoke(this, new AgentToolCallCompletedEventArgs
            {
                AgentId = state.AgentId,
                TurnIndex = turnIndex,
                ToolName = toolName,
                Success = false,
                Error = err
            });
        }

        // Record the tool call for loop detection
        RecordToolCall(state.AgentId, toolName, prms, execResult.Success);

        turn.ToolResult = execResult;
        state.Turns.Add(turn);

        // Add a controller retry hint after any tool failure
        if (execResult.Success == false)
        {
            EmitStatus("Adding retry hint", "Tool failed, providing guidance", "Will retry with corrected parameters");
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
        if (execResult.Success == false && DetectRepeatedFailures(state.AgentId, toolName, prms))
        {
            _logger.LogWarning($"Loop-breaker triggered for {toolName} with repeated failures");
            EmitStatus("Loop breaker triggered", "Repeated failures detected", "Will try different approach");
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
                EmitStatus("Planning", "Creating execution plan", "Will execute planned steps");
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
                EmitStatus("Finalizing", "Preparing final answer", "Task completion", 100);
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
                EmitStatus("Retrying", "Attempting previous action again", "Will retry with adjustments");
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

    public static IEnumerable<LlmMessage> BuildMessages(AgentState state, IDictionary<string, ITool> tools, AgentConfiguration config)
    {
        var sys = new LlmMessage { Role = "system", Content = Prompts.LlmSystemPrompt };

        var sb = new StringBuilder();
        sb.AppendLine("You will receive your GOAL, TOOL CATALOG, and HISTORY. Respond ONLY with a single JSON object per the MODEL OUTPUT CONTRACT.");
        sb.AppendLine();
        sb.AppendLine("GOAL:");
        sb.AppendLine(state.Goal);
        sb.AppendLine();
        sb.AppendLine("TOOL CATALOG (name and params you may call via action:\"tool_call\"):");

        foreach (var t in tools.Values)
        {
            if (t is IToolIntrospect ti)
            {
                sb.AppendLine($"{ti.Name}: {ti.Describe()}");
            }
            else
            {
                sb.AppendLine($"{t.Name}: {{\"params\":{{}}}}");
            }
        }
        sb.AppendLine("Use the JSON schemas exactly; do not invent fields.");
        sb.AppendLine();

        // Add status update instructions if enabled
        if (config.EmitPublicStatus)
        {
            sb.AppendLine("STATUS UPDATES (optional): You may include these public fields in your JSON response for UI updates:");
            sb.AppendLine("- \"status_title\": string (3-10 words, ≤60 chars) - brief status summary");
            sb.AppendLine("- \"status_details\": string (≤160 chars) - additional context");
            sb.AppendLine("- \"next_step_hint\": string (3-12 words, ≤60 chars) - what you'll do next");
            sb.AppendLine("- \"progress_pct\": integer (0-100) - completion percentage");
            sb.AppendLine("These fields must be public-only. Do not include internal reasoning or chain-of-thought.");
            sb.AppendLine();
        }

        sb.AppendLine("HISTORY (most recent last):");

        var orderedTurns = state.Turns.OrderBy(x => x.Index).ToList();
        var totalTurns = orderedTurns.Count;

        for (var i = 0; i < orderedTurns.Count; i++)
        {
            var t = orderedTurns[i];
            var isRecentTurn = i >= totalTurns - config.MaxRecentTurns;

            if (isRecentTurn || !config.EnableHistorySummarization)
            {
                // Full detail for recent turns
                if (t.LlmMessage != null)
                {
                    sb.AppendLine("LLM:");
                    sb.AppendLine(JsonUtil.ToJson(t.LlmMessage));
                }

                if (t.ToolCall != null)
                {
                    sb.AppendLine("TOOL_CALL:");
                    sb.AppendLine(JsonUtil.ToJson(t.ToolCall));
                }

                if (t.ToolResult != null)
                {
                    sb.AppendLine("TOOL_RESULT:");
                    // Truncate large outputs to prevent prompt bloat
                    var truncatedResult = TruncateToolResultOutput(t.ToolResult, config.MaxToolOutputSize);
                    sb.AppendLine(JsonUtil.ToJson(truncatedResult));
                }
                sb.AppendLine("---");
            }
            else
            {
                // Compact summary for older turns
                var summary = new StringBuilder();

                if (t.LlmMessage != null)
                {
                    var action = t.LlmMessage.Action.ToString().ToLowerInvariant();
                    var thoughts = TruncateString(t.LlmMessage.Thoughts, 100);
                    summary.Append($"LLM: {action} - {thoughts}");
                }

                if (t.ToolCall != null)
                {
                    if (summary.Length > 0)
                    {
                        summary.Append(" | ");
                    }
                    summary.Append($"TOOL: {t.ToolCall.Tool}");
                }

                if (t.ToolResult != null)
                {
                    if (summary.Length > 0)
                    {
                        summary.Append(" | ");
                    }
                    var status = t.ToolResult.Success ? "SUCCESS" : "FAILED";
                    summary.Append($"RESULT: {status}");

                    if (!t.ToolResult.Success && !string.IsNullOrEmpty(t.ToolResult.Error))
                    {
                        summary.Append($" ({TruncateString(t.ToolResult.Error, 50)})");
                    }
                }

                sb.AppendLine($"SUMMARY: {summary}");
                sb.AppendLine("---");
            }
        }

        sb.AppendLine();
        sb.AppendLine(
            "IMPORTANT: Reply with JSON only. No prose or markdown. When a tool call fails, read the validation_error details in HISTORY and immediately retry with corrected parameters. Avoid repeating identical failing calls.");

        var user = new LlmMessage { Role = "user", Content = sb.ToString() };
        return new[] { sys, user };
    }

    private static string TruncateString(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }
        return input.Length <= maxLength ? input : input.Substring(0, maxLength - 3) + "...";
    }

    private static ToolExecutionResult TruncateToolResultOutput(ToolExecutionResult result, int maxOutputSize)
    {
        if (result.Output == null || maxOutputSize <= 0)
        {
            return result;
        }

        var outputJson = JsonUtil.ToJson(result.Output);

        if (outputJson.Length <= maxOutputSize)
        {
            return result;
        }

        // Create a truncated copy
        var truncatedResult = new ToolExecutionResult
        {
            Success = result.Success,
            Error = result.Error,
            Tool = result.Tool,
            Params = result.Params,
            TurnId = result.TurnId,
            ExecutionTime = result.ExecutionTime,
            CreatedUtc = result.CreatedUtc,
            Output = new { truncated = true, original_size = outputJson.Length, preview = outputJson.Substring(0, maxOutputSize - 100) + "..." }
        };

        return truncatedResult;
    }

    // Helper class for tracking tool calls
    private sealed class ToolCallRecord
    {
        public string ToolName { get; set; } = string.Empty;
        public string ParametersHash { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}