using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Agents;

/// <summary>
/// A stateful agent implementation that orchestrates LLM-powered task execution.
/// This is the main entry point for agent operations, delegating specific responsibilities
/// to specialized components.
/// </summary>
public sealed class Agent : IAgent
{
    private readonly AgentConfiguration _config;
    private readonly ILlmClient _llm;
    private readonly ILogger _logger;
    private readonly IAgentStateStore _store;
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;

    // Current agent state for status updates
    private string? _agentId;
    private AgentState? _state;

    public Agent(
        ILlmClient llmClient,
        IAgentStateStore stateStore,
        ILogger? logger = null,
        AgentConfiguration? config = null)
    {
        _llm = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _store = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? new ConsoleLogger();
        _config = config ?? new AgentConfiguration();
        
        // Initialize specialized components
        _eventManager = new EventManager(_logger);
        _statusManager = new StatusManager(_config, _eventManager);
        _orchestrator = new AgentOrchestrator(_llm, _store, _config, _logger, _eventManager, _statusManager);
    }

    /// <summary>
    /// Executes a complete agent run from start to finish, handling the full task execution lifecycle.
    /// </summary>
    /// <param name="agentId">Unique identifier for the agent instance.</param>
    /// <param name="goal">The goal or task description for the agent to accomplish.</param>
    /// <param name="tools">Collection of tools available to the agent for task execution.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// An <see cref="AgentResult"/> containing the execution outcome, final output, and agent state.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method orchestrates the complete agent execution flow:
    /// </para>
    /// <list type="number">
    /// <item><description>Initializes or loads agent state</description></item>
    /// <item><description>Executes steps until completion or max turns reached</description></item>
    /// <item><description>Handles tool execution and LLM communication</description></item>
    /// <item><description>Manages state persistence and event emission</description></item>
    /// <item><description>Returns final result with success/failure status</description></item>
    /// </list>
    /// <para>
    /// The agent will continue executing steps until one of the following conditions is met:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The agent produces a final output (success)</description></item>
    /// <item><description>The maximum number of turns is reached</description></item>
    /// <item><description>A cancellation is requested</description></item>
    /// <item><description>An unrecoverable error occurs</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentId"/>, <paramref name="goal"/>, or <paramref name="tools"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="ct"/>.</exception>
    public async Task<AgentResult> RunAsync(string agentId, string goal, IEnumerable<ITool> tools, CancellationToken ct = default)
    {
        _logger.LogInformation($"Starting agent run for {agentId} with goal: {goal}");

        // Set current agent state for status updates
        _agentId = agentId;

        // Raise run started event
        _eventManager.RaiseRunStarted(agentId, goal);

        // Emit initial status
        _statusManager.EmitStatus(agentId, "Starting agent run", $"Goal: {goal}", "Initializing tools and state", 0);

        var state = await EnsureState(agentId, goal, ct);
        _state = state; // Set current state for status updates
        var registry = tools.ToRegistry();

        for (var i = 0; i < _config.MaxTurns; i++)
        {
            _logger.LogDebug($"Executing turn {i + 1}/{_config.MaxTurns}");

            // Emit status before step with improved progress calculation
            var baseProgress = Math.Min(100, i * 100 / _config.MaxTurns);
            var progressPct = Math.Max(0, Math.Min(100, baseProgress));
            _statusManager.EmitStatus(agentId, "Processing step", $"Turn {i + 1} of {_config.MaxTurns}", "Analyzing goal and history", progressPct);

            // Raise step started event with actual turn index
            _eventManager.RaiseStepStarted(agentId, state.Turns.Count);

            var step = await _orchestrator.ExecuteStepAsync(state, registry, ct);
            await _store.SaveAsync(agentId, state, ct);

            // Raise step completed event
            _eventManager.RaiseStepCompleted(agentId, state.Turns.Count, step);

            if (!string.IsNullOrWhiteSpace(step.Error))
            {
                _logger.LogWarning($"Step error: {step.Error}");
                _statusManager.EmitStatus(agentId, "Step encountered error", step.Error, "Will attempt to recover", progressPct);
                // Continue loop allowing LLM to recover if possible
            }

            if (!step.Continue)
            {
                var result = new AgentResult
                {
                    Succeeded = step.FinalOutput != null,
                    FinalOutput = step.FinalOutput,
                    State = state,
                    Error = step.FinalOutput == null ? step.Error ?? "Stopped without final output" : null
                };

                _logger.LogInformation($"Agent run completed. Success: {result.Succeeded}");

                // Emit completion status
                var finalStatus = result.Succeeded ? "Task completed successfully" : "Task failed";
                _statusManager.EmitStatus(agentId, finalStatus, result.FinalOutput ?? result.Error, null, 100);

                // Raise run completed event
                _eventManager.RaiseRunCompleted(agentId, result.Succeeded, result.FinalOutput, result.Error, i + 1);

                return result;
            }
        }

        _logger.LogWarning($"Max turns {_config.MaxTurns} reached without completion");

        var maxTurnsResult = new AgentResult
        {
            Succeeded = false,
            FinalOutput = null,
            State = state,
            Error = $"Max turns {_config.MaxTurns} reached without finish."
        };

        // Raise run completed event for max turns reached
        _eventManager.RaiseRunCompleted(agentId, false, null, $"Max turns {_config.MaxTurns} reached without finish.", _config.MaxTurns);

        return maxTurnsResult;
    }

    /// <summary>
    /// Executes a single step of the agent, allowing for incremental task execution and state inspection.
    /// </summary>
    /// <param name="agentId">Unique identifier for the agent instance.</param>
    /// <param name="goal">The goal or task description for the agent to accomplish.</param>
    /// <param name="tools">Collection of tools available to the agent for task execution.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// An <see cref="AgentStepResult"/> containing the step execution outcome and continuation status.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method executes one step of the agent's reasoning and action cycle:
    /// </para>
    /// <list type="number">
    /// <item><description>Loads or initializes agent state</description></item>
    /// <item><description>Builds messages for LLM communication</description></item>
    /// <item><description>Executes reasoning if enabled</description></item>
    /// <item><description>Calls the LLM for decision making</description></item>
    /// <item><description>Executes tools if requested</description></item>
    /// <item><description>Updates agent state and persists changes</description></item>
    /// </list>
    /// <para>
    /// The method returns a result indicating whether the agent should continue with additional steps
    /// or has completed its task. This allows for fine-grained control over agent execution.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentId"/>, <paramref name="goal"/>, or <paramref name="tools"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="ct"/>.</exception>
    public async Task<AgentStepResult> StepAsync(string agentId, string goal, IEnumerable<ITool> tools, CancellationToken ct = default)
    {
        _logger.LogInformation($"Executing single step for agent {agentId}");

        // Set current agent state for status updates
        _agentId = agentId;

        var state = await EnsureState(agentId, goal, ct);
        _state = state; // Set current state for status updates

        // Use state.Turns.Count for accurate turn indexing
        var turnIndex = state.Turns.Count;

        // Raise step started event
        _eventManager.RaiseStepStarted(agentId, turnIndex);

        var registry = tools.ToRegistry();
        var step = await _orchestrator.ExecuteStepAsync(state, registry, ct);
        await _store.SaveAsync(agentId, state, ct);

        // Raise step completed event
        _eventManager.RaiseStepCompleted(agentId, turnIndex, step);

        return new AgentStepResult
        {
            Continue = step.Continue,
            ExecutedTool = step.ExecutedTool,
            FinalOutput = step.FinalOutput,
            LlmMessage = step.LlmMessage,
            ToolResult = step.ToolResult,
            State = state,
            Error = step.Error
        };
    }

    // Events for real-time monitoring
    public event EventHandler<AgentRunStartedEventArgs>? RunStarted
    {
        add => _eventManager.RunStarted += value;
        remove => _eventManager.RunStarted -= value;
    }

    public event EventHandler<AgentStepStartedEventArgs>? StepStarted
    {
        add => _eventManager.StepStarted += value;
        remove => _eventManager.StepStarted -= value;
    }

    public event EventHandler<AgentLlmCallStartedEventArgs>? LlmCallStarted
    {
        add => _eventManager.LlmCallStarted += value;
        remove => _eventManager.LlmCallStarted -= value;
    }

    public event EventHandler<AgentLlmCallCompletedEventArgs>? LlmCallCompleted
    {
        add => _eventManager.LlmCallCompleted += value;
        remove => _eventManager.LlmCallCompleted -= value;
    }

    public event EventHandler<AgentToolCallStartedEventArgs>? ToolCallStarted
    {
        add => _eventManager.ToolCallStarted += value;
        remove => _eventManager.ToolCallStarted -= value;
    }

    public event EventHandler<AgentToolCallCompletedEventArgs>? ToolCallCompleted
    {
        add => _eventManager.ToolCallCompleted += value;
        remove => _eventManager.ToolCallCompleted -= value;
    }

    public event EventHandler<AgentStepCompletedEventArgs>? StepCompleted
    {
        add => _eventManager.StepCompleted += value;
        remove => _eventManager.StepCompleted -= value;
    }

    public event EventHandler<AgentRunCompletedEventArgs>? RunCompleted
    {
        add => _eventManager.RunCompleted += value;
        remove => _eventManager.RunCompleted -= value;
    }

    /// <summary>
    /// Event raised when the agent emits public status updates for UI consumption.
    /// </summary>
    public event EventHandler<AgentStatusEventArgs>? StatusUpdate
    {
        add => _eventManager.StatusUpdate += value;
        remove => _eventManager.StatusUpdate -= value;
    }

    private async Task<AgentState> EnsureState(string agentId, string goal, CancellationToken ct)
    {
        var state = await _store.LoadAsync(agentId, ct) ?? new AgentState
        {
            AgentId = agentId,
            Goal = goal,
            Turns = new List<AgentTurn>()
        };
        state.Goal = string.IsNullOrWhiteSpace(state.Goal) ? goal : state.Goal;
        return state;
    }
}
