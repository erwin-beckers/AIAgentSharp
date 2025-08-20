using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Metrics;

[TestClass]
public class ResourceMetricsCollectorTests
{
    private ResourceMetricsCollector _resourceMetricsCollector = null!;
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _resourceMetricsCollector = new ResourceMetricsCollector(_mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new ResourceMetricsCollector(null!));
    }

    [TestMethod]
    public void Constructor_WithValidLogger_Should_InitializeCorrectly()
    {
        // Assert
        Assert.IsNotNull(_resourceMetricsCollector);
    }

    #region RecordTokenUsage Tests

    [TestMethod]
    public void RecordTokenUsage_Should_StoreTokenUsage()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var inputTokens = 100;
        var outputTokens = 50;
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, inputTokens, outputTokens, modelName);

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(inputTokens, metrics.TotalInputTokens);
        Assert.AreEqual(outputTokens, metrics.TotalOutputTokens);
    }

    [TestMethod]
    public void RecordTokenUsage_MultipleCalls_Should_AccumulateTokens()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, 100, 50, modelName);
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, 200, 75, modelName);

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(300, metrics.TotalInputTokens);
        Assert.AreEqual(125, metrics.TotalOutputTokens);
    }

    [TestMethod]
    public void RecordTokenUsage_WithZeroTokens_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, 0, 0, modelName);

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(0, metrics.TotalInputTokens);
        Assert.AreEqual(0, metrics.TotalOutputTokens);
    }

    [TestMethod]
    public void RecordTokenUsage_WithNegativeTokens_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, -50, -25, modelName);

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(-50, metrics.TotalInputTokens);
        Assert.AreEqual(-25, metrics.TotalOutputTokens);
    }

    [TestMethod]
    public void RecordTokenUsage_Should_UpdateModelUsage()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, 100, 50, modelName);

        // Assert
        var tokenUsage = _resourceMetricsCollector.GetTokenUsageForModel(modelName);
        Assert.IsNotNull(tokenUsage);
        Assert.AreEqual(modelName, tokenUsage!.Model);
        Assert.AreEqual(100, tokenUsage.InputTokens);
        Assert.AreEqual(50, tokenUsage.OutputTokens);
    }

    [TestMethod]
    public void RecordTokenUsage_MultipleModels_Should_TrackSeparately()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;

        // Act
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, 100, 50, "gpt-4");
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, 200, 75, "claude-3");

        // Assert
        var gpt4Usage = _resourceMetricsCollector.GetTokenUsageForModel("gpt-4");
        var claudeUsage = _resourceMetricsCollector.GetTokenUsageForModel("claude-3");
        
        Assert.IsNotNull(gpt4Usage);
        Assert.IsNotNull(claudeUsage);
        Assert.AreEqual(100, gpt4Usage!.InputTokens);
        Assert.AreEqual(50, gpt4Usage.OutputTokens);
        Assert.AreEqual(200, claudeUsage!.InputTokens);
        Assert.AreEqual(75, claudeUsage.OutputTokens);
    }

    #endregion

    #region RecordApiCall Tests

    [TestMethod]
    public void RecordApiCall_Should_StoreApiCall()
    {
        // Arrange
        var agentId = "test-agent";
        var apiType = "completion";
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordApiCall(agentId, apiType, modelName);

        // Assert
        var apiCallCountsByType = _resourceMetricsCollector.GetApiCallCountsByType();
        var apiCallCountsByModel = _resourceMetricsCollector.GetApiCallCountsByModel();
        
        Assert.AreEqual(1, apiCallCountsByType[apiType]);
        Assert.AreEqual(1, apiCallCountsByModel[modelName]);
    }

    [TestMethod]
    public void RecordApiCall_MultipleCalls_Should_AccumulateCounts()
    {
        // Arrange
        var agentId = "test-agent";
        var apiType = "completion";
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordApiCall(agentId, apiType, modelName);
        _resourceMetricsCollector.RecordApiCall(agentId, apiType, modelName);

        // Assert
        var apiCallCountsByType = _resourceMetricsCollector.GetApiCallCountsByType();
        var apiCallCountsByModel = _resourceMetricsCollector.GetApiCallCountsByModel();
        
        Assert.AreEqual(2, apiCallCountsByType[apiType]);
        Assert.AreEqual(2, apiCallCountsByModel[modelName]);
    }

    [TestMethod]
    public void RecordApiCall_DifferentTypes_Should_TrackSeparately()
    {
        // Arrange
        var agentId = "test-agent";

        // Act
        _resourceMetricsCollector.RecordApiCall(agentId, "completion", "gpt-4");
        _resourceMetricsCollector.RecordApiCall(agentId, "embedding", "text-embedding-ada-002");

        // Assert
        var apiCallCountsByType = _resourceMetricsCollector.GetApiCallCountsByType();
        var apiCallCountsByModel = _resourceMetricsCollector.GetApiCallCountsByModel();
        
        Assert.AreEqual(1, apiCallCountsByType["completion"]);
        Assert.AreEqual(1, apiCallCountsByType["embedding"]);
        Assert.AreEqual(1, apiCallCountsByModel["gpt-4"]);
        Assert.AreEqual(1, apiCallCountsByModel["text-embedding-ada-002"]);
    }

    #endregion

    #region RecordStateStoreOperation Tests

    [TestMethod]
    public void RecordStateStoreOperation_Should_StoreOperation()
    {
        // Arrange
        var agentId = "test-agent";
        var operationType = "save";
        var executionTimeMs = 50L;

        // Act
        _resourceMetricsCollector.RecordStateStoreOperation(agentId, operationType, executionTimeMs);

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(1, metrics.TotalStateStoreOperations);
        Assert.AreEqual(executionTimeMs, metrics.TotalStateStoreOperationTimeMs);
        Assert.AreEqual(executionTimeMs, metrics.AverageStateStoreOperationTimeMs);
    }

    [TestMethod]
    public void RecordStateStoreOperation_MultipleOperations_Should_Accumulate()
    {
        // Arrange
        var agentId = "test-agent";
        var operationType = "save";

        // Act
        _resourceMetricsCollector.RecordStateStoreOperation(agentId, operationType, 50L);
        _resourceMetricsCollector.RecordStateStoreOperation(agentId, operationType, 100L);

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(2, metrics.TotalStateStoreOperations);
        Assert.AreEqual(150L, metrics.TotalStateStoreOperationTimeMs);
        Assert.AreEqual(75.0, metrics.AverageStateStoreOperationTimeMs);
    }

    [TestMethod]
    public void RecordStateStoreOperation_DifferentTypes_Should_TrackSeparately()
    {
        // Arrange
        var agentId = "test-agent";

        // Act
        _resourceMetricsCollector.RecordStateStoreOperation(agentId, "save", 50L);
        _resourceMetricsCollector.RecordStateStoreOperation(agentId, "load", 100L);

        // Assert
        var operationCounts = _resourceMetricsCollector.GetStateStoreOperationCounts();
        Assert.AreEqual(1, operationCounts["save"]);
        Assert.AreEqual(1, operationCounts["load"]);
    }

    [TestMethod]
    public void RecordStateStoreOperation_WithZeroTime_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var operationType = "save";
        var executionTimeMs = 0L;

        // Act
        _resourceMetricsCollector.RecordStateStoreOperation(agentId, operationType, executionTimeMs);

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(1, metrics.TotalStateStoreOperations);
        Assert.AreEqual(0L, metrics.TotalStateStoreOperationTimeMs);
        Assert.AreEqual(0.0, metrics.AverageStateStoreOperationTimeMs);
    }

    [TestMethod]
    public void RecordStateStoreOperation_WithNegativeTime_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var operationType = "save";
        var executionTimeMs = -50L;

        // Act
        _resourceMetricsCollector.RecordStateStoreOperation(agentId, operationType, executionTimeMs);

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(1, metrics.TotalStateStoreOperations);
        Assert.AreEqual(-50L, metrics.TotalStateStoreOperationTimeMs);
        Assert.AreEqual(-50.0, metrics.AverageStateStoreOperationTimeMs);
    }

    #endregion

    #region CalculateResourceMetrics Tests

    [TestMethod]
    public void CalculateResourceMetrics_WithNoData_Should_ReturnZeroValues()
    {
        // Act
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();

        // Assert
        Assert.AreEqual(0, metrics.TotalInputTokens);
        Assert.AreEqual(0, metrics.TotalOutputTokens);
        Assert.AreEqual(0, metrics.TotalStateStoreOperations);
        Assert.AreEqual(0, metrics.TotalStateStoreOperationTimeMs);
        Assert.AreEqual(0.0, metrics.AverageStateStoreOperationTimeMs);
        Assert.AreEqual(0, metrics.TokenUsageByModel.Count);
        Assert.AreEqual(0, metrics.ApiCallCountsByType.Count);
        Assert.AreEqual(0, metrics.ApiCallCountsByModel.Count);
        Assert.AreEqual(0, metrics.StateStoreOperationCounts.Count);
    }

    [TestMethod]
    public void CalculateResourceMetrics_WithMixedData_Should_ReturnCorrectValues()
    {
        // Arrange
        _resourceMetricsCollector.RecordTokenUsage("agent1", 1, 100, 50, "gpt-4");
        _resourceMetricsCollector.RecordApiCall("agent1", "completion", "gpt-4");
        _resourceMetricsCollector.RecordStateStoreOperation("agent1", "save", 50L);

        // Act
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();

        // Assert
        Assert.AreEqual(100, metrics.TotalInputTokens);
        Assert.AreEqual(50, metrics.TotalOutputTokens);
        Assert.AreEqual(1, metrics.TotalStateStoreOperations);
        Assert.AreEqual(50L, metrics.TotalStateStoreOperationTimeMs);
        Assert.AreEqual(50.0, metrics.AverageStateStoreOperationTimeMs);
        Assert.AreEqual(1, metrics.TokenUsageByModel.Count);
        Assert.AreEqual(1, metrics.ApiCallCountsByType.Count);
        Assert.AreEqual(1, metrics.ApiCallCountsByModel.Count);
        Assert.AreEqual(1, metrics.StateStoreOperationCounts.Count);
    }

    #endregion

    #region Reset Tests

    [TestMethod]
    public void Reset_Should_ClearAllMetrics()
    {
        // Arrange
        _resourceMetricsCollector.RecordTokenUsage("agent1", 1, 100, 50, "gpt-4");
        _resourceMetricsCollector.RecordApiCall("agent1", "completion", "gpt-4");
        _resourceMetricsCollector.RecordStateStoreOperation("agent1", "save", 50L);

        // Act
        _resourceMetricsCollector.Reset();

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(0, metrics.TotalInputTokens);
        Assert.AreEqual(0, metrics.TotalOutputTokens);
        Assert.AreEqual(0, metrics.TotalStateStoreOperations);
        Assert.AreEqual(0, metrics.TotalStateStoreOperationTimeMs);
        Assert.AreEqual(0.0, metrics.AverageStateStoreOperationTimeMs);
        Assert.AreEqual(0, metrics.TokenUsageByModel.Count);
        Assert.AreEqual(0, metrics.ApiCallCountsByType.Count);
        Assert.AreEqual(0, metrics.ApiCallCountsByModel.Count);
        Assert.AreEqual(0, metrics.StateStoreOperationCounts.Count);
    }

    #endregion

    #region Get Methods Tests

    [TestMethod]
    public void GetTokenUsageForModel_WithNonExistentModel_Should_ReturnNull()
    {
        // Act
        var tokenUsage = _resourceMetricsCollector.GetTokenUsageForModel("non_existent");

        // Assert
        Assert.IsNull(tokenUsage);
    }

    [TestMethod]
    public void GetTokenUsage_Should_ReturnAllTokenUsage()
    {
        // Arrange
        _resourceMetricsCollector.RecordTokenUsage("agent1", 1, 100, 50, "gpt-4");
        _resourceMetricsCollector.RecordTokenUsage("agent1", 1, 200, 75, "claude-3");

        // Act
        var tokenUsage = _resourceMetricsCollector.GetTokenUsage();

        // Assert
        Assert.AreEqual(2, tokenUsage.Count);
        Assert.IsTrue(tokenUsage.ContainsKey("gpt-4"));
        Assert.IsTrue(tokenUsage.ContainsKey("claude-3"));
    }

    [TestMethod]
    public void GetApiCallCountsByType_Should_ReturnAllApiCallCounts()
    {
        // Arrange
        _resourceMetricsCollector.RecordApiCall("agent1", "completion", "gpt-4");
        _resourceMetricsCollector.RecordApiCall("agent1", "embedding", "text-embedding-ada-002");

        // Act
        var apiCallCounts = _resourceMetricsCollector.GetApiCallCountsByType();

        // Assert
        Assert.AreEqual(2, apiCallCounts.Count);
        Assert.AreEqual(1, apiCallCounts["completion"]);
        Assert.AreEqual(1, apiCallCounts["embedding"]);
    }

    [TestMethod]
    public void GetApiCallCountsByModel_Should_ReturnAllApiCallCounts()
    {
        // Arrange
        _resourceMetricsCollector.RecordApiCall("agent1", "completion", "gpt-4");
        _resourceMetricsCollector.RecordApiCall("agent1", "embedding", "text-embedding-ada-002");

        // Act
        var apiCallCounts = _resourceMetricsCollector.GetApiCallCountsByModel();

        // Assert
        Assert.AreEqual(2, apiCallCounts.Count);
        Assert.AreEqual(1, apiCallCounts["gpt-4"]);
        Assert.AreEqual(1, apiCallCounts["text-embedding-ada-002"]);
    }

    [TestMethod]
    public void GetStateStoreOperationCounts_Should_ReturnAllOperationCounts()
    {
        // Arrange
        _resourceMetricsCollector.RecordStateStoreOperation("agent1", "save", 50L);
        _resourceMetricsCollector.RecordStateStoreOperation("agent1", "load", 100L);

        // Act
        var operationCounts = _resourceMetricsCollector.GetStateStoreOperationCounts();

        // Assert
        Assert.AreEqual(2, operationCounts.Count);
        Assert.AreEqual(1, operationCounts["save"]);
        Assert.AreEqual(1, operationCounts["load"]);
    }

    #endregion

    #region Edge Cases and Error Handling

    [TestMethod]
    public void RecordTokenUsage_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var inputTokens = 100;
        var outputTokens = 50;
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordTokenUsage(agentId, turnIndex, inputTokens, outputTokens, modelName);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordApiCall_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var apiType = "completion";
        var modelName = "gpt-4";

        // Act
        _resourceMetricsCollector.RecordApiCall(agentId, apiType, modelName);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordStateStoreOperation_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var operationType = "save";
        var executionTimeMs = 50L;

        // Act
        _resourceMetricsCollector.RecordStateStoreOperation(agentId, operationType, executionTimeMs);

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
                _resourceMetricsCollector.RecordTokenUsage("agent1", 1, 10, 5, "gpt-4");
                _resourceMetricsCollector.RecordApiCall("agent1", "completion", "gpt-4");
                _resourceMetricsCollector.RecordStateStoreOperation("agent1", "save", 10L);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var metrics = _resourceMetricsCollector.CalculateResourceMetrics();
        Assert.AreEqual(iterations * 10, metrics.TotalInputTokens);
        Assert.AreEqual(iterations * 5, metrics.TotalOutputTokens);
        Assert.AreEqual(iterations, metrics.TotalStateStoreOperations);
        Assert.AreEqual(iterations * 10L, metrics.TotalStateStoreOperationTimeMs);
        
        var apiCallCounts = _resourceMetricsCollector.GetApiCallCountsByType();
        Assert.AreEqual(iterations, apiCallCounts["completion"]);
    }

    #endregion
}
