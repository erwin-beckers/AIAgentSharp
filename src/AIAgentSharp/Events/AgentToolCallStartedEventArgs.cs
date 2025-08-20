using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Event arguments for when a tool call starts.
/// </summary>
[ExcludeFromCodeCoverage]
public class AgentToolCallStartedEventArgs : EventArgs
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
    ///     Gets or sets the name of the tool being called.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the parameters being passed to the tool.
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();

    /// <summary>
    ///     Gets or sets the timestamp when the tool call started.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}