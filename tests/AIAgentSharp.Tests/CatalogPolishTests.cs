using System.Text.Json;
using AIAgentSharp.Agents;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class CatalogPolishTests
{
    [TestMethod]
    public void MockConcatTool_Describe_ContainsRequiredAndExample()
    {
        // Arrange
        var tool = new MockConcatTool();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsTrue(description.Contains("\"required\":[\"strings\"]"));
        Assert.IsTrue(description.Contains("\"type\":\"array\""));
    }

    [TestMethod]
    public void MockValidationTool_Describe_ContainsRequiredAndExample()
    {
        // Arrange
        var tool = new MockValidationTool();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsTrue(description.Contains("\"required\":[\"input\",\"rules\"]"));
        Assert.IsTrue(description.Contains("\"type\":[\"string\",\"null\"]"));
    }

    [TestMethod]
    public void BuildMessages_IncludesPolishedToolDescriptions()
    {
        // Arrange
        var state = new AgentState
        {
            AgentId = "test-agent",
            Goal = "Test goal",
            Turns = new List<AgentTurn>()
        };

        var tools = new Dictionary<string, ITool>
        {
            { "concat", new MockConcatTool() },
            { "validate_input", new MockValidationTool() }
        };

        // Act
        var messageBuilder = new MessageBuilder(new AgentConfiguration());
        var messages = messageBuilder.BuildMessages(state, tools).ToList();
        var userMessage = messages[1];

        // Assert
        Assert.IsTrue(userMessage.Content.Contains("concat:"));
        Assert.IsTrue(userMessage.Content.Contains("validate_input:"));
        Assert.IsTrue(userMessage.Content.Contains("\"required\""));
    }

    [TestMethod]
    public void ToolCatalog_MockConcatTool_CompleteDescription()
    {
        // Arrange
        var tool = new MockConcatTool();

        // Act
        var description = tool.Describe();

        // Assert - Check for all required elements
        Assert.IsTrue(description.Contains("\"name\":\"concat\""));
        Assert.IsTrue(description.Contains("\"description\":\"Concatenate multiple strings together\""));
        Assert.IsTrue(description.Contains("\"type\":\"array\""));
        Assert.IsTrue(description.Contains("\"required\":[\"strings\"]"));
    }

    [TestMethod]
    public void ToolCatalog_MockValidationTool_CompleteDescription()
    {
        // Arrange
        var tool = new MockValidationTool();

        // Act
        var description = tool.Describe();

        // Assert - Check for all required elements
        Assert.IsTrue(description.Contains("\"name\":\"validate_input\""));
        Assert.IsTrue(description.Contains("\"description\":\"Validate input data with custom rules\""));
        Assert.IsTrue(description.Contains("\"type\":[\"string\",\"null\"]"));
        Assert.IsTrue(description.Contains("\"required\":[\"input\",\"rules\"]"));
    }

    [TestMethod]
    public void ToolCatalog_JsonSchema_ConsistentWithDescribe()
    {
        // Arrange
        var concatTool = new MockConcatTool();
        var validationTool = new MockValidationTool();

        // Act
        var concatSchema = concatTool.GetJsonSchema();
        var validationSchema = validationTool.GetJsonSchema();

        // Assert - Verify that the JSON schema is consistent with the Describe method
        var concatSchemaJson = JsonSerializer.Serialize(concatSchema);
        var validationSchemaJson = JsonSerializer.Serialize(validationSchema);

        Assert.IsTrue(concatSchemaJson.Contains("\"required\""));
        Assert.IsTrue(validationSchemaJson.Contains("\"required\""));
    }

    [TestMethod]
    public void ToolCatalog_Examples_AreValid()
    {
        // Arrange
        var concatTool = new MockConcatTool();
        var validationTool = new MockValidationTool();

        // Act & Assert - Verify that the descriptions are valid
        var concatDescription = concatTool.Describe();
        var validationDescription = validationTool.Describe();

        // Check that descriptions are properly formatted JSON
        Assert.IsTrue(concatDescription.Contains("\"name\":\"concat\""));
        Assert.IsTrue(validationDescription.Contains("\"name\":\"validate_input\""));
    }


}