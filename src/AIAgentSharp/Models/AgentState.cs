namespace AIAgentSharp;

/// <summary>
///     Represents the complete state of an agent, including its goal, conversation history, and metadata.
/// </summary>
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
    ///     Gets or sets the list of conversation turns between the agent and the LLM.
    /// </summary>
    public List<AgentTurn> Turns { get; set; } = new();

    /// <summary>
    ///     Gets or sets the timestamp when this state was last updated.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}