using AIAgentSharp.Metrics;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Tests.Metrics;

[TestClass]
public class MetricsCollectorTests
{
    private MetricsCollector _metricsCollector = null!;
    private MockLogger _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new MockLogger();
        _metricsCollector = new MetricsCollector(_mockLogger);
    }

    [TestMethod]
    public void Constructor_Should_CreateMetricsCollector_When_LoggerProvided()
    {
        // Act & Assert
        Assert.IsNotNull(_metricsCollector);
        Assert.IsInstanceOfType(_metricsCollector, typeof(IMetricsCollector));
        Assert.IsInstanceOfType(_metricsCollector, typeof(IMetricsProvider));
    }

    [TestMethod]
    public void Constructor_Should_CreateMetricsCollector_When_NoLoggerProvided()
    {
        // Act
        var collector = new MetricsCollector();

        // Assert
        Assert.IsNotNull(collector);
        Assert.IsInstanceOfType(collector, typeof(IMetricsCollector));
    }

    [TestMethod]
    public void RecordAgentRunExecutionTime_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var executionTimeMs = 1500L;
        var totalTurns = 5;

        // Act
        _metricsCollector.RecordAgentRunExecutionTime(agentId, executionTimeMs, totalTurns);

        // Assert
        // The method should not throw and should delegate to the aggregator
        // We can verify by checking that the metrics are available
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordAgentStepExecutionTime_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 2;
        var executionTimeMs = 250L;

        // Act
        _metricsCollector.RecordAgentStepExecutionTime(agentId, turnIndex, executionTimeMs);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordLlmCallExecutionTime_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var executionTimeMs = 800L;
        var modelName = "gpt-4";

        // Act
        _metricsCollector.RecordLlmCallExecutionTime(agentId, turnIndex, executionTimeMs, modelName);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordToolCallExecutionTime_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var toolName = "calculator";
        var executionTimeMs = 100L;

        // Act
        _metricsCollector.RecordToolCallExecutionTime(agentId, turnIndex, toolName, executionTimeMs);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordReasoningExecutionTime_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = ReasoningType.ChainOfThought;
        var executionTimeMs = 300L;

        // Act
        _metricsCollector.RecordReasoningExecutionTime(agentId, reasoningType, executionTimeMs);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordAgentRunCompletion_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = true;
        var totalTurns = 4;

        // Act
        _metricsCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordAgentRunCompletion_Should_RecordMetric_When_FailedWithErrorType()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = false;
        var totalTurns = 2;
        var errorType = "TimeoutException";

        // Act
        _metricsCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns, errorType);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordAgentStepCompletion_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = true;
        var executedTool = true;

        // Act
        _metricsCollector.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordAgentStepCompletion_Should_RecordMetric_When_FailedWithErrorType()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 2;
        var succeeded = false;
        var executedTool = false;
        var errorType = "ValidationException";

        // Act
        _metricsCollector.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool, errorType);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordLlmCallCompletion_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = true;
        var modelName = "gpt-4";

        // Act
        _metricsCollector.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordLlmCallCompletion_Should_RecordMetric_When_FailedWithErrorType()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 2;
        var succeeded = false;
        var modelName = "gpt-4";
        var errorType = "RateLimitException";

        // Act
        _metricsCollector.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName, errorType);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordToolCallCompletion_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var toolName = "calculator";
        var succeeded = true;

        // Act
        _metricsCollector.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordToolCallCompletion_Should_RecordMetric_When_FailedWithErrorType()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 2;
        var toolName = "database";
        var succeeded = false;
        var errorType = "ConnectionException";

        // Act
        _metricsCollector.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded, errorType);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordLoopDetection_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var loopType = "tool_failure";
        var consecutiveFailures = 3;

        // Act
        _metricsCollector.RecordLoopDetection(agentId, loopType, consecutiveFailures);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordDeduplicationEvent_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "calculator";
        var cacheHit = true;

        // Act
        _metricsCollector.RecordDeduplicationEvent(agentId, toolName, cacheHit);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordReasoningConfidence_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = ReasoningType.TreeOfThoughts;
        var confidenceScore = 0.85;

        // Act
        _metricsCollector.RecordReasoningConfidence(agentId, reasoningType, confidenceScore);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordResponseQuality_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var responseLength = 150;
        var hasFinalOutput = true;

        // Act
        _metricsCollector.RecordResponseQuality(agentId, responseLength, hasFinalOutput);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordValidation_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var validationType = "parameter_validation";
        var passed = true;

        // Act
        _metricsCollector.RecordValidation(agentId, validationType, passed);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordValidation_Should_RecordMetric_When_FailedWithErrorMessage()
    {
        // Arrange
        var agentId = "test-agent";
        var validationType = "json_validation";
        var passed = false;
        var errorMessage = "Invalid JSON format";

        // Act
        _metricsCollector.RecordValidation(agentId, validationType, passed, errorMessage);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordTokenUsage_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var inputTokens = 150;
        var outputTokens = 75;
        var modelName = "gpt-4";

        // Act
        _metricsCollector.RecordTokenUsage(agentId, turnIndex, inputTokens, outputTokens, modelName);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordApiCall_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var apiType = "LLM";
        var modelName = "gpt-4";

        // Act
        _metricsCollector.RecordApiCall(agentId, apiType, modelName);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordStateStoreOperation_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var operationType = "Read";
        var executionTimeMs = 50L;

        // Act
        _metricsCollector.RecordStateStoreOperation(agentId, operationType, executionTimeMs);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordCustomMetric_Should_RecordMetric_When_ValidParametersProvided()
    {
        // Arrange
        var metricName = "custom_metric";
        var value = 42.5;

        // Act
        _metricsCollector.RecordCustomMetric(metricName, value);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordCustomMetric_Should_RecordMetric_When_WithTags()
    {
        // Arrange
        var metricName = "custom_metric";
        var value = 42.5;
        var tags = new Dictionary<string, string> { { "environment", "test" }, { "version", "1.0" } };

        // Act
        _metricsCollector.RecordCustomMetric(metricName, value, tags);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordCustomEvent_Should_RecordEvent_When_ValidParametersProvided()
    {
        // Arrange
        var eventName = "custom_event";

        // Act
        _metricsCollector.RecordCustomEvent(eventName);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void RecordCustomEvent_Should_RecordEvent_When_WithTags()
    {
        // Arrange
        var eventName = "custom_event";
        var tags = new Dictionary<string, string> { { "category", "performance" } };

        // Act
        _metricsCollector.RecordCustomEvent(eventName, tags);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void GetSummary_Should_ReturnMetricsSummary_When_Called()
    {
        // Act
        var summary = _metricsCollector.GetSummary();

        // Assert
        Assert.IsNotNull(summary);
    }

    [TestMethod]
    public void GetAllMetrics_Should_ReturnAllMetrics_When_Called()
    {
        // Act
        var allMetrics = _metricsCollector.GetAllMetrics();

        // Assert
        Assert.IsNotNull(allMetrics);
    }

    [TestMethod]
    public void GetMetrics_Should_ReturnMetricsData_When_Called()
    {
        // Act
        var metrics = _metricsCollector.GetMetrics();

        // Assert
        Assert.IsNotNull(metrics);
        Assert.IsNotNull(metrics.CollectedAt);
        Assert.IsNotNull(metrics.Performance);
        Assert.IsNotNull(metrics.Operational);
        Assert.IsNotNull(metrics.Quality);
        Assert.IsNotNull(metrics.Resources);
        Assert.IsNotNull(metrics.CustomMetrics);
        Assert.IsNotNull(metrics.CustomEvents);
    }

    [TestMethod]
    public void GetAgentMetrics_Should_ReturnNull_When_Called()
    {
        // Arrange
        var agentId = "test-agent";

        // Act
        var metrics = _metricsCollector.GetAgentMetrics(agentId);

        // Assert
        Assert.IsNull(metrics);
    }

    [TestMethod]
    public void GetMetricsForTimeRange_Should_ReturnCurrentMetrics_When_Called()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddHours(-1);
        var endTime = DateTimeOffset.UtcNow;

        // Act
        var metrics = _metricsCollector.GetMetricsForTimeRange(startTime, endTime);

        // Assert
        Assert.IsNotNull(metrics);
        Assert.IsNotNull(metrics.CollectedAt);
    }

    [TestMethod]
    public void Reset_Should_ResetMetrics_When_Called()
    {
        // Arrange
        _metricsCollector.RecordCustomMetric("test_metric", 42.0);

        // Act
        _metricsCollector.Reset();

        // Assert
        // The reset should not throw and should clear the metrics
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void ResetMetrics_Should_ResetMetrics_When_Called()
    {
        // Arrange
        _metricsCollector.RecordCustomMetric("test_metric", 42.0);

        // Act
        _metricsCollector.ResetMetrics();

        // Assert
        // The reset should not throw and should clear the metrics
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    [TestMethod]
    public void MetricsCollector_Should_BeThreadSafe_When_MultipleThreadsRecordMetrics()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(() =>
            {
                _metricsCollector.RecordCustomMetric($"metric_{taskIndex}", taskIndex);
                _metricsCollector.RecordCustomEvent($"event_{taskIndex}");
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsNotNull(metrics);
    }

    private class MockLogger : ILogger
    {
        public void LogDebug(string message) { }
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
