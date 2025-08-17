using AIAgentSharp.Agents;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class StatusUpdateTests
{
    private Agent _agent = null!;
    private MockLlmClient _llm = null!;
    private TestLogger _logger = null!;
    private List<AgentStatusEventArgs> _statusEvents = null!;
    private MemoryAgentStateStore _store = null!;

    [TestInitialize]
    public void Setup()
    {
        _logger = new TestLogger();
        _store = new MemoryAgentStateStore();
        _llm = new MockLlmClient();
        _statusEvents = new List<AgentStatusEventArgs>();
    }

    [TestMethod]
    public void StatusUpdate_ConfigDisabled_NoEmissions()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = false };
        _agent = new Agent(_llm, _store, _logger, config);
        _agent.StatusUpdate += (sender, e) => _statusEvents.Add(e);

        // Act
        _llm.SetResponse(@"{""thoughts"":""test"",""action"":""finish"",""action_input"":{""final"":""done""}}");
        var result = _agent.StepAsync("test-agent", "test goal", new ITool[0]).Result;

        // Assert
        Assert.AreEqual(0, _statusEvents.Count, "No status events should be emitted when config is disabled");
    }

    [TestMethod]
    public void StatusUpdate_LifecycleStatuses_Emitted()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        _agent = new Agent(_llm, _store, _logger, config);
        _agent.StatusUpdate += (sender, e) => _statusEvents.Add(e);

        // Act
        _llm.SetResponse(@"{""thoughts"":""test"",""action"":""finish"",""action_input"":{""final"":""done""}}");
        var result = _agent.StepAsync("test-agent", "test goal", new ITool[0]).Result;

        // Assert
        Assert.IsTrue(_statusEvents.Count > 0, "Should emit lifecycle statuses");
        Assert.IsTrue(_statusEvents.Any(e => e.StatusTitle == "Analyzing task"), "Should emit analysis status");
        Assert.IsTrue(_statusEvents.Any(e => e.StatusTitle == "Finalizing"), "Should emit finalization status");
    }

    [TestMethod]
    public void StatusUpdate_LlmPublicFields_UsedWhenPresent()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        _agent = new Agent(_llm, _store, _logger, config);
        _agent.StatusUpdate += (sender, e) => _statusEvents.Add(e);

        var response = @"{
            ""thoughts"":""test"",
            ""action"":""finish"",
            ""action_input"":{""final"":""done""},
            ""status_title"":""Custom Status"",
            ""status_details"":""Custom details here"",
            ""next_step_hint"":""Will complete task"",
            ""progress_pct"":75
        }";

        // Act
        _llm.SetResponse(response);
        var result = _agent.StepAsync("test-agent", "test goal", new ITool[0]).Result;

        // Assert
        var customStatus = _statusEvents.FirstOrDefault(e => e.StatusTitle == "Custom Status");
        Assert.IsNotNull(customStatus, "Should emit LLM-provided status");
        Assert.AreEqual("Custom details here", customStatus.StatusDetails);
        Assert.AreEqual("Will complete task", customStatus.NextStepHint);
        Assert.AreEqual(75, customStatus.ProgressPct);
    }

    [TestMethod]
    public void StatusUpdate_LlmPublicFields_TruncatedWhenOverLimit()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        _agent = new Agent(_llm, _store, _logger, config);
        _agent.StatusUpdate += (sender, e) => _statusEvents.Add(e);

        var longTitle = new string('x', 100); // Over 60 char limit
        var longDetails = new string('y', 200); // Over 160 char limit
        var longHint = new string('z', 100); // Over 60 char limit

        var response = $@"{{
            ""thoughts"":""test"",
            ""action"":""finish"",
            ""action_input"":{{""final"":""done""}},
            ""status_title"":""{longTitle}"",
            ""status_details"":""{longDetails}"",
            ""next_step_hint"":""{longHint}"",
            ""progress_pct"":150
        }}";

        // Act
        _llm.SetResponse(response);
        var result = _agent.StepAsync("test-agent", "test goal", new ITool[0]).Result;

        // Assert
        var customStatus = _statusEvents.FirstOrDefault(e => e.StatusTitle.StartsWith("xxxxx"));
        Assert.IsNotNull(customStatus, "Should emit truncated status");
        Assert.AreEqual(60, customStatus.StatusTitle.Length, "Title should be truncated to 60 chars");
        Assert.AreEqual(160, customStatus.StatusDetails?.Length, "Details should be truncated to 160 chars");
        Assert.AreEqual(60, customStatus.NextStepHint?.Length, "Hint should be truncated to 60 chars");
        Assert.IsNull(customStatus.ProgressPct, "Invalid progress should be ignored");
    }

    [TestMethod]
    public void StatusUpdate_Fallback_WorksWhenLlmFieldsAbsent()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        _agent = new Agent(_llm, _store, _logger, config);
        _agent.StatusUpdate += (sender, e) => _statusEvents.Add(e);

        // Act - LLM response without status fields
        _llm.SetResponse(@"{""thoughts"":""test"",""action"":""finish"",""action_input"":{""final"":""done""}}");
        var result = _agent.StepAsync("test-agent", "test goal", new ITool[0]).Result;

        // Assert
        Assert.IsTrue(_statusEvents.Count > 0, "Should emit fallback statuses");
        Assert.IsTrue(_statusEvents.All(e => !string.IsNullOrEmpty(e.StatusTitle)), "All statuses should have titles");
        Assert.IsTrue(_statusEvents.Any(e => e.StatusTitle == "Analyzing task"), "Should emit engine-synthesized statuses");
    }

    [TestMethod]
    public void StatusUpdate_ValidationError_EmitsErrorStatus()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        _agent = new Agent(_llm, _store, _logger, config);
        _agent.StatusUpdate += (sender, e) => _statusEvents.Add(e);

        var tool = new RequiredParamTool();
        var tools = new[] { tool };

        // Act - LLM response with invalid tool call (missing required parameter)
        _llm.SetResponse(@"{""thoughts"":""test"",""action"":""tool_call"",""action_input"":{""tool"":""required_param"",""params"":{}}}");
        var result = _agent.StepAsync("test-agent", "test goal", tools).Result;

        // Assert
        var errorStatus = _statusEvents.FirstOrDefault(e => e.StatusTitle == "Tool execution failed");
        Assert.IsNotNull(errorStatus, "Should emit tool execution error status");
        Assert.IsTrue(errorStatus.StatusDetails?.Contains("required"), "Should include error details");
    }

    [TestMethod]
    public void StatusUpdate_NoRegressions_ExistingBehaviorPreserved()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        _agent = new Agent(_llm, _store, _logger, config);
        _agent.StatusUpdate += (sender, e) => _statusEvents.Add(e);

        var tool = new MockConcatTool();
        var tools = new[] { tool };

        // Act - Valid tool call
        _llm.SetResponse(@"{""thoughts"":""test"",""action"":""tool_call"",""action_input"":{""tool"":""concat"",""params"":{""strings"":[""hello"",""world""]}}}");
        var result = _agent.StepAsync("test-agent", "test goal", tools).Result;

        // Assert
        Assert.IsTrue(result.ExecutedTool, "Tool should still be executed");
        Assert.IsTrue(result.Continue, "Should continue execution");
        Assert.IsNotNull(result.ToolResult, "Should have tool result");
        Assert.IsTrue(result.ToolResult.Success, "Tool should succeed");
    }

    [TestMethod]
    public void StatusUpdate_CancellationSafety_NoExceptionsFromEventEmission()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        _agent = new Agent(_llm, _store, _logger, config);

        // Add event handler that throws exception
        _agent.StatusUpdate += (sender, e) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _llm.SetResponse(@"{""thoughts"":""test"",""action"":""finish"",""action_input"":{""final"":""done""}}");
        var result = _agent.StepAsync("test-agent", "test goal", new ITool[0]).Result;

        // Should complete successfully despite event handler exception
        Assert.IsFalse(result.Continue, "Should complete successfully");
        Assert.AreEqual("done", result.FinalOutput, "Should have final output");
    }

    [TestMethod]
    public void StatusUpdate_BuildMessages_IncludesStatusPromptWhenEnabled()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var state = new AgentState { AgentId = "test", Goal = "test goal" };
        var tools = new Dictionary<string, ITool>();

        // Act
        var messageBuilder = new MessageBuilder(config);
        var messages = messageBuilder.BuildMessages(state, tools).ToList();

        // Assert
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage, "Should have user message");
        Assert.IsTrue(userMessage.Content.Contains("STATUS UPDATES"), "Should include status update instructions");
        Assert.IsTrue(userMessage.Content.Contains("status_title"), "Should mention status_title field");
        Assert.IsTrue(userMessage.Content.Contains("public-only"), "Should mention public-only requirement");
    }

    [TestMethod]
    public void StatusUpdate_BuildMessages_ExcludesStatusPromptWhenDisabled()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = false };
        var state = new AgentState { AgentId = "test", Goal = "test goal" };
        var tools = new Dictionary<string, ITool>();

        // Act
        var messageBuilder = new MessageBuilder(config);
        var messages = messageBuilder.BuildMessages(state, tools).ToList();

        // Assert
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage, "Should have user message");
        Assert.IsFalse(userMessage.Content.Contains("STATUS UPDATES"), "Should not include status update instructions");
        Assert.IsFalse(userMessage.Content.Contains("status_title"), "Should not mention status_title field");
    }

    [TestMethod]
    public void StatusUpdate_JsonParsing_HandlesStatusFieldsCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var json = @"{
            ""thoughts"":""test"",
            ""action"":""finish"",
            ""action_input"":{""final"":""done""},
            ""status_title"":""Test Status"",
            ""status_details"":""Test details"",
            ""next_step_hint"":""Test hint"",
            ""progress_pct"":50
        }";

        // Act
        var modelMsg = JsonUtil.ParseStrict(json, config);

        // Assert
        Assert.AreEqual("Test Status", modelMsg.StatusTitle);
        Assert.AreEqual("Test details", modelMsg.StatusDetails);
        Assert.AreEqual("Test hint", modelMsg.NextStepHint);
        Assert.AreEqual(50, modelMsg.ProgressPct);
    }

    [TestMethod]
    public void StatusUpdate_JsonParsing_IgnoresInvalidProgressPct()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var json = @"{
            ""thoughts"":""test"",
            ""action"":""finish"",
            ""action_input"":{""final"":""done""},
            ""status_title"":""Test Status"",
            ""progress_pct"":150
        }";

        // Act
        var modelMsg = JsonUtil.ParseStrict(json, config);

        // Assert
        Assert.AreEqual("Test Status", modelMsg.StatusTitle);
        Assert.IsNull(modelMsg.ProgressPct, "Invalid progress should be ignored");
    }

    private class MockLlmClient : ILlmClient
    {
        private string _response = "";

        public Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
        {
            return Task.FromResult(_response);
        }

        public void SetResponse(string response)
        {
            _response = response;
        }
    }

    private class RequiredParamTool : ITool
    {
        public string Name => "required_param";

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            if (!parameters.ContainsKey("required_field"))
            {
                throw new ArgumentException("Missing required parameter: required_field");
            }

            return Task.FromResult<object?>("success");
        }
    }
}
