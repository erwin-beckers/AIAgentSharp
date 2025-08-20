using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AIAgentSharp;

/// <summary>
///     Represents the input data for an agent action, including tool calls and final outputs.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ActionInput
{
    /// <summary>
    ///     Gets or sets the name of the tool to call (for tool_call actions).
    ///     This is maintained for backward compatibility with single tool calls.
    /// </summary>
    [JsonPropertyName("tool")]
    public string? Tool { get; set; }

    /// <summary>
    ///     Gets or sets the parameters for the tool call (for tool_call actions).
    ///     This is maintained for backward compatibility with single tool calls.
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object?>? Params { get; set; }

    /// <summary>
    ///     Gets or sets multiple tool calls to execute in sequence (for multi_tool_call actions).
    ///     This enables the agent to execute multiple tools in a single turn.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }

    /// <summary>
    ///     Gets or sets a summary of the current progress (for plan actions).
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    ///     Gets or sets the final output (for finish actions).
    /// </summary>
    [JsonPropertyName("final")]
    public string? Final { get; set; }
}

/// <summary>
///     Represents a single tool call within a multi-tool call action.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ToolCall
{
    /// <summary>
    ///     Gets or sets the name of the tool to call.
    /// </summary>
    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the parameters for the tool call.
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object?> Params { get; set; } = new();

    /// <summary>
    ///     Gets or sets an optional description of why this tool is being called.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}