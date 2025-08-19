using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents.TreeOfThoughts.Strategies;

/// <summary>
/// Implements breadth-first exploration strategy for Tree of Thoughts.
/// </summary>
internal sealed class BreadthFirstExplorationStrategy : ITreeExplorationStrategy
{
    [ExcludeFromCodeCoverage]
    public async Task<ExplorationResult> ExploreAsync(
        ReasoningTree tree,
        AgentConfiguration config,
        TreeThoughtGenerator thoughtGenerator,
        TreeNodeEvaluator nodeEvaluator,
        TreeOperations treeOperations,
        IStatusManager statusManager,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var nodesExplored = 0;
        var maxDepthReached = 0;
        var bestPath = new List<string>();
        var bestScore = 0.0;

        var queue = new Queue<string>();
        queue.Enqueue(tree.RootId!);

        while (queue.Count > 0 && nodesExplored < config.MaxTreeNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeId = queue.Dequeue();
            var node = tree.Nodes[nodeId];

            if (node.State == ThoughtNodeState.Pruned)
                continue;

            nodesExplored++;
            maxDepthReached = Math.Max(maxDepthReached, node.Depth);

            statusManager.EmitStatus("reasoning", "Exploring thoughts", $"Evaluating node at depth {node.Depth}", $"Nodes explored: {nodesExplored}");

            // Evaluate the node
            var score = await nodeEvaluator.EvaluateThoughtNodeAsync(node, cancellationToken);
            treeOperations.EvaluateNode(tree, nodeId, score);

            // Update best path if this is a leaf node with better score
            if (node.IsLeaf && score > bestScore)
            {
                bestScore = score;
                bestPath = treeOperations.GetPathToNode(tree, nodeId);
            }

            // Generate children if not at max depth
            if (node.Depth < config.MaxTreeDepth && !tree.IsAtCapacity)
            {
                var children = await thoughtGenerator.GenerateChildThoughtsAsync(node, cancellationToken);
                foreach (var child in children)
                {
                    var childNode = treeOperations.AddChild(tree, nodeId, child.Thought, child.ThoughtType);
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
}
