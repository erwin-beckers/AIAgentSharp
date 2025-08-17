using System.Text.Json;
using AIAgentSharp.Agents;

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
        Assert.AreEqual("validate_input", validationTool.Name);
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
        Assert.AreEqual(1, required.GetArrayLength());
        Assert.AreEqual("strings", required[0].GetString());
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
        var llmCommunicator = new LlmCommunicator(new DelegateLlmClient((_, _) => Task.FromResult("")), new AgentConfiguration(), new ConsoleLogger(), new EventManager(new ConsoleLogger()), new StatusManager(new AgentConfiguration(), new EventManager(new ConsoleLogger())));
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
        var llmCommunicator = new LlmCommunicator(new DelegateLlmClient((_, _) => Task.FromResult("")), new AgentConfiguration(), new ConsoleLogger(), new EventManager(new ConsoleLogger()), new StatusManager(new AgentConfiguration(), new EventManager(new ConsoleLogger())));
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
        var llmCommunicator = new LlmCommunicator(new DelegateLlmClient((_, _) => Task.FromResult("")), new AgentConfiguration(), new ConsoleLogger(), new EventManager(new ConsoleLogger()), new StatusManager(new AgentConfiguration(), new EventManager(new ConsoleLogger())));
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
            SupportsFunctionCalling = true,
            ShouldReturnFunctionCall = true,
            FunctionName = "concat",
            FunctionArguments = "{\"strings\":[\"hello\",\"world\"]}"
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
            SupportsFunctionCalling = true,
            ShouldReturnFunctionCall = true,
            FunctionName = "concat",
            FunctionArguments = "invalid json"
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
            SupportsFunctionCalling = true,
            ShouldReturnFunctionCall = true,
            FunctionName = "unknown_tool",
            FunctionArguments = "{\"param\":\"value\"}"
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
            SupportsFunctionCalling = true,
            ShouldReturnFunctionCall = true,
            FunctionName = "validate_input",
            FunctionArguments = "{\"input\":\"test_value\",\"rules\":[\"rule1\"]}"
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
            SupportsFunctionCalling = true,
            ShouldReturnFunctionCall = true,
            FunctionName = "validate_input",
            FunctionArguments = "{\"input\":\"test_value\",\"rules\":[\"rule1\"]}",
            AssistantContent = "I need to validate the test parameters."
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
        Assert.AreEqual("validate_input", result.LlmMessage.ActionInput.Tool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsTrue(result.ToolResult.Success);
        Assert.AreEqual("validate_input", result.ToolResult.Tool);
    }

    // Helper classes for testing
    private class MockFunctionCallingLlmClient : IFunctionCallingLlmClient
    {
        public bool SupportsFunctionCalling { get; set; } = true;
        public bool ShouldReturnFunctionCall { get; set; }
        public string FunctionName { get; set; } = "test";
        public string FunctionArguments { get; set; } = "{}";
        public string AssistantContent { get; set; } = "";

        public Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
        {
            return Task.FromResult("{\"thoughts\":\"Planning next steps\",\"action\":\"plan\",\"action_input\":{\"summary\":\"Planning\"}}");
        }

        public Task<FunctionCallResult> CompleteWithFunctionsAsync(IEnumerable<LlmMessage> messages, IEnumerable<OpenAiFunctionSpec> functions, CancellationToken ct = default)
        {
            if (!SupportsFunctionCalling)
            {
                return Task.FromResult(new FunctionCallResult
                {
                    HasFunctionCall = false,
                    RawTextFallback = "{\"thoughts\":\"Planning next steps\",\"action\":\"plan\",\"action_input\":{\"summary\":\"Planning\"}}"
                });
            }

            if (ShouldReturnFunctionCall)
            {
                return Task.FromResult(new FunctionCallResult
                {
                    HasFunctionCall = true,
                    FunctionName = FunctionName,
                    FunctionArgumentsJson = FunctionArguments,
                    AssistantContent = AssistantContent
                });
            }

            return Task.FromResult(new FunctionCallResult
            {
                HasFunctionCall = false,
                RawTextFallback = "{\"thoughts\":\"Planning next steps\",\"action\":\"plan\",\"action_input\":{\"summary\":\"Planning\"}}"
            });
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
