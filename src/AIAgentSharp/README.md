# AIAgentSharp

A comprehensive, production-ready .NET 8.0 framework for building LLM-powered agents with tool calling capabilities. This framework provides a complete solution for creating intelligent agents that can reason, act, and observe using the Re/Act pattern or function calling.

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
- **🧪 Test Coverage**: Comprehensive test suite with 280+ tests

### 🔧 Advanced Tool Features

- **Strongly-Typed Tools**: Full type safety with `BaseTool<TParams, TResult>` and automatic validation
- **Tool Deduplication Control**: Tools can opt out of deduplication or set custom TTL via `IDedupeControl`
- **Automatic Schema Generation**: Complete JSON schema generation from C# types with validation attributes
- **Tool Field Attributes**: Rich metadata support with `ToolFieldAttribute` and `ToolParamsAttribute`
- **Tool Introspection**: Tools can provide detailed descriptions for LLM consumption
- **Parameter Validation**: Comprehensive validation with DataAnnotations and custom error messages

### 🎯 Advanced Execution Features

- **Step-by-Step Execution**: Execute individual agent steps with `StepAsync()` for fine-grained control
- **Loop Detection & Prevention**: Intelligent loop breaker with configurable failure thresholds
- **History Summarization**: Automatic summarization of older turns to manage token usage efficiently
- **Tool Result Caching**: Smart caching with configurable staleness thresholds
- **Consecutive Failure Detection**: Prevents infinite loops from repeated tool failures
- **Cancellation Support**: Full cancellation token support throughout the execution pipeline

### 📊 Advanced Monitoring & Events

- **Comprehensive Event System**: 9 different event types for complete execution monitoring
- **Public Status Updates**: UI-friendly status updates without exposing internal reasoning
- **Progress Tracking**: Real-time progress percentage and detailed status information
- **Execution Time Tracking**: Detailed timing for LLM calls and tool executions
- **Turn-level Monitoring**: Complete visibility into each agent turn and decision
- **Error Recovery**: Graceful handling of failures with detailed error information

### ⚙️ Advanced Configuration

- **Granular Field Size Limits**: Separate configurable limits for thoughts, final output, and summaries
- **Configurable Timeouts**: Separate timeouts for LLM and tool calls
- **Token Management**: Automatic prompt optimization and size management
- **Function Calling Toggle**: Can switch between Re/Act and function calling modes
- **Immutable Configuration**: Type-safe configuration with init-only properties

### 🔒 Production-Ready Features

- **Thread Safety**: All components are thread-safe for production use
- **Structured Logging**: Comprehensive logging with multiple log levels
- **Error Handling**: Robust error handling with detailed exception information
- **State Persistence**: Multiple state store implementations (memory, file-based)
- **Performance Optimization**: Advanced caching and token management strategies

## 📦 Installation

### NuGet Package

The easiest way to get started is to install the NuGet package:

```bash
dotnet add package AIAgentSharp
```

### Prerequisites

- .NET 8.0 or later
- OpenAI API key (or other LLM provider)

### Manual Installation

If you prefer to build from source:

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd AIAgentSharp
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
   dotnet run --project examples
   ```

## 🏗️ Architecture

The framework is built around several key components:

- **`Agent`**: The main agent implementation that orchestrates reasoning and tool execution
- **`ITool` Interface**: Extensible tool system with automatic schema generation
- **`BaseTool<TParams, TResult>`**: Base class for strongly-typed tools with validation
- **`IAgentStateStore`**: Pluggable state persistence layer
- **`ILlmClient`**: Abstract LLM client interface for different providers
- **Event System**: Comprehensive event system for monitoring and integration

## 🚀 Quick Start

### Basic Usage

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.Examples;

// Create components
var llm = new OpenAiLlmClient(apiKey);
var store = new MemoryAgentStateStore(); // or FileAgentStateStore("./agent_state")
var tools = new List<ITool> 
{ 
    new SearchFlightsTool(),
    new SearchHotelsTool(),
    new SearchAttractionsTool(),
    new CalculateTripCostTool()
};

// Configure agent
var config = new AgentConfiguration
{
    MaxTurns = 40,
    UseFunctionCalling = true,
    EmitPublicStatus = true
};

// Create agent
var agent = new Agent(llm, store, config: config);

// Subscribe to events (optional)
agent.StatusUpdate += (sender, e) => 
    Console.WriteLine($"Status: {e.StatusTitle} - {e.StatusDetails}");

// Run the agent
var result = await agent.RunAsync("travel-agent", "Plan a 3-day trip to Paris for a couple with a budget of $2000", tools);

Console.WriteLine($"Success: {result.Succeeded}");
Console.WriteLine($"Output: {result.FinalOutput}");
```

### Creating Custom Tools

```csharp
using AIAgentSharp;
using System.ComponentModel.DataAnnotations;

[ToolParams(Description = "Parameters for weather lookup")]
public sealed class WeatherParams
{
    [ToolField(Description = "City name", Example = "New York", Required = true)]
    [Required]
    [MinLength(1)]
    public string City { get; set; } = default!;
    
    [ToolField(Description = "Temperature unit", Example = "Celsius")]
    public string Unit { get; set; } = "Celsius";
}

public sealed class WeatherTool : BaseTool<WeatherParams, object>
{
    public override string Name => "get_weather";
    public override string Description => "Get current weather information for a city";

    protected override async Task<object> InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
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

### Step-by-Step Execution

```csharp
// Execute individual steps
var stepResult = await agent.StepAsync("agent-id", "goal", tools);

if (stepResult.Continue)
{
    Console.WriteLine("Agent continues execution");
    Console.WriteLine($"Thoughts: {stepResult.LlmMessage?.Thoughts}");
}
else
{
    Console.WriteLine("Agent completed");
    Console.WriteLine($"Final output: {stepResult.FinalOutput}");
}
```

### Advanced Tool Features

```csharp
// Tool with custom deduplication control
public class NonIdempotentTool : BaseTool<MyParams, object>, IDedupeControl
{
    public bool AllowDedupe => false; // Opt out of deduplication
    public TimeSpan? CustomTtl => TimeSpan.FromSeconds(30); // Custom TTL
    
    // ... rest of implementation
}

// Tool with rich metadata
[ToolParams(Description = "Advanced search parameters")]
public sealed class SearchParams
{
    [ToolField(Description = "Search query", Example = "machine learning", Required = true)]
    [Required]
    [MinLength(1)]
    public string Query { get; set; } = default!;
    
    [ToolField(Description = "Number of results", Example = "10")]
    [Range(1, 100)]
    public int MaxResults { get; set; } = 10;
    
    [ToolField(Description = "Search filters", Example = "recent")]
    public string? Filters { get; set; }
}
```

### Event Monitoring

```csharp
// Subscribe to events for monitoring
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

## 📚 Documentation

For comprehensive documentation, see:

- **[Complete API Documentation](../DOCUMENTATION.md)** - Detailed documentation covering all components, architecture, and usage examples
- **[API Reference](../DOCUMENTATION.md#api-reference)** - Complete API reference with examples
- **[Advanced Features](../DOCUMENTATION.md#advanced-features)** - Loop detection, history summarization, and more
- **[Performance Considerations](../DOCUMENTATION.md#performance-considerations)** - Optimization tips and best practices

## 🔧 Configuration

The framework provides extensive configuration options:

```csharp
var config = new AgentConfiguration
{
    // Turn limits
    MaxTurns = 50,
    MaxRecentTurns = 15,
    
    // Timeouts
    LlmTimeout = TimeSpan.FromMinutes(2),
    ToolTimeout = TimeSpan.FromMinutes(1),
    
    // History management
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

## 🧪 Testing

Run the comprehensive test suite:

```bash
dotnet test
```

The framework includes 280+ tests covering:
- Agent execution and state management
- Tool framework and validation
- LLM integration and function calling
- Event system and status updates
- Configuration and error handling

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](../DOCUMENTATION.md#contributing) for details.

### Development Setup

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd AIAgentSharp
   ```

2. **Install dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

4. **Run tests**:
   ```bash
   dotnet test
   ```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## 🙏 Acknowledgments

- Built with ❤️ for the .NET community
- Inspired by the Re/Act pattern and OpenAI's function calling
- Designed for production use with comprehensive error handling and monitoring

---

**AIAgentSharp** - Empowering .NET developers to build intelligent, tool-using AI agents with ease.
