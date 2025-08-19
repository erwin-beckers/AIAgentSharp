namespace AIAgentSharp.Metrics;

/// <summary>
/// Contains operational metrics including success/failure counts,
/// error tracking, and operational health indicators.
/// </summary>
public sealed class OperationalMetrics
{
    public long SuccessfulAgentRuns { get; set; }
    public long FailedAgentRuns { get; set; }
    public long SuccessfulAgentSteps { get; set; }
    public long FailedAgentSteps { get; set; }
    public long SuccessfulLlmCalls { get; set; }
    public long FailedLlmCalls { get; set; }
    public long SuccessfulToolCalls { get; set; }
    public long FailedToolCalls { get; set; }

    public double SuccessRate { get; set; }
    public double ErrorRate { get; set; }
    public double AgentRunSuccessRate { get; set; }
    public double AgentStepSuccessRate { get; set; }
    public double LlmCallSuccessRate { get; set; }
    public double ToolCallSuccessRate { get; set; }
    public double DeduplicationCacheHitRate { get; set; }

    public long LoopDetectionEvents { get; set; }
    public long DeduplicationCacheHits { get; set; }
    public long DeduplicationCacheMisses { get; set; }

    public Dictionary<string, long> ErrorCounts { get; set; } = new();
    public Dictionary<string, long> SuccessCountsByAgent { get; set; } = new();
    public Dictionary<string, long> FailureCountsByAgent { get; set; } = new();
    public Dictionary<string, long> ErrorCountsByType { get; set; } = new();
}
