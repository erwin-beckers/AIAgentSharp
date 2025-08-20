using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Represents a single execution time record for performance tracking.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecutionTimeRecord
{
    public string AgentId { get; set; } = string.Empty;
    public int TurnIndex { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
    public string OperationType { get; set; } = string.Empty;
}
