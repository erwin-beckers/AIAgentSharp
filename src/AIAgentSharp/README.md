# AIAgentSharp

A powerful .NET library for building AI agents with LLM integration, tool execution, and state management.

## Features

- **Multi-LLM Support**: Integrate with OpenAI, Anthropic Claude, Mistral AI, and Google Gemini
- **Tool Execution**: Execute custom tools and functions with automatic parameter extraction
- **State Management**: Persistent agent state with multiple storage backends
- **Event System**: Comprehensive event handling for monitoring and debugging
- **Reasoning Engines**: Chain-of-thought and tree-of-thoughts reasoning capabilities
- **Loop Detection**: Prevent infinite loops and repetitive behavior
- **Metrics Collection**: Built-in performance and usage metrics
- **Extensible Architecture**: Plugin-based design for easy customization

## Installation

```bash
dotnet add package AIAgentSharp
```

## Quick Start

### Basic Usage

```csharp
using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;

// Create LLM client (example with OpenAI)
var llm = new OpenAiLlmClient("your-api-key");

// Create state store
var store = new MemoryAgentStateStore();

// Create agent
var agent = new Agent(llm, store);

// Run agent
var result = await agent.RunAsync("my-agent", "Hello, how are you?", new List<ITool>());
Console.WriteLine(result.FinalOutput);
```

### With Tools

```csharp
// Create custom tools
public class CalculatorTool : BaseTool
{
    public override string Name => "calculator";
    public override string Description => "Performs mathematical calculations";

    [ToolParams("The mathematical expression to evaluate")]
    public string Expression { get; set; } = string.Empty;

    public override async Task<string> ExecuteAsync()
    {
        // Implementation here
        return $"Result: {Expression}";
    }
}

// Use tools with agent
var tools = new List<ITool> { new CalculatorTool() };
var result = await agent.RunAsync("agent-id", "What's 2+2?", tools);
```

### With State Persistence

```csharp
// Use file-based state store for persistence
var store = new FileAgentStateStore("agent-states");

// Agent state will be automatically saved and restored
var agent = new Agent(llm, store);
```

## Core Concepts

### Agents

Agents are the main entry point for AI interactions:

```csharp
var agent = new Agent(llm, stateStore, configuration);
```

### Tools

Tools allow agents to perform actions:

```csharp
public class MyTool : BaseTool
{
    public override string Name => "my_tool";
    public override string Description => "Description of what this tool does";

    [ToolParams("Parameter description")]
    public string Parameter { get; set; } = string.Empty;

    public override async Task<string> ExecuteAsync()
    {
        // Tool implementation
        return "Tool result";
    }
}
```

### State Stores

State stores manage agent memory:

```csharp
// In-memory (temporary)
var store = new MemoryAgentStateStore();

// File-based (persistent)
var store = new FileAgentStateStore("path/to/states");

// Custom implementation
public class MyStateStore : IAgentStateStore
{
    // Implementation
}
```

### Configuration

Configure agent behavior:

```csharp
var config = new AgentConfiguration
{
    MaxIterations = 10,
    EnableLoopDetection = true,
    EnableMetrics = true,
    ReasoningEngine = ReasoningEngine.ChainOfThought
};
```

## Advanced Features

### Reasoning Engines

```csharp
// Chain-of-thought reasoning
var config = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought
};

// Tree-of-thoughts reasoning
var config = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    MaxBranches = 5,
    MaxDepth = 3
};
```

### Event Handling

```csharp
// Subscribe to agent events
agent.AgentRunStarted += (sender, e) => Console.WriteLine($"Agent {e.AgentId} started");
agent.AgentRunCompleted += (sender, e) => Console.WriteLine($"Agent {e.AgentId} completed");
agent.AgentStepStarted += (sender, e) => Console.WriteLine($"Step {e.StepNumber} started");
agent.ToolCallStarted += (sender, e) => Console.WriteLine($"Tool {e.ToolName} called");
```

### Metrics Collection

```csharp
var metricsCollector = new MetricsCollector();
var config = new AgentConfiguration
{
    MetricsCollector = metricsCollector,
    EnableMetrics = true
};

// Access metrics
var metrics = metricsCollector.GetMetrics();
Console.WriteLine($"Total tokens used: {metrics.TotalTokens}");
```

### Custom Logging

```csharp
public class MyLogger : ILogger
{
    public void Log(LogLevel level, string message)
    {
        // Custom logging implementation
    }
}

var logger = new MyLogger();
var agent = new Agent(llm, store, config, logger);
```

## LLM Integrations

AIAgentSharp supports multiple LLM providers:

- **OpenAI**: `AIAgentSharp.OpenAI` package
- **Anthropic Claude**: `AIAgentSharp.Anthropic` package  
- **Mistral AI**: `AIAgentSharp.Mistral` package
- **Google Gemini**: `AIAgentSharp.Gemini` package

## Error Handling

```csharp
try
{
    var result = await agent.RunAsync("agent-id", "Hello", tools);
}
catch (AgentException ex)
{
    Console.WriteLine($"Agent error: {ex.Message}");
}
catch (ToolExecutionException ex)
{
    Console.WriteLine($"Tool execution error: {ex.Message}");
}
catch (LoopDetectedException ex)
{
    Console.WriteLine($"Loop detected: {ex.Message}");
}
```

## Performance Optimization

### Memory Management

```csharp
// Use memory store for temporary sessions
var store = new MemoryAgentStateStore();

// Use file store for long-running agents
var store = new FileAgentStateStore("agent-states");
```

### Configuration Tuning

```csharp
var config = new AgentConfiguration
{
    MaxIterations = 5,           // Limit iterations
    EnableLoopDetection = true,  // Prevent loops
    EnableMetrics = false,       // Disable metrics for performance
    ReasoningEngine = ReasoningEngine.None  // Disable reasoning for speed
};
```

## Dependencies

- **.NET 8.0**: Target framework
- **System.Text.Json**: JSON serialization
- **Microsoft.Extensions.Logging**: Logging infrastructure

## License

This package is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
