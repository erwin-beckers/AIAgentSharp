namespace AIAgentSharp;

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