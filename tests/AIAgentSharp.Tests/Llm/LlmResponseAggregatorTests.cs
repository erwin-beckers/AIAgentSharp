namespace AIAgentSharp.Tests.Llm;

[TestClass]
public class LlmResponseAggregatorTests
{
    [TestMethod]
    public void AggregateChunks_Should_ReturnEmptyResponse_When_NoChunksProvided()
    {
        // Arrange
        var chunks = new List<LlmStreamingChunk>();

        // Act
        var result = LlmResponseAggregator.AggregateChunks(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.Content);
        Assert.IsFalse(result.HasFunctionCall);
        Assert.IsNull(result.FunctionCall);
        Assert.IsNull(result.Usage);
        Assert.AreEqual(LlmResponseType.Text, result.ActualResponseType);
        Assert.IsNull(result.AdditionalMetadata);
    }

    [TestMethod]
    public void AggregateChunks_Should_ConcatenateContent_When_MultipleChunksProvided()
    {
        // Arrange
        var chunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Hello " },
            new LlmStreamingChunk { Content = "world" },
            new LlmStreamingChunk { Content = "!" }
        };

        // Act
        var result = LlmResponseAggregator.AggregateChunks(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello world!", result.Content);
        Assert.IsFalse(result.HasFunctionCall);
        Assert.IsNull(result.FunctionCall);
        Assert.IsNull(result.Usage);
    }

    [TestMethod]
    public void AggregateChunks_Should_PreserveFunctionCall_When_ChunkHasFunctionCall()
    {
        // Arrange
        var functionCall = new LlmFunctionCall
        {
            Name = "test_function",
            Arguments = new Dictionary<string, object> { { "param1", "value1" } }
        };

        var chunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Text before" },
            new LlmStreamingChunk { Content = "function call", FunctionCall = functionCall },
            new LlmStreamingChunk { Content = "Text after" }
        };

        // Act
        var result = LlmResponseAggregator.AggregateChunks(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Text beforefunction callText after", result.Content);
        Assert.IsTrue(result.HasFunctionCall);
        Assert.IsNotNull(result.FunctionCall);
        Assert.AreEqual("test_function", result.FunctionCall.Name);
    }

    [TestMethod]
    public void AggregateChunks_Should_UseLastChunkUsage_When_MultipleChunksHaveUsage()
    {
        // Arrange
        var usage1 = new LlmUsage { InputTokens = 5, OutputTokens = 5 };
        var usage2 = new LlmUsage { InputTokens = 10, OutputTokens = 10 };
        var usage3 = new LlmUsage { InputTokens = 15, OutputTokens = 15 };

        var chunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Chunk 1", Usage = usage1 },
            new LlmStreamingChunk { Content = "Chunk 2", Usage = usage2 },
            new LlmStreamingChunk { Content = "Chunk 3", Usage = usage3 }
        };

        // Act
        var result = LlmResponseAggregator.AggregateChunks(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Usage);
        Assert.AreEqual(15, result.Usage.InputTokens);
        Assert.AreEqual(15, result.Usage.OutputTokens);
    }

    [TestMethod]
    public void AggregateChunks_Should_UseLastChunkResponseType_When_MultipleChunksHaveResponseType()
    {
        // Arrange
        var chunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Chunk 1", ActualResponseType = LlmResponseType.Text },
            new LlmStreamingChunk { Content = "Chunk 2", ActualResponseType = LlmResponseType.FunctionCall },
            new LlmStreamingChunk { Content = "Chunk 3", ActualResponseType = LlmResponseType.Text }
        };

        // Act
        var result = LlmResponseAggregator.AggregateChunks(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(LlmResponseType.Text, result.ActualResponseType);
    }

    [TestMethod]
    public void AggregateChunks_Should_UseLastChunkMetadata_When_MultipleChunksHaveMetadata()
    {
        // Arrange
        var metadata1 = new Dictionary<string, object> { { "key1", "value1" } };
        var metadata2 = new Dictionary<string, object> { { "key2", "value2" } };
        var metadata3 = new Dictionary<string, object> { { "key3", "value3" } };

        var chunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Chunk 1", AdditionalMetadata = metadata1 },
            new LlmStreamingChunk { Content = "Chunk 2", AdditionalMetadata = metadata2 },
            new LlmStreamingChunk { Content = "Chunk 3", AdditionalMetadata = metadata3 }
        };

        // Act
        var result = LlmResponseAggregator.AggregateChunks(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.AdditionalMetadata);
        Assert.IsTrue(result.AdditionalMetadata.ContainsKey("key3"));
        Assert.AreEqual("value3", result.AdditionalMetadata["key3"]);
    }

    [TestMethod]
    public void AggregateChunks_Should_HandleEmptyContent_When_ChunkHasEmptyContent()
    {
        // Arrange
        var chunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Hello " },
            new LlmStreamingChunk { Content = "" },
            new LlmStreamingChunk { Content = "world!" }
        };

        // Act
        var result = LlmResponseAggregator.AggregateChunks(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello world!", result.Content);
    }

    [TestMethod]
    public async Task AggregateChunksAsync_Should_AggregateAsyncEnumerable_When_AsyncChunksProvided()
    {
        // Arrange
        var chunks = CreateAsyncChunks(new[]
        {
            new LlmStreamingChunk { Content = "Async " },
            new LlmStreamingChunk { Content = "chunk " },
            new LlmStreamingChunk { Content = "test" }
        });

        // Act
        var result = await LlmResponseAggregator.AggregateChunksAsync(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Async chunk test", result.Content);
    }

    [TestMethod]
    public async Task AggregateChunksAsync_Should_ReturnEmptyResponse_When_EmptyAsyncEnumerableProvided()
    {
        // Arrange
        var chunks = CreateAsyncChunks(Array.Empty<LlmStreamingChunk>());

        // Act
        var result = await LlmResponseAggregator.AggregateChunksAsync(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.Content);
        Assert.IsFalse(result.HasFunctionCall);
    }

    [TestMethod]
    public async Task ProcessChunksWithCallbackAsync_Should_CallCallbackForEachChunk_When_ChunksProvided()
    {
        // Arrange
        var receivedChunks = new List<LlmStreamingChunk>();
        var chunks = CreateAsyncChunks(new[]
        {
            new LlmStreamingChunk { Content = "Chunk 1" },
            new LlmStreamingChunk { Content = "Chunk 2" },
            new LlmStreamingChunk { Content = "Chunk 3" }
        });

        // Act
        var result = await LlmResponseAggregator.ProcessChunksWithCallbackAsync(chunks, chunk => receivedChunks.Add(chunk));

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Chunk 1Chunk 2Chunk 3", result.Content);
        Assert.AreEqual(3, receivedChunks.Count);
        Assert.AreEqual("Chunk 1", receivedChunks[0].Content);
        Assert.AreEqual("Chunk 2", receivedChunks[1].Content);
        Assert.AreEqual("Chunk 3", receivedChunks[2].Content);
    }

    [TestMethod]
    public async Task ProcessChunksWithCallbackAsync_Should_HandleCallbackException_When_CallbackThrows()
    {
        // Arrange
        var chunks = CreateAsyncChunks(new[]
        {
            new LlmStreamingChunk { Content = "Chunk 1" },
            new LlmStreamingChunk { Content = "Chunk 2" }
        });

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            await LlmResponseAggregator.ProcessChunksWithCallbackAsync(chunks, chunk => throw new InvalidOperationException("Callback error")));
    }

    [TestMethod]
    public void AggregateChunks_Should_UseFirstFunctionCall_When_MultipleFunctionCallsExist()
    {
        // Arrange
        var functionCall1 = new LlmFunctionCall
        {
            Name = "first_function",
            Arguments = new Dictionary<string, object> { { "param", "first" } }
        };
        var functionCall2 = new LlmFunctionCall
        {
            Name = "second_function", 
            Arguments = new Dictionary<string, object> { { "param", "second" } }
        };

        var chunks = new List<LlmStreamingChunk>
        {
            new LlmStreamingChunk { Content = "Text", FunctionCall = functionCall1 },
            new LlmStreamingChunk { Content = "More text", FunctionCall = functionCall2 }
        };

        // Act
        var result = LlmResponseAggregator.AggregateChunks(chunks);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasFunctionCall);
        Assert.IsNotNull(result.FunctionCall);
        Assert.AreEqual("first_function", result.FunctionCall.Name);
    }

    private static async IAsyncEnumerable<LlmStreamingChunk> CreateAsyncChunks(LlmStreamingChunk[] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return chunk;
            await Task.Delay(1); // Small delay to simulate async behavior
        }
    }
}
