namespace AIAgentSharp.Agents.TreeOfThoughts.Strategies;

/// <summary>
/// Factory for creating tree exploration strategies.
/// </summary>
internal static class TreeExplorationStrategyFactory
{
    private static readonly Dictionary<ExplorationStrategy, ITreeExplorationStrategy> _strategies = new()
    {
        [ExplorationStrategy.BestFirst] = new BestFirstExplorationStrategy(),
        [ExplorationStrategy.BreadthFirst] = new BreadthFirstExplorationStrategy(),
        [ExplorationStrategy.DepthFirst] = new DepthFirstExplorationStrategy(),
        [ExplorationStrategy.BeamSearch] = new BeamSearchExplorationStrategy(),
        [ExplorationStrategy.MonteCarlo] = new MonteCarloExplorationStrategy()
    };

    /// <summary>
    /// Gets the exploration strategy for the specified strategy type.
    /// </summary>
    /// <param name="strategy">The exploration strategy type.</param>
    /// <returns>The exploration strategy implementation.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported exploration strategy is specified.</exception>
    public static ITreeExplorationStrategy GetStrategy(ExplorationStrategy strategy)
    {
        if (!_strategies.TryGetValue(strategy, out var explorationStrategy))
        {
            throw new ArgumentException($"Unsupported exploration strategy: {strategy}");
        }

        return explorationStrategy;
    }

    /// <summary>
    /// Gets all available exploration strategies.
    /// </summary>
    /// <returns>Collection of all available exploration strategies.</returns>
    public static IEnumerable<ExplorationStrategy> GetAvailableStrategies()
    {
        return _strategies.Keys;
    }
}
