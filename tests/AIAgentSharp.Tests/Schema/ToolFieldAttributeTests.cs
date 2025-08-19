namespace AIAgentSharp.Tests.Schema;

[TestClass]
public class ToolFieldAttributeTests
{
    [TestMethod]
    public void Constructor_Should_CreateToolFieldAttribute_When_Called()
    {
        // Act
        var attribute = new ToolFieldAttribute();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.IsInstanceOfType(attribute, typeof(Attribute));
    }

    [TestMethod]
    public void Description_Should_BeNull_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.IsNull(attribute.Description);
    }

    [TestMethod]
    public void Description_Should_BeSet_When_Provided()
    {
        // Arrange
        var expectedDescription = "Test field description";

        // Act
        var attribute = new ToolFieldAttribute
        {
            Description = expectedDescription
        };

        // Assert
        Assert.AreEqual(expectedDescription, attribute.Description);
    }

    [TestMethod]
    public void Example_Should_BeNull_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.IsNull(attribute.Example);
    }

    [TestMethod]
    public void Example_Should_BeSet_When_StringProvided()
    {
        // Arrange
        var expectedExample = "example_value";

        // Act
        var attribute = new ToolFieldAttribute
        {
            Example = expectedExample
        };

        // Assert
        Assert.AreEqual(expectedExample, attribute.Example);
    }

    [TestMethod]
    public void Example_Should_BeSet_When_NumberProvided()
    {
        // Arrange
        var expectedExample = 42;

        // Act
        var attribute = new ToolFieldAttribute
        {
            Example = expectedExample
        };

        // Assert
        Assert.AreEqual(expectedExample, attribute.Example);
    }

    [TestMethod]
    public void Example_Should_BeSet_When_BooleanProvided()
    {
        // Arrange
        var expectedExample = true;

        // Act
        var attribute = new ToolFieldAttribute
        {
            Example = expectedExample
        };

        // Assert
        Assert.AreEqual(expectedExample, attribute.Example);
    }

    [TestMethod]
    public void Required_Should_BeFalse_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.IsFalse(attribute.Required);
    }

    [TestMethod]
    public void Required_Should_BeSet_When_True()
    {
        // Act
        var attribute = new ToolFieldAttribute
        {
            Required = true
        };

        // Assert
        Assert.IsTrue(attribute.Required);
    }

    [TestMethod]
    public void Required_Should_BeSet_When_False()
    {
        // Act
        var attribute = new ToolFieldAttribute
        {
            Required = false
        };

        // Assert
        Assert.IsFalse(attribute.Required);
    }

    [TestMethod]
    public void MinLength_Should_BeNegativeOne_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.AreEqual(-1, attribute.MinLength);
    }

    [TestMethod]
    public void MinLength_Should_BeSet_When_Provided()
    {
        // Arrange
        var expectedMinLength = 5;

        // Act
        var attribute = new ToolFieldAttribute
        {
            MinLength = expectedMinLength
        };

        // Assert
        Assert.AreEqual(expectedMinLength, attribute.MinLength);
    }

    [TestMethod]
    public void MinLength_Should_BeSet_When_Zero()
    {
        // Act
        var attribute = new ToolFieldAttribute
        {
            MinLength = 0
        };

        // Assert
        Assert.AreEqual(0, attribute.MinLength);
    }

    [TestMethod]
    public void MaxLength_Should_BeNegativeOne_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.AreEqual(-1, attribute.MaxLength);
    }

    [TestMethod]
    public void MaxLength_Should_BeSet_When_Provided()
    {
        // Arrange
        var expectedMaxLength = 100;

        // Act
        var attribute = new ToolFieldAttribute
        {
            MaxLength = expectedMaxLength
        };

        // Assert
        Assert.AreEqual(expectedMaxLength, attribute.MaxLength);
    }

    [TestMethod]
    public void Minimum_Should_BeNaN_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.IsTrue(double.IsNaN(attribute.Minimum));
    }

    [TestMethod]
    public void Minimum_Should_BeSet_When_Provided()
    {
        // Arrange
        var expectedMinimum = 10.5;

        // Act
        var attribute = new ToolFieldAttribute
        {
            Minimum = expectedMinimum
        };

        // Assert
        Assert.AreEqual(expectedMinimum, attribute.Minimum);
    }

    [TestMethod]
    public void Minimum_Should_BeSet_When_Negative()
    {
        // Arrange
        var expectedMinimum = -100.0;

        // Act
        var attribute = new ToolFieldAttribute
        {
            Minimum = expectedMinimum
        };

        // Assert
        Assert.AreEqual(expectedMinimum, attribute.Minimum);
    }

    [TestMethod]
    public void Maximum_Should_BeNaN_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.IsTrue(double.IsNaN(attribute.Maximum));
    }

    [TestMethod]
    public void Maximum_Should_BeSet_When_Provided()
    {
        // Arrange
        var expectedMaximum = 1000.0;

        // Act
        var attribute = new ToolFieldAttribute
        {
            Maximum = expectedMaximum
        };

        // Assert
        Assert.AreEqual(expectedMaximum, attribute.Maximum);
    }

    [TestMethod]
    public void Pattern_Should_BeNull_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.IsNull(attribute.Pattern);
    }

    [TestMethod]
    public void Pattern_Should_BeSet_When_Provided()
    {
        // Arrange
        var expectedPattern = @"^\d{3}-\d{2}-\d{4}$";

        // Act
        var attribute = new ToolFieldAttribute
        {
            Pattern = expectedPattern
        };

        // Assert
        Assert.AreEqual(expectedPattern, attribute.Pattern);
    }

    [TestMethod]
    public void Format_Should_BeNull_When_NotSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute();

        // Act & Assert
        Assert.IsNull(attribute.Format);
    }

    [TestMethod]
    public void Format_Should_BeSet_When_Email()
    {
        // Arrange
        var expectedFormat = "email";

        // Act
        var attribute = new ToolFieldAttribute
        {
            Format = expectedFormat
        };

        // Assert
        Assert.AreEqual(expectedFormat, attribute.Format);
    }

    [TestMethod]
    public void Format_Should_BeSet_When_DateTime()
    {
        // Arrange
        var expectedFormat = "date-time";

        // Act
        var attribute = new ToolFieldAttribute
        {
            Format = expectedFormat
        };

        // Assert
        Assert.AreEqual(expectedFormat, attribute.Format);
    }

    [TestMethod]
    public void Format_Should_BeSet_When_Uri()
    {
        // Arrange
        var expectedFormat = "uri";

        // Act
        var attribute = new ToolFieldAttribute
        {
            Format = expectedFormat
        };

        // Assert
        Assert.AreEqual(expectedFormat, attribute.Format);
    }

    [TestMethod]
    public void ToolFieldAttribute_Should_BeUsableAsAttribute_When_AppliedToProperty()
    {
        // Arrange & Act
        var testClass = new TestClassWithToolField();

        // Assert
        var property = typeof(TestClassWithToolField).GetProperty("TestProperty");
        Assert.IsNotNull(property);
        
        var attribute = property.GetCustomAttributes(typeof(ToolFieldAttribute), false).FirstOrDefault() as ToolFieldAttribute;
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Test property", attribute.Description);
        Assert.IsTrue(attribute.Required);
    }

    [TestMethod]
    public void ToolFieldAttribute_Should_NotBeUsableAsAttribute_When_AppliedToClass()
    {
        // This test verifies that the attribute usage is correctly restricted
        
        // Arrange & Act
        var attributeType = typeof(ToolFieldAttribute);
        var attributeUsage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false).FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        Assert.IsNotNull(attributeUsage);
        Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Property));
        Assert.IsFalse(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
        Assert.IsFalse(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
    }

    [TestMethod]
    public void ToolFieldAttribute_Should_SupportAllProperties_When_AllSet()
    {
        // Arrange
        var attribute = new ToolFieldAttribute
        {
            Description = "Complete test attribute",
            Example = "example_value",
            Required = true,
            MinLength = 1,
            MaxLength = 100,
            Minimum = 0.0,
            Maximum = 1000.0,
            Pattern = @"^\w+$",
            Format = "string"
        };

        // Assert
        Assert.AreEqual("Complete test attribute", attribute.Description);
        Assert.AreEqual("example_value", attribute.Example);
        Assert.IsTrue(attribute.Required);
        Assert.AreEqual(1, attribute.MinLength);
        Assert.AreEqual(100, attribute.MaxLength);
        Assert.AreEqual(0.0, attribute.Minimum);
        Assert.AreEqual(1000.0, attribute.Maximum);
        Assert.AreEqual(@"^\w+$", attribute.Pattern);
        Assert.AreEqual("string", attribute.Format);
    }

    // Test class for attribute usage testing
    public class TestClassWithToolField
    {
        [ToolField(Description = "Test property", Required = true)]
        public string TestProperty { get; set; } = "";
    }
}
