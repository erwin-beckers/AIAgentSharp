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

    [TestMethod]
    public void OnMetricsUpdated_Should_RaiseEventWithCorrectData()
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
        _metricsCollector.OnMetricsUpdated("TestMetric");

        // Assert
        Assert.IsTrue(eventRaised, "MetricsUpdated event should be raised");
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsTrue(eventArgs!.UpdatedMetrics.Contains("TestMetric"), "Should include the correct metric name");
        Assert.IsNotNull(eventArgs.Metrics, "Metrics data should be provided");
        Assert.IsTrue(eventArgs.UpdatedAt > DateTimeOffset.MinValue, "Should have valid timestamp");
    }

    [TestMethod]
    public void OnMetricsUpdated_Should_HandleNullEventHandlers()
    {
        // Act & Assert - Should not throw exception
        try
        {
            _metricsCollector.OnMetricsUpdated("TestMetric");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should not throw exception when no event handlers: {ex.Message}");
        }
    }

    [TestMethod]
    public void OnMetricsUpdated_Should_LogWarningWhenEventHandlerThrows()
    {
        // Arrange
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            throw new InvalidOperationException("Test exception");
        };

        // Act
        _metricsCollector.OnMetricsUpdated("TestMetric");

        // Assert
        _mockLogger.Verify(
            x => x.LogWarning(It.Is<string>(s => s.Contains("Failed to raise MetricsUpdated event"))),
            Times.Once,
            "Should log warning when event handler throws exception");
    }

    [TestMethod]
    public void GetAllMetrics_Should_ReturnMetricsFromAggregator()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 5);
        _metricsCollector.RecordLlmCallExecutionTime("test-agent", 1, 500, "gpt-4");

        // Act
        var metrics = _metricsCollector.GetAllMetrics();

        // Assert
        Assert.IsNotNull(metrics, "Metrics should not be null");
        Assert.IsNotNull(metrics.Performance, "Performance metrics should not be null");
        Assert.IsNotNull(metrics.Operational, "Operational metrics should not be null");
        Assert.IsNotNull(metrics.Quality, "Quality metrics should not be null");
        Assert.IsNotNull(metrics.Resource, "Resource metrics should not be null");
    }

    [TestMethod]
    public void GetMetrics_Should_ReturnMetricsDataWithCorrectStructure()
    {
        // Act
        var metricsData = _metricsCollector.GetMetrics();

        // Assert
        Assert.IsNotNull(metricsData, "Metrics data should not be null");
        Assert.IsTrue(metricsData.CollectedAt > DateTimeOffset.MinValue, "Should have valid collection timestamp");
        Assert.IsNotNull(metricsData.Performance, "Performance metrics should not be null");
        Assert.IsNotNull(metricsData.Operational, "Operational metrics should not be null");
        Assert.IsNotNull(metricsData.Quality, "Quality metrics should not be null");
        Assert.IsNotNull(metricsData.Resources, "Resource metrics should not be null");
        Assert.IsNotNull(metricsData.CustomMetrics, "Custom metrics should not be null");
        Assert.IsNotNull(metricsData.CustomEvents, "Custom events should not be null");
    }

    [TestMethod]
    public void GetAgentMetrics_Should_ReturnNull()
    {
        // Act
        var agentMetrics = _metricsCollector.GetAgentMetrics("test-agent");

        // Assert
        Assert.IsNull(agentMetrics, "Should return null for agent-specific metrics");
    }

    [TestMethod]
    public void GetMetricsForTimeRange_Should_ReturnCurrentMetrics()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddHours(-1);
        var endTime = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var metricsData = _metricsCollector.GetMetricsForTimeRange(startTime, endTime);

        // Assert
        Assert.IsNotNull(metricsData, "Metrics data should not be null");
        Assert.IsTrue(metricsData.CollectedAt > DateTimeOffset.MinValue, "Should have valid collection timestamp");
    }

    [TestMethod]
    public void ResetMetrics_Should_RaiseMetricsUpdatedEvent()
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
        _metricsCollector.ResetMetrics();

        // Assert
        Assert.IsTrue(eventRaised, "MetricsUpdated event should be raised");
        Assert.IsNotNull(eventArgs, "Event args should not be null");
        Assert.IsTrue(eventArgs!.UpdatedMetrics.Contains("Reset"), "Should include the correct metric name");
    }

    [TestMethod]
    public void GetSummary_Should_ReturnSummaryFromAggregator()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 5);

        // Act
        var summary = _metricsCollector.GetSummary();

        // Assert
        Assert.IsNotNull(summary, "Summary should not be null");
        Assert.IsTrue(summary.TotalAgentRuns > 0, "Should have recorded agent runs");
    }

    [TestMethod]
    public void Constructor_Should_UseProvidedLogger()
    {
        // Act
        var collector = new MetricsCollector(_mockLogger.Object);

        // Assert
        Assert.IsNotNull(collector, "Collector should be created successfully");
    }

    [TestMethod]
    public void Constructor_Should_UseConsoleLoggerWhenNoLoggerProvided()
    {
        // Act
        var collector = new MetricsCollector();

        // Assert
        Assert.IsNotNull(collector, "Collector should be created successfully with default logger");
    }

    [TestMethod]
    public void AllRecordingMethods_Should_RaiseMetricsUpdatedEvent()
    {
        // Arrange
        var eventCount = 0;
        var expectedEvents = 0;
        
        _metricsCollector.MetricsUpdated += (sender, e) =>
        {
            eventCount++;
        };

        // Act
        _metricsCollector.RecordAgentStepExecutionTime("test-agent", 1, 200);
        expectedEvents++;
        
        _metricsCollector.RecordReasoningExecutionTime("test-agent", ReasoningType.ChainOfThought, 300);
        expectedEvents++;
        
        _metricsCollector.RecordAgentRunCompletion("test-agent", true, 5);
        expectedEvents++;
        
        _metricsCollector.RecordAgentStepCompletion("test-agent", 1, true, true);
        expectedEvents++;
        
        _metricsCollector.RecordLlmCallCompletion("test-agent", 1, true, "gpt-4");
        expectedEvents++;
        
        _metricsCollector.RecordToolCallCompletion("test-agent", 1, "test-tool", true);
        expectedEvents++;
        
        _metricsCollector.RecordLoopDetection("test-agent", "failure-loop", 3);
        expectedEvents++;
        
        _metricsCollector.RecordDeduplicationEvent("test-agent", "test-tool", true);
        expectedEvents++;
        
        _metricsCollector.RecordReasoningConfidence("test-agent", ReasoningType.ChainOfThought, 0.85);
        expectedEvents++;
        
        _metricsCollector.RecordResponseQuality("test-agent", 1000, true);
        expectedEvents++;
        
        _metricsCollector.RecordValidation("test-agent", "output-validation", true);
        expectedEvents++;
        
        _metricsCollector.RecordTokenUsage("test-agent", 1, 100, 50, "gpt-4");
        expectedEvents++;
        
        _metricsCollector.RecordApiCall("test-agent", "completion", "gpt-4");
        expectedEvents++;
        
        _metricsCollector.RecordStateStoreOperation("test-agent", "save", 50);
        expectedEvents++;
        
        _metricsCollector.RecordCustomEvent("test-event");
        expectedEvents++;

        // Assert
        Assert.AreEqual(expectedEvents, eventCount, "Should raise events for all recording methods");
    }
}
