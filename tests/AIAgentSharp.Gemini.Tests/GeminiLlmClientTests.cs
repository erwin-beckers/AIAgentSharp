using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

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
    public async Task CompleteAsync_WithValidMessages_ReturnsResult()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var client = new GeminiLlmClient(httpClient, "test-api-key", "gemini-1.5-flash");
        
        var messages = new List<LlmMessage>
        {
            new LlmMessage { Role = "user", Content = "Hello" }
        };

        // Mock successful response
        var responseContent = @"{
            ""candidates"": [{
                ""content"": {
                    ""parts"": [{
                        ""text"": ""Hello! How can I help you today?""
                    }]
                }
            }],
            ""usageMetadata"": {
                ""promptTokenCount"": 5,
                ""candidatesTokenCount"": 10
            }
        }";

        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await client.CompleteAsync(messages);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello! How can I help you today?", result.Content);
        Assert.IsNotNull(result.Usage);
        Assert.AreEqual(5, result.Usage.InputTokens);
        Assert.AreEqual(10, result.Usage.OutputTokens);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task CompleteAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new GeminiLlmClient("test-api-key");

        // Act
        await client.CompleteAsync(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CompleteAsync_WithEmptyMessages_ThrowsArgumentException()
    {
        // Arrange
        var client = new GeminiLlmClient("test-api-key");
        var messages = new List<LlmMessage>();

        // Act
        await client.CompleteAsync(messages);
    }

    [TestMethod]
    public async Task CompleteWithFunctionsAsync_WithValidInput_ReturnsResult()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var client = new GeminiLlmClient(httpClient, "test-api-key", "gemini-1.5-flash");
        
        var messages = new List<LlmMessage>
        {
            new LlmMessage { Role = "user", Content = "What's the weather?" }
        };

        var functions = new List<OpenAiFunctionSpec>
        {
            new OpenAiFunctionSpec
            {
                Name = "get_weather",
                Description = "Get weather information",
                ParametersSchema = new { type = "object", properties = new { } }
            }
        };

        // Mock successful response
        var responseContent = @"{
            ""candidates"": [{
                ""content"": {
                    ""parts"": [{
                        ""text"": ""I'll get the weather for you.""
                    }]
                },
                ""functionCall"": {
                    ""name"": ""get_weather"",
                    ""args"": {
                        ""location"": ""New York""
                    }
                }
            }],
            ""usageMetadata"": {
                ""promptTokenCount"": 10,
                ""candidatesTokenCount"": 5
            }
        }";

        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await client.CompleteWithFunctionsAsync(messages, functions);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasFunctionCall);
        Assert.AreEqual("get_weather", result.FunctionName);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task CompleteWithFunctionsAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new GeminiLlmClient("test-api-key");
        var functions = new List<OpenAiFunctionSpec>();

        // Act
        await client.CompleteWithFunctionsAsync(null!, functions);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task CompleteWithFunctionsAsync_WithNullFunctions_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new GeminiLlmClient("test-api-key");
        var messages = new List<LlmMessage> { new LlmMessage { Role = "user", Content = "Hello" } };

        // Act
        await client.CompleteWithFunctionsAsync(messages, null!);
    }
}
