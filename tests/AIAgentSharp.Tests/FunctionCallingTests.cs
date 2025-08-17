using System.Text.Json;

namespace AIAgentSharp.Tests;

[TestClass]
public class FunctionCallingTests
{
    [TestMethod]
    public void Tools_ImplementIFunctionSchemaProvider()
    {
        // Arrange & Act
        var concatTool = new ConcatTool();
        var indicatorTool = new GetIndicatorTool();

        // Assert
        Assert.IsTrue(concatTool is IFunctionSchemaProvider);
        Assert.IsTrue(indicatorTool is IFunctionSchemaProvider);
        Assert.AreEqual("concat", concatTool.Name);
        Assert.AreEqual("get_indicator", indicatorTool.Name);
    }

    [TestMethod]
    public void ConcatTool_GetJsonSchema_ReturnsValidSchema()
    {
        // Arrange
        var tool = new ConcatTool();

        // Act
        var schema = tool.GetJsonSchema();

        // Assert
        Assert.IsNotNull(schema);
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual("object", root.GetProperty("type").GetString());
        Assert.IsTrue(root.TryGetProperty("properties", out var properties));
        Assert.IsTrue(properties.TryGetProperty("items", out var items));
        Assert.IsTrue(properties.TryGetProperty("sep", out var sep));

        Assert.AreEqual("array", items.GetProperty("type").GetString());
        Assert.AreEqual("string", sep.GetProperty("type").GetString());

        var required = root.GetProperty("required");
        Assert.AreEqual(1, required.GetArrayLength());
        Assert.AreEqual("items", required[0].GetString());
    }

    [TestMethod]
    public void GetIndicatorTool_GetJsonSchema_ReturnsValidSchema()
    {
        // Arrange
        var tool = new GetIndicatorTool();

        // Act
        var schema = tool.GetJsonSchema();
        var json = JsonSerializer.Serialize(schema);
        var root = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.IsNotNull(schema);

        Assert.AreEqual("object", root.GetProperty("type").GetString());
        Assert.IsTrue(root.TryGetProperty("properties", out var properties));
        Assert.IsTrue(properties.TryGetProperty("symbol", out var symbol));
        Assert.IsTrue(properties.TryGetProperty("indicator", out var indicator));
        Assert.IsTrue(properties.TryGetProperty("period", out var period));

        // Handle union types for nullable strings
        var symbolType = symbol.GetProperty("type");
        var indicatorType = indicator.GetProperty("type");
        var periodType = period.GetProperty("type");

        // Check if they are union types (arrays) or simple types (strings)
        if (symbolType.ValueKind == JsonValueKind.Array)
        {
            var symbolTypes = symbolType.EnumerateArray().ToArray();
            Assert.IsTrue(symbolTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", symbolType.GetString());
        }

        if (indicatorType.ValueKind == JsonValueKind.Array)
        {
            var indicatorTypes = indicatorType.EnumerateArray().ToArray();
            Assert.IsTrue(indicatorTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", indicatorType.GetString());
        }

        Assert.AreEqual("integer", periodType.GetString());

        var required = root.GetProperty("required");
        Assert.AreEqual(3, required.GetArrayLength());
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "symbol"));
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "indicator"));
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "period"));
    }

    [TestMethod]
    public void StatefulAgent_NormalizeFunctionCallToReact_WithAssistantContent()
    {
        // Arrange
        var agent = new AIAgentSharp(new DelegateLlmClient((_, _) => Task.FromResult("")), new MemoryAgentStateStore());
        var functionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "get_indicator",
            FunctionArgumentsJson = "{\"symbol\":\"MNQ\",\"indicator\":\"RSI\",\"period\":14}",
            AssistantContent = "I need to check the RSI value for MNQ to assess market conditions."
        };

        // Act
        var modelMsg = agent.NormalizeFunctionCallToReact(functionResult, 0);

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
    public void StatefulAgent_NormalizeFunctionCallToReact_WithoutAssistantContent()
    {
        // Arrange
        var agent = new AIAgentSharp(new DelegateLlmClient((_, _) => Task.FromResult("")), new MemoryAgentStateStore());
        var functionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "concat",
            FunctionArgumentsJson = "{\"items\":[\"hello\",\"world\"]}",
            AssistantContent = ""
        };

        // Act
        var modelMsg = agent.NormalizeFunctionCallToReact(functionResult, 0);

        // Assert
        Assert.AreEqual("Calling concat to advance the plan.", modelMsg.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, modelMsg.Action);
        Assert.AreEqual("concat", modelMsg.ActionInput.Tool);
        Assert.IsNotNull(modelMsg.ActionInput.Params);
        Assert.IsTrue(modelMsg.ActionInput.Params.ContainsKey("items"));
    }

    [TestMethod]
    public void StatefulAgent_NormalizeFunctionCallToReact_InvalidArguments()
    {
        // Arrange
        var agent = new AIAgentSharp(new DelegateLlmClient((_, _) => Task.FromResult("")), new MemoryAgentStateStore());
        var functionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "get_indicator",
            FunctionArgumentsJson = "invalid json",
            AssistantContent = "Testing"
        };

        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() =>
            agent.NormalizeFunctionCallToReact(functionResult, 0));
        Assert.IsTrue(ex.Message.Contains("Failed to parse function arguments"));
    }

    [TestMethod]
    public void StatefulAgent_WithFunctionCallingDisabled_UsesJsonPath()
    {
        // Arrange
        var mockClient = new MockFunctionCallingLlmClient();
        var config = new AgentConfiguration { UseFunctionCalling = false };
        var agent = new AIAgentSharp(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new ConcatTool() };

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
        var agent = new AIAgentSharp(mockClient, new MemoryAgentStateStore(), config: config);
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
        var agent = new AIAgentSharp(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new ConcatTool() };

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
            FunctionArguments = "{\"items\":[\"hello\",\"world\"]}"
        };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new AIAgentSharp(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new ConcatTool() };

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
        var agent = new AIAgentSharp(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new ConcatTool() };

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
        var agent = new AIAgentSharp(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new ConcatTool() };

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
            FunctionName = "get_indicator",
            FunctionArguments = "{\"symbol\":\"MNQ\",\"indicator\":\"RSI\",\"period\":14}"
        };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new AIAgentSharp(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new GetIndicatorTool() };

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
            FunctionName = "get_indicator",
            FunctionArguments = "{\"symbol\":\"MNQ\",\"indicator\":\"RSI\",\"period\":14}",
            AssistantContent = "I need to check the RSI value for MNQ to assess market conditions."
        };
        var config = new AgentConfiguration { UseFunctionCalling = true };
        var agent = new AIAgentSharp(mockClient, new MemoryAgentStateStore(), config: config);
        var tools = new List<ITool> { new GetIndicatorTool() };

        // Act
        var result = agent.StepAsync("test", "Test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual("I need to check the RSI value for MNQ to assess market conditions.", result.LlmMessage.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.LlmMessage.Action);
        Assert.IsNotNull(result.LlmMessage.ActionInput);
        Assert.AreEqual("get_indicator", result.LlmMessage.ActionInput.Tool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsTrue(result.ToolResult.Success);
        Assert.AreEqual("get_indicator", result.ToolResult.Tool);
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