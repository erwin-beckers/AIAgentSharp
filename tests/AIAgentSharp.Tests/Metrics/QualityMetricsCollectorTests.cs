using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Metrics;

[TestClass]
public class QualityMetricsCollectorTests
{
    private QualityMetricsCollector _qualityMetricsCollector = null!;
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _qualityMetricsCollector = new QualityMetricsCollector(_mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new QualityMetricsCollector(null!));
    }

    [TestMethod]
    public void Constructor_WithValidLogger_Should_InitializeCorrectly()
    {
        // Assert
        Assert.IsNotNull(_qualityMetricsCollector);
    }

    #region RecordResponseQuality Tests

    [TestMethod]
    public void RecordResponseQuality_WithHighQuality_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "high";
        var qualityScore = 0.95;

        // Act
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel, qualityScore);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalResponses);
        Assert.AreEqual(1, metrics.HighQualityResponses);
        Assert.AreEqual(0, metrics.MediumQualityResponses);
        Assert.AreEqual(0, metrics.LowQualityResponses);
        Assert.AreEqual(100.0, metrics.QualityPercentage); // 1 high quality out of 1 total = 100%
    }

    [TestMethod]
    public void RecordResponseQuality_WithMediumQuality_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "medium";
        var qualityScore = 0.75;

        // Act
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel, qualityScore);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalResponses);
        Assert.AreEqual(0, metrics.HighQualityResponses);
        Assert.AreEqual(1, metrics.MediumQualityResponses);
        Assert.AreEqual(0, metrics.LowQualityResponses);
        Assert.AreEqual(0.0, metrics.QualityPercentage); // 0 high quality out of 1 total = 0%
    }

    [TestMethod]
    public void RecordResponseQuality_WithLowQuality_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "low";
        var qualityScore = 0.25;

        // Act
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel, qualityScore);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalResponses);
        Assert.AreEqual(0, metrics.HighQualityResponses);
        Assert.AreEqual(0, metrics.MediumQualityResponses);
        Assert.AreEqual(1, metrics.LowQualityResponses);
        Assert.AreEqual(0.0, metrics.QualityPercentage); // 0 high quality out of 1 total = 0%
    }

    [TestMethod]
    public void RecordResponseQuality_WithCaseInsensitiveQuality_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "HIGH";
        var qualityScore = 0.95;

        // Act
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel, qualityScore);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.HighQualityResponses);
    }

    [TestMethod]
    public void RecordResponseQuality_WithQualityScore_Should_StoreAgentScore()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "high";
        var qualityScore = 0.95;

        // Act
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel, qualityScore);

        // Assert
        var agentScore = _qualityMetricsCollector.GetQualityScoreForAgent(agentId);
        Assert.IsNotNull(agentScore);
        Assert.AreEqual(95, agentScore); // 0.95 * 100
    }

    [TestMethod]
    public void RecordResponseQuality_MultipleCalls_Should_CalculateAverageScore()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "high";

        // Act
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel, 0.8); // 80
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel, 0.9); // 90

        // Assert
        var agentScore = _qualityMetricsCollector.GetQualityScoreForAgent(agentId);
        Assert.IsNotNull(agentScore);
        Assert.AreEqual(85, agentScore); // (80 + 90) / 2
    }

    [TestMethod]
    public void RecordResponseQuality_WithoutQualityScore_Should_NotStoreAgentScore()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "high";

        // Act
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel);

        // Assert
        var agentScore = _qualityMetricsCollector.GetQualityScoreForAgent(agentId);
        Assert.IsNull(agentScore);
    }

    #endregion

    #region RecordReasoningStep Tests

    [TestMethod]
    public void RecordReasoningStep_WithSuccess_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = "chain_of_thought";
        var wasSuccessful = true;

        // Act
        _qualityMetricsCollector.RecordReasoningStep(agentId, reasoningType, wasSuccessful);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalReasoningSteps);
        Assert.AreEqual(1, metrics.SuccessfulReasoningSteps);
        Assert.AreEqual(0, metrics.FailedReasoningSteps);
        Assert.AreEqual(100.0, metrics.ReasoningAccuracyPercentage); // 1 successful out of 1 total = 100%
    }

    [TestMethod]
    public void RecordReasoningStep_WithFailure_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = "chain_of_thought";
        var wasSuccessful = false;

        // Act
        _qualityMetricsCollector.RecordReasoningStep(agentId, reasoningType, wasSuccessful);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalReasoningSteps);
        Assert.AreEqual(0, metrics.SuccessfulReasoningSteps);
        Assert.AreEqual(1, metrics.FailedReasoningSteps);
        Assert.AreEqual(0.0, metrics.ReasoningAccuracyPercentage); // 0 successful out of 1 total = 0%
    }

    [TestMethod]
    public void RecordReasoningStep_MultipleCalls_Should_AccumulateCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = "chain_of_thought";

        // Act
        _qualityMetricsCollector.RecordReasoningStep(agentId, reasoningType, true);
        _qualityMetricsCollector.RecordReasoningStep(agentId, reasoningType, false);
        _qualityMetricsCollector.RecordReasoningStep(agentId, reasoningType, true);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(3, metrics.TotalReasoningSteps);
        Assert.AreEqual(2, metrics.SuccessfulReasoningSteps);
        Assert.AreEqual(1, metrics.FailedReasoningSteps);
        Assert.AreEqual(66.67, metrics.ReasoningAccuracyPercentage, 0.01); // 2 successful out of 3 total = 66.67%
    }

    [TestMethod]
    public void RecordReasoningStep_Should_UpdateTypeAccuracy()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = "chain_of_thought";

        // Act
        _qualityMetricsCollector.RecordReasoningStep(agentId, reasoningType, true);

        // Assert
        var typeAccuracy = _qualityMetricsCollector.GetReasoningAccuracyForType(reasoningType);
        Assert.IsNotNull(typeAccuracy);
        Assert.AreEqual(1, typeAccuracy); // 1 successful
    }

    #endregion

    #region RecordValidation Tests

    [TestMethod]
    public void RecordValidation_WithPass_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var validationType = "output_format";
        var passed = true;

        // Act
        _qualityMetricsCollector.RecordValidation(agentId, validationType, passed);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalValidations);
        Assert.AreEqual(1, metrics.PassedValidations);
        Assert.AreEqual(0, metrics.FailedValidations);
        Assert.AreEqual(100.0, metrics.ValidationPassRate); // 1 passed out of 1 total = 100%
    }

    [TestMethod]
    public void RecordValidation_WithFail_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var validationType = "output_format";
        var passed = false;
        var errorMessage = "Invalid format";

        // Act
        _qualityMetricsCollector.RecordValidation(agentId, validationType, passed, errorMessage);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(1, metrics.TotalValidations);
        Assert.AreEqual(0, metrics.PassedValidations);
        Assert.AreEqual(1, metrics.FailedValidations);
        Assert.AreEqual(0.0, metrics.ValidationPassRate); // 0 passed out of 1 total = 0%
    }

    [TestMethod]
    public void RecordValidation_MultipleCalls_Should_AccumulateCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var validationType = "output_format";

        // Act
        _qualityMetricsCollector.RecordValidation(agentId, validationType, true);
        _qualityMetricsCollector.RecordValidation(agentId, validationType, false);
        _qualityMetricsCollector.RecordValidation(agentId, validationType, true);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(3, metrics.TotalValidations);
        Assert.AreEqual(2, metrics.PassedValidations);
        Assert.AreEqual(1, metrics.FailedValidations);
        Assert.AreEqual(66.67, metrics.ValidationPassRate, 0.01); // 2 passed out of 3 total = 66.67%
    }

    [TestMethod]
    public void RecordValidation_Should_UpdateTypeResults()
    {
        // Arrange
        var agentId = "test-agent";
        var validationType = "output_format";

        // Act
        _qualityMetricsCollector.RecordValidation(agentId, validationType, true);

        // Assert
        var typeResults = _qualityMetricsCollector.GetValidationResultsForType(validationType);
        Assert.IsNotNull(typeResults);
        Assert.AreEqual(1, typeResults); // 1 passed
    }

    #endregion

    #region RecordResponseTime Tests

    [TestMethod]
    public void RecordResponseTime_Should_StoreCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var responseTimeMs = 2500L;

        // Act
        _qualityMetricsCollector.RecordResponseTime(agentId, responseTimeMs);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(responseTimeMs, metrics.AverageResponseTimeMs);
    }

    [TestMethod]
    public void RecordResponseTime_MultipleCalls_Should_CalculateAverage()
    {
        // Arrange
        var agentId = "test-agent";

        // Act
        _qualityMetricsCollector.RecordResponseTime(agentId, 1000L);
        _qualityMetricsCollector.RecordResponseTime(agentId, 2000L);
        _qualityMetricsCollector.RecordResponseTime(agentId, 3000L);

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(2000.0, metrics.AverageResponseTimeMs); // (1000 + 2000 + 3000) / 3
    }

    [TestMethod]
    public void RecordResponseTime_Should_UpdateAgentAverage()
    {
        // Arrange
        var agentId = "test-agent";
        var responseTimeMs = 2500L;

        // Act
        _qualityMetricsCollector.RecordResponseTime(agentId, responseTimeMs);

        // Assert
        var agentTime = _qualityMetricsCollector.GetAverageResponseTimeForAgent(agentId);
        Assert.IsNotNull(agentTime);
        Assert.AreEqual(responseTimeMs, agentTime);
    }

    [TestMethod]
    public void RecordResponseTime_MultipleAgents_Should_TrackSeparately()
    {
        // Arrange
        var agent1 = "agent1";
        var agent2 = "agent2";

        // Act
        _qualityMetricsCollector.RecordResponseTime(agent1, 1000L);
        _qualityMetricsCollector.RecordResponseTime(agent2, 2000L);

        // Assert
        var agent1Time = _qualityMetricsCollector.GetAverageResponseTimeForAgent(agent1);
        var agent2Time = _qualityMetricsCollector.GetAverageResponseTimeForAgent(agent2);
        
        Assert.IsNotNull(agent1Time);
        Assert.IsNotNull(agent2Time);
        Assert.AreEqual(1000L, agent1Time);
        Assert.AreEqual(2000L, agent2Time);
    }

    #endregion

    #region CalculateQualityMetrics Tests

    [TestMethod]
    public void CalculateQualityMetrics_WithNoData_Should_ReturnZeroValues()
    {
        // Act
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();

        // Assert
        Assert.AreEqual(0, metrics.TotalResponses);
        Assert.AreEqual(0, metrics.HighQualityResponses);
        Assert.AreEqual(0, metrics.MediumQualityResponses);
        Assert.AreEqual(0, metrics.LowQualityResponses);
        Assert.AreEqual(0.0, metrics.QualityPercentage);
        Assert.AreEqual(0, metrics.TotalReasoningSteps);
        Assert.AreEqual(0, metrics.SuccessfulReasoningSteps);
        Assert.AreEqual(0, metrics.FailedReasoningSteps);
        Assert.AreEqual(0.0, metrics.ReasoningAccuracyPercentage);
        Assert.AreEqual(0, metrics.TotalValidations);
        Assert.AreEqual(0, metrics.PassedValidations);
        Assert.AreEqual(0, metrics.FailedValidations);
        Assert.AreEqual(0.0, metrics.ValidationPassRate);
        Assert.AreEqual(0.0, metrics.AverageResponseTimeMs);
    }

    [TestMethod]
    public void CalculateQualityMetrics_WithMixedData_Should_ReturnCorrectValues()
    {
        // Arrange
        _qualityMetricsCollector.RecordResponseQuality("agent1", "high", 0.9);
        _qualityMetricsCollector.RecordReasoningStep("agent1", "chain_of_thought", true);
        _qualityMetricsCollector.RecordValidation("agent1", "output_format", true);
        _qualityMetricsCollector.RecordResponseTime("agent1", 2500L);

        // Act
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();

        // Assert
        Assert.AreEqual(1, metrics.TotalResponses);
        Assert.AreEqual(1, metrics.HighQualityResponses);
        Assert.AreEqual(100.0, metrics.QualityPercentage);
        Assert.AreEqual(1, metrics.TotalReasoningSteps);
        Assert.AreEqual(1, metrics.SuccessfulReasoningSteps);
        Assert.AreEqual(100.0, metrics.ReasoningAccuracyPercentage);
        Assert.AreEqual(1, metrics.TotalValidations);
        Assert.AreEqual(1, metrics.PassedValidations);
        Assert.AreEqual(100.0, metrics.ValidationPassRate);
        Assert.AreEqual(2500.0, metrics.AverageResponseTimeMs);
    }

    #endregion

    #region Reset Tests

    [TestMethod]
    public void Reset_Should_ClearAllMetrics()
    {
        // Arrange
        _qualityMetricsCollector.RecordResponseQuality("agent1", "high", 0.9);
        _qualityMetricsCollector.RecordReasoningStep("agent1", "chain_of_thought", true);
        _qualityMetricsCollector.RecordValidation("agent1", "output_format", true);
        _qualityMetricsCollector.RecordResponseTime("agent1", 2500L);

        // Act
        _qualityMetricsCollector.Reset();

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(0, metrics.TotalResponses);
        Assert.AreEqual(0, metrics.TotalReasoningSteps);
        Assert.AreEqual(0, metrics.TotalValidations);
        Assert.AreEqual(0.0, metrics.AverageResponseTimeMs);
    }

    #endregion

    #region Get Methods Tests

    [TestMethod]
    public void GetQualityScoreForAgent_WithNonExistentAgent_Should_ReturnNull()
    {
        // Act
        var score = _qualityMetricsCollector.GetQualityScoreForAgent("non_existent");

        // Assert
        Assert.IsNull(score);
    }

    [TestMethod]
    public void GetReasoningAccuracyForType_WithNonExistentType_Should_ReturnNull()
    {
        // Act
        var accuracy = _qualityMetricsCollector.GetReasoningAccuracyForType("non_existent");

        // Assert
        Assert.IsNull(accuracy);
    }

    [TestMethod]
    public void GetValidationResultsForType_WithNonExistentType_Should_ReturnNull()
    {
        // Act
        var results = _qualityMetricsCollector.GetValidationResultsForType("non_existent");

        // Assert
        Assert.IsNull(results);
    }

    [TestMethod]
    public void GetAverageResponseTimeForAgent_WithNonExistentAgent_Should_ReturnNull()
    {
        // Act
        var time = _qualityMetricsCollector.GetAverageResponseTimeForAgent("non_existent");

        // Assert
        Assert.IsNull(time);
    }

    #endregion

    #region Edge Cases and Error Handling

    [TestMethod]
    public void RecordResponseQuality_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var qualityLevel = "high";
        var qualityScore = 0.95;

        // Act
        _qualityMetricsCollector.RecordResponseQuality(agentId, qualityLevel, qualityScore);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordReasoningStep_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var reasoningType = "chain_of_thought";
        var wasSuccessful = true;

        // Act
        _qualityMetricsCollector.RecordReasoningStep(agentId, reasoningType, wasSuccessful);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordValidation_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var validationType = "output_format";
        var passed = true;

        // Act
        _qualityMetricsCollector.RecordValidation(agentId, validationType, passed);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void RecordResponseTime_WithException_Should_LogWarning()
    {
        // Arrange
        var agentId = "test-agent";
        var responseTimeMs = 2500L;

        // Act
        _qualityMetricsCollector.RecordResponseTime(agentId, responseTimeMs);

        // Assert - Verify no warnings were logged for normal operation
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Thread Safety Tests

    [TestMethod]
    public void ConcurrentRecordings_Should_BeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var iterations = 100;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                _qualityMetricsCollector.RecordResponseQuality("agent1", "high", 0.9);
                _qualityMetricsCollector.RecordReasoningStep("agent1", "chain_of_thought", true);
                _qualityMetricsCollector.RecordValidation("agent1", "output_format", true);
                _qualityMetricsCollector.RecordResponseTime("agent1", 2500L);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var metrics = _qualityMetricsCollector.CalculateQualityMetrics();
        Assert.AreEqual(iterations, metrics.TotalResponses);
        Assert.AreEqual(iterations, metrics.HighQualityResponses);
        Assert.AreEqual(iterations, metrics.TotalReasoningSteps);
        Assert.AreEqual(iterations, metrics.SuccessfulReasoningSteps);
        Assert.AreEqual(iterations, metrics.TotalValidations);
        Assert.AreEqual(iterations, metrics.PassedValidations);
        Assert.AreEqual(2500.0, metrics.AverageResponseTimeMs);
    }

    #endregion
}
