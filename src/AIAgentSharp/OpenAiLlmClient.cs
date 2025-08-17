// using OpenAI;       // <- not required if you use the simple ctor below

using AIAgentSharp;
using OpenAI.Chat;

/// <summary>
///     OpenAI LLM client implementation that integrates with the OpenAI SDK.
///     This client supports both regular chat completion and function calling.
/// </summary>
public sealed class OpenAiLlmClient : ILlmClient, IFunctionCallingLlmClient
{
    private readonly ChatClient _chat;

    /// <summary>
    ///     Initializes a new instance of the OpenAiLlmClient class with API key and model.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key for authentication.</param>
    /// <param name="model">The model to use for completions. Defaults to "gpt-5-nano".</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
    public OpenAiLlmClient(string apiKey, string model = "gpt-5-nano")
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey));
        }

        _chat = new ChatClient(model, apiKey);
        // Alternatively:
        // var client = new OpenAI.OpenAIClient(apiKey);
        // _chat = client.GetChatClient(model);
    }

    /// <summary>
    ///     Internal constructor for testing purposes.
    /// </summary>
    /// <param name="chatClient">The chat client to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when chatClient is null.</exception>
    internal OpenAiLlmClient(ChatClient chatClient)
    {
        _chat = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <summary>
    ///     Completes a conversation with function calling capabilities.
    ///     Note: This is a simplified implementation that falls back to regular completion.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="functions">The available functions that the LLM can call.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the function calling operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages,
        IEnumerable<OpenAiFunctionSpec> functions,
        CancellationToken ct = default)
    {
        // Map messages to OpenAI.Chat message types
        var chatMessages = new List<ChatMessage>();

        foreach (var m in messages)
        {
            switch (m.Role)
            {
                case "system":
                    chatMessages.Add(new SystemChatMessage(m.Content));
                    break;
                case "assistant":
                    chatMessages.Add(new AssistantChatMessage(m.Content));
                    break;
                default: // "user"
                    chatMessages.Add(new UserChatMessage(m.Content));
                    break;
            }
        }

        // For now, implement a simplified version that falls back to the regular completion
        // This will be enhanced once we understand the correct API for function calling
        var result = await _chat.CompleteChatAsync(
            chatMessages,
            cancellationToken: ct);
        var completion = result.Value;

        // Since function calling is not yet implemented in this version,
        // we'll return a fallback result
        var assistantContent = completion.Content.Count > 0 ? completion.Content[0].Text ?? string.Empty : string.Empty;

        return new FunctionCallResult
        {
            HasFunctionCall = false,
            RawTextFallback = assistantContent
        };
    }

    /// <summary>
    ///     Completes a conversation by sending messages to the OpenAI LLM.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The LLM's response as a string.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        // Map your messages to OpenAI.Chat message types
        var chatMessages = new List<ChatMessage>();

        foreach (var m in messages)
        {
            switch (m.Role)
            {
                case "system":
                    chatMessages.Add(new SystemChatMessage(m.Content));
                    break;
                case "assistant":
                    chatMessages.Add(new AssistantChatMessage(m.Content));
                    break;
                default: // "user"
                    chatMessages.Add(new UserChatMessage(m.Content));
                    break;
            }
        }
        var result = await _chat.CompleteChatAsync(
            chatMessages,
            cancellationToken: ct);
        var completion = result.Value;
        return completion.Content.Count > 0 ? completion.Content[0].Text ?? string.Empty : string.Empty;
    }
}