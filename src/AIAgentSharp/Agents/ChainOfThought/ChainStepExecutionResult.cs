using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp.Agents.ChainOfThought;

/// <summary>
/// Result of executing a Chain of Thought reasoning step.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ChainStepExecutionResult
{
    public bool Success { get; set; }
    public string Reasoning { get; set; } = "";
    public double Confidence { get; set; }
    public List<string> Insights { get; set; } = new();
    public ReasoningStepType StepType { get; set; }
    public string? Error { get; set; }
}