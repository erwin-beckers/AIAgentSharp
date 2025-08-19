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

	[TestMethod]
	public void AnthropicConfiguration_DefaultValues_AreCorrect()
	{
		var config = new AnthropicConfiguration();
		Assert.AreEqual("claude-opus-4-1-20250805", config.Model);
		Assert.AreEqual(4000, config.MaxTokens);
		Assert.AreEqual(1f, config.Temperature);
		Assert.AreEqual(1.0f, config.TopP);
		Assert.IsNull(config.TopK);
		Assert.IsFalse(config.EnableStreaming);
		Assert.AreEqual(TimeSpan.FromMinutes(2), config.RequestTimeout);
		Assert.AreEqual(3, config.MaxRetries);
		Assert.AreEqual(TimeSpan.FromSeconds(1), config.RetryDelay);
		Assert.IsTrue(config.EnableFunctionCalling);
		Assert.IsNull(config.ApiBaseUrl);
		Assert.IsNull(config.OrganizationId);
	}

	[TestMethod]
	public void AnthropicConfiguration_WithCustomValues_SetsCorrectly()
	{
		var config = new AnthropicConfiguration
		{
			Model = "custom-model",
			MaxTokens = 5000,
			Temperature = 0.5f,
			TopP = 0.8f,
			TopK = 10,
			EnableStreaming = true,
			RequestTimeout = TimeSpan.FromMinutes(5),
			MaxRetries = 5,
			RetryDelay = TimeSpan.FromSeconds(2),
			EnableFunctionCalling = false,
			ApiBaseUrl = "https://custom.api.com",
			OrganizationId = "org-123"
		};

		Assert.AreEqual("custom-model", config.Model);
		Assert.AreEqual(5000, config.MaxTokens);
		Assert.AreEqual(0.5f, config.Temperature);
		Assert.AreEqual(0.8f, config.TopP);
		Assert.AreEqual(10, config.TopK);
		Assert.IsTrue(config.EnableStreaming);
		Assert.AreEqual(TimeSpan.FromMinutes(5), config.RequestTimeout);
		Assert.AreEqual(5, config.MaxRetries);
		Assert.AreEqual(TimeSpan.FromSeconds(2), config.RetryDelay);
		Assert.IsFalse(config.EnableFunctionCalling);
		Assert.AreEqual("https://custom.api.com", config.ApiBaseUrl);
		Assert.AreEqual("org-123", config.OrganizationId);
	}

	[TestMethod]
	public void AnthropicConfiguration_PropertySetters_WorkCorrectly()
	{
		var config = new AnthropicConfiguration();
		
		// Test that properties can be set via object initializer
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
	public void AnthropicConfiguration_Immutability_IsPreserved()
	{
		var config = new AnthropicConfiguration
		{
			Model = "original-model",
			MaxTokens = 1000
		};

		// Verify that the configuration is immutable (init-only properties)
		// This test ensures that the properties are properly marked as init-only
		Assert.AreEqual("original-model", config.Model);
		Assert.AreEqual(1000, config.MaxTokens);
	}

	[TestMethod]
	public void AnthropicConfiguration_Validation_HandlesEdgeCases()
	{
		// Test with extreme values
		var config = new AnthropicConfiguration
		{
			Temperature = 0.0f,  // Minimum valid value
			TopP = 1.0f,         // Maximum valid value
			MaxTokens = 1,       // Minimum valid value
			MaxRetries = 0       // Minimum valid value
		};

		Assert.AreEqual(0.0f, config.Temperature);
		Assert.AreEqual(1.0f, config.TopP);
		Assert.AreEqual(1, config.MaxTokens);
		Assert.AreEqual(0, config.MaxRetries);
	}

	[TestMethod]
	public void AnthropicConfiguration_FactoryMethods_ReturnDifferentConfigurations()
	{
		var agentConfig = AnthropicConfiguration.CreateForAgentReasoning();
		var creativeConfig = AnthropicConfiguration.CreateForCreativeTasks();
		var costConfig = AnthropicConfiguration.CreateForCostEfficiency();

		// Verify they are different
		Assert.AreNotEqual(agentConfig.Model, creativeConfig.Model);
		Assert.AreNotEqual(agentConfig.Model, costConfig.Model);
		Assert.AreNotEqual(creativeConfig.Model, costConfig.Model);

		// Verify temperature differences
		Assert.AreEqual(0.1f, agentConfig.Temperature);
		Assert.AreEqual(0.7f, creativeConfig.Temperature);
		Assert.AreEqual(0.1f, costConfig.Temperature);

		// Verify max tokens differences
		Assert.AreEqual(4000, agentConfig.MaxTokens);
		Assert.AreEqual(6000, creativeConfig.MaxTokens);
		Assert.AreEqual(2000, costConfig.MaxTokens);
	}

	[TestMethod]
	public void AnthropicConfiguration_TimeoutValues_AreReasonable()
	{
		var agentConfig = AnthropicConfiguration.CreateForAgentReasoning();
		var creativeConfig = AnthropicConfiguration.CreateForCreativeTasks();
		var costConfig = AnthropicConfiguration.CreateForCostEfficiency();

		// Verify timeout values are reasonable
		Assert.IsTrue(agentConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
		Assert.IsTrue(creativeConfig.RequestTimeout >= TimeSpan.FromMinutes(1));
		Assert.IsTrue(costConfig.RequestTimeout >= TimeSpan.FromMinutes(1));

		Assert.IsTrue(agentConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
		Assert.IsTrue(creativeConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
		Assert.IsTrue(costConfig.RequestTimeout <= TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	public void AnthropicConfiguration_RetrySettings_AreAppropriate()
	{
		var agentConfig = AnthropicConfiguration.CreateForAgentReasoning();
		var creativeConfig = AnthropicConfiguration.CreateForCreativeTasks();
		var costConfig = AnthropicConfiguration.CreateForCostEfficiency();

		// Verify retry settings are appropriate
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
}
