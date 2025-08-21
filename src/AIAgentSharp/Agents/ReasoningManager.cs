using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Agents.TreeOfThoughts;
using AIAgentSharp.Agents.ChainOfThought;
using AIAgentSharp.Agents.Hybrid;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Agents;

/// <summary>
/// Manages reasoning engines and coordinates reasoning activities within the agent framework.
/// </summary>
public sealed class ReasoningManager : IReasoningManager
{
    private readonly ILlmClient _llm;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly IMetricsCollector _metricsCollector;
    private readonly Dictionary<ReasoningType, IReasoningEngine> _reasoningEngines;

    public ReasoningManager(
        ILlmClient llm,
        AgentConfiguration config,
        ILogger logger,
        IEventManager eventManager,
        IStatusManager statusManager,
        IMetricsCollector metricsCollector,
        ILlmCommunicator? llmCommunicator = null)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));

        // Initialize reasoning engines with shared LlmCommunicator
        _reasoningEngines = new Dictionary<ReasoningType, IReasoningEngine>
        {
            [ReasoningType.ChainOfThought] = new ChainOfThoughtEngine(_llm, _config, _logger, _eventManager, _statusManager, _metricsCollector, llmCommunicator),
            [ReasoningType.TreeOfThoughts] = new TreeOfThoughtsEngine(_llm, _config, _logger, _eventManager, _statusManager, _metricsCollector, llmCommunicator),
            [ReasoningType.Hybrid] = new HybridEngine(_llm, _config, _logger, _eventManager, _statusManager, _metricsCollector, llmCommunicator)
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
        if (string.IsNullOrEmpty(goal)) throw new ArgumentNullException(nameof(goal));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (tools == null) throw new ArgumentNullException(nameof(tools));
        cancellationToken.ThrowIfCancellationRequested();

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
        if (string.IsNullOrEmpty(goal)) throw new ArgumentNullException(nameof(goal));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (tools == null) throw new ArgumentNullException(nameof(tools));
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation($"Starting reasoning with type: {reasoningType}");

        if (!_reasoningEngines.TryGetValue(reasoningType, out var engine))
        {
            throw new InvalidOperationException($"No reasoning engine available for type: {reasoningType}");
        }

        return await engine.ReasonAsync(goal, context, tools, cancellationToken);
    }
}
