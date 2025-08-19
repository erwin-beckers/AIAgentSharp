# LLM Integration

AIAgentSharp provides seamless integration with multiple Large Language Model (LLM) providers, allowing you to choose the best model for your specific use case.

## Supported Providers

AIAgentSharp supports the following LLM providers:

| Provider | Package | Client Class | Models |
|----------|---------|--------------|--------|
| **OpenAI** | `AIAgentSharp.OpenAI` | `OpenAiLlmClient` | GPT-4, GPT-3.5-turbo, GPT-4-turbo |
| **Anthropic** | `AIAgentSharp.Anthropic` | `AnthropicLlmClient` | Claude-3, Claude-2, Claude-instant |
| **Google** | `AIAgentSharp.Gemini` | `GeminiLlmClient` | Gemini Pro, Gemini Flash |
| **Mistral AI** | `AIAgentSharp.Mistral` | `MistralLlmClient` | Mistral Large, Mistral Medium, Mistral Small |

## Basic Usage

### OpenAI Integration

```csharp
using AIAgentSharp.OpenAI;

// Create OpenAI client
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
var llm = new OpenAiLlmClient(apiKey);

// Use with agent
var agent = new Agent(llm, store);
```

### Anthropic Claude Integration

```csharp
using AIAgentSharp.Anthropic;

// Create Anthropic client
var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!;
var llm = new AnthropicLlmClient(apiKey);

// Use with agent
var agent = new Agent(llm, store);
```

### Google Gemini Integration

```csharp
using AIAgentSharp.Gemini;

// Create Google client
var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY")!;
var llm = new GeminiLlmClient(apiKey);

// Use with agent
var agent = new Agent(llm, store);
```

### Mistral AI Integration

```csharp
using AIAgentSharp.Mistral;

// Create Mistral client
var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY")!;
var llm = new MistralLlmClient(apiKey);

// Use with agent
var agent = new Agent(llm, store);
```

## Model Configuration

### OpenAI Models

```csharp
var llm = new OpenAiLlmClient(apiKey, new OpenAiConfiguration
{
    Model = "gpt-4",                    // Model to use
    MaxTokens = 4000,                   // Maximum response tokens
    Temperature = 0.7,                  // Creativity (0.0-1.0)
    TopP = 0.9,                         // Nucleus sampling
    FrequencyPenalty = 0.0,             // Frequency penalty
    PresencePenalty = 0.0,              // Presence penalty
    Timeout = TimeSpan.FromSeconds(30)  // Request timeout
});
```

### Anthropic Models

```csharp
var llm = new AnthropicLlmClient(apiKey, new AnthropicConfiguration
{
    Model = "claude-3-sonnet-20240229", // Model to use
    MaxTokens = 4000,                   // Maximum response tokens
    Temperature = 0.7,                  // Creativity (0.0-1.0)
    TopP = 0.9,                         // Nucleus sampling
    Timeout = TimeSpan.FromSeconds(30)  // Request timeout
});
```

### Google Models

```csharp
var llm = new GeminiLlmClient(apiKey, new GeminiConfiguration
{
    Model = "gemini-pro",               // Model to use
    MaxTokens = 4000,                   // Maximum response tokens
    Temperature = 0.7,                  // Creativity (0.0-1.0)
    TopP = 0.9,                         // Nucleus sampling
    TopK = 40,                          // Top-k sampling
    Timeout = TimeSpan.FromSeconds(30)  // Request timeout
});
```

### Mistral Models

```csharp
var llm = new MistralLlmClient(apiKey, new MistralConfiguration
{
    Model = "mistral-large-latest",     // Model to use
    MaxTokens = 4000,                   // Maximum response tokens
    Temperature = 0.7,                  // Creativity (0.0-1.0)
    TopP = 0.9,                         // Nucleus sampling
    Timeout = TimeSpan.FromSeconds(30)  // Request timeout
});
```

## Streaming Support

All LLM clients support streaming for real-time responses:

```csharp
// Subscribe to streaming events
agent.LlmChunkReceived += (sender, e) =>
{
    Console.Write(e.Chunk.Content); // Print chunks as they arrive
};

// Run agent with streaming
var result = await agent.RunAsync("streaming-agent", "Your goal", tools);
```

## Custom LLM Client

Create a custom LLM client by implementing `ILlmClient`:

```csharp
public class CustomLlmClient : ILlmClient
{
    public async Task<LlmResponse> CallAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        // Your custom LLM implementation here
        
        return new LlmResponse
        {
            Content = "Custom LLM response",
            Usage = new LlmUsage
            {
                PromptTokens = 100,
                CompletionTokens = 50,
                TotalTokens = 150
            }
        };
    }
    
    public IAsyncEnumerable<LlmStreamingChunk> StreamAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        // Your custom streaming implementation here
        
        return new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Custom", IsFinal = false },
            new LlmStreamingChunk { Content = " streaming", IsFinal = false },
            new LlmStreamingChunk { Content = " response", IsFinal = true }
        }.ToAsyncEnumerable();
    }
}

// Use custom client
var customLlm = new CustomLlmClient();
var agent = new Agent(customLlm, store);
```

## Provider-Specific Features

### OpenAI Function Calling

OpenAI supports native function calling:

```csharp
var config = new AgentConfiguration
{
    UseFunctionCalling = true,  // Enable OpenAI function calling
    ReasoningType = ReasoningType.None
};

var agent = new Agent(llm, store, config: config);
```

### Anthropic Tool Use

Anthropic Claude supports tool use:

```csharp
var config = new AgentConfiguration
{
    UseFunctionCalling = true,  // Enable Anthropic tool use
    ReasoningType = ReasoningType.None
};

var agent = new Agent(llm, store, config: config);
```

## Error Handling

Handle provider-specific errors:

```csharp
try
{
    var result = await agent.RunAsync("test-agent", "Your goal", tools);
}
catch (OpenAiException ex)
{
    Console.WriteLine($"OpenAI error: {ex.Message}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
}
catch (AnthropicException ex)
{
    Console.WriteLine($"Anthropic error: {ex.Message}");
    Console.WriteLine($"Error type: {ex.ErrorType}");
}
catch (GeminiException ex)
{
    Console.WriteLine($"Google error: {ex.Message}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
}
catch (MistralException ex)
{
    Console.WriteLine($"Mistral error: {ex.Message}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
}
```

## Performance Optimization

### Model Selection

Choose the right model for your use case:

```csharp
// For speed (lower cost)
var fastLlm = new OpenAiLlmClient(apiKey, new OpenAiConfiguration
{
    Model = "gpt-3.5-turbo",
    MaxTokens = 1000
});

// For quality (higher cost)
var qualityLlm = new OpenAiLlmClient(apiKey, new OpenAiConfiguration
{
    Model = "gpt-4",
    MaxTokens = 4000
});
```

### Caching

Implement caching for repeated requests:

```csharp
public class CachedLlmClient : ILlmClient
{
    private readonly ILlmClient _innerClient;
    private readonly IMemoryCache _cache;
    
    public CachedLlmClient(ILlmClient innerClient, IMemoryCache cache)
    {
        _innerClient = innerClient;
        _cache = cache;
    }
    
    public async Task<LlmResponse> CallAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(request);
        
        if (_cache.TryGetValue(cacheKey, out LlmResponse? cachedResponse))
        {
            return cachedResponse!;
        }
        
        var response = await _innerClient.CallAsync(request, cancellationToken);
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(30));
        
        return response;
    }
    
    private string GenerateCacheKey(LlmRequest request)
    {
        // Generate cache key based on request content
        return $"llm_{request.Messages.Count}_{request.MaxTokens}_{request.Temperature}";
    }
}
```

## Rate Limiting

Handle rate limits gracefully:

```csharp
public class RateLimitedLlmClient : ILlmClient
{
    private readonly ILlmClient _innerClient;
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<DateTime> _requestTimes;
    private readonly int _maxRequestsPerMinute;
    
    public RateLimitedLlmClient(ILlmClient innerClient, int maxRequestsPerMinute = 60)
    {
        _innerClient = innerClient;
        _semaphore = new SemaphoreSlim(1, 1);
        _requestTimes = new Queue<DateTime>();
        _maxRequestsPerMinute = maxRequestsPerMinute;
    }
    
    public async Task<LlmResponse> CallAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Check rate limit
            var now = DateTime.UtcNow;
            while (_requestTimes.Count > 0 && now - _requestTimes.Peek() > TimeSpan.FromMinutes(1))
            {
                _requestTimes.Dequeue();
            }
            
            if (_requestTimes.Count >= _maxRequestsPerMinute)
            {
                var oldestRequest = _requestTimes.Peek();
                var waitTime = TimeSpan.FromMinutes(1) - (now - oldestRequest);
                await Task.Delay(waitTime, cancellationToken);
            }
            
            _requestTimes.Enqueue(now);
            return await _innerClient.CallAsync(request, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

## Best Practices

### 1. Model Selection

```csharp
// For simple Q&A
var simpleLlm = new OpenAiLlmClient(apiKey, new OpenAiConfiguration
{
    Model = "gpt-3.5-turbo",
    Temperature = 0.3
});

// For creative tasks
var creativeLlm = new OpenAiLlmClient(apiKey, new OpenAiConfiguration
{
    Model = "gpt-4",
    Temperature = 0.8
});

// For reasoning tasks
var reasoningLlm = new AnthropicLlmClient(apiKey, new AnthropicConfiguration
{
    Model = "claude-3-sonnet-20240229",
    Temperature = 0.1
});
```

### 2. Error Handling

```csharp
// Implement retry logic
public async Task<LlmResponse> CallWithRetryAsync(LlmRequest request, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await _llmClient.CallAsync(request);
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // Exponential backoff
        }
    }
    
    throw new Exception("Max retries exceeded");
}
```

### 3. Cost Optimization

```csharp
// Monitor token usage
agent.LlmCallCompleted += (sender, e) =>
{
    var usage = e.Usage;
    Console.WriteLine($"Tokens used: {usage.TotalTokens}");
    Console.WriteLine($"Estimated cost: ${CalculateCost(usage)}");
};

private decimal CalculateCost(LlmUsage usage)
{
    // Calculate cost based on your provider's pricing
    return usage.PromptTokens * 0.00001m + usage.CompletionTokens * 0.00003m;
}
```

## Troubleshooting

### Common Issues

**API Key Issues**: Ensure your API key is valid and has sufficient credits.

**Rate Limits**: Implement retry logic with exponential backoff.

**Model Availability**: Check if your chosen model is available in your region.

**Network Issues**: Verify internet connectivity and firewall settings.

### Provider-Specific Issues

**OpenAI**: Check API key permissions and model access.

**Anthropic**: Verify Claude API access and model availability.

**Google**: Ensure API key has Gemini API enabled.

**Mistral**: Check API key validity and model access.

For more troubleshooting help, see the [Troubleshooting Guide](troubleshooting/common-issues.md).
