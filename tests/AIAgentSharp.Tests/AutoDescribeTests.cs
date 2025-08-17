using System.Text.Json;

// Added for List and Dictionary

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class AutoDescribeTests
{
    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    [TestMethod]
    public void Build_WithToolDescription_UsesProvidedDescription()
    {
        // Act
        var description = ToolDescriptionGenerator.Build<TestParams>("test_tool", "Custom tool description");

        // Assert
        var result = JsonSerializer.Deserialize<JsonElement>(description);
        Assert.AreEqual("test_tool", result.GetProperty("name").GetString());
        Assert.AreEqual("Custom tool description", result.GetProperty("description").GetString());
        Assert.IsTrue(result.TryGetProperty("params", out _));
    }

    [TestMethod]
    public void Build_WithoutToolDescription_UsesParamsAttributeDescription()
    {
        // Act
        var description = ToolDescriptionGenerator.Build<TestParamsWithDescription>("test_tool");

        // Assert
        var result = JsonSerializer.Deserialize<JsonElement>(description);
        Assert.AreEqual("test_tool", result.GetProperty("name").GetString());
        Assert.AreEqual("Parameters for test tool", result.GetProperty("description").GetString());
        Assert.IsTrue(result.TryGetProperty("params", out _));
    }

    [TestMethod]
    public void Build_WithoutToolDescriptionOrParamsAttribute_UsesDefaultDescription()
    {
        // Act
        var description = ToolDescriptionGenerator.Build<TestParams>("test_tool");

        // Assert
        var result = JsonSerializer.Deserialize<JsonElement>(description);
        Assert.AreEqual("test_tool", result.GetProperty("name").GetString());
        Assert.AreEqual("Parameters for test_tool", result.GetProperty("description").GetString());
        Assert.IsTrue(result.TryGetProperty("params", out _));
    }

    [TestMethod]
    public void Build_IncludesGeneratedSchema()
    {
        // Act
        var description = ToolDescriptionGenerator.Build<TestParams>("test_tool", "Test description");

        // Assert
        var result = JsonSerializer.Deserialize<JsonElement>(description);
        var paramsSchema = result.GetProperty("params");

        // Verify the schema structure
        Assert.AreEqual("object", paramsSchema.GetProperty("type").GetString());
        Assert.IsTrue(paramsSchema.TryGetProperty("properties", out _));
        Assert.IsTrue(paramsSchema.TryGetProperty("required", out _));
    }

    [TestMethod]
    public void Build_DeterministicOutput_ProducesConsistentResults()
    {
        // Act
        var description1 = ToolDescriptionGenerator.Build<TestParams>("test_tool", "Test description");
        var description2 = ToolDescriptionGenerator.Build<TestParams>("test_tool", "Test description");

        // Assert
        Assert.AreEqual(description1, description2);
    }

    [TestMethod]
    public void Build_ComplexParams_IncludesAllProperties()
    {
        // Act
        var description = ToolDescriptionGenerator.Build<TestParamsWithAllTypes>("complex_tool", "Complex tool");

        // Assert
        var result = JsonSerializer.Deserialize<JsonElement>(description);
        var paramsSchema = result.GetProperty("params");
        var properties = paramsSchema.GetProperty("properties");

        // Verify all properties are included
        Assert.IsTrue(properties.TryGetProperty("stringValue", out _));
        Assert.IsTrue(properties.TryGetProperty("intValue", out _));
        Assert.IsTrue(properties.TryGetProperty("doubleValue", out _));
        Assert.IsTrue(properties.TryGetProperty("boolValue", out _));
        Assert.IsTrue(properties.TryGetProperty("enumValue", out _));
        Assert.IsTrue(properties.TryGetProperty("arrayValue", out _));
        Assert.IsTrue(properties.TryGetProperty("listValue", out _));
        Assert.IsTrue(properties.TryGetProperty("dictValue", out _));
        Assert.IsTrue(properties.TryGetProperty("nullableString", out _));
        Assert.IsTrue(properties.TryGetProperty("nullableInt", out _));
    }

    [TestMethod]
    public void Build_SingleLineOutput_NoLineBreaks()
    {
        // Act
        var description = ToolDescriptionGenerator.Build<TestParams>("test_tool", "Test description");

        // Assert
        Assert.IsFalse(description.Contains('\n'));
        Assert.IsFalse(description.Contains('\r'));
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

    [ToolParams(Description = "Parameters for test tool")]
    public class TestParamsWithDescription
    {
        public string Value { get; set; } = string.Empty;
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