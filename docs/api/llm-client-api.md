# LLM Client API Reference

This document provides comprehensive API reference for LLM client implementations in AIAgentSharp.

## Core Interfaces

### ILlmClient Interface

The main interface that all LLM clients must implement.

```csharp
public interface ILlmClient
{
    IAsyncEnumerable<LlmStreamingChunk> StreamAsync(LlmRequest request, CancellationToken ct = default);
    Task<LlmResponse> CallAsync(LlmRequest request, CancellationToken ct = default);
}
```

#### Methods

- **StreamAsync**: Returns streaming chunks for real-time processing
- **CallAsync**: Returns a complete response (uses StreamAsync internally)

## Request and Response Models

### LlmRequest Class

Represents a request to an LLM provider.

```csharp
public class LlmRequest
{
    public IEnumerable<LlmMessage> Messages { get; set; }
    public LlmResponseType ResponseType { get; set; }
    public List<FunctionSpec>? Functions { get; set; }
    public string? FunctionChoice { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public Dictionary<string, object?>? AdditionalOptions { get; set; }
}
```

#### Properties

- **Messages**: Conversation history to send to the LLM
- **ResponseType**: Expected response type (Text or FunctionCall)
- **Functions**: Available functions for function calling
- **FunctionChoice**: How the LLM should choose functions
- **Temperature**: Randomness in responses (0.0-1.0)
- **MaxTokens**: Maximum tokens in response
- **AdditionalOptions**: Provider-specific options

### LlmMessage Class

Represents a single message in the conversation.

```csharp
public class LlmMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public FunctionCall? FunctionCall { get; set; }
    public string? Name { get; set; }
}
```

#### Properties

- **Role**: Message role ("system", "user", "assistant", "function")
- **Content**: Text content of the message
- **FunctionCall**: Function call data (if applicable)
- **Name**: Function name (for function role messages)

### LlmResponse Class

Represents a complete LLM response.

```csharp
public class LlmResponse
{
    public string Content { get; set; } = "";
    public bool HasFunctionCall { get; set; }
    public FunctionCall? FunctionCall { get; set; }
    public LlmUsage? Usage { get; set; }
    public LlmResponseType ActualResponseType { get; set; }
    public Dictionary<string, object?>? AdditionalMetadata { get; set; }
}
```

### LlmStreamingChunk Class

Represents a single chunk in a streaming response.

```csharp
public class LlmStreamingChunk
{
    public string Content { get; set; } = "";
    public bool IsFinal { get; set; }
    public FunctionCall? FunctionCall { get; set; }
    public LlmUsage? Usage { get; set; }
    public LlmResponseType ActualResponseType { get; set; }
    public Dictionary<string, object?>? AdditionalMetadata { get; set; }
}
```

## Response Types

### LlmResponseType Enum

```csharp
public enum LlmResponseType
{
    Text,
    FunctionCall
}
```

## Usage Tracking

### LlmUsage Class

Tracks token usage and costs for LLM calls.

```csharp
public class LlmUsage
{
    public uint InputTokens { get; set; }
    public uint OutputTokens { get; set; }
    public uint TotalTokens { get; set; }
    public string Model { get; set; } = "";
    public decimal? Cost { get; set; }
    public Dictionary<string, object?>? AdditionalMetrics { get; set; }
}
```

## Implemented Clients

### OpenAILlmClient

Client for OpenAI API (GPT models).

```csharp
public class OpenAILlmClient : ILlmClient
{
    public OpenAILlmClient(string apiKey, string model = "gpt-4", string? baseUrl = null)
    public OpenAILlmClient(OpenAIClient client, string model = "gpt-4")
}
```

**Supported Models:**
- gpt-4, gpt-4-turbo, gpt-4o, gpt-4o-mini
- gpt-3.5-turbo

### AnthropicLlmClient

Client for Anthropic API (Claude models).

```csharp
public class AnthropicLlmClient : ILlmClient
{
    public AnthropicLlmClient(string apiKey, string model = "claude-3-sonnet-20240229")
}
```

**Supported Models:**
- claude-3-opus-20240229
- claude-3-sonnet-20240229
- claude-3-haiku-20240307

### GeminiLlmClient

Client for Google Gemini API.

```csharp
public class GeminiLlmClient : ILlmClient
{
    public GeminiLlmClient(string apiKey, string model = "gemini-pro")
}
```

**Supported Models:**
- gemini-pro
- gemini-pro-vision

### MistralLlmClient

Client for Mistral AI API.

```csharp
public class MistralLlmClient : ILlmClient
{
    public MistralLlmClient(string apiKey, string model = "mistral-medium")
}
```

**Supported Models:**
- mistral-tiny
- mistral-small
- mistral-medium
- mistral-large

## Function Calling

### FunctionCall Class

Represents a function call from the LLM.

```csharp
public class FunctionCall
{
    public string Name { get; set; } = "";
    public string Arguments { get; set; } = "";
    public Dictionary<string, object?>? ParsedArguments { get; set; }
}
```

### FunctionSpec Class

Defines available functions for the LLM.

```csharp
public class FunctionSpec
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
}
```

## Error Handling

All LLM clients should handle common error scenarios:

- **Authentication errors**: Invalid API keys
- **Rate limiting**: Too many requests
- **Network errors**: Connection issues
- **Model errors**: Invalid model names
- **Content filtering**: Blocked content

Example error handling:

```csharp
try
{
    var response = await client.CallAsync(request, cancellationToken);
    // Process response
}
catch (HttpRequestException ex)
{
    // Handle network errors
    _logger.LogError($"Network error: {ex.Message}");
}
catch (TaskCanceledException ex)
{
    // Handle timeouts
    _logger.LogError($"Request timed out: {ex.Message}");
}
catch (ArgumentException ex)
{
    // Handle invalid requests
    _logger.LogError($"Invalid request: {ex.Message}");
}
```

## Best Practices

1. **Use Streaming**: Prefer `StreamAsync` for better user experience
2. **Handle Cancellation**: Always respect cancellation tokens
3. **Log Appropriately**: Log errors but not sensitive data
4. **Retry Logic**: Implement retry for transient failures
5. **Rate Limiting**: Respect provider rate limits
6. **Monitor Usage**: Track token usage and costs

## Configuration Examples

### OpenAI Configuration

```csharp
var client = new OpenAILlmClient(
    apiKey: "your-api-key",
    model: "gpt-4o",
    baseUrl: "https://api.openai.com/v1" // Optional custom endpoint
);
```

### Anthropic Configuration

```csharp
var client = new AnthropicLlmClient(
    apiKey: "your-api-key",
    model: "claude-3-sonnet-20240229"
);
```

### Custom Headers and Options

```csharp
var request = new LlmRequest
{
    Messages = messages,
    Temperature = 0.7,
    MaxTokens = 1000,
    AdditionalOptions = new Dictionary<string, object?>
    {
        ["custom_header"] = "value",
        ["provider_specific_option"] = true
    }
};
```

## See Also

- [LLM Integration](../llm-integration.md) - Overview of LLM integration
- [Agent Framework](../agent-framework.md) - How agents use LLM clients
- [Configuration](../configuration.md) - Configuration options

