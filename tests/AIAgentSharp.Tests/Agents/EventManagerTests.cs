using AIAgentSharp.Agents;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class EventManagerTests
{
    private Mock<ILogger> _mockLogger;
    private EventManager _eventManager;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _eventManager = new EventManager(_mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_Should_CreateEventManagerSuccessfully_When_LoggerProvided()
    {
        // Act
        var eventManager = new EventManager(_mockLogger.Object);

        // Assert
        Assert.IsNotNull(eventManager);
    }

    [TestMethod]
    public void Constructor_Should_UseConsoleLoggerAsDefault_When_LoggerIsNull()
    {
        // Act
        var eventManager = new EventManager(null!);

        // Assert
        Assert.IsNotNull(eventManager);
    }

    [TestMethod]
    public void RaiseRunStarted_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        AgentRunStartedEventArgs? capturedArgs = null;
        _eventManager.RunStarted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseRunStarted(agentId, goal);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(goal, capturedArgs.Goal);
    }

    [TestMethod]
    public void RaiseRunStarted_Should_HandleNullAgentId_When_AgentIdIsNull()
    {
        // Arrange
        var goal = "Test goal";
        AgentRunStartedEventArgs? capturedArgs = null;
        _eventManager.RunStarted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseRunStarted(null!, goal);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.IsNull(capturedArgs.AgentId);
        Assert.AreEqual(goal, capturedArgs.Goal);
    }

    [TestMethod]
    public void RaiseRunStarted_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        _eventManager.RunStarted += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseRunStarted(agentId, goal);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("RunStarted event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseStepStarted_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 5;
        AgentStepStartedEventArgs? capturedArgs = null;
        _eventManager.StepStarted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseStepStarted(agentId, turnIndex);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
    }

    [TestMethod]
    public void RaiseStepStarted_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 5;
        _eventManager.StepStarted += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseStepStarted(agentId, turnIndex);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("StepStarted event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseLlmCallStarted_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        AgentLlmCallStartedEventArgs? capturedArgs = null;
        _eventManager.LlmCallStarted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseLlmCallStarted(agentId, turnIndex);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
    }

    [TestMethod]
    public void RaiseLlmCallStarted_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        _eventManager.LlmCallStarted += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseLlmCallStarted(agentId, turnIndex);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("LlmCallStarted event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseLlmCallCompleted_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var llmMessage = new ModelMessage { Thoughts = "Test thoughts" };
        var error = "Test error";
        AgentLlmCallCompletedEventArgs? capturedArgs = null;
        _eventManager.LlmCallCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, llmMessage, error);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.AreEqual(llmMessage, capturedArgs.LlmMessage);
        Assert.AreEqual(error, capturedArgs.Error);
    }

    [TestMethod]
    public void RaiseLlmCallCompleted_Should_HandleNullParameters_When_ParametersAreNull()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        AgentLlmCallCompletedEventArgs? capturedArgs = null;
        _eventManager.LlmCallCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, null, null);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.IsNull(capturedArgs.LlmMessage);
        Assert.IsNull(capturedArgs.Error);
    }

    [TestMethod]
    public void RaiseLlmCallCompleted_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        _eventManager.LlmCallCompleted += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, null, null);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("LlmCallCompleted event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseLlmChunkReceived_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var chunk = new LlmStreamingChunk { Content = "Test chunk" };
        AgentLlmChunkReceivedEventArgs? capturedArgs = null;
        _eventManager.LlmChunkReceived += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseLlmChunkReceived(agentId, turnIndex, chunk);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.AreEqual(chunk, capturedArgs.Chunk);
    }

    [TestMethod]
    public void RaiseLlmChunkReceived_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var chunk = new LlmStreamingChunk { Content = "Test chunk" };
        _eventManager.LlmChunkReceived += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseLlmChunkReceived(agentId, turnIndex, chunk);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("LlmChunkReceived event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseToolCallStarted_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };
        AgentToolCallStartedEventArgs? capturedArgs = null;
        _eventManager.ToolCallStarted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseToolCallStarted(agentId, turnIndex, toolName, parameters);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.AreEqual(toolName, capturedArgs.ToolName);
        Assert.AreEqual(parameters, capturedArgs.Parameters);
    }

    [TestMethod]
    public void RaiseToolCallStarted_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var toolName = "test-tool";
        var parameters = new Dictionary<string, object?>();
        _eventManager.ToolCallStarted += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseToolCallStarted(agentId, turnIndex, toolName, parameters);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("ToolCallStarted event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseToolCallCompleted_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var toolName = "test-tool";
        var success = true;
        var output = "Test output";
        var error = "Test error";
        var executionTime = TimeSpan.FromSeconds(1.5);
        AgentToolCallCompletedEventArgs? capturedArgs = null;
        _eventManager.ToolCallCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, success, output, error, executionTime);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.AreEqual(toolName, capturedArgs.ToolName);
        Assert.AreEqual(success, capturedArgs.Success);
        Assert.AreEqual(output, capturedArgs.Output);
        Assert.AreEqual(error, capturedArgs.Error);
        Assert.AreEqual(executionTime, capturedArgs.ExecutionTime);
    }

    [TestMethod]
    public void RaiseToolCallCompleted_Should_UseDefaultExecutionTime_When_ExecutionTimeIsNull()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var toolName = "test-tool";
        var success = true;
        AgentToolCallCompletedEventArgs? capturedArgs = null;
        _eventManager.ToolCallCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, success, null, null, null);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(TimeSpan.Zero, capturedArgs.ExecutionTime);
    }

    [TestMethod]
    public void RaiseToolCallCompleted_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var toolName = "test-tool";
        var success = true;
        _eventManager.ToolCallCompleted += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, success);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("ToolCallCompleted event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseStepCompleted_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var stepResult = new AgentStepResult
        {
            Continue = true,
            ExecutedTool = true,
            FinalOutput = "Test output",
            Error = "Test error"
        };
        AgentStepCompletedEventArgs? capturedArgs = null;
        _eventManager.StepCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseStepCompleted(agentId, turnIndex, stepResult);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.AreEqual(stepResult.Continue, capturedArgs.Continue);
        Assert.AreEqual(stepResult.ExecutedTool, capturedArgs.ExecutedTool);
        Assert.AreEqual(stepResult.FinalOutput, capturedArgs.FinalOutput);
        Assert.AreEqual(stepResult.Error, capturedArgs.Error);
    }

    [TestMethod]
    public void RaiseStepCompleted_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 3;
        var stepResult = new AgentStepResult();
        _eventManager.StepCompleted += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseStepCompleted(agentId, turnIndex, stepResult);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("StepCompleted event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseRunCompleted_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = true;
        var finalOutput = "Test final output";
        var error = "Test error";
        var totalTurns = 10;
        AgentRunCompletedEventArgs? capturedArgs = null;
        _eventManager.RunCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseRunCompleted(agentId, succeeded, finalOutput, error, totalTurns);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(succeeded, capturedArgs.Succeeded);
        Assert.AreEqual(finalOutput, capturedArgs.FinalOutput);
        Assert.AreEqual(error, capturedArgs.Error);
        Assert.AreEqual(totalTurns, capturedArgs.TotalTurns);
    }

    [TestMethod]
    public void RaiseRunCompleted_Should_HandleNullParameters_When_ParametersAreNull()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = false;
        var totalTurns = 5;
        AgentRunCompletedEventArgs? capturedArgs = null;
        _eventManager.RunCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseRunCompleted(agentId, succeeded, null, null, totalTurns);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(succeeded, capturedArgs.Succeeded);
        Assert.IsNull(capturedArgs.FinalOutput);
        Assert.IsNull(capturedArgs.Error);
        Assert.AreEqual(totalTurns, capturedArgs.TotalTurns);
    }

    [TestMethod]
    public void RaiseRunCompleted_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = true;
        var totalTurns = 5;
        _eventManager.RunCompleted += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseRunCompleted(agentId, succeeded, null, null, totalTurns);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("RunCompleted event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void RaiseStatusUpdate_Should_RaiseEventSuccessfully_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var statusTitle = "Test Status";
        var statusDetails = "Test Details";
        var nextStepHint = "Next Step";
        var progressPct = 75;
        AgentStatusEventArgs? capturedArgs = null;
        _eventManager.StatusUpdate += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseStatusUpdate(agentId, statusTitle, statusDetails, nextStepHint, progressPct);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(0, capturedArgs.TurnIndex); // Default value
        Assert.AreEqual(statusTitle, capturedArgs.StatusTitle);
        Assert.AreEqual(statusDetails, capturedArgs.StatusDetails);
        Assert.AreEqual(nextStepHint, capturedArgs.NextStepHint);
        Assert.AreEqual(progressPct, capturedArgs.ProgressPct);
    }

    [TestMethod]
    public void RaiseStatusUpdate_Should_HandleNullOptionalParameters_When_OptionalParametersAreNull()
    {
        // Arrange
        var agentId = "test-agent";
        var statusTitle = "Test Status";
        AgentStatusEventArgs? capturedArgs = null;
        _eventManager.StatusUpdate += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseStatusUpdate(agentId, statusTitle, null, null, null);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(statusTitle, capturedArgs.StatusTitle);
        Assert.IsNull(capturedArgs.StatusDetails);
        Assert.IsNull(capturedArgs.NextStepHint);
        Assert.IsNull(capturedArgs.ProgressPct);
    }

    [TestMethod]
    public void RaiseStatusUpdate_Should_HandleExceptionGracefully_When_EventHandlerThrows()
    {
        // Arrange
        var agentId = "test-agent";
        var statusTitle = "Test Status";
        _eventManager.StatusUpdate += (sender, args) => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        _eventManager.RaiseStatusUpdate(agentId, statusTitle);

        // Verify logger was called
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("StatusUpdate event handler threw exception"))), Times.Once);
    }

    [TestMethod]
    public void MultipleEventHandlers_Should_AllBeCalled_When_MultipleHandlersRegistered()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        var callCount = 0;
        _eventManager.RunStarted += (sender, args) => callCount++;
        _eventManager.RunStarted += (sender, args) => callCount++;

        // Act
        _eventManager.RaiseRunStarted(agentId, goal);

        // Assert
        Assert.AreEqual(2, callCount);
    }

    [TestMethod]
    public void NoEventHandlers_Should_NotThrowException_When_NoHandlersRegistered()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";

        // Act & Assert - Should not throw
        _eventManager.RaiseRunStarted(agentId, goal);
    }
}
