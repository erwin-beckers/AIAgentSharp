namespace AIAgentSharp;

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