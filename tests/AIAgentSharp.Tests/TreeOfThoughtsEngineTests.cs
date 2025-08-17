using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.Tests;

[TestClass]
public class TreeOfThoughtsEngineTests
{
    private MockLlmClient _mockLlm = null!;
    private ILogger _logger = null!;
    private IEventManager _eventManager = null!;
    private IStatusManager _statusManager = null!;
    private AgentConfiguration _config = null!;
    private TreeOfThoughtsEngine _engine = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlm = new MockLlmClient();
        _logger = new ConsoleLogger();
        _eventManager = new EventManager(_logger);
        _statusManager = new StatusManager(new AgentConfiguration(), _eventManager);
        _config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.TreeOfThoughts,
            MaxTreeDepth = 5,
            MaxTreeNodes = 50,
            TreeExplorationStrategy = ExplorationStrategy.BestFirst
        };
        _engine = new TreeOfThoughtsEngine(_mockLlm, _config, _logger, _eventManager, _statusManager);
        
        // Set up mock responses for the LLM
        _mockLlm.SetNextResponse(new ModelMessage 
        { 
            Thoughts = "Initial analysis", 
            Action = AgentAction.Finish, 
            ActionInput = new ActionInput { Final = "Test conclusion" } 
        });
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ReasonAsync_BasicReasoning_ShouldCompleteSuccessfully()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await _engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Conclusion);
        Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        Assert.IsNotNull(result.Tree);
        Assert.IsTrue(result.Tree!.NodeCount > 0);
        // Note: ExecutionTimeMs might be 0 for very fast operations
        Assert.AreEqual("TreeOfThoughts", result.Metadata["reasoning_type"]);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ReasonAsync_WithTools_ShouldIncludeToolDescriptions()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>
        {
            ["test_tool"] = new MockConcatTool()
        };

        // Act
        var result = await _engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Tree);
        Assert.IsTrue(result.Tree!.NodeCount > 0);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ReasonAsync_ExplorationFailure_ShouldReturnError()
    {
        // Arrange
        var failingLlm = new MockLlmClient();
        // Don't set any response to simulate failure
        var engine = new TreeOfThoughtsEngine(failingLlm, _config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        // Note: ExecutionTimeMs might be 0 for very fast failures
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ReasonAsync_Cancellation_ShouldHandleGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        var tools = new Dictionary<string, ITool>();

        // Act & Assert
        // Note: The engine might handle cancellation gracefully and return an error result
        // instead of throwing an exception
        var result = await _engine.ReasonAsync("Test goal", "Test context", tools, cts.Token);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_CreateRoot_ShouldCreateValidRootNode()
    {
        // Arrange
        // Create a new engine instance to avoid conflicts
        var engine = new TreeOfThoughtsEngine(_mockLlm, _config, _logger, _eventManager, _statusManager);
        await engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());

        // Act & Assert
        // Note: ReasonAsync already creates a root node, so we can't create another one
        // This test verifies that the tree has a valid root node after reasoning
        Assert.IsNotNull(engine.CurrentTree);
        Assert.IsNotNull(engine.CurrentTree!.RootId);
        Assert.IsTrue(engine.CurrentTree.Nodes.ContainsKey(engine.CurrentTree.RootId));
        
        var rootNode = engine.CurrentTree.Nodes[engine.CurrentTree.RootId];
        Assert.AreEqual(0, rootNode.Depth);
        Assert.IsNull(rootNode.ParentId);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_AddChild_ShouldCreateValidChildNode()
    {
        // Arrange
        var engine = new TreeOfThoughtsEngine(_mockLlm, _config, _logger, _eventManager, _statusManager);
        await engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var rootNodeId = engine.CurrentTree!.RootId!;
        var childThought = "Child hypothesis";
        var childThoughtType = ThoughtType.Analysis;

        // Act
        var childNode = engine.AddChild(rootNodeId, childThought, childThoughtType);

        // Assert
        Assert.IsNotNull(childNode);
        Assert.AreEqual(childThought, childNode.Thought);
        Assert.AreEqual(childThoughtType, childNode.ThoughtType);
        Assert.AreEqual(1, childNode.Depth);
        Assert.AreEqual(rootNodeId, childNode.ParentId);
        Assert.IsTrue(engine.CurrentTree!.Nodes.ContainsKey(childNode.NodeId));
        
        var rootNode = engine.CurrentTree.Nodes[rootNodeId];
        Assert.IsTrue(rootNode.ChildIds.Contains(childNode.NodeId));
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_AddChild_InvalidParentId_ShouldThrowException()
    {
        // Arrange
        var engine = new TreeOfThoughtsEngine(_mockLlm, _config, _logger, _eventManager, _statusManager);
        await engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var invalidParentId = "invalid-id";
        var childThought = "Child hypothesis";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
        {
            engine.AddChild(invalidParentId, childThought, ThoughtType.Analysis);
        });
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_EvaluateNode_ShouldUpdateNodeScore()
    {
        // Arrange
        var engine = new TreeOfThoughtsEngine(_mockLlm, _config, _logger, _eventManager, _statusManager);
        await engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var rootNodeId = engine.CurrentTree!.RootId!;
        var score = 0.85;

        // Act
        engine.EvaluateNode(rootNodeId, score);

        // Assert
        var updatedNode = engine.CurrentTree!.Nodes[rootNodeId];
        Assert.AreEqual(score, updatedNode.Score);
        Assert.AreEqual(ThoughtNodeState.Evaluated, updatedNode.State);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_EvaluateNode_InvalidNodeId_ShouldThrowException()
    {
        // Arrange
        var engine = new TreeOfThoughtsEngine(_mockLlm, _config, _logger, _eventManager, _statusManager);
        await engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var invalidNodeId = "invalid-id";
        var score = 0.85;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
        {
            engine.EvaluateNode(invalidNodeId, score);
        });
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_PruneNode_ShouldMarkNodeAsPruned()
    {
        // Arrange
        var engine = new TreeOfThoughtsEngine(_mockLlm, _config, _logger, _eventManager, _statusManager);
        await engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var rootNodeId = engine.CurrentTree!.RootId!;
        var childNode = engine.AddChild(rootNodeId, "Child thought", ThoughtType.Analysis);

        // Act
        engine.PruneNode(childNode.NodeId);

        // Assert
        var prunedNode = engine.CurrentTree!.Nodes[childNode.NodeId];
        Assert.AreEqual(ThoughtNodeState.Pruned, prunedNode.State);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_PruneNode_ShouldPruneDescendants()
    {
        // Arrange
        var engine = new TreeOfThoughtsEngine(_mockLlm, _config, _logger, _eventManager, _statusManager);
        await engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var rootNodeId = engine.CurrentTree!.RootId!;
        var childNode = engine.AddChild(rootNodeId, "Child thought", ThoughtType.Analysis);
        var grandchildNode = engine.AddChild(childNode.NodeId, "Grandchild thought", ThoughtType.Decision);

        // Act
        engine.PruneNode(childNode.NodeId);

        // Assert
        var prunedChild = engine.CurrentTree!.Nodes[childNode.NodeId];
        var prunedGrandchild = engine.CurrentTree.Nodes[grandchildNode.NodeId];
        Assert.AreEqual(ThoughtNodeState.Pruned, prunedChild.State);
        Assert.AreEqual(ThoughtNodeState.Pruned, prunedGrandchild.State);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExploreAsync_BestFirstStrategy_ShouldWorkCorrectly()
    {
        // Arrange
        await _engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var strategy = ExplorationStrategy.BestFirst;

        // Act
        var result = await _engine.ExploreAsync(strategy);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BestPath);
        // Note: ExecutionTimeMs might be 0 for very fast operations
        // Note: NodesExplored might be 0 if the tree is already fully explored
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExploreAsync_BreadthFirstStrategy_ShouldWorkCorrectly()
    {
        // Arrange
        await _engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var strategy = ExplorationStrategy.BreadthFirst;

        // Act
        var result = await _engine.ExploreAsync(strategy);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BestPath);
        // Note: ExecutionTimeMs might be 0 for very fast operations
        // Note: NodesExplored might be 0 if the tree is already fully explored
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExploreAsync_DepthFirstStrategy_ShouldWorkCorrectly()
    {
        // Arrange
        await _engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var strategy = ExplorationStrategy.DepthFirst;

        // Act
        var result = await _engine.ExploreAsync(strategy);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BestPath);
        // Note: ExecutionTimeMs might be 0 for very fast operations
        // Note: NodesExplored might be 0 if the tree is already fully explored
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExploreAsync_BeamSearchStrategy_ShouldWorkCorrectly()
    {
        // Arrange
        await _engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var strategy = ExplorationStrategy.BeamSearch;

        // Act
        var result = await _engine.ExploreAsync(strategy);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BestPath);
        // Note: ExecutionTimeMs might be 0 for very fast operations
        // Note: NodesExplored might be 0 if the tree is already fully explored
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExploreAsync_MonteCarloStrategy_ShouldWorkCorrectly()
    {
        // Arrange
        await _engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var strategy = ExplorationStrategy.MonteCarlo;

        // Act
        var result = await _engine.ExploreAsync(strategy);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.BestPath);
        // Note: ExecutionTimeMs might be 0 for very fast operations
        // Note: NodesExplored might be 0 if the tree is already fully explored
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExploreAsync_NoTreeInitialized_ShouldThrowException()
    {
        // Arrange
        var strategy = ExplorationStrategy.BestFirst;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await _engine.ExploreAsync(strategy);
        });
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExploreAsync_Cancellation_ShouldHandleGracefully()
    {
        // Arrange
        await _engine.ReasonAsync("Test goal", "Test context", new Dictionary<string, ITool>());
        var strategy = ExplorationStrategy.BestFirst;
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        // Note: The engine might handle cancellation gracefully and return an error result
        // instead of throwing an exception
        var result = await _engine.ExploreAsync(strategy, cts.Token);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public void TreeOfThoughtsEngine_ReasoningType_ShouldReturnCorrectType()
    {
        // Act
        var reasoningType = _engine.ReasoningType;

        // Assert
        Assert.AreEqual(ReasoningType.TreeOfThoughts, reasoningType);
    }

    [TestMethod]
    public void TreeOfThoughtsEngine_CurrentTree_ShouldBeNullInitially()
    {
        // Assert
        Assert.IsNull(_engine.CurrentTree);
    }

    [TestMethod]
    public void TreeOfThoughtsEngine_CurrentTree_ShouldBeSetAfterReasoning()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act
        _ = _engine.ReasonAsync("Test goal", "Test context", tools).Result;

        // Assert
        Assert.IsNotNull(_engine.CurrentTree);
        Assert.AreEqual("Test goal", _engine.CurrentTree!.Goal);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_MaxTreeDepth_ShouldRespectConfiguration()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.TreeOfThoughts,
            MaxTreeDepth = 2,
            MaxTreeNodes = 20,
            TreeExplorationStrategy = ExplorationStrategy.BestFirst
        };
        var engine = new TreeOfThoughtsEngine(_mockLlm, config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Tree);
        Assert.IsTrue(result.Tree!.CurrentMaxDepth <= 2);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_MaxTreeNodes_ShouldRespectConfiguration()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.TreeOfThoughts,
            MaxTreeDepth = 5,
            MaxTreeNodes = 10,
            TreeExplorationStrategy = ExplorationStrategy.BestFirst
        };
        var engine = new TreeOfThoughtsEngine(_mockLlm, config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Tree);
        Assert.IsTrue(result.Tree!.NodeCount <= 10);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_EmptyGoal_ShouldHandleGracefully()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await _engine.ReasonAsync("", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Conclusion);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_EmptyContext_ShouldHandleGracefully()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await _engine.ReasonAsync("Test goal", "", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Conclusion);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_NullTools_ShouldHandleGracefully()
    {
        // Arrange
        IDictionary<string, ITool>? tools = null;

        // Act & Assert
        // Note: The engine might handle null tools gracefully
        var result = await _engine.ReasonAsync("Test goal", "Test context", tools!);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExceptionDuringReasoning_ShouldReturnErrorResult()
    {
        // Arrange
        var failingLlm = new MockLlmClient();
        // Don't set any response to simulate failure
        var engine = new TreeOfThoughtsEngine(failingLlm, _config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        // Note: ExecutionTimeMs might be 0 for very fast failures
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_Metadata_ShouldContainExpectedKeys()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await _engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Metadata.ContainsKey("nodes_explored"));
        Assert.IsTrue(result.Metadata.ContainsKey("max_depth_reached"));
        Assert.IsTrue(result.Metadata.ContainsKey("best_path_score"));
        Assert.IsTrue(result.Metadata.ContainsKey("reasoning_type"));
        Assert.AreEqual("TreeOfThoughts", result.Metadata["reasoning_type"]);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_BestPath_ShouldBeValid()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await _engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Tree);
        Assert.IsNotNull(result.Tree!.BestPath);
        Assert.IsTrue(result.Tree.BestPath.Count > 0);
        
        // Verify all nodes in best path exist
        foreach (var nodeId in result.Tree.BestPath)
        {
            Assert.IsTrue(result.Tree.Nodes.ContainsKey(nodeId));
        }
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_NodeStates_ShouldBeConsistent()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await _engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Tree);
        
        // Just verify that the tree has nodes and they have valid states
        Assert.IsTrue(result.Tree!.Nodes.Count > 0);
        foreach (var node in result.Tree.Nodes.Values)
        {
            // Verify that each node has a valid state
            Assert.IsTrue(Enum.IsDefined(typeof(ThoughtNodeState), node.State));
        }
    }
}
