namespace AIAgentSharp.Agents.TreeOfThoughts;

/// <summary>
/// Handles LLM communication for evaluating thought nodes in Tree of Thoughts reasoning.
/// </summary>
internal sealed class TreeNodeEvaluator
{
    private readonly TreeOfThoughtsCommunicator _communicator;

    public TreeNodeEvaluator(TreeOfThoughtsCommunicator communicator)
    {
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
    }

    /// <summary>
    /// Evaluates a thought node using LLM.
    /// </summary>
    public async Task<double> EvaluateThoughtNodeAsync(ThoughtNode node, CancellationToken cancellationToken)
    {
        return await _communicator.EvaluateThoughtNodeAsync(node, cancellationToken);
    }
}
