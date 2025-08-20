using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Event arguments for when an LLM call completes.
/// </summary>
[ExcludeFromCodeCoverage]
public class AgentLlmCallCompletedEventArgs : EventArgs
{
    /// <summary>
    ///     Gets or sets the unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the zero-based index of the current turn.
    /// </summary>
    public int TurnIndex { get; set; }

    /// <summary>
    ///     Gets or sets the message from the LLM, if successful.
    /// </summary>
    public ModelMessage? LlmMessage { get; set; }

    /// <summary>
    ///     Gets or sets the streaming chunk from the LLM, if streaming.
    /// </summary>
    public LlmStreamingChunk? StreamingChunk { get; set; }

    /// <summary>
    ///     Gets or sets the error message if the LLM call failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the LLM call completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}