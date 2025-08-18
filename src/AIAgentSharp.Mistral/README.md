# AIAgentSharp.Mistral

Mistral AI integration for AIAgentSharp - LLM-powered agents with Mistral AI models.

## Features

- **Mistral AI API Integration**: Full support for Mistral AI's chat completion API
- **Function Calling**: Native support for Mistral function calling with markdown JSON extraction
- **Configurable Models**: Support for Mistral Large, Mistral Medium, Mistral Small, and more
- **Advanced Configuration**: Comprehensive settings for temperature, tokens, and more
- **Enterprise Support**: Organization ID and custom endpoint support
- **Error Handling**: Robust error handling with retry logic
- **Logging**: Comprehensive logging for debugging and monitoring
- **Markdown JSON Parsing**: Intelligent extraction of JSON from markdown code blocks

## Installation

```bash
dotnet add package AIAgentSharp.Mistral
```

## Quick Start

### Basic Usage

```csharp
using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Mistral;
using AIAgentSharp.StateStores;

// Create Mistral client
var llm = new MistralLlmClient("your-mistral-api-key");

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
var config = MistralConfiguration.CreateForAgentReasoning();

// Or create custom configuration
var customConfig = new MistralConfiguration
{
    Model = "mistral-large-latest",
    Temperature = 0.2f,
    MaxTokens = 6000,
    EnableFunctionCalling = true,
    MaxRetries = 5,
    RequestTimeout = TimeSpan.FromMinutes(3)
};

// Create client with configuration
var llm = new MistralLlmClient("your-mistral-api-key", customConfig);
```

### Enterprise Usage

```csharp
var config = new MistralConfiguration
{
    Model = "mistral-large-latest",
    OrganizationId = "org-your-org-id",
    ApiBaseUrl = "https://your-custom-endpoint.com/v1",
    EnableFunctionCalling = true
};

var llm = new MistralLlmClient("your-api-key", config);
```

## Configuration Options

### Model Selection

```csharp
// Cost-effective reasoning
var config = MistralConfiguration.CreateForCostEfficiency(); // Uses mistral-small-latest

// Balanced performance
var config = MistralConfiguration.CreateForAgentReasoning(); // Uses mistral-large-latest

// Maximum capability
var config = MistralConfiguration.CreateForCreativeTasks(); // Uses mistral-large-latest
```

### Temperature Settings

```csharp
var config = new MistralConfiguration
{
    Temperature = 0.0f,  // Most deterministic
    // Temperature = 0.1f,  // Good for reasoning (default)
    // Temperature = 0.7f,  // Balanced creativity
    // Temperature = 1.0f,  // More creative
};
```

### Token Management

```csharp
var config = new MistralConfiguration
{
    MaxTokens = 2000,    // Shorter responses, lower cost
    // MaxTokens = 4000,  // Default
    // MaxTokens = 8000,  // Longer responses, higher cost
};
```

## Function Calling

The Mistral client supports function calling with intelligent markdown JSON parsing:

```csharp
// Create tools
var tools = new List<ITool>
{
    new WeatherTool(),
    new CalculatorTool()
};

// Function calling is automatically enabled when tools are provided
// The client can handle JSON responses wrapped in markdown code blocks
var result = await agent.RunAsync("agent-id", "What's 2+2 and the weather in NYC?", tools);
```

### Markdown JSON Parsing

The Mistral client includes robust JSON extraction from markdown responses:

```csharp
// Handles various markdown formats:
// ```json
// {
//   "action": "tool_call",
//   "tool_name": "search_flights",
//   "parameters": { ... }
// }
// ```

// Also handles incomplete markdown blocks:
// ```json
// {
//   "action": "tool_call",
//   "tool_name": "search_flights",
//   "parameters": { ... }
// }
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
    Console.WriteLine($"Mistral API error: {ex.Message}");
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
var llm = new MistralLlmClient("your-api-key", logger: logger);

// Or use your own logger implementation
var customLogger = new MyCustomLogger();
var llm = new MistralLlmClient("your-api-key", logger: customLogger);
```

## Performance Optimization

### Cost Optimization

```csharp
var config = MistralConfiguration.CreateForCostEfficiency();
// - Uses mistral-small-latest (cheaper)
// - Lower max tokens
// - Fewer retries
// - Shorter timeout
```

### Speed Optimization

```csharp
var config = new MistralConfiguration
{
    Model = "mistral-small-latest",  // Fastest model
    MaxTokens = 2000,                // Shorter responses
    RequestTimeout = TimeSpan.FromMinutes(1),  // Shorter timeout
    MaxRetries = 2                   // Fewer retries
};
```

### Quality Optimization

```csharp
var config = new MistralConfiguration
{
    Model = "mistral-large-latest",  // Highest quality
    Temperature = 0.0f,              // Most deterministic
    MaxTokens = 8000,                // Longer responses
    RequestTimeout = TimeSpan.FromMinutes(5)  // Longer timeout
};
```

## Supported Models

- **mistral-large-latest**: Most capable model, best for complex reasoning
- **mistral-medium-latest**: Good balance of capability and cost
- **mistral-small-latest**: Fastest and most cost-effective model

## Implementation Notes

This package uses direct HTTP API calls to Mistral AI since the official Mistral AI .NET SDK is not yet available on NuGet. The implementation includes:

- Direct HTTP client implementation
- JSON markdown parsing for function calls
- Comprehensive error handling
- Retry logic with exponential backoff
- Full support for all Mistral AI API features

## Dependencies

- **AIAgentSharp**: Core agent framework
- **System.Text.Json**: JSON serialization
- **System.Net.Http**: HTTP client functionality

## License

This package is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
