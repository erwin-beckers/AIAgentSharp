namespace AIAgentSharp.Tests.Validation;

[TestClass]
public class ToolValidationExceptionTests
{
    [TestMethod]
    public void Constructor_Should_CreateExceptionWithMessage_When_MessageOnlyProvided()
    {
        // Arrange
        var message = "Test validation error";

        // Act
        var exception = new ToolValidationException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsNotNull(exception.Missing);
        Assert.AreEqual(0, exception.Missing.Count);
        Assert.IsNotNull(exception.FieldErrors);
        Assert.AreEqual(0, exception.FieldErrors.Count);
    }

    [TestMethod]
    public void Constructor_Should_CreateExceptionWithMissingFields_When_MissingFieldsProvided()
    {
        // Arrange
        var message = "Test validation error";
        var missing = new List<string> { "field1", "field2" };

        // Act
        var exception = new ToolValidationException(message, missing);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsNotNull(exception.Missing);
        Assert.AreEqual(2, exception.Missing.Count);
        Assert.IsTrue(exception.Missing.Contains("field1"));
        Assert.IsTrue(exception.Missing.Contains("field2"));
        Assert.IsNotNull(exception.FieldErrors);
        Assert.AreEqual(0, exception.FieldErrors.Count);
    }

    [TestMethod]
    public void Constructor_Should_CreateExceptionWithFieldErrors_When_FieldErrorsProvided()
    {
        // Arrange
        var message = "Test validation error";
        var fieldErrors = new List<ToolValidationError>
        {
            new ToolValidationError("field1", "Error 1"),
            new ToolValidationError("field2", "Error 2")
        };

        // Act
        var exception = new ToolValidationException(message, fieldErrors: fieldErrors);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsNotNull(exception.Missing);
        Assert.AreEqual(0, exception.Missing.Count);
        Assert.IsNotNull(exception.FieldErrors);
        Assert.AreEqual(2, exception.FieldErrors.Count);
        Assert.AreEqual("field1", exception.FieldErrors[0].Field);
        Assert.AreEqual("Error 1", exception.FieldErrors[0].Message);
        Assert.AreEqual("field2", exception.FieldErrors[1].Field);
        Assert.AreEqual("Error 2", exception.FieldErrors[1].Message);
    }

    [TestMethod]
    public void Constructor_Should_CreateExceptionWithBothMissingAndFieldErrors_When_BothProvided()
    {
        // Arrange
        var message = "Test validation error";
        var missing = new List<string> { "missingField" };
        var fieldErrors = new List<ToolValidationError>
        {
            new ToolValidationError("invalidField", "Invalid value")
        };

        // Act
        var exception = new ToolValidationException(message, missing, fieldErrors);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsNotNull(exception.Missing);
        Assert.AreEqual(1, exception.Missing.Count);
        Assert.IsTrue(exception.Missing.Contains("missingField"));
        Assert.IsNotNull(exception.FieldErrors);
        Assert.AreEqual(1, exception.FieldErrors.Count);
        Assert.AreEqual("invalidField", exception.FieldErrors[0].Field);
        Assert.AreEqual("Invalid value", exception.FieldErrors[0].Message);
    }

    [TestMethod]
    public void Constructor_Should_HandleNullMissingList_When_NullMissingProvided()
    {
        // Arrange
        var message = "Test validation error";
        List<string>? missing = null;

        // Act
        var exception = new ToolValidationException(message, missing);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsNotNull(exception.Missing);
        Assert.AreEqual(0, exception.Missing.Count);
    }

    [TestMethod]
    public void Constructor_Should_HandleNullFieldErrorsList_When_NullFieldErrorsProvided()
    {
        // Arrange
        var message = "Test validation error";
        List<ToolValidationError>? fieldErrors = null;

        // Act
        var exception = new ToolValidationException(message, fieldErrors: fieldErrors);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsNotNull(exception.FieldErrors);
        Assert.AreEqual(0, exception.FieldErrors.Count);
    }

    [TestMethod]
    public void Constructor_Should_HandleEmptyLists_When_EmptyListsProvided()
    {
        // Arrange
        var message = "Test validation error";
        var missing = new List<string>();
        var fieldErrors = new List<ToolValidationError>();

        // Act
        var exception = new ToolValidationException(message, missing, fieldErrors);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsNotNull(exception.Missing);
        Assert.AreEqual(0, exception.Missing.Count);
        Assert.IsNotNull(exception.FieldErrors);
        Assert.AreEqual(0, exception.FieldErrors.Count);
    }

    [TestMethod]
    public void Exception_Should_InheritFromException_When_Created()
    {
        // Arrange
        var message = "Test validation error";

        // Act
        var exception = new ToolValidationException(message);

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
        Assert.IsInstanceOfType(exception, typeof(ToolValidationException));
    }

    [TestMethod]
    public void Properties_Should_BeReadOnly_When_Accessed()
    {
        // Arrange
        var exception = new ToolValidationException("Test");

        // Act & Assert
        // Verify properties are read-only by checking they have getters but no setters
        var missingProperty = typeof(ToolValidationException).GetProperty(nameof(ToolValidationException.Missing));
        var fieldErrorsProperty = typeof(ToolValidationException).GetProperty(nameof(ToolValidationException.FieldErrors));

        Assert.IsNotNull(missingProperty);
        Assert.IsNotNull(fieldErrorsProperty);
        Assert.IsTrue(missingProperty.CanRead);
        Assert.IsFalse(missingProperty.CanWrite);
        Assert.IsTrue(fieldErrorsProperty.CanRead);
        Assert.IsFalse(fieldErrorsProperty.CanWrite);
    }
}
