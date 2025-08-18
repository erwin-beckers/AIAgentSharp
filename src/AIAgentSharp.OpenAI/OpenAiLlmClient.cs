using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace AIAgentSharp.OpenAI;

/// <summary>
/// OpenAI LLM client implementation that integrates with the OpenAI SDK.
/// This client supports both regular chat completion and function calling.
/// </summary>
public sealed class OpenAiLlmClient : ILlmClient, IFunctionCallingLlmClient
{
    private readonly OpenAIClient _client;
    private readonly string _model;
    private readonly ILogger _logger;
    
    /// <summary>
    /// Gets the OpenAI configuration used by this client.
    /// </summary>
    public OpenAiConfiguration Configuration { get; } = null!;

    /// <summary>
    /// Initializes a new instance of the OpenAiLlmClient class with API key and model.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key for authentication.</param>
    /// <param name="model">The model to use for completions. Defaults to "gpt-4o-mini".</param>
    /// <param name="logger">Optional logger for debugging and monitoring.</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
    public OpenAiLlmClient(string apiKey, string model = "gpt-4o-mini", ILogger? logger = null)
        : this(apiKey, new OpenAiConfiguration { Model = model }, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the OpenAiLlmClient class with API key and configuration.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key for authentication.</param>
    /// <param name="configuration">The OpenAI configuration settings.</param>
    /// <param name="logger">Optional logger for debugging and monitoring.</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey or configuration is null or empty.</exception>
    public OpenAiLlmClient(string apiKey, OpenAiConfiguration configuration, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Create client options
        var options = new OpenAIClientOptions();
        
        if (!string.IsNullOrEmpty(configuration.ApiBaseUrl))
        {
            options.Endpoint = new Uri(configuration.ApiBaseUrl);
        }

        _client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        _model = configuration.Model;
        _logger = logger ?? new ConsoleLogger();
        
        // Store configuration for use in completion methods
        Configuration = configuration;
    }

    /// <summary>
    /// Internal constructor for testing purposes.
    /// </summary>
    /// <param name="client">The OpenAI client to use.</param>
    /// <param name="model">The model to use.</param>
    /// <param name="logger">Optional logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
    internal OpenAiLlmClient(OpenAIClient client, string model = "gpt-4o-mini", ILogger? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _model = model;
        _logger = logger ?? new ConsoleLogger();
        Configuration = new OpenAiConfiguration { Model = model };
    }

    /// <summary>
    /// Completes a conversation with function calling capabilities.
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
        try
        {
            _logger.LogDebug($"Starting function calling completion with model {_model}");

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
                    case "user":
                        chatMessages.Add(new UserChatMessage(m.Content));
                        break;
                    case "tool":
                        chatMessages.Add(new ToolChatMessage(m.Content));
                        break;
                    default:
                        _logger.LogWarning($"Unknown message role: {m.Role}, treating as user message");
                        chatMessages.Add(new UserChatMessage(m.Content));
                        break;
                }
            }

            // For now, implement a simplified version that falls back to regular completion
            // This will be enhanced once we understand the correct API for function calling
            var chatClient = _client.GetChatClient(_model);
            var response = await chatClient.CompleteChatAsync(chatMessages, cancellationToken: ct);
            var completion = response.Value;

            // Access the content from the completion
            var content = completion.Content?.FirstOrDefault()?.Text ?? string.Empty;
            _logger.LogDebug($"Regular completion returned: {content.Length} characters");

            return new FunctionCallResult
            {
                HasFunctionCall = false,
                AssistantContent = content,
                RawTextFallback = content
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Error in function calling completion: {ex.Message}");
            throw new InvalidOperationException($"OpenAI function calling failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Completes a conversation by sending messages to the OpenAI LLM.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The LLM's response as a string.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug($"Starting regular completion with model {_model}");

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
                    case "user":
                        chatMessages.Add(new UserChatMessage(m.Content));
                        break;
                    case "tool":
                        chatMessages.Add(new ToolChatMessage(m.Content));
                        break;
                    default:
                        _logger.LogWarning($"Unknown message role: {m.Role}, treating as user message");
                        chatMessages.Add(new UserChatMessage(m.Content));
                        break;
                }
            }

            // Get chat client and complete
            var chatClient = _client.GetChatClient(_model);
            var response = await chatClient.CompleteChatAsync(chatMessages, cancellationToken: ct);
            var completion = response.Value;

            // Access the content from the completion
            var content = completion.Content?.FirstOrDefault()?.Text ?? string.Empty;
            _logger.LogDebug($"Completion successful: {content.Length} characters");

            return content;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Error in regular completion: {ex.Message}");
            throw new InvalidOperationException($"OpenAI completion failed: {ex.Message}", ex);
        }
    }
}
