using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
/// Represents a unified response from an LLM that can contain text content, function calls, streaming data, and usage metadata.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LlmResponse
{
    /// <summary>
    /// Gets or sets the text content returned by the LLM.
    /// This is populated for text completion responses.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the LLM made a function call.
    /// This is true when the response contains function call data.
    /// </summary>
    public bool HasFunctionCall { get; init; } = false;

    /// <summary>
    /// Gets or sets the function call information if the LLM made a function call.
    /// </summary>
    public LlmFunctionCall? FunctionCall { get; init; }

    /// <summary>
    /// Gets or sets the streaming content as an async enumerable.
    /// This is populated when streaming is enabled.
    /// </summary>
    public IAsyncEnumerable<LlmStreamingChunk>? StreamingContent { get; init; }

    /// <summary>
    /// Gets or sets optional token usage metadata as reported by the provider.
    /// </summary>
    public LlmUsage? Usage { get; init; }

    /// <summary>
    /// Gets or sets the type of response that was actually returned.
    /// This may differ from the requested type if the provider doesn't support certain capabilities.
    /// </summary>
    public LlmResponseType ActualResponseType { get; init; } = LlmResponseType.Text;

    /// <summary>
    /// Gets or sets any additional metadata or provider-specific information.
    /// </summary>
    public Dictionary<string, object>? AdditionalMetadata { get; init; }
}