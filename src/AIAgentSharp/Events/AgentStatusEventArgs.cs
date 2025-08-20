using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Event arguments for the StatusUpdate event containing public status information.
/// </summary>
[ExcludeFromCodeCoverage]
public class AgentStatusEventArgs : EventArgs
{
    /// <summary>
    ///     The ID of the agent that emitted the status update.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    ///     The current turn index when the status was emitted.
    /// </summary>
    public int TurnIndex { get; set; }

    /// <summary>
    ///     Brief status summary (3-10 words, ≤60 chars).
    /// </summary>
    public string StatusTitle { get; set; } = string.Empty;

    /// <summary>
    ///     Additional context details (≤160 chars).
    /// </summary>
    public string? StatusDetails { get; set; }

    /// <summary>
    ///     Hint about what the agent will do next (3-12 words, ≤60 chars).
    /// </summary>
    public string? NextStepHint { get; set; }

    /// <summary>
    ///     Completion percentage (0-100).
    /// </summary>
    public int? ProgressPct { get; set; }

    /// <summary>
    ///     Timestamp when the status was emitted.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}