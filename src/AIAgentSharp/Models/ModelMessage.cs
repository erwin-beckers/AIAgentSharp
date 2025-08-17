using System.Text.Json.Serialization;

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
}