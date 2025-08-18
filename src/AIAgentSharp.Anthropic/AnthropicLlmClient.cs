using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using Tool = Anthropic.SDK.Common.Tool;

namespace AIAgentSharp.Anthropic;

/// <summary>
///     Anthropic Claude LLM client implementation that integrates with the Anthropic Claude API.
///     This client supports both regular chat completion and function calling via tools.
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
    ///     Sends a collection of messages to the LLM and returns the generated response.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the LLM completion.</returns>
    /// <exception cref="ArgumentNullException">Thrown when messages is null.</exception>
    /// <exception cref="ArgumentException">Thrown when messages is empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<LlmCompletionResult> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        var messageList = messages.ToList();

        if (messageList.Count == 0)
        {
            throw new ArgumentException("Messages cannot be empty.", nameof(messages));
        }

        // Convert AIAgentSharp messages to Anthropic messages
        var anthropicMessages = ConvertToAnthropicMessages(messageList);

        var parameters = new MessageParameters
        {
            Messages = anthropicMessages,
            MaxTokens = Configuration.MaxTokens,
            Model = _model,
            Temperature = (decimal)Configuration.Temperature
            //TopP = (decimal)Configuration.TopP,
            //TopK = Configuration.TopK
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, ct);

        var textContent = response.Content.OfType<TextContent>().FirstOrDefault();

        if (textContent?.Text == null)
        {
            throw new InvalidOperationException("Invalid response from Anthropic API");
        }

        return new LlmCompletionResult
        {
            Content = textContent.Text,
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

    /// <summary>
    ///     Completes a conversation with function calling capabilities.
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
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        if (functions == null)
        {
            throw new ArgumentNullException(nameof(functions));
        }

        var messageList = messages.ToList();

        if (messageList.Count == 0)
        {
            throw new ArgumentException("Messages cannot be empty.", nameof(messages));
        }

        // Convert AIAgentSharp messages to Anthropic messages
        var anthropicMessages = ConvertToAnthropicMessages(messageList);

        // Convert OpenAI function specs to Anthropic tools
        var tools = ConvertToAnthropicTools(functions);

        var parameters = new MessageParameters
        {
            Messages = anthropicMessages,
            Tools = tools,
            MaxTokens = Configuration.MaxTokens,
            Model = _model,
            Temperature = (decimal)Configuration.Temperature
            //TopP = (decimal)Configuration.TopP,
            //TopK = Configuration.TopK
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, ct);

        var textContent = response.Content.OfType<TextContent>().FirstOrDefault();
        var toolUseContent = response.Content.OfType<ToolUseContent>().FirstOrDefault();

        var result = new FunctionCallResult
        {
            AssistantContent = textContent?.Text ?? string.Empty,
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
            result = new FunctionCallResult
            {
                HasFunctionCall = true,
                FunctionName = toolUseContent.Name ?? string.Empty,
                FunctionArgumentsJson = toolUseContent.Input?.ToJsonString() ?? "{}",
                AssistantContent = textContent?.Text ?? string.Empty,
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

        return result;
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

    private static List<Tool> ConvertToAnthropicTools(IEnumerable<OpenAiFunctionSpec> functions)
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