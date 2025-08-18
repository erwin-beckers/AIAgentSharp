# AIAgentSharp.Gemini

Google Gemini integration for AIAgentSharp - LLM-powered agents with Google Gemini models.

## Features

- **Google Gemini API Integration**: Full support for Google's Gemini API
- **Function Calling**: Native support for Gemini function calling
- **Configurable Models**: Support for Gemini 1.5 Pro, Gemini 1.5 Flash, Gemini 1.0 Pro, and more
- **Advanced Configuration**: Comprehensive settings for temperature, tokens, and more
- **Enterprise Support**: Custom endpoint support
- **Error Handling**: Robust error handling with retry logic
- **Logging**: Comprehensive logging for debugging and monitoring
- **Multi-modal Support**: Support for text and image inputs

## Installation

```bash
dotnet add package AIAgentSharp.Gemini
```

## Quick Start

### Basic Usage

```csharp
using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Gemini;
using AIAgentSharp.StateStores;

// Create Gemini client
var llm = new GeminiLlmClient("your-gemini-api-key");

// Create agent
var store = new MemoryAgentStateStore();
var agent = new Agent(llm, store);

// Run agent
var result = await agent.RunAsync("my-agent", "Hello, how are you?", new List<ITool>());
Console.WriteLine(result.FinalOutput);
```

### Advanced Configuration

```csharp
// Create optimized configuration for agent reasoning
var config = GeminiConfiguration.CreateForAgentReasoning();

// Or create custom configuration
var customConfig = new GeminiConfiguration
{
    Model = "gemini-1.5-pro",
    Temperature = 0.2f,
    MaxTokens = 6000,
    EnableFunctionCalling = true,
    MaxRetries = 5,
    RequestTimeout = TimeSpan.FromMinutes(3)
};

// Create client with configuration
var llm = new GeminiLlmClient("your-gemini-api-key", customConfig);
```

### Enterprise Usage

```csharp
var config = new GeminiConfiguration
{
    Model = "gemini-1.5-pro",
    ApiBaseUrl = "https://your-custom-endpoint.com/v1",
    EnableFunctionCalling = true
};

var llm = new GeminiLlmClient("your-api-key", config);
```

## Configuration Options

### Model Selection

```csharp
// Cost-effective reasoning
var config = GeminiConfiguration.CreateForCostEfficiency(); // Uses gemini-1.5-flash

// Balanced performance
var config = GeminiConfiguration.CreateForAgentReasoning(); // Uses gemini-1.5-pro

// Maximum capability
var config = GeminiConfiguration.CreateForCreativeTasks(); // Uses gemini-1.5-pro
```

### Temperature Settings

```csharp
var config = new GeminiConfiguration
{
    Temperature = 0.0f,  // Most deterministic
    // Temperature = 0.1f,  // Good for reasoning (default)
    // Temperature = 0.7f,  // Balanced creativity
    // Temperature = 1.0f,  // More creative
};
```

### Token Management

```csharp
var config = new GeminiConfiguration
{
    MaxTokens = 2000,    // Shorter responses, lower cost
    // MaxTokens = 4000,  // Default
    // MaxTokens = 8000,  // Longer responses, higher cost
};
```

## Function Calling

The Gemini client supports native function calling:

```csharp
// Create tools
var tools = new List<ITool>
{
    new WeatherTool(),
    new CalculatorTool()
};

// Function calling is automatically enabled when tools are provided
var result = await agent.RunAsync("agent-id", "What's 2+2 and the weather in NYC?", tools);
```

## Multi-modal Support

The Gemini client supports both text and image inputs:

```csharp
// Text-only conversation
var result = await agent.RunAsync("agent-id", "Describe this image", tools);

// Multi-modal conversation (when supported by the model)
// Note: Multi-modal support depends on the specific Gemini model used
```

## Error Handling

The client includes robust error handling:

```csharp
try
{
    var result = await agent.RunAsync("agent-id", "Hello", tools);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Gemini API error: {ex.Message}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request was cancelled or timed out");
}
```

## Logging

Enable detailed logging for debugging:

```csharp
var logger = new ConsoleLogger();
var llm = new GeminiLlmClient("your-api-key", logger: logger);

// Or use your own logger implementation
var customLogger = new MyCustomLogger();
var llm = new GeminiLlmClient("your-api-key", logger: customLogger);
```

## Performance Optimization

### Cost Optimization

```csharp
var config = GeminiConfiguration.CreateForCostEfficiency();
// - Uses gemini-1.5-flash (cheaper)
// - Lower max tokens
// - Fewer retries
// - Shorter timeout
```

### Speed Optimization

```csharp
var config = new GeminiConfiguration
{
    Model = "gemini-1.5-flash",  // Fastest model
    MaxTokens = 2000,            // Shorter responses
    RequestTimeout = TimeSpan.FromMinutes(1),  // Shorter timeout
    MaxRetries = 2               // Fewer retries
};
```

### Quality Optimization

```csharp
var config = new GeminiConfiguration
{
    Model = "gemini-1.5-pro",    // Highest quality
    Temperature = 0.0f,          // Most deterministic
    MaxTokens = 8000,            // Longer responses
    RequestTimeout = TimeSpan.FromMinutes(5)  // Longer timeout
};
```

## Supported Models

- **gemini-1.5-pro**: Most capable model, best for complex reasoning and multi-modal tasks
- **gemini-1.5-flash**: Fast and cost-effective, good for most tasks
- **gemini-1.0-pro**: Legacy model, still effective for simple tasks

## Implementation Notes

This package uses direct HTTP API calls to Google's Gemini API. The implementation includes:

- Direct HTTP client implementation
- JSON parsing for function calls
- Comprehensive error handling
- Retry logic with exponential backoff
- Full support for all Gemini API features

## Dependencies

- **AIAgentSharp**: Core agent framework
- **System.Text.Json**: JSON serialization
- **System.Net.Http**: HTTP client functionality

## License

This package is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
