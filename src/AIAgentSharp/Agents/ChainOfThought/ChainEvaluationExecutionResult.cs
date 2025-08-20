using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp.Agents.ChainOfThought;

/// <summary>
/// Result of executing the evaluation step.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ChainEvaluationExecutionResult
{
    public bool Success { get; set; }
    public string Reasoning { get; set; } = "";
    public double Confidence { get; set; }
    public List<string> Insights { get; set; } = new();
    public string Conclusion { get; set; } = "";
    public ReasoningStepType StepType { get; set; }
    public string? Error { get; set; }
}