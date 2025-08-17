namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Interface for reasoning engines that can perform structured reasoning.
/// </summary>
public interface IReasoningEngine
{
    /// <summary>
    /// Gets the type of reasoning this engine supports.
    /// </summary>
    ReasoningType ReasoningType { get; }

    /// <summary>
    /// Performs reasoning on a given goal and context.
    /// </summary>
    /// <param name="goal">The goal or problem to reason about.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="tools">Available tools for the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reasoning result.</returns>
    Task<ReasoningResult> ReasonAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Chain of Thought reasoning engines.
/// </summary>
public interface IChainOfThoughtEngine : IReasoningEngine
{
    /// <summary>
    /// Gets the current reasoning chain.
    /// </summary>
    ReasoningChain? CurrentChain { get; }

    /// <summary>
    /// Adds a reasoning step to the current chain.
    /// </summary>
    /// <param name="reasoning">The reasoning content.</param>
    /// <param name="stepType">The type of reasoning step.</param>
    /// <param name="confidence">The confidence level.</param>
    /// <param name="insights">Optional insights.</param>
    /// <returns>The created reasoning step.</returns>
    ReasoningStep AddStep(string reasoning, ReasoningStepType stepType = ReasoningStepType.Analysis, double confidence = 0.5, List<string>? insights = null);

    /// <summary>
    /// Completes the current reasoning chain.
    /// </summary>
    /// <param name="conclusion">The final conclusion.</param>
    /// <param name="confidence">The confidence in the conclusion.</param>
    void CompleteChain(string conclusion, double confidence = 0.5);
}

/// <summary>
/// Interface for Tree of Thoughts reasoning engines.
/// </summary>
public interface ITreeOfThoughtsEngine : IReasoningEngine
{
    /// <summary>
    /// Gets the current reasoning tree.
    /// </summary>
    ReasoningTree? CurrentTree { get; }

    /// <summary>
    /// Creates a root node for the reasoning tree.
    /// </summary>
    /// <param name="thought">The initial thought.</param>
    /// <param name="thoughtType">The type of thought.</param>
    /// <returns>The created root node.</returns>
    ThoughtNode CreateRoot(string thought, ThoughtType thoughtType = ThoughtType.Hypothesis);

    /// <summary>
    /// Adds a child node to an existing parent.
    /// </summary>
    /// <param name="parentId">The parent node ID.</param>
    /// <param name="thought">The thought content.</param>
    /// <param name="thoughtType">The type of thought.</param>
    /// <returns>The created child node.</returns>
    ThoughtNode AddChild(string parentId, string thought, ThoughtType thoughtType = ThoughtType.Hypothesis);

    /// <summary>
    /// Evaluates a thought node.
    /// </summary>
    /// <param name="nodeId">The node ID to evaluate.</param>
    /// <param name="score">The evaluation score.</param>
    void EvaluateNode(string nodeId, double score);

    /// <summary>
    /// Prunes a node and its descendants.
    /// </summary>
    /// <param name="nodeId">The node ID to prune.</param>
    void PruneNode(string nodeId);

    /// <summary>
    /// Explores the tree using the specified strategy.
    /// </summary>
    /// <param name="strategy">The exploration strategy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exploration result.</returns>
    Task<ExplorationResult> ExploreAsync(ExplorationStrategy strategy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the type of reasoning supported by an engine.
/// </summary>
public enum ReasoningType
{
    /// <summary>
    /// No reasoning (disabled).
    /// </summary>
    None,

    /// <summary>
    /// Chain of Thought reasoning.
    /// </summary>
    ChainOfThought,

    /// <summary>
    /// Tree of Thoughts reasoning.
    /// </summary>
    TreeOfThoughts,

    /// <summary>
    /// Hybrid reasoning combining multiple approaches.
    /// </summary>
    Hybrid
}

/// <summary>
/// Represents the result of a reasoning operation.
/// </summary>
public sealed class ReasoningResult
{
    /// <summary>
    /// Gets or sets whether the reasoning was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the final conclusion or decision.
    /// </summary>
    public string Conclusion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence in the conclusion (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the reasoning chain if applicable.
    /// </summary>
    public ReasoningChain? Chain { get; set; }

    /// <summary>
    /// Gets or sets the reasoning tree if applicable.
    /// </summary>
    public ReasoningTree? Tree { get; set; }

    /// <summary>
    /// Gets or sets any error message if reasoning failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets metadata about the reasoning process.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the result of a tree exploration operation.
/// </summary>
public sealed class ExplorationResult
{
    /// <summary>
    /// Gets or sets whether the exploration was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the best path found.
    /// </summary>
    public List<string> BestPath { get; set; } = new();

    /// <summary>
    /// Gets or sets the score of the best path.
    /// </summary>
    public double BestPathScore { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the number of nodes explored.
    /// </summary>
    public int NodesExplored { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth reached.
    /// </summary>
    public int MaxDepthReached { get; set; }

    /// <summary>
    /// Gets or sets any error message if exploration failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets metadata about the exploration process.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
