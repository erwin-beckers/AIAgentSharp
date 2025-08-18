using AIAgentSharp.Agents;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Tests;

[TestClass]
public class StatusUpdateTests
{
    private MockLlmClient _mockLlmClient = null!;
    private MemoryAgentStateStore _stateStore = null!;
    private IMetricsCollector _metricsCollector = null!;
    private Agent _agent = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new MockLlmClient();
        _stateStore = new MemoryAgentStateStore();
        _metricsCollector = new MetricsCollector();
        _agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), new AgentConfiguration(), _metricsCollector);
    }

    [TestMethod]
    public async Task Agent_WithStatusFields_ShouldEmitStatus()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"status_title\":\"Processing\",\"status_details\":\"Working on the request\",\"next_step_hint\":\"Almost done\",\"progress_pct\":75}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test status updates", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithPartialStatusFields_ShouldHandlePartialStatus()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"status_title\":\"Processing\"}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test partial status updates", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithProgressOnly_ShouldHandleProgress()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"progress_pct\":50}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test progress updates", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithNextStepHint_ShouldHandleNextStep()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"next_step_hint\":\"Will complete soon\"}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test next step hint", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithStatusDetails_ShouldHandleStatusDetails()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"status_details\":\"Analyzing the request and preparing response\"}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test status details", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithInvalidProgress_ShouldHandleGracefully()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"progress_pct\":150}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test invalid progress", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithNegativeProgress_ShouldHandleGracefully()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"progress_pct\":-10}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test negative progress", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithMultipleStatusUpdates_ShouldHandleMultiple()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"status_title\":\"Processing\",\"status_details\":\"Working on the request\",\"next_step_hint\":\"Almost done\",\"progress_pct\":75}",
            "{\"thoughts\":\"I need to process this more\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"status_title\":\"Completed\",\"status_details\":\"Request processed successfully\",\"next_step_hint\":\"All done\",\"progress_pct\":100}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test multiple status updates", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithEmptyStatusFields_ShouldHandleEmpty()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"status_title\":\"\",\"status_details\":\"\",\"next_step_hint\":\"\",\"progress_pct\":0}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test empty status fields", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithNullStatusFields_ShouldHandleNull()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"},\"status_title\":null,\"status_details\":null,\"next_step_hint\":null,\"progress_pct\":null}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test null status fields", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    [TestMethod]
    public async Task Agent_WithMissingStatusFields_ShouldHandleMissing()
    {
        // Arrange
        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I need to process this\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        // Act
        var result = await _agent.RunAsync("test-agent", "Test missing status fields", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("The answer is 42", result.FinalOutput);
    }

    private class MockLlmClient : ILlmClient
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
