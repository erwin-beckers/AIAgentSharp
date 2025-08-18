using AIAgentSharp.Agents;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Tests;

[TestClass]
public class ToolAgnosticValidationTests
{
    private MockLlmClient _mockLlmClient;
    private MemoryAgentStateStore _stateStore;
    private IMetricsCollector _metricsCollector;
    private Agent _agent;

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new MockLlmClient();
        _stateStore = new MemoryAgentStateStore();
        _metricsCollector = new MetricsCollector();
        _agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), new AgentConfiguration(), _metricsCollector);
    }

    [TestMethod]
    public async Task Agent_WithValidToolCall_ShouldExecuteSuccessfully()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to add numbers\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":5,\"b\":3}}}",
            "{\"thoughts\":\"The result is 8\",\"action\":\"finish\",\"action_input\":{\"final\":\"The result is 8\"}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        var result = await _agent.RunAsync("test-agent", "Add 5 and 3", tools);

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput.Contains("8"));
    }

    [TestMethod]
    public async Task Agent_WithInvalidToolName_ShouldHandleError()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to call a non-existent tool\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"nonexistent\",\"params\":{\"param\":\"value\"}}}",
            "{\"thoughts\":\"I still need to call a non-existent tool\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"nonexistent\",\"params\":{\"param\":\"value\"}}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        // Use a stricter config to avoid long loops on invalid inputs
        var localAgent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), new AgentConfiguration { MaxTurns = 2 }, _metricsCollector);
        var result = await localAgent.RunAsync("test-agent", "Call non-existent tool", tools);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Error.Contains("tool") || result.Error.Contains("not found"));
    }

    [TestMethod]
    public async Task Agent_WithMissingToolParams_ShouldHandleError()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to add numbers\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{}}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        // Use a stricter config to avoid long loops on invalid inputs
        var localAgent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), new AgentConfiguration { MaxTurns = 2 }, _metricsCollector);
        var result = await localAgent.RunAsync("test-agent", "Add numbers without parameters", tools);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Error.Contains("parameter") || result.Error.Contains("required") || result.Error.Contains("Max turns") || result.Error.Contains("without completion"));
    }

    [TestMethod]
    public async Task Agent_WithInvalidToolParams_ShouldHandleError()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to add numbers\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":\"invalid\",\"b\":\"invalid\"}}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        // Use a stricter config to avoid long loops on invalid inputs
        var localAgent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), new AgentConfiguration { MaxTurns = 2 }, _metricsCollector);
        var result = await localAgent.RunAsync("test-agent", "Add numbers with invalid parameters", tools);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Error.Contains("parameter") || result.Error.Contains("invalid"));
    }

    [TestMethod]
    public async Task Agent_WithToolExecutionError_ShouldHandleError()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to divide by zero\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"divide\",\"params\":{\"a\":10,\"b\":0}}}"
        };

        var tools = new List<ITool> { new DivideTool() };

        // Act
        var localAgent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), new AgentConfiguration { MaxTurns = 2 }, _metricsCollector);
        var result = await localAgent.RunAsync("test-agent", "Divide by zero", tools);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Error.Contains("divide by zero") || result.Error.Contains("error"));
    }

    [TestMethod]
    public async Task Agent_WithMultipleValidToolCalls_ShouldExecuteSuccessfully()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to add numbers first\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":5,\"b\":3}}}",
            "{\"thoughts\":\"Now I need to multiply the result\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"multiply\",\"params\":{\"a\":8,\"b\":2}}}",
            "{\"thoughts\":\"The final result is 16\",\"action\":\"finish\",\"action_input\":{\"final\":\"The final result is 16\"}}"
        };

        var tools = new List<ITool> { new AddTool(), new MultiplyTool() };

        // Act
        var result = await _agent.RunAsync("test-agent", "Add 5 and 3, then multiply by 2", tools);

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput.Contains("16"));
    }

    [TestMethod]
    public async Task Agent_WithMixedValidAndInvalidToolCalls_ShouldHandleErrors()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to add numbers first\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":5,\"b\":3}}}",
            "{\"thoughts\":\"Now I need to call a non-existent tool\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"nonexistent\",\"params\":{\"param\":\"value\"}}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        var localAgent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), new AgentConfiguration { MaxTurns = 2 }, _metricsCollector);
        var result = await localAgent.RunAsync("test-agent", "Add numbers then call non-existent tool", tools);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Error.Contains("tool") || result.Error.Contains("not found"));
    }

    [TestMethod]
    public async Task Agent_WithComplexToolParams_ShouldHandleCorrectly()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to add complex numbers\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":3.14,\"b\":2.86}}}",
            "{\"thoughts\":\"The result is 6\",\"action\":\"finish\",\"action_input\":{\"final\":\"The result is 6\"}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        var result = await _agent.RunAsync("test-agent", "Add decimal numbers", tools);

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput.Contains("6"));
    }

    [TestMethod]
    public async Task Agent_WithToolCallAndFinalAnswer_ShouldCompleteSuccessfully()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to add numbers\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":5,\"b\":3}}}",
            "{\"thoughts\":\"The result is 8\",\"action\":\"finish\",\"action_input\":{\"final\":\"The result is 8\"}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        var result = await _agent.RunAsync("test-agent", "Add 5 and 3", tools);

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput.Contains("8"));
    }

    [TestMethod]
    public async Task Agent_WithDirectFinalAnswer_ShouldCompleteSuccessfully()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I can answer this directly\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        var result = await _agent.RunAsync("test-agent", "What is the answer to life?", tools);

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput.Contains("42"));
    }

    public class MockLlmClient : ILlmClient
    {
        public List<string> Responses { get; set; } = new();
        public int CallCount { get; private set; }

        public Task<LlmCompletionResult> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
        {
            CallCount++;
            string response;
            if (CallCount <= Responses.Count)
            {
                response = Responses[CallCount - 1];
            }
            else
            {
                // When we run out of responses, return an invalid response that will cause parsing to fail
                response = "INVALID_JSON_RESPONSE";
            }
            return Task.FromResult(new LlmCompletionResult { Content = response });
        }

        public Task<FunctionCallResult> CompleteWithFunctionsAsync(IEnumerable<LlmMessage> messages, IEnumerable<OpenAiFunctionSpec> functions, CancellationToken ct = default)
        {
            throw new NotSupportedException("Function calling not supported in mock");
        }
    }
}

// Extension method to access private method for testing
public static class StatefulAgentExtensions
{
}