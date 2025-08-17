namespace AIAgentSharp;

/// <summary>
///     Represents the result of a single agent step, including continuation status and step details.
/// </summary>
public sealed class AgentStepResult
{
    /// <summary>
    ///     Gets or sets whether the agent should continue to the next step.
    /// </summary>
    public bool Continue { get; set; }

    /// <summary>
    ///     Gets or sets whether a tool was executed in this step.
    /// </summary>
    public bool ExecutedTool { get; set; }

    /// <summary>
    ///     Gets or sets the final output if the agent completed its task.
    /// </summary>
    public string? FinalOutput { get; set; }

    /// <summary>
    ///     Gets or sets the message from the LLM for this step.
    /// </summary>
    public ModelMessage? LlmMessage { get; set; }

    /// <summary>
    ///     Gets or sets the result of tool execution if a tool was called.
    /// </summary>
    public ToolExecutionResult? ToolResult { get; set; }

    /// <summary>
    ///     Gets or sets the current state of the agent after this step.
    /// </summary>
    public AgentState State { get; set; } = new();

    /// <summary>
    ///     Gets or sets the error message if this step failed.
    /// </summary>
    public string? Error { get; set; }
}