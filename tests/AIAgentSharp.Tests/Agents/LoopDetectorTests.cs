using AIAgentSharp.Agents;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class LoopDetectorTests
{
    private Mock<ILogger> _mockLogger = null!;
    private AgentConfiguration _config = null!;
    private LoopDetector _loopDetector = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _config = new AgentConfiguration 
        { 
            MaxToolCallHistory = 10,
            ConsecutiveFailureThreshold = 3
        };
        _loopDetector = new LoopDetector(_config, _mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_Should_CreateLoopDetectorSuccessfully_When_ValidParametersProvided()
    {
        // Act
        var loopDetector = new LoopDetector(_config, _mockLogger.Object);

        // Assert
        Assert.IsNotNull(loopDetector);
    }

    [TestMethod]
    public void RecordToolCall_Should_RecordSuccessfulToolCall_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };
        var success = true;

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, success);

        // Assert - No exception should be thrown
        // The method doesn't return anything, so we just verify it completes successfully
    }

    [TestMethod]
    public void RecordToolCall_Should_RecordFailedToolCall_When_ToolCallFails()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };
        var success = false;

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, success);

        // Assert - No exception should be thrown
    }

    [TestMethod]
    public void RecordToolCall_Should_HandleNullParameters_When_ParametersAreNull()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var success = true;

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, null!, success);

        // Assert - No exception should be thrown
    }

    [TestMethod]
    public void RecordToolCall_Should_HandleEmptyParameters_When_ParametersAreEmpty()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?>();
        var success = true;

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, success);

        // Assert - No exception should be thrown
    }

    [TestMethod]
    public void RecordToolCall_Should_MaintainHistoryLimit_When_ExceedingMaxHistory()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };
        var success = true;

        // Act - Record more calls than the history limit
        for (int i = 0; i < _config.MaxToolCallHistory + 5; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, success);
        }

        // Assert - No exception should be thrown
        // The method should handle the history limit internally
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_ReturnFalse_When_NoHistoryExists()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_ReturnFalse_When_NotEnoughFailures()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record fewer failures than the threshold
        for (int i = 0; i < _config.ConsecutiveFailureThreshold - 1; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_ReturnTrue_When_ThresholdReached()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record exactly the threshold number of failures
        for (int i = 0; i < _config.ConsecutiveFailureThreshold; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_ReturnTrue_When_MoreThanThresholdFailures()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record more failures than the threshold
        for (int i = 0; i < _config.ConsecutiveFailureThreshold + 2; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_ResetCounter_When_SuccessfulCallOccurs()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record some failures
        for (int i = 0; i < _config.ConsecutiveFailureThreshold - 1; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Record a successful call
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Record more failures (but not enough to reach threshold again)
        for (int i = 0; i < _config.ConsecutiveFailureThreshold - 1; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_HandleDifferentParameters_When_SameToolDifferentParams()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters1 = new Dictionary<string, object?> { { "param1", "value1" } };
        var parameters2 = new Dictionary<string, object?> { { "param1", "value2" } };

        // Record failures with different parameters
        for (int i = 0; i < _config.ConsecutiveFailureThreshold; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters1, false);
        }

        // Act - Check for different parameters
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_HandleDifferentTools_When_SameParametersDifferentTool()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName1 = "test-tool-1";
        var toolName2 = "test-tool-2";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record failures with different tools
        for (int i = 0; i < _config.ConsecutiveFailureThreshold; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName1, parameters, false);
        }

        // Act - Check for different tool
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName2, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_HandleComplexParameters_When_ParametersHaveNestedObjects()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?>
        {
            { "param1", "value1" },
            { "param2", new { nested = "value" } },
            { "param3", new[] { 1, 2, 3 } }
        };

        // Record failures
        for (int i = 0; i < _config.ConsecutiveFailureThreshold; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_HandleNullValues_When_ParametersContainNulls()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?>
        {
            { "param1", "value1" },
            { "param2", null },
            { "param3", "value3" }
        };

        // Record failures
        for (int i = 0; i < _config.ConsecutiveFailureThreshold; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_HandleMultipleAgents_When_DifferentAgentIds()
    {
        // Arrange
        var agentId1 = "test-agent-1";
        var agentId2 = "test-agent-2";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record failures for first agent
        for (int i = 0; i < _config.ConsecutiveFailureThreshold; i++)
        {
            _loopDetector.RecordToolCall(agentId1, toolName, parameters, false);
        }

        // Act - Check for second agent
        var result = _loopDetector.DetectRepeatedFailures(agentId2, toolName, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_HandleMixedSuccessAndFailure_When_InterleavedCalls()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record mixed success and failure pattern
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_HandleEmptyToolName_When_ToolNameIsEmpty()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record failures
        for (int i = 0; i < _config.ConsecutiveFailureThreshold; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_Should_HandleNullToolName_When_ToolNameIsNull()
    {
        // Arrange
        var agentId = "test-agent";
        string? toolName = null;
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Record failures
        for (int i = 0; i < _config.ConsecutiveFailureThreshold; i++)
        {
            _loopDetector.RecordToolCall(agentId, toolName!, parameters, false);
        }

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName!, parameters);

        // Assert
        Assert.IsTrue(result);
    }
}
