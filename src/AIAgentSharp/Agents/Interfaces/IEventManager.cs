

namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Manages the emission of agent events for monitoring and integration purposes.
/// </summary>
public interface IEventManager
{
    // Events for real-time monitoring
    event EventHandler<AgentRunStartedEventArgs>? RunStarted;
    event EventHandler<AgentStepStartedEventArgs>? StepStarted;
    event EventHandler<AgentLlmCallStartedEventArgs>? LlmCallStarted;
    event EventHandler<AgentLlmCallCompletedEventArgs>? LlmCallCompleted;
    event EventHandler<AgentToolCallStartedEventArgs>? ToolCallStarted;
    event EventHandler<AgentToolCallCompletedEventArgs>? ToolCallCompleted;
    event EventHandler<AgentStepCompletedEventArgs>? StepCompleted;
    event EventHandler<AgentRunCompletedEventArgs>? RunCompleted;
    event EventHandler<AgentStatusEventArgs>? StatusUpdate;

    // Event raising methods
    void RaiseRunStarted(string agentId, string goal);
    void RaiseStepStarted(string agentId, int turnIndex);
    void RaiseLlmCallStarted(string agentId, int turnIndex);
    void RaiseLlmCallCompleted(string agentId, int turnIndex, ModelMessage? llmMessage, string? error = null);
    void RaiseToolCallStarted(string agentId, int turnIndex, string toolName, Dictionary<string, object?> parameters);
    void RaiseToolCallCompleted(string agentId, int turnIndex, string toolName, bool success, object? output = null, string? error = null, TimeSpan? executionTime = null);
    void RaiseStepCompleted(string agentId, int turnIndex, AgentStepResult stepResult);
    void RaiseRunCompleted(string agentId, bool succeeded, string? finalOutput, string? error, int totalTurns);
    void RaiseStatusUpdate(string agentId, string statusTitle, string? statusDetails = null, string? nextStepHint = null, int? progressPct = null);
}
