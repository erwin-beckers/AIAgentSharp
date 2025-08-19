namespace AIAgentSharp.Metrics;

/// <summary>
/// Represents a custom event with optional tags.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CustomEvent
{
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}
