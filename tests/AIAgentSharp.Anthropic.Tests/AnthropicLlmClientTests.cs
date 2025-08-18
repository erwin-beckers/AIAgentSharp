using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.Anthropic.Tests;

[TestClass]
public class AnthropicLlmClientTests
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
	public void Constructor_WithValidApiKey_CreatesInstance()
	{
		Assert.IsNotNull(new AnthropicLlmClient("test-api-key"));
	}

	[TestMethod]
	public void Constructor_WithValidApiKeyAndModel_CreatesInstance()
	{
		Assert.IsNotNull(new AnthropicLlmClient("test-api-key", "claude-3-5-haiku-20241022"));
	}

	[TestMethod]
	public void Constructor_WithValidApiKeyAndConfiguration_CreatesInstance()
	{
		Assert.IsNotNull(new AnthropicLlmClient("test-api-key", _configuration));
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
	{
		_ = new AnthropicLlmClient(null!);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void Constructor_WithEmptyApiKey_ThrowsArgumentNullException()
	{
		_ = new AnthropicLlmClient("");
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
	{
		_ = new AnthropicLlmClient("test-api-key", (AnthropicConfiguration)null!);
	}

	[TestMethod]
	public void Configuration_ReturnsExpectedConfiguration()
	{
		var client = new AnthropicLlmClient("test-api-key", _configuration);
		Assert.IsNotNull(client.Configuration);
		Assert.AreEqual(_configuration.Model, client.Configuration.Model);
	}

	[TestMethod]
	public void AnthropicConfiguration_CreateForAgentReasoning_ReturnsExpectedConfiguration()
	{
		var config = AnthropicConfiguration.CreateForAgentReasoning();
		Assert.AreEqual("claude-3-5-sonnet-20241022", config.Model);
		Assert.AreEqual(0.1f, config.Temperature);
		Assert.AreEqual(4000, config.MaxTokens);
		Assert.IsTrue(config.EnableFunctionCalling);
	}

	[TestMethod]
	public void AnthropicConfiguration_CreateForCreativeTasks_ReturnsExpectedConfiguration()
	{
		var config = AnthropicConfiguration.CreateForCreativeTasks();
		Assert.AreEqual("claude-3-opus-20240229", config.Model);
		Assert.AreEqual(0.7f, config.Temperature);
		Assert.AreEqual(6000, config.MaxTokens);
		Assert.IsTrue(config.EnableFunctionCalling);
	}

	[TestMethod]
	public void AnthropicConfiguration_CreateForCostEfficiency_ReturnsExpectedConfiguration()
	{
		var config = AnthropicConfiguration.CreateForCostEfficiency();
		Assert.AreEqual("claude-3-5-haiku-20241022", config.Model);
		Assert.AreEqual(0.1f, config.Temperature);
		Assert.AreEqual(2000, config.MaxTokens);
		Assert.IsTrue(config.EnableFunctionCalling);
	}
}
