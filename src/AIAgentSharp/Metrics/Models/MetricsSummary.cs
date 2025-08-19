namespace AIAgentSharp.Metrics;

/// <summary>
/// Provides a high-level summary of all metrics for quick overview
/// and dashboard displays.
/// </summary>
public sealed class MetricsSummary
{
    public long TotalAgentRuns { get; set; }
    public long SuccessfulAgentRuns { get; set; }
    public long FailedAgentRuns { get; set; }

    public long TotalAgentSteps { get; set; }
    public long SuccessfulAgentSteps { get; set; }
    public long FailedAgentSteps { get; set; }

    public long TotalLlmCalls { get; set; }
    public long SuccessfulLlmCalls { get; set; }
    public long FailedLlmCalls { get; set; }

    public long TotalToolCalls { get; set; }
    public long SuccessfulToolCalls { get; set; }
    public long FailedToolCalls { get; set; }

    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }

    public double AverageAgentRunTimeMs { get; set; }
    public double AverageAgentStepTimeMs { get; set; }
    public double AverageLlmCallTimeMs { get; set; }
    public double AverageToolCallTimeMs { get; set; }

    public double SuccessRate { get; set; }
    public double ErrorRate { get; set; }
    public double QualityPercentage { get; set; }
    public double ReasoningAccuracyPercentage { get; set; }
    public double ValidationPassRate { get; set; }

    public double RequestsPerSecond { get; set; }
    public Dictionary<string, long> ErrorCounts { get; set; } = new();
}
