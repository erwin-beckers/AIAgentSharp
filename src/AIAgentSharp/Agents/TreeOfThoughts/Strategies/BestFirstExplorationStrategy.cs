using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents.TreeOfThoughts.Strategies;

/// <summary>
/// Implements best-first exploration strategy for Tree of Thoughts.
/// </summary>
internal sealed class BestFirstExplorationStrategy : ITreeExplorationStrategy
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

        // Priority queue for best-first exploration
        var queue = new PriorityQueue<string, double>(Comparer<double>.Create((a, b) => b.CompareTo(a))); // Higher scores first
        queue.Enqueue(tree.RootId!, 0.5); // Start with root

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

            // Evaluate the node first
            var score = await nodeEvaluator.EvaluateThoughtNodeAsync(node, cancellationToken);
            treeOperations.EvaluateNode(tree, nodeId, score);

            // Update best path if this is a leaf node with better score
            if (node.IsLeaf && score > bestScore)
            {
                bestScore = score;
                bestPath = treeOperations.GetPathToNode(tree, nodeId);
                
                // Early termination: if we found a very good solution (score > 0.8), stop exploring
                if (bestScore > 0.8)
                {
                    statusManager.EmitStatus("reasoning", "Found excellent solution", $"Score: {bestScore:F2}", "Terminating exploration early");
                    break;
                }
            }

            // Generate children after evaluation
            if (node.Depth < config.MaxTreeDepth && !tree.IsAtCapacity)
            {
                var children = await thoughtGenerator.GenerateChildThoughtsAsync(node, cancellationToken);
                foreach (var child in children)
                {
                    var childNode = treeOperations.AddChild(tree, nodeId, child.Thought, child.ThoughtType);
                    queue.Enqueue(childNode.NodeId, child.EstimatedScore);
                }
            }
            
            // Early termination: if we've explored enough nodes and have a reasonable solution
            if (nodesExplored >= 15 && bestScore > 0.6)
            {
                statusManager.EmitStatus("reasoning", "Found good solution", $"Score: {bestScore:F2}", "Terminating exploration");
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
}
