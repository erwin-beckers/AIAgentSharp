using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using Tool = Anthropic.SDK.Common.Tool;

namespace AIAgentSharp.Anthropic;

/// <summary>
///     Anthropic Claude LLM client implementation that integrates with the Anthropic Claude API.
///     This client supports text completion, function calling, and streaming through a unified interface.
/// </summary>
public sealed class AnthropicLlmClient : ILlmClient
{
    private readonly AnthropicClient _client;
    private readonly string _model;

    /// <summary>
    ///     Initializes a new instance of the AnthropicLlmClient class with API key and model.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key for authentication.</param>
    /// <param name="model">The model to use for completions. Defaults to "claude-3-5-sonnet-20241022".</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
    public AnthropicLlmClient(string apiKey, string model = "claude-opus-4-1-20250805")
        : this(apiKey, new AnthropicConfiguration { Model = model })
    {
    }

    /// <summary>
    ///     Initializes a new instance of the AnthropicLlmClient class with API key and configuration.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key for authentication.</param>
    /// <param name="configuration">The Anthropic configuration settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey or configuration is null or empty.</exception>
    public AnthropicLlmClient(string apiKey, AnthropicConfiguration configuration)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _client = new AnthropicClient(apiKey);
        _model = configuration.Model;

        // Store configuration for use in completion methods
        Configuration = configuration;
    }

    /// <summary>
    ///     Internal constructor for testing purposes.
    /// </summary>
    /// <param name="client">The Anthropic client to use.</param>
    /// <param name="model">The model to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
    internal AnthropicLlmClient(AnthropicClient client, string model = "claude-3-5-sonnet-20241022")
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _model = model;
        Configuration = new AnthropicConfiguration { Model = model };
    }

    /// <summary>
    ///     Gets the Anthropic configuration used by this client.
    /// </summary>
    public AnthropicConfiguration Configuration { get; }



    /// <summary>
    /// Streams chunks from the Anthropic LLM based on the provided request.
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

        // Map messages to Anthropic message types
        var anthropicMessages = new List<Message>();

        foreach (var m in request.Messages)
        {
            switch (m.Role)
            {
                case "system":
                    // Anthropic doesn't support system messages in the same way
                    // We'll prepend system content to the first user message
                    break;
                case "assistant":
                    anthropicMessages.Add(new Message(RoleType.Assistant, m.Content));
                    break;
                case "user":
                    anthropicMessages.Add(new Message(RoleType.User, m.Content));
                    break;
                case "tool":
                    // Anthropic uses tool results differently
                    anthropicMessages.Add(new Message(RoleType.User, m.Content));
                    break;
                default:
                    anthropicMessages.Add(new Message(RoleType.User, m.Content));
                    break;
            }
        }

        // Determine the actual response type based on request and available functions
        var actualResponseType = DetermineActualResponseType(request);

        // Handle different response types
        switch (actualResponseType)
        {
            case LlmResponseType.FunctionCall:
                await foreach (var chunk in HandleFunctionCallRequestSafe(request, anthropicMessages, ct))
                {
                    yield return chunk;
                }
                break;

            case LlmResponseType.Streaming:
                await foreach (var chunk in HandleStreamingRequestSafe(request, anthropicMessages, ct))
                {
                    yield return chunk;
                }
                break;

            case LlmResponseType.Text:
            default:
                await foreach (var chunk in HandleTextRequestSafe(request, anthropicMessages, ct))
                {
                    yield return chunk;
                }
                break;
        }
    }

    private LlmResponseType DetermineActualResponseType(LlmRequest request)
    {
        // If streaming is explicitly requested, use streaming
        if (request.ResponseType == LlmResponseType.Streaming || request.EnableStreaming)
        {
            return LlmResponseType.Streaming;
        }

        // If functions are provided and function calling is requested, use function calling
        if (request.Functions != null && request.Functions.Any() && 
            (request.ResponseType == LlmResponseType.FunctionCall || request.ResponseType == LlmResponseType.Auto))
        {
            return LlmResponseType.FunctionCall;
        }

        // Default to text completion
        return LlmResponseType.Text;
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleTextRequestSafe(LlmRequest request, List<Message> anthropicMessages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        IAsyncEnumerable<LlmStreamingChunk> result;
        try
        {
            result = HandleTextRequest(request, anthropicMessages, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Anthropic text request failed: {ex.Message}", ex);
        }

        await foreach (var chunk in result)
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleTextRequest(LlmRequest request, List<Message> anthropicMessages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var parameters = new MessageParameters
        {
            Messages = anthropicMessages,
            MaxTokens = request.MaxTokens ?? Configuration.MaxTokens,
            Model = _model,
            Temperature = (decimal)(request.Temperature ?? Configuration.Temperature)
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, ct);

        var textContent = response.Content.OfType<TextContent>().FirstOrDefault();

        if (textContent?.Text == null)
        {
            throw new InvalidOperationException("Invalid response from Anthropic API");
        }

        yield return new LlmStreamingChunk
        {
            Content = textContent.Text,
            IsFinal = true,
            FinishReason = "stop",
            ActualResponseType = LlmResponseType.Text,
            Usage = response.Usage != null
                ? new LlmUsage
                {
                    InputTokens = response.Usage.InputTokens,
                    OutputTokens = response.Usage.OutputTokens,
                    Model = _model,
                    Provider = "Anthropic"
                }
                : null
        };
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleFunctionCallRequestSafe(LlmRequest request, List<Message> anthropicMessages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        IAsyncEnumerable<LlmStreamingChunk> result;
        try
        {
            result = HandleFunctionCallRequest(request, anthropicMessages, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Anthropic function call request failed: {ex.Message}", ex);
        }

        await foreach (var chunk in result)
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleFunctionCallRequest(LlmRequest request, List<Message> anthropicMessages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // Convert FunctionSpec to Anthropic tools
        var tools = ConvertToAnthropicTools(request.Functions!);

        var parameters = new MessageParameters
        {
            Messages = anthropicMessages,
            Tools = tools,
            MaxTokens = request.MaxTokens ?? Configuration.MaxTokens,
            Model = _model,
            Temperature = (decimal)(request.Temperature ?? Configuration.Temperature)
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, ct);

        var textContent = response.Content.OfType<TextContent>().FirstOrDefault();
        var toolUseContent = response.Content.OfType<ToolUseContent>().FirstOrDefault();

        var chunk = new LlmStreamingChunk
        {
            Content = textContent?.Text ?? string.Empty,
            IsFinal = true,
            FinishReason = "stop",
            ActualResponseType = LlmResponseType.Text, // Default to text, will update if function call found
            Usage = response.Usage != null
                ? new LlmUsage
                {
                    InputTokens = response.Usage.InputTokens,
                    OutputTokens = response.Usage.OutputTokens,
                    Model = _model,
                    Provider = "Anthropic"
                }
                : null
        };

        // Check for tool use
        if (toolUseContent != null)
        {
            chunk.ActualResponseType = LlmResponseType.FunctionCall;
            chunk.FunctionCall = new LlmFunctionCall
            {
                Name = toolUseContent.Name ?? string.Empty,
                ArgumentsJson = toolUseContent.Input?.ToJsonString() ?? "{}",
                Arguments = toolUseContent.Input != null 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(toolUseContent.Input.ToJsonString()) ?? new Dictionary<string, object>()
                    : new Dictionary<string, object>()
            };
        }

        yield return chunk;
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleStreamingRequestSafe(LlmRequest request, List<Message> anthropicMessages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        IAsyncEnumerable<LlmStreamingChunk> result;
        try
        {
            result = HandleStreamingRequest(request, anthropicMessages, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Anthropic streaming request failed: {ex.Message}", ex);
        }

        await foreach (var chunk in result)
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleStreamingRequest(LlmRequest request, List<Message> anthropicMessages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // For now, use the same logic as text request until streaming is properly implemented
        await foreach (var chunk in HandleTextRequest(request, anthropicMessages, ct))
        {
            chunk.ActualResponseType = LlmResponseType.Streaming;
            yield return chunk;
        }
    }

    private static List<Message> ConvertToAnthropicMessages(IEnumerable<LlmMessage> messages)
    {
        var anthropicMessages = new List<Message>();

        foreach (var message in messages)
        {
            var role = message.Role.ToLowerInvariant() switch
            {
                "system" => RoleType.User, // Anthropic doesn't have system role, convert to user
                "user" => RoleType.User,
                "assistant" => RoleType.Assistant,
                "tool_result" => RoleType.User, // Convert tool results to user messages
                _ => RoleType.User // Default to user for unknown roles
            };

            anthropicMessages.Add(new Message
            {
                Role = role,
                Content = [new TextContent { Text = message.Content }]
            });
        }

        return anthropicMessages;
    }

    private static List<Tool> ConvertToAnthropicTools(IEnumerable<FunctionSpec> functions)
    {
        var tools = new List<Tool>();

        foreach (var function in functions)
        {
            var schemaNode = JsonNode.Parse(JsonSerializer.Serialize(function.ParametersSchema))!;
            var tool = new Function(
                function.Name,
                function.Description ?? string.Empty,
                schemaNode
            );

            tools.Add(tool);
        }

        return tools;
    }
}