using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Event arguments for when an agent run starts.
/// </summary>
[ExcludeFromCodeCoverage]
public class AgentRunStartedEventArgs : EventArgs
{
    /// <summary>
    ///     Gets or sets the unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the goal or objective for the agent run.
    /// </summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the timestamp when the run started.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}