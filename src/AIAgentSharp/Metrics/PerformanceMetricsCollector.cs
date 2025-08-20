using System.Collections.Concurrent;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Collects and manages performance-related metrics including execution times,
/// throughput, and percentile calculations.
/// </summary>
public sealed class PerformanceMetricsCollector
{
    private readonly ILogger _logger;
    
    // Performance counters
    private long _totalAgentRuns;
    private long _totalAgentSteps;
    private long _totalLlmCalls;
    private long _totalToolCalls;
    private long _totalReasoningOperations;

    // Timing data for percentile calculations
    private readonly ConcurrentQueue<long> _agentRunTimes = new();
    private readonly ConcurrentQueue<long> _llmCallTimes = new();
    private readonly ConcurrentQueue<long> _toolCallTimes = new();
    private readonly ConcurrentQueue<ExecutionTimeRecord> _executionTimes = new();

    public PerformanceMetricsCollector(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records agent run execution time.
    /// </summary>
    public void RecordAgentRunExecutionTime(string agentId, long executionTimeMs, int totalTurns)
    {
        try
        {
            Interlocked.Increment(ref _totalAgentRuns);
            _agentRunTimes.Enqueue(executionTimeMs);
            
            // Keep only the last 1000 records for percentile calculations
            while (_agentRunTimes.Count > 1000)
            {
                _agentRunTimes.TryDequeue(out _);
            }

            _executionTimes.Enqueue(new ExecutionTimeRecord { AgentId = agentId, TurnIndex = 0, ExecutionTimeMs = executionTimeMs, Timestamp = DateTime.UtcNow, OperationType = "AgentRun" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record agent run execution time: {ex.Message}");
        }
    }

    /// <summary>
    /// Records agent step execution time.
    /// </summary>
    public void RecordAgentStepExecutionTime(string agentId, int turnIndex, long executionTimeMs)
    {
        try
        {
            Interlocked.Increment(ref _totalAgentSteps);
            _executionTimes.Enqueue(new ExecutionTimeRecord { AgentId = agentId, TurnIndex = turnIndex, ExecutionTimeMs = executionTimeMs, Timestamp = DateTime.UtcNow, OperationType = "AgentStep" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record agent step execution time: {ex.Message}");
        }
    }

    /// <summary>
    /// Records LLM call execution time.
    /// </summary>
    public void RecordLlmCallExecutionTime(string agentId, int turnIndex, long executionTimeMs, string modelName)
    {
        try
        {
            Interlocked.Increment(ref _totalLlmCalls);
            _llmCallTimes.Enqueue(executionTimeMs);
            
            // Keep only the last 1000 records for percentile calculations
            while (_llmCallTimes.Count > 1000)
            {
                _llmCallTimes.TryDequeue(out _);
            }

            _executionTimes.Enqueue(new ExecutionTimeRecord { AgentId = agentId, TurnIndex = turnIndex, ExecutionTimeMs = executionTimeMs, Timestamp = DateTime.UtcNow, OperationType = "LlmCall" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record LLM call execution time: {ex.Message}");
        }
    }

    /// <summary>
    /// Records tool call execution time.
    /// </summary>
    public void RecordToolCallExecutionTime(string agentId, int turnIndex, string toolName, long executionTimeMs)
    {
        try
        {
            Interlocked.Increment(ref _totalToolCalls);
            _toolCallTimes.Enqueue(executionTimeMs);
            
            // Keep only the last 1000 records for percentile calculations
            while (_toolCallTimes.Count > 1000)
            {
                _toolCallTimes.TryDequeue(out _);
            }

            _executionTimes.Enqueue(new ExecutionTimeRecord { AgentId = agentId, TurnIndex = turnIndex, ExecutionTimeMs = executionTimeMs, Timestamp = DateTime.UtcNow, OperationType = "ToolCall" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record tool call execution time: {ex.Message}");
        }
    }

    /// <summary>
    /// Records reasoning execution time.
    /// </summary>
    public void RecordReasoningExecutionTime(string agentId, ReasoningType reasoningType, long executionTimeMs)
    {
        try
        {
            Interlocked.Increment(ref _totalReasoningOperations);
            _executionTimes.Enqueue(new ExecutionTimeRecord { AgentId = agentId, TurnIndex = 0, ExecutionTimeMs = executionTimeMs, Timestamp = DateTime.UtcNow, OperationType = "Reasoning" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record reasoning execution time: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates performance metrics from collected data.
    /// </summary>
    public PerformanceMetrics CalculatePerformanceMetrics(List<ExecutionTimeRecord>? filteredRecords = null)
    {
        var records = filteredRecords ?? _executionTimes.ToList();
        
        if (!records.Any())
        {
            return new PerformanceMetrics();
        }

        var agentRunTimes = _agentRunTimes.ToList();
        var llmCallTimes = _llmCallTimes.ToList();
        var toolCallTimes = _toolCallTimes.ToList();

        return new PerformanceMetrics
        {
            TotalAgentRuns = _totalAgentRuns,
            TotalAgentSteps = _totalAgentSteps,
            TotalLlmCalls = _totalLlmCalls,
            TotalToolCalls = _totalToolCalls,
            TotalReasoningOperations = _totalReasoningOperations,
            
            AverageAgentRunTimeMs = agentRunTimes.Any() ? agentRunTimes.Average() : 0,
            AverageLlmCallTimeMs = llmCallTimes.Any() ? llmCallTimes.Average() : 0,
            AverageToolCallTimeMs = toolCallTimes.Any() ? toolCallTimes.Average() : 0,
            
            P95AgentRunTimeMs = CalculatePercentile(agentRunTimes, 95),
            P95LlmCallTimeMs = CalculatePercentile(llmCallTimes, 95),
            P95ToolCallTimeMs = CalculatePercentile(toolCallTimes, 95),
            
            P99AgentRunTimeMs = CalculatePercentile(agentRunTimes, 99),
            P99LlmCallTimeMs = CalculatePercentile(llmCallTimes, 99),
            P99ToolCallTimeMs = CalculatePercentile(toolCallTimes, 99),
            
            RequestsPerSecond = CalculateRequestsPerSecond(),
            TotalExecutionTimeMs = records.Sum(r => r.ExecutionTimeMs)
        };
    }

    /// <summary>
    /// Resets all performance metrics.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _totalAgentRuns, 0);
        Interlocked.Exchange(ref _totalAgentSteps, 0);
        Interlocked.Exchange(ref _totalLlmCalls, 0);
        Interlocked.Exchange(ref _totalToolCalls, 0);
        Interlocked.Exchange(ref _totalReasoningOperations, 0);

        while (_agentRunTimes.TryDequeue(out _)) { }
        while (_llmCallTimes.TryDequeue(out _)) { }
        while (_toolCallTimes.TryDequeue(out _)) { }
        while (_executionTimes.TryDequeue(out _)) { }
    }

    private double CalculateRequestsPerSecond()
    {
        var totalRequests = _totalAgentRuns + _totalLlmCalls + _totalToolCalls;
        if (totalRequests == 0) return 0;

        // Calculate based on the time span of collected data
        var records = _executionTimes.ToList();
        if (!records.Any()) return 0;

        var timeSpan = records.Max(r => r.Timestamp) - records.Min(r => r.Timestamp);
        return timeSpan.TotalSeconds > 0 ? totalRequests / timeSpan.TotalSeconds : 0;
    }

    private double CalculatePercentile(List<long> values, int percentile)
    {
        if (!values.Any()) return 0;

        var sortedValues = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling((percentile / 100.0) * sortedValues.Count) - 1;
        return index >= 0 && index < sortedValues.Count ? sortedValues[index] : 0;
    }
}
