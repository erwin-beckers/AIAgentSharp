using AIAgentSharp;
using AIAgentSharp.Agents.ChainOfThought;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.Tests.Agents.ChainOfThought;

[TestClass]
public class ChainPromptBuilderTests
{
    private ChainPromptBuilder _promptBuilder = null!;
    private Dictionary<string, ITool> _mockTools = null!;

    [TestInitialize]
    public void Setup()
    {
        _promptBuilder = new ChainPromptBuilder();
        _mockTools = new Dictionary<string, ITool>
        {
            ["tool1"] = new TestTool("tool1", "Test tool 1"),
            ["tool2"] = new TestTool("tool2", "Test tool 2")
        };
    }

    [TestMethod]
    public void BuildAnalysisPrompt_Should_IncludeGoalAndContext_When_ValidInputsProvided()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";

        // Act
        var prompt = _promptBuilder.BuildAnalysisPrompt(goal, context, _mockTools);

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsTrue(prompt.Contains("GOAL: Test goal"));
        Assert.IsTrue(prompt.Contains("CONTEXT: Test context"));
        Assert.IsTrue(prompt.Contains("Chain of Thought reasoning"));
        Assert.IsTrue(prompt.Contains("analyze"));
    }

    [TestMethod]
    public void BuildAnalysisPrompt_Should_IncludeToolDescriptions_When_ToolsProvided()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";

        // Act
        var prompt = _promptBuilder.BuildAnalysisPrompt(goal, context, _mockTools);

        // Assert
        Assert.IsTrue(prompt.Contains("AVAILABLE TOOLS"));
        Assert.IsTrue(prompt.Contains("tool1"));
        Assert.IsTrue(prompt.Contains("tool2"));
    }

    [TestMethod]
    public void BuildAnalysisPrompt_Should_IncludeJsonFormat_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";

        // Act
        var prompt = _promptBuilder.BuildAnalysisPrompt(goal, context, _mockTools);

        // Assert
        Assert.IsTrue(prompt.Contains("\"reasoning\""));
        Assert.IsTrue(prompt.Contains("\"confidence\""));
        Assert.IsTrue(prompt.Contains("\"insights\""));
    }

    [TestMethod]
    public void BuildAnalysisPrompt_Should_HandleEmptyTools_When_NoToolsProvided()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var emptyTools = new Dictionary<string, ITool>();

        // Act
        var prompt = _promptBuilder.BuildAnalysisPrompt(goal, context, emptyTools);

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsTrue(prompt.Contains("AVAILABLE TOOLS"));
        Assert.IsTrue(prompt.Contains("GOAL: Test goal"));
    }

    [TestMethod]
    public void BuildPlanningPrompt_Should_IncludeAnalysisInsights_When_InsightsProvided()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var insights = new List<string> { "Insight 1", "Insight 2" };

        // Act
        var prompt = _promptBuilder.BuildPlanningPrompt(goal, context, _mockTools, insights);

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsTrue(prompt.Contains("ANALYSIS INSIGHTS"));
        Assert.IsTrue(prompt.Contains("Insight 1"));
        Assert.IsTrue(prompt.Contains("Insight 2"));
        Assert.IsTrue(prompt.Contains("planning"));
    }

    [TestMethod]
    public void BuildPlanningPrompt_Should_IncludeGoalAndTools_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var insights = new List<string> { "Insight 1" };

        // Act
        var prompt = _promptBuilder.BuildPlanningPrompt(goal, context, _mockTools, insights);

        // Assert
        Assert.IsTrue(prompt.Contains("GOAL: Test goal"));
        Assert.IsTrue(prompt.Contains("CONTEXT: Test context"));
        Assert.IsTrue(prompt.Contains("tool1"));
        Assert.IsTrue(prompt.Contains("tool2"));
    }

    [TestMethod]
    public void BuildPlanningPrompt_Should_HandleEmptyInsights_When_NoInsightsProvided()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var emptyInsights = new List<string>();

        // Act
        var prompt = _promptBuilder.BuildPlanningPrompt(goal, context, _mockTools, emptyInsights);

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsTrue(prompt.Contains("ANALYSIS INSIGHTS"));
        Assert.IsTrue(prompt.Contains("planning"));
    }

    [TestMethod]
    public void BuildStrategyPrompt_Should_IncludePlanningInsights_When_InsightsProvided()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var insights = new List<string> { "Planning insight 1", "Planning insight 2" };

        // Act
        var prompt = _promptBuilder.BuildStrategyPrompt(goal, context, _mockTools, insights);

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsTrue(prompt.Contains("PLANNING INSIGHTS"));
        Assert.IsTrue(prompt.Contains("Planning insight 1"));
        Assert.IsTrue(prompt.Contains("Planning insight 2"));
        Assert.IsTrue(prompt.Contains("execution strategy"));
    }

    [TestMethod]
    public void BuildStrategyPrompt_Should_IncludeStrategyElements_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var insights = new List<string> { "Planning insight" };

        // Act
        var prompt = _promptBuilder.BuildStrategyPrompt(goal, context, _mockTools, insights);

        // Assert
        Assert.IsTrue(prompt.Contains("Which tools to use and in what order"));
        Assert.IsTrue(prompt.Contains("How to handle potential failures"));
        Assert.IsTrue(prompt.Contains("Key decision points and criteria"));
        Assert.IsTrue(prompt.Contains("Success metrics and stopping conditions"));
    }

    [TestMethod]
    public void BuildEvaluationPrompt_Should_IncludeAllInsights_When_InsightsProvided()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var allInsights = new List<string> { "Analysis insight", "Planning insight", "Strategy insight" };

        // Act
        var prompt = _promptBuilder.BuildEvaluationPrompt(goal, context, _mockTools, allInsights);

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsTrue(prompt.Contains("ALL INSIGHTS"));
        Assert.IsTrue(prompt.Contains("Analysis insight"));
        Assert.IsTrue(prompt.Contains("Planning insight"));
        Assert.IsTrue(prompt.Contains("Strategy insight"));
        Assert.IsTrue(prompt.Contains("evaluate"));
    }

    [TestMethod]
    public void BuildEvaluationPrompt_Should_IncludeEvaluationElements_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var insights = new List<string> { "Test insight" };

        // Act
        var prompt = _promptBuilder.BuildEvaluationPrompt(goal, context, _mockTools, insights);

        // Assert
        Assert.IsTrue(prompt.Contains("Likelihood of success"));
        Assert.IsTrue(prompt.Contains("Potential risks and mitigation"));
        Assert.IsTrue(prompt.Contains("Alternative approaches if needed"));
        Assert.IsTrue(prompt.Contains("Final recommendation"));
        Assert.IsTrue(prompt.Contains("\"conclusion\""));
    }

    [TestMethod]
    public void BuildValidationPrompt_Should_IncludeAllParameters_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var insights = new List<string> { "Insight 1", "Insight 2" };
        var conclusion = "Test conclusion";
        var confidence = 0.85;

        // Act
        var prompt = _promptBuilder.BuildValidationPrompt(goal, insights, conclusion, confidence);

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsTrue(prompt.Contains("GOAL: Test goal"));
        Assert.IsTrue(prompt.Contains("Insight 1"));
        Assert.IsTrue(prompt.Contains("Insight 2"));
        Assert.IsTrue(prompt.Contains("CONCLUSION: Test conclusion"));
        Assert.IsTrue(prompt.Replace(",",".").Contains("CONFIDENCE: 0.85"));
    }

    [TestMethod]
    public void BuildValidationPrompt_Should_IncludeValidationCriteria_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var insights = new List<string> { "Test insight" };
        var conclusion = "Test conclusion";
        var confidence = 0.75;

        // Act
        var prompt = _promptBuilder.BuildValidationPrompt(goal, insights, conclusion, confidence);

        // Assert
        Assert.IsTrue(prompt.Contains("Logical consistency"));
        Assert.IsTrue(prompt.Contains("Completeness of analysis"));
        Assert.IsTrue(prompt.Contains("Appropriateness of the conclusion"));
        Assert.IsTrue(prompt.Contains("Confidence level alignment"));
        Assert.IsTrue(prompt.Contains("\"is_valid\""));
        Assert.IsTrue(prompt.Contains("\"error\""));
    }

    [TestMethod]
    public void BuildValidationPrompt_Should_HandleEmptyInsights_When_NoInsightsProvided()
    {
        // Arrange
        var goal = "Test goal";
        var emptyInsights = new List<string>();
        var conclusion = "Test conclusion";
        var confidence = 0.5;

        // Act
        var prompt = _promptBuilder.BuildValidationPrompt(goal, emptyInsights, conclusion, confidence);

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsTrue(prompt.Contains("INSIGHTS"));
        Assert.IsTrue(prompt.Contains("validating"));
    }

    // Helper class for testing
    private class TestTool : ITool
    {
        public string Name { get; }
        public string Description { get; }

        public TestTool(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult<object?>("Test result");
        }
    }
}
