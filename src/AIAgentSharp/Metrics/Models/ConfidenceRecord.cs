using System.Diagnostics.CodeAnalysis;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Represents a confidence score record for reasoning operations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConfidenceRecord
{
    public string AgentId { get; set; } = string.Empty;
    public ReasoningType ReasoningType { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
