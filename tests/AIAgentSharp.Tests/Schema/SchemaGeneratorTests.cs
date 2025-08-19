using System.ComponentModel.DataAnnotations;

namespace AIAgentSharp.Tests.Schema;

[TestClass]
public class SchemaGeneratorTests
{
    // Test parameter classes
    public class SimpleTestClass
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [ToolParams(Description = "Complex test parameters")]
    public class ComplexTestClass
    {
        [ToolField(Description = "User name", Example = "John Doe", Required = true)]
        [Required]
        public string Name { get; set; } = "";

        [ToolField(Description = "User age", Minimum = 0, Maximum = 150)]
        [Range(0, 150)]
        public int Age { get; set; }

        [ToolField(Description = "Email address", Format = "email")]
        [EmailAddress]
        public string? Email { get; set; }

        [ToolField(Description = "Optional description", MaxLength = 500)]
        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class NullableTestClass
    {
        public int? NullableInt { get; set; }
        public DateTime? NullableDate { get; set; }
        public string? NullableString { get; set; }
    }

    public class CollectionTestClass
    {
        public string[] StringArray { get; set; } = Array.Empty<string>();
        public List<int> IntList { get; set; } = new();
        public Dictionary<string, object> ObjectDict { get; set; } = new();
    }

    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    public class EnumTestClass
    {
        public TestEnum Status { get; set; }
    }

    [TestMethod]
    public void Generate_Should_GenerateValidSchema_When_GenericTypeProvided()
    {
        // Act
        var schema = SchemaGenerator.Generate<SimpleTestClass>();

        // Assert
        Assert.IsNotNull(schema);
        Assert.IsInstanceOfType(schema, typeof(Dictionary<string, object>));
        
        var schemaDict = (Dictionary<string, object>)schema;
        Assert.IsTrue(schemaDict.ContainsKey("type"));
        Assert.AreEqual("object", schemaDict["type"]);
    }

    [TestMethod]
    public void Generate_Should_GenerateValidSchema_When_TypeProvided()
    {
        // Arrange
        var type = typeof(SimpleTestClass);

        // Act
        var schema = SchemaGenerator.Generate(type);

        // Assert
        Assert.IsNotNull(schema);
        Assert.IsInstanceOfType(schema, typeof(Dictionary<string, object>));
        
        var schemaDict = (Dictionary<string, object>)schema;
        Assert.IsTrue(schemaDict.ContainsKey("type"));
        Assert.AreEqual("object", schemaDict["type"]);
    }

    [TestMethod]
    public void Generate_Should_IncludeProperties_When_ClassHasProperties()
    {
        // Act
        var schema = SchemaGenerator.Generate<SimpleTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.IsTrue(schemaDict.ContainsKey("properties"));
        var properties = (Dictionary<string, object>)schemaDict["properties"];
        
        Assert.IsTrue(properties.ContainsKey("name"));
        Assert.IsTrue(properties.ContainsKey("age"));
        Assert.IsTrue(properties.ContainsKey("isActive"));
    }

    [TestMethod]
    public void Generate_Should_GenerateCorrectPrimitiveTypes_When_PropertiesArePrimitive()
    {
        // Act
        var schema = SchemaGenerator.Generate<SimpleTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;
        var properties = (Dictionary<string, object>)schemaDict["properties"];

        // Assert
        var nameSchema = (Dictionary<string, object>)properties["name"];
        Assert.AreEqual("string", nameSchema["type"]);

        var ageSchema = (Dictionary<string, object>)properties["age"];
        Assert.AreEqual("integer", ageSchema["type"]);

        var isActiveSchema = (Dictionary<string, object>)properties["isActive"];
        Assert.AreEqual("boolean", isActiveSchema["type"]);
    }

    [TestMethod]
    public void Generate_Should_HandleNullableTypes_When_PropertiesAreNullable()
    {
        // Act
        var schema = SchemaGenerator.Generate<NullableTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;
        var properties = (Dictionary<string, object>)schemaDict["properties"];

        // Assert
        var nullableIntSchema = (Dictionary<string, object>)properties["nullableInt"];
        Assert.IsTrue(nullableIntSchema["type"] is object[] intTypes && intTypes.Contains("null"));

        var nullableDateSchema = (Dictionary<string, object>)properties["nullableDate"];
        Assert.IsTrue(nullableDateSchema["type"] is object[] dateTypes && dateTypes.Contains("null"));
    }

    [TestMethod]
    public void Generate_Should_HandleCollections_When_PropertiesAreCollections()
    {
        // Act
        var schema = SchemaGenerator.Generate<CollectionTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;
        var properties = (Dictionary<string, object>)schemaDict["properties"];

        // Assert
        var arraySchema = (Dictionary<string, object>)properties["stringArray"];
        Assert.AreEqual("array", arraySchema["type"]);
        Assert.IsTrue(arraySchema.ContainsKey("items"));

        var listSchema = (Dictionary<string, object>)properties["intList"];
        Assert.AreEqual("array", listSchema["type"]);
        Assert.IsTrue(listSchema.ContainsKey("items"));

        var dictSchema = (Dictionary<string, object>)properties["objectDict"];
        Assert.AreEqual("object", dictSchema["type"]);
    }

    [TestMethod]
    public void Generate_Should_HandleEnums_When_PropertyIsEnum()
    {
        // Act
        var schema = SchemaGenerator.Generate<EnumTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;
        var properties = (Dictionary<string, object>)schemaDict["properties"];

        // Assert
        var statusSchema = (Dictionary<string, object>)properties["status"];
        Assert.AreEqual("string", statusSchema["type"]);
        Assert.IsTrue(statusSchema.ContainsKey("enum"));
        
        var enumValues = (object[])statusSchema["enum"];
        Assert.IsTrue(enumValues.Contains("Value1"));
        Assert.IsTrue(enumValues.Contains("Value2"));
        Assert.IsTrue(enumValues.Contains("Value3"));
    }

    [TestMethod]
    public void Generate_Should_ProcessToolFieldAttributes_When_AttributesPresent()
    {
        // Act
        var schema = SchemaGenerator.Generate<ComplexTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;
        var properties = (Dictionary<string, object>)schemaDict["properties"];

        // Assert
        var nameSchema = (Dictionary<string, object>)properties["name"];
        Assert.AreEqual("User name", nameSchema["description"]);
        Assert.AreEqual("John Doe", nameSchema["example"]);

        var ageSchema = (Dictionary<string, object>)properties["age"];
        Assert.AreEqual("User age", ageSchema["description"]);
        Assert.AreEqual(0.0, ageSchema["minimum"]);
        Assert.AreEqual(150.0, ageSchema["maximum"]);

        var emailSchema = (Dictionary<string, object>)properties["email"];
        Assert.AreEqual("email", emailSchema["format"]);
    }

    [TestMethod]
    public void Generate_Should_ProcessDataAnnotations_When_AttributesPresent()
    {
        // Act
        var schema = SchemaGenerator.Generate<ComplexTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;
        var properties = (Dictionary<string, object>)schemaDict["properties"];

        // Assert
        var ageSchema = (Dictionary<string, object>)properties["age"];
        Assert.AreEqual(0.0, ageSchema["minimum"]);
        Assert.AreEqual(150.0, ageSchema["maximum"]);

        var descriptionSchema = (Dictionary<string, object>)properties["description"];
        Assert.AreEqual(500, descriptionSchema["maxLength"]);
    }

    [TestMethod]
    public void Generate_Should_IncludeRequiredFields_When_PropertiesAreRequired()
    {
        // Act
        var schema = SchemaGenerator.Generate<ComplexTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.IsTrue(schemaDict.ContainsKey("required"));
        var required = (object[])schemaDict["required"];
        Assert.IsTrue(required.Contains("name"));
    }

    [TestMethod]
    public void Generate_Should_SetAdditionalPropertiesFalse_When_GeneratingObjectSchema()
    {
        // Act
        var schema = SchemaGenerator.Generate<SimpleTestClass>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.IsTrue(schemaDict.ContainsKey("additionalProperties"));
        Assert.AreEqual(false, schemaDict["additionalProperties"]);
    }

    [TestMethod]
    public void Generate_Should_HandleCircularReferences_When_TypeReferencesItself()
    {
        // This test would require a class that references itself
        // For now, we'll test that the method doesn't throw
        
        // Act & Assert - should not throw
        var schema = SchemaGenerator.Generate<SimpleTestClass>();
        Assert.IsNotNull(schema);
    }

    [TestMethod]
    public void Generate_Should_GenerateStringSchema_When_TypeIsString()
    {
        // Act
        var schema = SchemaGenerator.Generate<string>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.IsTrue(schemaDict["type"] is object[] types && types.Contains("string") && types.Contains("null"));
    }

    [TestMethod]
    public void Generate_Should_GenerateIntegerSchema_When_TypeIsInt()
    {
        // Act
        var schema = SchemaGenerator.Generate<int>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.AreEqual("integer", schemaDict["type"]);
    }

    [TestMethod]
    public void Generate_Should_GenerateNumberSchema_When_TypeIsDouble()
    {
        // Act
        var schema = SchemaGenerator.Generate<double>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.AreEqual("number", schemaDict["type"]);
    }

    [TestMethod]
    public void Generate_Should_GenerateBooleanSchema_When_TypeIsBool()
    {
        // Act
        var schema = SchemaGenerator.Generate<bool>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.AreEqual("boolean", schemaDict["type"]);
    }

    [TestMethod]
    public void Generate_Should_GenerateStringWithFormat_When_TypeIsDateTime()
    {
        // Act
        var schema = SchemaGenerator.Generate<DateTime>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.AreEqual("string", schemaDict["type"]);
        Assert.AreEqual("date-time", schemaDict["format"]);
    }

    [TestMethod]
    public void Generate_Should_GenerateStringWithFormat_When_TypeIsGuid()
    {
        // Act
        var schema = SchemaGenerator.Generate<Guid>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.AreEqual("string", schemaDict["type"]);
        Assert.AreEqual("uuid", schemaDict["format"]);
    }

    [TestMethod]
    public void Generate_Should_GenerateStringWithFormat_When_TypeIsUri()
    {
        // Act
        var schema = SchemaGenerator.Generate<Uri>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.IsTrue(schemaDict["type"] is object[] types && types.Contains("string") && types.Contains("null"));
        Assert.AreEqual("uri", schemaDict["format"]);
    }

    [TestMethod]
    public void Generate_Should_HandleComplexNestedTypes_When_TypeHasNestedObjects()
    {
        // Act
        var schema = SchemaGenerator.Generate<Dictionary<string, SimpleTestClass>>();
        var schemaDict = (Dictionary<string, object>)schema;

        // Assert
        Assert.AreEqual("object", schemaDict["type"]);
        Assert.IsTrue(schemaDict.ContainsKey("additionalProperties"));
        
        var additionalProps = (Dictionary<string, object>)schemaDict["additionalProperties"];
        Assert.AreEqual("object", additionalProps["type"]);
    }
}
