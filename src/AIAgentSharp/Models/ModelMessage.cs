using System.Text.Json.Serialization;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp;

/// <summary>
///     Represents a message from the LLM, including thoughts, actions, and optional public status fields.
/// </summary>
public sealed class ModelMessage
{
    /// <summary>
    ///     Gets or sets the LLM's internal thoughts and reasoning.
    /// </summary>
    [JsonPropertyName("thoughts")]
    public string Thoughts { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the action the LLM decided to take.
    /// </summary>
    [JsonPropertyName("action")]
    public AgentAction Action { get; set; }

    /// <summary>
    ///     Gets or sets the input data for the chosen action.
    /// </summary>
    [JsonPropertyName("action_input")]
    public ActionInput ActionInput { get; set; } = new();

    /// <summary>
    ///     Gets or sets the raw action string as received from the LLM.
    /// </summary>
    [JsonIgnore]
    public string ActionRaw { get; set; } = string.Empty;

    /// <summary>
    ///     Optional public status title for UI updates (3-10 words, ≤60 chars).
    ///     Must be public-only, not contain internal reasoning.
    /// </summary>
    [JsonPropertyName("status_title")]
    public string? StatusTitle { get; set; }

    /// <summary>
    ///     Optional public status details for UI updates (≤160 chars).
    ///     Must be public-only, not contain internal reasoning.
    /// </summary>
    [JsonPropertyName("status_details")]
    public string? StatusDetails { get; set; }

    /// <summary>
    ///     Optional hint about next step for UI updates (3-12 words, ≤60 chars).
    ///     Must be public-only, not contain internal reasoning.
    /// </summary>
    [JsonPropertyName("next_step_hint")]
    public string? NextStepHint { get; set; }

    /// <summary>
    ///     Optional completion percentage for UI updates (0-100).
    /// </summary>
    [JsonPropertyName("progress_pct")]
    public int? ProgressPct { get; set; }

    /// <summary>
    ///     Optional reasoning chain information for Chain of Thought reasoning.
    /// </summary>
    [JsonPropertyName("reasoning_chain")]
    public ReasoningChain? ReasoningChain { get; set; }

    /// <summary>
    ///     Optional reasoning tree information for Tree of Thoughts reasoning.
    /// </summary>
    [JsonPropertyName("reasoning_tree")]
    public ReasoningTree? ReasoningTree { get; set; }

    /// <summary>
    ///     Optional reasoning confidence score (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("reasoning_confidence")]
    public double? ReasoningConfidence { get; set; }

    /// <summary>
    ///     Optional reasoning type used for this message.
    /// </summary>
    [JsonPropertyName("reasoning_type")]
    public ReasoningType? ReasoningType { get; set; }
}