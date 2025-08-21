using AIAgentSharp.Agents.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Represents the complete state of an agent, including its goal, conversation history, and metadata.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AgentState
{
    /// <summary>
    ///     Gets or sets the unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the agent's goal or objective.
    /// </summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets additional messages to be included in the conversation context.
    ///     These messages will be added alongside the existing system prompt and goal.
    /// </summary>
    public List<LlmMessage> AdditionalMessages { get; set; } = new();

    /// <summary>
    ///     Gets or sets the list of conversation turns between the agent and the LLM.
    /// </summary>
    public List<AgentTurn> Turns { get; set; } = new();

    /// <summary>
    ///     Gets or sets the timestamp when this state was last updated.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the current reasoning chain if using Chain of Thought reasoning.
    /// </summary>
    public ReasoningChain? CurrentReasoningChain { get; set; }

    /// <summary>
    ///     Gets or sets the current reasoning tree if using Tree of Thoughts reasoning.
    /// </summary>
    public ReasoningTree? CurrentReasoningTree { get; set; }

    /// <summary>
    ///     Gets or sets the type of reasoning being used.
    /// </summary>
    public ReasoningType? ReasoningType { get; set; }

    /// <summary>
    ///     Gets or sets metadata about reasoning activities.
    /// </summary>
    public Dictionary<string, object> ReasoningMetadata { get; set; } = new();
}