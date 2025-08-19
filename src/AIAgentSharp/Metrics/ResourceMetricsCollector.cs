using System.Collections.Concurrent;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Collects and manages resource-related metrics including token usage,
/// API calls, and state store operations.
/// </summary>
public sealed class ResourceMetricsCollector
{
    private readonly ILogger _logger;
    
    // Token usage tracking
    private long _totalInputTokens;
    private long _totalOutputTokens;
    private readonly ConcurrentDictionary<string, TokenUsage> _tokenUsageByModel = new();

    // API call tracking
    private readonly ConcurrentDictionary<string, long> _apiCallCountsByType = new();
    private readonly ConcurrentDictionary<string, long> _apiCallCountsByModel = new();

    // State store operations
    private long _totalStateStoreOperations;
    private long _totalStateStoreOperationTimeMs;
    private readonly ConcurrentDictionary<string, long> _stateStoreOperationCounts = new();

    public ResourceMetricsCollector(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records token usage for a specific model.
    /// </summary>
    public void RecordTokenUsage(string agentId, int turnIndex, int inputTokens, int outputTokens, string modelName)
    {
        try
        {
            Interlocked.Add(ref _totalInputTokens, inputTokens);
            Interlocked.Add(ref _totalOutputTokens, outputTokens);

            _tokenUsageByModel.AddOrUpdate(modelName, 
                new TokenUsage { Model = modelName, InputTokens = inputTokens, OutputTokens = outputTokens },
                (_, existing) => new TokenUsage 
                { 
                    Model = modelName, 
                    InputTokens = existing.InputTokens + inputTokens, 
                    OutputTokens = existing.OutputTokens + outputTokens 
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record token usage: {ex.Message}");
        }
    }

    /// <summary>
    /// Records an API call.
    /// </summary>
    public void RecordApiCall(string agentId, string apiType, string modelName)
    {
        try
        {
            _apiCallCountsByType.AddOrUpdate(apiType, 1, (_, count) => count + 1);
            _apiCallCountsByModel.AddOrUpdate(modelName, 1, (_, count) => count + 1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record API call: {ex.Message}");
        }
    }

    /// <summary>
    /// Records a state store operation.
    /// </summary>
    public void RecordStateStoreOperation(string agentId, string operationType, long executionTimeMs)
    {
        try
        {
            Interlocked.Increment(ref _totalStateStoreOperations);
            Interlocked.Add(ref _totalStateStoreOperationTimeMs, executionTimeMs);
            _stateStoreOperationCounts.AddOrUpdate(operationType, 1, (_, count) => count + 1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record state store operation: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates resource metrics from collected data.
    /// </summary>
    public ResourceMetrics CalculateResourceMetrics()
    {
        return new ResourceMetrics
        {
            TotalInputTokens = _totalInputTokens,
            TotalOutputTokens = _totalOutputTokens,
            TotalStateStoreOperations = _totalStateStoreOperations,
            TotalStateStoreOperationTimeMs = _totalStateStoreOperationTimeMs,
            
            AverageStateStoreOperationTimeMs = _totalStateStoreOperations > 0 
                ? (double)_totalStateStoreOperationTimeMs / _totalStateStoreOperations 
                : 0,
            
            TokenUsageByModel = new Dictionary<string, TokenUsage>(_tokenUsageByModel),
            ApiCallCountsByType = new Dictionary<string, long>(_apiCallCountsByType),
            ApiCallCountsByModel = new Dictionary<string, long>(_apiCallCountsByModel),
            StateStoreOperationCounts = new Dictionary<string, long>(_stateStoreOperationCounts)
        };
    }

    /// <summary>
    /// Resets all resource metrics.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _totalInputTokens, 0);
        Interlocked.Exchange(ref _totalOutputTokens, 0);
        Interlocked.Exchange(ref _totalStateStoreOperations, 0);
        Interlocked.Exchange(ref _totalStateStoreOperationTimeMs, 0);

        _tokenUsageByModel.Clear();
        _apiCallCountsByType.Clear();
        _apiCallCountsByModel.Clear();
        _stateStoreOperationCounts.Clear();
    }

    /// <summary>
    /// Gets token usage for a specific model.
    /// </summary>
    public TokenUsage? GetTokenUsageForModel(string modelName)
    {
        return _tokenUsageByModel.TryGetValue(modelName, out var usage) ? usage : null;
    }

    /// <summary>
    /// Gets all token usage data.
    /// </summary>
    public IReadOnlyDictionary<string, TokenUsage> GetTokenUsage()
    {
        return new Dictionary<string, TokenUsage>(_tokenUsageByModel);
    }

    /// <summary>
    /// Gets API call counts by type.
    /// </summary>
    public IReadOnlyDictionary<string, long> GetApiCallCountsByType()
    {
        return new Dictionary<string, long>(_apiCallCountsByType);
    }

    /// <summary>
    /// Gets API call counts by model.
    /// </summary>
    public IReadOnlyDictionary<string, long> GetApiCallCountsByModel()
    {
        return new Dictionary<string, long>(_apiCallCountsByModel);
    }

    /// <summary>
    /// Gets state store operation counts.
    /// </summary>
    public IReadOnlyDictionary<string, long> GetStateStoreOperationCounts()
    {
        return new Dictionary<string, long>(_stateStoreOperationCounts);
    }
}
