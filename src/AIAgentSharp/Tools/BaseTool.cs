using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace AIAgentSharp;

/// <summary>
/// Base class for strongly-typed tools that provides automatic parameter validation, schema generation,
/// and introspection capabilities. This class simplifies tool creation by handling common boilerplate
/// code and providing a type-safe interface for tool implementations.
/// </summary>
/// <typeparam name="TParams">
/// The type of the tool's parameters. This should be a class with properties decorated with
/// <see cref="ToolFieldAttribute"/> and validation attributes like <see cref="RequiredAttribute"/>.
/// </typeparam>
/// <typeparam name="TResult">
/// The type of the tool's result. This can be any serializable type that represents the
/// output of the tool execution.
/// </typeparam>
/// <remarks>
/// <para>
/// <see cref="BaseTool{TParams, TResult}"/> is the recommended base class for creating tools
/// in the AIAgentSharp framework. It provides several key benefits:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <strong>Type Safety</strong>: Parameters are strongly-typed, reducing runtime errors
/// </description></item>
/// <item><description>
/// <strong>Automatic Validation</strong>: Uses DataAnnotations for parameter validation
/// </description></item>
/// <item><description>
/// <strong>Schema Generation</strong>: Automatically generates JSON schemas for LLM consumption
/// </description></item>
/// <item><description>
/// <strong>Introspection</strong>: Provides tool descriptions for the LLM
/// </description></item>
/// <item><description>
/// <strong>Error Handling</strong>: Comprehensive error handling and validation
/// </description></item>
/// </list>
/// <para>
/// To create a tool using this base class:
/// </para>
/// <list type="number">
/// <item><description>Define a parameter class with appropriate properties and validation attributes</description></item>
/// <item><description>Inherit from <see cref="BaseTool{TParams, TResult}"/></description></item>
/// <item><description>Implement the abstract <see cref="Name"/> and <see cref="Description"/> properties</description></item>
/// <item><description>Override <see cref="InvokeTypedAsync(TParams, CancellationToken)"/> with your tool logic</description></item>
/// </list>
/// </remarks>
/// <example>
/// <para>Complete tool implementation example:</para>
/// <code>
/// [ToolParams(Description = "Parameters for weather lookup")]
/// public sealed class WeatherParams
/// {
///     [ToolField(Description = "City name", Example = "New York", Required = true)]
///     [Required]
///     [MinLength(1)]
///     public string City { get; set; } = default!;
///     
///     [ToolField(Description = "Temperature unit", Example = "Celsius")]
///     public string Unit { get; set; } = "Celsius";
/// }
/// 
/// public sealed class WeatherTool : BaseTool&lt;WeatherParams, object&gt;
/// {
///     public override string Name => "get_weather";
///     public override string Description => "Get current weather information for a city";
/// 
///     protected override async Task&lt;object&gt; InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
///     {
///         ct.ThrowIfCancellationRequested();
///         
///         // Your weather API logic here
///         var weather = await FetchWeatherAsync(parameters.City, parameters.Unit, ct);
///         
///         return new { 
///             city = parameters.City,
///             temperature = weather.Temperature,
///             unit = parameters.Unit,
///             description = weather.Description,
///             humidity = weather.Humidity
///         };
///     }
/// }
/// </code>
/// </example>
public abstract class BaseTool<TParams, TResult> : ITool, IToolIntrospect, IFunctionSchemaProvider
{
    /// <summary>
    /// Gets a human-readable description of what the tool does.
    /// </summary>
    /// <value>
    /// A clear, concise description that explains the tool's purpose and functionality.
    /// This description is used by the LLM to understand when and how to use the tool.
    /// </value>
    /// <remarks>
    /// <para>
    /// The description should be:
    /// - Clear and concise (1-2 sentences)
    /// - Action-oriented (e.g., "Get weather information" not "A tool for weather")
    /// - Specific about what the tool does
    /// - Written from the user's perspective
    /// </para>
    /// <para>
    /// Good examples:
    /// - "Get current weather information for a city"
    /// - "Search for available flights between two cities"
    /// - "Calculate the total cost of a trip including flights and hotels"
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public override string Description => "Get current weather information for a city";
    /// </code>
    /// </example>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the JSON schema for the tool's parameters.
    /// </summary>
    /// <returns>
    /// A JSON schema object that describes the structure and validation rules for the tool's parameters.
    /// This schema is used by the LLM to understand what parameters the tool expects and how to provide them.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The schema is automatically generated from the <typeparamref name="TParams"/> type and includes:
    /// - Property types and formats
    /// - Required vs optional fields
    /// - Validation constraints (min/max values, patterns, etc.)
    /// - Field descriptions and examples from <see cref="ToolFieldAttribute"/>
    /// </para>
    /// <para>
    /// This method is called by the framework to provide the LLM with information about
    /// the tool's parameter requirements, enabling proper function calling.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Example generated schema:</para>
    /// <code>
    /// {
    ///   "type": "object",
    ///   "properties": {
    ///     "city": {
    ///       "type": "string",
    ///       "description": "City name",
    ///       "example": "New York"
    ///     },
    ///     "unit": {
    ///       "type": "string",
    ///       "description": "Temperature unit",
    ///       "example": "Celsius"
    ///     }
    ///   },
    ///   "required": ["city"]
    /// }
    /// </code>
    /// </example>
    public object GetJsonSchema()
    {
        return SchemaGenerator.Generate<TParams>();
    }

    /// <summary>
    /// Gets the unique name of the tool.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies this tool. The name should be:
    /// - Descriptive and human-readable
    /// - Lowercase with underscores (snake_case)
    /// - Unique within the set of tools available to an agent
    /// - Action-oriented (e.g., "get_weather", "search_flights")
    /// </value>
    /// <remarks>
    /// <para>
    /// The tool name is used by the LLM to identify and call the tool. It should be
    /// descriptive enough that the LLM can understand what the tool does from the name alone.
    /// </para>
    /// <para>
    /// Good naming conventions:
    /// - Use verb_noun format: "get_weather", "search_flights", "calculate_cost"
    /// - Be specific but concise: "search_hotels" not "search_accommodation_options"
    /// - Use lowercase with underscores for consistency
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public override string Name => "get_weather";
    /// </code>
    /// </example>
    public abstract string Name { get; }

    /// <summary>
    /// Invokes the tool with raw parameters, performing validation and type conversion.
    /// This method handles parameter validation, deserialization, and delegates to the typed implementation.
    /// </summary>
    /// <param name="parameters">
    /// The raw parameters as a dictionary. Keys should match the property names of
    /// <typeparamref name="TParams"/>, and values should be of appropriate types.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the tool execution.
    /// </param>
    /// <returns>
    /// The result of the tool execution, which will be serialized to JSON for the LLM.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parameters"/> is null.
    /// </exception>
    /// <exception cref="ToolValidationException">
    /// Thrown when parameter validation fails, including missing required fields,
    /// invalid data types, or validation attribute violations.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when parameters cannot be deserialized to the expected type.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements the <see cref="ITool.InvokeAsync"/> interface and provides
    /// the following functionality:
    /// </para>
    /// <list type="number">
    /// <item><description>Validates that all required parameters are present</description></item>
    /// <item><description>Deserializes the parameters to the strongly-typed <typeparamref name="TParams"/></description></item>
    /// <item><description>Validates the parameters using DataAnnotations attributes</description></item>
    /// <item><description>Calls the abstract <see cref="InvokeTypedAsync(TParams, CancellationToken)"/> method</description></item>
    /// <item><description>Returns the result for LLM consumption</description></item>
    /// </list>
    /// <para>
    /// Tool implementations should override <see cref="InvokeTypedAsync(TParams, CancellationToken)"/>
    /// instead of this method to get the benefits of strongly-typed parameters.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Invokes the tool with the provided parameters, handling validation, deserialization, and execution.
    /// </summary>
    /// <param name="parameters">Dictionary of parameters to pass to the tool.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// The result of the tool execution, or null if the operation was cancelled.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides the complete tool invocation lifecycle:
    /// </para>
    /// <list type="number">
    /// <item><description>Validates required parameters are present</description></item>
    /// <item><description>Deserializes parameters to strongly-typed objects</description></item>
    /// <item><description>Applies DataAnnotations validation</description></item>
    /// <item><description>Calls the abstract <see cref="InvokeTypedAsync(TParams, CancellationToken)"/> method</description></item>
    /// <item><description>Returns the result for LLM consumption</description></item>
    /// </list>
    /// <para>
    /// Tool implementations should override <see cref="InvokeTypedAsync(TParams, CancellationToken)"/>
    /// instead of this method to get the benefits of strongly-typed parameters.
    /// </para>
    /// </remarks>
    /// <exception cref="ToolValidationException">Thrown when parameter validation fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="ct"/>.</exception>
    public async Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
    {
        try
        {
            // Check for missing required fields first
            var missing = GetMissingRequiredFields<TParams>(parameters);

            if (missing.Count > 0)
            {
                throw new ToolValidationException("Invalid parameters payload.", missing.ToList());
            }

            var json = JsonSerializer.Serialize(parameters, JsonUtil.JsonOptions);
            var typedParams = JsonSerializer.Deserialize<TParams>(json, JsonUtil.JsonOptions);

            if (typedParams == null)
            {
                throw new ToolValidationException("Invalid parameters payload.", missing.ToList());
            }

            // Validate DataAnnotations attributes
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(typedParams);

            if (!Validator.TryValidateObject(typedParams, validationContext, validationResults, true))
            {
                var fieldErrors = validationResults
                    .SelectMany(vr => vr.MemberNames.Select(memberName =>
                        new ToolValidationError(JsonNamingPolicy.CamelCase.ConvertName(memberName), vr.ErrorMessage ?? "Validation failed")))
                    .ToList();

                throw new ToolValidationException("Parameter validation failed.", fieldErrors: fieldErrors);
            }

            var result = await InvokeTypedAsync(typedParams, ct);
            return result;
        }
        catch (JsonException ex)
        {
            //Trace.WriteLine(JsonSerializer.Serialize(parameters, JsonUtil.JsonOptions));
            //Trace.WriteLine(ex);
            //Console.WriteLine(JsonSerializer.Serialize(parameters, JsonUtil.JsonOptions));
            //Console.WriteLine(ex);
            var missing = GetMissingRequiredFields<TParams>(parameters);
            throw new ToolValidationException($"Failed to deserialize parameters: {ex.Message}", missing.ToList());
        }
        catch(Exception ex)
        {
            //Trace.WriteLine(JsonSerializer.Serialize(parameters, JsonUtil.JsonOptions));
            //Trace.WriteLine(ex);
            //Console.WriteLine(JsonSerializer.Serialize(parameters, JsonUtil.JsonOptions));
            //Console.WriteLine(ex);
            throw ;
        }
    }

    /// <summary>
    /// Gets a concise description of the tool and its parameters.
    /// </summary>
    /// <returns>
    /// A JSON string describing the tool and its parameters. This description is used
    /// by the LLM to understand the tool's capabilities and parameter requirements.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The description includes:
    /// - Tool name and description
    /// - Parameter names and types
    /// - Required vs optional parameters
    /// - Parameter descriptions and examples
    /// </para>
    /// <para>
    /// This method is called by the framework to provide the LLM with information about
    /// available tools and their capabilities.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Example output:</para>
    /// <code>
    /// {
    ///   "name": "get_weather",
    ///   "description": "Get current weather information for a city",
    ///   "parameters": {
    ///     "city": {
    ///       "type": "string",
    ///       "description": "City name",
    ///       "example": "New York",
    ///       "required": true
    ///     },
    ///     "unit": {
    ///       "type": "string",
    ///       "description": "Temperature unit",
    ///       "example": "Celsius",
    ///       "required": false
    ///     }
    ///   }
    /// }
    /// </code>
    /// </example>
    public string Describe()
    {
        return ToolDescriptionGenerator.Build<TParams>(Name, Description);
    }

    /// <summary>
    /// Invokes the tool with strongly-typed parameters.
    /// </summary>
    /// <param name="parameters">
    /// The strongly-typed parameters for the tool. This object has been validated
    /// and contains all required fields with appropriate types.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the tool execution.
    /// </param>
    /// <returns>
    /// The result of the tool execution. This should be a serializable object that
    /// provides useful information to the agent for further reasoning.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the main method that tool implementations should override. It provides:
    /// - Strongly-typed parameters that have been validated
    /// - Type safety for both input and output
    /// - Simplified error handling
    /// - Better IntelliSense and compile-time checking
    /// </para>
    /// <para>
    /// Tool implementations should:
    /// - Check the cancellation token early in the method
    /// - Handle exceptions gracefully and provide meaningful error messages
    /// - Return structured data that is useful for the agent's decision-making
    /// - Keep the method focused on the core tool functionality
    /// </para>
    /// <para>
    /// The returned object will be serialized to JSON and provided to the LLM as
    /// part of the agent's reasoning context. Therefore, the result should be
    /// structured and contain relevant information for the agent's next steps.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Example implementation:</para>
    /// <code>
    /// protected override async Task&lt;object&gt; InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
    /// {
    ///     ct.ThrowIfCancellationRequested();
    ///     
    ///     try
    ///     {
    ///         var weather = await FetchWeatherAsync(parameters.City, parameters.Unit, ct);
    ///         
    ///         return new { 
    ///             city = parameters.City,
    ///             temperature = weather.Temperature,
    ///             unit = parameters.Unit,
    ///             description = weather.Description,
    ///             humidity = weather.Humidity,
    ///             timestamp = DateTimeOffset.UtcNow
    ///         };
    ///     }
    ///     catch (HttpRequestException ex)
    ///     {
    ///         throw new ToolExecutionException($"Failed to fetch weather for {parameters.City}: {ex.Message}");
    ///     }
    /// }
    /// </code>
    /// </example>
    protected abstract Task<TResult> InvokeTypedAsync(TParams parameters, CancellationToken ct = default);

    /// <summary>
    ///     Gets an empty list of missing required fields (template method).
    /// </summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <returns>An empty list.</returns>
    protected static IReadOnlyList<string> GetMissingRequiredFields<T>()
    {
        return Array.Empty<string>();
    }

    /// <summary>
    ///     Gets the list of missing required fields for the given parameters.
    /// </summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <param name="parameters">The parameters to check.</param>
    /// <returns>A list of missing required field names.</returns>
    protected static IReadOnlyList<string> GetMissingRequiredFields<T>(Dictionary<string, object?> parameters)
    {
        return RequiredFieldHelper.GetMissingRequiredFields<T>(parameters);
    }
}