namespace AIAgentSharp.Tests;

[TestClass]
public class OpenAiLlmClientTests
{
    [TestMethod]
    public void OpenAiLlmClient_ImplementsIFunctionCallingLlmClient()
    {
        // Arrange & Act
        var client = new OpenAiLlmClient("test-key");

        // Assert
        Assert.IsInstanceOfType(client, typeof(IFunctionCallingLlmClient));
    }

    [TestMethod]
    [TestCategory("Integration")]
    [Ignore("Requires valid OpenAI API key")]
    public async Task CompleteWithFunctionsAsync_CurrentlyFallsBackToText()
    {
        // Arrange
        var client = new OpenAiLlmClient("test-key");

        var messages = new List<LlmMessage>
        {
            new() { Role = "system", Content = "You are a helpful assistant." },
            new() { Role = "user", Content = "Hello, how are you?" }
        };

        var functions = new List<OpenAiFunctionSpec>
        {
            new()
            {
                Name = "get_indicator",
                Description = "Fetches a single indicator value for a symbol.",
                ParametersSchema = new { type = "object" }
            }
        };

        // Act
        var result = await client.CompleteWithFunctionsAsync(messages, functions);

        // Assert
        Assert.IsFalse(result.HasFunctionCall);
        Assert.IsNotNull(result.RawTextFallback);
        Assert.IsNull(result.FunctionName);
        Assert.IsNull(result.FunctionArgumentsJson);
    }

    [TestMethod]
    public async Task CompleteWithFunctionsAsync_HandlesCancellation()
    {
        // Arrange
        var client = new OpenAiLlmClient("test-key");

        var messages = new List<LlmMessage>
        {
            new() { Role = "user", Content = "Test message" }
        };

        var functions = new List<OpenAiFunctionSpec>
        {
            new()
            {
                Name = "test_function",
                Description = "Test function",
                ParametersSchema = new { type = "object" }
            }
        };

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
        {
            await client.CompleteWithFunctionsAsync(messages, functions, cts.Token);
        });
    }

    [TestMethod]
    [TestCategory("Integration")]
    [Ignore("Requires valid OpenAI API key")]
    public async Task CompleteWithFunctionsAsync_AcceptsComplexSchema()
    {
        // Arrange
        var client = new OpenAiLlmClient("test-key");

        var messages = new List<LlmMessage>
        {
            new() { Role = "user", Content = "Test complex schema" }
        };

        var complexSchema = new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string", description = "The name" },
                age = new { type = "integer", minimum = 0, maximum = 150 },
                isActive = new { type = "boolean" },
                tags = new
                {
                    type = "array",
                    items = new { type = "string" },
                    minItems = 1,
                    maxItems = 10
                }
            },
            required = new[] { "name", "age" },
            additionalProperties = false
        };

        var functions = new List<OpenAiFunctionSpec>
        {
            new()
            {
                Name = "complex_function",
                Description = "A function with complex schema",
                ParametersSchema = complexSchema
            }
        };

        // Act
        var result = await client.CompleteWithFunctionsAsync(messages, functions);

        // Assert
        Assert.IsFalse(result.HasFunctionCall);
        Assert.IsNotNull(result.RawTextFallback);
    }
}