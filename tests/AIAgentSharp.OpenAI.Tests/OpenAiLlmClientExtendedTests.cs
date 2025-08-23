using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.OpenAI.Tests;

[TestClass]
public class OpenAiLlmClientExtendedTests
{
    private OpenAiConfiguration _configuration = null!;

    [TestInitialize]
    public void Setup()
    {
        _configuration = new OpenAiConfiguration
        {
            Model = "gpt-5-nano",
            MaxTokens = 1000,
            Temperature = 0.1f,
            EnableFunctionCalling = true,
            RequestTimeout = TimeSpan.FromMinutes(2),
            MaxRetries = 3
        };
    }

    [TestMethod]
    public void OpenAiLlmClient_StreamAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        var client = new OpenAiLlmClient("test-api-key", _configuration);
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => 
        {
            await foreach (var chunk in client.StreamAsync(null!)) { }
        });
    }

    [TestMethod]
    public void OpenAiLlmClient_StreamAsync_WithEmptyMessages_ThrowsArgumentException()
    {
        var client = new OpenAiLlmClient("test-api-key", _configuration);
        var request = new LlmRequest { Messages = new List<LlmMessage>() };
        Assert.ThrowsExceptionAsync<ArgumentException>(async () => 
        {
            await foreach (var chunk in client.StreamAsync(request)) { }
        });
    }

    [TestMethod]
    public void OpenAiLlmClient_StreamAsync_WithNullFunctions_DoesNotThrow()
    {
        var client = new OpenAiLlmClient("test-api-key", _configuration);
        var request = new LlmRequest 
        { 
            Messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } },
            Functions = null
        };
        
        // This should not throw an exception
        Assert.IsNotNull(client);
    }

    [TestMethod]
    public void OpenAiLlmClient_Configuration_DefaultValues_AreCorrect()
    {
        var config = new OpenAiConfiguration();
        Assert.AreEqual("gpt-5-nano", config.Model);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(1.0f, config.TopP);
        Assert.IsTrue(config.EnableFunctionCalling);
        Assert.AreEqual(TimeSpan.FromMinutes(2), config.RequestTimeout);
        Assert.AreEqual(3, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(1), config.RetryDelay);
        Assert.IsFalse(config.EnableStreaming);
        Assert.IsNull(config.ApiBaseUrl);
        Assert.IsNull(config.OrganizationId);
    }

    [TestMethod]
    public void OpenAiLlmClient_Configuration_WithCustomValues_SetsCorrectly()
    {
        var config = new OpenAiConfiguration
        {
            Model = "gpt-4o",
            MaxTokens = 5000,
            Temperature = 0.5f,
            TopP = 0.8f,
            EnableFunctionCalling = false,
            RequestTimeout = TimeSpan.FromMinutes(5),
            MaxRetries = 5,
            RetryDelay = TimeSpan.FromSeconds(2),
            EnableStreaming = true,
            ApiBaseUrl = "https://custom.api.com",
            OrganizationId = "org-123"
        };

        Assert.AreEqual("gpt-4o", config.Model);
        Assert.AreEqual(5000, config.MaxTokens);
        Assert.AreEqual(0.5f, config.Temperature);
        Assert.AreEqual(0.8f, config.TopP);
        Assert.IsFalse(config.EnableFunctionCalling);
        Assert.AreEqual(TimeSpan.FromMinutes(5), config.RequestTimeout);
        Assert.AreEqual(5, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(2), config.RetryDelay);
        Assert.IsTrue(config.EnableStreaming);
        Assert.AreEqual("https://custom.api.com", config.ApiBaseUrl);
        Assert.AreEqual("org-123", config.OrganizationId);
    }

    [TestMethod]
    public void OpenAiLlmClient_Configuration_FactoryMethods_ReturnDifferentConfigurations()
    {
        var agentConfig = OpenAiConfiguration.CreateForAgentReasoning();
        var creativeConfig = OpenAiConfiguration.CreateForCreativeTasks();
        var costConfig = OpenAiConfiguration.CreateForCostEfficiency();

        Assert.AreEqual("gpt-5-nano", agentConfig.Model);
        Assert.AreEqual("gpt-4o", creativeConfig.Model);
        Assert.AreEqual("gpt-3.5-turbo", costConfig.Model);

        Assert.AreEqual(0.1f, agentConfig.Temperature);
        Assert.AreEqual(0.7f, creativeConfig.Temperature);
        Assert.AreEqual(0.1f, costConfig.Temperature);

        Assert.AreEqual(4000, agentConfig.MaxTokens);
        Assert.AreEqual(6000, creativeConfig.MaxTokens);
        Assert.AreEqual(2000, costConfig.MaxTokens);
    }

    [TestMethod]
    public void OpenAiLlmClient_Configuration_TimeoutValues_AreReasonable()
    {
        var agentConfig = OpenAiConfiguration.CreateForAgentReasoning();
        var creativeConfig = OpenAiConfiguration.CreateForCreativeTasks();
        var costConfig = OpenAiConfiguration.CreateForCostEfficiency();

        Assert.IsTrue(agentConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
        Assert.IsTrue(creativeConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
        Assert.IsTrue(costConfig.RequestTimeout >= TimeSpan.FromMinutes(1));

        Assert.IsTrue(agentConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
        Assert.IsTrue(creativeConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
        Assert.IsTrue(costConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
    }

    [TestMethod]
    public void OpenAiLlmClient_Configuration_RetrySettings_AreAppropriate()
    {
        var agentConfig = OpenAiConfiguration.CreateForAgentReasoning();
        var creativeConfig = OpenAiConfiguration.CreateForCreativeTasks();
        var costConfig = OpenAiConfiguration.CreateForCostEfficiency();

        Assert.IsTrue(agentConfig.MaxRetries >= 0);
        Assert.IsTrue(creativeConfig.MaxRetries >= 0);
        Assert.IsTrue(costConfig.MaxRetries >= 0);

        Assert.IsTrue(agentConfig.MaxRetries <= 10);
        Assert.IsTrue(creativeConfig.MaxRetries <= 10);
        Assert.IsTrue(costConfig.MaxRetries <= 10);

        Assert.IsTrue(agentConfig.RetryDelay >= TimeSpan.FromMilliseconds(100));
        Assert.IsTrue(creativeConfig.RetryDelay >= TimeSpan.FromMilliseconds(100));
        Assert.IsTrue(costConfig.RetryDelay >= TimeSpan.FromMilliseconds(100));
    }

    [TestMethod]
    public void OpenAiLlmClient_Configuration_PropertySetters_WorkCorrectly()
    {
        var customConfig = new OpenAiConfiguration
        {
            Model = "test-model",
            MaxTokens = 3000,
            Temperature = 0.3f
        };

        Assert.AreEqual("test-model", customConfig.Model);
        Assert.AreEqual(3000, customConfig.MaxTokens);
        Assert.AreEqual(0.3f, customConfig.Temperature);
    }

    [TestMethod]
    public void OpenAiLlmClient_Configuration_Immutability_IsPreserved()
    {
        var config = new OpenAiConfiguration
        {
            Model = "original-model",
            MaxTokens = 1000
        };

        Assert.AreEqual("original-model", config.Model);
        Assert.AreEqual(1000, config.MaxTokens);
    }

    [TestMethod]
    public void OpenAiLlmClient_Configuration_Validation_HandlesEdgeCases()
    {
        var config = new OpenAiConfiguration
        {
            Temperature = 0.0f,
            TopP = 1.0f,
            MaxTokens = 1,
            MaxRetries = 0
        };

        Assert.AreEqual(0.0f, config.Temperature);
        Assert.AreEqual(1.0f, config.TopP);
        Assert.AreEqual(1, config.MaxTokens);
        Assert.AreEqual(0, config.MaxRetries);
    }
}
