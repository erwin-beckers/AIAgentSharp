# AIAgentSharp
[![CI](https://github.com/erwin-beckers/AIAgentSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/erwin-beckers/AIAgentSharp/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/erwin-beckers/AIAgentSharp/graph/badge.svg?token=0IIY8VJPIV)](https://codecov.io/gh/erwin-beckers/AIAgentSharp)

A comprehensive, production-ready .NET 8.0 framework for building LLM-powered agents with advanced reasoning capabilities and tool calling. This framework provides a complete solution for creating intelligent agents that can reason, act, and observe using multiple reasoning strategies including Chain of Thought and Tree of Thoughts.

📖 **[📚 Full Documentation](DOCUMENTATION.md)** - Complete guide with examples, API reference, and advanced features

## 🚀 Key Features

- **🧠 Advanced Reasoning**: Chain of Thought (CoT) and Tree of Thoughts (ToT) reasoning engines
- **🔄 Re/Act Pattern**: Full implementation of the Reasoning and Acting pattern for LLM agents
- **🔧 Advanced Tool Calling**: Support for OpenAI-style function calling with multi-tool execution
- **🛠️ Rich Tool Framework**: Strongly-typed tools with automatic schema generation and validation
- **⚡ Multi-Tool Calling**: Execute multiple tools simultaneously in a single LLM response
- **💾 State Persistence**: Multiple state store implementations (in-memory, file-based)
- **📊 Real-time Monitoring**: Comprehensive event system and metrics collection
- **🔄 Loop Detection**: Intelligent loop breaker to prevent infinite loops
- **📝 History Management**: Configurable history summarization to manage prompt size
- **⚡ Performance Optimized**: Efficient token management and prompt optimization
- **🔒 Production Ready**: Thread-safe implementations with comprehensive error handling
- **🧪 Well Tested**: Comprehensive test suite with 600+ tests
- **🎯 Fluent API**: Intuitive, chainable configuration for easy agent setup

## 📦 Installation

### Available NuGet Packages

| Package | Version | Description |
|---------|---------|-------------|
| `AIAgentSharp` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp) | Core framework with abstract LLM interfaces, reasoning engines, and tool framework |
| `AIAgentSharp.OpenAI` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.OpenAI) | OpenAI integration package with `OpenAiLlmClient` implementation |
| `AIAgentSharp.Anthropic` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.Anthropic) | Anthropic Claude integration package with `AnthropicLlmClient` implementation |
| `AIAgentSharp.Gemini` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.Gemini) | Google Gemini integration package with `GeminiLlmClient` implementation |
| `AIAgentSharp.Mistral` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.Mistral) | Mistral AI integration package with `MistralLlmClient` implementation |

### Prerequisites

- .NET 8.0 or later
- LLM provider API key (OpenAI, Anthropic, etc.)

### Quick Start

1. **Install the packages**:
   ```bash
   dotnet add package AIAgentSharp
   dotnet add package AIAgentSharp.OpenAI
   ```

2. **Set your API key**:
   ```bash
   # Windows
   set LLM_API_KEY=your-api-key-here
   
   # Linux/macOS
   export LLM_API_KEY=your-api-key-here
   ```

3. **Create a basic agent with the fluent API**:
   ```csharp
   using AIAgentSharp.Fluent;
   using AIAgentSharp.OpenAI;

   var agent = AIAgent.Create(new OpenAiLlmClient(Environment.GetEnvironmentVariable("LLM_API_KEY")!))
       .WithStorage(new MemoryAgentStateStore())
       .Build();
   
   var result = await agent.RunAsync("my-agent", "Hello, how are you?", new List<ITool>());
   Console.WriteLine(result.FinalOutput);
   ```

## 🚀 Usage

### Fluent API (Recommended)

The fluent API provides an intuitive, chainable way to configure agents:

```csharp
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;

// Simple agent
var simpleAgent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(new CalculatorTool(), new WeatherTool())
    .WithReasoning(ReasoningType.ChainOfThought)
    .WithStorage(new MemoryAgentStateStore())
    .Build();

// Advanced agent with detailed configuration
var advancedAgent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(tools => tools
        .Add(new CalculatorTool())
        .Add(new WeatherTool())
        .Add(new DatabaseTool())
    )
    .WithReasoning(ReasoningType.TreeOfThoughts, options => options
        .SetExplorationStrategy(ExplorationStrategy.BestFirst)
        .SetMaxDepth(5)
    )
    .WithStorage(new FileAgentStateStore("agent-state.json"))
    .WithMetrics(new CustomMetricsCollector())
    .Build();

// Create an agent with fluent API
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(new CalculatorTool(), new WeatherTool())
    .WithReasoning(ReasoningType.ChainOfThought)
    .WithStorage(new MemoryAgentStateStore())
    .WithEventHandling(events => events
        .OnRunStarted(e => Console.WriteLine($"🚀 Started: {e.AgentId}"))
        .OnStepCompleted(e => Console.WriteLine($"✅ Step {e.TurnIndex + 1} done"))
        .OnToolCallStarted(e => Console.WriteLine($"🔧 Tool: {e.ToolName}"))
        .OnRunCompleted(e => Console.WriteLine($"🏁 Completed: {e.Succeeded}"))
    )
    .Build();

// Run the agent
var result = await agent.RunAsync("my-agent", "Calculate 15 * 23", agent.Tools);
Console.WriteLine($"Success: {result.Succeeded}");
Console.WriteLine($"Output: {result.FinalOutput}");
```

### Traditional API

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.Examples;
using AIAgentSharp.OpenAI;

// Create components
var llm = new OpenAiLlmClient(apiKey);
var store = new MemoryAgentStateStore();
var tools = new List<ITool>(); // add your tools here

// Create agent
var agent = new Agent(llm, store);

// Subscribe to events (optional)
agent.StatusUpdate += (sender, e) => 
    Console.WriteLine($"Status: {e.StatusTitle} - {e.StatusDetails}");

// Run the agent
var result = await agent.RunAsync("travel-agent", "Plan a trip to Paris", tools);

Console.WriteLine($"Success: {result.Succeeded}");
Console.WriteLine($"Output: {result.FinalOutput}");
```

### With Reasoning

```csharp
// Configure for Chain of Thought reasoning
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true,
    UseFunctionCalling = true,
    EmitPublicStatus = true
};

var agent = new Agent(llm, store, config: config);
var result = await agent.RunAsync("reasoning-agent", "Solve a complex problem", tools);

// Access reasoning chain
if (result.State?.CurrentReasoningChain != null)
{
    var chain = result.State.CurrentReasoningChain;
    Console.WriteLine($"Reasoning Steps: {chain.Steps.Count}");
    Console.WriteLine($"Final Confidence: {chain.FinalConfidence:F2}");
}
```

### Creating Tools

```csharp
using System.ComponentModel.DataAnnotations;

[ToolParams(Description = "Weather lookup parameters")]
public sealed class WeatherParams
{
    [ToolField(Description = "City name", Example = "New York", Required = true)]
    [Required]
    public string City { get; set; } = default!;
    
    [ToolField(Description = "Temperature unit", Example = "Celsius")]
    public string Unit { get; set; } = "Celsius";
}

public sealed class WeatherTool : BaseTool<WeatherParams, object>
{
    public override string Name => "get_weather";
    public override string Description => "Get current weather for a city";

    protected override async Task<object> InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
    {
        // Your weather API logic here
        return new { 
            city = parameters.City,
            temperature = "22°C",
            unit = parameters.Unit,
            description = "Sunny"
        };
    }
}
```

### Multi-Tool Calling

AIAgentSharp supports advanced multi-tool calling, allowing the LLM to execute multiple tools in a single response for more efficient task completion:

```csharp
// The agent can automatically call multiple tools in one turn
var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(
        new SearchFlightsTool(),
        new SearchHotelsTool(),
        new SearchAttractionsTool(),
        new CalculateTripCostTool()
    )
    .WithReasoning(ReasoningType.ChainOfThought)
    .Build();

// Example: "Plan a trip to Tokyo" - the agent might call:
// 1. SearchFlightsTool
// 2. SearchHotelsTool  
// 3. SearchAttractionsTool
// All in a single LLM response!

var result = await agent.RunAsync("travel-agent", 
    "Plan a 5-day business trip to Tokyo for 3 people with a $8000 budget", 
    agent.Tools);

// Multi-tool execution is automatically handled and reported
Console.WriteLine($"Total tools executed: {result.State?.CurrentTurn?.ToolExecutionResults?.Count ?? 0}");
```

### Different LLM Providers

```csharp
// OpenAI
using AIAgentSharp.OpenAI;
var llm = new OpenAiLlmClient(apiKey);

// Anthropic Claude
using AIAgentSharp.Anthropic;
var llm = new AnthropicLlmClient(apiKey);

// Google Gemini
using AIAgentSharp.Gemini;
var llm = new GeminiLlmClient(apiKey);

// Mistral AI
using AIAgentSharp.Mistral;
var llm = new MistralLlmClient(apiKey);

// Custom provider
public class MyCustomLlmClient : ILlmClient
{
    // Implementation details...
}
```

### Event Monitoring

```csharp
// Subscribe to events for monitoring
agent.RunStarted += (sender, e) => 
    Console.WriteLine($"Agent {e.AgentId} started with goal: {e.Goal}");

agent.ToolCallStarted += (sender, e) => 
    Console.WriteLine($"Tool {e.ToolName} called");

agent.StatusUpdate += (sender, e) => 
    Console.WriteLine($"Status: {e.StatusTitle} - Progress: {e.ProgressPct}%");

agent.RunCompleted += (sender, e) => 
    Console.WriteLine($"Agent completed with {e.TotalTurns} turns");

// Real-time streaming of reasoning content (Chain of Thought, Tree of Thoughts)
agent.LlmChunkReceived += (sender, e) => 
{
    // Automatically filtered to show only clean reasoning content
    // No JSON schemas or technical formatting - just the agent's thoughts
    Console.Write(e.Chunk.Content);
};
```

### Metrics

```csharp
// Access metrics
var metrics = agent.Metrics.GetMetrics();
Console.WriteLine($"Total Agent Runs: {metrics.Performance.TotalAgentRuns}");
Console.WriteLine($"Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}");
Console.WriteLine($"Total Tokens Used: {metrics.Resources.TotalTokens:N0}");

// Subscribe to real-time metrics updates
agent.Metrics.MetricsUpdated += (sender, e) =>
    Console.WriteLine($"Latest success rate: {e.Metrics.Operational.AgentRunSuccessRate:P2}");
```

## 📚 Documentation

For comprehensive documentation, API reference, architecture details, and advanced features:

- **[Complete Documentation](DOCUMENTATION.md)** - Detailed documentation covering all components, architecture, and usage examples
- **[API Reference](DOCUMENTATION.md#api-reference)** - Complete API reference with examples  
- **[Advanced Features](DOCUMENTATION.md#advanced-features)** - Reasoning engines, tool framework, monitoring, and more
- **[Architecture](DOCUMENTATION.md#architecture)** - Detailed architecture and component documentation

## 🧪 Testing

Run the comprehensive test suite:

```bash
dotnet test
```

The framework includes 600+ tests covering all components and features.

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](DOCUMENTATION.md#contributing) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**AIAgentSharp** - Empowering .NET developers to build intelligent, reasoning-enabled AI agents with ease.