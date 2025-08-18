# AIAgentSharp

[![codecov](https://codecov.io/gh/erwin-beckers/AIAgentSharp/graph/badge.svg?token=0IIY8VJPIV)](https://codecov.io/gh/erwin-beckers/AIAgentSharp)

A comprehensive, production-ready .NET 8.0 framework for building LLM-powered agents with advanced reasoning capabilities and tool calling. This framework provides a complete solution for creating intelligent agents that can reason, act, and observe using multiple reasoning strategies including Chain of Thought, Tree of Thoughts, and hybrid approaches.

## 🚀 Features

- **🧠 Advanced Reasoning Engines**: Multiple reasoning strategies including Chain of Thought (CoT), Tree of Thoughts (ToT), and hybrid approaches
- **🔄 Re/Act Pattern Support**: Full implementation of the Re/Act (Reasoning and Acting) pattern for LLM agents
- **🔧 Function Calling**: Support for OpenAI-style function calling when available
- **🛠️ Enhanced Tool Framework**: Rich tool system with automatic schema generation, validation, introspection, and structured error handling
- **💾 State Persistence**: Multiple state store implementations (in-memory, file-based)
- **📊 Real-time Monitoring**: Comprehensive event system for monitoring agent activity
- **📱 Public Status Updates**: Real-time status updates for UI consumption without exposing internal reasoning
- **🔄 Loop Detection**: Intelligent loop breaker to prevent infinite loops
- **🔄 Deduplication**: Smart caching of tool results to improve performance
- **📝 History Management**: Configurable history summarization to manage prompt size
- **⚡ Performance Optimized**: Efficient token management and prompt optimization
- **🔒 Thread Safe**: Thread-safe implementations for production use
- **🧪 Test Coverage**: Comprehensive test suite with 400+ tests

### 🧠 Advanced Reasoning Features

- **Chain of Thought (CoT)**: Sequential step-by-step reasoning for complex problem decomposition
- **Tree of Thoughts (ToT)**: Multi-branch exploration of solution paths with configurable exploration strategies
- **Hybrid Reasoning**: Combination of multiple reasoning approaches for optimal results
- **Reasoning Validation**: Built-in validation of reasoning quality and confidence scoring
- **Exploration Strategies**: Best-first, breadth-first, depth-first, beam search, and Monte Carlo exploration
- **Reasoning Metadata**: Comprehensive tracking of reasoning activities and insights

### 🔧 Enhanced Tool Features

- **Strongly-Typed Tools**: Full type safety with `BaseTool<TParams, TResult>` and automatic validation
- **Structured Error Handling**: `ToolValidationException` with detailed field-level error reporting
- **Tool Deduplication Control**: Tools can opt out of deduplication or set custom TTL via `IDedupeControl`
- **Automatic Schema Generation**: Complete JSON schema generation from C# types with validation attributes
- **Tool Field Attributes**: Rich metadata support with `ToolFieldAttribute` and `ToolParamsAttribute`
- **Tool Introspection**: Tools can provide detailed descriptions for LLM consumption
- **Parameter Validation**: Comprehensive validation with DataAnnotations and custom error messages
- **Validation Error Bubbling**: Structured error propagation from tools to agent decision-making

### 🎯 Advanced Execution Features

- **Step-by-Step Execution**: Execute individual agent steps with `StepAsync()` for fine-grained control
- **Loop Detection & Prevention**: Intelligent loop breaker with configurable failure thresholds
- **History Summarization**: Automatic summarization of older turns to manage token usage efficiently
- **Tool Result Caching**: Smart caching with configurable staleness thresholds
- **Consecutive Failure Detection**: Prevents infinite loops from repeated tool failures
- **Cancellation Support**: Full cancellation token support throughout the execution pipeline
- **Reasoning Integration**: Seamless integration of reasoning engines with agent decision-making

### 📊 Advanced Monitoring & Events

- **Comprehensive Event System**: 9 different event types for complete execution monitoring
- **Public Status Updates**: UI-friendly status updates without exposing internal reasoning
- **Progress Tracking**: Real-time progress percentage and detailed status information
- **Execution Time Tracking**: Detailed timing for LLM calls, tool executions, and reasoning activities
- **Turn-level Monitoring**: Complete visibility into each agent turn and decision
- **Error Recovery**: Graceful handling of failures with detailed error information
- **Reasoning Monitoring**: Real-time tracking of reasoning engine activities and insights

### ⚙️ Advanced Configuration

- **Reasoning Configuration**: Granular control over reasoning strategies, depth limits, and exploration parameters
- **Granular Field Size Limits**: Separate configurable limits for thoughts, final output, and summaries
- **Configurable Timeouts**: Separate timeouts for LLM and tool calls
- **Token Management**: Automatic prompt optimization and size management
- **Function Calling Toggle**: Can switch between Re/Act and function calling modes
- **Immutable Configuration**: Type-safe configuration with init-only properties
- **Exploration Strategy Control**: Configurable tree exploration strategies for optimal performance

### 🔒 Production-Ready Features

- **Thread Safety**: All components are thread-safe for production use
- **Structured Logging**: Comprehensive logging with multiple log levels
- **Error Handling**: Robust error handling with detailed exception information
- **State Persistence**: Multiple state store implementations (memory, file-based)
- **Performance Optimization**: Advanced caching and token management strategies
- **Validation Framework**: Comprehensive validation at all levels with structured error reporting

## 📦 Installation

### Available NuGet Packages

| Package | Version | Description |
|---------|---------|-------------|
| `AIAgentSharp` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp) | Core framework with abstract LLM interfaces, reasoning engines, and tool framework |
| `AIAgentSharp.OpenAI` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.OpenAI) | OpenAI integration package with `OpenAiLlmClient` implementation |

### Multiple LLM Provider Support

AIAgentSharp supports multiple LLM providers through a flexible architecture:

- **AIAgentSharp** - Core framework with abstract LLM interfaces
- **AIAgentSharp.OpenAI** - OpenAI integration package
- **Custom LLM Providers** - Implement `ILlmClient` for your preferred provider

### Quick Start with NuGet Packages

#### Core Framework
```bash
dotnet add package AIAgentSharp
```

#### OpenAI Integration
```bash
dotnet add package AIAgentSharp.OpenAI
```

### Prerequisites

- .NET 8.0 or later
- LLM provider API key (OpenAI, Anthropic, etc.)

### Basic Setup

1. **Create a new .NET project** (if you don't have one):
   ```bash
   dotnet new console -n MyAgentApp
   cd MyAgentApp
   ```

2. **Install the packages**:
   ```bash
   dotnet add package AIAgentSharp
   dotnet add package AIAgentSharp.OpenAI
   ```

3. **Add required using statements** to your `Program.cs`:
   ```csharp
   using AIAgentSharp.Agents;
   using AIAgentSharp.StateStores;
   using AIAgentSharp.Examples;
   using AIAgentSharp.OpenAI;
   ```

4. **Set up your API key**:
   ```bash
   # Windows
   set OPENAI_API_KEY=your-api-key-here
   
   # Linux/macOS
   export OPENAI_API_KEY=your-api-key-here
   ```

5. **Create a basic agent with OpenAI**:
   ```csharp
   var llm = new OpenAiLlmClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
   var store = new MemoryAgentStateStore();
   var agent = new Agent(llm, store);
   
   var result = await agent.RunAsync("my-agent", "Hello, how are you?", new List<ITool>());
   Console.WriteLine(result.FinalOutput);
   ```

### Using Different LLM Providers

#### OpenAI Integration
```csharp
using AIAgentSharp.OpenAI;

var llm = new OpenAiLlmClient(apiKey);
var agent = new Agent(llm, store);
```

#### Custom LLM Provider
```csharp
// Implement ILlmClient for your preferred provider
public class MyCustomLlmClient : ILlmClient
{
    // Implementation details...
}

var llm = new MyCustomLlmClient();
var agent = new Agent(llm, store);
```

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
- **`ReasoningManager`**: Manages different reasoning engines (CoT, ToT, Hybrid)
- **`ChainOfThoughtEngine`**: Implements sequential step-by-step reasoning
- **`TreeOfThoughtsEngine`**: Implements multi-branch solution exploration
- **`ITool` Interface**: Extensible tool system with automatic schema generation
- **`BaseTool<TParams, TResult>`**: Base class for strongly-typed tools with validation
- **`IAgentStateStore`**: Pluggable state persistence layer
- **`ILlmClient`**: Abstract LLM client interface for different providers
- **`OpenAiLlmClient`**: OpenAI integration implementation
- **Event System**: Comprehensive event system for monitoring and integration

### Multi-Provider Architecture

AIAgentSharp uses a clean separation between the core framework and LLM provider implementations:

```
AIAgentSharp (Core Framework)
├── ILlmClient (Abstract Interface)
├── Agent (Provider-agnostic)
├── Reasoning Engines
└── Tool Framework

AIAgentSharp.OpenAI (Provider Implementation)
├── OpenAiLlmClient (ILlmClient implementation)
├── OpenAiConfiguration
└── OpenAI-specific utilities

Custom Providers
├── YourCustomLlmClient (ILlmClient implementation)
└── Provider-specific configuration
```

## 🚀 Quick Start

### Basic Usage

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.Examples;
using AIAgentSharp.OpenAI;

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

// Configure agent with reasoning
var config = new AgentConfiguration
{
    MaxTurns = 40,
    UseFunctionCalling = true,
    EmitPublicStatus = true,
    ReasoningType = ReasoningType.ChainOfThought, // Enable reasoning
    MaxReasoningSteps = 8,
    EnableReasoningValidation = true
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

### Advanced Reasoning Examples

#### Chain of Thought Reasoning

```csharp
// Configure for Chain of Thought reasoning
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.7,
    MaxTurns = 25,
    UseFunctionCalling = true,
    EmitPublicStatus = true
};

var agent = new Agent(llm, store, config: config);

// Subscribe to reasoning events
agent.StatusUpdate += (sender, e) =>
{
    if (e.StatusTitle?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true)
    {
        Console.WriteLine($"🤔 {e.StatusTitle}: {e.StatusDetails}");
    }
};

var result = await agent.RunAsync("cot-agent", "Solve a complex business problem", tools);

// Access reasoning chain
if (result.State?.CurrentReasoningChain != null)
{
    var chain = result.State.CurrentReasoningChain;
    Console.WriteLine($"Reasoning Steps: {chain.Steps.Count}");
    Console.WriteLine($"Final Confidence: {chain.FinalConfidence:F2}");
}
```

#### Tree of Thoughts Reasoning

```csharp
// Configure for Tree of Thoughts reasoning
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxTreeDepth = 6,
    MaxTreeNodes = 80,
    TreeExplorationStrategy = ExplorationStrategy.BestFirst,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.6,
    MaxTurns = 30,
    UseFunctionCalling = true,
    EmitPublicStatus = true
};

var agent = new Agent(llm, store, config: config);

var result = await agent.RunAsync("tot-agent", "Explore multiple solution approaches", tools);

// Access reasoning tree
if (result.State?.CurrentReasoningTree != null)
{
    var tree = result.State.CurrentReasoningTree;
    Console.WriteLine($"Nodes Explored: {tree.NodeCount}");
    Console.WriteLine($"Best Path Length: {tree.BestPath.Count}");
    Console.WriteLine($"Exploration Strategy: {tree.ExplorationStrategy}");
}
```

#### Hybrid Reasoning

```csharp
// Configure for hybrid reasoning (combines multiple approaches)
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.Hybrid,
    MaxReasoningSteps = 6,
    MaxTreeDepth = 4,
    MaxTreeNodes = 50,
    TreeExplorationStrategy = ExplorationStrategy.BeamSearch,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.65,
    MaxTurns = 30,
    UseFunctionCalling = true,
    EmitPublicStatus = true
};

var agent = new Agent(llm, store, config: config);

var result = await agent.RunAsync("hybrid-agent", "Complex problem requiring multiple approaches", tools);
```

### Creating Enhanced Tools with Validation

```csharp
using AIAgentSharp;
using System.ComponentModel.DataAnnotations;

[ToolParams(Description = "Advanced weather lookup with validation")]
public sealed class WeatherParams
{
    [ToolField(Description = "City name", Example = "New York", Required = true)]
    [Required]
    [MinLength(1, ErrorMessage = "City name cannot be empty")]
    [MaxLength(100, ErrorMessage = "City name too long")]
    public string City { get; set; } = default!;
    
    [ToolField(Description = "Temperature unit", Example = "Celsius")]
    [Required]
    [RegularExpression("^(Celsius|Fahrenheit|Kelvin)$", ErrorMessage = "Unit must be Celsius, Fahrenheit, or Kelvin")]
    public string Unit { get; set; } = "Celsius";

    [ToolField(Description = "Include forecast", Example = "true")]
    public bool IncludeForecast { get; set; } = false;

    [ToolField(Description = "Forecast days", Example = "5")]
    [Range(1, 14, ErrorMessage = "Forecast days must be between 1 and 14")]
    public int? ForecastDays { get; set; }
}

public sealed class WeatherTool : BaseTool<WeatherParams, object>
{
    public override string Name => "get_weather";
    public override string Description => "Get current weather information for a city with optional forecast";

    protected override async Task<object> InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        // Validate forecast parameters
        if (parameters.IncludeForecast && !parameters.ForecastDays.HasValue)
        {
            throw new ToolValidationException(
                "Forecast days required when including forecast",
                fieldErrors: new List<ToolValidationError> 
                { 
                    new("forecastDays", "Forecast days is required when includeForecast is true") 
                }
            );
        }
        
        // Your weather API logic here
        var weather = await FetchWeatherAsync(parameters.City, parameters.Unit, ct);
        
        var result = new { 
            city = parameters.City,
            temperature = weather.Temperature,
            unit = parameters.Unit,
            description = weather.Description
        };

        if (parameters.IncludeForecast && parameters.ForecastDays.HasValue)
        {
            var forecast = await FetchForecastAsync(parameters.City, parameters.ForecastDays.Value, ct);
            // Add forecast to result
        }
        
        return result;
    }
}
```

### Step-by-Step Execution with Reasoning

```csharp
// Execute individual steps with reasoning
var stepResult = await agent.StepAsync("agent-id", "goal", tools);

if (stepResult.Continue)
{
    Console.WriteLine("Agent continues execution");
    Console.WriteLine($"Thoughts: {stepResult.LlmMessage?.Thoughts}");
    
    // Check if reasoning was performed
    if (stepResult.State?.CurrentReasoningChain != null)
    {
        Console.WriteLine($"Chain of Thought steps: {stepResult.State.CurrentReasoningChain.Steps.Count}");
    }
    else if (stepResult.State?.CurrentReasoningTree != null)
    {
        Console.WriteLine($"Tree of Thoughts nodes: {stepResult.State.CurrentReasoningTree.NodeCount}");
    }
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

// Tool with rich metadata and validation
[ToolParams(Description = "Advanced search with comprehensive validation")]
public sealed class SearchParams
{
    [ToolField(Description = "Search query", Example = "machine learning", Required = true)]
    [Required]
    [MinLength(1, ErrorMessage = "Query cannot be empty")]
    [MaxLength(500, ErrorMessage = "Query too long")]
    public string Query { get; set; } = default!;
    
    [ToolField(Description = "Number of results", Example = "10")]
    [Range(1, 100, ErrorMessage = "Max results must be between 1 and 100")]
    public int MaxResults { get; set; } = 10;
    
    [ToolField(Description = "Search filters", Example = "recent")]
    [RegularExpression("^(recent|popular|trending|all)$", ErrorMessage = "Invalid filter value")]
    public string? Filters { get; set; }

    [ToolField(Description = "Include metadata", Example = "true")]
    public bool IncludeMetadata { get; set; } = false;
}
```

### Event Monitoring with Reasoning

```csharp
// Subscribe to events for comprehensive monitoring
agent.RunStarted += (sender, e) => 
    Console.WriteLine($"Agent {e.AgentId} started with goal: {e.Goal}");

agent.StepStarted += (sender, e) => 
    Console.WriteLine($"Step {e.TurnIndex + 1} started");

agent.ToolCallStarted += (sender, e) => 
    Console.WriteLine($"Tool {e.ToolName} called with params: {JsonSerializer.Serialize(e.Parameters)}");

agent.ToolCallCompleted += (sender, e) => 
    Console.WriteLine($"Tool {e.ToolName} completed in {e.ExecutionTime.TotalMilliseconds}ms");

agent.StatusUpdate += (sender, e) => 
{
    if (e.StatusTitle?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true)
    {
        Console.WriteLine($"🧠 Reasoning: {e.StatusTitle} - {e.StatusDetails}");
    }
    else
    {
        Console.WriteLine($"Status: {e.StatusTitle} - Progress: {e.ProgressPct}%");
    }
};

agent.RunCompleted += (sender, e) => 
    Console.WriteLine($"Agent completed with {e.TotalTurns} turns");
```

## 📚 Documentation

For comprehensive documentation, see:

- **[Complete API Documentation](DOCUMENTATION.md)** - Detailed documentation covering all components, architecture, and usage examples
- **[API Reference](DOCUMENTATION.md#api-reference)** - Complete API reference with examples
- **[Advanced Features](DOCUMENTATION.md#advanced-features)** - Reasoning engines, loop detection, history summarization, and more
- **[Performance Considerations](DOCUMENTATION.md#performance-considerations)** - Optimization tips and best practices

## 🔧 Configuration

The framework provides extensive configuration options including advanced reasoning settings:

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
    MaxSummaryLength = 40000,
    
    // Reasoning Configuration
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxReasoningSteps = 12,
    MaxTreeDepth = 8,
    MaxTreeNodes = 100,
    TreeExplorationStrategy = ExplorationStrategy.BeamSearch,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.7,
    MaxReasoningTimeMs = 30000
};
```

### Reasoning Configuration Options

```csharp
// Chain of Thought Configuration
var cotConfig = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.7
};

// Tree of Thoughts Configuration
var totConfig = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxTreeDepth = 6,
    MaxTreeNodes = 80,
    TreeExplorationStrategy = ExplorationStrategy.BestFirst,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.6
};

// Hybrid Reasoning Configuration
var hybridConfig = new AgentConfiguration
{
    ReasoningType = ReasoningType.Hybrid,
    MaxReasoningSteps = 6,
    MaxTreeDepth = 4,
    MaxTreeNodes = 50,
    TreeExplorationStrategy = ExplorationStrategy.BeamSearch,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.65
};
```

## 🧪 Testing

Run the comprehensive test suite:

```bash
dotnet test
```

The framework includes 400+ tests covering:
- Agent execution and state management
- Reasoning engines (Chain of Thought, Tree of Thoughts, Hybrid)
- Tool framework and validation
- LLM integration and function calling
- Event system and status updates
- Configuration and error handling
- Tree exploration strategies and validation

### Code Coverage

This project uses automated code coverage reporting with [Codecov](https://codecov.io). Coverage reports are generated automatically on every push and pull request.

**Run coverage locally:**
```bash
# Using the provided script (recommended)
powershell -ExecutionPolicy Bypass -File scripts/run-coverage.ps1

# Or manually
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

**Coverage Configuration:**
- Uses Coverlet for coverage collection
- Excludes test assemblies and xUnit files
- Generates Cobertura XML format for Codecov integration
- Coverage badge displayed in README header
- Minimum coverage target: 80%

**Coverage Exclusions:**
- Test files (`**/*Tests.cs`, `**/*Test.cs`)
- Generated code (`obj/**/*`, `bin/**/*`)
- Compiler-generated attributes
- Obsolete code

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](DOCUMENTATION.md#contributing) for details.

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

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with ❤️ for the .NET community
- Inspired by the Re/Act pattern and OpenAI's function calling
- Advanced reasoning capabilities based on Chain of Thought and Tree of Thoughts research
- Designed for production use with comprehensive error handling and monitoring.

---

**AIAgentSharp** - Empowering .NET developers to build intelligent, reasoning-enabled AI agents with ease.
