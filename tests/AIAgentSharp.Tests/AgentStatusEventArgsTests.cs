using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AIAgentSharp.Tests;

[TestClass]
public class AgentStatusEventArgsTests
{
    [TestMethod]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var args = new AgentStatusEventArgs();

        // Assert
        Assert.IsNotNull(args);
        Assert.AreEqual(string.Empty, args.AgentId);
        Assert.AreEqual(0, args.TurnIndex);
        Assert.AreEqual(string.Empty, args.StatusTitle);
        Assert.IsNull(args.StatusDetails);
        Assert.IsNull(args.NextStepHint);
        Assert.IsNull(args.ProgressPct);
        Assert.IsTrue(args.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1)); // Should be recent
    }

    [TestMethod]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var args = new AgentStatusEventArgs();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        args.AgentId = "test-agent-123";
        args.TurnIndex = 5;
        args.StatusTitle = "Processing request";
        args.StatusDetails = "Analyzing user input";
        args.NextStepHint = "Will generate response";
        args.ProgressPct = 75;
        args.Timestamp = timestamp;

        // Assert
        Assert.AreEqual("test-agent-123", args.AgentId);
        Assert.AreEqual(5, args.TurnIndex);
        Assert.AreEqual("Processing request", args.StatusTitle);
        Assert.AreEqual("Analyzing user input", args.StatusDetails);
        Assert.AreEqual("Will generate response", args.NextStepHint);
        Assert.AreEqual(75, args.ProgressPct);
        Assert.AreEqual(timestamp, args.Timestamp);
    }

    [TestMethod]
    public void Properties_CanHandleNullValues()
    {
        // Arrange
        var args = new AgentStatusEventArgs();

        // Act
        args.StatusDetails = null;
        args.NextStepHint = null;
        args.ProgressPct = null;

        // Assert
        Assert.IsNull(args.StatusDetails);
        Assert.IsNull(args.NextStepHint);
        Assert.IsNull(args.ProgressPct);
    }

    [TestMethod]
    public void Properties_CanHandleEmptyStrings()
    {
        // Arrange
        var args = new AgentStatusEventArgs();

        // Act
        args.AgentId = "";
        args.StatusTitle = "";

        // Assert
        Assert.AreEqual("", args.AgentId);
        Assert.AreEqual("", args.StatusTitle);
    }

    [TestMethod]
    public void Properties_CanHandleSpecialCharacters()
    {
        // Arrange
        var args = new AgentStatusEventArgs();

        // Act
        args.AgentId = "agent-123_456";
        args.StatusTitle = "Status: Processing...";
        args.StatusDetails = "Details with special chars: @#$%^&*()";

        // Assert
        Assert.AreEqual("agent-123_456", args.AgentId);
        Assert.AreEqual("Status: Processing...", args.StatusTitle);
        Assert.AreEqual("Details with special chars: @#$%^&*()", args.StatusDetails);
    }
}
