namespace AIAgentSharp.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LoggingTests
{
    [TestMethod]
    public void ConsoleLogger_LogInformation_WritesToConsole()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.LogInformation("test message");

            // Assert
            stringWriter.Flush();
            var output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("[INFO] test message"), $"Expected output to contain '[INFO] test message', but got: '{output}'");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public void ConsoleLogger_LogWarning_WritesToConsole()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.LogWarning("test warning");

            // Assert
            stringWriter.Flush();
            var output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("[WARN] test warning"), $"Expected output to contain '[WARN] test warning', but got: '{output}'");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public void ConsoleLogger_LogError_WritesToConsole()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.LogError("test error");

            // Assert
            stringWriter.Flush();
            var output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("[ERROR] test error"), $"Expected output to contain '[ERROR] test error', but got: '{output}'");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public void ConsoleLogger_LogDebug_WritesToConsole()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.LogDebug("test debug");

            // Assert
            stringWriter.Flush();
            var output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("[DEBUG] test debug"), $"Expected output to contain '[DEBUG] test debug', but got: '{output}'");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public void ConsoleLogger_AllLogLevels_UseCorrectPrefixes()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.LogInformation("info");
            logger.LogWarning("warn");
            logger.LogError("error");
            logger.LogDebug("debug");

            // Assert
            stringWriter.Flush();
            var output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("[INFO] info"), $"Expected output to contain '[INFO] info', but got: '{output}'");
            Assert.IsTrue(output.Contains("[WARN] warn"), $"Expected output to contain '[WARN] warn', but got: '{output}'");
            Assert.IsTrue(output.Contains("[ERROR] error"), $"Expected output to contain '[ERROR] error', but got: '{output}'");
            Assert.IsTrue(output.Contains("[DEBUG] debug"), $"Expected output to contain '[DEBUG] debug', but got: '{output}'");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public void ConsoleLogger_LogMethods_DoNotThrowExceptions()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act & Assert - should not throw
        logger.LogInformation("test");
        logger.LogWarning("test");
        logger.LogError("test");
        logger.LogDebug("test");
    }

    [TestMethod]
    public void ConsoleLogger_LogMethods_HandleNullMessages()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.LogInformation(null!);
            logger.LogWarning(null!);
            logger.LogError(null!);
            logger.LogDebug(null!);

            // Assert
            stringWriter.Flush();
            var output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("[INFO] "), $"Expected output to contain '[INFO] ', but got: '{output}'");
            Assert.IsTrue(output.Contains("[WARN] "), $"Expected output to contain '[WARN] ', but got: '{output}'");
            Assert.IsTrue(output.Contains("[ERROR] "), $"Expected output to contain '[ERROR] ', but got: '{output}'");
            Assert.IsTrue(output.Contains("[DEBUG] "), $"Expected output to contain '[DEBUG] ', but got: '{output}'");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public void ConsoleLogger_LogMethods_HandleEmptyMessages()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            logger.LogInformation("");
            logger.LogWarning("");
            logger.LogError("");
            logger.LogDebug("");

            // Assert
            stringWriter.Flush();
            var output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("[INFO] "), $"Expected output to contain '[INFO] ', but got: '{output}'");
            Assert.IsTrue(output.Contains("[WARN] "), $"Expected output to contain '[WARN] ', but got: '{output}'");
            Assert.IsTrue(output.Contains("[ERROR] "), $"Expected output to contain '[ERROR] ', but got: '{output}'");
            Assert.IsTrue(output.Contains("[DEBUG] "), $"Expected output to contain '[DEBUG] ', but got: '{output}'");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}