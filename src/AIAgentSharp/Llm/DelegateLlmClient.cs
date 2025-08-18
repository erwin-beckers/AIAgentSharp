namespace AIAgentSharp;

/// <summary>
///     A delegate-based LLM client that allows you to plug in any LLM implementation.
///     This is useful for testing or when you want to use a custom LLM client.
/// </summary>
public sealed class DelegateLlmClient : ILlmClient
{
    private readonly Func<IEnumerable<LlmMessage>, CancellationToken, Task<LlmCompletionResult>> _impl;
    private readonly Func<IEnumerable<LlmMessage>, IEnumerable<OpenAiFunctionSpec>, CancellationToken, Task<FunctionCallResult>>? _functionImpl;

    /// <summary>
    ///     Initializes a new instance of the DelegateLlmClient class.
    /// </summary>
    /// <param name="impl">The delegate that implements the LLM completion logic.</param>
    /// <param name="functionImpl">Optional delegate that implements function calling logic.</param>
    /// <exception cref="ArgumentNullException">Thrown when impl is null.</exception>
    public DelegateLlmClient(
        Func<IEnumerable<LlmMessage>, CancellationToken, Task<LlmCompletionResult>> impl,
        Func<IEnumerable<LlmMessage>, IEnumerable<OpenAiFunctionSpec>, CancellationToken, Task<FunctionCallResult>>? functionImpl = null)
    {
        _impl = impl ?? throw new ArgumentNullException(nameof(impl));
        _functionImpl = functionImpl;
    }

    /// <summary>
    ///     Completes a conversation by delegating to the provided implementation.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The LLM's response with usage metadata.</returns>
    public Task<LlmCompletionResult> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        return _impl(messages, ct);
    }

    /// <summary>
    ///     Completes a conversation with function calling by delegating to the provided implementation.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="functions">The available functions that the LLM can call.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the function calling operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when function calling is not implemented.</exception>
    public Task<FunctionCallResult> CompleteWithFunctionsAsync(IEnumerable<LlmMessage> messages, IEnumerable<OpenAiFunctionSpec> functions, CancellationToken ct = default)
    {
        if (_functionImpl == null)
        {
            throw new NotSupportedException("Function calling is not supported by this delegate client.");
        }
        
        return _functionImpl(messages, functions, ct);
    }
}