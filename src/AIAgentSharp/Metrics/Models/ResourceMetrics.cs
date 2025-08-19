namespace AIAgentSharp.Metrics;

/// <summary>
/// Contains resource-related metrics including token usage,
/// API calls, and state store operations.
/// </summary>
public sealed class ResourceMetrics
{
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public long TotalStateStoreOperations { get; set; }
    public long TotalStateStoreOperationTimeMs { get; set; }
    public double AverageStateStoreOperationTimeMs { get; set; }
    public double AverageInputTokensPerCall { get; set; }
    public double AverageOutputTokensPerCall { get; set; }

    public Dictionary<string, TokenUsage> TokenUsageByModel { get; set; } = new();
    public Dictionary<string, long> ApiCallCountsByType { get; set; } = new();
    public Dictionary<string, long> ApiCallCountsByModel { get; set; } = new();
    public Dictionary<string, long> StateStoreOperationCounts { get; set; } = new();
}

/// <summary>
/// Represents token usage for a specific model.
/// </summary>
public sealed class TokenUsage
{
    public string Model { get; set; } = string.Empty;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
}
