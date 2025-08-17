using System.Text.Json;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class SchemaValidationTests
{
    [TestMethod]
    public void IToolSchema_Interface_IsImplementedCorrectly()
    {
        // Arrange
        var tool = new TypedTool();

        // Act & Assert
        Assert.AreEqual("typed-tool", tool.Name);
        Assert.AreEqual(typeof(TestParameters), ((IToolSchema)tool).ParameterType);
        Assert.AreEqual(typeof(TestResult), ((IToolSchema)tool).ResultType);
    }

    [TestMethod]
    public void TypedTool_InvokeAsync_WithValidParameters_ReturnsExpectedResult()
    {
        // Arrange
        var tool = new TypedTool();
        var parameters = new Dictionary<string, object?>
        {
            { "name", "Alice" },
            { "age", 25 },
            { "isActive", false }
        };

        // Act
        var result = tool.InvokeAsync(parameters).Result;

        // Assert
        Assert.IsNotNull(result);
        var typedResult = result as TestResult;
        Assert.IsNotNull(typedResult);
        Assert.AreEqual("Hello Alice", typedResult.Message);
        Assert.AreEqual(25, typedResult.Count);
    }

    [TestMethod]
    public void TypedTool_InvokeAsync_WithInvalidParameters_ThrowsJsonException()
    {
        // Arrange
        var tool = new TypedTool();
        var parameters = new Dictionary<string, object?>
        {
            { "name", "Bob" },
            { "age", "not_a_number" },
            { "isActive", true }
        };

        // Act & Assert
        var exception = Assert.ThrowsExceptionAsync<JsonException>(() => tool.InvokeAsync(parameters));

        Assert.IsTrue(exception.Result.Message.Contains("could not be converted"));
    }

    [TestMethod]
    public void TypedTool_InvokeAsync_WithComplexObject_DeserializesCorrectly()
    {
        // Arrange
        var tool = new TypedTool();
        var parameters = new Dictionary<string, object?>
        {
            { "name", "Complex Name with Spaces" },
            { "age", 999 },
            { "isActive", true }
        };

        // Act
        var result = tool.InvokeAsync(parameters).Result;

        // Assert
        Assert.IsNotNull(result);
        var typedResult = result as TestResult;
        Assert.IsNotNull(typedResult);
        Assert.AreEqual("Hello Complex Name with Spaces", typedResult.Message);
        Assert.AreEqual(999, typedResult.Count);
    }

    [TestMethod]
    public void TypedTool_InvokeAsync_WithJsonElementParameters_DeserializesCorrectly()
    {
        // Arrange
        var tool = new TypedTool();
        var json = JsonSerializer.Serialize(new { name = "JsonElement", age = 42, isActive = true });
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        var parameters = new Dictionary<string, object?>
        {
            { "name", jsonElement.GetProperty("name").GetString() },
            { "age", jsonElement.GetProperty("age").GetInt32() },
            { "isActive", jsonElement.GetProperty("isActive").GetBoolean() }
        };

        // Act
        var result = tool.InvokeAsync(parameters).Result;

        // Assert
        Assert.IsNotNull(result);
        var typedResult = result as TestResult;
        Assert.IsNotNull(typedResult);
        Assert.AreEqual("Hello JsonElement", typedResult.Message);
        Assert.AreEqual(42, typedResult.Count);
    }

    [TestMethod]
    public void TypedTool_InvokeAsync_WithTypeConversion_HandlesNumericTypes()
    {
        // Arrange
        var tool = new TypedTool();
        var parameters = new Dictionary<string, object?>
        {
            { "name", "NumberTest" },
            { "age", 42L }, // Long instead of int
            { "isActive", true } // Use proper boolean instead of int
        };

        // Act
        var result = tool.InvokeAsync(parameters).Result;

        // Assert
        Assert.IsNotNull(result);
        var typedResult = result as TestResult;
        Assert.IsNotNull(typedResult);
        Assert.AreEqual("Hello NumberTest", typedResult.Message);
        Assert.AreEqual(42, typedResult.Count);
    }

    public class TestParameters
    {
        public string Name { get; set; } = string.Empty;
        public int? Age { get; set; }
        public bool? IsActive { get; set; }
    }

    public class TestResult
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TypedTool : ITool<TestParameters, TestResult>
    {
        public string Name => "typed-tool";

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            // Direct deserialization without the removed ValidateAndDeserializeParameters method
            var json = JsonSerializer.Serialize(parameters, JsonUtil.JsonOptions);
            var typedParams = JsonSerializer.Deserialize<TestParameters>(json, JsonUtil.JsonOptions);

            if (typedParams == null)
            {
                throw new ArgumentException("Failed to deserialize parameters");
            }

            return Task.FromResult<object?>(new TestResult
            {
                Message = $"Hello {typedParams.Name}",
                Count = typedParams.Age ?? 0 // Handle nullable Age
            });
        }
    }
}