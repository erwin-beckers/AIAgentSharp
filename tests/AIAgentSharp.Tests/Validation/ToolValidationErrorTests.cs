namespace AIAgentSharp.Tests.Validation;

[TestClass]
public class ToolValidationErrorTests
{
    [TestMethod]
    public void Constructor_Should_CreateErrorWithFieldAndMessage_When_ValidParametersProvided()
    {
        // Arrange
        var field = "testField";
        var message = "Test validation error";

        // Act
        var error = new ToolValidationError(field, message);

        // Assert
        Assert.AreEqual(field, error.Field);
        Assert.AreEqual(message, error.Message);
    }

    [TestMethod]
    public void Constructor_Should_HandleEmptyField_When_EmptyFieldProvided()
    {
        // Arrange
        var field = "";
        var message = "Test validation error";

        // Act
        var error = new ToolValidationError(field, message);

        // Assert
        Assert.AreEqual(field, error.Field);
        Assert.AreEqual(message, error.Message);
    }

    [TestMethod]
    public void Constructor_Should_HandleEmptyMessage_When_EmptyMessageProvided()
    {
        // Arrange
        var field = "testField";
        var message = "";

        // Act
        var error = new ToolValidationError(field, message);

        // Assert
        Assert.AreEqual(field, error.Field);
        Assert.AreEqual(message, error.Message);
    }

    [TestMethod]
    public void Properties_Should_BeReadOnly_When_Accessed()
    {
        // Arrange
        var field = "testField";
        var message = "Test validation error";
        var error = new ToolValidationError(field, message);

        // Act & Assert
        // Verify properties are read-only by checking they have getters but no setters
        var fieldProperty = typeof(ToolValidationError).GetProperty(nameof(ToolValidationError.Field));
        var messageProperty = typeof(ToolValidationError).GetProperty(nameof(ToolValidationError.Message));

        Assert.IsNotNull(fieldProperty);
        Assert.IsNotNull(messageProperty);
        Assert.IsTrue(fieldProperty.CanRead);
        Assert.IsFalse(fieldProperty.CanWrite);
        Assert.IsTrue(messageProperty.CanRead);
        Assert.IsFalse(messageProperty.CanWrite);
    }
}
