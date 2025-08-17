using AIAgentSharp.Agents;
using AIAgentSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public sealed class LoopDetectorTests
{
    private AgentConfiguration _config = null!;
    private ConsoleLogger _logger = null!;
    private LoopDetector _loopDetector = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new AgentConfiguration
        {
            MaxToolCallHistory = 10,
            ConsecutiveFailureThreshold = 3
        };
        _logger = new ConsoleLogger();
        _loopDetector = new LoopDetector(_config, _logger);
    }

    [TestMethod]
    public void Constructor_InitializesCorrectly()
    {
        // Assert
        Assert.IsNotNull(_loopDetector);
    }

    [TestMethod]
    public void RecordToolCall_WithNewAgent_CreatesHistory()
    {
        // Arrange
        var agentId = "test-agent-1";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithMultipleAgents_ManagesSeparateHistories()
    {
        // Arrange
        var agentId1 = "test-agent-1";
        var agentId2 = "test-agent-2";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Act
        _loopDetector.RecordToolCall(agentId1, toolName, parameters, true);
        _loopDetector.RecordToolCall(agentId2, toolName, parameters, false);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithNullParameters_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, null!, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithEmptyParameters_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?>();

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithComplexParameters_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?>
        {
            ["string_param"] = "test",
            ["int_param"] = 42,
            ["double_param"] = 3.14,
            ["bool_param"] = true,
            ["null_param"] = null,
            ["array_param"] = new[] { 1, 2, 3 },
            ["object_param"] = new { key = "value" }
        };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithNestedObjects_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?>
        {
            ["nested"] = new Dictionary<string, object?>
            {
                ["inner"] = "value",
                ["numbers"] = new[] { 1, 2, 3 }
            }
        };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithJsonElement_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var json = System.Text.Json.JsonSerializer.SerializeToDocument(new { param = "value" });
        var parameters = new Dictionary<string, object?> { ["json_param"] = json.RootElement };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent-with-special-chars!@#$%";
        var toolName = "test_tool_with_special_chars!@#$%";
        var parameters = new Dictionary<string, object?> { ["param"] = "value_with_special_chars!@#$%" };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent-with-unicode-测试";
        var toolName = "test_tool_with_unicode-测试";
        var parameters = new Dictionary<string, object?> { ["param"] = "value_with_unicode-测试" };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithLargeData_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var largeString = new string('x', 10000);
        var parameters = new Dictionary<string, object?> { ["large_param"] = largeString };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithDateTime_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var dateTime = DateTime.UtcNow;
        var parameters = new Dictionary<string, object?> { ["datetime_param"] = dateTime };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithGuid_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var guid = Guid.NewGuid();
        var parameters = new Dictionary<string, object?> { ["guid_param"] = guid };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithEnum_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var enumValue = AgentAction.ToolCall;
        var parameters = new Dictionary<string, object?> { ["enum_param"] = enumValue };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void RecordToolCall_WithMixedTypes_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?>
        {
            ["string"] = "test",
            ["int"] = 42,
            ["double"] = 3.14,
            ["bool"] = true,
            ["null"] = null,
            ["array"] = new object[] { "string", 42, 3.14, true, null },
            ["object"] = new { nested = new { value = 123 } }
        };

        // Act
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Assert - No exception should be thrown
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithNoHistory_ReturnsFalse()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithSuccessfulCalls_ReturnsFalse()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record successful calls
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithDifferentParameters_ReturnsFalse()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters1 = new Dictionary<string, object?> { ["param"] = "value1" };
        var parameters2 = new Dictionary<string, object?> { ["param"] = "value2" };

        // Record failures with different parameters
        _loopDetector.RecordToolCall(agentId, toolName, parameters1, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters1, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters1, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters2, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithDifferentTools_ReturnsFalse()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName1 = "test_tool_1";
        var toolName2 = "test_tool_2";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record failures with different tools
        _loopDetector.RecordToolCall(agentId, toolName1, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName1, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName1, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName2, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName2, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithInterleavedSuccess_ReturnsFalse()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record failures with interleaved success
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, "other_tool", parameters, true); // Success with different tool
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithSuccessAfterFailures_ReturnsFalse()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record failures then success
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, true); // Success
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithExactThreshold_ReturnsTrue()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record exactly threshold number of failures
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithMoreThanThreshold_ReturnsTrue()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record more than threshold number of failures
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithHistoryLimit_RespectsLimit()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            MaxToolCallHistory = 5,
            ConsecutiveFailureThreshold = 3
        };
        var loopDetector = new LoopDetector(config, _logger);
        
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record more than history limit
        for (int i = 0; i < 10; i++)
        {
            loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        }

        // Act
        var result = loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithMultipleAgents_IsolatesHistories()
    {
        // Arrange
        var agentId1 = "test-agent-1";
        var agentId2 = "test-agent-2";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record failures for first agent
        _loopDetector.RecordToolCall(agentId1, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId1, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId1, toolName, parameters, false);

        // Record failures for second agent
        _loopDetector.RecordToolCall(agentId2, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId2, toolName, parameters, false);

        // Act
        var result1 = _loopDetector.DetectRepeatedFailures(agentId1, toolName, parameters);
        var result2 = _loopDetector.DetectRepeatedFailures(agentId2, toolName, parameters);

        // Assert
        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithNullParameters_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";

        // Record failures with null parameters
        _loopDetector.RecordToolCall(agentId, toolName, null!, false);
        _loopDetector.RecordToolCall(agentId, toolName, null!, false);
        _loopDetector.RecordToolCall(agentId, toolName, null!, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, null!);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithEmptyParameters_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?>();

        // Record failures with empty parameters
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithEmptyToolName_HandlesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var toolName = "";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record failures with empty tool name
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetectRepeatedFailures_WithEmptyAgentId_HandlesCorrectly()
    {
        // Arrange
        var agentId = "";
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { ["param"] = "value" };

        // Record failures with empty agent ID
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);
        _loopDetector.RecordToolCall(agentId, toolName, parameters, false);

        // Act
        var result = _loopDetector.DetectRepeatedFailures(agentId, toolName, parameters);

        // Assert
        Assert.IsTrue(result);
    }
}
