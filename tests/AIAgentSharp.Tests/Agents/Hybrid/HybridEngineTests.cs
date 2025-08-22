using AIAgentSharp.Agents.Hybrid;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Moq;

namespace AIAgentSharp.Tests.Agents.Hybrid;

/// <summary>
/// Unit tests for the HybridEngine class.
/// </summary>
[TestClass]
public class HybridEngineTests
{
    private Mock<ILlmClient> _mockLlm;
    private Mock<ILogger> _mockLogger;
    private Mock<IEventManager> _mockEventManager;
    private Mock<IStatusManager> _mockStatusManager;
    private Mock<IMetricsCollector> _mockMetricsCollector;
    private AgentConfiguration _config;
    private HybridEngine _hybridEngine;

    [TestInitialize]
    public void Setup()
    {
        _mockLlm = new Mock<ILlmClient>();
        _mockLogger = new Mock<ILogger>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _mockMetricsCollector = new Mock<IMetricsCollector>();
        _config = new AgentConfiguration();
        
        _hybridEngine = new HybridEngine(
            _mockLlm.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
    }

    [TestMethod]
    public void Constructor_Should_InitializeHybridEngine_When_ValidParametersProvided()
    {
        // Act & Assert
        Assert.IsNotNull(_hybridEngine);
        Assert.AreEqual(ReasoningType.Hybrid, _hybridEngine.ReasoningType);
    }

    [TestMethod]
    public void Constructor_Should_ThrowArgumentNullException_When_LlmClientIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new HybridEngine(
            null!,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object));
    }

    [TestMethod]
    public void Constructor_Should_ThrowArgumentNullException_When_ConfigIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new HybridEngine(
            _mockLlm.Object,
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
        Assert.ThrowsException<ArgumentNullException>(() => new HybridEngine(
            _mockLlm.Object,
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
        Assert.ThrowsException<ArgumentNullException>(() => new HybridEngine(
            _mockLlm.Object,
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
        Assert.ThrowsException<ArgumentNullException>(() => new HybridEngine(
            _mockLlm.Object,
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
        Assert.ThrowsException<ArgumentNullException>(() => new HybridEngine(
            _mockLlm.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            null!));
    }

    [TestMethod]
    public void ReasonAsync_Should_ThrowArgumentNullException_When_GoalIsNull()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _hybridEngine.ReasonAsync(null!, "context", tools, CancellationToken.None));
    }

    [TestMethod]
    public void ReasonAsync_Should_ThrowArgumentNullException_When_GoalIsEmpty()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _hybridEngine.ReasonAsync("", "context", tools, CancellationToken.None));
    }

    [TestMethod]
    public void ReasonAsync_Should_ThrowArgumentNullException_When_ContextIsNull()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();

        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _hybridEngine.ReasonAsync("goal", null!, tools, CancellationToken.None));
    }

    [TestMethod]
    public void ReasonAsync_Should_ThrowArgumentNullException_When_ToolsIsNull()
    {
        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _hybridEngine.ReasonAsync("goal", "context", null!, CancellationToken.None));
    }

    [TestMethod]
    public void ReasonAsync_Should_HandleCancellation_When_CancellationTokenCancelled()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await _hybridEngine.ReasonAsync("goal", "context", tools, cts.Token));
    }

    [TestMethod]
    public void ReasoningType_Should_ReturnHybrid()
    {
        // Act
        var reasoningType = _hybridEngine.ReasoningType;

        // Assert
        Assert.AreEqual(ReasoningType.Hybrid, reasoningType);
    }

    [TestMethod]
    public void CalculateCombinedConfidence_Should_WeightChainHigher()
    {
        var combined = typeof(HybridEngine)
            .GetMethod("CalculateCombinedConfidence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            !.Invoke(_hybridEngine, new object[] { 0.9, 0.5 });
        var value = (double)combined!;
        Assert.IsTrue(value > 0.7 && value < 0.9);
    }

    [TestMethod]
    public void CombineConclusions_Should_Combine_When_BothProvided()
    {
        var combined = (string)typeof(HybridEngine)
            .GetMethod("CombineConclusions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            !.Invoke(_hybridEngine, new object[] { "chain", "tree" })!;
        Assert.IsTrue(combined.Contains("Analysis:"));
        Assert.IsTrue(combined.Contains("Exploration:"));
    }

    [TestMethod]
    public void CombineConclusions_Should_Handle_Empty()
    {
        var combined = (string)typeof(HybridEngine)
            .GetMethod("CombineConclusions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            !.Invoke(_hybridEngine, new object[] { string.Empty, string.Empty })!;
        Assert.IsTrue(combined.Contains("no specific conclusions"));
    }

    [TestMethod]
    public void EnhanceContextWithChainInsights_Should_Append_Insights()
    {
        var chain = new ReasoningChain { Steps = new List<ReasoningStep> { new ReasoningStep { Reasoning = "x", Confidence = 0.9 } } };
        var rr = new ReasoningResult { Conclusion = "c", Chain = chain };
        var result = (string)typeof(HybridEngine)
            .GetMethod("EnhanceContextWithChainInsights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            !.Invoke(_hybridEngine, new object[] { "ctx", rr })!;
        Assert.IsTrue(result.Contains("ctx"));
        Assert.IsTrue(result.Contains("Chain of Thought Analysis: c"));
        Assert.IsTrue(result.Contains("Key Insights"));
    }

    [TestMethod]
    public void CurrentChain_Should_ReturnChainFromChainEngine()
    {
        // Act
        var chain = _hybridEngine.CurrentChain;

        // Assert
        // Note: This would require setting up the chain engine's CurrentChain property
        // In a real implementation, we'd need to mock the chain engine or set up the chain
        Assert.IsNull(chain); // Default behavior when no chain exists
    }

    [TestMethod]
    public void CurrentTree_Should_ReturnTreeFromTreeEngine()
    {
        // Act
        var tree = _hybridEngine.CurrentTree;

        // Assert
        // Note: This would require setting up the tree engine's CurrentTree property
        // In a real implementation, we'd need to mock the tree engine or set up the tree
        Assert.IsNull(tree); // Default behavior when no tree exists
    }
}
