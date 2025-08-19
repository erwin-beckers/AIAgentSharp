# Performance Optimization

This guide provides best practices for optimizing AIAgentSharp agent performance in production environments.

## Overview

Performance optimization in AIAgentSharp involves:
- **Token management** and cost optimization
- **Response time** optimization
- **Memory usage** management
- **Concurrent execution** strategies
- **Caching** and state management
- **Monitoring** and profiling

## Token Management

### Optimize Prompts

Reduce token usage by optimizing prompts:

```csharp
// ❌ Verbose prompt
var verbosePrompt = @"
You are a helpful AI assistant. Please analyze the following complex problem step by step.
Consider all possible angles and provide a comprehensive solution with detailed explanations.
The problem is: " + problem;

// ✅ Optimized prompt
var optimizedPrompt = "Solve: " + problem;
```

### Use History Summarization

Enable history summarization to manage context length:

```csharp
var config = new AgentConfiguration
{
    MaxHistoryLength = 20,
    EnableHistorySummarization = true,
    HistorySummarizationThreshold = 10
};
```

### Implement Token Counting

Monitor token usage:

```csharp
agent.LlmChunkReceived += (sender, e) =>
{
    var tokenCount = e.Chunk.Content?.Length ?? 0;
    Console.WriteLine($"Received {tokenCount} characters");
};
```

## Response Time Optimization

### Configure Timeouts

Set appropriate timeouts:

```csharp
var config = new AgentConfiguration
{
    LlmTimeout = TimeSpan.FromSeconds(30),
    ToolTimeout = TimeSpan.FromSeconds(10),
    TotalTimeout = TimeSpan.FromMinutes(2)
};
```

### Use Streaming

Enable streaming for faster initial responses:

```csharp
// Streaming provides faster perceived response times
agent.LlmChunkReceived += (sender, e) =>
{
    if (!e.Chunk.IsFinal)
    {
        Console.Write(e.Chunk.Content); // Show partial responses
    }
};
```

### Optimize Tool Execution

Make tools fast and efficient:

```csharp
public sealed class OptimizedTool : BaseTool<ToolParams, object>
{
    private readonly IMemoryCache _cache;

    public OptimizedTool(IMemoryCache cache)
    {
        _cache = cache;
    }

    protected override async Task<object> InvokeTypedAsync(ToolParams parameters, CancellationToken ct = default)
    {
        // Check cache first
        var cacheKey = $"tool_result_{parameters.GetHashCode()}";
        if (_cache.TryGetValue(cacheKey, out object? cachedResult))
        {
            return cachedResult!;
        }

        // Execute tool
        var result = await ExecuteToolAsync(parameters, ct);
        
        // Cache result
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        
        return result;
    }
}
```

## Memory Management

### Limit State Size

Configure state management to prevent memory bloat:

```csharp
var config = new AgentConfiguration
{
    MaxHistoryLength = 50,
    EnableHistorySummarization = true,
    HistorySummarizationThreshold = 25,
    MaxMemoryUsageMB = 512
};
```

### Implement State Cleanup

Regularly clean up old state:

```csharp
// Clean up old agent states
public async Task CleanupOldStatesAsync()
{
    var agents = await GetAllAgentIdsAsync();
    var cutoff = DateTime.UtcNow.AddDays(-7);
    
    foreach (var agentId in agents)
    {
        var state = await GetStateAsync(agentId);
        if (state?.LastUpdated < cutoff)
        {
            await DeleteStateAsync(agentId);
        }
    }
}
```

### Use Memory-Efficient Data Structures

```csharp
// ❌ Memory inefficient
public class InefficientState
{
    public List<string> FullHistory { get; set; } = new();
    public Dictionary<string, object> LargeMetadata { get; set; } = new();
}

// ✅ Memory efficient
public class EfficientState
{
    public List<string> SummarizedHistory { get; set; } = new();
    public Dictionary<string, string> CompactMetadata { get; set; } = new();
}
```

## Concurrent Execution

### Configure Concurrency Limits

```csharp
var config = new AgentConfiguration
{
    MaxConcurrentTurns = 3,
    MaxConcurrentAgents = 10
};
```

### Use Async/Await Properly

```csharp
// ✅ Proper async usage
public async Task<object> ProcessDataAsync(DataParams parameters, CancellationToken ct = default)
{
    var tasks = parameters.Items.Select(async item =>
    {
        await Task.Delay(100, ct); // Simulate work
        return ProcessItem(item);
    });
    
    var results = await Task.WhenAll(tasks);
    return results;
}

// ❌ Blocking operations
public object ProcessData(DataParams parameters)
{
    var results = new List<object>();
    foreach (var item in parameters.Items)
    {
        Thread.Sleep(100); // Blocking!
        results.Add(ProcessItem(item));
    }
    return results;
}
```

## Caching Strategies

### Implement Tool Result Caching

```csharp
public sealed class CachedWeatherTool : BaseTool<WeatherParams, object>
{
    private readonly IMemoryCache _cache;
    private readonly IWeatherService _weatherService;

    public CachedWeatherTool(IMemoryCache cache, IWeatherService weatherService)
    {
        _cache = cache;
        _weatherService = weatherService;
    }

    protected override async Task<object> InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
    {
        var cacheKey = $"weather_{parameters.City}_{parameters.Country}";
        
        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            return cached!;
        }

        var weather = await _weatherService.GetWeatherAsync(parameters.City, ct);
        
        // Cache for 15 minutes
        _cache.Set(cacheKey, weather, TimeSpan.FromMinutes(15));
        
        return weather;
    }
}
```

### Cache LLM Responses

```csharp
public class CachedLlmClient : ILlmClient
{
    private readonly ILlmClient _innerClient;
    private readonly IMemoryCache _cache;

    public async Task<LlmResponse> CallAsync(LlmRequest request, CancellationToken ct = default)
    {
        var cacheKey = $"llm_{request.GetHashCode()}";
        
        if (_cache.TryGetValue(cacheKey, out LlmResponse? cached))
        {
            return cached!;
        }

        var response = await _innerClient.CallAsync(request, ct);
        
        // Cache for 1 hour
        _cache.Set(cacheKey, response, TimeSpan.FromHours(1));
        
        return response;
    }
}
```

## Monitoring and Profiling

### Track Performance Metrics

```csharp
agent.Metrics.MetricsUpdated += (sender, e) =>
{
    var metrics = e.Metrics;
    
    // Monitor key performance indicators
    if (metrics.Performance.AverageResponseTimeMs > 5000)
    {
        Console.WriteLine("WARNING: High response time detected");
    }
    
    if (metrics.Resources.TotalTokens > 10000)
    {
        Console.WriteLine("WARNING: High token usage detected");
    }
    
    if (metrics.Operational.AgentRunErrorRate > 0.1)
    {
        Console.WriteLine("WARNING: High error rate detected");
    }
};
```

### Profile Agent Performance

```csharp
public class PerformanceProfiler
{
    private readonly Dictionary<string, Stopwatch> _timers = new();
    private readonly Dictionary<string, List<long>> _measurements = new();

    public void StartTimer(string operation)
    {
        _timers[operation] = Stopwatch.StartNew();
    }

    public void StopTimer(string operation)
    {
        if (_timers.TryGetValue(operation, out var timer))
        {
            timer.Stop();
            if (!_measurements.ContainsKey(operation))
            {
                _measurements[operation] = new List<long>();
            }
            _measurements[operation].Add(timer.ElapsedMilliseconds);
        }
    }

    public PerformanceReport GenerateReport()
    {
        var report = new PerformanceReport();
        
        foreach (var kvp in _measurements)
        {
            var measurements = kvp.Value;
            report.Operations[kvp.Key] = new OperationMetrics
            {
                AverageTimeMs = measurements.Average(),
                MinTimeMs = measurements.Min(),
                MaxTimeMs = measurements.Max(),
                Count = measurements.Count
            };
        }
        
        return report;
    }
}
```

## Configuration Optimization

### Production Configuration

```csharp
public static AgentConfiguration GetOptimizedProductionConfig()
{
    return new AgentConfiguration
    {
        // Performance settings
        MaxTurns = 15,
        MaxConcurrentTurns = 2,
        LlmTimeout = TimeSpan.FromSeconds(30),
        ToolTimeout = TimeSpan.FromSeconds(10),
        
        // Memory management
        MaxHistoryLength = 30,
        EnableHistorySummarization = true,
        HistorySummarizationThreshold = 15,
        MaxMemoryUsageMB = 256,
        
        // State management
        PersistStateAfterEachTurn = false,
        PersistStateInterval = 5,
        EnableStateCompression = true,
        
        // Metrics and monitoring
        EnableMetricsCollection = true,
        EnableMetricsPersistence = true,
        MetricsCollectionInterval = TimeSpan.FromSeconds(10),
        
        // Error handling
        MaxRetries = 2,
        RetryDelay = TimeSpan.FromSeconds(1),
        EnableErrorRecovery = true
    };
}
```

### Development Configuration

```csharp
public static AgentConfiguration GetOptimizedDevelopmentConfig()
{
    return new AgentConfiguration
    {
        // More verbose for debugging
        MaxTurns = 50,
        EnableDetailedLogging = true,
        DebugMode = true,
        
        // Faster metrics collection
        EnableMetricsCollection = true,
        MetricsCollectionInterval = TimeSpan.FromSeconds(1),
        
        // More lenient timeouts
        LlmTimeout = TimeSpan.FromMinutes(2),
        ToolTimeout = TimeSpan.FromSeconds(30)
    };
}
```

## Best Practices Summary

### 1. Token Optimization

- **Keep prompts concise** and focused
- **Enable history summarization** for long conversations
- **Monitor token usage** and set alerts
- **Use appropriate models** for the task complexity

### 2. Response Time

- **Set realistic timeouts** based on your use case
- **Use streaming** for better perceived performance
- **Optimize tool execution** with caching
- **Profile slow operations** and optimize them

### 3. Memory Management

- **Limit history length** appropriately
- **Enable state compression** for large states
- **Clean up old state** regularly
- **Monitor memory usage** and set limits

### 4. Concurrency

- **Set appropriate concurrency limits**
- **Use async/await** properly
- **Avoid blocking operations**
- **Test under load**

### 5. Caching

- **Cache tool results** where appropriate
- **Cache LLM responses** for repeated queries
- **Use appropriate cache expiration** times
- **Monitor cache hit rates**

### 6. Monitoring

- **Track key performance indicators**
- **Set up alerts** for performance issues
- **Profile operations** regularly
- **Monitor resource usage**

## Performance Checklist

- [ ] **Token usage** is optimized and monitored
- [ ] **Response times** are within acceptable limits
- [ ] **Memory usage** is controlled and monitored
- [ ] **Concurrency limits** are set appropriately
- [ ] **Caching** is implemented where beneficial
- [ ] **Timeouts** are configured realistically
- [ ] **Error handling** is robust and fast
- [ ] **Monitoring** is in place for all key metrics
- [ ] **Profiling** is done regularly
- [ ] **Configuration** is optimized for the environment

By following these performance optimization best practices, you can ensure your AIAgentSharp agents run efficiently and reliably in production environments.
