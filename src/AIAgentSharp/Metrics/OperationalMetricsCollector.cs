using System.Collections.Concurrent;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Collects and manages operational metrics including success/failure counts,
/// error tracking, and operational health indicators.
/// </summary>
public sealed class OperationalMetricsCollector
{
    private readonly ILogger _logger;
    
    // Failure counters
    private long _failedAgentRuns;
    private long _failedAgentSteps;
    private long _failedLlmCalls;
    private long _failedToolCalls;
    private long _loopDetectionEvents;
    private long _deduplicationCacheHits;
    private long _deduplicationCacheMisses;

    // Error tracking
    private readonly ConcurrentDictionary<string, long> _errorCounts = new();

    public OperationalMetricsCollector(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records agent run completion.
    /// </summary>
    public void RecordAgentRunCompletion(string agentId, bool succeeded, int totalTurns, string? errorType = null)
    {
        try
        {
            if (!succeeded)
            {
                Interlocked.Increment(ref _failedAgentRuns);
                if (!string.IsNullOrEmpty(errorType))
                {
                    _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record agent run completion: {ex.Message}");
        }
    }

    /// <summary>
    /// Records agent step completion.
    /// </summary>
    public void RecordAgentStepCompletion(string agentId, int turnIndex, bool succeeded, bool executedTool, string? errorType = null)
    {
        try
        {
            if (!succeeded)
            {
                Interlocked.Increment(ref _failedAgentSteps);
                if (!string.IsNullOrEmpty(errorType))
                {
                    _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record agent step completion: {ex.Message}");
        }
    }

    /// <summary>
    /// Records LLM call completion.
    /// </summary>
    public void RecordLlmCallCompletion(string agentId, int turnIndex, bool succeeded, string modelName, string? errorType = null)
    {
        try
        {
            if (!succeeded)
            {
                Interlocked.Increment(ref _failedLlmCalls);
                if (!string.IsNullOrEmpty(errorType))
                {
                    _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record LLM call completion: {ex.Message}");
        }
    }

    /// <summary>
    /// Records tool call completion.
    /// </summary>
    public void RecordToolCallCompletion(string agentId, int turnIndex, string toolName, bool succeeded, string? errorType = null)
    {
        try
        {
            if (!succeeded)
            {
                Interlocked.Increment(ref _failedToolCalls);
                if (!string.IsNullOrEmpty(errorType))
                {
                    _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record tool call completion: {ex.Message}");
        }
    }

    /// <summary>
    /// Records loop detection events.
    /// </summary>
    public void RecordLoopDetection(string agentId, string loopType, int consecutiveFailures)
    {
        try
        {
            Interlocked.Increment(ref _loopDetectionEvents);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record loop detection: {ex.Message}");
        }
    }

    /// <summary>
    /// Records deduplication events.
    /// </summary>
    public void RecordDeduplicationEvent(string agentId, string toolName, bool cacheHit)
    {
        try
        {
            if (cacheHit)
            {
                Interlocked.Increment(ref _deduplicationCacheHits);
            }
            else
            {
                Interlocked.Increment(ref _deduplicationCacheMisses);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record deduplication event: {ex.Message}");
        }
    }

    /// <summary>
    /// Records a custom error.
    /// </summary>
    public void RecordError(string agentId, int turnIndex, string errorType, string errorMessage)
    {
        try
        {
            _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record error: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates operational metrics from collected data.
    /// </summary>
    public OperationalMetrics CalculateOperationalMetrics()
    {
        var totalAgentRuns = _failedAgentRuns; // This will be updated by the main collector
        var totalAgentSteps = _failedAgentSteps;
        var totalLlmCalls = _failedLlmCalls;
        var totalToolCalls = _failedToolCalls;

        return new OperationalMetrics
        {
            FailedAgentRuns = _failedAgentRuns,
            FailedAgentSteps = _failedAgentSteps,
            FailedLlmCalls = _failedLlmCalls,
            FailedToolCalls = _failedToolCalls,
            LoopDetectionEvents = _loopDetectionEvents,
            DeduplicationCacheHits = _deduplicationCacheHits,
            DeduplicationCacheMisses = _deduplicationCacheMisses,
            
            AgentRunSuccessRate = totalAgentRuns > 0 ? (double)(totalAgentRuns - _failedAgentRuns) / totalAgentRuns * 100 : 100,
            AgentStepSuccessRate = totalAgentSteps > 0 ? (double)(totalAgentSteps - _failedAgentSteps) / totalAgentSteps * 100 : 100,
            LlmCallSuccessRate = totalLlmCalls > 0 ? (double)(totalLlmCalls - _failedLlmCalls) / totalLlmCalls * 100 : 100,
            ToolCallSuccessRate = totalToolCalls > 0 ? (double)(totalToolCalls - _failedToolCalls) / totalToolCalls * 100 : 100,
            
            DeduplicationCacheHitRate = (_deduplicationCacheHits + _deduplicationCacheMisses) > 0 
                ? (double)_deduplicationCacheHits / (_deduplicationCacheHits + _deduplicationCacheMisses) * 100 
                : 0,
            
            ErrorCounts = new Dictionary<string, long>(_errorCounts)
        };
    }

    /// <summary>
    /// Resets all operational metrics.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _failedAgentRuns, 0);
        Interlocked.Exchange(ref _failedAgentSteps, 0);
        Interlocked.Exchange(ref _failedLlmCalls, 0);
        Interlocked.Exchange(ref _failedToolCalls, 0);
        Interlocked.Exchange(ref _loopDetectionEvents, 0);
        Interlocked.Exchange(ref _deduplicationCacheHits, 0);
        Interlocked.Exchange(ref _deduplicationCacheMisses, 0);

        _errorCounts.Clear();
    }

    /// <summary>
    /// Gets the current error counts.
    /// </summary>
    public IReadOnlyDictionary<string, long> GetErrorCounts()
    {
        return new Dictionary<string, long>(_errorCounts);
    }
}
