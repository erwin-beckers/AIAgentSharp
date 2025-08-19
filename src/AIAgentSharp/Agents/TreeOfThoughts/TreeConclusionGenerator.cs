namespace AIAgentSharp.Agents.TreeOfThoughts;

/// <summary>
/// Handles LLM communication for generating conclusions from Tree of Thoughts reasoning.
/// </summary>
internal sealed class TreeConclusionGenerator
{
    private readonly TreeOfThoughtsCommunicator _communicator;

    public TreeConclusionGenerator(TreeOfThoughtsCommunicator communicator)
    {
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
    }

    /// <summary>
    /// Generates a conclusion from the best path found in the tree.
    /// </summary>
    public async Task<string> GenerateConclusionFromPathAsync(List<string> bestPath, string goal, string context, IDictionary<string, ITool> tools, ReasoningTree tree, CancellationToken cancellationToken)
    {
        return await _communicator.GenerateConclusionFromPathAsync(bestPath, goal, context, tools, tree, cancellationToken);
    }
}
