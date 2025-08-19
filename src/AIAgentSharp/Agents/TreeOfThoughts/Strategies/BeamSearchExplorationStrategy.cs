using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents.TreeOfThoughts.Strategies;

/// <summary>
/// Implements beam search exploration strategy for Tree of Thoughts.
/// </summary>
internal sealed class BeamSearchExplorationStrategy : ITreeExplorationStrategy
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
        const int beamWidth = 3; // Configurable beam width

        var currentLevel = new List<string> { tree.RootId! };
        var nextLevel = new List<string>();

        while (currentLevel.Count > 0 && nodesExplored < config.MaxTreeNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Evaluate all nodes at current level
            foreach (var nodeId in currentLevel)
            {
                if (nodesExplored >= config.MaxTreeNodes) break;

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
                        nextLevel.Add(childNode.NodeId);
                    }
                }
            }

            // Select top beamWidth nodes for next level
            if (nextLevel.Count > 0)
            {
                var scoredNodes = nextLevel.Select(id => new { Id = id, Score = tree.Nodes[id].Score }).ToList();
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
}
