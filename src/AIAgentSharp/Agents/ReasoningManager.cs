using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// Manages reasoning engines and coordinates reasoning activities within the agent framework.
/// </summary>
public sealed class ReasoningManager
{
    private readonly ILlmClient _llm;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly Dictionary<ReasoningType, IReasoningEngine> _reasoningEngines;

    public ReasoningManager(
        ILlmClient llm,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));

        // Initialize reasoning engines
        _reasoningEngines = new Dictionary<ReasoningType, IReasoningEngine>
        {
            [ReasoningType.ChainOfThought] = new ChainOfThoughtEngine(_llm, _config, _logger, _eventManager, _statusManager),
            [ReasoningType.TreeOfThoughts] = new TreeOfThoughtsEngine(_llm, _config, _logger, _eventManager, _statusManager)
        };
    }

    /// <summary>
    /// Performs reasoning using the configured reasoning type.
    /// </summary>
    /// <param name="goal">The goal to reason about.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="tools">Available tools for the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reasoning result.</returns>
    /// <summary>
    /// Performs reasoning using the default reasoning strategy to analyze the goal and context.
    /// </summary>
    /// <param name="goal">The goal or objective to reason about.</param>
    /// <param name="context">Additional context information for reasoning.</param>
    /// <param name="tools">Available tools that can be used during reasoning.</param>
    /// <param name="cancellationToken">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A <see cref="ReasoningResult"/> containing the reasoning analysis and insights.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses the default reasoning strategy configured in the agent to analyze
    /// the given goal and context. The reasoning process may involve:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Breaking down complex goals into sub-tasks</description></item>
    /// <item><description>Identifying relevant tools and approaches</description></item>
    /// <item><description>Analyzing potential challenges and solutions</description></item>
    /// <item><description>Providing confidence levels and insights</description></item>
    /// </list>
    /// <para>
    /// The reasoning result can be used to enhance the agent's decision-making process
    /// and provide better context for subsequent actions.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    public async Task<ReasoningResult> ReasonAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Starting reasoning with type: {_config.ReasoningType}");

        if (!_reasoningEngines.TryGetValue(_config.ReasoningType, out var engine))
        {
            throw new InvalidOperationException($"No reasoning engine available for type: {_config.ReasoningType}");
        }

        return await engine.ReasonAsync(goal, context, tools, cancellationToken);
    }

    /// <summary>
    /// Performs reasoning using a specific reasoning type.
    /// </summary>
    /// <param name="reasoningType">The reasoning type to use.</param>
    /// <param name="goal">The goal to reason about.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="tools">Available tools for the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reasoning result.</returns>
    /// <summary>
    /// Performs reasoning using a specific reasoning strategy to analyze the goal and context.
    /// </summary>
    /// <param name="reasoningType">The type of reasoning strategy to use.</param>
    /// <param name="goal">The goal or objective to reason about.</param>
    /// <param name="context">Additional context information for reasoning.</param>
    /// <param name="tools">Available tools that can be used during reasoning.</param>
    /// <param name="cancellationToken">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A <see cref="ReasoningResult"/> containing the reasoning analysis and insights.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method allows explicit control over the reasoning strategy used. Available strategies include:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="ReasoningType.ChainOfThought"/>: Sequential step-by-step reasoning</description></item>
    /// <item><description><see cref="ReasoningType.TreeOfThoughts"/>: Multi-branch exploration of solutions</description></item>
    /// <item><description><see cref="ReasoningType.Hybrid"/>: Combination of multiple reasoning approaches</description></item>
    /// </list>
    /// <para>
    /// Each reasoning type provides different approaches to problem-solving and may be more
    /// suitable for different types of tasks or complexity levels.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when an unsupported reasoning type is specified.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    public async Task<ReasoningResult> ReasonAsync(ReasoningType reasoningType, string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Starting reasoning with type: {reasoningType}");

        if (!_reasoningEngines.TryGetValue(reasoningType, out var engine))
        {
            throw new InvalidOperationException($"No reasoning engine available for type: {reasoningType}");
        }

        return await engine.ReasonAsync(goal, context, tools, cancellationToken);
    }

    /// <summary>
    /// Gets the current reasoning chain if using Chain of Thought reasoning.
    /// </summary>
    /// <returns>The current reasoning chain, or null if not using CoT.</returns>
    public ReasoningChain? GetCurrentChain()
    {
        if (_config.ReasoningType == ReasoningType.ChainOfThought && 
            _reasoningEngines[_config.ReasoningType] is IChainOfThoughtEngine cotEngine)
        {
            return cotEngine.CurrentChain;
        }

        return null;
    }

    /// <summary>
    /// Gets the current reasoning tree if using Tree of Thoughts reasoning.
    /// </summary>
    /// <returns>The current reasoning tree, or null if not using ToT.</returns>
    public ReasoningTree? GetCurrentTree()
    {
        if (_config.ReasoningType == ReasoningType.TreeOfThoughts && 
            _reasoningEngines[_config.ReasoningType] is ITreeOfThoughtsEngine totEngine)
        {
            return totEngine.CurrentTree;
        }

        return null;
    }

    /// <summary>
    /// Gets the Chain of Thought engine if available.
    /// </summary>
    /// <returns>The CoT engine, or null if not available.</returns>
    public IChainOfThoughtEngine? GetChainOfThoughtEngine()
    {
        return _reasoningEngines.TryGetValue(ReasoningType.ChainOfThought, out var engine) 
            ? engine as IChainOfThoughtEngine 
            : null;
    }

    /// <summary>
    /// Gets the Tree of Thoughts engine if available.
    /// </summary>
    /// <returns>The ToT engine, or null if not available.</returns>
    public ITreeOfThoughtsEngine? GetTreeOfThoughtsEngine()
    {
        return _reasoningEngines.TryGetValue(ReasoningType.TreeOfThoughts, out var engine) 
            ? engine as ITreeOfThoughtsEngine 
            : null;
    }

    /// <summary>
    /// Checks if a specific reasoning type is supported.
    /// </summary>
    /// <param name="reasoningType">The reasoning type to check.</param>
    /// <returns>True if the reasoning type is supported.</returns>
    public bool IsReasoningTypeSupported(ReasoningType reasoningType)
    {
        return _reasoningEngines.ContainsKey(reasoningType);
    }

    /// <summary>
    /// Gets all supported reasoning types.
    /// </summary>
    /// <returns>Collection of supported reasoning types.</returns>
    public IEnumerable<ReasoningType> GetSupportedReasoningTypes()
    {
        return _reasoningEngines.Keys;
    }

    /// <summary>
    /// Performs hybrid reasoning using multiple reasoning approaches.
    /// </summary>
    /// <param name="goal">The goal to reason about.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="tools">Available tools for the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The combined reasoning result.</returns>
    public async Task<ReasoningResult> PerformHybridReasoningAsync(string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting hybrid reasoning combining multiple approaches");

        var results = new List<ReasoningResult>();

        // Perform Chain of Thought reasoning
        if (_reasoningEngines.TryGetValue(ReasoningType.ChainOfThought, out var cotEngine))
        {
            _logger.LogDebug("Performing Chain of Thought reasoning");
            var cotResult = await cotEngine.ReasonAsync(goal, context, tools, cancellationToken);
            results.Add(cotResult);
        }

        // Perform Tree of Thoughts reasoning
        if (_reasoningEngines.TryGetValue(ReasoningType.TreeOfThoughts, out var totEngine))
        {
            _logger.LogDebug("Performing Tree of Thoughts reasoning");
            var totResult = await totEngine.ReasonAsync(goal, context, tools, cancellationToken);
            results.Add(totResult);
        }

        // Combine results
        return await CombineReasoningResults(results, goal, context, tools, cancellationToken);
    }

    private async Task<ReasoningResult> CombineReasoningResults(List<ReasoningResult> results, string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        if (results.Count == 0)
        {
            return new ReasoningResult
            {
                Success = false,
                Error = "No reasoning results to combine",
                ExecutionTimeMs = 0
            };
        }

        if (results.Count == 1)
        {
            return results[0];
        }

        // Combine conclusions and calculate overall confidence
        var successfulResults = results.Where(r => r.Success).ToList();
        if (successfulResults.Count == 0)
        {
            return new ReasoningResult
            {
                Success = false,
                Error = "All reasoning approaches failed",
                ExecutionTimeMs = results.Sum(r => r.ExecutionTimeMs)
            };
        }

        var combinedConclusion = await GenerateCombinedConclusionAsync(successfulResults, goal, context, tools, cancellationToken);
        var averageConfidence = successfulResults.Average(r => r.Confidence);
        var totalExecutionTime = results.Sum(r => r.ExecutionTimeMs);

        return new ReasoningResult
        {
            Success = true,
            Conclusion = combinedConclusion,
            Confidence = averageConfidence,
            ExecutionTimeMs = totalExecutionTime,
            Metadata = new Dictionary<string, object>
            {
                ["reasoning_type"] = "Hybrid",
                ["approaches_used"] = successfulResults.Count,
                ["individual_results"] = successfulResults
            }
        };
    }

    private async Task<string> GenerateCombinedConclusionAsync(List<ReasoningResult> results, string goal, string context, IDictionary<string, ITool> tools, CancellationToken cancellationToken)
    {
        var conclusions = results.Select((r, i) => $"Approach {i + 1} ({r.Confidence:F2} confidence): {r.Conclusion}").ToList();
        var conclusionsText = string.Join("\n\n", conclusions);

        var toolDescriptions = string.Join("\n", tools.Values.Select(t => $"- {t.Name}"));
        
        var prompt = $@"You are synthesizing conclusions from multiple reasoning approaches.

GOAL: {goal}
CONTEXT: {context}

INDIVIDUAL CONCLUSIONS:
{conclusionsText}

AVAILABLE TOOLS:
{toolDescriptions}

TASK: Synthesize a comprehensive conclusion that combines the insights from all reasoning approaches. Consider:
1. Common themes and agreements across approaches
2. Complementary insights from different perspectives
3. Confidence levels and reliability of each approach
4. Practical next steps and recommendations

Provide your synthesized conclusion in the following JSON format:
{{
  ""conclusion"": ""Your comprehensive synthesized conclusion""
}}

Focus on creating a unified, actionable conclusion that leverages the strengths of all approaches.";

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = prompt } };
        var response = await _llm.CompleteAsync(messages, cancellationToken);
        return ExtractConclusionFromResponse(response);
    }

    private string ExtractConclusionFromResponse(string response)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            return json.RootElement.GetProperty("conclusion").GetString() ?? "";
        }
        catch
        {
            return response;
        }
    }
}
