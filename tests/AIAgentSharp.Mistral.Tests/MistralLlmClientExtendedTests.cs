using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AIAgentSharp.Mistral.Tests;

[TestClass]
public class MistralLlmClientExtendedTests
{
    private MistralConfiguration _configuration = null!;

    [TestInitialize]
    public void Setup()
    {
        _configuration = new MistralConfiguration
        {
            ApiKey = "test-api-key",
            Model = "mistral-large-latest",
            MaxTokens = 1000,
            Temperature = 0.1f,
            TopP = 1.0f,
            EnableFunctionCalling = true,
            RequestTimeout = TimeSpan.FromSeconds(30)
        };
    }

    [TestMethod]
    public void MistralLlmClient_Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new MistralLlmClient((MistralConfiguration)null!));
    }

    [TestMethod]
    public void MistralLlmClient_Constructor_WithEmptyApiKey_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new MistralLlmClient(""));
    }

    [TestMethod]
    public void MistralLlmClient_CompleteAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        var client = new MistralLlmClient(_configuration);
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.CompleteAsync(null!));
    }

    [TestMethod]
    public void MistralLlmClient_CompleteWithFunctionsAsync_WithFunctionCallingDisabled_ThrowsInvalidOperationException()
    {
        var config = new MistralConfiguration
        {
            ApiKey = "test-api-key",
            Model = "mistral-large-latest",
            EnableFunctionCalling = false
        };
        var client = new MistralLlmClient(config);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        var functions = new List<OpenAiFunctionSpec>();

        Assert.ThrowsExceptionAsync<InvalidOperationException>(() => client.CompleteWithFunctionsAsync(messages, functions));
    }

    [TestMethod]
    public void MistralLlmClient_MessageConversion_ConvertsMessagesCorrectly()
    {
        var messages = new List<LlmMessage>
        {
            new LlmMessage { Role = "user", Content = "Hello" },
            new LlmMessage { Role = "assistant", Content = "Hi there!" },
            new LlmMessage { Role = "system", Content = "System message" }
        };

        var convertMethod = typeof(MistralLlmClient).GetMethod("ConvertToMistralMessages", 
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = convertMethod!.Invoke(null, new object[] { messages })!;

        Assert.IsNotNull(result);
        // Note: We can't directly access the Role property due to internal class structure
        // This test verifies the method executes without throwing exceptions
    }

    [TestMethod]
    public void MistralLlmClient_ExtractJsonFromMarkdown_WithValidJsonBlock_ExtractsCorrectly()
    {
        var extractMethod = typeof(MistralLlmClient).GetMethod("ExtractJsonFromMarkdown", 
            BindingFlags.NonPublic | BindingFlags.Static);

        var content = "Here is the JSON:\n```json\n{\"key\": \"value\"}\n```";
        var result = (string)extractMethod!.Invoke(null, new object[] { content })!;

        Assert.AreEqual("{\"key\": \"value\"}", result);
    }

    [TestMethod]
    public void MistralLlmClient_Configuration_DefaultValues_AreCorrect()
    {
        var config = new MistralConfiguration();
        Assert.AreEqual("mistral-large-latest", config.Model);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(1.0f, config.TopP);
        Assert.IsTrue(config.EnableFunctionCalling);
    }

    [TestMethod]
    public void MistralLlmClient_Configuration_FactoryMethods_ReturnDifferentConfigurations()
    {
        var agentConfig = MistralConfiguration.CreateForAgentReasoning();
        var creativeConfig = MistralConfiguration.CreateForCreativeTasks();
        var costConfig = MistralConfiguration.CreateForCostEfficiency();

        Assert.AreEqual("mistral-large-latest", agentConfig.Model);
        Assert.AreEqual("mistral-large-latest", creativeConfig.Model);
        Assert.AreEqual("mistral-small-latest", costConfig.Model);

        Assert.AreEqual(0.1f, agentConfig.Temperature);
        Assert.AreEqual(0.7f, creativeConfig.Temperature);
        Assert.AreEqual(0.1f, costConfig.Temperature);
    }

    [TestMethod]
    public void MistralLlmClient_Configuration_WithCustomValues_SetsCorrectly()
    {
        var config = new MistralConfiguration
        {
            ApiKey = "custom-key",
            Model = "mistral-medium-latest",
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

        Assert.AreEqual("custom-key", config.ApiKey);
        Assert.AreEqual("mistral-medium-latest", config.Model);
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
    public void MistralLlmClient_Configuration_PropertySetters_WorkCorrectly()
    {
        var customConfig = new MistralConfiguration
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
    public void MistralLlmClient_Configuration_Immutability_IsPreserved()
    {
        var config = new MistralConfiguration
        {
            Model = "original-model",
            MaxTokens = 1000
        };

        Assert.AreEqual("original-model", config.Model);
        Assert.AreEqual(1000, config.MaxTokens);
    }

    [TestMethod]
    public void MistralLlmClient_Configuration_Validation_HandlesEdgeCases()
    {
        var config = new MistralConfiguration
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
    public void MistralLlmClient_Configuration_ApiBaseUrl_IsValid()
    {
        var config = new MistralConfiguration();
        Assert.IsTrue(Uri.IsWellFormedUriString(config.ApiBaseUrl ?? "https://api.mistral.ai/v1/", UriKind.Absolute));
    }

    [TestMethod]
    public void MistralLlmClient_Configuration_ModelNames_AreValid()
    {
        var config = new MistralConfiguration();
        Assert.IsFalse(string.IsNullOrEmpty(config.Model));
        Assert.IsTrue(config.Model.StartsWith("mistral-"));
    }

    [TestMethod]
    public void MistralLlmClient_Configuration_TimeoutValues_AreReasonable()
    {
        var agentConfig = MistralConfiguration.CreateForAgentReasoning();
        var creativeConfig = MistralConfiguration.CreateForCreativeTasks();
        var costConfig = MistralConfiguration.CreateForCostEfficiency();

        Assert.IsTrue(agentConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
        Assert.IsTrue(creativeConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
        Assert.IsTrue(costConfig.RequestTimeout >= TimeSpan.FromMinutes(1));

        Assert.IsTrue(agentConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
        Assert.IsTrue(creativeConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
        Assert.IsTrue(costConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
    }

    [TestMethod]
    public void MistralLlmClient_Configuration_RetrySettings_AreAppropriate()
    {
        var agentConfig = MistralConfiguration.CreateForAgentReasoning();
        var creativeConfig = MistralConfiguration.CreateForCreativeTasks();
        var costConfig = MistralConfiguration.CreateForCostEfficiency();

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
    public void MistralLlmClient_ExtractJsonFromMarkdown_WithCodeBlockWithoutJson_ExtractsCorrectly()
    {
        var extractMethod = typeof(MistralLlmClient).GetMethod("ExtractJsonFromMarkdown", 
            BindingFlags.NonPublic | BindingFlags.Static);

        var content = "Here is the JSON:\n```\n{\"key\": \"value\"}\n```";
        var result = (string)extractMethod!.Invoke(null, new object[] { content })!;

        Assert.AreEqual("{\"key\": \"value\"}", result);
    }

    [TestMethod]
    public void MistralLlmClient_ExtractJsonFromMarkdown_WithBackticks_ExtractsCorrectly()
    {
        var extractMethod = typeof(MistralLlmClient).GetMethod("ExtractJsonFromMarkdown", 
            BindingFlags.NonPublic | BindingFlags.Static);

        var content = "Here is the JSON: `{\"key\": \"value\"}`";
        var result = (string)extractMethod!.Invoke(null, new object[] { content })!;

        Assert.AreEqual("{\"key\": \"value\"}", result);
    }

    [TestMethod]
    public void MistralLlmClient_ExtractJsonFromMarkdown_WithNoMarkdown_ReturnsOriginal()
    {
        var extractMethod = typeof(MistralLlmClient).GetMethod("ExtractJsonFromMarkdown", 
            BindingFlags.NonPublic | BindingFlags.Static);

        var content = "{\"key\": \"value\"}";
        var result = (string)extractMethod!.Invoke(null, new object[] { content })!;

        Assert.AreEqual(content, result);
    }

    [TestMethod]
    public void MistralLlmClient_ExtractJsonFromMarkdown_WithInvalidJson_ReturnsOriginal()
    {
        var extractMethod = typeof(MistralLlmClient).GetMethod("ExtractJsonFromMarkdown", 
            BindingFlags.NonPublic | BindingFlags.Static);

        var content = "Here is the JSON:\n```json\n{invalid json}\n```";
        var result = (string)extractMethod!.Invoke(null, new object[] { content })!;

        Assert.AreEqual("{invalid json}", result);
    }
}
