using System.Diagnostics;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// Implements Chain of Thought (CoT) reasoning for structured step-by-step thinking.
/// </summary>
public sealed class ChainOfThoughtEngine : IChainOfThoughtEngine
{
    private readonly ILlmClient _llm;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;

    public ChainOfThoughtEngine(
        ILlmClient llm,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
    }

    public ReasoningType ReasoningType => ReasoningType.ChainOfThought;

    public ReasoningChain? CurrentChain { get; private set; }

    /// <summary>
    /// Performs Chain of Thought reasoning to analyze the goal and generate insights.
    /// </summary>
    /// <param name="goal">The goal or objective to reason about.</param>
    /// <param name="context">Additional context information for reasoning.</param>
    /// <param name="tools">Available tools that can be used during reasoning.</param>
    /// <param name="cancellationToken">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A <see cref="ReasoningResult"/> containing the reasoning analysis and insights.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Chain of Thought reasoning follows a sequential, step-by-step approach to problem-solving:
    /// </para>
    /// <list type="number">
    /// <item><description>Analyzes the goal and context</description></item>
    /// <item><description>Breaks down complex problems into manageable steps</description></item>
    /// <item><description>Considers available tools and their applicability</description></item>
    /// <item><description>Evaluates potential approaches and strategies</description></item>
    /// <item><description>Provides confidence levels and recommendations</description></item>
    /// </list>
    /// <para>
    /// This approach is particularly effective for tasks that benefit from systematic,
    /// linear thinking and clear step-by-step reasoning.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    public async Task<ReasoningResult> ReasonAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation($"Starting Chain of Thought reasoning for goal: {goal}");

        try
        {
            // Initialize reasoning chain
            CurrentChain = new ReasoningChain
            {
                Goal = goal,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            _statusManager.EmitStatus("reasoning", "Initializing reasoning", "Setting up structured thinking process", "Preparing to analyze goal");

            // Perform step-by-step reasoning
            var result = await PerformReasoningStepsAsync(goal, context, tools, cancellationToken);
            
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Chain = CurrentChain;

            _logger.LogInformation($"Chain of Thought reasoning completed in {stopwatch.ElapsedMilliseconds}ms. Success: {result.Success}");

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError($"Chain of Thought reasoning failed: {ex.Message}");

            return new ReasoningResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Chain = CurrentChain
            };
        }
    }

    public ReasoningStep AddStep(string reasoning, ReasoningStepType stepType = ReasoningStepType.Analysis, double confidence = 0.5, List<string>? insights = null)
    {
        if (CurrentChain == null)
        {
            throw new InvalidOperationException("No active reasoning chain. Call ReasonAsync first.");
        }

        var step = CurrentChain.AddStep(reasoning, stepType, confidence, insights);
        _logger.LogDebug($"Added reasoning step {step.StepNumber}: {stepType} - {reasoning.Substring(0, Math.Min(100, reasoning.Length))}...");

        return step;
    }

    public void CompleteChain(string conclusion, double confidence = 0.5)
    {
        if (CurrentChain == null)
        {
            throw new InvalidOperationException("No active reasoning chain. Call ReasonAsync first.");
        }

        CurrentChain.Complete(conclusion, confidence);
        _logger.LogInformation($"Completed reasoning chain with conclusion: {conclusion}");
    }

    private async Task<ReasoningResult> PerformReasoningStepsAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        var stepCount = 0;
        var totalConfidence = 0.0;
        var insights = new List<string>();

        // Step 1: Initial Analysis
        var analysisStep = await PerformAnalysisStepAsync(goal, context, tools, cancellationToken);
        if (!analysisStep.Success)
        {
            return new ReasoningResult { Success = false, Error = analysisStep.Error };
        }

        stepCount++;
        totalConfidence += analysisStep.Confidence;
        insights.AddRange(analysisStep.Insights);

        // Step 2: Planning
        var planningStep = await PerformPlanningStepAsync(goal, context, tools, analysisStep.Insights, cancellationToken);
        if (!planningStep.Success)
        {
            return new ReasoningResult { Success = false, Error = planningStep.Error };
        }

        stepCount++;
        totalConfidence += planningStep.Confidence;
        insights.AddRange(planningStep.Insights);

        // Step 3: Execution Strategy
        var strategyStep = await PerformStrategyStepAsync(goal, context, tools, planningStep.Insights, cancellationToken);
        if (!strategyStep.Success)
        {
            return new ReasoningResult { Success = false, Error = strategyStep.Error };
        }

        stepCount++;
        totalConfidence += strategyStep.Confidence;
        insights.AddRange(strategyStep.Insights);

        // Step 4: Evaluation
        var evaluationStep = await PerformEvaluationStepAsync(goal, context, tools, insights, cancellationToken);
        if (!evaluationStep.Success)
        {
            return new ReasoningResult { Success = false, Error = evaluationStep.Error };
        }

        stepCount++;
        totalConfidence += evaluationStep.Confidence;
        insights.AddRange(evaluationStep.Insights);

        // Calculate final confidence and conclusion
        var finalConfidence = totalConfidence / stepCount;
        var conclusion = evaluationStep.Conclusion;

        // Validate reasoning if enabled
        if (_config.EnableReasoningValidation)
        {
            var validationResult = await ValidateReasoningAsync(goal, insights, conclusion, finalConfidence, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning($"Reasoning validation failed: {validationResult.Error}");
                if (finalConfidence < _config.MinReasoningConfidence)
                {
                    return new ReasoningResult
                    {
                        Success = false,
                        Error = $"Reasoning confidence {finalConfidence:F2} below threshold {_config.MinReasoningConfidence:F2}",
                        Conclusion = conclusion,
                        Confidence = finalConfidence
                    };
                }
            }
        }

        CompleteChain(conclusion, finalConfidence);

        return new ReasoningResult
        {
            Success = true,
            Conclusion = conclusion,
            Confidence = finalConfidence,
            Metadata = new Dictionary<string, object>
            {
                ["steps_completed"] = stepCount,
                ["total_insights"] = insights.Count,
                ["reasoning_type"] = "ChainOfThought"
            }
        };
    }

    private async Task<ReasoningStepResult> PerformAnalysisStepAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Analyzing problem", "Breaking down the goal into components", "Understanding requirements");

        var prompt = BuildAnalysisPrompt(goal, context, tools);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);

        var reasoning = ExtractReasoningFromResponse(response);
        var confidence = ExtractConfidenceFromResponse(response);
        var insights = ExtractInsightsFromResponse(response);

        var step = AddStep(reasoning, ReasoningStepType.Analysis, confidence, insights);

        return new ReasoningStepResult
        {
            Success = true,
            Confidence = confidence,
            Insights = insights,
            Step = step
        };
    }

    private async Task<ReasoningStepResult> PerformPlanningStepAsync(string goal, string context, IDictionary<string, ITool> tools, List<string> analysisInsights, CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Planning approach", "Developing solution strategy", "Creating execution plan");

        var prompt = BuildPlanningPrompt(goal, context, tools, analysisInsights);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);

        var reasoning = ExtractReasoningFromResponse(response);
        var confidence = ExtractConfidenceFromResponse(response);
        var insights = ExtractInsightsFromResponse(response);

        var step = AddStep(reasoning, ReasoningStepType.Planning, confidence, insights);

        return new ReasoningStepResult
        {
            Success = true,
            Confidence = confidence,
            Insights = insights,
            Step = step
        };
    }

    private async Task<ReasoningStepResult> PerformStrategyStepAsync(string goal, string context, IDictionary<string, ITool> tools, List<string> planningInsights, CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Developing strategy", "Determining execution approach", "Selecting optimal path");

        var prompt = BuildStrategyPrompt(goal, context, tools, planningInsights);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);

        var reasoning = ExtractReasoningFromResponse(response);
        var confidence = ExtractConfidenceFromResponse(response);
        var insights = ExtractInsightsFromResponse(response);

        var step = AddStep(reasoning, ReasoningStepType.Decision, confidence, insights);

        return new ReasoningStepResult
        {
            Success = true,
            Confidence = confidence,
            Insights = insights,
            Step = step
        };
    }

    private async Task<ReasoningStepResult> PerformEvaluationStepAsync(string goal, string context, IDictionary<string, ITool> tools, List<string> allInsights, CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Evaluating solution", "Assessing approach quality", "Finalizing decision");

        var prompt = BuildEvaluationPrompt(goal, context, tools, allInsights);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);

        var reasoning = ExtractReasoningFromResponse(response);
        var confidence = ExtractConfidenceFromResponse(response);
        var insights = ExtractInsightsFromResponse(response);
        var conclusion = ExtractConclusionFromResponse(response);

        var step = AddStep(reasoning, ReasoningStepType.Evaluation, confidence, insights);

        return new ReasoningStepResult
        {
            Success = true,
            Confidence = confidence,
            Insights = insights,
            Conclusion = conclusion,
            Step = step
        };
    }

    private async Task<ValidationResult> ValidateReasoningAsync(string goal, List<string> insights, string conclusion, double confidence, CancellationToken cancellationToken)
    {
        var prompt = BuildValidationPrompt(goal, insights, conclusion, confidence);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);

        var isValid = ExtractValidationResult(response);
        var error = isValid ? null : ExtractValidationError(response);

        return new ValidationResult
        {
            IsValid = isValid,
            Error = error
        };
    }

    private string BuildAnalysisPrompt(string goal, string context, IDictionary<string, ITool> tools)
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

    private string BuildPlanningPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> analysisInsights)
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

    private string BuildStrategyPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> planningInsights)
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

    private string BuildEvaluationPrompt(string goal, string context, IDictionary<string, ITool> tools, List<string> allInsights)
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

    private string BuildValidationPrompt(string goal, List<string> insights, string conclusion, double confidence)
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

    private string ExtractReasoningFromResponse(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            return json.RootElement.GetProperty("reasoning").GetString() ?? "";
        }
        catch
        {
            return response;
        }
    }

    private double ExtractConfidenceFromResponse(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            return json.RootElement.GetProperty("confidence").GetDouble();
        }
        catch
        {
            return 0.5;
        }
    }

    private List<string> ExtractInsightsFromResponse(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            var insightsArray = json.RootElement.GetProperty("insights");
            return insightsArray.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private string ExtractConclusionFromResponse(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            return json.RootElement.GetProperty("conclusion").GetString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private bool ExtractValidationResult(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            return json.RootElement.GetProperty("is_valid").GetBoolean();
        }
        catch
        {
            return true; // Default to valid if parsing fails
        }
    }

    private string ExtractValidationError(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            return json.RootElement.GetProperty("error").GetString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private class ReasoningStepResult
    {
        public bool Success { get; set; }
        public double Confidence { get; set; }
        public List<string> Insights { get; set; } = new();
        public string Conclusion { get; set; } = "";
        public ReasoningStep Step { get; set; } = null!;
        public string? Error { get; set; }
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
    }
}
