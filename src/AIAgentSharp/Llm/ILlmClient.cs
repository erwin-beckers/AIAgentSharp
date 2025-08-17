namespace AIAgentSharp;

/// <summary>
///     Defines the interface for LLM clients that can complete conversations.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    ///     Completes a conversation by sending messages to the LLM and returning the response.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The LLM's response as a string.</returns>
    Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default);
}