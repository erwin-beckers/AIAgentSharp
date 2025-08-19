namespace AIAgentSharp.Agents.ChainOfThought;

/// <summary>
/// Interface for building prompts for Chain of Thought reasoning steps.
/// </summary>
public interface IChainPromptBuilder
{
    /// <summary>
    /// Builds a prompt for the analysis step.
    /// </summary>
    string BuildAnalysisPrompt(string goal, string context, IDictionary<string, ITool> tools);

    /// <summary>
    /// Builds a prompt for the planning step.
    /// </summary>
    string BuildPlanningPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> analysisInsights);

    /// <summary>
    /// Builds a prompt for the strategy step.
    /// </summary>
    string BuildStrategyPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> planningInsights);

    /// <summary>
    /// Builds a prompt for the evaluation step.
    /// </summary>
    string BuildEvaluationPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> allInsights);

    /// <summary>
    /// Builds a prompt for validation.
    /// </summary>
    string BuildValidationPrompt(string goal, List<string> insights, string conclusion, double confidence);
}
