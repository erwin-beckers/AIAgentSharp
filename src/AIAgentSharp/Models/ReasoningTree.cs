using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AIAgentSharp;

/// <summary>
/// Represents a complete Tree of Thoughts reasoning structure.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReasoningTree
{
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
    /// Gets a value indicating whether the tree has reached its maximum capacity.
    /// </summary>
    [JsonIgnore]
    public bool IsAtCapacity => NodeCount >= MaxNodes;

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
}