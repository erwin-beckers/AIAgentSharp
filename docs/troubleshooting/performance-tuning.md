# Performance Tuning Guide

This guide provides comprehensive strategies and techniques for optimizing the performance of AIAgentSharp applications. Performance tuning is crucial for building scalable, responsive AI agents that can handle high loads efficiently.

## Overview

Performance tuning in AIAgentSharp involves:
- Identifying performance bottlenecks
- Optimizing LLM interactions
- Improving tool execution efficiency
- Managing memory and resources
- Scaling agent operations
- Monitoring and measuring performance

## Performance Analysis

### 1. Performance Metrics

```csharp
public class PerformanceMetrics
{
    public TimeSpan ResponseTime { get; set; }
    public int TokensUsed { get; set; }
    public double RequestsPerSecond { get; set; }
    public double SuccessRate { get; set; }
    public long MemoryUsage { get; set; }
    public int ConcurrentRequests { get; set; }
    public TimeSpan ToolExecutionTime { get; set; }
    public int ToolCallsPerRequest { get; set; }
}

public class PerformanceAnalyzer
{
    private readonly ILogger _logger;
    private readonly List<PerformanceMetrics> _metrics;
    
    public PerformanceAnalyzer(ILogger logger)
    {
        _logger = logger;
        _metrics = new List<PerformanceMetrics>();
    }
    
    public void RecordMetrics(PerformanceMetrics metrics)
    {
        _metrics.Add(metrics);
        
        // Keep only recent metrics
        if (_metrics.Count > 1000)
        {
            _metrics.RemoveAt(0);
        }
    }
    
    public PerformanceReport GenerateReport()
    {
        if (!_metrics.Any())
        {
            return new PerformanceReport();
        }
        
        return new PerformanceReport
        {
            AverageResponseTime = _metrics.Average(m => m.ResponseTime.TotalMilliseconds),
            AverageTokensUsed = _metrics.Average(m => m.TokensUsed),
            AverageRequestsPerSecond = _metrics.Average(m => m.RequestsPerSecond),
            AverageSuccessRate = _metrics.Average(m => m.SuccessRate),
            AverageMemoryUsage = _metrics.Average(m => m.MemoryUsage),
            AverageToolExecutionTime = _metrics.Average(m => m.ToolExecutionTime.TotalMilliseconds),
            AverageToolCallsPerRequest = _metrics.Average(m => m.ToolCallsPerRequest),
            TotalRequests = _metrics.Count,
            PerformanceTrends = AnalyzeTrends()
        };
    }
    
    private List<PerformanceTrend> AnalyzeTrends()
    {
        var trends = new List<PerformanceTrend>();
        
        // Analyze response time trends
        var responseTimeTrend = AnalyzeTrend(_metrics.Select(m => m.ResponseTime.TotalMilliseconds));
        trends.Add(new PerformanceTrend { Metric = "ResponseTime", Trend = responseTimeTrend });
        
        // Analyze token usage trends
        var tokenUsageTrend = AnalyzeTrend(_metrics.Select(m => (double)m.TokensUsed));
        trends.Add(new PerformanceTrend { Metric = "TokenUsage", Trend = tokenUsageTrend });
        
        return trends;
    }
    
    private TrendDirection AnalyzeTrend(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count < 2) return TrendDirection.Stable;
        
        var firstHalf = valuesList.Take(valuesList.Count / 2).Average();
        var secondHalf = valuesList.Skip(valuesList.Count / 2).Average();
        
        var difference = secondHalf - firstHalf;
        var percentageChange = Math.Abs(difference) / firstHalf;
        
        if (percentageChange < 0.05) return TrendDirection.Stable;
        return difference > 0 ? TrendDirection.Increasing : TrendDirection.Decreasing;
    }
}

public class PerformanceReport
{
    public double AverageResponseTime { get; set; }
    public double AverageTokensUsed { get; set; }
    public double AverageRequestsPerSecond { get; set; }
    public double AverageSuccessRate { get; set; }
    public double AverageMemoryUsage { get; set; }
    public double AverageToolExecutionTime { get; set; }
    public double AverageToolCallsPerRequest { get; set; }
    public int TotalRequests { get; set; }
    public List<PerformanceTrend> PerformanceTrends { get; set; } = new();
}

public class PerformanceTrend
{
    public string Metric { get; set; }
    public TrendDirection Trend { get; set; }
}

public enum TrendDirection
{
    Increasing,
    Decreasing,
    Stable
}
```

### 2. Performance Profiling

```csharp
public class PerformanceProfiler
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, List<ProfilingData>> _profilingData;
    
    public PerformanceProfiler(ILogger logger)
    {
        _logger = logger;
        _profilingData = new Dictionary<string, List<ProfilingData>>();
    }
    
    public IDisposable ProfileOperation(string operationName)
    {
        return new ProfilingScope(this, operationName);
    }
    
    public void RecordProfilingData(string operationName, TimeSpan duration, object context = null)
    {
        if (!_profilingData.ContainsKey(operationName))
        {
            _profilingData[operationName] = new List<ProfilingData>();
        }
        
        _profilingData[operationName].Add(new ProfilingData
        {
            Timestamp = DateTime.UtcNow,
            Duration = duration,
            Context = context
        });
        
        // Keep only recent data
        if (_profilingData[operationName].Count > 1000)
        {
            _profilingData[operationName].RemoveAt(0);
        }
    }
    
    public ProfilingReport GenerateReport(string operationName = null)
    {
        var operations = operationName != null 
            ? new[] { operationName } 
            : _profilingData.Keys.ToArray();
        
        var report = new ProfilingReport();
        
        foreach (var op in operations)
        {
            if (_profilingData.ContainsKey(op))
            {
                var data = _profilingData[op];
                var operationReport = new OperationProfilingReport
                {
                    OperationName = op,
                    TotalExecutions = data.Count,
                    AverageDuration = data.Average(d => d.Duration.TotalMilliseconds),
                    MinDuration = data.Min(d => d.Duration.TotalMilliseconds),
                    MaxDuration = data.Max(d => d.Duration.TotalMilliseconds),
                    P95Duration = CalculatePercentile(data.Select(d => d.Duration.TotalMilliseconds), 95),
                    P99Duration = CalculatePercentile(data.Select(d => d.Duration.TotalMilliseconds), 99)
                };
                
                report.Operations.Add(operationReport);
            }
        }
        
        return report;
    }
    
    private double CalculatePercentile(IEnumerable<double> values, int percentile)
    {
        var sortedValues = values.OrderBy(v => v).ToArray();
        var index = (int)Math.Ceiling((percentile / 100.0) * sortedValues.Length) - 1;
        return sortedValues[index];
    }
}

public class ProfilingScope : IDisposable
{
    private readonly PerformanceProfiler _profiler;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private readonly object _context;
    
    public ProfilingScope(PerformanceProfiler profiler, string operationName, object context = null)
    {
        _profiler = profiler;
        _operationName = operationName;
        _context = context;
        _stopwatch = Stopwatch.StartNew();
    }
    
    public void Dispose()
    {
        _stopwatch.Stop();
        _profiler.RecordProfilingData(_operationName, _stopwatch.Elapsed, _context);
    }
}

public class ProfilingData
{
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public object Context { get; set; }
}

public class ProfilingReport
{
    public List<OperationProfilingReport> Operations { get; set; } = new();
}

public class OperationProfilingReport
{
    public string OperationName { get; set; }
    public int TotalExecutions { get; set; }
    public double AverageDuration { get; set; }
    public double MinDuration { get; set; }
    public double MaxDuration { get; set; }
    public double P95Duration { get; set; }
    public double P99Duration { get; set; }
}
```

## LLM Performance Optimization

### 1. Prompt Optimization

```csharp
public class PromptOptimizer
{
    private readonly ILogger _logger;
    private readonly ITokenizer _tokenizer;
    
    public PromptOptimizer(ILogger logger, ITokenizer tokenizer = null)
    {
        _logger = logger;
        _tokenizer = tokenizer ?? new DefaultTokenizer();
    }
    
    public string OptimizePrompt(string prompt, int maxTokens = 4000)
    {
        var tokens = _tokenizer.Tokenize(prompt);
        
        if (tokens.Count <= maxTokens)
        {
            return prompt;
        }
        
        _logger.LogInformation("Prompt optimization needed: {OriginalTokens} tokens, max {MaxTokens}", 
            tokens.Count, maxTokens);
        
        // Remove redundant whitespace
        var optimized = Regex.Replace(prompt, @"\s+", " ").Trim();
        
        // Remove unnecessary formatting
        optimized = RemoveUnnecessaryFormatting(optimized);
        
        // Truncate if still too long
        var optimizedTokens = _tokenizer.Tokenize(optimized);
        if (optimizedTokens.Count > maxTokens)
        {
            optimized = TruncatePrompt(optimized, maxTokens);
        }
        
        var finalTokens = _tokenizer.Tokenize(optimized);
        _logger.LogInformation("Prompt optimized: {OriginalTokens} -> {FinalTokens} tokens", 
            tokens.Count, finalTokens.Count);
        
        return optimized;
    }
    
    private string RemoveUnnecessaryFormatting(string prompt)
    {
        // Remove excessive newlines
        prompt = Regex.Replace(prompt, @"\n{3,}", "\n\n");
        
        // Remove excessive spaces
        prompt = Regex.Replace(prompt, @" {2,}", " ");
        
        // Remove unnecessary punctuation
        prompt = Regex.Replace(prompt, @"[.!?]{2,}", ".");
        
        return prompt;
    }
    
    private string TruncatePrompt(string prompt, int maxTokens)
    {
        // Keep the most important parts (usually the end)
        var sentences = prompt.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var optimized = new List<string>();
        var currentTokens = 0;
        
        // Start from the end and work backwards
        for (int i = sentences.Length - 1; i >= 0; i--)
        {
            var sentence = sentences[i].Trim() + ".";
            var sentenceTokens = _tokenizer.Tokenize(sentence).Count;
            
            if (currentTokens + sentenceTokens <= maxTokens)
            {
                optimized.Insert(0, sentence);
                currentTokens += sentenceTokens;
            }
            else
            {
                break;
            }
        }
        
        return string.Join(" ", optimized);
    }
    
    public LLMRequest CreateOptimizedRequest(string prompt, int maxTokens = 4000)
    {
        var optimizedPrompt = OptimizePrompt(prompt, maxTokens);
        
        return new LLMRequest
        {
            Prompt = optimizedPrompt,
            MaxTokens = maxTokens,
            Temperature = 0.7f,
            TopP = 0.9f,
            FrequencyPenalty = 0.0f,
            PresencePenalty = 0.0f
        };
    }
}
```

### 2. Response Caching

```csharp
public class ResponseCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly TimeSpan _defaultExpiration;
    
    public ResponseCache(IMemoryCache cache, ILogger logger, TimeSpan defaultExpiration = default)
    {
        _cache = cache;
        _logger = logger;
        _defaultExpiration = defaultExpiration == default ? TimeSpan.FromHours(1) : defaultExpiration;
    }
    
    public async Task<LLMResponse> GetCachedResponseAsync(string prompt, string model)
    {
        var cacheKey = GenerateCacheKey(prompt, model);
        
        if (_cache.TryGetValue(cacheKey, out LLMResponse cachedResponse))
        {
            _logger.LogDebug("Cache hit for prompt: {CacheKey}", cacheKey);
            return cachedResponse;
        }
        
        _logger.LogDebug("Cache miss for prompt: {CacheKey}", cacheKey);
        return null;
    }
    
    public async Task CacheResponseAsync(string prompt, string model, LLMResponse response, TimeSpan? expiration = null)
    {
        var cacheKey = GenerateCacheKey(prompt, model);
        var cacheExpiration = expiration ?? _defaultExpiration;
        
        var cacheEntry = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };
        
        _cache.Set(cacheKey, response, cacheEntry);
        _logger.LogDebug("Cached response for prompt: {CacheKey}", cacheKey);
    }
    
    public async Task<LLMResponse> GetOrSetCachedResponseAsync(
        string prompt, 
        string model, 
        Func<Task<LLMResponse>> responseFactory,
        TimeSpan? expiration = null)
    {
        var cachedResponse = await GetCachedResponseAsync(prompt, model);
        if (cachedResponse != null)
        {
            return cachedResponse;
        }
        
        var response = await responseFactory();
        await CacheResponseAsync(prompt, model, response, expiration);
        
        return response;
    }
    
    private string GenerateCacheKey(string prompt, string model)
    {
        var hash = ComputeHash(prompt);
        return $"llm_response:{model}:{hash}";
    }
    
    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
    
    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
        
        _logger.LogInformation("Response cache cleared");
    }
    
    public CacheStatistics GetStatistics()
    {
        // This would typically be implemented based on the specific cache implementation
        return new CacheStatistics
        {
            TotalEntries = 0, // Would be calculated from cache
            HitRate = 0.0, // Would be calculated from cache
            MemoryUsage = 0 // Would be calculated from cache
        };
    }
}

public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public double HitRate { get; set; }
    public long MemoryUsage { get; set; }
}
```

### 3. Batch Processing

```csharp
public class BatchProcessor
{
    private readonly ILLMClient _llmClient;
    private readonly ILogger _logger;
    private readonly int _batchSize;
    private readonly TimeSpan _batchTimeout;
    
    public BatchProcessor(ILLMClient llmClient, ILogger logger, int batchSize = 10, TimeSpan batchTimeout = default)
    {
        _llmClient = llmClient;
        _logger = logger;
        _batchSize = batchSize;
        _batchTimeout = batchTimeout == default ? TimeSpan.FromSeconds(30) : batchTimeout;
    }
    
    public async Task<List<LLMResponse>> ProcessBatchAsync(List<LLMRequest> requests)
    {
        var results = new List<LLMResponse>();
        var batches = CreateBatches(requests);
        
        _logger.LogInformation("Processing {TotalRequests} requests in {BatchCount} batches", 
            requests.Count, batches.Count);
        
        foreach (var batch in batches)
        {
            var batchResults = await ProcessBatchAsync(batch);
            results.AddRange(batchResults);
        }
        
        return results;
    }
    
    private List<List<LLMRequest>> CreateBatches(List<LLMRequest> requests)
    {
        var batches = new List<List<LLMRequest>>();
        
        for (int i = 0; i < requests.Count; i += _batchSize)
        {
            var batch = requests.Skip(i).Take(_batchSize).ToList();
            batches.Add(batch);
        }
        
        return batches;
    }
    
    private async Task<List<LLMResponse>> ProcessBatchAsync(List<LLMRequest> batch)
    {
        var tasks = batch.Select(request => _llmClient.GenerateAsync(request)).ToArray();
        
        try
        {
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch processing failed");
            
            // Process requests individually as fallback
            var fallbackResults = new List<LLMResponse>();
            foreach (var request in batch)
            {
                try
                {
                    var response = await _llmClient.GenerateAsync(request);
                    fallbackResults.Add(response);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Individual request failed");
                    fallbackResults.Add(new LLMResponse 
                    { 
                        Content = "Error processing request",
                        Success = false 
                    });
                }
            }
            
            return fallbackResults;
        }
    }
    
    public async Task<List<T>> ProcessBatchWithTransformAsync<T>(
        List<LLMRequest> requests, 
        Func<LLMResponse, T> transform)
    {
        var responses = await ProcessBatchAsync(requests);
        return responses.Select(transform).ToList();
    }
}
```

## Tool Performance Optimization

### 1. Tool Caching

```csharp
public class ToolCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly Dictionary<string, TimeSpan> _cacheExpirations;
    
    public ToolCache(IMemoryCache cache, ILogger logger)
    {
        _cache = cache;
        _logger = logger;
        _cacheExpirations = new Dictionary<string, TimeSpan>
        {
            { "get_weather", TimeSpan.FromMinutes(15) },
            { "search_web", TimeSpan.FromMinutes(5) },
            { "get_stock_price", TimeSpan.FromMinutes(1) }
        };
    }
    
    public async Task<ToolResult> GetCachedResultAsync(string toolName, ToolParameters parameters)
    {
        var cacheKey = GenerateCacheKey(toolName, parameters);
        
        if (_cache.TryGetValue(cacheKey, out ToolResult cachedResult))
        {
            _logger.LogDebug("Tool cache hit: {ToolName}", toolName);
            return cachedResult;
        }
        
        return null;
    }
    
    public async Task CacheResultAsync(string toolName, ToolParameters parameters, ToolResult result)
    {
        var cacheKey = GenerateCacheKey(toolName, parameters);
        var expiration = GetCacheExpiration(toolName);
        
        var cacheEntry = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        
        _cache.Set(cacheKey, result, cacheEntry);
        _logger.LogDebug("Tool result cached: {ToolName}", toolName);
    }
    
    private string GenerateCacheKey(string toolName, ToolParameters parameters)
    {
        var parametersJson = JsonSerializer.Serialize(parameters);
        var hash = ComputeHash(parametersJson);
        return $"tool_result:{toolName}:{hash}";
    }
    
    private TimeSpan GetCacheExpiration(string toolName)
    {
        return _cacheExpirations.TryGetValue(toolName, out var expiration) 
            ? expiration 
            : TimeSpan.FromMinutes(5);
    }
    
    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

### 2. Parallel Tool Execution

```csharp
public class ParallelToolExecutor
{
    private readonly ILogger _logger;
    private readonly int _maxConcurrency;
    private readonly SemaphoreSlim _semaphore;
    
    public ParallelToolExecutor(ILogger logger, int maxConcurrency = 5)
    {
        _logger = logger;
        _maxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }
    
    public async Task<List<ToolResult>> ExecuteToolsParallelAsync(
        List<(ITool Tool, ToolParameters Parameters)> toolCalls)
    {
        var tasks = toolCalls.Select(call => ExecuteToolWithSemaphoreAsync(call.Tool, call.Parameters));
        var results = await Task.WhenAll(tasks);
        
        return results.ToList();
    }
    
    private async Task<ToolResult> ExecuteToolWithSemaphoreAsync(ITool tool, ToolParameters parameters)
    {
        await _semaphore.WaitAsync();
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await tool.ExecuteAsync(parameters);
            stopwatch.Stop();
            
            _logger.LogDebug("Tool {ToolName} executed in {Duration}ms", 
                tool.Name, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {ToolName} execution failed", tool.Name);
            return new ToolResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<List<ToolResult>> ExecuteToolsWithDependenciesAsync(
        List<ToolExecutionTask> tasks)
    {
        var results = new List<ToolResult>();
        var completedTasks = new HashSet<string>();
        var taskMap = tasks.ToDictionary(t => t.Id, t => t);
        
        while (completedTasks.Count < tasks.Count)
        {
            var readyTasks = tasks.Where(t => 
                !completedTasks.Contains(t.Id) && 
                t.Dependencies.All(d => completedTasks.Contains(d))).ToList();
            
            if (!readyTasks.Any())
            {
                _logger.LogWarning("Circular dependency detected in tool execution");
                break;
            }
            
            var parallelResults = await ExecuteToolsParallelAsync(
                readyTasks.Select(t => (t.Tool, t.Parameters)).ToList());
            
            for (int i = 0; i < readyTasks.Count; i++)
            {
                var task = readyTasks[i];
                var result = parallelResults[i];
                
                completedTasks.Add(task.Id);
                results.Add(result);
                
                _logger.LogDebug("Completed tool task: {TaskId}", task.Id);
            }
        }
        
        return results;
    }
}

public class ToolExecutionTask
{
    public string Id { get; set; }
    public ITool Tool { get; set; }
    public ToolParameters Parameters { get; set; }
    public List<string> Dependencies { get; set; } = new();
}
```

## Memory Optimization

### 1. Memory Management

```csharp
public class MemoryManager
{
    private readonly ILogger _logger;
    private readonly long _memoryLimit;
    private readonly Timer _cleanupTimer;
    
    public MemoryManager(ILogger logger, long memoryLimitMB = 500)
    {
        _logger = logger;
        _memoryLimit = memoryLimitMB * 1024 * 1024;
        _cleanupTimer = new Timer(CleanupMemory, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    public void MonitorMemoryUsage()
    {
        var currentMemory = GC.GetTotalMemory(false);
        var memoryMB = currentMemory / (1024 * 1024);
        
        _logger.LogDebug("Current memory usage: {MemoryMB}MB", memoryMB);
        
        if (currentMemory > _memoryLimit)
        {
            _logger.LogWarning("Memory usage exceeded limit: {MemoryMB}MB > {LimitMB}MB", 
                memoryMB, _memoryLimit / (1024 * 1024));
            
            ForceGarbageCollection();
        }
    }
    
    private void CleanupMemory(object state)
    {
        var beforeMemory = GC.GetTotalMemory(false);
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var afterMemory = GC.GetTotalMemory(false);
        var freedMemory = beforeMemory - afterMemory;
        
        if (freedMemory > 0)
        {
            _logger.LogInformation("Memory cleanup freed {FreedMB}MB", 
                freedMemory / (1024 * 1024));
        }
    }
    
    private void ForceGarbageCollection()
    {
        _logger.LogInformation("Forcing garbage collection");
        
        var beforeMemory = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterMemory = GC.GetTotalMemory(false);
        
        var freedMemory = beforeMemory - afterMemory;
        _logger.LogInformation("Garbage collection freed {FreedMB}MB", 
            freedMemory / (1024 * 1024));
    }
    
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
```

### 2. Object Pooling

```csharp
public class ObjectPool<T> where T : class
{
    private readonly ConcurrentQueue<T> _pool;
    private readonly Func<T> _factory;
    private readonly Action<T> _reset;
    private readonly int _maxSize;
    private int _currentSize;
    
    public ObjectPool(Func<T> factory, Action<T> reset = null, int maxSize = 100)
    {
        _pool = new ConcurrentQueue<T>();
        _factory = factory;
        _reset = reset;
        _maxSize = maxSize;
        _currentSize = 0;
    }
    
    public T Get()
    {
        if (_pool.TryDequeue(out T item))
        {
            return item;
        }
        
        if (_currentSize < _maxSize)
        {
            Interlocked.Increment(ref _currentSize);
            return _factory();
        }
        
        // Wait for an item to become available
        while (!_pool.TryDequeue(out item))
        {
            Thread.Sleep(10);
        }
        
        return item;
    }
    
    public void Return(T item)
    {
        if (item == null) return;
        
        _reset?.Invoke(item);
        
        if (_pool.Count < _maxSize)
        {
            _pool.Enqueue(item);
        }
    }
    
    public int PoolSize => _pool.Count;
    public int CurrentSize => _currentSize;
}

public class LLMRequestPool
{
    private readonly ObjectPool<LLMRequest> _pool;
    
    public LLMRequestPool()
    {
        _pool = new ObjectPool<LLMRequest>(
            factory: () => new LLMRequest(),
            reset: request =>
            {
                request.Prompt = null;
                request.MaxTokens = 0;
                request.Temperature = 0.0f;
            },
            maxSize: 50
        );
    }
    
    public LLMRequest GetRequest()
    {
        return _pool.Get();
    }
    
    public void ReturnRequest(LLMRequest request)
    {
        _pool.Return(request);
    }
}
```

## Scaling Strategies

### 1. Load Balancing

```csharp
public class LoadBalancer
{
    private readonly List<ILLMClient> _clients;
    private readonly ILogger _logger;
    private int _currentIndex;
    private readonly object _lock = new object();
    
    public LoadBalancer(List<ILLMClient> clients, ILogger logger)
    {
        _clients = clients;
        _logger = logger;
        _currentIndex = 0;
    }
    
    public ILLMClient GetNextClient()
    {
        lock (_lock)
        {
            var client = _clients[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _clients.Count;
            return client;
        }
    }
    
    public async Task<LLMResponse> ExecuteWithLoadBalancingAsync(LLMRequest request)
    {
        var client = GetNextClient();
        
        try
        {
            return await client.GenerateAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client failed, trying next client");
            
            // Try next client
            client = GetNextClient();
            return await client.GenerateAsync(request);
        }
    }
}
```

### 2. Connection Pooling

```csharp
public class ConnectionPool
{
    private readonly ConcurrentQueue<HttpClient> _pool;
    private readonly Func<HttpClient> _factory;
    private readonly int _maxSize;
    private int _currentSize;
    
    public ConnectionPool(Func<HttpClient> factory, int maxSize = 20)
    {
        _pool = new ConcurrentQueue<HttpClient>();
        _factory = factory;
        _maxSize = maxSize;
        _currentSize = 0;
    }
    
    public HttpClient GetConnection()
    {
        if (_pool.TryDequeue(out HttpClient connection))
        {
            return connection;
        }
        
        if (_currentSize < _maxSize)
        {
            Interlocked.Increment(ref _currentSize);
            return _factory();
        }
        
        // Wait for a connection to become available
        while (!_pool.TryDequeue(out connection))
        {
            Thread.Sleep(10);
        }
        
        return connection;
    }
    
    public void ReturnConnection(HttpClient connection)
    {
        if (connection == null) return;
        
        if (_pool.Count < _maxSize)
        {
            _pool.Enqueue(connection);
        }
        else
        {
            connection.Dispose();
            Interlocked.Decrement(ref _currentSize);
        }
    }
}
```

## Performance Monitoring

### 1. Real-time Monitoring

```csharp
public class PerformanceMonitor
{
    private readonly ILogger _logger;
    private readonly Timer _monitorTimer;
    private readonly List<PerformanceMetrics> _recentMetrics;
    private readonly object _lock = new object();
    
    public PerformanceMonitor(ILogger logger)
    {
        _logger = logger;
        _recentMetrics = new List<PerformanceMetrics>();
        _monitorTimer = new Timer(MonitorPerformance, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }
    
    public void RecordMetrics(PerformanceMetrics metrics)
    {
        lock (_lock)
        {
            _recentMetrics.Add(metrics);
            
            // Keep only last 100 metrics
            if (_recentMetrics.Count > 100)
            {
                _recentMetrics.RemoveAt(0);
            }
        }
    }
    
    private void MonitorPerformance(object state)
    {
        lock (_lock)
        {
            if (!_recentMetrics.Any()) return;
            
            var avgResponseTime = _recentMetrics.Average(m => m.ResponseTime.TotalMilliseconds);
            var avgRequestsPerSecond = _recentMetrics.Average(m => m.RequestsPerSecond);
            var avgSuccessRate = _recentMetrics.Average(m => m.SuccessRate);
            var avgMemoryUsage = _recentMetrics.Average(m => m.MemoryUsage);
            
            _logger.LogInformation(
                "Performance Summary - ResponseTime: {ResponseTime}ms, RPS: {RPS}, SuccessRate: {SuccessRate}%, Memory: {Memory}MB",
                avgResponseTime.ToString("F2"),
                avgRequestsPerSecond.ToString("F2"),
                (avgSuccessRate * 100).ToString("F1"),
                (avgMemoryUsage / (1024 * 1024)).ToString("F1"));
            
            // Check for performance issues
            if (avgResponseTime > 5000)
            {
                _logger.LogWarning("High response time detected: {ResponseTime}ms", avgResponseTime);
            }
            
            if (avgSuccessRate < 0.95)
            {
                _logger.LogWarning("Low success rate detected: {SuccessRate}%", avgSuccessRate * 100);
            }
            
            if (avgMemoryUsage > 500 * 1024 * 1024) // 500MB
            {
                _logger.LogWarning("High memory usage detected: {Memory}MB", 
                    avgMemoryUsage / (1024 * 1024));
            }
        }
    }
    
    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}
```

## Performance Best Practices

### 1. General Optimization
- Profile before optimizing
- Measure baseline performance
- Optimize the critical path
- Use appropriate data structures
- Implement caching strategies

### 2. LLM Optimization
- Optimize prompt length and content
- Use response caching
- Implement batch processing
- Monitor token usage
- Use appropriate models

### 3. Tool Optimization
- Cache tool results
- Execute tools in parallel
- Optimize tool parameters
- Monitor tool performance
- Implement fallback mechanisms

### 4. Memory Management
- Monitor memory usage
- Implement object pooling
- Use weak references
- Dispose resources properly
- Set memory limits

### 5. Scaling
- Use load balancing
- Implement connection pooling
- Monitor system resources
- Set up auto-scaling
- Use distributed caching

## Performance Checklist

- [ ] Set up performance monitoring
- [ ] Implement response caching
- [ ] Optimize prompt engineering
- [ ] Use parallel tool execution
- [ ] Implement memory management
- [ ] Set up load balancing
- [ ] Monitor performance metrics
- [ ] Optimize critical paths
- [ ] Implement fallback strategies
- [ ] Set performance targets

## Common Performance Anti-patterns

1. **Premature Optimization**: Profile first, optimize second
2. **Ignoring Caching**: Implement appropriate caching strategies
3. **Sequential Processing**: Use parallel execution where possible
4. **Memory Leaks**: Monitor and manage memory usage
5. **Over-Engineering**: Keep solutions simple and effective
6. **Not Monitoring**: Always monitor performance metrics

## Next Steps

- Set up performance monitoring
- Implement caching strategies
- Optimize critical components
- Set performance benchmarks
- Monitor and tune continuously
- Document performance procedures

This guide provides a comprehensive approach to performance tuning for AIAgentSharp applications, helping you build fast, scalable, and efficient AI agents.
