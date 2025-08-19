using System.Diagnostics;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Agents.ChainOfThought;

/// <summary>
/// Implements Chain of Thought (CoT) reasoning for structured step-by-step thinking.
/// </summary>
public sealed class ChainOfThoughtEngine : IChainOfThoughtEngine
{
    private readonly ILlmClient _llm;
    private readonly ChainPromptBuilder _promptBuilder;
    private readonly ChainStepExecutor _stepExecutor;
    private readonly ChainOperations _chainOperations;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly IMetricsCollector _metricsCollector;

    public ChainOfThoughtEngine(
        ILlmClient llm,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager,
        IMetricsCollector metricsCollector)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));

        // Initialize components
        _promptBuilder = new ChainPromptBuilder();
        var llmCommunicator = new LlmCommunicator(llm, config, logger, eventManager, statusManager, metricsCollector);
        _stepExecutor = new ChainStepExecutor(_promptBuilder, llmCommunicator, statusManager);
        _chainOperations = new ChainOperations(logger);
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
            CurrentChain = _chainOperations.CreateChain(goal);

            _statusManager.EmitStatus("reasoning", "Initializing reasoning", "Setting up structured thinking process", "Preparing to analyze goal");

            // Perform step-by-step reasoning
            var result = await PerformReasoningStepsAsync(goal, context, tools, cancellationToken);
            
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Chain = CurrentChain;

            // Record metrics for reasoning completion
            _metricsCollector.RecordReasoningExecutionTime("agent", ReasoningType.ChainOfThought, stopwatch.ElapsedMilliseconds);
            _metricsCollector.RecordReasoningConfidence("agent", ReasoningType.ChainOfThought, result.Confidence);

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

        return _chainOperations.AddStep(CurrentChain, reasoning, stepType, confidence, insights);
    }

    public void CompleteChain(string conclusion, double confidence = 0.5)
    {
        if (CurrentChain == null)
        {
            throw new InvalidOperationException("No active reasoning chain. Call ReasonAsync first.");
        }

        _chainOperations.CompleteChain(CurrentChain, conclusion, confidence);
    }

    private async Task<ReasoningResult> PerformReasoningStepsAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        var stepCount = 0;
        var totalConfidence = 0.0;
        var insights = new List<string>();

        // Step 1: Initial Analysis
        var analysisStep = await _stepExecutor.PerformAnalysisStepAsync(goal, context, tools, cancellationToken);
        if (!analysisStep.Success)
        {
            return new ReasoningResult { Success = false, Error = analysisStep.Error };
        }

        var analysisReasoningStep = _chainOperations.AddStep(CurrentChain!, analysisStep.Reasoning, analysisStep.StepType, analysisStep.Confidence, analysisStep.Insights);
        stepCount++;
        totalConfidence += analysisStep.Confidence;
        insights.AddRange(analysisStep.Insights);

        // Step 2: Planning
        var planningStep = await _stepExecutor.PerformPlanningStepAsync(goal, context, tools, analysisStep.Insights, cancellationToken);
        if (!planningStep.Success)
        {
            return new ReasoningResult { Success = false, Error = planningStep.Error };
        }

        var planningReasoningStep = _chainOperations.AddStep(CurrentChain!, planningStep.Reasoning, planningStep.StepType, planningStep.Confidence, planningStep.Insights);
        stepCount++;
        totalConfidence += planningStep.Confidence;
        insights.AddRange(planningStep.Insights);

        // Step 3: Execution Strategy
        var strategyStep = await _stepExecutor.PerformStrategyStepAsync(goal, context, tools, planningStep.Insights, cancellationToken);
        if (!strategyStep.Success)
        {
            return new ReasoningResult { Success = false, Error = strategyStep.Error };
        }

        var strategyReasoningStep = _chainOperations.AddStep(CurrentChain!, strategyStep.Reasoning, strategyStep.StepType, strategyStep.Confidence, strategyStep.Insights);
        stepCount++;
        totalConfidence += strategyStep.Confidence;
        insights.AddRange(strategyStep.Insights);

        // Step 4: Evaluation
        var evaluationStep = await _stepExecutor.PerformEvaluationStepAsync(goal, context, tools, insights, cancellationToken);
        if (!evaluationStep.Success)
        {
            return new ReasoningResult { Success = false, Error = evaluationStep.Error };
        }

        var evaluationReasoningStep = _chainOperations.AddStep(CurrentChain!, evaluationStep.Reasoning, evaluationStep.StepType, evaluationStep.Confidence, evaluationStep.Insights);
        stepCount++;
        totalConfidence += evaluationStep.Confidence;
        insights.AddRange(evaluationStep.Insights);

        // Calculate final confidence and conclusion
        var finalConfidence = totalConfidence / stepCount;
        var conclusion = evaluationStep.Conclusion;

        // Validate reasoning if enabled
        if (_config.EnableReasoningValidation)
        {
            var validationResult = await _stepExecutor.PerformValidationAsync(goal, insights, conclusion, finalConfidence, cancellationToken);
            
            // Record validation metrics
            _metricsCollector.RecordValidation(string.Empty, ReasoningType.ChainOfThought.ToString(), validationResult.IsValid, validationResult.Error);
            
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

        _chainOperations.CompleteChain(CurrentChain!, conclusion, finalConfidence);

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
}
