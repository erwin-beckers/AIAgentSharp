using System.Collections.Concurrent;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Collects and manages quality-related metrics including response quality,
/// reasoning accuracy, and validation results.
/// </summary>
public sealed class QualityMetricsCollector
{
    private readonly ILogger _logger;
    
    // Quality tracking
    private long _totalResponses;
    private long _highQualityResponses;
    private long _mediumQualityResponses;
    private long _lowQualityResponses;
    private readonly ConcurrentDictionary<string, long> _qualityScoresByAgent = new();

    // Reasoning accuracy
    private long _totalReasoningSteps;
    private long _successfulReasoningSteps;
    private long _failedReasoningSteps;
    private readonly ConcurrentDictionary<string, long> _reasoningAccuracyByType = new();

    // Validation results
    private long _totalValidations;
    private long _passedValidations;
    private long _failedValidations;
    private readonly ConcurrentDictionary<string, long> _validationResultsByType = new();

    // Response time quality
    private readonly ConcurrentQueue<long> _responseTimes = new();
    private readonly ConcurrentDictionary<string, long> _averageResponseTimeByAgent = new();

    public QualityMetricsCollector(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records response quality for an agent.
    /// </summary>
    public void RecordResponseQuality(string agentId, string qualityLevel, double? qualityScore = null)
    {
        try
        {
            Interlocked.Increment(ref _totalResponses);

            switch (qualityLevel.ToLowerInvariant())
            {
                case "high":
                    Interlocked.Increment(ref _highQualityResponses);
                    break;
                case "medium":
                    Interlocked.Increment(ref _mediumQualityResponses);
                    break;
                case "low":
                    Interlocked.Increment(ref _lowQualityResponses);
                    break;
            }

            if (qualityScore.HasValue)
            {
                _qualityScoresByAgent.AddOrUpdate(agentId, 
                    (long)(qualityScore.Value * 100), 
                    (_, existing) => (long)((existing + qualityScore.Value * 100) / 2));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record response quality: {ex.Message}");
        }
    }

    /// <summary>
    /// Records reasoning step accuracy.
    /// </summary>
    public void RecordReasoningStep(string agentId, string reasoningType, bool wasSuccessful)
    {
        try
        {
            Interlocked.Increment(ref _totalReasoningSteps);

            if (wasSuccessful)
            {
                Interlocked.Increment(ref _successfulReasoningSteps);
            }
            else
            {
                Interlocked.Increment(ref _failedReasoningSteps);
            }

            _reasoningAccuracyByType.AddOrUpdate(reasoningType, 
                wasSuccessful ? 1 : 0, 
                (_, existing) => existing + (wasSuccessful ? 1 : 0));
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record reasoning step: {ex.Message}");
        }
    }

    /// <summary>
    /// Records validation result.
    /// </summary>
    public void RecordValidation(string agentId, string validationType, bool passed, string? errorMessage = null)
    {
        try
        {
            Interlocked.Increment(ref _totalValidations);

            if (passed)
            {
                Interlocked.Increment(ref _passedValidations);
            }
            else
            {
                Interlocked.Increment(ref _failedValidations);
            }

            _validationResultsByType.AddOrUpdate(validationType, 
                passed ? 1 : 0, 
                (_, existing) => existing + (passed ? 1 : 0));
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record validation: {ex.Message}");
        }
    }

    /// <summary>
    /// Records response time for quality analysis.
    /// </summary>
    public void RecordResponseTime(string agentId, long responseTimeMs)
    {
        try
        {
            _responseTimes.Enqueue(responseTimeMs);

            // Keep only the last 1000 response times to avoid memory issues
            while (_responseTimes.Count > 1000)
            {
                _responseTimes.TryDequeue(out _);
            }

            _averageResponseTimeByAgent.AddOrUpdate(agentId, 
                responseTimeMs, 
                (_, existing) => (existing + responseTimeMs) / 2);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to record response time: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates quality metrics from collected data.
    /// </summary>
    public QualityMetrics CalculateQualityMetrics()
    {
        var responseTimes = _responseTimes.ToArray();
        var averageResponseTime = responseTimes.Length > 0 ? responseTimes.Average() : 0;

        return new QualityMetrics
        {
            TotalResponses = _totalResponses,
            HighQualityResponses = _highQualityResponses,
            MediumQualityResponses = _mediumQualityResponses,
            LowQualityResponses = _lowQualityResponses,
            
            QualityPercentage = _totalResponses > 0 
                ? (double)_highQualityResponses / _totalResponses * 100 
                : 0,
            
            TotalReasoningSteps = _totalReasoningSteps,
            SuccessfulReasoningSteps = _successfulReasoningSteps,
            FailedReasoningSteps = _failedReasoningSteps,
            
            ReasoningAccuracyPercentage = _totalReasoningSteps > 0 
                ? (double)_successfulReasoningSteps / _totalReasoningSteps * 100 
                : 0,
            
            TotalValidations = _totalValidations,
            PassedValidations = _passedValidations,
            FailedValidations = _failedValidations,
            
            ValidationPassRate = _totalValidations > 0 
                ? (double)_passedValidations / _totalValidations * 100 
                : 0,
            
            AverageResponseTimeMs = averageResponseTime,
            
            QualityScoresByAgent = new Dictionary<string, long>(_qualityScoresByAgent),
            ReasoningAccuracyByType = new Dictionary<string, long>(_reasoningAccuracyByType),
            ValidationResultsByType = new Dictionary<string, long>(_validationResultsByType),
            AverageResponseTimeByAgent = new Dictionary<string, long>(_averageResponseTimeByAgent)
        };
    }

    /// <summary>
    /// Resets all quality metrics.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _totalResponses, 0);
        Interlocked.Exchange(ref _highQualityResponses, 0);
        Interlocked.Exchange(ref _mediumQualityResponses, 0);
        Interlocked.Exchange(ref _lowQualityResponses, 0);
        Interlocked.Exchange(ref _totalReasoningSteps, 0);
        Interlocked.Exchange(ref _successfulReasoningSteps, 0);
        Interlocked.Exchange(ref _failedReasoningSteps, 0);
        Interlocked.Exchange(ref _totalValidations, 0);
        Interlocked.Exchange(ref _passedValidations, 0);
        Interlocked.Exchange(ref _failedValidations, 0);

        _qualityScoresByAgent.Clear();
        _reasoningAccuracyByType.Clear();
        _validationResultsByType.Clear();
        _averageResponseTimeByAgent.Clear();

        while (_responseTimes.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Gets quality score for a specific agent.
    /// </summary>
    public long? GetQualityScoreForAgent(string agentId)
    {
        return _qualityScoresByAgent.TryGetValue(agentId, out var score) ? score : null;
    }

    /// <summary>
    /// Gets reasoning accuracy for a specific type.
    /// </summary>
    public long? GetReasoningAccuracyForType(string reasoningType)
    {
        return _reasoningAccuracyByType.TryGetValue(reasoningType, out var accuracy) ? accuracy : null;
    }

    /// <summary>
    /// Gets validation results for a specific type.
    /// </summary>
    public long? GetValidationResultsForType(string validationType)
    {
        return _validationResultsByType.TryGetValue(validationType, out var results) ? results : null;
    }

    /// <summary>
    /// Gets average response time for a specific agent.
    /// </summary>
    public long? GetAverageResponseTimeForAgent(string agentId)
    {
        return _averageResponseTimeByAgent.TryGetValue(agentId, out var time) ? time : null;
    }
}
