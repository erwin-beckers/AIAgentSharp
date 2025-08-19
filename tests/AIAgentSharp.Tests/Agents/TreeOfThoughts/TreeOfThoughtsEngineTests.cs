using AIAgentSharp.Agents.TreeOfThoughts;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Moq;

namespace AIAgentSharp.Tests.Agents.TreeOfThoughts;

[TestClass]
public class TreeOfThoughtsEngineTests
{
    private Mock<ILlmClient> _mockLlmClient = null!;
    private Mock<ILogger> _mockLogger = null!;
    private Mock<IEventManager> _mockEventManager = null!;
    private Mock<IStatusManager> _mockStatusManager = null!;
    private Mock<IMetricsCollector> _mockMetricsCollector = null!;
    private AgentConfiguration _config = null!;
    private TreeOfThoughtsEngine _engine = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new Mock<ILlmClient>();
        _mockLogger = new Mock<ILogger>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _mockMetricsCollector = new Mock<IMetricsCollector>();
        
        _config = new AgentConfiguration
        {
            EnableReasoningValidation = true,
            MinReasoningConfidence = 0.7,
            MaxTreeDepth = 3,
            MaxTreeNodes = 10
        };

        _engine = new TreeOfThoughtsEngine(
            _mockLlmClient.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
    }

    [TestMethod]
    public void Constructor_Should_ThrowArgumentNullException_When_ConfigIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new TreeOfThoughtsEngine(
                _mockLlmClient.Object,
                null!,
                _mockLogger.Object,
                _mockEventManager.Object,
                _mockStatusManager.Object,
                _mockMetricsCollector.Object));
    }

    [TestMethod]
    public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new TreeOfThoughtsEngine(
                _mockLlmClient.Object,
                _config,
                null!,
                _mockEventManager.Object,
                _mockStatusManager.Object,
                _mockMetricsCollector.Object));
    }

    [TestMethod]
    public void Constructor_Should_ThrowArgumentNullException_When_EventManagerIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new TreeOfThoughtsEngine(
                _mockLlmClient.Object,
                _config,
                _mockLogger.Object,
                null!,
                _mockStatusManager.Object,
                _mockMetricsCollector.Object));
    }

    [TestMethod]
    public void Constructor_Should_ThrowArgumentNullException_When_StatusManagerIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new TreeOfThoughtsEngine(
                _mockLlmClient.Object,
                _config,
                _mockLogger.Object,
                _mockEventManager.Object,
                null!,
                _mockMetricsCollector.Object));
    }

    [TestMethod]
    public void Constructor_Should_ThrowArgumentNullException_When_MetricsCollectorIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new TreeOfThoughtsEngine(
                _mockLlmClient.Object,
                _config,
                _mockLogger.Object,
                _mockEventManager.Object,
                _mockStatusManager.Object,
                null!));
    }

    [TestMethod]
    public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
    {
        // Assert
        Assert.IsNotNull(_engine);
        Assert.AreEqual(ReasoningType.TreeOfThoughts, _engine.ReasoningType);
        Assert.IsNull(_engine.CurrentTree);
    }

    [TestMethod]
    public void CreateRoot_Should_ThrowInvalidOperationException_When_NoActiveTree()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            _engine.CreateRoot("test thought"));
    }

    [TestMethod]
    public void AddChild_Should_ThrowInvalidOperationException_When_NoActiveTree()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            _engine.AddChild("parent-id", "test thought"));
    }

    [TestMethod]
    public void EvaluateNode_Should_ThrowInvalidOperationException_When_NoActiveTree()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            _engine.EvaluateNode("node-id", 0.8));
    }

    [TestMethod]
    public void PruneNode_Should_ThrowInvalidOperationException_When_NoActiveTree()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            _engine.PruneNode("node-id"));
    }

    [TestMethod]
    public async Task ReasonAsync_Should_ReturnFailedResult_When_LlmFails()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        // Mock LLM client to return empty chunks (simulating failure)
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(new List<LlmStreamingChunk>().ToAsyncEnumerable());

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsNotNull(result.Tree);
        Assert.AreEqual(goal, result.Tree!.Goal);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_ReturnSuccessResult_When_AllStepsSucceed()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        // Mock successful responses for root thought generation
        var rootThoughtChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"thought\": \"Test root thought\", \"thought_type\": \"Hypothesis\"}", IsFinal = true }
        };

        // Mock successful responses for child thoughts generation
        var childThoughtsChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"[
                {""thought"": ""Child thought 1"", ""type"": ""Hypothesis""},
                {""thought"": ""Child thought 2"", ""type"": ""Analysis""}
            ]", IsFinal = true }
        };

        // Mock successful responses for evaluation
        var evaluationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"score\": 0.8, \"reasoning\": \"Good evaluation\"}", IsFinal = true }
        };

        // Mock successful responses for conclusion
        var conclusionChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Test conclusion based on tree exploration", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(rootThoughtChunks.ToAsyncEnumerable())     // Root thought generation
            .Returns(childThoughtsChunks.ToAsyncEnumerable())   // Child thoughts generation
            .Returns(evaluationChunks.ToAsyncEnumerable())      // Node evaluation
            .Returns(evaluationChunks.ToAsyncEnumerable())      // Another node evaluation
            .Returns(conclusionChunks.ToAsyncEnumerable());     // Conclusion generation

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Conclusion);
        Assert.IsTrue(result.Confidence > 0);
        Assert.IsNotNull(result.Tree);
        Assert.AreEqual(goal, result.Tree!.Goal);
        Assert.IsNotNull(result.Metadata);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_RecordMetrics_When_ReasoningCompletes()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        var rootThoughtChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"thought\": \"Test root thought\", \"thought_type\": \"Hypothesis\"}", IsFinal = true }
        };

        var childThoughtsChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"children\": [{\"thought\": \"Child thought 1\", \"thought_type\": \"Analysis\", \"estimated_score\": 0.8}]}", IsFinal = true }
        };

        var evaluationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"score\": 0.8, \"reasoning\": \"Good evaluation\"}", IsFinal = true }
        };

        var conclusionChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"conclusion\": \"Test conclusion\"}", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(rootThoughtChunks.ToAsyncEnumerable())
            .Returns(childThoughtsChunks.ToAsyncEnumerable())
            .Returns(evaluationChunks.ToAsyncEnumerable())
            .Returns(conclusionChunks.ToAsyncEnumerable());

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        _mockMetricsCollector.Verify(x => x.RecordReasoningExecutionTime("Test goal", ReasoningType.TreeOfThoughts, It.IsAny<long>()), Times.Once);
        _mockMetricsCollector.Verify(x => x.RecordReasoningConfidence("Test goal", ReasoningType.TreeOfThoughts, It.IsAny<double>()), Times.Once);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_HandleCancellation_When_CancellationTokenIsCancelled()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools, cancellationTokenSource.Token);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_HandleException_When_LlmThrows()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual("Test exception", result.Error);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Tree of Thoughts reasoning failed"))), Times.Once);
    }

    [TestMethod]
    public async Task ExploreAsync_Should_ThrowInvalidOperationException_When_NoActiveTree()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _engine.ExploreAsync(ExplorationStrategy.BestFirst));
    }

    [TestMethod]
    public async Task ReasonAsync_Should_CreateTreeAndAllowOperations_When_TreeIsActive()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        var rootThoughtChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"thought\": \"Test root thought\", \"thought_type\": \"Hypothesis\"}", IsFinal = true }
        };

        var childThoughtsChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"children\": [{\"thought\": \"Child thought 1\", \"thought_type\": \"Analysis\", \"estimated_score\": 0.8}]}", IsFinal = true }
        };

        var evaluationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"score\": 0.9, \"reasoning\": \"Good evaluation\"}", IsFinal = true }
        };

        var conclusionChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"conclusion\": \"Test conclusion\"}", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(rootThoughtChunks.ToAsyncEnumerable())
            .Returns(childThoughtsChunks.ToAsyncEnumerable())
            .Returns(evaluationChunks.ToAsyncEnumerable())
            .Returns(conclusionChunks.ToAsyncEnumerable());

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(_engine.CurrentTree);
        Assert.AreEqual(goal, _engine.CurrentTree!.Goal);
        
        // Test tree operations - these should now work without throwing exceptions
        // Get the existing root node that was created by ReasonAsync
        var existingRootId = _engine.CurrentTree!.RootId;
        Assert.IsNotNull(existingRootId);
        
        var childNode = _engine.AddChild(existingRootId!, "Child thought", ThoughtType.Analysis);
        Assert.IsNotNull(childNode);
        Assert.AreEqual("Child thought", childNode.Thought);
        Assert.AreEqual(existingRootId, childNode.ParentId);
        
        // Test evaluation
        _engine.EvaluateNode(childNode.NodeId, 0.75);
        Assert.AreEqual(0.75, childNode.Score);
        
        // Test pruning
        _engine.PruneNode(childNode.NodeId);
        Assert.AreEqual(ThoughtNodeState.Pruned, childNode.State);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_UseSpecifiedExplorationStrategy_When_StrategyProvided()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        var rootThoughtChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"thought\": \"Test root thought\", \"thought_type\": \"Hypothesis\"}", IsFinal = true }
        };

        var childThoughtsChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"children\": [{\"thought\": \"Child thought 1\", \"thought_type\": \"Analysis\", \"estimated_score\": 0.8}]}", IsFinal = true }
        };

        var evaluationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"score\": 0.8, \"reasoning\": \"Good evaluation\"}", IsFinal = true }
        };

        var conclusionChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "{\"conclusion\": \"Test conclusion\"}", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(rootThoughtChunks.ToAsyncEnumerable())
            .Returns(childThoughtsChunks.ToAsyncEnumerable())
            .Returns(evaluationChunks.ToAsyncEnumerable())
            .Returns(conclusionChunks.ToAsyncEnumerable())
            // Second engine calls
            .Returns(rootThoughtChunks.ToAsyncEnumerable())
            .Returns(childThoughtsChunks.ToAsyncEnumerable())
            .Returns(evaluationChunks.ToAsyncEnumerable())
            .Returns(conclusionChunks.ToAsyncEnumerable());

        // Act - Test reasoning (exploration strategy is set in config)
        var result1 = await _engine.ReasonAsync(goal, context, tools);
        Assert.IsTrue(result1.Success);

        // Test with different configuration
        var config2 = new AgentConfiguration { TreeExplorationStrategy = ExplorationStrategy.BreadthFirst };
        var engine2 = new TreeOfThoughtsEngine(_mockLlmClient.Object, config2, _mockLogger.Object, _mockEventManager.Object, _mockStatusManager.Object, _mockMetricsCollector.Object);
        var result2 = await engine2.ReasonAsync(goal, context, tools);
        Assert.IsTrue(result2.Success);
    }
}
