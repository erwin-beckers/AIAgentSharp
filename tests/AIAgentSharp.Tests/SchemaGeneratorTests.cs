using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

// Added for .Select()

// Added for BindingFlags

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class SchemaGeneratorTests
{
    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    [TestMethod]
    public void Generate_SimpleTypes_ProducesCorrectSchema()
    {
        // Act
        var stringSchema = SchemaGenerator.Generate<string>();
        var intSchema = SchemaGenerator.Generate<int>();
        var doubleSchema = SchemaGenerator.Generate<double>();
        var boolSchema = SchemaGenerator.Generate<bool>();
        var dateTimeSchema = SchemaGenerator.Generate<DateTime>();
        var guidSchema = SchemaGenerator.Generate<Guid>();

        // Assert
        Assert.AreEqual("string", GetSchemaType(stringSchema));
        Assert.AreEqual("integer", GetSchemaType(intSchema));
        Assert.AreEqual("number", GetSchemaType(doubleSchema));
        Assert.AreEqual("boolean", GetSchemaType(boolSchema));
        Assert.AreEqual("string", GetSchemaType(dateTimeSchema));
        Assert.AreEqual("date-time", GetSchemaFormat(dateTimeSchema));
        Assert.AreEqual("string", GetSchemaType(guidSchema));
        Assert.AreEqual("uuid", GetSchemaFormat(guidSchema));
    }

    [TestMethod]
    public void Generate_NullableTypes_ProducesCorrectSchema()
    {
        // Act
        var nullableStringSchema = SchemaGenerator.Generate<string?>();
        var nullableIntSchema = SchemaGenerator.Generate<int?>();
        var nullableDoubleSchema = SchemaGenerator.Generate<double?>();

        // Assert
        Assert.AreEqual("string", GetSchemaType(nullableStringSchema));
        Assert.AreEqual("integer", GetSchemaType(nullableIntSchema));
        Assert.AreEqual("number", GetSchemaType(nullableDoubleSchema));
    }

    [TestMethod]
    public void Generate_Enum_ProducesCorrectSchema()
    {
        // Act
        var schema = SchemaGenerator.Generate<TestEnum>();

        // Assert
        Assert.AreEqual("string", GetSchemaType(schema));
        var enumValues = GetSchemaEnum(schema);
        CollectionAssert.AreEqual(new[] { "Value1", "Value2", "Value3" }, enumValues);
    }

    [TestMethod]
    public void Generate_Arrays_ProducesCorrectSchema()
    {
        // Act
        var stringArraySchema = SchemaGenerator.Generate<string[]>();
        var intListSchema = SchemaGenerator.Generate<List<int>>();
        var doubleEnumerableSchema = SchemaGenerator.Generate<IEnumerable<double>>();

        // Assert
        Assert.AreEqual("array", GetSchemaType(stringArraySchema));
        Assert.AreEqual("string", GetSchemaType(GetSchemaItems(stringArraySchema)));

        Assert.AreEqual("array", GetSchemaType(intListSchema));
        Assert.AreEqual("integer", GetSchemaType(GetSchemaItems(intListSchema)));

        Assert.AreEqual("array", GetSchemaType(doubleEnumerableSchema));
        Assert.AreEqual("number", GetSchemaType(GetSchemaItems(doubleEnumerableSchema)));
    }

    [TestMethod]
    public void Generate_Dictionary_ProducesCorrectSchema()
    {
        // Act
        var dictSchema = SchemaGenerator.Generate<Dictionary<string, int>>();

        // Assert
        Assert.AreEqual("object", GetSchemaType(dictSchema));
        var additionalProps = GetSchemaAdditionalProperties(dictSchema);
        Assert.AreEqual("integer", GetSchemaType(additionalProps));
    }

    [TestMethod]
    public void Generate_ComplexObject_ProducesCorrectSchema()
    {
        // Arrange
        var testParams = new TestParams();

        // Act
        var schema = SchemaGenerator.Generate<TestParams>();

        // Assert
        Assert.AreEqual("object", GetSchemaType(schema));

        var properties = GetSchemaProperties(schema);
        Assert.IsTrue(properties.ContainsKey("requiredString"));
        Assert.IsTrue(properties.ContainsKey("optionalString"));
        Assert.IsTrue(properties.ContainsKey("requiredInt"));
        Assert.IsTrue(properties.ContainsKey("optionalInt"));
        Assert.IsTrue(properties.ContainsKey("enumValue"));
        Assert.IsTrue(properties.ContainsKey("arrayValue"));

        var required = GetSchemaRequired(schema);

        // Debug output
        Console.WriteLine($"Required properties: {string.Join(", ", required)}");

        CollectionAssert.Contains(required, "requiredInt");
        CollectionAssert.Contains(required, "enumValue");
        CollectionAssert.Contains(required, "requiredString"); // Non-nullable reference types are required
        CollectionAssert.DoesNotContain(required, "optionalString");
        CollectionAssert.DoesNotContain(required, "optionalInt");
    }

    [TestMethod]
    public void Generate_WithToolFieldAttributes_IncludesAttributes()
    {
        // Act
        var schema = SchemaGenerator.Generate<TestParamsWithAttributes>();

        // Assert
        var properties = GetSchemaProperties(schema);

        var stringProp = properties["testString"];
        Assert.AreEqual("Test description", GetSchemaDescription(stringProp));
        Assert.AreEqual("example", GetSchemaExample(stringProp));
        Assert.AreEqual(5, GetSchemaMinLength(stringProp));
        Assert.AreEqual(100, GetSchemaMaxLength(stringProp));
        Assert.AreEqual("^[a-zA-Z]+$", GetSchemaPattern(stringProp));
        Assert.AreEqual("email", GetSchemaFormat(stringProp));
    }

    [TestMethod]
    public void Generate_WithDataAnnotations_IncludesAnnotations()
    {
        // Act
        var schema = SchemaGenerator.Generate<TestParamsWithDataAnnotations>();

        // Assert
        var properties = GetSchemaProperties(schema);

        var stringProp = properties["testString"];
        Assert.AreEqual("Required field", GetSchemaDescription(stringProp));
        Assert.AreEqual(10, GetSchemaMaxLength(stringProp));
        Assert.AreEqual(2, GetSchemaMinLength(stringProp));
        Assert.AreEqual("^[a-z]+$", GetSchemaPattern(stringProp));
        Assert.AreEqual("email", GetSchemaFormat(stringProp));
    }

    [TestMethod]
    public void Generate_DeterministicOrder_ProducesConsistentOutput()
    {
        // Act
        var schema1 = SchemaGenerator.Generate<TestParams>();
        var schema2 = SchemaGenerator.Generate<TestParams>();

        // Assert
        var json1 = JsonSerializer.Serialize(schema1, JsonUtil.JsonOptions);
        var json2 = JsonSerializer.Serialize(schema2, JsonUtil.JsonOptions);

        Assert.AreEqual(json1, json2);
    }

    [TestMethod]
    public void Generate_NestedObjects_HandlesRecursion()
    {
        // Act
        var schema = SchemaGenerator.Generate<TestParamsWithNested>();

        // Assert
        Assert.AreEqual("object", GetSchemaType(schema));

        var properties = GetSchemaProperties(schema);
        Assert.IsTrue(properties.ContainsKey("nested"));

        var nestedSchema = properties["nested"];
        Assert.AreEqual("object", GetSchemaType(nestedSchema));

        var nestedProperties = GetSchemaProperties(nestedSchema);
        Assert.IsTrue(nestedProperties.ContainsKey("value"));
    }

    [TestMethod]
    public void Generate_CircularReference_HandlesGracefully()
    {
        // Act & Assert - Should not throw
        var schema = SchemaGenerator.Generate<TestParamsWithCircular>();

        Assert.AreEqual("object", GetSchemaType(schema));
        var properties = GetSchemaProperties(schema);
        Assert.IsTrue(properties.ContainsKey("self"));

        var selfSchema = properties["self"];
        Assert.AreEqual("object", GetSchemaType(selfSchema));
        Assert.AreEqual("Circular reference detected", GetSchemaDescription(selfSchema));
    }

    [TestMethod]
    public void Generate_AllPublicProperties_Included()
    {
        // Act
        var schema = SchemaGenerator.Generate<TestParamsWithAllTypes>();

        // Assert
        var properties = GetSchemaProperties(schema);

        // All public properties should be included
        Assert.IsTrue(properties.ContainsKey("stringValue"));
        Assert.IsTrue(properties.ContainsKey("intValue"));
        Assert.IsTrue(properties.ContainsKey("doubleValue"));
        Assert.IsTrue(properties.ContainsKey("boolValue"));
        Assert.IsTrue(properties.ContainsKey("enumValue"));
        Assert.IsTrue(properties.ContainsKey("arrayValue"));
        Assert.IsTrue(properties.ContainsKey("listValue"));
        Assert.IsTrue(properties.ContainsKey("dictValue"));
        Assert.IsTrue(properties.ContainsKey("nullableString"));
        Assert.IsTrue(properties.ContainsKey("nullableInt"));
    }

    [TestMethod]
    public void SchemaGenerator_NullableTypes_GenerateUnionSchemas()
    {
        var nullableStringSchema = SchemaGenerator.Generate<string?>();
        var nullableIntSchema = SchemaGenerator.Generate<int?>();
        var nullableDoubleSchema = SchemaGenerator.Generate<double?>();

        // Verify nullable types generate union schemas with null
        // Handle both anonymous objects and dictionaries
        var stringTypes = GetTypeArray(nullableStringSchema);
        Assert.AreEqual(2, stringTypes.Length);
        Assert.AreEqual("string", stringTypes[0]);
        Assert.AreEqual("null", stringTypes[1]);

        // Verify int nullable
        var intTypes = GetTypeArray(nullableIntSchema);
        Assert.AreEqual(2, intTypes.Length);
        Assert.AreEqual("integer", intTypes[0]);
        Assert.AreEqual("null", intTypes[1]);

        // Verify double nullable
        var doubleTypes = GetTypeArray(nullableDoubleSchema);
        Assert.AreEqual(2, doubleTypes.Length);
        Assert.AreEqual("number", doubleTypes[0]);
        Assert.AreEqual("null", doubleTypes[1]);
    }

    [TestMethod]
    public void Debug_PropertyRequiredLogic()
    {
        // Test the IsPropertyRequired logic directly
        var type = typeof(TestParams);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var isRequired = RequiredFieldHelper.IsPropertyRequired(prop);
            Console.WriteLine($"Property: {prop.Name}, Type: {prop.PropertyType}, IsRequired: {isRequired}");
        }
    }

    // Helper methods to extract values from schema objects
    private static object[] GetTypeArray(object schema)
    {
        if (schema is IDictionary<string, object> dict && dict.ContainsKey("type"))
        {
            return (object[])dict["type"];
        }

        // For anonymous objects, use reflection to get the type property
        var type = schema.GetType();
        var typeProperty = type.GetProperty("type");

        if (typeProperty != null)
        {
            return (object[])typeProperty.GetValue(schema)!;
        }

        throw new InvalidOperationException("Could not extract type array from schema");
    }

    private static string GetSchemaType(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var typeElement = element.GetProperty("type");

        // Handle both string and array types
        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString()!;
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            // For union types, return the first non-null type
            var types = typeElement.EnumerateArray().ToArray();

            foreach (var type in types)
            {
                if (type.ValueKind == JsonValueKind.String && type.GetString() != "null")
                {
                    return type.GetString()!;
                }
            }
            return "string"; // fallback
        }

        return "string"; // fallback
    }

    private static string? GetSchemaFormat(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return element.TryGetProperty("format", out var format) ? format.GetString() : null;
    }

    private static string[] GetSchemaEnum(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var enumElement = element.GetProperty("enum");
        return enumElement.EnumerateArray().Select(v => v.GetString()!).ToArray();
    }

    private static object GetSchemaItems(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var itemsElement = element.GetProperty("items");
        return JsonSerializer.Deserialize<object>(itemsElement.GetRawText(), JsonUtil.JsonOptions)!;
    }

    private static object GetSchemaAdditionalProperties(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var additionalPropsElement = element.GetProperty("additionalProperties");
        return JsonSerializer.Deserialize<object>(additionalPropsElement.GetRawText(), JsonUtil.JsonOptions)!;
    }

    private static Dictionary<string, object> GetSchemaProperties(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var propertiesElement = element.GetProperty("properties");
        var properties = new Dictionary<string, object>();

        foreach (var prop in propertiesElement.EnumerateObject())
        {
            properties[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText(), JsonUtil.JsonOptions)!;
        }

        return properties;
    }

    private static string[] GetSchemaRequired(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var requiredElement = element.GetProperty("required");
        return requiredElement.EnumerateArray().Select(v => v.GetString()!).ToArray();
    }

    private static string? GetSchemaDescription(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return element.TryGetProperty("description", out var desc) ? desc.GetString() : null;
    }

    private static object? GetSchemaExample(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);

        if (element.TryGetProperty("example", out var example))
        {
            return example.ValueKind switch
            {
                JsonValueKind.String => example.GetString(),
                JsonValueKind.Number => example.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => example.EnumerateArray().Select(v => v.GetString()).ToArray(),
                JsonValueKind.Object => JsonSerializer.Deserialize<object>(example.GetRawText(), JsonUtil.JsonOptions),
                _ => example.GetString() ?? example.ToString()
            };
        }
        return null;
    }

    private static int GetSchemaMinLength(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return element.GetProperty("minLength").GetInt32();
    }

    private static int GetSchemaMaxLength(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return element.GetProperty("maxLength").GetInt32();
    }

    private static string? GetSchemaPattern(object schema)
    {
        var json = JsonSerializer.Serialize(schema, JsonUtil.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return element.TryGetProperty("pattern", out var pattern) ? pattern.GetString() : null;
    }

    // Test data classes
    public class TestParams
    {
        public string RequiredString { get; set; } = string.Empty;
        public string? OptionalString { get; set; }
        public int RequiredInt { get; set; }
        public int? OptionalInt { get; set; }
        public TestEnum EnumValue { get; set; }
        public string[] ArrayValue { get; set; } = Array.Empty<string>();
    }

    public class TestParamsWithAttributes
    {
        [ToolField(Description = "Test description", Example = "example", MinLength = 5, MaxLength = 100, Pattern = "^[a-zA-Z]+$", Format = "email")]
        public string TestString { get; set; } = string.Empty;
    }

    public class TestParamsWithDataAnnotations
    {
        [Required(ErrorMessage = "Required field")]
        [StringLength(10, MinimumLength = 2)]
        [RegularExpression("^[a-z]+$")]
        [EmailAddress]
        public string TestString { get; set; } = string.Empty;
    }

    public class TestParamsWithNested
    {
        public NestedObject Nested { get; set; } = new();
    }

    public class NestedObject
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestParamsWithCircular
    {
        public TestParamsWithCircular Self { get; set; } = null!;
    }

    public class TestParamsWithAllTypes
    {
        public string StringValue { get; set; } = string.Empty;
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
        public TestEnum EnumValue { get; set; }
        public string[] ArrayValue { get; set; } = Array.Empty<string>();
        public List<int> ListValue { get; set; } = new();
        public Dictionary<string, object> DictValue { get; set; } = new();
        public string? NullableString { get; set; }
        public int? NullableInt { get; set; }
    }
}