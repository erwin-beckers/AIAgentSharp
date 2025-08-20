using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
/// Represents a unified request to an LLM that can handle text completion, function calling, and streaming.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LlmRequest
{
    /// <summary>
    /// Gets or sets the conversation messages to send to the LLM.
    /// </summary>
    public IEnumerable<LlmMessage> Messages { get; init; } = Enumerable.Empty<LlmMessage>();

    /// <summary>
    /// Gets or sets the optional function specifications for function calling.
    /// </summary>
    public IEnumerable<FunctionSpec>? Functions { get; init; }

    /// <summary>
    /// Gets or sets the type of response expected from the LLM.
    /// </summary>
    public LlmResponseType ResponseType { get; init; } = LlmResponseType.Text;

    /// <summary>
    /// Gets or sets whether to enable streaming responses.
    /// This is used when ResponseType is Streaming.
    /// </summary>
    public bool EnableStreaming { get; init; } = false;

    /// <summary>
    /// Gets or sets the temperature for response generation (0.0 to 2.0).
    /// Lower values make responses more deterministic, higher values more creative.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Gets or sets the top-p parameter for nucleus sampling (0.0 to 1.0).
    /// </summary>
    public double? TopP { get; init; }

    /// <summary>
    /// Gets or sets the frequency penalty for repetition (-2.0 to 2.0).
    /// </summary>
    public double? FrequencyPenalty { get; init; }

    /// <summary>
    /// Gets or sets the presence penalty for new topics (-2.0 to 2.0).
    /// </summary>
    public double? PresencePenalty { get; init; }

    /// <summary>
    /// Gets or sets the stop sequences that will cause the LLM to stop generating.
    /// </summary>
    public IEnumerable<string>? StopSequences { get; init; }

    /// <summary>
    /// Gets or sets additional model-specific parameters as a dictionary.
    /// </summary>
    public Dictionary<string, object>? AdditionalParameters { get; init; }
}