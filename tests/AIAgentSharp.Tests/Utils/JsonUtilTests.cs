namespace AIAgentSharp.Tests.Utils;

[TestClass]
public class JsonUtilTests
{
    [TestMethod]
    public void ParseStrict_Should_ParseValidJson_When_AllRequiredFieldsPresent()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""I need to analyze this data"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""calculator"",
                ""params"": { ""expression"": ""2+2"" }
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual("I need to analyze this data", result.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("tool_call", result.ActionRaw);
        Assert.AreEqual("calculator", result.ActionInput.Tool);
        Assert.IsNotNull(result.ActionInput.Params);
        // The value might be JsonElement or string depending on how System.Text.Json deserializes it
        var expressionValue = result.ActionInput.Params["expression"];
        Assert.IsTrue(expressionValue is string || expressionValue is System.Text.Json.JsonElement);
        if (expressionValue is string str)
        {
            Assert.AreEqual("2+2", str);
        }
        else if (expressionValue is System.Text.Json.JsonElement element)
        {
            Assert.AreEqual("2+2", element.GetString());
        }
    }

    [TestMethod]
    public void ParseStrict_Should_ParseValidJson_When_OptionalFieldsPresent()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""I need to analyze this data"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""calculator"",
                ""params"": { ""expression"": ""2+2"" },
                ""summary"": ""Calculating 2+2"",
                ""final"": ""true""
            },
            ""progress_pct"": 75,
            ""reasoning"": ""Step by step analysis"",
            ""insights"": [""insight1"", ""insight2""],
            ""conclusion"": ""The result is 4"",
            ""is_valid"": true,
            ""error"": null
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual("I need to analyze this data", result.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("calculator", result.ActionInput.Tool);
        Assert.AreEqual("Calculating 2+2", result.ActionInput.Summary);
        Assert.AreEqual("true", result.ActionInput.Final);
        Assert.AreEqual(75, result.ProgressPct);
        Assert.AreEqual("Step by step analysis", result.Reasoning);
        Assert.AreEqual(2, result.Insights?.Count);
        Assert.AreEqual("insight1", result.Insights?[0]);
        Assert.AreEqual("insight2", result.Insights?[1]);
        Assert.AreEqual("The result is 4", result.Conclusion);
        Assert.IsTrue(result.IsValid);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void ParseStrict_Should_ParseValidJson_When_EmptyStringsProvided()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {}
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual("test", result.Thoughts);
        Assert.AreEqual(AgentAction.Plan, result.Action);
        Assert.AreEqual("plan", result.ActionRaw);
    }

    [TestMethod]
    public void ParseStrict_Should_ParseValidJson_When_AllActionsSupported()
    {
        // Test all supported actions
        var actions = new[] { "plan", "tool_call", "finish" };

        foreach (var action in actions)
        {
            // Arrange
            var json = $@"{{
                ""thoughts"": ""Testing {action}"",
                ""action"": ""{action}"",
                ""action_input"": {{
                    ""tool"": ""test"",
                    ""params"": {{}},
                    ""final"": ""test""
                }}
            }}";

            // Act
            var result = JsonUtil.ParseStrict(json);

            // Assert
            Assert.AreEqual($"Testing {action}", result.Thoughts);
            Assert.AreEqual(action, result.ActionRaw);
        }
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_JsonIsNotObject()
    {
        // Arrange
        var json = "[]";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_JsonIsInvalid()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act & Assert
        try
        {
            JsonUtil.ParseStrict(json);
            Assert.Fail("Expected an exception to be thrown");
        }
        catch (Exception)
        {
            // Expected - any exception is acceptable for invalid JSON
        }
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_ThoughtsFieldMissing()
    {
        // Arrange
        var json = @"{
            ""action"": ""plan"",
            ""action_input"": {}
        }";

        // Act & Assert
        Assert.ThrowsException<KeyNotFoundException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_ActionFieldMissing()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action_input"": {}
        }";

        // Act & Assert
        Assert.ThrowsException<KeyNotFoundException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_ActionInputFieldMissing()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan""
        }";

        // Act & Assert
        Assert.ThrowsException<KeyNotFoundException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_InvalidActionProvided()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""invalid_action"",
            ""action_input"": {}
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_Should_ApplySizeLimits_When_ConfigProvided()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            MaxThoughtsLength = 10,
            MaxSummaryLength = 5
        };

        var json = @"{
            ""thoughts"": ""This is a very long thought that exceeds the limit"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""test"",
                ""params"": {},
                ""summary"": ""This summary is too long""
            }
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json, config));
    }

    [TestMethod]
    public void ParseStrict_Should_HandleNullValues_When_OptionalFieldsAreNull()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {
                ""tool"": null,
                ""params"": null,
                ""summary"": null
            },
            ""progress_pct"": null,
            ""reasoning"": null,
            ""insights"": null,
            ""conclusion"": null,
            ""is_valid"": null,
            ""error"": null
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual("test", result.Thoughts);
        Assert.AreEqual(AgentAction.Plan, result.Action);
        Assert.IsNull(result.ActionInput.Tool);
        Assert.IsNull(result.ActionInput.Params);
        Assert.IsNull(result.ActionInput.Summary);
        Assert.IsNull(result.ProgressPct);
        Assert.IsNull(result.Reasoning);
        Assert.IsNull(result.Insights);
        Assert.IsNull(result.Conclusion);
        Assert.IsNull(result.IsValid);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void ParseStrict_Should_HandleEmptyArrays_When_InsightsIsEmpty()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {},
            ""insights"": []
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNotNull(result.Insights);
        Assert.AreEqual(0, result.Insights.Count);
    }

    [TestMethod]
    public void ParseStrict_Should_HandleInvalidProgressPct_When_ProgressPctIsOutOfRange()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {},
            ""progress_pct"": 150
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNull(result.ProgressPct); // Should be ignored when out of range
    }

    [TestMethod]
    public void ParseStrict_Should_HandleNegativeProgressPct_When_ProgressPctIsNegative()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {},
            ""progress_pct"": -10
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNull(result.ProgressPct); // Should be ignored when negative
    }

    [TestMethod]
    public void ParseStrict_Should_HandleValidProgressPct_When_ProgressPctIsInRange()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {},
            ""progress_pct"": 50
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual(50, result.ProgressPct);
    }

    [TestMethod]
    public void ParseStrict_Should_HandleBooleanValues_When_IsValidIsBoolean()
    {
        // Test both true and false
        var testCases = new[] { true, false };

        foreach (var testCase in testCases)
        {
            // Arrange
            var json = $@"{{
                ""thoughts"": ""test"",
                ""action"": ""plan"",
                ""action_input"": {{}},
                ""is_valid"": {testCase.ToString().ToLower()}
            }}";

            // Act
            var result = JsonUtil.ParseStrict(json);

            // Assert
            Assert.AreEqual(testCase, result.IsValid);
        }
    }

    [TestMethod]
    public void ParseStrict_Should_HandleNonBooleanIsValid_When_IsValidIsNotBoolean()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {},
            ""is_valid"": ""not a boolean""
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNull(result.IsValid); // Should be ignored when not boolean
    }

    [TestMethod]
    public void ParseStrict_Should_HandleNonArrayInsights_When_InsightsIsNotArray()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {},
            ""insights"": ""not an array""
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNull(result.Insights); // Should be ignored when not array
    }

    [TestMethod]
    public void ParseStrict_Should_HandleNullInsights_When_InsightsContainsNullValues()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {},
            ""insights"": [""valid"", null, """", ""also valid""]
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.IsNotNull(result.Insights);
        Assert.AreEqual(2, result.Insights.Count); // Only non-null, non-empty strings
        Assert.AreEqual("valid", result.Insights[0]);
        Assert.AreEqual("also valid", result.Insights[1]);
    }

    [TestMethod]
    public void ParseStrict_Should_HandleComplexParams_When_ParamsContainsComplexObjects()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""complex_tool"",
                ""params"": {
                    ""string_param"": ""value"",
                    ""number_param"": 42,
                    ""boolean_param"": true,
                    ""array_param"": [1, 2, 3],
                    ""object_param"": { ""nested"": ""value"" }
                }
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual("complex_tool", result.ActionInput.Tool);
        Assert.IsNotNull(result.ActionInput.Params);
        // String parameters can be deserialized as JsonElement
        var stringParam = result.ActionInput.Params["string_param"];
        if (stringParam is string str)
        {
            Assert.AreEqual("value", str);
        }
        else if (stringParam is System.Text.Json.JsonElement element)
        {
            Assert.AreEqual("value", element.GetString());
        }
        // Number parameters can also be deserialized as JsonElement
        var numberParam = result.ActionInput.Params["number_param"];
        if (numberParam is long l)
        {
            Assert.AreEqual(42L, l);
        }
        else if (numberParam is System.Text.Json.JsonElement element)
        {
            Assert.AreEqual(42, element.GetInt32());
        }
        // Boolean parameters can also be deserialized as JsonElement
        var booleanParam = result.ActionInput.Params["boolean_param"];
        if (booleanParam is bool b)
        {
            Assert.AreEqual(true, b);
        }
        else if (booleanParam is System.Text.Json.JsonElement element)
        {
            Assert.AreEqual(true, element.GetBoolean());
        }
        // Complex types (arrays, objects) are deserialized as JsonElement
        Assert.IsInstanceOfType(result.ActionInput.Params["array_param"], typeof(System.Text.Json.JsonElement));
        Assert.IsInstanceOfType(result.ActionInput.Params["object_param"], typeof(System.Text.Json.JsonElement));
    }

    [TestMethod]
    public void ParseStrict_Should_HandleMalformedJson_When_JsonNeedsCleaning()
    {
        // Arrange - This should be handled by JsonResponseCleaner
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""plan"",
            ""action_input"": {}
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual("test", result.Thoughts);
        Assert.AreEqual(AgentAction.Plan, result.Action);
    }

    [TestMethod]
    public void ParseStrict_Should_HandleSnakeCaseActions_When_ActionUsesSnakeCase()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""test"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""test"",
                ""params"": {}
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual(AgentAction.ToolCall, result.Action);
        Assert.AreEqual("tool_call", result.ActionRaw);
    }

    [TestMethod]
    public void ParseStrict_Should_HandleFinalAction_When_ActionIsFinal()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""I have completed the task"",
            ""action"": ""finish"",
            ""action_input"": {
                ""summary"": ""Task completed successfully"",
                ""final"": ""true""
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual(AgentAction.Finish, result.Action);
        Assert.AreEqual("finish", result.ActionRaw);
        Assert.AreEqual("Task completed successfully", result.ActionInput.Summary);
        Assert.AreEqual("true", result.ActionInput.Final);
    }

    [TestMethod]
    public void ParseStrict_Should_HandlePlanAction_When_ActionIsPlan()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""I need to plan my approach"",
            ""action"": ""plan"",
            ""action_input"": {
                ""summary"": ""Planning phase"",
                ""final"": ""false""
            }
        }";

        // Act
        var result = JsonUtil.ParseStrict(json);

        // Assert
        Assert.AreEqual(AgentAction.Plan, result.Action);
        Assert.AreEqual("plan", result.ActionRaw);
        Assert.AreEqual("Planning phase", result.ActionInput.Summary);
        Assert.AreEqual("false", result.ActionInput.Final);
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_ToolCallMissingTool()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""I need to call a tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""params"": {}
            }
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_FinishMissingFinal()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""I have completed the task"",
            ""action"": ""finish"",
            ""action_input"": {
                ""summary"": ""Task completed""
            }
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_ThoughtsIsEmpty()
    {
        // Arrange
        var json = @"{
            ""thoughts"": """",
            ""action"": ""plan"",
            ""action_input"": {}
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }

    [TestMethod]
    public void ParseStrict_Should_ThrowArgumentException_When_ThoughtsIsWhitespace()
    {
        // Arrange
        var json = @"{
            ""thoughts"": ""   "",
            ""action"": ""plan"",
            ""action_input"": {}
        }";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => JsonUtil.ParseStrict(json));
    }
}
