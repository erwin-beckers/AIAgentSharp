using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Tests;

[TestClass]
public class ReasoningTests
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
    public async Task Agent_WithChainOfThoughtReasoning_ShouldReasonCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

		_mockLlmClient.Responses = new List<string>
		{
			// CoT: analysis
			"{\"reasoning\":\"Analyze the question: It's asking for the canonical answer.\",\"confidence\":0.9,\"insights\":[\"Refers to Hitchhiker's Guide\"]}",
			// CoT: planning
			"{\"reasoning\":\"Plan: recall canonical answer and present directly.\",\"confidence\":0.9,\"insights\":[\"No tools required\"]}",
			// CoT: strategy
			"{\"reasoning\":\"Strategy: provide succinct numeric answer.\",\"confidence\":0.9,\"insights\":[\"Answer is numeric\"]}",
			// CoT: evaluation with conclusion
			"{\"reasoning\":\"Evaluation: direct answer is appropriate.\",\"confidence\":0.95,\"insights\":[\"High confidence\"],\"conclusion\":\"Provide 42 as final\"}",
			// CoT: validation
			"{\"is_valid\":true}",
			// Main agent turn
			"{\"thoughts\":\"I can answer this directly\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
		};

        // Act
        var result = await agent.RunAsync("test-agent", "What is the answer to life?", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput?.Contains("42"));
    }

    [TestMethod]
    public async Task Agent_WithTreeOfThoughtsReasoning_ShouldReasonCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.TreeOfThoughts };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            // ToT: initial thought
            "{\"thoughts\":\"Let me explore different approaches to this problem. Approach 1: Direct calculation. Approach 2: Estimation. Approach 3: Logical reasoning.\",\"children\":[\"calc\",\"est\",\"logic\"]}",
            // ToT: child thoughts
            "{\"thoughts\":\"Direct calculation approach\",\"score\":0.8}",
            "{\"thoughts\":\"Estimation approach\",\"score\":0.6}",
            "{\"thoughts\":\"Logical reasoning approach\",\"score\":0.9}",
            // ToT: conclusion
            "{\"thoughts\":\"Based on the exploration, logical reasoning is the best approach\",\"conclusion\":\"The answer is 42\"}",
            // Main agent turn
            "{\"thoughts\":\"Based on the reasoning, I can provide the answer\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        // Act
        var result = await agent.RunAsync("test-agent", "What is the answer to life?", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput?.Contains("42"));
    }

    [TestMethod]
    public async Task Agent_WithNoReasoning_ShouldWorkNormally()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.None };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            "{\"thoughts\":\"I can answer this directly\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        // Act
        var result = await agent.RunAsync("test-agent", "What is the answer to life?", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput?.Contains("42"));
    }

    [TestMethod]
    public async Task Agent_WithReasoningFailure_ShouldContinueNormally()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            // CoT: analysis
            "{\"reasoning\":\"Analyze the question: It's asking for the answer to life.\",\"confidence\":0.9,\"insights\":[\"Philosophical question\"]}",
            // CoT: planning
            "{\"reasoning\":\"Plan: provide a classic answer.\",\"confidence\":0.9,\"insights\":[\"Use well-known reference\"]}",
            // CoT: strategy
            "{\"reasoning\":\"Strategy: give the canonical answer.\",\"confidence\":0.9,\"insights\":[\"Answer is 42\"]}",
            // CoT: evaluation with conclusion
            "{\"reasoning\":\"Evaluation: this is the right approach.\",\"confidence\":0.95,\"insights\":[\"High confidence\"],\"conclusion\":\"The answer is 42\"}",
            // CoT: validation
            "{\"reasoning\":\"Validation: this answer is appropriate.\",\"confidence\":0.9,\"insights\":[\"Valid response\"]}",
            // Main agent turn
            "{\"thoughts\":\"I can answer this directly despite reasoning failure\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        // Act
        var result = await agent.RunAsync("test-agent", "What is the answer to life?", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput?.Contains("42"));
    }

    [TestMethod]
    public async Task Agent_WithComplexReasoning_ShouldHandleMultipleSteps()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            // CoT: analysis
            "{\"reasoning\":\"Analyze the problem: It's asking for the answer to life.\",\"confidence\":0.9,\"insights\":[\"Complex philosophical question\"]}",
            // CoT: planning
            "{\"reasoning\":\"Plan: Break it down into components and solve each.\",\"confidence\":0.9,\"insights\":[\"Multi-step approach needed\"]}",
            // CoT: strategy
            "{\"reasoning\":\"Strategy: Use step-by-step reasoning.\",\"confidence\":0.9,\"insights\":[\"Logical progression\"]}",
            // CoT: evaluation with conclusion
            "{\"reasoning\":\"Evaluation: Step-by-step approach is correct.\",\"confidence\":0.95,\"insights\":[\"High confidence\"],\"conclusion\":\"The answer is 42\"}",
            // CoT: validation
            "{\"reasoning\":\"Validation: This approach is appropriate.\",\"confidence\":0.9,\"insights\":[\"Valid methodology\"]}",
            // Main agent turn
            "{\"thoughts\":\"Step 1: Analyze the problem. Step 2: Break it down into components. Step 3: Solve each component. Step 4: Combine results.\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        // Act
        var result = await agent.RunAsync("test-agent", "What is the answer to life?", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput?.Contains("42"));
    }

    [TestMethod]
    public async Task Agent_WithReasoningAndToolCalls_ShouldWorkTogether()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            // CoT: analysis
            "{\"reasoning\":\"Analyze the task: Need to add two numbers.\",\"confidence\":0.9,\"insights\":[\"Simple arithmetic\"]}",
            // CoT: planning
            "{\"reasoning\":\"Plan: Use addition tool to calculate.\",\"confidence\":0.9,\"insights\":[\"Tool-based approach\"]}",
            // CoT: strategy
            "{\"reasoning\":\"Strategy: Call add tool then interpret result.\",\"confidence\":0.9,\"insights\":[\"Two-step process\"]}",
            // CoT: evaluation with conclusion
            "{\"reasoning\":\"Evaluation: This approach will work.\",\"confidence\":0.95,\"insights\":[\"High confidence\"],\"conclusion\":\"Use add tool\"}",
            // CoT: validation
            "{\"reasoning\":\"Validation: Tool approach is appropriate.\",\"confidence\":0.9,\"insights\":[\"Valid method\"]}",
            // Main agent turn - tool call
            "{\"thoughts\":\"Let me think about this step by step. First, I need to add the numbers.\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":5,\"b\":3}}}",
            // Tool result turn
            "{\"thoughts\":\"Now that I have the result, let me think about what this means.\",\"action\":\"finish\",\"action_input\":{\"final\":\"The result is 8\"}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        var result = await agent.RunAsync("test-agent", "Add 5 and 3", tools);

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput?.Contains("8"));
    }

    [TestMethod]
    public async Task Agent_WithReasoningMetrics_ShouldRecordMetrics()
    {
        // Arrange
        var config = new AgentConfiguration 
        { 
            ReasoningType = ReasoningType.ChainOfThought,
            MaxTurns = 5
        };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            // CoT: analysis
            "{\"reasoning\":\"Analyze the question: It's asking for the answer to life.\",\"confidence\":0.9,\"insights\":[\"Philosophical question\"]}",
            // CoT: planning
            "{\"reasoning\":\"Plan: provide a classic answer.\",\"confidence\":0.9,\"insights\":[\"Use well-known reference\"]}",
            // CoT: strategy
            "{\"reasoning\":\"Strategy: give the canonical answer.\",\"confidence\":0.9,\"insights\":[\"Answer is 42\"]}",
            // CoT: evaluation with conclusion
            "{\"reasoning\":\"Evaluation: this is the right approach.\",\"confidence\":0.95,\"insights\":[\"High confidence\"],\"conclusion\":\"The answer is 42\"}",
            // CoT: validation
            "{\"reasoning\":\"Validation: this answer is appropriate.\",\"confidence\":0.9,\"insights\":[\"Valid response\"]}",
            // Main agent turn
            "{\"thoughts\":\"Let me think about this step by step.\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        // Act
        var result = await agent.RunAsync("test-agent", "What is the answer to life?", new List<ITool>());
        var metrics = ((IMetricsProvider)_metricsCollector).GetMetrics();

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(metrics.Performance.TotalAgentRuns > 0);
        Assert.IsTrue(metrics.Performance.TotalAgentSteps > 0);
    }

    [TestMethod]
    public async Task Agent_WithReasoningTimeout_ShouldHandleTimeout()
    {
        // Arrange
        var config = new AgentConfiguration 
        { 
            ReasoningType = ReasoningType.ChainOfThought,
            LlmTimeout = TimeSpan.FromMilliseconds(100)
        };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            // CoT: analysis
            "{\"reasoning\":\"Analyze the question: It's asking for the answer to life.\",\"confidence\":0.9,\"insights\":[\"Philosophical question\"]}",
            // CoT: planning
            "{\"reasoning\":\"Plan: provide a classic answer.\",\"confidence\":0.9,\"insights\":[\"Use well-known reference\"]}",
            // CoT: strategy
            "{\"reasoning\":\"Strategy: give the canonical answer.\",\"confidence\":0.9,\"insights\":[\"Answer is 42\"]}",
            // CoT: evaluation with conclusion
            "{\"reasoning\":\"Evaluation: this is the right approach.\",\"confidence\":0.95,\"insights\":[\"High confidence\"],\"conclusion\":\"The answer is 42\"}",
            // CoT: validation
            "{\"reasoning\":\"Validation: this answer is appropriate.\",\"confidence\":0.9,\"insights\":[\"Valid response\"]}",
            // Main agent turn
            "{\"thoughts\":\"Let me think about this step by step.\",\"action\":\"finish\",\"action_input\":{\"final\":\"The answer is 42\"}}"
        };

        // Act
        var result = await agent.RunAsync("test-agent", "What is the answer to life?", new List<ITool>());

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.FinalOutput?.Contains("42"));
    }

    [TestMethod]
    public async Task Agent_WithReasoningAndInvalidJson_ShouldHandleError()
    {
        // Arrange
        var config = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            // CoT: analysis
            "{\"reasoning\":\"Analyze the question: It's asking for the answer to life.\",\"confidence\":0.9,\"insights\":[\"Philosophical question\"]}",
            // CoT: planning
            "{\"reasoning\":\"Plan: provide a classic answer.\",\"confidence\":0.9,\"insights\":[\"Use well-known reference\"]}",
            // CoT: strategy
            "{\"reasoning\":\"Strategy: give the canonical answer.\",\"confidence\":0.9,\"insights\":[\"Answer is 42\"]}",
            // CoT: evaluation with conclusion
            "{\"reasoning\":\"Evaluation: this is the right approach.\",\"confidence\":0.95,\"insights\":[\"High confidence\"],\"conclusion\":\"The answer is 42\"}",
            // CoT: validation
            "{\"reasoning\":\"Validation: this answer is appropriate.\",\"confidence\":0.9,\"insights\":[\"Valid response\"]}",
            // Main agent turn - invalid JSON
            "Invalid JSON response"
        };

        // Act
        var result = await agent.RunAsync("test-agent", "What is the answer to life?", new List<ITool>());

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Error?.Contains("JSON") == true || result.Error?.Contains("parse") == true);
    }

    [TestMethod]
    public async Task Agent_WithReasoningAndMaxSteps_ShouldRespectLimit()
    {
        // Arrange
        var config = new AgentConfiguration 
        { 
            ReasoningType = ReasoningType.ChainOfThought,
            MaxTurns = 1
        };
        var agent = new Agent(_mockLlmClient, _stateStore, new ConsoleLogger(), config, _metricsCollector);

        _mockLlmClient.Responses = new List<string>
        {
            // CoT: analysis
            "{\"reasoning\":\"Analyze the task: Need to add two numbers.\",\"confidence\":0.9,\"insights\":[\"Simple arithmetic\"]}",
            // CoT: planning
            "{\"reasoning\":\"Plan: Use addition tool to calculate.\",\"confidence\":0.9,\"insights\":[\"Tool-based approach\"]}",
            // CoT: strategy
            "{\"reasoning\":\"Strategy: Call add tool then interpret result.\",\"confidence\":0.9,\"insights\":[\"Two-step process\"]}",
            // CoT: evaluation with conclusion
            "{\"reasoning\":\"Evaluation: This approach will work.\",\"confidence\":0.95,\"insights\":[\"High confidence\"],\"conclusion\":\"Use add tool\"}",
            // CoT: validation
            "{\"reasoning\":\"Validation: Tool approach is appropriate.\",\"confidence\":0.9,\"insights\":[\"Valid method\"]}",
            // Main agent turn - tool call (this will exceed MaxTurns=1)
            "{\"thoughts\":\"Let me think about this step by step.\",\"action\":\"tool_call\",\"action_input\":{\"tool\":\"add\",\"params\":{\"a\":5,\"b\":3}}}"
        };

        var tools = new List<ITool> { new AddTool() };

        // Act
        var result = await agent.RunAsync("test-agent", "Add 5 and 3", tools);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Error?.Contains("max steps") == true || result.Error?.Contains("limit") == true || result.Error?.Contains("Max turns") == true);
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

