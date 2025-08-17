using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class JsonUtilTests
{
    [TestMethod]
    public void JsonOptions_Properties_AreSetCorrectly()
    {
        // Arrange & Act
        var options = JsonUtil.JsonOptions;

        // Assert
        Assert.IsNotNull(options);
        Assert.AreEqual(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.AreEqual(JsonCommentHandling.Skip, options.ReadCommentHandling);
        Assert.IsFalse(options.AllowTrailingCommas);
        Assert.IsFalse(options.WriteIndented);
        Assert.AreEqual(JsonIgnoreCondition.Never, options.DefaultIgnoreCondition);
    }

    [TestMethod]
    public void ParseStrict_ValidJson_ReturnsModelMessage()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": ""test thoughts"",
            ""action"": ""plan"",
            ""action_input"": {
                ""summary"": ""test summary""
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test thoughts", result.Thoughts);
        Assert.AreEqual("plan", result.ActionRaw);
        Assert.AreEqual(AgentAction.Plan, result.Action);
        Assert.IsNotNull(result.ActionInput);
        Assert.AreEqual("test summary", result.ActionInput.Summary);
    }

    [TestMethod]
    public void ParseStrict_ToolCallAction_ReturnsCorrectModelMessage()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": ""I need to call a tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""summary"": ""Calling test tool"",
                ""tool"": ""test-tool"",
                ""params"": { ""key"": ""value"" }
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I need to call a tool", result.Thoughts);
        Assert.AreEqual("tool_call", result.ActionRaw);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("test-tool", result.ActionInput.Tool);
        var paramValue = result.ActionInput.Params?["key"];
        Assert.AreEqual("value", paramValue?.ToString());
    }

    [TestMethod]
    public void ParseStrict_FinishAction_ReturnsCorrectModelMessage()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": ""I am finished"",
            ""action"": ""finish"",
            ""action_input"": {
                ""summary"": ""Task completed"",
                ""final"": ""Final result""
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("I am finished", result.Thoughts);
        Assert.AreEqual("finish", result.ActionRaw);
        Assert.AreEqual(AgentAction.Finish, result.Action);
        Assert.AreEqual("Final result", result.ActionInput.Final);
    }

    [TestMethod]
    public void ParseStrict_NonObjectJson_ThrowsArgumentException()
    {
        // Arrange
        const string arrayJson = "[]";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(arrayJson));
    }

    [TestMethod]
    public void ParseStrict_ToolCallWithoutTool_ThrowsArgumentException()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": ""I need to call a tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""summary"": ""Calling test tool"",
                ""params"": { ""key"": ""value"" }
            }
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_FinishWithoutFinal_ThrowsArgumentException()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": ""I am finished"",
            ""action"": ""finish"",
            ""action_input"": {
                ""summary"": ""Task completed""
            }
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ToJson_ModelMessage_ReturnsValidJson()
    {
        // Arrange
        var modelMessage = new ModelMessage
        {
            Thoughts = "test thoughts",
            ActionRaw = "plan",
            ActionInput = new ActionInput
            {
                Summary = "test summary",
                Params = new Dictionary<string, object?> { { "key", "value" } }
            }
        };

        // Act
        var json = JsonUtil.ToJson(modelMessage);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("test thoughts"));
        Assert.IsTrue(json.Contains("plan"));
        Assert.IsTrue(json.Contains("test summary"));

        // Verify it can be parsed back
        var parsed = JsonUtil.ParseStrict(json);
        Assert.AreEqual("test thoughts", parsed.Thoughts);
        Assert.AreEqual("plan", parsed.ActionRaw);
    }

    [TestMethod]
    public void ToJson_AgentState_ReturnsValidJson()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "test goal",
            Turns = new List<AgentTurn>
            {
                new() { Index = 0, LlmMessage = new ModelMessage { Thoughts = "test", ActionRaw = "plan" } }
            },
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonUtil.ToJson(state);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("test-agent"));
        Assert.IsTrue(json.Contains("test goal"));

        // Verify it can be deserialized back
        var deserialized = JsonSerializer.Deserialize<AgentState>(json, JsonUtil.JsonOptions);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual("test-agent", deserialized.AgentId);
        Assert.AreEqual("test goal", deserialized.Goal);
    }

    [TestMethod]
    public void ToJson_ComplexObject_ReturnsValidJson()
    {
        // Arrange
        var complexObject = new
        {
            Name = "test",
            Values = new[] { 1, 2, 3 },
            Nested = new { Key = "value" }
        };

        // Act
        var json = JsonUtil.ToJson(complexObject);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("test"));
        Assert.IsTrue(json.Contains("1"));
        Assert.IsTrue(json.Contains("2"));
        Assert.IsTrue(json.Contains("3"));
        Assert.IsTrue(json.Contains("value"));
    }

    [TestMethod]
    public void ToJson_NullObject_ReturnsNullString()
    {
        // Arrange
        object? nullObject = null;

        // Act
        var json = JsonUtil.ToJson(nullObject!);

        // Assert
        Assert.AreEqual("null", json);
    }

    [TestMethod]
    public void ParseStrict_CaseInsensitiveAction_WorksCorrectly()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": ""test thoughts"",
            ""action"": ""PLAN"",
            ""action_input"": {
                ""summary"": ""test summary""
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("PLAN", result.ActionRaw);
        Assert.AreEqual(AgentAction.Plan, result.Action);
    }

    [TestMethod]
    public void ParseStrict_EmptyThoughts_ThrowsArgumentException()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": """",
            ""action"": ""plan"",
            ""action_input"": {
                ""summary"": ""test summary""
            }
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_WhitespaceThoughts_ThrowsArgumentException()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": ""   "",
            ""action"": ""plan"",
            ""action_input"": {
                ""summary"": ""test summary""
            }
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_EmptyAction_ThrowsArgumentException()
    {
        // Arrange
        const string json = @"{
            ""thoughts"": ""test thoughts"",
            ""action"": """",
            ""action_input"": {
                ""summary"": ""test summary""
            }
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }
}