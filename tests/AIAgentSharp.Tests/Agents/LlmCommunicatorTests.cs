using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class LlmCommunicatorTests
{
    private Mock<ILlmClient> _mockLlm = null!;
    private AgentConfiguration _config = null!;
    private Mock<ILogger> _mockLogger = null!;
    private Mock<IEventManager> _mockEventManager = null!;
    private Mock<IStatusManager> _mockStatusManager = null!;
    private LlmCommunicator _communicator = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlm = new Mock<ILlmClient>();
        _config = new AgentConfiguration { LlmTimeout = TimeSpan.FromSeconds(30) };
        _mockLogger = new Mock<ILogger>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();

        _communicator = new LlmCommunicator(
            _mockLlm.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object);
    }

    [TestMethod]
    public async Task CallWithFunctionsAsync_WithValidInputs_ShouldReturnFunctionResult()
    {
        // Arrange
        var messages = new List<LlmMessage>();
        var functionSpecs = new List<OpenAiFunctionSpec>();
        var agentId = "test-agent";
        var turnIndex = 1;
        var expectedResult = new FunctionCallResult { HasFunctionCall = false };

        // Create a mock that implements IFunctionCallingLlmClient
        var mockFunctionClient = new Mock<IFunctionCallingLlmClient>();
        mockFunctionClient.Setup(x => x.CompleteWithFunctionsAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<IEnumerable<OpenAiFunctionSpec>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var communicator = new LlmCommunicator(
            mockFunctionClient.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object);

        // Act
        var result = await communicator.CallWithFunctionsAsync(messages, functionSpecs, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        _mockEventManager.Verify(x => x.RaiseLlmCallStarted(agentId, turnIndex), Times.Once);
        _mockEventManager.Verify(x => x.RaiseLlmCallCompleted(agentId, turnIndex, null, null), Times.Once);
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_WithValidInputs_ShouldReturnModelMessage()
    {
        // Arrange
        var messages = new List<LlmMessage>();
        var agentId = "test-agent";
        var turnIndex = 1;
        var turnId = "turn_1_123";
        var state = new AgentState { AgentId = agentId, Turns = new List<AgentTurn>() };
        var jsonResponse = "{\"thoughts\":\"test\",\"action\":\"finish\",\"action_input\":{\"final\":\"result\"}}";

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _communicator.CallLlmAndParseAsync(messages, agentId, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Thoughts);
        Assert.AreEqual(AgentAction.Finish, result.Action);
    }

    [TestMethod]
    public async Task ParseJsonResponse_WithValidJson_ShouldReturnModelMessage()
    {
        // Arrange
        var llmRaw = "{\"thoughts\":\"test\",\"action\":\"finish\",\"action_input\":{\"final\":\"result\"}}";
        var turnIndex = 1;
        var turnId = "turn_1_123";
        var state = new AgentState { AgentId = "test-agent", Turns = new List<AgentTurn>() };

        // Act
        var result = await _communicator.ParseJsonResponse(llmRaw, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Thoughts);
        Assert.AreEqual(AgentAction.Finish, result.Action);
        _mockEventManager.Verify(x => x.RaiseLlmCallCompleted(state.AgentId, turnIndex, result, null), Times.Once);
    }

    [TestMethod]
    public async Task ParseJsonResponse_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var llmRaw = "invalid json";
        var turnIndex = 1;
        var turnId = "turn_1_123";
        var state = new AgentState { AgentId = "test-agent", Turns = new List<AgentTurn>() };

        // Act
        var result = await _communicator.ParseJsonResponse(llmRaw, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNull(result);
        _mockEventManager.Verify(x => x.RaiseLlmCallCompleted(state.AgentId, turnIndex, null, It.IsAny<string>()), Times.Once);
        _mockStatusManager.Verify(x => x.EmitStatus(state.AgentId, "Invalid model output", "JSON parsing failed", "Will retry with corrected format", null), Times.Once);
    }

    [TestMethod]
    public void NormalizeFunctionCallToReact_WithValidInput_ShouldReturnModelMessage()
    {
        // Arrange
        var functionResult = new FunctionCallResult
        {
            FunctionName = "test_function",
            FunctionArgumentsJson = "{\"param1\":\"value1\"}",
            AssistantContent = "Calling test function"
        };
        var turnIndex = 1;

        // Act
        var result = _communicator.NormalizeFunctionCallToReact(functionResult, turnIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Calling test function", result.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("test_function", result.ActionInput.Tool);
        Assert.IsNotNull(result.ActionInput.Params);
        Assert.AreEqual("value1", result.ActionInput.Params["param1"]);
    }

    [TestMethod]
    public void NormalizeFunctionCallToReact_WithInvalidJson_ShouldThrowArgumentException()
    {
        // Arrange
        var functionResult = new FunctionCallResult
        {
            FunctionName = "test_function",
            FunctionArgumentsJson = "invalid json",
            AssistantContent = "Calling test function"
        };
        var turnIndex = 1;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            _communicator.NormalizeFunctionCallToReact(functionResult, turnIndex));
    }

        [TestMethod]
    public async Task CallLlmAndParseAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var messages = new List<LlmMessage>();
        var agentId = "test-agent";
        var turnIndex = 1;
        var turnId = "turn_1_123";
        var state = new AgentState { AgentId = agentId, Turns = new List<AgentTurn>() };
        var cts = new CancellationTokenSource();
        
        // Set up a slow LLM response to ensure cancellation can be tested
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => 
            {
                await Task.Delay(1000, cts.Token); // This will throw if cancelled
                return "test response";
            });

        // Act & Assert
        var task = _communicator.CallLlmAndParseAsync(messages, agentId, turnIndex, turnId, state, cts.Token);
        
        // Cancel after a short delay to ensure the operation has started
        await Task.Delay(100);
        cts.Cancel();
        
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => task);
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_WithLlmException_ShouldReturnNull()
    {
        // Arrange
        var messages = new List<LlmMessage>();
        var agentId = "test-agent";
        var turnIndex = 1;
        var turnId = "turn_1_123";
        var state = new AgentState { AgentId = agentId, Turns = new List<AgentTurn>() };

        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM error"));

        // Act
        var result = await _communicator.CallLlmAndParseAsync(messages, agentId, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(1, state.Turns.Count);
        Assert.IsNotNull(state.Turns[0].ToolResult);
        var toolResult = state.Turns[0].ToolResult;
        Assert.IsNotNull(toolResult);
        Assert.IsFalse(toolResult.Success);
        Assert.IsNotNull(toolResult.Error);
        Assert.IsTrue(toolResult.Error.Contains("LLM error"));
    }

    [TestMethod]
    public async Task ParseJsonResponse_WithStatusFields_ShouldEmitStatus()
    {
        // Arrange
        var llmRaw = "{\"thoughts\":\"test\",\"action\":\"finish\",\"action_input\":{\"final\":\"result\"},\"status_title\":\"Processing\",\"status_details\":\"Working on it\",\"next_step_hint\":\"Almost done\",\"progress_pct\":75}";
        var turnIndex = 1;
        var turnId = "turn_1_123";
        var state = new AgentState { AgentId = "test-agent", Turns = new List<AgentTurn>() };

        // Act
        var result = await _communicator.ParseJsonResponse(llmRaw, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        _mockStatusManager.Verify(x => x.EmitStatus(state.AgentId, "Processing", "Working on it", "Almost done", 75), Times.Once);
    }

    [TestMethod]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Assert
        Assert.IsNotNull(_communicator);
    }

    [TestMethod]
    public async Task CallWithFunctionsAsync_WithNonFunctionCallingLlm_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var messages = new List<LlmMessage>();
        var functionSpecs = new List<OpenAiFunctionSpec>();
        var agentId = "test-agent";
        var turnIndex = 1;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _communicator.CallWithFunctionsAsync(messages, functionSpecs, agentId, turnIndex, CancellationToken.None));
    }
}
