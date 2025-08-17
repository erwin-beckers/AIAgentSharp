namespace AIAgentSharp;

/// <summary>
///     Defines the interface for storing and retrieving agent state.
/// </summary>
public interface IAgentStateStore
{
    /// <summary>
    ///     Loads the state for a specific agent.
    /// </summary>
    /// <param name="agentId">The unique identifier for the agent.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The agent state, or null if not found.</returns>
    Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default);

    /// <summary>
    ///     Saves the state for a specific agent.
    /// </summary>
    /// <param name="agentId">The unique identifier for the agent.</param>
    /// <param name="state">The agent state to save.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default);
}