using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// Manages public status updates for UI consumption without exposing internal reasoning.
/// </summary>
public sealed class StatusManager : IStatusManager
{
    private readonly AgentConfiguration _config;
    private readonly IEventManager _eventManager;
    private readonly ILogger _logger;

    public StatusManager(AgentConfiguration config, IEventManager eventManager, ILogger? logger = null)
    {
        _config = config;
        _eventManager = eventManager;
        _logger = logger ?? new ConsoleLogger();
    }

    /// <summary>
    /// Emits a status update event with exception safety.
    /// Never throws exceptions from event emission.
    /// </summary>
    public void EmitStatus(string agentId, string statusTitle, string? statusDetails = null, string? nextStepHint = null, int? progressPct = null)
    {
        if (!_config.EmitPublicStatus)
        {
            return;
        }

        _eventManager.RaiseStatusUpdate(agentId, statusTitle, statusDetails, nextStepHint, progressPct);
    }
}
