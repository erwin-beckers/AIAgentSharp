using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AIAgentSharp.Gemini.Tests;

[TestClass]
public class GeminiConfigurationTests
{
    [TestMethod]
    public void Configuration_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new GeminiConfiguration();

        // Assert
        Assert.AreEqual("gemini-2.5-flash", config.Model);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(1.0f, config.TopP);
        Assert.IsNull(config.TopK);
        Assert.IsFalse(config.EnableStreaming);
        Assert.AreEqual(TimeSpan.FromMinutes(2), config.RequestTimeout);
        Assert.AreEqual(3, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(1), config.RetryDelay);
        Assert.IsTrue(config.EnableFunctionCalling);
    }

    [TestMethod]
    public void CreateForAgentReasoning_ShouldReturnOptimizedConfiguration()
    {
        // Act
        var config = GeminiConfiguration.CreateForAgentReasoning();

        // Assert
        Assert.AreEqual("gemini-2.5-flash", config.Model);
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
        var config = GeminiConfiguration.CreateForCreativeTasks();

        // Assert
        Assert.AreEqual("gemini-1.5-pro", config.Model);
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
        var config = GeminiConfiguration.CreateForCostEfficiency();

        // Assert
        Assert.AreEqual("gemini-1.0-pro", config.Model);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(2000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
        Assert.AreEqual(2, config.MaxRetries);
        Assert.AreEqual(TimeSpan.FromMinutes(1), config.RequestTimeout);
    }
}


