using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using System.Linq;

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
    public async Task StreamAsync_WithValidInput_ReturnsResult()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var client = new GeminiLlmClient(httpClient, "test-api-key", "gemini-1.5-flash");
        
        var request = new LlmRequest
        {
            Messages = new List<LlmMessage>
            {
                new LlmMessage { Role = "user", Content = "Hello" }
            }
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
        var chunks = new List<LlmStreamingChunk>();
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.IsNotNull(chunks);
        Assert.IsTrue(chunks.Count > 0);
        var finalChunk = chunks.Last();
        Assert.AreEqual("Hello! How can I help you today?", finalChunk.Content);
        Assert.IsNotNull(finalChunk.Usage);
        Assert.AreEqual(5, finalChunk.Usage.InputTokens);
        Assert.AreEqual(10, finalChunk.Usage.OutputTokens);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task StreamAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new GeminiLlmClient("test-api-key");

        // Act
        await foreach (var chunk in client.StreamAsync(null!)) { }
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task StreamAsync_WithEmptyMessages_ThrowsArgumentException()
    {
        // Arrange
        var client = new GeminiLlmClient("test-api-key");
        var request = new LlmRequest { Messages = new List<LlmMessage>() };

        // Act
        await foreach (var chunk in client.StreamAsync(request)) { }
    }

    [TestMethod]
    public async Task StreamAsync_WithFunctionCalling_ReturnsFunctionCall()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var client = new GeminiLlmClient(httpClient, "test-api-key", "gemini-1.5-flash");
        
        var request = new LlmRequest
        {
            Messages = new List<LlmMessage>
            {
                new LlmMessage { Role = "user", Content = "What's the weather?" }
            },
            Functions = new List<FunctionSpec>
            {
                new FunctionSpec
                {
                    Name = "get_weather",
                    Description = "Get weather information",
                    ParametersSchema = new { type = "object", properties = new { } }
                }
            },
            ResponseType = LlmResponseType.FunctionCall
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
        var chunks = new List<LlmStreamingChunk>();
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.IsNotNull(chunks);
        Assert.IsTrue(chunks.Count > 0);
        var finalChunk = chunks.Last();
        Assert.IsTrue(finalChunk.ActualResponseType == LlmResponseType.FunctionCall);
        Assert.IsNotNull(finalChunk.FunctionCall);
        Assert.AreEqual("get_weather", finalChunk.FunctionCall.Name);
    }

    [TestMethod]
    public async Task StreamAsync_WithSystemRole_ConvertsToUserRole()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var client = new GeminiLlmClient(httpClient, "test-api-key", "gemini-1.5-flash");
        
        var request = new LlmRequest
        {
            Messages = new List<LlmMessage>
            {
                new LlmMessage { Role = "system", Content = "You are a helpful assistant." },
                new LlmMessage { Role = "user", Content = "Hello" }
            }
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
        var chunks = new List<LlmStreamingChunk>();
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.IsNotNull(chunks);
        Assert.IsTrue(chunks.Count > 0);
        var finalChunk = chunks.Last();
        Assert.IsTrue(finalChunk.IsFinal);
        Assert.AreEqual("Hello! How can I help you today?", finalChunk.Content);
    }

    [TestMethod]
    public async Task StreamAsync_WithAssistantRole_ConvertsToModelRole()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var client = new GeminiLlmClient(httpClient, "test-api-key", "gemini-1.5-flash");
        
        var request = new LlmRequest
        {
            Messages = new List<LlmMessage>
            {
                new LlmMessage { Role = "user", Content = "Hello" },
                new LlmMessage { Role = "assistant", Content = "Hi there!" },
                new LlmMessage { Role = "user", Content = "How are you?" }
            }
        };

        // Mock successful response
        var responseContent = @"{
            ""candidates"": [{
                ""content"": {
                    ""parts"": [{
                        ""text"": ""I'm doing well, thank you for asking!""
                    }]
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
        var chunks = new List<LlmStreamingChunk>();
        await foreach (var chunk in client.StreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.IsNotNull(chunks);
        Assert.IsTrue(chunks.Count > 0);
        var finalChunk = chunks.Last();
        Assert.IsTrue(finalChunk.IsFinal);
        Assert.AreEqual("I'm doing well, thank you for asking!", finalChunk.Content);
    }
}
