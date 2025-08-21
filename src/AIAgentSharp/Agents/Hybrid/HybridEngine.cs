using System.Diagnostics;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Agents.ChainOfThought;
using AIAgentSharp.Agents.TreeOfThoughts;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Agents.Hybrid;

/// <summary>
/// Implements Hybrid reasoning that combines Chain of Thought and Tree of Thoughts approaches
/// for comprehensive problem-solving. This engine starts with structured analysis and then
/// explores multiple solution paths when needed.
/// </summary>
public sealed class HybridEngine : IReasoningEngine
{
    private readonly ILlmClient _llm;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly IMetricsCollector _metricsCollector;
    private readonly ChainOfThoughtEngine _chainEngine;
    private readonly TreeOfThoughtsEngine _treeEngine;

    public HybridEngine(
        ILlmClient llm,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager,
        IMetricsCollector metricsCollector,
        ILlmCommunicator? llmCommunicator = null)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));

        // Initialize sub-engines with shared LlmCommunicator
        _chainEngine = new ChainOfThoughtEngine(_llm, _config, _logger, _eventManager, _statusManager, _metricsCollector, llmCommunicator);
        _treeEngine = new TreeOfThoughtsEngine(_llm, _config, _logger, _eventManager, _statusManager, _metricsCollector, llmCommunicator);
    }

    public ReasoningType ReasoningType => ReasoningType.Hybrid;

    /// <summary>
    /// Gets the current reasoning chain from the Chain of Thought phase.
    /// </summary>
    public ReasoningChain? CurrentChain => _chainEngine.CurrentChain;

    /// <summary>
    /// Gets the current reasoning tree from the Tree of Thoughts phase.
    /// </summary>
    public ReasoningTree? CurrentTree => _treeEngine.CurrentTree;

    /// <summary>
    /// Performs hybrid reasoning by combining Chain of Thought and Tree of Thoughts approaches.
    /// </summary>
    /// <param name="goal">The goal or objective to reason about.</param>
    /// <param name="context">Additional context information for reasoning.</param>
    /// <param name="tools">Available tools that can be used during reasoning.</param>
    /// <param name="cancellationToken">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A <see cref="ReasoningResult"/> containing the hybrid reasoning analysis and insights.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Hybrid reasoning follows a two-phase approach:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <strong>Phase 1 - Chain of Thought</strong>: Performs structured, step-by-step analysis
    /// to break down the problem and identify key components
    /// </description></item>
    /// <item><description>
    /// <strong>Phase 2 - Tree of Thoughts</strong>: Explores multiple solution paths based on
    /// the insights gained from the initial analysis
    /// </description></item>
    /// </list>
    /// <para>
    /// This approach combines the systematic nature of Chain of Thought with the exploratory
    /// capabilities of Tree of Thoughts, providing both depth and breadth in reasoning.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    public async Task<ReasoningResult> ReasonAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation($"Starting Hybrid reasoning for goal: {goal}");

        try
        {
            _statusManager.EmitStatus("reasoning", "Initializing hybrid reasoning", "Setting up combined reasoning approach", "Preparing to analyze goal");

            // Phase 1: Chain of Thought Analysis
            var chainResult = await PerformChainOfThoughtPhaseAsync(goal, context, tools, cancellationToken);
            
            if (!chainResult.Success)
            {
                _logger.LogWarning("Chain of Thought phase failed, falling back to Tree of Thoughts only");
                return await PerformTreeOfThoughtsOnlyAsync(goal, context, tools, cancellationToken);
            }

            // Phase 2: Tree of Thoughts Exploration
            var treeResult = await PerformTreeOfThoughtsPhaseAsync(goal, context, tools, chainResult, cancellationToken);

            // Combine results
            var hybridResult = CombineResults(chainResult, treeResult);
            
            stopwatch.Stop();
            hybridResult.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            // Record metrics
            _metricsCollector.RecordReasoningExecutionTime("agent", ReasoningType.Hybrid, stopwatch.ElapsedMilliseconds);
            _metricsCollector.RecordReasoningConfidence("agent", ReasoningType.Hybrid, hybridResult.Confidence);

            _logger.LogInformation($"Hybrid reasoning completed in {stopwatch.ElapsedMilliseconds}ms. Success: {hybridResult.Success}");

            return hybridResult;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hybrid reasoning was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Hybrid reasoning failed: {ex.Message}");
            return new ReasoningResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Performs the Chain of Thought phase of hybrid reasoning.
    /// </summary>
    /// <param name="goal">The goal to analyze.</param>
    /// <param name="context">Additional context.</param>
    /// <param name="tools">Available tools.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Chain of Thought reasoning result.</returns>
    private async Task<ReasoningResult> PerformChainOfThoughtPhaseAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Chain of Thought Analysis", "Performing structured step-by-step analysis", "Breaking down problem components");
        
        _logger.LogDebug("Starting Chain of Thought phase");
        return await _chainEngine.ReasonAsync(goal, context, tools, cancellationToken);
    }

    /// <summary>
    /// Performs the Tree of Thoughts phase of hybrid reasoning.
    /// </summary>
    /// <param name="goal">The goal to explore.</param>
    /// <param name="context">Additional context.</param>
    /// <param name="tools">Available tools.</param>
    /// <param name="chainResult">Results from the Chain of Thought phase.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Tree of Thoughts reasoning result.</returns>
    private async Task<ReasoningResult> PerformTreeOfThoughtsPhaseAsync(string goal, string context, IDictionary<string, ITool> tools, ReasoningResult chainResult, CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Tree of Thoughts Exploration", "Exploring multiple solution paths", "Evaluating alternatives");

        _logger.LogDebug("Starting Tree of Thoughts phase");

        // Enhance context with Chain of Thought insights
        var enhancedContext = EnhanceContextWithChainInsights(context, chainResult);
        
        return await _treeEngine.ReasonAsync(goal, enhancedContext, tools, cancellationToken);
    }

    /// <summary>
    /// Performs Tree of Thoughts reasoning as a fallback when Chain of Thought fails.
    /// </summary>
    /// <param name="goal">The goal to explore.</param>
    /// <param name="context">Additional context.</param>
    /// <param name="tools">Available tools.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Tree of Thoughts reasoning result.</returns>
    private async Task<ReasoningResult> PerformTreeOfThoughtsOnlyAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        _statusManager.EmitStatus("reasoning", "Tree of Thoughts Fallback", "Using Tree of Thoughts as fallback", "Exploring solution space");

        _logger.LogDebug("Using Tree of Thoughts as fallback");
        return await _treeEngine.ReasonAsync(goal, context, tools, cancellationToken);
    }

    /// <summary>
    /// Enhances the context with insights from the Chain of Thought phase.
    /// </summary>
    /// <param name="originalContext">The original context.</param>
    /// <param name="chainResult">Results from Chain of Thought reasoning.</param>
    /// <returns>Enhanced context with Chain of Thought insights.</returns>
    private string EnhanceContextWithChainInsights(string originalContext, ReasoningResult chainResult)
    {
        var enhancedParts = new List<string>();

        if (!string.IsNullOrEmpty(originalContext))
        {
            enhancedParts.Add(originalContext);
        }

        if (!string.IsNullOrEmpty(chainResult.Conclusion))
        {
            enhancedParts.Add($"Chain of Thought Analysis: {chainResult.Conclusion}");
        }

        if (chainResult.Chain?.Steps != null && chainResult.Chain.Steps.Any())
        {
            var keyInsights = chainResult.Chain.Steps
                .Where(s => s.Confidence > 0.7) // Only include high-confidence insights
                .Take(3) // Limit to top 3 insights
                .Select(s => s.Reasoning)
                .ToList();

            if (keyInsights.Any())
            {
                enhancedParts.Add($"Key Insights from Analysis:\n{string.Join("\n", keyInsights)}");
            }
        }

        var enhancedContext = string.Join("\n\n", enhancedParts);
        
        return enhancedContext;
    }

    /// <summary>
    /// Combines results from Chain of Thought and Tree of Thoughts phases.
    /// </summary>
    /// <param name="chainResult">Results from Chain of Thought phase.</param>
    /// <param name="treeResult">Results from Tree of Thoughts phase.</param>
    /// <returns>Combined hybrid reasoning result.</returns>
    private ReasoningResult CombineResults(ReasoningResult chainResult, ReasoningResult treeResult)
    {
        var combinedConclusion = CombineConclusions(chainResult.Conclusion, treeResult.Conclusion);
        var combinedConfidence = CalculateCombinedConfidence(chainResult.Confidence, treeResult.Confidence);

        return new ReasoningResult
        {
            Success = chainResult.Success && treeResult.Success,
            Conclusion = combinedConclusion,
            Confidence = combinedConfidence,
            Chain = chainResult.Chain,
            Tree = treeResult.Tree,
            Metadata = new Dictionary<string, object>
            {
                ["method"] = "hybrid",
                ["chain_confidence"] = chainResult.Confidence,
                ["tree_confidence"] = treeResult.Confidence,
                ["combined_confidence"] = combinedConfidence,
                ["chain_success"] = chainResult.Success,
                ["tree_success"] = treeResult.Success
            }
        };
    }

    /// <summary>
    /// Combines conclusions from both reasoning phases.
    /// </summary>
    /// <param name="chainConclusion">Conclusion from Chain of Thought.</param>
    /// <param name="treeConclusion">Conclusion from Tree of Thoughts.</param>
    /// <returns>Combined conclusion.</returns>
    private string CombineConclusions(string chainConclusion, string treeConclusion)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(chainConclusion))
        {
            parts.Add($"Analysis: {chainConclusion}");
        }

        if (!string.IsNullOrEmpty(treeConclusion))
        {
            parts.Add($"Exploration: {treeConclusion}");
        }

        if (parts.Count == 0)
        {
            return "Hybrid reasoning completed but no specific conclusions were reached.";
        }

        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// Calculates combined confidence from both reasoning phases.
    /// </summary>
    /// <param name="chainConfidence">Confidence from Chain of Thought.</param>
    /// <param name="treeConfidence">Confidence from Tree of Thoughts.</param>
    /// <returns>Combined confidence score.</returns>
    private double CalculateCombinedConfidence(double chainConfidence, double treeConfidence)
    {
        // Weight Chain of Thought slightly higher as it provides structured analysis
        const double chainWeight = 0.6;
        const double treeWeight = 0.4;

        return (chainConfidence * chainWeight) + (treeConfidence * treeWeight);
    }
}
