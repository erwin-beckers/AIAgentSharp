namespace AIAgentSharp.Tests;

[TestClass]
public sealed class LlmClientTests
{
    [TestMethod]
    public async Task DelegateLlmClient_CompleteAsync_CallsDelegate()
    {
        // Arrange
        var expectedMessages = new List<LlmMessage>
        {
            new() { Role = "system", Content = "test system" },
            new() { Role = "user", Content = "test user" }
        };
        var expectedResult = "test completion";

        var delegateCalled = false;
        var delegateLlmClient = new DelegateLlmClient((messages, ct) =>
        {
            delegateCalled = true;
            Assert.AreEqual(expectedMessages.Count, messages.Count());
            Assert.AreEqual(expectedMessages[0].Content, messages.First().Content);
            Assert.AreEqual(expectedMessages[1].Content, messages.Skip(1).First().Content);
            return Task.FromResult(expectedResult);
        });

        // Act
        var result = await delegateLlmClient.CompleteAsync(expectedMessages);

        // Assert
        Assert.IsTrue(delegateCalled);
        Assert.AreEqual(expectedResult, result);
    }

    [TestMethod]
    public void DelegateLlmClient_NullDelegate_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new DelegateLlmClient(null!));
    }

    [TestMethod]
    public async Task DelegateLlmClient_WithCancellationToken_PassesToken()
    {
        // Arrange
        var tokenPassed = false;
        var delegateLlmClient = new DelegateLlmClient((messages, ct) =>
        {
            tokenPassed = true;
            Assert.IsTrue(ct.CanBeCanceled);
            return Task.FromResult("test");
        });

        using var cts = new CancellationTokenSource();

        // Act
        await delegateLlmClient.CompleteAsync(new[] { new LlmMessage() }, cts.Token);

        // Assert
        Assert.IsTrue(tokenPassed);
    }

    [TestMethod]
    public async Task DelegateLlmClient_EmptyMessages_HandlesCorrectly()
    {
        // Arrange
        var delegateLlmClient = new DelegateLlmClient((messages, ct) =>
        {
            Assert.AreEqual(0, messages.Count());
            return Task.FromResult("empty result");
        });

        // Act
        var result = await delegateLlmClient.CompleteAsync(Enumerable.Empty<LlmMessage>());

        // Assert
        Assert.AreEqual("empty result", result);
    }

    [TestMethod]
    public async Task DelegateLlmClient_ExceptionInDelegate_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("test exception");
        var delegateLlmClient = new DelegateLlmClient((messages, ct) =>
        {
            throw expectedException;
        });

        // Act & Assert
        var actualException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => delegateLlmClient.CompleteAsync(new[] { new LlmMessage() }));
        Assert.AreEqual(expectedException, actualException);
    }
}