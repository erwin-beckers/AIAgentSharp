using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Tests.Configuration;

[TestClass]
public class AgentConfigurationTests
{
    [TestMethod]
    public void Constructor_Should_CreateConfigurationWithDefaultValues_When_NoParametersProvided()
    {
        // Act
        var config = new AgentConfiguration();

        // Assert
        Assert.AreEqual(10, config.MaxRecentTurns);
        Assert.AreEqual(100, config.MaxTurns);
        Assert.AreEqual(TimeSpan.FromMinutes(5), config.LlmTimeout);
        Assert.AreEqual(TimeSpan.FromMinutes(2), config.ToolTimeout);
        Assert.IsTrue(config.UseFunctionCalling);
        Assert.IsTrue(config.EmitPublicStatus);
        Assert.IsTrue(config.EnableHistorySummarization);
        Assert.AreEqual(TimeSpan.FromMinutes(5), config.DedupeStalenessThreshold);
        Assert.AreEqual(ReasoningType.None, config.ReasoningType);
        Assert.AreEqual(10, config.MaxReasoningSteps);
        Assert.AreEqual(5, config.MaxTreeDepth);
        Assert.AreEqual(50, config.MaxTreeNodes);
        Assert.AreEqual(ExplorationStrategy.BestFirst, config.TreeExplorationStrategy);
        Assert.IsTrue(config.EnableReasoningValidation);
        Assert.AreEqual(0.7, config.MinReasoningConfidence);
    }

    [TestMethod]
    public void Constructor_Should_SetCustomValues_When_ObjectInitializerUsed()
    {
        // Arrange
        var customConfig = new AgentConfiguration
        {
            MaxRecentTurns = 15,
            MaxTurns = 50,
            LlmTimeout = TimeSpan.FromMinutes(3),
            ToolTimeout = TimeSpan.FromMinutes(1),
            UseFunctionCalling = false,
            EmitPublicStatus = false,
            EnableHistorySummarization = false,
            DedupeStalenessThreshold = TimeSpan.FromMinutes(10),
            ReasoningType = ReasoningType.ChainOfThought,
            MaxReasoningSteps = 20,
            MaxTreeDepth = 8,
            MaxTreeNodes = 100,
            TreeExplorationStrategy = ExplorationStrategy.DepthFirst,
            EnableReasoningValidation = false,
            MinReasoningConfidence = 0.8
        };

        // Assert
        Assert.AreEqual(15, customConfig.MaxRecentTurns);
        Assert.AreEqual(50, customConfig.MaxTurns);
        Assert.AreEqual(TimeSpan.FromMinutes(3), customConfig.LlmTimeout);
        Assert.AreEqual(TimeSpan.FromMinutes(1), customConfig.ToolTimeout);
        Assert.IsFalse(customConfig.UseFunctionCalling);
        Assert.IsFalse(customConfig.EmitPublicStatus);
        Assert.IsFalse(customConfig.EnableHistorySummarization);
        Assert.AreEqual(TimeSpan.FromMinutes(10), customConfig.DedupeStalenessThreshold);
        Assert.AreEqual(ReasoningType.ChainOfThought, customConfig.ReasoningType);
        Assert.AreEqual(20, customConfig.MaxReasoningSteps);
        Assert.AreEqual(8, customConfig.MaxTreeDepth);
        Assert.AreEqual(100, customConfig.MaxTreeNodes);
        Assert.AreEqual(ExplorationStrategy.DepthFirst, customConfig.TreeExplorationStrategy);
        Assert.IsFalse(customConfig.EnableReasoningValidation);
        Assert.AreEqual(0.8, customConfig.MinReasoningConfidence);
    }

    [TestMethod]
    public void Constructor_Should_AllowPartialCustomization_When_OnlySomePropertiesSet()
    {
        // Arrange
        var partialConfig = new AgentConfiguration
        {
            MaxTurns = 25,
            ReasoningType = ReasoningType.TreeOfThoughts
        };

        // Assert - Custom values
        Assert.AreEqual(25, partialConfig.MaxTurns);
        Assert.AreEqual(ReasoningType.TreeOfThoughts, partialConfig.ReasoningType);

        // Assert - Default values should remain
        Assert.AreEqual(10, partialConfig.MaxRecentTurns);
        Assert.AreEqual(TimeSpan.FromMinutes(5), partialConfig.LlmTimeout);
        Assert.AreEqual(TimeSpan.FromMinutes(2), partialConfig.ToolTimeout);
        Assert.IsTrue(partialConfig.UseFunctionCalling);
        Assert.IsTrue(partialConfig.EmitPublicStatus);
        Assert.IsTrue(partialConfig.EnableHistorySummarization);
        Assert.AreEqual(TimeSpan.FromMinutes(5), partialConfig.DedupeStalenessThreshold);
        Assert.AreEqual(10, partialConfig.MaxReasoningSteps);
        Assert.AreEqual(5, partialConfig.MaxTreeDepth);
        Assert.AreEqual(50, partialConfig.MaxTreeNodes);
        Assert.AreEqual(ExplorationStrategy.BestFirst, partialConfig.TreeExplorationStrategy);
        Assert.IsTrue(partialConfig.EnableReasoningValidation);
        Assert.AreEqual(0.7, partialConfig.MinReasoningConfidence);
    }

    [TestMethod]
    public void Constructor_Should_SupportAllReasoningTypes_When_ReasoningTypeSet()
    {
        // Test all reasoning types
        var configNone = new AgentConfiguration { ReasoningType = ReasoningType.None };
        var configChain = new AgentConfiguration { ReasoningType = ReasoningType.ChainOfThought };
        var configTree = new AgentConfiguration { ReasoningType = ReasoningType.TreeOfThoughts };
        var configHybrid = new AgentConfiguration { ReasoningType = ReasoningType.Hybrid };

        // Assert
        Assert.AreEqual(ReasoningType.None, configNone.ReasoningType);
        Assert.AreEqual(ReasoningType.ChainOfThought, configChain.ReasoningType);
        Assert.AreEqual(ReasoningType.TreeOfThoughts, configTree.ReasoningType);
        Assert.AreEqual(ReasoningType.Hybrid, configHybrid.ReasoningType);
    }

    [TestMethod]
    public void Constructor_Should_SupportAllExplorationStrategies_When_StrategySet()
    {
        // Test all exploration strategies
        var configBestFirst = new AgentConfiguration { TreeExplorationStrategy = ExplorationStrategy.BestFirst };
        var configDepthFirst = new AgentConfiguration { TreeExplorationStrategy = ExplorationStrategy.DepthFirst };
        var configBreadthFirst = new AgentConfiguration { TreeExplorationStrategy = ExplorationStrategy.BreadthFirst };
        var configBeamSearch = new AgentConfiguration { TreeExplorationStrategy = ExplorationStrategy.BeamSearch };
        var configMonteCarlo = new AgentConfiguration { TreeExplorationStrategy = ExplorationStrategy.MonteCarlo };

        // Assert
        Assert.AreEqual(ExplorationStrategy.BestFirst, configBestFirst.TreeExplorationStrategy);
        Assert.AreEqual(ExplorationStrategy.DepthFirst, configDepthFirst.TreeExplorationStrategy);
        Assert.AreEqual(ExplorationStrategy.BreadthFirst, configBreadthFirst.TreeExplorationStrategy);
        Assert.AreEqual(ExplorationStrategy.BeamSearch, configBeamSearch.TreeExplorationStrategy);
        Assert.AreEqual(ExplorationStrategy.MonteCarlo, configMonteCarlo.TreeExplorationStrategy);
    }

    [TestMethod]
    public void Constructor_Should_SupportEdgeCaseValues_When_ExtremeValuesProvided()
    {
        // Arrange
        var edgeConfig = new AgentConfiguration
        {
            MaxRecentTurns = 1,
            MaxTurns = 1,
            MaxReasoningSteps = 1,
            MaxTreeDepth = 1,
            MaxTreeNodes = 1,
            MinReasoningConfidence = 0.0,
            LlmTimeout = TimeSpan.FromMilliseconds(1),
            ToolTimeout = TimeSpan.FromMilliseconds(1),
            DedupeStalenessThreshold = TimeSpan.FromMilliseconds(1)
        };

        // Assert
        Assert.AreEqual(1, edgeConfig.MaxRecentTurns);
        Assert.AreEqual(1, edgeConfig.MaxTurns);
        Assert.AreEqual(1, edgeConfig.MaxReasoningSteps);
        Assert.AreEqual(1, edgeConfig.MaxTreeDepth);
        Assert.AreEqual(1, edgeConfig.MaxTreeNodes);
        Assert.AreEqual(0.0, edgeConfig.MinReasoningConfidence);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), edgeConfig.LlmTimeout);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), edgeConfig.ToolTimeout);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), edgeConfig.DedupeStalenessThreshold);
    }

    [TestMethod]
    public void Constructor_Should_SupportLargeValues_When_LargeValuesProvided()
    {
        // Arrange
        var largeConfig = new AgentConfiguration
        {
            MaxRecentTurns = int.MaxValue,
            MaxTurns = int.MaxValue,
            MaxReasoningSteps = int.MaxValue,
            MaxTreeDepth = int.MaxValue,
            MaxTreeNodes = int.MaxValue,
            MinReasoningConfidence = 1.0,
            LlmTimeout = TimeSpan.FromDays(365),
            ToolTimeout = TimeSpan.FromDays(365),
            DedupeStalenessThreshold = TimeSpan.FromDays(365)
        };

        // Assert
        Assert.AreEqual(int.MaxValue, largeConfig.MaxRecentTurns);
        Assert.AreEqual(int.MaxValue, largeConfig.MaxTurns);
        Assert.AreEqual(int.MaxValue, largeConfig.MaxReasoningSteps);
        Assert.AreEqual(int.MaxValue, largeConfig.MaxTreeDepth);
        Assert.AreEqual(int.MaxValue, largeConfig.MaxTreeNodes);
        Assert.AreEqual(1.0, largeConfig.MinReasoningConfidence);
        Assert.AreEqual(TimeSpan.FromDays(365), largeConfig.LlmTimeout);
        Assert.AreEqual(TimeSpan.FromDays(365), largeConfig.ToolTimeout);
        Assert.AreEqual(TimeSpan.FromDays(365), largeConfig.DedupeStalenessThreshold);
    }

    [TestMethod]
    public void Constructor_Should_SupportBooleanCombinations_When_BooleanFlagsSet()
    {
        // Test all boolean combinations
        var config1 = new AgentConfiguration
        {
            UseFunctionCalling = true,
            EmitPublicStatus = true,
            EnableHistorySummarization = true,
            EnableReasoningValidation = true
        };

        var config2 = new AgentConfiguration
        {
            UseFunctionCalling = false,
            EmitPublicStatus = false,
            EnableHistorySummarization = false,
            EnableReasoningValidation = false
        };

        var config3 = new AgentConfiguration
        {
            UseFunctionCalling = true,
            EmitPublicStatus = false,
            EnableHistorySummarization = true,
            EnableReasoningValidation = false
        };

        // Assert
        Assert.IsTrue(config1.UseFunctionCalling);
        Assert.IsTrue(config1.EmitPublicStatus);
        Assert.IsTrue(config1.EnableHistorySummarization);
        Assert.IsTrue(config1.EnableReasoningValidation);

        Assert.IsFalse(config2.UseFunctionCalling);
        Assert.IsFalse(config2.EmitPublicStatus);
        Assert.IsFalse(config2.EnableHistorySummarization);
        Assert.IsFalse(config2.EnableReasoningValidation);

        Assert.IsTrue(config3.UseFunctionCalling);
        Assert.IsFalse(config3.EmitPublicStatus);
        Assert.IsTrue(config3.EnableHistorySummarization);
        Assert.IsFalse(config3.EnableReasoningValidation);
    }

    [TestMethod]
    public void Constructor_Should_SupportConfidenceRange_When_ConfidenceValuesSet()
    {
        // Test confidence values across the valid range
        var configMin = new AgentConfiguration { MinReasoningConfidence = 0.0 };
        var configMid = new AgentConfiguration { MinReasoningConfidence = 0.5 };
        var configMax = new AgentConfiguration { MinReasoningConfidence = 1.0 };

        // Assert
        Assert.AreEqual(0.0, configMin.MinReasoningConfidence);
        Assert.AreEqual(0.5, configMid.MinReasoningConfidence);
        Assert.AreEqual(1.0, configMax.MinReasoningConfidence);
    }

    [TestMethod]
    public void Constructor_Should_SupportNegativeTimeouts_When_NegativeTimeoutsProvided()
    {
        // Arrange
        var negativeConfig = new AgentConfiguration
        {
            LlmTimeout = TimeSpan.FromTicks(-1),
            ToolTimeout = TimeSpan.FromTicks(-1),
            DedupeStalenessThreshold = TimeSpan.FromTicks(-1)
        };

        // Assert
        Assert.AreEqual(TimeSpan.FromTicks(-1), negativeConfig.LlmTimeout);
        Assert.AreEqual(TimeSpan.FromTicks(-1), negativeConfig.ToolTimeout);
        Assert.AreEqual(TimeSpan.FromTicks(-1), negativeConfig.DedupeStalenessThreshold);
    }

    [TestMethod]
    public void Constructor_Should_SupportZeroValues_When_ZeroValuesProvided()
    {
        // Arrange
        var zeroConfig = new AgentConfiguration
        {
            MaxRecentTurns = 0,
            MaxTurns = 0,
            MaxReasoningSteps = 0,
            MaxTreeDepth = 0,
            MaxTreeNodes = 0,
            MinReasoningConfidence = 0.0,
            LlmTimeout = TimeSpan.Zero,
            ToolTimeout = TimeSpan.Zero,
            DedupeStalenessThreshold = TimeSpan.Zero
        };

        // Assert
        Assert.AreEqual(0, zeroConfig.MaxRecentTurns);
        Assert.AreEqual(0, zeroConfig.MaxTurns);
        Assert.AreEqual(0, zeroConfig.MaxReasoningSteps);
        Assert.AreEqual(0, zeroConfig.MaxTreeDepth);
        Assert.AreEqual(0, zeroConfig.MaxTreeNodes);
        Assert.AreEqual(0.0, zeroConfig.MinReasoningConfidence);
        Assert.AreEqual(TimeSpan.Zero, zeroConfig.LlmTimeout);
        Assert.AreEqual(TimeSpan.Zero, zeroConfig.ToolTimeout);
        Assert.AreEqual(TimeSpan.Zero, zeroConfig.DedupeStalenessThreshold);
    }
}
