using System.Text;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// Builds messages for LLM communication, including prompt construction and history management.
/// </summary>
public sealed class MessageBuilder : IMessageBuilder
{
    private readonly AgentConfiguration _config;

    public MessageBuilder(AgentConfiguration config)
    {
        _config = config ?? throw new NullReferenceException(nameof(config));
    }

    public IEnumerable<LlmMessage> BuildMessages(AgentState state, IDictionary<string, ITool> tools)
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
        if (_config.EmitPublicStatus)
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
            var isRecentTurn = i >= totalTurns - _config.MaxRecentTurns;

            if (isRecentTurn || !_config.EnableHistorySummarization)
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
                    var truncatedResult = TruncateToolResultOutput(t.ToolResult, _config.MaxToolOutputSize);
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
        var previewLength = Math.Max(1, maxOutputSize - 20); // Ensure we have at least 1 character for preview
        var truncatedResult = new ToolExecutionResult
        {
            Success = result.Success,
            Error = result.Error,
            Tool = result.Tool,
            Params = result.Params,
            TurnId = result.TurnId,
            ExecutionTime = result.ExecutionTime,
            CreatedUtc = result.CreatedUtc,
            Output = new { truncated = true, original_size = outputJson.Length, preview = outputJson.Substring(0, previewLength) + "..." }
        };

        return truncatedResult;
    }
}
