using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Metrics;

[TestClass]
public class MetricsCollectorTests
{
    private MetricsCollector _metricsCollector = null!;
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _metricsCollector = new MetricsCollector(_mockLogger.Object);
    }

    [TestMethod]
    public void RecordAgentRunExecutionTime_Should_RaiseMetricsUpdatedEvent()
    {
        // Arrange
        var eventRaised = false;
        MetricsUpdatedEventArgs? eventArgs = null;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 5);

        // Assert
        Assert.IsTrue(eventRaised, "MetricsUpdated event should be raised");
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsTrue(eventArgs!.UpdatedMetrics.Contains("AgentRunExecutionTime"), "Should include the correct metric name");
        Assert.IsNotNull(eventArgs.Metrics, "Metrics data should be provided");
    }

    [TestMethod]
    public void RecordLlmCallExecutionTime_Should_RaiseMetricsUpdatedEvent()
    {
        // Arrange
        var eventRaised = false;
        MetricsUpdatedEventArgs? eventArgs = null;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        _metricsCollector.RecordLlmCallExecutionTime("test-agent", 1, 500, "gpt-4");

        // Assert
        Assert.IsTrue(eventRaised, "MetricsUpdated event should be raised");
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsTrue(eventArgs!.UpdatedMetrics.Contains("LlmCallExecutionTime"), "Should include the correct metric name");
    }

    [TestMethod]
    public void RecordToolCallExecutionTime_Should_RaiseMetricsUpdatedEvent()
    {
        // Arrange
        var eventRaised = false;
        MetricsUpdatedEventArgs? eventArgs = null;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        _metricsCollector.RecordToolCallExecutionTime("test-agent", 1, "weather-tool", 200);

        // Assert
        Assert.IsTrue(eventRaised, "MetricsUpdated event should be raised");
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsTrue(eventArgs!.UpdatedMetrics.Contains("ToolCallExecutionTime"), "Should include the correct metric name");
    }

    [TestMethod]
    public void RecordCustomMetric_Should_RaiseMetricsUpdatedEvent()
    {
        // Arrange
        var eventRaised = false;
        MetricsUpdatedEventArgs? eventArgs = null;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        _metricsCollector.RecordCustomMetric("test-metric", 42.5);

        // Assert
        Assert.IsTrue(eventRaised, "MetricsUpdated event should be raised");
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsTrue(eventArgs!.UpdatedMetrics.Contains("CustomMetric"), "Should include the correct metric name");
    }

    [TestMethod]
    public void Reset_Should_RaiseMetricsUpdatedEvent()
    {
        // Arrange
        var eventRaised = false;
        MetricsUpdatedEventArgs? eventArgs = null;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        _metricsCollector.Reset();

        // Assert
        Assert.IsTrue(eventRaised, "MetricsUpdated event should be raised");
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsTrue(eventArgs!.UpdatedMetrics.Contains("Reset"), "Should include the correct metric name");
    }

    [TestMethod]
    public void MultipleMetricRecordings_Should_RaiseMultipleEvents()
    {
        // Arrange
        var eventCount = 0;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventCount++;
        };

        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 5);
        _metricsCollector.RecordLlmCallExecutionTime("test-agent", 1, 500, "gpt-4");
        _metricsCollector.RecordToolCallExecutionTime("test-agent", 1, "weather-tool", 200);

        // Assert
        Assert.AreEqual(3, eventCount, "Should raise 3 events for 3 metric recordings");
    }

    [TestMethod]
    public void MetricsUpdatedEvent_Should_IncludeCorrectTimestamp()
    {
        // Arrange
        MetricsUpdatedEventArgs? eventArgs = null;
        var beforeRecording = DateTimeOffset.UtcNow;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventArgs = e;
        };

        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 5);
        var afterRecording = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsTrue(eventArgs!.UpdatedAt >= beforeRecording, "Timestamp should be after recording started");
        Assert.IsTrue(eventArgs.UpdatedAt <= afterRecording, "Timestamp should be before recording ended");
    }

    [TestMethod]
    public void MetricsUpdatedEvent_Should_IncludeMetricsData()
    {
        // Arrange
        MetricsUpdatedEventArgs? eventArgs = null;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventArgs = e;
        };

        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 5);

        // Assert
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsNotNull(eventArgs!.Metrics, "Metrics data should be provided");
        Assert.IsTrue(eventArgs.Metrics.CollectedAt > DateTimeOffset.MinValue, "Metrics should have a valid collection timestamp");
    }

    [TestMethod]
    public void NoEventHandlers_Should_NotThrowException()
    {
        // Act & Assert
        try
        {
            _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 5);
            _metricsCollector.RecordLlmCallExecutionTime("test-agent", 1, 500, "gpt-4");
            _metricsCollector.RecordCustomMetric("test-metric", 42.5);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should not throw exception when no event handlers are registered: {ex.Message}");
        }
    }

    [TestMethod]
    public void EventHandlerException_Should_BeLogged()
    {
        // Arrange
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            throw new InvalidOperationException("Test exception");
        };

        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 5);

        // Assert
        _mockLogger.Verify(
            x => x.LogWarning(It.Is<string>(s => s.Contains("Failed to raise MetricsUpdated event"))),
            Times.Once,
            "Should log warning when event handler throws exception");
    }
}
