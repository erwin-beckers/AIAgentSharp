using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Fluent;

/// <summary>
/// Fluent API builder for creating AIAgent instances with a more intuitive configuration approach.
/// </summary>
public class AIAgentBuilder
{
    private ILlmClient? _llmClient;
    private readonly List<ITool> _tools = new();
    private ReasoningType _reasoningType = ReasoningType.None;
    private ExplorationStrategy _explorationStrategy = ExplorationStrategy.DepthFirst;
    private int _maxDepth = 3;
    private int _maxTreeNodes = 50;
    private IAgentStateStore? _stateStore;
    private IMetricsCollector? _metricsCollector;
    private readonly List<Action<AgentStepCompletedEventArgs>> _stepCompletedHandlers = new();
    private readonly List<Action<AgentToolCallStartedEventArgs>> _toolCallStartedHandlers = new();
    private readonly List<Action<AgentToolCallCompletedEventArgs>> _toolCallCompletedHandlers = new();
    private readonly List<Action<AgentLlmCallStartedEventArgs>> _llmCallStartedHandlers = new();
    private readonly List<Action<AgentLlmCallCompletedEventArgs>> _llmCallCompletedHandlers = new();
    private readonly List<Action<AgentRunStartedEventArgs>> _runStartedHandlers = new();
    private readonly List<Action<AgentRunCompletedEventArgs>> _runCompletedHandlers = new();
    private readonly List<Action<AgentStepStartedEventArgs>> _stepStartedHandlers = new();
    private readonly List<Action<AgentStatusEventArgs>> _statusUpdateHandlers = new();
    private readonly List<Action<AgentLlmChunkReceivedEventArgs>> _llmChunkReceivedHandlers = new();
    private bool _enableStreaming = false;
    private readonly List<LlmMessage> _additionalMessages = new();
    // Configuration overrides
    private int? _maxRecentTurns;
    private bool? _enableHistorySummarization;
    private int? _maxToolOutputSize;
    private int? _maxThoughtsLength;
    private int? _maxFinalLength;
    private int? _maxSummaryLength;
    private TimeSpan? _llmTimeout;
    private TimeSpan? _toolTimeout;
    private bool? _emitPublicStatus;

    /// <summary>
    /// Sets the LLM client for the agent.
    /// </summary>
    /// <param name="llmClient">The LLM client to use</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithLlm(ILlmClient llmClient)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        return this;
    }

    /// <summary>
    /// Adds a custom system message to be included alongside the existing AIAgentSharp system prompt.
    /// </summary>
    /// <param name="content">The content of the system message</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithSystemMessage(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            _additionalMessages.Add(new LlmMessage { Role = "system", Content = content });
        }
        return this;
    }

    /// <summary>
    /// Adds a custom user message to be included in the conversation context.
    /// </summary>
    /// <param name="content">The content of the user message</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithUserMessage(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            _additionalMessages.Add(new LlmMessage { Role = "user", Content = content });
        }
        return this;
    }

    /// <summary>
    /// Adds a custom assistant message to be included in the conversation context.
    /// </summary>
    /// <param name="content">The content of the assistant message</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithAssistantMessage(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            _additionalMessages.Add(new LlmMessage { Role = "assistant", Content = content });
        }
        return this;
    }

    /// <summary>
    /// Adds multiple custom messages to be included in the conversation context.
    /// </summary>
    /// <param name="messages">The messages to add</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithMessages(params LlmMessage[] messages)
    {
        if (messages != null)
        {
            _additionalMessages.AddRange(messages.Where(m => m != null && !string.IsNullOrWhiteSpace(m.Content)));
        }
        return this;
    }

    /// <summary>
    /// Adds multiple custom messages to be included in the conversation context.
    /// </summary>
    /// <param name="messages">The messages to add</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithMessages(IEnumerable<LlmMessage> messages)
    {
        if (messages != null)
        {
            _additionalMessages.AddRange(messages.Where(m => m != null && !string.IsNullOrWhiteSpace(m.Content)));
        }
        return this;
    }

    /// <summary>
    /// Configures additional messages using a fluent action.
    /// </summary>
    /// <param name="configureMessages">Action to configure additional messages</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithMessages(Action<MessageCollectionBuilder> configureMessages)
    {
        var messageBuilder = new MessageCollectionBuilder();
        configureMessages(messageBuilder);
        _additionalMessages.AddRange(messageBuilder.Messages);
        return this;
    }

    /// <summary>
    /// Adds a single tool to the agent.
    /// </summary>
    /// <param name="tool">The tool to add</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithTool(ITool tool)
    {
        if (tool != null)
            _tools.Add(tool);
        return this;
    }

    /// <summary>
    /// Adds multiple tools to the agent.
    /// </summary>
    /// <param name="tools">The tools to add</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithTools(params ITool[] tools)
    {
        if (tools != null)
            _tools.AddRange(tools.Where(t => t != null));
        return this;
    }

    /// <summary>
    /// Adds multiple tools to the agent.
    /// </summary>
    /// <param name="tools">The tools to add</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithTools(IEnumerable<ITool> tools)
    {
        if (tools != null)
            _tools.AddRange(tools.Where(t => t != null));
        return this;
    }

    /// <summary>
    /// Configures tools using a fluent action.
    /// </summary>
    /// <param name="configureTools">Action to configure tools</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithTools(Action<ToolCollectionBuilder> configureTools)
    {
        var toolBuilder = new ToolCollectionBuilder();
        configureTools(toolBuilder);
        _tools.AddRange(toolBuilder.Tools);
        return this;
    }

    /// <summary>
    /// Sets the reasoning type for the agent.
    /// </summary>
    /// <param name="reasoningType">The reasoning type to use</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithReasoning(ReasoningType reasoningType)
    {
        _reasoningType = reasoningType;
        return this;
    }

    /// <summary>
    /// Sets the reasoning type with advanced configuration options.
    /// </summary>
    /// <param name="reasoningType">The reasoning type to use</param>
    /// <param name="configureOptions">Action to configure reasoning options</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithReasoning(ReasoningType reasoningType, Action<ReasoningOptionsBuilder> configureOptions)
    {
        _reasoningType = reasoningType;
        
        var optionsBuilder = new ReasoningOptionsBuilder();
        configureOptions(optionsBuilder);
        
        _explorationStrategy = optionsBuilder.ExplorationStrategy;
        _maxDepth = optionsBuilder.MaxDepth;
        _maxTreeNodes = optionsBuilder.MaxTreeNodes;
        
        return this;
    }

    /// <summary>
    /// Sets the state store for the agent.
    /// </summary>
    /// <param name="stateStore">The state store to use</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithStorage(IAgentStateStore stateStore)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        return this;
    }

    /// <summary>
    /// Sets the metrics collector for the agent.
    /// </summary>
    /// <param name="metricsCollector">The metrics collector to use</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithMetrics(IMetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        return this;
    }

    /// <summary>
    /// Configures event handling for the agent.
    /// </summary>
    /// <param name="configureEvents">Action to configure event handlers</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithEventHandling(Action<EventHandlingBuilder> configureEvents)
    {
        var eventBuilder = new EventHandlingBuilder();
        configureEvents(eventBuilder);
        
        _stepCompletedHandlers.AddRange(eventBuilder.StepCompletedHandlers);
        _toolCallStartedHandlers.AddRange(eventBuilder.ToolCallStartedHandlers);
        _toolCallCompletedHandlers.AddRange(eventBuilder.ToolCallCompletedHandlers);
        _llmCallStartedHandlers.AddRange(eventBuilder.LlmCallStartedHandlers);
        _llmCallCompletedHandlers.AddRange(eventBuilder.LlmCallCompletedHandlers);
        _runStartedHandlers.AddRange(eventBuilder.RunStartedHandlers);
        _runCompletedHandlers.AddRange(eventBuilder.RunCompletedHandlers);
        _stepStartedHandlers.AddRange(eventBuilder.StepStartedHandlers);
        _statusUpdateHandlers.AddRange(eventBuilder.StatusUpdateHandlers);
        _llmChunkReceivedHandlers.AddRange(eventBuilder.LlmChunkReceivedHandlers);
        
        return this;
    }

    /// <summary>
    /// Enables streaming for LLM calls.
    /// </summary>
    /// <param name="enable">Whether to enable streaming</param>
    /// <returns>The builder instance for method chaining</returns>
    public AIAgentBuilder WithStreaming(bool enable = true)
    {
        _enableStreaming = enable;
        return this;
    }

    /// <summary>
    /// Configures history retention and summarization behavior.
    /// </summary>
    /// <param name="maxRecentTurns">Number of recent turns to keep in full detail.</param>
    /// <param name="enableSummarization">Whether to summarize older history.</param>
    public AIAgentBuilder WithHistory(int maxRecentTurns, bool enableSummarization = true)
    {
        if (maxRecentTurns <= 0) throw new ArgumentOutOfRangeException(nameof(maxRecentTurns));
        _maxRecentTurns = maxRecentTurns;
        _enableHistorySummarization = enableSummarization;
        return this;
    }

    /// <summary>
    /// Sets maximum character limits for various output sections.
    /// Any null parameter will leave the default as-is.
    /// </summary>
    public AIAgentBuilder WithOutputLimits(int? maxThoughtsLength = null, int? maxFinalLength = null, int? maxSummaryLength = null, int? maxToolOutputSize = null)
    {
        if (maxThoughtsLength.HasValue && maxThoughtsLength.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxThoughtsLength));
        if (maxFinalLength.HasValue && maxFinalLength.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxFinalLength));
        if (maxSummaryLength.HasValue && maxSummaryLength.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxSummaryLength));
        if (maxToolOutputSize.HasValue && maxToolOutputSize.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxToolOutputSize));

        _maxThoughtsLength = maxThoughtsLength ?? _maxThoughtsLength;
        _maxFinalLength = maxFinalLength ?? _maxFinalLength;
        _maxSummaryLength = maxSummaryLength ?? _maxSummaryLength;
        _maxToolOutputSize = maxToolOutputSize ?? _maxToolOutputSize;
        return this;
    }

    /// <summary>
    /// Overrides default timeouts for LLM and tools.
    /// </summary>
    public AIAgentBuilder WithTimeouts(TimeSpan? llmTimeout = null, TimeSpan? toolTimeout = null)
    {
        if (llmTimeout.HasValue && llmTimeout.Value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(llmTimeout));
        if (toolTimeout.HasValue && toolTimeout.Value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(toolTimeout));
        _llmTimeout = llmTimeout ?? _llmTimeout;
        _toolTimeout = toolTimeout ?? _toolTimeout;
        return this;
    }

    /// <summary>
    /// Enables or disables emission of public status updates.
    /// </summary>
    public AIAgentBuilder WithStatusUpdates(bool enable)
    {
        _emitPublicStatus = enable;
        return this;
    }

    /// <summary>
    /// Gets the tools configured for this agent.
    /// </summary>
    public IEnumerable<ITool> Tools => _tools.AsReadOnly();

    /// <summary>
    /// Builds and returns the configured Agent instance.
    /// </summary>
    /// <returns>The configured Agent instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when required components are not configured</exception>
    public Agents.Agent Build()
    {
        if (_llmClient == null)
            throw new InvalidOperationException("LLM client must be configured using WithLlm()");

        // Create configuration using defaults with selective overrides
        var defaults = new AgentConfiguration();
        var configuration = new AgentConfiguration
        {
            ReasoningType = _reasoningType,
            TreeExplorationStrategy = _explorationStrategy,
            MaxTreeDepth = _maxDepth,
            MaxTreeNodes = _maxTreeNodes,
            UseFunctionCalling = !_enableStreaming,
            AdditionalMessages = _additionalMessages,
            MaxRecentTurns = _maxRecentTurns ?? defaults.MaxRecentTurns,
            EnableHistorySummarization = _enableHistorySummarization ?? defaults.EnableHistorySummarization,
            MaxToolOutputSize = _maxToolOutputSize ?? defaults.MaxToolOutputSize,
            MaxThoughtsLength = _maxThoughtsLength ?? defaults.MaxThoughtsLength,
            MaxFinalLength = _maxFinalLength ?? defaults.MaxFinalLength,
            MaxSummaryLength = _maxSummaryLength ?? defaults.MaxSummaryLength,
            LlmTimeout = _llmTimeout ?? defaults.LlmTimeout,
            ToolTimeout = _toolTimeout ?? defaults.ToolTimeout,
            EmitPublicStatus = _emitPublicStatus ?? defaults.EmitPublicStatus
        };

        // Create agent
        var agent = new Agents.Agent(_llmClient, _stateStore ?? new MemoryAgentStateStore(), null, configuration, _metricsCollector);

        // Wire up event handlers
        foreach (var handler in _stepCompletedHandlers)
        {
            agent.StepCompleted += (sender, e) => handler(e);
        }
        
        foreach (var handler in _toolCallStartedHandlers)
        {
            agent.ToolCallStarted += (sender, e) => handler(e);
        }
        
        foreach (var handler in _toolCallCompletedHandlers)
        {
            agent.ToolCallCompleted += (sender, e) => handler(e);
        }
        
        foreach (var handler in _llmCallStartedHandlers)
        {
            agent.LlmCallStarted += (sender, e) => handler(e);
        }
        
        foreach (var handler in _llmCallCompletedHandlers)
        {
            agent.LlmCallCompleted += (sender, e) => handler(e);
        }
        
        foreach (var handler in _runStartedHandlers)
        {
            agent.RunStarted += (sender, e) => handler(e);
        }
        
        foreach (var handler in _runCompletedHandlers)
        {
            agent.RunCompleted += (sender, e) => handler(e);
        }
        
        foreach (var handler in _stepStartedHandlers)
        {
            agent.StepStarted += (sender, e) => handler(e);
        }
        
        foreach (var handler in _statusUpdateHandlers)
        {
            agent.StatusUpdate += (sender, e) => handler(e);
        }

        foreach (var handler in _llmChunkReceivedHandlers)
        {
            agent.LlmChunkReceived += (sender, e) => handler(e);
        }

        return agent;
    }
}

/// <summary>
/// Builder for configuring message collections.
/// </summary>
public class MessageCollectionBuilder
{
    public List<LlmMessage> Messages { get; } = new();

    /// <summary>
    /// Adds a system message to the collection.
    /// </summary>
    /// <param name="content">The content of the system message</param>
    /// <returns>The builder instance for method chaining</returns>
    public MessageCollectionBuilder AddSystemMessage(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            Messages.Add(new LlmMessage { Role = "system", Content = content });
        }
        return this;
    }

    /// <summary>
    /// Adds a user message to the collection.
    /// </summary>
    /// <param name="content">The content of the user message</param>
    /// <returns>The builder instance for method chaining</returns>
    public MessageCollectionBuilder AddUserMessage(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            Messages.Add(new LlmMessage { Role = "user", Content = content });
        }
        return this;
    }

    /// <summary>
    /// Adds an assistant message to the collection.
    /// </summary>
    /// <param name="content">The content of the assistant message</param>
    /// <returns>The builder instance for method chaining</returns>
    public MessageCollectionBuilder AddAssistantMessage(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            Messages.Add(new LlmMessage { Role = "assistant", Content = content });
        }
        return this;
    }

    /// <summary>
    /// Adds a message to the collection.
    /// </summary>
    /// <param name="role">The role of the message (system, user, assistant)</param>
    /// <param name="content">The content of the message</param>
    /// <returns>The builder instance for method chaining</returns>
    public MessageCollectionBuilder AddMessage(string role, string content)
    {
        if (!string.IsNullOrWhiteSpace(role) && !string.IsNullOrWhiteSpace(content))
        {
            Messages.Add(new LlmMessage { Role = role, Content = content });
        }
        return this;
    }

    /// <summary>
    /// Adds multiple messages to the collection.
    /// </summary>
    /// <param name="messages">The messages to add</param>
    /// <returns>The builder instance for method chaining</returns>
    public MessageCollectionBuilder AddMessages(params LlmMessage[] messages)
    {
        if (messages != null)
        {
            Messages.AddRange(messages.Where(m => m != null && !string.IsNullOrWhiteSpace(m.Content)));
        }
        return this;
    }
}

/// <summary>
/// Builder for configuring tool collections.
/// </summary>
public class ToolCollectionBuilder
{
    public List<ITool> Tools { get; } = new();

    /// <summary>
    /// Adds a tool to the collection.
    /// </summary>
    /// <param name="tool">The tool to add</param>
    /// <returns>The builder instance for method chaining</returns>
    public ToolCollectionBuilder Add(ITool tool)
    {
        if (tool != null)
            Tools.Add(tool);
        return this;
    }

    /// <summary>
    /// Adds multiple tools to the collection.
    /// </summary>
    /// <param name="tools">The tools to add</param>
    /// <returns>The builder instance for method chaining</returns>
    public ToolCollectionBuilder Add(params ITool[] tools)
    {
        if (tools != null)
            Tools.AddRange(tools.Where(t => t != null));
        return this;
    }
}

/// <summary>
/// Builder for configuring reasoning options.
/// </summary>
public class ReasoningOptionsBuilder
{
    public ExplorationStrategy ExplorationStrategy { get; set; } = ExplorationStrategy.DepthFirst;
    public int MaxDepth { get; set; } = 3;
    public int MaxTreeNodes { get; set; } = 50;

    /// <summary>
    /// Sets the exploration strategy.
    /// </summary>
    /// <param name="strategy">The exploration strategy to use</param>
    /// <returns>The builder instance for method chaining</returns>
    public ReasoningOptionsBuilder SetExplorationStrategy(ExplorationStrategy strategy)
    {
        ExplorationStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the maximum depth for reasoning.
    /// </summary>
    /// <param name="maxDepth">The maximum depth</param>
    /// <returns>The builder instance for method chaining</returns>
    public ReasoningOptionsBuilder SetMaxDepth(int maxDepth)
    {
        MaxDepth = maxDepth;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of tree nodes for Tree of Thoughts reasoning.
    /// </summary>
    /// <param name="maxTreeNodes">The maximum number of tree nodes</param>
    /// <returns>The builder instance for method chaining</returns>
    public ReasoningOptionsBuilder SetMaxTreeNodes(int maxTreeNodes)
    {
        MaxTreeNodes = maxTreeNodes;
        return this;
    }
}

/// <summary>
/// Builder for configuring event handling.
/// </summary>
public class EventHandlingBuilder
{
    public List<Action<AgentStepCompletedEventArgs>> StepCompletedHandlers { get; } = new();
    public List<Action<AgentToolCallStartedEventArgs>> ToolCallStartedHandlers { get; } = new();
    public List<Action<AgentToolCallCompletedEventArgs>> ToolCallCompletedHandlers { get; } = new();
    public List<Action<AgentLlmCallStartedEventArgs>> LlmCallStartedHandlers { get; } = new();
    public List<Action<AgentLlmCallCompletedEventArgs>> LlmCallCompletedHandlers { get; } = new();
    public List<Action<AgentRunStartedEventArgs>> RunStartedHandlers { get; } = new();
    public List<Action<AgentRunCompletedEventArgs>> RunCompletedHandlers { get; } = new();
    public List<Action<AgentStepStartedEventArgs>> StepStartedHandlers { get; } = new();
    public List<Action<AgentStatusEventArgs>> StatusUpdateHandlers { get; } = new();
    public List<Action<AgentLlmChunkReceivedEventArgs>> LlmChunkReceivedHandlers { get; } = new();

    /// <summary>
    /// Adds a handler for step completed events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnStepCompleted(Action<AgentStepCompletedEventArgs> handler)
    {
        if (handler != null)
            StepCompletedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for tool call started events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnToolCallStarted(Action<AgentToolCallStartedEventArgs> handler)
    {
        if (handler != null)
            ToolCallStartedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for tool call completed events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnToolCallCompleted(Action<AgentToolCallCompletedEventArgs> handler)
    {
        if (handler != null)
            ToolCallCompletedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for LLM call started events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnLlmCallStarted(Action<AgentLlmCallStartedEventArgs> handler)
    {
        if (handler != null)
            LlmCallStartedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for LLM call completed events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnLlmCallCompleted(Action<AgentLlmCallCompletedEventArgs> handler)
    {
        if (handler != null)
            LlmCallCompletedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for run started events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnRunStarted(Action<AgentRunStartedEventArgs> handler)
    {
        if (handler != null)
            RunStartedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for run completed events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnRunCompleted(Action<AgentRunCompletedEventArgs> handler)
    {
        if (handler != null)
            RunCompletedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for step started events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnStepStarted(Action<AgentStepStartedEventArgs> handler)
    {
        if (handler != null)
            StepStartedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for status update events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnStatusUpdate(Action<AgentStatusEventArgs> handler)
    {
        if (handler != null)
            StatusUpdateHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler for LLM chunk received events.
    /// </summary>
    /// <param name="handler">The event handler</param>
    /// <returns>The builder instance for method chaining</returns>
    public EventHandlingBuilder OnLlmChunkReceived(Action<AgentLlmChunkReceivedEventArgs> handler)
    {
        if (handler != null)
            LlmChunkReceivedHandlers.Add(handler);
        return this;
    }
}
