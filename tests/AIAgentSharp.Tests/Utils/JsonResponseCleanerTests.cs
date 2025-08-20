using AIAgentSharp.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.Tests.Utils;

[TestClass]
public class JsonResponseCleanerTests
{
    [TestMethod]
    public void CleanJsonResponse_Should_ReturnNull_When_InputIsNull()
    {
        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(null!);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ReturnEmptyString_When_InputIsEmpty()
    {
        // Act
        var result = JsonResponseCleaner.CleanJsonResponse("");

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ReturnEmptyString_When_InputIsWhitespace()
    {
        // Act
        var result = JsonResponseCleaner.CleanJsonResponse("   ");

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_RemoveMarkdownCodeBlocks_WithJsonLanguage()
    {
        // Arrange
        var input = "```json\n{\"name\": \"test\"}\n```";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_RemoveMarkdownCodeBlocks_WithoutLanguage()
    {
        // Arrange
        var input = "```\n{\"name\": \"test\"}\n```";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleCodeBlocks_WithExtraWhitespace()
    {
        // Arrange
        var input = "```json\n  {\"name\": \"test\"}  \n```";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ReturnOriginalContent_When_NoCodeBlocks()
    {
        // Arrange
        var input = "{\"name\": \"test\"}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleDuplicateIdenticalJsonObjects()
    {
        // Arrange
        var input = "{\"name\": \"test\"}{\"name\": \"test\"}{\"name\": \"test\"}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_ReturnFirstObject_When_DuplicateObjectsAreDifferent()
    {
        // Arrange
        var input = "{\"name\": \"test1\"}{\"name\": \"test2\"}{\"name\": \"test3\"}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test1\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleNestedJsonObjects()
    {
        // Arrange
        var input = "{\"name\": \"test\", \"data\": {\"value\": 123}}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\", \"data\": {\"value\": 123}}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleJsonArrays()
    {
        // Arrange
        var input = "{\"items\": [1, 2, 3]}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"items\": [1, 2, 3]}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleComplexNestedStructures()
    {
        // Arrange
        var input = "{\"name\": \"test\", \"data\": {\"items\": [{\"id\": 1}, {\"id\": 2}]}}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\", \"data\": {\"items\": [{\"id\": 1}, {\"id\": 2}]}}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleTextBeforeJson()
    {
        // Arrange
        var input = "Here is the JSON response: {\"name\": \"test\"}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleTextAfterJson()
    {
        // Arrange
        var input = "{\"name\": \"test\"} This is the end of the response.";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleTextBeforeAndAfterJson()
    {
        // Arrange
        var input = "Here is the JSON response: {\"name\": \"test\"} This is the end.";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleMultipleJsonObjects_WithText()
    {
        // Arrange
        var input = "First object: {\"name\": \"test1\"} Second object: {\"name\": \"test2\"}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test1\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleIncompleteJsonObject()
    {
        // Arrange
        var input = "{\"name\": \"test\"";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleUnbalancedBraces()
    {
        // Arrange
        var input = "{\"name\": \"test\"}}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleJsonWithEscapedQuotes()
    {
        // Arrange
        var input = "{\"name\": \"test with \\\"quotes\\\"\"}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test with \\\"quotes\\\"\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleJsonWithNewlines()
    {
        // Arrange
        var input = "{\n\"name\": \"test\",\n\"value\": 123\n}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\n\"name\": \"test\",\n\"value\": 123\n}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleJsonWithTabs()
    {
        // Arrange
        var input = "{\t\"name\": \"test\",\t\"value\": 123\t}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\t\"name\": \"test\",\t\"value\": 123\t}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleCodeBlocksWithExtraContent()
    {
        // Arrange
        var input = "```json\n{\"name\": \"test\"}\n```\n\nAdditional text here";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleMultipleCodeBlocks()
    {
        // Arrange
        var input = "```json\n{\"name\": \"test1\"}\n```\n```json\n{\"name\": \"test2\"}\n```";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test1\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleCodeBlocksWithDifferentLanguages()
    {
        // Arrange
        var input = "```javascript\n{\"name\": \"test\"}\n```";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleCodeBlocksWithNoClosingBackticks()
    {
        // Arrange
        var input = "```json\n{\"name\": \"test\"}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleCodeBlocksWithExtraBackticks()
    {
        // Arrange
        var input = "````json\n{\"name\": \"test\"}\n````";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleRealWorldExample()
    {
        // Arrange
        var input = @"Here's the JSON response:

```json
{
  ""status"": ""success"",
  ""data"": {
    ""id"": 123,
    ""name"": ""Test Item"",
    ""tags"": [""tag1"", ""tag2""]
  }
}
```

This completes the response.";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\"status\": \"success\""));
        Assert.IsTrue(result.Contains("\"id\": 123"));
        Assert.IsTrue(result.Contains("\"name\": \"Test Item\""));
        Assert.IsTrue(result.Contains("\"tags\": [\"tag1\", \"tag2\"]"));
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleJsonWithComments()
    {
        // Arrange
        var input = "{\"name\": \"test\"} // This is a comment";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\"}", result);
    }

    [TestMethod]
    public void CleanJsonResponse_Should_HandleJsonWithTrailingComma()
    {
        // Arrange
        var input = "{\"name\": \"test\",}";

        // Act
        var result = JsonResponseCleaner.CleanJsonResponse(input);

        // Assert
        Assert.AreEqual("{\"name\": \"test\",}", result);
    }
}
