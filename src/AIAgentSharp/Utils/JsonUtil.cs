using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIAgentSharp;

/// <summary>
///     Provides utility methods for JSON serialization and parsing with strict validation.
/// </summary>
public static class JsonUtil
{
    /// <summary>
    ///     Gets the JSON serializer options used throughout the agent framework.
    ///     Configured for camelCase naming, snake_case enums, and strict parsing.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    /// <summary>
    ///     Parses a JSON string into a ModelMessage with strict validation and optional size limits.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="config">Optional configuration for applying size limits to fields.</param>
    /// <returns>A validated ModelMessage object.</returns>
    /// <exception cref="ArgumentException">Thrown when the JSON is invalid or required fields are missing.</exception>
    public static ModelMessage ParseStrict(string json, AgentConfiguration? config = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("JSON must be an object");
        }

        var thoughts = root.GetProperty("thoughts").GetString() ?? string.Empty;
        var actionRaw = root.GetProperty("action").GetString() ?? string.Empty;
        var actionInput = root.GetProperty("action_input");

        // Apply size limits if config is provided
        if (config != null)
        {
            if (thoughts.Length > config.MaxThoughtsLength)
            {
                throw new ArgumentException($"'thoughts' field exceeds maximum length of {config.MaxThoughtsLength} characters");
            }
        }

        // Use JSON converter to parse the enum with snake_case support
        AgentAction action;

        try
        {
            action = JsonSerializer.Deserialize<AgentAction>($"\"{actionRaw}\"", JsonOptions);
        }
        catch
        {
            throw new ArgumentException($"Invalid action: {actionRaw}");
        }

        var result = new ModelMessage
        {
            Thoughts = thoughts,
            Action = action,
            ActionRaw = actionRaw,
            ActionInput = new ActionInput()
        };

        if (actionInput.TryGetProperty("tool", out var toolProp))
        {
            result.ActionInput.Tool = toolProp.GetString();
        }

        if (actionInput.TryGetProperty("params", out var paramsProp))
        {
            result.ActionInput.Params = JsonSerializer.Deserialize<Dictionary<string, object?>>(paramsProp.GetRawText(), JsonOptions);
        }

        if (actionInput.TryGetProperty("summary", out var summaryProp))
        {
            var summary = summaryProp.GetString();

            if (config != null && summary != null && summary.Length > config.MaxSummaryLength)
            {
                throw new ArgumentException($"'summary' field exceeds maximum length of {config.MaxSummaryLength} characters");
            }
            result.ActionInput.Summary = summary;
        }

        if (actionInput.TryGetProperty("final", out var finalProp))
        {
            var final = finalProp.GetString();

            if (config != null && final != null && final.Length > config.MaxFinalLength)
            {
                throw new ArgumentException($"'final' field exceeds maximum length of {config.MaxFinalLength} characters");
            }
            result.ActionInput.Final = final;
        }

        // Parse optional public status fields with max-length guards
        if (root.TryGetProperty("status_title", out var statusTitleProp))
        {
            var statusTitle = statusTitleProp.GetString();

            if (statusTitle != null)
            {
                if (statusTitle.Length > 60)
                {
                    statusTitle = statusTitle.Substring(0, 60);
                }
                result.StatusTitle = statusTitle;
            }
        }

        if (root.TryGetProperty("status_details", out var statusDetailsProp))
        {
            var statusDetails = statusDetailsProp.GetString();

            if (statusDetails != null)
            {
                if (statusDetails.Length > 160)
                {
                    statusDetails = statusDetails.Substring(0, 160);
                }
                result.StatusDetails = statusDetails;
            }
        }

        if (root.TryGetProperty("next_step_hint", out var nextStepHintProp))
        {
            var nextStepHint = nextStepHintProp.GetString();

            if (nextStepHint != null)
            {
                if (nextStepHint.Length > 60)
                {
                    nextStepHint = nextStepHint.Substring(0, 60);
                }
                result.NextStepHint = nextStepHint;
            }
        }

        if (root.TryGetProperty("progress_pct", out var progressPctProp))
        {
            if (progressPctProp.ValueKind == JsonValueKind.Number)
            {
                var progressPct = progressPctProp.GetInt32();

                if (progressPct >= 0 && progressPct <= 100)
                {
                    result.ProgressPct = progressPct;
                }
            }
        }

        // Enforce action-specific validation
        if (string.IsNullOrWhiteSpace(result.Thoughts))
        {
            throw new ArgumentException("Missing 'thoughts'.");
        }

        switch (result.Action)
        {
            case AgentAction.ToolCall:
                if (string.IsNullOrWhiteSpace(result.ActionInput.Tool))
                {
                    throw new ArgumentException("tool_call requires action_input.tool");
                }
                // Ensure params exists (empty object is fine)
                result.ActionInput.Params ??= new Dictionary<string, object?>();
                break;
            case AgentAction.Finish:
                if (string.IsNullOrWhiteSpace(result.ActionInput.Final))
                {
                    throw new ArgumentException("finish requires action_input.final");
                }
                break;
        }

        return result;
    }

    /// <summary>
    ///     Serializes an object to JSON using the framework's standard options.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    public static string ToJson(object obj)
    {
        return JsonSerializer.Serialize(obj, JsonOptions);
    }
}