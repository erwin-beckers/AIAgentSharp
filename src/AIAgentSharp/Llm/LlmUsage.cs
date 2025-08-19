namespace AIAgentSharp;

/// <summary>
/// Represents token usage information returned by an LLM provider for a single call.
/// </summary>
public sealed class LlmUsage
{
    /// <summary>
    /// Gets or sets the number of input/prompt tokens counted by the provider.
    /// </summary>
    public long InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output/completion tokens counted by the provider.
    /// </summary>
    public long OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the model name as reported by the provider (if available).
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical provider name (e.g., "OpenAI", "Anthropic").
    /// </summary>
    public string Provider { get; set; } = string.Empty;
}


