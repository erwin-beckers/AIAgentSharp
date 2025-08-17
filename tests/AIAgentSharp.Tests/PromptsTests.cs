namespace AIAgentSharp.Tests;

[TestClass]
public sealed class PromptsTests
{
    [TestMethod]
    public void LlmSystemPrompt_IsNotNull()
    {
        // Act
        var prompt = Prompts.LlmSystemPrompt;

        // Assert
        Assert.IsNotNull(prompt);
        Assert.IsFalse(string.IsNullOrWhiteSpace(prompt));
    }

    [TestMethod]
    public void LlmSystemPrompt_ContainsRequiredElements()
    {
        // Act
        var prompt = Prompts.LlmSystemPrompt;

        // Assert
        Assert.IsTrue(prompt.Contains("stateful"));
        Assert.IsTrue(prompt.Contains("tool-using"));
        Assert.IsTrue(prompt.Contains("JSON"));
        Assert.IsTrue(prompt.Contains("MODEL OUTPUT CONTRACT"));
        Assert.IsTrue(prompt.Contains("plan"));
        Assert.IsTrue(prompt.Contains("tool_call"));
        Assert.IsTrue(prompt.Contains("finish"));
        Assert.IsTrue(prompt.Contains("retry"));
    }

    [TestMethod]
    public void LlmSystemPrompt_ContainsRules()
    {
        // Act
        var prompt = Prompts.LlmSystemPrompt;

        // Assert
        Assert.IsTrue(prompt.Contains("MODEL OUTPUT CONTRACT:"));
        Assert.IsTrue(prompt.Contains("EXAMPLES:"));
        Assert.IsTrue(prompt.Contains("IMPORTANT:"));
        Assert.IsTrue(prompt.Contains("JSON only"));
    }

    [TestMethod]
    public void LlmSystemPrompt_ContainsModelOutputContract()
    {
        // Act
        var prompt = Prompts.LlmSystemPrompt;

        // Assert
        Assert.IsTrue(prompt.Contains("MODEL OUTPUT CONTRACT:"));
        Assert.IsTrue(prompt.Contains("\"thoughts\""));
        Assert.IsTrue(prompt.Contains("\"action\""));
        Assert.IsTrue(prompt.Contains("\"action_input\""));
        Assert.IsTrue(prompt.Contains("\"summary\""));
        Assert.IsTrue(prompt.Contains("\"tool\""));
        Assert.IsTrue(prompt.Contains("\"params\""));
        Assert.IsTrue(prompt.Contains("\"final\""));
    }

    [TestMethod]
    public void LlmSystemPrompt_IsConstant()
    {
        // Act
        var prompt1 = Prompts.LlmSystemPrompt;
        var prompt2 = Prompts.LlmSystemPrompt;

        // Assert
        Assert.AreEqual(prompt1, prompt2);
        Assert.AreSame(prompt1, prompt2);
    }

    [TestMethod]
    public void LlmSystemPrompt_Length_IsReasonable()
    {
        // Act
        var prompt = Prompts.LlmSystemPrompt;

        // Assert
        Assert.IsTrue(prompt.Length > 500); // Should be substantial
        Assert.IsTrue(prompt.Length < 10000); // Should not be excessive
    }

    [TestMethod]
    public void LlmSystemPrompt_ContainsActionValues()
    {
        // Act
        var prompt = Prompts.LlmSystemPrompt;

        // Assert
        Assert.IsTrue(prompt.Contains("\"plan\""));
        Assert.IsTrue(prompt.Contains("\"tool_call\""));
        Assert.IsTrue(prompt.Contains("\"finish\""));
        Assert.IsTrue(prompt.Contains("\"retry\""));
    }

    [TestMethod]
    public void LlmSystemPrompt_ContainsJsonFormatting()
    {
        // Act
        var prompt = Prompts.LlmSystemPrompt;

        // Assert
        Assert.IsTrue(prompt.Contains("JSON"));
        Assert.IsTrue(prompt.Contains("JSON object"));
        Assert.IsTrue(prompt.Contains("No extra text"));
    }
}