namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Interface for reasoning manager that coordinates reasoning activities.
/// </summary>
public interface IReasoningManager
{
    /// <summary>
    /// Performs reasoning using the configured reasoning type.
    /// </summary>
    /// <param name="goal">The goal to reason about.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="tools">Available tools for the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reasoning result.</returns>
    Task<ReasoningResult> ReasonAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs reasoning using a specific reasoning type.
    /// </summary>
    /// <param name="reasoningType">The reasoning type to use.</param>
    /// <param name="goal">The goal to reason about.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="tools">Available tools for the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reasoning result.</returns>
    Task<ReasoningResult> ReasonAsync(ReasoningType reasoningType, string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default);
}
