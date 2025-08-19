using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Default implementation of the metrics collector that provides comprehensive
/// metrics collection for AIAgentSharp operations.
/// </summary>
/// <remarks>
/// <para>
/// The MetricsCollector is now a wrapper around MetricsAggregator, which provides
/// better separation of concerns and more focused metric collection.
/// </para>
/// <para>
/// The collector maintains in-memory storage of metrics and provides real-time
/// access to the collected data. For production use, consider implementing
/// persistence or integration with external monitoring systems.
/// </para>
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
        => _aggregator.RecordAgentRunExecutionTime(agentId, executionTimeMs, totalTurns);

    public void RecordAgentStepExecutionTime(string agentId, int turnIndex, long executionTimeMs)
        => _aggregator.RecordAgentStepExecutionTime(agentId, turnIndex, executionTimeMs);

    public void RecordLlmCallExecutionTime(string agentId, int turnIndex, long executionTimeMs, string modelName)
        => _aggregator.RecordLlmCallExecutionTime(agentId, turnIndex, executionTimeMs, modelName);

    public void RecordToolCallExecutionTime(string agentId, int turnIndex, string toolName, long executionTimeMs)
        => _aggregator.RecordToolCallExecutionTime(agentId, turnIndex, toolName, executionTimeMs);

    public void RecordReasoningExecutionTime(string agentId, ReasoningType reasoningType, long executionTimeMs)
        => _aggregator.RecordReasoningExecutionTime(agentId, reasoningType, executionTimeMs);

    public void RecordAgentRunCompletion(string agentId, bool succeeded, int totalTurns, string? errorType = null)
        => _aggregator.RecordAgentRunCompletion(agentId, succeeded, totalTurns, errorType);

    public void RecordAgentStepCompletion(string agentId, int turnIndex, bool succeeded, bool executedTool, string? errorType = null)
        => _aggregator.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool, errorType);

    public void RecordLlmCallCompletion(string agentId, int turnIndex, bool succeeded, string modelName, string? errorType = null)
        => _aggregator.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName, errorType);

    public void RecordToolCallCompletion(string agentId, int turnIndex, string toolName, bool succeeded, string? errorType = null)
        => _aggregator.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded, errorType);

    public void RecordLoopDetection(string agentId, string loopType, int consecutiveFailures)
        => _aggregator.RecordLoopDetection(agentId, loopType, consecutiveFailures);

    public void RecordDeduplicationEvent(string agentId, string toolName, bool cacheHit)
        => _aggregator.RecordDeduplicationEvent(agentId, toolName, cacheHit);

    public void RecordReasoningConfidence(string agentId, ReasoningType reasoningType, double confidenceScore)
        => _aggregator.RecordReasoningConfidence(agentId, reasoningType, confidenceScore);

    public void RecordResponseQuality(string agentId, int responseLength, bool hasFinalOutput)
        => _aggregator.RecordResponseQuality(agentId, responseLength, hasFinalOutput);

    public void RecordValidation(string agentId, string validationType, bool passed, string? errorMessage = null)
        => _aggregator.RecordValidation(agentId, validationType, passed, errorMessage);

    public void RecordTokenUsage(string agentId, int turnIndex, int inputTokens, int outputTokens, string modelName)
        => _aggregator.RecordTokenUsage(agentId, turnIndex, inputTokens, outputTokens, modelName);

    public void RecordApiCall(string agentId, string apiType, string modelName)
        => _aggregator.RecordApiCall(agentId, apiType, modelName);

    public void RecordStateStoreOperation(string agentId, string operationType, long executionTimeMs)
        => _aggregator.RecordStateStoreOperation(agentId, operationType, executionTimeMs);

    public void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null)
        => _aggregator.RecordCustomMetric(metricName, value, tags);

    public void RecordCustomEvent(string eventName, Dictionary<string, string>? tags = null)
        => _aggregator.RecordCustomEvent(eventName, tags);

    // IMetricsProvider Implementation - Delegate to aggregator methods

    public MetricsSummary GetSummary() => _aggregator.GetSummary();

    public AllMetrics GetAllMetrics() => _aggregator.CalculateAllMetrics();

    public void Reset() => _aggregator.Reset();

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

    public void ResetMetrics() => _aggregator.Reset();

    public event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;


}
