using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Represents the final result of an agent run, including success status and final output.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AgentResult
{
    /// <summary>
    ///     Gets or sets whether the agent run was successful.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    ///     Gets or sets the final output from the agent.
    /// </summary>
    public string? FinalOutput { get; set; }

    /// <summary>
    ///     Gets or sets the final state of the agent.
    /// </summary>
    public AgentState State { get; set; } = new();

    /// <summary>
    ///     Gets or sets the error message if the agent run failed.
    /// </summary>
    public string? Error { get; set; }
}