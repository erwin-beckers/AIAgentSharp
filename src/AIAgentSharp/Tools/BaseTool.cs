using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AIAgentSharp;

/// <summary>
///     Base class for strongly-typed tools that provides automatic parameter validation and schema generation.
///     This class handles parameter deserialization, validation, and provides default implementations
///     for tool introspection and function schema generation.
/// </summary>
/// <typeparam name="TParams">The type of the tool's parameters.</typeparam>
/// <typeparam name="TResult">The type of the tool's result.</typeparam>
public abstract class BaseTool<TParams, TResult> : ITool, IToolIntrospect, IFunctionSchemaProvider
{
    /// <summary>
    ///     Gets the description of the tool.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    ///     Gets the JSON schema for the tool's parameters.
    /// </summary>
    /// <returns>A JSON schema object for the tool's parameters.</returns>
    public object GetJsonSchema()
    {
        return SchemaGenerator.Generate<TParams>();
    }

    /// <summary>
    ///     Gets the unique name of the tool.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    ///     Invokes the tool with raw parameters, performing validation and type conversion.
    ///     This method handles parameter validation, deserialization, and delegates to the typed implementation.
    /// </summary>
    /// <param name="parameters">The raw parameters as a dictionary.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the tool execution.</returns>
    /// <exception cref="ToolValidationException">Thrown when parameter validation fails.</exception>
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
            var missing = GetMissingRequiredFields<TParams>(parameters);
            throw new ToolValidationException($"Failed to deserialize parameters: {ex.Message}", missing.ToList());
        }
    }

    /// <summary>
    ///     Gets a concise description of the tool and its parameters.
    /// </summary>
    /// <returns>A JSON string describing the tool and its parameters.</returns>
    public string Describe()
    {
        return ToolDescriptionGenerator.Build<TParams>(Name, Description);
    }

    /// <summary>
    ///     Invokes the tool with strongly-typed parameters.
    /// </summary>
    /// <param name="parameters">The strongly-typed parameters for the tool.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the tool execution.</returns>
    public abstract Task<TResult> InvokeTypedAsync(TParams parameters, CancellationToken ct = default);

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