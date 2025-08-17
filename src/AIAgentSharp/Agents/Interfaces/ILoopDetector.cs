namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Detects and prevents infinite loops by tracking tool call history and failure patterns.
/// </summary>
public interface ILoopDetector
{
    /// <summary>
    /// Records a tool call for loop detection purposes.
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="toolName">Name of the tool called</param>
    /// <param name="parameters">Parameters used in the call</param>
    /// <param name="success">Whether the call was successful</param>
    void RecordToolCall(string agentId, string toolName, Dictionary<string, object?> parameters, bool success);

    /// <summary>
    /// Detects if there are repeated failures for the same tool call.
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="toolName">Name of the tool</param>
    /// <param name="parameters">Parameters for the call</param>
    /// <returns>True if repeated failures are detected</returns>
    bool DetectRepeatedFailures(string agentId, string toolName, Dictionary<string, object?> parameters);
}
