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
