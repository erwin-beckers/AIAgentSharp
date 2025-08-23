using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics;

namespace AIAgentSharp.OpenAI;

/// <summary>
/// OpenAI LLM client implementation that integrates with the OpenAI SDK.
/// This client supports text completion, function calling, and streaming through a unified interface.
/// </summary>
public sealed class OpenAiLlmClient : ILlmClient
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
    /// <param name="model">The model to use for completions. Defaults to "gpt-5-nano".</param>
    /// <param name="logger">Optional logger for debugging and monitoring.</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
    public OpenAiLlmClient(string apiKey, string model = "gpt-5-nano", ILogger? logger = null)
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
    internal OpenAiLlmClient(OpenAIClient client, string model = "gpt-5-nano", ILogger? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _model = model;
        _logger = logger ?? new ConsoleLogger();
        Configuration = new OpenAiConfiguration { Model = model };
    }

    /// <summary>
    /// Streams chunks from the OpenAI LLM based on the provided request.
    /// This method always returns chunks, regardless of whether streaming is enabled.
    /// </summary>
    /// <param name="request">The unified request containing messages, functions, and configuration.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of LLM streaming chunks.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async IAsyncEnumerable<LlmStreamingChunk> StreamAsync(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Messages == null || !request.Messages.Any())
        {
            throw new ArgumentException("Request must contain at least one message.", nameof(request));
        }

        _logger.LogDebug($"Starting streaming completion with model {_model}, response type: {request.ResponseType}");

        // Map messages to OpenAI.Chat message types
        var chatMessages = new List<ChatMessage>();

        Trace.WriteLine("");
        Trace.WriteLine("");
        Trace.WriteLine("---------------------------------------------------------------------------");
        foreach (var m in request.Messages)
        {
            Console.WriteLine($"LLM: snd {m.Role}:{m.Content}");
            Trace.WriteLine($"LLM: snd {m.Role}:{m.Content}");
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
        // Removed early process termination to allow tool schemas to be sent to the API
        // Create chat completion options
        var options = new ChatCompletionOptions();

        // Set temperature and top_p if provided
        if (request.Temperature.HasValue)
        {
            options.Temperature = (float)request.Temperature.Value;
        }

        if (request.TopP.HasValue)
        {
            options.TopP = (float)request.TopP.Value;
        }

        var chatClient = _client.GetChatClient(_model);
        var usage = new LlmUsage
        {
            Model = _model,
            Provider = "OpenAI"
        };

        // Respect EnableStreaming flag; default to streaming when true, otherwise non-streaming
        if (request.EnableStreaming)
        {
            var completionUpdates = chatClient.CompleteChatStreamingAsync(chatMessages, options);
            var contentBuilder = new System.Text.StringBuilder();

            await foreach (var completionUpdate in completionUpdates.WithCancellation(ct))
            {
                // Accumulate the text content as new updates arrive
                foreach (var contentPart in completionUpdate.ContentUpdate)
                {
                    contentBuilder.Append(contentPart.Text);

                    // Yield a chunk for each content update
                    yield return new LlmStreamingChunk
                    {
                        Content = contentPart.Text,
                        IsFinal = false,
                        ActualResponseType = LlmResponseType.Streaming,
                        Usage = usage
                    };
                }

                // Handle finish reasons
                if (completionUpdate.FinishReason.HasValue)
                {
                    switch (completionUpdate.FinishReason.Value)
                    {
                        case ChatFinishReason.Stop:
                            // Final chunk with complete content
                            yield return new LlmStreamingChunk
                            {
                                Content = string.Empty, // Don't send duplicate content
                                IsFinal = true,
                                FinishReason = "stop",
                                ActualResponseType = LlmResponseType.Text,
                                Usage = usage
                            };
                            break;

                        case ChatFinishReason.Length:
                            yield return new LlmStreamingChunk
                            {
                                Content = string.Empty, // Don't send duplicate content
                                IsFinal = true,
                                FinishReason = "length",
                                ActualResponseType = LlmResponseType.Text,
                                Usage = usage
                            };
                            break;

                        case ChatFinishReason.ContentFilter:
                            yield return new LlmStreamingChunk
                            {
                                Content = string.Empty, // Don't send duplicate content
                                IsFinal = true,
                                FinishReason = "content_filter",
                                ActualResponseType = LlmResponseType.Text,
                                Usage = usage
                            };
                            break;
                    }
                }
            }
        }
        else
        {
            var response = await chatClient.CompleteChatAsync(chatMessages, options, ct);
            var completion = response.Value;
            var content = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
            Trace.WriteLine($"RECV:{content}");
            usage.InputTokens = completion.Usage.InputTokenCount;
            usage.OutputTokens = completion.Usage.OutputTokenCount;
            yield return new LlmStreamingChunk
            {
                Content = content,
                IsFinal = true,
                FinishReason = completion.FinishReason.ToString().ToLowerInvariant(),
                ActualResponseType = LlmResponseType.Text,
                Usage = usage
            };
        }
    }

    private static List<ChatTool> ConvertToOpenAiTools(IEnumerable<FunctionSpec> functions)
    {
        var tools = new List<ChatTool>();

        foreach (var function in functions)
        {
            string? parametersJson = null;
            if (function.ParametersSchema != null)
            {
                parametersJson = System.Text.Json.JsonSerializer.Serialize(function.ParametersSchema);
            }

            var tool = ChatTool.CreateFunctionTool(
                functionName: function.Name,
                functionDescription: function.Description ?? string.Empty,
                functionParameters: !string.IsNullOrEmpty(parametersJson)
                    ? BinaryData.FromBytes(System.Text.Encoding.UTF8.GetBytes(parametersJson))
                    : null
            );
            tools.Add(tool);
        }

        return tools;
    }
}
