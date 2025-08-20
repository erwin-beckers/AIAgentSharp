namespace AIAgentSharp;

/// <summary>
/// Represents the type of thought in a Tree of Thoughts.
/// </summary>
public enum ThoughtType
{
    /// <summary>
    /// Initial hypothesis or assumption.
    /// </summary>
    Hypothesis,

    /// <summary>
    /// Observation or fact.
    /// </summary>
    Observation,

    /// <summary>
    /// Decision or choice point.
    /// </summary>
    Decision,

    /// <summary>
    /// Analysis or reasoning.
    /// </summary>
    Analysis,

    /// <summary>
    /// Conclusion or result.
    /// </summary>
    Conclusion,

    /// <summary>
    /// Question or uncertainty.
    /// </summary>
    Question,

    /// <summary>
    /// Alternative or option.
    /// </summary>
    Alternative
}