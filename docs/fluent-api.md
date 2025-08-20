# Fluent API Guide

The AIAgentSharp Fluent API provides an intuitive, chainable way to configure and create agents. This modern approach makes agent configuration more readable, discoverable, and less error-prone.

> **Note**: All examples in the AIAgentSharp project now use the fluent API by default. The traditional API is still available for backward compatibility.

## 🎯 Overview

The fluent API is built around the `AIAgentBuilder` class and provides a natural, English-like syntax for configuring agents. It follows modern .NET patterns similar to `IHostBuilder` and `IServiceCollection`.

## 🚀 Quick Start

```csharp
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;

// Create a simple agent
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(new CalculatorTool())
    .WithReasoning(ReasoningType.ChainOfThought)
    .WithStorage(new MemoryAgentStateStore())
    .Build();

// Run the agent
var result = await agent.RunAsync("my-agent", "Calculate 15 * 23", agent.Tools);
```

## 📋 API Reference

### Entry Points

#### `AIAgent.Create()`
Creates a new builder instance.

```csharp
// Start with empty builder
var builder = AIAgent.Create();

// Start with LLM client
var builder = AIAgent.Create(new OpenAiLlmClient(apiKey));
```

### Core Configuration Methods

#### `WithLlm(ILlmClient llmClient)`
Sets the LLM client for the agent.

```csharp
.WithLlm(new OpenAiLlmClient(apiKey))
.WithLlm(new AnthropicLlmClient(apiKey))
.WithLlm(new GeminiLlmClient(apiKey))
```

#### `WithTools()`
Configures tools for the agent. Multiple overloads available:

```csharp
// Single tool
.WithTool(new CalculatorTool())

// Multiple tools as parameters
.WithTools(new CalculatorTool(), new WeatherTool(), new DatabaseTool())

// Multiple tools as collection
.WithTools(toolsList)

// Fluent tool configuration
.WithTools(tools => tools
    .Add(new CalculatorTool())
    .Add(new WeatherTool())
    .Add(new DatabaseTool())
)
```

#### `WithReasoning()`
Configures the reasoning strategy for the agent.

```csharp
// Simple reasoning type
.WithReasoning(ReasoningType.ChainOfThought)
.WithReasoning(ReasoningType.TreeOfThoughts)
.WithReasoning(ReasoningType.None)

// Advanced reasoning with options
.WithReasoning(ReasoningType.TreeOfThoughts, options => options
    .SetExplorationStrategy(ExplorationStrategy.BestFirst)
    .SetMaxDepth(5)
)
```

#### `WithStorage(IAgentStateStore stateStore)`
Sets the state store for the agent.

```csharp
.WithStorage(new MemoryAgentStateStore())
.WithStorage(new FileAgentStateStore("agent-state.json"))
```

#### `WithMetrics(IMetricsCollector metricsCollector)`
Sets the metrics collector for the agent.

```csharp
.WithMetrics(new CustomMetricsCollector())
.WithMetrics(new MetricsCollector(logger))
```

#### `WithEventHandling(Action<EventHandlingBuilder> configureEvents)`
Configures event handlers for the agent using a fluent API.

```csharp
.WithEventHandling(events => events
    .OnRunStarted(e => Console.WriteLine($"Run started: {e.AgentId}"))
    .OnStepCompleted(e => Console.WriteLine($"Step completed: {e.TurnIndex + 1}"))
    .OnToolCallStarted(e => Console.WriteLine($"Tool called: {e.ToolName}"))
    .OnLlmCallStarted(e => Console.WriteLine($"LLM call started: {e.TurnIndex + 1}"))
    .OnRunCompleted(e => Console.WriteLine($"Run completed: {e.Succeeded}"))
)
```

Available event handlers:
- `OnRunStarted(Action<AgentRunStartedEventArgs>)` - When agent run starts
- `OnRunCompleted(Action<AgentRunCompletedEventArgs>)` - When agent run completes
- `OnStepStarted(Action<AgentStepStartedEventArgs>)` - When a step starts
- `OnStepCompleted(Action<AgentStepCompletedEventArgs>)` - When a step completes
- `OnToolCallStarted(Action<AgentToolCallStartedEventArgs>)` - When a tool call starts
- `OnToolCallCompleted(Action<AgentToolCallCompletedEventArgs>)` - When a tool call completes
- `OnLlmCallStarted(Action<AgentLlmCallStartedEventArgs>)` - When an LLM call starts
- `OnLlmCallCompleted(Action<AgentLlmCallCompletedEventArgs>)` - When an LLM call completes
- `OnLlmChunkReceived(Action<AgentLlmChunkReceivedEventArgs>)` - **NEW!** Real-time streaming of clean reasoning content
- `OnStatusUpdate(Action<AgentStatusEventArgs>)` - When status updates occur

### Advanced Configuration

#### Reasoning Options

The `ReasoningOptionsBuilder` provides fine-grained control over reasoning behavior:

```csharp
.WithReasoning(ReasoningType.TreeOfThoughts, options => options
    .SetExplorationStrategy(ExplorationStrategy.BestFirst)  // or DepthFirst, BreadthFirst, etc.
    .SetMaxDepth(5)  // Maximum tree depth
)
```

Available exploration strategies:
- `ExplorationStrategy.DepthFirst` - Explore deep paths first
- `ExplorationStrategy.BreadthFirst` - Explore wide paths first
- `ExplorationStrategy.BestFirst` - Explore most promising paths first
- `ExplorationStrategy.BeamSearch` - Beam search exploration
- `ExplorationStrategy.MonteCarlo` - Monte Carlo tree search

#### Tool Collection Builder

The `ToolCollectionBuilder` provides a fluent way to configure tool collections:

```csharp
.WithTools(tools => tools
    .Add(new CalculatorTool())
    .Add(new WeatherTool())
    .Add(new DatabaseTool())
    .Add(tool1, tool2, tool3)  // Multiple tools at once
)
```

## 🔧 Complete Examples

### Simple Agent

```csharp
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(new CalculatorTool())
    .WithStorage(new MemoryAgentStateStore())
    .Build();
```

### Advanced Agent with Reasoning

```csharp
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(tools => tools
        .Add(new SearchFlightsTool())
        .Add(new SearchHotelsTool())
        .Add(new SearchAttractionsTool())
        .Add(new CalculateTripCostTool())
    )
    .WithReasoning(ReasoningType.ChainOfThought, options => options
        .SetMaxDepth(8)
    )
    .WithStorage(new FileAgentStateStore("travel-agent-state.json"))
    .WithMetrics(new CustomMetricsCollector())
    .WithEventHandling(events => events
        .OnRunStarted(e => Console.WriteLine($"🚀 Run started: {e.AgentId}"))
        .OnStepCompleted(e => Console.WriteLine($"✅ Step {e.TurnIndex + 1} completed"))
        .OnToolCallStarted(e => Console.WriteLine($"🔧 Tool called: {e.ToolName}"))
        .OnLlmChunkReceived(e => Console.Write(e.Chunk.Content)) // Real-time reasoning display
        .OnRunCompleted(e => Console.WriteLine($"🏁 Run completed: {e.Succeeded}"))
    )
    .Build();
```

### Real-Time Reasoning Display

The `OnLlmChunkReceived` event provides real-time access to clean reasoning content:

```csharp
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithReasoning(ReasoningType.ChainOfThought)
    .WithEventHandling(events => events
        .OnLlmChunkReceived(e => 
        {
            // Automatically filtered content - no JSON schemas or technical formatting
            // Just the agent's clean thoughts and reasoning process
            Console.Write(e.Chunk.Content);
        })
        .OnLlmCallCompleted(e => Console.WriteLine()) // Add newline when complete
    )
    .Build();

// Output example:
// "To plan this complex business trip, I need to consider the budget constraints, 
// dietary restrictions, and different arrival times. Let me start by searching 
// for flights that accommodate the team's scheduling needs..."
```

### Tree of Thoughts Agent

```csharp
var agent = AIAgent.Create(new AnthropicLlmClient(apiKey))
    .WithTools(new MarketingAnalysisTool(), new CompetitorResearchTool())
    .WithReasoning(ReasoningType.TreeOfThoughts, options => options
        .SetExplorationStrategy(ExplorationStrategy.BestFirst)
        .SetMaxDepth(5)
    )
    .WithStorage(new MemoryAgentStateStore())
    .Build();
```

### Minimal Agent

```csharp
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .Build();
```

## 🎨 Best Practices

### 1. Use Descriptive Variable Names

```csharp
// Good
var travelPlanningAgent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(travelTools)
    .WithReasoning(ReasoningType.ChainOfThought)
    .Build();

// Avoid
var agent = AIAgent.Create(llm).WithTools(tools).Build();
```

### 2. Group Related Configuration

```csharp
// Good - logical grouping
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(travelTools)
    .WithReasoning(ReasoningType.ChainOfThought, options => options
        .SetMaxDepth(8)
    )
    .WithStorage(new FileAgentStateStore("travel-agent.json"))
    .WithMetrics(new TravelMetricsCollector())
    .Build();
```

### 3. Use Tool Collections for Multiple Tools

```csharp
// Good - clear and extensible
.WithTools(tools => tools
    .Add(new SearchFlightsTool())
    .Add(new SearchHotelsTool())
    .Add(new SearchAttractionsTool())
)

// Less ideal for many tools
.WithTools(new SearchFlightsTool(), new SearchHotelsTool(), new SearchAttractionsTool())
```

### 4. Leverage Type Safety

```csharp
// The fluent API provides compile-time checking
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithReasoning(ReasoningType.TreeOfThoughts, options => options
        .SetExplorationStrategy(ExplorationStrategy.BestFirst)  // Type-safe enum
        .SetMaxDepth(5)  // Compile-time validation
    )
    .Build();
```

## 🔄 Migration from Traditional API

### Before (Traditional API)

```csharp
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 8,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.7,
    MaxTurns = 20,
    UseFunctionCalling = true,
    EmitPublicStatus = true
};

var agent = new Agent(llm, store, config: config);
```

### After (Fluent API)

```csharp
var agent = AIAgent.Create(llm)
    .WithStorage(store)
    .WithReasoning(ReasoningType.ChainOfThought, options => options
        .SetMaxDepth(8)
    )
    .Build();
```

## 🎯 Benefits

### 1. **Readability**
The fluent API reads like natural English, making code self-documenting.

### 2. **Discoverability**
IntelliSense shows all available options, making it easy to discover features.

### 3. **Type Safety**
Compile-time checking prevents configuration errors.

### 4. **Extensibility**
Easy to add new configuration options without breaking existing code.

### 5. **Consistency**
Follows established .NET patterns like `IHostBuilder` and `IServiceCollection`.

### 6. **Maintainability**
Clear separation of concerns and logical grouping of related configuration.

## 🔧 Advanced Usage

### Custom Tool Collections

```csharp
var travelTools = new List<ITool>
{
    new SearchFlightsTool(),
    new SearchHotelsTool(),
    new SearchAttractionsTool()
};

var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(travelTools)
    .Build();
```

### Conditional Configuration

```csharp
var builder = AIAgent.Create(new OpenAiLlmClient(apiKey));

if (useAdvancedReasoning)
{
    builder = builder.WithReasoning(ReasoningType.TreeOfThoughts, options => options
        .SetExplorationStrategy(ExplorationStrategy.BestFirst)
        .SetMaxDepth(5)
    );
}
else
{
    builder = builder.WithReasoning(ReasoningType.ChainOfThought);
}

var agent = builder.Build();
```

### Factory Pattern

```csharp
public static class AgentFactory
{
    public static AIAgentBuilder CreateTravelAgent(ILlmClient llm)
    {
        return AIAgent.Create(llm)
            .WithTools(tools => tools
                .Add(new SearchFlightsTool())
                .Add(new SearchHotelsTool())
                .Add(new SearchAttractionsTool())
            )
            .WithReasoning(ReasoningType.ChainOfThought)
            .WithStorage(new FileAgentStateStore("travel-agent.json"));
    }
}

// Usage
var agent = AgentFactory.CreateTravelAgent(new OpenAiLlmClient(apiKey))
    .WithMetrics(new CustomMetricsCollector())
    .Build();
```

## 🚀 Next Steps

- Explore the [Tool Framework](tool-framework.md) to create custom tools
- Learn about [Reasoning Engines](reasoning-engines.md) for advanced problem solving
- Check out [Event System](event-system.md) for monitoring and debugging
- Review [Best Practices](best-practices.md) for production deployments
