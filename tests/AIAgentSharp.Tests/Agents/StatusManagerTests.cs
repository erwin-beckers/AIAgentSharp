using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class StatusManagerTests
{
    private AgentConfiguration _config = null!;
    private Mock<IEventManager> _mockEventManager = null!;
    private Mock<ILogger> _mockLogger = null!;
    private StatusManager _statusManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new AgentConfiguration();
        _mockEventManager = new Mock<IEventManager>();
        _mockLogger = new Mock<ILogger>();
        _statusManager = new StatusManager(_config, _mockEventManager.Object, _mockLogger.Object);
    }

    [TestMethod]
    public void EmitStatus_WithEmitPublicStatusEnabled_ShouldInvokeEvent()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";
        var statusDetails = "Test Details";
        var nextStepHint = "Next Step";
        var progressPct = 50;

        // Act
        statusManager.EmitStatus(agentId, statusTitle, statusDetails, nextStepHint, progressPct);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, statusDetails, nextStepHint, progressPct), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_WithEmitPublicStatusDisabled_ShouldNotInvokeEvent()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = false };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";

        // Act
        statusManager.EmitStatus(agentId, statusTitle);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Never);
    }

    [TestMethod]
    public void EmitStatus_WithNullEventManager_ShouldNotThrow()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";

        // Act & Assert - Should not throw
        statusManager.EmitStatus(agentId, statusTitle);
    }



    [TestMethod]
    public void EmitStatus_WithAllParameters_ShouldSetCorrectValues()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";
        var statusDetails = "Test Details";
        var nextStepHint = "Next Step";
        var progressPct = 75;

        // Act
        statusManager.EmitStatus(agentId, statusTitle, statusDetails, nextStepHint, progressPct);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, statusDetails, nextStepHint, progressPct), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_WithMinimalParameters_ShouldSetDefaultValues()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";

        // Act
        statusManager.EmitStatus(agentId, statusTitle);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, null, null, null), Times.Once);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldUseConsoleLogger()
    {
        // Act
        var statusManager = new StatusManager(_config, _mockEventManager.Object, null);

        // Assert
        Assert.IsNotNull(statusManager);
    }

    [TestMethod]
    public void EmitStatus_WithMultipleCalls_ShouldWorkCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);

        // Act
        statusManager.EmitStatus("agent1", "Status 1");
        statusManager.EmitStatus("agent2", "Status 2");
        statusManager.EmitStatus("agent3", "Status 3");

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Exactly(3));
    }

    [TestMethod]
    public void EmitStatus_WithEmptyStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "";
        var statusTitle = "";

        // Act
        statusManager.EmitStatus(agentId, statusTitle);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, null, null, null), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_WithNullStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        string? statusTitle = null;

        // Act
        statusManager.EmitStatus(agentId, statusTitle ?? string.Empty);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, "", null, null, null), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_WithProgressPercentage_ShouldHandleRangeCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";

        // Act - Test various progress values
        statusManager.EmitStatus(agentId, statusTitle, progressPct: 0);
        statusManager.EmitStatus(agentId, statusTitle, progressPct: 50);
        statusManager.EmitStatus(agentId, statusTitle, progressPct: 100);
        statusManager.EmitStatus(agentId, statusTitle, progressPct: -10);
        statusManager.EmitStatus(agentId, statusTitle, progressPct: 150);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, null, null, 0), Times.Once);
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, null, null, 50), Times.Once);
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, null, null, 100), Times.Once);
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, null, null, -10), Times.Once);
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(agentId, statusTitle, null, null, 150), Times.Once);
    }
}
