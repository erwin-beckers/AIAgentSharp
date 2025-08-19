namespace AIAgentSharp.Tests.StateStores;

[TestClass]
public class MemoryAgentStateStoreTests
{
    private MemoryAgentStateStore _stateStore = null!;

    [TestInitialize]
    public void Setup()
    {
        _stateStore = new MemoryAgentStateStore();
    }

    [TestMethod]
    public void Constructor_Should_CreateMemoryAgentStateStore_When_Called()
    {
        // Act & Assert
        Assert.IsNotNull(_stateStore);
        Assert.IsInstanceOfType(_stateStore, typeof(IAgentStateStore));
    }

    [TestMethod]
    public async Task LoadAsync_Should_ReturnNull_When_AgentIdDoesNotExist()
    {
        // Arrange
        var agentId = "non-existent-agent";

        // Act
        var result = await _stateStore.LoadAsync(agentId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SaveAsync_Should_SaveState_When_ValidParametersProvided()
    {
        // Arrange
        var agentId = "test-agent";
        var state = new AgentState
        {
            AgentId = agentId,
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        // Act
        await _stateStore.SaveAsync(agentId, state);

        // Assert
        var loadedState = await _stateStore.LoadAsync(agentId);
        Assert.IsNotNull(loadedState);
        Assert.AreEqual(agentId, loadedState.AgentId);
        Assert.AreEqual("Test goal", loadedState.Goal);
    }

    [TestMethod]
    public async Task SaveAsync_Should_OverwriteExistingState_When_AgentIdAlreadyExists()
    {
        // Arrange
        var agentId = "test-agent";
        var initialState = new AgentState
        {
            AgentId = agentId,
            Goal = "Initial goal",
            Turns = new List<AgentTurn>()
        };
        var updatedState = new AgentState
        {
            AgentId = agentId,
            Goal = "Updated goal",
            Turns = new List<AgentTurn>()
        };

        // Act
        await _stateStore.SaveAsync(agentId, initialState);
        await _stateStore.SaveAsync(agentId, updatedState);

        // Assert
        var loadedState = await _stateStore.LoadAsync(agentId);
        Assert.IsNotNull(loadedState);
        Assert.AreEqual("Updated goal", loadedState.Goal);
    }

    [TestMethod]
    public async Task LoadAsync_Should_ReturnSavedState_When_StateWasSaved()
    {
        // Arrange
        var agentId = "test-agent";
        var state = new AgentState
        {
            AgentId = agentId,
            Goal = "Test goal",
            Turns = new List<AgentTurn>
            {
                new AgentTurn
                {
                    Index = 0,
                    TurnId = "turn-1"
                }
            }
        };

        // Act
        await _stateStore.SaveAsync(agentId, state);
        var result = await _stateStore.LoadAsync(agentId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(agentId, result.AgentId);
        Assert.AreEqual("Test goal", result.Goal);
        Assert.AreEqual(1, result.Turns.Count);
        Assert.AreEqual(0, result.Turns[0].Index);
        Assert.AreEqual("turn-1", result.Turns[0].TurnId);
    }

    [TestMethod]
    public async Task SaveAsync_Should_HandleMultipleAgents_When_DifferentAgentIds()
    {
        // Arrange
        var agentId1 = "agent-1";
        var agentId2 = "agent-2";
        var state1 = new AgentState { AgentId = agentId1, Goal = "Goal 1" };
        var state2 = new AgentState { AgentId = agentId2, Goal = "Goal 2" };

        // Act
        await _stateStore.SaveAsync(agentId1, state1);
        await _stateStore.SaveAsync(agentId2, state2);

        // Assert
        var loadedState1 = await _stateStore.LoadAsync(agentId1);
        var loadedState2 = await _stateStore.LoadAsync(agentId2);

        Assert.IsNotNull(loadedState1);
        Assert.IsNotNull(loadedState2);
        Assert.AreEqual("Goal 1", loadedState1.Goal);
        Assert.AreEqual("Goal 2", loadedState2.Goal);
    }

    [TestMethod]
    public async Task LoadAsync_Should_NotThrow_When_CancellationTokenCancelled()
    {
        // Arrange
        var agentId = "test-agent";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // The MemoryAgentStateStore doesn't check cancellation tokens, so this should not throw
        var result = await _stateStore.LoadAsync(agentId, cts.Token);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SaveAsync_Should_NotThrow_When_CancellationTokenCancelled()
    {
        // Arrange
        var agentId = "test-agent";
        var state = new AgentState { AgentId = agentId, Goal = "Test goal" };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // The MemoryAgentStateStore doesn't check cancellation tokens, so this should not throw
        await _stateStore.SaveAsync(agentId, state, cts.Token);
        
        // Verify the state was still saved
        var loadedState = await _stateStore.LoadAsync(agentId);
        Assert.IsNotNull(loadedState);
        Assert.AreEqual("Test goal", loadedState.Goal);
    }

    [TestMethod]
    public async Task StateStore_Should_BeThreadSafe_When_MultipleThreadsAccess()
    {
        // Arrange
        var tasks = new List<Task>();
        var agentIds = new List<string>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var agentId = $"agent-{i}";
            agentIds.Add(agentId);
            var state = new AgentState { AgentId = agentId, Goal = $"Goal {i}" };

            tasks.Add(Task.Run(async () =>
            {
                await _stateStore.SaveAsync(agentId, state);
                var loadedState = await _stateStore.LoadAsync(agentId);
                Assert.IsNotNull(loadedState);
                Assert.AreEqual(agentId, loadedState.AgentId);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var agentId in agentIds)
        {
            var state = await _stateStore.LoadAsync(agentId);
            Assert.IsNotNull(state);
            Assert.AreEqual(agentId, state.AgentId);
        }
    }

    [TestMethod]
    public async Task SaveAsync_Should_HandleNullState_When_NullStateProvided()
    {
        // Arrange
        var agentId = "test-agent";
        AgentState? state = null;

        // Act & Assert
        // This should not throw, but the behavior depends on the implementation
        // For now, we'll just verify it doesn't crash
        await _stateStore.SaveAsync(agentId, state!);
    }

    [TestMethod]
    public async Task SaveAsync_Should_HandleEmptyAgentId_When_EmptyAgentIdProvided()
    {
        // Arrange
        var agentId = "";
        var state = new AgentState { AgentId = agentId, Goal = "Test goal" };

        // Act
        await _stateStore.SaveAsync(agentId, state);

        // Assert
        var loadedState = await _stateStore.LoadAsync(agentId);
        Assert.IsNotNull(loadedState);
        Assert.AreEqual("", loadedState.AgentId);
    }

    [TestMethod]
    public async Task LoadAsync_Should_HandleEmptyAgentId_When_EmptyAgentIdProvided()
    {
        // Arrange
        var agentId = "";

        // Act
        var result = await _stateStore.LoadAsync(agentId);

        // Assert
        Assert.IsNull(result);
    }
}
