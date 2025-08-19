using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Contains quality-related metrics including response quality,
/// reasoning accuracy, and validation results.
/// </summary>
public sealed class QualityMetrics
{
    public long TotalResponses { get; set; }
    public long HighQualityResponses { get; set; }
    public long MediumQualityResponses { get; set; }
    public long LowQualityResponses { get; set; }
    public double QualityPercentage { get; set; }

    public long TotalReasoningSteps { get; set; }
    public long SuccessfulReasoningSteps { get; set; }
    public long FailedReasoningSteps { get; set; }
    public double ReasoningAccuracyPercentage { get; set; }

    public long TotalValidations { get; set; }
    public long PassedValidations { get; set; }
    public long FailedValidations { get; set; }
    public double ValidationPassRate { get; set; }

    public double AverageResponseTimeMs { get; set; }
    public double AverageReasoningConfidence { get; set; }
    public double AverageResponseLength { get; set; }
    public double FinalOutputPercentage { get; set; }
    public Dictionary<ReasoningType, double> AverageConfidenceByReasoningType { get; set; } = new();
    public Dictionary<string, long> ConfidenceScoreDistribution { get; set; } = new();

    public Dictionary<string, long> QualityScoresByAgent { get; set; } = new();
    public Dictionary<string, long> ReasoningAccuracyByType { get; set; } = new();
    public Dictionary<string, long> ValidationResultsByType { get; set; } = new();
    public Dictionary<string, long> AverageResponseTimeByAgent { get; set; } = new();
}
