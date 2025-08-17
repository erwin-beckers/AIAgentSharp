namespace AIAgentSharp;

/// <summary>
///     Defines the core interface for an agent that can run tasks and execute steps.
/// </summary>
public interface IAgent
{
    /// <summary>
    ///     Runs the agent to completion, executing multiple steps until the goal is achieved or max turns is reached.
    /// </summary>
    /// <param name="agentId">The unique identifier for the agent session.</param>
    /// <param name="goal">The goal or objective for the agent to accomplish.</param>
    /// <param name="tools">The collection of tools available to the agent.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The final result of the agent run.</returns>
    Task<AgentResult> RunAsync(string agentId, string goal, IEnumerable<ITool> tools, CancellationToken ct = default);

    /// <summary>
    ///     Executes a single step of the agent's reasoning and action cycle.
    /// </summary>
    /// <param name="agentId">The unique identifier for the agent session.</param>
    /// <param name="goal">The goal or objective for the agent to accomplish.</param>
    /// <param name="tools">The collection of tools available to the agent.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the single step execution.</returns>
    Task<AgentStepResult> StepAsync(string agentId, string goal, IEnumerable<ITool> tools, CancellationToken ct = default);
}