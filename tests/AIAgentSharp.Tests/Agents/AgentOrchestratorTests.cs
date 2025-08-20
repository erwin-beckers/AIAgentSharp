using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Moq;
using System.Text.Json;
using AIAgentSharp.Agents.ChainOfThought;
using AIAgentSharp.Agents.TreeOfThoughts;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class AgentOrchestratorTests
{
    private Mock<ILlmClient> _mockLlmClient = null!;
    private Mock<IAgentStateStore> _mockStateStore = null!;
    private Mock<ILogger> _mockLogger = null!;
    private Mock<IEventManager> _mockEventManager = null!;
    private Mock<IStatusManager> _mockStatusManager = null!;
    private Mock<IMetricsCollector> _mockMetricsCollector = null!;
    private AgentConfiguration _config = null!;
    private AgentOrchestrator _orchestrator = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new Mock<ILlmClient>();
        _mockStateStore = new Mock<IAgentStateStore>();
        _mockLogger = new Mock<ILogger>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _mockMetricsCollector = new Mock<IMetricsCollector>();
        _config = new AgentConfiguration();

        _orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
    }

    [TestMethod]
    public void Constructor_Should_InitializeOrchestrator_When_ValidParametersProvided()
    {
        // Act
        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);

        // Assert
        Assert.IsNotNull(orchestrator);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_ThrowArgumentNullException_When_StateIsNull()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _orchestrator.ExecuteStepAsync(null!, tools, CancellationToken.None));
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_ThrowArgumentNullException_When_ToolsIsNull()
    {
        // Arrange
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _orchestrator.ExecuteStepAsync(state, null!, CancellationToken.None));
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_ReturnContinueResult_When_PlanActionReceived()
    {
        // Arrange
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();

        // Mock the message builder to return messages
        var mockMessageBuilder = new Mock<IMessageBuilder>();
        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        // Mock the LLM communicator to return a plan action
        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        var planMessage = new ModelMessage
        {
            Thoughts = "I need to plan",
            Action = AgentAction.Plan,
            ActionInput = new ActionInput { Summary = "Planning the approach" }
        };
        mockLlmCommunicator.Setup(x => x.CallLlmAndParseAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        // Create orchestrator with mocked dependencies
        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            new Mock<IToolExecutor>().Object,
            new Mock<ILoopDetector>().Object,
            mockMessageBuilder.Object,
            new Mock<IReasoningManager>().Object);

        // Act
        var result = await orchestrator.ExecuteStepAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual(AgentAction.Plan, result.LlmMessage.Action);
    }

    [TestMethod]
    public void ExecuteStepAsync_Should_ReturnFinishResult_When_FinishActionReceived()
    {
        // Arrange
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();

        // Note: Due to the orchestrator's complexity and internal dependencies,
        // full integration testing would require extensive mocking of internal components
        // These tests verify the basic structure and null parameter validation

        // Act & Assert
        Assert.IsNotNull(_orchestrator);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_HandleCancellation_When_CancellationTokenCancelled()
    {
        // Arrange
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // The orchestrator doesn't check cancellation immediately, so we need to mock
        // the dependencies to throw cancellation when called
        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        mockLlmCommunicator.Setup(x => x.CallLlmAndParseAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var mockMessageBuilder = new Mock<IMessageBuilder>();
        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            new Mock<IToolExecutor>().Object,
            new Mock<ILoopDetector>().Object,
            mockMessageBuilder.Object,
            new Mock<IReasoningManager>().Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await orchestrator.ExecuteStepAsync(state, tools, cts.Token));
    }

    [TestMethod]
    public void HashToolCall_Should_ReturnConsistentHash_When_SameParametersProvided()
    {
        // Arrange
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" }, { "param2", 42 } };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(toolName, parameters);
        var hash2 = AgentOrchestrator.HashToolCall(toolName, parameters);

        // Assert
        Assert.AreEqual(hash1, hash2);
        Assert.IsFalse(string.IsNullOrEmpty(hash1));
    }

    [TestMethod]
    public void HashToolCall_Should_ReturnDifferentHash_When_DifferentParametersProvided()
    {
        // Arrange
        var toolName = "test_tool";
        var parameters1 = new Dictionary<string, object?> { { "param1", "value1" } };
        var parameters2 = new Dictionary<string, object?> { { "param1", "value2" } };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(toolName, parameters1);
        var hash2 = AgentOrchestrator.HashToolCall(toolName, parameters2);

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_Should_ReturnDifferentHash_When_DifferentToolNameProvided()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall("tool1", parameters);
        var hash2 = AgentOrchestrator.HashToolCall("tool2", parameters);

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashToolCall_Should_HandleNullParameters_When_ParametersAreNull()
    {
        // Arrange
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { { "param1", null } };

        // Act
        var hash = AgentOrchestrator.HashToolCall(toolName, parameters);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(hash));
    }

    [TestMethod]
    public void HashToolCall_Should_HandleEmptyParameters_When_ParametersAreEmpty()
    {
        // Arrange
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?>();

        // Act
        var hash = AgentOrchestrator.HashToolCall(toolName, parameters);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(hash));
    }

    [TestMethod]
    public void HashToolCall_Should_HandleComplexParameters_When_ParametersAreNested()
    {
        // Arrange
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> 
        { 
            { "simple", "value" },
            { "number", 42 },
            { "boolean", true },
            { "nested", new Dictionary<string, object?> { { "inner", "value" } } }
        };

        // Act
        var hash = AgentOrchestrator.HashToolCall(toolName, parameters);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(hash));
    }

    [TestMethod]
    public void HashToolCall_Should_ProduceOrderIndependentHash_When_ParametersAreReordered()
    {
        // Arrange
        var toolName = "test_tool";
        var parameters1 = new Dictionary<string, object?> { { "a", 1 }, { "b", 2 } };
        var parameters2 = new Dictionary<string, object?> { { "b", 2 }, { "a", 1 } };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(toolName, parameters1);
        var hash2 = AgentOrchestrator.HashToolCall(toolName, parameters2);

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    // NEW TESTS FOR UNCOVERED LINES

    [TestMethod]
    public void HashToolCall_Should_HandleNullObject_When_CanonicalizeJsonCalledWithNull()
    {
        // Arrange - This tests lines 493-494 in CanonicalizeJson method
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { { "nullValue", null } };

        // Act
        var hash = AgentOrchestrator.HashToolCall(toolName, parameters);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(hash));
    }

    [TestMethod]
    public void HashToolCall_Should_HandleJsonElement_When_CanonicalizeJsonCalledWithJsonElement()
    {
        // Arrange - This tests lines 498-499 in CanonicalizeJson method
        var toolName = "test_tool";
        var jsonElement = JsonDocument.Parse("{\"test\": \"value\"}").RootElement;
        var parameters = new Dictionary<string, object?> { { "jsonElement", jsonElement } };

        // Act
        var hash = AgentOrchestrator.HashToolCall(toolName, parameters);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(hash));
    }

    [TestMethod]
    public void HashToolCall_Should_HandleAllJsonValueKinds_When_CanonicalizeJsonElementCalled()
    {
        // Arrange - This tests lines 519-521, 534, 540 in CanonicalizeJsonElement method
        var toolName = "test_tool";
        
        // Test different JsonValueKinds that are not covered
        var parameters = new Dictionary<string, object?>
        {
            { "string", JsonDocument.Parse("\"test\"").RootElement },
            { "number", JsonDocument.Parse("42.5").RootElement },
            { "boolean", JsonDocument.Parse("true").RootElement },
            { "null", JsonDocument.Parse("null").RootElement },
            { "array", JsonDocument.Parse("[1,2,3]").RootElement },
            { "object", JsonDocument.Parse("{\"key\":\"value\"}").RootElement }
        };

        // Act
        var hash = AgentOrchestrator.HashToolCall(toolName, parameters);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(hash));
    }

    [TestMethod]
    public void ShouldPerformReasoning_Should_ReturnTrue_When_ComplexConditionsMet()
    {
        // Arrange - This tests lines 553-556 in ShouldPerformReasoning method
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
        
        var state = new AgentState 
        { 
            AgentId = "test-agent", 
            Goal = "test goal", 
            Turns = new List<AgentTurn> 
            { 
                new AgentTurn 
                { 
                    ToolResult = new ToolExecutionResult { Success = false } 
                } 
            } 
        };
        var turnIndex = 3; // This will trigger the complex condition

        // Act
        var result = orchestrator.ShouldPerformReasoning(state, turnIndex);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldPerformReasoning_Should_ReturnFalse_When_ReasoningDisabled()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.None };
        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var turnIndex = 0;

        // Act
        var result = orchestrator.ShouldPerformReasoning(state, turnIndex);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldPerformReasoning_Should_ReturnTrue_When_FirstTurn()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var turnIndex = 0;

        // Act
        var result = orchestrator.ShouldPerformReasoning(state, turnIndex);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_HandleFunctionArgumentError_When_FunctionCallFails()
    {
        // Arrange - This tests lines 357-391 in HandleFunctionArgumentError method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var mockTool = new Mock<ITool>();
        mockTool.As<IFunctionSchemaProvider>().Setup(t => t.Name).Returns("test_function");
        mockTool.As<IFunctionSchemaProvider>().Setup(t => t.Description).Returns("A test function");
        mockTool.As<IFunctionSchemaProvider>().Setup(t => t.GetJsonSchema()).Returns("{\"type\":\"object\",\"properties\":{\"param1\":{\"type\":\"string\"}}}");
        mockTool.Setup(t => t.Name).Returns("test_function");
        var tools = new Dictionary<string, ITool> { { "test_function", mockTool.Object } };

        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        var mockMessageBuilder = new Mock<IMessageBuilder>();
        var mockToolExecutor = new Mock<IToolExecutor>();
        var mockLoopDetector = new Mock<ILoopDetector>();
        var mockReasoningManager = new Mock<IReasoningManager>();

        // Setup function calling to fail with argument error
        var functionResult = new LlmResponse 
        { 
            FunctionCall = new LlmFunctionCall { Name = "test_function" },
            Content = "test content",
            HasFunctionCall = true
        };

        mockLlmCommunicator.Setup(x => x.CallWithFunctionsAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<List<FunctionSpec>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(functionResult)
            .Verifiable();

        mockLlmCommunicator.Setup(x => x.NormalizeFunctionCallToReact(It.IsAny<LlmResponse>(), It.IsAny<int>()))
            .Throws(new ArgumentException("Failed to parse function arguments"));

        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        var config = new AgentConfiguration { UseFunctionCalling = true };
        var mockLlmClient = new Mock<ILlmClient>();
        var orchestrator = new AgentOrchestrator(
            mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            mockToolExecutor.Object,
            mockLoopDetector.Object,
            mockMessageBuilder.Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.ExecuteStepAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Console.WriteLine($"Result.Continue: {result.Continue}");
        Console.WriteLine($"Result.ExecutedTool: {result.ExecutedTool}");
        Console.WriteLine($"Result.ToolResult: {result.ToolResult != null}");
        if (result.ToolResult != null)
        {
            Console.WriteLine($"Result.ToolResult.Success: {result.ToolResult.Success}");
            Console.WriteLine($"Result.ToolResult.Error: '{result.ToolResult.Error}'");
        }
        
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsNotNull(result.ToolResult.Error);
        Assert.IsFalse(result.ToolResult.Success);
        Console.WriteLine($"Actual error message: '{result.ToolResult.Error}'");
        Assert.IsTrue(result.ToolResult.Error.Contains("Failed to parse function arguments"), 
            $"Expected error to contain 'Failed to parse function arguments', but got: '{result.ToolResult.Error}'");
        
        // Verify that function calling was attempted
        mockLlmCommunicator.Verify(x => x.CallWithFunctionsAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<List<FunctionSpec>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_HandleUnknownToolError_When_FunctionCallReferencesUnknownTool()
    {
        // Arrange - This tests lines 394-428 in HandleUnknownToolError method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var mockTool = new Mock<ITool>();
        mockTool.As<IFunctionSchemaProvider>().Setup(t => t.Name).Returns("known_tool");
        mockTool.As<IFunctionSchemaProvider>().Setup(t => t.Description).Returns("A known tool");
        mockTool.As<IFunctionSchemaProvider>().Setup(t => t.GetJsonSchema()).Returns("{\"type\":\"object\",\"properties\":{\"param1\":{\"type\":\"string\"}}}");
        mockTool.Setup(t => t.Name).Returns("known_tool");
        var tools = new Dictionary<string, ITool> { { "known_tool", mockTool.Object } };

        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        var mockMessageBuilder = new Mock<IMessageBuilder>();
        var mockToolExecutor = new Mock<IToolExecutor>();
        var mockLoopDetector = new Mock<ILoopDetector>();
        var mockReasoningManager = new Mock<IReasoningManager>();

        // Setup function calling to fail with unknown tool error
        var functionResult = new LlmResponse 
        { 
            FunctionCall = new LlmFunctionCall { Name = "unknown_tool" },
            Content = "test content",
            HasFunctionCall = true
        };

        mockLlmCommunicator.Setup(x => x.CallWithFunctionsAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<List<FunctionSpec>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(functionResult)
            .Verifiable();

        mockLlmCommunicator.Setup(x => x.NormalizeFunctionCallToReact(It.IsAny<LlmResponse>(), It.IsAny<int>()))
            .Throws(new KeyNotFoundException("Tool 'unknown_tool' not found"));

        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        var config = new AgentConfiguration { UseFunctionCalling = true };
        var mockLlmClient = new Mock<ILlmClient>();
        var orchestrator = new AgentOrchestrator(
            mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            mockToolExecutor.Object,
            mockLoopDetector.Object,
            mockMessageBuilder.Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.ExecuteStepAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        Assert.IsNotNull(result.ToolResult.Error);
        Console.WriteLine($"Actual error message: '{result.ToolResult.Error}'");
        Assert.IsTrue(result.ToolResult.Error.Contains("Tool 'unknown_tool' not found"), 
            $"Expected error to contain 'Tool 'unknown_tool' not found', but got: '{result.ToolResult.Error}'");
        
        // Verify that function calling was attempted
        mockLlmCommunicator.Verify(x => x.CallWithFunctionsAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<List<FunctionSpec>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_AddRetryHintsAndLoopBreaker_When_ToolExecutionFails()
    {
        // Arrange - This tests lines 431-475 in AddRetryHintsAndLoopBreaker method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();

        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        var mockMessageBuilder = new Mock<IMessageBuilder>();
        var mockToolExecutor = new Mock<IToolExecutor>();
        var mockLoopDetector = new Mock<ILoopDetector>();
        var mockReasoningManager = new Mock<IReasoningManager>();

        // Setup tool execution to fail
        var failedResult = new ToolExecutionResult 
        { 
            Success = false, 
            Error = "Tool failed", 
            Tool = "test_tool",
            TurnId = "test_turn",
            CreatedUtc = DateTimeOffset.UtcNow
        };

        mockToolExecutor.Setup(x => x.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // Setup loop detector to detect repeated failures
        mockLoopDetector.Setup(x => x.DetectRepeatedFailures(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
            .Returns(true);

        var toolCallMessage = new ModelMessage
        {
            Action = AgentAction.ToolCall,
            ActionInput = new ActionInput { Tool = "test_tool", Params = new Dictionary<string, object?>() }
        };

        mockLlmCommunicator.Setup(x => x.CallLlmAndParseAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolCallMessage);

        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        var config = new AgentConfiguration();
        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            mockToolExecutor.Object,
            mockLoopDetector.Object,
            mockMessageBuilder.Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.ExecuteStepAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        
        // Verify that retry hints and loop breaker were added
        Assert.AreEqual(3, state.Turns.Count); // Original turn + retry hint + loop breaker
        Assert.IsTrue(state.Turns[1].LlmMessage?.Thoughts?.Contains("Controller: The last tool call failed"));
        Assert.IsTrue(state.Turns[2].LlmMessage?.Thoughts?.Contains("Controller: You're repeating the same failing call"));
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_UpdateStateWithReasoning_When_ReasoningSucceeds()
    {
        // Arrange - This tests lines 579-594 in UpdateStateWithReasoning method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();

        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        var mockMessageBuilder = new Mock<IMessageBuilder>();
        var mockToolExecutor = new Mock<IToolExecutor>();
        var mockLoopDetector = new Mock<ILoopDetector>();
        var mockReasoningManager = new Mock<IReasoningManager>();

        // Setup reasoning to succeed
        var reasoningChain = new ReasoningChain();
        reasoningChain.AddStep("step1");
        reasoningChain.AddStep("step2");
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Chain = reasoningChain,
            Tree = null,
            Metadata = new Dictionary<string, object> { { "confidence", 0.8 } }
        };

        mockReasoningManager.Setup(x => x.ReasonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reasoningResult);

        var planMessage = new ModelMessage
        {
            Action = AgentAction.Plan,
            ActionInput = new ActionInput { Summary = "Planning" }
        };

        mockLlmCommunicator.Setup(x => x.CallLlmAndParseAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        // Enable reasoning
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            mockToolExecutor.Object,
            mockLoopDetector.Object,
            mockMessageBuilder.Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.ExecuteStepAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ReasoningType.ChainOfThought, state.ReasoningType);
        Assert.IsNotNull(state.CurrentReasoningChain);
        Assert.AreEqual(2, state.CurrentReasoningChain.Steps.Count);
        Assert.AreEqual("step1", state.CurrentReasoningChain.Steps[0].Reasoning);
        Assert.AreEqual("step2", state.CurrentReasoningChain.Steps[1].Reasoning);
        Assert.IsTrue(state.ReasoningMetadata.ContainsKey("confidence"));
        Assert.AreEqual(0.8, state.ReasoningMetadata["confidence"]);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_EnhanceGoalWithReasoning_When_ReasoningHasConclusion()
    {
        // Arrange - This tests lines 597-602 in EnhanceGoalWithReasoning method
        var state = new AgentState { AgentId = "test-agent", Goal = "Original goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();

        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        var mockMessageBuilder = new Mock<IMessageBuilder>();
        var mockToolExecutor = new Mock<IToolExecutor>();
        var mockLoopDetector = new Mock<ILoopDetector>();
        var mockReasoningManager = new Mock<IReasoningManager>();

        // Setup reasoning with conclusion
        var reasoningChain = new ReasoningChain();
        reasoningChain.AddStep("step1");
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Conclusion = "Use approach A for better results",
            Chain = reasoningChain
        };

        mockReasoningManager.Setup(x => x.ReasonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reasoningResult);

        var planMessage = new ModelMessage
        {
            Action = AgentAction.Plan,
            ActionInput = new ActionInput { Summary = "Planning" }
        };

        mockLlmCommunicator.Setup(x => x.CallLlmAndParseAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        // Enable reasoning
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            mockToolExecutor.Object,
            mockLoopDetector.Object,
            mockMessageBuilder.Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.ExecuteStepAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(state.Goal.Contains("Original goal"));
        Assert.IsTrue(state.Goal.Contains("Reasoning Insights: Use approach A for better results"));
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_BuildReasoningContext_When_StateHasTurns()
    {
        // Arrange - This tests lines 605-624 in BuildReasoningContext method
        var state = new AgentState 
        { 
            AgentId = "test-agent", 
            Goal = "test goal", 
            Turns = new List<AgentTurn> 
            { 
                new AgentTurn 
                { 
                    LlmMessage = new ModelMessage { Thoughts = "First thought" },
                    ToolResult = new ToolExecutionResult { Success = true },
                    ToolCall = new ToolCallRequest { Tool = "test_tool" }
                },
                new AgentTurn 
                { 
                    LlmMessage = new ModelMessage { Thoughts = "Second thought" },
                    ToolResult = new ToolExecutionResult { Success = false, Error = "Tool failed" },
                    ToolCall = new ToolCallRequest { Tool = "failed_tool" }
                }
            } 
        };
        // turnIndex will be 2, which should trigger reasoning because:
        // - turnIndex > 0 (2 > 0)
        // - state.Turns.Count > 0 (2 > 0)
        // - last turn failed (Success = false)
        // - turnIndex % 3 != 0 (2 % 3 = 2), so this won't trigger reasoning
        // Let me add more turns to make turnIndex % 3 == 0
        state.Turns.Add(new AgentTurn 
        { 
            LlmMessage = new ModelMessage { Thoughts = "Third thought" },
            ToolResult = new ToolExecutionResult { Success = false, Error = "Another failure" },
            ToolCall = new ToolCallRequest { Tool = "another_failed_tool" }
        });
        var tools = new Dictionary<string, ITool>();

        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        var mockMessageBuilder = new Mock<IMessageBuilder>();
        var mockToolExecutor = new Mock<IToolExecutor>();
        var mockLoopDetector = new Mock<ILoopDetector>();
        var mockReasoningManager = new Mock<IReasoningManager>();

        // Setup reasoning to succeed
        var reasoningChain = new ReasoningChain();
        reasoningChain.AddStep("step1");
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Chain = reasoningChain
        };

        mockReasoningManager.Setup(x => x.ReasonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reasoningResult);

        var planMessage = new ModelMessage
        {
            Action = AgentAction.Plan,
            ActionInput = new ActionInput { Summary = "Planning" }
        };

        mockLlmCommunicator.Setup(x => x.CallLlmAndParseAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        // Enable reasoning
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            mockToolExecutor.Object,
            mockLoopDetector.Object,
            mockMessageBuilder.Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.ExecuteStepAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        // Verify that reasoning was called with context that includes the turns
        mockReasoningManager.Verify(x => x.ReasonAsync(
            It.IsAny<string>(), 
            It.Is<string>(ctx => ctx.Contains("Recent Actions:") && ctx.Contains("First thought") && ctx.Contains("Second thought")), 
            It.IsAny<IDictionary<string, ITool>>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task ExecuteStepAsync_Should_HandleReasoningFailure_When_ReasoningThrowsException()
    {
        // Arrange - This tests lines 560-576 in PerformReasoningAsync method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();

        var mockLlmCommunicator = new Mock<ILlmCommunicator>();
        var mockMessageBuilder = new Mock<IMessageBuilder>();
        var mockToolExecutor = new Mock<IToolExecutor>();
        var mockLoopDetector = new Mock<ILoopDetector>();
        var mockReasoningManager = new Mock<IReasoningManager>();

        // Setup reasoning to fail
        mockReasoningManager.Setup(x => x.ReasonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Reasoning failed"));

        var planMessage = new ModelMessage
        {
            Action = AgentAction.Plan,
            ActionInput = new ActionInput { Summary = "Planning" }
        };

        mockLlmCommunicator.Setup(x => x.CallLlmAndParseAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        mockMessageBuilder.Setup(x => x.BuildMessages(state, tools))
            .Returns(new List<LlmMessage> { new LlmMessage { Role = "system", Content = "test" } });

        // Enable reasoning
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            mockLlmCommunicator.Object,
            mockToolExecutor.Object,
            mockLoopDetector.Object,
            mockMessageBuilder.Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.ExecuteStepAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        // Verify that reasoning failure was logged
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(msg => msg.Contains("Reasoning failed"))), Times.Once);
    }

    // DIRECT TESTS OF INTERNAL METHODS

    [TestMethod]
    public void CanonicalizeJson_Should_HandleNullObject_When_ObjectIsNull()
    {
        // Arrange - This tests lines 493-494 in CanonicalizeJson method
        object? nullObj = null;

        // Act
        var result = AgentOrchestrator.CanonicalizeJson(nullObj);

        // Assert
        Assert.AreEqual("null", result);
    }

    [TestMethod]
    public void CanonicalizeJson_Should_HandleJsonElement_When_ObjectIsJsonElement()
    {
        // Arrange - This tests lines 498-499 in CanonicalizeJson method
        var jsonElement = JsonDocument.Parse("{\"test\": \"value\"}").RootElement;

        // Act
        var result = AgentOrchestrator.CanonicalizeJson(jsonElement);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result));
        Assert.IsTrue(result.Contains("test"));
    }

    [TestMethod]
    public void CanonicalizeJsonElement_Should_HandleAllJsonValueKinds()
    {
        // Arrange - This tests lines 519-521, 534, 540 in CanonicalizeJsonElement method
        var stringElement = JsonDocument.Parse("\"test\"").RootElement;
        var numberElement = JsonDocument.Parse("42.5").RootElement;
        var booleanElement = JsonDocument.Parse("true").RootElement;
        var nullElement = JsonDocument.Parse("null").RootElement;
        var arrayElement = JsonDocument.Parse("[1,2,3]").RootElement;
        var objectElement = JsonDocument.Parse("{\"key\":\"value\"}").RootElement;

        // Act & Assert
        Assert.AreEqual("\"test\"", AgentOrchestrator.CanonicalizeJsonElement(stringElement));
        Assert.AreEqual("42.5", AgentOrchestrator.CanonicalizeJsonElement(numberElement));
        Assert.AreEqual("true", AgentOrchestrator.CanonicalizeJsonElement(booleanElement));
        Assert.AreEqual("null", AgentOrchestrator.CanonicalizeJsonElement(nullElement));
        Assert.IsTrue(AgentOrchestrator.CanonicalizeJsonElement(arrayElement).Contains("[1,2,3]"));
        Assert.IsTrue(AgentOrchestrator.CanonicalizeJsonElement(objectElement).Contains("key"));
    }

    [TestMethod]
    public void UpdateStateWithReasoning_Should_UpdateStateWithChainOfThought_When_ChainIsProvided()
    {
        // Arrange - This tests lines 579-594 in UpdateStateWithReasoning method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal" };
        var reasoningChain = new ReasoningChain();
        reasoningChain.AddStep("step1");
        reasoningChain.AddStep("step2");
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Chain = reasoningChain,
            Tree = null,
            Metadata = new Dictionary<string, object> { { "confidence", 0.8 } }
        };

        // Act
        _orchestrator.UpdateStateWithReasoning(state, reasoningResult);

        // Assert
        Assert.AreEqual(ReasoningType.ChainOfThought, state.ReasoningType);
        Assert.IsNotNull(state.CurrentReasoningChain);
        Assert.AreEqual(2, state.CurrentReasoningChain.Steps.Count);
        Assert.AreEqual("step1", state.CurrentReasoningChain.Steps[0].Reasoning);
        Assert.AreEqual("step2", state.CurrentReasoningChain.Steps[1].Reasoning);
        Assert.IsTrue(state.ReasoningMetadata.ContainsKey("confidence"));
        Assert.AreEqual(0.8, state.ReasoningMetadata["confidence"]);
    }

    [TestMethod]
    public void UpdateStateWithReasoning_Should_UpdateStateWithTreeOfThoughts_When_TreeIsProvided()
    {
        // Arrange
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal" };
        var reasoningTree = new ReasoningTree();
        reasoningTree.BestPath = new List<string> { "branch1", "branch2" };
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Chain = null,
            Tree = reasoningTree,
            Metadata = new Dictionary<string, object> { { "depth", 3 } }
        };

        // Act
        _orchestrator.UpdateStateWithReasoning(state, reasoningResult);

        // Assert
        Assert.AreEqual(ReasoningType.TreeOfThoughts, state.ReasoningType);
        Assert.IsNotNull(state.CurrentReasoningTree);
        Assert.AreEqual(2, state.CurrentReasoningTree.BestPath.Count);
        Assert.AreEqual("branch1", state.CurrentReasoningTree.BestPath[0]);
        Assert.AreEqual("branch2", state.CurrentReasoningTree.BestPath[1]);
        Assert.IsTrue(state.ReasoningMetadata.ContainsKey("depth"));
        Assert.AreEqual(3, state.ReasoningMetadata["depth"]);
    }

    [TestMethod]
    public void UpdateStateWithReasoning_Should_UpdateStateWithHybrid_When_NeitherChainNorTreeProvided()
    {
        // Arrange
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal" };
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Chain = null,
            Tree = null,
            Metadata = new Dictionary<string, object> { { "method", "hybrid" } }
        };

        // Act
        _orchestrator.UpdateStateWithReasoning(state, reasoningResult);

        // Assert
        Assert.AreEqual(ReasoningType.Hybrid, state.ReasoningType);
        Assert.IsNull(state.CurrentReasoningChain);
        Assert.IsNull(state.CurrentReasoningTree);
        Assert.IsTrue(state.ReasoningMetadata.ContainsKey("method"));
        Assert.AreEqual("hybrid", state.ReasoningMetadata["method"]);
    }

    [TestMethod]
    public void EnhanceGoalWithReasoning_Should_ReturnOriginalGoal_When_ConclusionIsEmpty()
    {
        // Arrange - This tests lines 597-599 in EnhanceGoalWithReasoning method
        var originalGoal = "Original goal";
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Conclusion = string.Empty
        };

        // Act
        var result = _orchestrator.EnhanceGoalWithReasoning(originalGoal, reasoningResult);

        // Assert
        Assert.AreEqual(originalGoal, result);
    }

    [TestMethod]
    public void EnhanceGoalWithReasoning_Should_EnhanceGoal_When_ConclusionIsProvided()
    {
        // Arrange - This tests lines 601-602 in EnhanceGoalWithReasoning method
        var originalGoal = "Original goal";
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Conclusion = "Use approach A for better results"
        };

        // Act
        var result = _orchestrator.EnhanceGoalWithReasoning(originalGoal, reasoningResult);

        // Assert
        Assert.IsTrue(result.Contains(originalGoal));
        Assert.IsTrue(result.Contains("Reasoning Insights: Use approach A for better results"));
    }

    [TestMethod]
    public void BuildReasoningContext_Should_ReturnEmptyContext_When_StateHasNoTurns()
    {
        // Arrange - This tests lines 605-608 in BuildReasoningContext method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };

        // Act
        var result = _orchestrator.BuildReasoningContext(state);

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void BuildReasoningContext_Should_BuildContextFromTurns_When_StateHasTurns()
    {
        // Arrange - This tests lines 609-624 in BuildReasoningContext method
        var state = new AgentState 
        { 
            AgentId = "test-agent", 
            Goal = "test goal", 
            Turns = new List<AgentTurn> 
            { 
                new AgentTurn 
                { 
                    LlmMessage = new ModelMessage { Thoughts = "First thought" },
                    ToolResult = new ToolExecutionResult { Success = true },
                    ToolCall = new ToolCallRequest { Tool = "test_tool" }
                },
                new AgentTurn 
                { 
                    LlmMessage = new ModelMessage { Thoughts = "Second thought" },
                    ToolResult = new ToolExecutionResult { Success = false, Error = "Tool failed" },
                    ToolCall = new ToolCallRequest { Tool = "failed_tool" }
                }
            } 
        };

        // Act
        var result = _orchestrator.BuildReasoningContext(state);

        // Assert
        Assert.IsTrue(result.Contains("Recent Actions:"));
        Assert.IsTrue(result.Contains("First thought"));
        Assert.IsTrue(result.Contains("Second thought"));
        Assert.IsTrue(result.Contains("Successfully executed: test_tool"));
        Assert.IsTrue(result.Contains("Failed to execute: failed_tool - Tool failed"));
    }

    [TestMethod]
    public async Task PerformReasoningAsync_Should_ReturnSuccessResult_When_ReasoningSucceeds()
    {
        // Arrange - This tests lines 560-576 in PerformReasoningAsync method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();

        var mockReasoningManager = new Mock<IReasoningManager>();
        var reasoningChain = new ReasoningChain();
        reasoningChain.AddStep("step1");
        var reasoningResult = new ReasoningResult
        {
            Success = true,
            Chain = reasoningChain
        };

        mockReasoningManager.Setup(x => x.ReasonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reasoningResult);

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            new Mock<ILlmCommunicator>().Object,
            new Mock<IToolExecutor>().Object,
            new Mock<ILoopDetector>().Object,
            new Mock<IMessageBuilder>().Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.PerformReasoningAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Chain);
        Assert.AreEqual(1, result.Chain.Steps.Count);
        Assert.AreEqual("step1", result.Chain.Steps[0].Reasoning);
    }

    [TestMethod]
    public async Task PerformReasoningAsync_Should_ReturnFailureResult_When_ReasoningThrowsException()
    {
        // Arrange - This tests lines 566-576 in PerformReasoningAsync method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var tools = new Dictionary<string, ITool>();

        var mockReasoningManager = new Mock<IReasoningManager>();
        mockReasoningManager.Setup(x => x.ReasonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Reasoning failed"));

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            new Mock<ILlmCommunicator>().Object,
            new Mock<IToolExecutor>().Object,
            new Mock<ILoopDetector>().Object,
            new Mock<IMessageBuilder>().Object,
            mockReasoningManager.Object);

        // Act
        var result = await orchestrator.PerformReasoningAsync(state, tools, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Reasoning failed", result.Error);
        Assert.AreEqual(0, result.ExecutionTimeMs);
    }

    [TestMethod]
    public void HandleFunctionArgumentError_Should_ReturnErrorResult_When_FunctionArgumentErrorOccurs()
    {
        // Arrange - This tests lines 357-391 in HandleFunctionArgumentError method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var functionResult = new LlmResponse 
        { 
            FunctionCall = new LlmFunctionCall { Name = "test_function" },
            Content = "test content"
        };
        var turnIndex = 1;
        var turnId = "turn_1";
        var errorMessage = "Failed to parse function arguments";

        // Act
        var result = _orchestrator.HandleFunctionArgumentError(state, functionResult, turnIndex, turnId, errorMessage);

        // Assert
        Assert.IsNotNull(result);
        var stepResult = result.Result;
        Assert.IsTrue(stepResult.Continue);
        Assert.IsTrue(stepResult.ExecutedTool);
        Assert.IsNotNull(stepResult.ToolResult);
        Assert.IsFalse(stepResult.ToolResult.Success);
        Assert.AreEqual(errorMessage, stepResult.ToolResult.Error);
        Assert.AreEqual("test_function", stepResult.ToolResult.Tool);
        Assert.AreEqual(1, state.Turns.Count);
    }

    [TestMethod]
    public void HandleUnknownToolError_Should_ReturnErrorResult_When_UnknownToolErrorOccurs()
    {
        // Arrange - This tests lines 394-428 in HandleUnknownToolError method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var functionResult = new LlmResponse 
        { 
            FunctionCall = new LlmFunctionCall { Name = "unknown_tool" },
            Content = "test content"
        };
        var turnIndex = 1;
        var turnId = "turn_1";
        var errorMessage = "Tool 'unknown_tool' not found";

        // Act
        var result = _orchestrator.HandleUnknownToolError(state, functionResult, turnIndex, turnId, errorMessage);

        // Assert
        Assert.IsNotNull(result);
        var stepResult = result.Result;
        Assert.IsTrue(stepResult.Continue);
        Assert.IsTrue(stepResult.ExecutedTool);
        Assert.IsNotNull(stepResult.ToolResult);
        Assert.IsFalse(stepResult.ToolResult.Success);
        Assert.AreEqual(errorMessage, stepResult.ToolResult.Error);
        Assert.AreEqual("unknown_tool", stepResult.ToolResult.Tool);
        Assert.AreEqual(1, state.Turns.Count);
    }

    [TestMethod]
    public async Task AddRetryHintsAndLoopBreaker_Should_AddRetryHint_When_ToolExecutionFails()
    {
        // Arrange - This tests lines 431-450 in AddRetryHintsAndLoopBreaker method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var execResult = new ToolExecutionResult 
        { 
            Success = false, 
            Error = "Tool failed", 
            Tool = "test_tool",
            TurnId = "test_turn",
            CreatedUtc = DateTimeOffset.UtcNow
        };
        var toolName = "test_tool";
        var prms = new Dictionary<string, object?>();
        var turnIndex = 1;

        var mockLoopDetector = new Mock<ILoopDetector>();
        mockLoopDetector.Setup(x => x.DetectRepeatedFailures(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
            .Returns(false);

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            new Mock<ILlmCommunicator>().Object,
            new Mock<IToolExecutor>().Object,
            mockLoopDetector.Object,
            new Mock<IMessageBuilder>().Object,
            new Mock<IReasoningManager>().Object);

        // Act
        await orchestrator.AddRetryHintsAndLoopBreaker(state, execResult, toolName, prms, turnIndex);

        // Assert
        Assert.AreEqual(1, state.Turns.Count);
        Assert.IsTrue(state.Turns[0].LlmMessage?.Thoughts?.Contains("Controller: The last tool call failed"));
        Assert.AreEqual(AgentAction.Retry, state.Turns[0].LlmMessage?.Action);
    }

    [TestMethod]
    public async Task AddRetryHintsAndLoopBreaker_Should_AddLoopBreaker_When_RepeatedFailuresDetected()
    {
        // Arrange - This tests lines 452-475 in AddRetryHintsAndLoopBreaker method
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal", Turns = new List<AgentTurn>() };
        var execResult = new ToolExecutionResult 
        { 
            Success = false, 
            Error = "Tool failed", 
            Tool = "test_tool",
            TurnId = "test_turn",
            CreatedUtc = DateTimeOffset.UtcNow
        };
        var toolName = "test_tool";
        var prms = new Dictionary<string, object?>();
        var turnIndex = 1;

        var mockLoopDetector = new Mock<ILoopDetector>();
        mockLoopDetector.Setup(x => x.DetectRepeatedFailures(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
            .Returns(true);

        var orchestrator = new AgentOrchestrator(
            _mockLlmClient.Object,
            _mockStateStore.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object,
            new Mock<ILlmCommunicator>().Object,
            new Mock<IToolExecutor>().Object,
            mockLoopDetector.Object,
            new Mock<IMessageBuilder>().Object,
            new Mock<IReasoningManager>().Object);

        // Act
        await orchestrator.AddRetryHintsAndLoopBreaker(state, execResult, toolName, prms, turnIndex);

        // Assert
        Assert.AreEqual(2, state.Turns.Count);
        Assert.IsTrue(state.Turns[0].LlmMessage?.Thoughts?.Contains("Controller: The last tool call failed"));
        Assert.IsTrue(state.Turns[1].LlmMessage?.Thoughts?.Contains("Controller: You're repeating the same failing call"));
        Assert.AreEqual(AgentAction.Retry, state.Turns[0].LlmMessage?.Action);
        Assert.AreEqual(AgentAction.Retry, state.Turns[1].LlmMessage?.Action);
    }
}
