using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class ToolExecutorTests
{
    private Mock<ILogger> _mockLogger;
    private Mock<IEventManager> _mockEventManager;
    private Mock<IStatusManager> _mockStatusManager;
    private Mock<IMetricsCollector> _mockMetricsCollector;
    private AgentConfiguration _config;
    private ToolExecutor _toolExecutor;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _mockMetricsCollector = new Mock<IMetricsCollector>();
        _config = new AgentConfiguration();

        _toolExecutor = new ToolExecutor(
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
    }

    [TestMethod]
    public void Constructor_Should_InitializeToolExecutor_When_ValidParametersProvided()
    {
        // Act
        var toolExecutor = new ToolExecutor(
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);

        // Assert
        Assert.IsNotNull(toolExecutor);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_ReturnSuccessResult_When_ToolExecutesSuccessfully()
    {
        // Arrange
        var toolName = "test_tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool executed successfully");

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Tool executed successfully", result.Output);
        Assert.AreEqual(toolName, result.Tool);
        Assert.AreEqual(parameters, result.Params);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_ReturnFailureResult_When_ToolNotFound()
    {
        // Arrange
        var toolName = "nonexistent_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;
        var tools = new Dictionary<string, ITool>();

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("not found"));
        Assert.AreEqual(toolName, result.Tool);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_ReturnFailureResult_When_ToolThrowsException()
    {
        // Arrange
        var toolName = "failing_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tool execution failed"));

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("Tool execution failed"));
        Assert.AreEqual(toolName, result.Tool);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_ReturnFailureResult_When_ToolValidationFails()
    {
        // Arrange
        var toolName = "validation_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;

        var validationException = new ToolValidationException("Validation failed");
        validationException.Missing.Add("required_param");
        validationException.FieldErrors.Add(new ToolValidationError("field1", "Field is invalid"));

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("Validation failed"));
        Assert.IsNotNull(result.Output);
        Assert.AreEqual(toolName, result.Tool);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_ReturnFailureResult_When_ToolTimeoutOccurs()
    {
        // Arrange
        var toolName = "timeout_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;
        var config = new AgentConfiguration { ToolTimeout = TimeSpan.FromMilliseconds(100) };

        var toolExecutor = new ToolExecutor(
            config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .Returns(async (Dictionary<string, object?> p, CancellationToken ct) =>
            {
                await Task.Delay(1000, ct); // Delay longer than timeout
                return "Should not reach here";
            });

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("deadline exceeded"));
        Assert.AreEqual(toolName, result.Tool);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_ReturnFailureResult_When_CancellationRequested()
    {
        // Arrange
        var toolName = "cancellation_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .Returns(async (Dictionary<string, object?> p, CancellationToken ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);
                return "Should not reach here";
            });

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, cts.Token);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("cancelled by user"));
        Assert.AreEqual(toolName, result.Tool);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_EmitEvents_When_ToolExecutes()
    {
        // Arrange
        var toolName = "event_tool";
        var parameters = new Dictionary<string, object?> { { "param1", "value1" } };
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool result");

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        _mockEventManager.Verify(x => x.RaiseToolCallStarted(agentId, turnIndex, toolName, parameters), Times.Once);
        _mockEventManager.Verify(x => x.RaiseToolCallCompleted(agentId, turnIndex, toolName, true, "Tool result", null, It.IsAny<TimeSpan>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_EmitStatusUpdates_When_ToolExecutes()
    {
        // Arrange
        var toolName = "status_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool result");

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        _mockStatusManager.Verify(x => x.EmitStatus(agentId, "Executing tool", It.IsAny<string>(), "Processing tool result", null), Times.Once);
        _mockStatusManager.Verify(x => x.EmitStatus(agentId, "Tool completed", It.IsAny<string>(), "Analyzing result", null), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_RecordMetrics_When_ToolExecutesSuccessfully()
    {
        // Arrange
        var toolName = "metrics_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool result");

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        _mockMetricsCollector.Verify(x => x.RecordToolCallExecutionTime(agentId, turnIndex, toolName, It.IsAny<long>()), Times.Once);
        _mockMetricsCollector.Verify(x => x.RecordToolCallCompletion(agentId, turnIndex, toolName, true, null), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_RecordMetrics_When_ToolExecutionFails()
    {
        // Arrange
        var toolName = "failing_metrics_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tool failed"));

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        _mockMetricsCollector.Verify(x => x.RecordToolCallCompletion(agentId, turnIndex, toolName, false, "ExecutionError"), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_SetExecutionTime_When_ToolExecutes()
    {
        // Arrange
        var toolName = "timing_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .Returns(async (Dictionary<string, object?> p, CancellationToken ct) =>
            {
                await Task.Delay(50, ct);
                return "Tool result";
            });

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.ExecutionTime > TimeSpan.Zero);
        Assert.IsTrue(result.ExecutionTime < TimeSpan.FromSeconds(1)); // Should be much less than 1 second
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_HandleNullParameters_When_ParametersAreNull()
    {
        // Arrange
        var toolName = "null_params_tool";
        Dictionary<string, object?>? parameters = null;
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool result");

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await _toolExecutor.ExecuteToolAsync(toolName, parameters!, tools, agentId, turnIndex, CancellationToken.None));
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_HandleEmptyParameters_When_ParametersAreEmpty()
    {
        // Arrange
        var toolName = "empty_params_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool result");

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Tool result", result.Output);
    }

    [TestMethod]
    public async Task ExecuteToolAsync_Should_SetCreatedUtc_When_ToolExecutes()
    {
        // Arrange
        var toolName = "timestamp_tool";
        var parameters = new Dictionary<string, object?>();
        var agentId = "test-agent";
        var turnIndex = 1;
        var beforeExecution = DateTimeOffset.UtcNow;

        var mockTool = new Mock<ITool>();
        mockTool.Setup(x => x.Name).Returns(toolName);
        mockTool.Setup(x => x.InvokeAsync(parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool result");

        var tools = new Dictionary<string, ITool> { { toolName, mockTool.Object } };

        // Act
        var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters, tools, agentId, turnIndex, CancellationToken.None);
        var afterExecution = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.CreatedUtc >= beforeExecution);
        Assert.IsTrue(result.CreatedUtc <= afterExecution);
    }
}
