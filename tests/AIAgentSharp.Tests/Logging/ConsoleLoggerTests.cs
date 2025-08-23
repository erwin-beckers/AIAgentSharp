namespace AIAgentSharp.Tests.Logging;

[TestClass]
public class ConsoleLoggerTests
{
    // Console logger is currently a no-op; ensure methods don't throw

    [TestMethod]
    public void Constructor_Should_CreateLoggerSuccessfully()
    {
        // Act
        var logger = new ConsoleLogger();

        // Assert
        Assert.IsNotNull(logger);
    }

    [TestMethod]
    public void LogInformation_Should_NotThrow_When_Called()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        logger.LogInformation("Test information message");
        logger.LogInformation(string.Empty);
        logger.LogInformation(null!);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void LogWarning_Should_NotThrow_When_Called()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        logger.LogWarning("Test warning message");
        logger.LogWarning(string.Empty);
        logger.LogWarning(null!);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void LogError_Should_NotThrow_When_Called()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        logger.LogError("Test error message");
        logger.LogError(string.Empty);
        logger.LogError(null!);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void LogDebug_Should_NotThrow_When_Called()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        logger.LogDebug("Test debug message");
        logger.LogDebug(string.Empty);
        logger.LogDebug(null!);
        Assert.IsTrue(true);
    }
    [TestMethod]
    public void MultipleLogCalls_Should_NotThrow()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        logger.LogInformation("Info message");
        logger.LogWarning("Warning message");
        logger.LogError("Error message");
        logger.LogDebug("Debug message");

        // Assert
        Assert.IsTrue(true);
    }
}
