namespace AIAgentSharp;

/// <summary>
///     Defines the possible actions an agent can take.
/// </summary>
public enum AgentAction
{
    /// <summary>
    ///     The agent is planning its next steps.
    /// </summary>
    Plan,

    /// <summary>
    ///     The agent is calling a tool.
    /// </summary>
    ToolCall,

    /// <summary>
    ///     The agent is calling multiple tools in sequence.
    /// </summary>
    MultiToolCall,

    /// <summary>
    ///     The agent is finishing its task.
    /// </summary>
    Finish,

    /// <summary>
    ///     The agent is retrying a previous action.
    /// </summary>
    Retry
}