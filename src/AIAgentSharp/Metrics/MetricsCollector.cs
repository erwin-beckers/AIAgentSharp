using System.Collections.Concurrent;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Default implementation of the metrics collector that provides comprehensive
/// metrics collection for AIAgentSharp operations.
/// </summary>
/// <remarks>
/// <para>
/// The MetricsCollector is designed to be lightweight and non-blocking, ensuring
/// that metric collection doesn't impact the performance of agent operations.
/// All operations are thread-safe and handle exceptions gracefully.
/// </para>
/// <para>
/// The collector maintains in-memory storage of metrics and provides real-time
/// access to the collected data. For production use, consider implementing
/// persistence or integration with external monitoring systems.
/// </para>
/// </remarks>
public sealed class MetricsCollector : IMetricsCollector, IMetricsProvider
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, AgentMetrics> _agentMetrics = new();
    private readonly ConcurrentDictionary<string, long> _errorCounts = new();
    private readonly ConcurrentDictionary<string, TokenUsage> _tokenUsageByModel = new();
    private readonly ConcurrentDictionary<string, long> _apiCallCountsByType = new();
    private readonly ConcurrentDictionary<string, long> _apiCallCountsByModel = new();
    private readonly ConcurrentDictionary<string, long> _stateStoreOperationCounts = new();
    private readonly ConcurrentDictionary<string, CustomMetric> _customMetrics = new();
    private readonly ConcurrentQueue<CustomEvent> _customEvents = new();
    private readonly ConcurrentQueue<ExecutionTimeRecord> _executionTimes = new();
    private readonly ConcurrentQueue<ConfidenceRecord> _confidenceScores = new();

    // Performance counters
    private long _totalAgentRuns;
    private long _totalAgentSteps;
    private long _totalLlmCalls;
    private long _totalToolCalls;
    private long _totalReasoningOperations;
    private long _failedAgentRuns;
    private long _failedAgentSteps;
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

    // Timing data for percentile calculations
    private readonly ConcurrentQueue<long> _agentRunTimes = new();
    private readonly ConcurrentQueue<long> _llmCallTimes = new();
    private readonly ConcurrentQueue<long> _toolCallTimes = new();

    public MetricsCollector(ILogger? logger = null)
    {
        _logger = logger ?? new ConsoleLogger();
    }

    // IMetricsCollector Implementation

    public void RecordAgentRunExecutionTime(string agentId, long executionTimeMs, int totalTurns)
    {
        try
        {
            Interlocked.Increment(ref _totalAgentRuns);
            _agentRunTimes.Enqueue(executionTimeMs);
            
            // Keep only the last 1000 records for percentile calculations
            while (_agentRunTimes.Count > 1000)
            {
                _agentRunTimes.TryDequeue(out _);
            }

            GetOrCreateAgentMetrics(agentId).RecordRunExecutionTime(executionTimeMs, totalTurns);
            _executionTimes.Enqueue(new ExecutionTimeRecord("AgentRun", agentId, executionTimeMs));
            
            RaiseMetricsUpdated("AgentRunExecutionTime");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record agent run execution time: {ex.Message}");
        }
    }

    public void RecordAgentStepExecutionTime(string agentId, int turnIndex, long executionTimeMs)
    {
        try
        {
            Interlocked.Increment(ref _totalAgentSteps);
            GetOrCreateAgentMetrics(agentId).RecordStepExecutionTime(turnIndex, executionTimeMs);
            _executionTimes.Enqueue(new ExecutionTimeRecord("AgentStep", agentId, executionTimeMs));
            
            RaiseMetricsUpdated("AgentStepExecutionTime");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record agent step execution time: {ex.Message}");
        }
    }

    public void RecordLlmCallExecutionTime(string agentId, int turnIndex, long executionTimeMs, string modelName)
    {
        try
        {
            Interlocked.Increment(ref _totalLlmCalls);
            _llmCallTimes.Enqueue(executionTimeMs);
            
            // Keep only the last 1000 records for percentile calculations
            while (_llmCallTimes.Count > 1000)
            {
                _llmCallTimes.TryDequeue(out _);
            }

            GetOrCreateAgentMetrics(agentId).RecordLlmCallExecutionTime(turnIndex, executionTimeMs, modelName);
            _executionTimes.Enqueue(new ExecutionTimeRecord("LlmCall", agentId, executionTimeMs));
            
            RaiseMetricsUpdated("LlmCallExecutionTime");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record LLM call execution time: {ex.Message}");
        }
    }

    public void RecordToolCallExecutionTime(string agentId, int turnIndex, string toolName, long executionTimeMs)
    {
        try
        {
            Interlocked.Increment(ref _totalToolCalls);
            _toolCallTimes.Enqueue(executionTimeMs);
            
            // Keep only the last 1000 records for percentile calculations
            while (_toolCallTimes.Count > 1000)
            {
                _toolCallTimes.TryDequeue(out _);
            }

            GetOrCreateAgentMetrics(agentId).RecordToolCallExecutionTime(turnIndex, toolName, executionTimeMs);
            _executionTimes.Enqueue(new ExecutionTimeRecord("ToolCall", agentId, executionTimeMs));
            
            RaiseMetricsUpdated("ToolCallExecutionTime");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record tool call execution time: {ex.Message}");
        }
    }

    public void RecordReasoningExecutionTime(string agentId, ReasoningType reasoningType, long executionTimeMs)
    {
        try
        {
            Interlocked.Increment(ref _totalReasoningOperations);
            GetOrCreateAgentMetrics(agentId).RecordReasoningExecutionTime(reasoningType, executionTimeMs);
            _executionTimes.Enqueue(new ExecutionTimeRecord("Reasoning", agentId, executionTimeMs));
            
            RaiseMetricsUpdated("ReasoningExecutionTime");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record reasoning execution time: {ex.Message}");
        }
    }

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

            GetOrCreateAgentMetrics(agentId).RecordRunCompletion(succeeded, totalTurns, errorType);
            RaiseMetricsUpdated("AgentRunCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record agent run completion: {ex.Message}");
        }
    }

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

            GetOrCreateAgentMetrics(agentId).RecordStepCompletion(turnIndex, succeeded, executedTool, errorType);
            RaiseMetricsUpdated("AgentStepCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record agent step completion: {ex.Message}");
        }
    }

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

            GetOrCreateAgentMetrics(agentId).RecordLlmCallCompletion(turnIndex, succeeded, modelName, errorType);
            RaiseMetricsUpdated("LlmCallCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record LLM call completion: {ex.Message}");
        }
    }

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

            GetOrCreateAgentMetrics(agentId).RecordToolCallCompletion(turnIndex, toolName, succeeded, errorType);
            RaiseMetricsUpdated("ToolCallCompletion");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record tool call completion: {ex.Message}");
        }
    }

    public void RecordLoopDetection(string agentId, string loopType, int consecutiveFailures)
    {
        try
        {
            Interlocked.Increment(ref _loopDetectionEvents);
            GetOrCreateAgentMetrics(agentId).RecordLoopDetection(loopType, consecutiveFailures);
            RaiseMetricsUpdated("LoopDetection");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record loop detection: {ex.Message}");
        }
    }

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

            GetOrCreateAgentMetrics(agentId).RecordDeduplicationEvent(toolName, cacheHit);
            RaiseMetricsUpdated("DeduplicationEvent");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record deduplication event: {ex.Message}");
        }
    }

    public void RecordReasoningConfidence(string agentId, ReasoningType reasoningType, double confidenceScore)
    {
        try
        {
            GetOrCreateAgentMetrics(agentId).RecordReasoningConfidence(reasoningType, confidenceScore);
            _confidenceScores.Enqueue(new ConfidenceRecord(agentId, reasoningType, confidenceScore));
            
            // Keep only the last 1000 records
            while (_confidenceScores.Count > 1000)
            {
                _confidenceScores.TryDequeue(out _);
            }
            
            RaiseMetricsUpdated("ReasoningConfidence");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record reasoning confidence: {ex.Message}");
        }
    }

    public void RecordResponseQuality(string agentId, int responseLength, bool hasFinalOutput)
    {
        try
        {
            Interlocked.Add(ref _totalResponseLength, responseLength);
            Interlocked.Increment(ref _totalResponses);
            
            if (hasFinalOutput)
            {
                Interlocked.Increment(ref _responsesWithFinalOutput);
            }

            GetOrCreateAgentMetrics(agentId).RecordResponseQuality(responseLength, hasFinalOutput);
            RaiseMetricsUpdated("ResponseQuality");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record response quality: {ex.Message}");
        }
    }

    public void RecordTokenUsage(string agentId, int turnIndex, int inputTokens, int outputTokens, string modelName)
    {
        try
        {
            Interlocked.Add(ref _totalInputTokens, inputTokens);
            Interlocked.Add(ref _totalOutputTokens, outputTokens);

            var tokenUsage = _tokenUsageByModel.GetOrAdd(modelName, _ => new TokenUsage());
            tokenUsage.AddTokens(inputTokens, outputTokens);

            GetOrCreateAgentMetrics(agentId).RecordTokenUsage(turnIndex, inputTokens, outputTokens, modelName);
            RaiseMetricsUpdated("TokenUsage");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record token usage: {ex.Message}");
        }
    }

    public void RecordApiCall(string agentId, string apiType, string modelName)
    {
        try
        {
            _apiCallCountsByType.AddOrUpdate(apiType, 1, (_, count) => count + 1);
            _apiCallCountsByModel.AddOrUpdate(modelName, 1, (_, count) => count + 1);

            GetOrCreateAgentMetrics(agentId).RecordApiCall(apiType, modelName);
            RaiseMetricsUpdated("ApiCall");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record API call: {ex.Message}");
        }
    }

    public void RecordStateStoreOperation(string agentId, string operationType, long executionTimeMs)
    {
        try
        {
            Interlocked.Increment(ref _totalStateStoreOperations);
            Interlocked.Add(ref _totalStateStoreOperationTimeMs, executionTimeMs);
            _stateStoreOperationCounts.AddOrUpdate(operationType, 1, (_, count) => count + 1);

            GetOrCreateAgentMetrics(agentId).RecordStateStoreOperation(operationType, executionTimeMs);
            RaiseMetricsUpdated("StateStoreOperation");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record state store operation: {ex.Message}");
        }
    }

    public void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        try
        {
            var customMetric = new CustomMetric
            {
                Name = metricName,
                Value = value,
                RecordedAt = DateTimeOffset.UtcNow,
                Tags = tags ?? new Dictionary<string, string>()
            };

            _customMetrics.AddOrUpdate(metricName, customMetric, (_, _) => customMetric);
            RaiseMetricsUpdated("CustomMetric");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record custom metric: {ex.Message}");
        }
    }

    public void RecordCustomEvent(string eventName, Dictionary<string, string>? tags = null)
    {
        try
        {
            var customEvent = new CustomEvent
            {
                Name = eventName,
                RecordedAt = DateTimeOffset.UtcNow,
                Tags = tags ?? new Dictionary<string, string>()
            };

            _customEvents.Enqueue(customEvent);
            
            // Keep only the last 1000 events
            while (_customEvents.Count > 1000)
            {
                _customEvents.TryDequeue(out _);
            }
            
            RaiseMetricsUpdated("CustomEvent");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record custom event: {ex.Message}");
        }
    }

    // IMetricsProvider Implementation

    public MetricsData GetMetrics()
    {
        return new MetricsData
        {
            CollectedAt = DateTimeOffset.UtcNow,
            Performance = CalculatePerformanceMetrics(),
            Operational = CalculateOperationalMetrics(),
            Quality = CalculateQualityMetrics(),
            Resources = CalculateResourceMetrics(),
            CustomMetrics = new Dictionary<string, CustomMetric>(_customMetrics),
            CustomEvents = _customEvents.ToList()
        };
    }

    public MetricsData? GetAgentMetrics(string agentId)
    {
        if (!_agentMetrics.TryGetValue(agentId, out var agentMetrics))
        {
            return null;
        }

        return new MetricsData
        {
            CollectedAt = DateTimeOffset.UtcNow,
            Performance = agentMetrics.CalculatePerformanceMetrics(),
            Operational = agentMetrics.CalculateOperationalMetrics(),
            Quality = agentMetrics.CalculateQualityMetrics(),
            Resources = agentMetrics.CalculateResourceMetrics(),
            CustomMetrics = new Dictionary<string, CustomMetric>(),
            CustomEvents = new List<CustomEvent>()
        };
    }

    public MetricsData GetMetricsForTimeRange(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        // Filter execution times and confidence scores by time range
        var filteredExecutionTimes = _executionTimes
            .Where(record => record.Timestamp >= startTime && record.Timestamp <= endTime)
            .ToList();

        var filteredConfidenceScores = _confidenceScores
            .Where(record => record.Timestamp >= startTime && record.Timestamp <= endTime)
            .ToList();

        var filteredCustomEvents = _customEvents
            .Where(evt => evt.RecordedAt >= startTime && evt.RecordedAt <= endTime)
            .ToList();

        return new MetricsData
        {
            CollectedAt = DateTimeOffset.UtcNow,
            Performance = CalculatePerformanceMetrics(filteredExecutionTimes),
            Operational = CalculateOperationalMetrics(),
            Quality = CalculateQualityMetrics(filteredConfidenceScores),
            Resources = CalculateResourceMetrics(),
            CustomMetrics = new Dictionary<string, CustomMetric>(_customMetrics),
            CustomEvents = filteredCustomEvents
        };
    }

    public void ResetMetrics()
    {
        _agentMetrics.Clear();
        _errorCounts.Clear();
        _tokenUsageByModel.Clear();
        _apiCallCountsByType.Clear();
        _apiCallCountsByModel.Clear();
        _stateStoreOperationCounts.Clear();
        _customMetrics.Clear();
        
        while (_customEvents.TryDequeue(out _)) { }
        while (_executionTimes.TryDequeue(out _)) { }
        while (_confidenceScores.TryDequeue(out _)) { }
        while (_agentRunTimes.TryDequeue(out _)) { }
        while (_llmCallTimes.TryDequeue(out _)) { }
        while (_toolCallTimes.TryDequeue(out _)) { }

        // Reset counters
        _totalAgentRuns = 0;
        _totalAgentSteps = 0;
        _totalLlmCalls = 0;
        _totalToolCalls = 0;
        _totalReasoningOperations = 0;
        _failedAgentRuns = 0;
        _failedAgentSteps = 0;
        _failedLlmCalls = 0;
        _failedToolCalls = 0;
        _loopDetectionEvents = 0;
        _deduplicationCacheHits = 0;
        _deduplicationCacheMisses = 0;
        _totalInputTokens = 0;
        _totalOutputTokens = 0;
        _totalStateStoreOperations = 0;
        _totalStateStoreOperationTimeMs = 0;

        RaiseMetricsUpdated("Reset");
    }

    public string ExportMetrics(MetricsExportFormat format)
    {
        var metrics = GetMetrics();
        
        return format switch
        {
            MetricsExportFormat.Json => System.Text.Json.JsonSerializer.Serialize(metrics, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            MetricsExportFormat.Csv => ExportToCsv(metrics),
            MetricsExportFormat.Prometheus => ExportToPrometheus(metrics),
            MetricsExportFormat.Text => ExportToText(metrics),
            _ => throw new ArgumentException($"Unsupported export format: {format}")
        };
    }

    public event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;

    // Private helper methods

    private AgentMetrics GetOrCreateAgentMetrics(string agentId)
    {
        return _agentMetrics.GetOrAdd(agentId, _ => new AgentMetrics(agentId));
    }

    private void RaiseMetricsUpdated(string metricType)
    {
        try
        {
            MetricsUpdated?.Invoke(this, new MetricsUpdatedEventArgs
            {
                Metrics = GetMetrics(),
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedMetrics = new List<string> { metricType }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to raise metrics updated event: {ex.Message}");
        }
    }

    private PerformanceMetrics CalculatePerformanceMetrics(List<ExecutionTimeRecord>? filteredRecords = null)
    {
        var records = filteredRecords ?? _executionTimes.ToList();
        
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
            TotalAgentRuns = _totalAgentRuns,
            TotalAgentSteps = _totalAgentSteps,
            TotalLlmCalls = _totalLlmCalls,
            TotalToolCalls = _totalToolCalls,
            TotalReasoningOperations = _totalReasoningOperations,
            RequestsPerSecond = CalculateRequestsPerSecond(),
            P95AgentRunTimeMs = CalculatePercentile(agentRunTimes, 95),
            P95LlmCallTimeMs = CalculatePercentile(llmCallTimes, 95),
            P95ToolCallTimeMs = CalculatePercentile(toolCallTimes, 95)
        };
    }

    private OperationalMetrics CalculateOperationalMetrics()
    {
        var totalRuns = _totalAgentRuns;
        var totalSteps = _totalAgentSteps;
        var totalLlmCalls = _totalLlmCalls;
        var totalToolCalls = _totalToolCalls;
        var totalDeduplicationCalls = _deduplicationCacheHits + _deduplicationCacheMisses;

        return new OperationalMetrics
        {
            AgentRunSuccessRate = totalRuns > 0 ? (double)(totalRuns - _failedAgentRuns) / totalRuns : 0,
            AgentStepSuccessRate = totalSteps > 0 ? (double)(totalSteps - _failedAgentSteps) / totalSteps : 0,
            LlmCallSuccessRate = totalLlmCalls > 0 ? (double)(totalLlmCalls - _failedLlmCalls) / totalLlmCalls : 0,
            ToolCallSuccessRate = totalToolCalls > 0 ? (double)(totalToolCalls - _failedToolCalls) / totalToolCalls : 0,
            FailedAgentRuns = _failedAgentRuns,
            FailedAgentSteps = _failedAgentSteps,
            FailedLlmCalls = _failedLlmCalls,
            FailedToolCalls = _failedToolCalls,
            LoopDetectionEvents = _loopDetectionEvents,
            DeduplicationCacheHits = _deduplicationCacheHits,
            DeduplicationCacheMisses = _deduplicationCacheMisses,
            DeduplicationCacheHitRate = totalDeduplicationCalls > 0 ? (double)_deduplicationCacheHits / totalDeduplicationCalls : 0,
            ErrorCountsByType = new Dictionary<string, long>(_errorCounts)
        };
    }

    private QualityMetrics CalculateQualityMetrics(List<ConfidenceRecord>? filteredRecords = null)
    {
        var records = filteredRecords ?? _confidenceScores.ToList();
        var confidenceScores = records.Select(r => r.ConfidenceScore).ToList();

        var confidenceByType = records
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

    private ResourceMetrics CalculateResourceMetrics()
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
            ApiCallCountsByType = new Dictionary<string, long>(_apiCallCountsByType),
            ApiCallCountsByModel = new Dictionary<string, long>(_apiCallCountsByModel),
            StateStoreOperationCounts = new Dictionary<string, long>(_stateStoreOperationCounts),
            AverageStateStoreOperationTimeMs = totalStateStoreOperations > 0 ? (double)_totalStateStoreOperationTimeMs / totalStateStoreOperations : 0
        };
    }

    private double CalculateRequestsPerSecond()
    {
        // This is a simplified calculation - in a real implementation,
        // you might want to track requests over time windows
        return _totalAgentRuns > 0 ? 1.0 : 0.0; // Placeholder
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

    private string ExportToCsv(MetricsData metrics)
    {
        // Implementation for CSV export
        return "Metric,Value\n" +
               $"TotalAgentRuns,{metrics.Performance.TotalAgentRuns}\n" +
               $"TotalAgentSteps,{metrics.Performance.TotalAgentSteps}\n" +
               $"AgentRunSuccessRate,{metrics.Operational.AgentRunSuccessRate:F2}\n" +
               $"AverageAgentRunTimeMs,{metrics.Performance.AverageAgentRunTimeMs:F2}\n";
    }

    private string ExportToPrometheus(MetricsData metrics)
    {
        // Implementation for Prometheus export
        return $"# HELP aiagentsharp_agent_runs_total Total number of agent runs\n" +
               $"# TYPE aiagentsharp_agent_runs_total counter\n" +
               $"aiagentsharp_agent_runs_total {metrics.Performance.TotalAgentRuns}\n" +
               $"# HELP aiagentsharp_agent_steps_total Total number of agent steps\n" +
               $"# TYPE aiagentsharp_agent_steps_total counter\n" +
               $"aiagentsharp_agent_steps_total {metrics.Performance.TotalAgentSteps}\n" +
               $"# HELP aiagentsharp_agent_run_success_rate Agent run success rate\n" +
               $"# TYPE aiagentsharp_agent_run_success_rate gauge\n" +
               $"aiagentsharp_agent_run_success_rate {metrics.Operational.AgentRunSuccessRate}\n";
    }

    private string ExportToText(MetricsData metrics)
    {
        // Implementation for human-readable text export
        return $"AIAgentSharp Metrics Report\n" +
               $"Generated: {metrics.CollectedAt:yyyy-MM-dd HH:mm:ss UTC}\n\n" +
               $"Performance Metrics:\n" +
               $"  Total Agent Runs: {metrics.Performance.TotalAgentRuns}\n" +
               $"  Total Agent Steps: {metrics.Performance.TotalAgentSteps}\n" +
               $"  Average Agent Run Time: {metrics.Performance.AverageAgentRunTimeMs:F2}ms\n" +
               $"  Average LLM Call Time: {metrics.Performance.AverageLlmCallTimeMs:F2}ms\n\n" +
               $"Operational Metrics:\n" +
               $"  Agent Run Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}\n" +
               $"  LLM Call Success Rate: {metrics.Operational.LlmCallSuccessRate:P2}\n" +
               $"  Tool Call Success Rate: {metrics.Operational.ToolCallSuccessRate:P2}\n\n" +
               $"Resource Metrics:\n" +
               $"  Total Input Tokens: {metrics.Resources.TotalInputTokens:N0}\n" +
               $"  Total Output Tokens: {metrics.Resources.TotalOutputTokens:N0}\n" +
               $"  Total Tokens: {metrics.Resources.TotalTokens:N0}\n";
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
