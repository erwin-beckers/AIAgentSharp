using System.Text.Json;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class ConcatToolTests
{
    [TestMethod]
    public void Name_ReturnsConcat()
    {
        // Arrange & Act
        var tool = new ConcatTool();

        // Assert
        Assert.AreEqual("concat", tool.Name);
    }

    [TestMethod]
    public async Task InvokeAsync_WithStringArray_ConcatenatesCorrectly()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new[] { "hello", "world", "test" } }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello, world, test", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithCustomSeparator_ConcatenatesCorrectly()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new[] { "hello", "world", "test" } },
            { "sep", " | " }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello | world | test", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithListOfObjects_ConcatenatesCorrectly()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new List<object> { "hello", 123, true } }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello, 123, True", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithJsonElementArray_ConcatenatesCorrectly()
    {
        // Arrange
        var tool = new ConcatTool();
        var jsonArray = JsonSerializer.Deserialize<JsonElement>("[\"hello\", \"world\", \"test\"]");
        var parameters = new Dictionary<string, object?>
        {
            { "items", jsonArray }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello, world, test", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithSingleItem_ReturnsItemAsString()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", "single item" }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("single item", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithEmptyArray_ReturnsEmptyString()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new string[0] }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithNullValuesInArray_HandlesCorrectly()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new object?[] { "hello", null, "world" } }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello, , world", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithEmptyStringSeparator_ConcatenatesWithoutSeparator()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new[] { "hello", "world", "test" } },
            { "sep", "" }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("helloworldtest", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithComplexObjects_ConvertsToStrings()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            {
                "items", new object[]
                {
                    new { Name = "John", Age = 30 },
                    new { Name = "Jane", Age = 25 }
                }
            }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.IsNotNull(resultValue);
        var resultString = resultValue.ToString();
        Assert.IsNotNull(resultString);
        Assert.IsTrue(resultString.Contains("John"));
        Assert.IsTrue(resultString.Contains("Jane"));
    }

    [TestMethod]
    public async Task InvokeAsync_WithNonStringSeparator_UsesDefaultSeparator()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new[] { "hello", "world" } },
            { "sep", 123 }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello, world", resultValue); // Should use default separator
    }

    [TestMethod]
    public async Task InvokeAsync_WithNullSeparator_UsesDefaultSeparator()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new[] { "hello", "world" } },
            { "sep", null }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello, world", resultValue); // Should use default separator
    }

    [TestMethod]
    public async Task InvokeAsync_WithoutItemsParameter_ThrowsArgumentException()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => tool.InvokeAsync(parameters));

        Assert.IsTrue(exception.Message.Contains("Missing required parameter: items"));
    }

    [TestMethod]
    public async Task InvokeAsync_WithNullItems_HandlesGracefully()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", null }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithUnsupportedItemsType_HandlesGracefully()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", 123 } // Unsupported type
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("123", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new[] { "hello", "world" } }
        };
        var cts = new CancellationTokenSource();

        // Act
        var result = await tool.InvokeAsync(parameters, cts.Token);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello, world", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithLargeArray_HandlesCorrectly()
    {
        // Arrange
        var tool = new ConcatTool();
        var largeArray = Enumerable.Range(1, 1000).Select(i => $"item{i}").ToArray();
        var parameters = new Dictionary<string, object?>
        {
            { "items", largeArray }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.IsNotNull(resultValue);
        var resultString = resultValue.ToString();
        Assert.IsNotNull(resultString);
        Assert.IsTrue(resultString.Contains("item1"));
        Assert.IsTrue(resultString.Contains("item1000"));
        Assert.AreEqual(1000, resultString.Split(',').Length);
    }

    [TestMethod]
    public async Task InvokeAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new[] { "hello\nworld", "test\tstring", "quote\"test" } },
            { "sep", " | " }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("hello\nworld | test\tstring | quote\"test", resultValue);
    }

    [TestMethod]
    public async Task InvokeAsync_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var tool = new ConcatTool();
        var parameters = new Dictionary<string, object?>
        {
            { "items", new[] { "café", "naïve", "résumé", "🎉", "🚀" } }
        };

        // Act
        var result = await tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultType = result.GetType();
        var resultProperty = resultType.GetProperty("result");
        Assert.IsNotNull(resultProperty);
        var resultValue = resultProperty.GetValue(result);
        Assert.AreEqual("café, naïve, résumé, 🎉, 🚀", resultValue);
    }
}