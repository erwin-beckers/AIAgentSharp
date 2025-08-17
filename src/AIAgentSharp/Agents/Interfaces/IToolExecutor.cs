

namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Handles tool execution, including invocation, validation, and error handling.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes a tool with the given parameters.
    /// </summary>
    /// <param name="toolName">Name of the tool to execute</param>
    /// <param name="parameters">Parameters for the tool</param>
    /// <param name="tools">Available tools dictionary</param>
    /// <param name="agentId">Agent identifier for events</param>
    /// <param name="turnIndex">Current turn index for events</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    Task<ToolExecutionResult> ExecuteToolAsync(string toolName, Dictionary<string, object?> parameters, IDictionary<string, ITool> tools, string agentId, int turnIndex, CancellationToken ct);
}
