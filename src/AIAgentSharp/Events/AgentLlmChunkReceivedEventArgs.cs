namespace AIAgentSharp;

/// <summary>
///     Event arguments for when an LLM chunk is received during streaming.
/// </summary>
public class AgentLlmChunkReceivedEventArgs : EventArgs
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
    ///     Gets or sets the streaming chunk from the LLM.
    /// </summary>
    public LlmStreamingChunk Chunk { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the timestamp when the chunk was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
