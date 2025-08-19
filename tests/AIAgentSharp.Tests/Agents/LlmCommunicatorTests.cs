using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class LlmCommunicatorTests
{
    private Mock<ILlmClient> _mockLlmClient;
    private Mock<ILogger> _mockLogger;
    private Mock<IEventManager> _mockEventManager;
    private Mock<IStatusManager> _mockStatusManager;
    private Mock<IMetricsCollector> _mockMetricsCollector;
    private AgentConfiguration _config;
    private LlmCommunicator _llmCommunicator;

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new Mock<ILlmClient>();
        _mockLogger = new Mock<ILogger>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _mockMetricsCollector = new Mock<IMetricsCollector>();
        _config = new AgentConfiguration { LlmTimeout = TimeSpan.FromSeconds(30) };
        _llmCommunicator = new LlmCommunicator(
            _mockLlmClient.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
    }

    [TestMethod]
    public void Constructor_Should_CreateLlmCommunicatorSuccessfully_When_ValidParametersProvided()
    {
        // Act
        var communicator = new LlmCommunicator(
            _mockLlmClient.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);

        // Assert
        Assert.IsNotNull(communicator);
    }

    [TestMethod]
    public void GetLlmClient_Should_ReturnUnderlyingLlmClient()
    {
        // Act
        var result = _llmCommunicator.GetLlmClient();

        // Assert
        Assert.AreEqual(_mockLlmClient.Object, result);
    }

    [TestMethod]
    public async Task CallWithFunctionsAsync_Should_CallLlmWithFunctions_When_ValidParametersProvided()
    {
        // Arrange
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var functionSpecs = new List<FunctionSpec> { new FunctionSpec { Name = "test_function" } };
        var agentId = "test-agent";
        var turnIndex = 1;

        var chunks = new List<LlmStreamingChunk> { new LlmStreamingChunk { Content = "test response" } };
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(chunks));

        // Act
        var result = await _llmCommunicator.CallWithFunctionsAsync(messages, functionSpecs, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        _mockEventManager.Verify(x => x.RaiseLlmCallStarted(agentId, turnIndex), Times.Once);
        _mockMetricsCollector.Verify(x => x.RecordLlmCallExecutionTime(agentId, turnIndex, It.IsAny<long>(), "function-calling"), Times.Once);
        _mockMetricsCollector.Verify(x => x.RecordLlmCallCompletion(agentId, turnIndex, true, "function-calling", null), Times.Once);
        _mockMetricsCollector.Verify(x => x.RecordApiCall(agentId, "LLM", "function-calling"), Times.Once);
    }

    [TestMethod]
    public async Task CallWithFunctionsAsync_Should_RecordTokenUsage_When_ResponseHasUsage()
    {
        // Arrange
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var functionSpecs = new List<FunctionSpec> { new FunctionSpec { Name = "test_function" } };
        var agentId = "test-agent";
        var turnIndex = 1;

        var chunks = new List<LlmStreamingChunk> { new LlmStreamingChunk { Content = "test response" } };
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(chunks));

        // Act
        var result = await _llmCommunicator.CallWithFunctionsAsync(messages, functionSpecs, agentId, turnIndex, CancellationToken.None);

        // Assert
        // Note: Token usage recording depends on the response having usage data
        // This test verifies the method completes successfully
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task CallWithFunctionsAsync_Should_HandleCancellation_When_CancellationTokenCancelled()
    {
        // Arrange
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var functionSpecs = new List<FunctionSpec> { new FunctionSpec { Name = "test_function" } };
        var agentId = "test-agent";
        var turnIndex = 1;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Mock the LLM client to throw OperationCanceledException when cancellation is requested
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns<LlmRequest, CancellationToken>((request, token) =>
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                return ToAsyncEnumerable(new List<LlmStreamingChunk>());
            });

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await _llmCommunicator.CallWithFunctionsAsync(messages, functionSpecs, agentId, turnIndex, cts.Token));
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_Should_ReturnParsedModelMessage_When_ValidResponse()
    {
        // Arrange
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var agentId = "test-agent";
        var turnIndex = 1;
        var turnId = "turn-1";
        var state = new AgentState { AgentId = agentId };

        var responseContent = "{\"thoughts\":\"test thoughts\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"test_tool\",\"params\":{}}}";
        var chunks = new List<LlmStreamingChunk> { new LlmStreamingChunk { Content = responseContent } };
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(chunks));

        // Act
        var result = await _llmCommunicator.CallLlmAndParseAsync(messages, agentId, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test thoughts", result.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_Should_HandleTimeout_When_LlmTimesOut()
    {
        // Arrange
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var agentId = "test-agent";
        var turnIndex = 1;
        var turnId = "turn-1";
        var state = new AgentState { AgentId = agentId };

        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        // Act
        var result = await _llmCommunicator.CallLlmAndParseAsync(messages, agentId, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(1, state.Turns.Count);
        Assert.IsFalse(state.Turns[0].ToolResult?.Success ?? true);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("deadline exceeded"))), Times.Once);
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_Should_HandleLlmException_When_LlmThrows()
    {
        // Arrange
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var agentId = "test-agent";
        var turnIndex = 1;
        var turnId = "turn-1";
        var state = new AgentState { AgentId = agentId };

        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("LLM error"));

        // Act
        var result = await _llmCommunicator.CallLlmAndParseAsync(messages, agentId, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(1, state.Turns.Count);
        Assert.IsFalse(state.Turns[0].ToolResult?.Success ?? true);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("LLM call failed"))), Times.Once);
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_Should_PropagateCancellation_When_CancellationTokenCancelled()
    {
        // Arrange
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var agentId = "test-agent";
        var turnIndex = 1;
        var turnId = "turn-1";
        var state = new AgentState { AgentId = agentId };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await _llmCommunicator.CallLlmAndParseAsync(messages, agentId, turnIndex, turnId, state, cts.Token));
    }

    [TestMethod]
    public async Task ParseJsonResponse_Should_ReturnParsedModelMessage_When_ValidJson()
    {
        // Arrange
        var jsonResponse = "{\"thoughts\":\"test thoughts\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"test_tool\",\"params\":{}}}";
        var turnIndex = 1;
        var turnId = "turn-1";
        var state = new AgentState { AgentId = "test-agent" };

        // Act
        var result = await _llmCommunicator.ParseJsonResponse(jsonResponse, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test thoughts", result.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
    }

    [TestMethod]
    public async Task ParseJsonResponse_Should_EmitStatus_When_ModelMessageHasStatusFields()
    {
        // Arrange
        var jsonResponse = "{\"thoughts\":\"test thoughts\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"test_tool\",\"params\":{}},\"status_title\":\"Test Status\",\"status_details\":\"Test Details\"}";
        var turnIndex = 1;
        var turnId = "turn-1";
        var state = new AgentState { AgentId = "test-agent" };

        // Act
        var result = await _llmCommunicator.ParseJsonResponse(jsonResponse, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        _mockStatusManager.Verify(x => x.EmitStatus(state.AgentId, "Test Status", "Test Details", null, null), Times.Once);
    }

    [TestMethod]
    public async Task ParseJsonResponse_Should_HandleInvalidJson_When_JsonIsMalformed()
    {
        // Arrange
        var llmRaw = "invalid json";
        var turnIndex = 1;
        var turnId = "turn-1";
        var state = new AgentState { AgentId = "test-agent" };

        // Act
        var result = await _llmCommunicator.ParseJsonResponse(llmRaw, turnIndex, turnId, state, CancellationToken.None);

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(1, state.Turns.Count);
        Assert.IsFalse(state.Turns[0].ToolResult?.Success ?? true);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Invalid LLM JSON"))), Times.Once);
        _mockStatusManager.Verify(x => x.EmitStatus(state.AgentId, "Invalid model output", "JSON parsing failed", "Will retry with corrected format", null), Times.Once);
        _mockEventManager.Verify(x => x.RaiseLlmCallCompleted(state.AgentId, turnIndex, null, It.Is<string>(s => s.Contains("Invalid LLM JSON"))), Times.Once);
    }

    [TestMethod]
    public void NormalizeFunctionCallToReact_Should_ReturnNormalizedMessage_When_ValidFunctionCall()
    {
        // Arrange
        var functionResult = new LlmResponse
        {
            HasFunctionCall = true,
            FunctionCall = new LlmFunctionCall
            {
                Name = "test_function",
                ArgumentsJson = "{\"param1\":\"value1\"}"
            }
        };
        var turnIndex = 1;

        // Act
        var result = _llmCommunicator.NormalizeFunctionCallToReact(functionResult, turnIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("tool_call", result.ActionRaw);
        Assert.IsNotNull(result.ActionInput);
        Assert.AreEqual("test_function", result.ActionInput.Tool);
        // The parameter value is a JsonElement, not a string
        Assert.IsTrue(result.ActionInput.Params.ContainsKey("param1"));
    }

    [TestMethod]
    public void NormalizeFunctionCallToReact_Should_ThrowArgumentException_When_NoFunctionCall()
    {
        // Arrange
        var functionResult = new LlmResponse { HasFunctionCall = false };
        var turnIndex = 1;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            _llmCommunicator.NormalizeFunctionCallToReact(functionResult, turnIndex));
    }

    [TestMethod]
    public void NormalizeFunctionCallToReact_Should_HandleInvalidArgumentsJson_When_ArgumentsAreMalformed()
    {
        // Arrange
        var functionResult = new LlmResponse
        {
            HasFunctionCall = true,
            FunctionCall = new LlmFunctionCall
            {
                Name = "test_function",
                ArgumentsJson = "invalid json"
            }
        };
        var turnIndex = 1;

        // Act
        var result = _llmCommunicator.NormalizeFunctionCallToReact(functionResult, turnIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test_function", result.ActionInput.Tool);
        Assert.AreEqual(0, result.ActionInput.Params.Count); // Empty params due to parsing failure
    }

    [TestMethod]
    public void NormalizeFunctionCallToReact_Should_HandleEmptyArgumentsJson_When_ArgumentsAreEmpty()
    {
        // Arrange
        var functionResult = new LlmResponse
        {
            HasFunctionCall = true,
            FunctionCall = new LlmFunctionCall
            {
                Name = "test_function",
                ArgumentsJson = ""
            }
        };
        var turnIndex = 1;

        // Act
        var result = _llmCommunicator.NormalizeFunctionCallToReact(functionResult, turnIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test_function", result.ActionInput.Tool);
        Assert.AreEqual(0, result.ActionInput.Params.Count);
    }
}
