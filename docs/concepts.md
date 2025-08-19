# Basic Concepts

This guide introduces the core concepts and terminology used in AIAgentSharp.

## What is AIAgentSharp?

AIAgentSharp is a comprehensive .NET framework for building intelligent AI agents that can:

- **Reason**: Use advanced reasoning engines to think through problems
- **Act**: Execute tools and perform actions
- **Observe**: Process results and adapt their approach
- **Learn**: Maintain state and improve over time

## Core Components

### Agent

An **Agent** is the main orchestrator that manages the complete lifecycle of an AI task:

```csharp
var agent = new Agent(llm, store, config);
var result = await agent.RunAsync("my-agent", "Your goal", tools);
```

**Key Responsibilities:**
- Goal processing and understanding
- Tool selection and execution
- Reasoning and decision-making
- State management and persistence
- Event monitoring and metrics

### LLM Client

An **LLM Client** provides the interface to Large Language Models:

```csharp
var llm = new OpenAiLlmClient(apiKey);
// or
var llm = new AnthropicLlmClient(apiKey);
```

**Supported Providers:**
- OpenAI (GPT-4, GPT-3.5-turbo)
- Anthropic (Claude-3, Claude-2)
- Google (Gemini Pro, Gemini Flash)
- Mistral AI (Mistral Large, Medium, Small)

### State Store

A **State Store** manages agent state persistence:

```csharp
var store = new MemoryAgentStateStore();  // In-memory
// or
var store = new FileAgentStateStore("agent-state.json");  // File-based
```

**Stored Information:**
- Conversation history
- Reasoning chains
- Tool execution results
- Agent metadata

### Tools

**Tools** are functions that agents can call to perform actions:

```csharp
public class WeatherTool : BaseTool<WeatherParams, object>
{
    public override string Name => "get_weather";
    public override string Description => "Get weather information";
    
    protected override async Task<object> InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
    {
        // Tool implementation
    }
}
```

**Tool Features:**
- Type-safe parameters and return values
- Automatic schema generation
- Built-in validation
- Error handling

## Reasoning Engines

### Chain of Thought (CoT)

Chain of Thought reasoning breaks down problems into sequential steps:

```
Problem → Analysis → Planning → Execution → Evaluation → Solution
```

**Use Cases:**
- Mathematical reasoning
- Step-by-step problem solving
- Logical deduction
- Process planning

### Tree of Thoughts (ToT)

Tree of Thoughts explores multiple solution paths simultaneously:

```
Problem
├── Approach 1 → Evaluate → Expand
├── Approach 2 → Evaluate → Prune
└── Approach 3 → Evaluate → Select Best
```

**Use Cases:**
- Creative problem solving
- Decision making with trade-offs
- Complex planning scenarios
- Optimization problems

## Agent Lifecycle

### 1. Initialization

```csharp
var agent = new Agent(llm, store, config);
agent.StatusUpdate += (sender, e) => Console.WriteLine($"Status: {e.StatusTitle}");
```

### 2. Execution

```csharp
var result = await agent.RunAsync("agent-id", "goal", tools);
```

### 3. Processing

The agent follows this process:
1. **Goal Understanding**: Parse and understand the user's goal
2. **Reasoning**: Use reasoning engines to plan the approach
3. **Tool Selection**: Choose appropriate tools for the task
4. **Execution**: Execute tools and process results
5. **Evaluation**: Assess progress and adjust if needed
6. **Completion**: Provide final output

### 4. Result Handling

```csharp
if (result.Succeeded)
{
    Console.WriteLine($"Output: {result.FinalOutput}");
    
    // Access reasoning information
    if (result.State?.CurrentReasoningChain != null)
    {
        var chain = result.State.CurrentReasoningChain;
        Console.WriteLine($"Confidence: {chain.FinalConfidence:F2}");
    }
}
```

## Event System

AIAgentSharp provides comprehensive event monitoring:

### Agent Events

```csharp
// Lifecycle events
agent.RunStarted += (sender, e) => { /* Agent started */ };
agent.RunCompleted += (sender, e) => { /* Agent completed */ };

// Status updates
agent.StatusUpdate += (sender, e) => { /* Status changed */ };

// Tool events
agent.ToolCallStarted += (sender, e) => { /* Tool called */ };
agent.ToolCallCompleted += (sender, e) => { /* Tool completed */ };

// Reasoning events
agent.ReasoningStarted += (sender, e) => { /* Reasoning started */ };
agent.ReasoningCompleted += (sender, e) => { /* Reasoning completed */ };

// LLM events
agent.LlmCallStarted += (sender, e) => { /* LLM call started */ };
agent.LlmCallCompleted += (sender, e) => { /* LLM call completed */ };
agent.LlmChunkReceived += (sender, e) => { /* LLM chunk received */ };
```

### Metrics Events

```csharp
agent.Metrics.MetricsUpdated += (sender, e) =>
{
    var metrics = e.Metrics;
    Console.WriteLine($"Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}");
    Console.WriteLine($"Total Tokens: {metrics.Resources.TotalTokens:N0}");
};
```

## Configuration

### Agent Configuration

```csharp
var config = new AgentConfiguration
{
    // Conversation limits
    MaxTurns = 10,                    // Maximum conversation turns
    MaxTokens = 4000,                 // Maximum tokens per response
    
    // Reasoning settings
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 5,
    EnableReasoningValidation = true,
    
    // Function calling
    UseFunctionCalling = true,
    
    // Monitoring
    EmitPublicStatus = true,
    EnableLoopDetection = true,
    
    // Response settings
    Temperature = 0.7,
    TopP = 0.9
};
```

### LLM Configuration

```csharp
var llmConfig = new OpenAiConfiguration
{
    Model = "gpt-4",
    MaxTokens = 4000,
    Temperature = 0.7,
    Timeout = TimeSpan.FromSeconds(30)
};

var llm = new OpenAiLlmClient(apiKey, llmConfig);
```

## State Management

### Agent State

```csharp
var state = result.State;

// Conversation history
var history = state.ConversationHistory;

// Current reasoning chain
var reasoningChain = state.CurrentReasoningChain;

// Metadata
var metadata = state.Metadata;
```

### State Persistence

```csharp
// Save state
await store.SaveStateAsync("agent-id", state);

// Load state
var savedState = await store.GetStateAsync("agent-id");
```

## Error Handling

### Agent Errors

```csharp
if (!result.Succeeded)
{
    switch (result.ErrorType)
    {
        case AgentErrorType.MaxTurnsExceeded:
            // Agent exceeded maximum turns
            break;
        case AgentErrorType.LlmError:
            // LLM communication error
            break;
        case AgentErrorType.ToolError:
            // Tool execution error
            break;
    }
}
```

### Tool Errors

```csharp
try
{
    var toolResult = await tool.InvokeAsync(parameters);
}
catch (ToolExecutionException ex)
{
    Console.WriteLine($"Tool error: {ex.Message}");
}
```

## Performance Monitoring

### Metrics

```csharp
var metrics = agent.Metrics.GetMetrics();

// Performance metrics
Console.WriteLine($"Total Runs: {metrics.Performance.TotalAgentRuns}");
Console.WriteLine($"Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}");

// Resource metrics
Console.WriteLine($"Total Tokens: {metrics.Resources.TotalTokens:N0}");
Console.WriteLine($"Average Tokens per Run: {metrics.Resources.AverageTokensPerRun:F0}");

// Reasoning metrics
Console.WriteLine($"Reasoning Success Rate: {metrics.Reasoning.ReasoningSuccessRate:P2}");
Console.WriteLine($"Average Reasoning Steps: {metrics.Reasoning.AverageReasoningSteps:F1}");
```

### Real-time Monitoring

```csharp
agent.Metrics.MetricsUpdated += (sender, e) =>
{
    var metrics = e.Metrics;
    // Update dashboard or logging
};
```

## Best Practices

### 1. Agent Design

- **Clear Goals**: Write specific, actionable goals
- **Relevant Tools**: Only provide tools the agent might need
- **Appropriate Configuration**: Match configuration to use case

### 2. Error Handling

- **Graceful Degradation**: Handle errors without crashing
- **Retry Logic**: Implement retries for transient failures
- **User Feedback**: Provide meaningful error messages

### 3. Performance

- **Token Management**: Monitor and optimize token usage
- **Caching**: Cache frequently used data
- **Parallel Processing**: Run multiple agents when possible

### 4. Monitoring

- **Event Subscription**: Subscribe to relevant events
- **Metrics Tracking**: Monitor performance metrics
- **Logging**: Log important events and errors

## Common Patterns

### Agent Factory

```csharp
public class AgentFactory
{
    public static Agent CreateAgent(AgentType type, string apiKey)
    {
        var llm = CreateLlmClient(type, apiKey);
        var store = new MemoryAgentStateStore();
        var config = CreateConfig(type);
        
        return new Agent(llm, store, config);
    }
    
    private static ILlmClient CreateLlmClient(AgentType type, string apiKey)
    {
        return type switch
        {
            AgentType.OpenAI => new OpenAiLlmClient(apiKey),
            AgentType.Anthropic => new AnthropicLlmClient(apiKey),
            _ => throw new ArgumentException($"Unknown agent type: {type}")
        };
    }
}
```

### Tool Registry

```csharp
public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new();
    
    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
    }
    
    public List<ITool> GetRelevantTools(string goal)
    {
        // Implement tool selection logic
        return _tools.Values.Where(t => IsRelevant(goal, t)).ToList();
    }
}
```

## Next Steps

Now that you understand the basic concepts, explore:

1. **[Quick Start Guide](quick-start.md)** - Get up and running quickly
2. **[Agent Framework](agent-framework.md)** - Learn about agent configuration and lifecycle
3. **[Tool Framework](tool-framework.md)** - Create custom tools
4. **[Reasoning Engines](reasoning-engines.md)** - Understand reasoning capabilities
5. **[Examples](examples/)** - See practical usage patterns
