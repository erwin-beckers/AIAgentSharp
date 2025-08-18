using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AIAgentSharp.Gemini.Tests;

[TestClass]
public class GeminiLlmClientTests
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
            TopP = 1.0f
        };
    }

    [TestMethod]
    public void Constructor_WithValidApiKey_CreatesInstance()
    {
        // Act & Assert
        Assert.IsNotNull(new GeminiLlmClient("test-api-key"));
    }

    [TestMethod]
    public void Constructor_WithValidApiKeyAndModel_CreatesInstance()
    {
        // Act & Assert
        Assert.IsNotNull(new GeminiLlmClient("test-api-key", "gemini-1.5-pro"));
    }

    [TestMethod]
    public void Constructor_WithValidApiKeyAndConfiguration_CreatesInstance()
    {
        // Act & Assert
        Assert.IsNotNull(new GeminiLlmClient("test-api-key", _configuration));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Act
        _ = new GeminiLlmClient(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_WithEmptyApiKey_ThrowsArgumentNullException()
    {
        // Act
        _ = new GeminiLlmClient("");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        _ = new GeminiLlmClient("test-api-key", (GeminiConfiguration)null!);
    }

    [TestMethod]
    public void GeminiConfiguration_Create_ReturnsValidConfiguration()
    {
        // Act
        var config = GeminiConfiguration.CreateForAgentReasoning();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("gemini-1.5-flash", config.Model);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
    }

    [TestMethod]
    public void GeminiConfiguration_CreateForAgentReasoning_ReturnsValidConfiguration()
    {
        // Act
        var config = GeminiConfiguration.CreateForAgentReasoning();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("gemini-1.5-flash", config.Model);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
    }

    [TestMethod]
    public void GeminiConfiguration_CreateForCreativeTasks_ReturnsValidConfiguration()
    {
        // Act
        var config = GeminiConfiguration.CreateForCreativeTasks();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("gemini-1.5-pro", config.Model);
        Assert.AreEqual(0.7f, config.Temperature);
        Assert.AreEqual(6000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
    }

    [TestMethod]
    public void GeminiConfiguration_CreateForCostEfficiency_ReturnsValidConfiguration()
    {
        // Act
        var config = GeminiConfiguration.CreateForCostEfficiency();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("gemini-1.0-pro", config.Model);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(2000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
    }
}
