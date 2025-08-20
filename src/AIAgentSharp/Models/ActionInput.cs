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
    /// </summary>
    [JsonPropertyName("tool")]
    public string? Tool { get; set; }

    /// <summary>
    ///     Gets or sets the parameters for the tool call (for tool_call actions).
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object?>? Params { get; set; }

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