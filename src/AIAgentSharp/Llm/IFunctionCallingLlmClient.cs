namespace AIAgentSharp;

/// <summary>
///     Defines the interface for LLM clients that support function calling.
/// </summary>
public interface IFunctionCallingLlmClient : ILlmClient
{
    /// <summary>
    ///     Completes a conversation with function calling capabilities.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="functions">The available functions that the LLM can call.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the function calling operation.</returns>
    Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages,
        IEnumerable<OpenAiFunctionSpec> functions,
        CancellationToken ct = default);
}