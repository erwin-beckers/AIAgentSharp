namespace AIAgentSharp;

/// <summary>
///     Event arguments for when an agent run completes.
/// </summary>
public class AgentRunCompletedEventArgs : EventArgs
{
    /// <summary>
    ///     Gets or sets the unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets whether the agent run was successful.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    ///     Gets or sets the final output from the agent.
    /// </summary>
    public string? FinalOutput { get; set; }

    /// <summary>
    ///     Gets or sets the error message if the agent run failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    ///     Gets or sets the total number of turns executed.
    /// </summary>
    public int TotalTurns { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the run completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}