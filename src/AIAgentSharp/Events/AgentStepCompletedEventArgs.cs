using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Event arguments for when an agent step completes.
/// </summary>
[ExcludeFromCodeCoverage]
public class AgentStepCompletedEventArgs : EventArgs
{
    /// <summary>
    ///     Gets or sets the unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the zero-based index of the current turn.
    /// </summary>
    public int TurnIndex { get; set; }

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
    ///     Gets or sets the error message if this step failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the step completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}