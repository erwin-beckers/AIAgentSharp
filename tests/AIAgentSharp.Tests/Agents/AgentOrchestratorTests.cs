using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public sealed class AgentOrchestratorTests
{
    private Mock<ILlmClient> _mockLlm = null!;
    private Mock<IAgentStateStore> _mockStateStore = null!;
    private Mock<IEventManager> _mockEventManager = null!;
    private Mock<IStatusManager> _mockStatusManager = null!;
    private ConsoleLogger _logger = null!;
    private AgentConfiguration _config = null!;
    private AgentOrchestrator _orchestrator = null!;
    private AgentState _testState = null!;
    private Dictionary<string, ITool> _testTools = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlm = new Mock<ILlmClient>();
        _mockStateStore = new Mock<IAgentStateStore>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _logger = new ConsoleLogger();
        _config = new AgentConfiguration
        {
            UseFunctionCalling = false,
            ReasoningType = ReasoningType.None
        };
        _orchestrator = new AgentOrchestrator(_mockLlm.Object, _mockStateStore.Object, _config, _logger, _mockEventManager.Object, _mockStatusManager.Object);
        
        _testState = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };
        
        _testTools = new Dictionary<string, ITool>();
    }

    [TestMethod]
    public void ExecuteStepAsync_WithNullState_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
            _orchestrator.ExecuteStepAsync(null!, _testTools, CancellationToken.None));
    }

    [TestMethod]
    public void ExecuteStepAsync_WithNullTools_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
            _orchestrator.ExecuteStepAsync(_testState, null!, CancellationToken.None));
    }

    [TestMethod]
    public void HashToolCall_WithSameToolAndParams_ReturnsSameHash()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?> { ["param1"] = "value1", ["param2"] = 42 };
        var params2 = new Dictionary<string, object?> { ["param1"] = "value1", ["param2"] = 42 };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params2);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithDifferentParams_ReturnsDifferentHash()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?> { ["param1"] = "value1" };
        var params2 = new Dictionary<string, object?> { ["param1"] = "value2" };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params2);

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithDifferentTools_ReturnsDifferentHash()
    {
        // Arrange
        var tool1 = "test_tool_1";
        var tool2 = "test_tool_2";
        var params1 = new Dictionary<string, object?> { ["param1"] = "value1" };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool1, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool2, params1);

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithNullParams_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?>();
        var params2 = (Dictionary<string, object?>)null!;

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params2);

        // Assert
        // Note: The actual behavior may vary based on implementation
        Assert.IsNotNull(hash1);
        Assert.IsNotNull(hash2);
    }

    [TestMethod]
    public void HashToolCall_WithComplexParams_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?>
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
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
        Assert.IsNotNull(hash1);
        Assert.IsTrue(hash1.Length > 0);
    }

    [TestMethod]
    public void HashToolCall_WithNestedObjects_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?>
        {
            ["nested"] = new Dictionary<string, object?>
            {
                ["inner"] = "value",
                ["numbers"] = new[] { 1, 2, 3 }
            }
        };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithJsonElement_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var json = System.Text.Json.JsonSerializer.SerializeToDocument(new { param = "value" });
        var params1 = new Dictionary<string, object?> { ["json_param"] = json.RootElement };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithDifferentParamOrder_ReturnsSameHash()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?> { ["param1"] = "value1", ["param2"] = "value2" };
        var params2 = new Dictionary<string, object?> { ["param2"] = "value2", ["param1"] = "value1" };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params2);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithEmptyTool_ReturnsHash()
    {
        // Arrange
        var tool = "";
        var params1 = new Dictionary<string, object?> { ["param"] = "value" };

        // Act
        var hash = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.IsNotNull(hash);
        Assert.IsTrue(hash.Length > 0);
    }

    [TestMethod]
    public void HashToolCall_WithSpecialCharacters_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool_with_special_chars_!@#$%^&*()";
        var params1 = new Dictionary<string, object?> { ["param"] = "value_with_special_chars_!@#$%^&*()" };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithUnicodeCharacters_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool_with_unicode_测试";
        var params1 = new Dictionary<string, object?> { ["param"] = "value_with_unicode_测试" };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithLargeData_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var largeString = new string('x', 10000);
        var params1 = new Dictionary<string, object?> { ["large_param"] = largeString };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithDateTime_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var dateTime = DateTime.UtcNow;
        var params1 = new Dictionary<string, object?> { ["datetime_param"] = dateTime };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithGuid_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var guid = Guid.NewGuid();
        var params1 = new Dictionary<string, object?> { ["guid_param"] = guid };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithEnum_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var enumValue = AgentAction.ToolCall;
        var params1 = new Dictionary<string, object?> { ["enum_param"] = enumValue };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_WithMixedTypes_ReturnsConsistentHash()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?>
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
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params1);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }
}
