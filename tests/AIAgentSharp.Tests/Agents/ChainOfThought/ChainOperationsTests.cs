using AIAgentSharp;
using AIAgentSharp.Agents.ChainOfThought;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Agents.ChainOfThought;

[TestClass]
public class ChainOperationsTests
{
    private ChainOperations _chainOperations = null!;
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _chainOperations = new ChainOperations(_mockLogger.Object);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Act & Assert
        new ChainOperations(null!);
    }

    [TestMethod]
    public void CreateChain_Should_ReturnValidReasoningChain_When_GoalProvided()
    {
        // Arrange
        var goal = "Test goal";

        // Act
        var chain = _chainOperations.CreateChain(goal);

        // Assert
        Assert.IsNotNull(chain);
        Assert.AreEqual(goal, chain.Goal);
        Assert.IsTrue(chain.CreatedUtc > DateTimeOffset.MinValue);
        Assert.IsTrue(chain.CreatedUtc <= DateTimeOffset.UtcNow);
        Assert.IsFalse(chain.IsComplete);
        Assert.AreEqual(0, chain.Steps.Count);
    }

    [TestMethod]
    public void CreateChain_Should_HandleEmptyGoal_When_EmptyStringProvided()
    {
        // Arrange
        var goal = "";

        // Act
        var chain = _chainOperations.CreateChain(goal);

        // Assert
        Assert.IsNotNull(chain);
        Assert.AreEqual("", chain.Goal);
        Assert.IsTrue(chain.CreatedUtc > DateTimeOffset.MinValue);
    }

    [TestMethod]
    public void CreateChain_Should_HandleNullGoal_When_NullProvided()
    {
        // Arrange
        string? goal = null;

        // Act
        var chain = _chainOperations.CreateChain(goal!);

        // Assert
        Assert.IsNotNull(chain);
        Assert.IsNull(chain.Goal);
    }

    [TestMethod]
    public void AddStep_Should_AddStepToChain_When_ValidParametersProvided()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var reasoning = "Test reasoning";
        var stepType = ReasoningStepType.Analysis;
        var confidence = 0.85;
        var insights = new List<string> { "Insight 1", "Insight 2" };

        // Act
        var step = _chainOperations.AddStep(chain, reasoning, stepType, confidence, insights);

        // Assert
        Assert.IsNotNull(step);
        Assert.AreEqual(reasoning, step.Reasoning);
        Assert.AreEqual(stepType, step.StepType);
        Assert.AreEqual(confidence, step.Confidence);
        Assert.AreEqual(insights, step.Insights);
        Assert.AreEqual(1, chain.Steps.Count);
        Assert.AreEqual(step, chain.Steps[0]);
    }

    [TestMethod]
    public void AddStep_Should_AssignCorrectStepNumber_When_MultipleStepsAdded()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");

        // Act
        var step1 = _chainOperations.AddStep(chain, "Reasoning 1", ReasoningStepType.Analysis);
        var step2 = _chainOperations.AddStep(chain, "Reasoning 2", ReasoningStepType.Planning);
        var step3 = _chainOperations.AddStep(chain, "Reasoning 3", ReasoningStepType.Evaluation);

        // Assert
        Assert.AreEqual(1, step1.StepNumber);
        Assert.AreEqual(2, step2.StepNumber);
        Assert.AreEqual(3, step3.StepNumber);
        Assert.AreEqual(3, chain.Steps.Count);
    }

    [TestMethod]
    public void AddStep_Should_UseDefaultValues_When_OptionalParametersNotProvided()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var reasoning = "Test reasoning";

        // Act
        var step = _chainOperations.AddStep(chain, reasoning);

        // Assert
        Assert.AreEqual(ReasoningStepType.Analysis, step.StepType);
        Assert.AreEqual(0.5, step.Confidence);
        Assert.IsNotNull(step.Insights);
        Assert.AreEqual(0, step.Insights.Count);
    }

    [TestMethod]
    public void AddStep_Should_LogDebugMessage_When_StepAdded()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var reasoning = "Test reasoning for logging verification";

        // Act
        _chainOperations.AddStep(chain, reasoning, ReasoningStepType.Planning);

        // Assert
        _mockLogger.Verify(
            x => x.LogDebug(It.Is<string>(s => s.Contains("Added reasoning step") && s.Contains("Planning"))),
            Times.Once);
    }

    [TestMethod]
    public void AddStep_Should_TruncateLongReasoning_When_LoggingDebugMessage()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var longReasoning = new string('A', 200); // 200 characters

        // Act
        _chainOperations.AddStep(chain, longReasoning);

        // Assert
        _mockLogger.Verify(
            x => x.LogDebug(It.Is<string>(s => s.Contains("Added reasoning step") && s.Length < longReasoning.Length + 50)),
            Times.Once);
    }

    [TestMethod]
    public void AddStep_Should_HandleNullInsights_When_NullProvided()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var reasoning = "Test reasoning";

        // Act
        var step = _chainOperations.AddStep(chain, reasoning, ReasoningStepType.Analysis, 0.7, null);

        // Assert
        Assert.IsNotNull(step.Insights);
        Assert.AreEqual(0, step.Insights.Count);
    }

    [TestMethod]
    public void CompleteChain_Should_CompleteChainWithConclusion_When_ValidParametersProvided()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        _chainOperations.AddStep(chain, "Test reasoning", ReasoningStepType.Analysis);
        var conclusion = "Test conclusion";
        var confidence = 0.9;

        // Act
        _chainOperations.CompleteChain(chain, conclusion, confidence);

        // Assert
        Assert.IsTrue(chain.IsComplete);
        Assert.AreEqual(conclusion, chain.FinalConclusion);
        Assert.AreEqual(confidence, chain.FinalConfidence);
        Assert.IsTrue(chain.CompletedUtc.HasValue);
        Assert.IsTrue(chain.CompletedUtc.Value <= DateTimeOffset.UtcNow);
    }

    [TestMethod]
    public void CompleteChain_Should_UseDefaultConfidence_When_ConfidenceNotProvided()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var conclusion = "Test conclusion";

        // Act
        _chainOperations.CompleteChain(chain, conclusion);

        // Assert
        Assert.IsTrue(chain.IsComplete);
        Assert.AreEqual(conclusion, chain.FinalConclusion);
        Assert.AreEqual(0.5, chain.FinalConfidence);
    }

    [TestMethod]
    public void CompleteChain_Should_LogInformationMessage_When_ChainCompleted()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var conclusion = "Test conclusion for logging";

        // Act
        _chainOperations.CompleteChain(chain, conclusion);

        // Assert
        _mockLogger.Verify(
            x => x.LogInformation(It.Is<string>(s => s.Contains("Completed reasoning chain") && s.Contains(conclusion))),
            Times.Once);
    }

    [TestMethod]
    public void CompleteChain_Should_HandleEmptyConclusion_When_EmptyStringProvided()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var conclusion = "";

        // Act
        _chainOperations.CompleteChain(chain, conclusion);

        // Assert
        Assert.IsTrue(chain.IsComplete);
        Assert.AreEqual("", chain.FinalConclusion);
    }

    [TestMethod]
    public void CompleteChain_Should_HandleNullConclusion_When_NullProvided()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        string? conclusion = null;

        // Act
        _chainOperations.CompleteChain(chain, conclusion!);

        // Assert
        Assert.IsTrue(chain.IsComplete);
        Assert.IsNull(chain.FinalConclusion);
    }

    [TestMethod]
    public void AddStep_Should_SetExecutionTime_When_StepAdded()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");
        var reasoning = "Test reasoning";

        // Act
        var step = _chainOperations.AddStep(chain, reasoning);

        // Assert
        Assert.IsTrue(step.ExecutionTimeMs >= 0);
        Assert.IsTrue(step.CreatedUtc > DateTimeOffset.MinValue);
        Assert.IsTrue(step.CreatedUtc <= DateTimeOffset.UtcNow);
    }

    [TestMethod]
    public void AddStep_Should_PreserveStepOrder_When_MultipleStepsAdded()
    {
        // Arrange
        var chain = _chainOperations.CreateChain("Test goal");

        // Act
        var step1 = _chainOperations.AddStep(chain, "First reasoning", ReasoningStepType.Analysis);
        var step2 = _chainOperations.AddStep(chain, "Second reasoning", ReasoningStepType.Planning);
        var step3 = _chainOperations.AddStep(chain, "Third reasoning", ReasoningStepType.Evaluation);

        // Assert
        Assert.AreEqual("First reasoning", chain.Steps[0].Reasoning);
        Assert.AreEqual("Second reasoning", chain.Steps[1].Reasoning);
        Assert.AreEqual("Third reasoning", chain.Steps[2].Reasoning);
        Assert.AreEqual(ReasoningStepType.Analysis, chain.Steps[0].StepType);
        Assert.AreEqual(ReasoningStepType.Planning, chain.Steps[1].StepType);
        Assert.AreEqual(ReasoningStepType.Evaluation, chain.Steps[2].StepType);
    }

    [TestMethod]
    public void CreateChain_Should_InitializeEmptyStepsCollection_When_Created()
    {
        // Arrange
        var goal = "Test goal";

        // Act
        var chain = _chainOperations.CreateChain(goal);

        // Assert
        Assert.IsNotNull(chain.Steps);
        Assert.AreEqual(0, chain.Steps.Count);
        Assert.IsFalse(chain.IsComplete);
        Assert.AreEqual(string.Empty, chain.FinalConclusion);
        Assert.AreEqual(0.5, chain.FinalConfidence);
    }
}
