# State Management

AIAgentSharp provides comprehensive state management capabilities to persist agent state across sessions and enable long-running conversations.

## Overview

State management in AIAgentSharp allows agents to:
- **Persist conversations** across multiple sessions
- **Maintain context** and history for complex tasks
- **Resume conversations** from where they left off
- **Share state** between different agent instances
- **Optimize performance** through intelligent state management

## State Store Implementations

### Memory State Store

The `MemoryAgentStateStore` keeps state in memory and is perfect for:
- **Development and testing**
- **Short-lived conversations**
- **Single-process applications**

```csharp
using AIAgentSharp.StateStores;

var store = new MemoryAgentStateStore();
var agent = new Agent(llm, store);
```

### File State Store

The `FileAgentStateStore` persists state to disk and is ideal for:
- **Long-running conversations**
- **Multi-session persistence**
- **Debugging and analysis**

```csharp
using AIAgentSharp.StateStores;

var store = new FileAgentStateStore("agent-states");
var agent = new Agent(llm, store);
```

### Custom State Stores

Implement `IAgentStateStore` to create custom state stores:

```csharp
public class DatabaseStateStore : IAgentStateStore
{
    public async Task<AgentState?> GetStateAsync(string agentId, CancellationToken ct = default)
    {
        // Retrieve state from database
    }

    public async Task SaveStateAsync(string agentId, AgentState state, CancellationToken ct = default)
    {
        // Save state to database
    }

    public async Task DeleteStateAsync(string agentId, CancellationToken ct = default)
    {
        // Delete state from database
    }
}
```

## Agent State Structure

The `AgentState` class contains all information about an agent's current state:

```csharp
public class AgentState
{
    public string AgentId { get; set; } = default!;
    public string Goal { get; set; } = default!;
    public List<ModelMessage> History { get; set; } = new();
    public int CurrentTurn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public ReasoningChain? CurrentReasoningChain { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### Key Properties

- **AgentId**: Unique identifier for the agent
- **Goal**: The current goal or task
- **History**: Complete conversation history
- **CurrentTurn**: Current turn number in the conversation
- **CurrentReasoningChain**: Active reasoning chain (if using reasoning)
- **Metadata**: Custom data for extensibility

## State Lifecycle

### 1. State Creation

State is automatically created when an agent starts:

```csharp
var result = await agent.RunAsync("my-agent", "Hello, how are you?", tools);
// State is created automatically
```

### 2. State Updates

State is updated after each turn:

```csharp
// State is automatically updated after each turn
var result = await agent.RunAsync("my-agent", "Continue the conversation", tools);
```

### 3. State Retrieval

Retrieve existing state:

```csharp
var existingState = await store.GetStateAsync("my-agent");
if (existingState != null)
{
    Console.WriteLine($"Resuming conversation with {existingState.History.Count} messages");
}
```

### 4. State Cleanup

Clean up old state:

```csharp
await store.DeleteStateAsync("my-agent");
```

## History Management

### Automatic History Management

AIAgentSharp automatically manages conversation history:

```csharp
var config = new AgentConfiguration
{
    MaxHistoryLength = 50,           // Maximum messages to keep
    EnableHistorySummarization = true, // Summarize old messages
    HistorySummarizationThreshold = 20 // When to start summarizing
};

var agent = new Agent(llm, store, config: config);
```

### Manual History Management

Access and modify history directly:

```csharp
var state = await store.GetStateAsync("my-agent");
if (state != null)
{
    // Add custom message
    state.History.Add(new ModelMessage
    {
        Role = "user",
        Content = "Custom message"
    });
    
    // Save updated state
    await store.SaveStateAsync("my-agent", state);
}
```

## State Persistence Strategies

### Immediate Persistence

State is saved after each turn:

```csharp
var config = new AgentConfiguration
{
    PersistStateAfterEachTurn = true
};
```

### Batch Persistence

Save state periodically:

```csharp
var config = new AgentConfiguration
{
    PersistStateAfterEachTurn = false,
    PersistStateInterval = 5 // Save every 5 turns
};
```

## State Sharing and Isolation

### Agent Isolation

Each agent has its own isolated state:

```csharp
// These agents have separate state
var agent1 = new Agent(llm, store);
var agent2 = new Agent(llm, store);

await agent1.RunAsync("agent-1", "Hello", tools);
await agent2.RunAsync("agent-2", "Hello", tools);
// Each has its own state
```

### State Sharing

Share state between agents:

```csharp
// Use the same agent ID to share state
await agent1.RunAsync("shared-agent", "Hello", tools);
await agent2.RunAsync("shared-agent", "Continue", tools);
// Both use the same state
```

## Performance Considerations

### Memory Usage

- **Memory State Store**: Fast but uses RAM
- **File State Store**: Slower but persistent
- **Custom Stores**: Depends on implementation

### Optimization Tips

1. **Limit history length** to prevent memory bloat
2. **Enable summarization** for long conversations
3. **Use appropriate persistence strategy**
4. **Clean up old state** regularly

```csharp
var config = new AgentConfiguration
{
    MaxHistoryLength = 30,
    EnableHistorySummarization = true,
    HistorySummarizationThreshold = 15,
    PersistStateAfterEachTurn = false,
    PersistStateInterval = 3
};
```

## Error Handling

### State Store Errors

Handle state store failures gracefully:

```csharp
try
{
    var state = await store.GetStateAsync("my-agent");
    // Use state
}
catch (Exception ex)
{
    // Handle state store errors
    Console.WriteLine($"State store error: {ex.Message}");
    // Continue without state or use fallback
}
```

### State Corruption

Handle corrupted state:

```csharp
var state = await store.GetStateAsync("my-agent");
if (state != null && IsStateCorrupted(state))
{
    // Delete corrupted state
    await store.DeleteStateAsync("my-agent");
    // Start fresh
}
```

## Best Practices

### 1. Choose Appropriate State Store

- **Development**: Use `MemoryAgentStateStore`
- **Production**: Use `FileAgentStateStore` or custom database store
- **Testing**: Use `MemoryAgentStateStore` for isolation

### 2. Manage State Lifecycle

- **Clean up old state** regularly
- **Monitor state size** and performance
- **Handle state errors** gracefully

### 3. Optimize for Performance

- **Limit history length** appropriately
- **Enable summarization** for long conversations
- **Use batch persistence** when possible

### 4. Ensure Data Integrity

- **Validate state** before using
- **Handle corruption** gracefully
- **Backup important state** if needed

## Examples

### Complete State Management Example

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

// Create state store
var store = new FileAgentStateStore("agent-states");

// Create agent with state management
var llm = new OpenAiLlmClient(apiKey);
var agent = new Agent(llm, store);

// Check for existing state
var existingState = await store.GetStateAsync("travel-agent");
if (existingState != null)
{
    Console.WriteLine($"Resuming conversation with {existingState.History.Count} messages");
}

// Run agent (state is automatically managed)
var result = await agent.RunAsync("travel-agent", "Plan a trip to Paris", tools);

// Access current state
var currentState = await store.GetStateAsync("travel-agent");
Console.WriteLine($"Conversation has {currentState?.History.Count} messages");

// Clean up when done
await store.DeleteStateAsync("travel-agent");
```

This comprehensive state management system ensures your agents can maintain context, persist conversations, and provide a seamless user experience across multiple sessions.
