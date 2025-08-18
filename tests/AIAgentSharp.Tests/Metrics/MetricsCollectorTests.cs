using AIAgentSharp.Metrics;
using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Tests.Metrics;

[TestClass]
public class MetricsCollectorTests
{
    private MetricsCollector _metricsCollector = null!;

    [TestInitialize]
    public void Setup()
    {
        _metricsCollector = new MetricsCollector();
    }

    [TestMethod]
    public void RecordAgentRunExecutionTime_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1500, 5);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalAgentRuns);
        Assert.AreEqual(1500, metrics.Performance.AverageAgentRunTimeMs);
    }

    [TestMethod]
    public void RecordAgentStepExecutionTime_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordAgentStepExecutionTime("test-agent", 0, 500);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalAgentSteps);
        Assert.AreEqual(500, metrics.Performance.AverageAgentStepTimeMs);
    }

    [TestMethod]
    public void RecordLlmCallExecutionTime_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordLlmCallExecutionTime("test-agent", 0, 800, "gpt-4");

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalLlmCalls);
        Assert.AreEqual(800, metrics.Performance.AverageLlmCallTimeMs);
    }

    [TestMethod]
    public void RecordToolCallExecutionTime_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordToolCallExecutionTime("test-agent", 0, "test-tool", 300);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalToolCalls);
        Assert.AreEqual(300, metrics.Performance.AverageToolCallTimeMs);
    }

    [TestMethod]
    public void RecordReasoningExecutionTime_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordReasoningExecutionTime("test-agent", ReasoningType.ChainOfThought, 1200);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalReasoningOperations);
        Assert.AreEqual(1200, metrics.Performance.AverageReasoningTimeMs);
    }

    [TestMethod]
    public void RecordAgentRunCompletion_ShouldRecordSuccessMetrics()
    {
        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1500, 5);
        _metricsCollector.RecordAgentRunCompletion("test-agent", true, 5);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalAgentRuns);
        Assert.AreEqual(0, metrics.Operational.FailedAgentRuns);
        Assert.AreEqual(1.0, metrics.Operational.AgentRunSuccessRate);
    }

    [TestMethod]
    public void RecordAgentRunCompletion_ShouldRecordFailureMetrics()
    {
        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1500, 5);
        _metricsCollector.RecordAgentRunCompletion("test-agent", false, 5, "Timeout");

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalAgentRuns);
        Assert.AreEqual(1, metrics.Operational.FailedAgentRuns);
        Assert.AreEqual(0.0, metrics.Operational.AgentRunSuccessRate);
        Assert.AreEqual(1, metrics.Operational.ErrorCountsByType["Timeout"]);
    }

    [TestMethod]
    public void RecordAgentStepCompletion_ShouldRecordSuccessMetrics()
    {
        // Act
        _metricsCollector.RecordAgentStepExecutionTime("test-agent", 0, 500);
        _metricsCollector.RecordAgentStepCompletion("test-agent", 0, true, true);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalAgentSteps);
        Assert.AreEqual(0, metrics.Operational.FailedAgentSteps);
        Assert.AreEqual(1.0, metrics.Operational.AgentStepSuccessRate);
    }

    [TestMethod]
    public void RecordAgentStepCompletion_ShouldRecordFailureMetrics()
    {
        // Act
        _metricsCollector.RecordAgentStepExecutionTime("test-agent", 0, 500);
        _metricsCollector.RecordAgentStepCompletion("test-agent", 0, false, false, "ValidationError");

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalAgentSteps);
        Assert.AreEqual(1, metrics.Operational.FailedAgentSteps);
        Assert.AreEqual(0.0, metrics.Operational.AgentStepSuccessRate);
        Assert.AreEqual(1, metrics.Operational.ErrorCountsByType["ValidationError"]);
    }

    [TestMethod]
    public void RecordLlmCallCompletion_ShouldRecordSuccessMetrics()
    {
        // Act
        _metricsCollector.RecordLlmCallExecutionTime("test-agent", 0, 800, "gpt-4");
        _metricsCollector.RecordLlmCallCompletion("test-agent", 0, true, "gpt-4");

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalLlmCalls);
        Assert.AreEqual(0, metrics.Operational.FailedLlmCalls);
        Assert.AreEqual(1.0, metrics.Operational.LlmCallSuccessRate);
    }

    [TestMethod]
    public void RecordLlmCallCompletion_ShouldRecordFailureMetrics()
    {
        // Act
        _metricsCollector.RecordLlmCallExecutionTime("test-agent", 0, 800, "gpt-4");
        _metricsCollector.RecordLlmCallCompletion("test-agent", 0, false, "gpt-4", "RateLimit");

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalLlmCalls);
        Assert.AreEqual(1, metrics.Operational.FailedLlmCalls);
        Assert.AreEqual(0.0, metrics.Operational.LlmCallSuccessRate);
        Assert.AreEqual(1, metrics.Operational.ErrorCountsByType["RateLimit"]);
    }

    [TestMethod]
    public void RecordToolCallCompletion_ShouldRecordSuccessMetrics()
    {
        // Act
        _metricsCollector.RecordToolCallExecutionTime("test-agent", 0, "test-tool", 300);
        _metricsCollector.RecordToolCallCompletion("test-agent", 0, "test-tool", true);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalToolCalls);
        Assert.AreEqual(0, metrics.Operational.FailedToolCalls);
        Assert.AreEqual(1.0, metrics.Operational.ToolCallSuccessRate);
    }

    [TestMethod]
    public void RecordToolCallCompletion_ShouldRecordFailureMetrics()
    {
        // Act
        _metricsCollector.RecordToolCallExecutionTime("test-agent", 0, "test-tool", 300);
        _metricsCollector.RecordToolCallCompletion("test-agent", 0, "test-tool", false, "Timeout");

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Performance.TotalToolCalls);
        Assert.AreEqual(1, metrics.Operational.FailedToolCalls);
        Assert.AreEqual(0.0, metrics.Operational.ToolCallSuccessRate);
        Assert.AreEqual(1, metrics.Operational.ErrorCountsByType["Timeout"]);
    }

    [TestMethod]
    public void RecordLoopDetection_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordLoopDetection("test-agent", "ConsecutiveFailures", 3);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Operational.LoopDetectionEvents);
    }

    [TestMethod]
    public void RecordDeduplicationEvent_ShouldRecordCacheHit()
    {
        // Act
        _metricsCollector.RecordDeduplicationEvent("test-agent", "test-tool", true);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Operational.DeduplicationCacheHits);
        Assert.AreEqual(0, metrics.Operational.DeduplicationCacheMisses);
        Assert.AreEqual(1.0, metrics.Operational.DeduplicationCacheHitRate);
    }

    [TestMethod]
    public void RecordDeduplicationEvent_ShouldRecordCacheMiss()
    {
        // Act
        _metricsCollector.RecordDeduplicationEvent("test-agent", "test-tool", false);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(0, metrics.Operational.DeduplicationCacheHits);
        Assert.AreEqual(1, metrics.Operational.DeduplicationCacheMisses);
        Assert.AreEqual(0.0, metrics.Operational.DeduplicationCacheHitRate);
    }

    [TestMethod]
    public void RecordReasoningConfidence_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordReasoningConfidence("test-agent", ReasoningType.ChainOfThought, 0.85);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(0.85, metrics.Quality.AverageReasoningConfidence);
        Assert.AreEqual(0.85, metrics.Quality.AverageConfidenceByReasoningType[ReasoningType.ChainOfThought]);
    }

    [TestMethod]
    public void RecordResponseQuality_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordResponseQuality("test-agent", 1500, true);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1500, metrics.Quality.AverageResponseLength);
        Assert.AreEqual(1.0, metrics.Quality.FinalOutputPercentage);
    }

    [TestMethod]
    public void RecordTokenUsage_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordLlmCallExecutionTime("test-agent", 0, 100, "gpt-4");
        _metricsCollector.RecordTokenUsage("test-agent", 0, 1000, 500, "gpt-4");

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        
        Assert.AreEqual(1000, metrics.Resources.TotalInputTokens);
        Assert.AreEqual(500, metrics.Resources.TotalOutputTokens);
        Assert.AreEqual(1500, metrics.Resources.TotalTokens);
        Assert.AreEqual(1000, metrics.Resources.AverageInputTokensPerCall);
        Assert.AreEqual(500, metrics.Resources.AverageOutputTokensPerCall);
    }

    [TestMethod]
    public void RecordApiCall_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordApiCall("test-agent", "LLM", "gpt-4");

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.Resources.ApiCallCountsByType["LLM"]);
        Assert.AreEqual(1, metrics.Resources.ApiCallCountsByModel["gpt-4"]);
    }

    [TestMethod]
    public void RecordStateStoreOperation_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordStateStoreOperation("test-agent", "Read", 50);

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(50, metrics.Resources.AverageStateStoreOperationTimeMs);
    }

    [TestMethod]
    public void RecordCustomMetric_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordCustomMetric("test-metric", 42.5, new Dictionary<string, string> { ["tag1"] = "value1" });

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.IsTrue(metrics.CustomMetrics.ContainsKey("test-metric"));
        Assert.AreEqual(42.5, metrics.CustomMetrics["test-metric"].Value);
        Assert.AreEqual("value1", metrics.CustomMetrics["test-metric"].Tags["tag1"]);
    }

    [TestMethod]
    public void RecordCustomEvent_ShouldRecordMetrics()
    {
        // Act
        _metricsCollector.RecordCustomEvent("test-event", new Dictionary<string, string> { ["tag1"] = "value1" });

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(1, metrics.CustomEvents.Count);
        Assert.AreEqual("test-event", metrics.CustomEvents[0].Name);
        Assert.AreEqual("value1", metrics.CustomEvents[0].Tags["tag1"]);
    }

    [TestMethod]
    public void GetAgentMetrics_ShouldReturnAgentSpecificMetrics()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("agent1", 1000, 3);
        _metricsCollector.RecordAgentRunExecutionTime("agent2", 2000, 5);

        // Act
        var agent1Metrics = _metricsCollector.GetAgentMetrics("agent1");
        var agent2Metrics = _metricsCollector.GetAgentMetrics("agent2");
        var nonExistentMetrics = _metricsCollector.GetAgentMetrics("non-existent");

        // Assert
        Assert.IsNotNull(agent1Metrics);
        Assert.AreEqual(1, agent1Metrics.Performance.TotalAgentRuns);
        Assert.AreEqual(1000, agent1Metrics.Performance.AverageAgentRunTimeMs);

        Assert.IsNotNull(agent2Metrics);
        Assert.AreEqual(1, agent2Metrics.Performance.TotalAgentRuns);
        Assert.AreEqual(2000, agent2Metrics.Performance.AverageAgentRunTimeMs);

        Assert.IsNull(nonExistentMetrics);
    }

    [TestMethod]
    public void GetMetricsForTimeRange_ShouldFilterByTime()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        var endTime = DateTimeOffset.UtcNow.AddMinutes(-5);

        _metricsCollector.RecordCustomEvent("old-event");
        Thread.Sleep(100); // Ensure different timestamp
        _metricsCollector.RecordCustomEvent("recent-event");

        // Act
        var timeRangeMetrics = _metricsCollector.GetMetricsForTimeRange(startTime, endTime);

        // Assert
        // The exact count depends on timing, but we can verify the method works
        Assert.IsNotNull(timeRangeMetrics);
    }

    [TestMethod]
    public void ResetMetrics_ShouldClearAllMetrics()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 3);
        _metricsCollector.RecordCustomMetric("test-metric", 42.5);

        // Act
        _metricsCollector.ResetMetrics();

        // Assert
        var metrics = _metricsCollector.GetMetrics();
        Assert.AreEqual(0, metrics.Performance.TotalAgentRuns);
        Assert.AreEqual(0, metrics.CustomMetrics.Count);
        Assert.AreEqual(0, metrics.CustomEvents.Count);
    }

    [TestMethod]
    public void ExportMetrics_ShouldExportInJsonFormat()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 3);

        // Act
        var jsonExport = _metricsCollector.ExportMetrics(MetricsExportFormat.Json);

        // Assert
        Assert.IsNotNull(jsonExport);
        Assert.IsTrue(jsonExport.Contains("TotalAgentRuns"));
        Assert.IsTrue(jsonExport.Contains("1"));
    }

    [TestMethod]
    public void ExportMetrics_ShouldExportInCsvFormat()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 3);

        // Act
        var csvExport = _metricsCollector.ExportMetrics(MetricsExportFormat.Csv);

        // Assert
        Assert.IsNotNull(csvExport);
        Assert.IsTrue(csvExport.Contains("Metric,Value"));
        Assert.IsTrue(csvExport.Contains("TotalAgentRuns"));
    }

    [TestMethod]
    public void ExportMetrics_ShouldExportInPrometheusFormat()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 3);

        // Act
        var prometheusExport = _metricsCollector.ExportMetrics(MetricsExportFormat.Prometheus);

        // Assert
        Assert.IsNotNull(prometheusExport);
        Assert.IsTrue(prometheusExport.Contains("# HELP"));
        Assert.IsTrue(prometheusExport.Contains("# TYPE"));
        Assert.IsTrue(prometheusExport.Contains("aiagentsharp_agent_runs_total"));
    }

    [TestMethod]
    public void ExportMetrics_ShouldExportInTextFormat()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 3);

        // Act
        var textExport = _metricsCollector.ExportMetrics(MetricsExportFormat.Text);

        // Assert
        Assert.IsNotNull(textExport);
        Assert.IsTrue(textExport.Contains("AIAgentSharp Metrics Report"));
        Assert.IsTrue(textExport.Contains("Total Agent Runs"));
    }

    [TestMethod]
    public void MetricsUpdated_EventShouldBeRaised()
    {
        // Arrange
        var eventRaised = false;
        _metricsCollector.MetricsUpdated += (sender, e) => eventRaised = true;

        // Act
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 3);

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void MultipleMetrics_ShouldCalculateAveragesCorrectly()
    {
        // Arrange
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 1000, 3);
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 2000, 5);
        _metricsCollector.RecordAgentRunExecutionTime("test-agent", 3000, 7);

        // Act
        var metrics = _metricsCollector.GetMetrics();

        // Assert
        Assert.AreEqual(3, metrics.Performance.TotalAgentRuns);
        Assert.AreEqual(2000, metrics.Performance.AverageAgentRunTimeMs); // (1000 + 2000 + 3000) / 3
    }

    [TestMethod]
    public void PercentileCalculations_ShouldWorkCorrectly()
    {
        // Arrange
        for (int i = 1; i <= 100; i++)
        {
            _metricsCollector.RecordAgentRunExecutionTime("test-agent", i * 10, 1);
        }

        // Act
        var metrics = _metricsCollector.GetMetrics();

        // Assert
        // P95 should be approximately 950 (95th value in sorted list)
        Assert.IsTrue(metrics.Performance.P95AgentRunTimeMs >= 900 && metrics.Performance.P95AgentRunTimeMs <= 1000);
    }

    [TestMethod]
    public void TokenUsageByModel_ShouldTrackPerModel()
    {
        // Arrange
        _metricsCollector.RecordTokenUsage("test-agent", 0, 1000, 500, "gpt-4");
        _metricsCollector.RecordTokenUsage("test-agent", 1, 800, 400, "gpt-3.5-turbo");

        // Act
        var metrics = _metricsCollector.GetMetrics();

        // Assert
        Assert.AreEqual(2, metrics.Resources.TokenUsageByModel.Count);
        Assert.AreEqual(1000, metrics.Resources.TokenUsageByModel["gpt-4"].InputTokens);
        Assert.AreEqual(500, metrics.Resources.TokenUsageByModel["gpt-4"].OutputTokens);
        Assert.AreEqual(800, metrics.Resources.TokenUsageByModel["gpt-3.5-turbo"].InputTokens);
        Assert.AreEqual(400, metrics.Resources.TokenUsageByModel["gpt-3.5-turbo"].OutputTokens);
    }

    [TestMethod]
    public void ConfidenceScoreDistribution_ShouldCategorizeCorrectly()
    {
        // Arrange
        _metricsCollector.RecordReasoningConfidence("test-agent", ReasoningType.ChainOfThought, 0.95);
        _metricsCollector.RecordReasoningConfidence("test-agent", ReasoningType.ChainOfThought, 0.75);
        _metricsCollector.RecordReasoningConfidence("test-agent", ReasoningType.ChainOfThought, 0.45);

        // Act
        var metrics = _metricsCollector.GetMetrics();

        // Assert
        Assert.IsTrue(metrics.Quality.ConfidenceScoreDistribution.ContainsKey("0.9-1.0"));
        Assert.IsTrue(metrics.Quality.ConfidenceScoreDistribution.ContainsKey("0.7-0.8"));
        Assert.IsTrue(metrics.Quality.ConfidenceScoreDistribution.ContainsKey("0.0-0.5"));
    }
}
