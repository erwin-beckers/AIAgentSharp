using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Represents a single turn in the agent's conversation, including LLM messages, tool calls, and results.
/// </summary>
[ExcludeFromCodeCoverage]
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
    ///     This is maintained for backward compatibility with single tool calls.
    /// </summary>
    public ToolCallRequest? ToolCall { get; set; }

    /// <summary>
    ///     Gets or sets the result of the tool execution.
    ///     This is maintained for backward compatibility with single tool calls.
    /// </summary>
    public ToolExecutionResult? ToolResult { get; set; }

    /// <summary>
    ///     Gets or sets multiple tool call requests made by the agent.
    ///     This enables support for multiple tool calls in a single turn.
    /// </summary>
    public List<ToolCallRequest>? ToolCalls { get; set; }

    /// <summary>
    ///     Gets or sets the results of multiple tool executions.
    ///     This enables support for multiple tool results in a single turn.
    /// </summary>
    public List<ToolExecutionResult>? ToolResults { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier for this turn, used for idempotency.
    /// </summary>
    public string TurnId { get; set; } = string.Empty;
}