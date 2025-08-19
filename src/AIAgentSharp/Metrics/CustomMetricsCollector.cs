using System.Collections.Concurrent;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Collects and manages custom metrics, tags, and metadata that can be
/// defined by users or specific use cases.
/// </summary>
public sealed class CustomMetricsCollector
{
    private readonly ILogger _logger;
    
    // Custom metrics storage
    private readonly ConcurrentDictionary<string, double> _customMetrics = new();
    private readonly ConcurrentDictionary<string, long> _customCounters = new();
    private readonly ConcurrentDictionary<string, string> _customTags = new();
    private readonly ConcurrentDictionary<string, object> _customMetadata = new();

    // Metric history for trending
    private readonly ConcurrentDictionary<string, ConcurrentQueue<double>> _metricHistory = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<long>> _counterHistory = new();

    // Metric categories
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, double>> _metricsByCategory = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, long>> _countersByCategory = new();

    public CustomMetricsCollector(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records a custom metric value.
    /// </summary>
    public void RecordMetric(string metricName, double value, string? category = null)
    {
        try
        {
            _customMetrics.AddOrUpdate(metricName, value, (_, _) => value);

            // Add to history
            _metricHistory.AddOrUpdate(metricName, 
                new ConcurrentQueue<double>(), 
                (_, queue) => queue);
            
            var queue = _metricHistory[metricName];
            queue.Enqueue(value);

            // Keep only the last 100 values to avoid memory issues
            while (queue.Count > 100)
            {
                queue.TryDequeue(out _);
            }

            // Add to category if specified
            if (!string.IsNullOrEmpty(category))
            {
                _metricsByCategory.AddOrUpdate(category, 
                    new ConcurrentDictionary<string, double> { [metricName] = value },
                    (_, dict) => { dict[metricName] = value; return dict; });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record metric '{metricName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Records a custom counter increment.
    /// </summary>
    public void RecordCounter(string counterName, long increment = 1, string? category = null)
    {
        try
        {
            _customCounters.AddOrUpdate(counterName, increment, (_, existing) => existing + increment);

            // Add to history
            _counterHistory.AddOrUpdate(counterName, 
                new ConcurrentQueue<long>(), 
                (_, queue) => queue);
            
            var queue = _counterHistory[counterName];
            queue.Enqueue(increment);

            // Keep only the last 100 values to avoid memory issues
            while (queue.Count > 100)
            {
                queue.TryDequeue(out _);
            }

            // Add to category if specified
            if (!string.IsNullOrEmpty(category))
            {
                _countersByCategory.AddOrUpdate(category, 
                    new ConcurrentDictionary<string, long> { [counterName] = increment },
                    (_, dict) => { dict[counterName] = increment; return dict; });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record counter '{counterName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Sets a custom tag.
    /// </summary>
    public void SetTag(string tagName, string tagValue)
    {
        try
        {
            _customTags[tagName] = tagValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to set tag '{tagName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Sets custom metadata.
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        try
        {
            _customMetadata[key] = value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to set metadata '{key}': {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates custom metrics from collected data.
    /// </summary>
    public CustomMetrics CalculateCustomMetrics()
    {
        var metrics = new Dictionary<string, double>(_customMetrics);
        var counters = new Dictionary<string, long>(_customCounters);
        var tags = new Dictionary<string, string>(_customTags);
        var metadata = new Dictionary<string, object>(_customMetadata);

        // Calculate trends for metrics
        var metricTrends = new Dictionary<string, double>();
        foreach (var kvp in _metricHistory)
        {
            var values = kvp.Value.ToArray();
            if (values.Length >= 2)
            {
                var recent = values.TakeLast(10).Average();
                var older = values.Take(values.Length - 10).Average();
                metricTrends[kvp.Key] = recent - older;
            }
        }

        // Calculate trends for counters
        var counterTrends = new Dictionary<string, long>();
        foreach (var kvp in _counterHistory)
        {
            var values = kvp.Value.ToArray();
            if (values.Length >= 2)
            {
                var recent = values.TakeLast(10).Sum();
                var older = values.Take(values.Length - 10).Sum();
                counterTrends[kvp.Key] = recent - older;
            }
        }

        return new CustomMetrics
        {
            Metrics = metrics,
            Counters = counters,
            Tags = tags,
            Metadata = metadata,
            MetricTrends = metricTrends,
            CounterTrends = counterTrends,
            MetricsByCategory = new Dictionary<string, Dictionary<string, double>>(_metricsByCategory.ToDictionary(
                kvp => kvp.Key, 
                kvp => new Dictionary<string, double>(kvp.Value))),
            CountersByCategory = new Dictionary<string, Dictionary<string, long>>(_countersByCategory.ToDictionary(
                kvp => kvp.Key, 
                kvp => new Dictionary<string, long>(kvp.Value)))
        };
    }

    /// <summary>
    /// Resets all custom metrics.
    /// </summary>
    public void Reset()
    {
        _customMetrics.Clear();
        _customCounters.Clear();
        _customTags.Clear();
        _customMetadata.Clear();
        _metricHistory.Clear();
        _counterHistory.Clear();
        _metricsByCategory.Clear();
        _countersByCategory.Clear();
    }

    /// <summary>
    /// Gets a custom metric value.
    /// </summary>
    public double? GetMetric(string metricName)
    {
        return _customMetrics.TryGetValue(metricName, out var value) ? value : null;
    }

    /// <summary>
    /// Gets a custom counter value.
    /// </summary>
    public long? GetCounter(string counterName)
    {
        return _customCounters.TryGetValue(counterName, out var value) ? value : null;
    }

    /// <summary>
    /// Gets a custom tag value.
    /// </summary>
    public string? GetTag(string tagName)
    {
        return _customTags.TryGetValue(tagName, out var value) ? value : null;
    }

    /// <summary>
    /// Gets custom metadata.
    /// </summary>
    public object? GetMetadata(string key)
    {
        return _customMetadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Gets all metrics for a specific category.
    /// </summary>
    public IReadOnlyDictionary<string, double>? GetMetricsByCategory(string category)
    {
        return _metricsByCategory.TryGetValue(category, out var metrics) 
            ? new Dictionary<string, double>(metrics) 
            : null;
    }

    /// <summary>
    /// Gets all counters for a specific category.
    /// </summary>
    public IReadOnlyDictionary<string, long>? GetCountersByCategory(string category)
    {
        return _countersByCategory.TryGetValue(category, out var counters) 
            ? new Dictionary<string, long>(counters) 
            : null;
    }

    /// <summary>
    /// Gets metric history for trending analysis.
    /// </summary>
    public double[]? GetMetricHistory(string metricName)
    {
        return _metricHistory.TryGetValue(metricName, out var history) 
            ? history.ToArray() 
            : null;
    }

    /// <summary>
    /// Gets counter history for trending analysis.
    /// </summary>
    public long[]? GetCounterHistory(string counterName)
    {
        return _counterHistory.TryGetValue(counterName, out var history) 
            ? history.ToArray() 
            : null;
    }

    /// <summary>
    /// Removes a specific metric.
    /// </summary>
    public void RemoveMetric(string metricName)
    {
        _customMetrics.TryRemove(metricName, out _);
        _metricHistory.TryRemove(metricName, out _);
        
        foreach (var category in _metricsByCategory.Values)
        {
            category.TryRemove(metricName, out _);
        }
    }

    /// <summary>
    /// Removes a specific counter.
    /// </summary>
    public void RemoveCounter(string counterName)
    {
        _customCounters.TryRemove(counterName, out _);
        _counterHistory.TryRemove(counterName, out _);
        
        foreach (var category in _countersByCategory.Values)
        {
            category.TryRemove(counterName, out _);
        }
    }
}
