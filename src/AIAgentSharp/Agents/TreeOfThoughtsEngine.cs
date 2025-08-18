using System.Diagnostics;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Agents;

/// <summary>
/// Implements Tree of Thoughts (ToT) reasoning for branching exploration of multiple solution paths.
/// </summary>
public sealed class TreeOfThoughtsEngine : ITreeOfThoughtsEngine
{
    private readonly ILlmClient _llm;
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
        IMetricsCollector metricsCollector)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
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
            var rootThought = await GenerateRootThoughtAsync(goal, context, tools, cancellationToken);
            var rootNode = CreateRoot(rootThought, ThoughtType.Hypothesis);

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
            var conclusion = await GenerateConclusionFromPathAsync(bestPath, goal, context, tools, cancellationToken);

            CurrentTree.Complete(bestPath);
            
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

        return CurrentTree.CreateRoot(thought, thoughtType);
    }

    public ThoughtNode AddChild(string parentId, string thought, ThoughtType thoughtType = ThoughtType.Hypothesis)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        return CurrentTree.AddChild(parentId, thought, thoughtType);
    }

    public void EvaluateNode(string nodeId, double score)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        CurrentTree.EvaluateNode(nodeId, score);
    }

    public void PruneNode(string nodeId)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        CurrentTree.PruneNode(nodeId);
    }

    public async Task<ExplorationResult> ExploreAsync(ExplorationStrategy strategy, CancellationToken cancellationToken = default)
    {
        if (CurrentTree == null)
        {
            throw new InvalidOperationException("No active reasoning tree. Call ReasonAsync first.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            switch (strategy)
            {
                case ExplorationStrategy.BestFirst:
                    return await ExploreBestFirstAsync(cancellationToken);
                case ExplorationStrategy.BreadthFirst:
                    return await ExploreBreadthFirstAsync(cancellationToken);
                case ExplorationStrategy.DepthFirst:
                    return await ExploreDepthFirstAsync(cancellationToken);
                case ExplorationStrategy.BeamSearch:
                    return await ExploreBeamSearchAsync(cancellationToken);
                case ExplorationStrategy.MonteCarlo:
                    return await ExploreMonteCarloAsync(cancellationToken);
                default:
                    throw new ArgumentException($"Unsupported exploration strategy: {strategy}");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ExplorationResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    private async Task<ExplorationResult> ExploreBestFirstAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var nodesExplored = 0;
        var maxDepthReached = 0;
        var bestPath = new List<string>();
        var bestScore = 0.0;

        // Priority queue for best-first exploration
        var queue = new PriorityQueue<string, double>(Comparer<double>.Create((a, b) => b.CompareTo(a))); // Higher scores first
        queue.Enqueue(CurrentTree!.RootId!, 0.5); // Start with root

        while (queue.Count > 0 && nodesExplored < _config.MaxTreeNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeId = queue.Dequeue();
            var node = CurrentTree!.Nodes[nodeId];

            if (node.State == ThoughtNodeState.Pruned)
                continue;

            nodesExplored++;
            maxDepthReached = Math.Max(maxDepthReached, node.Depth);

            _statusManager.EmitStatus("reasoning", "Exploring thoughts", $"Evaluating node at depth {node.Depth}", $"Nodes explored: {nodesExplored}");

            // Evaluate the node first
            var score = await EvaluateThoughtNodeAsync(node, cancellationToken);
            EvaluateNode(nodeId, score);

            // Update best path if this is a leaf node with better score
            if (node.IsLeaf && score > bestScore)
            {
                bestScore = score;
                bestPath = CurrentTree.GetPathToNode(nodeId);
            }

            // Generate children after evaluation
            if (node.Depth < _config.MaxTreeDepth && !CurrentTree.IsAtCapacity)
            {
                var children = await GenerateChildThoughtsAsync(node, cancellationToken);
                foreach (var child in children)
                {
                    var childNode = AddChild(nodeId, child.Thought, child.ThoughtType);
                    queue.Enqueue(childNode.NodeId, child.EstimatedScore);
                }
            }

            // Children already generated above
        }

        stopwatch.Stop();

        return new ExplorationResult
        {
            Success = true,
            BestPath = bestPath,
            BestPathScore = bestScore,
            NodesExplored = nodesExplored,
            MaxDepthReached = maxDepthReached,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<ExplorationResult> ExploreBreadthFirstAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var nodesExplored = 0;
        var maxDepthReached = 0;
        var bestPath = new List<string>();
        var bestScore = 0.0;

        var queue = new Queue<string>();
        queue.Enqueue(CurrentTree!.RootId!);

        while (queue.Count > 0 && nodesExplored < _config.MaxTreeNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeId = queue.Dequeue();
            var node = CurrentTree!.Nodes[nodeId];

            if (node.State == ThoughtNodeState.Pruned)
                continue;

            nodesExplored++;
            maxDepthReached = Math.Max(maxDepthReached, node.Depth);

            _statusManager.EmitStatus("reasoning", "Exploring thoughts", $"Evaluating node at depth {node.Depth}", $"Nodes explored: {nodesExplored}");

            // Evaluate the node
            var score = await EvaluateThoughtNodeAsync(node, cancellationToken);
            EvaluateNode(nodeId, score);

            // Update best path if this is a leaf node with better score
            if (node.IsLeaf && score > bestScore)
            {
                bestScore = score;
                bestPath = CurrentTree.GetPathToNode(nodeId);
            }

            // Generate children if not at max depth
            if (node.Depth < _config.MaxTreeDepth && !CurrentTree.IsAtCapacity)
            {
                var children = await GenerateChildThoughtsAsync(node, cancellationToken);
                foreach (var child in children)
                {
                    var childNode = AddChild(nodeId, child.Thought, child.ThoughtType);
                    queue.Enqueue(childNode.NodeId);
                }
            }
        }

        stopwatch.Stop();

        return new ExplorationResult
        {
            Success = true,
            BestPath = bestPath,
            BestPathScore = bestScore,
            NodesExplored = nodesExplored,
            MaxDepthReached = maxDepthReached,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<ExplorationResult> ExploreDepthFirstAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var nodesExplored = 0;
        var maxDepthReached = 0;
        var bestPath = new List<string>();
        var bestScore = 0.0;

        var stack = new Stack<string>();
        stack.Push(CurrentTree!.RootId!);

        while (stack.Count > 0 && nodesExplored < _config.MaxTreeNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeId = stack.Pop();
            var node = CurrentTree!.Nodes[nodeId];

            if (node.State == ThoughtNodeState.Pruned)
                continue;

            nodesExplored++;
            maxDepthReached = Math.Max(maxDepthReached, node.Depth);

            _statusManager.EmitStatus("reasoning", "Exploring thoughts", $"Evaluating node at depth {node.Depth}", $"Nodes explored: {nodesExplored}");

            // Evaluate the node
            var score = await EvaluateThoughtNodeAsync(node, cancellationToken);
            EvaluateNode(nodeId, score);

            // Update best path if this is a leaf node with better score
            if (node.IsLeaf && score > bestScore)
            {
                bestScore = score;
                bestPath = CurrentTree.GetPathToNode(nodeId);
            }

            // Generate children if not at max depth
            if (node.Depth < _config.MaxTreeDepth && !CurrentTree.IsAtCapacity)
            {
                var children = await GenerateChildThoughtsAsync(node, cancellationToken);
                // Push children in reverse order to maintain exploration order
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    var child = children[i];
                    var childNode = AddChild(nodeId, child.Thought, child.ThoughtType);
                    stack.Push(childNode.NodeId);
                }
            }
        }

        stopwatch.Stop();

        return new ExplorationResult
        {
            Success = true,
            BestPath = bestPath,
            BestPathScore = bestScore,
            NodesExplored = nodesExplored,
            MaxDepthReached = maxDepthReached,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<ExplorationResult> ExploreBeamSearchAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var nodesExplored = 0;
        var maxDepthReached = 0;
        var bestPath = new List<string>();
        var bestScore = 0.0;
        const int beamWidth = 3; // Configurable beam width

        var currentLevel = new List<string> { CurrentTree!.RootId! };
        var nextLevel = new List<string>();

        while (currentLevel.Count > 0 && nodesExplored < _config.MaxTreeNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Evaluate all nodes at current level
            foreach (var nodeId in currentLevel)
            {
                if (nodesExplored >= _config.MaxTreeNodes) break;

                var node = CurrentTree!.Nodes[nodeId];
                if (node.State == ThoughtNodeState.Pruned)
                    continue;

                nodesExplored++;
                maxDepthReached = Math.Max(maxDepthReached, node.Depth);

                _statusManager.EmitStatus("reasoning", "Exploring thoughts", $"Evaluating node at depth {node.Depth}", $"Nodes explored: {nodesExplored}");

                // Evaluate the node
                var score = await EvaluateThoughtNodeAsync(node, cancellationToken);
                EvaluateNode(nodeId, score);

                // Update best path if this is a leaf node with better score
                if (node.IsLeaf && score > bestScore)
                {
                    bestScore = score;
                    bestPath = CurrentTree.GetPathToNode(nodeId);
                }

                // Generate children if not at max depth
                if (node.Depth < _config.MaxTreeDepth && !CurrentTree.IsAtCapacity)
                {
                    var children = await GenerateChildThoughtsAsync(node, cancellationToken);
                    foreach (var child in children)
                    {
                        var childNode = AddChild(nodeId, child.Thought, child.ThoughtType);
                        nextLevel.Add(childNode.NodeId);
                    }
                }
            }

            // Select top beamWidth nodes for next level
            if (nextLevel.Count > 0)
            {
                var scoredNodes = nextLevel.Select(id => new { Id = id, Score = CurrentTree!.Nodes[id].Score }).ToList();
                currentLevel = scoredNodes.OrderByDescending(n => n.Score).Take(beamWidth).Select(n => n.Id).ToList();
                nextLevel.Clear();
            }
            else
            {
                break;
            }
        }

        stopwatch.Stop();

        return new ExplorationResult
        {
            Success = true,
            BestPath = bestPath,
            BestPathScore = bestScore,
            NodesExplored = nodesExplored,
            MaxDepthReached = maxDepthReached,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<ExplorationResult> ExploreMonteCarloAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var nodesExplored = 0;
        var maxDepthReached = 0;
        var bestPath = new List<string>();
        var bestScore = 0.0;
        var random = new Random();

        // Perform multiple random walks
        const int numWalks = 10;
        for (int walk = 0; walk < numWalks && nodesExplored < _config.MaxTreeNodes; walk++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentPath = new List<string>();
            var currentNodeId = CurrentTree!.RootId!;

            while (currentNodeId != null && nodesExplored < _config.MaxTreeNodes)
            {
                var node = CurrentTree!.Nodes[currentNodeId];
                if (node.State == ThoughtNodeState.Pruned)
                    break;

                nodesExplored++;
                maxDepthReached = Math.Max(maxDepthReached, node.Depth);
                currentPath.Add(currentNodeId);

                _statusManager.EmitStatus("reasoning", "Exploring thoughts", $"Random walk {walk + 1}, depth {node.Depth}", $"Nodes explored: {nodesExplored}");

                // Evaluate the node
                var score = await EvaluateThoughtNodeAsync(node, cancellationToken);
                EvaluateNode(currentNodeId, score);

                // Update best path if this is a leaf node with better score
                if (node.IsLeaf && score > bestScore)
                {
                    bestScore = score;
                    bestPath = new List<string>(currentPath);
                }

                // Randomly select next node or stop
                if (node.Depth < _config.MaxTreeDepth && !CurrentTree.IsAtCapacity && random.NextDouble() > 0.3)
                {
                    var children = await GenerateChildThoughtsAsync(node, cancellationToken);
                    if (children.Count > 0)
                    {
                        var randomChild = children[random.Next(children.Count)];
                        var childNode = AddChild(currentNodeId, randomChild.Thought, randomChild.ThoughtType);
                        currentNodeId = childNode.NodeId;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        stopwatch.Stop();

        return new ExplorationResult
        {
            Success = true,
            BestPath = bestPath,
            BestPathScore = bestScore,
            NodesExplored = nodesExplored,
            MaxDepthReached = maxDepthReached,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<string> GenerateRootThoughtAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));
        
        var prompt = $@"You are starting a Tree of Thoughts exploration for a complex problem.

GOAL: {goal}
CONTEXT: {context}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Generate an initial thought or hypothesis about how to approach this goal. This should be a high-level starting point that can branch into multiple directions.

Provide your initial thought in the following JSON format:
{{
  ""thought"": ""Your initial thought or hypothesis here..."",
  ""thought_type"": ""Hypothesis""
}}

Focus on creating a thought that opens up multiple exploration paths.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);
        var content = response.Content;
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(content);
            if (!json.RootElement.TryGetProperty("thought", out var thoughtProp))
            {
                throw new FormatException("Invalid LLM JSON: missing 'thought' property");
            }
            var thought = thoughtProp.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(thought))
            {
                throw new FormatException("Invalid LLM JSON: empty 'thought' value");
            }
            return thought;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new FormatException($"Invalid LLM JSON: {ex.Message}");
        }
    }

    private async Task<List<ChildThought>> GenerateChildThoughtsAsync(ThoughtNode parentNode, CancellationToken cancellationToken)
    {
        var prompt = $@"You are generating child thoughts in a Tree of Thoughts exploration.

PARENT THOUGHT: {parentNode.Thought}
PARENT DEPTH: {parentNode.Depth}
PARENT SCORE: {parentNode.Score:F2}

TASK: Generate 2-3 child thoughts that explore different aspects or directions from the parent thought. Each child should represent a different approach, alternative, or next step.

Provide your child thoughts in the following JSON format:
{{
  ""children"": [
    {{
      ""thought"": ""First child thought..."",
      ""thought_type"": ""Analysis"",
      ""estimated_score"": 0.75
    }},
    {{
      ""thought"": ""Second child thought..."",
      ""thought_type"": ""Alternative"",
      ""estimated_score"": 0.65
    }}
  ]
}}

Focus on generating diverse, meaningful thoughts that advance the reasoning.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);
        var content = response.Content;
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(content);
            if (!json.RootElement.TryGetProperty("children", out var childrenArray) || childrenArray.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                throw new FormatException("Invalid LLM JSON: missing 'children' array");
            }
            return ExtractChildThoughtsFromResponse(content);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new FormatException($"Invalid LLM JSON: {ex.Message}");
        }
    }

    private async Task<double> EvaluateThoughtNodeAsync(ThoughtNode node, CancellationToken cancellationToken)
    {
        var prompt = $@"You are evaluating a thought in a Tree of Thoughts exploration.

THOUGHT: {node.Thought}
THOUGHT TYPE: {node.ThoughtType}
DEPTH: {node.Depth}

TASK: Evaluate the quality and potential of this thought on a scale from 0.0 to 1.0. Consider:
1. Relevance to the goal
2. Logical soundness
3. Potential for leading to a solution
4. Novelty and creativity
5. Feasibility

Provide your evaluation in the following JSON format:
{{
  ""score"": 0.85,
  ""reasoning"": ""Brief explanation of the score""
}}

Be objective and thorough in your evaluation.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);
        var content = response.Content;
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(content);
            if (!json.RootElement.TryGetProperty("score", out _))
            {
                throw new FormatException("Invalid LLM JSON: missing 'score' property");
            }
            return ExtractScoreFromResponse(content);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new FormatException($"Invalid LLM JSON: {ex.Message}");
        }
    }

    private async Task<string> GenerateConclusionFromPathAsync(List<string> bestPath, string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        if (bestPath.Count == 0)
            return "No viable solution path found.";

        var pathThoughts = bestPath.Select(id => CurrentTree!.Nodes[id].Thought).ToList();
        var pathText = string.Join("\n", pathThoughts.Select((thought, i) => $"Step {i + 1}: {thought}"));

        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));
        
        var prompt = $@"You are synthesizing a conclusion from the best path found in a Tree of Thoughts exploration.

GOAL: {goal}
CONTEXT: {context}

BEST PATH THOUGHTS:
{pathText}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Based on the best path of thoughts, provide a clear, actionable conclusion that summarizes the approach and next steps.

Provide your conclusion in the following JSON format:
{{
  ""conclusion"": ""Your comprehensive conclusion and recommendation""
}}

Focus on creating a practical, actionable conclusion.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);
        var content = response.Content;
        return ExtractConclusionFromResponse(content);
    }

    private string ExtractThoughtFromResponse(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            return json.RootElement.GetProperty("thought").GetString() ?? "";
        }
        catch
        {
            return response;
        }
    }

    private List<ChildThought> ExtractChildThoughtsFromResponse(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            var childrenArray = json.RootElement.GetProperty("children");
            var children = new List<ChildThought>();

            foreach (var child in childrenArray.EnumerateArray())
            {
                children.Add(new ChildThought
                {
                    Thought = child.GetProperty("thought").GetString() ?? "",
                    ThoughtType = Enum.Parse<ThoughtType>(child.GetProperty("thought_type").GetString() ?? "Hypothesis"),
                    EstimatedScore = child.GetProperty("estimated_score").GetDouble()
                });
            }

            return children;
        }
        catch
        {
            return new List<ChildThought>();
        }
    }

    private double ExtractScoreFromResponse(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            return json.RootElement.GetProperty("score").GetDouble();
        }
        catch
        {
            return 0.5;
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
            return response;
        }
    }

    private class ChildThought
    {
        public string Thought { get; set; } = "";
        public ThoughtType ThoughtType { get; set; } = ThoughtType.Hypothesis;
        public double EstimatedScore { get; set; } = 0.5;
    }
}
