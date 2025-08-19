namespace AIAgentSharp.Tests.Schema;

[TestClass]
public class IToolSchemaTests
{
    // Test implementation of IToolSchema
    public class TestToolSchema : IToolSchema
    {
        public Type ParameterType { get; }
        public Type ResultType { get; }

        public TestToolSchema(Type parameterType, Type resultType)
        {
            ParameterType = parameterType;
            ResultType = resultType;
        }
    }

    // Test parameter and result types
    public class TestParameters
    {
        public string Input { get; set; } = "";
    }

    public class TestResult
    {
        public string Output { get; set; } = "";
    }

    [TestMethod]
    public void IToolSchema_Should_DefineParameterTypeProperty_When_InterfaceIsDefined()
    {
        // Arrange
        var parameterType = typeof(TestParameters);
        var resultType = typeof(TestResult);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.IsNotNull(toolSchema.ParameterType);
        Assert.AreEqual(parameterType, toolSchema.ParameterType);
    }

    [TestMethod]
    public void IToolSchema_Should_DefineResultTypeProperty_When_InterfaceIsDefined()
    {
        // Arrange
        var parameterType = typeof(TestParameters);
        var resultType = typeof(TestResult);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.IsNotNull(toolSchema.ResultType);
        Assert.AreEqual(resultType, toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportPrimitiveTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(string);
        var resultType = typeof(int);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(string), toolSchema.ParameterType);
        Assert.AreEqual(typeof(int), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportValueTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(int);
        var resultType = typeof(bool);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(int), toolSchema.ParameterType);
        Assert.AreEqual(typeof(bool), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportNullableTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(int?);
        var resultType = typeof(string);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(int?), toolSchema.ParameterType);
        Assert.AreEqual(typeof(string), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportGenericTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(List<string>);
        var resultType = typeof(Dictionary<string, object>);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(List<string>), toolSchema.ParameterType);
        Assert.AreEqual(typeof(Dictionary<string, object>), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportSameType_When_ParameterAndResultAreSame()
    {
        // Arrange
        var sameType = typeof(string);

        // Act
        var toolSchema = new TestToolSchema(sameType, sameType);

        // Assert
        Assert.AreEqual(sameType, toolSchema.ParameterType);
        Assert.AreEqual(sameType, toolSchema.ResultType);
        Assert.AreSame(toolSchema.ParameterType, toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportObjectResult_When_ResultTypeIsObject()
    {
        // Arrange
        var parameterType = typeof(TestParameters);
        var resultType = typeof(object);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(TestParameters), toolSchema.ParameterType);
        Assert.AreEqual(typeof(object), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportObjectTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(object);
        var resultType = typeof(object);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(object), toolSchema.ParameterType);
        Assert.AreEqual(typeof(object), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportInterfaceTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(IToolSchema);
        var resultType = typeof(IDisposable);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(IToolSchema), toolSchema.ParameterType);
        Assert.AreEqual(typeof(IDisposable), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportArrayTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(string[]);
        var resultType = typeof(int[]);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(string[]), toolSchema.ParameterType);
        Assert.AreEqual(typeof(int[]), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportEnumTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(DayOfWeek);
        var resultType = typeof(ConsoleColor);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(DayOfWeek), toolSchema.ParameterType);
        Assert.AreEqual(typeof(ConsoleColor), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportComplexTypes_When_UsedAsParameterType()
    {
        // Arrange
        var parameterType = typeof(TestParameters);
        var resultType = typeof(TestResult);

        // Act
        var toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.AreEqual(typeof(TestParameters), toolSchema.ParameterType);
        Assert.AreEqual(typeof(TestResult), toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_BeCastableToInterface_When_Implemented()
    {
        // Arrange
        var parameterType = typeof(TestParameters);
        var resultType = typeof(TestResult);

        // Act
        IToolSchema toolSchema = new TestToolSchema(parameterType, resultType);

        // Assert
        Assert.IsNotNull(toolSchema);
        Assert.IsInstanceOfType(toolSchema, typeof(IToolSchema));
        Assert.AreEqual(parameterType, toolSchema.ParameterType);
        Assert.AreEqual(resultType, toolSchema.ResultType);
    }

    [TestMethod]
    public void IToolSchema_Should_SupportMultipleImplementations_When_InterfaceIsUsed()
    {
        // Arrange
        var parameterType1 = typeof(string);
        var resultType1 = typeof(int);
        var parameterType2 = typeof(int);
        var resultType2 = typeof(string);

        // Act
        IToolSchema toolSchema1 = new TestToolSchema(parameterType1, resultType1);
        IToolSchema toolSchema2 = new TestToolSchema(parameterType2, resultType2);

        // Assert
        Assert.AreEqual(parameterType1, toolSchema1.ParameterType);
        Assert.AreEqual(resultType1, toolSchema1.ResultType);
        Assert.AreEqual(parameterType2, toolSchema2.ParameterType);
        Assert.AreEqual(resultType2, toolSchema2.ResultType);
    }
}
