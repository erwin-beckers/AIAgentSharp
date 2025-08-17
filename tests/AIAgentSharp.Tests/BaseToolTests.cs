using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class BaseToolTests
{
    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    [TestMethod]
    public void BaseTool_GetJsonSchema_EqualsSchemaGenerator()
    {
        // Arrange
        var tool = new TestBaseTool();

        // Act
        var toolSchema = tool.GetJsonSchema();
        var generatorSchema = SchemaGenerator.Generate<TestParams>();

        // Assert
        var toolJson = JsonSerializer.Serialize(toolSchema, JsonUtil.JsonOptions);
        var generatorJson = JsonSerializer.Serialize(generatorSchema, JsonUtil.JsonOptions);
        Assert.AreEqual(generatorJson, toolJson);
    }

    [TestMethod]
    public void BaseTool_Describe_EqualsToolDescriptionGenerator()
    {
        // Arrange
        var tool = new TestBaseTool();

        // Act
        var toolDescription = tool.Describe();
        var generatorDescription = ToolDescriptionGenerator.Build<TestParams>(tool.Name, tool.Description);

        // Assert
        Assert.AreEqual(generatorDescription, toolDescription);
    }

    [TestMethod]
    public async Task BaseTool_InvokeAsync_WithValidParameters_Succeeds()
    {
        // Arrange
        var tool = new TestBaseTool();
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = "test",
            ["requiredInt"] = 42,
            ["enumValue"] = "Value1",
            ["arrayValue"] = new[] { "item1", "item2" }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultJson = JsonSerializer.Serialize(result, JsonUtil.JsonOptions);
        var resultElement = JsonSerializer.Deserialize<JsonElement>(resultJson);
        Assert.AreEqual("test", resultElement.GetProperty("requiredString").GetString());
        Assert.AreEqual(42, resultElement.GetProperty("requiredInt").GetInt32());
        Assert.AreEqual("Value1", resultElement.GetProperty("enumValue").GetString());
    }

    [TestMethod]
    public async Task BaseTool_InvokeAsync_WithMissingRequiredParameters_ThrowsValidationException()
    {
        // Arrange
        var tool = new TestBaseTool();
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = "test"
            // Missing requiredInt, enumValue, arrayValue
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ToolValidationException>(() => tool.InvokeAsync(parameters));

        Assert.IsTrue(exception.Message.Contains("Invalid parameters payload"));
        Assert.IsTrue(exception.Missing.Contains("requiredInt"));
        Assert.IsTrue(exception.Missing.Contains("enumValue"));
        Assert.IsTrue(exception.Missing.Contains("arrayValue"));
    }

    [TestMethod]
    public async Task BaseTool_InvokeAsync_WithInvalidJson_ThrowsValidationException()
    {
        // Arrange
        var tool = new TestBaseTool();
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = "test",
            ["requiredInt"] = "not_a_number", // Invalid type
            ["enumValue"] = "Value1",
            ["arrayValue"] = new[] { "item1", "item2" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ToolValidationException>(() => tool.InvokeAsync(parameters));

        Assert.IsTrue(exception.Message.Contains("Failed to deserialize parameters"));
    }

    [TestMethod]
    public void BaseTool_GetMissingRequiredFields_WithValidParameters_ReturnsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = "test",
            ["requiredInt"] = 42,
            ["enumValue"] = "Value1",
            ["arrayValue"] = new[] { "item1", "item2" }
        };

        // Act
        var missing = TestBaseTool.GetMissingRequiredFields<TestParams>(parameters);

        // Assert
        Assert.AreEqual(0, missing.Count);
    }

    [TestMethod]
    public void BaseTool_GetMissingRequiredFields_WithMissingParameters_ReturnsMissing()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = "test"
            // Missing requiredInt, enumValue, arrayValue
        };

        // Act
        var missing = TestBaseTool.GetMissingRequiredFields<TestParams>(parameters);

        // Assert
        Assert.AreEqual(3, missing.Count);
        Assert.IsTrue(missing.Contains("requiredInt"));
        Assert.IsTrue(missing.Contains("enumValue"));
        Assert.IsTrue(missing.Contains("arrayValue"));
    }

    [TestMethod]
    public void BaseTool_GetMissingRequiredFields_WithNullValues_ReturnsMissing()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = null,
            ["requiredInt"] = 42,
            ["enumValue"] = "",
            ["arrayValue"] = new[] { "item1", "item2" }
        };

        // Act
        var missing = TestBaseTool.GetMissingRequiredFields<TestParams>(parameters);

        // Assert
        Assert.AreEqual(2, missing.Count);
        Assert.IsTrue(missing.Contains("requiredString"));
        Assert.IsTrue(missing.Contains("enumValue"));
    }

    [TestMethod]
    public void BaseTool_GetMissingRequiredFields_WithNullableParameters_DoesNotRequire()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = "test",
            ["requiredInt"] = 42,
            ["enumValue"] = "Value1",
            ["arrayValue"] = new[] { "item1", "item2" }
            // Not including optionalString and optionalInt
        };

        // Act
        var missing = TestBaseTool.GetMissingRequiredFields<TestParams>(parameters);

        // Assert
        Assert.AreEqual(0, missing.Count);
    }

    [TestMethod]
    public void BaseTool_GetMissingRequiredFields_WithToolFieldRequired_RespectsAttribute()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = "test",
            ["requiredInt"] = 42,
            ["enumValue"] = "Value1",
            ["arrayValue"] = new[] { "item1", "item2" }
            // Missing explicitly required field
        };

        // Act
        var missing = TestBaseTool.GetMissingRequiredFields<TestParamsWithExplicitRequired>(parameters);

        // Assert
        Assert.AreEqual(1, missing.Count);
        Assert.IsTrue(missing.Contains("explicitlyRequired"));
    }

    [TestMethod]
    public void BaseTool_GetMissingRequiredFields_WithDataAnnotationsRequired_RespectsAttribute()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["requiredString"] = "test",
            ["requiredInt"] = 42,
            ["enumValue"] = "Value1",
            ["arrayValue"] = new[] { "item1", "item2" }
            // Missing DataAnnotations required field
        };

        // Act
        var missing = TestBaseTool.GetMissingRequiredFields<TestParamsWithDataAnnotationsRequired>(parameters);

        // Assert
        Assert.AreEqual(1, missing.Count);
        Assert.IsTrue(missing.Contains("dataAnnotationsRequired"));
    }

    // Test implementation of BaseTool
    public class TestBaseTool : BaseTool<TestParams, object>
    {
        public override string Name => "test_tool";
        public override string Description => "Test tool for unit testing";

        public override Task<object> InvokeTypedAsync(TestParams parameters, CancellationToken ct = default)
        {
            return Task.FromResult<object>(new
            {
                requiredString = parameters.RequiredString,
                requiredInt = parameters.RequiredInt,
                enumValue = parameters.EnumValue.ToString(),
                arrayValue = parameters.ArrayValue
            });
        }

        // Expose the protected method for testing
        public new static IReadOnlyList<string> GetMissingRequiredFields<T>(Dictionary<string, object?> parameters)
        {
            return BaseTool<TestParams, object>.GetMissingRequiredFields<T>(parameters);
        }
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

    public class TestParamsWithExplicitRequired
    {
        public string RequiredString { get; set; } = string.Empty;
        public int RequiredInt { get; set; }
        public TestEnum EnumValue { get; set; }
        public string[] ArrayValue { get; set; } = Array.Empty<string>();

        [ToolField(Required = true)]
        public string ExplicitlyRequired { get; set; } = string.Empty;
    }

    public class TestParamsWithDataAnnotationsRequired
    {
        public string RequiredString { get; set; } = string.Empty;
        public int RequiredInt { get; set; }
        public TestEnum EnumValue { get; set; }
        public string[] ArrayValue { get; set; } = Array.Empty<string>();

        [Required]
        public string DataAnnotationsRequired { get; set; } = string.Empty;
    }
}