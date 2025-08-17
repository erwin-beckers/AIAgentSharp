using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace AIAgentSharp;

/// <summary>
///     Generates JSON schemas for C# types, supporting DataAnnotations and custom attributes.
///     This class is used to create OpenAI-compatible function schemas for tools.
/// </summary>
public static class SchemaGenerator
{
    /// <summary>
    ///     Generates a JSON schema for the specified generic type.
    /// </summary>
    /// <typeparam name="T">The type to generate a schema for.</typeparam>
    /// <returns>A JSON schema object.</returns>
    public static object Generate<T>()
    {
        return Generate(typeof(T));
    }

    /// <summary>
    ///     Generates a JSON schema for the specified type.
    /// </summary>
    /// <param name="type">The type to generate a schema for.</param>
    /// <returns>A JSON schema object.</returns>
    public static object Generate(Type type)
    {
        var visited = new HashSet<Type>();
        return GenerateSchema(type, visited);
    }

    private static object GenerateSchema(Type type, HashSet<Type> visited)
    {
        if (visited.Contains(type))
        {
            // Prevent infinite recursion for circular references
            return new { type = "object", description = "Circular reference detected" };
        }

        visited.Add(type);

        try
        {
            // Handle nullable types first - use union type for better expressiveness
            var underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null)
            {
                // Nullable value type (e.g., int?, DateTime?)
                var baseSchema = GenerateSchema(underlyingType, visited);

                // For nullable types, create a union of the base type and null
                if (baseSchema is IDictionary<string, object> baseDict)
                {
                    var baseType = baseDict["type"];
                    var newDict = new Dictionary<string, object>(baseDict);
                    newDict["type"] = new[] { baseType, "null" };
                    return newDict;
                }
                else
                {
                    // For anonymous objects, create a new dictionary with union type
                    var baseType = GetBaseTypeFromSchema(baseSchema);
                    var result = new Dictionary<string, object> { ["type"] = new[] { baseType, "null" } };
                    
                    // Preserve format property for nullable types
                    if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
                    {
                        result["format"] = "date-time";
                    }
                    else if (underlyingType == typeof(Guid))
                    {
                        result["format"] = "uuid";
                    }
                    else if (underlyingType == typeof(Uri))
                    {
                        result["format"] = "uri";
                    }
                    
                    return result;
                }
            }

            // Handle base types
            if (type == typeof(string))
            {
                return new Dictionary<string, object> { ["type"] = new[] { "string", "null" } }; // Assume string is nullable in tool context
            }

            if (type == typeof(int) || type == typeof(long))
            {
                return new Dictionary<string, object> { ["type"] = "integer" };
            }

            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                return new Dictionary<string, object> { ["type"] = "number" };
            }

            if (type == typeof(bool))
            {
                return new Dictionary<string, object> { ["type"] = "boolean" };
            }

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return new Dictionary<string, object> { ["type"] = "string", ["format"] = "date-time" };
            }

            if (type == typeof(Guid))
            {
                return new Dictionary<string, object> { ["type"] = "string", ["format"] = "uuid" };
            }

            if (type == typeof(Uri))
            {
                return new Dictionary<string, object> { ["type"] = new[] { "string", "null" }, ["format"] = "uri" };
            }

            // Handle enums
            if (type.IsEnum)
            {
                var enumValues = Enum.GetValues(type).Cast<object>().Select(v => v.ToString()).ToArray();
                return new Dictionary<string, object> { ["type"] = "string", ["enum"] = enumValues };
            }

            // Handle arrays and lists
            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                return new Dictionary<string, object> { ["type"] = "array", ["items"] = GenerateSchema(elementType, visited) };
            }

            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(IList<>) ||
                                       type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var elementType = type.GetGenericArguments()[0];
                return new Dictionary<string, object> { ["type"] = "array", ["items"] = GenerateSchema(elementType, visited) };
            }

            // Handle dictionaries
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = type.GetGenericArguments()[0];
                var valueType = type.GetGenericArguments()[1];

                if (keyType == typeof(string))
                {
                    return new Dictionary<string, object> { ["type"] = "object", ["additionalProperties"] = GenerateSchema(valueType, visited) };
                }
            }

            // Handle objects
            if (type.IsClass || type.IsValueType)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite)
                    .OrderBy(p => p.Name)
                    .ToList();

                var schemaProperties = new Dictionary<string, object>();
                var required = new List<string>();

                foreach (var prop in properties)
                {
                    var propertyName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                    var propertySchema = GeneratePropertySchema(prop, visited);

                    if (propertySchema != null)
                    {
                        schemaProperties[propertyName] = propertySchema;

                        // Check if property is required
                        if (IsPropertyRequired(prop))
                        {
                            required.Add(propertyName);
                        }
                    }
                }

                var result = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = schemaProperties,
                    ["additionalProperties"] = false
                };

                if (required.Count > 0)
                {
                    result["required"] = required.ToArray();
                }

                return result;
            }

            // Fallback
            return new Dictionary<string, object> { ["type"] = "object" };
        }
        finally
        {
            visited.Remove(type);
        }
    }

    private static object? GeneratePropertySchema(PropertyInfo property, HashSet<Type> visited)
    {
        var baseSchema = GenerateSchema(property.PropertyType, visited);
        var schemaDict = new Dictionary<string, object>();

        // Convert base schema to dictionary
        if (baseSchema is Dictionary<string, object> dict)
        {
            schemaDict = new Dictionary<string, object>(dict);
        }
        else
        {
            // For simple types, we need to extract properties
            var json = JsonSerializer.Serialize(baseSchema, JsonUtil.JsonOptions);
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            foreach (var prop in element.EnumerateObject())
            {
                schemaDict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Array => prop.Value.EnumerateArray().Select(v => v.GetString()).ToArray(),
                    JsonValueKind.Object => JsonSerializer.Deserialize<object>(prop.Value.GetRawText(), JsonUtil.JsonOptions)!,
                    _ => prop.Value.GetString() ?? prop.Value.ToString()
                };
            }
        }

        // Add ToolField attributes
        var toolFieldAttr = property.GetCustomAttribute<ToolFieldAttribute>();

        if (toolFieldAttr != null)
        {
            if (!string.IsNullOrEmpty(toolFieldAttr.Description))
            {
                schemaDict["description"] = toolFieldAttr.Description;
            }

            if (toolFieldAttr.Example != null)
            {
                schemaDict["example"] = toolFieldAttr.Example;
            }

            if (toolFieldAttr.MinLength >= 0)
            {
                schemaDict["minLength"] = toolFieldAttr.MinLength;
            }

            if (toolFieldAttr.MaxLength >= 0)
            {
                schemaDict["maxLength"] = toolFieldAttr.MaxLength;
            }

            if (!double.IsNaN(toolFieldAttr.Minimum))
            {
                schemaDict["minimum"] = toolFieldAttr.Minimum;
            }

            if (!double.IsNaN(toolFieldAttr.Maximum))
            {
                schemaDict["maximum"] = toolFieldAttr.Maximum;
            }

            if (!string.IsNullOrEmpty(toolFieldAttr.Pattern))
            {
                schemaDict["pattern"] = toolFieldAttr.Pattern;
            }

            if (!string.IsNullOrEmpty(toolFieldAttr.Format))
            {
                schemaDict["format"] = toolFieldAttr.Format;
            }
        }

        // Add DataAnnotations attributes
        var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();

        if (requiredAttr != null && !string.IsNullOrEmpty(requiredAttr.ErrorMessage))
        {
            schemaDict["description"] = requiredAttr.ErrorMessage;
        }

        var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();

        if (stringLengthAttr != null)
        {
            if (stringLengthAttr.MaximumLength > 0)
            {
                schemaDict["maxLength"] = stringLengthAttr.MaximumLength;
            }

            if (stringLengthAttr.MinimumLength > 0)
            {
                schemaDict["minLength"] = stringLengthAttr.MinimumLength;
            }
        }

        var minLengthAttr = property.GetCustomAttribute<MinLengthAttribute>();

        if (minLengthAttr != null)
        {
            schemaDict["minLength"] = minLengthAttr.Length;
        }

        var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();

        if (maxLengthAttr != null)
        {
            schemaDict["maxLength"] = maxLengthAttr.Length;
        }

        var rangeAttr = property.GetCustomAttribute<RangeAttribute>();

        if (rangeAttr != null)
        {
            if (double.TryParse(rangeAttr.Minimum.ToString(), out var min))
            {
                schemaDict["minimum"] = min;
            }

            if (double.TryParse(rangeAttr.Maximum.ToString(), out var max))
            {
                schemaDict["maximum"] = max;
            }
        }

        var regexAttr = property.GetCustomAttribute<RegularExpressionAttribute>();

        if (regexAttr != null)
        {
            schemaDict["pattern"] = regexAttr.Pattern;
        }

        var emailAttr = property.GetCustomAttribute<EmailAddressAttribute>();

        if (emailAttr != null)
        {
            schemaDict["format"] = "email";
        }

        var urlAttr = property.GetCustomAttribute<UrlAttribute>();

        if (urlAttr != null)
        {
            schemaDict["format"] = "uri";
        }

        var phoneAttr = property.GetCustomAttribute<PhoneAttribute>();

        if (phoneAttr != null)
        {
            schemaDict["format"] = "phone";
        }

        return schemaDict;
    }

    private static string GetBaseTypeFromSchema(object schema)
    {
        // Extract the base type from various schema formats
        if (schema is IDictionary<string, object> dict && dict.ContainsKey("type"))
        {
            return dict["type"].ToString()!;
        }

        // For anonymous objects, try to get the type property via reflection
        var type = schema.GetType();
        var typeProperty = type.GetProperty("type");

        if (typeProperty != null)
        {
            return typeProperty.GetValue(schema)?.ToString() ?? "string";
        }

        // Fallback
        return "string";
    }

    public static bool IsPropertyRequired(PropertyInfo property)
    {
        return RequiredFieldHelper.IsPropertyRequired(property);
    }
}