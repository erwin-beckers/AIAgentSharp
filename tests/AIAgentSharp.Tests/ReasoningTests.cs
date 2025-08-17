using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.Tests;

[TestClass]
public class ReasoningTests
{
    private ILlmClient _mockLlm = null!;
    private IAgentStateStore _stateStore = null!;
    private ILogger _logger = null!;
    private IEventManager _eventManager = null!;
    private IStatusManager _statusManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlm = new MockLlmClient();
        _stateStore = new MemoryAgentStateStore();
        _logger = new ConsoleLogger();
        _eventManager = new EventManager(_logger);
        _statusManager = new StatusManager(new AgentConfiguration(), _eventManager);
    }

    [TestMethod]
    public async Task ChainOfThoughtEngine_BasicReasoning_ShouldCompleteSuccessfully()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.ChainOfThought,
            MaxReasoningSteps = 5,
            EnableReasoningValidation = true,
            MinReasoningConfidence = 0.6
        };

        var engine = new ChainOfThoughtEngine(_mockLlm, config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Conclusion);
        Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        Assert.IsNotNull(result.Chain);
        Assert.IsTrue(result.Chain!.Steps.Count > 0);
        Assert.IsTrue(result.ExecutionTimeMs > 0);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_BasicReasoning_ShouldCompleteSuccessfully()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.TreeOfThoughts,
            MaxTreeDepth = 4,
            MaxTreeNodes = 20,
            TreeExplorationStrategy = ExplorationStrategy.BestFirst
        };

        var engine = new TreeOfThoughtsEngine(_mockLlm, config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await engine.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Conclusion);
        Assert.IsTrue(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        Assert.IsNotNull(result.Tree);
        Assert.IsTrue(result.Tree!.NodeCount > 0);
        Assert.IsTrue(result.ExecutionTimeMs > 0);
    }

    [TestMethod]
    public async Task ReasoningManager_ChainOfThought_ShouldUseCorrectEngine()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.ChainOfThought
        };

        var manager = new ReasoningManager(_mockLlm, config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await manager.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Chain);
        Assert.IsNull(result.Tree);
    }

    [TestMethod]
    public async Task ReasoningManager_TreeOfThoughts_ShouldUseCorrectEngine()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.TreeOfThoughts
        };

        var manager = new ReasoningManager(_mockLlm, config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await manager.ReasonAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Tree);
        Assert.IsNull(result.Chain);
    }

    [TestMethod]
    public async Task ReasoningManager_HybridReasoning_ShouldCombineApproaches()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.Hybrid
        };

        var manager = new ReasoningManager(_mockLlm, config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await manager.PerformHybridReasoningAsync("Test goal", "Test context", tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Conclusion);
        Assert.IsTrue(result.ExecutionTimeMs > 0);
        Assert.IsTrue(result.Metadata.ContainsKey("reasoning_type"));
        Assert.AreEqual("Hybrid", result.Metadata["reasoning_type"]);
    }

    [TestMethod]
    public async Task TreeOfThoughtsEngine_ExplorationStrategies_ShouldWorkCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.TreeOfThoughts,
            MaxTreeDepth = 3,
            MaxTreeNodes = 15
        };

        var engine = new TreeOfThoughtsEngine(_mockLlm, config, _logger, _eventManager, _statusManager);
        var tools = new Dictionary<string, ITool>();

        // First initialize the tree by calling ReasonAsync
        await engine.ReasonAsync("Test goal", "Test context", tools);

        // Test different exploration strategies
        var strategies = new[] 
        { 
            ExplorationStrategy.BestFirst,
            ExplorationStrategy.BreadthFirst,
            ExplorationStrategy.DepthFirst,
            ExplorationStrategy.BeamSearch,
            ExplorationStrategy.MonteCarlo
        };

        foreach (var strategy in strategies)
        {
            // Act
            var explorationResult = await engine.ExploreAsync(strategy);

            // Assert
            Assert.IsTrue(explorationResult.Success);
            Assert.IsTrue(explorationResult.NodesExplored > 0);
            Assert.IsTrue(explorationResult.ExecutionTimeMs > 0);
        }
    }

    [TestMethod]
    public void ReasoningChain_AddSteps_ShouldWorkCorrectly()
    {
        // Arrange
        var chain = new ReasoningChain { Goal = "Test goal" };

        // Act
        var step1 = chain.AddStep("First reasoning step", ReasoningStepType.Analysis, 0.8);
        var step2 = chain.AddStep("Second reasoning step", ReasoningStepType.Planning, 0.9);
        chain.Complete("Final conclusion", 0.85);

        // Assert
        Assert.AreEqual(2, chain.Steps.Count);
        Assert.AreEqual(1, step1.StepNumber);
        Assert.AreEqual(2, step2.StepNumber);
        Assert.AreEqual(ReasoningStepType.Analysis, step1.StepType);
        Assert.AreEqual(ReasoningStepType.Planning, step2.StepType);
        Assert.AreEqual(0.8, step1.Confidence);
        Assert.AreEqual(0.9, step2.Confidence);
        Assert.IsTrue(chain.IsComplete);
        Assert.AreEqual("Final conclusion", chain.FinalConclusion);
        Assert.AreEqual(0.85, chain.FinalConfidence);
    }

    [TestMethod]
    public void ReasoningTree_AddNodes_ShouldWorkCorrectly()
    {
        // Arrange
        var tree = new ReasoningTree 
        { 
            Goal = "Test goal",
            MaxDepth = 3,
            MaxNodes = 10
        };

        // Act
        var root = tree.CreateRoot("Root thought", ThoughtType.Hypothesis);
        var child1 = tree.AddChild(root.NodeId, "Child thought 1", ThoughtType.Analysis);
        var child2 = tree.AddChild(root.NodeId, "Child thought 2", ThoughtType.Decision);
        var grandchild = tree.AddChild(child1.NodeId, "Grandchild thought", ThoughtType.Conclusion);

        // Assert
        Assert.AreEqual(4, tree.NodeCount);
        Assert.AreEqual(0, root.Depth);
        Assert.AreEqual(1, child1.Depth);
        Assert.AreEqual(1, child2.Depth);
        Assert.AreEqual(2, grandchild.Depth);
        Assert.AreEqual(2, root.ChildIds.Count);
        Assert.AreEqual(1, child1.ChildIds.Count);
        Assert.AreEqual(0, child2.ChildIds.Count);
        Assert.AreEqual(0, grandchild.ChildIds.Count);
    }

    [TestMethod]
    public void ReasoningTree_EvaluateAndPrune_ShouldWorkCorrectly()
    {
        // Arrange
        var tree = new ReasoningTree { Goal = "Test goal" };
        var root = tree.CreateRoot("Root thought");
        var child1 = tree.AddChild(root.NodeId, "Child thought 1");
        var child2 = tree.AddChild(root.NodeId, "Child thought 2");

        // Act
        tree.EvaluateNode(child1.NodeId, 0.8);
        tree.EvaluateNode(child2.NodeId, 0.3);
        tree.PruneNode(child2.NodeId);

        // Assert
        Assert.AreEqual(ThoughtNodeState.Evaluated, tree.Nodes[child1.NodeId].State);
        Assert.AreEqual(ThoughtNodeState.Pruned, tree.Nodes[child2.NodeId].State);
        Assert.AreEqual(0.8, tree.Nodes[child1.NodeId].Score);
        Assert.AreEqual(0.3, tree.Nodes[child2.NodeId].Score);
    }

    [TestMethod]
    public void AgentConfiguration_ReasoningSettings_ShouldBeConfigurable()
    {
        // Arrange & Act
        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.TreeOfThoughts,
            MaxReasoningSteps = 15,
            MaxTreeDepth = 8,
            MaxTreeNodes = 100,
            TreeExplorationStrategy = ExplorationStrategy.BeamSearch,
            EnableReasoningValidation = false,
            MinReasoningConfidence = 0.8
        };

        // Assert
        Assert.AreEqual(ReasoningType.TreeOfThoughts, config.ReasoningType);
        Assert.AreEqual(15, config.MaxReasoningSteps);
        Assert.AreEqual(8, config.MaxTreeDepth);
        Assert.AreEqual(100, config.MaxTreeNodes);
        Assert.AreEqual(ExplorationStrategy.BeamSearch, config.TreeExplorationStrategy);
        Assert.IsFalse(config.EnableReasoningValidation);
        Assert.AreEqual(0.8, config.MinReasoningConfidence);
    }

    [TestMethod]
    public void AgentState_ReasoningProperties_ShouldBeSerializable()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            ReasoningType = ReasoningType.ChainOfThought,
            CurrentReasoningChain = new ReasoningChain { Goal = "Chain goal" },
            CurrentReasoningTree = new ReasoningTree { Goal = "Tree goal" },
            ReasoningMetadata = new Dictionary<string, object> { ["test"] = "value" }
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(state);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("test-agent"));
        Assert.IsTrue(json.Contains("Chain goal"));
        Assert.IsTrue(json.Contains("Tree goal"));
    }

    [TestMethod]
    public void ModelMessage_ReasoningProperties_ShouldBeSerializable()
    {
        // Arrange
        var message = new ModelMessage
        {
            Thoughts = "Test thoughts",
            ReasoningChain = new ReasoningChain { Goal = "Chain goal" },
            ReasoningTree = new ReasoningTree { Goal = "Tree goal" },
            ReasoningConfidence = 0.85,
            ReasoningType = ReasoningType.ChainOfThought
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(message);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("Test thoughts"));
        Assert.IsTrue(json.Contains("Chain goal"));
        Assert.IsTrue(json.Contains("Tree goal"));
        Assert.IsTrue(json.Contains("0.85"));
    }

    private class MockLlmClient : ILlmClient
    {
        public async Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken); // Simulate some processing time

            // Get the content from the first user message
            var userMessage = messages.FirstOrDefault(m => m.Role == "user");
            var prompt = userMessage?.Content ?? "";

            // Return mock responses based on prompt content
            if (prompt.Contains("analysis"))
            {
                return @"{
                    ""reasoning"": ""Analyzing the problem step by step..."",
                    ""confidence"": 0.85,
                    ""insights"": [""insight1"", ""insight2""]
                }";
            }
            else if (prompt.Contains("planning"))
            {
                return @"{
                    ""reasoning"": ""Creating a detailed plan..."",
                    ""confidence"": 0.9,
                    ""insights"": [""plan1"", ""plan2""]
                }";
            }
            else if (prompt.Contains("strategy"))
            {
                return @"{
                    ""reasoning"": ""Developing execution strategy..."",
                    ""confidence"": 0.8,
                    ""insights"": [""strategy1"", ""strategy2""]
                }";
            }
            else if (prompt.Contains("evaluation"))
            {
                return @"{
                    ""reasoning"": ""Evaluating the solution..."",
                    ""confidence"": 0.95,
                    ""insights"": [""eval1"", ""eval2""],
                    ""conclusion"": ""Final conclusion and recommendation""
                }";
            }
            else if (prompt.Contains("thought"))
            {
                return @"{
                    ""thought"": ""Initial hypothesis about the problem"",
                    ""thought_type"": ""Hypothesis""
                }";
            }
            else if (prompt.Contains("children"))
            {
                return @"{
                    ""children"": [
                        {
                            ""thought"": ""First child thought"",
                            ""thought_type"": ""Analysis"",
                            ""estimated_score"": 0.75
                        },
                        {
                            ""thought"": ""Second child thought"",
                            ""thought_type"": ""Alternative"",
                            ""estimated_score"": 0.65
                        }
                    ]
                }";
            }
            else if (prompt.Contains("score"))
            {
                return @"{
                    ""score"": 0.85,
                    ""reasoning"": ""This thought shows good potential""
                }";
            }
            else if (prompt.Contains("conclusion"))
            {
                return @"{
                    ""conclusion"": ""Synthesized conclusion from multiple approaches""
                }";
            }
            else if (prompt.Contains("validation"))
            {
                return @"{
                    ""is_valid"": true,
                    ""error"": """"
                }";
            }
            else
            {
                return @"{
                    ""reasoning"": ""Default reasoning response"",
                    ""confidence"": 0.7,
                    ""insights"": [""default insight""]
                }";
            }
        }
    }
}
