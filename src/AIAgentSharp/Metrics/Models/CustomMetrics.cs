using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Contains custom metrics, tags, and metadata that can be
/// defined by users or specific use cases.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CustomMetrics
{
    public Dictionary<string, double> Metrics { get; set; } = new();
    public Dictionary<string, long> Counters { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public Dictionary<string, double> MetricTrends { get; set; } = new();
    public Dictionary<string, long> CounterTrends { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>> MetricsByCategory { get; set; } = new();
    public Dictionary<string, Dictionary<string, long>> CountersByCategory { get; set; } = new();
}
