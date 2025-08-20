using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AIAgentSharp;

/// <summary>
/// Represents a node in a Tree of Thoughts reasoning structure.
/// </summary>
[ExcludeFromCodeCoverage]
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