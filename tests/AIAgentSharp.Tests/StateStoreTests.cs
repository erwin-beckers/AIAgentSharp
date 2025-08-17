namespace AIAgentSharp.Tests;

[TestClass]
public sealed class StateStoreTests
{
    private string _tempDirectory = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"agent-test-{Guid.NewGuid()}");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [TestMethod]
    public async Task FileAgentStateStore_LoadAsync_LogsErrorsOnFailure()
    {
        // Arrange
        var logger = new TestLogger();
        var tempDir = Path.Combine(Path.GetTempPath(), "FileAgentStateStoreTest");
        Directory.CreateDirectory(tempDir);
        var store = new FileAgentStateStore(tempDir, logger);

        // Create a corrupted file that will cause an exception when trying to read
        var filePath = Path.Combine(tempDir, "test-agent.jsonl");
        await File.WriteAllTextAsync(filePath, "invalid json content");

        // Act
        var result = await store.LoadAsync("test-agent");

        // Assert
        Assert.IsNull(result);
        Assert.IsTrue(logger.ErrorMessages.Count > 0);
        Assert.IsTrue(logger.ErrorMessages.Any(msg => msg.Contains("Failed to load agent state")));

        // Cleanup
        try
        {
            File.Delete(filePath);
        }
        catch
        {
        }

        try
        {
            Directory.Delete(tempDir);
        }
        catch
        {
        }
    }

    [TestMethod]
    public async Task MemoryAgentStateStore_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var store = new MemoryAgentStateStore();
        var agentId = "concurrent-test";
        var initialState = new AgentState { AgentId = agentId, Goal = "Test concurrent access" };

        // Save initial state
        await store.SaveAsync(agentId, initialState);

        // Act - Perform concurrent load and save operations
        var tasks = new List<Task>();
        var random = new Random();

        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                // Randomly choose between load and save operations
                if (random.Next(2) == 0)
                {
                    // Load operation
                    var loadedState = await store.LoadAsync(agentId);
                    Assert.IsNotNull(loadedState);
                    Assert.AreEqual(agentId, loadedState.AgentId);
                }
                else
                {
                    // Save operation
                    var stateToSave = new AgentState
                    {
                        AgentId = agentId,
                        Goal = $"Updated goal {Guid.NewGuid()}"
                    };
                    await store.SaveAsync(agentId, stateToSave);
                }
            }));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - Final state should be consistent
        var finalState = await store.LoadAsync(agentId);
        Assert.IsNotNull(finalState);
        Assert.AreEqual(agentId, finalState.AgentId);
    }

    [TestClass]
    public sealed class MemoryAgentStateStoreTests
    {
        [TestMethod]
        public async Task LoadAsync_NonExistentAgent_ReturnsNull()
        {
            // Arrange
            var store = new MemoryAgentStateStore();

            // Act
            var result = await store.LoadAsync("non-existent-agent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveAsync_AndLoadAsync_ReturnsSavedState()
        {
            // Arrange
            var store = new MemoryAgentStateStore();
            var agentId = "test-agent";
            var state = new AgentState
            {
                AgentId = agentId,
                Goal = "test goal",
                Turns = new List<AgentTurn> { new() { Index = 0 } },
                UpdatedUtc = DateTimeOffset.UtcNow
            };

            // Act
            await store.SaveAsync(agentId, state);
            var loadedState = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.AreEqual(state.AgentId, loadedState.AgentId);
            Assert.AreEqual(state.Goal, loadedState.Goal);
            Assert.AreEqual(state.Turns.Count, loadedState.Turns.Count);
            Assert.AreEqual(state.UpdatedUtc, loadedState.UpdatedUtc);
        }

        [TestMethod]
        public async Task SaveAsync_OverwritesExistingState()
        {
            // Arrange
            var store = new MemoryAgentStateStore();
            var agentId = "test-agent";
            var state1 = new AgentState { AgentId = agentId, Goal = "goal 1" };
            var state2 = new AgentState { AgentId = agentId, Goal = "goal 2" };

            // Act
            await store.SaveAsync(agentId, state1);
            await store.SaveAsync(agentId, state2);
            var loadedState = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.AreEqual("goal 2", loadedState.Goal);
        }

        [TestMethod]
        public async Task LoadAsync_ReturnsSameReference()
        {
            // Arrange
            var store = new MemoryAgentStateStore();
            var agentId = "test-agent";
            var state = new AgentState { AgentId = agentId, Goal = "test goal" };

            // Act
            await store.SaveAsync(agentId, state);
            var loadedState1 = await store.LoadAsync(agentId);
            var loadedState2 = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState1);
            Assert.IsNotNull(loadedState2);
            Assert.AreSame(loadedState1, loadedState2);
            Assert.AreEqual(loadedState1.AgentId, loadedState2.AgentId);
            Assert.AreEqual(loadedState1.Goal, loadedState2.Goal);
        }

        [TestMethod]
        public async Task SaveAsync_StoresReference()
        {
            // Arrange
            var store = new MemoryAgentStateStore();
            var agentId = "test-agent";
            var state = new AgentState { AgentId = agentId, Goal = "test goal" };

            // Act
            await store.SaveAsync(agentId, state);
            state.Goal = "modified goal";
            var loadedState = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.AreEqual("modified goal", loadedState.Goal);
        }

        [TestMethod]
        public async Task ConcurrentAccess_IsThreadSafe()
        {
            // Arrange
            var store = new MemoryAgentStateStore();
            var agentId = "test-agent";
            var tasks = new List<Task>();

            // Act
            for (var i = 0; i < 10; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    var state = new AgentState { AgentId = agentId, Goal = $"goal {index}" };
                    await store.SaveAsync(agentId, state);
                    await store.LoadAsync(agentId);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var finalState = await store.LoadAsync(agentId);
            Assert.IsNotNull(finalState);
        }

        [TestMethod]
        public async Task MultipleAgents_AreStoredSeparately()
        {
            // Arrange
            var store = new MemoryAgentStateStore();
            var state1 = new AgentState { AgentId = "agent-1", Goal = "goal 1" };
            var state2 = new AgentState { AgentId = "agent-2", Goal = "goal 2" };

            // Act
            await store.SaveAsync("agent-1", state1);
            await store.SaveAsync("agent-2", state2);
            var loadedState1 = await store.LoadAsync("agent-1");
            var loadedState2 = await store.LoadAsync("agent-2");

            // Assert
            Assert.IsNotNull(loadedState1);
            Assert.IsNotNull(loadedState2);
            Assert.AreEqual("goal 1", loadedState1.Goal);
            Assert.AreEqual("goal 2", loadedState2.Goal);
        }
    }

    [TestClass]
    public sealed class FileAgentStateStoreTests
    {
        private string _tempDirectory = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"agent-file-test-{Guid.NewGuid()}");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [TestMethod]
        public async Task LoadAsync_NonExistentAgent_ReturnsNull()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);

            // Act
            var result = await store.LoadAsync("non-existent-agent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveAsync_AndLoadAsync_ReturnsSavedState()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);
            var agentId = "test-agent";
            var state = new AgentState
            {
                AgentId = agentId,
                Goal = "test goal",
                Turns = new List<AgentTurn>
                {
                    new() { Index = 0, LlmMessage = new ModelMessage { Thoughts = "test", ActionRaw = "plan" } }
                },
                UpdatedUtc = DateTimeOffset.UtcNow
            };

            // Act
            await store.SaveAsync(agentId, state);
            var loadedState = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.AreEqual(state.AgentId, loadedState.AgentId);
            Assert.AreEqual(state.Goal, loadedState.Goal);
            Assert.AreEqual(state.Turns.Count, loadedState.Turns.Count);
        }

        [TestMethod]
        public async Task SaveAsync_OverwritesExistingState()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);
            var agentId = "test-agent";
            var state1 = new AgentState { AgentId = agentId, Goal = "goal 1" };
            var state2 = new AgentState { AgentId = agentId, Goal = "goal 2" };

            // Act
            await store.SaveAsync(agentId, state1);
            await store.SaveAsync(agentId, state2);
            var loadedState = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.AreEqual("goal 2", loadedState.Goal);
        }

        [TestMethod]
        public async Task SaveAsync_CreatesDirectoryIfNotExists()
        {
            // Arrange
            var nonExistentDirectory = Path.Combine(_tempDirectory, "subdir");
            var store = new FileAgentStateStore(nonExistentDirectory);
            var state = new AgentState { AgentId = "test-agent", Goal = "test goal" };

            // Act
            await store.SaveAsync("test-agent", state);

            // Assert
            Assert.IsTrue(Directory.Exists(nonExistentDirectory));
        }

        [TestMethod]
        public async Task LoadAsync_EmptyFile_ReturnsNull()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);
            var agentId = "test-agent";
            var filePath = Path.Combine(_tempDirectory, $"{agentId}.jsonl");

            // Create empty file
            Directory.CreateDirectory(_tempDirectory);
            File.WriteAllText(filePath, "");

            // Act
            var result = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsync_InvalidFile_ReturnsNull()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);
            var agentId = "test-agent";
            var filePath = Path.Combine(_tempDirectory, $"{agentId}.jsonl");

            // Create invalid file with invalid header
            Directory.CreateDirectory(_tempDirectory);
            File.WriteAllText(filePath, "invalid json content");

            // Act
            var result = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveAsync_HandlesValidFileName()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);
            var agentId = "test-agent-with-valid-chars";
            var state = new AgentState { AgentId = agentId, Goal = "test goal" };

            // Act
            await store.SaveAsync(agentId, state);
            var loadedState = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.AreEqual(agentId, loadedState.AgentId);
            Assert.AreEqual("test goal", loadedState.Goal);
        }

        [TestMethod]
        public async Task MultipleAgents_AreStoredSeparately()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);
            var state1 = new AgentState { AgentId = "agent-1", Goal = "goal 1" };
            var state2 = new AgentState { AgentId = "agent-2", Goal = "goal 2" };

            // Act
            await store.SaveAsync("agent-1", state1);
            await store.SaveAsync("agent-2", state2);
            var loadedState1 = await store.LoadAsync("agent-1");
            var loadedState2 = await store.LoadAsync("agent-2");

            // Assert
            Assert.IsNotNull(loadedState1);
            Assert.IsNotNull(loadedState2);
            Assert.AreEqual("goal 1", loadedState1.Goal);
            Assert.AreEqual("goal 2", loadedState2.Goal);
        }

        [TestMethod]
        public async Task SaveAsync_WithComplexState_SerializesCorrectly()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);
            var agentId = "test-agent";
            var state = new AgentState
            {
                AgentId = agentId,
                Goal = "test goal",
                Turns = new List<AgentTurn>
                {
                    new()
                    {
                        Index = 0,
                        LlmMessage = new ModelMessage
                        {
                            Thoughts = "test thoughts",
                            Action = AgentAction.ToolCall,
                            ActionInput = new ActionInput
                            {
                                Summary = "test summary",
                                Tool = "test-tool",
                                Params = new Dictionary<string, object?> { { "key", "value" } }
                            }
                        },
                        ToolCall = new ToolCallRequest
                        {
                            Tool = "test-tool",
                            Params = new Dictionary<string, object?> { { "key", "value" } }
                        },
                        ToolResult = new ToolExecutionResult
                        {
                            Success = true,
                            Output = "test output",
                            Tool = "test-tool",
                            Params = new Dictionary<string, object?> { { "key", "value" } }
                        }
                    }
                },
                UpdatedUtc = DateTimeOffset.UtcNow
            };

            // Act
            await store.SaveAsync(agentId, state);
            var loadedState = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.AreEqual(state.AgentId, loadedState.AgentId);
            Assert.AreEqual(state.Goal, loadedState.Goal);
            Assert.AreEqual(state.Turns.Count, loadedState.Turns.Count);

            var turn = loadedState.Turns[0];
            Assert.IsNotNull(turn.LlmMessage);
            Assert.AreEqual("test thoughts", turn.LlmMessage.Thoughts);
            Assert.AreEqual(AgentAction.ToolCall, turn.LlmMessage.Action);
            Assert.AreEqual("test-tool", turn.LlmMessage.ActionInput.Tool);

            Assert.IsNotNull(turn.ToolCall);
            Assert.AreEqual("test-tool", turn.ToolCall.Tool);

            Assert.IsNotNull(turn.ToolResult);
            Assert.IsTrue(turn.ToolResult.Success);
            Assert.AreEqual("test output", turn.ToolResult.Output?.ToString());
        }

        [TestMethod]
        public async Task SaveAsync_UpdatesHeaderTimestamp()
        {
            // Arrange
            var store = new FileAgentStateStore(_tempDirectory);
            var agentId = "test-agent";
            var state = new AgentState { AgentId = agentId, Goal = "test goal" };

            // Act
            await store.SaveAsync(agentId, state);
            await Task.Delay(100); // Ensure timestamp difference
            await store.SaveAsync(agentId, state);
            var loadedState = await store.LoadAsync(agentId);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.IsTrue(loadedState.UpdatedUtc > DateTimeOffset.UtcNow.AddSeconds(-1));
        }
    }
}

// Test logger for capturing log messages
public class TestLogger : ILogger
{
    public List<string> InformationMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public List<string> ErrorMessages { get; } = new();
    public List<string> DebugMessages { get; } = new();

    public void LogInformation(string message)
    {
        InformationMessages.Add(message);
    }

    public void LogWarning(string message)
    {
        WarningMessages.Add(message);
    }

    public void LogError(string message)
    {
        ErrorMessages.Add(message);
    }

    public void LogDebug(string message)
    {
        DebugMessages.Add(message);
    }
}