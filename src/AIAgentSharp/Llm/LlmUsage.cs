namespace AIAgentSharp;

/// <summary>
/// Represents token usage information returned by an LLM provider for a single call.
/// </summary>
public sealed class LlmUsage
{
    /// <summary>
    /// Gets the number of input/prompt tokens counted by the provider.
    /// </summary>
    public long InputTokens { get; init; }

    /// <summary>
    /// Gets the number of output/completion tokens counted by the provider.
    /// </summary>
    public long OutputTokens { get; init; }

    /// <summary>
    /// Gets the model name as reported by the provider (if available).
    /// </summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Gets the logical provider name (e.g., "OpenAI", "Anthropic").
    /// </summary>
    public string Provider { get; init; } = string.Empty;
}


