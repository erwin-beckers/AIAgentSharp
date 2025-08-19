namespace AIAgentSharp.Agents.ChainOfThought;

/// <summary>
/// Handles Chain of Thought reasoning chain operations.
/// </summary>
public sealed class ChainOperations
{
    private readonly ILogger _logger;

    public ChainOperations(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds a reasoning step to the chain.
    /// </summary>
    public ReasoningStep AddStep(
        ReasoningChain chain, 
        string reasoning, 
        ReasoningStepType stepType = ReasoningStepType.Analysis, 
        double confidence = 0.5, 
        List<string>? insights = null)
    {
        var step = chain.AddStep(reasoning, stepType, confidence, insights);
        _logger.LogDebug($"Added reasoning step {step.StepNumber}: {stepType} - {reasoning.Substring(0, Math.Min(100, reasoning.Length))}...");

        return step;
    }

    /// <summary>
    /// Completes the reasoning chain with a conclusion.
    /// </summary>
    public void CompleteChain(ReasoningChain chain, string conclusion, double confidence = 0.5)
    {
        chain.Complete(conclusion, confidence);
        _logger.LogInformation($"Completed reasoning chain with conclusion: {conclusion}");
    }

    /// <summary>
    /// Creates a new reasoning chain.
    /// </summary>
    public ReasoningChain CreateChain(string goal)
    {
        return new ReasoningChain
        {
            Goal = goal,
            CreatedUtc = DateTimeOffset.UtcNow
        };
    }
}
