using System.Text.Json.Serialization;

namespace AIAgentSharp;

/// <summary>
/// Represents a complete Chain of Thought reasoning sequence.
/// </summary>
public sealed class ReasoningChain
{
    /// <summary>
    /// Gets or sets the unique identifier for this reasoning chain.
    /// </summary>
    [JsonPropertyName("chain_id")]
    public string ChainId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the goal or problem being addressed by this reasoning chain.
    /// </summary>
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordered sequence of reasoning steps.
    /// </summary>
    [JsonPropertyName("steps")]
    public List<ReasoningStep> Steps { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall confidence in the final conclusion.
    /// </summary>
    [JsonPropertyName("final_confidence")]
    public double FinalConfidence { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the final conclusion or decision reached.
    /// </summary>
    [JsonPropertyName("final_conclusion")]
    public string FinalConclusion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this reasoning chain was created.
    /// </summary>
    [JsonPropertyName("created_utc")]
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this reasoning chain was completed.
    /// </summary>
    [JsonPropertyName("completed_utc")]
    public DateTimeOffset? CompletedUtc { get; set; }

    /// <summary>
    /// Gets or sets the total execution time for the entire reasoning chain.
    /// </summary>
    [JsonPropertyName("total_execution_time_ms")]
    public long TotalExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets metadata about the reasoning chain.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the current step number (1-based).
    /// </summary>
    [JsonIgnore]
    public int CurrentStepNumber => Steps.Count + 1;

    /// <summary>
    /// Gets a value indicating whether this reasoning chain is complete.
    /// </summary>
    [JsonIgnore]
    public bool IsComplete => CompletedUtc.HasValue;

    /// <summary>
    /// Adds a new reasoning step to the chain.
    /// </summary>
    /// <param name="reasoning">The reasoning content.</param>
    /// <param name="stepType">The type of reasoning step.</param>
    /// <param name="confidence">The confidence level (0.0 to 1.0).</param>
    /// <param name="insights">Optional insights from this step.</param>
    /// <returns>The created reasoning step.</returns>
    public ReasoningStep AddStep(string reasoning, ReasoningStepType stepType = ReasoningStepType.Analysis, double confidence = 0.5, List<string>? insights = null)
    {
        var step = new ReasoningStep
        {
            StepNumber = CurrentStepNumber,
            Reasoning = reasoning,
            StepType = stepType,
            Confidence = Math.Max(0.0, Math.Min(1.0, confidence)),
            Insights = insights ?? new List<string>()
        };

        Steps.Add(step);
        return step;
    }

    /// <summary>
    /// Completes the reasoning chain with a final conclusion.
    /// </summary>
    /// <param name="conclusion">The final conclusion.</param>
    /// <param name="confidence">The confidence in the conclusion.</param>
    public void Complete(string conclusion, double confidence = 0.5)
    {
        FinalConclusion = conclusion;
        FinalConfidence = Math.Max(0.0, Math.Min(1.0, confidence));
        CompletedUtc = DateTimeOffset.UtcNow;
        TotalExecutionTimeMs = Steps.Sum(s => s.ExecutionTimeMs);
    }
}
