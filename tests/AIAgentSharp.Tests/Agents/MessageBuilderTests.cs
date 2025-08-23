using AIAgentSharp.Agents;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class MessageBuilderTests
{
    private AgentConfiguration _config = null!;
    private MessageBuilder _messageBuilder = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new AgentConfiguration
        {
            MaxRecentTurns = 5,
            EnableHistorySummarization = true,
            EmitPublicStatus = true,
            MaxToolOutputSize = 1000
        };
        _messageBuilder = new MessageBuilder(_config);
    }

    [TestMethod]
    public void Constructor_Should_CreateMessageBuilderSuccessfully_When_ValidConfigProvided()
    {
        // Act
        var messageBuilder = new MessageBuilder(_config);

        // Assert
        Assert.IsNotNull(messageBuilder);
    }

    [TestMethod]
    public void Constructor_Should_ThrowNullReferenceException_When_ConfigIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() => new MessageBuilder(null!));
    }

    [TestMethod]
    public void BuildMessages_Should_ReturnSystemMessage_When_ValidStateAndToolsProvided()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        Assert.IsNotNull(result);
        var messages = result.ToList();
        Assert.IsTrue(messages.Count >= 2); // At least system + user; additional messages may exist
        
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(systemMessage);
        Assert.IsNotNull(userMessage);
        
        // System message should contain the predefined prompt
        Assert.IsTrue(systemMessage.Content.Contains("You are a stateful, tool-using agent"));
        
        // User message should contain the dynamic content
        // In new behavior, goal is sent as plain user content and tool catalog is in system message
        Assert.IsTrue(systemMessage.Content.Contains("TOOL CATALOG"));
        Assert.IsTrue(userMessage.Content.Contains("Test goal"));
    }

    [TestMethod]
    public void BuildMessages_Should_IncludeToolCatalog_When_ToolsProvided()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns("test_tool");
        mockTool.Setup(x => x.InvokeAsync(It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test result");
        
        var tools = new Dictionary<string, ITool>
        {
            { "test_tool", mockTool.Object }
        };

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        Assert.IsNotNull(systemMessage);
        Assert.IsTrue(systemMessage.Content.Contains("test_tool"));
    }

    [TestMethod]
    public void BuildMessages_Should_IncludeMultiToolCallInstructions_When_BuildingMessages()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        Assert.IsNotNull(systemMessage);
        
        // Should include multi-tool call instructions in system prompt
        Assert.IsTrue(systemMessage.Content.Contains("\"multi_tool_call\""));
    }

    [TestMethod]
    public void BuildMessages_Should_IncludeActionsAvailableSection_When_BuildingMessages()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        Assert.IsNotNull(systemMessage);
        
        // Should include allowed actions in system prompt
        Assert.IsTrue(systemMessage.Content.Contains("\"tool_call\""));
        Assert.IsTrue(systemMessage.Content.Contains("\"multi_tool_call\""));
        Assert.IsTrue(systemMessage.Content.Contains("\"plan\""));
        Assert.IsTrue(systemMessage.Content.Contains("\"finish\""));
        Assert.IsTrue(systemMessage.Content.Contains("\"retry\""));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleMultipleToolCallsInHistory_When_RecentTurnsContainMultiToolCalls()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new AgentTurn
                {
                    Index = 0,
                    TurnId = "turn_0",
                    LlmMessage = new ModelMessage
                    {
                        Thoughts = "I need to call multiple tools",
                        Action = AgentAction.MultiToolCall,
                        ActionInput = new ActionInput
                        {
                            ToolCalls = new List<ToolCall>
                            {
                                new ToolCall
                                {
                                    Tool = "tool1",
                                    Params = new Dictionary<string, object?> { { "param1", "value1" } },
                                    Reason = "First tool"
                                },
                                new ToolCall
                                {
                                    Tool = "tool2",
                                    Params = new Dictionary<string, object?> { { "param2", "value2" } },
                                    Reason = "Second tool"
                                }
                            }
                        }
                    },
                    ToolCalls = new List<ToolCallRequest>
                    {
                        new ToolCallRequest { Tool = "tool1", Params = new Dictionary<string, object?> { { "param1", "value1" } }, TurnId = "hash1" },
                        new ToolCallRequest { Tool = "tool2", Params = new Dictionary<string, object?> { { "param2", "value2" } }, TurnId = "hash2" }
                    },
                    ToolResults = new List<ToolExecutionResult>
                    {
                        new ToolExecutionResult
                        {
                            Success = true,
                            Tool = "tool1",
                            Output = "result1",
                            TurnId = "hash1",
                            CreatedUtc = DateTimeOffset.UtcNow
                        },
                        new ToolExecutionResult
                        {
                            Success = true,
                            Tool = "tool2",
                            Output = "result2",
                            TurnId = "hash2",
                            CreatedUtc = DateTimeOffset.UtcNow
                        }
                    }
                }
            }
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        
        // Should include multi-tool call history (history content message)
        Assert.IsTrue(historyMessage.Content.Contains("MULTI_TOOL_CALLS:"));
        Assert.IsTrue(historyMessage.Content.Contains("MULTI_TOOL_RESULTS:"));
        Assert.IsTrue(historyMessage.Content.Contains("tool1"));
        Assert.IsTrue(historyMessage.Content.Contains("tool2"));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleMultipleToolCallsInSummarizedHistory_When_OldTurnsContainMultiToolCalls()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        // Add many turns to trigger summarization
        for (int i = 0; i < 10; i++)
        {
            state.Turns.Add(new AgentTurn
            {
                Index = i,
                TurnId = $"turn_{i}",
                LlmMessage = new ModelMessage
                {
                    Thoughts = $"Turn {i} thoughts",
                    Action = i % 2 == 0 ? AgentAction.ToolCall : AgentAction.MultiToolCall,
                    ActionInput = i % 2 == 0 ? 
                        new ActionInput { Tool = "single_tool", Params = new Dictionary<string, object?>() } :
                        new ActionInput 
                        { 
                            ToolCalls = new List<ToolCall>
                            {
                                new ToolCall { Tool = "multi_tool1", Params = new Dictionary<string, object?>() },
                                new ToolCall { Tool = "multi_tool2", Params = new Dictionary<string, object?>() }
                            }
                        }
                },
                ToolCalls = i % 2 == 0 ? null : new List<ToolCallRequest>
                {
                    new ToolCallRequest { Tool = "multi_tool1", Params = new Dictionary<string, object?>(), TurnId = $"hash_{i}_1" },
                    new ToolCallRequest { Tool = "multi_tool2", Params = new Dictionary<string, object?>(), TurnId = $"hash_{i}_2" }
                },
                ToolResults = i % 2 == 0 ? null : new List<ToolExecutionResult>
                {
                    new ToolExecutionResult { Success = true, Tool = "multi_tool1", TurnId = $"hash_{i}_1", CreatedUtc = DateTimeOffset.UtcNow },
                    new ToolExecutionResult { Success = true, Tool = "multi_tool2", TurnId = $"hash_{i}_2", CreatedUtc = DateTimeOffset.UtcNow }
                }
            });
        }

        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        
        // Should include summarized multi-tool calls
        Assert.IsTrue(historyMessage.Content.Contains("MULTI_TOOLS:"));
        Assert.IsTrue(historyMessage.Content.Contains("MULTI_RESULTS:"));
        Assert.IsTrue(historyMessage.Content.Contains("multi_tool1, multi_tool2"));
    }

    [TestMethod]
    public void BuildMessages_Should_IncludeMultiToolCallHint_When_BuildingMessages()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        Assert.IsNotNull(systemMessage);
        
        // Should include hint about multiple tool calls in system prompt
        Assert.IsTrue(systemMessage.Content.Contains("multi_tool_call"));
    }

    [TestMethod]
    public void BuildMessages_Should_IncludeStatusUpdateInstructions_When_EmitPublicStatusIsTrue()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        Assert.IsNotNull(systemMessage);
        Assert.IsTrue(systemMessage.Content.Contains("STATUS UPDATES"));
        Assert.IsTrue(systemMessage.Content.Contains("status_title"));
        Assert.IsTrue(systemMessage.Content.Contains("status_details"));
        Assert.IsTrue(systemMessage.Content.Contains("next_step_hint"));
        Assert.IsTrue(systemMessage.Content.Contains("progress_pct"));
    }

    [TestMethod]
    public void BuildMessages_Should_NotIncludeStatusUpdateInstructions_When_EmitPublicStatusIsFalse()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = false };
        var messageBuilder = new MessageBuilder(config);
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        Assert.IsNotNull(systemMessage);
        Assert.IsFalse(systemMessage.Content.Contains("STATUS UPDATES"));
    }

    [TestMethod]
    public void BuildMessages_Should_IncludeFullHistory_When_HistorySummarizationDisabled()
    {
        // Arrange
        var config = new AgentConfiguration { EnableHistorySummarization = false };
        var messageBuilder = new MessageBuilder(config);
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new AgentTurn
                {
                    Index = 0,
                    LlmMessage = new ModelMessage { Thoughts = "Test thoughts", Action = AgentAction.ToolCall },
                    ToolCall = new ToolCallRequest { Tool = "test_tool" },
                    ToolResult = new ToolExecutionResult { Success = true, Output = "Test output" }
                }
            }
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        Assert.IsTrue(historyMessage.Content.Contains("LLM:"));
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_CALL:"));
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_RESULT:"));
    }

    [TestMethod]
    public void BuildMessages_Should_IncludeRecentTurnsInFullDetail_When_HistorySummarizationEnabled()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        // Add more turns than MaxRecentTurns
        for (int i = 0; i < 10; i++)
        {
            state.Turns.Add(new AgentTurn
            {
                Index = i,
                LlmMessage = new ModelMessage { Thoughts = $"Thoughts {i}", Action = AgentAction.ToolCall },
                ToolCall = new ToolCallRequest { Tool = $"tool_{i}" },
                ToolResult = new ToolExecutionResult { Success = true, Output = $"Output {i}" }
            });
        }

        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        
        // Should include full detail for recent turns
        Assert.IsTrue(historyMessage.Content.Contains("LLM:"));
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_CALL:"));
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_RESULT:"));
    }

    [TestMethod]
    public void BuildMessages_Should_IncludeSummarizedHistory_When_HistorySummarizationEnabled()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        // Add more turns than MaxRecentTurns
        for (int i = 0; i < 10; i++)
        {
            state.Turns.Add(new AgentTurn
            {
                Index = i,
                LlmMessage = new ModelMessage { Thoughts = $"Thoughts {i}", Action = AgentAction.ToolCall },
                ToolCall = new ToolCallRequest { Tool = $"tool_{i}" },
                ToolResult = new ToolExecutionResult { Success = true, Output = $"Output {i}" }
            });
        }

        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        
        // Should include summarized history for older turns
        Assert.IsTrue(historyMessage.Content.Contains("LLM: toolcall -"));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleEmptyState_When_StateHasNoTurns()
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
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        Assert.IsNotNull(result);
        var messages = result.ToList();
        Assert.IsTrue(messages.Count >= 2);
        
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        Assert.IsTrue(historyMessage.Content.Contains("HISTORY"));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleNullTools_When_ToolsIsNull()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };

        // Act & Assert - Should throw NullReferenceException
        Assert.ThrowsException<NullReferenceException>(() => 
            _messageBuilder.BuildMessages(state, null!));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleEmptyTools_When_ToolsIsEmpty()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        Assert.IsNotNull(result);
        var messages = result.ToList();
        Assert.IsTrue(messages.Count >= 2);
        
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        Assert.IsNotNull(systemMessage);
        Assert.IsTrue(systemMessage.Content.Contains("TOOL CATALOG"));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleToolWithoutIntrospect_When_ToolDoesNotImplementIToolIntrospect()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal"
        };
        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns("test_tool");
        
        var tools = new Dictionary<string, ITool>
        {
            { "test_tool", mockTool.Object }
        };

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        Assert.IsNotNull(systemMessage);
        Assert.IsTrue(systemMessage.Content.Contains("test_tool"));
        Assert.IsTrue(systemMessage.Content.Contains("{\"params\":{}}"));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleNullLlmMessage_When_TurnHasNullLlmMessage()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new AgentTurn
                {
                    Index = 0,
                    LlmMessage = null,
                    ToolCall = new ToolCallRequest { Tool = "test_tool" },
                    ToolResult = new ToolExecutionResult { Success = true, Output = "Test output" }
                }
            }
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        Assert.IsNotNull(result);
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_CALL:"));
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_RESULT:"));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleNullToolCall_When_TurnHasNullToolCall()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new AgentTurn
                {
                    Index = 0,
                    LlmMessage = new ModelMessage { Thoughts = "Test thoughts", Action = AgentAction.ToolCall },
                    ToolCall = null,
                    ToolResult = new ToolExecutionResult { Success = true, Output = "Test output" }
                }
            }
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        Assert.IsNotNull(result);
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        Assert.IsTrue(historyMessage.Content.Contains("LLM:"));
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_RESULT:"));
    }

    [TestMethod]
    public void BuildMessages_Should_HandleNullToolResult_When_TurnHasNullToolResult()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new AgentTurn
                {
                    Index = 0,
                    LlmMessage = new ModelMessage { Thoughts = "Test thoughts", Action = AgentAction.ToolCall },
                    ToolCall = new ToolCallRequest { Tool = "test_tool" },
                    ToolResult = null
                }
            }
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        Assert.IsNotNull(result);
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        Assert.IsTrue(historyMessage.Content.Contains("LLM:"));
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_CALL:"));
    }

    [TestMethod]
    public void BuildMessages_Should_TruncateLargeToolOutput_When_ToolOutputExceedsMaxSize()
    {
        // Arrange
        var largeOutput = new string('x', _config.MaxToolOutputSize + 100);
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new AgentTurn
                {
                    Index = 0,
                    LlmMessage = new ModelMessage { Thoughts = "Test thoughts", Action = AgentAction.ToolCall },
                    ToolCall = new ToolCallRequest { Tool = "test_tool" },
                    ToolResult = new ToolExecutionResult { Success = true, Output = largeOutput }
                }
            }
        };
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = _messageBuilder.BuildMessages(state, tools);

        // Assert
        var messages = result.ToList();
        var historyMessage = messages.LastOrDefault(m => m.Role == "user");
        Assert.IsNotNull(historyMessage);
        
        Assert.IsTrue(historyMessage.Content.Contains("TOOL_RESULT:"));
        // The output should be truncated, so the content should contain the truncated indicator
        Assert.IsTrue(historyMessage.Content.Contains("\"truncated\":true"));
        Assert.IsTrue(historyMessage.Content.Contains("\"original_size\":"));
    }
}
