using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AIAgentSharp;

/// <summary>
/// Represents a single reasoning step in a Chain of Thought (CoT) sequence.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReasoningStep
{
    /// <summary>
    /// Gets or sets the unique identifier for this reasoning step.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the step number in the reasoning sequence.
    /// </summary>
    [JsonPropertyName("step_number")]
    public int StepNumber { get; set; }

    /// <summary>
    /// Gets or sets the reasoning content for this step.
    /// </summary>
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence level for this reasoning step (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the type of reasoning step (analysis, decision, observation, etc.).
    /// </summary>
    [JsonPropertyName("step_type")]
    public ReasoningStepType StepType { get; set; } = ReasoningStepType.Analysis;

    /// <summary>
    /// Gets or sets any intermediate conclusions or insights from this step.
    /// </summary>
    [JsonPropertyName("insights")]
    public List<string> Insights { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when this step was created.
    /// </summary>
    [JsonPropertyName("created_utc")]
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the execution time for this reasoning step.
    /// </summary>
    [JsonPropertyName("execution_time_ms")]
    public long ExecutionTimeMs { get; set; }
}