using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents.TreeOfThoughts.Strategies;

/// <summary>
/// Implements Monte Carlo exploration strategy for Tree of Thoughts.
/// </summary>
internal sealed class MonteCarloExplorationStrategy : ITreeExplorationStrategy
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
        var random = new Random();

        // Perform multiple random walks
        const int numWalks = 10;
        for (int walk = 0; walk < numWalks && nodesExplored < config.MaxTreeNodes; walk++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentPath = new List<string>();
            var currentNodeId = tree.RootId!;

            while (currentNodeId != null && nodesExplored < config.MaxTreeNodes)
            {
                var node = tree.Nodes[currentNodeId];
                if (node.State == ThoughtNodeState.Pruned)
                    break;

                nodesExplored++;
                maxDepthReached = Math.Max(maxDepthReached, node.Depth);
                currentPath.Add(currentNodeId);

                statusManager.EmitStatus("reasoning", "Exploring thoughts", $"Random walk {walk + 1}, depth {node.Depth}", $"Nodes explored: {nodesExplored}");

                // Evaluate the node
                var score = await nodeEvaluator.EvaluateThoughtNodeAsync(node, cancellationToken);
                treeOperations.EvaluateNode(tree, currentNodeId, score);

                // Update best path if this is a leaf node with better score
                if (node.IsLeaf && score > bestScore)
                {
                    bestScore = score;
                    bestPath = new List<string>(currentPath);
                }

                // Randomly select next node or stop
                if (node.Depth < config.MaxTreeDepth && !tree.IsAtCapacity && random.NextDouble() > 0.3)
                {
                    var children = await thoughtGenerator.GenerateChildThoughtsAsync(node, cancellationToken);
                    if (children.Count > 0)
                    {
                        var randomChild = children[random.Next(children.Count)];
                        var childNode = treeOperations.AddChild(tree, currentNodeId, randomChild.Thought, randomChild.ThoughtType);
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
}
