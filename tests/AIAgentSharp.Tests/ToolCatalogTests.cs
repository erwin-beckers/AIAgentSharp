using System.Text.Json;

namespace AIAgentSharp.Tests;

[TestClass]
public class ToolCatalogTests
{
    [TestMethod]
    public void ConcatTool_ImplementsIToolIntrospect()
    {
        // Arrange & Act
        var tool = new ConcatTool();

        // Assert
        Assert.IsTrue(tool is IToolIntrospect);
        Assert.AreEqual("concat", tool.Name);
    }

    [TestMethod]
    public void ConcatTool_Describe_ReturnsValidJson()
    {
        // Arrange
        var tool = new ConcatTool();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsNotNull(description);
        Assert.IsTrue(description.StartsWith("{") && description.EndsWith("}"));

        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(description);
        var root = jsonDoc.RootElement;

        Assert.AreEqual("concat", root.GetProperty("name").GetString());
        Assert.AreEqual("Concatenate strings with a separator.", root.GetProperty("description").GetString());

        var paramsObj = root.GetProperty("params");
        var properties = paramsObj.GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("items", out var items));
        Assert.IsTrue(properties.TryGetProperty("sep", out var sep));

        Assert.AreEqual("array", items.GetProperty("type").GetString());
        Assert.AreEqual("string", items.GetProperty("items").GetProperty("type").GetString());

        Assert.AreEqual("string", sep.GetProperty("type").GetString());
    }

    [TestMethod]
    public void GetIndicatorTool_ImplementsIToolIntrospect()
    {
        // Arrange & Act
        var tool = new GetIndicatorTool();

        // Assert
        Assert.IsTrue(tool is IToolIntrospect);
        Assert.AreEqual("get_indicator", tool.Name);
    }

    [TestMethod]
    public void GetIndicatorTool_Describe_ReturnsValidJson()
    {
        // Arrange
        var tool = new GetIndicatorTool();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsNotNull(description);
        Assert.IsTrue(description.StartsWith("{") && description.EndsWith("}"));

        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(description);
        var root = jsonDoc.RootElement;

        Assert.AreEqual("get_indicator", root.GetProperty("name").GetString());
        Assert.AreEqual("Fetch a single indicator value for a symbol.", root.GetProperty("description").GetString());

        var paramsObj = root.GetProperty("params");

        // The params object is a JSON schema, so properties are nested
        var properties = paramsObj.GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("symbol", out var symbol));
        Assert.IsTrue(properties.TryGetProperty("indicator", out var indicator));
        Assert.IsTrue(properties.TryGetProperty("period", out var period));

        // Verify symbol parameter (now a union type)
        var symbolType = symbol.GetProperty("type");

        if (symbolType.ValueKind == JsonValueKind.Array)
        {
            var symbolTypes = symbolType.EnumerateArray().ToArray();
            Assert.IsTrue(symbolTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", symbolType.GetString());
        }

        // Verify indicator parameter (now a union type)
        var indicatorType = indicator.GetProperty("type");

        if (indicatorType.ValueKind == JsonValueKind.Array)
        {
            var indicatorTypes = indicatorType.EnumerateArray().ToArray();
            Assert.IsTrue(indicatorTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", indicatorType.GetString());
        }

        // Verify period parameter
        Assert.AreEqual("integer", period.GetProperty("type").GetString());
        Assert.AreEqual(1, period.GetProperty("minimum").GetInt32());

        // Verify required fields
        var required = paramsObj.GetProperty("required");
        Assert.AreEqual(3, required.GetArrayLength());
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "symbol"));
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "indicator"));
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "period"));
    }

    [TestMethod]
    public void GetIndicatorTool_Describe_DoesNotIncludeReturnsSection()
    {
        // Arrange
        var tool = new GetIndicatorTool();

        // Act
        var description = tool.Describe();

        // Assert
        var jsonDoc = JsonDocument.Parse(description);
        var root = jsonDoc.RootElement;

        // The current implementation doesn't include a returns section
        Assert.IsFalse(root.TryGetProperty("returns", out _));
    }

    [TestMethod]
    public void ToolCatalog_BuildMessages_IncludesToolCatalog()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        var tools = new Dictionary<string, ITool>
        {
            ["concat"] = new ConcatTool(),
            ["get_indicator"] = new GetIndicatorTool()
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());

        // Assert
        Assert.AreEqual(2, messages.Count());
        var userMessage = messages.Last();
        Assert.AreEqual("user", userMessage.Role);

        var content = userMessage.Content;
        Assert.IsTrue(content.Contains("TOOL CATALOG"));
        Assert.IsTrue(content.Contains("concat:"));
        Assert.IsTrue(content.Contains("get_indicator:"));
        Assert.IsTrue(content.Contains("GOAL:"));
        Assert.IsTrue(content.Contains("HISTORY"));
    }

    [TestMethod]
    public void ToolCatalog_BuildMessages_IncludesToolDescriptions()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        var tools = new Dictionary<string, ITool>
        {
            ["concat"] = new ConcatTool(),
            ["get_indicator"] = new GetIndicatorTool()
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("concat: {\"name\":\"concat\""));
        Assert.IsTrue(content.Contains("get_indicator: {\"name\":\"get_indicator\""));
        Assert.IsTrue(content.Contains("\"description\":\"Concatenate strings with a separator.\""));
        Assert.IsTrue(content.Contains("\"description\":\"Fetch a single indicator value for a symbol.\""));
    }

    [TestMethod]
    public void ToolCatalog_BuildMessages_HandlesNonIntrospectTools()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        var nonIntrospectTool = new NonIntrospectTool();
        var tools = new Dictionary<string, ITool>
        {
            ["basic_tool"] = nonIntrospectTool
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("basic_tool: {\"params\":{}}"));
    }

    [TestMethod]
    public void ToolCatalog_BuildMessages_IncludesAllSections()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        var tools = new Dictionary<string, ITool>
        {
            ["concat"] = new ConcatTool()
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());
        var content = messages.Last().Content;

        // Assert
        var lines = content.Split('\n');
        var lineList = lines.ToList();

        Assert.IsTrue(lineList.Any(l => l.Contains("GOAL, TOOL CATALOG, and HISTORY")));
        Assert.IsTrue(lineList.Any(l => l.Contains("GOAL:")));
        Assert.IsTrue(lineList.Any(l => l.Contains("TOOL CATALOG")));
        Assert.IsTrue(lineList.Any(l => l.Contains("HISTORY")));
        Assert.IsTrue(lineList.Any(l => l.Contains("IMPORTANT: Reply with JSON only")));
    }

    [TestMethod]
    public void ToolCatalog_BuildMessages_WithHistory()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new()
                {
                    Index = 0,
                    LlmMessage = new ModelMessage
                    {
                        Thoughts = "Test thought",
                        Action = AgentAction.Plan,
                        ActionInput = new ActionInput { Summary = "Test summary" }
                    }
                }
            }
        };

        var tools = new Dictionary<string, ITool>
        {
            ["concat"] = new ConcatTool()
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("LLM:"));
        Assert.IsTrue(content.Contains("Test thought"));
        Assert.IsTrue(content.Contains("plan"));
    }

    [TestMethod]
    public void ToolCatalog_JsonSchema_IsValid()
    {
        // Arrange
        var tools = new ITool[] { new ConcatTool(), new GetIndicatorTool() };

        // Act & Assert
        foreach (var tool in tools)
        {
            if (tool is IToolIntrospect introspect)
            {
                var description = introspect.Describe();

                // Verify it's valid JSON
                var jsonDoc = JsonDocument.Parse(description);
                var root = jsonDoc.RootElement;

                // Verify required fields
                Assert.IsTrue(root.TryGetProperty("name", out _));
                Assert.IsTrue(root.TryGetProperty("description", out _));
                Assert.IsTrue(root.TryGetProperty("params", out _));

                // Verify params is an object
                var paramsObj = root.GetProperty("params");
                Assert.AreEqual(JsonValueKind.Object, paramsObj.ValueKind);
            }
        }
    }

    [TestMethod]
    public void ToolCatalog_IndicatorType_IsString()
    {
        // Arrange
        var tool = new GetIndicatorTool();

        // Act
        var description = tool.Describe();
        var jsonDoc = JsonDocument.Parse(description);
        var indicatorParam = jsonDoc.RootElement.GetProperty("params").GetProperty("properties").GetProperty("indicator");

        // Assert - handle union type for nullable string
        var indicatorType = indicatorParam.GetProperty("type");

        if (indicatorType.ValueKind == JsonValueKind.Array)
        {
            var indicatorTypes = indicatorType.EnumerateArray().ToArray();
            Assert.IsTrue(indicatorTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", indicatorType.GetString());
        }

        // Check that indicator is in the required array
        var requiredArray = jsonDoc.RootElement.GetProperty("params").GetProperty("required");
        var requiredFields = requiredArray.EnumerateArray().Select(v => v.GetString()).ToArray();
        Assert.IsTrue(requiredFields.Contains("indicator"));
    }

    [TestMethod]
    public void ToolCatalog_RequiredFields_AreCorrect()
    {
        // Arrange
        var concatTool = new ConcatTool();
        var indicatorTool = new GetIndicatorTool();

        // Act
        var concatDesc = JsonDocument.Parse(concatTool.Describe());
        var indicatorDesc = JsonDocument.Parse(indicatorTool.Describe());

        // Assert - ConcatTool (uses required array at top level)
        var concatParams = concatDesc.RootElement.GetProperty("params");
        var concatRequiredArray = concatParams.GetProperty("required");
        var concatRequiredFields = concatRequiredArray.EnumerateArray().Select(v => v.GetString()).ToArray();

        Assert.IsTrue(concatRequiredFields.Contains("items"));
        Assert.IsFalse(concatRequiredFields.Contains("sep"));

        // Assert - GetIndicatorTool (uses required array at top level)
        var indicatorParams = indicatorDesc.RootElement.GetProperty("params");
        var requiredArray = indicatorParams.GetProperty("required");
        var requiredFields = requiredArray.EnumerateArray().Select(v => v.GetString()).ToArray();

        Assert.IsTrue(requiredFields.Contains("symbol"));
        Assert.IsTrue(requiredFields.Contains("indicator"));
        Assert.IsTrue(requiredFields.Contains("period"));
    }

    [TestMethod]
    public void ToolCatalog_BuildMessages_EmptyToolsList()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        var tools = new Dictionary<string, ITool>();

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("TOOL CATALOG"));
        Assert.IsTrue(content.Contains("HISTORY"));
        // Should not contain any tool descriptions
        Assert.IsFalse(content.Contains("concat:"));
        Assert.IsFalse(content.Contains("get_indicator:"));
    }

    [TestMethod]
    public void ToolCatalog_BuildMessages_MixedToolTypes()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        var tools = new Dictionary<string, ITool>
        {
            ["concat"] = new ConcatTool(),
            ["basic_tool"] = new NonIntrospectTool()
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("concat: {\"name\":\"concat\""));
        Assert.IsTrue(content.Contains("basic_tool: {\"params\":{}}"));
    }

    [TestMethod]
    public void ToolCatalog_BuildMessages_WithToolCallsInHistory()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new()
                {
                    Index = 0,
                    ToolCall = new ToolCallRequest
                    {
                        Tool = "get_indicator",
                        Params = new Dictionary<string, object?>
                        {
                            ["symbol"] = "MNQ",
                            ["indicator"] = "RSI",
                            ["period"] = 14
                        }
                    },
                    ToolResult = new ToolExecutionResult
                    {
                        Success = true,
                        Output = new { value = 65.5 }
                    }
                }
            }
        };

        var tools = new Dictionary<string, ITool>
        {
            ["get_indicator"] = new GetIndicatorTool()
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("TOOL_CALL:"));
        Assert.IsTrue(content.Contains("TOOL_RESULT:"));
        Assert.IsTrue(content.Contains("get_indicator"));
        Assert.IsTrue(content.Contains("MNQ"));
        Assert.IsTrue(content.Contains("RSI"));
    }

    [TestMethod]
    public void ToolCatalog_JsonSchema_ConsistentFormat()
    {
        // Arrange
        var tools = new ITool[] { new ConcatTool(), new GetIndicatorTool() };

        // Act & Assert
        foreach (var tool in tools)
        {
            if (tool is IToolIntrospect introspect)
            {
                var description = introspect.Describe();
                var jsonDoc = JsonDocument.Parse(description);
                var root = jsonDoc.RootElement;

                // Verify consistent structure
                Assert.IsTrue(root.TryGetProperty("name", out var name));
                Assert.IsTrue(root.TryGetProperty("description", out var desc));
                Assert.IsTrue(root.TryGetProperty("params", out var paramsObj));

                // Verify name is string
                Assert.AreEqual(JsonValueKind.String, name.ValueKind);

                // Verify description is string
                Assert.AreEqual(JsonValueKind.String, desc.ValueKind);

                // Verify params is object
                Assert.AreEqual(JsonValueKind.Object, paramsObj.ValueKind);

                // Verify description is not empty
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc.GetString()));
            }
        }
    }

    [TestMethod]
    public void ToolCatalog_ParameterConstraints_AreValid()
    {
        // Arrange
        var tool = new GetIndicatorTool();

        // Act
        var description = tool.Describe();
        var jsonDoc = JsonDocument.Parse(description);
        var periodParam = jsonDoc.RootElement.GetProperty("params").GetProperty("properties").GetProperty("period");

        // Assert
        Assert.IsTrue(periodParam.TryGetProperty("minimum", out var minimum));
        Assert.AreEqual(1, minimum.GetInt32());
        Assert.IsTrue(minimum.GetInt32() > 0);
    }

    [TestMethod]
    public void ToolCatalog_ReturnsSection_IsOptional()
    {
        // Arrange
        var concatTool = new ConcatTool();

        // Act
        var description = concatTool.Describe();
        var jsonDoc = JsonDocument.Parse(description);
        var root = jsonDoc.RootElement;

        // Assert - ConcatTool does not have returns section (it's optional)
        Assert.IsFalse(root.TryGetProperty("returns", out var returns));
    }

    [TestMethod]
    public void ToolCatalog_SystemMessage_IsIncluded()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        var tools = new Dictionary<string, ITool>
        {
            ["concat"] = new ConcatTool()
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration());

        // Assert
        Assert.AreEqual(2, messages.Count());
        var systemMessage = messages.First();
        Assert.AreEqual("system", systemMessage.Role);
        Assert.IsTrue(systemMessage.Content.Contains("MODEL OUTPUT CONTRACT"));
    }

    // Helper class for testing non-introspect tools
    private class NonIntrospectTool : ITool
    {
        public string Name => "basic_tool";

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            return Task.FromResult<object?>(new { result = "basic" });
        }
    }
}