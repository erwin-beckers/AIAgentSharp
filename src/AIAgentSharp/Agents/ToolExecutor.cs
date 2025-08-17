using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// Handles tool execution, including invocation, validation, and error handling.
/// </summary>
public sealed class ToolExecutor : IToolExecutor
{
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly TimeSpan _toolTimeout;

    public ToolExecutor(
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager)
    {
        _config = config;
        _logger = logger;
        _eventManager = eventManager;
        _statusManager = statusManager;
        _toolTimeout = config.ToolTimeout;
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(string toolName, Dictionary<string, object?> parameters, IDictionary<string, ITool> tools, string agentId, int turnIndex, CancellationToken ct)
    {
        var dedupeId = HashToolCall(toolName, parameters);

        // Raise tool call started event
        _eventManager.RaiseToolCallStarted(agentId, turnIndex, toolName, parameters);

        // Emit status on tool start
        _statusManager.EmitStatus(agentId, "Executing tool", $"Running {toolName}", "Processing tool result");

        ToolExecutionResult execResult;

        try
        {
            var tool = tools.RequireTool(toolName);

            using var toolTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            toolTimeoutCts.CancelAfter(_toolTimeout);

            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug($"Invoking tool {toolName} with timeout {_toolTimeout}");

            var output = await tool.InvokeAsync(parameters, toolTimeoutCts.Token);

            stopwatch.Stop();
            execResult = new ToolExecutionResult
            {
                Success = true,
                Output = output,
                Tool = toolName,
                Params = parameters,
                TurnId = dedupeId,
                ExecutionTime = stopwatch.Elapsed,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            _logger.LogInformation($"Tool {toolName} executed successfully in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");

            // Emit status on tool success
            _statusManager.EmitStatus(agentId, "Tool completed", $"{toolName} executed successfully", "Analyzing result");

            // Raise tool call completed event for success
            _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, true, output, null, stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var err = $"Tool {toolName} call was cancelled by user";
            _logger.LogError(err);
            _statusManager.EmitStatus(agentId, "Tool cancelled", err, "Will retry or try different approach");
            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = err,
                Tool = toolName,
                Params = parameters,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            // Raise tool call completed event for cancellation
            _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, false, null, err);
        }
        catch (OperationCanceledException)
        {
            var err = $"Tool {toolName} call deadline exceeded after {_toolTimeout}";
            _logger.LogError(err);
            _statusManager.EmitStatus(agentId, "Tool timeout", err, "Will retry with different approach");
            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = err,
                Tool = toolName,
                Params = parameters,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow,
                // Add a compact machine-readable payload:
                Output = new
                {
                    type = "timeout"
                }
            };

            // Raise tool call completed event for timeout
            _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, false, null, err);
        }
        catch (ToolValidationException ex)
        {
            var err = $"Tool {toolName} validation failed: {ex.Message}";
            _logger.LogError(err);
            _statusManager.EmitStatus(agentId, "Validation error", ex.Message, "Will retry with corrected parameters");

            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                Tool = toolName,
                Params = parameters,
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
            _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, false, null, err);
        }
        catch (KeyNotFoundException ex) when (ex.Message.Contains("not found"))
        {
            var err = $"Tool {toolName} not found: {ex.Message}";
            _logger.LogError(err);
            _statusManager.EmitStatus(agentId, "Tool not found", err, "Will try different approach");

            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                Tool = toolName,
                Params = parameters,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            // Raise tool call completed event for unknown tool error
            _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, false, null, err);
        }
        catch (Exception ex)
        {
            var err = $"Tool {toolName} execution failed: {ex.Message}";
            _logger.LogError(err);
            _statusManager.EmitStatus(agentId, "Tool execution failed", err, "Will retry or try different approach");

            execResult = new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                Tool = toolName,
                Params = parameters,
                TurnId = dedupeId,
                CreatedUtc = DateTimeOffset.UtcNow,
                // Add a compact machine-readable payload:
                Output = new
                {
                    type = "tool_error"
                }
            };

            // Raise tool call completed event for execution error
            _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, false, null, err);
        }

        return execResult;
    }

    private static string HashToolCall(string tool, Dictionary<string, object?> prms)
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
