using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using Moq;

namespace AIAgentSharp.Tests.Agents;

[TestClass]
public class StatusManagerTests
{
    private Mock<IEventManager> _mockEventManager = null!;
    private Mock<ILogger> _mockLogger = null!;
    private AgentConfiguration _config = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockEventManager = new Mock<IEventManager>();
        _mockLogger = new Mock<ILogger>();
        _config = new AgentConfiguration();
    }

    [TestMethod]
    public void Constructor_Should_CreateStatusManagerSuccessfully_When_ValidParametersProvided()
    {
        // Act
        var statusManager = new StatusManager(_config, _mockEventManager.Object, _mockLogger.Object);

        // Assert
        Assert.IsNotNull(statusManager);
    }

    [TestMethod]
    public void Constructor_Should_HandleNullConfig_When_ConfigIsNull()
    {
        // Act & Assert - Should not throw, just use null config
        var statusManager = new StatusManager(null!, _mockEventManager.Object, _mockLogger.Object);
        
        // Assert
        Assert.IsNotNull(statusManager);
    }

    [TestMethod]
    public void Constructor_Should_HandleNullEventManager_When_EventManagerIsNull()
    {
        // Act & Assert - Should not throw, just use null event manager
        var statusManager = new StatusManager(_config, null!, _mockLogger.Object);
        
        // Assert
        Assert.IsNotNull(statusManager);
    }

    [TestMethod]
    public void Constructor_Should_UseConsoleLoggerAsDefault_When_LoggerIsNull()
    {
        // Act
        var statusManager = new StatusManager(_config, _mockEventManager.Object, null);

        // Assert
        Assert.IsNotNull(statusManager);
    }

    [TestMethod]
    public void EmitStatus_Should_EmitStatusEvent_When_EmitPublicStatusIsTrue()
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
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(
            agentId, statusTitle, statusDetails, nextStepHint, progressPct), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_Should_NotEmitStatusEvent_When_EmitPublicStatusIsFalse()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = false };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";

        // Act
        statusManager.EmitStatus(agentId, statusTitle);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(
            It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => s == null), 
            It.Is<string>(s => s == null), It.Is<int?>(i => i == null)), Times.Never);
    }

    [TestMethod]
    public void EmitStatus_Should_HandleNullAgentId_When_AgentIdIsNull()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var statusTitle = "Test Status";

        // Act & Assert - Should not throw, just pass null to event manager
        statusManager.EmitStatus(null!, statusTitle);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(
            null!, statusTitle, null, null, null), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_Should_HandleNullStatusTitle_When_StatusTitleIsNull()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";

        // Act & Assert - Should not throw, just pass null to event manager
        statusManager.EmitStatus(agentId, null!);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(
            agentId, null!, null, null, null), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_Should_EmitEventWithNullOptionalParameters_When_OptionalParametersAreNull()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";

        // Act
        statusManager.EmitStatus(agentId, statusTitle, null, null, null);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(
            agentId, statusTitle, null, null, null), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_Should_PropagateEventManagerException_When_EventManagerThrows()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        _mockEventManager.Setup(x => x.RaiseStatusUpdate(
            It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => s == null), 
            It.Is<string>(s => s == null), It.Is<int?>(i => i == null)))
            .Throws(new InvalidOperationException("Test exception"));
        
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "Test Status";

        // Act & Assert - Should propagate the exception
        Assert.ThrowsException<InvalidOperationException>(() => 
            statusManager.EmitStatus(agentId, statusTitle));
    }

    [TestMethod]
    public void EmitStatus_Should_EmitEventWithAllParameters_When_AllParametersProvided()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent-123";
        var statusTitle = "Processing Complete";
        var statusDetails = "Successfully processed 100 items";
        var nextStepHint = "Ready for next phase";
        var progressPct = 75;

        // Act
        statusManager.EmitStatus(agentId, statusTitle, statusDetails, nextStepHint, progressPct);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(
            agentId, statusTitle, statusDetails, nextStepHint, progressPct), Times.Once);
    }

    [TestMethod]
    public void EmitStatus_Should_EmitEventWithEmptyStrings_When_EmptyStringsProvided()
    {
        // Arrange
        var config = new AgentConfiguration { EmitPublicStatus = true };
        var statusManager = new StatusManager(config, _mockEventManager.Object, _mockLogger.Object);
        var agentId = "test-agent";
        var statusTitle = "";
        var statusDetails = "";
        var nextStepHint = "";

        // Act
        statusManager.EmitStatus(agentId, statusTitle, statusDetails, nextStepHint);

        // Assert
        _mockEventManager.Verify(x => x.RaiseStatusUpdate(
            agentId, statusTitle, statusDetails, nextStepHint, null), Times.Once);
    }
}
