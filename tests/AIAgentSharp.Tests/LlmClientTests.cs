namespace AIAgentSharp.Tests;

[TestClass]
public class LlmClientTests
{
    [TestMethod]
    public async Task DelegateLlmClient_CompleteAsync_ReturnsExpectedResult()
    {
        // Arrange
        var expectedContent = "Test response";
        var expectedUsage = new LlmUsage
        {
            InputTokens = 10,
            OutputTokens = 20,
            Model = "test-model",
            Provider = "test-provider"
        };

        var delegateClient = new DelegateLlmClient(
            (messages, ct) => Task.FromResult(new LlmCompletionResult
            {
                Content = expectedContent,
                Usage = expectedUsage
            }));

        var messages = new List<LlmMessage>
        {
            new LlmMessage { Role = "user", Content = "Hello" }
        };

        // Act
        var result = await delegateClient.CompleteAsync(messages);

        // Assert
        Assert.AreEqual(expectedContent, result.Content);
        Assert.AreEqual(expectedUsage.InputTokens, result.Usage?.InputTokens);
        Assert.AreEqual(expectedUsage.OutputTokens, result.Usage?.OutputTokens);
        Assert.AreEqual(expectedUsage.Model, result.Usage?.Model);
        Assert.AreEqual(expectedUsage.Provider, result.Usage?.Provider);
    }

    [TestMethod]
    public async Task DelegateLlmClient_CompleteWithFunctionsAsync_ThrowsNotSupportedException()
    {
        // Arrange
        var delegateClient = new DelegateLlmClient(
            (messages, ct) => Task.FromResult(new LlmCompletionResult { Content = "test" }));

        var messages = new List<LlmMessage>
        {
            new LlmMessage { Role = "user", Content = "Hello" }
        };

        var functions = new List<OpenAiFunctionSpec>
        {
            new OpenAiFunctionSpec { Name = "test" }
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotSupportedException>(
            () => delegateClient.CompleteWithFunctionsAsync(messages, functions));
    }

    [TestMethod]
    public async Task DelegateLlmClient_CompleteWithFunctionsAsync_WithFunctionImpl_ReturnsExpectedResult()
    {
        // Arrange
        var expectedFunctionResult = new FunctionCallResult
        {
            HasFunctionCall = true,
            FunctionName = "test_function",
            FunctionArgumentsJson = "{\"param\":\"value\"}",
            AssistantContent = "Function called",
            Usage = new LlmUsage
            {
                InputTokens = 15,
                OutputTokens = 25,
                Model = "test-model",
                Provider = "test-provider"
            }
        };

        var delegateClient = new DelegateLlmClient(
            (messages, ct) => Task.FromResult(new LlmCompletionResult { Content = "test" }),
            (messages, functions, ct) => Task.FromResult(expectedFunctionResult));

        var messages = new List<LlmMessage>
        {
            new LlmMessage { Role = "user", Content = "Hello" }
        };

        var functions = new List<OpenAiFunctionSpec>
        {
            new OpenAiFunctionSpec { Name = "test" }
        };

        // Act
        var result = await delegateClient.CompleteWithFunctionsAsync(messages, functions);

        // Assert
        Assert.AreEqual(expectedFunctionResult.HasFunctionCall, result.HasFunctionCall);
        Assert.AreEqual(expectedFunctionResult.FunctionName, result.FunctionName);
        Assert.AreEqual(expectedFunctionResult.FunctionArgumentsJson, result.FunctionArgumentsJson);
        Assert.AreEqual(expectedFunctionResult.AssistantContent, result.AssistantContent);
        Assert.AreEqual(expectedFunctionResult.Usage?.InputTokens, result.Usage?.InputTokens);
        Assert.AreEqual(expectedFunctionResult.Usage?.OutputTokens, result.Usage?.OutputTokens);
    }
}