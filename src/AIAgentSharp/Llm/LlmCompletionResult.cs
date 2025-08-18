namespace AIAgentSharp;

/// <summary>
/// Represents a provider-agnostic completion result with optional usage metadata.
/// </summary>
public sealed class LlmCompletionResult
{
    /// <summary>
    /// Gets the text content returned by the LLM.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets optional token usage metadata as reported by the provider.
    /// </summary>
    public LlmUsage? Usage { get; init; }
}


