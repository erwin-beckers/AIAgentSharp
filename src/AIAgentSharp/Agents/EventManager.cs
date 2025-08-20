using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// Manages the emission of agent events for monitoring and integration purposes.
/// </summary>
public sealed class EventManager : IEventManager
{
    private readonly ILogger _logger;

    public EventManager(ILogger logger)
    {
        _logger = logger;
    }

    // Events for real-time monitoring
    public event EventHandler<AgentRunStartedEventArgs>? RunStarted;
    public event EventHandler<AgentStepStartedEventArgs>? StepStarted;
    public event EventHandler<AgentLlmCallStartedEventArgs>? LlmCallStarted;
    public event EventHandler<AgentLlmCallCompletedEventArgs>? LlmCallCompleted;
    public event EventHandler<AgentLlmChunkReceivedEventArgs>? LlmChunkReceived;
    public event EventHandler<AgentToolCallStartedEventArgs>? ToolCallStarted;
    public event EventHandler<AgentToolCallCompletedEventArgs>? ToolCallCompleted;
    public event EventHandler<AgentStepCompletedEventArgs>? StepCompleted;
    public event EventHandler<AgentRunCompletedEventArgs>? RunCompleted;
    public event EventHandler<AgentStatusEventArgs>? StatusUpdate;

    public void RaiseRunStarted(string agentId, string goal)
    {
        try
        {
            RunStarted?.Invoke(this, new AgentRunStartedEventArgs
            {
                AgentId = agentId,
                Goal = goal
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"RunStarted event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseStepStarted(string agentId, int turnIndex)
    {
        try
        {
            StepStarted?.Invoke(this, new AgentStepStartedEventArgs
            {
                AgentId = agentId,
                TurnIndex = turnIndex
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"StepStarted event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseLlmCallStarted(string agentId, int turnIndex)
    {
        try
        {
            LlmCallStarted?.Invoke(this, new AgentLlmCallStartedEventArgs
            {
                AgentId = agentId,
                TurnIndex = turnIndex
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"LlmCallStarted event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseLlmCallCompleted(string agentId, int turnIndex, ModelMessage? llmMessage, string? error = null)
    {
        try
        {
            LlmCallCompleted?.Invoke(this, new AgentLlmCallCompletedEventArgs
            {
                AgentId = agentId,
                TurnIndex = turnIndex,
                LlmMessage = llmMessage,
                Error = error
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"LlmCallCompleted event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseLlmChunkReceived(string agentId, int turnIndex, LlmStreamingChunk chunk)
    {
        try
        {
            LlmChunkReceived?.Invoke(this, new AgentLlmChunkReceivedEventArgs
            {
                AgentId = agentId,
                TurnIndex = turnIndex,
                Chunk = chunk
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"LlmChunkReceived event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseToolCallStarted(string agentId, int turnIndex, string toolName, Dictionary<string, object?> parameters)
    {
        try
        {
            ToolCallStarted?.Invoke(this, new AgentToolCallStartedEventArgs
            {
                AgentId = agentId,
                TurnIndex = turnIndex,
                ToolName = toolName,
                Parameters = parameters
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"ToolCallStarted event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseToolCallCompleted(string agentId, int turnIndex, string toolName, bool success, object? output = null, string? error = null, TimeSpan? executionTime = null)
    {
        try
        {
            ToolCallCompleted?.Invoke(this, new AgentToolCallCompletedEventArgs
            {
                AgentId = agentId,
                TurnIndex = turnIndex,
                ToolName = toolName,
                Success = success,
                Output = output,
                Error = error,
                ExecutionTime = executionTime ?? TimeSpan.Zero
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"ToolCallCompleted event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseStepCompleted(string agentId, int turnIndex, AgentStepResult stepResult)
    {
        try
        {
            StepCompleted?.Invoke(this, new AgentStepCompletedEventArgs
            {
                AgentId = agentId,
                TurnIndex = turnIndex,
                Continue = stepResult.Continue,
                ExecutedTool = stepResult.ExecutedTool,
                FinalOutput = stepResult.FinalOutput,
                Error = stepResult.Error,
                MultiToolResults = stepResult.MultiToolResults
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"StepCompleted event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseRunCompleted(string agentId, bool succeeded, string? finalOutput, string? error, int totalTurns)
    {
        try
        {
            RunCompleted?.Invoke(this, new AgentRunCompletedEventArgs
            {
                AgentId = agentId,
                Succeeded = succeeded,
                FinalOutput = finalOutput,
                Error = error,
                TotalTurns = totalTurns
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"RunCompleted event handler threw exception: {ex.Message}");
        }
    }

    public void RaiseStatusUpdate(string agentId, string statusTitle, string? statusDetails = null, string? nextStepHint = null, int? progressPct = null)
    {
        try
        {
            StatusUpdate?.Invoke(this, new AgentStatusEventArgs
            {
                AgentId = agentId,
                TurnIndex = 0, // Will be updated by the caller if needed
                StatusTitle = statusTitle,
                StatusDetails = statusDetails,
                NextStepHint = nextStepHint,
                ProgressPct = progressPct
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"StatusUpdate event handler threw exception: {ex.Message}");
        }
    }
}
