namespace AIAgentSharp;

/// <summary>
/// Represents the type of reasoning step.
/// </summary>
public enum ReasoningStepType
{
    /// <summary>
    /// Analysis of the current situation or problem.
    /// </summary>
    Analysis,

    /// <summary>
    /// Decision making step.
    /// </summary>
    Decision,

    /// <summary>
    /// Observation of results or outcomes.
    /// </summary>
    Observation,

    /// <summary>
    /// Planning or strategy development.
    /// </summary>
    Planning,

    /// <summary>
    /// Evaluation of options or alternatives.
    /// </summary>
    Evaluation,

    /// <summary>
    /// Synthesis of information or conclusions.
    /// </summary>
    Synthesis
}