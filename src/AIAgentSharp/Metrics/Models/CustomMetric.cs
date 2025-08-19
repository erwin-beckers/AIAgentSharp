namespace AIAgentSharp.Metrics;

/// <summary>
/// Represents a custom metric with a numeric value and optional tags.
/// </summary>
public sealed class CustomMetric
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}
