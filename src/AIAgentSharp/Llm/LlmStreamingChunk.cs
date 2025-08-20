using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
/// Represents a chunk of streaming content from the LLM.
/// This is the primary response type for all LLM interactions.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LlmStreamingChunk
{
    /// <summary>
    /// Gets or sets the content of this streaming chunk.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the final chunk in the stream.
    /// </summary>
    public bool IsFinal { get; init; } = false;

    /// <summary>
    /// Gets or sets the finish reason for the stream (if this is the final chunk).
    /// </summary>
    public string? FinishReason { get; init; }

    /// <summary>
    /// Gets or sets any function call data in this chunk (if applicable).
    /// </summary>
    public LlmFunctionCall? FunctionCall { get; set; }

    /// <summary>
    /// Gets or sets optional token usage metadata as reported by the provider.
    /// This is typically only present in the final chunk.
    /// </summary>
    public LlmUsage? Usage { get; set; }

    /// <summary>
    /// Gets or sets the type of response that was actually returned.
    /// This may differ from the requested type if the provider doesn't support certain capabilities.
    /// </summary>
    public LlmResponseType ActualResponseType { get; set; } = LlmResponseType.Text;

    /// <summary>
    /// Gets or sets additional metadata for this chunk.
    /// </summary>
    public Dictionary<string, object>? AdditionalMetadata { get; init; }
}