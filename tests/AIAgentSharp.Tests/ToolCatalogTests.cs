using System.Text.Json;
using AIAgentSharp.Agents;

namespace AIAgentSharp.Tests;

[TestClass]
public class ToolCatalogTests
{
    [TestMethod]
    public void MockConcatTool_ImplementsIToolIntrospect()
    {
        // Arrange & Act
        var tool = new MockConcatTool();

        // Assert
        Assert.IsTrue(tool is IToolIntrospect);
        Assert.AreEqual("concat", tool.Name);
    }

    [TestMethod]
    public void MockConcatTool_Describe_ReturnsValidJson()
    {
        // Arrange
        var tool = new MockConcatTool();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsNotNull(description);
        Assert.IsTrue(description.StartsWith("{") && description.EndsWith("}"));

        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(description);
        var root = jsonDoc.RootElement;

        Assert.AreEqual("concat", root.GetProperty("name").GetString());
        Assert.AreEqual("Concatenate multiple strings together", root.GetProperty("description").GetString());

        var paramsObj = root.GetProperty("params");
        var properties = paramsObj.GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("strings", out var strings));

        Assert.AreEqual("array", strings.GetProperty("type").GetString());
        var stringsItemsType = strings.GetProperty("items").GetProperty("type");
        if (stringsItemsType.ValueKind == JsonValueKind.Array)
        {
            var stringsItemsTypes = stringsItemsType.EnumerateArray().ToArray();
            Assert.IsTrue(stringsItemsTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", stringsItemsType.GetString());
        }
    }

    [TestMethod]
    public void MockValidationTool_ImplementsIToolIntrospect()
    {
        // Arrange & Act
        var tool = new MockValidationTool();

        // Assert
        Assert.IsTrue(tool is IToolIntrospect);
        Assert.AreEqual("validate_input", tool.Name);
    }

    [TestMethod]
    public void MockValidationTool_Describe_ReturnsValidJson()
    {
        // Arrange
        var tool = new MockValidationTool();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsNotNull(description);
        Assert.IsTrue(description.StartsWith("{") && description.EndsWith("}"));

        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(description);
        var root = jsonDoc.RootElement;

        Assert.AreEqual("validate_input", root.GetProperty("name").GetString());
        Assert.AreEqual("Validate input data with custom rules", root.GetProperty("description").GetString());

        var paramsObj = root.GetProperty("params");

        // The params object is a JSON schema, so properties are nested
        var properties = paramsObj.GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("input", out var input));
        Assert.IsTrue(properties.TryGetProperty("rules", out var rules));

        // Verify input parameter (handle union type)
        var inputType = input.GetProperty("type");
        if (inputType.ValueKind == JsonValueKind.Array)
        {
            var inputTypes = inputType.EnumerateArray().ToArray();
            Assert.IsTrue(inputTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", inputType.GetString());
        }

        // Verify rules parameter
        Assert.AreEqual("array", rules.GetProperty("type").GetString());
        var rulesItemsType = rules.GetProperty("items").GetProperty("type");
        if (rulesItemsType.ValueKind == JsonValueKind.Array)
        {
            var rulesItemsTypes = rulesItemsType.EnumerateArray().ToArray();
            Assert.IsTrue(rulesItemsTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", rulesItemsType.GetString());
        }

        // Verify required fields
        var required = paramsObj.GetProperty("required");
        Assert.AreEqual(2, required.GetArrayLength());
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "input"));
        Assert.IsTrue(required.EnumerateArray().Any(r => r.GetString() == "rules"));
    }

    [TestMethod]
    public void MockValidationTool_Describe_DoesNotIncludeReturnsSection()
    {
        // Arrange
        var tool = new MockValidationTool();

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
            ["concat"] = new MockConcatTool(),
            ["validate_input"] = new MockValidationTool()
        };

        // Act
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);

        // Assert
        Assert.AreEqual(2, messages.Count());
        var userMessage = messages.Last();
        Assert.AreEqual("user", userMessage.Role);

        var content = userMessage.Content;
        Assert.IsTrue(content.Contains("TOOL CATALOG"));
        Assert.IsTrue(content.Contains("concat:"));
        Assert.IsTrue(content.Contains("validate_input:"));
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
            ["concat"] = new MockConcatTool(),
            ["validate_input"] = new MockValidationTool()
        };

        // Act
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("concat: {\"name\":\"concat\""));
        Assert.IsTrue(content.Contains("validate_input: {\"name\":\"validate_input\""));
        Assert.IsTrue(content.Contains("\"description\":\"Concatenate multiple strings together\""));
        Assert.IsTrue(content.Contains("\"description\":\"Validate input data with custom rules\""));
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
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);
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
            ["concat"] = new MockConcatTool()
        };

        // Act
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);
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
            ["concat"] = new MockConcatTool()
        };

        // Act
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);
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
        var tools = new ITool[] { new MockConcatTool(), new MockValidationTool() };

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
    public void ToolCatalog_ValidationRulesType_IsArray()
    {
        // Arrange
        var tool = new MockValidationTool();

        // Act
        var description = tool.Describe();
        var jsonDoc = JsonDocument.Parse(description);
        var rulesParam = jsonDoc.RootElement.GetProperty("params").GetProperty("properties").GetProperty("rules");

        // Assert - rules should be an array of strings
        var rulesType = rulesParam.GetProperty("type");
        Assert.AreEqual("array", rulesType.GetString());

        var itemsType = rulesParam.GetProperty("items").GetProperty("type");
        if (itemsType.ValueKind == JsonValueKind.Array)
        {
            var itemsTypes = itemsType.EnumerateArray().ToArray();
            Assert.IsTrue(itemsTypes.Any(t => t.GetString() == "string"));
        }
        else
        {
            Assert.AreEqual("string", itemsType.GetString());
        }

        // Check that rules is in the required array
        var requiredArray = jsonDoc.RootElement.GetProperty("params").GetProperty("required");
        var requiredFields = requiredArray.EnumerateArray().Select(v => v.GetString()).ToArray();
        Assert.IsTrue(requiredFields.Contains("rules"));
    }

    [TestMethod]
    public void ToolCatalog_RequiredFields_AreCorrect()
    {
        // Arrange
        var concatTool = new MockConcatTool();
        var validationTool = new MockValidationTool();

        // Act
        var concatDesc = JsonDocument.Parse(concatTool.Describe());
        var validationDesc = JsonDocument.Parse(validationTool.Describe());

        // Assert - MockConcatTool (uses required array at top level)
        var concatParams = concatDesc.RootElement.GetProperty("params");
        var concatRequiredArray = concatParams.GetProperty("required");
        var concatRequiredFields = concatRequiredArray.EnumerateArray().Select(v => v.GetString()).ToArray();

        Assert.IsTrue(concatRequiredFields.Contains("strings"));
        Assert.IsFalse(concatRequiredFields.Contains("sep"));

        // Assert - MockValidationTool (uses required array at top level)
        var validationParams = validationDesc.RootElement.GetProperty("params");
        var requiredArray = validationParams.GetProperty("required");
        var requiredFields = requiredArray.EnumerateArray().Select(v => v.GetString()).ToArray();

        Assert.IsTrue(requiredFields.Contains("input"));
        Assert.IsTrue(requiredFields.Contains("rules"));
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
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("TOOL CATALOG"));
        Assert.IsTrue(content.Contains("HISTORY"));
        // Should not contain any tool descriptions
        Assert.IsFalse(content.Contains("concat:"));
        Assert.IsFalse(content.Contains("validate_input:"));
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
            ["concat"] = new MockConcatTool(),
            ["basic_tool"] = new NonIntrospectTool()
        };

        // Act
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);
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
                        Tool = "validate_input",
                        Params = new Dictionary<string, object?>
                        {
                            ["input"] = "test data",
                            ["rules"] = new[] { "rule1", "rule2" }
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
            ["validate_input"] = new MockValidationTool()
        };

        // Act
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);
        var content = messages.Last().Content;

        // Assert
        Assert.IsTrue(content.Contains("TOOL_CALL:"));
        Assert.IsTrue(content.Contains("TOOL_RESULT:"));
        Assert.IsTrue(content.Contains("validate_input"));
        Assert.IsTrue(content.Contains("test data"));
        Assert.IsTrue(content.Contains("rule1"));
    }

    [TestMethod]
    public void ToolCatalog_JsonSchema_ConsistentFormat()
    {
        // Arrange
        var tools = new ITool[] { new MockConcatTool(), new MockValidationTool() };

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
        var tool = new MockValidationTool();

        // Act
        var description = tool.Describe();
        var jsonDoc = JsonDocument.Parse(description);
        var inputParam = jsonDoc.RootElement.GetProperty("params").GetProperty("properties").GetProperty("input");

        // Assert
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
        Assert.IsTrue(inputParam.TryGetProperty("minLength", out var minLength));
        Assert.AreEqual(1, minLength.GetInt32());
        Assert.IsTrue(minLength.GetInt32() > 0);
    }

    [TestMethod]
    public void ToolCatalog_ReturnsSection_IsOptional()
    {
        // Arrange
        var concatTool = new MockConcatTool();

        // Act
        var description = concatTool.Describe();
        var jsonDoc = JsonDocument.Parse(description);
        var root = jsonDoc.RootElement;

        // Assert - MockConcatTool does not have returns section (it's optional)
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
            ["concat"] = new MockConcatTool()
        };

        // Act
        var messages = new MessageBuilder(new AgentConfiguration()).BuildMessages(state, tools);

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
