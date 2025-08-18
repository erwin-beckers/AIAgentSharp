using AIAgentSharp.Agents;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class EventManagerTests
{
    private Mock<ILogger> _mockLogger = null!;
    private EventManager _eventManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _eventManager = new EventManager(_mockLogger.Object);
    }

    [TestMethod]
    public void RaiseRunStarted_ShouldInvokeEvent()
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
    public void RaiseStepStarted_ShouldInvokeEvent()
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
    public void RaiseLlmCallStarted_ShouldInvokeEvent()
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
    public void RaiseLlmCallCompleted_WithMessage_ShouldInvokeEvent()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 2;
        var modelMessage = new ModelMessage { Thoughts = "Test thoughts" };
        AgentLlmCallCompletedEventArgs? capturedArgs = null;
        _eventManager.LlmCallCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, modelMessage);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.AreEqual(modelMessage, capturedArgs.LlmMessage);
        Assert.IsNull(capturedArgs.Error);
    }

    [TestMethod]
    public void RaiseLlmCallCompleted_WithError_ShouldInvokeEvent()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 1;
        var error = "Test error";
        AgentLlmCallCompletedEventArgs? capturedArgs = null;
        _eventManager.LlmCallCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseLlmCallCompleted(agentId, turnIndex, null, error);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.IsNull(capturedArgs.LlmMessage);
        Assert.AreEqual(error, capturedArgs.Error);
    }

    [TestMethod]
    public void RaiseToolCallStarted_ShouldInvokeEvent()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 4;
        var toolName = "test_tool";
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
    public void RaiseToolCallCompleted_WithSuccess_ShouldInvokeEvent()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 6;
        var toolName = "test_tool";
        var output = new { result = "success" };
        var executionTime = TimeSpan.FromMilliseconds(100);
        AgentToolCallCompletedEventArgs? capturedArgs = null;
        _eventManager.ToolCallCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, true, output, null, executionTime);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.AreEqual(toolName, capturedArgs.ToolName);
        Assert.IsTrue(capturedArgs.Success);
        Assert.AreEqual(output, capturedArgs.Output);
        Assert.AreEqual(executionTime, capturedArgs.ExecutionTime);
    }

    [TestMethod]
    public void RaiseToolCallCompleted_WithError_ShouldInvokeEvent()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 7;
        var toolName = "test_tool";
        var error = "Tool failed";
        AgentToolCallCompletedEventArgs? capturedArgs = null;
        _eventManager.ToolCallCompleted += (sender, args) => capturedArgs = args;

        // Act
        _eventManager.RaiseToolCallCompleted(agentId, turnIndex, toolName, false, null, error);

        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(agentId, capturedArgs.AgentId);
        Assert.AreEqual(turnIndex, capturedArgs.TurnIndex);
        Assert.AreEqual(toolName, capturedArgs.ToolName);
        Assert.IsFalse(capturedArgs.Success);
        Assert.AreEqual(error, capturedArgs.Error);
    }

    [TestMethod]
    public void RaiseStepCompleted_ShouldInvokeEvent()
    {
        // Arrange
        var agentId = "test-agent";
        var turnIndex = 8;
        var stepResult = new AgentStepResult { Continue = true, ExecutedTool = false };
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
    }

    [TestMethod]
    public void RaiseRunCompleted_ShouldInvokeEvent()
    {
        // Arrange
        var agentId = "test-agent";
        var succeeded = true;
        var finalOutput = "Task completed";
        var error = (string?)null;
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
    public void EventHandlers_WithExceptions_ShouldNotThrow()
    {
        // Arrange
        _eventManager.RunStarted += (sender, args) => throw new Exception("Test exception");
        _eventManager.StepStarted += (sender, args) => throw new Exception("Test exception");
        _eventManager.LlmCallStarted += (sender, args) => throw new Exception("Test exception");
        _eventManager.LlmCallCompleted += (sender, args) => throw new Exception("Test exception");
        _eventManager.ToolCallStarted += (sender, args) => throw new Exception("Test exception");
        _eventManager.ToolCallCompleted += (sender, args) => throw new Exception("Test exception");
        _eventManager.StepCompleted += (sender, args) => throw new Exception("Test exception");
        _eventManager.RunCompleted += (sender, args) => throw new Exception("Test exception");

        // Act & Assert - Should not throw exceptions
        _eventManager.RaiseRunStarted("test", "goal");
        _eventManager.RaiseStepStarted("test", 1);
        _eventManager.RaiseLlmCallStarted("test", 1);
        _eventManager.RaiseLlmCallCompleted("test", 1, null);
        _eventManager.RaiseToolCallStarted("test", 1, "tool", new Dictionary<string, object?>());
        _eventManager.RaiseToolCallCompleted("test", 1, "tool", true);
        _eventManager.RaiseStepCompleted("test", 1, new AgentStepResult());
        _eventManager.RaiseRunCompleted("test", true, "output", null, 1);

        // Verify that exceptions were logged
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("Test exception"))), Times.Exactly(8));
    }

    [TestMethod]
    public void Events_WithNoHandlers_ShouldNotThrow()
    {
        // Act & Assert - Should not throw when no handlers are attached
        _eventManager.RaiseRunStarted("test", "goal");
        _eventManager.RaiseStepStarted("test", 1);
        _eventManager.RaiseLlmCallStarted("test", 1);
        _eventManager.RaiseLlmCallCompleted("test", 1, null);
        _eventManager.RaiseToolCallStarted("test", 1, "tool", new Dictionary<string, object?>());
        _eventManager.RaiseToolCallCompleted("test", 1, "tool", true);
        _eventManager.RaiseStepCompleted("test", 1, new AgentStepResult());
        _eventManager.RaiseRunCompleted("test", true, "output", null, 1);
    }

    [TestMethod]
    public void MultipleEventHandlers_ShouldAllBeInvoked()
    {
        // Arrange
        var callCount = 0;
        _eventManager.RunStarted += (sender, args) => callCount++;
        _eventManager.RunStarted += (sender, args) => callCount++;

        // Act
        _eventManager.RaiseRunStarted("test", "goal");

        // Assert
        Assert.AreEqual(2, callCount);
    }

    [TestMethod]
    public void EventRemoval_ShouldWorkCorrectly()
    {
        // Arrange
        var callCount = 0;
        EventHandler<AgentRunStartedEventArgs> handler = (sender, args) => callCount++;
        _eventManager.RunStarted += handler;

        // Act
        _eventManager.RaiseRunStarted("test", "goal");
        _eventManager.RunStarted -= handler;
        _eventManager.RaiseRunStarted("test", "goal");

        // Assert
        Assert.AreEqual(1, callCount);
    }
}
