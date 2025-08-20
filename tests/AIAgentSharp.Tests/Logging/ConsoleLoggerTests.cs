namespace AIAgentSharp.Tests.Logging;

[TestClass]
public class ConsoleLoggerTests
{
    private StringWriter _stringWriter = null!;
    private TextWriter _originalOut = null!;

    [TestInitialize]
    public void Setup()
    {
        _originalOut = Console.Out;
        _stringWriter = new StringWriter();
        Console.SetOut(_stringWriter);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Console.SetOut(_originalOut);
        _stringWriter?.Dispose();
    }

    [TestMethod]
    public void Constructor_Should_CreateLoggerSuccessfully()
    {
        // Act
        var logger = new ConsoleLogger();

        // Assert
        Assert.IsNotNull(logger);
    }

    [TestMethod]
    public void LogInformation_Should_WriteMessageWithInfoPrefix_When_ValidMessageProvided()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = "Test information message";

        // Act
        logger.LogInformation(message);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[INFO]"));
        Assert.IsTrue(output.Contains(message));
    }

    [TestMethod]
    public void LogInformation_Should_HandleNullMessage_When_MessageIsNull()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act & Assert - Should not throw, just write null to console
        logger.LogInformation(null!);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[INFO]"));
    }

    [TestMethod]
    public void LogInformation_Should_WriteEmptyMessageWithPrefix_When_MessageIsEmpty()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = string.Empty;

        // Act
        logger.LogInformation(message);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[INFO]"));
        Assert.IsTrue(output.Contains(message));
    }

    [TestMethod]
    public void LogWarning_Should_WriteMessageWithWarnPrefix_When_ValidMessageProvided()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = "Test warning message";

        // Act
        logger.LogWarning(message);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[WARN]"));
        Assert.IsTrue(output.Contains(message));
    }

    [TestMethod]
    public void LogWarning_Should_HandleNullMessage_When_MessageIsNull()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act & Assert - Should not throw, just write null to console
        logger.LogWarning(null!);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[WARN]"));
    }

    [TestMethod]
    public void LogWarning_Should_WriteEmptyMessageWithPrefix_When_MessageIsEmpty()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = string.Empty;

        // Act
        logger.LogWarning(message);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[WARN]"));
        Assert.IsTrue(output.Contains(message));
    }

    [TestMethod]
    public void LogError_Should_WriteMessageWithErrorPrefix_When_ValidMessageProvided()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = "Test error message";

        // Act
        logger.LogError(message);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[ERROR]"));
        Assert.IsTrue(output.Contains(message));
    }

    [TestMethod]
    public void LogError_Should_HandleNullMessage_When_MessageIsNull()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act & Assert - Should not throw, just write null to console
        logger.LogError(null!);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[ERROR]"));
    }

    [TestMethod]
    public void LogError_Should_WriteEmptyMessageWithPrefix_When_MessageIsEmpty()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = string.Empty;

        // Act
        logger.LogError(message);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[ERROR]"));
        Assert.IsTrue(output.Contains(message));
    }

    [TestMethod]
    public void LogDebug_Should_WriteMessageWithDebugPrefix_When_ValidMessageProvided()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = "Test debug message";

        // Act
        logger.LogDebug(message);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[DEBUG]"));
        Assert.IsTrue(output.Contains(message));
    }

    [TestMethod]
    public void LogDebug_Should_HandleNullMessage_When_MessageIsNull()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act & Assert - Should not throw, just write null to console
        logger.LogDebug(null!);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[DEBUG]"));
    }

    [TestMethod]
    public void LogDebug_Should_WriteEmptyMessageWithPrefix_When_MessageIsEmpty()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = string.Empty;

        // Act
        logger.LogDebug(message);

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[DEBUG]"));
        Assert.IsTrue(output.Contains(message));
    }

    [TestMethod]
    public void MultipleLogCalls_Should_WriteAllMessagesWithCorrectPrefixes()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        logger.LogInformation("Info message");
        logger.LogWarning("Warning message");
        logger.LogError("Error message");
        logger.LogDebug("Debug message");

        // Assert
        var output = _stringWriter.ToString();
        Assert.IsTrue(output.Contains("[INFO] Info message"));
        Assert.IsTrue(output.Contains("[WARN] Warning message"));
        Assert.IsTrue(output.Contains("[ERROR] Error message"));
        Assert.IsTrue(output.Contains("[DEBUG] Debug message"));
    }
}
