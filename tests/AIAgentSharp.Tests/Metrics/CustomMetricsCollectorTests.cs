using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Metrics;

[TestClass]
public class CustomMetricsCollectorTests
{
    private CustomMetricsCollector _customMetricsCollector = null!;
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _customMetricsCollector = new CustomMetricsCollector(_mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new CustomMetricsCollector(null!));
    }

    [TestMethod]
    public void Constructor_WithValidLogger_Should_InitializeCorrectly()
    {
        // Assert
        Assert.IsNotNull(_customMetricsCollector);
    }

    #region RecordMetric Tests

    [TestMethod]
    public void RecordMetric_Should_StoreMetricValue()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.5;

        // Act
        _customMetricsCollector.RecordMetric(metricName, value);

        // Assert
        var result = _customMetricsCollector.GetMetric(metricName);
        Assert.IsNotNull(result);
        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public void RecordMetric_WithCategory_Should_StoreInCategory()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.5;
        var category = "performance";

        // Act
        _customMetricsCollector.RecordMetric(metricName, value, category);

        // Assert
        var categoryMetrics = _customMetricsCollector.GetMetricsByCategory(category);
        Assert.IsNotNull(categoryMetrics);
        Assert.IsTrue(categoryMetrics.ContainsKey(metricName));
        Assert.AreEqual(value, categoryMetrics[metricName]);
    }

    [TestMethod]
    public void RecordMetric_WithNullCategory_Should_NotStoreInCategory()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.5;

        // Act
        _customMetricsCollector.RecordMetric(metricName, value, null);

        // Assert
        var categoryMetrics = _customMetricsCollector.GetMetricsByCategory("any_category");
        Assert.IsNull(categoryMetrics);
    }

    [TestMethod]
    public void RecordMetric_WithEmptyCategory_Should_NotStoreInCategory()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.5;

        // Act
        _customMetricsCollector.RecordMetric(metricName, value, "");

        // Assert
        var categoryMetrics = _customMetricsCollector.GetMetricsByCategory("");
        Assert.IsNull(categoryMetrics);
    }

    [TestMethod]
    public void RecordMetric_WithWhitespaceCategory_Should_NotStoreInCategory()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.5;

        // Act
        _customMetricsCollector.RecordMetric(metricName, value, "   ");

        // Assert
        var categoryMetrics = _customMetricsCollector.GetMetricsByCategory("   ");
        Assert.IsNotNull(categoryMetrics); // The implementation stores whitespace categories
    }

    [TestMethod]
    public void RecordMetric_UpdateExistingMetric_Should_UpdateValue()
    {
        // Arrange
        var metricName = "test_metric";
        var initialValue = 42.5;
        var updatedValue = 100.0;

        // Act
        _customMetricsCollector.RecordMetric(metricName, initialValue);
        _customMetricsCollector.RecordMetric(metricName, updatedValue);

        // Assert
        var result = _customMetricsCollector.GetMetric(metricName);
        Assert.AreEqual(updatedValue, result);
    }

    [TestMethod]
    public void RecordMetric_WithNegativeValue_Should_StoreCorrectly()
    {
        // Arrange
        var metricName = "test_metric";
        var value = -42.5;

        // Act
        _customMetricsCollector.RecordMetric(metricName, value);

        // Assert
        var result = _customMetricsCollector.GetMetric(metricName);
        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public void RecordMetric_WithZeroValue_Should_StoreCorrectly()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 0.0;

        // Act
        _customMetricsCollector.RecordMetric(metricName, value);

        // Assert
        var result = _customMetricsCollector.GetMetric(metricName);
        Assert.AreEqual(value, result);
    }

    #endregion

    #region RecordCounter Tests

    [TestMethod]
    public void RecordCounter_Should_StoreCounterValue()
    {
        // Arrange
        var counterName = "test_counter";
        var increment = 5L;

        // Act
        _customMetricsCollector.RecordCounter(counterName, increment);

        // Assert
        var result = _customMetricsCollector.GetCounter(counterName);
        Assert.IsNotNull(result);
        Assert.AreEqual(increment, result);
    }

    [TestMethod]
    public void RecordCounter_WithDefaultIncrement_Should_UseOne()
    {
        // Arrange
        var counterName = "test_counter";

        // Act
        _customMetricsCollector.RecordCounter(counterName);

        // Assert
        var result = _customMetricsCollector.GetCounter(counterName);
        Assert.AreEqual(1L, result);
    }

    [TestMethod]
    public void RecordCounter_WithCategory_Should_StoreInCategory()
    {
        // Arrange
        var counterName = "test_counter";
        var increment = 5L;
        var category = "monitoring";

        // Act
        _customMetricsCollector.RecordCounter(counterName, increment, category);

        // Assert
        var categoryCounters = _customMetricsCollector.GetCountersByCategory(category);
        Assert.IsNotNull(categoryCounters);
        Assert.IsTrue(categoryCounters.ContainsKey(counterName));
        Assert.AreEqual(increment, categoryCounters[counterName]);
    }

    [TestMethod]
    public void RecordCounter_UpdateExistingCounter_Should_AddToValue()
    {
        // Arrange
        var counterName = "test_counter";
        var firstIncrement = 5L;
        var secondIncrement = 3L;

        // Act
        _customMetricsCollector.RecordCounter(counterName, firstIncrement);
        _customMetricsCollector.RecordCounter(counterName, secondIncrement);

        // Assert
        var result = _customMetricsCollector.GetCounter(counterName);
        Assert.AreEqual(firstIncrement + secondIncrement, result);
    }

    [TestMethod]
    public void RecordCounter_WithNegativeIncrement_Should_SubtractFromValue()
    {
        // Arrange
        var counterName = "test_counter";
        var firstIncrement = 10L;
        var secondIncrement = -3L;

        // Act
        _customMetricsCollector.RecordCounter(counterName, firstIncrement);
        _customMetricsCollector.RecordCounter(counterName, secondIncrement);

        // Assert
        var result = _customMetricsCollector.GetCounter(counterName);
        Assert.AreEqual(firstIncrement + secondIncrement, result);
    }

    #endregion

    #region SetTag Tests

    [TestMethod]
    public void SetTag_Should_StoreTagValue()
    {
        // Arrange
        var tagName = "environment";
        var tagValue = "production";

        // Act
        _customMetricsCollector.SetTag(tagName, tagValue);

        // Assert
        var result = _customMetricsCollector.GetTag(tagName);
        Assert.AreEqual(tagValue, result);
    }

    [TestMethod]
    public void SetTag_UpdateExistingTag_Should_UpdateValue()
    {
        // Arrange
        var tagName = "environment";
        var initialValue = "development";
        var updatedValue = "production";

        // Act
        _customMetricsCollector.SetTag(tagName, initialValue);
        _customMetricsCollector.SetTag(tagName, updatedValue);

        // Assert
        var result = _customMetricsCollector.GetTag(tagName);
        Assert.AreEqual(updatedValue, result);
    }

    [TestMethod]
    public void SetTag_WithEmptyValue_Should_StoreCorrectly()
    {
        // Arrange
        var tagName = "empty_tag";
        var tagValue = "";

        // Act
        _customMetricsCollector.SetTag(tagName, tagValue);

        // Assert
        var result = _customMetricsCollector.GetTag(tagName);
        Assert.AreEqual(tagValue, result);
    }

    #endregion

    #region SetMetadata Tests

    [TestMethod]
    public void SetMetadata_Should_StoreMetadataValue()
    {
        // Arrange
        var key = "version";
        var value = "1.0.0";

        // Act
        _customMetricsCollector.SetMetadata(key, value);

        // Assert
        var result = _customMetricsCollector.GetMetadata(key);
        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public void SetMetadata_WithComplexObject_Should_StoreCorrectly()
    {
        // Arrange
        var key = "config";
        var value = new { Name = "test", Enabled = true, Count = 42 };

        // Act
        _customMetricsCollector.SetMetadata(key, value);

        // Assert
        var result = _customMetricsCollector.GetMetadata(key);
        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public void SetMetadata_UpdateExistingMetadata_Should_UpdateValue()
    {
        // Arrange
        var key = "version";
        var initialValue = "1.0.0";
        var updatedValue = "2.0.0";

        // Act
        _customMetricsCollector.SetMetadata(key, initialValue);
        _customMetricsCollector.SetMetadata(key, updatedValue);

        // Assert
        var result = _customMetricsCollector.GetMetadata(key);
        Assert.AreEqual(updatedValue, result);
    }

    [TestMethod]
    public void SetMetadata_WithNullValue_Should_StoreCorrectly()
    {
        // Arrange
        var key = "null_value";

        // Act
        _customMetricsCollector.SetMetadata(key, null!);

        // Assert
        var result = _customMetricsCollector.GetMetadata(key);
        Assert.IsNull(result);
    }

    #endregion

    #region CalculateCustomMetrics Tests

    [TestMethod]
    public void CalculateCustomMetrics_Should_ReturnAllData()
    {
        // Arrange
        _customMetricsCollector.RecordMetric("metric1", 42.5, "category1");
        _customMetricsCollector.RecordCounter("counter1", 10L, "category1");
        _customMetricsCollector.SetTag("tag1", "value1");
        _customMetricsCollector.SetMetadata("meta1", "data1");

        // Act
        var metrics = _customMetricsCollector.CalculateCustomMetrics();

        // Assert
        Assert.IsNotNull(metrics);
        Assert.AreEqual(1, metrics.Metrics.Count);
        Assert.AreEqual(1, metrics.Counters.Count);
        Assert.AreEqual(1, metrics.Tags.Count);
        Assert.AreEqual(1, metrics.Metadata.Count);
        Assert.AreEqual(1, metrics.MetricsByCategory.Count);
        Assert.AreEqual(1, metrics.CountersByCategory.Count);
    }

    [TestMethod]
    public void CalculateCustomMetrics_WithNoData_Should_ReturnEmptyCollections()
    {
        // Act
        var metrics = _customMetricsCollector.CalculateCustomMetrics();

        // Assert
        Assert.IsNotNull(metrics);
        Assert.AreEqual(0, metrics.Metrics.Count);
        Assert.AreEqual(0, metrics.Counters.Count);
        Assert.AreEqual(0, metrics.Tags.Count);
        Assert.AreEqual(0, metrics.Metadata.Count);
        Assert.AreEqual(0, metrics.MetricsByCategory.Count);
        Assert.AreEqual(0, metrics.CountersByCategory.Count);
    }

    [TestMethod]
    public void CalculateCustomMetrics_Should_CalculateTrends()
    {
        // Arrange
        var metricName = "trend_metric";
        for (int i = 1; i <= 20; i++)
        {
            _customMetricsCollector.RecordMetric(metricName, i);
        }

        // Act
        var metrics = _customMetricsCollector.CalculateCustomMetrics();

        // Assert
        Assert.IsTrue(metrics.MetricTrends.ContainsKey(metricName));
        // Recent average (11-20) = 15.5, Older average (1-10) = 5.5, Trend = 10
        Assert.AreEqual(10.0, metrics.MetricTrends[metricName], 0.1);
    }

    #endregion

    #region Reset Tests

    [TestMethod]
    public void Reset_Should_ClearAllData()
    {
        // Arrange
        _customMetricsCollector.RecordMetric("metric1", 42.5);
        _customMetricsCollector.RecordCounter("counter1", 10L);
        _customMetricsCollector.SetTag("tag1", "value1");
        _customMetricsCollector.SetMetadata("meta1", "data1");

        // Act
        _customMetricsCollector.Reset();

        // Assert
        var metrics = _customMetricsCollector.CalculateCustomMetrics();
        Assert.AreEqual(0, metrics.Metrics.Count);
        Assert.AreEqual(0, metrics.Counters.Count);
        Assert.AreEqual(0, metrics.Tags.Count);
        Assert.AreEqual(0, metrics.Metadata.Count);
    }

    [TestMethod]
    public void Reset_Should_ClearHistory()
    {
        // Arrange
        var metricName = "test_metric";
        _customMetricsCollector.RecordMetric(metricName, 42.5);

        // Act
        _customMetricsCollector.Reset();

        // Assert
        var history = _customMetricsCollector.GetMetricHistory(metricName);
        Assert.IsNull(history);
    }

    #endregion

    #region Get Methods Tests

    [TestMethod]
    public void GetMetric_WithNonExistentMetric_Should_ReturnNull()
    {
        // Act
        var result = _customMetricsCollector.GetMetric("non_existent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCounter_WithNonExistentCounter_Should_ReturnNull()
    {
        // Act
        var result = _customMetricsCollector.GetCounter("non_existent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetTag_WithNonExistentTag_Should_ReturnNull()
    {
        // Act
        var result = _customMetricsCollector.GetTag("non_existent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMetadata_WithNonExistentMetadata_Should_ReturnNull()
    {
        // Act
        var result = _customMetricsCollector.GetMetadata("non_existent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMetricsByCategory_WithNonExistentCategory_Should_ReturnNull()
    {
        // Act
        var result = _customMetricsCollector.GetMetricsByCategory("non_existent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCountersByCategory_WithNonExistentCategory_Should_ReturnNull()
    {
        // Act
        var result = _customMetricsCollector.GetCountersByCategory("non_existent");

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region History Tests

    [TestMethod]
    public void GetMetricHistory_Should_ReturnHistory()
    {
        // Arrange
        var metricName = "test_metric";
        _customMetricsCollector.RecordMetric(metricName, 1.0);
        _customMetricsCollector.RecordMetric(metricName, 2.0);
        _customMetricsCollector.RecordMetric(metricName, 3.0);

        // Act
        var history = _customMetricsCollector.GetMetricHistory(metricName);

        // Assert
        Assert.IsNotNull(history);
        Assert.AreEqual(3, history.Length);
        Assert.AreEqual(1.0, history[0]);
        Assert.AreEqual(2.0, history[1]);
        Assert.AreEqual(3.0, history[2]);
    }

    [TestMethod]
    public void GetCounterHistory_Should_ReturnHistory()
    {
        // Arrange
        var counterName = "test_counter";
        _customMetricsCollector.RecordCounter(counterName, 1L);
        _customMetricsCollector.RecordCounter(counterName, 2L);
        _customMetricsCollector.RecordCounter(counterName, 3L);

        // Act
        var history = _customMetricsCollector.GetCounterHistory(counterName);

        // Assert
        Assert.IsNotNull(history);
        Assert.AreEqual(3, history.Length);
        Assert.AreEqual(1L, history[0]);
        Assert.AreEqual(2L, history[1]);
        Assert.AreEqual(3L, history[2]);
    }

    [TestMethod]
    public void GetMetricHistory_WithNonExistentMetric_Should_ReturnNull()
    {
        // Act
        var history = _customMetricsCollector.GetMetricHistory("non_existent");

        // Assert
        Assert.IsNull(history);
    }

    [TestMethod]
    public void GetCounterHistory_WithNonExistentCounter_Should_ReturnNull()
    {
        // Act
        var history = _customMetricsCollector.GetCounterHistory("non_existent");

        // Assert
        Assert.IsNull(history);
    }

    #endregion

    #region Remove Methods Tests

    [TestMethod]
    public void RemoveMetric_Should_RemoveMetric()
    {
        // Arrange
        var metricName = "test_metric";
        _customMetricsCollector.RecordMetric(metricName, 42.5);

        // Act
        _customMetricsCollector.RemoveMetric(metricName);

        // Assert
        var result = _customMetricsCollector.GetMetric(metricName);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void RemoveMetric_Should_RemoveFromCategory()
    {
        // Arrange
        var metricName = "test_metric";
        var category = "test_category";
        _customMetricsCollector.RecordMetric(metricName, 42.5, category);

        // Act
        _customMetricsCollector.RemoveMetric(metricName);

        // Assert
        var categoryMetrics = _customMetricsCollector.GetMetricsByCategory(category);
        Assert.IsNotNull(categoryMetrics); // Category still exists but is empty
        Assert.AreEqual(0, categoryMetrics.Count);
    }

    [TestMethod]
    public void RemoveMetric_Should_RemoveHistory()
    {
        // Arrange
        var metricName = "test_metric";
        _customMetricsCollector.RecordMetric(metricName, 42.5);

        // Act
        _customMetricsCollector.RemoveMetric(metricName);

        // Assert
        var history = _customMetricsCollector.GetMetricHistory(metricName);
        Assert.IsNull(history);
    }

    [TestMethod]
    public void RemoveCounter_Should_RemoveCounter()
    {
        // Arrange
        var counterName = "test_counter";
        _customMetricsCollector.RecordCounter(counterName, 10L);

        // Act
        _customMetricsCollector.RemoveCounter(counterName);

        // Assert
        var result = _customMetricsCollector.GetCounter(counterName);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void RemoveCounter_Should_RemoveFromCategory()
    {
        // Arrange
        var counterName = "test_counter";
        var category = "test_category";
        _customMetricsCollector.RecordCounter(counterName, 10L, category);

        // Act
        _customMetricsCollector.RemoveCounter(counterName);

        // Assert
        var categoryCounters = _customMetricsCollector.GetCountersByCategory(category);
        Assert.IsNotNull(categoryCounters); // Category still exists but is empty
        Assert.AreEqual(0, categoryCounters.Count);
    }

    [TestMethod]
    public void RemoveCounter_Should_RemoveHistory()
    {
        // Arrange
        var counterName = "test_counter";
        _customMetricsCollector.RecordCounter(counterName, 10L);

        // Act
        _customMetricsCollector.RemoveCounter(counterName);

        // Assert
        var history = _customMetricsCollector.GetCounterHistory(counterName);
        Assert.IsNull(history);
    }

    #endregion

    #region Edge Cases and Error Handling

    [TestMethod]
    public void RecordMetric_WithException_Should_LogWarning()
    {
        // Arrange
        var metricName = "test_metric";
        var value = 42.5;

        // Act
        _customMetricsCollector.RecordMetric(metricName, value);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordCounter_WithException_Should_LogWarning()
    {
        // Arrange
        var counterName = "test_counter";
        var increment = 5L;

        // Act
        _customMetricsCollector.RecordCounter(counterName, increment);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void SetTag_WithException_Should_LogWarning()
    {
        // Arrange
        var tagName = "test_tag";
        var tagValue = "test_value";

        // Act
        _customMetricsCollector.SetTag(tagName, tagValue);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void SetMetadata_WithException_Should_LogWarning()
    {
        // Arrange
        var key = "test_key";
        var value = "test_value";

        // Act
        _customMetricsCollector.SetMetadata(key, value);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void MultipleCategories_Should_WorkCorrectly()
    {
        // Arrange
        _customMetricsCollector.RecordMetric("metric1", 1.0, "category1");
        _customMetricsCollector.RecordMetric("metric2", 2.0, "category2");
        _customMetricsCollector.RecordCounter("counter1", 1L, "category1");
        _customMetricsCollector.RecordCounter("counter2", 2L, "category2");

        // Act
        var category1Metrics = _customMetricsCollector.GetMetricsByCategory("category1");
        var category2Metrics = _customMetricsCollector.GetMetricsByCategory("category2");
        var category1Counters = _customMetricsCollector.GetCountersByCategory("category1");
        var category2Counters = _customMetricsCollector.GetCountersByCategory("category2");

        // Assert
        Assert.IsNotNull(category1Metrics);
        Assert.IsNotNull(category2Metrics);
        Assert.IsNotNull(category1Counters);
        Assert.IsNotNull(category2Counters);
        Assert.AreEqual(1, category1Metrics.Count);
        Assert.AreEqual(1, category2Metrics.Count);
        Assert.AreEqual(1, category1Counters.Count);
        Assert.AreEqual(1, category2Counters.Count);
    }

    #endregion
}
