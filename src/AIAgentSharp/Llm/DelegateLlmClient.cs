namespace AIAgentSharp;

/// <summary>
///     A delegate-based LLM client that allows you to plug in any LLM implementation.
///     This is useful for testing or when you want to use a custom LLM client.
/// </summary>
public sealed class DelegateLlmClient : ILlmClient
{
    private readonly Func<IEnumerable<LlmMessage>, CancellationToken, Task<string>> _impl;

    /// <summary>
    ///     Initializes a new instance of the DelegateLlmClient class.
    /// </summary>
    /// <param name="impl">The delegate that implements the LLM completion logic.</param>
    /// <exception cref="ArgumentNullException">Thrown when impl is null.</exception>
    public DelegateLlmClient(Func<IEnumerable<LlmMessage>, CancellationToken, Task<string>> impl)
    {
        _impl = impl ?? throw new ArgumentNullException(nameof(impl));
    }

    /// <summary>
    ///     Completes a conversation by delegating to the provided implementation.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The LLM's response as a string.</returns>
    public Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        return _impl(messages, ct);
    }
}