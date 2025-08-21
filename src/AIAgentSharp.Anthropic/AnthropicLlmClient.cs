using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using System.Text.Json.Nodes;
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
    /// <param name="model">The model to use for completions. Defaults to "claude-opus-4-1-20250805".</param>
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
        Configuration = configuration;
    }

    /// <summary>
    ///     Internal constructor for testing purposes.
    /// </summary>
    /// <param name="client">The Anthropic client to use.</param>
    /// <param name="model">The model to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
    internal AnthropicLlmClient(AnthropicClient client, string model = "claude-opus-4-1-20250805")
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

        // Convert AIAgentSharp messages to Anthropic messages
        var anthropicMessages = ConvertToAnthropicMessages(request.Messages);

        // Create parameters
        var parameters = new MessageParameters
        {
            Messages = anthropicMessages,
            MaxTokens = request.MaxTokens ?? Configuration.MaxTokens,
            Model = _model,
            Temperature = (decimal)(request.Temperature ?? Configuration.Temperature),
            Stream = true
        };

        // Add tools if functions are provided
        if (request.Functions != null && request.Functions.Any())
        {
            parameters.Tools = ConvertToAnthropicTools(request.Functions);
        }

        var usage = new LlmUsage
        {
            Model = _model,
            Provider = "Anthropic"
        };

        // Use the official SDK's streaming method
        await foreach (var response in _client.Messages.StreamClaudeMessageAsync(parameters, ct))
        {
            if (response.Delta?.Text != null)
            {
                yield return new LlmStreamingChunk
                {
                    Content = response.Delta.Text,
                    IsFinal = false,
                    ActualResponseType = LlmResponseType.Streaming,
                    Usage = usage
                };
            }

            // Update usage from final response
            if (response.Usage != null)
            {
                usage.InputTokens = response.Usage.InputTokens;
                usage.OutputTokens = response.Usage.OutputTokens;
            }

            // Handle function calls if present - simplified for now
            // TODO: Implement proper function call handling when SDK supports it
        }

        // Yield final chunk
        yield return new LlmStreamingChunk
        {
            Content = string.Empty,
            IsFinal = true,
            FinishReason = "stop",
            ActualResponseType = LlmResponseType.Text,
            Usage = usage
        };
    }

    private static List<Message> ConvertToAnthropicMessages(IEnumerable<LlmMessage> messages)
    {
        var anthropicMessages = new List<Message>();
        var systemMessages = new List<string>();

        // First pass: collect system messages
        foreach (var m in messages)
        {
            if (m.Role == "system")
            {
                systemMessages.Add(m.Content);
            }
        }

        // Second pass: convert messages and prepend system content to first user message
        bool systemMessagesPrepended = false;
        foreach (var m in messages)
        {
            switch (m.Role)
            {
                case "system":
                    // Already handled in first pass
                    break;
                case "assistant":
                    anthropicMessages.Add(new Message(RoleType.Assistant, m.Content));
                    break;
                case "user":
                    var userContent = m.Content;
                    
                    // Prepend system messages to the first user message
                    if (!systemMessagesPrepended && systemMessages.Any())
                    {
                        var combinedContent = string.Join("\n\n", systemMessages) + "\n\n" + userContent;
                        anthropicMessages.Add(new Message(RoleType.User, combinedContent));
                        systemMessagesPrepended = true;
                    }
                    else
                    {
                        anthropicMessages.Add(new Message(RoleType.User, userContent));
                    }
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

        return anthropicMessages;
    }

    private static List<Tool> ConvertToAnthropicTools(IEnumerable<FunctionSpec> functions)
    {
        var tools = new List<Tool>();

        foreach (var function in functions)
        {
            var schemaNode = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(function.ParametersSchema))!;
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