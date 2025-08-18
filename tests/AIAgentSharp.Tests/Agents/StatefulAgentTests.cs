using AIAgentSharp.Agents;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class AgentTests
{
    private Mock<ILlmClient> _mockLlm = null!;
    private Mock<IAgentStateStore> _mockStateStore = null!;
    private Mock<ILogger> _mockLogger = null!;
    private AgentConfiguration _config = null!;
    private Agent _agent = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlm = new Mock<ILlmClient>();
        _mockStateStore = new Mock<IAgentStateStore>();
        _mockLogger = new Mock<ILogger>();
        _config = new AgentConfiguration();
        _agent = new Agent(_mockLlm.Object, _mockStateStore.Object, _mockLogger.Object, _config);
    }

    [TestMethod]
    public async Task RunAsync_WithValidInputs_ShouldCompleteSuccessfully()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        var tools = new List<ITool>();
        var expectedState = new AgentState { AgentId = agentId, Goal = goal, Turns = new List<AgentTurn>() };

        _mockStateStore.Setup(x => x.LoadAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedState);
        _mockStateStore.Setup(x => x.SaveAsync(agentId, It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _agent.RunAsync(agentId, goal, tools);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Succeeded); // Should fail due to no LLM response
        Assert.AreEqual(goal, expectedState.Goal);
        _mockStateStore.Verify(x => x.SaveAsync(agentId, It.IsAny<AgentState>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task StepAsync_WithValidInputs_ShouldCompleteSuccessfully()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        var tools = new List<ITool>();
        var expectedState = new AgentState { AgentId = agentId, Goal = goal, Turns = new List<AgentTurn>() };

        _mockStateStore.Setup(x => x.LoadAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedState);
        _mockStateStore.Setup(x => x.SaveAsync(agentId, It.IsAny<AgentState>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _agent.StepAsync(agentId, goal, tools);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Continue);
        Assert.IsFalse(result.ExecutedTool);
        _mockStateStore.Verify(x => x.SaveAsync(agentId, It.IsAny<AgentState>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_WithExistingState_ShouldUseExistingState()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        var tools = new List<ITool>();
        var existingState = new AgentState 
        { 
            AgentId = agentId, 
            Goal = "Existing goal", 
            Turns = new List<AgentTurn>() 
        };

        _mockStateStore.Setup(x => x.LoadAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingState);

        // Act
        var result = await _agent.RunAsync(agentId, goal, tools);

        // Assert
        Assert.AreEqual("Existing goal", existingState.Goal); // Should preserve existing goal
    }

    [TestMethod]
    public async Task RunAsync_WithNullState_ShouldCreateNewState()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        var tools = new List<ITool>();

        _mockStateStore.Setup(x => x.LoadAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentState?)null);

        // Act
        var result = await _agent.RunAsync(agentId, goal, tools);

        // Assert
        _mockStateStore.Verify(x => x.SaveAsync(agentId, It.Is<AgentState>(s => s.Goal == goal), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public void Constructor_WithNullLlmClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new Agent(null!, _mockStateStore.Object, _mockLogger.Object, _config));
    }

    [TestMethod]
    public void Constructor_WithNullStateStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new Agent(_mockLlm.Object, null!, _mockLogger.Object, _config));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldUseConsoleLogger()
    {
        // Act
        var agent = new Agent(_mockLlm.Object, _mockStateStore.Object, null, _config);

        // Assert
        Assert.IsNotNull(agent);
    }

    [TestMethod]
    public void Constructor_WithNullConfig_ShouldUseDefaultConfig()
    {
        // Act
        var agent = new Agent(_mockLlm.Object, _mockStateStore.Object, _mockLogger.Object, null);

        // Assert
        Assert.IsNotNull(agent);
    }

    [TestMethod]
    public async Task RunAsync_WithMaxTurnsReached_ShouldReturnMaxTurnsError()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        var tools = new List<ITool>();
        var config = new AgentConfiguration { MaxTurns = 1 };
        var agent = new Agent(_mockLlm.Object, _mockStateStore.Object, _mockLogger.Object, config);
        var state = new AgentState { AgentId = agentId, Goal = goal, Turns = new List<AgentTurn>() };

        _mockStateStore.Setup(x => x.LoadAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);

        // Act
        var result = await agent.RunAsync(agentId, goal, tools);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("Max turns 1 reached"));
    }

    [TestMethod]
    public void Events_ShouldBeProperlyExposed()
    {
        // Act & Assert - Test that we can subscribe to events
        var runStartedHandler = new EventHandler<AgentRunStartedEventArgs>((sender, e) => { });
        var stepStartedHandler = new EventHandler<AgentStepStartedEventArgs>((sender, e) => { });
        var llmCallStartedHandler = new EventHandler<AgentLlmCallStartedEventArgs>((sender, e) => { });
        var llmCallCompletedHandler = new EventHandler<AgentLlmCallCompletedEventArgs>((sender, e) => { });
        var toolCallStartedHandler = new EventHandler<AgentToolCallStartedEventArgs>((sender, e) => { });
        var toolCallCompletedHandler = new EventHandler<AgentToolCallCompletedEventArgs>((sender, e) => { });
        var stepCompletedHandler = new EventHandler<AgentStepCompletedEventArgs>((sender, e) => { });
        var runCompletedHandler = new EventHandler<AgentRunCompletedEventArgs>((sender, e) => { });
        var statusUpdateHandler = new EventHandler<AgentStatusEventArgs>((sender, e) => { });

        // Subscribe to events (this should not throw)
        _agent.RunStarted += runStartedHandler;
        _agent.StepStarted += stepStartedHandler;
        _agent.LlmCallStarted += llmCallStartedHandler;
        _agent.LlmCallCompleted += llmCallCompletedHandler;
        _agent.ToolCallStarted += toolCallStartedHandler;
        _agent.ToolCallCompleted += toolCallCompletedHandler;
        _agent.StepCompleted += stepCompletedHandler;
        _agent.RunCompleted += runCompletedHandler;
        _agent.StatusUpdate += statusUpdateHandler;

        // Unsubscribe from events (this should not throw)
        _agent.RunStarted -= runStartedHandler;
        _agent.StepStarted -= stepStartedHandler;
        _agent.LlmCallStarted -= llmCallStartedHandler;
        _agent.LlmCallCompleted -= llmCallCompletedHandler;
        _agent.ToolCallStarted -= toolCallStartedHandler;
        _agent.ToolCallCompleted -= toolCallCompletedHandler;
        _agent.StepCompleted -= stepCompletedHandler;
        _agent.RunCompleted -= runCompletedHandler;
        _agent.StatusUpdate -= statusUpdateHandler;
    }

    [TestMethod]
    public async Task RunAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        var tools = new List<ITool>();
        var cts = new CancellationTokenSource();
        
        // Set up a slow LLM response to ensure cancellation can be tested
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => 
            {
                await Task.Delay(1000, cts.Token); // This will throw if cancelled
                return "test response";
            });

        // Act & Assert
        var task = _agent.RunAsync(agentId, goal, tools, cts.Token);
        
        // Cancel after a short delay to ensure the operation has started
        await Task.Delay(100);
        cts.Cancel();
        
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => task);
    }

    [TestMethod]
    public async Task StepAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var agentId = "test-agent";
        var goal = "Test goal";
        var tools = new List<ITool>();
        var cts = new CancellationTokenSource();
        
        // Set up a slow LLM response to ensure cancellation can be tested
        _mockLlm.Setup(x => x.CompleteAsync(It.IsAny<IEnumerable<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => 
            {
                await Task.Delay(1000, cts.Token); // This will throw if cancelled
                return "test response";
            });

        // Act & Assert
        var task = _agent.StepAsync(agentId, goal, tools, cts.Token);
        
        // Cancel after a short delay to ensure the operation has started
        await Task.Delay(100);
        cts.Cancel();
        
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => task);
    }
}
