using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Contains all metrics from all collectors in a unified structure.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AllMetrics
{
    public PerformanceMetrics Performance { get; set; } = new();
    public OperationalMetrics Operational { get; set; } = new();
    public ResourceMetrics Resource { get; set; } = new();
    public QualityMetrics Quality { get; set; } = new();
    public CustomMetrics Custom { get; set; } = new();
}
