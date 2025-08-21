using System.Reflection;
using System.Text.Json;
using AIAgentSharp.Schema;

namespace AIAgentSharp;

/// <summary>
/// Generates JSON schemas for C# types, supporting DataAnnotations and custom attributes.
/// This class is used to create OpenAI-compatible function schemas for tools.
/// </summary>
public static class SchemaGenerator
{
    private static readonly TypeSchemaGenerator _typeSchemaGenerator;
    private static readonly AttributeProcessor _attributeProcessor;
    private static readonly PropertySchemaGenerator _propertySchemaGenerator;

    static SchemaGenerator()
    {
        _typeSchemaGenerator = new TypeSchemaGenerator();
        _attributeProcessor = new AttributeProcessor();
        _propertySchemaGenerator = new PropertySchemaGenerator(_typeSchemaGenerator, _attributeProcessor);
    }

    /// <summary>
    /// Generates a JSON schema for the specified generic type.
    /// </summary>
    /// <typeparam name="T">The type to generate a schema for.</typeparam>
    /// <returns>A JSON schema object.</returns>
    public static object Generate<T>()
    {
        return Generate(typeof(T));
    }

    /// <summary>
    /// Generates a JSON schema for the specified type.
    /// </summary>
    /// <param name="type">The type to generate a schema for.</param>
    /// <returns>A JSON schema object.</returns>
    public static object Generate(Type type)
    {
        var visited = new HashSet<Type>();
        return GenerateSchema(type, visited);
    }

    public static Dictionary<string, object> GenerateSchema(Type type, HashSet<Type> visited)
    {
        // Check for custom schema override first
        var customSchemaAttr = type.GetCustomAttribute<Schema.ToolSchemaAttribute>();
        if (customSchemaAttr != null)
        {
            try
            {
                var customSchema = JsonSerializer.Deserialize<Dictionary<string, object>>(customSchemaAttr.Schema, JsonUtil.JsonOptions);
                if (customSchema != null)
                {
                    // Add additional rules if provided
                    if (!string.IsNullOrEmpty(customSchemaAttr.AdditionalRules))
                    {
                        if (!customSchema.ContainsKey("description"))
                        {
                            customSchema["description"] = customSchemaAttr.AdditionalRules;
                        }
                        else
                        {
                            // Append additional rules to existing description
                            var existingDesc = customSchema["description"].ToString();
                            customSchema["description"] = $"{existingDesc}\n\n{customSchemaAttr.AdditionalRules}";
                        }
                    }
                    return customSchema;
                }
            }
            catch (JsonException ex)
            {
                // Log error and fall back to auto-generation
                System.Diagnostics.Debug.WriteLine($"Failed to parse custom schema for type {type.Name}: {ex.Message}");
            }
        }

        // Handle circular references
        if (visited.Contains(type))
        {
            return new Dictionary<string, object> { 
                ["type"] = "object",
                ["description"] = "Circular reference detected"
            };
        }

        // Handle nullable value types (e.g., int?, DateTime?)
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return GenerateNullableValueTypeSchema(underlyingType, visited);
        }

        // Handle nullable reference types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var genericArg = type.GetGenericArguments()[0];
            return GenerateNullableValueTypeSchema(genericArg, visited);
        }

        // Handle primitive types
        if (IsPrimitiveType(type))
        {
            return GeneratePrimitiveTypeSchema(type);
        }

        // Handle enums
        if (type.IsEnum)
        {
            return GenerateEnumSchema(type);
        }

        // Handle collections
        if (IsCollectionType(type))
        {
            return GenerateCollectionSchema(type, visited);
        }

        // Handle dictionaries
        if (IsDictionaryType(type))
        {
            return GenerateDictionarySchema(type, visited);
        }

        // Handle complex objects
        if (type.IsClass || type.IsValueType)
        {
            return GenerateObjectSchema(type, visited);
        }

        // Fallback
        return new Dictionary<string, object> { ["type"] = "object" };
    }

    public static Dictionary<string, object> GenerateNullableValueTypeSchema(Type underlyingType, HashSet<Type> visited)
    {
        var baseSchema = GenerateSchema(underlyingType, visited);

        if (baseSchema is IDictionary<string, object> baseDict)
        {
            var baseType = baseDict["type"];
            var newDict = new Dictionary<string, object>(baseDict);
            newDict["type"] = new[] { baseType, "null" };
            return newDict;
        }
        else
        {
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

    public static Dictionary<string, object> GeneratePrimitiveTypeSchema(Type type)
    {
        if (type == typeof(string))
        {
            return new Dictionary<string, object> { ["type"] = new[] { "string", "null" } };
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

        return new Dictionary<string, object> { ["type"] = "object" };
    }

    public static Dictionary<string, object> GenerateEnumSchema(Type type)
    {
        var enumValues = Enum.GetValues(type).Cast<object>().Select(v => v.ToString()).ToArray();
        return new Dictionary<string, object> { ["type"] = "string", ["enum"] = enumValues };
    }

    public static Dictionary<string, object> GenerateCollectionSchema(Type type, HashSet<Type> visited)
    {
        Type elementType;

        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
        }
        else
        {
            elementType = type.GetGenericArguments()[0];
        }

        return new Dictionary<string, object> { ["type"] = "array", ["items"] = GenerateSchema(elementType, visited) };
    }

    public static Dictionary<string, object> GenerateDictionarySchema(Type type, HashSet<Type> visited)
    {
        var keyType = type.GetGenericArguments()[0];
        var valueType = type.GetGenericArguments()[1];

        if (keyType == typeof(string))
        {
            return new Dictionary<string, object> { ["type"] = "object", ["additionalProperties"] = GenerateSchema(valueType, visited) };
        }

        return new Dictionary<string, object> { ["type"] = "object" };
    }

    public static Dictionary<string, object> GenerateObjectSchema(Type type, HashSet<Type> visited)
    {
        // Add to visited set before processing to prevent circular references
        visited.Add(type);
        
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .OrderBy(p => p.Name)
            .ToList();

        var schemaProperties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var prop in properties)
        {
            var propertyName = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
            var propertySchema = _propertySchemaGenerator.GeneratePropertySchema(prop, visited);

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

    public static bool IsPropertyRequired(PropertyInfo property)
    {
        return RequiredFieldHelper.IsPropertyRequired(property);
    }

    public static bool IsPrimitiveType(Type type)
    {
        return type == typeof(string) || type == typeof(int) || type == typeof(long) ||
               type == typeof(double) || type == typeof(float) || type == typeof(decimal) ||
               type == typeof(bool) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
               type == typeof(Guid) || type == typeof(Uri);
    }

    public static bool IsCollectionType(Type type)
    {
        return type.IsArray || 
               (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || 
                                      type.GetGenericTypeDefinition() == typeof(IList<>) ||
                                      type.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    public static bool IsDictionaryType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

    public static string GetBaseTypeFromSchema(object schema)
    {
        if (schema is IDictionary<string, object> dict && dict.ContainsKey("type"))
        {
            return dict["type"].ToString()!;
        }

        var type = schema.GetType();
        var typeProperty = type.GetProperty("type");

        if (typeProperty != null)
        {
            return typeProperty.GetValue(schema)?.ToString() ?? "string";
        }

        return "string";
    }
}