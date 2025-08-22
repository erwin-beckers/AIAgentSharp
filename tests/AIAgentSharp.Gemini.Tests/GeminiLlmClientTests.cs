using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AIAgentSharp.Gemini.Tests;

[TestClass]
public class GeminiLlmClientTests
{
    private const string TestApiKey = "test-api-key";
    private const string DefaultModel = "gemini-2.5-flash";

    [TestMethod]
    public void Constructor_WithApiKey_ShouldCreateClient()
    {
        // Act
        var client = new GeminiLlmClient(TestApiKey);

        // Assert
        Assert.IsNotNull(client);
        Assert.AreEqual(DefaultModel, client.Configuration.Model);
    }

    [TestMethod]
    public void Constructor_WithApiKeyAndModel_ShouldCreateClient()
    {
        // Arrange
        const string customModel = "gemini-1.5-pro";

        // Act
        var client = new GeminiLlmClient(TestApiKey, model: customModel);

        // Assert
        Assert.IsNotNull(client);
        Assert.AreEqual(customModel, client.Configuration.Model);
    }

    [TestMethod]
    public void Constructor_WithApiKeyAndConfiguration_ShouldCreateClient()
    {
        // Arrange
        var config = new GeminiConfiguration
        {
            Model = "gemini-1.5-pro",
            Temperature = 0.5f,
            MaxTokens = 6000
        };

        // Act
        var client = new GeminiLlmClient(TestApiKey, config);

        // Assert
        Assert.IsNotNull(client);
        Assert.AreEqual("gemini-1.5-pro", client.Configuration.Model);
        Assert.AreEqual(0.5f, client.Configuration.Temperature);
        Assert.AreEqual(6000, client.Configuration.MaxTokens);
    }

    [TestMethod]
    public void Constructor_WithNullApiKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new GeminiLlmClient(null!));
    }

    [TestMethod]
    public void Constructor_WithEmptyApiKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new GeminiLlmClient(""));
    }

    [TestMethod]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new GeminiLlmClient(TestApiKey, configuration: null!));
    }
}


