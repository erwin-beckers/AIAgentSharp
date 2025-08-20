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
        Assert.IsTrue(messages.Count == 2); // System message + User message
        
        var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(systemMessage);
        Assert.IsNotNull(userMessage);
        
        // System message should contain the predefined prompt
        Assert.IsTrue(systemMessage.Content.Contains("You are a stateful, tool-using agent"));
        
        // User message should contain the dynamic content
        Assert.IsTrue(userMessage.Content.Contains("GOAL:"));
        Assert.IsTrue(userMessage.Content.Contains("Test goal"));
        Assert.IsTrue(userMessage.Content.Contains("TOOL CATALOG"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("TOOL CATALOG"));
        Assert.IsTrue(userMessage.Content.Contains("test_tool"));
        Assert.IsTrue(userMessage.Content.Contains("{\"params\":{}}"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("STATUS UPDATES"));
        Assert.IsTrue(userMessage.Content.Contains("status_title"));
        Assert.IsTrue(userMessage.Content.Contains("status_details"));
        Assert.IsTrue(userMessage.Content.Contains("next_step_hint"));
        Assert.IsTrue(userMessage.Content.Contains("progress_pct"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsFalse(userMessage.Content.Contains("STATUS UPDATES"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("LLM:"));
        Assert.IsTrue(userMessage.Content.Contains("TOOL_CALL:"));
        Assert.IsTrue(userMessage.Content.Contains("TOOL_RESULT:"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        
        // Should include full detail for recent turns
        Assert.IsTrue(userMessage.Content.Contains("LLM:"));
        Assert.IsTrue(userMessage.Content.Contains("TOOL_CALL:"));
        Assert.IsTrue(userMessage.Content.Contains("TOOL_RESULT:"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        
        // Should include summarized history for older turns
        Assert.IsTrue(userMessage.Content.Contains("LLM: toolcall -"));
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
        Assert.IsTrue(messages.Count == 2);
        
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("HISTORY"));
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
        Assert.IsTrue(messages.Count == 2);
        
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("TOOL CATALOG"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("test_tool"));
        Assert.IsTrue(userMessage.Content.Contains("{\"params\":{}}"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("TOOL_CALL:"));
        Assert.IsTrue(userMessage.Content.Contains("TOOL_RESULT:"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("LLM:"));
        Assert.IsTrue(userMessage.Content.Contains("TOOL_RESULT:"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        Assert.IsTrue(userMessage.Content.Contains("LLM:"));
        Assert.IsTrue(userMessage.Content.Contains("TOOL_CALL:"));
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
        var userMessage = messages.FirstOrDefault(m => m.Role == "user");
        Assert.IsNotNull(userMessage);
        
        Assert.IsTrue(userMessage.Content.Contains("TOOL_RESULT:"));
        // The output should be truncated, so the content should contain the truncated indicator
        Assert.IsTrue(userMessage.Content.Contains("\"truncated\":true"));
        Assert.IsTrue(userMessage.Content.Contains("\"original_size\":"));
    }
}
