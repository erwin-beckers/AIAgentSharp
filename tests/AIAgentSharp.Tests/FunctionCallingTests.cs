using System.Text.Json;
using AIAgentSharp.Agents;
using AIAgentSharp.Metrics;

namespace AIAgentSharp.Tests;

[TestClass]
public class FunctionCallingTests
{
    [TestMethod]
    public void Tools_ImplementIFunctionSchemaProvider()
    {
        // Arrange & Act
        var concatTool = new MockConcatTool();
        var validationTool = new MockValidationTool();

        // Assert
        Assert.IsTrue(concatTool is IFunctionSchemaProvider);
        Assert.IsTrue(validationTool is IFunctionSchemaProvider);
        Assert.AreEqual("concat", concatTool.Name);
        Assert.AreEqual("validation_tool", validationTool.Name);
    }

    [TestMethod]
    public void MockConcatTool_GetJsonSchema_ReturnsValidSchema()
    {
        // Arrange
        var tool = new MockConcatTool();

        // Act
        var schema = tool.GetJsonSchema();

        // Assert
        Assert.IsNotNull(schema);
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual("object", root.GetProperty("type").GetString());
        Assert.IsTrue(root.TryGetProperty("properties", out var properties));
        Assert.IsTrue(properties.TryGetProperty("strings", out var strings));

        Assert.AreEqual("array", strings.GetProperty("type").GetString());

        var required = root.GetProperty("required");
        Assert.AreEqual(2, required.GetArrayLength());
        Assert.AreEqual("strings", required[1].GetString());
        Assert.AreEqual("separator", required[0].GetString());
    }

    [TestMethod]
    public void MockValidationTool_GetJsonSchema_ReturnsValidSchema()
    {
        // Arrange
        var tool = new MockValidationTool();

        // Act
        var schema = tool.GetJsonSchema();
        var json = JsonSerializer.Serialize(schema);
        var root = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.IsNotNull(schema);

        Assert.AreEqual("object", root.GetProperty("type").GetString());
        Assert.IsTrue(root.TryGetProperty("properties", out var properties));
        Assert.IsTrue(properties.TryGetProperty("input", out var inputParam));
        Assert.IsTrue(properties.TryGetProperty("rules", out var rulesParam));

        var inputType = inputParam.GetProperty("type");
        if (inputType.ValueKind == JsonValueKind.Array)
        {
            var inputTypes = inputType.EnumerateArray().ToArray();
            Assert.IsTrue(inputTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", inputType.GetString());
        }

        Assert.AreEqual("array", rulesParam.GetProperty("type").GetString());

        var required = root.GetProperty("required");
        Assert.AreEqual(2, required.GetArrayLength());
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "input"));
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "rules"));
    }

    [TestMethod]
    public void LlmCommunicator_NormalizeFunctionCallToReact_WithAssistantContent()
    {
        // Arrange
        var llmCommunicator = new LlmCommunicator(new DelegateLlmClient((_, _) => Task.FromResult(new LlmCompletionResult { Content = string.Empty })), new AgentConfiguration(), new ConsoleLogger(), new EventManager(new ConsoleLogger()), new StatusManager(new AgentConfiguration(), new EventManager(new ConsoleLogger())), new MetricsCollector(new ConsoleLogger()));
        var functionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "get_indicator",
            FunctionArgumentsJson = "{\"symbol\":\"MNQ\",\"indicator\":\"RSI\",\"period\":14}",
            AssistantContent = "I need to check the RSI value for MNQ to assess market conditions."
        };

        // Act
        var modelMsg = llmCommunicator.NormalizeFunctionCallToReact(functionResult, 0);

        // Assert
        Assert.AreEqual("I need to check the RSI value for MNQ to assess market conditions.", modelMsg.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, modelMsg.Action);
        Assert.AreEqual("get_indicator", modelMsg.ActionInput.Tool);
        Assert.IsNotNull(modelMsg.ActionInput.Params);
        Assert.AreEqual("MNQ", modelMsg.ActionInput.Params["symbol"]);
        Assert.AreEqual("RSI", modelMsg.ActionInput.Params["indicator"]);
        Assert.AreEqual(14, Convert.ToInt32(modelMsg.ActionInput.Params["period"]));
    }

    [TestMethod]
    public void LlmCommunicator_NormalizeFunctionCallToReact_WithoutAssistantContent()
    {
        // Arrange
        var llmCommunicator = new LlmCommunicator(new DelegateLlmClient((_, _) => Task.FromResult(new LlmCompletionResult { Content = string.Empty })), new AgentConfiguration(), new ConsoleLogger(), new EventManager(new ConsoleLogger()), new StatusManager(new AgentConfiguration(), new EventManager(new ConsoleLogger())), new MetricsCollector(new ConsoleLogger()));
        var functionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "concat",
            FunctionArgumentsJson = "{\"strings\":[\"hello\",\"world\"]}",
            AssistantContent = ""
        };

        // Act
        var modelMsg = llmCommunicator.NormalizeFunctionCallToReact(functionResult, 0);

        // Assert
        Assert.AreEqual("Calling concat to advance the plan.", modelMsg.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, modelMsg.Action);
        Assert.AreEqual("concat", modelMsg.ActionInput.Tool);
        Assert.IsNotNull(modelMsg.ActionInput.Params);
        Assert.IsTrue(modelMsg.ActionInput.Params.ContainsKey("strings"));
    }

    [TestMethod]
    public void LlmCommunicator_NormalizeFunctionCallToReact_InvalidArguments()
    {
        // Arrange
        var llmCommunicator = new LlmCommunicator(new DelegateLlmClient((_, _) => Task.FromResult(new LlmCompletionResult { Content = string.Empty })), new AgentConfiguration(), new ConsoleLogger(), new EventManager(new ConsoleLogger()), new StatusManager(new AgentConfiguration(), new EventManager(new ConsoleLogger())), new MetricsCollector(new ConsoleLogger()));
        var functionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "get_indicator",
            FunctionArgumentsJson = "invalid json",
            AssistantContent = "Testing"
        };

        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() =>
            llmCommunicator.NormalizeFunctionCallToReact(functionResult, 0));
        Assert.IsTrue(ex.Message.Contains("Failed to parse function arguments"));
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingDisabled_UsesJsonPath()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient();
        var config = new AgentConfiguration { UseFunctionCalling = false };
        var agent = new Agent(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new MockConcatTool() };

        // Act
        var result = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual(AgentAction.Plan, result.LlmMessage.Action);
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingEnabled_NoFunctionSchemas_UsesJsonPath()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient();
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new Agent(mockClient, new MemoryAgentStateStore(), config: config);
        var nonSchemaTool = new NonSchemaTool();
        var tools = new List<ITool> { nonSchemaTool };

        // Act
        var result = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual(AgentAction.Plan, result.LlmMessage.Action);
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingEnabled_ModelWithoutSupport_UsesJsonPath()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient { SupportsFunctionCalling = false };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new Agent(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new MockConcatTool() };

        // Act
        var result = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual(AgentAction.Plan, result.LlmMessage.Action);
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingEnabled_ValidFunctionCall_ExecutesTool()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient
        {
            FunctionCallResponses = new List<FunctionCallResult>
            {
                new FunctionCallResult
                {
                    HasFunctionCall = true,
                    FunctionName = "concat",
                    FunctionArgumentsJson = "{\"strings\":[\"hello\",\"world\"],\"separator\":\",\"}"
                }
            }
        };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new Agent(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new MockConcatTool() };

        // Act
        var result = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsTrue(result.ToolResult.Success);
        Assert.AreEqual("concat", result.ToolResult.Tool);
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingEnabled_MalformedArguments_RecordsError()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient
        {
            FunctionCallResponses = new List<FunctionCallResult>
            {
                new FunctionCallResult
                {
                    HasFunctionCall = true,
                    FunctionName = "concat",
                    FunctionArgumentsJson = "invalid json"
                }
            }
        };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new Agent(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new MockConcatTool() };

        // Act
        var result = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        Assert.IsNotNull(result.ToolResult.Error);
        Assert.IsTrue(result.ToolResult.Error.Contains("Failed to parse function arguments"));
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingEnabled_UnknownTool_RecordsError()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient
        {
            FunctionCallResponses = new List<FunctionCallResult>
            {
                new FunctionCallResult
                {
                    HasFunctionCall = true,
                    FunctionName = "unknown_tool",
                    FunctionArgumentsJson = "{\"param\":\"value\"}"
                }
            }
        };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new Agent(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new MockConcatTool() };

        // Act
        var result = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        Assert.IsNotNull(result.ToolResult.Error);
        Assert.IsTrue(result.ToolResult.Error.Contains("Tool 'unknown_tool' not found"));
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingEnabled_RepeatedCall_UsesIdempotency()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient
        {
            FunctionCallResponses = new List<FunctionCallResult>
            {
                new FunctionCallResult
                {
                    HasFunctionCall = true,
                    FunctionName = "validation_tool",
                    FunctionArgumentsJson = "{\"input\":\"test_value\",\"rules\":[\"rule1\"]}"
                }
            }
        };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new Agent(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new MockValidationTool() };

        // Act - First call
        var result1 = agent.StepAsync("test", "Test goal", tools).Result;

        // Act - Second call with same parameters
        var result2 = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result1.Continue);
        Assert.IsTrue(result1.ExecutedTool);
        Assert.IsTrue(result2.Continue);
        Assert.IsTrue(result2.ExecutedTool);
        Assert.IsNotNull(result1.ToolResult);
        Assert.IsNotNull(result2.ToolResult);
        Assert.AreEqual(result1.ToolResult.TurnId, result2.ToolResult.TurnId);
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingEnabled_CompleteWorkflow()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient
        {
            FunctionCallResponses = new List<FunctionCallResult>
            {
                new FunctionCallResult
                {
                    HasFunctionCall = true,
                    FunctionName = "validation_tool",
                    FunctionArgumentsJson = "{\"input\":\"test_value\",\"rules\":[\"rule1\"]}",
                    AssistantContent = "I need to validate the test parameters."
                }
            }
        };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new Agent(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new MockValidationTool() };

        // Act
        var result = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual("I need to validate the test parameters.", result.LlmMessage.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.LlmMessage.Action);
        Assert.IsNotNull(result.LlmMessage.ActionInput);
        Assert.AreEqual("validation_tool", result.LlmMessage.ActionInput.Tool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsTrue(result.ToolResult.Success);
        Assert.AreEqual("validation_tool", result.ToolResult.Tool);
    }

    // Helper classes for testing
    private class MockFunctionCallingLlmClient : ILlmClient
    {
        public bool SupportsFunctionCalling { get; set; } = true;
        public List<FunctionCallResult> FunctionCallResponses { get; set; } = new();
        public List<LlmCompletionResult> RegularResponses { get; set; } = new();
        public int CallCount { get; private set; }

        public Task<LlmCompletionResult> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
        {
            CallCount++;
            var response = RegularResponses.Count > 0 ? RegularResponses[Math.Min(CallCount - 1, RegularResponses.Count - 1)] : new LlmCompletionResult { Content = "{\"thoughts\":\"I need to plan my approach\",\"action\":\"plan\",\"action_input\":{\"summary\":\"Planning the next steps\"}}" };
            return Task.FromResult(response);
        }

        public Task<FunctionCallResult> CompleteWithFunctionsAsync(IEnumerable<LlmMessage> messages, IEnumerable<OpenAiFunctionSpec> functions, CancellationToken ct = default)
        {
            CallCount++;
            var response = FunctionCallResponses.Count > 0 ? FunctionCallResponses[Math.Min(CallCount - 1, FunctionCallResponses.Count - 1)] : new FunctionCallResult { HasFunctionCall = false, AssistantContent = "{\"thoughts\":\"I need to plan my approach\",\"action\":\"plan\",\"action_input\":{\"summary\":\"Planning the next steps\"}}" };
            return Task.FromResult(response);
        }
    }

    private class NonSchemaTool : ITool
    {
        public string Name => "non_schema_tool";

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            return Task.FromResult<object?>(new { result = "basic" });
        }
    }
}
