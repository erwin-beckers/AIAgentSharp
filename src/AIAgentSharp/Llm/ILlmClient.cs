namespace AIAgentSharp;

/// <summary>
/// Represents a client for interacting with Large Language Models (LLMs).
/// This interface provides a unified way to communicate with different LLM providers.
/// All responses are returned as streaming chunks, allowing for real-time processing.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Streams chunks from the LLM based on the provided request.
    /// This method always returns chunks, regardless of whether the LLM supports native streaming.
    /// For non-streaming LLMs, this will return a single chunk with the complete response.
    /// </summary>
    /// <param name="request">The request containing messages, functions, and configuration.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of LLM streaming chunks. Each chunk contains partial content,
    /// and the final chunk will have IsFinal=true.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="ArgumentException">Thrown when request contains invalid data.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the LLM operation fails.</exception>
    /// 
    /// <example>
    /// <code>
    /// // Basic text completion - always returns chunks
    /// var request = new LlmRequest
    /// {
    ///     Messages = new[] { new LlmMessage { Role = "user", Content = "Hello, how are you?" } },
    ///     ResponseType = LlmResponseType.Text
    /// };
    /// 
    /// await foreach (var chunk in llmClient.StreamAsync(request))
    /// {
    ///     Console.Write(chunk.Content); // Real-time output
    ///     if (chunk.IsFinal)
    ///     {
    ///         Console.WriteLine($"\nFinished: {chunk.FinishReason}");
    ///     }
    /// }
    /// </code>
    /// 
    /// <code>
    /// // Function calling - chunks may contain function call data
    /// var request = new LlmRequest
    /// {
    ///     Messages = new[] { new LlmMessage { Role = "user", Content = "What's the weather like?" } },
    ///     Functions = new[] { weatherFunction },
    ///     ResponseType = LlmResponseType.FunctionCall
    /// };
    /// 
    /// await foreach (var chunk in llmClient.StreamAsync(request))
    /// {
    ///     if (chunk.FunctionCall != null)
    ///     {
    ///         Console.WriteLine($"Function: {chunk.FunctionCall.Name}");
    ///         Console.WriteLine($"Arguments: {chunk.FunctionCall.ArgumentsJson}");
    ///     }
    ///     Console.Write(chunk.Content);
    /// }
    /// </code>
    /// 
    /// <code>
    /// // Agent can aggregate chunks into complete response
    /// var chunks = new List<LlmStreamingChunk>();
    /// await foreach (var chunk in llmClient.StreamAsync(request))
    /// {
    ///     chunks.Add(chunk);
    ///     // Emit real-time events for UI updates
    ///     OnChunkReceived(chunk);
    /// }
    /// 
    /// // Process complete response when done
    /// var completeResponse = AggregateChunks(chunks);
    /// </code>
    /// </example>
    IAsyncEnumerable<LlmStreamingChunk> StreamAsync(LlmRequest request, CancellationToken ct = default);
}