namespace AIAgentSharp.Agents.ChainOfThought;

/// <summary>
/// Handles prompt construction for Chain of Thought reasoning steps.
/// </summary>
public sealed class ChainPromptBuilder : IChainPromptBuilder
{
    /// <summary>
    /// Builds a prompt for the analysis step.
    /// </summary>
    public string BuildAnalysisPrompt(string goal, string context, IDictionary<string, ITool> tools)
    {
        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));

        return $@"You are performing Chain of Thought reasoning to analyze a problem.

GOAL: {goal}
CONTEXT: {context}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Analyze the goal and break it down into its core components. Identify:
1. What needs to be accomplished
2. Key requirements and constraints
3. Potential challenges or obstacles
4. Required information or data
5. Logical steps to achieve the goal

Provide your analysis in the following JSON format:
{{
  ""reasoning"": ""Your detailed analysis here..."",
  ""confidence"": 0.85,
  ""insights"": [""insight1"", ""insight2"", ""insight3""]
}}

Focus on understanding the problem deeply before proposing solutions.";
    }

    /// <summary>
    /// Builds a prompt for the planning step.
    /// </summary>
    public string BuildPlanningPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> analysisInsights)
    {
        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));
        var insightsText = string.Join("\n", analysisInsights.Select(i => $"- {i}"));

        return $@"You are performing Chain of Thought reasoning to plan a solution.

GOAL: {goal}
CONTEXT: {context}

ANALYSIS INSIGHTS:
{insightsText}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Based on the analysis, create a detailed plan for achieving the goal. Consider:
1. Logical sequence of steps
2. Tool selection and usage strategy
3. Alternative approaches if primary fails
4. Success criteria and validation
5. Risk mitigation strategies

Provide your planning in the following JSON format:
{{
  ""reasoning"": ""Your detailed planning here..."",
  ""confidence"": 0.85,
  ""insights"": [""insight1"", ""insight2"", ""insight3""]
}}

Focus on creating a clear, actionable plan.";
    }

    /// <summary>
    /// Builds a prompt for the strategy step.
    /// </summary>
    public string BuildStrategyPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> planningInsights)
    {
        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));
        var insightsText = string.Join("\n", planningInsights.Select(i => $"- {i}"));

        return $@"You are performing Chain of Thought reasoning to develop an execution strategy.

GOAL: {goal}
CONTEXT: {context}

PLANNING INSIGHTS:
{insightsText}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Based on the planning, determine the optimal execution strategy. Decide:
1. Which tools to use and in what order
2. How to handle potential failures
3. When to proceed vs. when to reconsider
4. Key decision points and criteria
5. Success metrics and stopping conditions

Provide your strategy in the following JSON format:
{{
  ""reasoning"": ""Your detailed strategy here..."",
  ""confidence"": 0.85,
  ""insights"": [""insight1"", ""insight2"", ""insight3""]
}}

Focus on making concrete decisions about execution approach.";
    }

    /// <summary>
    /// Builds a prompt for the evaluation step.
    /// </summary>
    public string BuildEvaluationPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> allInsights)
    {
        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));
        var insightsText = string.Join("\n", allInsights.Select(i => $"- {i}"));

        return $@"You are performing Chain of Thought reasoning to evaluate the proposed solution.

GOAL: {goal}
CONTEXT: {context}

ALL INSIGHTS:
{insightsText}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Evaluate the proposed approach and provide a final conclusion. Assess:
1. Likelihood of success
2. Potential risks and mitigation
3. Alternative approaches if needed
4. Final recommendation
5. Confidence in the solution

Provide your evaluation in the following JSON format:
{{
  ""reasoning"": ""Your detailed evaluation here..."",
  ""confidence"": 0.85,
  ""insights"": [""insight1"", ""insight2"", ""insight3""],
  ""conclusion"": ""Your final conclusion and recommendation""
}}

Focus on providing a clear, actionable conclusion.";
    }

    /// <summary>
    /// Builds a prompt for validation.
    /// </summary>
    public string BuildValidationPrompt(string goal, List<string> insights, string conclusion, double confidence)
    {
        var insightsText = string.Join("\n", insights.Select(i => $"- {i}"));

        return $@"You are validating Chain of Thought reasoning quality.

GOAL: {goal}
INSIGHTS: {insightsText}
CONCLUSION: {conclusion}
CONFIDENCE: {confidence:F2}

TASK: Validate the reasoning process by checking:
1. Logical consistency of the reasoning chain
2. Completeness of analysis and planning
3. Appropriateness of the conclusion given the goal
4. Confidence level alignment with reasoning quality
5. Any gaps or assumptions that need addressing

Respond with JSON:
{{
  ""is_valid"": true/false,
  ""error"": ""Description of any issues found""
}}

Be thorough but fair in your validation.";
    }
}
