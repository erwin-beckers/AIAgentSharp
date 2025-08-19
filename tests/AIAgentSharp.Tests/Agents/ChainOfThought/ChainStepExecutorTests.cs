using AIAgentSharp;
using AIAgentSharp.Agents.ChainOfThought;
using AIAgentSharp.Agents.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIAgentSharp.Tests.Agents.ChainOfThought;

[TestClass]
public class ChainStepExecutorTests
{
    private ChainStepExecutor _stepExecutor = null!;
    private Mock<IChainPromptBuilder> _mockPromptBuilder = null!;
    private Mock<ILlmCommunicator> _mockLlmCommunicator = null!;
    private Mock<ILlmClient> _mockLlmClient = null!;
    private Mock<IStatusManager> _mockStatusManager = null!;
    private Dictionary<string, ITool> _mockTools = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockPromptBuilder = new Mock<IChainPromptBuilder>();
        _mockLlmCommunicator = new Mock<ILlmCommunicator>();
        _mockLlmClient = new Mock<ILlmClient>();
        _mockStatusManager = new Mock<IStatusManager>();
        
        // Setup the mock to return the LLM client
        _mockLlmCommunicator.Setup(x => x.GetLlmClient()).Returns(_mockLlmClient.Object);
        
        _stepExecutor = new ChainStepExecutor(
            _mockPromptBuilder.Object,
            _mockLlmCommunicator.Object,
            _mockStatusManager.Object);

        _mockTools = new Dictionary<string, ITool>
        {
            ["test_tool"] = new TestTool("test_tool", "Test tool description")
        };
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_Should_ThrowArgumentNullException_When_PromptBuilderIsNull()
    {
        // Act & Assert
        new ChainStepExecutor(null!, _mockLlmCommunicator.Object, _mockStatusManager.Object);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_Should_ThrowArgumentNullException_When_LlmCommunicatorIsNull()
    {
        // Act & Assert
        new ChainStepExecutor(_mockPromptBuilder.Object, null!, _mockStatusManager.Object);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_Should_ThrowArgumentNullException_When_StatusManagerIsNull()
    {
        // Act & Assert
        new ChainStepExecutor(_mockPromptBuilder.Object, _mockLlmCommunicator.Object, null!);
    }

    [TestMethod]
    public async Task PerformAnalysisStepAsync_Should_ReturnSuccessResult_When_ValidLlmResponseReceived()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var expectedPrompt = "Test analysis prompt";
        
        var mockResponse = @"{
            ""reasoning"": ""Test analysis reasoning"",
            ""confidence"": 0.85,
            ""insights"": [""Analysis insight 1"", ""Analysis insight 2""]
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildAnalysisPrompt(goal, context, _mockTools))
            .Returns(expectedPrompt);
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        var result = await _stepExecutor.PerformAnalysisStepAsync(goal, context, _mockTools, default(CancellationToken));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Test analysis reasoning", result.Reasoning);
        Assert.AreEqual(0.85, result.Confidence);
        Assert.AreEqual(2, result.Insights.Count);
        Assert.AreEqual("Analysis insight 1", result.Insights[0]);
        Assert.AreEqual("Analysis insight 2", result.Insights[1]);
    }

    [TestMethod]
    public async Task PerformAnalysisStepAsync_Should_EmitStatus_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var mockResponse = @"{
            ""reasoning"": ""Test reasoning"",
            ""confidence"": 0.8,
            ""insights"": [""insight1""]
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildAnalysisPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>()))
            .Returns("Test prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        await _stepExecutor.PerformAnalysisStepAsync(goal, context, _mockTools, default(CancellationToken));

        // Assert
        _mockStatusManager.Verify(x => x.EmitStatus(
            "reasoning",
            "Analyzing problem",
            "Breaking down the goal into components",
            "Understanding requirements",
            null),
            Times.Once);
    }

    [TestMethod]
    public async Task PerformAnalysisStepAsync_Should_ReturnFailureResult_When_LlmResponseIsNull()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";

        _mockPromptBuilder.Setup(x => x.BuildAnalysisPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>()))
            .Returns("Test prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(new List<LlmStreamingChunk>().ToAsyncEnumerable());

        // Act
        var result = await _stepExecutor.PerformAnalysisStepAsync(goal, context, _mockTools, default(CancellationToken));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Empty LLM response", result.Error);
    }

    [TestMethod]
    public async Task PerformPlanningStepAsync_Should_ReturnSuccessResult_When_ValidLlmResponseReceived()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var analysisInsights = new List<string> { "Analysis insight 1", "Analysis insight 2" };
        var expectedPrompt = "Test planning prompt";
        
        var mockResponse = @"{
            ""reasoning"": ""Test planning reasoning"",
            ""confidence"": 0.9,
            ""insights"": [""Planning insight 1""]
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildPlanningPrompt(goal, context, _mockTools, analysisInsights))
            .Returns(expectedPrompt);
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        var result = await _stepExecutor.PerformPlanningStepAsync(goal, context, _mockTools, analysisInsights, default(CancellationToken));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Test planning reasoning", result.Reasoning);
        Assert.AreEqual(0.9, result.Confidence);
        Assert.AreEqual(1, result.Insights.Count);
        Assert.AreEqual("Planning insight 1", result.Insights[0]);
    }

    [TestMethod]
    public async Task PerformPlanningStepAsync_Should_EmitStatus_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var analysisInsights = new List<string> { "Test insight" };
        var mockResponse = @"{
            ""reasoning"": ""Test reasoning"",
            ""confidence"": 0.8,
            ""insights"": [""insight1""]
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildPlanningPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<List<string>>()))
            .Returns("Test prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        await _stepExecutor.PerformPlanningStepAsync(goal, context, _mockTools, analysisInsights, default(CancellationToken));

        // Assert
        _mockStatusManager.Verify(x => x.EmitStatus(
            "reasoning",
            "Planning approach",
            "Developing solution strategy",
            "Creating execution plan",
            null),
            Times.Once);
    }

    [TestMethod]
    public async Task PerformStrategyStepAsync_Should_ReturnSuccessResult_When_ValidLlmResponseReceived()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var planningInsights = new List<string> { "Planning insight 1" };
        
        var mockResponse = @"{
            ""reasoning"": ""Test strategy reasoning"",
            ""confidence"": 0.8,
            ""insights"": [""Strategy insight 1"", ""Strategy insight 2""]
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildStrategyPrompt(goal, context, _mockTools, planningInsights))
            .Returns("Test strategy prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        var result = await _stepExecutor.PerformStrategyStepAsync(goal, context, _mockTools, planningInsights, default(CancellationToken));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Test strategy reasoning", result.Reasoning);
        Assert.AreEqual(0.8, result.Confidence);
        Assert.AreEqual(2, result.Insights.Count);
    }

    [TestMethod]
    public async Task PerformEvaluationStepAsync_Should_ReturnSuccessResult_When_ValidLlmResponseReceived()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var allInsights = new List<string> { "Insight 1", "Insight 2", "Insight 3" };
        
        var mockResponse = @"{
            ""reasoning"": ""Test evaluation reasoning"",
            ""confidence"": 0.95,
            ""insights"": [""Evaluation insight""],
            ""conclusion"": ""Test conclusion""
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildEvaluationPrompt(goal, context, _mockTools, allInsights))
            .Returns("Test evaluation prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        var result = await _stepExecutor.PerformEvaluationStepAsync(goal, context, _mockTools, allInsights, default(CancellationToken));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Test evaluation reasoning", result.Reasoning);
        Assert.AreEqual(0.95, result.Confidence);
        Assert.AreEqual(1, result.Insights.Count);
        Assert.AreEqual("Test conclusion", result.Conclusion);
    }

    [TestMethod]
    public async Task PerformValidationAsync_Should_ReturnSuccessResult_When_ValidLlmResponseReceived()
    {
        // Arrange
        var goal = "Test goal";
        var insights = new List<string> { "Insight 1", "Insight 2" };
        var conclusion = "Test conclusion";
        var confidence = 0.85;
        
        var mockResponse = @"{
            ""is_valid"": true
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildValidationPrompt(goal, insights, conclusion, confidence))
            .Returns("Test validation prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        var result = await _stepExecutor.PerformValidationAsync(goal, insights, conclusion, confidence, default(CancellationToken));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsValid);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public async Task PerformValidationAsync_Should_ReturnInvalidResult_When_ValidationFails()
    {
        // Arrange
        var goal = "Test goal";
        var insights = new List<string> { "Insight 1" };
        var conclusion = "Test conclusion";
        var confidence = 0.5;
        
        var mockResponse = @"{
            ""is_valid"": false,
            ""error"": ""Validation failed: reasoning is incomplete""
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildValidationPrompt(goal, insights, conclusion, confidence))
            .Returns("Test validation prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        var result = await _stepExecutor.PerformValidationAsync(goal, insights, conclusion, confidence, default(CancellationToken));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("Validation failed: reasoning is incomplete", result.Error);
    }

    [TestMethod]
    public async Task PerformAnalysisStepAsync_Should_HandleNullInsights_When_LlmResponseHasNullInsights()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        
        var mockResponse = @"{
            ""reasoning"": ""Test reasoning"",
            ""confidence"": 0.7
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildAnalysisPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>()))
            .Returns("Test prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        var result = await _stepExecutor.PerformAnalysisStepAsync(goal, context, _mockTools, default(CancellationToken));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Insights);
        Assert.AreEqual(0, result.Insights.Count);
    }

    [TestMethod]
    public async Task PerformEvaluationStepAsync_Should_EmitStatus_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var context = "Test context";
        var allInsights = new List<string> { "Test insight" };
        var mockResponse = @"{
            ""reasoning"": ""Test reasoning"",
            ""confidence"": 0.8,
            ""insights"": [""insight1""]
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildEvaluationPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, ITool>>(), It.IsAny<List<string>>()))
            .Returns("Test prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        await _stepExecutor.PerformEvaluationStepAsync(goal, context, _mockTools, allInsights, default(CancellationToken));

        // Assert
        _mockStatusManager.Verify(x => x.EmitStatus(
            "reasoning",
            "Evaluating solution",
            "Assessing approach quality",
            "Finalizing decision",
            null),
            Times.Once);
    }

    [TestMethod]
    public async Task PerformValidationAsync_Should_EmitStatus_When_Called()
    {
        // Arrange
        var goal = "Test goal";
        var insights = new List<string> { "Test insight" };
        var conclusion = "Test conclusion";
        var confidence = 0.8;
        var mockResponse = @"{
            ""is_valid"": true,
            ""reasoning"": ""Valid solution""
        }";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = mockResponse, IsFinal = true }
        };

        _mockPromptBuilder.Setup(x => x.BuildValidationPrompt(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<double>()))
            .Returns("Test prompt");
        
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Act
        await _stepExecutor.PerformValidationAsync(goal, insights, conclusion, confidence, default(CancellationToken));

        // Assert
        _mockStatusManager.Verify(x => x.EmitStatus(
            "reasoning",
            "Validating reasoning",
            "Checking logic and consistency",
            "Quality assurance",
            null),
            Times.Once);
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

        public Task<object> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>("Test result");
        }
    }
}
