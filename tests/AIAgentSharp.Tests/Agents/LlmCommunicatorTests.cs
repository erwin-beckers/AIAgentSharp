using Moq;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class LlmCommunicatorTests
{
    private Mock<ILlmClient> _mockLlmClient;
    private Mock<IEventManager> _mockEventManager;
    private Mock<IStatusManager> _mockStatusManager;
    private Mock<ILogger> _mockLogger;
    private IMetricsCollector _metricsCollector;
    private AgentConfiguration _config;
    private LlmCommunicator _communicator;

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new Mock<ILlmClient>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _mockLogger = new Mock<ILogger>();
        _metricsCollector = new MetricsCollector();
        _config = new AgentConfiguration();
        _communicator = new LlmCommunicator(
            _mockLlmClient.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _metricsCollector);
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_WithValidResponse_ShouldReturnModelMessage()
    {
        // Arrange
        var expectedResponse = new LlmCompletionResult
        {
            Content = "{\"thoughts\":\"I can answer this directly\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var state = new AgentState { AgentId = "test-agent" };

        // Act
        var result = await _communicator.CallLlmAndParseAsync(messages, "test-agent", 0, "turn-123", state, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I can answer this directly", result.Thoughts);
        Assert.AreEqual(AgentAction.Finish, result.Action);
        Assert.AreEqual("The answer is 42", result.ActionInput?.Final);
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_WithToolCall_ShouldReturnToolCallMessage()
    {
        // Arrange
        var expectedResponse = new LlmCompletionResult
        {
            Content = "{\"thoughts\":\"I need to add numbers\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":5,\"b\":3}}}"
        };

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var state = new AgentState { AgentId = "test-agent" };

        // Act
        var result = await _communicator.CallLlmAndParseAsync(messages, "test-agent", 0, "turn-123", state, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I need to add numbers", result.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("add", result.ActionInput?.Tool);
        Assert.IsNotNull(result.ActionInput?.Params);
        Assert.AreEqual("5", result.ActionInput.Params["a"]?.ToString());
        Assert.AreEqual("3", result.ActionInput.Params["b"]?.ToString());
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var expectedResponse = new LlmCompletionResult
        {
            Content = "Invalid JSON response"
        };

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var state = new AgentState { AgentId = "test-agent" };

        // Act
        var result = await _communicator.CallLlmAndParseAsync(messages, "test-agent", 0, "turn-123", state, CancellationToken.None);

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(1, state.Turns.Count);
        Assert.IsFalse(state.Turns[0].ToolResult?.Success ?? true);
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_WithTimeout_ShouldHandleTimeout()
    {
        // Arrange
        var config = new AgentConfiguration { LlmTimeout = TimeSpan.FromMilliseconds(100) };
        var communicator = new LlmCommunicator(
            _mockLlmClient.Object,
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _metricsCollector);

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(async (IEnumerable<LlmMessage> messages, CancellationToken ct) =>
            {
                await Task.Delay(200, ct);
                return new LlmCompletionResult { Content = "test" };
            });

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var state = new AgentState { AgentId = "test-agent" };

        // Act
        var result = await communicator.CallLlmAndParseAsync(messages, "test-agent", 0, "turn-123", state, CancellationToken.None);

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(1, state.Turns.Count);
        Assert.IsFalse(state.Turns[0].ToolResult?.Success ?? true);
        Assert.IsTrue(state.Turns[0].ToolResult?.Error?.Contains("deadline exceeded") ?? false);
    }

    [TestMethod]
    public async Task CallWithFunctionsAsync_WithValidFunctionCall_ShouldReturnFunctionResult()
    {
        // Arrange
        var expectedResponse = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "add",
            FunctionArgumentsJson = "{\"a\":5,\"b\":3}",
            AssistantContent = "I need to add 5 and 3"
        };

        _mockLlmClient.Setup(x => x.CompleteWithFunctionsAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<IEnumerable<OpenAiFunctionSpec>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var functions = new List<OpenAiFunctionSpec> { new OpenAiFunctionSpec { Name = "add" } };

        // Act
        var result = await _communicator.CallWithFunctionsAsync(messages, functions, "test-agent", 0, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasFunctionCall);
        Assert.AreEqual("add", result.FunctionName);
        Assert.AreEqual("{\"a\":5,\"b\":3}", result.FunctionArgumentsJson);
        Assert.AreEqual("I need to add 5 and 3", result.AssistantContent);
    }

    [TestMethod]
    public async Task CallWithFunctionsAsync_WithNoFunctionCall_ShouldReturnNoFunctionResult()
    {
        // Arrange
        var expectedResponse = new FunctionCallResult
        {
            HasFunctionCall = false,
            AssistantContent = "I can answer this directly"
        };

        _mockLlmClient.Setup(x => x.CompleteWithFunctionsAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<IEnumerable<OpenAiFunctionSpec>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var functions = new List<OpenAiFunctionSpec> { new OpenAiFunctionSpec { Name = "add" } };

        // Act
        var result = await _communicator.CallWithFunctionsAsync(messages, functions, "test-agent", 0, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.HasFunctionCall);
        Assert.AreEqual("I can answer this directly", result.AssistantContent);
    }

    [TestMethod]
    public async Task CallWithFunctionsAsync_WithUsageData_ShouldRecordMetrics()
    {
        // Arrange
        var expectedResponse = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "add",
            FunctionArgumentsJson = "{\"a\":5,\"b\":3}",
            AssistantContent = "I need to add 5 and 3",
            Usage = new LlmUsage
            {
                InputTokens = 100,
                OutputTokens = 50,
                Model = "test-model",
                Provider = "test-provider"
            }
        };

        _mockLlmClient.Setup(x => x.CompleteWithFunctionsAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<IEnumerable<OpenAiFunctionSpec>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var messages = new List<LlmMessage> {new LlmMessage { Role = "user", Content = "test" } };
        var functions = new List<OpenAiFunctionSpec> { new OpenAiFunctionSpec { Name = "add" } };

        // Act
        var result = await _communicator.CallWithFunctionsAsync(messages, functions, "test-agent", 0, CancellationToken.None);
        var metrics = ((IMetricsProvider)_metricsCollector).GetMetrics();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(metrics.Resources.TotalInputTokens > 0);
        Assert.IsTrue(metrics.Resources.TotalOutputTokens > 0);
    }

    [TestMethod]
    public void NormalizeFunctionCallToReact_WithValidFunctionCall_ShouldReturnModelMessage()
    {
        // Arrange
        var functionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "add",
            FunctionArgumentsJson = "{\"a\":5,\"b\":3}",
            AssistantContent = "I need to add 5 and 3"
        };

        // Act
        var result = _communicator.NormalizeFunctionCallToReact(functionResult, 0);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I need to add 5 and 3", result.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("add", result.ActionInput?.Tool);
        Assert.IsNotNull(result.ActionInput?.Params);
        Assert.AreEqual(5, result.ActionInput.Params["a"]);
        Assert.AreEqual(3, result.ActionInput.Params["b"]);
    }

    [TestMethod]
    public void  NormalizeFunctionCallToReact_WithComplexArguments_ShouldParseCorrectly()
    {
        // Arrange
        var functionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "complex_tool",
            FunctionArgumentsJson = "{\"string_param\":\"test\",\"int_param\":42,\"bool_param\":true,\"array_param\":[1,2,3]}",
            AssistantContent = "I need to call a complex tool"
        };

        // Act
        var result = _communicator.NormalizeFunctionCallToReact(functionResult, 0);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I need to call a complex tool", result.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("complex_tool", result.ActionInput?.Tool);
        Assert.IsNotNull(result.ActionInput?.Params);
        Assert.AreEqual("test", result.ActionInput.Params["string_param"]);
        Assert.AreEqual(42, result.ActionInput.Params["int_param"]);
        Assert.AreEqual(true, result.ActionInput.Params["bool_param"]);
        Assert.IsInstanceOfType(result.ActionInput.Params["array_param"], typeof(List<object>));
    }

    [TestMethod]
    public async Task CallLlmAndParseAsync_WithMetrics_ShouldRecordMetrics()
    {
        // Arrange
        var expectedResponse = new LlmCompletionResult
        {
            Content = "{\"thoughts\":\"I can answer this directly\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}",
            Usage = new LlmUsage
            {
                InputTokens = 100,
                OutputTokens = 50,
                Model = "test-model",
                Provider = "test-provider"
            }
        };

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var state = new AgentState { AgentId = "test-agent" };

        // Act
        var result = await _communicator.CallLlmAndParseAsync(messages, "test-agent", 0, "turn-123", state, CancellationToken.None);
        var metrics = ((IMetricsProvider)_metricsCollector).GetMetrics();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(metrics.Performance.TotalLlmCalls > 0);
        Assert.IsTrue(metrics.Resources.TotalInputTokens > 0);
        Assert.IsTrue(metrics.Resources.TotalOutputTokens > 0);
    }
}
