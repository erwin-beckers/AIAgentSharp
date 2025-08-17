using System.Text.Json;
using AIAgentSharp.Agents;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class AgentTests
{
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();

        // Act
        var agent = new Agent(llmClient, stateStore);

        // Assert
        Assert.IsNotNull(agent);
    }

    [TestMethod]
    public void Constructor_WithCustomLogger_CreatesInstance()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();
        var logger = new ConsoleLogger();

        // Act
        var agent = new Agent(llmClient, stateStore, logger);

        // Assert
        Assert.IsNotNull(agent);
    }

    [TestMethod]
    public void Constructor_WithCustomTimeouts_CreatesInstance()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();
        var llmTimeout = TimeSpan.FromMinutes(5);
        var toolTimeout = TimeSpan.FromMinutes(3);

        // Act
        var config = new AgentConfiguration
        {
            MaxTurns = 50,
            LlmTimeout = llmTimeout,
            ToolTimeout = toolTimeout
        };
        var agent = new Agent(llmClient, stateStore, config: config);

        // Assert
        Assert.IsNotNull(agent);
    }

    [TestMethod]
    public void Constructor_NullLlmClient_ThrowsArgumentNullException()
    {
        // Arrange
        var stateStore = new MemoryAgentStateStore();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Agent(null!, stateStore));
    }

    [TestMethod]
    public void Constructor_NullStateStore_ThrowsArgumentNullException()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Agent(llmClient, null!));
    }

    [TestMethod]
    public async Task StepAsync_NewAgent_CreatesInitialState()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.State);
        Assert.AreEqual("test-agent", result.State.AgentId);
        Assert.AreEqual("test goal", result.State.Goal);
        Assert.AreEqual(1, result.State.Turns.Count);
    }

    [TestMethod]
    public async Task StepAsync_ExistingAgent_PreservesState()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Create initial state
        await agent.StepAsync("test-agent", "original goal", new ITool[0]);

        // Act - step again with different goal
        var result = await agent.StepAsync("test-agent", "new goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.State);
        Assert.AreEqual("test-agent", result.State.AgentId);
        Assert.AreEqual("original goal", result.State.Goal); // Should preserve original goal
        Assert.AreEqual(2, result.State.Turns.Count); // Should have existing turn + new turn
    }

    [TestMethod]
    public async Task StepAsync_WithValidToolCall_ExecutesTool()
    {
        // Arrange
        var llmResponse = @"{
            ""thoughts"": ""I need to concatenate strings"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""concat"",
                ""params"": {
                    ""strings"": [""hello"", ""world""]
                }
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);
        var tools = new ITool[] { new MockConcatTool() };

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", tools);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsTrue(result.ToolResult.Success);
        Assert.AreEqual("concat", result.ToolResult.Tool);
        Assert.IsNotNull(result.ToolResult.Output);
        Assert.IsTrue(result.ToolResult.ExecutionTime > TimeSpan.Zero);
        Assert.IsFalse(string.IsNullOrEmpty(result.ToolResult.TurnId));
    }

    [TestMethod]
    public async Task StepAsync_WithInvalidTool_ReturnsError()
    {
        // Arrange
        var llmResponse = @"{
            ""thoughts"": ""I need to call a non-existent tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""non-existent-tool"",
                ""params"": {}
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        var toolResult = result.ToolResult;
        if (toolResult == null) throw new InvalidOperationException("ToolResult should not be null");
        Assert.IsFalse(toolResult.Success);
        Assert.IsNotNull(toolResult.Error);
        Assert.IsTrue(toolResult.Error.Contains("not found"));
    }

    [TestMethod]
    public async Task StepAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("invalid json"));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("Invalid LLM JSON"));
    }

    [TestMethod]
    public async Task StepAsync_WithFinishAction_ReturnsFinalOutput()
    {
        // Arrange
        var llmResponse = @"{
            ""thoughts"": ""I have completed the task"",
            ""action"": ""finish"",
            ""action_input"": {
                ""final"": ""Task completed successfully""
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Continue);
        Assert.AreEqual("Task completed successfully", result.FinalOutput);
    }

    [TestMethod]
    public async Task StepAsync_WithPlanAction_Continues()
    {
        // Arrange
        var llmResponse = @"{
            ""thoughts"": ""I need to plan my approach"",
            ""action"": ""plan"",
            ""action_input"": {
                ""summary"": ""Planning the next steps""
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual(AgentAction.Plan, result.LlmMessage.Action);
    }

    [TestMethod]
    public async Task StepAsync_WithRetryAction_Continues()
    {
        // Arrange
        var llmResponse = @"{
            ""thoughts"": ""I need to retry"",
            ""action"": ""retry"",
            ""action_input"": {
                ""summary"": ""Retrying the operation""
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.LlmMessage);
        Assert.AreEqual(AgentAction.Retry, result.LlmMessage.Action);
    }

    [TestMethod]
    public async Task StepAsync_WithLlmTimeout_HandlesTimeout()
    {
        // Arrange
        var llmClient = new DelegateLlmClient(async (messages, ct) =>
        {
            await Task.Delay(1000, ct); // Simulate slow response
            return "test";
        });
        var stateStore = new MemoryAgentStateStore();
        var config = new AgentConfiguration
        {
            MaxTurns = 50,
            LlmTimeout = TimeSpan.FromMilliseconds(100)
        };
        var agent = new Agent(llmClient, stateStore, config: config);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("deadline exceeded"));
    }

    [TestMethod]
    public async Task StepAsync_WithToolTimeout_HandlesTimeout()
    {
        // Arrange
        var slowTool = new SlowTool();
        var llmResponse = @"{
            ""thoughts"": ""I need to call a slow tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""slow-tool"",
                ""params"": {}
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var config = new AgentConfiguration
        {
            MaxTurns = 50,
            LlmTimeout = TimeSpan.FromMinutes(1),
            ToolTimeout = TimeSpan.FromMilliseconds(100)
        };
        var agent = new Agent(llmClient, stateStore, config: config);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[] { slowTool });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        Assert.IsNotNull(result.ToolResult.Error);
        Assert.IsTrue(result.ToolResult.Error.Contains("deadline exceeded"));
    }

    [TestMethod]
    public async Task StepAsync_WithSchemaValidation_ValidatesParameters()
    {
        // Arrange
        var typedTool = new SchemaValidationTests.TypedTool();
        var llmResponse = @"{
            ""thoughts"": ""I need to call a typed tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""typed-tool"",
                ""params"": {
                    ""name"": ""John"",
                    ""age"": 30,
                    ""isActive"": true
                }
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[] { typedTool });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsTrue(result.ToolResult.Success);
        Assert.AreEqual("typed-tool", result.ToolResult.Tool);
    }

    [TestMethod]
    public async Task StepAsync_WithSchemaValidation_InvalidParameters_ReturnsError()
    {
        // Arrange
        var typedTool = new SchemaValidationTests.TypedTool();
        var llmResponse = @"{
            ""thoughts"": ""I need to call a typed tool with invalid params"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""typed-tool"",
                ""params"": {
                    ""name"": ""John"",
                    ""age"": ""invalid_age"",
                    ""isActive"": true
                }
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // Act
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[] { typedTool });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        Assert.IsNotNull(result.ToolResult.Error);
        Assert.IsTrue(result.ToolResult.Error.Contains("could not be converted"));
    }

    [TestMethod]
    public async Task RunAsync_WithMaxTurns_StopsAtLimit()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();
        var config = new AgentConfiguration { MaxTurns = 3 };
        var agent = new Agent(llmClient, stateStore, config: config);

        // Act
        var result = await agent.RunAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("Max turns 3 reached"));
    }

    [TestMethod]
    public async Task StepAsync_WithIdempotency_ReusesExistingResults()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        // First step
        var firstResult = await agent.StepAsync("test-agent", "test goal", new ITool[0]);
        var firstTurnId = firstResult.State.Turns[0].TurnId;

        // Act - second step should reuse the same turn
        var secondResult = await agent.StepAsync("test-agent", "test goal", new ITool[0]);

        // Assert
        Assert.IsNotNull(secondResult);
        Assert.AreEqual(2, secondResult.State.Turns.Count);
        Assert.AreEqual(firstTurnId, secondResult.State.Turns[0].TurnId);
    }

    [TestMethod]
    public async Task StepAsync_TimeoutAndCancellation_ProperlyDistinguished()
    {
        // Arrange - Create a tool that takes longer than the timeout
        var slowTool = new SlowTool();
        var llmResponse = @"{
            ""thoughts"": ""I need to call a slow tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""slow-tool"",
                ""params"": {}
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var config = new AgentConfiguration
        {
            MaxTurns = 50,
            LlmTimeout = TimeSpan.FromMinutes(1),
            ToolTimeout = TimeSpan.FromMilliseconds(100)
        };
        var agent = new Agent(llmClient, stateStore, config: config);

        // Act - This should trigger a deadline exceeded (not cancellation)
        var result = await agent.StepAsync("test-agent", "test goal", new ITool[] { slowTool });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        Assert.IsFalse(result.ToolResult.Success);
        Assert.IsNotNull(result.ToolResult.Error);
        Assert.IsTrue(result.ToolResult.Error.Contains("deadline exceeded"));
        Assert.IsFalse(result.ToolResult.Error.Contains("cancelled by user"));
    }

    [TestMethod]
    public async Task StepAsync_GeneralLlmError_CreatesErrorTurn()
    {
        // Arrange - Create an LLM client that throws a general exception
        var llmClient = new DelegateLlmClient((messages, ct) =>
            throw new HttpRequestException("Internal server error (500)"));
        var stateStore = new MemoryAgentStateStore();
        var config = new AgentConfiguration
        {
            MaxTurns = 50,
            LlmTimeout = TimeSpan.FromMinutes(1),
            ToolTimeout = TimeSpan.FromMinutes(1)
        };
        var agent = new Agent(llmClient, stateStore, config: config);
        var tools = new List<ITool>();

        // Act
        var result = await agent.StepAsync("test-agent", "Test goal", tools);

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNotNull(result.ToolResult);
        var toolResult = result.ToolResult;
        Assert.IsNotNull(toolResult);
        Assert.IsFalse(toolResult.Success);
        Assert.IsNotNull(toolResult);
        Assert.IsNotNull(toolResult.Error);
        Assert.IsTrue(toolResult.Error.Contains("LLM call failed"));
        Assert.IsTrue(toolResult.Error.Contains("Internal server error"));
        Assert.AreEqual(1, result.State.Turns.Count);
        Assert.IsNotNull(result.State.Turns[0].ToolResult);
        var stateToolResult = result.State.Turns[0].ToolResult;
        Assert.IsNotNull(stateToolResult);
        Assert.IsFalse(stateToolResult.Success);
    }

    [TestMethod]
    public void Constructor_WithCustomConfiguration_CreatesInstance()
    {
        // Arrange
        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult("test"));
        var stateStore = new MemoryAgentStateStore();
        var config = new AgentConfiguration
        {
            MaxRecentTurns = 3,
            MaxThoughtsLength = 500,
            EnableHistorySummarization = false
        };

        // Act
        var agent = new Agent(llmClient, stateStore, config: config);

        // Assert
        Assert.IsNotNull(agent);
    }

    [TestMethod]
    public void BuildMessages_WithHistorySummarization_PrunesOldTurns()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            MaxRecentTurns = 2,
            EnableHistorySummarization = true
        };

        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new() { Index = 0, LlmMessage = new ModelMessage { Thoughts = "Old turn 1", Action = AgentAction.Plan } },
                new() { Index = 1, LlmMessage = new ModelMessage { Thoughts = "Old turn 2", Action = AgentAction.Plan } },
                new() { Index = 2, LlmMessage = new ModelMessage { Thoughts = "Recent turn 1", Action = AgentAction.Plan } },
                new() { Index = 3, LlmMessage = new ModelMessage { Thoughts = "Recent turn 2", Action = AgentAction.Plan } },
                new() { Index = 4, LlmMessage = new ModelMessage { Thoughts = "Recent turn 3", Action = AgentAction.Plan } }
            }
        };

        var tools = new Dictionary<string, ITool>();

        // Act
        var messageBuilder = new MessageBuilder(config);
        var messages = messageBuilder.BuildMessages(state, tools).ToList();
        var userMessage = messages[1];

        // Assert
        Assert.IsNotNull(userMessage.Content);
        Assert.IsTrue(userMessage.Content.Contains("SUMMARY:"));
        Assert.IsTrue(userMessage.Content.Contains("LLM:"));
        Assert.IsTrue(userMessage.Content.Contains("Recent turn 2"));
        Assert.IsTrue(userMessage.Content.Contains("Recent turn 3"));
        // The first 3 turns should be summarized, not shown in full detail
        // Check that old turns appear in summary format, not full JSON format
        Assert.IsTrue(userMessage.Content.Contains("SUMMARY: LLM: plan - Old turn 1"));
        Assert.IsTrue(userMessage.Content.Contains("SUMMARY: LLM: plan - Old turn 2"));
        Assert.IsFalse(userMessage.Content.Contains("\"thoughts\":\"Old turn 1\""));
        Assert.IsFalse(userMessage.Content.Contains("\"thoughts\":\"Old turn 2\""));
    }

    [TestMethod]
    public void JsonUtil_ParseStrict_WithSizeLimits_EnforcesLimits()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            MaxThoughtsLength = 10,
            MaxFinalLength = 10,
            MaxSummaryLength = 10
        };

        var longThoughts = new string('a', 20);
        var json = $@"{{
            ""thoughts"": ""{longThoughts}"",
            ""action"": ""finish"",
            ""action_input"": {{
                ""final"": ""short""
            }}
        }}";

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            JsonUtil.ParseStrict(json, config));
        Assert.IsTrue(exception.Message.Contains("exceeds maximum length"));
    }

    [TestMethod]
    public void ConvertJsonElementToNativeType_PreservesLargeIntegers()
    {
        // Arrange
        var largeNumber = long.MaxValue;
        var json = $@"{{
            ""thoughts"": ""test"",
            ""action"": ""tool_call"",
            ""action_input"": {{
                ""tool"": ""test"",
                ""params"": {{
                    ""large_number"": {largeNumber}
                }}
            }}
        }}";

        // Act
        var result = JsonUtil.ParseStrict(json);
        Assert.IsNotNull(result.ActionInput.Params);
        var paramsDict = result.ActionInput.Params;
        var largeNumberValue = paramsDict["large_number"];

        // Assert - The value should be preserved as JsonElement in this context
        // since ParseStrict doesn't convert parameters to native types
        Assert.IsNotNull(largeNumberValue);
        Assert.AreEqual(largeNumber.ToString(), largeNumberValue.ToString());
    }

    [TestMethod]
    public void HashToolCall_WithNestedObjects_ProducesConsistentHashes()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?>
        {
            ["nested"] = new Dictionary<string, object?>
            {
                ["b"] = "value2",
                ["a"] = "value1",
                ["array"] = new object[] { 1, 2, 3 }
            },
            ["simple"] = "test"
        };

        var params2 = new Dictionary<string, object?>
        {
            ["simple"] = "test",
            ["nested"] = new Dictionary<string, object?>
            {
                ["a"] = "value1",
                ["array"] = new object[] { 1, 2, 3 },
                ["b"] = "value2"
            }
        };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params2);

        // Assert
        Assert.AreEqual(hash1, hash2, "Hashes should be identical for logically equivalent parameters");
    }

    [TestMethod]
    public void HashToolCall_WithDifferentNestedObjects_ProducesDifferentHashes()
    {
        // Arrange
        var tool = "test_tool";
        var params1 = new Dictionary<string, object?>
        {
            ["nested"] = new Dictionary<string, object?>
            {
                ["a"] = "value1",
                ["b"] = "value2"
            }
        };

        var params2 = new Dictionary<string, object?>
        {
            ["nested"] = new Dictionary<string, object?>
            {
                ["a"] = "value1",
                ["b"] = "different_value"
            }
        };

        // Act
        var hash1 = AgentOrchestrator.HashToolCall(tool, params1);
        var hash2 = AgentOrchestrator.HashToolCall(tool, params2);

        // Assert
        Assert.AreNotEqual(hash1, hash2, "Hashes should be different for different parameter values");
    }

    [TestMethod]
    public async Task StepAsync_ToolWithDedupeDisabled_DoesNotReuseResults()
    {
        // Arrange
        var llmResponse = @"{
            ""thoughts"": ""I need to call the non-deduped tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""non-deduped-tool"",
                ""params"": { ""value"": ""test"" }
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        var nonDedupedTool = new NonDedupedTool();
        var tools = new[] { nonDedupedTool };

        // Act - First call
        var result1 = await agent.StepAsync("test-agent", "test goal", tools);

        // Act - Second call with same parameters
        var result2 = await agent.StepAsync("test-agent", "test goal", tools);

        // Assert
        Assert.IsTrue(result1.ExecutedTool);
        Assert.IsTrue(result2.ExecutedTool);
        Assert.IsNotNull(result1.ToolResult);
        Assert.IsNotNull(result2.ToolResult);
        Assert.IsTrue(result1.ToolResult.Success);
        Assert.IsTrue(result2.ToolResult.Success);
        // Should not dedupe, so both calls should have different execution times
        Assert.AreNotEqual(result1.ToolResult.ExecutionTime, result2.ToolResult.ExecutionTime);
    }

    [TestMethod]
    public async Task StepAsync_ToolWithCustomTtl_RespectsCustomTtl()
    {
        // Arrange
        var llmResponse = @"{
            ""thoughts"": ""I need to call the custom TTL tool"",
            ""action"": ""tool_call"",
            ""action_input"": {
                ""tool"": ""custom-ttl-tool"",
                ""params"": { ""value"": ""test"" }
            }
        }";

        var llmClient = new DelegateLlmClient((messages, ct) => Task.FromResult(llmResponse));
        var stateStore = new MemoryAgentStateStore();
        var agent = new Agent(llmClient, stateStore);

        var customTtlTool = new CustomTtlTool();
        var tools = new[] { customTtlTool };

        // Act - First call
        var result1 = await agent.StepAsync("test-agent", "test goal", tools);

        // Act - Second call with same parameters (should dedupe due to short TTL)
        var result2 = await agent.StepAsync("test-agent", "test goal", tools);

        // Assert
        Assert.IsTrue(result1.ExecutedTool);
        Assert.IsTrue(result2.ExecutedTool);
        Assert.IsNotNull(result1.ToolResult);
        Assert.IsNotNull(result2.ToolResult);
        Assert.IsTrue(result1.ToolResult.Success);
        Assert.IsTrue(result2.ToolResult.Success);
        // Should dedupe due to short TTL, so both calls should have the same execution time
        Assert.AreEqual(result1.ToolResult.ExecutionTime, result2.ToolResult.ExecutionTime);
    }

    [TestMethod]
    public void MockConcatTool_Schema_IncludesAdditionalPropertiesFalse()
    {
        // Arrange
        var concatTool = new MockConcatTool();

        // Act
        var schema = concatTool.GetJsonSchema();
        var schemaJson = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);

        // Assert
        Assert.IsTrue(schemaJson.Contains("\"additionalProperties\":false"),
            "MockConcatTool schema should include additionalProperties = false");
    }

    private class SlowTool : ITool
    {
        public string Name => "slow-tool";

        public async Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            await Task.Delay(1000, ct); // Simulate slow execution
            return "slow result";
        }
    }

    private class NonDedupedTool : ITool, IDedupeControl
    {
        public bool AllowDedupe => false;
        public TimeSpan? CustomTtl => null;
        public string Name => "non-deduped-tool";

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            // Simulate some work
            Thread.Sleep(10);
            return Task.FromResult<object?>("result");
        }
    }

    private class CustomTtlTool : ITool, IDedupeControl
    {
        public bool AllowDedupe => true;
        public TimeSpan? CustomTtl => TimeSpan.FromMinutes(10); // Longer than default
        public string Name => "custom-ttl-tool";

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            // Simulate some work
            Thread.Sleep(10);
            return Task.FromResult<object?>("result");
        }
    }
}
