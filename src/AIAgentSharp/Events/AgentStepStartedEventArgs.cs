using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Event arguments for when an agent step starts.
/// </summary>
[ExcludeFromCodeCoverage]
public class AgentStepStartedEventArgs : EventArgs
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
    ///     Gets or sets the timestamp when the step started.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}