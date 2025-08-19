namespace AIAgentSharp;

/// <summary>
/// Helper class for aggregating streaming chunks into complete responses.
/// This is used by the agent layer when it needs to process complete responses.
/// </summary>
public static class LlmResponseAggregator
{
    private static readonly ILogger _logger = new ConsoleLogger();

    /// <summary>
    /// Aggregates a collection of streaming chunks into a complete LlmResponse.
    /// </summary>
    /// <param name="chunks">The streaming chunks to aggregate.</param>
    /// <returns>A complete LlmResponse containing the aggregated data.</returns>
    public static LlmResponse AggregateChunks(IEnumerable<LlmStreamingChunk> chunks)
    {
        var chunkList = chunks.ToList();
        if (!chunkList.Any())
        {
            return new LlmResponse();
        }

        // Aggregate content from all chunks
        var content = string.Join("", chunkList.Select(c => c.Content));

        _logger.LogDebug($"Aggregated {chunkList.Count} chunks into content of length {content.Length}");
        _logger.LogDebug($"Final aggregated content: {content}");

        // Find function call from any chunk that has it
        var functionCall = chunkList.FirstOrDefault(c => c.FunctionCall != null)?.FunctionCall;

        // Get usage from the final chunk
        var usage = chunkList.LastOrDefault(c => c.Usage != null)?.Usage;

        // Get response type from the final chunk
        var actualResponseType = chunkList.LastOrDefault()?.ActualResponseType ?? LlmResponseType.Text;

        // Get additional metadata from the final chunk
        var additionalMetadata = chunkList.LastOrDefault()?.AdditionalMetadata;

        return new LlmResponse
        {
            Content = content,
            HasFunctionCall = functionCall != null,
            FunctionCall = functionCall,
            Usage = usage,
            ActualResponseType = actualResponseType,
            AdditionalMetadata = additionalMetadata
        };
    }

    /// <summary>
    /// Aggregates streaming chunks asynchronously and returns a complete LlmResponse.
    /// </summary>
    /// <param name="chunks">The async enumerable of streaming chunks.</param>
    /// <returns>A task that completes with the aggregated LlmResponse.</returns>
    public static async Task<LlmResponse> AggregateChunksAsync(IAsyncEnumerable<LlmStreamingChunk> chunks)
    {
        var chunkList = new List<LlmStreamingChunk>();
        await foreach (var chunk in chunks)
        {
            chunkList.Add(chunk);
        }

        return AggregateChunks(chunkList);
    }

    /// <summary>
    /// Processes streaming chunks and calls a callback for each chunk while also aggregating them.
    /// </summary>
    /// <param name="chunks">The async enumerable of streaming chunks.</param>
    /// <param name="onChunkReceived">Callback to be called for each chunk received.</param>
    /// <returns>A task that completes with the aggregated LlmResponse.</returns>
    public static async Task<LlmResponse> ProcessChunksWithCallbackAsync(
        IAsyncEnumerable<LlmStreamingChunk> chunks,
        Action<LlmStreamingChunk> onChunkReceived)
    {
        var chunkList = new List<LlmStreamingChunk>();
        var chunkIndex = 0;
        await foreach (var chunk in chunks)
        {
            chunkList.Add(chunk);
            onChunkReceived(chunk);
            chunkIndex++;
        }

        return AggregateChunks(chunkList);
    }
}
