# AIAgentSharp - Complete Project Documentation

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Core Components](#core-components)
4. [Tool Framework](#tool-framework)
5. [State Management](#state-management)
6. [LLM Integration](#llm-integration)
7. [Event System](#event-system)
8. [Metrics System](#metrics-system)
9. [Configuration](#configuration)
10. [Installation and Setup](#installation-and-setup)
11. [Usage Examples](#usage-examples)
12. [Advanced Features](#advanced-features)
13. [Testing](#testing)
14. [Performance Considerations](#performance-considerations)
15. [Error Handling](#error-handling)
16. [API Reference](#api-reference)
17. [Contributing](#contributing)

## Project Overview

**AIAgentSharp** is a comprehensive, production-ready .NET 8.0 framework for building LLM-powered agents with tool calling capabilities. The framework implements the Re/Act (Reasoning and Acting) pattern and supports OpenAI-style function calling, providing a complete solution for creating intelligent agents that can reason, act, and observe.

### Multiple LLM Provider Support

AIAgentSharp supports multiple LLM providers through a flexible architecture:

- **AIAgentSharp** - Core framework with abstract LLM interfaces, reasoning engines, and tool framework
- **AIAgentSharp.OpenAI** - OpenAI integration package with `OpenAiLlmClient` implementation
- **AIAgentSharp.Anthropic** - Anthropic Claude integration package with `AnthropicLlmClient` implementation
- **AIAgentSharp.Gemini** - Google Gemini integration package with `GeminiLlmClient` implementation
- **AIAgentSharp.Mistral** - Mistral AI integration package with `MistralLlmClient` implementation
- **Custom LLM Providers** - Implement `ILlmClient` for your preferred provider

### Available NuGet Packages

| Package | Version | Description |
|---------|---------|-------------|
| `AIAgentSharp` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp) | Core framework with abstract LLM interfaces, reasoning engines, and tool framework |
| `AIAgentSharp.OpenAI` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.OpenAI) | OpenAI integration package with `OpenAiLlmClient` implementation |
| `AIAgentSharp.Anthropic` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.Anthropic) | Anthropic Claude integration package with `AnthropicLlmClient` implementation |
| `AIAgentSharp.Gemini` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.Gemini) | Google Gemini integration package with `GeminiLlmClient` implementation |
| `AIAgentSharp.Mistral` | ![NuGet](https://img.shields.io/nuget/v/AIAgentSharp.Mistral) | Mistral AI integration package with `MistralLlmClient` implementation |

### Key Features

- **🔄 Re/Act Pattern Support**: Full implementation of the Re/Act pattern for LLM agents
- **🔧 Function Calling**: Support for OpenAI-style function calling when available
- **🛠️ Tool Framework**: Rich tool system with automatic schema generation and validation
- **💾 State Persistence**: Multiple state store implementations (in-memory, file-based)
- **📊 Real-time Monitoring**: Comprehensive event system for monitoring agent activity
- **📱 Public Status Updates**: Real-time status updates for UI consumption
- **🔄 Loop Detection**: Intelligent loop breaker to prevent infinite loops
- **🔄 Deduplication**: Smart caching of tool results to improve performance
- **📝 History Management**: Configurable history summarization to manage prompt size
- **⚡ Performance Optimized**: Efficient token management and prompt optimization
- **🔒 Thread Safe**: Thread-safe implementations for production use

### Project Structure

```
AIAgentSharp/
├── src/AIAgentSharp/           # Core framework source
│   ├── Agents/                 # Core agent implementation
│   ├── Contracts/              # Interface definitions
│   ├── Models/                 # Data models and DTOs
│   ├── Tools/                  # Tool framework
│   ├── Configuration/          # Configuration classes
│   ├── Events/                 # Event system
│   ├── Llm/                    # LLM integration interfaces
│   ├── StateStores/            # State persistence
│   ├── Schema/                 # Schema generation
│   ├── Validation/             # Validation framework
│   ├── Utils/                  # Utility classes
│   └── Logging/                # Logging infrastructure
├── src/AIAgentSharp.OpenAI/    # OpenAI integration package
│   ├── OpenAiLlmClient.cs      # OpenAI LLM client implementation
│   ├── OpenAiConfiguration.cs  # OpenAI-specific configuration
│   └── README.md               # OpenAI package documentation
├── src/AIAgentSharp.Anthropic/ # Anthropic integration package
│   ├── AnthropicLlmClient.cs   # Anthropic LLM client implementation
│   ├── AnthropicConfiguration.cs # Anthropic-specific configuration
│   └── README.md               # Anthropic package documentation
├── src/AIAgentSharp.Gemini/    # Gemini integration package
│   ├── GeminiLlmClient.cs      # Gemini LLM client implementation
│   ├── GeminiConfiguration.cs  # Gemini-specific configuration
│   └── README.md               # Gemini package documentation
├── src/AIAgentSharp.Mistral/   # Mistral integration package
│   ├── MistralLlmClient.cs     # Mistral LLM client implementation
│   ├── MistralConfiguration.cs # Mistral-specific configuration
│   └── README.md               # Mistral package documentation
├── tests/                      # Comprehensive test suite
```

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

AIAgentSharp.Anthropic (Provider Implementation)
├── AnthropicLlmClient (ILlmClient implementation)
├── AnthropicConfiguration
└── Anthropic-specific utilities

AIAgentSharp.Gemini (Provider Implementation)
├── GeminiLlmClient (ILlmClient implementation)
├── GeminiConfiguration
└── Google AI Platform utilities

AIAgentSharp.Mistral (Provider Implementation)
├── MistralLlmClient (ILlmClient implementation)
├── MistralConfiguration
└── Mistral AI-specific utilities

Custom Providers
├── YourCustomLlmClient (ILlmClient implementation)
└── Provider-specific configuration
```

## Architecture

The AIAgentSharp framework follows a modular, component-based architecture with clear separation of concerns:

### Core Architecture Components

1. **Agent**: Main orchestrator that coordinates all operations
2. **AgentOrchestrator**: Handles the execution flow and state management
3. **LlmCommunicator**: Manages LLM interactions and response parsing
4. **ToolExecutor**: Handles tool execution, validation, and caching
5. **EventManager**: Manages event emission and subscription
6. **StatusManager**: Handles public status updates
7. **LoopDetector**: Prevents infinite loops
8. **MessageBuilder**: Constructs prompts for LLM communication

### Design Patterns

- **Re/Act Pattern**: Reasoning and Acting cycle for agent decision making
- **Strategy Pattern**: Pluggable components (LLM clients, state stores, tools)
- **Observer Pattern**: Event system for monitoring and integration
- **Factory Pattern**: Tool creation and schema generation
- **Builder Pattern**: Message construction and prompt building

## Core Components

### 1. Agent Class

The main entry point for agent operations, implementing the `IAgent` interface.

```csharp
public sealed class Agent : IAgent
{
    // Core dependencies
    private readonly AgentConfiguration _config;
    private readonly ILlmClient _llm;
    private readonly ILogger _logger;
    private readonly IAgentStateStore _store;
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
}
```

**Key Responsibilities:**
- Orchestrates agent execution flow
- Manages state persistence
- Emits events and status updates
- Handles configuration and timeouts
- Provides high-level API for agent operations

### 2. AgentOrchestrator

Handles the core execution logic and coordinates between components.

```csharp
public sealed class AgentOrchestrator : IAgentOrchestrator
{
    // Dependencies
    private readonly ILlmClient _llm;
    private readonly IAgentStateStore _store;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
    private readonly ILlmCommunicator _llmCommunicator;
    private readonly IToolExecutor _toolExecutor;
    private readonly ILoopDetector _loopDetector;
    private readonly IMessageBuilder _messageBuilder;
}
```

**Key Responsibilities:**
- Executes individual agent steps
- Manages tool execution and caching
- Handles loop detection and prevention
- Coordinates LLM communication
- Manages agent state transitions

### 3. LlmCommunicator

Manages LLM interactions and response parsing.

```csharp
public sealed class LlmCommunicator : ILlmCommunicator
{
    private readonly ILlmClient _llm;
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly IStatusManager _statusManager;
}
```

**Key Responsibilities:**
- Sends prompts to LLM
- Parses LLM responses
- Handles function calling vs Re/Act mode
- Manages timeouts and retries
- Emits LLM-related events

### 4. ToolExecutor

Handles tool execution, validation, and result caching.

```csharp
public sealed class ToolExecutor : IToolExecutor
{
    private readonly AgentConfiguration _config;
    private readonly ILogger _logger;
    private readonly IEventManager _eventManager;
    private readonly Dictionary<string, ToolExecutionResult> _resultCache;
}
```

**Key Responsibilities:**
- Executes tools with parameters
- Validates tool parameters
- Caches tool results for deduplication
- Handles tool timeouts and errors
- Emits tool execution events

## Tool Framework

The tool framework provides a flexible and extensible system for creating tools that agents can use.

### Core Interfaces

#### ITool Interface

```csharp
public interface ITool
{
    string Name { get; }
    Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default);
}
```

#### IToolIntrospect Interface

```csharp
public interface IToolIntrospect
{
    string Describe();
}
```

#### IFunctionSchemaProvider Interface

```csharp
public interface IFunctionSchemaProvider
{
    object GetJsonSchema();
}
```

### BaseTool Class

The `BaseTool<TParams, TResult>` class provides a strongly-typed foundation for creating tools:

```csharp
public abstract class BaseTool<TParams, TResult> : ITool, IToolIntrospect, IFunctionSchemaProvider
{
    public abstract string Description { get; }
    public abstract string Name { get; }
    
    public object GetJsonSchema() => SchemaGenerator.Generate<TParams>();
    public string Describe() => ToolDescriptionGenerator.Build<TParams>(Name, Description);
    
    public async Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
    {
        // Parameter validation and deserialization
        var typedParams = ValidateAndDeserialize<TParams>(parameters);
        return await InvokeTypedAsync(typedParams, ct);
    }
    
    protected abstract Task<TResult> InvokeTypedAsync(TParams parameters, CancellationToken ct = default);
}
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

### Tool Validation

The framework provides comprehensive parameter validation:

```csharp
public class ValidationExampleParams
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Range(0, 120)]
    public int Age { get; set; }
    
    [EmailAddress]
    public string? Email { get; set; }
}
```

### Tool Deduplication Control

Tools can control their deduplication behavior:

```csharp
public class NonIdempotentTool : BaseTool<MyParams, object>, IDedupeControl
{
    public bool AllowDedupe => false; // Opt out of deduplication
    public TimeSpan? CustomTtl => TimeSpan.FromSeconds(30); // Custom TTL
}
```

## State Management

The framework provides flexible state persistence through the `IAgentStateStore` interface.

### IAgentStateStore Interface

```csharp
public interface IAgentStateStore
{
    Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default);
    Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default);
}
```

### Built-in State Stores

#### MemoryAgentStateStore

```csharp
public class MemoryAgentStateStore : IAgentStateStore
{
    private readonly ConcurrentDictionary<string, AgentState> _states = new();
    
    public async Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default)
    {
        return _states.TryGetValue(agentId, out var state) ? state : null;
    }
    
    public async Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default)
    {
        _states[agentId] = state;
    }
}
```

#### FileAgentStateStore

```csharp
public class FileAgentStateStore : IAgentStateStore
{
    private readonly string _directory;
    
    public FileAgentStateStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }
    
    public async Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_directory, $"{agentId}.json");
        if (!File.Exists(filePath)) return null;
        
        var json = await File.ReadAllTextAsync(filePath, ct);
        return JsonSerializer.Deserialize<AgentState>(json, JsonUtil.JsonOptions);
    }
    
    public async Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_directory, $"{agentId}.json");
        var json = JsonSerializer.Serialize(state, JsonUtil.JsonOptions);
        await File.WriteAllTextAsync(filePath, json, ct);
    }
}
```

### AgentState Model

```csharp
public sealed class AgentState
{
    public string AgentId { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
    public List<AgentTurn> Turns { get; init; } = new();
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastModifiedUtc { get; init; } = DateTimeOffset.UtcNow;
}
```

## LLM Integration

The framework supports multiple LLM providers through the `ILlmClient` interface, with a clean separation between the core framework and provider implementations.

### ILlmClient Interface

```csharp
public interface ILlmClient
{
    Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default);
}
```

### Available LLM Providers

#### AIAgentSharp.OpenAI Package

The `AIAgentSharp.OpenAI` package provides OpenAI integration:

```csharp
using AIAgentSharp.OpenAI;

// Basic usage
var llm = new OpenAiLlmClient(apiKey);

// With custom configuration
var config = new OpenAiConfiguration
{
    Model = "gpt-4o-mini",
    Temperature = 0.1f,
    MaxTokens = 4000,
    EnableFunctionCalling = true
};
var llm = new OpenAiLlmClient(apiKey, config);
```

#### AIAgentSharp.Anthropic Package

The `AIAgentSharp.Anthropic` package provides Anthropic Claude integration:

```csharp
using AIAgentSharp.Anthropic;

// Basic usage
var llm = new AnthropicLlmClient(apiKey);

// With custom configuration
var config = new AnthropicConfiguration
{
    Model = "claude-3-5-sonnet-20241022",
    Temperature = 0.1f,
    MaxTokens = 4000,
    EnableFunctionCalling = true
};
var llm = new AnthropicLlmClient(apiKey, config);
```

#### AIAgentSharp.Gemini Package

The `AIAgentSharp.Gemini` package provides Google Gemini integration:

```csharp
using AIAgentSharp.Gemini;

// Basic usage
var llm = new GeminiLlmClient(apiKey);

// With custom configuration
var config = new GeminiConfiguration
{
    Model = "gemini-1.5-flash",
    Temperature = 0.1f,
    MaxTokens = 4000,
    EnableFunctionCalling = true,
    ProjectId = "your-project-id",
    Region = "us-central1"
};
var llm = new GeminiLlmClient(apiKey, config);
```

#### AIAgentSharp.Mistral Package

The `AIAgentSharp.Mistral` package provides Mistral AI integration:

```csharp
using AIAgentSharp.Mistral;

// Basic usage
var llm = new MistralLlmClient(apiKey);

// With custom configuration
var config = new MistralConfiguration
{
    Model = "mistral-large-latest",
    Temperature = 0.1f,
    MaxTokens = 4000,
    EnableFunctionCalling = true
};
var llm = new MistralLlmClient(apiKey, config);
```

#### OpenAiLlmClient Features

```csharp
public sealed class OpenAiLlmClient : ILlmClient
{
    // Constructor overloads
    public OpenAiLlmClient(string apiKey, string model = "gpt-4o-mini", ILogger? logger = null);
    public OpenAiLlmClient(string apiKey, OpenAiConfiguration configuration, ILogger? logger = null);
    
    // Function calling support
    public async Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages, 
        IEnumerable<OpenAiFunctionSpec> functions, 
        CancellationToken ct = default);
}
```

#### OpenAiConfiguration Options

```csharp
public sealed class OpenAiConfiguration
{
    public string Model { get; init; } = "gpt-4o-mini";
    public int MaxTokens { get; init; } = 4000;
    public float Temperature { get; init; } = 0.1f;
    public float TopP { get; init; } = 1.0f;
    public float FrequencyPenalty { get; init; } = 0.0f;
    public float PresencePenalty { get; init; } = 0.0f;
    public bool EnableStreaming { get; init; } = false;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public int MaxRetries { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public bool EnableFunctionCalling { get; init; } = true;
    public string? OrganizationId { get; init; }
    public string? ApiBaseUrl { get; init; }
    
    // Factory methods for common use cases
    public static OpenAiConfiguration CreateForAgentReasoning();
    public static OpenAiConfiguration CreateForCreativeTasks();
    public static OpenAiConfiguration CreateForCostEfficiency();
}
```

#### AnthropicConfiguration Options

```csharp
public sealed class AnthropicConfiguration
{
    public string Model { get; init; } = "claude-3-5-sonnet-20241022";
    public int MaxTokens { get; init; } = 4000;
    public float Temperature { get; init; } = 0.1f;
    public float TopP { get; init; } = 1.0f;
    public int? TopK { get; init; }
    public bool EnableStreaming { get; init; } = false;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public int MaxRetries { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public bool EnableFunctionCalling { get; init; } = true;
    public string? ApiBaseUrl { get; init; }
    public string? OrganizationId { get; init; }
    
    // Factory methods for common use cases
    public static AnthropicConfiguration CreateForAgentReasoning();
    public static AnthropicConfiguration CreateForCreativeTasks();
    public static AnthropicConfiguration CreateForCostEfficiency();
}
```

#### GeminiConfiguration Options

```csharp
public sealed class GeminiConfiguration
{
    public string Model { get; init; } = "gemini-1.5-flash";
    public int MaxTokens { get; init; } = 4000;
    public float Temperature { get; init; } = 0.1f;
    public float TopP { get; init; } = 1.0f;
    public int? TopK { get; init; }
    public bool EnableStreaming { get; init; } = false;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public int MaxRetries { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public bool EnableFunctionCalling { get; init; } = true;
    public string? ProjectId { get; init; }
    public string Region { get; init; } = "us-central1";
    public string? ApiBaseUrl { get; init; }
    
    // Factory methods for common use cases
    public static GeminiConfiguration CreateForAgentReasoning();
    public static GeminiConfiguration CreateForCreativeTasks();
    public static GeminiConfiguration CreateForCostEfficiency();
}
```

#### MistralConfiguration Options

```csharp
public sealed class MistralConfiguration
{
    public string Model { get; init; } = "mistral-large-latest";
    public int MaxTokens { get; init; } = 4000;
    public float Temperature { get; init; } = 0.1f;
    public float TopP { get; init; } = 1.0f;
    public int? TopK { get; init; }
    public bool EnableStreaming { get; init; } = false;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public int MaxRetries { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public bool EnableFunctionCalling { get; init; } = true;
    public string? ApiBaseUrl { get; init; }
    public string? OrganizationId { get; init; }
    
    // Factory methods for common use cases
    public static MistralConfiguration CreateForAgentReasoning();
    public static MistralConfiguration CreateForCreativeTasks();
    public static MistralConfiguration CreateForCostEfficiency();
}
```

#### Custom LLM Provider Implementation

You can implement your own LLM provider by implementing the `ILlmClient` interface:

```csharp
public class AnthropicLlmClient : ILlmClient
{
    private readonly string _apiKey;
    private readonly string _model;
    
    public AnthropicLlmClient(string apiKey, string model = "claude-3-sonnet-20240229")
    {
        _apiKey = apiKey;
        _model = model;
    }
    
    public async Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        // Implement Anthropic API integration
        // Convert messages to Anthropic format
        // Make API call and return response
        throw new NotImplementedException("Implement Anthropic API integration");
    }
}

// Usage
var llm = new AnthropicLlmClient(apiKey);
var agent = new Agent(llm, store);
```

#### DelegateLlmClient (Testing/Development)

```csharp
public class DelegateLlmClient : ILlmClient
{
    private readonly Func<IEnumerable<LlmMessage>, CancellationToken, Task<string>> _delegate;
    
    public DelegateLlmClient(Func<IEnumerable<LlmMessage>, CancellationToken, Task<string>> @delegate)
    {
        _delegate = @delegate;
    }
    
    public async Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        return await _delegate(messages, ct);
    }
}
```

### Function Calling Support

The framework supports OpenAI-style function calling through the `IFunctionCallingLlmClient` interface:

```csharp
public interface IFunctionCallingLlmClient : ILlmClient
{
    Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages, 
        IEnumerable<OpenAiFunctionSpec> functions, 
        CancellationToken ct = default);
}
```

## Event System

The framework provides a comprehensive event system for monitoring and integration.

### Core Events

```csharp
// Agent lifecycle events
public event EventHandler<AgentRunStartedEventArgs>? RunStarted;
public event EventHandler<AgentRunCompletedEventArgs>? RunCompleted;

// Step execution events
public event EventHandler<AgentStepStartedEventArgs>? StepStarted;
public event EventHandler<AgentStepCompletedEventArgs>? StepCompleted;

// LLM interaction events
public event EventHandler<AgentLlmCallStartedEventArgs>? LlmCallStarted;
public event EventHandler<AgentLlmCallCompletedEventArgs>? LlmCallCompleted;

// Tool execution events
public event EventHandler<AgentToolCallStartedEventArgs>? ToolCallStarted;
public event EventHandler<AgentToolCallCompletedEventArgs>? ToolCallCompleted;

// Status update events
public event EventHandler<AgentStatusEventArgs>? StatusUpdate;
```

### Event Usage Example

```csharp
// Subscribe to events
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

### Public Status Updates

The framework supports real-time status updates for UI consumption:

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

## Metrics System

The AIAgentSharp framework provides a comprehensive metrics system for monitoring performance, operational health, quality indicators, and resource usage. The metrics system is designed to be lightweight, thread-safe, and non-blocking to ensure it doesn't impact agent performance.

### Core Metrics Categories

The metrics system collects data across four main categories:

#### 1. Performance Metrics
- **Execution Times**: Agent runs, steps, LLM calls, tool calls, reasoning operations
- **Throughput**: Requests per second, total operations
- **Percentiles**: P95 execution times for performance analysis
- **Counters**: Total operations by type

#### 2. Operational Metrics
- **Success Rates**: Agent runs, steps, LLM calls, tool calls
- **Error Rates**: Failure counts by error type
- **Loop Detection**: Loop detection events and patterns
- **Deduplication**: Cache hit rates and effectiveness

#### 3. Quality Metrics
- **Reasoning Confidence**: Confidence scores by reasoning type
- **Response Quality**: Response length and final output rates
- **Confidence Distribution**: Distribution of confidence scores
- **Quality Indicators**: User satisfaction and response completeness

#### 4. Resource Metrics
- **Token Usage**: Input/output tokens by model
- **API Calls**: Call counts by type and model
- **State Store Operations**: Operation counts and timing
- **Resource Consumption**: Memory and processing metrics

### Metrics Interfaces

#### IMetricsCollector

The core interface for recording metrics:

```csharp
public interface IMetricsCollector
{
    // Performance Metrics
    void RecordAgentRunExecutionTime(string agentId, long executionTimeMs, int totalTurns);
    void RecordAgentStepExecutionTime(string agentId, int turnIndex, long executionTimeMs);
    void RecordLlmCallExecutionTime(string agentId, int turnIndex, long executionTimeMs, string modelName);
    void RecordToolCallExecutionTime(string agentId, int turnIndex, string toolName, long executionTimeMs);
    void RecordReasoningExecutionTime(string agentId, ReasoningType reasoningType, long executionTimeMs);

    // Operational Metrics
    void RecordAgentRunCompletion(string agentId, bool succeeded, int totalTurns, string? errorType = null);
    void RecordAgentStepCompletion(string agentId, int turnIndex, bool succeeded, bool executedTool, string? errorType = null);
    void RecordLlmCallCompletion(string agentId, int turnIndex, bool succeeded, string modelName, string? errorType = null);
    void RecordToolCallCompletion(string agentId, int turnIndex, string toolName, bool succeeded, string? errorType = null);
    void RecordLoopDetection(string agentId, string loopType, int consecutiveFailures);
    void RecordDeduplicationEvent(string agentId, string toolName, bool cacheHit);

    // Quality Metrics
    void RecordReasoningConfidence(string agentId, ReasoningType reasoningType, double confidenceScore);
    void RecordResponseQuality(string agentId, int responseLength, bool hasFinalOutput);

    // Resource Metrics
    void RecordTokenUsage(string agentId, int turnIndex, int inputTokens, int outputTokens, string modelName);
    void RecordApiCall(string agentId, string apiType, string modelName);
    void RecordStateStoreOperation(string agentId, string operationType, long executionTimeMs);

    // Custom Metrics
    void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null);
    void RecordCustomEvent(string eventName, Dictionary<string, string>? tags = null);
}
```

#### IMetricsProvider

The interface for accessing collected metrics:

```csharp
public interface IMetricsProvider
{
    MetricsData GetMetrics();
    MetricsData? GetAgentMetrics(string agentId);
    MetricsData GetMetricsForTimeRange(DateTimeOffset startTime, DateTimeOffset endTime);
    void ResetMetrics();
    string ExportMetrics(MetricsExportFormat format);
    event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;
}
```

### Metrics Data Models

#### MetricsData

The main container for all metrics:

```csharp
public sealed class MetricsData
{
    public DateTimeOffset CollectedAt { get; init; }
    public PerformanceMetrics Performance { get; init; }
    public OperationalMetrics Operational { get; init; }
    public QualityMetrics Quality { get; init; }
    public ResourceMetrics Resources { get; init; }
    public Dictionary<string, CustomMetric> CustomMetrics { get; init; }
    public List<CustomEvent> CustomEvents { get; init; }
}
```

#### PerformanceMetrics

```csharp
public sealed class PerformanceMetrics
{
    public double AverageAgentRunTimeMs { get; init; }
    public double AverageAgentStepTimeMs { get; init; }
    public double AverageLlmCallTimeMs { get; init; }
    public double AverageToolCallTimeMs { get; init; }
    public double AverageReasoningTimeMs { get; init; }
    public long TotalAgentRuns { get; init; }
    public long TotalAgentSteps { get; init; }
    public long TotalLlmCalls { get; init; }
    public long TotalToolCalls { get; init; }
    public long TotalReasoningOperations { get; init; }
    public double RequestsPerSecond { get; init; }
    public double P95AgentRunTimeMs { get; init; }
    public double P95LlmCallTimeMs { get; init; }
    public double P95ToolCallTimeMs { get; init; }
}
```

#### OperationalMetrics

```csharp
public sealed class OperationalMetrics
{
    public double AgentRunSuccessRate { get; init; }
    public double AgentStepSuccessRate { get; init; }
    public double LlmCallSuccessRate { get; init; }
    public double ToolCallSuccessRate { get; init; }
    public long FailedAgentRuns { get; init; }
    public long FailedAgentSteps { get; init; }
    public long FailedLlmCalls { get; init; }
    public long FailedToolCalls { get; init; }
    public long LoopDetectionEvents { get; init; }
    public long DeduplicationCacheHits { get; init; }
    public long DeduplicationCacheMisses { get; init; }
    public double DeduplicationCacheHitRate { get; init; }
    public Dictionary<string, long> ErrorCountsByType { get; init; }
}
```

#### QualityMetrics

```csharp
public sealed class QualityMetrics
{
    public double AverageReasoningConfidence { get; init; }
    public double AverageResponseLength { get; init; }
    public double FinalOutputPercentage { get; init; }
    public Dictionary<ReasoningType, double> AverageConfidenceByReasoningType { get; init; }
    public Dictionary<string, long> ConfidenceScoreDistribution { get; init; }
}
```

#### ResourceMetrics

```csharp
public sealed class ResourceMetrics
{
    public long TotalInputTokens { get; init; }
    public long TotalOutputTokens { get; init; }
    public long TotalTokens { get; init; }
    public double AverageInputTokensPerCall { get; init; }
    public double AverageOutputTokensPerCall { get; init; }
    public Dictionary<string, TokenUsage> TokenUsageByModel { get; init; }
    public Dictionary<string, long> ApiCallCountsByType { get; init; }
    public Dictionary<string, long> ApiCallCountsByModel { get; init; }
    public Dictionary<string, long> StateStoreOperationCounts { get; init; }
    public double AverageStateStoreOperationTimeMs { get; init; }
}
```

### Using Metrics

#### Basic Metrics Access

```csharp
// Access metrics through the agent
var metrics = agent.Metrics.GetMetrics();

// Print performance metrics
Console.WriteLine($"Total Agent Runs: {metrics.Performance.TotalAgentRuns}");
Console.WriteLine($"Average Run Time: {metrics.Performance.AverageAgentRunTimeMs:F2}ms");
Console.WriteLine($"Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}");
Console.WriteLine($"Total Tokens Used: {metrics.Resources.TotalTokens:N0}");
```

#### Agent-Specific Metrics

```csharp
// Get metrics for a specific agent
var agentMetrics = agent.Metrics.GetAgentMetrics("my-agent");
if (agentMetrics != null)
{
    Console.WriteLine($"Agent-specific success rate: {agentMetrics.Operational.AgentRunSuccessRate:P2}");
    Console.WriteLine($"Agent-specific average run time: {agentMetrics.Performance.AverageAgentRunTimeMs:F2}ms");
}
```

#### Time-Range Filtering

```csharp
// Get metrics for a specific time range
var startTime = DateTimeOffset.UtcNow.AddHours(-1);
var endTime = DateTimeOffset.UtcNow;
var timeRangeMetrics = agent.Metrics.GetMetricsForTimeRange(startTime, endTime);

Console.WriteLine($"Metrics for last hour:");
Console.WriteLine($"  Runs: {timeRangeMetrics.Performance.TotalAgentRuns}");
Console.WriteLine($"  Success Rate: {timeRangeMetrics.Operational.AgentRunSuccessRate:P2}");
```

#### Real-Time Monitoring

```csharp
// Subscribe to real-time metrics updates
agent.Metrics.MetricsUpdated += (sender, e) =>
{
    Console.WriteLine($"Metrics updated: {string.Join(", ", e.UpdatedMetrics)}");
    Console.WriteLine($"Latest success rate: {e.Metrics.Operational.AgentRunSuccessRate:P2}");
    Console.WriteLine($"Total runs: {e.Metrics.Performance.TotalAgentRuns}");
};
```

#### Custom Metrics

```csharp
// Record custom metrics
agent.Metrics.RecordCustomMetric("user_satisfaction", 4.5, new Dictionary<string, string> 
{ 
    ["agent_type"] = "travel_planner",
    ["user_id"] = "user123"
});

// Record custom events
agent.Metrics.RecordCustomEvent("user_feedback", new Dictionary<string, string> 
{ 
    ["rating"] = "excellent",
    ["category"] = "travel_planning",
    ["user_id"] = "user123"
});
```

### Metrics Export

The framework supports multiple export formats for integration with monitoring systems:

#### JSON Export

```csharp
var jsonMetrics = agent.Metrics.ExportMetrics(MetricsExportFormat.Json);
Console.WriteLine(jsonMetrics);
```

#### Prometheus Export

```csharp
var prometheusMetrics = agent.Metrics.ExportMetrics(MetricsExportFormat.Prometheus);
Console.WriteLine(prometheusMetrics);
// Output includes:
// # HELP aiagentsharp_agent_runs_total Total number of agent runs
// # TYPE aiagentsharp_agent_runs_total counter
// aiagentsharp_agent_runs_total 42
```

#### CSV Export

```csharp
var csvMetrics = agent.Metrics.ExportMetrics(MetricsExportFormat.Csv);
Console.WriteLine(csvMetrics);
// Output includes:
// Metric,Value
// TotalAgentRuns,42
// AgentRunSuccessRate,0.95
```

#### Text Export

```csharp
var textMetrics = agent.Metrics.ExportMetrics(MetricsExportFormat.Text);
Console.WriteLine(textMetrics);
// Output includes:
// AIAgentSharp Metrics Report
// Generated: 2024-01-15 10:30:00 UTC
// 
// Performance Metrics:
//   Total Agent Runs: 42
//   Average Agent Run Time: 1250.50ms
```

### Metrics Reset

```csharp
// Reset all metrics (useful for testing or new monitoring periods)
agent.Metrics.ResetMetrics();

// Verify reset
var metrics = agent.Metrics.GetMetrics();
Console.WriteLine($"After reset - Total runs: {metrics.Performance.TotalAgentRuns}"); // 0
```

### Integration with Monitoring Systems

#### Prometheus Integration

```csharp
// Export metrics for Prometheus scraping
app.MapGet("/metrics", () => agent.Metrics.ExportMetrics(MetricsExportFormat.Prometheus))
    .WithName("Metrics")
    .WithOpenApi();
```

#### Custom Monitoring Dashboard

```csharp
// Create a metrics endpoint for your dashboard
app.MapGet("/api/metrics", () =>
{
    var metrics = agent.Metrics.GetMetrics();
    return new
    {
        performance = new
        {
            totalRuns = metrics.Performance.TotalAgentRuns,
            averageRunTime = metrics.Performance.AverageAgentRunTimeMs,
            successRate = metrics.Operational.AgentRunSuccessRate
        },
        resources = new
        {
            totalTokens = metrics.Resources.TotalTokens,
            averageTokensPerCall = metrics.Resources.AverageInputTokensPerCall
        }
    };
});
```

### Performance Considerations

- **Non-blocking**: All metrics operations are designed to be non-blocking
- **Thread-safe**: All metrics collection is thread-safe for concurrent access
- **Memory efficient**: Metrics are stored in memory with configurable limits
- **Real-time**: Metrics are updated in real-time as operations occur
- **Export efficient**: Export operations are optimized for performance

### Best Practices

1. **Monitor Key Metrics**: Focus on success rates, execution times, and error rates
2. **Set Up Alerts**: Use the real-time events to set up alerts for critical failures
3. **Export Regularly**: Export metrics to external monitoring systems for long-term analysis
4. **Use Custom Metrics**: Record business-specific metrics for better insights
5. **Reset Periodically**: Reset metrics at appropriate intervals (e.g., daily, weekly)
6. **Monitor Resource Usage**: Track token usage and API call patterns for cost optimization

## Configuration

The `AgentConfiguration` class provides extensive configuration options including advanced reasoning settings:

```csharp
public sealed class AgentConfiguration
{
    // Turn limits
    public int MaxTurns { get; init; } = 100;
    public int MaxRecentTurns { get; init; } = 10;
    
    // Timeouts
    public TimeSpan LlmTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan ToolTimeout { get; init; } = TimeSpan.FromMinutes(2);
    
    // History management
    public bool EnableHistorySummarization { get; init; } = true;
    public int MaxToolOutputSize { get; init; } = 2000;
    
    // Loop detection
    public int ConsecutiveFailureThreshold { get; init; } = 3;
    public int MaxToolCallHistory { get; init; } = 20;
    
    // Deduplication
    public TimeSpan DedupeStalenessThreshold { get; init; } = TimeSpan.FromMinutes(5);
    
    // Features
    public bool UseFunctionCalling { get; init; } = true;
    public bool EmitPublicStatus { get; init; } = true;
    
    // Field size limits
    public int MaxThoughtsLength { get; init; } = 20000;
    public int MaxFinalLength { get; init; } = 50000;
    public int MaxSummaryLength { get; init; } = 40000;
    
    // Reasoning Configuration
    public ReasoningType ReasoningType { get; init; } = ReasoningType.None;
    public int MaxReasoningSteps { get; init; } = 10;
    public int MaxTreeDepth { get; init; } = 5;
    public int MaxTreeNodes { get; init; } = 50;
    public ExplorationStrategy TreeExplorationStrategy { get; init; } = ExplorationStrategy.BestFirst;
    public bool EnableReasoningValidation { get; init; } = true;
    public double MinReasoningConfidence { get; init; } = 0.7;
}
```

### Reasoning Configuration Options

#### Chain of Thought Configuration
```csharp
var cotConfig = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.7
};
```

#### Tree of Thoughts Configuration
```csharp
var totConfig = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxTreeDepth = 6,
    MaxTreeNodes = 80,
    TreeExplorationStrategy = ExplorationStrategy.BestFirst,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.6
};
```

## Installation and Setup

### Prerequisites

- .NET 8.0 or later
- LLM provider API key (OpenAI, Anthropic, etc.)

### NuGet Package Installation

#### Core Framework
```bash
dotnet add package AIAgentSharp
```

#### OpenAI Integration
```bash
dotnet add package AIAgentSharp.OpenAI
```

### Manual Installation

If you prefer to build from source:

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd AIAgentSharp
   ```

2. **Set your API key**:
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

## Usage Examples

### Basic Usage

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.OpenAI;

// Create components
var llm = new OpenAiLlmClient(apiKey);
var store = new MemoryAgentStateStore();
var tools = new List<ITool>(); // add your tools here

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

### Custom State Store

```csharp
public class DatabaseStateStore : IAgentStateStore
{
    private readonly IDbConnection _connection;
    
    public DatabaseStateStore(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default)
    {
        // Load from database
        var sql = "SELECT state_data FROM agent_states WHERE agent_id = @agentId";
        var stateData = await _connection.QuerySingleOrDefaultAsync<string>(sql, new { agentId });
        
        if (stateData == null) return null;
        
        return JsonSerializer.Deserialize<AgentState>(stateData, JsonUtil.JsonOptions);
    }
    
    public async Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default)
    {
        // Save to database
        var sql = @"
            INSERT INTO agent_states (agent_id, state_data, updated_at) 
            VALUES (@agentId, @stateData, @updatedAt)
            ON DUPLICATE KEY UPDATE 
                state_data = @stateData, 
                updated_at = @updatedAt";
        
        var stateData = JsonSerializer.Serialize(state, JsonUtil.JsonOptions);
        await _connection.ExecuteAsync(sql, new { agentId, stateData, updatedAt = DateTime.UtcNow });
    }
}
```

## Advanced Features

### Advanced Reasoning Engines

AIAgentSharp supports multiple reasoning strategies for complex problem-solving:

#### Chain of Thought (CoT) Reasoning

Sequential step-by-step reasoning for complex problem decomposition:

```csharp
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.7
};

var agent = new Agent(llm, store, config: config);
```

#### Tree of Thoughts (ToT) Reasoning

Multi-branch exploration of solution paths with configurable exploration strategies:

```csharp
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxTreeDepth = 6,
    MaxTreeNodes = 80,
    TreeExplorationStrategy = ExplorationStrategy.BestFirst,
    EnableReasoningValidation = true,
    MinReasoningConfidence = 0.6
};

var agent = new Agent(llm, store, config: config);
```

#### Exploration Strategies

- **BestFirst**: Explore the most promising paths first (default)
- **BreadthFirst**: Explore all paths at the same depth before going deeper
- **DepthFirst**: Explore one path to maximum depth before backtracking
- **BeamSearch**: Maintain a limited set of most promising paths
- **MonteCarlo**: Use random sampling for exploration

### Loop Detection

The framework includes intelligent loop detection to prevent infinite loops:

```csharp
public class LoopDetector : ILoopDetector
{
    private readonly AgentConfiguration _config;
    private readonly Dictionary<string, List<ToolCallRecord>> _toolCallHistory = new();
    
    public bool ShouldBreakLoop(string agentId, ToolCallRequest toolCall)
    {
        // Check for repeated tool calls with same parameters
        // Check for consecutive failures
        // Check for circular reasoning patterns
    }
}
```

### History Summarization

The framework automatically manages conversation history to prevent prompt bloat:

```csharp
public class MessageBuilder
{
    public IEnumerable<LlmMessage> BuildMessages(AgentState state, Dictionary<string, ITool> tools)
    {
        // Keep recent turns in full detail
        var recentTurns = state.Turns.TakeLast(_config.MaxRecentTurns);
        
        // Summarize older turns
        var olderTurns = state.Turns.SkipLast(_config.MaxRecentTurns);
        var summaries = SummarizeTurns(olderTurns);
        
        // Build optimized prompt
        return BuildOptimizedPrompt(state, recentTurns, summaries, tools);
    }
}
```

### Tool Result Caching

The framework caches tool results to improve performance:

```csharp
public class ToolExecutor
{
    private readonly Dictionary<string, ToolExecutionResult> _resultCache = new();
    
    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, Dictionary<string, object?> parameters, CancellationToken ct = default)
    {
        var cacheKey = GenerateCacheKey(toolName, parameters);
        
        // Check cache for recent results
        if (_resultCache.TryGetValue(cacheKey, out var cachedResult))
        {
            if (!IsStale(cachedResult))
            {
                return cachedResult; // Return cached result
            }
        }
        
        // Execute tool and cache result
        var result = await ExecuteToolAsync(toolName, parameters, ct);
        _resultCache[cacheKey] = result;
        
        return result;
    }
}
```

## Testing

The framework includes comprehensive tests covering all components:

### Test Structure

```
tests/AIAgentSharp.Tests/
├── Agents/                    # Agent implementation tests
├── ToolTests/                 # Tool framework tests
├── StateStoreTests/           # State persistence tests
├── LlmTests/                  # LLM integration tests
├── EventTests/                # Event system tests
├── ConfigurationTests/        # Configuration tests
└── IntegrationTests/          # End-to-end tests
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Examples

```csharp
[TestClass]
public class AgentTests
{
    [TestMethod]
    public async Task StepAsync_NewAgent_CreatesInitialState()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.State);
        Assert.AreEqual("test-agent", result.State.AgentId);
        Assert.AreEqual("test goal", result.State.Goal);
        Assert.AreEqual(1, result.State.Turns.Count);
    }
}
```

## Performance Considerations

### Token Management

- **History Summarization**: Older turns are automatically summarized to reduce token usage
- **Tool Output Truncation**: Large tool outputs are truncated to prevent prompt bloat
- **Field Size Limits**: Configurable limits for thoughts, final output, and summaries

### Caching Strategies

- **Tool Result Caching**: Results are cached and reused when appropriate
- **Schema Caching**: Tool schemas are cached to avoid regeneration
- **State Caching**: Agent state is cached in memory during execution

### Async Operations

- **Non-blocking I/O**: All operations are async for better resource utilization
- **Cancellation Support**: All operations support cancellation tokens
- **Timeout Management**: Configurable timeouts for LLM and tool calls

### Memory Management

- **Disposable Resources**: Proper disposal of resources
- **Memory-efficient Collections**: Use of efficient data structures
- **Garbage Collection**: Minimized allocations in hot paths

## Error Handling

The framework provides comprehensive error handling:

### Exception Types

```csharp
// Tool validation errors
public class ToolValidationException : Exception
{
    public List<string> Missing { get; }
    public List<ToolValidationError> FieldErrors { get; }
}

// Tool execution errors
public class ToolExecutionException : Exception
{
    public string ToolName { get; }
    public Dictionary<string, object?> Parameters { get; }
}

// LLM communication errors
public class LlmCommunicationException : Exception
{
    public string Model { get; }
    public TimeSpan Timeout { get; }
}
```

### Error Handling Example

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
    foreach (var error in ex.FieldErrors)
    {
        Console.WriteLine($"Field '{error.Field}': {error.Message}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
catch (LlmCommunicationException ex)
{
    Console.WriteLine($"LLM communication failed: {ex.Message}");
}
```

### Recovery Strategies

- **Automatic Retry**: Failed tool calls are retried with exponential backoff
- **Graceful Degradation**: Agent continues execution even with partial failures
- **Error Propagation**: Detailed error information is preserved and propagated
- **State Recovery**: Agent state is preserved even during failures

## API Reference

### Core Interfaces

#### IAgent

```csharp
public interface IAgent
{
    Task<AgentResult> RunAsync(string agentId, string goal, IEnumerable<ITool> tools, CancellationToken ct = default);
    Task<AgentStepResult> StepAsync(string agentId, string goal, IEnumerable<ITool> tools, CancellationToken ct = default);
}
```

#### ITool

```csharp
public interface ITool
{
    string Name { get; }
    Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default);
}
```

#### ILlmClient

```csharp
public interface ILlmClient
{
    Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default);
}
```

#### IAgentStateStore

```csharp
public interface IAgentStateStore
{
    Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default);
    Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default);
}
```

### Key Classes

#### Agent

```csharp
public sealed class Agent : IAgent
{
    public Agent(ILlmClient llmClient, IAgentStateStore stateStore, ILogger? logger = null, AgentConfiguration? config = null);
    
    // Events
    public event EventHandler<AgentRunStartedEventArgs>? RunStarted;
    public event EventHandler<AgentRunCompletedEventArgs>? RunCompleted;
    public event EventHandler<AgentStepStartedEventArgs>? StepStarted;
    public event EventHandler<AgentStepCompletedEventArgs>? StepCompleted;
    public event EventHandler<AgentLlmCallStartedEventArgs>? LlmCallStarted;
    public event EventHandler<AgentLlmCallCompletedEventArgs>? LlmCallCompleted;
    public event EventHandler<AgentToolCallStartedEventArgs>? ToolCallStarted;
    public event EventHandler<AgentToolCallCompletedEventArgs>? ToolCallCompleted;
    public event EventHandler<AgentStatusEventArgs>? StatusUpdate;
}
```

#### BaseTool<TParams, TResult>

```csharp
public abstract class BaseTool<TParams, TResult> : ITool, IToolIntrospect, IFunctionSchemaProvider
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public object GetJsonSchema();
    public string Describe();
    public async Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default);
    
    protected abstract Task<TResult> InvokeTypedAsync(TParams parameters, CancellationToken ct = default);
}
```

#### AgentConfiguration

```csharp
public sealed class AgentConfiguration
{
    public int MaxTurns { get; init; } = 100;
    public int MaxRecentTurns { get; init; } = 10;
    public TimeSpan LlmTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan ToolTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public bool UseFunctionCalling { get; init; } = true;
    public bool EmitPublicStatus { get; init; } = true;
    public bool EnableHistorySummarization { get; init; } = true;
    public TimeSpan DedupeStalenessThreshold { get; init; } = TimeSpan.FromMinutes(5);
    public int MaxThoughtsLength { get; init; } = 20000;
    public int MaxFinalLength { get; init; } = 50000;
    public int MaxSummaryLength { get; init; } = 40000;
    public int MaxToolOutputSize { get; init; } = 2000;
    public int ConsecutiveFailureThreshold { get; init; } = 3;
    public int MaxToolCallHistory { get; init; } = 20;
}
```

### Models

#### AgentState

```csharp
public sealed class AgentState
{
    public string AgentId { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
    public List<AgentTurn> Turns { get; init; } = new();
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastModifiedUtc { get; init; } = DateTimeOffset.UtcNow;
}
```

#### AgentTurn

```csharp
public sealed class AgentTurn
{
    public int Index { get; init; }
    public string TurnId { get; init; } = Guid.NewGuid().ToString();
    public ModelMessage? LlmMessage { get; init; }
    public ToolCallRequest? ToolCall { get; init; }
    public ToolExecutionResult? ToolResult { get; init; }
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
```

#### ToolExecutionResult

```csharp
public sealed class ToolExecutionResult
{
    public string Tool { get; init; } = string.Empty;
    public bool Success { get; init; }
    public object? Output { get; init; }
    public string? Error { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public string TurnId { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
```

### Events

#### AgentRunStartedEventArgs

```csharp
public sealed class AgentRunStartedEventArgs : EventArgs
{
    public string AgentId { get; }
    public string Goal { get; }
    public DateTimeOffset StartedUtc { get; }
}
```

#### AgentStatusEventArgs

```csharp
public sealed class AgentStatusEventArgs : EventArgs
{
    public string AgentId { get; }
    public int TurnIndex { get; }
    public string StatusTitle { get; }
    public string? StatusDetails { get; }
    public string? NextStepHint { get; }
    public int? ProgressPct { get; }
    public DateTimeOffset Timestamp { get; }
}
```

## Contributing

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

### Code Style

- Follow C# coding conventions
- Use XML documentation for public APIs
- Write unit tests for new functionality
- Ensure all tests pass before submitting PR

### Pull Request Process

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Update documentation if needed
7. Submit a pull request

### Testing Guidelines

- Write unit tests for all new functionality
- Maintain test coverage above 90%
- Use descriptive test names
- Follow AAA pattern (Arrange, Act, Assert)
- Mock external dependencies

---

**AIAgentSharp** - Built with ❤️ for the .NET community

*This documentation covers the complete AIAgentSharp project, including all components, architecture, usage examples, and implementation details. For more information, see the source code and test files.*
