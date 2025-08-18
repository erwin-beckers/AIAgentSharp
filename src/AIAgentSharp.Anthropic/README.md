# AIAgentSharp.Anthropic

Anthropic Claude integration for AIAgentSharp - LLM-powered agents with Anthropic Claude models.

## Features

- **Anthropic Claude API Integration**: Full support for Anthropic's Claude API
- **Function Calling**: Native support for Anthropic function calling
- **Configurable Models**: Support for Claude 3.5 Sonnet, Claude 3 Opus, Claude 3 Haiku, and more
- **Advanced Configuration**: Comprehensive settings for temperature, tokens, and more
- **Enterprise Support**: Organization ID and custom endpoint support
- **Error Handling**: Robust error handling with retry logic
- **Logging**: Comprehensive logging for debugging and monitoring

## Installation

```bash
dotnet add package AIAgentSharp.Anthropic
```

## Quick Start

### Basic Usage

```csharp
using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Anthropic;
using AIAgentSharp.StateStores;

// Create Anthropic client
var llm = new AnthropicLlmClient("your-anthropic-api-key");

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
var config = AnthropicConfiguration.CreateForAgentReasoning();

// Or create custom configuration
var customConfig = new AnthropicConfiguration
{
    Model = "claude-3-5-sonnet-20241022",
    Temperature = 0.2f,
    MaxTokens = 6000,
    EnableFunctionCalling = true,
    MaxRetries = 5,
    RequestTimeout = TimeSpan.FromMinutes(3)
};

// Create client with configuration
var llm = new AnthropicLlmClient("your-anthropic-api-key", customConfig);
```

### Enterprise Usage

```csharp
var config = new AnthropicConfiguration
{
    Model = "claude-3-5-sonnet-20241022",
    OrganizationId = "org-your-org-id",
    ApiBaseUrl = "https://your-custom-endpoint.com/v1",
    EnableFunctionCalling = true
};

var llm = new AnthropicLlmClient("your-api-key", config);
```

## Configuration Options

### Model Selection

```csharp
// Cost-effective reasoning
var config = AnthropicConfiguration.CreateForCostEfficiency(); // Uses claude-3-haiku-20240307

// Balanced performance
var config = AnthropicConfiguration.CreateForAgentReasoning(); // Uses claude-3-5-sonnet-20241022

// Maximum capability
var config = AnthropicConfiguration.CreateForCreativeTasks(); // Uses claude-3-opus-20240229
```

### Temperature Settings

```csharp
var config = new AnthropicConfiguration
{
    Temperature = 0.0f,  // Most deterministic
    // Temperature = 0.1f,  // Good for reasoning (default)
    // Temperature = 0.7f,  // Balanced creativity
    // Temperature = 1.0f,  // More creative
};
```

### Token Management

```csharp
var config = new AnthropicConfiguration
{
    MaxTokens = 2000,    // Shorter responses, lower cost
    // MaxTokens = 4000,  // Default
    // MaxTokens = 8000,  // Longer responses, higher cost
};
```

## Function Calling

The Anthropic client supports native function calling:

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

## Error Handling

The client includes robust error handling:

```csharp
try
{
    var result = await agent.RunAsync("agent-id", "Hello", tools);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Anthropic API error: {ex.Message}");
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
var llm = new AnthropicLlmClient("your-api-key", logger: logger);

// Or use your own logger implementation
var customLogger = new MyCustomLogger();
var llm = new AnthropicLlmClient("your-api-key", logger: customLogger);
```

## Performance Optimization

### Cost Optimization

```csharp
var config = AnthropicConfiguration.CreateForCostEfficiency();
// - Uses claude-3-haiku-20240307 (cheaper)
// - Lower max tokens
// - Fewer retries
// - Shorter timeout
```

### Speed Optimization

```csharp
var config = new AnthropicConfiguration
{
    Model = "claude-3-haiku-20240307",  // Fastest model
    MaxTokens = 2000,                   // Shorter responses
    RequestTimeout = TimeSpan.FromMinutes(1),  // Shorter timeout
    MaxRetries = 2                      // Fewer retries
};
```

### Quality Optimization

```csharp
var config = new AnthropicConfiguration
{
    Model = "claude-3-opus-20240229",   // Highest quality
    Temperature = 0.0f,                 // Most deterministic
    MaxTokens = 8000,                   // Longer responses
    RequestTimeout = TimeSpan.FromMinutes(5)  // Longer timeout
};
```

## Supported Models

- **claude-3-opus-20240229**: Most capable model, best for complex reasoning
- **claude-3-5-sonnet-20241022**: Fast and cost-effective, good for most tasks
- **claude-3-sonnet-20240229**: Good balance of capability and cost
- **claude-3-haiku-20240307**: Fastest and most cost-effective model

## Dependencies

- **AIAgentSharp**: Core agent framework
- **Anthropic.SDK**: Official Anthropic .NET SDK

## License

This package is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
