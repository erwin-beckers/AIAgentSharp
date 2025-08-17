# Agent Framework

A comprehensive, production-ready framework for building LLM-powered agents with tool calling capabilities in C#. This framework provides a complete solution for creating intelligent agents that can reason, act, and observe using the Re/Act pattern or function calling.

## 🚀 Features

- **🔄 Re/Act Pattern Support**: Full implementation of the Re/Act (Reasoning and Acting) pattern for LLM agents
- **🔧 Function Calling**: Support for OpenAI-style function calling when available
- **🛠️ Tool Framework**: Rich tool system with automatic schema generation, validation, and introspection
- **💾 State Persistence**: Multiple state store implementations (in-memory, file-based)
- **📊 Real-time Monitoring**: Comprehensive event system for monitoring agent activity
- **📱 Public Status Updates**: Real-time status updates for UI consumption without exposing internal reasoning
- **🔄 Loop Detection**: Intelligent loop breaker to prevent infinite loops
- **🔄 Deduplication**: Smart caching of tool results to improve performance
- **📝 History Management**: Configurable history summarization to manage prompt size
- **⚡ Performance Optimized**: Efficient token management and prompt optimization
- **🔒 Thread Safe**: Thread-safe implementations for production use
- **🧪 Test Coverage**: Comprehensive test suite with 255+ tests

## 🏗️ Architecture

The framework is built around several key components:

- **`StatefulAgent`**: The main agent implementation that orchestrates reasoning and tool execution
- **`ITool` Interface**: Extensible tool system with automatic schema generation
- **`BaseTool<TParams, TResult>`**: Base class for strongly-typed tools with validation
- **`IAgentStateStore`**: Pluggable state persistence layer
- **`ILlmClient`**: Abstract LLM client interface for different providers
- **Event System**: Comprehensive event system for monitoring and integration

## 📦 Installation

### Prerequisites

- .NET 8.0 or later
- OpenAI API key (or other LLM provider)

### Setup

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd Agent
   ```

2. **Set your OpenAI API key**:
   ```bash
   # Windows
   set OPENAI_API_KEY=your-api-key-here
   
   # Linux/macOS
   export OPENAI_API_KEY=your-api-key-here
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

4. **Run the example**:
   ```bash
   dotnet run --project Agent
   ```

## 🚀 Quick Start

### Basic Usage

```csharp
using Agent.Shared;

// Create components
var llm = new OpenAiLlmClient(apiKey, "gpt-5-nano");
var store = new MemoryAgentStateStore(); // or FileAgentStateStore("./agent_state")
var tools = new List<ITool> { new ConcatTool(), new GetIndicatorTool() };

// Configure agent
var config = new AgentConfiguration
{
    MaxTurns = 40,
    UseFunctionCalling = true,
    EmitPublicStatus = true
};

// Create agent
var agent = new StatefulAgent(llm, store, config: config);

// Subscribe to events (optional)
agent.StatusUpdate += (sender, e) => 
    Console.WriteLine($"Status: {e.StatusTitle} - {e.StatusDetails}");

// Run the agent
var result = await agent.RunAsync("my-agent", "Your goal here", tools);

Console.WriteLine($"Success: {result.Succeeded}");
Console.WriteLine($"Output: {result.FinalOutput}");
```

### Creating Custom Tools

#### Using BaseTool (Recommended)

```csharp
[ToolParams(Description = "Parameters for weather lookup")]
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

    public override async Task<object> InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        // Your weather API logic here
        var weather = await FetchWeatherAsync(parameters.City, parameters.Unit, ct);
        
        return new { 
            city = parameters.City,
            temperature = weather.Temperature,
            unit = parameters.Unit,
            description = weather.Description
        };
    }
}
```

#### Manual Implementation

```csharp
public sealed class CustomTool : ITool, IToolIntrospect, IFunctionSchemaProvider
{
    public string Name => "custom_tool";
    public string Description => "A custom tool description";

    public async Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
    {
        // Your tool logic here
        return new { result = "success" };
    }

    public string Describe() => JsonSerializer.Serialize(new { 
        name = "custom_tool", 
        description = "A custom tool description" 
    });

    public object GetJsonSchema() => new { 
        type = "object", 
        properties = new { }, 
        required = new string[] { } 
    };
}
```

### Event Monitoring

```csharp
// Subscribe to various events
agent.RunStarted += (sender, e) => 
    Console.WriteLine($"Agent {e.AgentId} started with goal: {e.Goal}");

agent.StepStarted += (sender, e) => 
    Console.WriteLine($"Step {e.TurnIndex + 1} started");

agent.ToolCallStarted += (sender, e) => 
    Console.WriteLine($"Tool {e.ToolName} called with params: {JsonSerializer.Serialize(e.Parameters)}");

agent.ToolCallCompleted += (sender, e) => 
    Console.WriteLine($"Tool {e.ToolName} completed in {e.ExecutionTime.TotalMilliseconds}ms");

agent.StatusUpdate += (sender, e) => 
    Console.WriteLine($"Status: {e.StatusTitle} - Progress: {e.ProgressPct}%");

agent.RunCompleted += (sender, e) => 
    Console.WriteLine($"Agent completed with {e.TotalTurns} turns");
```

## ⚙️ Configuration

The `AgentConfiguration` class provides extensive configuration options:

```csharp
var config = new AgentConfiguration
{
    // Turn limits
    MaxTurns = 100,
    
    // Timeouts
    LlmTimeout = TimeSpan.FromMinutes(5),
    ToolTimeout = TimeSpan.FromMinutes(2),
    
    // History management
    MaxRecentTurns = 10,
    EnableHistorySummarization = true,
    MaxToolOutputSize = 2000,
    
    // Loop detection
    ConsecutiveFailureThreshold = 3,
    MaxToolCallHistory = 20,
    
    // Deduplication
    DedupeStalenessThreshold = TimeSpan.FromMinutes(5),
    
    // Features
    UseFunctionCalling = true,
    EmitPublicStatus = true,
    
    // Field size limits
    MaxThoughtsLength = 20000,
    MaxFinalLength = 50000,
    MaxSummaryLength = 40000
};
```

## 🏪 State Stores

### Memory State Store
```csharp
var store = new MemoryAgentStateStore(); // In-memory, not persistent
```

### File State Store
```csharp
var store = new FileAgentStateStore("./agent_state"); // Persistent to files
```

### Custom State Store
```csharp
public class CustomStateStore : IAgentStateStore
{
    public async Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default)
    {
        // Your loading logic
    }

    public async Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default)
    {
        // Your saving logic
    }
}
```

## 🤖 LLM Integration

### OpenAI Integration
```csharp
var llm = new OpenAiLlmClient(apiKey, "gpt-5-nano");
```

### Custom LLM Provider
```csharp
public class CustomLlmClient : ILlmClient
{
    public async Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        // Your LLM integration logic
        return "LLM response";
    }
}
```

## 🔧 Advanced Features

### Tool Deduplication Control

```csharp
public class NonIdempotentTool : BaseTool<MyParams, object>, IDedupeControl
{
    public bool AllowDedupe => false; // Opt out of deduplication
    public TimeSpan? CustomTtl => TimeSpan.FromSeconds(30); // Custom TTL
}
```

### Public Status Updates

The framework supports real-time status updates that can be used in UIs:

```csharp
agent.StatusUpdate += (sender, e) =>
{
    // Update UI with:
    // e.StatusTitle - Brief status (3-10 words)
    // e.StatusDetails - Additional context (≤160 chars)
    // e.NextStepHint - What's next (3-12 words)
    // e.ProgressPct - Completion percentage (0-100)
};
```

### History Summarization

The framework automatically manages conversation history to prevent prompt bloat:

- Recent turns are kept in full detail
- Older turns are summarized
- Large tool outputs are truncated
- Configurable limits for all fields

## 🧪 Testing

The framework includes comprehensive tests:

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

## 📚 API Reference

### Core Interfaces

- **`IAgent`**: Main agent interface
- **`ITool`**: Tool interface
- **`ILlmClient`**: LLM client interface
- **`IAgentStateStore`**: State persistence interface

### Key Classes

- **`StatefulAgent`**: Main agent implementation
- **`BaseTool<TParams, TResult>`**: Base class for typed tools
- **`AgentConfiguration`**: Configuration options
- **`AgentState`**: Agent state model
- **`ToolExecutionResult`**: Tool execution results

### Events

- **`RunStarted`**: Agent run begins
- **`StepStarted`**: Individual step begins
- **`ToolCallStarted`**: Tool execution begins
- **`ToolCallCompleted`**: Tool execution completes
- **`StatusUpdate`**: Public status updates
- **`RunCompleted`**: Agent run completes

## 🔍 Examples

### Trading Agent Example

```csharp
var tools = new List<ITool> 
{ 
    new GetIndicatorTool(),
    new CalculateRiskTool(),
    new PlaceOrderTool()
};

var goal = "Analyze MNQ using RSI and ATR, then place a conservative trade if conditions are favorable.";

var result = await agent.RunAsync("trading-agent", goal, tools);
```

### Data Processing Agent

```csharp
var tools = new List<ITool> 
{ 
    new FileReaderTool(),
    new DataProcessorTool(),
    new ReportGeneratorTool()
};

var goal = "Read the sales data file, calculate monthly totals, and generate a summary report.";

var result = await agent.RunAsync("data-agent", goal, tools);
```

## 🚨 Error Handling

The framework provides comprehensive error handling:

```csharp
try
{
    var result = await agent.RunAsync("agent-id", goal, tools);
    if (!result.Succeeded)
    {
        Console.WriteLine($"Agent failed: {result.Error}");
    }
}
catch (ToolValidationException ex)
{
    Console.WriteLine($"Tool validation failed: {ex.Message}");
    Console.WriteLine($"Missing fields: {string.Join(", ", ex.Missing)}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

## 🔧 Performance Considerations

- **Token Management**: The framework automatically manages prompt size through history summarization
- **Tool Caching**: Results are cached and reused when appropriate
- **Loop Prevention**: Intelligent loop detection prevents infinite loops
- **Async Operations**: All operations are async for better resource utilization

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

- **Issues**: Report bugs and request features on GitHub
- **Documentation**: Check the XML documentation in the code
- **Examples**: See the `Program.cs` file for usage examples
- **Tests**: Browse the test files for implementation examples

## 🔄 Version History

- **v1.0.0**: Initial release with core agent functionality
- **v1.1.0**: Added public status updates and improved configuration
- **v1.2.0**: Enhanced tool framework with better validation
- **v1.3.0**: Added history summarization and performance optimizations

---

**Built with ❤️ for the .NET community**
