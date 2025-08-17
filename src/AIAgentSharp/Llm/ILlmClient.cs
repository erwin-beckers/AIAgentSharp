namespace AIAgentSharp;

/// <summary>
/// Defines the interface for LLM (Large Language Model) clients that can communicate with
/// language models to generate responses based on input messages.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a standardized way to interact with different LLM providers
/// such as OpenAI, Anthropic, or custom models. The framework uses this interface to
/// send prompts to LLMs and receive responses for agent reasoning.
/// </para>
/// <para>
/// Implementations should handle:
/// - Authentication and API key management
/// - Request formatting and serialization
/// - Response parsing and error handling
/// - Rate limiting and retry logic
/// - Model-specific configurations
/// </para>
/// <para>
/// For OpenAI models with function calling support, consider implementing
/// <see cref="IFunctionCallingLlmClient"/> instead of this interface.
/// </para>
/// </remarks>
/// <example>
/// <para>Basic OpenAI client implementation:</para>
/// <code>
/// public class OpenAiLlmClient : ILlmClient
/// {
///     private readonly OpenAIClient _client;
///     private readonly string _model;
///     
///     public OpenAiLlmClient(string apiKey, string model = "gpt-4")
///     {
///         _client = new OpenAIClient(apiKey);
///         _model = model;
///     }
///     
///     public async Task&lt;string&gt; CompleteAsync(IEnumerable&lt;LlmMessage&gt; messages, CancellationToken ct = default)
///     {
///         var chatMessages = messages.Select(m => new ChatMessage(m.Role, m.Content)).ToList();
///         var response = await _client.GetChatCompletionsAsync(_model, chatMessages, ct);
///         return response.Value.Choices[0].Message.Content;
///     }
/// }
/// </code>
/// </example>
public interface ILlmClient
{
    /// <summary>
    /// Sends a collection of messages to the LLM and returns the generated response.
    /// </summary>
    /// <param name="messages">
    /// A collection of messages that form the conversation context. The messages should
    /// be ordered chronologically, with the most recent message typically being the
    /// user's request or the agent's current reasoning step.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the LLM request. This is important
    /// for managing timeouts and allowing users to cancel long-running operations.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous LLM completion. The result is the
    /// generated text response from the language model.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="messages"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="messages"/> is empty or contains invalid messages.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when there is a network or HTTP-related error communicating with the LLM service.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the LLM service returns an error response or invalid data.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is called by the agent framework to get LLM responses for reasoning
    /// and decision-making. The framework handles message formatting, context management,
    /// and response parsing.
    /// </para>
    /// <para>
    /// The messages collection typically includes:
    /// - System messages defining the agent's role and capabilities
    /// - User messages containing the original goal or request
    /// - Assistant messages showing previous reasoning steps
    /// - Tool result messages providing context from tool executions
    /// </para>
    /// <para>
    /// Implementations should:
    /// - Handle authentication and API key management
    /// - Implement proper error handling and retry logic
    /// - Respect rate limits and quotas
    /// - Provide meaningful error messages for debugging
    /// - Handle model-specific response formats
    /// </para>
    /// <para>
    /// The response should be a well-formed text that the agent can parse and use for
    /// further reasoning or tool selection. For Re/Act agents, this typically includes
    /// thoughts, reasoning, and tool calls in a structured format.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Example usage in an agent context:</para>
    /// <code>
    /// var messages = new List&lt;LlmMessage&gt;
    /// {
    ///     new LlmMessage("system", "You are a helpful AI agent that can use tools to accomplish tasks."),
    ///     new LlmMessage("user", "What's the weather like in New York?"),
    ///     new LlmMessage("assistant", "I need to check the weather in New York. Let me use the weather tool."),
    ///     new LlmMessage("tool_result", "Temperature: 22°C, Conditions: Partly cloudy")
    /// };
    /// 
    /// var response = await llmClient.CompleteAsync(messages, ct);
    /// // Response: "Based on the weather data, it's currently 22°C and partly cloudy in New York."
    /// </code>
    /// </example>
    Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default);
}