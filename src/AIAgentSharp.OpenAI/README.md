# AIAgentSharp.OpenAI

OpenAI integration for AIAgentSharp - LLM-powered agents with OpenAI models.

## Features

- **OpenAI API Integration**: Full support for OpenAI's chat completion API
- **Function Calling**: Native support for OpenAI function calling
- **Configurable Models**: Support for GPT-4o, GPT-4o-mini, GPT-3.5-turbo, and more
- **Advanced Configuration**: Comprehensive settings for temperature, tokens, penalties, and more
- **Enterprise Support**: Organization ID and custom endpoint support
- **Error Handling**: Robust error handling with retry logic
- **Logging**: Comprehensive logging for debugging and monitoring

## Installation

```bash
dotnet add package AIAgentSharp.OpenAI
```

## Quick Start

### Basic Usage

```csharp
using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.OpenAI;
using AIAgentSharp.StateStores;

// Create OpenAI client
var llm = new OpenAiLlmClient("your-openai-api-key");

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
var config = OpenAiConfiguration.CreateForAgentReasoning();

// Or create custom configuration
var customConfig = new OpenAiConfiguration
{
    Model = "gpt-4o",
    Temperature = 0.2f,
    MaxTokens = 6000,
    EnableFunctionCalling = true,
    MaxRetries = 5,
    RequestTimeout = TimeSpan.FromMinutes(3)
};

// Create client with configuration
var llm = new OpenAiLlmClient("your-openai-api-key", customConfig);
```

### Enterprise Usage

```csharp
var config = new OpenAiConfiguration
{
    Model = "gpt-4o",
    OrganizationId = "org-your-org-id",
    ApiBaseUrl = "https://your-custom-endpoint.com/v1",
    EnableFunctionCalling = true
};

var llm = new OpenAiLlmClient("your-api-key", config);
```

## Configuration Options

### Model Selection

```csharp
// Cost-effective reasoning
var config = OpenAiConfiguration.CreateForCostEfficiency(); // Uses gpt-3.5-turbo

// Balanced performance
var config = OpenAiConfiguration.CreateForAgentReasoning(); // Uses gpt-4o-mini

// Maximum capability
var config = OpenAiConfiguration.CreateForCreativeTasks(); // Uses gpt-4o
```

### Temperature Settings

```csharp
var config = new OpenAiConfiguration
{
    Temperature = 0.0f,  // Most deterministic
    // Temperature = 0.1f,  // Good for reasoning (default)
    // Temperature = 0.7f,  // Balanced creativity
    // Temperature = 1.0f,  // More creative
};
```

### Token Management

```csharp
var config = new OpenAiConfiguration
{
    MaxTokens = 2000,    // Shorter responses, lower cost
    // MaxTokens = 4000,  // Default
    // MaxTokens = 8000,  // Longer responses, higher cost
};
```

## Function Calling

The OpenAI client supports native function calling:

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
    Console.WriteLine($"OpenAI API error: {ex.Message}");
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
var llm = new OpenAiLlmClient("your-api-key", logger: logger);

// Or use your own logger implementation
var customLogger = new MyCustomLogger();
var llm = new OpenAiLlmClient("your-api-key", logger: customLogger);
```

## Performance Optimization

### Cost Optimization

```csharp
var config = OpenAiConfiguration.CreateForCostEfficiency();
// - Uses gpt-3.5-turbo (cheaper)
// - Lower max tokens
// - Fewer retries
// - Shorter timeout
```

### Speed Optimization

```csharp
var config = new OpenAiConfiguration
{
    Model = "gpt-4o-mini",  // Fastest model
    MaxTokens = 2000,       // Shorter responses
    RequestTimeout = TimeSpan.FromMinutes(1),  // Shorter timeout
    MaxRetries = 2          // Fewer retries
};
```

### Quality Optimization

```csharp
var config = new OpenAiConfiguration
{
    Model = "gpt-4o",       // Highest quality
    Temperature = 0.0f,     // Most deterministic
    MaxTokens = 8000,       // Longer responses
    RequestTimeout = TimeSpan.FromMinutes(5)  // Longer timeout
};
```

## Supported Models

- **gpt-4o**: Most capable model, best for complex reasoning
- **gpt-4o-mini**: Fast and cost-effective, good for most tasks
- **gpt-4-turbo**: Good balance of capability and cost
- **gpt-3.5-turbo**: Legacy model, still effective for simple tasks

## Dependencies

- **AIAgentSharp**: Core agent framework
- **OpenAI**: Official OpenAI .NET SDK

## License

This package is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
