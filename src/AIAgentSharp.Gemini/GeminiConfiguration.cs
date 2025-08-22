using System;
namespace AIAgentSharp.Gemini;

/// <summary>
/// Configuration options for Google Gemini LLM integration.
/// </summary>
/// <remarks>
/// This client uses the official Gemini API to access Gemini models.
/// You need a Gemini API key for authentication.
/// 
/// To get an API key:
/// 1. Go to the Google AI Studio (https://makersuite.google.com/app/apikey)
/// 2. Create a new API key
/// 3. Use this API key with the GeminiLlmClient constructor
/// 
/// API Documentation: https://ai.google.dev/gemini-api/docs
/// </remarks>
public sealed class GeminiConfiguration
{
    /// <summary>
    /// Gets or sets the Google Gemini model to use for completions.
    /// </summary>
    /// <value>
    /// The model name. Default is "gemini-2.5-flash".
    /// </value>
    /// <remarks>
    /// Common models include:
    /// - gemini-1.5-flash: Fast and cost-effective
    /// - gemini-1.5-pro: More capable but more expensive
    /// - gemini-1.0-pro: Legacy model, still effective
    /// - gemini-1.5-flash-exp: Experimental version with extended capabilities
    /// </remarks>
    public string Model { get; init; } = "gemini-2.5-flash";

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
    /// Gets or sets the top-k parameter for response generation.
    /// </summary>
    /// <value>
    /// The top-k value. Default is null (disabled).
    /// </value>
    /// <remarks>
    /// Limits the number of tokens considered for each step. Lower values make responses more focused.
    /// </remarks>
    public int? TopK { get; init; }

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
    /// Creates a new instance with default settings optimized for agent reasoning.
    /// </summary>
    /// <returns>A configuration optimized for agent tasks.</returns>
    public static GeminiConfiguration CreateForAgentReasoning()
    {
        return new GeminiConfiguration
        {
            Model = "gemini-2.5-flash",
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
    public static GeminiConfiguration CreateForCreativeTasks()
    {
        return new GeminiConfiguration
        {
            Model = "gemini-1.5-pro",
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
    public static GeminiConfiguration CreateForCostEfficiency()
    {
        return new GeminiConfiguration
        {
            Model = "gemini-1.0-pro",
            Temperature = 0.1f,
            MaxTokens = 2000,
            EnableFunctionCalling = true,
            MaxRetries = 2,
            RequestTimeout = TimeSpan.FromMinutes(1)
        };
    }
}
