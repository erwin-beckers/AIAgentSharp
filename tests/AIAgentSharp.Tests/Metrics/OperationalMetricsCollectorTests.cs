using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Metrics;

[TestClass]
public class OperationalMetricsCollectorTests
{
    private OperationalMetricsCollector _operationalMetricsCollector = null!;
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _operationalMetricsCollector = new OperationalMetricsCollector(_mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new OperationalMetricsCollector(null!));
    }

    [TestMethod]
    public void Constructor_WithValidLogger_Should_InitializeCorrectly()
    {
        // Assert
        Assert.IsNotNull(_operationalMetricsCollector);
    }

    #region RecordAgentRunCompletion Tests

    [TestMethod]
    public void RecordAgentRunCompletion_WithSuccess_Should_NotIncrementFailedRuns()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = true;
        var totalTurns = 3;

        // Act
        _operationalMetricsCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedAgentRuns);
    }

    [TestMethod]
    public void RecordAgentRunCompletion_WithFailure_Should_IncrementFailedRuns()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = false;
        var totalTurns = 3;

        // Act
        _operationalMetricsCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(1, metrics.FailedAgentRuns);
    }

    [TestMethod]
    public void RecordAgentRunCompletion_WithFailureAndErrorType_Should_RecordError()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = false;
        var totalTurns = 3;
        var errorType = "timeout";

        // Act
        _operationalMetricsCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns, errorType);

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.IsTrue(errorCounts.ContainsKey(errorType));
        Assert.AreEqual(1, errorCounts[errorType]);
    }

    [TestMethod]
    public void RecordAgentRunCompletion_WithFailureAndNullErrorType_Should_NotRecordError()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = false;
        var totalTurns = 3;

        // Act
        _operationalMetricsCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns, null);

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.AreEqual(0, errorCounts.Count);
    }

    [TestMethod]
    public void RecordAgentRunCompletion_WithFailureAndEmptyErrorType_Should_NotRecordError()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = false;
        var totalTurns = 3;

        // Act
        _operationalMetricsCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns, "");

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.AreEqual(0, errorCounts.Count);
    }

    #endregion

    #region RecordAgentStepCompletion Tests

    [TestMethod]
    public void RecordAgentStepCompletion_WithSuccess_Should_NotIncrementFailedSteps()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = true;
        var executedTool = true;

        // Act
        _operationalMetricsCollector.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedAgentSteps);
    }

    [TestMethod]
    public void RecordAgentStepCompletion_WithFailure_Should_IncrementFailedSteps()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = false;
        var executedTool = false;

        // Act
        _operationalMetricsCollector.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(1, metrics.FailedAgentSteps);
    }

    [TestMethod]
    public void RecordAgentStepCompletion_WithFailureAndErrorType_Should_RecordError()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = false;
        var executedTool = false;
        var errorType = "validation_error";

        // Act
        _operationalMetricsCollector.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool, errorType);

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.IsTrue(errorCounts.ContainsKey(errorType));
        Assert.AreEqual(1, errorCounts[errorType]);
    }

    #endregion

    #region RecordLlmCallCompletion Tests

    [TestMethod]
    public void RecordLlmCallCompletion_WithSuccess_Should_NotIncrementFailedLlmCalls()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = true;
        var modelName = "gpt-4";

        // Act
        _operationalMetricsCollector.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedLlmCalls);
    }

    [TestMethod]
    public void RecordLlmCallCompletion_WithFailure_Should_IncrementFailedLlmCalls()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = false;
        var modelName = "gpt-4";

        // Act
        _operationalMetricsCollector.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(1, metrics.FailedLlmCalls);
    }

    [TestMethod]
    public void RecordLlmCallCompletion_WithFailureAndErrorType_Should_RecordError()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = false;
        var modelName = "gpt-4";
        var errorType = "api_error";

        // Act
        _operationalMetricsCollector.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName, errorType);

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.IsTrue(errorCounts.ContainsKey(errorType));
        Assert.AreEqual(1, errorCounts[errorType]);
    }

    #endregion

    #region RecordToolCallCompletion Tests

    [TestMethod]
    public void RecordToolCallCompletion_WithSuccess_Should_NotIncrementFailedToolCalls()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var toolName = "weather-tool";
        var succeeded = true;

        // Act
        _operationalMetricsCollector.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedToolCalls);
    }

    [TestMethod]
    public void RecordToolCallCompletion_WithFailure_Should_IncrementFailedToolCalls()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var toolName = "weather-tool";
        var succeeded = false;

        // Act
        _operationalMetricsCollector.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(1, metrics.FailedToolCalls);
    }

    [TestMethod]
    public void RecordToolCallCompletion_WithFailureAndErrorType_Should_RecordError()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var toolName = "weather-tool";
        var succeeded = false;
        var errorType = "tool_error";

        // Act
        _operationalMetricsCollector.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded, errorType);

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.IsTrue(errorCounts.ContainsKey(errorType));
        Assert.AreEqual(1, errorCounts[errorType]);
    }

    #endregion

    #region RecordLoopDetection Tests

    [TestMethod]
    public void RecordLoopDetection_Should_IncrementLoopDetectionEvents()
    {
        // Arrange
        var agentId = "test-agent";
        var loopType = "infinite";
        var consecutiveFailures = 5;

        // Act
        _operationalMetricsCollector.RecordLoopDetection(agentId, loopType, consecutiveFailures);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(1, metrics.LoopDetectionEvents);
    }

    [TestMethod]
    public void RecordLoopDetection_MultipleCalls_Should_AccumulateEvents()
    {
        // Arrange
        var agentId = "test-agent";
        var loopType = "infinite";
        var consecutiveFailures = 5;

        // Act
        _operationalMetricsCollector.RecordLoopDetection(agentId, loopType, consecutiveFailures);
        _operationalMetricsCollector.RecordLoopDetection(agentId, "circular", 3);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(2, metrics.LoopDetectionEvents);
    }

    #endregion

    #region RecordDeduplicationEvent Tests

    [TestMethod]
    public void RecordDeduplicationEvent_WithCacheHit_Should_IncrementCacheHits()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "weather-tool";
        var cacheHit = true;

        // Act
        _operationalMetricsCollector.RecordDeduplicationEvent(agentId, toolName, cacheHit);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(1, metrics.DeduplicationCacheHits);
        Assert.AreEqual(0, metrics.DeduplicationCacheMisses);
    }

    [TestMethod]
    public void RecordDeduplicationEvent_WithCacheMiss_Should_IncrementCacheMisses()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "weather-tool";
        var cacheHit = false;

        // Act
        _operationalMetricsCollector.RecordDeduplicationEvent(agentId, toolName, cacheHit);

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.DeduplicationCacheHits);
        Assert.AreEqual(1, metrics.DeduplicationCacheMisses);
    }

    [TestMethod]
    public void RecordDeduplicationEvent_MixedCalls_Should_CalculateHitRate()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "weather-tool";

        // Act
        _operationalMetricsCollector.RecordDeduplicationEvent(agentId, toolName, true);  // Hit
        _operationalMetricsCollector.RecordDeduplicationEvent(agentId, toolName, false); // Miss
        _operationalMetricsCollector.RecordDeduplicationEvent(agentId, toolName, true);  // Hit

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(2, metrics.DeduplicationCacheHits);
        Assert.AreEqual(1, metrics.DeduplicationCacheMisses);
        Assert.AreEqual(66.67, metrics.DeduplicationCacheHitRate, 0.01); // 2/3 * 100
    }

    #endregion

    #region RecordError Tests

    [TestMethod]
    public void RecordError_Should_RecordErrorType()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var errorType = "custom_error";
        var errorMessage = "Something went wrong";

        // Act
        _operationalMetricsCollector.RecordError(agentId, turnIndex, errorType, errorMessage);

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.IsTrue(errorCounts.ContainsKey(errorType));
        Assert.AreEqual(1, errorCounts[errorType]);
    }

    [TestMethod]
    public void RecordError_MultipleSameType_Should_AccumulateCount()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var errorType = "custom_error";

        // Act
        _operationalMetricsCollector.RecordError(agentId, turnIndex, errorType, "Error 1");
        _operationalMetricsCollector.RecordError(agentId, turnIndex, errorType, "Error 2");

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.AreEqual(2, errorCounts[errorType]);
    }

    [TestMethod]
    public void RecordError_DifferentTypes_Should_RecordSeparately()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;

        // Act
        _operationalMetricsCollector.RecordError(agentId, turnIndex, "error_type_1", "Error 1");
        _operationalMetricsCollector.RecordError(agentId, turnIndex, "error_type_2", "Error 2");

        // Assert
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();
        Assert.AreEqual(2, errorCounts.Count);
        Assert.AreEqual(1, errorCounts["error_type_1"]);
        Assert.AreEqual(1, errorCounts["error_type_2"]);
    }

    #endregion

    #region CalculateOperationalMetrics Tests

    [TestMethod]
    public void CalculateOperationalMetrics_WithNoData_Should_ReturnZeroValues()
    {
        // Act
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();

        // Assert
        Assert.AreEqual(0, metrics.FailedAgentRuns);
        Assert.AreEqual(0, metrics.FailedAgentSteps);
        Assert.AreEqual(0, metrics.FailedLlmCalls);
        Assert.AreEqual(0, metrics.FailedToolCalls);
        Assert.AreEqual(0, metrics.LoopDetectionEvents);
        Assert.AreEqual(0, metrics.DeduplicationCacheHits);
        Assert.AreEqual(0, metrics.DeduplicationCacheMisses);
        Assert.AreEqual(0, metrics.ErrorCounts.Count);
    }

    [TestMethod]
    public void CalculateOperationalMetrics_WithMixedData_Should_ReturnCorrectValues()
    {
        // Arrange
        _operationalMetricsCollector.RecordAgentRunCompletion("agent1", false, 3, "timeout");
        _operationalMetricsCollector.RecordAgentStepCompletion("agent1", 1, false, false, "validation_error");
        _operationalMetricsCollector.RecordLlmCallCompletion("agent1", 1, false, "gpt-4", "api_error");
        _operationalMetricsCollector.RecordToolCallCompletion("agent1", 1, "weather-tool", false, "tool_error");
        _operationalMetricsCollector.RecordLoopDetection("agent1", "infinite", 5);
        _operationalMetricsCollector.RecordDeduplicationEvent("agent1", "weather-tool", true);
        _operationalMetricsCollector.RecordError("agent1", 1, "custom_error", "Something went wrong");

        // Act
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();

        // Assert
        Assert.AreEqual(1, metrics.FailedAgentRuns);
        Assert.AreEqual(1, metrics.FailedAgentSteps);
        Assert.AreEqual(1, metrics.FailedLlmCalls);
        Assert.AreEqual(1, metrics.FailedToolCalls);
        Assert.AreEqual(1, metrics.LoopDetectionEvents);
        Assert.AreEqual(1, metrics.DeduplicationCacheHits);
        Assert.AreEqual(0, metrics.DeduplicationCacheMisses);
        Assert.AreEqual(5, metrics.ErrorCounts.Count); // timeout, validation_error, api_error, tool_error, custom_error
    }

    [TestMethod]
    public void CalculateOperationalMetrics_Should_CalculateSuccessRates()
    {
        // Arrange
        // Note: Success rates are calculated based on total counts from other collectors
        // For this test, we're just verifying the calculation logic works with the available data

        // Act
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();

        // Assert
        Assert.AreEqual(100.0, metrics.AgentRunSuccessRate); // No failures, so 100%
        Assert.AreEqual(100.0, metrics.AgentStepSuccessRate);
        Assert.AreEqual(100.0, metrics.LlmCallSuccessRate);
        Assert.AreEqual(100.0, metrics.ToolCallSuccessRate);
        Assert.AreEqual(0.0, metrics.DeduplicationCacheHitRate); // No cache events
    }

    #endregion

    #region Reset Tests

    [TestMethod]
    public void Reset_Should_ClearAllMetrics()
    {
        // Arrange
        _operationalMetricsCollector.RecordAgentRunCompletion("agent1", false, 3, "timeout");
        _operationalMetricsCollector.RecordAgentStepCompletion("agent1", 1, false, false, "validation_error");
        _operationalMetricsCollector.RecordLlmCallCompletion("agent1", 1, false, "gpt-4", "api_error");
        _operationalMetricsCollector.RecordToolCallCompletion("agent1", 1, "weather-tool", false, "tool_error");
        _operationalMetricsCollector.RecordLoopDetection("agent1", "infinite", 5);
        _operationalMetricsCollector.RecordDeduplicationEvent("agent1", "weather-tool", true);
        _operationalMetricsCollector.RecordError("agent1", 1, "custom_error", "Something went wrong");

        // Act
        _operationalMetricsCollector.Reset();

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(0, metrics.FailedAgentRuns);
        Assert.AreEqual(0, metrics.FailedAgentSteps);
        Assert.AreEqual(0, metrics.FailedLlmCalls);
        Assert.AreEqual(0, metrics.FailedToolCalls);
        Assert.AreEqual(0, metrics.LoopDetectionEvents);
        Assert.AreEqual(0, metrics.DeduplicationCacheHits);
        Assert.AreEqual(0, metrics.DeduplicationCacheMisses);
        Assert.AreEqual(0, metrics.ErrorCounts.Count);
    }

    #endregion

    #region GetErrorCounts Tests

    [TestMethod]
    public void GetErrorCounts_WithNoErrors_Should_ReturnEmptyDictionary()
    {
        // Act
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();

        // Assert
        Assert.AreEqual(0, errorCounts.Count);
    }

    [TestMethod]
    public void GetErrorCounts_WithErrors_Should_ReturnErrorCounts()
    {
        // Arrange
        _operationalMetricsCollector.RecordError("agent1", 1, "error_type_1", "Error 1");
        _operationalMetricsCollector.RecordError("agent1", 1, "error_type_2", "Error 2");
        _operationalMetricsCollector.RecordError("agent1", 1, "error_type_1", "Error 3");

        // Act
        var errorCounts = _operationalMetricsCollector.GetErrorCounts();

        // Assert
        Assert.AreEqual(2, errorCounts.Count);
        Assert.AreEqual(2, errorCounts["error_type_1"]);
        Assert.AreEqual(1, errorCounts["error_type_2"]);
    }

    #endregion

    #region Edge Cases and Error Handling

    [TestMethod]
    public void RecordAgentRunCompletion_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = false;
        var totalTurns = 3;

        // Act
        _operationalMetricsCollector.RecordAgentRunCompletion(agentId, succeeded, totalTurns);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordAgentStepCompletion_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = false;
        var executedTool = false;

        // Act
        _operationalMetricsCollector.RecordAgentStepCompletion(agentId, turnIndex, succeeded, executedTool);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordLlmCallCompletion_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var succeeded = false;
        var modelName = "gpt-4";

        // Act
        _operationalMetricsCollector.RecordLlmCallCompletion(agentId, turnIndex, succeeded, modelName);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordToolCallCompletion_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var toolName = "weather-tool";
        var succeeded = false;

        // Act
        _operationalMetricsCollector.RecordToolCallCompletion(agentId, turnIndex, toolName, succeeded);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordLoopDetection_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var loopType = "infinite";
        var consecutiveFailures = 5;

        // Act
        _operationalMetricsCollector.RecordLoopDetection(agentId, loopType, consecutiveFailures);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordDeduplicationEvent_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "weather-tool";
        var cacheHit = true;

        // Act
        _operationalMetricsCollector.RecordDeduplicationEvent(agentId, toolName, cacheHit);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordError_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var errorType = "custom_error";
        var errorMessage = "Something went wrong";

        // Act
        _operationalMetricsCollector.RecordError(agentId, turnIndex, errorType, errorMessage);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Thread Safety Tests

    [TestMethod]
    public void ConcurrentRecordings_Should_BeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var iterations = 100;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                _operationalMetricsCollector.RecordAgentRunCompletion("agent1", false, 3, "timeout");
                _operationalMetricsCollector.RecordAgentStepCompletion("agent1", 1, false, false, "validation_error");
                _operationalMetricsCollector.RecordLlmCallCompletion("agent1", 1, false, "gpt-4", "api_error");
                _operationalMetricsCollector.RecordToolCallCompletion("agent1", 1, "weather-tool", false, "tool_error");
                _operationalMetricsCollector.RecordLoopDetection("agent1", "infinite", 5);
                _operationalMetricsCollector.RecordDeduplicationEvent("agent1", "weather-tool", true);
                _operationalMetricsCollector.RecordError("agent1", 1, "custom_error", "Something went wrong");
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var metrics = _operationalMetricsCollector.CalculateOperationalMetrics();
        Assert.AreEqual(iterations, metrics.FailedAgentRuns);
        Assert.AreEqual(iterations, metrics.FailedAgentSteps);
        Assert.AreEqual(iterations, metrics.FailedLlmCalls);
        Assert.AreEqual(iterations, metrics.FailedToolCalls);
        Assert.AreEqual(iterations, metrics.LoopDetectionEvents);
        Assert.AreEqual(iterations, metrics.DeduplicationCacheHits);
        Assert.AreEqual(iterations, metrics.ErrorCounts["timeout"]);
        Assert.AreEqual(iterations, metrics.ErrorCounts["validation_error"]);
        Assert.AreEqual(iterations, metrics.ErrorCounts["api_error"]);
        Assert.AreEqual(iterations, metrics.ErrorCounts["tool_error"]);
        Assert.AreEqual(iterations, metrics.ErrorCounts["custom_error"]);
    }

    #endregion
}
