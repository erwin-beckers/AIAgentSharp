using System.ComponentModel.DataAnnotations;

namespace AIAgentSharp.Tests.Schema;

[TestClass]
public class ToolDescriptionGeneratorTests
{
    // Test parameter classes
    [ToolParams(Description = "Test parameters for calculator")]
    public class CalculatorParams
    {
        [ToolField(Description = "First number", Required = true)]
        [Required]
        public double A { get; set; }

        [ToolField(Description = "Second number", Required = true)]
        [Required]
        public double B { get; set; }

        [ToolField(Description = "Operation to perform")]
        public string Operation { get; set; } = "add";
    }

    public class SimpleParams
    {
        [ToolField(Description = "Input string")]
        public string Input { get; set; } = "";
    }

    [ToolParams(Description = "Complex parameters")]
    public class ComplexParams
    {
        [ToolField(Description = "Required integer")]
        [Required]
        public int RequiredInt { get; set; }

        [ToolField(Description = "Optional string")]
        public string? OptionalString { get; set; }

        [ToolField(Description = "Boolean flag")]
        public bool Flag { get; set; }
    }

    [TestMethod]
    public void Build_Should_GenerateValidJson_When_ValidParametersProvided()
    {
        // Arrange
        var toolName = "calculator";
        var toolDescription = "Performs mathematical calculations";

        // Act
        var result = ToolDescriptionGenerator.Build<CalculatorParams>(toolName, toolDescription);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result));
        
        // Verify it's valid JSON
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("name", out var nameProp));
        Assert.AreEqual(toolName, nameProp.GetString());
        
        Assert.IsTrue(root.TryGetProperty("description", out var descProp));
        Assert.AreEqual(toolDescription, descProp.GetString());
        
        Assert.IsTrue(root.TryGetProperty("params", out var paramsProp));
        Assert.IsTrue(paramsProp.ValueKind == System.Text.Json.JsonValueKind.Object);
    }

    [TestMethod]
    public void Build_Should_UseToolParamsAttributeDescription_When_NoDescriptionProvided()
    {
        // Arrange
        var toolName = "calculator";

        // Act
        var result = ToolDescriptionGenerator.Build<CalculatorParams>(toolName);

        // Assert
        Assert.IsNotNull(result);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("description", out var descProp));
        Assert.AreEqual("Test parameters for calculator", descProp.GetString());
    }

    [TestMethod]
    public void Build_Should_GenerateDefaultDescription_When_NoToolParamsAttribute()
    {
        // Arrange
        var toolName = "simple_tool";

        // Act
        var result = ToolDescriptionGenerator.Build<SimpleParams>(toolName);

        // Assert
        Assert.IsNotNull(result);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("description", out var descProp));
        Assert.AreEqual($"Parameters for {toolName}", descProp.GetString());
    }

    [TestMethod]
    public void Build_Should_IncludeParameterSchema_When_Called()
    {
        // Arrange
        var toolName = "calculator";

        // Act
        var result = ToolDescriptionGenerator.Build<CalculatorParams>(toolName);

        // Assert
        Assert.IsNotNull(result);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("params", out var paramsProp));
        Assert.IsTrue(paramsProp.ValueKind == System.Text.Json.JsonValueKind.Object);
        
        // Verify the schema contains expected properties
        var paramsObj = paramsProp;
        Assert.IsTrue(paramsObj.TryGetProperty("properties", out var properties));
        Assert.IsTrue(properties.TryGetProperty("a", out var aProp));
        Assert.IsTrue(properties.TryGetProperty("b", out var bProp));
        Assert.IsTrue(properties.TryGetProperty("operation", out var opProp));
    }

    [TestMethod]
    public void Build_Should_HandleComplexParameters_When_ComplexParamsProvided()
    {
        // Arrange
        var toolName = "complex_tool";

        // Act
        var result = ToolDescriptionGenerator.Build<ComplexParams>(toolName);

        // Assert
        Assert.IsNotNull(result);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("name", out var nameProp));
        Assert.AreEqual(toolName, nameProp.GetString());
        
        Assert.IsTrue(root.TryGetProperty("description", out var descProp));
        Assert.AreEqual("Complex parameters", descProp.GetString());
        
        Assert.IsTrue(root.TryGetProperty("params", out var paramsProp));
        Assert.IsTrue(paramsProp.ValueKind == System.Text.Json.JsonValueKind.Object);
    }

    [TestMethod]
    public void Build_Should_HandleEmptyToolName_When_EmptyNameProvided()
    {
        // Arrange
        var toolName = "";

        // Act
        var result = ToolDescriptionGenerator.Build<SimpleParams>(toolName);

        // Assert
        Assert.IsNotNull(result);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("name", out var nameProp));
        Assert.AreEqual("", nameProp.GetString());
    }

    [TestMethod]
    public void Build_Should_HandleNullToolDescription_When_NullDescriptionProvided()
    {
        // Arrange
        var toolName = "test_tool";
        string? toolDescription = null;

        // Act
        var result = ToolDescriptionGenerator.Build<CalculatorParams>(toolName, toolDescription);

        // Assert
        Assert.IsNotNull(result);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("description", out var descProp));
        Assert.AreEqual("Test parameters for calculator", descProp.GetString());
    }

    [TestMethod]
    public void Build_Should_GenerateConsistentOutput_When_CalledMultipleTimes()
    {
        // Arrange
        var toolName = "calculator";
        var toolDescription = "Performs calculations";

        // Act
        var result1 = ToolDescriptionGenerator.Build<CalculatorParams>(toolName, toolDescription);
        var result2 = ToolDescriptionGenerator.Build<CalculatorParams>(toolName, toolDescription);

        // Assert
        Assert.AreEqual(result1, result2);
    }

    [TestMethod]
    public void Build_Should_IncludeRequiredFields_When_ParametersHaveRequiredAttributes()
    {
        // Arrange
        var toolName = "calculator";

        // Act
        var result = ToolDescriptionGenerator.Build<CalculatorParams>(toolName);

        // Assert
        Assert.IsNotNull(result);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("params", out var paramsProp));
        Assert.IsTrue(paramsProp.TryGetProperty("required", out var requiredProp));
        Assert.IsTrue(requiredProp.ValueKind == System.Text.Json.JsonValueKind.Array);
        
        var requiredArray = requiredProp;
        var requiredList = new List<string>();
        foreach (var item in requiredArray.EnumerateArray())
        {
            requiredList.Add(item.GetString()!);
        }
        
        Assert.IsTrue(requiredList.Contains("a"));
        Assert.IsTrue(requiredList.Contains("b"));
    }

    [TestMethod]
    public void Build_Should_HandleParametersWithToolFieldAttributes_When_AttributesPresent()
    {
        // Arrange
        var toolName = "test_tool";

        // Act
        var result = ToolDescriptionGenerator.Build<ComplexParams>(toolName);

        // Assert
        Assert.IsNotNull(result);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        Assert.IsTrue(root.TryGetProperty("params", out var paramsProp));
        Assert.IsTrue(paramsProp.TryGetProperty("properties", out var properties));
        
        // Check that properties have descriptions from ToolField attributes
        Assert.IsTrue(properties.TryGetProperty("requiredInt", out var requiredIntProp));
        Assert.IsTrue(requiredIntProp.TryGetProperty("description", out var requiredIntDesc));
        Assert.AreEqual("Required integer", requiredIntDesc.GetString());
        
        Assert.IsTrue(properties.TryGetProperty("optionalString", out var optionalStringProp));
        Assert.IsTrue(optionalStringProp.TryGetProperty("description", out var optionalStringDesc));
        Assert.AreEqual("Optional string", optionalStringDesc.GetString());
    }
}
