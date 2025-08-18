# AIAgentSharp Multi-Provider Architecture

## Overview

AIAgentSharp is designed with a clean separation between the core framework and LLM provider implementations. This architecture allows you to easily switch between different LLM providers or implement your own custom provider while maintaining the same agent functionality.

## Architecture Principles

### 1. Provider Agnostic Core
The core framework (`AIAgentSharp`) contains no provider-specific code. All LLM interactions are abstracted through the `ILlmClient` interface.

### 2. Pluggable Provider Implementations
Each LLM provider is implemented as a separate NuGet package, allowing you to:
- Install only the providers you need
- Reduce package size and dependencies
- Maintain clean separation of concerns
- Add new providers without modifying the core framework

### 3. Consistent Interface
All provider implementations implement the same `ILlmClient` interface, ensuring:
- Consistent API across all providers
- Easy switching between providers
- Predictable behavior regardless of the underlying LLM

## Available Providers

### 1. AIAgentSharp.OpenAI
**Package**: `AIAgentSharp.OpenAI`  
**Provider**: OpenAI  
**Models**: GPT-4, GPT-3.5, GPT-4o, GPT-4o-mini  
**Features**: Function calling, streaming, organization support

```csharp
using AIAgentSharp.OpenAI;

var llm = new OpenAiLlmClient(apiKey);
var config = new OpenAiConfiguration
{
    Model = "gpt-4o-mini",
    Temperature = 0.1f,
    MaxTokens = 4000
};
var llm = new OpenAiLlmClient(apiKey, config);
```

### 2. AIAgentSharp.Anthropic
**Package**: `AIAgentSharp.Anthropic`  
**Provider**: Anthropic  
**Models**: Claude 3.5 Sonnet, Claude 3.5 Haiku, Claude 3 Opus  
**Features**: Function calling via tools, streaming, organization support

```csharp
using AIAgentSharp.Anthropic;

var llm = new AnthropicLlmClient(apiKey);
var config = new AnthropicConfiguration
{
    Model = "claude-3-5-sonnet-20241022",
    Temperature = 0.1f,
    MaxTokens = 4000
};
var llm = new AnthropicLlmClient(apiKey, config);
```

### 3. AIAgentSharp.Gemini
**Package**: `AIAgentSharp.Gemini`  
**Provider**: Google AI Platform  
**Models**: Gemini 1.5 Flash, Gemini 1.5 Pro, Gemini 1.0 Pro  
**Features**: Function calling, Google Cloud integration

```csharp
using AIAgentSharp.Gemini;

var llm = new GeminiLlmClient(apiKey);
var config = new GeminiConfiguration
{
    Model = "gemini-1.5-flash",
    Temperature = 0.1f,
    MaxTokens = 4000,
    ProjectId = "your-project-id",
    Region = "us-central1"
};
var llm = new GeminiLlmClient(apiKey, config);
```

### 4. AIAgentSharp.Mistral
**Package**: `AIAgentSharp.Mistral`  
**Provider**: Mistral AI  
**Models**: Mistral Large, Mistral Medium, Mistral Small  
**Features**: Function calling via tools, streaming, organization support

```csharp
using AIAgentSharp.Mistral;

var llm = new MistralLlmClient(apiKey);
var config = new MistralConfiguration
{
    Model = "mistral-large-latest",
    Temperature = 0.1f,
    MaxTokens = 4000
};
var llm = new MistralLlmClient(apiKey, config);
```

## Core Interface

### ILlmClient Interface

All providers implement the `ILlmClient` interface:

```csharp
public interface ILlmClient
{
    Task<LlmCompletionResult> CompleteAsync(
        IEnumerable<LlmMessage> messages, 
        CancellationToken ct = default);
    
    Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages,
        IEnumerable<OpenAiFunctionSpec> functions,
        CancellationToken ct = default);
}
```

### Message Format

All providers use the same message format:

```csharp
public sealed class LlmMessage
{
    public string Role { get; set; } = string.Empty; // "system", "user", "assistant"
    public string Content { get; set; } = string.Empty;
}
```

### Function Calling

All providers support OpenAI-style function calling:

```csharp
public sealed class OpenAiFunctionSpec
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public object ParametersSchema { get; init; } = new { };
}
```

## Provider-Specific Features

### OpenAI (AIAgentSharp.OpenAI)
- **Function Calling**: Native OpenAI function calling support
- **Organization Support**: Multi-tenant organization management
- **Custom Endpoints**: Support for OpenAI-compatible APIs
- **Advanced Parameters**: Frequency penalty, presence penalty

### Anthropic (AIAgentSharp.Anthropic)
- **Tool Calling**: Anthropic's tool-based function calling
- **System Message Handling**: Automatic conversion of system messages to user messages
- **Organization Support**: Enterprise organization management
- **Top-K Parameter**: Additional sampling parameter

### Gemini (AIAgentSharp.Gemini)
- **Google Cloud Integration**: Native Google AI Platform integration
- **Project Management**: Google Cloud project-based access control
- **Regional Deployment**: Support for different Google Cloud regions
- **API Key Authentication**: Simple API key-based authentication

### Mistral (AIAgentSharp.Mistral)
- **Tool Calling**: Mistral's tool-based function calling
- **System Message Support**: Native system message support
- **Organization Support**: Enterprise organization management
- **Top-K Parameter**: Additional sampling parameter

## Configuration Comparison

| Feature | OpenAI | Anthropic | Gemini | Mistral |
|---------|--------|-----------|--------|---------|
| Default Model | gpt-4o-mini | claude-3-5-sonnet-20241022 | gemini-1.5-flash | mistral-large-latest |
| Function Calling | ✅ Native | ✅ Tools | ✅ Native | ✅ Tools |
| System Messages | ✅ Native | 🔄 Converted | 🔄 Converted | ✅ Native |
| Streaming | ✅ | ✅ | ✅ | ✅ |
| Organization Support | ✅ | ✅ | ❌ | ✅ |
| Custom Endpoints | ✅ | ✅ | ✅ | ✅ |
| Top-K Parameter | ❌ | ✅ | ✅ | ✅ |
| Frequency Penalty | ✅ | ❌ | ❌ | ❌ |
| Presence Penalty | ✅ | ❌ | ❌ | ❌ |

## Installation

### Core Framework
```bash
dotnet add package AIAgentSharp
```

### Provider Packages
```bash
# OpenAI
dotnet add package AIAgentSharp.OpenAI

# Anthropic
dotnet add package AIAgentSharp.Anthropic

# Gemini
dotnet add package AIAgentSharp.Gemini

# Mistral
dotnet add package AIAgentSharp.Mistral
```

## Usage Examples

### Basic Usage with Any Provider

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;

// Choose your provider
using AIAgentSharp.OpenAI;      // or
using AIAgentSharp.Anthropic;   // or
using AIAgentSharp.Gemini;      // or
using AIAgentSharp.Mistral;

// Create components
var llm = new OpenAiLlmClient(apiKey); // or AnthropicLlmClient, etc.
var store = new MemoryAgentStateStore();
var agent = new Agent(llm, store);

// Use the agent (same API regardless of provider)
var result = await agent.RunAsync("my-agent", "Hello, how are you?", new List<ITool>());
Console.WriteLine(result.FinalOutput);
```

### Switching Between Providers

```csharp
// Easy to switch between providers
ILlmClient llm;

// OpenAI
llm = new OpenAiLlmClient(openaiApiKey);

// Anthropic
llm = new AnthropicLlmClient(anthropicApiKey);

// Gemini
llm = new GeminiLlmClient(geminiApiKey);

// Mistral
llm = new MistralLlmClient(mistralApiKey);

// Use the same agent with any provider
var agent = new Agent(llm, store);
```

### Provider-Specific Configuration

```csharp
// OpenAI with custom configuration
var openaiConfig = new OpenAiConfiguration
{
    Model = "gpt-4o",
    Temperature = 0.7f,
    MaxTokens = 6000,
    FrequencyPenalty = 0.1f,
    PresencePenalty = 0.1f
};
var openaiLlm = new OpenAiLlmClient(apiKey, openaiConfig);

// Anthropic with custom configuration
var anthropicConfig = new AnthropicConfiguration
{
    Model = "claude-3-opus-20240229",
    Temperature = 0.7f,
    MaxTokens = 6000,
    TopK = 40
};
var anthropicLlm = new AnthropicLlmClient(apiKey, anthropicConfig);

// Gemini with custom configuration
var geminiConfig = new GeminiConfiguration
{
    Model = "gemini-1.5-pro",
    Temperature = 0.7f,
    MaxTokens = 6000,
    ProjectId = "my-project",
    Region = "us-west1"
};
var geminiLlm = new GeminiLlmClient(apiKey, geminiConfig);

// Mistral with custom configuration
var mistralConfig = new MistralConfiguration
{
    Model = "mistral-large-latest",
    Temperature = 0.7f,
    MaxTokens = 6000,
    TopK = 40
};
var mistralLlm = new MistralLlmClient(apiKey, mistralConfig);
```

## Creating Custom Providers

You can implement your own LLM provider by implementing the `ILlmClient` interface:

```csharp
public class MyCustomLlmClient : ILlmClient
{
    private readonly string _apiKey;
    private readonly string _model;
    
    public MyCustomLlmClient(string apiKey, string model = "my-model")
    {
        _apiKey = apiKey;
        _model = model;
    }
    
    public async Task<LlmCompletionResult> CompleteAsync(
        IEnumerable<LlmMessage> messages, 
        CancellationToken ct = default)
    {
        // Convert messages to your provider's format
        var providerMessages = ConvertToProviderFormat(messages);
        
        // Make API call to your provider
        var response = await CallProviderApi(providerMessages, ct);
        
        // Convert response to AIAgentSharp format
        return new LlmCompletionResult
        {
            Content = response.Content,
            Usage = new LlmUsage
            {
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens,
                Model = _model,
                Provider = "MyCustomProvider"
            }
        };
    }
    
    public async Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages,
        IEnumerable<OpenAiFunctionSpec> functions,
        CancellationToken ct = default)
    {
        // Implement function calling for your provider
        // Return appropriate FunctionCallResult
        throw new NotSupportedException("Function calling not supported by this provider");
    }
    
    private object ConvertToProviderFormat(IEnumerable<LlmMessage> messages)
    {
        // Convert AIAgentSharp messages to your provider's format
        return new { /* your provider's message format */ };
    }
    
    private async Task<object> CallProviderApi(object messages, CancellationToken ct)
    {
        // Make actual API call to your provider
        return new { /* your provider's response format */ };
    }
}

// Usage
var llm = new MyCustomLlmClient(apiKey);
var agent = new Agent(llm, store);
```

## Best Practices

### 1. Choose the Right Provider
- **OpenAI**: Best for general-purpose tasks with strong function calling
- **Anthropic**: Excellent for reasoning and analysis tasks
- **Gemini**: Good for Google Cloud integration and cost efficiency
- **Mistral**: Great for European compliance and open-source models

### 2. Configuration Management
```csharp
// Use factory methods for common use cases
var config = OpenAiConfiguration.CreateForAgentReasoning();
var llm = new OpenAiLlmClient(apiKey, config);

// Or create custom configurations
var customConfig = new AnthropicConfiguration
{
    Model = "claude-3-5-sonnet-20241022",
    Temperature = 0.1f,
    MaxTokens = 4000,
    EnableFunctionCalling = true
};
```

### 3. Error Handling
```csharp
try
{
    var result = await agent.RunAsync("my-agent", prompt, tools);
    Console.WriteLine(result.FinalOutput);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("API"))
{
    // Handle provider-specific API errors
    Console.WriteLine("LLM API error: " + ex.Message);
}
catch (OperationCanceledException)
{
    // Handle timeouts and cancellations
    Console.WriteLine("Operation was cancelled or timed out");
}
```

### 4. Testing
```csharp
// Use DelegateLlmClient for testing
var mockLlm = new DelegateLlmClient((messages, ct) => 
    Task.FromResult("Mock response"));
var agent = new Agent(mockLlm, store);
```

## Migration Guide

### From OpenAI to Other Providers

1. **Install the new provider package**:
   ```bash
   dotnet add package AIAgentSharp.Anthropic
   ```

2. **Update using statements**:
   ```csharp
   // Remove
   using AIAgentSharp.OpenAI;
   
   // Add
   using AIAgentSharp.Anthropic;
   ```

3. **Update client instantiation**:
   ```csharp
   // Before
   var llm = new OpenAiLlmClient(apiKey);
   
   // After
   var llm = new AnthropicLlmClient(apiKey);
   ```

4. **Update configuration (if needed)**:
   ```csharp
   // Before
   var config = new OpenAiConfiguration { Model = "gpt-4o-mini" };
   
   // After
   var config = new AnthropicConfiguration { Model = "claude-3-5-sonnet-20241022" };
   ```

The rest of your code remains the same!

## Troubleshooting

### Common Issues

1. **Function Calling Not Working**
   - Ensure the provider supports function calling
   - Check that `EnableFunctionCalling` is set to `true`
   - Verify function schemas are correctly formatted

2. **System Messages Not Working**
   - Anthropic and Gemini convert system messages to user messages
   - Mistral supports system messages natively
   - Check provider documentation for specific behavior

3. **API Key Issues**
   - Verify API keys are valid and have sufficient credits
   - Check provider-specific authentication requirements
   - Ensure proper environment variable setup

4. **Rate Limiting**
   - Implement retry logic with exponential backoff
   - Use appropriate `MaxRetries` and `RetryDelay` settings
   - Consider using different models for different use cases

### Provider-Specific Issues

- **OpenAI**: Check organization settings and API key permissions
- **Anthropic**: Verify Claude model availability and API access
- **Gemini**: Ensure Google Cloud project setup and API enablement
- **Mistral**: Check API key permissions and model availability

## Conclusion

AIAgentSharp's multi-provider architecture provides flexibility and choice while maintaining a consistent API. You can easily switch between providers, implement custom providers, and leverage provider-specific features as needed. The clean separation ensures that your agent logic remains provider-agnostic while allowing you to optimize for specific use cases and requirements.
