using AIAgentSharp;
using AIAgentSharp.Agents.ChainOfThought;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Metrics;
using Moq;

public static class AsyncEnumerableExtensions
{
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        return new AsyncEnumerableWrapper<T>(source);
    }

    private class AsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> _source;

        public AsyncEnumerableWrapper(IEnumerable<T> source)
        {
            _source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumeratorWrapper<T>(_source.GetEnumerator());
        }
    }

    private class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;

        public AsyncEnumeratorWrapper(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public T Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_enumerator.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _enumerator.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

[TestClass]
public class ChainOfThoughtEngineTests
{
    private Mock<ILlmClient> _mockLlmClient = null!;
    private Mock<ILogger> _mockLogger = null!;
    private Mock<IEventManager> _mockEventManager = null!;
    private Mock<IStatusManager> _mockStatusManager = null!;
    private Mock<IMetricsCollector> _mockMetricsCollector = null!;
    private AgentConfiguration _config = null!;
    private ChainOfThoughtEngine _engine = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLlmClient = new Mock<ILlmClient>();
        _mockLogger = new Mock<ILogger>();
        _mockEventManager = new Mock<IEventManager>();
        _mockStatusManager = new Mock<IStatusManager>();
        _mockMetricsCollector = new Mock<IMetricsCollector>();
        
        _config = new AgentConfiguration
        {
            EnableReasoningValidation = true,
            MinReasoningConfidence = 0.7
        };

        _engine = new ChainOfThoughtEngine(
            _mockLlmClient.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
    }

    [TestMethod]
    public void Constructor_Should_ThrowArgumentNullException_When_ConfigIsNull()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new ChainOfThoughtEngine(
                _mockLlmClient.Object,
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
        Assert.ThrowsException<ArgumentNullException>(() => 
            new ChainOfThoughtEngine(
                _mockLlmClient.Object,
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
        Assert.ThrowsException<ArgumentNullException>(() => 
            new ChainOfThoughtEngine(
                _mockLlmClient.Object,
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
        Assert.ThrowsException<ArgumentNullException>(() => 
            new ChainOfThoughtEngine(
                _mockLlmClient.Object,
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
        Assert.ThrowsException<ArgumentNullException>(() => 
            new ChainOfThoughtEngine(
                _mockLlmClient.Object,
                _config,
                _mockLogger.Object,
                _mockEventManager.Object,
                _mockStatusManager.Object,
                null!));
    }

    [TestMethod]
    public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
    {
        // Assert
        Assert.IsNotNull(_engine);
        Assert.AreEqual(ReasoningType.ChainOfThought, _engine.ReasoningType);
        Assert.IsNull(_engine.CurrentChain);
    }

    [TestMethod]
    public void AddStep_Should_ThrowInvalidOperationException_When_NoActiveChain()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            _engine.AddStep("test reasoning"));
    }

    [TestMethod]
    public void CompleteChain_Should_ThrowInvalidOperationException_When_NoActiveChain()
    {
        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            _engine.CompleteChain("test conclusion"));
    }

    [TestMethod]
    public async Task ReasonAsync_Should_ReturnFailedResult_When_StepExecutionFails()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        // Mock LLM client to return null (simulating failure)
        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(new List<LlmStreamingChunk>().ToAsyncEnumerable());

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsNotNull(result.Chain);
        Assert.AreEqual(goal, result.Chain!.Goal);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_ReturnFailedResult_When_ValidationFailsAndConfidenceBelowThreshold()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        // Mock successful step execution but low confidence
        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test reasoning"",
                ""confidence"": 0.3,
                ""insights"": [""insight1""]
            }", IsFinal = true }
        };

        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable());

        // Mock validation to fail
        var validationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""is_valid"": false,
                ""error"": ""Validation failed""
            }", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable())  // Analysis step
            .Returns(mockChunks.ToAsyncEnumerable())  // Planning step
            .Returns(mockChunks.ToAsyncEnumerable())  // Strategy step
            .Returns(mockChunks.ToAsyncEnumerable())  // Evaluation step
            .Returns(validationChunks.ToAsyncEnumerable()); // Validation step

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Error!.Contains("confidence"));
        Assert.IsTrue(result.Error.Contains("threshold"));
    }

    [TestMethod]
    public async Task ReasonAsync_Should_ReturnSuccessResult_When_AllStepsSucceed()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        // Mock successful responses for all steps
        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test reasoning"",
                ""confidence"": 0.8,
                ""insights"": [""insight1"", ""insight2""]
            }", IsFinal = true }
        };

        var evaluationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test evaluation"",
                ""confidence"": 0.8,
                ""insights"": [""insight3""],
                ""conclusion"": ""Test conclusion""
            }", IsFinal = true }
        };

        var validationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""is_valid"": true
            }", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable())      // Analysis step
            .Returns(mockChunks.ToAsyncEnumerable())      // Planning step
            .Returns(mockChunks.ToAsyncEnumerable())      // Strategy step
            .Returns(evaluationChunks.ToAsyncEnumerable()) // Evaluation step
            .Returns(validationChunks.ToAsyncEnumerable()); // Validation step

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Conclusion);
        Assert.AreEqual("Test conclusion", result.Conclusion);
        Assert.AreEqual(0.8, result.Confidence);
        Assert.IsNotNull(result.Chain);
        Assert.AreEqual(goal, result.Chain!.Goal);
        Assert.IsNotNull(result.Metadata);
        Assert.AreEqual(4, result.Metadata!["steps_completed"]);
        Assert.AreEqual(7, result.Metadata["total_insights"]);
        Assert.AreEqual("ChainOfThought", result.Metadata["reasoning_type"]);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_RecordMetrics_When_ReasoningCompletes()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test reasoning"",
                ""confidence"": 0.8,
                ""insights"": [""insight1""]
            }", IsFinal = true }
        };

        var evaluationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test evaluation"",
                ""confidence"": 0.8,
                ""insights"": [""insight2""],
                ""conclusion"": ""Test conclusion""
            }", IsFinal = true }
        };

        var validationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""is_valid"": true
            }", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(evaluationChunks.ToAsyncEnumerable())
            .Returns(validationChunks.ToAsyncEnumerable());

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        _mockMetricsCollector.Verify(x => x.RecordReasoningExecutionTime("agent", ReasoningType.ChainOfThought, It.IsAny<long>()), Times.Once);
        _mockMetricsCollector.Verify(x => x.RecordReasoningConfidence("agent", ReasoningType.ChainOfThought, 0.8), Times.Once);
        _mockMetricsCollector.Verify(x => x.RecordValidation(string.Empty, ReasoningType.ChainOfThought.ToString(), true, null), Times.Once);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_HandleCancellation_When_CancellationTokenIsCancelled()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools, cancellationTokenSource.Token);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_HandleException_When_StepExecutionThrows()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        _mockLlmClient.Setup(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Test exception", result.Error);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Chain of Thought reasoning failed"))), Times.Once);
    }

    [TestMethod]
    public async Task ReasonAsync_Should_NotValidate_When_ValidationIsDisabled()
    {
        // Arrange
        _config = new AgentConfiguration
        {
            EnableReasoningValidation = false,
            MinReasoningConfidence = 0.7
        };
        _engine = new ChainOfThoughtEngine(
            _mockLlmClient.Object,
            _config,
            _mockLogger.Object,
            _mockEventManager.Object,
            _mockStatusManager.Object,
            _mockMetricsCollector.Object);
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test reasoning"",
                ""confidence"": 0.8,
                ""insights"": [""insight1""]
            }", IsFinal = true }
        };

        var evaluationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test evaluation"",
                ""confidence"": 0.8,
                ""insights"": [""insight2""],
                ""conclusion"": ""Test conclusion""
            }", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(evaluationChunks.ToAsyncEnumerable());

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success);
        // Should only call LLM 4 times (no validation step)
        _mockLlmClient.Verify(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [TestMethod]
    public async Task ReasonAsync_Should_AllowAddStepAndCompleteChain_When_ChainIsActive()
    {
        // Arrange
        var tools = new Dictionary<string, ITool>();
        var goal = "Test goal";
        var context = "Test context";

        var mockChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test reasoning"",
                ""confidence"": 0.8,
                ""insights"": [""insight1""]
            }", IsFinal = true }
        };

        var evaluationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""reasoning"": ""Test evaluation"",
                ""confidence"": 0.8,
                ""insights"": [""insight2""],
                ""conclusion"": ""Test conclusion""
            }", IsFinal = true }
        };

        var validationChunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = @"{
                ""is_valid"": true
            }", IsFinal = true }
        };

        _mockLlmClient.SetupSequence(x => x.StreamAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(mockChunks.ToAsyncEnumerable())
            .Returns(evaluationChunks.ToAsyncEnumerable())
            .Returns(validationChunks.ToAsyncEnumerable());

        // Act
        var result = await _engine.ReasonAsync(goal, context, tools);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(_engine.CurrentChain);
        
        // Test AddStep
        var step = _engine.AddStep("Additional reasoning", ReasoningStepType.Analysis, 0.9, new List<string> { "extra insight" });
        Assert.IsNotNull(step);
        Assert.AreEqual(5, step.StepNumber); // Should be the 5th step (4 from execution + 1 added)
        
        // Test CompleteChain
        _engine.CompleteChain("Final conclusion", 0.95);
        Assert.IsTrue(_engine.CurrentChain!.IsComplete);
        Assert.AreEqual("Final conclusion", _engine.CurrentChain.FinalConclusion);
        Assert.AreEqual(0.95, _engine.CurrentChain.FinalConfidence);
    }
}
