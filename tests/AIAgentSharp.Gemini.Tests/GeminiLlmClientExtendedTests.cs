using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace AIAgentSharp.Gemini.Tests;

[TestClass]
public class GeminiLlmClientExtendedTests
{
    private GeminiConfiguration _configuration = null!;

    [TestInitialize]
    public void Setup()
    {
        _configuration = new GeminiConfiguration
        {
            Model = "gemini-1.5-flash",
            MaxTokens = 1000,
            Temperature = 0.1f,
            TopP = 1.0f,
            EnableFunctionCalling = true,
            RequestTimeout = TimeSpan.FromSeconds(30)
        };
    }

    [TestMethod]
    public void GeminiLlmClient_Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new GeminiLlmClient("test-api-key", (GeminiConfiguration)null!));
    }

    [TestMethod]
    public void GeminiLlmClient_Constructor_WithEmptyApiKey_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new GeminiLlmClient(""));
    }

    [TestMethod]
    public void GeminiLlmClient_CompleteAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        var client = new GeminiLlmClient("test-api-key", _configuration);
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.CompleteAsync(null!));
    }

    [TestMethod]
    public void GeminiLlmClient_CompleteWithFunctionsAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        var client = new GeminiLlmClient("test-api-key", _configuration);
        var functions = new List<OpenAiFunctionSpec>();
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.CompleteWithFunctionsAsync(null!, functions));
    }

    [TestMethod]
    public void GeminiLlmClient_CompleteWithFunctionsAsync_WithNullFunctions_ThrowsArgumentNullException()
    {
        var client = new GeminiLlmClient("test-api-key", _configuration);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.CompleteWithFunctionsAsync(messages, null!));
    }

    [TestMethod]
    public void GeminiLlmClient_Configuration_DefaultValues_AreCorrect()
    {
        var config = new GeminiConfiguration();
        Assert.AreEqual("gemini-1.5-flash", config.Model);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(1.0f, config.TopP);
        Assert.IsTrue(config.EnableFunctionCalling);
        Assert.AreEqual(TimeSpan.FromMinutes(2), config.RequestTimeout);
        Assert.AreEqual(3, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(1), config.RetryDelay);
        Assert.IsFalse(config.EnableStreaming);
    }

    [TestMethod]
    public void GeminiLlmClient_Configuration_WithCustomValues_SetsCorrectly()
    {
        var config = new GeminiConfiguration
        {
            Model = "gemini-1.5-pro",
            MaxTokens = 5000,
            Temperature = 0.5f,
            TopP = 0.8f,
            EnableFunctionCalling = false,
            RequestTimeout = TimeSpan.FromMinutes(5),
            MaxRetries = 5,
            RetryDelay = TimeSpan.FromSeconds(2),
            EnableStreaming = true
        };

        Assert.AreEqual("gemini-1.5-pro", config.Model);
        Assert.AreEqual(5000, config.MaxTokens);
        Assert.AreEqual(0.5f, config.Temperature);
        Assert.AreEqual(0.8f, config.TopP);
        Assert.IsFalse(config.EnableFunctionCalling);
        Assert.AreEqual(TimeSpan.FromMinutes(5), config.RequestTimeout);
        Assert.AreEqual(5, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(2), config.RetryDelay);
        Assert.IsTrue(config.EnableStreaming);
    }

    [TestMethod]
    public void GeminiLlmClient_Configuration_FactoryMethods_ReturnDifferentConfigurations()
    {
        var agentConfig = GeminiConfiguration.CreateForAgentReasoning();
        var creativeConfig = GeminiConfiguration.CreateForCreativeTasks();
        var costConfig = GeminiConfiguration.CreateForCostEfficiency();

        Assert.AreEqual("gemini-1.5-flash", agentConfig.Model);
        Assert.AreEqual("gemini-1.5-pro", creativeConfig.Model);
        Assert.AreEqual("gemini-1.0-pro", costConfig.Model);

        Assert.AreEqual(0.1f, agentConfig.Temperature);
        Assert.AreEqual(0.7f, creativeConfig.Temperature);
        Assert.AreEqual(0.1f, costConfig.Temperature);

        Assert.AreEqual(4000, agentConfig.MaxTokens);
        Assert.AreEqual(6000, creativeConfig.MaxTokens);
        Assert.AreEqual(2000, costConfig.MaxTokens);
    }

    [TestMethod]
    public void GeminiLlmClient_Configuration_TimeoutValues_AreReasonable()
    {
        var agentConfig = GeminiConfiguration.CreateForAgentReasoning();
        var creativeConfig = GeminiConfiguration.CreateForCreativeTasks();
        var costConfig = GeminiConfiguration.CreateForCostEfficiency();

        Assert.IsTrue(agentConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
        Assert.IsTrue(creativeConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
        Assert.IsTrue(costConfig.RequestTimeout >= TimeSpan.FromMinutes(1));

        Assert.IsTrue(agentConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
        Assert.IsTrue(creativeConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
        Assert.IsTrue(costConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
    }

    [TestMethod]
    public void GeminiLlmClient_Configuration_RetrySettings_AreAppropriate()
    {
        var agentConfig = GeminiConfiguration.CreateForAgentReasoning();
        var creativeConfig = GeminiConfiguration.CreateForCreativeTasks();
        var costConfig = GeminiConfiguration.CreateForCostEfficiency();

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
    public void GeminiLlmClient_Configuration_PropertySetters_WorkCorrectly()
    {
        var customConfig = new GeminiConfiguration
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
    public void GeminiLlmClient_Configuration_Immutability_IsPreserved()
    {
        var config = new GeminiConfiguration
        {
            Model = "original-model",
            MaxTokens = 1000
        };

        Assert.AreEqual("original-model", config.Model);
        Assert.AreEqual(1000, config.MaxTokens);
    }

    [TestMethod]
    public void GeminiLlmClient_Configuration_Validation_HandlesEdgeCases()
    {
        var config = new GeminiConfiguration
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

    [TestMethod]
    public void GeminiLlmClient_Configuration_ModelNames_AreValid()
    {
        var config = new GeminiConfiguration();
        Assert.IsFalse(string.IsNullOrEmpty(config.Model));
        Assert.IsTrue(config.Model.StartsWith("gemini-"));
    }
}
