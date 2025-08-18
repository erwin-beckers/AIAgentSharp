using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace AIAgentSharp.Mistral.Tests;

[TestClass]
public class MistralLlmClientTests
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
    public void Constructor_WithConfiguration_SetsProperties()
    {
        // Act
        var client = new MistralLlmClient(_configuration);

        // Assert
        Assert.IsNotNull(client);
    }

    [TestMethod]
    public void Constructor_WithApiKey_CreatesConfiguration()
    {
        // Act
        var client = new MistralLlmClient("test-api-key");

        // Assert
        Assert.IsNotNull(client);
    }

    [TestMethod]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new MistralLlmClient((MistralConfiguration)null!));
    }

    [TestMethod]
    public void Constructor_WithEmptyApiKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new MistralLlmClient(""));
    }

    [TestMethod]
    public void MistralConfiguration_Create_ReturnsValidConfiguration()
    {
        // Act
        var config = MistralConfiguration.Create("test-api-key");

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("test-api-key", config.ApiKey);
        Assert.AreEqual("mistral-large-latest", config.Model);
    }

    [TestMethod]
    public void MistralConfiguration_CreateForAgentReasoning_ReturnsValidConfiguration()
    {
        // Act
        var config = MistralConfiguration.CreateForAgentReasoning();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("mistral-large-latest", config.Model);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(4000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
    }

    [TestMethod]
    public void MistralConfiguration_CreateForCreativeTasks_ReturnsValidConfiguration()
    {
        // Act
        var config = MistralConfiguration.CreateForCreativeTasks();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("mistral-large-latest", config.Model);
        Assert.AreEqual(0.7f, config.Temperature);
        Assert.AreEqual(6000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
    }

    [TestMethod]
    public void MistralConfiguration_CreateForCostEfficiency_ReturnsValidConfiguration()
    {
        // Act
        var config = MistralConfiguration.CreateForCostEfficiency();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("mistral-small-latest", config.Model);
        Assert.AreEqual(0.1f, config.Temperature);
        Assert.AreEqual(2000, config.MaxTokens);
        Assert.IsTrue(config.EnableFunctionCalling);
    }

    [TestMethod]
    public void ParseFunctionCallFromContent_WithMarkdownWrappedJson_ExtractsCorrectly()
    {
        // Arrange
        var client = new MistralLlmClient(_configuration);
        var method = typeof(MistralLlmClient).GetMethod("ParseFunctionCallFromContent", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        var jsonContent = @"```json
{
  ""action"": ""tool_call"",
  ""tool_name"": ""search_flights"",
  ""parameters"": {
    ""departureDate"": ""2023-11-15"",
    ""destination"": ""CDG"",
    ""origin"": ""JFK"",
    ""passengers"": 2,
    ""returnDate"": ""2023-11-18""
  },
  ""status_title"": ""Searching Flights to Paris"",
  ""status_details"": ""Looking for round-trip flights from JFK to CDG for two passengers."",
  ""next_step_hint"": ""Find suitable flights for the trip"",
  ""progress_pct"": 10
}
```";

        // Act
        var result = method!.Invoke(null, new object[] { jsonContent }) as (string FunctionName, string Arguments)?;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("search_flights", result.Value.FunctionName);
        Assert.IsTrue(result.Value.Arguments.Contains("departureDate"));
        Assert.IsTrue(result.Value.Arguments.Contains("CDG"));
    }

    [TestMethod]
    public void ParseFunctionCallFromContent_WithPlainJson_ExtractsCorrectly()
    {
        // Arrange
        var client = new MistralLlmClient(_configuration);
        var method = typeof(MistralLlmClient).GetMethod("ParseFunctionCallFromContent", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        var jsonContent = @"{
  ""action"": ""tool_call"",
  ""tool_name"": ""search_flights"",
  ""parameters"": {
    ""departureDate"": ""2023-11-15"",
    ""destination"": ""CDG"",
    ""origin"": ""JFK"",
    ""passengers"": 2,
    ""returnDate"": ""2023-11-18""
  },
  ""status_title"": ""Searching Flights to Paris"",
  ""status_details"": ""Looking for round-trip flights from JFK to CDG for two passengers."",
  ""next_step_hint"": ""Find suitable flights for the trip"",
  ""progress_pct"": 10
}";

        // Act
        var result = method!.Invoke(null, new object[] { jsonContent }) as (string FunctionName, string Arguments)?;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("search_flights", result.Value.FunctionName);
        Assert.IsTrue(result.Value.Arguments.Contains("departureDate"));
        Assert.IsTrue(result.Value.Arguments.Contains("CDG"));
    }

    [TestMethod]
    public void ParseFunctionCallFromContent_WithExactUserContent_ExtractsCorrectly()
    {
        // Arrange
        var client = new MistralLlmClient(_configuration);
        var method = typeof(MistralLlmClient).GetMethod("ParseFunctionCallFromContent", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        // This is the exact content format that the user is experiencing issues with
        var jsonContent = @"```json
{
  ""action"": ""tool_call"",
  ""tool_name"": ""search_flights"",
  ""parameters"": {
    ""departureDate"": ""2023-11-15"",
    ""destination"": ""CDG"",
    ""origin"": ""JFK"",
    ""passengers"": 2,
    ""returnDate"": ""2023-11-18""
  },
  ""status_title"": ""Searching Flights to Paris"",
  ""status_details"": ""Looking for round-trip flights from JFK to CDG for two passengers."",
  ""next_step_hint"": ""Find suitable flights for the trip"",
  ""progress_pct"": 10
}
```";

        // Act
        var result = method!.Invoke(null, new object[] { jsonContent }) as (string FunctionName, string Arguments)?;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("search_flights", result.Value.FunctionName);
        Assert.IsTrue(result.Value.Arguments.Contains("departureDate"));
        Assert.IsTrue(result.Value.Arguments.Contains("CDG"));
        Assert.IsTrue(result.Value.Arguments.Contains("JFK"));
        Assert.IsTrue(result.Value.Arguments.Contains("2"));
        Assert.IsTrue(result.Value.Arguments.Contains("2023-11-15"));
        Assert.IsTrue(result.Value.Arguments.Contains("2023-11-18"));
    }

    [TestMethod]
    public void ParseFunctionCallFromContent_WithJsonMarkerButNoClosingMarker_ExtractsCorrectly()
    {
        // Arrange
        var client = new MistralLlmClient(_configuration);
        var method = typeof(MistralLlmClient).GetMethod("ParseFunctionCallFromContent", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        // This tests the case where there's a ```json marker but no closing ``` marker
        var jsonContent = @"```json
{
  ""action"": ""tool_call"",
  ""tool_name"": ""search_flights"",
  ""parameters"": {
    ""departureDate"": ""2023-11-15"",
    ""destination"": ""CDG"",
    ""origin"": ""JFK"",
    ""passengers"": 2,
    ""returnDate"": ""2023-11-18""
  },
  ""status_title"": ""Searching Flights to Paris"",
  ""status_details"": ""Looking for round-trip flights from JFK to CDG for two passengers."",
  ""next_step_hint"": ""Find suitable flights for the trip"",
  ""progress_pct"": 10
}";

        // Act
        var result = method!.Invoke(null, new object[] { jsonContent }) as (string FunctionName, string Arguments)?;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("search_flights", result.Value.FunctionName);
        Assert.IsTrue(result.Value.Arguments.Contains("departureDate"));
        Assert.IsTrue(result.Value.Arguments.Contains("CDG"));
        Assert.IsTrue(result.Value.Arguments.Contains("JFK"));
        Assert.IsTrue(result.Value.Arguments.Contains("2"));
        Assert.IsTrue(result.Value.Arguments.Contains("2023-11-15"));
        Assert.IsTrue(result.Value.Arguments.Contains("2023-11-18"));
    }
}
