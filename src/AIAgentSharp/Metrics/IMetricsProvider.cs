namespace AIAgentSharp.Metrics;

/// <summary>
/// Defines the interface for providing access to collected metrics data.
/// This interface allows users to retrieve comprehensive metrics about AIAgentSharp operations.
/// </summary>
/// <remarks>
/// <para>
/// The metrics provider offers access to all collected metrics organized by category.
/// Metrics are collected in real-time and can be retrieved at any time for monitoring
/// and analysis purposes.
/// </para>
/// <para>
/// The provider supports both snapshot retrieval and continuous monitoring through
/// events. All operations are thread-safe and designed for concurrent access.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public interface IMetricsProvider
{
    /// <summary>
    /// Gets a snapshot of all collected metrics data.
    /// </summary>
    /// <returns>
    /// A <see cref="MetricsData"/> object containing all collected metrics.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns a point-in-time snapshot of all metrics collected since
    /// the last reset or since the application started. The data is organized into
    /// logical categories for easy analysis.
    /// </para>
    /// <para>
    /// The returned data is immutable and represents the state at the time of the call.
    /// For real-time monitoring, consider subscribing to the <see cref="MetricsUpdated"/>
    /// event.
    /// </para>
    /// </remarks>
    MetricsData GetMetrics();

    /// <summary>
    /// Gets metrics for a specific agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <returns>
    /// A <see cref="MetricsData"/> object containing metrics for the specified agent,
    /// or null if no metrics are available for the agent.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method filters the collected metrics to include only data related to
    /// the specified agent. This is useful for monitoring individual agent performance
    /// and behavior.
    /// </para>
    /// <para>
    /// If no metrics are available for the specified agent, the method returns null.
    /// </para>
    /// </remarks>
    MetricsData? GetAgentMetrics(string agentId);

    /// <summary>
    /// Gets metrics for a specific time range.
    /// </summary>
    /// <param name="startTime">The start time for the metrics range.</param>
    /// <param name="endTime">The end time for the metrics range.</param>
    /// <returns>
    /// A <see cref="MetricsData"/> object containing metrics for the specified time range.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method filters the collected metrics to include only data recorded within
    /// the specified time range. This is useful for analyzing performance over specific
    /// periods or for generating time-based reports.
    /// </para>
    /// <para>
    /// The time range is inclusive of both start and end times.
    /// </para>
    /// </remarks>
    MetricsData GetMetricsForTimeRange(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// Resets all collected metrics, clearing the current data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method clears all collected metrics and resets counters to zero.
    /// Use this method when you want to start fresh metrics collection, such as
    /// after deploying a new version or when starting a new monitoring period.
    /// </para>
    /// <para>
    /// After calling this method, all subsequent metric calls will start from zero.
    /// </para>
    /// </remarks>
    void ResetMetrics();

    /// <summary>
    /// Gets all collected metrics data in a comprehensive format.
    /// </summary>
    /// <returns>
    /// An <see cref="AllMetrics"/> object containing all collected metrics from all collectors.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns all metrics data including performance, operational, quality, 
    /// resource, and custom metrics. Users can then export this data to any format they prefer.
    /// </para>
    /// <para>
    /// The returned data includes metrics from all collectors and represents the complete
    /// state of the metrics system at the time of the call.
    /// </para>
    /// </remarks>
    AllMetrics GetAllMetrics();

    /// <summary>
    /// Event that is raised when metrics are updated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this event to receive real-time notifications when new metrics
    /// are recorded. This is useful for building real-time dashboards or for
    /// triggering alerts based on metric thresholds.
    /// </para>
    /// <para>
    /// The event provides the updated metrics data and information about what
    /// changed since the last update.
    /// </para>
    /// </remarks>
    event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;
}

/// <summary>
/// Event arguments for metrics update events.
/// </summary>
public sealed class MetricsUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the updated metrics data.
    /// </summary>
    public MetricsData Metrics { get; init; } = new();

    /// <summary>
    /// Gets the timestamp when the metrics were updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets information about what metrics were updated.
    /// </summary>
    public List<string> UpdatedMetrics { get; init; } = new();
}
