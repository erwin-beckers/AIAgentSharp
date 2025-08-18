using System.Collections.Concurrent;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Tracks metrics for a specific agent, providing detailed performance and operational data.
/// </summary>
/// <remarks>
/// <para>
/// AgentMetrics maintains comprehensive metrics for a single agent, including performance
/// data, operational statistics, quality metrics, and resource usage. This class is
/// designed to be thread-safe and efficient for concurrent access.
/// </para>
/// <para>
/// The metrics are organized into the same categories as the global metrics, allowing
/// for consistent analysis and reporting across different levels of granularity.
/// </para>
/// </remarks>
internal sealed class AgentMetrics
{
    private readonly string _agentId;
    private readonly ConcurrentQueue<ExecutionTimeRecord> _executionTimes = new();
    private readonly ConcurrentQueue<ConfidenceRecord> _confidenceScores = new();
    private readonly ConcurrentDictionary<string, TokenUsage> _tokenUsageByModel = new();
    private readonly ConcurrentDictionary<string, long> _errorCounts = new();

    // Performance counters
    private long _totalRuns;
    private long _totalSteps;
    private long _totalLlmCalls;
    private long _totalToolCalls;
    private long _totalReasoningOperations;
    private long _failedRuns;
    private long _failedSteps;
    private long _failedLlmCalls;
    private long _failedToolCalls;
    private long _loopDetectionEvents;
    private long _deduplicationCacheHits;
    private long _deduplicationCacheMisses;
    private long _totalInputTokens;
    private long _totalOutputTokens;
    private long _totalStateStoreOperations;
    private long _totalStateStoreOperationTimeMs;

    // Quality metrics
    private long _totalResponseLength;
    private long _responsesWithFinalOutput;
    private long _totalResponses;

    public AgentMetrics(string agentId)
    {
        _agentId = agentId;
    }

    public void RecordRunExecutionTime(long executionTimeMs, int totalTurns)
    {
        Interlocked.Increment(ref _totalRuns);
        _executionTimes.Enqueue(new ExecutionTimeRecord("AgentRun", _agentId, executionTimeMs));
        
        // Keep only the last 1000 records
        while (_executionTimes.Count > 1000)
        {
            _executionTimes.TryDequeue(out _);
        }
    }

    public void RecordStepExecutionTime(int turnIndex, long executionTimeMs)
    {
        Interlocked.Increment(ref _totalSteps);
        _executionTimes.Enqueue(new ExecutionTimeRecord("AgentStep", _agentId, executionTimeMs));
        
        // Keep only the last 1000 records
        while (_executionTimes.Count > 1000)
        {
            _executionTimes.TryDequeue(out _);
        }
    }

    public void RecordLlmCallExecutionTime(int turnIndex, long executionTimeMs, string modelName)
    {
        Interlocked.Increment(ref _totalLlmCalls);
        _executionTimes.Enqueue(new ExecutionTimeRecord("LlmCall", _agentId, executionTimeMs));
        
        // Keep only the last 1000 records
        while (_executionTimes.Count > 1000)
        {
            _executionTimes.TryDequeue(out _);
        }
    }

    public void RecordToolCallExecutionTime(int turnIndex, string toolName, long executionTimeMs)
    {
        Interlocked.Increment(ref _totalToolCalls);
        _executionTimes.Enqueue(new ExecutionTimeRecord("ToolCall", _agentId, executionTimeMs));
        
        // Keep only the last 1000 records
        while (_executionTimes.Count > 1000)
        {
            _executionTimes.TryDequeue(out _);
        }
    }

    public void RecordReasoningExecutionTime(ReasoningType reasoningType, long executionTimeMs)
    {
        Interlocked.Increment(ref _totalReasoningOperations);
        _executionTimes.Enqueue(new ExecutionTimeRecord("Reasoning", _agentId, executionTimeMs));
        
        // Keep only the last 1000 records
        while (_executionTimes.Count > 1000)
        {
            _executionTimes.TryDequeue(out _);
        }
    }

    public void RecordRunCompletion(bool succeeded, int totalTurns, string? errorType)
    {
        if (!succeeded)
        {
            Interlocked.Increment(ref _failedRuns);
            if (!string.IsNullOrEmpty(errorType))
            {
                _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
            }
        }
    }

    public void RecordStepCompletion(int turnIndex, bool succeeded, bool executedTool, string? errorType)
    {
        if (!succeeded)
        {
            Interlocked.Increment(ref _failedSteps);
            if (!string.IsNullOrEmpty(errorType))
            {
                _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
            }
        }
    }

    public void RecordLlmCallCompletion(int turnIndex, bool succeeded, string modelName, string? errorType)
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

    public void RecordToolCallCompletion(int turnIndex, string toolName, bool succeeded, string? errorType)
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

    public void RecordLoopDetection(string loopType, int consecutiveFailures)
    {
        Interlocked.Increment(ref _loopDetectionEvents);
    }

    public void RecordDeduplicationEvent(string toolName, bool cacheHit)
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

    public void RecordReasoningConfidence(ReasoningType reasoningType, double confidenceScore)
    {
        _confidenceScores.Enqueue(new ConfidenceRecord(_agentId, reasoningType, confidenceScore));
        
        // Keep only the last 1000 records
        while (_confidenceScores.Count > 1000)
        {
            _confidenceScores.TryDequeue(out _);
        }
    }

    public void RecordResponseQuality(int responseLength, bool hasFinalOutput)
    {
        Interlocked.Add(ref _totalResponseLength, responseLength);
        Interlocked.Increment(ref _totalResponses);
        
        if (hasFinalOutput)
        {
            Interlocked.Increment(ref _responsesWithFinalOutput);
        }
    }

    public void RecordTokenUsage(int turnIndex, int inputTokens, int outputTokens, string modelName)
    {
        Interlocked.Add(ref _totalInputTokens, inputTokens);
        Interlocked.Add(ref _totalOutputTokens, outputTokens);

        var tokenUsage = _tokenUsageByModel.GetOrAdd(modelName, _ => new TokenUsage());
        tokenUsage.AddTokens(inputTokens, outputTokens);
    }

    public void RecordApiCall(string apiType, string modelName)
    {
        // This is tracked at the global level, but we could add agent-specific tracking here if needed
    }

    public void RecordStateStoreOperation(string operationType, long executionTimeMs)
    {
        Interlocked.Increment(ref _totalStateStoreOperations);
        Interlocked.Add(ref _totalStateStoreOperationTimeMs, executionTimeMs);
    }

    public PerformanceMetrics CalculatePerformanceMetrics()
    {
        var records = _executionTimes.ToList();
        
        var agentRunTimes = records.Where(r => r.Type == "AgentRun").Select(r => r.ExecutionTimeMs).ToList();
        var llmCallTimes = records.Where(r => r.Type == "LlmCall").Select(r => r.ExecutionTimeMs).ToList();
        var toolCallTimes = records.Where(r => r.Type == "ToolCall").Select(r => r.ExecutionTimeMs).ToList();

        return new PerformanceMetrics
        {
            AverageAgentRunTimeMs = agentRunTimes.Any() ? agentRunTimes.Average() : 0,
            AverageAgentStepTimeMs = records.Where(r => r.Type == "AgentStep").Select(r => r.ExecutionTimeMs).DefaultIfEmpty(0).Average(),
            AverageLlmCallTimeMs = llmCallTimes.Any() ? llmCallTimes.Average() : 0,
            AverageToolCallTimeMs = toolCallTimes.Any() ? toolCallTimes.Average() : 0,
            AverageReasoningTimeMs = records.Where(r => r.Type == "Reasoning").Select(r => r.ExecutionTimeMs).DefaultIfEmpty(0).Average(),
            TotalAgentRuns = _totalRuns,
            TotalAgentSteps = _totalSteps,
            TotalLlmCalls = _totalLlmCalls,
            TotalToolCalls = _totalToolCalls,
            TotalReasoningOperations = _totalReasoningOperations,
            RequestsPerSecond = 0, // Would need time-based tracking
            P95AgentRunTimeMs = CalculatePercentile(agentRunTimes, 95),
            P95LlmCallTimeMs = CalculatePercentile(llmCallTimes, 95),
            P95ToolCallTimeMs = CalculatePercentile(toolCallTimes, 95)
        };
    }

    public OperationalMetrics CalculateOperationalMetrics()
    {
        var totalDeduplicationCalls = _deduplicationCacheHits + _deduplicationCacheMisses;

        return new OperationalMetrics
        {
            AgentRunSuccessRate = _totalRuns > 0 ? (double)(_totalRuns - _failedRuns) / _totalRuns : 0,
            AgentStepSuccessRate = _totalSteps > 0 ? (double)(_totalSteps - _failedSteps) / _totalSteps : 0,
            LlmCallSuccessRate = _totalLlmCalls > 0 ? (double)(_totalLlmCalls - _failedLlmCalls) / _totalLlmCalls : 0,
            ToolCallSuccessRate = _totalToolCalls > 0 ? (double)(_totalToolCalls - _failedToolCalls) / _totalToolCalls : 0,
            FailedAgentRuns = _failedRuns,
            FailedAgentSteps = _failedSteps,
            FailedLlmCalls = _failedLlmCalls,
            FailedToolCalls = _failedToolCalls,
            LoopDetectionEvents = _loopDetectionEvents,
            DeduplicationCacheHits = _deduplicationCacheHits,
            DeduplicationCacheMisses = _deduplicationCacheMisses,
            DeduplicationCacheHitRate = totalDeduplicationCalls > 0 ? (double)_deduplicationCacheHits / totalDeduplicationCalls : 0,
            ErrorCountsByType = new Dictionary<string, long>(_errorCounts)
        };
    }

    public QualityMetrics CalculateQualityMetrics()
    {
        var confidenceScores = _confidenceScores.Select(r => r.ConfidenceScore).ToList();

        var confidenceByType = _confidenceScores
            .GroupBy(r => r.ReasoningType)
            .ToDictionary(g => g.Key, g => g.Average(r => r.ConfidenceScore));

        var confidenceDistribution = confidenceScores
            .GroupBy(score => GetConfidenceBucket(score))
            .ToDictionary(g => g.Key, g => (long)g.Count());

        return new QualityMetrics
        {
            AverageReasoningConfidence = confidenceScores.Any() ? confidenceScores.Average() : 0,
            AverageResponseLength = _totalResponses > 0 ? (double)_totalResponseLength / _totalResponses : 0,
            FinalOutputPercentage = _totalResponses > 0 ? (double)_responsesWithFinalOutput / _totalResponses : 0,
            AverageConfidenceByReasoningType = confidenceByType,
            ConfidenceScoreDistribution = confidenceDistribution
        };
    }

    public ResourceMetrics CalculateResourceMetrics()
    {
        var totalLlmCalls = _totalLlmCalls;
        var totalStateStoreOperations = _totalStateStoreOperations;

        return new ResourceMetrics
        {
            TotalInputTokens = _totalInputTokens,
            TotalOutputTokens = _totalOutputTokens,
            AverageInputTokensPerCall = totalLlmCalls > 0 ? (double)_totalInputTokens / totalLlmCalls : 0,
            AverageOutputTokensPerCall = totalLlmCalls > 0 ? (double)_totalOutputTokens / totalLlmCalls : 0,
            TokenUsageByModel = new Dictionary<string, TokenUsage>(_tokenUsageByModel),
            ApiCallCountsByType = new Dictionary<string, long>(),
            ApiCallCountsByModel = new Dictionary<string, long>(),
            StateStoreOperationCounts = new Dictionary<string, long>(),
            AverageStateStoreOperationTimeMs = totalStateStoreOperations > 0 ? (double)_totalStateStoreOperationTimeMs / totalStateStoreOperations : 0
        };
    }

    private double CalculatePercentile(List<long> values, int percentile)
    {
        if (!values.Any()) return 0;
        
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
        return index >= 0 && index < sorted.Count ? sorted[index] : 0;
    }

    private string GetConfidenceBucket(double confidence)
    {
        return confidence switch
        {
            >= 0.9 => "0.9-1.0",
            >= 0.8 => "0.8-0.9",
            >= 0.7 => "0.7-0.8",
            >= 0.6 => "0.6-0.7",
            >= 0.5 => "0.5-0.6",
            _ => "0.0-0.5"
        };
    }

    // Helper classes for internal data storage

    /// <summary>
    /// Internal record for storing execution time data for performance analysis.
    /// </summary>
    /// <remarks>
    /// This class is used internally to track execution times for various operations
    /// and is stored in a thread-safe queue for percentile calculations.
    /// </remarks>
    private sealed class ExecutionTimeRecord
    {
        /// <summary>
        /// Gets the type of operation (AgentRun, AgentStep, LlmCall, ToolCall, Reasoning).
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the unique identifier of the agent that performed the operation.
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// Gets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; }

        /// <summary>
        /// Gets the timestamp when this record was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionTimeRecord"/> class.
        /// </summary>
        /// <param name="type">The type of operation.</param>
        /// <param name="agentId">The unique identifier of the agent.</param>
        /// <param name="executionTimeMs">The execution time in milliseconds.</param>
        public ExecutionTimeRecord(string type, string agentId, long executionTimeMs)
        {
            Type = type;
            AgentId = agentId;
            ExecutionTimeMs = executionTimeMs;
            Timestamp = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Internal record for storing confidence score data for quality analysis.
    /// </summary>
    /// <remarks>
    /// This class is used internally to track confidence scores from reasoning operations
    /// and is stored in a thread-safe queue for quality metrics calculations.
    /// </remarks>
    private sealed class ConfidenceRecord
    {
        /// <summary>
        /// Gets the unique identifier of the agent that performed the reasoning.
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// Gets the type of reasoning performed.
        /// </summary>
        public ReasoningType ReasoningType { get; }

        /// <summary>
        /// Gets the confidence score (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; }

        /// <summary>
        /// Gets the timestamp when this record was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfidenceRecord"/> class.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent.</param>
        /// <param name="reasoningType">The type of reasoning performed.</param>
        /// <param name="confidenceScore">The confidence score (0.0 to 1.0).</param>
        public ConfidenceRecord(string agentId, ReasoningType reasoningType, double confidenceScore)
        {
            AgentId = agentId;
            ReasoningType = reasoningType;
            ConfidenceScore = confidenceScore;
            Timestamp = DateTimeOffset.UtcNow;
        }
    }
}
