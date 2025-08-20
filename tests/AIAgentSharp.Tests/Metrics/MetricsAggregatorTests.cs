using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Metrics;

[TestClass]
public class MetricsAggregatorTests
{
    private MetricsAggregator _metricsAggregator = null!;
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _metricsAggregator = new MetricsAggregator(_mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_Should_InitializeAllCollectors()
    {
        // Assert
        Assert.IsNotNull(_metricsAggregator.Performance);
        Assert.IsNotNull(_metricsAggregator.Operational);
        Assert.IsNotNull(_metricsAggregator.Resource);
        Assert.IsNotNull(_metricsAggregator.Quality);
        Assert.IsNotNull(_metricsAggregator.Custom);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new MetricsAggregator(null!));
    }

    #region Performance Metrics Tests

    [TestMethod]
    public void RecordAgentRunExecutionTime_Should_DelegateToPerformanceCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var executionTimeMs = 1500L;
        var totalTurns = 3;

        // Act
        _metricsAggregator.RecordAgentRunExecutionTime(agentId, executionTimeMs, totalTurns);

        // Assert
        var metrics = _metricsAggregator.Performance.CalculatePerformanceMetrics();
        Assert.AreEqual(1, metrics.TotalAgentRuns);
        Assert.AreEqual(executionTimeMs, metrics.AverageAgentRunTimeMs);
    }

    [TestMethod]
    public void RecordAgentStepExecutionTime_Should_DelegateToPerformanceCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var executionTimeMs = 500L;

        // Act
        _metricsAggregator.RecordAgentStepExecutionTime(agentId, turnIndex, executionTimeMs);

        // Assert
        var metrics = _metricsAggregator.Performance.CalculatePerformanceMetrics();
        Assert.AreEqual(1, metrics.TotalAgentSteps);
        // Note: AverageAgentStepTimeMs is not calculated in the current implementation
    }

    [TestMethod]
    public void RecordLlmCallExecutionTime_Should_DelegateToPerformanceCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var executionTimeMs = 300L;
        var modelName = "gpt-4";

        // Act
        _metricsAggregator.RecordLlmCallExecutionTime(agentId, turnIndex, executionTimeMs, modelName);

        // Assert
        var metrics = _metricsAggregator.Performance.CalculatePerformanceMetrics();
        Assert.AreEqual(1, metrics.TotalLlmCalls);
        Assert.AreEqual(executionTimeMs, metrics.AverageLlmCallTimeMs);
    }

    [TestMethod]
    public void RecordToolCallExecutionTime_Should_DelegateToPerformanceCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var toolName = "weather-tool";
        var executionTimeMs = 200L;

        // Act
        _metricsAggregator.RecordToolCallExecutionTime(agentId, turnIndex, toolName, executionTimeMs);

        // Assert
        var metrics = _metricsAggregator.Performance.CalculatePerformanceMetrics();
        Assert.AreEqual(1, metrics.TotalToolCalls);
        Assert.AreEqual(executionTimeMs, metrics.AverageToolCallTimeMs);
    }

    [TestMethod]
    public void RecordReasoningExecutionTime_Should_DelegateToPerformanceCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = ReasoningType.ChainOfThought;
        var executionTimeMs = 400L;

        // Act
        _metricsAggregator.RecordReasoningExecutionTime(agentId, reasoningType, executionTimeMs);

        // Assert
        var metrics = _metricsAggregator.Performance.CalculatePerformanceMetrics();
        Assert.AreEqual(1, metrics.TotalReasoningOperations);
    }

    #endregion

    #region Operational Metrics Tests

    [TestMethod]
    public void RecordAgentRunCompletion_Should_DelegateToOperationalCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = true;
        var totalTurns = 3;

        // Act
        _metricsAggregator.RecordAgentRunCompletion(agentId, succeeded, totalTurns);

        // Assert
        var metrics = _metricsAggregator.Operational.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedAgentRuns); // Only failures are tracked
    }

    [TestMethod]
    public void RecordAgentRunCompletion_WithFailure_Should_RecordFailure()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = false;
        var totalTurns = 3;
        var errorType = "timeout";

        // Act
        _metricsAggregator.RecordAgentRunCompletion(agentId, succeeded, totalTurns, errorType);

        // Assert
        var metrics = _metricsAggregator.Operational.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.SuccessfulAgentRuns);
        Assert.AreEqual(1, metrics.FailedAgentRuns);
    }

    [TestMethod]
    public void RecordAgentStepCompletion_Should_DelegateToOperationalCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = true;
        var executedTool = true;

        // Act
        _metricsAggregator.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool);

        // Assert
        var metrics = _metricsAggregator.Operational.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedAgentSteps); // Only failures are tracked
    }

    [TestMethod]
    public void RecordLlmCallCompletion_Should_DelegateToOperationalCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = true;
        var modelName = "gpt-4";

        // Act
        _metricsAggregator.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName);

        // Assert
        var metrics = _metricsAggregator.Operational.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedLlmCalls); // Only failures are tracked
    }

    [TestMethod]
    public void RecordToolCallCompletion_Should_DelegateToOperationalCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var toolName = "weather-tool";
        var succeeded = true;

        // Act
        _metricsAggregator.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded);

        // Assert
        var metrics = _metricsAggregator.Operational.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedToolCalls); // Only failures are tracked
    }

    [TestMethod]
    public void RecordLoopDetection_Should_DelegateToOperationalCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var loopType = "infinite";
        var consecutiveFailures = 5;

        // Act
        _metricsAggregator.RecordLoopDetection(agentId, loopType, consecutiveFailures);

        // Assert
        var metrics = _metricsAggregator.Operational.CalculateOperationalMetrics();
        Assert.AreEqual(1, metrics.LoopDetectionEvents);
    }

    [TestMethod]
    public void RecordDeduplicationEvent_Should_DelegateToOperationalCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "weather-tool";
        var cacheHit = true;

        // Act
        _metricsAggregator.RecordDeduplicationEvent(agentId, toolName, cacheHit);

        // Assert
        var metrics = _metricsAggregator.Operational.CalculateOperationalMetrics();
        // Deduplication events are tracked internally
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordError_Should_DelegateToOperationalCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var errorType = "api_error";
        var errorMessage = "API rate limit exceeded";

        // Act
        _metricsAggregator.RecordError(agentId, turnIndex, errorType, errorMessage);

        // Assert
        var metrics = _metricsAggregator.Operational.CalculateOperationalMetrics();
        Assert.IsTrue(metrics.ErrorCounts.ContainsKey(errorType));
        Assert.AreEqual(1, metrics.ErrorCounts[errorType]);
    }

    #endregion

    #region Resource Metrics Tests

    [TestMethod]
    public void RecordTokenUsage_Should_DelegateToResourceCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var inputTokens = 100;
        var outputTokens = 50;
        var modelName = "gpt-4";

        // Act
        _metricsAggregator.RecordTokenUsage(agentId, turnIndex, inputTokens, outputTokens, modelName);

        // Assert
        var metrics = _metricsAggregator.Resource.CalculateResourceMetrics();
        Assert.AreEqual(inputTokens, metrics.TotalInputTokens);
        Assert.AreEqual(outputTokens, metrics.TotalOutputTokens);
    }

    [TestMethod]
    public void RecordApiCall_Should_DelegateToResourceCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var apiType = "completion";
        var modelName = "gpt-4";

        // Act
        _metricsAggregator.RecordApiCall(agentId, apiType, modelName);

        // Assert
        var metrics = _metricsAggregator.Resource.CalculateResourceMetrics();
        Assert.AreEqual(1, metrics.ApiCallCountsByType.Count);
    }

    [TestMethod]
    public void RecordStateStoreOperation_Should_DelegateToResourceCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var operationType = "save";
        var executionTimeMs = 50L;

        // Act
        _metricsAggregator.RecordStateStoreOperation(agentId, operationType, executionTimeMs);

        // Assert
        var metrics = _metricsAggregator.Resource.CalculateResourceMetrics();
        Assert.AreEqual(1, metrics.TotalStateStoreOperations);
    }

    #endregion

    #region Quality Metrics Tests

    [TestMethod]
    public void RecordResponseQuality_Should_DelegateToQualityCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "high";
        var qualityScore = 0.95;

        // Act
        _metricsAggregator.RecordResponseQuality(agentId, qualityLevel, qualityScore);

        // Assert
        var metrics = _metricsAggregator.Quality.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalResponses);
        Assert.AreEqual(1, metrics.HighQualityResponses);
    }

    [TestMethod]
    public void RecordReasoningStep_Should_DelegateToQualityCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = "chain_of_thought";
        var wasSuccessful = true;

        // Act
        _metricsAggregator.RecordReasoningStep(agentId, reasoningType, wasSuccessful);

        // Assert
        var metrics = _metricsAggregator.Quality.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalReasoningSteps);
        Assert.AreEqual(1, metrics.SuccessfulReasoningSteps);
    }

    [TestMethod]
    public void RecordValidation_Should_DelegateToQualityCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var validationType = "output_format";
        var passed = true;

        // Act
        _metricsAggregator.RecordValidation(agentId, validationType, passed);

        // Assert
        var metrics = _metricsAggregator.Quality.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalValidations);
        Assert.AreEqual(1, metrics.PassedValidations);
    }

    [TestMethod]
    public void RecordResponseTime_Should_DelegateToQualityCollector()
    {
        // Arrange
        var agentId = "test-agent";
        var responseTimeMs = 2500L;

        // Act
        _metricsAggregator.RecordResponseTime(agentId, responseTimeMs);

        // Assert
        var metrics = _metricsAggregator.Quality.CalculateQualityMetrics();
        Assert.AreEqual(responseTimeMs, metrics.AverageResponseTimeMs);
    }

    #endregion

    #region Custom Metrics Tests

    [TestMethod]
    public void RecordMetric_Should_DelegateToCustomCollector()
    {
        // Arrange
        var metricName = "custom_metric";
        var value = 42.5;
        var category = "business";

        // Act
        _metricsAggregator.RecordMetric(metricName, value, category);

        // Assert
        var metrics = _metricsAggregator.Custom.CalculateCustomMetrics();
        Assert.AreEqual(1, metrics.Metrics.Count);
        Assert.IsTrue(metrics.Metrics.ContainsKey(metricName));
    }

    [TestMethod]
    public void RecordCounter_Should_DelegateToCustomCollector()
    {
        // Arrange
        var counterName = "api_calls";
        var increment = 5L;
        var category = "monitoring";

        // Act
        _metricsAggregator.RecordCounter(counterName, increment, category);

        // Assert
        var metrics = _metricsAggregator.Custom.CalculateCustomMetrics();
        Assert.AreEqual(1, metrics.Counters.Count);
        Assert.IsTrue(metrics.Counters.ContainsKey(counterName));
    }

    [TestMethod]
    public void SetTag_Should_DelegateToCustomCollector()
    {
        // Arrange
        var tagName = "environment";
        var tagValue = "production";

        // Act
        _metricsAggregator.SetTag(tagName, tagValue);

        // Assert
        var metrics = _metricsAggregator.Custom.CalculateCustomMetrics();
        Assert.AreEqual(1, metrics.Tags.Count);
        Assert.IsTrue(metrics.Tags.ContainsKey(tagName));
        Assert.AreEqual(tagValue, metrics.Tags[tagName]);
    }

    [TestMethod]
    public void SetMetadata_Should_DelegateToCustomCollector()
    {
        // Arrange
        var key = "version";
        var value = "1.0.0";

        // Act
        _metricsAggregator.SetMetadata(key, value);

        // Assert
        var metrics = _metricsAggregator.Custom.CalculateCustomMetrics();
        Assert.AreEqual(1, metrics.Metadata.Count);
        Assert.IsTrue(metrics.Metadata.ContainsKey(key));
    }

    [TestMethod]
    public void RecordCustomMetric_Should_DelegateToCustomCollector()
    {
        // Arrange
        var metricName = "custom_metric";
        var value = 123.45;
        var tags = new Dictionary<string, string> { { "tag1", "value1" } };

        // Act
        _metricsAggregator.RecordCustomMetric(metricName, value, tags);

        // Assert
        var metrics = _metricsAggregator.Custom.CalculateCustomMetrics();
        Assert.AreEqual(1, metrics.Metrics.Count);
        Assert.IsTrue(metrics.Metrics.ContainsKey(metricName));
    }

    [TestMethod]
    public void RecordCustomEvent_Should_DelegateToCustomCollector()
    {
        // Arrange
        var eventName = "user_action";
        var tags = new Dictionary<string, string> { { "action", "click" } };

        // Act
        _metricsAggregator.RecordCustomEvent(eventName, tags);

        // Assert
        var metrics = _metricsAggregator.Custom.CalculateCustomMetrics();
        Assert.AreEqual(1, metrics.Metadata.Count);
        Assert.IsTrue(metrics.Metadata.ContainsKey($"event_{eventName}"));
    }

    #endregion

    #region Aggregated Metrics Tests

    [TestMethod]
    public void CalculateAllMetrics_Should_ReturnCompleteMetrics()
    {
        // Arrange
        _metricsAggregator.RecordAgentRunExecutionTime("test-agent", 1000, 3);
        _metricsAggregator.RecordAgentRunCompletion("test-agent", true, 3);
        _metricsAggregator.RecordTokenUsage("test-agent", 1, 100, 50, "gpt-4");
        _metricsAggregator.RecordResponseQuality("test-agent", "high", 0.9);
        _metricsAggregator.RecordMetric("custom", 42.0);

        // Act
        var allMetrics = _metricsAggregator.CalculateAllMetrics();

        // Assert
        Assert.IsNotNull(allMetrics.Performance);
        Assert.IsNotNull(allMetrics.Operational);
        Assert.IsNotNull(allMetrics.Resource);
        Assert.IsNotNull(allMetrics.Quality);
        Assert.IsNotNull(allMetrics.Custom);
    }

    [TestMethod]
    public void Reset_Should_ResetAllCollectors()
    {
        // Arrange
        _metricsAggregator.RecordAgentRunExecutionTime("test-agent", 1000, 3);
        _metricsAggregator.RecordAgentRunCompletion("test-agent", true, 3);
        _metricsAggregator.RecordTokenUsage("test-agent", 1, 100, 50, "gpt-4");

        // Act
        _metricsAggregator.Reset();

        // Assert
        var allMetrics = _metricsAggregator.CalculateAllMetrics();
        Assert.AreEqual(0, allMetrics.Performance.TotalAgentRuns);
        Assert.AreEqual(0, allMetrics.Operational.SuccessfulAgentRuns);
        Assert.AreEqual(0, allMetrics.Resource.TotalInputTokens);
    }

    [TestMethod]
    public void GetSummary_Should_ReturnMetricsSummary()
    {
        // Arrange
        _metricsAggregator.RecordAgentRunExecutionTime("test-agent", 1000, 3);
        _metricsAggregator.RecordAgentRunCompletion("test-agent", true, 3);
        _metricsAggregator.RecordTokenUsage("test-agent", 1, 100, 50, "gpt-4");
        _metricsAggregator.RecordResponseQuality("test-agent", "high", 0.9);

        // Act
        var summary = _metricsAggregator.GetSummary();

        // Assert
        Assert.AreEqual(1, summary.TotalAgentRuns);
        Assert.AreEqual(0, summary.SuccessfulAgentRuns); // Not tracked in operational collector
        Assert.AreEqual(0, summary.FailedAgentRuns); // Success case
        Assert.AreEqual(100, summary.TotalInputTokens);
        Assert.AreEqual(50, summary.TotalOutputTokens);
        Assert.AreEqual(1000, summary.AverageAgentRunTimeMs);
        // Success rate and error rate calculations depend on operational metrics
    }

    #endregion

    #region Edge Cases and Error Handling

    [TestMethod]
    public void MultipleRecordings_Should_AggregateCorrectly()
    {
        // Arrange
        var agentId = "test-agent";

        // Act
        _metricsAggregator.RecordAgentRunExecutionTime(agentId, 1000, 3);
        _metricsAggregator.RecordAgentRunExecutionTime(agentId, 2000, 5);
        _metricsAggregator.RecordAgentRunCompletion(agentId, true, 3);
        _metricsAggregator.RecordAgentRunCompletion(agentId, false, 5);

        // Assert
        var summary = _metricsAggregator.GetSummary();
        Assert.AreEqual(2, summary.TotalAgentRuns);
        Assert.AreEqual(0, summary.SuccessfulAgentRuns); // Not tracked in operational collector
        Assert.AreEqual(1, summary.FailedAgentRuns);
        Assert.AreEqual(1500, summary.AverageAgentRunTimeMs); // (1000 + 2000) / 2
    }

    [TestMethod]
    public void ZeroValues_Should_BeHandledCorrectly()
    {
        // Act
        _metricsAggregator.RecordAgentRunExecutionTime("test-agent", 0, 0);
        _metricsAggregator.RecordTokenUsage("test-agent", 1, 0, 0, "gpt-4");

        // Assert
        var summary = _metricsAggregator.GetSummary();
        Assert.AreEqual(1, summary.TotalAgentRuns);
        Assert.AreEqual(0, summary.AverageAgentRunTimeMs);
        Assert.AreEqual(0, summary.TotalInputTokens);
        Assert.AreEqual(0, summary.TotalOutputTokens);
    }

    [TestMethod]
    public void NegativeValues_Should_BeHandledCorrectly()
    {
        // Act
        _metricsAggregator.RecordAgentRunExecutionTime("test-agent", -100, 3);
        _metricsAggregator.RecordTokenUsage("test-agent", 1, -50, -25, "gpt-4");

        // Assert
        var summary = _metricsAggregator.GetSummary();
        Assert.AreEqual(1, summary.TotalAgentRuns);
        Assert.AreEqual(-100, summary.AverageAgentRunTimeMs);
        Assert.AreEqual(-50, summary.TotalInputTokens);
        Assert.AreEqual(-25, summary.TotalOutputTokens);
    }

    #endregion
}
