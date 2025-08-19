namespace AIAgentSharp.Tests.Schema;

[TestClass]
public class ToolParamsAttributeTests
{
    [TestMethod]
    public void Constructor_Should_CreateToolParamsAttribute_When_Called()
    {
        // Act
        var attribute = new ToolParamsAttribute();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.IsInstanceOfType(attribute, typeof(Attribute));
    }

    [TestMethod]
    public void Description_Should_BeNull_When_NotSet()
    {
        // Arrange
        var attribute = new ToolParamsAttribute();

        // Act & Assert
        Assert.IsNull(attribute.Description);
    }

    [TestMethod]
    public void Description_Should_BeSet_When_Provided()
    {
        // Arrange
        var expectedDescription = "Test parameter description";

        // Act
        var attribute = new ToolParamsAttribute
        {
            Description = expectedDescription
        };

        // Assert
        Assert.AreEqual(expectedDescription, attribute.Description);
    }

    [TestMethod]
    public void Description_Should_BeSet_When_EmptyString()
    {
        // Arrange
        var expectedDescription = "";

        // Act
        var attribute = new ToolParamsAttribute
        {
            Description = expectedDescription
        };

        // Assert
        Assert.AreEqual(expectedDescription, attribute.Description);
    }

    [TestMethod]
    public void Description_Should_BeSet_When_WhitespaceString()
    {
        // Arrange
        var expectedDescription = "   ";

        // Act
        var attribute = new ToolParamsAttribute
        {
            Description = expectedDescription
        };

        // Assert
        Assert.AreEqual(expectedDescription, attribute.Description);
    }

    [TestMethod]
    public void Description_Should_BeSet_When_LongString()
    {
        // Arrange
        var expectedDescription = "This is a very long description that contains multiple words and should be handled properly by the ToolParamsAttribute class. It should support long descriptions without any issues.";

        // Act
        var attribute = new ToolParamsAttribute
        {
            Description = expectedDescription
        };

        // Assert
        Assert.AreEqual(expectedDescription, attribute.Description);
    }

    [TestMethod]
    public void Description_Should_BeSet_When_SpecialCharacters()
    {
        // Arrange
        var expectedDescription = "Description with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var attribute = new ToolParamsAttribute
        {
            Description = expectedDescription
        };

        // Assert
        Assert.AreEqual(expectedDescription, attribute.Description);
    }

    [TestMethod]
    public void Description_Should_BeSet_When_UnicodeCharacters()
    {
        // Arrange
        var expectedDescription = "Description with unicode: 你好世界 🌍 🚀";

        // Act
        var attribute = new ToolParamsAttribute
        {
            Description = expectedDescription
        };

        // Assert
        Assert.AreEqual(expectedDescription, attribute.Description);
    }

    [TestMethod]
    public void ToolParamsAttribute_Should_BeUsableAsAttribute_When_AppliedToClass()
    {
        // Arrange & Act
        var testClass = new TestClassWithToolParams();

        // Assert
        var attribute = typeof(TestClassWithToolParams).GetCustomAttributes(typeof(ToolParamsAttribute), false).FirstOrDefault() as ToolParamsAttribute;
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Test parameters", attribute.Description);
    }

    [TestMethod]
    public void ToolParamsAttribute_Should_BeUsableAsAttribute_When_AppliedToStruct()
    {
        // Arrange & Act
        var testStruct = new TestStructWithToolParams();

        // Assert
        var attribute = typeof(TestStructWithToolParams).GetCustomAttributes(typeof(ToolParamsAttribute), false).FirstOrDefault() as ToolParamsAttribute;
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Test struct parameters", attribute.Description);
    }

    [TestMethod]
    public void ToolParamsAttribute_Should_NotBeUsableAsAttribute_When_AppliedToMethod()
    {
        // This test verifies that the attribute usage is correctly restricted
        // The compiler should prevent this, but we can test the attribute usage definition
        
        // Arrange & Act
        var attributeType = typeof(ToolParamsAttribute);
        var attributeUsage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false).FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        Assert.IsNotNull(attributeUsage);
        Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
        Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Struct));
        Assert.IsFalse(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
        Assert.IsFalse(attributeUsage.ValidOn.HasFlag(AttributeTargets.Property));
    }

    // Test classes for attribute usage testing
    [ToolParams(Description = "Test parameters")]
    public class TestClassWithToolParams
    {
        public string Property { get; set; } = "";
    }

    [ToolParams(Description = "Test struct parameters")]
    public struct TestStructWithToolParams
    {
        public string Property { get; set; }
    }
}
