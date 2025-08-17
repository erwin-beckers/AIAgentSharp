namespace AIAgentSharp;

/// <summary>
///     Represents a single turn in the agent's conversation, including LLM messages, tool calls, and results.
/// </summary>
public sealed class AgentTurn
{
    /// <summary>
    ///     Gets or sets the zero-based index of this turn in the conversation.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    ///     Gets or sets the message from the LLM for this turn.
    /// </summary>
    public ModelMessage? LlmMessage { get; set; }

    /// <summary>
    ///     Gets or sets the tool call request made by the agent.
    /// </summary>
    public ToolCallRequest? ToolCall { get; set; }

    /// <summary>
    ///     Gets or sets the result of the tool execution.
    /// </summary>
    public ToolExecutionResult? ToolResult { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier for this turn, used for idempotency.
    /// </summary>
    public string TurnId { get; set; } = string.Empty;
}