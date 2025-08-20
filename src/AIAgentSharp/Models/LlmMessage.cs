using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AIAgentSharp;

/// <summary>
///     Represents a message in the conversation with the LLM.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LlmMessage
{
    /// <summary>
    ///     Gets or sets the role of the message sender (system, user, or assistant).
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}