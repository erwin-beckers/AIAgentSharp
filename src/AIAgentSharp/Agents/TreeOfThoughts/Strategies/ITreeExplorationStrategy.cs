using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents.TreeOfThoughts.Strategies;

/// <summary>
/// Interface for tree exploration strategies in Tree of Thoughts reasoning.
/// </summary>
internal interface ITreeExplorationStrategy
{
    /// <summary>
    /// Explores the tree using the specific strategy.
    /// </summary>
    /// <param name="tree">The reasoning tree to explore.</param>
    /// <param name="config">The agent configuration.</param>
    /// <param name="thoughtGenerator">The thought generator for creating child thoughts.</param>
    /// <param name="nodeEvaluator">The node evaluator for scoring thoughts.</param>
    /// <param name="treeOperations">The tree operations for manipulating the tree.</param>
    /// <param name="statusManager">The status manager for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exploration result.</returns>
    Task<ExplorationResult> ExploreAsync(
        ReasoningTree tree,
        AgentConfiguration config,
        TreeThoughtGenerator thoughtGenerator,
        TreeNodeEvaluator nodeEvaluator,
        TreeOperations treeOperations,
        IStatusManager statusManager,
        CancellationToken cancellationToken);
}
