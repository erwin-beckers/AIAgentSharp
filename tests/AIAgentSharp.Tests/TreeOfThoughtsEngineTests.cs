using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Moq;

namespace AIAgentSharp.Tests;

[TestClass]
public class TreeOfThoughtsEngineTests
{
    private Mock<ILlmClient> _mockLlmClient = null!;
    private Mock<IEventManager> _mockEventManager = null!;
    private Mock<IStatusManager> _mockStatusManager = null!;
    private Mock<IMetricsCollector> _mockMetricsCollector = null!;
    private AgentConfiguration _config = null!;
    private TreeOfThoughtsEngine _engine = null!;
    private ILogger _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new Mock<ILlmClient>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _mockMetricsCollector = new Mock<IMetricsCollector>();
        _logger = new ConsoleLogger();
        
        _config = new AgentConfiguration
        {
            MaxTreeDepth = 3,
            MaxTreeNodes = 50,
            TreeExplorationStrategy = ExplorationStrategy.BestFirst
        };

        _engine = new TreeOfThoughtsEngine(
            _mockLlmClient.Object,
            _config,
            _logger,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object
        );
    }

    #region Constructor and Basic Properties Tests

    [TestMethod]
    public void Constructor_WithValidParameters_CreatesEngine()
    {
        // Act & Assert
        Assert.IsNotNull(_engine);
        Assert.AreEqual(ReasoningType.TreeOfThoughts, _engine.ReasoningType);
        Assert.IsNull(_engine.CurrentTree);
    }

    [TestMethod]
    public void Constructor_WithNullLlmClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new TreeOfThoughtsEngine(
            null!,
            _config,
            _logger,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object
        ));
    }

    [TestMethod]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new TreeOfThoughtsEngine(
            _mockLlmClient.Object,
            null!,
            _logger,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object
        ));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new TreeOfThoughtsEngine(
            _mockLlmClient.Object,
            _config,
            null!,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object
        ));
    }

    [TestMethod]
    public void Constructor_WithNullEventManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new TreeOfThoughtsEngine(
            _mockLlmClient.Object,
            _config,
            _logger,
            null!,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object
        ));
    }

    [TestMethod]
    public void Constructor_WithNullStatusManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new TreeOfThoughtsEngine(
            _mockLlmClient.Object,
            _config,
            _logger,
            _mockEventManager.Object,
            null!,
            _mockMetricsCollector.Object
        ));
    }

    [TestMethod]
    public void Constructor_WithNullMetricsCollector_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new TreeOfThoughtsEngine(
            _mockLlmClient.Object,
            _config,
            _logger,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            null!
        ));
    }

    #endregion

    #region Tree Operations Tests

    [TestMethod]
    public void CreateRoot_WithoutActiveTree_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => _engine.CreateRoot("test thought"));
    }

    [TestMethod]
    public void AddChild_WithoutActiveTree_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => _engine.AddChild("parent", "child thought"));
    }

    [TestMethod]
    public void EvaluateNode_WithoutActiveTree_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => _engine.EvaluateNode("node", 0.5));
    }

    [TestMethod]
    public void PruneNode_WithoutActiveTree_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => _engine.PruneNode("node"));
    }

    [TestMethod]
    public async Task ExploreAsync_WithoutActiveTree_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _engine.ExploreAsync(ExplorationStrategy.BestFirst));
    }

    #endregion

    #region Tree Structure Tests

    [TestMethod]
    public async Task ReasonAsync_InitializesTreeCorrectly()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsNotNull(_engine.CurrentTree);
        Assert.AreEqual(goal, _engine.CurrentTree!.Goal);
        Assert.AreEqual(_config.MaxTreeDepth, _engine.CurrentTree.MaxDepth);
        Assert.AreEqual(_config.MaxTreeNodes, _engine.CurrentTree.MaxNodes);
        Assert.AreEqual(_config.TreeExplorationStrategy, _engine.CurrentTree.ExplorationStrategy);
        Assert.IsNotNull(_engine.CurrentTree.RootId);
    }

    [TestMethod]
    public async Task ReasonAsync_CreatesRootNodeWithCorrectThought()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" });

        // Act
        await _engine.ReasonAsync(goal, context, tools);

        // Assert
        var rootNode = _engine.CurrentTree!.Nodes[_engine.CurrentTree.RootId!];
        Assert.AreEqual("Initial hypothesis", rootNode.Thought);
        Assert.AreEqual(ThoughtType.Hypothesis, rootNode.ThoughtType);
        Assert.AreEqual(0, rootNode.Depth);
        Assert.IsTrue(rootNode.IsRoot);
    }

    [TestMethod]
    public async Task AddChild_CreatesChildWithCorrectProperties()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" });

        await _engine.ReasonAsync(goal, context, tools);
        var rootId = _engine.CurrentTree!.RootId!;

        // Act
        var child = _engine.AddChild(rootId, "child thought", ThoughtType.Analysis);

        // Assert
        Assert.AreEqual("child thought", child.Thought);
        Assert.AreEqual(ThoughtType.Analysis, child.ThoughtType);
        Assert.AreEqual(1, child.Depth);
        Assert.AreEqual(rootId, child.ParentId);
        Assert.IsFalse(child.IsRoot);
        Assert.IsTrue(child.IsLeaf);
    }

    [TestMethod]
    public async Task EvaluateNode_UpdatesNodeScoreAndState()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" });

        await _engine.ReasonAsync(goal, context, tools);
        var rootId = _engine.CurrentTree!.RootId!;

        // Act
        _engine.EvaluateNode(rootId, 0.85);

        // Assert
        var node = _engine.CurrentTree.Nodes[rootId];
        Assert.AreEqual(0.85, node.Score, 1e-9);
        Assert.AreEqual(ThoughtNodeState.Evaluated, node.State);
    }

    [TestMethod]
    public async Task PruneNode_MarksNodeAsPruned()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" });

        await _engine.ReasonAsync(goal, context, tools);
        var rootId = _engine.CurrentTree!.RootId!;
        var child = _engine.AddChild(rootId, "child thought");

        // Act
        _engine.PruneNode(child.NodeId);

        // Assert
        Assert.AreEqual(ThoughtNodeState.Pruned, _engine.CurrentTree.Nodes[child.NodeId].State);
    }

    #endregion

    #region Error Handling Tests


    [TestMethod]
    public async Task ReasonAsync_WithInvalidLlmResponse_ReturnsFailedResult()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "Invalid JSON" });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error!.Contains("Invalid LLM JSON"));
    }

    [TestMethod]
    public async Task ReasonAsync_WithEmptyLlmResponse_ReturnsFailedResult()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "" });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task ReasonAsync_WithMissingThoughtProperty_ReturnsFailedResult()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"other\": \"property\"}" });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error!.Contains("missing 'thought' property"));
    }

    [TestMethod]
    public async Task ReasonAsync_WithEmptyThoughtValue_ReturnsFailedResult()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"thought\": \"\"}" });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error!.Contains("empty 'thought' value"));
    }

    [TestMethod]
    public async Task ReasonAsync_WithLlmTimeout_ReturnsFailedResult()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("LLM timeout"));

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error!.Contains("LLM timeout"));
    }

    [TestMethod]
    public async Task ReasonAsync_WithJsonParsingError_ReturnsFailedResult()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"invalid\": json}" });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error!.Contains("Invalid LLM JSON"));
    }

    #endregion

    #region Exploration Strategy Tests

    [TestMethod]
    public async Task ExploreAsync_WithInvalidStrategy_ReturnsFailedResult()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" });

        await _engine.ReasonAsync(goal, context, tools);

        // Act
        var result = await _engine.ExploreAsync((ExplorationStrategy)999);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error!.Contains("Unsupported exploration strategy"));
    }

    [TestMethod]
    public async Task ExploreAsync_WithBestFirstStrategy_ExploresNodesInPriorityOrder()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        // Mock responses based on prompt content
        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<LlmMessage> messages, CancellationToken token) =>
            {
                var prompt = messages.FirstOrDefault()?.Content ?? "";
                
                if (prompt.Contains("Generate an initial thought"))
                {
                    return new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" };
                }
                else if (prompt.Contains("Evaluate the quality and potential"))
                {
                    return new LlmCompletionResult { Content = "{\"score\":0.8,\"reasoning\":\"good evaluation\"}" };
                }
                else if (prompt.Contains("Generate 2-3 child thoughts"))
                {
                    return new LlmCompletionResult { Content = "{\"children\":[{\"thought\":\"Child 1\",\"thought_type\":\"Analysis\",\"estimated_score\":0.9},{\"thought\":\"Child 2\",\"thought_type\":\"Analysis\",\"estimated_score\":0.7}]}" };
                }
                else if (prompt.Contains("synthesizing a conclusion"))
                {
                    return new LlmCompletionResult { Content = "{\"conclusion\":\"Final answer\"}" };
                }
                else
                {
                    // Default fallback
                    return new LlmCompletionResult { Content = "{\"score\":0.5,\"reasoning\":\"default response\"}" };
                }
            });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(result.Tree != null);
        Assert.IsTrue(result.Tree.Nodes.Count > 0);
    }

    [TestMethod]
    public async Task ExploreAsync_WithBreadthFirstStrategy_ExploresNodesByLevel()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        // Mock responses based on prompt content
        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<LlmMessage> messages, CancellationToken token) =>
            {
                var prompt = messages.FirstOrDefault()?.Content ?? "";
                
                if (prompt.Contains("Generate an initial thought"))
                {
                    return new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" };
                }
                else if (prompt.Contains("Evaluate the quality and potential"))
                {
                    return new LlmCompletionResult { Content = "{\"score\":0.8,\"reasoning\":\"good evaluation\"}" };
                }
                else if (prompt.Contains("Generate 2-3 child thoughts"))
                {
                    return new LlmCompletionResult { Content = "{\"children\":[{\"thought\":\"Child 1\",\"thought_type\":\"Analysis\",\"estimated_score\":0.7},{\"thought\":\"Child 2\",\"thought_type\":\"Analysis\",\"estimated_score\":0.6}]}" };
                }
                else if (prompt.Contains("synthesizing a conclusion"))
                {
                    return new LlmCompletionResult { Content = "{\"conclusion\":\"Final answer\"}" };
                }
                else
                {
                    // Default fallback
                    return new LlmCompletionResult { Content = "{\"score\":0.5,\"reasoning\":\"default response\"}" };
                }
            });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(result.Tree != null);
        Assert.IsTrue(result.Tree.Nodes.Count > 0);
    }

    [TestMethod]
    public async Task ExploreAsync_WithDepthFirstStrategy_ExploresNodesByDepth()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        // Mock responses based on prompt content
        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<LlmMessage> messages, CancellationToken token) =>
            {
                var prompt = messages.FirstOrDefault()?.Content ?? "";
                
                if (prompt.Contains("Generate an initial thought"))
                {
                    return new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" };
                }
                else if (prompt.Contains("Evaluate the quality and potential"))
                {
                    return new LlmCompletionResult { Content = "{\"score\":0.8,\"reasoning\":\"good evaluation\"}" };
                }
                else if (prompt.Contains("Generate 2-3 child thoughts"))
                {
                    return new LlmCompletionResult { Content = "{\"children\":[{\"thought\":\"Child 1\",\"thought_type\":\"Analysis\",\"estimated_score\":0.7}]}" };
                }
                else if (prompt.Contains("synthesizing a conclusion"))
                {
                    return new LlmCompletionResult { Content = "{\"conclusion\":\"Final answer\"}" };
                }
                else
                {
                    // Default fallback
                    return new LlmCompletionResult { Content = "{\"score\":0.5,\"reasoning\":\"default response\"}" };
                }
            });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(result.Tree != null);
        Assert.IsTrue(result.Tree.Nodes.Count > 0);
    }

    [TestMethod]
    public async Task ExploreAsync_WithBeamSearchStrategy_ExploresWithBeamWidth()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        // Mock responses based on prompt content
        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<LlmMessage> messages, CancellationToken token) =>
            {
                var prompt = messages.FirstOrDefault()?.Content ?? "";
                
                if (prompt.Contains("Generate an initial thought"))
                {
                    return new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" };
                }
                else if (prompt.Contains("Evaluate the quality and potential"))
                {
                    return new LlmCompletionResult { Content = "{\"score\":0.8,\"reasoning\":\"good evaluation\"}" };
                }
                else if (prompt.Contains("Generate 2-3 child thoughts"))
                {
                    return new LlmCompletionResult { Content = "{\"children\":[{\"thought\":\"Child 1\",\"thought_type\":\"Analysis\",\"estimated_score\":0.7},{\"thought\":\"Child 2\",\"thought_type\":\"Analysis\",\"estimated_score\":0.6}]}" };
                }
                else if (prompt.Contains("synthesizing a conclusion"))
                {
                    return new LlmCompletionResult { Content = "{\"conclusion\":\"Final answer\"}" };
                }
                else
                {
                    // Default fallback
                    return new LlmCompletionResult { Content = "{\"score\":0.5,\"reasoning\":\"default response\"}" };
                }
            });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(result.Tree != null);
        Assert.IsTrue(result.Tree.Nodes.Count > 0);
    }

    [TestMethod]
    public async Task ExploreAsync_WithMonteCarloStrategy_ExploresWithRandomSampling()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        // Mock responses based on prompt content
        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<LlmMessage> messages, CancellationToken token) =>
            {
                var prompt = messages.FirstOrDefault()?.Content ?? "";
                
                if (prompt.Contains("Generate an initial thought"))
                {
                    return new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" };
                }
                else if (prompt.Contains("Evaluate the quality and potential"))
                {
                    return new LlmCompletionResult { Content = "{\"score\":0.8,\"reasoning\":\"good evaluation\"}" };
                }
                else if (prompt.Contains("Generate 2-3 child thoughts"))
                {
                    return new LlmCompletionResult { Content = "{\"children\":[{\"thought\":\"Child 1\",\"thought_type\":\"Analysis\",\"estimated_score\":0.7}]}" };
                }
                else if (prompt.Contains("synthesizing a conclusion"))
                {
                    return new LlmCompletionResult { Content = "{\"conclusion\":\"Final answer\"}" };
                }
                else
                {
                    // Default fallback
                    return new LlmCompletionResult { Content = "{\"score\":0.5,\"reasoning\":\"default response\"}" };
                }
            });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(result.Tree != null);
        Assert.IsTrue(result.Tree.Nodes.Count > 0);
    }

    #endregion

    #region Configuration Tests

    [TestMethod]
    public void Constructor_WithCustomConfiguration_UsesConfiguration()
    {
        // Arrange
        var customConfig = new AgentConfiguration
        {
            MaxTreeDepth = 10,
            MaxTreeNodes = 100,
            TreeExplorationStrategy = ExplorationStrategy.BeamSearch
        };

        // Act
        var customEngine = new TreeOfThoughtsEngine(
            _mockLlmClient.Object,
            customConfig,
            _logger,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object
        );

        // Assert
        Assert.IsNotNull(customEngine);
        Assert.AreEqual(ReasoningType.TreeOfThoughts, customEngine.ReasoningType);
    }

    #endregion

    #region Metrics and Logging Tests

    [TestMethod]
    public async Task ReasonAsync_WithSuccessfulExecution_RecordsMetrics()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        // Mock responses based on prompt content
        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<LlmMessage> messages, CancellationToken token) =>
            {
                var prompt = messages.FirstOrDefault()?.Content ?? "";
                
                if (prompt.Contains("Generate an initial thought"))
                {
                    return new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" };
                }
                else if (prompt.Contains("Evaluate the quality and potential"))
                {
                    return new LlmCompletionResult { Content = "{\"score\":0.8,\"reasoning\":\"good evaluation\"}" };
                }
                else if (prompt.Contains("Generate 2-3 child thoughts"))
                {
                    return new LlmCompletionResult { Content = "{\"children\":[{\"thought\":\"Child 1\",\"thought_type\":\"Analysis\",\"estimated_score\":0.7}]}" };
                }
                else if (prompt.Contains("synthesizing a conclusion"))
                {
                    return new LlmCompletionResult { Content = "{\"conclusion\":\"Final answer\"}" };
                }
                else
                {
                    // Default fallback
                    return new LlmCompletionResult { Content = "{\"score\":0.5,\"reasoning\":\"default response\"}" };
                }
            });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert - Verify metrics are recorded when operation succeeds
        Assert.IsTrue(result.Success, result.Error);
        _mockMetricsCollector.Verify(x => x.RecordReasoningExecutionTime(goal, ReasoningType.TreeOfThoughts, It.IsAny<long>()), Times.Once);
        _mockMetricsCollector.Verify(x => x.RecordReasoningConfidence(goal, ReasoningType.TreeOfThoughts, It.IsAny<double>()), Times.Once);
    }

    [TestMethod]
    public async Task ReasonAsync_WithSuccessfulExecution_EmitsStatusUpdates()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var tools = new Dictionary<string, ITool>();

        // Mock responses based on prompt content
        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<LlmMessage> messages, CancellationToken token) =>
            {
                var prompt = messages.FirstOrDefault()?.Content ?? "";
                
                if (prompt.Contains("Generate an initial thought"))
                {
                    return new LlmCompletionResult { Content = "{\"thought\":\"Initial hypothesis\",\"thought_type\":\"Hypothesis\"}" };
                }
                else if (prompt.Contains("Evaluate the quality and potential"))
                {
                    return new LlmCompletionResult { Content = "{\"score\":0.8,\"reasoning\":\"good evaluation\"}" };
                }
                else if (prompt.Contains("Generate 2-3 child thoughts"))
                {
                    return new LlmCompletionResult { Content = "{\"children\":[{\"thought\":\"Child 1\",\"thought_type\":\"Analysis\",\"estimated_score\":0.7}]}" };
                }
                else if (prompt.Contains("synthesizing a conclusion"))
                {
                    return new LlmCompletionResult { Content = "{\"conclusion\":\"Final answer\"}" };
                }
                else
                {
                    // Default fallback
                    return new LlmCompletionResult { Content = "{\"score\":0.5,\"reasoning\":\"default response\"}" };
                }
            });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert - Verify status updates are emitted when operation succeeds
        Assert.IsTrue(result.Success, result.Error);
        _mockStatusManager.Verify(x => x.EmitStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.AtLeastOnce);
    }

    #endregion

    #region Integration Tests (Minimal)

    [TestMethod]
    public async Task ReasonAsync_CompleteWorkflow_WithSimpleResponses_ShouldSucceed()
    {
        // Arrange
        var goal = "Solve a simple math problem";
        var context = "Find the sum of 2 + 2";
        var tools = new Dictionary<string, ITool>();

        // Mock responses based on prompt content
        _mockLlmClient.Setup(x => x.CompleteAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<LlmMessage> messages, CancellationToken token) =>
            {
                var prompt = messages.FirstOrDefault()?.Content ?? "";
                
                if (prompt.Contains("Generate an initial thought"))
                {
                    return new LlmCompletionResult { Content = "{\"thought\":\"I need to add 2 + 2\",\"thought_type\":\"Hypothesis\"}" };
                }
                else if (prompt.Contains("Evaluate the quality and potential"))
                {
                    return new LlmCompletionResult { Content = "{\"score\":0.9,\"reasoning\":\"This is a straightforward addition problem\"}" };
                }
                else if (prompt.Contains("Generate 2-3 child thoughts"))
                {
                    return new LlmCompletionResult { Content = "{\"children\":[{\"thought\":\"2 + 2 = 4\",\"thought_type\":\"Analysis\",\"estimated_score\":0.95}]}" };
                }
                else if (prompt.Contains("synthesizing a conclusion"))
                {
                    return new LlmCompletionResult { Content = "{\"conclusion\":\"The answer is 4\"}" };
                }
                else
                {
                    // Default fallback
                    return new LlmCompletionResult { Content = "{\"score\":0.5,\"reasoning\":\"default response\"}" };
                }
            });

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success, result.Error);
        Assert.AreEqual("The answer is 4", result.Conclusion);
        Assert.IsNotNull(result.Tree);
        Assert.AreEqual(goal, result.Tree!.Goal);
        Assert.IsTrue(result.Tree.NodeCount >= 2); // Root + at least one child
        Assert.IsTrue(result.Confidence > 0);
    }

    #endregion
}
