namespace AIAgentSharp;

/// <summary>
///     An in-memory implementation of IAgentStateStore that stores agent states in a dictionary.
///     This implementation is thread-safe and suitable for testing or single-instance deployments.
/// </summary>
public sealed class MemoryAgentStateStore : IAgentStateStore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, AgentState> _states = new();

    /// <summary>
    ///     Loads the state for a specific agent from memory.
    /// </summary>
    /// <param name="agentId">The unique identifier for the agent.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The agent state, or null if not found.</returns>
    public Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_states.TryGetValue(agentId, out var state) ? state : null);
        }
    }

    /// <summary>
    ///     Saves the state for a specific agent to memory.
    /// </summary>
    /// <param name="agentId">The unique identifier for the agent.</param>
    /// <param name="state">The agent state to save.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    public Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _states[agentId] = state;
        }
        return Task.CompletedTask;
    }
}