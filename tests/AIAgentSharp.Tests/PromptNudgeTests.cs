using AIAgentSharp.Agents;

namespace AIAgentSharp.Tests;

[TestClass]
public sealed class PromptNudgeTests
{
    [TestMethod]
    public void SystemPrompt_ContainsLoopBreakerNudge()
    {
        // Act & Assert
        Assert.IsTrue(Prompts.LlmSystemPrompt.Contains("Avoid repeating identical failing calls"));
    }

    [TestMethod]
    public void BuildMessages_ContainsPromptNudge()
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
            { "test_tool", new TestTool() }
        };

        // Act
        var messageBuilder = new MessageBuilder(new AgentConfiguration());
        var messages = messageBuilder.BuildMessages(state, tools).ToList();

        // Assert
        Assert.AreEqual(2, messages.Count); // System and user messages
        var userMessage = messages[1];
        Assert.IsTrue(userMessage.Content.Contains("When a tool call fails, read the validation_error details in HISTORY and immediately retry with corrected parameters"));
        Assert.IsTrue(userMessage.Content.Contains("Avoid repeating identical failing calls"));
    }

    [TestMethod]
    public void BuildMessages_PromptNudgeAppearsInCorrectLocation()
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
            { "test_tool", new TestTool() }
        };

        // Act
        var messageBuilder = new MessageBuilder(new AgentConfiguration());
        var messages = messageBuilder.BuildMessages(state, tools).ToList();
        var userMessage = messages[1];

        // Assert - The nudge should appear near the end of the message
        var lines = userMessage.Content.Split('\n');
        var lastLines = lines.TakeLast(5).ToList();
        var hasNudge = lastLines.Any(line => line.Contains("Avoid repeating identical failing calls"));
        Assert.IsTrue(hasNudge, "Prompt nudge should appear in the last few lines of the user message");
    }

    private class TestTool : ITool
    {
        public string Name => "test_tool";

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            return Task.FromResult<object?>(new { result = "test" });
        }
    }
}