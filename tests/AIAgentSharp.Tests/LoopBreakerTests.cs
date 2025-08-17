using System.Text.Json;
using AIAgentSharp.Agents;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class LoopBreakerTests
{
    private Agent _agent = null!;
    private MockLlmClient _llmClient = null!;
    private MemoryAgentStateStore _stateStore = null!;
    private Dictionary<string, ITool> _tools = null!;

    [TestInitialize]
    public void Setup()
    {
        _llmClient = new MockLlmClient();
        _stateStore = new MemoryAgentStateStore();
        var config = new AgentConfiguration { UseFunctionCalling = false };
        _agent = new Agent(_llmClient, _stateStore, config: config);
        _tools = new Dictionary<string, ITool>
        {
            { "validation_tool", new ValidationTestTool() }
        };
        
        Console.WriteLine($"Setup: MockLlmClient created: {_llmClient.GetHashCode()}");
        Console.WriteLine($"Setup: Agent created with LLM client: {_agent.GetHashCode()}");
    }

    [TestMethod]
    public void LoopBreaker_ThreeConsecutiveFailures_TriggersControllerTurn()
    {
        // Arrange - Set up LLM to make the same failing call 3 times
        var failingCall = @"{
            ""thoughts"": ""I need to call the validation tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""validation_tool"",
                ""params"": {
                    ""required_param"": """"
                }
            }
        }";

        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);

        // Act - Execute 3 steps with the same failing call
        var result1 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result2 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result3 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsNotNull(result3);
        Assert.IsTrue(result1.ExecutedTool);
        Assert.IsTrue(result2.ExecutedTool);
        Assert.IsTrue(result3.ExecutedTool);
        Assert.IsNotNull(result1.ToolResult);
        Assert.IsNotNull(result2.ToolResult);
        Assert.IsNotNull(result3.ToolResult);
        Assert.IsFalse(result1.ToolResult.Success);
        Assert.IsFalse(result2.ToolResult.Success);
        Assert.IsFalse(result3.ToolResult.Success);

        // Check that the third step added a loop-breaker controller turn
        var finalState = result3.State;
        Assert.IsNotNull(finalState);
        Assert.IsTrue(finalState.Turns.Count >= 3);

        // The last turn should be a controller turn with loop-breaker message
        var lastTurn = finalState.Turns[finalState.Turns.Count - 1];
        Assert.IsNotNull(lastTurn.LlmMessage);
        Assert.AreEqual(AgentAction.Retry, lastTurn.LlmMessage.Action);
        Assert.IsTrue(lastTurn.LlmMessage.Thoughts.Contains("You're repeating the same failing call"));
    }

    [TestMethod]
    public void LoopBreaker_DifferentParameters_DoesNotTrigger()
    {
        // Arrange - Set up LLM to make different failing calls
        var failingCall1 = @"{
            ""thoughts"": ""I need to call the validation tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""validation_tool"",
                ""params"": {
                    ""required_param"": """"
                }
            }
        }";

        var failingCall2 = @"{
            ""thoughts"": ""I need to call the validation tool with different params"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""validation_tool"",
                ""params"": {
                    ""required_param"": ""   ""
                }
            }
        }";

        var failingCall3 = @"{
            ""thoughts"": ""I need to call the validation tool with yet different params"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""validation_tool"",
                ""params"": {
                    ""required_param"": ""\t""
                }
            }
        }";

        _llmClient.SetNextResponse(failingCall1);
        _llmClient.SetNextResponse(failingCall2);
        _llmClient.SetNextResponse(failingCall3);

        // Act - Execute 3 steps with different failing calls
        var result1 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result2 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result3 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsNotNull(result3);
        Assert.IsTrue(result1.ExecutedTool);
        Assert.IsTrue(result2.ExecutedTool);
        Assert.IsTrue(result3.ExecutedTool);
        Assert.IsNotNull(result1.ToolResult);
        Assert.IsNotNull(result2.ToolResult);
        Assert.IsNotNull(result3.ToolResult);
        Assert.IsFalse(result1.ToolResult.Success);
        Assert.IsFalse(result2.ToolResult.Success);
        Assert.IsFalse(result3.ToolResult.Success);

        // Check that no loop-breaker controller turn was added (different parameters)
        var finalState = result3.State;
        Assert.IsNotNull(finalState);
        // The loop-breaker should not trigger because the parameters are different
        // We expect 3 tool call turns plus potentially some controller turns for validation failures
        Assert.IsTrue(finalState.Turns.Count >= 3);

        // Verify that the last turn is not a loop-breaker turn
        var lastTurn = finalState.Turns[finalState.Turns.Count - 1];
        Assert.IsFalse(lastTurn.LlmMessage?.Thoughts.Contains("You're repeating the same failing call") ?? false);
    }

    [TestMethod]
    public void LoopBreaker_SuccessfulCall_ResetsCounter()
    {
        // Arrange - Set up LLM to make 2 failing calls, then 1 successful call, then 2 more failing calls
        var failingCall = @"{
            ""thoughts"": ""I need to call the validation tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""validation_tool"",
                ""params"": {
                    ""required_param"": """"
                }
            }
        }";

        var successfulCall = @"{
            ""thoughts"": ""I need to call the validation tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""validation_tool"",
                ""params"": {
                    ""required_param"": ""valid_value""
                }
            }
        }";

        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(successfulCall);
        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);

        // Act - Execute 5 steps
        var result1 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result2 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result3 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result4 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result5 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsNotNull(result3);
        Assert.IsNotNull(result4);
        Assert.IsNotNull(result5);
        Assert.IsNotNull(result1.ToolResult);
        Assert.IsNotNull(result2.ToolResult);
        Assert.IsNotNull(result3.ToolResult);
        Assert.IsNotNull(result4.ToolResult);
        Assert.IsNotNull(result5.ToolResult);
        Assert.IsFalse(result1.ToolResult.Success);
        Assert.IsFalse(result2.ToolResult.Success);
        Assert.IsTrue(result3.ToolResult.Success); // The successful call
        Assert.IsFalse(result4.ToolResult.Success);
        Assert.IsFalse(result5.ToolResult.Success);

        // Check that the loop-breaker controller turn was NOT added (only 2 consecutive failures after success)
        var finalState = result5.State;
        Assert.IsNotNull(finalState);
        // We expect 5 tool call turns plus potentially some controller turns for validation failures
        Assert.IsTrue(finalState.Turns.Count >= 5);

        // Verify that the last turn is NOT a loop-breaker turn (because we only have 2 consecutive failures after success)
        var lastTurn = finalState.Turns[finalState.Turns.Count - 1];
        Assert.IsFalse(lastTurn.LlmMessage?.Thoughts.Contains("You're repeating the same failing call") ?? false);
    }

    [TestMethod]
    public void LoopBreaker_ThreeConsecutiveFailures_TriggersControllerTurn_Simple()
    {
        // Arrange - Set up LLM to make the same failing call 3 times
        var failingCall = @"{
            ""thoughts"": ""I need to call the validation tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""validation_tool"",
                ""params"": {
                    ""required_param"": """"
                }
            }
        }";

        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);

        // Act - Execute 3 steps with the same failing call
        var result1 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result2 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;
        var result3 = _agent.StepAsync("test-agent", "test goal", _tools.Values).Result;

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsNotNull(result3);
        Assert.IsTrue(result1.ExecutedTool);
        Assert.IsTrue(result2.ExecutedTool);
        Assert.IsTrue(result3.ExecutedTool);
        Assert.IsNotNull(result1.ToolResult);
        Assert.IsNotNull(result2.ToolResult);
        Assert.IsNotNull(result3.ToolResult);
        Assert.IsFalse(result1.ToolResult.Success);
        Assert.IsFalse(result2.ToolResult.Success);
        Assert.IsFalse(result3.ToolResult.Success);

        // Check that the third step added a loop-breaker controller turn
        var finalState = result3.State;
        Assert.IsNotNull(finalState);
        Assert.IsTrue(finalState.Turns.Count >= 3);

        // The last turn should be a controller turn with loop-breaker message
        var lastTurn = finalState.Turns[finalState.Turns.Count - 1];
        Assert.IsNotNull(lastTurn.LlmMessage);
        Assert.AreEqual(AgentAction.Retry, lastTurn.LlmMessage.Action);
        Assert.IsTrue(lastTurn.LlmMessage.Thoughts.Contains("You're repeating the same failing call"));
    }

    [TestMethod]
    public void LoopBreaker_DifferentAgents_IndependentTracking()
    {
        // Arrange - Set up LLM to make the same failing call
        var failingCall = @"{
            ""thoughts"": ""I need to call the validation tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""validation_tool"",
                ""params"": {
                    ""required_param"": """"
                }
            }
        }";

        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);
        _llmClient.SetNextResponse(failingCall);

        // Act - Execute 3 steps for agent1, then 3 steps for agent2
        var agent1Result1 = _agent.StepAsync("agent1", "test goal", _tools.Values).Result;
        var agent1Result2 = _agent.StepAsync("agent1", "test goal", _tools.Values).Result;
        var agent1Result3 = _agent.StepAsync("agent1", "test goal", _tools.Values).Result;

        var agent2Result1 = _agent.StepAsync("agent2", "test goal", _tools.Values).Result;
        var agent2Result2 = _agent.StepAsync("agent2", "test goal", _tools.Values).Result;
        var agent2Result3 = _agent.StepAsync("agent2", "test goal", _tools.Values).Result;

        // Assert - Both agents should have loop-breaker triggered
        Assert.IsNotNull(agent1Result3.State);
        Assert.IsNotNull(agent2Result3.State);
        Assert.IsTrue(agent1Result3.State.Turns.Count > 3); // Has controller turn
        Assert.IsTrue(agent2Result3.State.Turns.Count > 3); // Has controller turn

        // Both should have loop-breaker controller turns
        var agent1LastTurn = agent1Result3.State.Turns[agent1Result3.State.Turns.Count - 1];
        var agent2LastTurn = agent2Result3.State.Turns[agent2Result3.State.Turns.Count - 1];
        Assert.IsNotNull(agent1LastTurn.LlmMessage);
        Assert.IsNotNull(agent2LastTurn.LlmMessage);
        Assert.IsTrue(agent1LastTurn.LlmMessage.Thoughts.Contains("You're repeating the same failing call"));
        Assert.IsTrue(agent2LastTurn.LlmMessage.Thoughts.Contains("You're repeating the same failing call"));
    }



    // Helper class for testing
    private class ValidationTestTool : ITool
    {
        public string Name => "validation_tool";

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            if (!parameters.TryGetValue("required_param", out var requiredParam) ||
                string.IsNullOrWhiteSpace(GetStringValue(requiredParam)))
            {
                var missing = new List<string> { "required_param" };
                throw new ToolValidationException("Missing required parameter: required_param", missing);
            }

            var stringValue = GetStringValue(requiredParam);
            return Task.FromResult<object?>(new { result = "success", param = stringValue });
        }

        private static string GetStringValue(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is string str)
            {
                return str;
            }

            if (value is JsonElement element)
            {
                return element.ValueKind == JsonValueKind.String
                    ? element.GetString() ?? string.Empty
                    : element.ToString();
            }

            return value.ToString() ?? string.Empty;
        }
    }

    private class MockLlmClient : ILlmClient
    {
        private readonly Queue<string> _responses = new();

        public Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
        {
            // Get the content from the first user message
            var userMessage = messages.FirstOrDefault(m => m.Role == "user");
            var prompt = userMessage?.Content ?? "";

            // Handle reasoning-related prompts
            if (prompt.Contains("analysis") || (prompt.Contains("reasoning") && !prompt.Contains("HISTORY")) || prompt.Contains("Chain of Thought") || 
                prompt.Contains("Tree of Thoughts") || prompt.Contains("structured thinking"))
            {
                // Return a simple reasoning response for reasoning prompts
                return Task.FromResult(@"{
                    ""reasoning"": ""Analyzing the problem step by step..."",
                    ""confidence"": 0.85,
                    ""insights"": [""insight1"", ""insight2""],
                    ""conclusion"": ""Proceed with the original plan""
                }");
            }

            // Handle regular agent prompts
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response set. Call SetNextResponse first.");
            }

            var response = _responses.Dequeue();
            return Task.FromResult(response);
        }

        public void SetNextResponse(string response)
        {
            _responses.Enqueue(response);
            Console.WriteLine($"MockLlmClient queued response: {response}");
        }
    }
}