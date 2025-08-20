# Fluent API Quick Reference

A quick reference guide for the AIAgentSharp Fluent API.

## 🚀 Basic Usage

```csharp
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;

var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(new CalculatorTool())
    .WithReasoning(ReasoningType.ChainOfThought)
    .WithStorage(new MemoryAgentStateStore())
    .Build();
```

## 📋 Method Reference

### Entry Points
```csharp
AIAgent.Create()                    // Empty builder
AIAgent.Create(llmClient)          // Builder with LLM client
```

### Core Methods
```csharp
.WithLlm(llmClient)                // Set LLM client
.WithTool(tool)                    // Add single tool
.WithTools(tool1, tool2, ...)      // Add multiple tools
.WithTools(toolsList)              // Add tool collection
.WithTools(tools => tools.Add())   // Fluent tool configuration
.WithReasoning(ReasoningType)      // Set reasoning type
.WithStorage(stateStore)           // Set state store
.WithMetrics(metricsCollector)     // Set metrics collector
.WithEventHandling(events => ...)  // Configure event handlers
.Build()                           // Create agent instance
```

### Reasoning Options
```csharp
.WithReasoning(ReasoningType.TreeOfThoughts, options => options
    .SetExplorationStrategy(ExplorationStrategy.BestFirst)
    .SetMaxDepth(5)
)
```

### Event Handling
```csharp
.WithEventHandling(events => events
    .OnRunStarted(e => Console.WriteLine($"Started: {e.AgentId}"))
    .OnStepCompleted(e => Console.WriteLine($"Step {e.TurnIndex + 1} done"))
    .OnToolCallStarted(e => Console.WriteLine($"Tool: {e.ToolName}"))
    .OnRunCompleted(e => Console.WriteLine($"Completed: {e.Succeeded}"))
)
```

### Tool Collection Builder
```csharp
.WithTools(tools => tools
    .Add(new CalculatorTool())
    .Add(new WeatherTool())
    .Add(tool1, tool2, tool3)
)
```

## 🎯 Common Patterns

### Simple Agent
```csharp
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithStorage(new MemoryAgentStateStore())
    .Build();
```

### Agent with Tools
```csharp
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(new CalculatorTool(), new WeatherTool())
    .WithStorage(new MemoryAgentStateStore())
    .Build();
```

### Agent with Reasoning
```csharp
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(tools)
    .WithReasoning(ReasoningType.ChainOfThought)
    .WithStorage(new MemoryAgentStateStore())
    .Build();
```

### Advanced Agent
```csharp
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(tools => tools
        .Add(new SearchFlightsTool())
        .Add(new SearchHotelsTool())
    )
    .WithReasoning(ReasoningType.TreeOfThoughts, options => options
        .SetExplorationStrategy(ExplorationStrategy.BestFirst)
        .SetMaxDepth(5)
    )
    .WithStorage(new FileAgentStateStore("agent-state.json"))
    .WithMetrics(new CustomMetricsCollector())
    .WithEventHandling(events => events
        .OnRunStarted(e => Console.WriteLine($"🚀 Started: {e.AgentId}"))
        .OnStepCompleted(e => Console.WriteLine($"✅ Step {e.TurnIndex + 1} done"))
        .OnToolCallStarted(e => Console.WriteLine($"🔧 Tool: {e.ToolName}"))
        .OnRunCompleted(e => Console.WriteLine($"🏁 Completed: {e.Succeeded}"))
    )
    .Build();
```

## 🔧 Available Options

### Reasoning Types
- `ReasoningType.None` - No reasoning
- `ReasoningType.ChainOfThought` - Step-by-step reasoning
- `ReasoningType.TreeOfThoughts` - Multi-path exploration
- `ReasoningType.Hybrid` - Combined approaches

### Exploration Strategies
- `ExplorationStrategy.DepthFirst` - Explore deep paths first
- `ExplorationStrategy.BreadthFirst` - Explore wide paths first
- `ExplorationStrategy.BestFirst` - Explore most promising paths first
- `ExplorationStrategy.BeamSearch` - Beam search exploration
- `ExplorationStrategy.MonteCarlo` - Monte Carlo tree search

### State Stores
- `MemoryAgentStateStore()` - In-memory storage
- `FileAgentStateStore("path.json")` - File-based storage

## 🎨 Best Practices

### 1. Use Descriptive Names
```csharp
var travelPlanningAgent = AIAgent.Create(llm)
    .WithTools(travelTools)
    .Build();
```

### 2. Group Related Configuration
```csharp
var agent = AIAgent.Create(llm)
    .WithTools(travelTools)
    .WithReasoning(ReasoningType.ChainOfThought, options => options
        .SetMaxDepth(8)
    )
    .WithStorage(new FileAgentStateStore("travel-agent.json"))
    .Build();
```

### 3. Use Tool Collections for Multiple Tools
```csharp
.WithTools(tools => tools
    .Add(new SearchFlightsTool())
    .Add(new SearchHotelsTool())
    .Add(new SearchAttractionsTool())
)
```

### 4. Leverage Type Safety
```csharp
.WithReasoning(ReasoningType.TreeOfThoughts, options => options
    .SetExplorationStrategy(ExplorationStrategy.BestFirst)  // Type-safe enum
    .SetMaxDepth(5)  // Compile-time validation
)
```

## 🔄 Migration from Traditional API

### Before
```csharp
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 8
};
var agent = new Agent(llm, store, config: config);
```

### After
```csharp
var agent = AIAgent.Create(llm)
    .WithStorage(store)
    .WithReasoning(ReasoningType.ChainOfThought, options => options
        .SetMaxDepth(8)
    )
    .Build();
```

## 📚 More Information

- **[Full Fluent API Guide](fluent-api.md)** - Complete documentation
- **[Examples](../examples/)** - Working examples
- **[API Reference](../api/)** - Detailed API documentation
