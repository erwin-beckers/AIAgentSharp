namespace AIAgentSharp.Agents.TreeOfThoughts;

/// <summary>
/// Handles tree manipulation operations for Tree of Thoughts reasoning.
/// </summary>
internal sealed class TreeOperations
{
    /// <summary>
    /// Creates a root node for the reasoning tree.
    /// </summary>
    public ThoughtNode CreateRoot(ReasoningTree tree, string thought, ThoughtType thoughtType = ThoughtType.Hypothesis)
    {
        if (tree.RootId != null)
        {
            throw new InvalidOperationException("Tree already has a root node.");
        }

        var rootNode = new ThoughtNode
        {
            NodeId = Guid.NewGuid().ToString(),
            ParentId = null,
            Depth = 0,
            Thought = thought,
            ThoughtType = thoughtType,
            State = ThoughtNodeState.Active
        };

        tree.Nodes[rootNode.NodeId] = rootNode;
        tree.RootId = rootNode.NodeId;
        return rootNode;
    }

    /// <summary>
    /// Adds a child node to an existing parent node.
    /// </summary>
    public ThoughtNode AddChild(ReasoningTree tree, string parentId, string thought, ThoughtType thoughtType = ThoughtType.Hypothesis)
    {
        if (!tree.Nodes.TryGetValue(parentId, out var parentNode))
        {
            throw new ArgumentException($"Parent node with ID {parentId} not found.");
        }

        if (parentNode.Depth >= tree.MaxDepth)
        {
            throw new InvalidOperationException($"Cannot add child to node at maximum depth {tree.MaxDepth}.");
        }

        if (tree.IsAtCapacity)
        {
            throw new InvalidOperationException($"Tree has reached maximum capacity of {tree.MaxNodes} nodes.");
        }

        var childNode = new ThoughtNode
        {
            NodeId = Guid.NewGuid().ToString(),
            ParentId = parentId,
            Depth = parentNode.Depth + 1,
            Thought = thought,
            ThoughtType = thoughtType,
            State = ThoughtNodeState.Active
        };

        tree.Nodes[childNode.NodeId] = childNode;
        parentNode.ChildIds.Add(childNode.NodeId);
        return childNode;
    }

    /// <summary>
    /// Evaluates a thought node and updates its score.
    /// </summary>
    public void EvaluateNode(ReasoningTree tree, string nodeId, double score)
    {
        if (!tree.Nodes.TryGetValue(nodeId, out var node))
        {
            throw new ArgumentException($"Node with ID {nodeId} not found.");
        }

        node.Score = Math.Max(0.0, Math.Min(1.0, score));
        node.State = ThoughtNodeState.Evaluated;
        node.EvaluatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Prunes a node and its descendants from the tree.
    /// </summary>
    public void PruneNode(ReasoningTree tree, string nodeId)
    {
        if (!tree.Nodes.TryGetValue(nodeId, out var node))
        {
            throw new ArgumentException($"Node with ID {nodeId} not found.");
        }

        // Mark this node and all descendants as pruned
        var nodesToPrune = GetDescendants(tree, nodeId);
        nodesToPrune.Add(nodeId);

        foreach (var id in nodesToPrune)
        {
            if (tree.Nodes.TryGetValue(id, out var n))
            {
                n.State = ThoughtNodeState.Pruned;
            }
        }
    }

    /// <summary>
    /// Gets all descendant node IDs of a given node.
    /// </summary>
    public List<string> GetDescendants(ReasoningTree tree, string nodeId)
    {
        var descendants = new List<string>();
        if (!tree.Nodes.TryGetValue(nodeId, out var node))
        {
            return descendants;
        }

        foreach (var childId in node.ChildIds)
        {
            descendants.Add(childId);
            descendants.AddRange(GetDescendants(tree, childId));
        }

        return descendants;
    }

    /// <summary>
    /// Gets the path from root to a specific node.
    /// </summary>
    public List<string> GetPathToNode(ReasoningTree tree, string nodeId)
    {
        var path = new List<string>();
        var currentId = nodeId;

        while (currentId != null && tree.Nodes.TryGetValue(currentId, out var node))
        {
            path.Insert(0, currentId);
            currentId = node.ParentId;
        }

        return path;
    }

    /// <summary>
    /// Completes the reasoning tree with a best path.
    /// </summary>
    public void Complete(ReasoningTree tree, List<string> bestPath)
    {
        tree.BestPath = bestPath;
        tree.CompletedUtc = DateTimeOffset.UtcNow;

        // Mark nodes in the best path
        foreach (var nodeId in bestPath)
        {
            if (tree.Nodes.TryGetValue(nodeId, out var node))
            {
                node.State = ThoughtNodeState.BestPath;
            }
        }
    }
}
