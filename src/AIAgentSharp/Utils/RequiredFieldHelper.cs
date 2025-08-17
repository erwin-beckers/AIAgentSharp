using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace AIAgentSharp;

/// <summary>
///     Provides utilities for detecting required fields in tool parameters using DataAnnotations and nullability
///     information.
///     This class unifies the detection of required fields across schema generation and runtime validation.
/// </summary>
public static class RequiredFieldHelper
{
    /// <summary>
    ///     Thread-local nullability context for proper handling of nullable reference types.
    /// </summary>
    private static readonly ThreadLocal<NullabilityInfoContext> _nullabilityContext = new(() => new NullabilityInfoContext());

    /// <summary>
    ///     Determines if a property is required based on attributes and nullability information.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns>True if the property is required, false otherwise.</returns>
    public static bool IsPropertyRequired(PropertyInfo property)
    {
        // Check ToolField attribute first (explicit override)
        var toolFieldAttr = property.GetCustomAttribute<ToolFieldAttribute>();

        if (toolFieldAttr != null && toolFieldAttr.Required)
        {
            return true;
        }

        // Check Required attribute
        if (property.GetCustomAttribute<RequiredAttribute>() != null)
        {
            return true;
        }

        // Check nullability for value types
        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);

        if (underlyingType != null)
        {
            return false; // Nullable value type is not required
        }

        // For reference types, use NullabilityInfoContext for proper NRT handling
        if (property.PropertyType.IsClass)
        {
            var nullabilityInfo = _nullabilityContext.Value!.Create(property);
            return nullabilityInfo.WriteState == NullabilityState.NotNull;
        }

        return true; // Value types are required by default
    }

    /// <summary>
    ///     Gets the list of missing required fields for a given parameter dictionary.
    /// </summary>
    /// <typeparam name="T">The parameter type to check against.</typeparam>
    /// <param name="parameters">The parameters to validate.</param>
    /// <returns>A list of missing required field names.</returns>
    public static IReadOnlyList<string> GetMissingRequiredFields<T>(Dictionary<string, object?> parameters)
    {
        var missing = new List<string>();
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (IsPropertyRequired(prop))
            {
                var propertyName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);

                if (!parameters.ContainsKey(propertyName) ||
                    parameters[propertyName] == null ||
                    (parameters[propertyName] is string str && string.IsNullOrWhiteSpace(str)))
                {
                    missing.Add(propertyName);
                }
            }
        }

        return missing;
    }
}