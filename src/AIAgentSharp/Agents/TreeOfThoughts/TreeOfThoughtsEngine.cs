using System.Diagnostics;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Agents.TreeOfThoughts.Strategies;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Agents.TreeOfThoughts;

/// <summary>
/// Implements Tree of Thoughts (ToT) reasoning for branching exploration of multiple solution paths.
/// </summary>
public sealed class TreeOfThoughtsEngine : ITreeOfThoughtsEngine
{
    private readonly TreeThoughtGenerator _thoughtGenerator;
    private readonly TreeNodeEvaluator _nodeEvaluator;
    private readonly TreeConclusionGenerator _conclusionGenerator;
    private readonly TreeOperations _treeOperations;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly IMetricsCollector _metricsCollector;

    public TreeOfThoughtsEngine(
        ILlmClient llm,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager,
        IMetricsCollector metricsCollector,
        ILlmCommunicator? llmCommunicator = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));

        // Initialize components using shared LLM communication
        var communicator = llmCommunicator ?? new LlmCommunicator(llm, config, logger, eventManager, statusManager, metricsCollector);
        var totCommunicator = new TreeOfThoughtsCommunicator(communicator);
        _thoughtGenerator = new TreeThoughtGenerator(totCommunicator);
        _nodeEvaluator = new TreeNodeEvaluator(totCommunicator);
        _conclusionGenerator = new TreeConclusionGenerator(totCommunicator);
        _treeOperations = new TreeOperations();
    }

    public ReasoningType ReasoningType => ReasoningType.TreeOfThoughts;

    public ReasoningTree? CurrentTree { get; private set; }

    /// <summary>
    /// Performs Tree of Thoughts reasoning to explore multiple solution paths and find optimal approaches.
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
    /// Tree of Thoughts reasoning explores multiple solution paths simultaneously:
    /// </para>
    /// <list type="number">
    /// <item><description>Generates initial thoughts and hypotheses</description></item>
    /// <item><description>Explores multiple branches of reasoning</description></item>
    /// <item><description>Evaluates and scores different approaches</description></item>
    /// <item><description>Prunes less promising paths</description></item>
    /// <item><description>Synthesizes insights from the best paths</description></item>
    /// </list>
    /// <para>
    /// This approach is particularly effective for complex problems with multiple valid
    /// solution approaches, allowing the agent to explore and evaluate alternatives
    /// before committing to a specific strategy.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    public async Task<ReasoningResult> ReasonAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation($"Starting Tree of Thoughts reasoning for goal: {goal}");

        try
        {
            // Initialize reasoning tree
            CurrentTree = new ReasoningTree
            {
                Goal = goal,
                CreatedUtc = DateTimeOffset.UtcNow,
                MaxDepth = _config.MaxTreeDepth,
                MaxNodes = _config.MaxTreeNodes,
                ExplorationStrategy = _config.TreeExplorationStrategy
            };

            _statusManager.EmitStatus("reasoning", "Initializing tree exploration", "Setting up branching reasoning structure", "Preparing to explore solution space");

            // Create root node
            var rootThought = await _thoughtGenerator.GenerateRootThoughtAsync(goal, context, tools, cancellationToken);
            var rootNode = _treeOperations.CreateRoot(CurrentTree, rootThought, ThoughtType.Hypothesis);

            // Explore the tree
            var explorationResult = await ExploreAsync(_config.TreeExplorationStrategy, cancellationToken);
            
            if (!explorationResult.Success)
            {
                return new ReasoningResult
                {
                    Success = false,
                    Error = explorationResult.Error,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Tree = CurrentTree
                };
            }

            // Find the best path and conclusion
            var bestPath = explorationResult.BestPath;
            var bestPathScore = explorationResult.BestPathScore;
            var conclusion = await _conclusionGenerator.GenerateConclusionFromPathAsync(bestPath, goal, context, tools, CurrentTree, cancellationToken);

            _treeOperations.Complete(CurrentTree, bestPath);
            
            stopwatch.Stop();

            _logger.LogInformation($"Tree of Thoughts reasoning completed in {stopwatch.ElapsedMilliseconds}ms. Nodes explored: {explorationResult.NodesExplored}");

            // Record reasoning metrics (use goal as a surrogate id for tests)
            _metricsCollector.RecordReasoningExecutionTime(goal, ReasoningType.TreeOfThoughts, stopwatch.ElapsedMilliseconds);
            _metricsCollector.RecordReasoningConfidence(goal, ReasoningType.TreeOfThoughts, bestPathScore);

            return new ReasoningResult
            {
                Success = true,
                Conclusion = conclusion,
                Confidence = bestPathScore,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Tree = CurrentTree,
                Metadata = new Dictionary<string, object>
                {
                    ["nodes_explored"] = explorationResult.NodesExplored,
                    ["max_depth_reached"] = explorationResult.MaxDepthReached,
                    ["best_path_score"] = bestPathScore,
                    ["reasoning_type"] = "TreeOfThoughts"
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError($"Tree of Thoughts reasoning failed: {ex.Message}");

            return new ReasoningResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Tree = CurrentTree
            };
        }
    }

    public ThoughtNode CreateRoot(string thought, ThoughtType thoughtType = ThoughtType.Hypothesis)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        return _treeOperations.CreateRoot(CurrentTree, thought, thoughtType);
    }

    public ThoughtNode AddChild(string parentId, string thought, ThoughtType thoughtType = ThoughtType.Hypothesis)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        return _treeOperations.AddChild(CurrentTree, parentId, thought, thoughtType);
    }

    public void EvaluateNode(string nodeId, double score)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        _treeOperations.EvaluateNode(CurrentTree, nodeId, score);
    }

    public void PruneNode(string nodeId)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        _treeOperations.PruneNode(CurrentTree, nodeId);
    }

    public async Task<ExplorationResult> ExploreAsync(ExplorationStrategy strategy, CancellationToken cancellationToken = default)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        var explorationStrategy = TreeExplorationStrategyFactory.GetStrategy(strategy);
        return await explorationStrategy.ExploreAsync(
            CurrentTree, 
            _config, 
            _thoughtGenerator, 
            _nodeEvaluator, 
            _treeOperations, 
            _statusManager, 
            cancellationToken);
    }
}
