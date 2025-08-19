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












