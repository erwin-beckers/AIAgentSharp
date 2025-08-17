namespace AIAgentSharp;

/// <summary>
///     Event arguments for when a tool call completes.
/// </summary>
public class AgentToolCallCompletedEventArgs : EventArgs
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
    ///     Gets or sets the name of the tool that was called.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets whether the tool call was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the output from the tool execution.
    /// </summary>
    public object? Output { get; set; }

    /// <summary>
    ///     Gets or sets the error message if the tool call failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    ///     Gets or sets the time taken to execute the tool.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the tool call completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the list of missing required parameters, if any.
    /// </summary>
    public List<string>? Missing { get; set; }

    /// <summary>
    ///     Gets or sets the list of validation errors, if any.
    /// </summary>
    public List<string>? Errors { get; set; }
}