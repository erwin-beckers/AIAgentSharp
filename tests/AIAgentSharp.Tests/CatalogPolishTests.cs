using System.Text.Json;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class CatalogPolishTests
{
    [TestMethod]
    public void ConcatTool_Describe_ContainsRequiredAndExample()
    {
        // Arrange
        var tool = new ConcatTool();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsTrue(description.Contains("\"required\":[\"items\"]"));
        Assert.IsTrue(description.Contains("\"example\":[\"hello\",\"world\"]"));
        Assert.IsTrue(description.Contains("\"example\":\", \""));
    }

    [TestMethod]
    public void GetIndicatorTool_Describe_ContainsRequiredEnumAndExample()
    {
        // Arrange
        var tool = new GetIndicatorTool();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsTrue(description.Contains("\"required\":[\"indicator\",\"period\",\"symbol\"]"));
        // No longer an enum, now a string type
        Assert.IsTrue(description.Contains("\"example\":\"MNQ\""));
        Assert.IsTrue(description.Contains("\"example\":\"RSI\""));
        Assert.IsTrue(description.Contains("\"example\":14"));
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
            { "concat", new ConcatTool() },
            { "get_indicator", new GetIndicatorTool() }
        };

        // Act
        var messages = AIAgentSharp.BuildMessages(state, tools, new AgentConfiguration()).ToList();
        var userMessage = messages[1];

        // Assert
        Assert.IsTrue(userMessage.Content.Contains("concat:"));
        Assert.IsTrue(userMessage.Content.Contains("get_indicator:"));
        Assert.IsTrue(userMessage.Content.Contains("\"required\""));
        Assert.IsTrue(userMessage.Content.Contains("\"example\":"));
        // No longer checking for enum since indicator is now a string
    }

    [TestMethod]
    public void ToolCatalog_ConcatTool_CompleteDescription()
    {
        // Arrange
        var tool = new ConcatTool();

        // Act
        var description = tool.Describe();

        // Assert - Check for all required elements
        Assert.IsTrue(description.Contains("\"name\":\"concat\""));
        Assert.IsTrue(description.Contains("\"description\":\"Concatenate strings with a separator.\""));
        Assert.IsTrue(description.Contains("\"type\":\"array\""));
        Assert.IsTrue(description.Contains("\"type\":\"string\""));
        Assert.IsTrue(description.Contains("\"required\":[\"items\"]"));
        Assert.IsTrue(description.Contains("\"example\":[\"hello\",\"world\"]"));
        Assert.IsTrue(description.Contains("\"example\":\", \""));
    }

    [TestMethod]
    public void ToolCatalog_GetIndicatorTool_CompleteDescription()
    {
        // Arrange
        var tool = new GetIndicatorTool();

        // Act
        var description = tool.Describe();

        // Assert - Check for all required elements
        Assert.IsTrue(description.Contains("\"name\":\"get_indicator\""));
        Assert.IsTrue(description.Contains("\"description\":\"Fetch a single indicator value for a symbol.\""));
        Assert.IsTrue(description.Contains("\"type\":[\"string\",\"null\"]") || description.Contains("\"type\":\"string\""));
        Assert.IsTrue(description.Contains("\"type\":\"integer\""));
        // No longer an enum, now a string type
        Assert.IsTrue(description.Contains("\"minimum\":1"));
        Assert.IsTrue(description.Contains("\"required\":[\"indicator\",\"period\",\"symbol\"]"));
        Assert.IsTrue(description.Contains("\"example\":\"MNQ\""));
        Assert.IsTrue(description.Contains("\"example\":\"RSI\""));
        Assert.IsTrue(description.Contains("\"example\":14"));
    }

    [TestMethod]
    public void ToolCatalog_JsonSchema_ConsistentWithDescribe()
    {
        // Arrange
        var concatTool = new ConcatTool();
        var indicatorTool = new GetIndicatorTool();

        // Act
        var concatSchema = concatTool.GetJsonSchema();
        var indicatorSchema = indicatorTool.GetJsonSchema();

        // Assert - Verify that the JSON schema is consistent with the Describe method
        var concatSchemaJson = JsonSerializer.Serialize(concatSchema);
        var indicatorSchemaJson = JsonSerializer.Serialize(indicatorSchema);

        Assert.IsTrue(concatSchemaJson.Contains("\"required\""));
        Assert.IsTrue(indicatorSchemaJson.Contains("\"required\""));
        // No longer an enum, now a string type
        Assert.IsTrue(indicatorSchemaJson.Contains("\"minimum\""));
    }

    [TestMethod]
    public void ToolCatalog_Examples_AreValid()
    {
        // Arrange
        var concatTool = new ConcatTool();
        var indicatorTool = new GetIndicatorTool();

        // Act & Assert - Verify that the examples in the descriptions are valid
        var concatDescription = concatTool.Describe();
        var indicatorDescription = indicatorTool.Describe();

        // Check that examples are properly formatted JSON
        Assert.IsTrue(concatDescription.Contains("\"example\":[\"hello\",\"world\"]"));
        Assert.IsTrue(concatDescription.Contains("\"example\":\", \""));
        Assert.IsTrue(indicatorDescription.Contains("\"example\":\"MNQ\""));
        Assert.IsTrue(indicatorDescription.Contains("\"example\":\"RSI\""));
        Assert.IsTrue(indicatorDescription.Contains("\"example\":14"));
    }
}