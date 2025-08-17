namespace AIAgentSharp;

/// <summary>
/// Defines the core interface for tools that can be invoked by AI agents.
/// Tools are the primary mechanism through which agents interact with external systems,
/// APIs, or perform computational tasks.
/// </summary>
/// <remarks>
/// <para>
/// Tools are the building blocks of agent capabilities. Each tool represents a specific
/// action or operation that an agent can perform. Tools can range from simple data
/// retrieval operations to complex business logic implementations.
/// </para>
/// <para>
/// For strongly-typed tools with automatic schema generation, consider using the
/// <see cref="BaseTool{TParams, TResult}"/> base class instead of implementing this
/// interface directly.
/// </para>
/// <para>
/// Tools should be designed to be:
/// - <strong>Idempotent</strong>: Multiple calls with the same parameters should produce the same result
/// - <strong>Stateless</strong>: Tools should not maintain internal state between calls
/// - <strong>Thread-safe</strong>: Tools may be called concurrently from multiple threads
/// - <strong>Resilient</strong>: Tools should handle errors gracefully and provide meaningful error messages
/// </para>
/// </remarks>
/// <example>
/// <para>Basic tool implementation:</para>
/// <code>
/// public class WeatherTool : ITool
/// {
///     public string Name => "get_weather";
///     
///     public async Task&lt;object?&gt; InvokeAsync(Dictionary&lt;string, object?&gt; parameters, CancellationToken ct = default)
///     {
///         var city = parameters["city"]?.ToString();
///         var weather = await FetchWeatherAsync(city, ct);
///         return new { temperature = weather.Temperature, description = weather.Description };
///     }
/// }
/// </code>
/// </example>
public interface ITool
{
    /// <summary>
    /// Gets the unique name of the tool.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies this tool. The name should be:
    /// - Descriptive and human-readable
    /// - Lowercase with underscores (snake_case)
    /// - Unique within the set of tools available to an agent
    /// </value>
    /// <example>
    /// Examples of good tool names:
    /// - "get_weather"
    /// - "search_flights"
    /// - "calculate_distance"
    /// - "send_email"
    /// </example>
    string Name { get; }

    /// <summary>
    /// Invokes the tool with the specified parameters.
    /// </summary>
    /// <param name="parameters">
    /// A dictionary containing the parameters for the tool invocation.
    /// The keys should match the expected parameter names, and values should be
    /// of the appropriate types for the tool's requirements.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the tool execution.
    /// Tools should respect this token and cancel their operations when requested.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous tool execution. The result should be:
    /// - <c>null</c> if the tool has no meaningful output
    /// - An object that can be serialized to JSON for LLM consumption
    /// - Structured data that provides useful information to the agent
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parameters"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is called by the agent framework when the LLM decides to use this tool.
    /// The framework handles parameter validation, error handling, and result caching.
    /// </para>
    /// <para>
    /// Tool implementations should:
    /// - Validate input parameters before processing
    /// - Handle exceptions gracefully and return meaningful error information
    /// - Respect the cancellation token for long-running operations
    /// - Return results that are useful for the agent's decision-making process
    /// </para>
    /// <para>
    /// The returned object will be serialized to JSON and provided to the LLM as
    /// part of the agent's reasoning context. Therefore, the result should be
    /// structured and contain relevant information for the agent's next steps.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Example implementation with parameter validation:</para>
    /// <code>
    /// public async Task&lt;object?&gt; InvokeAsync(Dictionary&lt;string, object?&gt; parameters, CancellationToken ct = default)
    /// {
    ///     ct.ThrowIfCancellationRequested();
    ///     
    ///     if (!parameters.TryGetValue("city", out var cityObj) || cityObj?.ToString() is not string city)
    ///     {
    ///         throw new ArgumentException("City parameter is required and must be a string");
    ///     }
    ///     
    ///     if (string.IsNullOrWhiteSpace(city))
    ///     {
    ///         throw new ArgumentException("City cannot be empty");
    ///     }
    ///     
    ///     try
    ///     {
    ///         var weather = await FetchWeatherAsync(city, ct);
    ///         return new { 
    ///             city = city,
    ///             temperature = weather.Temperature,
    ///             unit = "Celsius",
    ///             description = weather.Description,
    ///             humidity = weather.Humidity
    ///         };
    ///     }
    ///     catch (HttpRequestException ex)
    ///     {
    ///         throw new ToolExecutionException($"Failed to fetch weather for {city}: {ex.Message}");
    ///     }
    /// }
    /// </code>
    /// </example>
    Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default);
}