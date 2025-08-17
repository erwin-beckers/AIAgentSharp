namespace AIAgentSharp.Tests;

[TestClass]
public sealed class ModelsTests
{
    [TestMethod]
    public void AgentResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new AgentResult();

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.FinalOutput);
        Assert.IsNotNull(result.State);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void AgentResult_WithValues_AreSetCorrectly()
    {
        // Arrange
        var state = new AgentState { AgentId = "test-agent", Goal = "test goal" };
        const string finalOutput = "test output";
        const string error = "test error";

        // Act
        var result = new AgentResult
        {
            Succeeded = true,
            FinalOutput = finalOutput,
            State = state,
            Error = error
        };

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(finalOutput, result.FinalOutput);
        Assert.AreEqual(state, result.State);
        Assert.AreEqual(error, result.Error);
    }

    [TestMethod]
    public void AgentStepResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new AgentStepResult();

        // Assert
        Assert.IsFalse(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        Assert.IsNull(result.FinalOutput);
        Assert.IsNull(result.LlmMessage);
        Assert.IsNull(result.ToolResult);
        Assert.IsNotNull(result.State);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void AgentStepResult_WithValues_AreSetCorrectly()
    {
        // Arrange
        var state = new AgentState { AgentId = "test-agent" };
        var llmMessage = new ModelMessage { Thoughts = "test thoughts" };
        var toolResult = new ToolExecutionResult { Success = true };

        // Act
        var result = new AgentStepResult
        {
            Continue = true,
            ExecutedTool = true,
            FinalOutput = "test output",
            LlmMessage = llmMessage,
            ToolResult = toolResult,
            State = state,
            Error = "test error"
        };

        // Assert
        Assert.IsTrue(result.Continue);
        Assert.IsTrue(result.ExecutedTool);
        Assert.AreEqual("test output", result.FinalOutput);
        Assert.AreEqual(llmMessage, result.LlmMessage);
        Assert.AreEqual(toolResult, result.ToolResult);
        Assert.AreEqual(state, result.State);
        Assert.AreEqual("test error", result.Error);
    }

    [TestMethod]
    public void AgentState_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var state = new AgentState();

        // Assert
        Assert.AreEqual(string.Empty, state.AgentId);
        Assert.AreEqual(string.Empty, state.Goal);
        Assert.IsNotNull(state.Turns);
        Assert.AreEqual(0, state.Turns.Count);
        Assert.IsTrue(state.UpdatedUtc > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [TestMethod]
    public void AgentState_WithValues_AreSetCorrectly()
    {
        // Arrange
        var turns = new List<AgentTurn> { new() { Index = 0 } };

        // Act
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "test goal",
            Turns = turns,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.AreEqual("test-agent", state.AgentId);
        Assert.AreEqual("test goal", state.Goal);
        Assert.AreEqual(turns, state.Turns);
        Assert.AreEqual(1, state.Turns.Count);
    }

    [TestMethod]
    public void AgentTurn_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var turn = new AgentTurn();

        // Assert
        Assert.AreEqual(0, turn.Index);
        Assert.IsNull(turn.LlmMessage);
        Assert.IsNull(turn.ToolCall);
        Assert.IsNull(turn.ToolResult);
        Assert.AreEqual(string.Empty, turn.TurnId);
    }

    [TestMethod]
    public void AgentTurn_WithValues_AreSetCorrectly()
    {
        // Arrange
        var llmMessage = new ModelMessage { Thoughts = "test" };
        var toolCall = new ToolCallRequest { Tool = "test-tool" };
        var toolResult = new ToolExecutionResult { Success = true };

        // Act
        var turn = new AgentTurn
        {
            Index = 1,
            LlmMessage = llmMessage,
            ToolCall = toolCall,
            ToolResult = toolResult,
            TurnId = "turn_1_123"
        };

        // Assert
        Assert.AreEqual(1, turn.Index);
        Assert.AreEqual(llmMessage, turn.LlmMessage);
        Assert.AreEqual(toolCall, turn.ToolCall);
        Assert.AreEqual(toolResult, turn.ToolResult);
        Assert.AreEqual("turn_1_123", turn.TurnId);
    }

    [TestMethod]
    public void ToolCallRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new ToolCallRequest();

        // Assert
        Assert.AreEqual(string.Empty, request.Tool);
        Assert.IsNotNull(request.Params);
        Assert.AreEqual(0, request.Params.Count);
        Assert.AreEqual(string.Empty, request.TurnId);
    }

    [TestMethod]
    public void ToolCallRequest_WithValues_AreSetCorrectly()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        var request = new ToolCallRequest
        {
            Tool = "test-tool",
            Params = parameters,
            TurnId = "turn_1_123"
        };

        // Assert
        Assert.AreEqual("test-tool", request.Tool);
        Assert.AreEqual(parameters, request.Params);
        Assert.AreEqual("turn_1_123", request.TurnId);
    }

    [TestMethod]
    public void ToolExecutionResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new ToolExecutionResult();

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Output);
        Assert.IsNull(result.Error);
        Assert.AreEqual(string.Empty, result.Tool);
        Assert.IsNotNull(result.Params);
        Assert.AreEqual(0, result.Params.Count);
        Assert.AreEqual(string.Empty, result.TurnId);
        Assert.AreEqual(TimeSpan.Zero, result.ExecutionTime);
    }

    [TestMethod]
    public void ToolExecutionResult_WithValues_AreSetCorrectly()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "key", "value" } };
        var executionTime = TimeSpan.FromMilliseconds(100);

        // Act
        var result = new ToolExecutionResult
        {
            Success = true,
            Output = "test output",
            Error = "test error",
            Tool = "test-tool",
            Params = parameters,
            TurnId = "turn_1_123",
            ExecutionTime = executionTime
        };

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual("test output", result.Output);
        Assert.AreEqual("test error", result.Error);
        Assert.AreEqual("test-tool", result.Tool);
        Assert.AreEqual(parameters, result.Params);
        Assert.AreEqual("turn_1_123", result.TurnId);
        Assert.AreEqual(executionTime, result.ExecutionTime);
    }

    [TestMethod]
    public void ModelMessage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var message = new ModelMessage();

        // Assert
        Assert.AreEqual(string.Empty, message.Thoughts);
        Assert.AreEqual(AgentAction.Plan, message.Action);
        Assert.IsNotNull(message.ActionInput);
        Assert.AreEqual(string.Empty, message.ActionRaw);
    }

    [TestMethod]
    public void ModelMessage_WithValues_AreSetCorrectly()
    {
        // Arrange
        var actionInput = new ActionInput { Tool = "test-tool" };

        // Act
        var message = new ModelMessage
        {
            Thoughts = "test thoughts",
            Action = AgentAction.ToolCall,
            ActionInput = actionInput,
            ActionRaw = "tool_call"
        };

        // Assert
        Assert.AreEqual("test thoughts", message.Thoughts);
        Assert.AreEqual(AgentAction.ToolCall, message.Action);
        Assert.AreEqual(actionInput, message.ActionInput);
        Assert.AreEqual("tool_call", message.ActionRaw);
    }

    [TestMethod]
    public void ActionInput_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var input = new ActionInput();

        // Assert
        Assert.IsNull(input.Tool);
        Assert.IsNull(input.Params);
        Assert.IsNull(input.Summary);
        Assert.IsNull(input.Final);
    }

    [TestMethod]
    public void ActionInput_WithValues_AreSetCorrectly()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        var input = new ActionInput
        {
            Tool = "test-tool",
            Params = parameters,
            Summary = "test summary",
            Final = "test final"
        };

        // Assert
        Assert.AreEqual("test-tool", input.Tool);
        Assert.AreEqual(parameters, input.Params);
        Assert.AreEqual("test summary", input.Summary);
        Assert.AreEqual("test final", input.Final);
    }

    [TestMethod]
    public void LlmMessage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var message = new LlmMessage();

        // Assert
        Assert.AreEqual(string.Empty, message.Role);
        Assert.AreEqual(string.Empty, message.Content);
    }

    [TestMethod]
    public void LlmMessage_WithValues_AreSetCorrectly()
    {
        // Act
        var message = new LlmMessage
        {
            Role = "user",
            Content = "test content"
        };

        // Assert
        Assert.AreEqual("user", message.Role);
        Assert.AreEqual("test content", message.Content);
    }

    [TestMethod]
    public void AgentAction_EnumValues_AreCorrect()
    {
        // Assert
        Assert.AreEqual(0, (int)AgentAction.Plan);
        Assert.AreEqual(1, (int)AgentAction.ToolCall);
        Assert.AreEqual(2, (int)AgentAction.Finish);
        Assert.AreEqual(3, (int)AgentAction.Retry);
    }

    [TestMethod]
    public void AgentAction_ToString_ReturnsExpectedValues()
    {
        // Assert
        Assert.AreEqual("Plan", AgentAction.Plan.ToString());
        Assert.AreEqual("ToolCall", AgentAction.ToolCall.ToString());
        Assert.AreEqual("Finish", AgentAction.Finish.ToString());
        Assert.AreEqual("Retry", AgentAction.Retry.ToString());
    }
}