# Agent Framework

The Agent Framework is the core component of AIAgentSharp that orchestrates the interaction between LLMs, tools, and reasoning engines to create intelligent, autonomous agents.

## Overview

The `Agent` class is the main entry point for creating AI agents. It manages the complete lifecycle of an agent run, including:

- **Goal Processing**: Understanding and breaking down user goals
- **Reasoning**: Using advanced reasoning engines (Chain of Thought, Tree of Thoughts)
- **Tool Execution**: Calling and managing tool invocations
- **State Management**: Maintaining conversation and reasoning state
- **Event Monitoring**: Providing real-time status updates

## Basic Usage

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

// Create components
var llm = new OpenAiLlmClient(apiKey);
var store = new MemoryAgentStateStore();
var tools = new List<ITool>(); // Your tools here

// Create agent
var agent = new Agent(llm, store);

// Run the agent
var result = await agent.RunAsync(
    agentId: "my-agent",
    goal: "Help me plan a vacation to Paris",
    tools: tools
);
```

## Agent Configuration

The `AgentConfiguration` class allows you to customize agent behavior:

```csharp
var config = new AgentConfiguration
{
    // Conversation limits
    MaxTurns = 10,                    // Maximum conversation turns
    MaxTokens = 4000,                 // Maximum tokens per response
    
    // Reasoning settings
    ReasoningType = ReasoningType.ChainOfThought,  // Enable reasoning
    MaxReasoningSteps = 5,            // Maximum reasoning steps
    EnableReasoningValidation = true, // Validate reasoning quality
    
    // Function calling
    UseFunctionCalling = true,        // Enable OpenAI-style function calling
    
    // Monitoring
    EmitPublicStatus = true,          // Emit status events
    EnableLoopDetection = true,       // Prevent infinite loops
    
    // Response settings
    Temperature = 0.7,                // Creativity (0.0-1.0)
    TopP = 0.9,                       // Nucleus sampling
    
    // History management
    MaxHistoryLength = 20,            // Maximum conversation history
    EnableHistorySummarization = true // Summarize old messages
};

var agent = new Agent(llm, store, config: config);
```

## Agent Lifecycle

### 1. Initialization

```csharp
var agent = new Agent(llm, store, config: config);

// Subscribe to events (optional)
agent.RunStarted += (sender, e) => 
    Console.WriteLine($"Agent {e.AgentId} started with goal: {e.Goal}");

agent.StatusUpdate += (sender, e) => 
    Console.WriteLine($"Status: {e.StatusTitle} - {e.StatusDetails}");

agent.RunCompleted += (sender, e) => 
    Console.WriteLine($"Agent completed with {e.TotalTurns} turns");
```

### 2. Execution

```csharp
var result = await agent.RunAsync(
    agentId: "travel-agent",
    goal: "Plan a 3-day trip to Tokyo",
    tools: travelTools
);
```

### 3. Result Processing

```csharp
if (result.Succeeded)
{
    Console.WriteLine($"Final Output: {result.FinalOutput}");
    
    // Access reasoning information
    if (result.State?.CurrentReasoningChain != null)
    {
        var chain = result.State.CurrentReasoningChain;
        Console.WriteLine($"Reasoning Steps: {chain.Steps.Count}");
        Console.WriteLine($"Final Confidence: {chain.FinalConfidence:F2}");
    }
    
    // Access conversation history
    foreach (var turn in result.State?.ConversationHistory ?? new List<ConversationTurn>())
    {
        Console.WriteLine($"Turn {turn.TurnNumber}: {turn.UserInput}");
        Console.WriteLine($"Response: {turn.AgentResponse}");
    }
}
else
{
    Console.WriteLine($"Agent failed: {result.ErrorMessage}");
}
```

## Agent State

The agent maintains state across runs using the `AgentState` class:

```csharp
// Access current state
var state = result.State;

// Conversation history
var history = state.ConversationHistory;

// Current reasoning chain
var reasoningChain = state.CurrentReasoningChain;

// Agent metadata
var metadata = state.Metadata;
```

## Event System

The agent provides comprehensive event monitoring:

### Available Events

```csharp
// Agent lifecycle events
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

### Event Arguments

Each event provides relevant information:

```csharp
agent.StatusUpdate += (sender, e) =>
{
    Console.WriteLine($"Agent: {e.AgentId}");
    Console.WriteLine($"Status: {e.StatusTitle}");
    Console.WriteLine($"Details: {e.StatusDetails}");
    Console.WriteLine($"Progress: {e.ProgressPct}%");
};

agent.ToolCallStarted += (sender, e) =>
{
    Console.WriteLine($"Tool: {e.ToolName}");
    Console.WriteLine($"Parameters: {e.Parameters}");
    Console.WriteLine($"Turn: {e.TurnNumber}");
};
```

## Error Handling

The agent provides robust error handling:

```csharp
try
{
    var result = await agent.RunAsync("test-agent", "Your goal", tools);
    
    if (!result.Succeeded)
    {
        Console.WriteLine($"Agent failed: {result.ErrorMessage}");
        Console.WriteLine($"Error type: {result.ErrorType}");
        
        // Handle specific error types
        switch (result.ErrorType)
        {
            case AgentErrorType.MaxTurnsExceeded:
                Console.WriteLine("Agent exceeded maximum turns");
                break;
            case AgentErrorType.LlmError:
                Console.WriteLine("LLM communication error");
                break;
            case AgentErrorType.ToolError:
                Console.WriteLine("Tool execution error");
                break;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Performance Optimization

### Token Management

```csharp
var config = new AgentConfiguration
{
    MaxTokens = 2000,                 // Limit response size
    EnableHistorySummarization = true, // Summarize old messages
    MaxHistoryLength = 10             // Keep only recent history
};
```

### Caching

```csharp
// Use persistent state store for caching
var store = new FileAgentStateStore("agent-cache.json");
var agent = new Agent(llm, store, config);
```

### Parallel Processing

```csharp
// Run multiple agents in parallel
var tasks = goals.Select(goal => 
    agent.RunAsync($"agent-{Guid.NewGuid()}", goal, tools));

var results = await Task.WhenAll(tasks);
```

## Best Practices

### 1. Agent Naming

Use descriptive agent IDs for better monitoring:

```csharp
// Good
await agent.RunAsync("travel-planner-paris", "Plan Paris trip", tools);

// Avoid
await agent.RunAsync("agent1", "Plan Paris trip", tools);
```

### 2. Goal Clarity

Write clear, specific goals:

```csharp
// Good
"Plan a 3-day business trip to Tokyo with budget under $2000"

// Avoid
"Plan a trip"
```

### 3. Tool Selection

Only provide relevant tools:

```csharp
// Only include tools the agent might need
var relevantTools = allTools.Where(t => 
    IsRelevantForGoal(goal, t)).ToList();
```

### 4. Configuration Tuning

Adjust configuration based on use case:

```csharp
// For complex reasoning
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true
};

// For simple Q&A
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.None,
    MaxTurns = 3
};
```

## Advanced Features

### Custom State Stores

```csharp
public class CustomStateStore : IAgentStateStore
{
    public async Task<AgentState?> GetStateAsync(string agentId)
    {
        // Your custom implementation
    }
    
    public async Task SaveStateAsync(string agentId, AgentState state)
    {
        // Your custom implementation
    }
}

var store = new CustomStateStore();
var agent = new Agent(llm, store);
```

### Custom Metrics

```csharp
// Subscribe to metrics updates
agent.Metrics.MetricsUpdated += (sender, e) =>
{
    var metrics = e.Metrics;
    Console.WriteLine($"Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}");
    Console.WriteLine($"Average Tokens: {metrics.Resources.AverageTokensPerRun:F0}");
};
```

## Troubleshooting

### Common Issues

**Agent not responding**: Check LLM API key and network connectivity.

**Tool not being called**: Verify tool parameters match the LLM's function calling format.

**High token usage**: Enable history summarization and reduce max history length.

**Slow performance**: Consider using a faster LLM model or reducing reasoning steps.

For more troubleshooting help, see the [Troubleshooting Guide](troubleshooting/common-issues.md).
