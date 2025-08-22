using Microsoft.VisualStudio.TestTools.UnitTesting;
using AIAgentSharp.Fluent;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using Moq;

namespace AIAgentSharp.Tests.Fluent;

[TestClass]
public class AIAgentBuilderTests
{
    [TestMethod]
    public void WithStreaming_False_Should_Set_UseFunctionCalling_True()
    {
        var mockLlm = new Mock<ILlmClient>();
        var agent = AIAgent.Create(mockLlm.Object)
            .WithStreaming(false)
            .Build();

        var config = typeof(Agent)
            .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            !.GetValue(agent) as AgentConfiguration;

        Assert.IsNotNull(config);
        Assert.IsTrue(config!.UseFunctionCalling);
    }

    [TestMethod]
    public void ReasoningOptions_Should_Map_To_AgentConfiguration()
    {
        var mockLlm = new Mock<ILlmClient>();
        var agent = AIAgent.Create(mockLlm.Object)
            .WithReasoning(ReasoningType.TreeOfThoughts, o => o
                .SetExplorationStrategy(ExplorationStrategy.BestFirst)
                .SetMaxDepth(7)
                .SetMaxTreeNodes(123))
            .Build();

        var config = typeof(Agent)
            .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            !.GetValue(agent) as AgentConfiguration;

        Assert.IsNotNull(config);
        Assert.AreEqual(ExplorationStrategy.BestFirst, config!.TreeExplorationStrategy);
        Assert.AreEqual(7, config.MaxTreeDepth);
        Assert.AreEqual(123, config.MaxTreeNodes);
    }

    [TestMethod]
    public void Tools_And_Messages_Should_Be_Wired()
    {
        var mockLlm = new Mock<ILlmClient>();
        var dummyTool = new Mock<ITool>().Object;
        var agent = AIAgent.Create(mockLlm.Object)
            .WithTool(dummyTool)
            .WithSystemMessage("sys")
            .WithUserMessage("usr")
            .WithAssistantMessage("asst")
            .Build();

        var config = typeof(Agent)
            .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            !.GetValue(agent) as AgentConfiguration;

        Assert.IsNotNull(config);
        Assert.IsTrue(config!.AdditionalMessages.Any(m => m.Role == "system" && m.Content == "sys"));
        Assert.IsTrue(config.AdditionalMessages.Any(m => m.Role == "user" && m.Content == "usr"));
        Assert.IsTrue(config.AdditionalMessages.Any(m => m.Role == "assistant" && m.Content == "asst"));
    }
}


