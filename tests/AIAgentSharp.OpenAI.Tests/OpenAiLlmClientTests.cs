using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.OpenAI.Tests;

[TestClass]
public class OpenAiLlmClientTests
{
    private const string TestApiKey = "test-api-key";
    private const string TestModel = "gpt-4o-mini";

    [TestMethod]
    public void Constructor_WithApiKey_ShouldCreateClient()
    {
        // Act
        var client = new OpenAiLlmClient(TestApiKey);

        // Assert
        Assert.IsNotNull(client);
        Assert.AreEqual(TestModel, client.Configuration.Model);
    }

    [TestMethod]
    public void Constructor_WithApiKeyAndModel_ShouldCreateClient()
    {
        // Arrange
        const string customModel = "gpt-4o";

        // Act
        var client = new OpenAiLlmClient(TestApiKey, model: customModel);

        // Assert
        Assert.IsNotNull(client);
        Assert.AreEqual(customModel, client.Configuration.Model);
    }

    [TestMethod]
    public void Constructor_WithApiKeyAndConfiguration_ShouldCreateClient()
    {
        // Arrange
        var config = new OpenAiConfiguration
        {
            Model = "gpt-4o",
            Temperature = 0.5f,
            MaxTokens = 6000
        };

        // Act
        var client = new OpenAiLlmClient(TestApiKey, config);

        // Assert
        Assert.IsNotNull(client);
        Assert.AreEqual("gpt-4o", client.Configuration.Model);
        Assert.AreEqual(0.5f, client.Configuration.Temperature);
        Assert.AreEqual(6000, client.Configuration.MaxTokens);
    }

    [TestMethod]
    public void Constructor_WithNullApiKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new OpenAiLlmClient(null!));
    }

    [TestMethod]
    public void Constructor_WithEmptyApiKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new OpenAiLlmClient(""));
    }

    [TestMethod]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new OpenAiLlmClient(TestApiKey, configuration: null!));
    }

    [TestMethod]
    public void CreateForAgentReasoning_ShouldReturnOptimizedConfiguration()
    {
        // Act
        var config = OpenAiConfiguration.CreateForAgentReasoning();

        // Assert
        Assert.AreEqual("gpt-4o-mini", config.Model);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
        Assert.AreEqual(3, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromMinutes(2), config.RequestTimeout);
    }

    [TestMethod]
    public void CreateForCreativeTasks_ShouldReturnOptimizedConfiguration()
    {
        // Act
        var config = OpenAiConfiguration.CreateForCreativeTasks();

        // Assert
        Assert.AreEqual("gpt-4o", config.Model);
        Assert.AreEqual(0.7f, config.Temperature);
        Assert.AreEqual(6000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
        Assert.AreEqual(3, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromMinutes(3), config.RequestTimeout);
    }

    [TestMethod]
    public void CreateForCostEfficiency_ShouldReturnOptimizedConfiguration()
    {
        // Act
        var config = OpenAiConfiguration.CreateForCostEfficiency();

        // Assert
        Assert.AreEqual("gpt-3.5-turbo", config.Model);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(2000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
        Assert.AreEqual(2, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromMinutes(1), config.RequestTimeout);
    }

    [TestMethod]
    public void Configuration_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new OpenAiConfiguration();

        // Assert
        Assert.AreEqual("gpt-4o-mini", config.Model);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(1.0f, config.TopP);
        Assert.AreEqual(0.0f, config.FrequencyPenalty);
        Assert.AreEqual(0.0f, config.PresencePenalty);
        Assert.IsFalse(config.EnableStreaming);
        Assert.AreEqual(TimeSpan.FromMinutes(2), config.RequestTimeout);
        Assert.AreEqual(3, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(1), config.RetryDelay);
        Assert.IsTrue(config.EnableFunctionCalling);
        Assert.IsNull(config.OrganizationId);
        Assert.IsNull(config.ApiBaseUrl);
    }

    [TestMethod]
    public void Configuration_WithCustomValues_ShouldBeCorrect()
    {
        // Arrange
        var config = new OpenAiConfiguration
        {
            Model = "gpt-4o",
            MaxTokens = 8000,
            Temperature = 0.5f,
            TopP = 0.9f,
            FrequencyPenalty = 0.1f,
            PresencePenalty = 0.2f,
            EnableStreaming = true,
            RequestTimeout = TimeSpan.FromMinutes(5),
            MaxRetries = 5,
            RetryDelay = TimeSpan.FromSeconds(2),
            EnableFunctionCalling = false,
            OrganizationId = "org-test",
            ApiBaseUrl = "https://test.com/v1"
        };

        // Assert
        Assert.AreEqual("gpt-4o", config.Model);
        Assert.AreEqual(8000, config.MaxTokens);
        Assert.AreEqual(0.5f, config.Temperature);
        Assert.AreEqual(0.9f, config.TopP);
        Assert.AreEqual(0.1f, config.FrequencyPenalty);
        Assert.AreEqual(0.2f, config.PresencePenalty);
        Assert.IsTrue(config.EnableStreaming);
        Assert.AreEqual(TimeSpan.FromMinutes(5), config.RequestTimeout);
        Assert.AreEqual(5, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(2), config.RetryDelay);
        Assert.IsFalse(config.EnableFunctionCalling);
        Assert.AreEqual("org-test", config.OrganizationId);
        Assert.AreEqual("https://test.com/v1", config.ApiBaseUrl);
    }
}
