namespace AIAgentSharp.Agents.TreeOfThoughts;

/// <summary>
/// Handles LLM communication for generating thoughts in Tree of Thoughts reasoning.
/// </summary>
internal sealed class TreeThoughtGenerator
{
    private readonly TreeOfThoughtsCommunicator _communicator;

    public TreeThoughtGenerator(TreeOfThoughtsCommunicator communicator)
    {
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
    }

    /// <summary>
    /// Generates the initial root thought for the tree.
    /// </summary>
    public async Task<string> GenerateRootThoughtAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        return await _communicator.GenerateRootThoughtAsync(goal, context, tools, cancellationToken);
    }

    /// <summary>
    /// Generates child thoughts from a parent node.
    /// </summary>
    public async Task<List<ChildThought>> GenerateChildThoughtsAsync(ThoughtNode parentNode, CancellationToken cancellationToken)
    {
        return await _communicator.GenerateChildThoughtsAsync(parentNode, cancellationToken);
    }

    internal class ChildThought
    {
        public string Thought { get; set; } = "";
        public ThoughtType ThoughtType { get; set; } = ThoughtType.Hypothesis;
        public double EstimatedScore { get; set; } = 0.5;
    }
}
