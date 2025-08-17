namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Manages public status updates for UI consumption without exposing internal reasoning.
/// </summary>
public interface IStatusManager
{
    /// <summary>
    /// Emits a status update event with exception safety.
    /// </summary>
    /// <param name="agentId">The agent identifier</param>
    /// <param name="statusTitle">Brief status summary (3-10 words)</param>
    /// <param name="statusDetails">Additional context (≤160 chars)</param>
    /// <param name="nextStepHint">What's next (3-12 words)</param>
    /// <param name="progressPct">Completion percentage (0-100)</param>
    void EmitStatus(string agentId, string statusTitle, string? statusDetails = null, string? nextStepHint = null, int? progressPct = null);
}
