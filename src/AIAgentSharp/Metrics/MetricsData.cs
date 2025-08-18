using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Represents comprehensive metrics data collected from AIAgentSharp operations.
/// This class provides structured access to all collected metrics organized by category.
/// </summary>
/// <remarks>
/// <para>
/// MetricsData provides a snapshot of all metrics collected during agent operations.
/// The data is organized into logical categories for easy analysis and reporting.
/// </para>
/// <para>
/// All metrics are thread-safe and can be accessed concurrently. The data represents
/// a point-in-time snapshot and should be refreshed periodically for real-time monitoring.
/// </para>
/// </remarks>
public sealed class MetricsData
{
    /// <summary>
    /// Gets the timestamp when this metrics data was collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the performance metrics including execution times and throughput.
    /// </summary>
    public PerformanceMetrics Performance { get; init; } = new();

    /// <summary>
    /// Gets the operational metrics including success rates and error rates.
    /// </summary>
    public OperationalMetrics Operational { get; init; } = new();

    /// <summary>
    /// Gets the quality metrics including reasoning confidence and response quality.
    /// </summary>
    public QualityMetrics Quality { get; init; } = new();

    /// <summary>
    /// Gets the resource metrics including token usage and API calls.
    /// </summary>
    public ResourceMetrics Resources { get; init; } = new();

    /// <summary>
    /// Gets custom metrics recorded by the application.
    /// </summary>
    public Dictionary<string, CustomMetric> CustomMetrics { get; init; } = new();

    /// <summary>
    /// Gets custom events recorded by the application.
    /// </summary>
    public List<CustomEvent> CustomEvents { get; init; } = new();
}

/// <summary>
/// Represents performance-related metrics including execution times and throughput.
/// </summary>
public sealed class PerformanceMetrics
{
    /// <summary>
    /// Gets the average execution time of agent runs in milliseconds.
    /// </summary>
    public double AverageAgentRunTimeMs { get; init; }

    /// <summary>
    /// Gets the average execution time of agent steps in milliseconds.
    /// </summary>
    public double AverageAgentStepTimeMs { get; init; }

    /// <summary>
    /// Gets the average execution time of LLM calls in milliseconds.
    /// </summary>
    public double AverageLlmCallTimeMs { get; init; }

    /// <summary>
    /// Gets the average execution time of tool calls in milliseconds.
    /// </summary>
    public double AverageToolCallTimeMs { get; init; }

    /// <summary>
    /// Gets the average execution time of reasoning operations in milliseconds.
    /// </summary>
    public double AverageReasoningTimeMs { get; init; }

    /// <summary>
    /// Gets the total number of agent runs executed.
    /// </summary>
    public long TotalAgentRuns { get; init; }

    /// <summary>
    /// Gets the total number of agent steps executed.
    /// </summary>
    public long TotalAgentSteps { get; init; }

    /// <summary>
    /// Gets the total number of LLM calls made.
    /// </summary>
    public long TotalLlmCalls { get; init; }

    /// <summary>
    /// Gets the total number of tool calls made.
    /// </summary>
    public long TotalToolCalls { get; init; }

    /// <summary>
    /// Gets the total number of reasoning operations performed.
    /// </summary>
    public long TotalReasoningOperations { get; init; }

    /// <summary>
    /// Gets the requests per second (throughput) for agent operations.
    /// </summary>
    public double RequestsPerSecond { get; init; }

    /// <summary>
    /// Gets the 95th percentile execution time for agent runs in milliseconds.
    /// </summary>
    public double P95AgentRunTimeMs { get; init; }

    /// <summary>
    /// Gets the 95th percentile execution time for LLM calls in milliseconds.
    /// </summary>
    public double P95LlmCallTimeMs { get; init; }

    /// <summary>
    /// Gets the 95th percentile execution time for tool calls in milliseconds.
    /// </summary>
    public double P95ToolCallTimeMs { get; init; }
}

/// <summary>
/// Represents operational metrics including success rates and error rates.
/// </summary>
public sealed class OperationalMetrics
{
    /// <summary>
    /// Gets the success rate of agent runs (0.0 to 1.0).
    /// </summary>
    public double AgentRunSuccessRate { get; init; }

    /// <summary>
    /// Gets the success rate of agent steps (0.0 to 1.0).
    /// </summary>
    public double AgentStepSuccessRate { get; init; }

    /// <summary>
    /// Gets the success rate of LLM calls (0.0 to 1.0).
    /// </summary>
    public double LlmCallSuccessRate { get; init; }

    /// <summary>
    /// Gets the success rate of tool calls (0.0 to 1.0).
    /// </summary>
    public double ToolCallSuccessRate { get; init; }

    /// <summary>
    /// Gets the total number of failed agent runs.
    /// </summary>
    public long FailedAgentRuns { get; init; }

    /// <summary>
    /// Gets the total number of failed agent steps.
    /// </summary>
    public long FailedAgentSteps { get; init; }

    /// <summary>
    /// Gets the total number of failed LLM calls.
    /// </summary>
    public long FailedLlmCalls { get; init; }

    /// <summary>
    /// Gets the total number of failed tool calls.
    /// </summary>
    public long FailedToolCalls { get; init; }

    /// <summary>
    /// Gets the number of loop detection events.
    /// </summary>
    public long LoopDetectionEvents { get; init; }

    /// <summary>
    /// Gets the number of deduplication cache hits.
    /// </summary>
    public long DeduplicationCacheHits { get; init; }

    /// <summary>
    /// Gets the number of deduplication cache misses.
    /// </summary>
    public long DeduplicationCacheMisses { get; init; }

    /// <summary>
    /// Gets the deduplication cache hit rate (0.0 to 1.0).
    /// </summary>
    public double DeduplicationCacheHitRate { get; init; }

    /// <summary>
    /// Gets error counts by error type.
    /// </summary>
    public Dictionary<string, long> ErrorCountsByType { get; init; } = new();
}

/// <summary>
/// Represents quality metrics including reasoning confidence and response quality.
/// </summary>
public sealed class QualityMetrics
{
    /// <summary>
    /// Gets the average reasoning confidence score (0.0 to 1.0).
    /// </summary>
    public double AverageReasoningConfidence { get; init; }

    /// <summary>
    /// Gets the average response length in characters.
    /// </summary>
    public double AverageResponseLength { get; init; }

    /// <summary>
    /// Gets the percentage of responses that include a final output.
    /// </summary>
    public double FinalOutputPercentage { get; init; }

    /// <summary>
    /// Gets the average reasoning confidence by reasoning type.
    /// </summary>
    public Dictionary<ReasoningType, double> AverageConfidenceByReasoningType { get; init; } = new();

    /// <summary>
    /// Gets the distribution of reasoning confidence scores.
    /// </summary>
    public Dictionary<string, long> ConfidenceScoreDistribution { get; init; } = new();
}

/// <summary>
/// Represents resource metrics including token usage and API calls.
/// </summary>
public sealed class ResourceMetrics
{
    /// <summary>
    /// Gets the total number of input tokens consumed.
    /// </summary>
    public long TotalInputTokens { get; init; }

    /// <summary>
    /// Gets the total number of output tokens generated.
    /// </summary>
    public long TotalOutputTokens { get; init; }

    /// <summary>
    /// Gets the total number of tokens consumed (input + output).
    /// </summary>
    public long TotalTokens => TotalInputTokens + TotalOutputTokens;

    /// <summary>
    /// Gets the average number of input tokens per LLM call.
    /// </summary>
    public double AverageInputTokensPerCall { get; init; }

    /// <summary>
    /// Gets the average number of output tokens per LLM call.
    /// </summary>
    public double AverageOutputTokensPerCall { get; init; }

    /// <summary>
    /// Gets the token usage by model.
    /// </summary>
    public Dictionary<string, TokenUsage> TokenUsageByModel { get; init; } = new();

    /// <summary>
    /// Gets the API call counts by type.
    /// </summary>
    public Dictionary<string, long> ApiCallCountsByType { get; init; } = new();

    /// <summary>
    /// Gets the API call counts by model.
    /// </summary>
    public Dictionary<string, long> ApiCallCountsByModel { get; init; } = new();

    /// <summary>
    /// Gets the state store operation counts by type.
    /// </summary>
    public Dictionary<string, long> StateStoreOperationCounts { get; init; } = new();

    /// <summary>
    /// Gets the average state store operation time in milliseconds.
    /// </summary>
    public double AverageStateStoreOperationTimeMs { get; init; }
}

/// <summary>
/// Represents token usage statistics for a specific model.
/// </summary>
public sealed class TokenUsage
{
    private long _inputTokens;
    private long _outputTokens;
    private long _callCount;

    /// <summary>
    /// Gets the total number of input tokens.
    /// </summary>
    public long InputTokens => _inputTokens;

    /// <summary>
    /// Gets the total number of output tokens.
    /// </summary>
    public long OutputTokens => _outputTokens;

    /// <summary>
    /// Gets the total number of tokens (input + output).
    /// </summary>
    public long TotalTokens => _inputTokens + _outputTokens;

    /// <summary>
    /// Gets the average number of input tokens per call.
    /// </summary>
    public double AverageInputTokensPerCall => _callCount > 0 ? (double)_inputTokens / _callCount : 0;

    /// <summary>
    /// Gets the average number of output tokens per call.
    /// </summary>
    public double AverageOutputTokensPerCall => _callCount > 0 ? (double)_outputTokens / _callCount : 0;

    /// <summary>
    /// Adds tokens to the usage statistics.
    /// </summary>
    /// <param name="inputTokens">The number of input tokens to add.</param>
    /// <param name="outputTokens">The number of output tokens to add.</param>
    public void AddTokens(long inputTokens, long outputTokens)
    {
        Interlocked.Add(ref _inputTokens, inputTokens);
        Interlocked.Add(ref _outputTokens, outputTokens);
        Interlocked.Increment(ref _callCount);
    }
}

/// <summary>
/// Represents a custom metric with a numeric value and optional tags.
/// </summary>
public sealed class CustomMetric
{
    /// <summary>
    /// Gets the name of the metric.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the numeric value of the metric.
    /// </summary>
    public double Value { get; init; }

    /// <summary>
    /// Gets the timestamp when the metric was recorded.
    /// </summary>
    public DateTimeOffset RecordedAt { get; init; }

    /// <summary>
    /// Gets optional tags for categorizing the metric.
    /// </summary>
    public Dictionary<string, string> Tags { get; init; } = new();
}

/// <summary>
/// Represents a custom event with optional tags.
/// </summary>
public sealed class CustomEvent
{
    /// <summary>
    /// Gets the name of the event.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the event was recorded.
    /// </summary>
    public DateTimeOffset RecordedAt { get; init; }

    /// <summary>
    /// Gets optional tags for categorizing the event.
    /// </summary>
    public Dictionary<string, string> Tags { get; init; } = new();
}
