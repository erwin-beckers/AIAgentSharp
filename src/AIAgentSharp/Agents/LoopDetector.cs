using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// Detects and prevents infinite loops by tracking tool call history and failure patterns.
/// </summary>
public sealed class LoopDetector : ILoopDetector
{
    private const int MaxAgentHistory = 100; // Maximum number of agents to track
    private const int AgentHistoryTtlHours = 24; // TTL for agent history in hours
    private readonly Dictionary<string, DateTimeOffset> _agentLastActivity = new();
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;

    // Loop-breaker heuristic: track last K tool calls per agentId
    private readonly Dictionary<string, Queue<ToolCallRecord>> _toolCallHistory = new();

    public LoopDetector(AgentConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public void RecordToolCall(string agentId, string toolName, Dictionary<string, object?> parameters, bool success)
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

    public bool DetectRepeatedFailures(string agentId, string toolName, Dictionary<string, object?> parameters)
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

    // Helper class for tracking tool calls
    private sealed class ToolCallRecord
    {
        public string ToolName { get; set; } = string.Empty;
        public string ParametersHash { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
