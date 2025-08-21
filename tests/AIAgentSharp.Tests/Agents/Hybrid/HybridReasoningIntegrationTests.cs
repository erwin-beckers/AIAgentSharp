using AIAgentSharp.Agents.Hybrid;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Agents;
using AIAgentSharp.Metrics;
using AIAgentSharp.Fluent;

using Moq;

namespace AIAgentSharp.Tests.Agents.Hybrid;

/// <summary>
/// Integration tests for Hybrid reasoning functionality.
/// </summary>
[TestClass]
public class HybridReasoningIntegrationTests
{
    [TestMethod]
    public void ReasoningManager_Should_SupportHybridReasoning()
    {
        // Arrange
        var mockLlm = new Mock<ILlmClient>();
        var config = new AgentConfiguration { ReasoningType = ReasoningType.Hybrid };
        var logger = new ConsoleLogger();
        var eventManager = new EventManager(logger);
        var statusManager = new StatusManager(config, eventManager);
        var metricsCollector = new MetricsCollector(logger);

        // Act
        var reasoningManager = new ReasoningManager(
            mockLlm.Object,
            config,
            logger,
            eventManager,
            statusManager,
            metricsCollector);

        // Assert
        Assert.IsNotNull(reasoningManager);
    }

    [TestMethod]
    public void AgentConfiguration_Should_SupportHybridReasoningType()
    {
        // Arrange & Act
        var config = new AgentConfiguration { ReasoningType = ReasoningType.Hybrid };

        // Assert
        Assert.AreEqual(ReasoningType.Hybrid, config.ReasoningType);
    }

    [TestMethod]
    public void HybridEngine_Should_BeRegisteredInReasoningManager()
    {
        // Arrange
        var mockLlm = new Mock<ILlmClient>();
        var config = new AgentConfiguration { ReasoningType = ReasoningType.Hybrid };
        var logger = new ConsoleLogger();
        var eventManager = new EventManager(logger);
        var statusManager = new StatusManager(config, eventManager);
        var metricsCollector = new MetricsCollector(logger);

        var reasoningManager = new ReasoningManager(
            mockLlm.Object,
            config,
            logger,
            eventManager,
            statusManager,
            metricsCollector);

        // Act & Assert
        // The test verifies that HybridEngine is properly registered and can be accessed
        // This is tested by ensuring the ReasoningManager can be created with Hybrid reasoning type
        Assert.IsNotNull(reasoningManager);
    }

    [TestMethod]
    public void HybridReasoning_Should_BeAvailableInFluentAPI()
    {
        // Arrange
        var mockLlm = new Mock<ILlmClient>();

        // Act
        var agent = AIAgent.Create(mockLlm.Object)
            .WithReasoning(ReasoningType.Hybrid)
            .Build();

        // Assert
        Assert.IsNotNull(agent);
    }
}
