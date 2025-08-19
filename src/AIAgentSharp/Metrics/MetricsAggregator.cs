using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Aggregates and coordinates all metric collectors, providing a unified
/// interface for metrics collection and retrieval.
/// </summary>
public sealed class MetricsAggregator : IMetricsCollector
{
    private readonly PerformanceMetricsCollector _performanceCollector;
    private readonly OperationalMetricsCollector _operationalCollector;
    private readonly ResourceMetricsCollector _resourceCollector;
    private readonly QualityMetricsCollector _qualityCollector;
    private readonly CustomMetricsCollector _customCollector;
    private readonly ILogger _logger;

    public MetricsAggregator(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _performanceCollector = new PerformanceMetricsCollector(logger);
        _operationalCollector = new OperationalMetricsCollector(logger);
        _resourceCollector = new ResourceMetricsCollector(logger);
        _qualityCollector = new QualityMetricsCollector(logger);
        _customCollector = new CustomMetricsCollector(logger);
    }

    #region Performance Metrics

    public void RecordAgentRunExecutionTime(string agentId, long executionTimeMs, int totalTurns)
    {
        _performanceCollector.RecordAgentRunExecutionTime(agentId, executionTimeMs, totalTurns);
    }

    public void RecordAgentStepExecutionTime(string agentId, int turnIndex, long executionTimeMs)
    {
        _performanceCollector.RecordAgentStepExecutionTime(agentId, turnIndex, executionTimeMs);
    }

    public void RecordLlmCallExecutionTime(string agentId, int turnIndex, long executionTimeMs, string modelName)
    {
        _performanceCollector.RecordLlmCallExecutionTime(agentId, turnIndex, executionTimeMs, modelName);
    }

    public void RecordToolCallExecutionTime(string agentId, int turnIndex, string toolName, long executionTimeMs)
    {
        _performanceCollector.RecordToolCallExecutionTime(agentId, turnIndex, toolName, executionTimeMs);
    }

    public void RecordReasoningExecutionTime(string agentId, ReasoningType reasoningType, long executionTimeMs)
    {
        _performanceCollector.RecordReasoningExecutionTime(agentId, 0, executionTimeMs);
    }

    #endregion

    #region Operational Metrics

    public void RecordAgentRunCompletion(string agentId, bool succeeded, int totalTurns, string? errorType = null)
    {
        _operationalCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns, errorType);
    }

    public void RecordAgentStepCompletion(string agentId, int turnIndex, bool succeeded, bool executedTool, string? errorType = null)
    {
        _operationalCollector.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool, errorType);
    }

    public void RecordLlmCallCompletion(string agentId, int turnIndex, bool succeeded, string modelName, string? errorType = null)
    {
        _operationalCollector.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName, errorType);
    }

    public void RecordToolCallCompletion(string agentId, int turnIndex, string toolName, bool succeeded, string? errorType = null)
    {
        _operationalCollector.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded, errorType);
    }

    public void RecordLoopDetection(string agentId, string loopType, int consecutiveFailures)
    {
        _operationalCollector.RecordLoopDetection(agentId, loopType, consecutiveFailures);
    }

    public void RecordDeduplicationEvent(string agentId, string toolName, bool cacheHit)
    {
        _operationalCollector.RecordDeduplicationEvent(agentId, toolName, cacheHit);
    }

    public void RecordError(string agentId, int turnIndex, string errorType, string errorMessage)
    {
        _operationalCollector.RecordError(agentId, turnIndex, errorType, errorMessage);
    }

    #endregion

    #region Resource Metrics

    public void RecordTokenUsage(string agentId, int turnIndex, int inputTokens, int outputTokens, string modelName)
    {
        _resourceCollector.RecordTokenUsage(agentId, turnIndex, inputTokens, outputTokens, modelName);
    }

    public void RecordApiCall(string agentId, string apiType, string modelName)
    {
        _resourceCollector.RecordApiCall(agentId, apiType, modelName);
    }

    public void RecordStateStoreOperation(string agentId, string operationType, long executionTimeMs)
    {
        _resourceCollector.RecordStateStoreOperation(agentId, operationType, executionTimeMs);
    }

    #endregion

    #region Quality Metrics

    public void RecordResponseQuality(string agentId, string qualityLevel, double? qualityScore = null)
    {
        _qualityCollector.RecordResponseQuality(agentId, qualityLevel, qualityScore);
    }

    public void RecordReasoningStep(string agentId, string reasoningType, bool wasSuccessful)
    {
        _qualityCollector.RecordReasoningStep(agentId, reasoningType, wasSuccessful);
    }

    public void RecordValidation(string agentId, string validationType, bool passed)
    {
        _qualityCollector.RecordValidation(agentId, validationType, passed);
    }

    public void RecordResponseTime(string agentId, long responseTimeMs)
    {
        _qualityCollector.RecordResponseTime(agentId, responseTimeMs);
    }

    #endregion

    #region Custom Metrics

    public void RecordMetric(string metricName, double value, string? category = null)
    {
        _customCollector.RecordMetric(metricName, value, category);
    }

    public void RecordCounter(string counterName, long increment = 1, string? category = null)
    {
        _customCollector.RecordCounter(counterName, increment, category);
    }

    public void SetTag(string tagName, string tagValue)
    {
        _customCollector.SetTag(tagName, tagValue);
    }

    public void SetMetadata(string key, object value)
    {
        _customCollector.SetMetadata(key, value);
    }

    public void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        _customCollector.RecordMetric(metricName, value);
    }

    public void RecordCustomEvent(string eventName, Dictionary<string, string>? tags = null)
    {
        // Store as metadata for now
        _customCollector.SetMetadata($"event_{eventName}", DateTime.UtcNow);
    }

    #endregion

    #region Quality Metrics

    public void RecordReasoningConfidence(string agentId, ReasoningType reasoningType, double confidenceScore)
    {
        _qualityCollector.RecordReasoningStep(agentId, reasoningType.ToString(), confidenceScore > 0.5);
    }

    public void RecordResponseQuality(string agentId, int responseLength, bool hasFinalOutput)
    {
        var qualityLevel = hasFinalOutput ? "high" : (responseLength > 100 ? "medium" : "low");
        _qualityCollector.RecordResponseQuality(agentId, qualityLevel);
    }

    public void RecordValidation(string agentId, string validationType, bool passed, string? errorMessage = null)
        => _qualityCollector.RecordValidation(agentId, validationType, passed, errorMessage);

    #endregion

    #region Aggregated Metrics

    /// <summary>
    /// Calculates all metrics from all collectors.
    /// </summary>
    public AllMetrics CalculateAllMetrics()
    {
        return new AllMetrics
        {
            Performance = _performanceCollector.CalculatePerformanceMetrics(),
            Operational = _operationalCollector.CalculateOperationalMetrics(),
            Resource = _resourceCollector.CalculateResourceMetrics(),
            Quality = _qualityCollector.CalculateQualityMetrics(),
            Custom = _customCollector.CalculateCustomMetrics()
        };
    }

    /// <summary>
    /// Resets all metrics in all collectors.
    /// </summary>
    public void Reset()
    {
        _performanceCollector.Reset();
        _operationalCollector.Reset();
        _resourceCollector.Reset();
        _qualityCollector.Reset();
        _customCollector.Reset();
    }

    /// <summary>
    /// Gets a summary of all metrics.
    /// </summary>
    public MetricsSummary GetSummary()
    {
        var performance = _performanceCollector.CalculatePerformanceMetrics();
        var operational = _operationalCollector.CalculateOperationalMetrics();
        var resource = _resourceCollector.CalculateResourceMetrics();
        var quality = _qualityCollector.CalculateQualityMetrics();

        return new MetricsSummary
        {
            TotalAgentRuns = performance.TotalAgentRuns,
            SuccessfulAgentRuns = operational.SuccessfulAgentRuns,
            FailedAgentRuns = operational.FailedAgentRuns,
            
            TotalAgentSteps = performance.TotalAgentSteps,
            SuccessfulAgentSteps = operational.SuccessfulAgentSteps,
            FailedAgentSteps = operational.FailedAgentSteps,
            
            TotalLlmCalls = performance.TotalLlmCalls,
            SuccessfulLlmCalls = operational.SuccessfulLlmCalls,
            FailedLlmCalls = operational.FailedLlmCalls,
            
            TotalToolCalls = performance.TotalToolCalls,
            SuccessfulToolCalls = operational.SuccessfulToolCalls,
            FailedToolCalls = operational.FailedToolCalls,
            
            TotalInputTokens = resource.TotalInputTokens,
            TotalOutputTokens = resource.TotalOutputTokens,
            
            AverageAgentRunTimeMs = performance.AverageAgentRunTimeMs,
            AverageAgentStepTimeMs = performance.AverageAgentStepTimeMs,
            AverageLlmCallTimeMs = performance.AverageLlmCallTimeMs,
            AverageToolCallTimeMs = performance.AverageToolCallTimeMs,
            
            SuccessRate = operational.SuccessRate,
            ErrorRate = operational.ErrorRate,
            QualityPercentage = quality.QualityPercentage,
            ReasoningAccuracyPercentage = quality.ReasoningAccuracyPercentage,
            ValidationPassRate = quality.ValidationPassRate,
            
            RequestsPerSecond = performance.RequestsPerSecond,
            ErrorCounts = operational.ErrorCounts
        };
    }

    #endregion

    #region Individual Collector Access

    /// <summary>
    /// Gets the performance metrics collector.
    /// </summary>
    public PerformanceMetricsCollector Performance => _performanceCollector;

    /// <summary>
    /// Gets the operational metrics collector.
    /// </summary>
    public OperationalMetricsCollector Operational => _operationalCollector;

    /// <summary>
    /// Gets the resource metrics collector.
    /// </summary>
    public ResourceMetricsCollector Resource => _resourceCollector;

    /// <summary>
    /// Gets the quality metrics collector.
    /// </summary>
    public QualityMetricsCollector Quality => _qualityCollector;

    /// <summary>
    /// Gets the custom metrics collector.
    /// </summary>
    public CustomMetricsCollector Custom => _customCollector;

    #endregion
}
