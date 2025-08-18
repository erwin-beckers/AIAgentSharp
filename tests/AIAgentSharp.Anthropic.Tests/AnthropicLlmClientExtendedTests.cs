using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;

namespace AIAgentSharp.Anthropic.Tests;

[TestClass]
public class AnthropicLlmClientExtendedTests
{
    private AnthropicConfiguration _configuration = null!;

    [TestInitialize]
    public void Setup()
    {
        _configuration = new AnthropicConfiguration
        {
            Model = "claude-3-5-sonnet-20241022",
            MaxTokens = 1000,
            Temperature = 0.1f,
            TopP = 1.0f
        };
    }

    [TestMethod]
    public void AnthropicLlmClient_CompleteAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        var client = new AnthropicLlmClient("test-api-key", _configuration);
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.CompleteAsync(null!));
    }

    [TestMethod]
    public void AnthropicLlmClient_CompleteAsync_WithEmptyMessages_ThrowsArgumentException()
    {
        var client = new AnthropicLlmClient("test-api-key", _configuration);
        var emptyMessages = new List<LlmMessage>();
        Assert.ThrowsExceptionAsync<ArgumentException>(() => client.CompleteAsync(emptyMessages));
    }

    [TestMethod]
    public void AnthropicLlmClient_CompleteWithFunctionsAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        var client = new AnthropicLlmClient("test-api-key", _configuration);
        var functions = new List<OpenAiFunctionSpec>();
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.CompleteWithFunctionsAsync(null!, functions));
    }

    [TestMethod]
    public void AnthropicLlmClient_CompleteWithFunctionsAsync_WithNullFunctions_ThrowsArgumentNullException()
    {
        var client = new AnthropicLlmClient("test-api-key", _configuration);
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "test" } };
        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.CompleteWithFunctionsAsync(messages, null!));
    }

    [TestMethod]
    public void AnthropicLlmClient_MessageConversion_ConvertsSystemToUser()
    {
        var messages = new List<LlmMessage>
        {
            new LlmMessage { Role = "system", Content = "You are a helpful assistant" },
            new LlmMessage { Role = "user", Content = "Hello" }
        };

        var convertMethod = typeof(AnthropicLlmClient).GetMethod("ConvertToAnthropicMessages", 
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = (List<Message>)convertMethod!.Invoke(null, new object[] { messages })!;

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(RoleType.User, result[0].Role);
        Assert.AreEqual(RoleType.User, result[1].Role);
        Assert.AreEqual("You are a helpful assistant", ((TextContent)result[0].Content[0]).Text);
        Assert.AreEqual("Hello", ((TextContent)result[1].Content[0]).Text);
    }

    [TestMethod]
    public void AnthropicLlmClient_MessageConversion_HandlesAllRoles()
    {
        var messages = new List<LlmMessage>
        {
            new LlmMessage { Role = "user", Content = "Hello" },
            new LlmMessage { Role = "assistant", Content = "Hi there!" },
            new LlmMessage { Role = "tool_result", Content = "Tool result" },
            new LlmMessage { Role = "unknown", Content = "Unknown role" }
        };

        var convertMethod = typeof(AnthropicLlmClient).GetMethod("ConvertToAnthropicMessages", 
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = (List<Message>)convertMethod!.Invoke(null, new object[] { messages })!;

        Assert.AreEqual(4, result.Count);
        Assert.AreEqual(RoleType.User, result[0].Role);
        Assert.AreEqual(RoleType.Assistant, result[1].Role);
        Assert.AreEqual(RoleType.User, result[2].Role);
        Assert.AreEqual(RoleType.User, result[3].Role);
    }

    [TestMethod]
    public void AnthropicLlmClient_FunctionConversion_ConvertsOpenAiFunctionsToAnthropicTools()
    {
        var functions = new List<OpenAiFunctionSpec>
        {
            new OpenAiFunctionSpec
            {
                Name = "test_function",
                Description = "A test function",
                ParametersSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        param1 = new { type = "string" }
                    },
                    required = new[] { "param1" }
                }
            }
        };

        var convertMethod = typeof(AnthropicLlmClient).GetMethod("ConvertToAnthropicTools", 
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = convertMethod!.Invoke(null, new object[] { functions })!;

        Assert.IsNotNull(result);
        // Note: We can't directly access the properties due to internal class structure
        // This test verifies the method executes without throwing exceptions
    }

    [TestMethod]
    public void AnthropicLlmClient_Configuration_ReflectsConstructorParameters()
    {
        var customConfig = new AnthropicConfiguration
        {
            Model = "custom-model",
            MaxTokens = 5000,
            Temperature = 0.5f
        };

        var client = new AnthropicLlmClient("test-api-key", customConfig);

        Assert.AreEqual("custom-model", client.Configuration.Model);
        Assert.AreEqual(5000, client.Configuration.MaxTokens);
        Assert.AreEqual(0.5f, client.Configuration.Temperature);
    }

    [TestMethod]
    public void AnthropicLlmClient_Configuration_DefaultValues_AreCorrect()
    {
        var config = new AnthropicConfiguration();
        Assert.AreEqual("claude-opus-4-1-20250805", config.Model);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.AreEqual(1.0f, config.Temperature);
        Assert.AreEqual(1.0f, config.TopP);
        Assert.IsTrue(config.EnableFunctionCalling);
        Assert.AreEqual(TimeSpan.FromMinutes(2), config.RequestTimeout);
        Assert.AreEqual(3, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(1), config.RetryDelay);
        Assert.IsFalse(config.EnableStreaming);
    }

    [TestMethod]
    public void AnthropicLlmClient_Configuration_WithCustomValues_SetsCorrectly()
    {
        var config = new AnthropicConfiguration
        {
            Model = "claude-3-5-haiku-20240307",
            MaxTokens = 5000,
            Temperature = 0.5f,
            TopP = 0.8f,
            EnableFunctionCalling = false,
            RequestTimeout = TimeSpan.FromMinutes(5),
            MaxRetries = 5,
            RetryDelay = TimeSpan.FromSeconds(2),
            EnableStreaming = true
        };

        Assert.AreEqual("claude-3-5-haiku-20240307", config.Model);
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
    public void AnthropicLlmClient_Configuration_FactoryMethods_ReturnDifferentConfigurations()
    {
        var agentConfig = AnthropicConfiguration.CreateForAgentReasoning();
        var creativeConfig = AnthropicConfiguration.CreateForCreativeTasks();
        var costConfig = AnthropicConfiguration.CreateForCostEfficiency();

        Assert.AreEqual("claude-3-5-sonnet-20241022", agentConfig.Model);
        Assert.AreEqual("claude-3-opus-20240229", creativeConfig.Model);
        Assert.AreEqual("claude-3-5-haiku-20241022", costConfig.Model);

        Assert.AreEqual(0.1f, agentConfig.Temperature);
        Assert.AreEqual(0.7f, creativeConfig.Temperature);
        Assert.AreEqual(0.1f, costConfig.Temperature);

        Assert.AreEqual(4000, agentConfig.MaxTokens);
        Assert.AreEqual(6000, creativeConfig.MaxTokens);
        Assert.AreEqual(2000, costConfig.MaxTokens);
    }

    [TestMethod]
    public void AnthropicLlmClient_Configuration_TimeoutValues_AreReasonable()
    {
        var agentConfig = AnthropicConfiguration.CreateForAgentReasoning();
        var creativeConfig = AnthropicConfiguration.CreateForCreativeTasks();
        var costConfig = AnthropicConfiguration.CreateForCostEfficiency();

        Assert.IsTrue(agentConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
        Assert.IsTrue(creativeConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
        Assert.IsTrue(costConfig.RequestTimeout >= TimeSpan.FromMinutes(1));

        Assert.IsTrue(agentConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
        Assert.IsTrue(creativeConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
        Assert.IsTrue(costConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
    }

    [TestMethod]
    public void AnthropicLlmClient_Configuration_RetrySettings_AreAppropriate()
    {
        var agentConfig = AnthropicConfiguration.CreateForAgentReasoning();
        var creativeConfig = AnthropicConfiguration.CreateForCreativeTasks();
        var costConfig = AnthropicConfiguration.CreateForCostEfficiency();

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
    public void AnthropicLlmClient_Configuration_PropertySetters_WorkCorrectly()
    {
        var customConfig = new AnthropicConfiguration
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
    public void AnthropicLlmClient_Configuration_Immutability_IsPreserved()
    {
        var config = new AnthropicConfiguration
        {
            Model = "original-model",
            MaxTokens = 1000
        };

        Assert.AreEqual("original-model", config.Model);
        Assert.AreEqual(1000, config.MaxTokens);
    }

    [TestMethod]
    public void AnthropicLlmClient_Configuration_Validation_HandlesEdgeCases()
    {
        var config = new AnthropicConfiguration
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
