using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
///     Default implementation of the metrics collector that provides comprehensive
///     metrics collection for AIAgentSharp operations.
/// </summary>
/// <remarks>
///     <para>
///         The MetricsCollector is now a wrapper around MetricsAggregator, which provides
///         better separation of concerns and more focused metric collection.
///     </para>
///     <para>
///         The collector maintains in-memory storage of metrics and provides real-time
///         access to the collected data. For production use, consider implementing
///         persistence or integration with external monitoring systems.
///     </para>
/// </remarks>
public sealed class MetricsCollector : IMetricsCollector, IMetricsProvider
{
    private readonly MetricsAggregator _aggregator;
    private readonly ILogger _logger;

    public MetricsCollector(ILogger? logger = null)
    {
        _logger = logger ?? new ConsoleLogger();
        _aggregator = new MetricsAggregator(_logger);
    }

    // IMetricsCollector Implementation - All methods delegate to the aggregator

    public void RecordAgentRunExecutionTime(string agentId, long executionTimeMs, int totalTurns)
    {
        _aggregator.RecordAgentRunExecutionTime(agentId, executionTimeMs, totalTurns);
        OnMetricsUpdated("AgentRunExecutionTime");
    }

    public void RecordAgentStepExecutionTime(string agentId, int turnIndex, long executionTimeMs)
    {
        _aggregator.RecordAgentStepExecutionTime(agentId, turnIndex, executionTimeMs);
        OnMetricsUpdated("AgentStepExecutionTime");
    }

    public void RecordLlmCallExecutionTime(string agentId, int turnIndex, long executionTimeMs, string modelName)
    {
        _aggregator.RecordLlmCallExecutionTime(agentId, turnIndex, executionTimeMs, modelName);
        OnMetricsUpdated("LlmCallExecutionTime");
    }

    public void RecordToolCallExecutionTime(string agentId, int turnIndex, string toolName, long executionTimeMs)
    {
        _aggregator.RecordToolCallExecutionTime(agentId, turnIndex, toolName, executionTimeMs);
        OnMetricsUpdated("ToolCallExecutionTime");
    }

    public void RecordReasoningExecutionTime(string agentId, ReasoningType reasoningType, long executionTimeMs)
    {
        _aggregator.RecordReasoningExecutionTime(agentId, reasoningType, executionTimeMs);
        OnMetricsUpdated("ReasoningExecutionTime");
    }

    public void RecordAgentRunCompletion(string agentId, bool succeeded, int totalTurns, string? errorType = null)
    {
        _aggregator.RecordAgentRunCompletion(agentId, succeeded, totalTurns, errorType);
        OnMetricsUpdated("AgentRunCompletion");
    }

    public void RecordAgentStepCompletion(string agentId, int turnIndex, bool succeeded, bool executedTool, string? errorType = null)
    {
        _aggregator.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool, errorType);
        OnMetricsUpdated("AgentStepCompletion");
    }

    public void RecordLlmCallCompletion(string agentId, int turnIndex, bool succeeded, string modelName, string? errorType = null)
    {
        _aggregator.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName, errorType);
        OnMetricsUpdated("LlmCallCompletion");
    }

    public void RecordToolCallCompletion(string agentId, int turnIndex, string toolName, bool succeeded, string? errorType = null)
    {
        _aggregator.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded, errorType);
        OnMetricsUpdated("ToolCallCompletion");
    }

    public void RecordLoopDetection(string agentId, string loopType, int consecutiveFailures)
    {
        _aggregator.RecordLoopDetection(agentId, loopType, consecutiveFailures);
        OnMetricsUpdated("LoopDetection");
    }

    public void RecordDeduplicationEvent(string agentId, string toolName, bool cacheHit)
    {
        _aggregator.RecordDeduplicationEvent(agentId, toolName, cacheHit);
        OnMetricsUpdated("DeduplicationEvent");
    }

    public void RecordReasoningConfidence(string agentId, ReasoningType reasoningType, double confidenceScore)
    {
        _aggregator.RecordReasoningConfidence(agentId, reasoningType, confidenceScore);
        OnMetricsUpdated("ReasoningConfidence");
    }

    public void RecordResponseQuality(string agentId, int responseLength, bool hasFinalOutput)
    {
        _aggregator.RecordResponseQuality(agentId, responseLength, hasFinalOutput);
        OnMetricsUpdated("ResponseQuality");
    }

    public void RecordValidation(string agentId, string validationType, bool passed, string? errorMessage = null)
    {
        _aggregator.RecordValidation(agentId, validationType, passed, errorMessage);
        OnMetricsUpdated("Validation");
    }

    public void RecordTokenUsage(string agentId, int turnIndex, int inputTokens, int outputTokens, string modelName)
    {
        _aggregator.RecordTokenUsage(agentId, turnIndex, inputTokens, outputTokens, modelName);
        OnMetricsUpdated("TokenUsage");
    }

    public void RecordApiCall(string agentId, string apiType, string modelName)
    {
        _aggregator.RecordApiCall(agentId, apiType, modelName);
        OnMetricsUpdated("ApiCall");
    }

    public void RecordStateStoreOperation(string agentId, string operationType, long executionTimeMs)
    {
        _aggregator.RecordStateStoreOperation(agentId, operationType, executionTimeMs);
        OnMetricsUpdated("StateStoreOperation");
    }

    public void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        _aggregator.RecordCustomMetric(metricName, value, tags);
        OnMetricsUpdated("CustomMetric");
    }

    public void RecordCustomEvent(string eventName, Dictionary<string, string>? tags = null)
    {
        _aggregator.RecordCustomEvent(eventName, tags);
        OnMetricsUpdated("CustomEvent");
    }

    public AllMetrics GetAllMetrics()
    {
        return _aggregator.CalculateAllMetrics();
    }

    // IMetricsProvider interface methods
    public MetricsData GetMetrics()
    {
        var allMetrics = _aggregator.CalculateAllMetrics();
        return new MetricsData
        {
            CollectedAt = DateTimeOffset.UtcNow,
            Performance = allMetrics.Performance,
            Operational = allMetrics.Operational,
            Quality = allMetrics.Quality,
            Resources = allMetrics.Resource,
            CustomMetrics = new Dictionary<string, CustomMetric>(),
            CustomEvents = new List<CustomEvent>()
        };
    }

    public MetricsData? GetAgentMetrics(string agentId)
    {
        // For now, return null as we don't have agent-specific metrics in the new structure
        return null;
    }

    public MetricsData GetMetricsForTimeRange(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        // For now, return current metrics as we don't have time-based filtering in the new structure
        return GetMetrics();
    }

    public void ResetMetrics()
    {
        _aggregator.Reset();
        OnMetricsUpdated("Reset");
    }

    // IMetricsProvider Implementation - Delegate to aggregator methods

    public MetricsSummary GetSummary()
    {
        return _aggregator.GetSummary();
    }

    public void Reset()
    {
        _aggregator.Reset();
        OnMetricsUpdated("Reset");
    }

    public event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;

    /// <summary>
    /// Raises the MetricsUpdated event with the specified metric name.
    /// </summary>
    /// <param name="metricName">The name of the metric that was updated.</param>
    public void OnMetricsUpdated(string metricName)
    {
        try
        {
            var metrics = GetMetrics();
            var eventArgs = new MetricsUpdatedEventArgs
            {
                Metrics = metrics,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedMetrics = new List<string> { metricName }
            };

            MetricsUpdated?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to raise MetricsUpdated event: {ex.Message}");
        }
    }
}