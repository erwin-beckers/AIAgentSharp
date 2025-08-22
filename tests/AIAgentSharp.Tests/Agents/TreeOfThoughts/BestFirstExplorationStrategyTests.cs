using Microsoft.VisualStudio.TestTools.UnitTesting;
using AIAgentSharp.Agents.TreeOfThoughts;
using AIAgentSharp.Agents.TreeOfThoughts.Strategies;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Tests.Agents.TreeOfThoughts;

[TestClass]
public class BestFirstExplorationStrategyTests
{
    private sealed class NoopStatus : IStatusManager
    {
        public void EmitStatus(string agentId, string statusTitle, string? statusDetails = null, string? nextStepHint = null, int? progressPct = null) { }
    }

    [TestMethod]
    public async Task ExploreAsync_Should_RespectDepthAndNodeLimits()
    {
        var config = new AgentConfiguration { MaxTreeDepth = 2, MaxTreeNodes = 3 };
        var tree = new ReasoningTree { MaxDepth = config.MaxTreeDepth, MaxNodes = config.MaxTreeNodes };

        var communicator = new TreeOfThoughtsCommunicator(new DummyLlmCommunicator());
        var generator = new TreeThoughtGenerator(communicator);
        var evaluator = new TreeNodeEvaluator(communicator);
        var ops = new TreeOperations();

        var root = ops.CreateRoot(tree, "root");

        // Replace generator methods to be deterministic without LLM calls
        var genChildren = new List<TreeThoughtGenerator.ChildThought>
        {
            new() { Thought = "a", ThoughtType = ThoughtType.Analysis, EstimatedScore = 0.9 },
            new() { Thought = "b", ThoughtType = ThoughtType.Alternative, EstimatedScore = 0.7 }
        };

        // Monkey-patch via local wrapper
        var testGenerator = new TestGenerator(genChildren);

        var strategy = new BestFirstExplorationStrategy();
        var result = await strategy.ExploreAsync(
            tree,
            config,
            testGenerator,
            new TestEvaluator(0.8),
            ops,
            new NoopStatus(),
            CancellationToken.None);

        Assert.IsTrue(result.NodesExplored <= config.MaxTreeNodes);
        Assert.IsTrue(result.MaxDepthReached <= config.MaxTreeDepth);
    }

    private sealed class TestGenerator : TreeThoughtGenerator
    {
        private readonly List<TreeThoughtGenerator.ChildThought> _children;
        public TestGenerator(List<TreeThoughtGenerator.ChildThought> children) : base(new TreeOfThoughtsCommunicator(new DummyLlmCommunicator()))
        {
            _children = children;
        }
        public async Task<List<TreeThoughtGenerator.ChildThought>> GenerateChildThoughtsAsync_Public(ThoughtNode parentNode, CancellationToken cancellationToken)
            => await Task.FromResult(_children);
    }

    private sealed class TestEvaluator : TreeNodeEvaluator
    {
        private readonly double _score;
        public TestEvaluator(double score) : base(new TreeOfThoughtsCommunicator(new DummyLlmCommunicator()))
        {
            _score = score;
        }
        public async Task<double> EvaluateThoughtNodeAsync_Public(ThoughtNode node, CancellationToken cancellationToken)
            => await Task.FromResult(_score);
    }

    private sealed class DummyLlmCommunicator : ILlmCommunicator
    {
        public Task<LlmResponse> CallWithFunctionsAsync(IEnumerable<LlmMessage> messages, List<FunctionSpec> functionSpecs, string agentId, int turnIndex, CancellationToken ct)
            => Task.FromResult(new LlmResponse());
        public Task<string> CallLlmWithStreamingAsync(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, CancellationToken ct)
            => Task.FromResult("{}");
        public Task<ModelMessage?> CallLlmAndParseAsync(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, string turnId, AgentState state, CancellationToken ct)
            => Task.FromResult<ModelMessage?>(null);
        public Task<ModelMessage?> ParseJsonResponse(string llmRaw, int turnIndex, string turnId, AgentState state, CancellationToken ct)
            => Task.FromResult<ModelMessage?>(null);
        public ModelMessage NormalizeFunctionCallToReact(LlmResponse functionResult, int turnIndex)
            => new ModelMessage();
        public ILlmClient GetLlmClient() => throw new NotImplementedException();
    }
}


