using System;
using System.Collections.Generic;
using System.Linq;
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
        var messages = new List<LlmMessage>();

        // Partition additional messages by role: add all system prompts first, then assistant, user later
        var additional = state.AdditionalMessages ?? new List<LlmMessage>();
        var additionalSystem = additional.Where(
            m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)
        );
        // Build the main content with goal and tools
        var sb = new StringBuilder();
        sb.Append(Prompts.LlmSystemPrompt);

        // Add status update instructions if enabled
        if (_config.EmitPublicStatus)
        {
            sb.AppendLine(
                "STATUS UPDATES (optional): You may include these public fields in your JSON response for UI updates:"
            );
            sb.AppendLine(
                "- \"status_title\": string (3-10 words, ≤60 chars) - brief status summary"
            );
            sb.AppendLine("- \"status_details\": string (≤160 chars) - additional context");
            sb.AppendLine(
                "- \"next_step_hint\": string (3-12 words, ≤60 chars) - what you'll do next"
            );
            sb.AppendLine("- \"progress_pct\": integer (0-100) - completion percentage");
            sb.AppendLine(
                "These fields must be public-only. Do not include internal reasoning or chain-of-thought."
            );
            sb.AppendLine();
        }

        sb.AppendLine("JSON FORMAT RULES:");
        sb.AppendLine("- Use only valid JSON syntax");
        sb.AppendLine("- No comments (// or /* */)");
        sb.AppendLine("- No trailing commas");
        sb.AppendLine("- All strings must be quoted");
        sb.AppendLine("- No explanatory text in parameter values");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT: Reply with JSON only. No prose or markdown.");
        sb.AppendLine();

        if (_config.UseCentralizedSchemas)
        {
            // Build centralized schema registry from tools' parameter types
            var schemaRegistry = BuildSchemaRegistry(tools);

            if (schemaRegistry.Schemas.Count > 0)
            {
                sb.AppendLine("GLOBAL SCHEMAS:");
                foreach (var kv in schemaRegistry.Schemas)
                {
                    sb.AppendLine($"{kv.Key}: {JsonUtil.ToJson(kv.Value)}");
                }
                sb.AppendLine();
            }

            if (schemaRegistry.Rules.Count > 0)
            {
                sb.AppendLine("SCHEMA RULES:");
                foreach (var kv in schemaRegistry.Rules)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Value))
                    {
                        sb.AppendLine($"{kv.Key}: {kv.Value}");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("TOOL CATALOG:");

            foreach (var t in tools.Values)
            {
                if (t is IToolIntrospect ti)
                {
                    var compact = BuildCompactToolDescription(ti);
                    sb.AppendLine($"{ti.Name}: {compact}");
                }
                else
                {
                    sb.AppendLine($"{t.Name}: {{\"params\":{{}}}} ");
                }
            }
        }
        else
        {
        sb.AppendLine("TOOL CATALOG:");

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
        }

        var user = new LlmMessage { Role = "system", Content = sb.ToString() };
        messages.Add(user);


        // Add additional system prompts (before any constructed content)

        var additionalAssistant = additional.Where(
            m => string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase)
        );
        messages.AddRange(additionalSystem);

        // Add assistant prompts next
        messages.AddRange(additionalAssistant);

        user = new LlmMessage { Role = "user", Content = state.Goal };
        messages.Add(user);

        // Finally, append any additional user messages provided by configuration

        var additionalUser = additional
            .Where(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (additionalUser.Count > 0)
        {
            messages.AddRange(additionalUser);
        }

        AddHistory(state, messages, out sb, out user);
        return messages;
    }

    private static (Dictionary<string, object> Schemas, Dictionary<string, string> Rules) BuildSchemaRegistry(IDictionary<string, ITool> tools)
    {
        var schemas = new Dictionary<string, object>();
        var rules = new Dictionary<string, string>();

        foreach (var tool in tools.Values)
        {
            var toolType = tool.GetType();
            var genericBase = toolType.BaseType;
            if (genericBase == null || !genericBase.IsGenericType)
            {
                continue;
            }

            var args = genericBase.GetGenericArguments();
            if (args.Length < 1)
            {
                continue;
            }

            var paramsType = args[0];

            // Walk parameter properties to find any ToolSchema on property types
            foreach (var prop in paramsType.GetProperties())
            {
                var propType = prop.PropertyType;

                // Type-level ToolSchema
                var typeAttr = propType.GetCustomAttributes(typeof(AIAgentSharp.Schema.ToolSchemaAttribute), false).FirstOrDefault() as AIAgentSharp.Schema.ToolSchemaAttribute;
                if (typeAttr != null)
                {
                    var key = propType.FullName ?? propType.Name;
                    if (!schemas.ContainsKey(key))
                    {
                        schemas[key] = SchemaGenerator.Generate(propType);
                        if (!string.IsNullOrWhiteSpace(typeAttr.AdditionalRules))
                        {
                            rules[key] = typeAttr.AdditionalRules!;
                        }
                    }
                }

                // Property-level ToolSchema
                var propAttr = prop.GetCustomAttributes(typeof(AIAgentSharp.Schema.ToolSchemaAttribute), false).FirstOrDefault() as AIAgentSharp.Schema.ToolSchemaAttribute;
                if (propAttr != null)
                {
                    var key = (paramsType.FullName ?? paramsType.Name) + "." + prop.Name;
                    if (!schemas.ContainsKey(key))
                    {
                        schemas[key] = SchemaGenerator.GenerateSchema(prop.PropertyType, new HashSet<Type>());
                        if (!string.IsNullOrWhiteSpace(propAttr.AdditionalRules))
                        {
                            rules[key] = propAttr.AdditionalRules!;
                        }
                    }
                }
            }
        }

        return (schemas, rules);
    }

    private static string BuildCompactToolDescription(IToolIntrospect ti)
    {
        // Build a compact descriptor referencing GLOBAL SCHEMAS for any known custom-typed properties named like 'strategy'
        // Fallback to original description if not available
        try
        {
            var t = ti.GetType();
            var baseType = t.BaseType;
            if (baseType == null || !baseType.IsGenericType) return ti.Describe();

            var args = baseType.GetGenericArguments();
            var paramsType = args[0];
            var props = paramsType.GetProperties().OrderBy(p => p.Name).ToList();

            var compact = new Dictionary<string, object?>
            {
                ["name"] = ti.Name,
                ["description"] = (t.GetProperty("Description")?.GetValue(ti) as string) ?? "",
            };

            var paramShape = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>(),
                ["additionalProperties"] = false
            };

            var propDict = (Dictionary<string, object?>)paramShape["properties"]!;
            var required = new List<string>();

            foreach (var p in props)
            {
                var propName = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(p.Name);
                var schemaAttr = p.GetCustomAttributes(typeof(AIAgentSharp.Schema.ToolSchemaAttribute), false).FirstOrDefault() as AIAgentSharp.Schema.ToolSchemaAttribute;
                var typeAttr = p.PropertyType.GetCustomAttributes(typeof(AIAgentSharp.Schema.ToolSchemaAttribute), false).FirstOrDefault() as AIAgentSharp.Schema.ToolSchemaAttribute;

                if (schemaAttr != null)
                {
                    // property-level custom schema
                    var refId = (paramsType.FullName ?? paramsType.Name) + "." + p.Name;
                    propDict[propName] = new Dictionary<string, object?> { ["$ref"] = $"GLOBAL_SCHEMAS:{refId}" };
                }
                else if (typeAttr != null)
                {
                    // type-level custom schema
                    var refId = p.PropertyType.FullName ?? p.PropertyType.Name;
                    propDict[propName] = new Dictionary<string, object?> { ["$ref"] = $"GLOBAL_SCHEMAS:{refId}" };
                }
                else
                {
                    // fallback minimal schema for primitive/object
                    propDict[propName] = SchemaGenerator.GenerateSchema(p.PropertyType, new HashSet<Type>());
                }

                if (RequiredFieldHelper.IsPropertyRequired(p))
                {
                    required.Add(propName);
                }
            }

            if (required.Count > 0)
            {
                paramShape["required"] = required.ToArray();
            }

            compact["params"] = paramShape;
            return JsonUtil.ToJson(compact);
        }
        catch
        {
            return ti.Describe();
        }
    }
    private void AddHistory(AgentState state, List<LlmMessage> messages, out StringBuilder sb, out LlmMessage user)
    {
        sb = new StringBuilder();
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

                // Handle single tool call (backward compatibility)
                if (t.ToolCall != null)
                {
                    sb.AppendLine("TOOL_CALL:");
                    sb.AppendLine(JsonUtil.ToJson(t.ToolCall));
                }

                if (t.ToolResult != null)
                {
                    sb.AppendLine("TOOL_RESULT:");
                    // Truncate large outputs to prevent prompt bloat
                    var truncatedResult = TruncateToolResultOutput(
                        t.ToolResult,
                        _config.MaxToolOutputSize
                    );
                    sb.AppendLine(JsonUtil.ToJson(truncatedResult));
                }

                // Handle multiple tool calls
                if (t.ToolCalls != null && t.ToolCalls.Count > 0)
                {
                    sb.AppendLine("MULTI_TOOL_CALLS:");
                    foreach (var toolCall in t.ToolCalls)
                    {
                        sb.AppendLine(JsonUtil.ToJson(toolCall));
                    }
                }

                if (t.ToolResults != null && t.ToolResults.Count > 0)
                {
                    sb.AppendLine("MULTI_TOOL_RESULTS:");
                    foreach (var toolResult in t.ToolResults)
                    {
                        var truncatedResult = TruncateToolResultOutput(
                            toolResult,
                            _config.MaxToolOutputSize
                        );
                        sb.AppendLine(JsonUtil.ToJson(truncatedResult));
                    }
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

                // Handle single tool call (backward compatibility)
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

                // Handle multiple tool calls
                if (t.ToolCalls != null && t.ToolCalls.Count > 0)
                {
                    if (summary.Length > 0)
                    {
                        summary.Append(" | ");
                    }
                    var toolNames = string.Join(", ", t.ToolCalls.Select(tc => tc.Tool));
                    summary.Append($"MULTI_TOOLS: {toolNames}");
                }

                if (t.ToolResults != null && t.ToolResults.Count > 0)
                {
                    if (summary.Length > 0)
                    {
                        summary.Append(" | ");
                    }
                    var successCount = t.ToolResults.Count(r => r.Success);
                    var totalCount = t.ToolResults.Count;
                    summary.Append($"MULTI_RESULTS: {successCount}/{totalCount} success");
                }

                sb.AppendLine($"SUMMARY: {summary}");
                sb.AppendLine("---");
            }
        }

        user = new LlmMessage { Role = "user", Content = sb.ToString() };
        messages.Add(user);
    }

    private static string TruncateString(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }
        return input.Length <= maxLength ? input : input.Substring(0, maxLength - 3) + "...";
    }

    private static ToolExecutionResult TruncateToolResultOutput(
        ToolExecutionResult result,
        int maxOutputSize
    )
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
            Output = new
            {
                truncated = true,
                original_size = outputJson.Length,
                preview = outputJson.Substring(0, previewLength) + "..."
            }
        };

        return truncatedResult;
    }
}
