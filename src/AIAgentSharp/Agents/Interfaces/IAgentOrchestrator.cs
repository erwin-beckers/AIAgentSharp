

namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Orchestrates the execution of individual agent steps, coordinating between
/// LLM communication, tool execution, and state management.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Executes a single step in the agent's reasoning and action cycle.
    /// </summary>
    /// <param name="state">The current agent state</param>
    /// <param name="tools">Available tools for the agent to use</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result of the step execution</returns>
    Task<AgentStepResult> ExecuteStepAsync(AgentState state, IDictionary<string, ITool> tools, CancellationToken ct);
}
