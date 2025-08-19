# Agent API Reference

Complete reference for the `Agent` class and its public API.

## Agent Class

The main `Agent` class is the core component of AIAgentSharp that orchestrates LLM interactions, tool execution, and reasoning.

### Constructor

```csharp
public Agent(
    ILlmClient llmClient,
    IAgentStateStore stateStore,
    AgentConfiguration? config = null,
    ILogger<Agent>? logger = null
)
```

**Parameters:**
- `llmClient`: The LLM client to use for communication
- `stateStore`: State store for persisting agent state
- `config`: Optional configuration for agent behavior
- `logger`: Optional logger for debugging and monitoring

### Properties

#### Public Properties

```csharp
// Events
public event EventHandler<AgentRunStartedEventArgs>? RunStarted;
public event EventHandler<AgentRunCompletedEventArgs>? RunCompleted;
public event EventHandler<AgentStatusUpdateEventArgs>? StatusUpdate;
public event EventHandler<AgentToolCallStartedEventArgs>? ToolCallStarted;
public event EventHandler<AgentToolCallCompletedEventArgs>? ToolCallCompleted;
public event EventHandler<AgentReasoningStartedEventArgs>? ReasoningStarted;
public event EventHandler<AgentReasoningCompletedEventArgs>? ReasoningCompleted;
public event EventHandler<AgentLlmChunkReceivedEventArgs>? LlmChunkReceived;

// Metrics and monitoring
public IMetricsCollector Metrics { get; }
public IEventManager EventManager { get; }
public AgentConfiguration Configuration { get; }
```

#### Internal Properties

```csharp
internal ILlmClient LlmClient { get; }
internal IAgentStateStore StateStore { get; }
internal ILogger<Agent> Logger { get; }
```

### Methods

#### Core Methods

##### RunAsync

```csharp
public async Task<AgentRunResult> RunAsync(
    string agentId,
    string goal,
    IDictionary<string, ITool> tools,
    CancellationToken ct = default
)
```

**Parameters:**
- `agentId`: Unique identifier for the agent session
- `goal`: The goal or task for the agent to accomplish
- `tools`: Dictionary of available tools indexed by name
- `ct`: Cancellation token for async operation

**Returns:**
- `AgentRunResult`: Result containing success status, output, and metadata

**Example:**
```csharp
var result = await agent.RunAsync(
    "travel-agent",
    "Plan a trip to Paris for next week",
    tools,
    CancellationToken.None
);

if (result.Succeeded)
{
    Console.WriteLine($"Success: {result.FinalOutput}");
}
else
{
    Console.WriteLine($"Failed: {result.ErrorMessage}");
}
```

##### RunWithReasoningAsync

```csharp
public async Task<AgentRunResult> RunWithReasoningAsync(
    string agentId,
    string goal,
    IDictionary<string, ITool> tools,
    ReasoningType reasoningType = ReasoningType.ChainOfThought,
    CancellationToken ct = default
)
```

**Parameters:**
- `agentId`: Unique identifier for the agent session
- `goal`: The goal or task for the agent to accomplish
- `tools`: Dictionary of available tools indexed by name
- `reasoningType`: Type of reasoning to use (ChainOfThought or TreeOfThoughts)
- `ct`: Cancellation token for async operation

**Returns:**
- `AgentRunResult`: Result containing success status, output, reasoning chain, and metadata

**Example:**
```csharp
var result = await agent.RunWithReasoningAsync(
    "complex-agent",
    "Solve a complex mathematical problem",
    tools,
    ReasoningType.ChainOfThought
);

if (result.Succeeded && result.State?.CurrentReasoningChain != null)
{
    var chain = result.State.CurrentReasoningChain;
    Console.WriteLine($"Reasoning steps: {chain.Steps.Count}");
    Console.WriteLine($"Final confidence: {chain.FinalConfidence:F2}");
}
```

#### State Management Methods

##### GetStateAsync

```csharp
public async Task<AgentState?> GetStateAsync(string agentId, CancellationToken ct = default)
```

**Parameters:**
- `agentId`: The agent ID to retrieve state for
- `ct`: Cancellation token

**Returns:**
- `AgentState?`: The current state or null if not found

**Example:**
```csharp
var state = await agent.GetStateAsync("my-agent");
if (state != null)
{
    Console.WriteLine($"Agent has {state.History.Count} messages in history");
}
```

##### DeleteStateAsync

```csharp
public async Task DeleteStateAsync(string agentId, CancellationToken ct = default)
```

**Parameters:**
- `agentId`: The agent ID to delete state for
- `ct`: Cancellation token

**Example:**
```csharp
await agent.DeleteStateAsync("my-agent");
```

#### Utility Methods

##### ValidateConfiguration

```csharp
public ValidationResult ValidateConfiguration()
```

**Returns:**
- `ValidationResult`: Validation result with any errors or warnings

**Example:**
```csharp
var validation = agent.ValidateConfiguration();
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"Configuration error: {error}");
    }
}
```

##### GetAvailableTools

```csharp
public IDictionary<string, ITool> GetAvailableTools()
```

**Returns:**
- `IDictionary<string, ITool>`: Dictionary of available tools

**Example:**
```csharp
var tools = agent.GetAvailableTools();
foreach (var tool in tools)
{
    Console.WriteLine($"Tool: {tool.Key} - {tool.Value.Description}");
}
```

## AgentRunResult

The result returned by agent execution methods.

### Properties

```csharp
public bool Succeeded { get; }
public string? FinalOutput { get; }
public string? ErrorMessage { get; }
public AgentState? State { get; }
public int TotalTurns { get; }
public TimeSpan ExecutionTime { get; }
public Dictionary<string, object> Metadata { get; }
```

### Example Usage

```csharp
var result = await agent.RunAsync("test-agent", "Hello", tools);

Console.WriteLine($"Success: {result.Succeeded}");
Console.WriteLine($"Output: {result.FinalOutput}");
Console.WriteLine($"Turns: {result.TotalTurns}");
Console.WriteLine($"Execution time: {result.ExecutionTime.TotalSeconds:F2}s");

if (!result.Succeeded)
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

## AgentState

Represents the current state of an agent.

### Properties

```csharp
public string AgentId { get; set; }
public string Goal { get; set; }
public List<ModelMessage> History { get; set; }
public int CurrentTurn { get; set; }
public DateTime CreatedAt { get; set; }
public DateTime LastUpdated { get; set; }
public ReasoningChain? CurrentReasoningChain { get; set; }
public Dictionary<string, object> Metadata { get; set; }
```

### Example Usage

```csharp
var state = await agent.GetStateAsync("my-agent");
if (state != null)
{
    Console.WriteLine($"Agent: {state.AgentId}");
    Console.WriteLine($"Goal: {state.Goal}");
    Console.WriteLine($"Current turn: {state.CurrentTurn}");
    Console.WriteLine($"History length: {state.History.Count}");
    Console.WriteLine($"Created: {state.CreatedAt}");
    Console.WriteLine($"Last updated: {state.LastUpdated}");
    
    if (state.CurrentReasoningChain != null)
    {
        Console.WriteLine($"Reasoning steps: {state.CurrentReasoningChain.Steps.Count}");
    }
}
```

## Event Arguments

### AgentRunStartedEventArgs

```csharp
public class AgentRunStartedEventArgs : EventArgs
{
    public string AgentId { get; }
    public string Goal { get; }
    public DateTime StartedAt { get; }
}
```

### AgentRunCompletedEventArgs

```csharp
public class AgentRunCompletedEventArgs : EventArgs
{
    public string AgentId { get; }
    public bool Success { get; }
    public int TotalTurns { get; }
    public TimeSpan ExecutionTime { get; }
    public DateTime CompletedAt { get; }
}
```

### AgentStatusUpdateEventArgs

```csharp
public class AgentStatusUpdateEventArgs : EventArgs
{
    public string AgentId { get; }
    public string StatusTitle { get; }
    public string StatusDetails { get; }
    public string? NextStepHint { get; }
    public int? ProgressPct { get; }
    public DateTime Timestamp { get; }
}
```

### AgentToolCallStartedEventArgs

```csharp
public class AgentToolCallStartedEventArgs : EventArgs
{
    public string AgentId { get; }
    public string ToolName { get; }
    public object Parameters { get; }
    public DateTime StartedAt { get; }
}
```

### AgentToolCallCompletedEventArgs

```csharp
public class AgentToolCallCompletedEventArgs : EventArgs
{
    public string AgentId { get; }
    public string ToolName { get; }
    public bool Success { get; }
    public object? Result { get; }
    public string? ErrorMessage { get; }
    public TimeSpan ExecutionTime { get; }
    public DateTime CompletedAt { get; }
}
```

### AgentReasoningStartedEventArgs

```csharp
public class AgentReasoningStartedEventArgs : EventArgs
{
    public string AgentId { get; }
    public ReasoningType ReasoningType { get; }
    public string Goal { get; }
    public DateTime StartedAt { get; }
}
```

### AgentReasoningCompletedEventArgs

```csharp
public class AgentReasoningCompletedEventArgs : EventArgs
{
    public string AgentId { get; }
    public ReasoningType ReasoningType { get; }
    public bool Success { get; }
    public ReasoningChain? ReasoningChain { get; }
    public string? ErrorMessage { get; }
    public TimeSpan ExecutionTime { get; }
    public DateTime CompletedAt { get; }
}
```

### AgentLlmChunkReceivedEventArgs

```csharp
public class AgentLlmChunkReceivedEventArgs : EventArgs
{
    public string AgentId { get; }
    public LlmStreamingChunk Chunk { get; }
    public DateTime ReceivedAt { get; }
}
```

## Complete Example

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

// Create components
var llm = new OpenAiLlmClient(apiKey);
var store = new MemoryAgentStateStore();

// Create agent with configuration
var config = new AgentConfiguration
{
    MaxTurns = 20,
    EnableReasoning = true,
    ReasoningType = ReasoningType.ChainOfThought,
    UseFunctionCalling = true,
    EmitPublicStatus = true
};

var agent = new Agent(llm, store, config);

// Subscribe to events
agent.RunStarted += (sender, e) =>
    Console.WriteLine($"Agent {e.AgentId} started with goal: {e.Goal}");

agent.StatusUpdate += (sender, e) =>
    Console.WriteLine($"Status: {e.StatusTitle} - {e.StatusDetails} (Progress: {e.ProgressPct}%)");

agent.ToolCallStarted += (sender, e) =>
    Console.WriteLine($"Tool {e.ToolName} called");

agent.ToolCallCompleted += (sender, e) =>
    Console.WriteLine($"Tool {e.ToolName} completed in {e.ExecutionTime.TotalMilliseconds:F0}ms");

agent.RunCompleted += (sender, e) =>
    Console.WriteLine($"Agent completed with {e.TotalTurns} turns in {e.ExecutionTime.TotalSeconds:F2}s");

// Create tools
var tools = new Dictionary<string, ITool>
{
    ["calculator"] = new CalculatorTool(),
    ["weather"] = new WeatherTool()
};

// Run agent
var result = await agent.RunAsync("test-agent", "Calculate 15 + 27 and get weather for London", tools);

// Process result
if (result.Succeeded)
{
    Console.WriteLine($"Success! Output: {result.FinalOutput}");
    Console.WriteLine($"Total turns: {result.TotalTurns}");
    Console.WriteLine($"Execution time: {result.ExecutionTime.TotalSeconds:F2}s");
    
    // Access state
    if (result.State != null)
    {
        Console.WriteLine($"History length: {result.State.History.Count}");
    }
}
else
{
    Console.WriteLine($"Failed: {result.ErrorMessage}");
}

// Clean up
await agent.DeleteStateAsync("test-agent");
```

This comprehensive API reference provides all the details you need to work with the Agent class effectively.
