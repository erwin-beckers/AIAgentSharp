namespace AIAgentSharp.OpenAI;

/// <summary>
/// Configuration options for OpenAI LLM integration.
/// </summary>
public sealed class OpenAiConfiguration
{
    /// <summary>
    /// Gets or sets the OpenAI model to use for completions.
    /// </summary>
    /// <value>
    /// The model name. Default is "gpt-4o-mini".
    /// </value>
    /// <remarks>
    /// Common models include:
    /// - gpt-4o-mini: Fast and cost-effective
    /// - gpt-4o: More capable but more expensive
    /// - gpt-4-turbo: Good balance of capability and cost
    /// - gpt-3.5-turbo: Legacy model, still effective for simple tasks
    /// </remarks>
    public string Model { get; init; } = "gpt-4o-mini";

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    /// <value>
    /// The maximum token count. Default is 4000.
    /// </value>
    /// <remarks>
    /// Higher values allow longer responses but increase costs.
    /// Consider your use case and budget when setting this value.
    /// </remarks>
    public int MaxTokens { get; init; } = 4000;

    /// <summary>
    /// Gets or sets the temperature for response generation.
    /// </summary>
    /// <value>
    /// The temperature value between 0.0 and 2.0. Default is 0.1.
    /// </value>
    /// <remarks>
    /// - 0.0: Most deterministic responses
    /// - 0.1-0.3: Good for structured tasks and reasoning
    /// - 0.5-0.7: Balanced creativity and consistency
    /// - 1.0+: More creative and varied responses
    /// </remarks>
    public float Temperature { get; init; } = 0.1f;

    /// <summary>
    /// Gets or sets the top-p parameter for nucleus sampling.
    /// </summary>
    /// <value>
    /// The top-p value between 0.0 and 1.0. Default is 1.0.
    /// </value>
    /// <remarks>
    /// Controls diversity via nucleus sampling. Lower values make responses more focused.
    /// </remarks>
    public float TopP { get; init; } = 1.0f;

    /// <summary>
    /// Gets or sets the frequency penalty for repetition.
    /// </summary>
    /// <value>
    /// The frequency penalty between -2.0 and 2.0. Default is 0.0.
    /// </value>
    /// <remarks>
    /// Positive values reduce repetition, negative values increase it.
    /// </remarks>
    public float FrequencyPenalty { get; init; } = 0.0f;

    /// <summary>
    /// Gets or sets the presence penalty for topic repetition.
    /// </summary>
    /// <value>
    /// The presence penalty between -2.0 and 2.0. Default is 0.0.
    /// </value>
    /// <remarks>
    /// Positive values encourage the model to talk about new topics.
    /// </remarks>
    public float PresencePenalty { get; init; } = 0.0f;

    /// <summary>
    /// Gets or sets whether to enable streaming responses.
    /// </summary>
    /// <value>
    /// True if streaming is enabled; otherwise, false. Default is false.
    /// </value>
    /// <remarks>
    /// Streaming provides real-time response generation but requires special handling.
    /// </remarks>
    public bool EnableStreaming { get; init; } = false;

    /// <summary>
    /// Gets or sets the timeout for API requests.
    /// </summary>
    /// <value>
    /// The timeout duration. Default is 2 minutes.
    /// </value>
    /// <remarks>
    /// Longer timeouts are needed for complex reasoning tasks.
    /// </remarks>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the number of retry attempts for failed requests.
    /// </summary>
    /// <value>
    /// The number of retries. Default is 3.
    /// </value>
    /// <remarks>
    /// Retries help handle transient network issues and rate limits.
    /// </remarks>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts.
    /// </summary>
    /// <value>
    /// The retry delay. Default is 1 second.
    /// </value>
    /// <remarks>
    /// Exponential backoff is recommended for production use.
    /// </remarks>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets whether to include function calling in responses.
    /// </summary>
    /// <value>
    /// True if function calling should be enabled; otherwise, false. Default is true.
    /// </value>
    /// <remarks>
    /// Function calling allows the model to invoke tools and functions.
    /// </remarks>
    public bool EnableFunctionCalling { get; init; } = true;

    /// <summary>
    /// Gets or sets the organization ID for OpenAI API calls.
    /// </summary>
    /// <value>
    /// The organization ID. Default is null.
    /// </value>
    /// <remarks>
    /// Required for enterprise OpenAI accounts.
    /// </remarks>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// Gets or sets the API base URL for custom endpoints.
    /// </summary>
    /// <value>
    /// The base URL. Default is null (uses OpenAI's default).
    /// </value>
    /// <remarks>
    /// Useful for using OpenAI-compatible APIs or proxies.
    /// </remarks>
    public string? ApiBaseUrl { get; init; }

    /// <summary>
    /// Creates a new instance with default settings optimized for agent reasoning.
    /// </summary>
    /// <returns>A configuration optimized for agent tasks.</returns>
    public static OpenAiConfiguration CreateForAgentReasoning()
    {
        return new OpenAiConfiguration
        {
            Model = "gpt-4o-mini",
            Temperature = 0.1f,
            MaxTokens = 4000,
            EnableFunctionCalling = true,
            MaxRetries = 3,
            RequestTimeout = TimeSpan.FromMinutes(2)
        };
    }

    /// <summary>
    /// Creates a new instance with settings optimized for creative tasks.
    /// </summary>
    /// <returns>A configuration optimized for creative tasks.</returns>
    public static OpenAiConfiguration CreateForCreativeTasks()
    {
        return new OpenAiConfiguration
        {
            Model = "gpt-4o",
            Temperature = 0.7f,
            MaxTokens = 6000,
            EnableFunctionCalling = true,
            MaxRetries = 3,
            RequestTimeout = TimeSpan.FromMinutes(3)
        };
    }

    /// <summary>
    /// Creates a new instance with settings optimized for cost efficiency.
    /// </summary>
    /// <returns>A configuration optimized for cost efficiency.</returns>
    public static OpenAiConfiguration CreateForCostEfficiency()
    {
        return new OpenAiConfiguration
        {
            Model = "gpt-3.5-turbo",
            Temperature = 0.1f,
            MaxTokens = 2000,
            EnableFunctionCalling = true,
            MaxRetries = 2,
            RequestTimeout = TimeSpan.FromMinutes(1)
        };
    }
}
