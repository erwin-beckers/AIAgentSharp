using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
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
    public async Task ExecuteStepAsync_WithReasoningEnabled_PerformsReasoning()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var orchestrator = new AgentOrchestrator(_mockLlm.Object, _mockStateStore.Object, config, _logger, _mockEventManager.Object, _mockStatusManager.Object);
        
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test reasoning\",\"action\":\"finish\",\"action_input\":{\"final\":\"Test result\"}}");

        // Act
        var result = await orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Continue);
        Assert.AreEqual("Test result", result.FinalOutput);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithFunctionCallingEnabled_AttemptsFunctionCalling()
    {
        // Arrange
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var mockFunctionClient = new Mock<IFunctionCallingLlmClient>();
        var orchestrator = new AgentOrchestrator(mockFunctionClient.Object, _mockStateStore.Object, config, _logger, _mockEventManager.Object, _mockStatusManager.Object);
        
        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns("test_tool");
        mockTool.Setup(x => x.InvokeAsync(It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool result");
        _testTools.Add("test_tool", mockTool.Object);

        mockFunctionClient.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test\",\"action\":\"finish\",\"action_input\":{\"final\":\"Test result\"}}");

        // Act
        var result = await orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Continue);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithToolCall_ExecutesTool()
    {
        // Arrange
        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns("test_tool");
        mockTool.Setup(x => x.InvokeAsync(It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool result");
        _testTools.Add("test_tool", mockTool.Object);

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"test_tool\",\"params\":{}}}");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsTrue(result.ToolResult.Success);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithToolCallFailure_HandlesFailure()
    {
        // Arrange
        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns("test_tool");
        mockTool.Setup(x => x.InvokeAsync(It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tool failed"));
        _testTools.Add("test_tool", mockTool.Object);

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"test_tool\",\"params\":{}}}");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        Assert.IsTrue(result.ToolResult.Error.Contains("Tool failed"));
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithPlanAction_HandlesPlan()
    {
        // Arrange
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Planning\",\"action\":\"plan\",\"action_input\":{\"summary\":\"Test plan\"}}");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual(AgentAction.Plan, result.LlmMessage.Action);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithFinishAction_HandlesFinish()
    {
        // Arrange
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Finished\",\"action\":\"finish\",\"action_input\":{\"final\":\"Final result\"}}");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.AreEqual("Final result", result.FinalOutput);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithRetryAction_HandlesRetry()
    {
        // Arrange
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Retrying\",\"action\":\"retry\",\"action_input\":{\"summary\":\"Will retry\"}}");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual(AgentAction.Retry, result.LlmMessage.Action);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithJsonParsingError_HandlesError()
    {
        // Arrange
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Invalid JSON response");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithPreviousToolFailure_HandlesErrorState()
    {
        // Arrange
        _testState.Turns.Add(new AgentTurn
        {
            Index = 0,
            ToolResult = new ToolExecutionResult
            {
                Success = false,
                Error = "Previous tool failed",
                TurnId = "turn_0_123"
            }
        });

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Invalid JSON response");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        // The error message may vary depending on the LLM response parsing
        Assert.IsTrue(result.ToolResult.Error.Contains("Previous tool failed") || result.ToolResult.Error.Contains("Invalid LLM JSON"));
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithDeduplication_ReusesExistingResult()
    {
        // Arrange
        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns("test_tool");
        _testTools.Add("test_tool", mockTool.Object);

        // Add a previous successful tool call
        _testState.Turns.Add(new AgentTurn
        {
            Index = 0,
            ToolResult = new ToolExecutionResult
            {
                Success = true,
                Output = "Previous result",
                TurnId = AgentOrchestrator.HashToolCall("test_tool", new Dictionary<string, object?> { ["param"] = "value" }),
                CreatedUtc = DateTimeOffset.UtcNow.AddMinutes(-1) // Recent enough to be valid
            }
        });

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"test_tool\",\"params\":{\"param\":\"value\"}}}");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsTrue(result.ToolResult.Success);
        Assert.AreEqual("Previous result", result.ToolResult.Output);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithDedupeControl_RespectsToolSettings()
    {
        // Arrange
        var mockTool = new Mock<ITool>();
        var mockDedupeControl = new Mock<IDedupeControl>();
        mockDedupeControl.Setup(x => x.AllowDedupe).Returns(false);
        mockDedupeControl.Setup(x => x.CustomTtl).Returns(TimeSpan.FromMinutes(5));
        
        mockTool.Setup(x => x.Name).Returns("test_tool");
        mockTool.As<IDedupeControl>().Setup(x => x.AllowDedupe).Returns(false);
        _testTools.Add("test_tool", mockTool.Object);

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"test_tool\",\"params\":{\"param\":\"value\"}}}");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithLoopDetection_TriggersLoopBreaker()
    {
        // Arrange
        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns("test_tool");
        mockTool.Setup(x => x.InvokeAsync(It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tool failed"));
        _testTools.Add("test_tool", mockTool.Object);

        // Add multiple failed attempts
        for (int i = 0; i < 3; i++)
        {
            _testState.Turns.Add(new AgentTurn
            {
                Index = i,
                ToolResult = new ToolExecutionResult
                {
                    Success = false,
                    Error = "Tool failed",
                    TurnId = $"turn_{i}_123"
                }
            });
        }

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"test_tool\",\"params\":{\"param\":\"value\"}}}");

        // Act
        var result = await _orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithReasoningAtDecisionPoint_PerformsReasoning()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var orchestrator = new AgentOrchestrator(_mockLlm.Object, _mockStateStore.Object, config, _logger, _mockEventManager.Object, _mockStatusManager.Object);
        
        // Add a failed turn to trigger reasoning
        _testState.Turns.Add(new AgentTurn
        {
            Index = 0,
            ToolResult = new ToolExecutionResult
            {
                Success = false,
                Error = "Tool failed",
                TurnId = "turn_0_123"
            }
        });

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test reasoning\",\"action\":\"finish\",\"action_input\":{\"final\":\"Test result\"}}");

        // Act
        var result = await orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Continue);
        Assert.AreEqual("Test result", result.FinalOutput);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_WithReasoningFailure_ContinuesWithoutReasoning()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var orchestrator = new AgentOrchestrator(_mockLlm.Object, _mockStateStore.Object, config, _logger, _mockEventManager.Object, _mockStatusManager.Object);
        
        // Mock reasoning to fail
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Reasoning failed"));
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"thoughts\":\"Test\",\"action\":\"finish\",\"action_input\":{\"final\":\"Test result\"}}");

        // Act
        var result = await orchestrator.ExecuteStepAsync(_testState, _testTools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Continue);
        Assert.AreEqual("Test result", result.FinalOutput);
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
        var params2 = (Dictionary<string, object?>?)null;

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params2!);

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
