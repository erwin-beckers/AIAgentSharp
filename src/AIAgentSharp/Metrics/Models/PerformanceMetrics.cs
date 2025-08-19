namespace AIAgentSharp.Metrics;

/// <summary>
/// Contains performance-related metrics including execution times,
/// throughput, and percentile calculations.
/// </summary>
public sealed class PerformanceMetrics
{
    public long TotalAgentRuns { get; set; }
    public long TotalAgentSteps { get; set; }
    public long TotalLlmCalls { get; set; }
    public long TotalToolCalls { get; set; }
    public long TotalReasoningOperations { get; set; }

    public double AverageAgentRunTimeMs { get; set; }
    public double AverageAgentStepTimeMs { get; set; }
    public double AverageLlmCallTimeMs { get; set; }
    public double AverageToolCallTimeMs { get; set; }
    public double AverageReasoningTimeMs { get; set; }

    public double RequestsPerSecond { get; set; }

    public double P95AgentRunTimeMs { get; set; }
    public double P95AgentStepTimeMs { get; set; }
    public double P95LlmCallTimeMs { get; set; }
    public double P95ToolCallTimeMs { get; set; }
    public double P95ReasoningTimeMs { get; set; }

    public double P99AgentRunTimeMs { get; set; }
    public double P99AgentStepTimeMs { get; set; }
    public double P99LlmCallTimeMs { get; set; }
    public double P99ToolCallTimeMs { get; set; }
    public double P99ReasoningTimeMs { get; set; }

    public Dictionary<string, long> ExecutionTimesByAgent { get; set; } = new();
    public Dictionary<string, long> ExecutionTimesByStep { get; set; } = new();
    public Dictionary<string, long> ExecutionTimesByLlmCall { get; set; } = new();
    public Dictionary<string, long> ExecutionTimesByToolCall { get; set; } = new();
    public Dictionary<string, long> ExecutionTimesByReasoning { get; set; } = new();
    public double TotalExecutionTimeMs { get; set; }
}
