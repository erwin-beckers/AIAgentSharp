using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
/// Represents a node in a Tree of Thoughts reasoning structure.
/// </summary>
public sealed class ThoughtNode
{
    /// <summary>
    /// Gets or sets the unique identifier for this thought node.
    /// </summary>
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the parent node ID, null for root nodes.
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the depth of this node in the tree (0 for root).
    /// </summary>
    [JsonPropertyName("depth")]
    public int Depth { get; set; }

    /// <summary>
    /// Gets or sets the thought content for this node.
    /// </summary>
    [JsonPropertyName("thought")]
    public string Thought { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the evaluation score for this thought (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the type of thought (hypothesis, observation, decision, etc.).
    /// </summary>
    [JsonPropertyName("thought_type")]
    public ThoughtType ThoughtType { get; set; } = ThoughtType.Hypothesis;

    /// <summary>
    /// Gets or sets the child node IDs.
    /// </summary>
    [JsonPropertyName("child_ids")]
    public List<string> ChildIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the state of this thought node.
    /// </summary>
    [JsonPropertyName("state")]
    public ThoughtNodeState State { get; set; } = ThoughtNodeState.Active;

    /// <summary>
    /// Gets or sets the timestamp when this node was created.
    /// </summary>
    [JsonPropertyName("created_utc")]
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this node was evaluated.
    /// </summary>
    [JsonPropertyName("evaluated_utc")]
    public DateTimeOffset? EvaluatedUtc { get; set; }

    /// <summary>
    /// Gets or sets metadata about this thought node.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether this node is a leaf node (no children).
    /// </summary>
    [JsonIgnore]
    public bool IsLeaf => ChildIds.Count == 0;

    /// <summary>
    /// Gets a value indicating whether this node is the root node.
    /// </summary>
    [JsonIgnore]
    public bool IsRoot => ParentId == null;
}

/// <summary>
/// Represents the type of thought in a Tree of Thoughts.
/// </summary>
public enum ThoughtType
{
    /// <summary>
    /// Initial hypothesis or assumption.
    /// </summary>
    Hypothesis,

    /// <summary>
    /// Observation or fact.
    /// </summary>
    Observation,

    /// <summary>
    /// Decision or choice point.
    /// </summary>
    Decision,

    /// <summary>
    /// Analysis or reasoning.
    /// </summary>
    Analysis,

    /// <summary>
    /// Conclusion or result.
    /// </summary>
    Conclusion,

    /// <summary>
    /// Question or uncertainty.
    /// </summary>
    Question,

    /// <summary>
    /// Alternative or option.
    /// </summary>
    Alternative
}

/// <summary>
/// Represents the state of a thought node.
/// </summary>
public enum ThoughtNodeState
{
    /// <summary>
    /// Node is active and being explored.
    /// </summary>
    Active,

    /// <summary>
    /// Node has been evaluated.
    /// </summary>
    Evaluated,

    /// <summary>
    /// Node has been pruned (removed from consideration).
    /// </summary>
    Pruned,

    /// <summary>
    /// Node represents the best path found.
    /// </summary>
    BestPath,

    /// <summary>
    /// Node has been completed.
    /// </summary>
    Completed
}

/// <summary>
/// Represents a complete Tree of Thoughts reasoning structure.
/// </summary>
public sealed class ReasoningTree
{
    /// <summary>
    /// Gets or sets the unique identifier for this reasoning tree.
    /// </summary>
    [JsonPropertyName("tree_id")]
    public string TreeId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the goal or problem being addressed by this reasoning tree.
    /// </summary>
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets all nodes in the tree, indexed by node ID.
    /// </summary>
    [JsonPropertyName("nodes")]
    public Dictionary<string, ThoughtNode> Nodes { get; set; } = new();

    /// <summary>
    /// Gets or sets the root node ID.
    /// </summary>
    [JsonPropertyName("root_id")]
    public string? RootId { get; set; }

    /// <summary>
    /// Gets or sets the best path found through the tree.
    /// </summary>
    [JsonPropertyName("best_path")]
    public List<string> BestPath { get; set; } = new();

    /// <summary>
    /// Gets or sets the exploration strategy used.
    /// </summary>
    [JsonPropertyName("exploration_strategy")]
    public ExplorationStrategy ExplorationStrategy { get; set; } = ExplorationStrategy.BestFirst;

    /// <summary>
    /// Gets or sets the maximum depth allowed for the tree.
    /// </summary>
    [JsonPropertyName("max_depth")]
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of nodes allowed in the tree.
    /// </summary>
    [JsonPropertyName("max_nodes")]
    public int MaxNodes { get; set; } = 100;

    /// <summary>
    /// Gets or sets the timestamp when this reasoning tree was created.
    /// </summary>
    [JsonPropertyName("created_utc")]
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this reasoning tree was completed.
    /// </summary>
    [JsonPropertyName("completed_utc")]
    public DateTimeOffset? CompletedUtc { get; set; }

    /// <summary>
    /// Gets or sets the total execution time for the entire reasoning tree.
    /// </summary>
    [JsonPropertyName("total_execution_time_ms")]
    public long TotalExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets metadata about the reasoning tree.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the current number of nodes in the tree.
    /// </summary>
    [JsonIgnore]
    public int NodeCount => Nodes.Count;

    /// <summary>
    /// Gets the current maximum depth reached in the tree.
    /// </summary>
    [JsonIgnore]
    public int CurrentMaxDepth => Nodes.Values.Any() ? Nodes.Values.Max(n => n.Depth) : 0;

    /// <summary>
    /// Gets a value indicating whether this reasoning tree is complete.
    /// </summary>
    [JsonIgnore]
    public bool IsComplete => CompletedUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the tree has reached its maximum capacity.
    /// </summary>
    [JsonIgnore]
    public bool IsAtCapacity => NodeCount >= MaxNodes;

    /// <summary>
    /// Creates a root node for the tree.
    /// </summary>
    /// <param name="thought">The initial thought content.</param>
    /// <param name="thoughtType">The type of thought.</param>
    /// <returns>The created root node.</returns>
    public ThoughtNode CreateRoot(string thought, ThoughtType thoughtType = ThoughtType.Hypothesis)
    {
        if (RootId != null)
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

        Nodes[rootNode.NodeId] = rootNode;
        RootId = rootNode.NodeId;
        return rootNode;
    }

    /// <summary>
    /// Adds a child node to an existing parent node.
    /// </summary>
    /// <param name="parentId">The parent node ID.</param>
    /// <param name="thought">The thought content.</param>
    /// <param name="thoughtType">The type of thought.</param>
    /// <returns>The created child node.</returns>
    public ThoughtNode AddChild(string parentId, string thought, ThoughtType thoughtType = ThoughtType.Hypothesis)
    {
        if (!Nodes.TryGetValue(parentId, out var parentNode))
        {
            throw new ArgumentException($"Parent node with ID {parentId} not found.");
        }

        if (parentNode.Depth >= MaxDepth)
        {
            throw new InvalidOperationException($"Cannot add child to node at maximum depth {MaxDepth}.");
        }

        if (IsAtCapacity)
        {
            throw new InvalidOperationException($"Tree has reached maximum capacity of {MaxNodes} nodes.");
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

        Nodes[childNode.NodeId] = childNode;
        parentNode.ChildIds.Add(childNode.NodeId);
        return childNode;
    }

    /// <summary>
    /// Evaluates a thought node and updates its score.
    /// </summary>
    /// <param name="nodeId">The node ID to evaluate.</param>
    /// <param name="score">The evaluation score (0.0 to 1.0).</param>
    public void EvaluateNode(string nodeId, double score)
    {
        if (!Nodes.TryGetValue(nodeId, out var node))
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
    /// <param name="nodeId">The node ID to prune.</param>
    public void PruneNode(string nodeId)
    {
        if (!Nodes.TryGetValue(nodeId, out var node))
        {
            throw new ArgumentException($"Node with ID {nodeId} not found.");
        }

        // Mark this node and all descendants as pruned
        var nodesToPrune = GetDescendants(nodeId);
        nodesToPrune.Add(nodeId);

        foreach (var id in nodesToPrune)
        {
            if (Nodes.TryGetValue(id, out var n))
            {
                n.State = ThoughtNodeState.Pruned;
            }
        }
    }

    /// <summary>
    /// Gets all descendant node IDs of a given node.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>List of descendant node IDs.</returns>
    public List<string> GetDescendants(string nodeId)
    {
        var descendants = new List<string>();
        if (!Nodes.TryGetValue(nodeId, out var node))
        {
            return descendants;
        }

        foreach (var childId in node.ChildIds)
        {
            descendants.Add(childId);
            descendants.AddRange(GetDescendants(childId));
        }

        return descendants;
    }

    /// <summary>
    /// Gets the path from root to a specific node.
    /// </summary>
    /// <param name="nodeId">The target node ID.</param>
    /// <returns>List of node IDs representing the path.</returns>
    public List<string> GetPathToNode(string nodeId)
    {
        var path = new List<string>();
        var currentId = nodeId;

        while (currentId != null && Nodes.TryGetValue(currentId, out var node))
        {
            path.Insert(0, currentId);
            currentId = node.ParentId;
        }

        return path;
    }

    /// <summary>
    /// Completes the reasoning tree with a best path.
    /// </summary>
    /// <param name="bestPath">The best path found.</param>
    public void Complete(List<string> bestPath)
    {
        BestPath = bestPath;
        CompletedUtc = DateTimeOffset.UtcNow;

        // Mark nodes in the best path
        foreach (var nodeId in bestPath)
        {
            if (Nodes.TryGetValue(nodeId, out var node))
            {
                node.State = ThoughtNodeState.BestPath;
            }
        }
    }
}

/// <summary>
/// Represents the exploration strategy for Tree of Thoughts.
/// </summary>
public enum ExplorationStrategy
{
    /// <summary>
    /// Breadth-first exploration.
    /// </summary>
    BreadthFirst,

    /// <summary>
    /// Depth-first exploration.
    /// </summary>
    DepthFirst,

    /// <summary>
    /// Best-first exploration based on node scores.
    /// </summary>
    BestFirst,

    /// <summary>
    /// Beam search with limited beam width.
    /// </summary>
    BeamSearch,

    /// <summary>
    /// Monte Carlo tree search.
    /// </summary>
    MonteCarlo
}
