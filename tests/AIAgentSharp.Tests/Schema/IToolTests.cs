namespace AIAgentSharp.Tests.Schema;

[TestClass]
public class IToolTests
{
    // Test implementation of ITool<TParams, TResult>
    public class TestTool<TParams, TResult> : ITool<TParams, TResult>
    {
        public string Name { get; } = "TestTool";
        public string Description { get; } = "Test tool description";

        public Task<TResult> InvokeAsync(TParams parameters)
        {
            // Mock implementation
            return Task.FromResult(default(TResult)!);
        }

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            // Mock implementation for the non-generic ITool interface
            return Task.FromResult<object?>(null);
        }

        public string GetJsonSchema()
        {
            return "{}";
        }

        public string Describe()
        {
            return Description;
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
    public void ITool_Should_DefineParameterTypeProperty_When_InterfaceIsDefined()
    {
        // Arrange
        var tool = new TestTool<TestParameters, TestResult>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;

        // Assert
        Assert.IsNotNull(parameterType);
        Assert.AreEqual(typeof(TestParameters), parameterType);
    }

    [TestMethod]
    public void ITool_Should_DefineResultTypeProperty_When_InterfaceIsDefined()
    {
        // Arrange
        var tool = new TestTool<TestParameters, TestResult>();

        // Act
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.IsNotNull(resultType);
        Assert.AreEqual(typeof(TestResult), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportPrimitiveTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<string, int>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(string), parameterType);
        Assert.AreEqual(typeof(int), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportValueTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<int, bool>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(int), parameterType);
        Assert.AreEqual(typeof(bool), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportNullableTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<int?, string?>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(int?), parameterType);
        Assert.AreEqual(typeof(string), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportGenericTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<List<string>, Dictionary<string, object>>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(List<string>), parameterType);
        Assert.AreEqual(typeof(Dictionary<string, object>), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportSameType_When_ParameterAndResultAreSame()
    {
        // Arrange
        var tool = new TestTool<string, string>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(string), parameterType);
        Assert.AreEqual(typeof(string), resultType);
        Assert.AreSame(parameterType, resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportObjectTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<object, object>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(object), parameterType);
        Assert.AreEqual(typeof(object), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportInterfaceTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<IToolSchema, IDisposable>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(IToolSchema), parameterType);
        Assert.AreEqual(typeof(IDisposable), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportArrayTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<string[], int[]>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(string[]), parameterType);
        Assert.AreEqual(typeof(int[]), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportEnumTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<DayOfWeek, ConsoleColor>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(DayOfWeek), parameterType);
        Assert.AreEqual(typeof(ConsoleColor), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportComplexTypes_When_UsedAsGenericParameters()
    {
        // Arrange
        var tool = new TestTool<TestParameters, TestResult>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(TestParameters), parameterType);
        Assert.AreEqual(typeof(TestResult), resultType);
    }

    [TestMethod]
    public void ITool_Should_BeCastableToIToolSchema_When_Implemented()
    {
        // Arrange
        var tool = new TestTool<TestParameters, TestResult>();

        // Act
        IToolSchema toolSchema = tool;

        // Assert
        Assert.IsNotNull(toolSchema);
        Assert.IsInstanceOfType(toolSchema, typeof(IToolSchema));
        Assert.AreEqual(typeof(TestParameters), toolSchema.ParameterType);
        Assert.AreEqual(typeof(TestResult), toolSchema.ResultType);
    }

    [TestMethod]
    public void ITool_Should_BeCastableToITool_When_Implemented()
    {
        // Arrange
        var tool = new TestTool<TestParameters, TestResult>();

        // Act
        ITool baseTool = tool;

        // Assert
        Assert.IsNotNull(baseTool);
        Assert.IsInstanceOfType(baseTool, typeof(ITool));
        Assert.AreEqual("TestTool", baseTool.Name);
    }

    [TestMethod]
    public void ITool_Should_SupportMultipleImplementations_When_InterfaceIsUsed()
    {
        // Arrange
        var tool1 = new TestTool<string, int>();
        var tool2 = new TestTool<int, string>();

        // Act
        var parameterType1 = ((IToolSchema)tool1).ParameterType;
        var resultType1 = ((IToolSchema)tool1).ResultType;
        var parameterType2 = ((IToolSchema)tool2).ParameterType;
        var resultType2 = ((IToolSchema)tool2).ResultType;

        // Assert
        Assert.AreEqual(typeof(string), parameterType1);
        Assert.AreEqual(typeof(int), resultType1);
        Assert.AreEqual(typeof(int), parameterType2);
        Assert.AreEqual(typeof(string), resultType2);
    }

    [TestMethod]
    public void ITool_Should_SupportObjectResult_When_ResultTypeIsObject()
    {
        // Arrange
        var tool = new TestTool<TestParameters, object>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(TestParameters), parameterType);
        Assert.AreEqual(typeof(object), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportTaskResult_When_ResultTypeIsTask()
    {
        // Arrange
        var tool = new TestTool<TestParameters, Task<TestResult>>();

        // Act
        var parameterType = ((IToolSchema)tool).ParameterType;
        var resultType = ((IToolSchema)tool).ResultType;

        // Assert
        Assert.AreEqual(typeof(TestParameters), parameterType);
        Assert.AreEqual(typeof(Task<TestResult>), resultType);
    }

    [TestMethod]
    public void ITool_Should_SupportAsyncInvoke_When_InvokeAsyncIsCalled()
    {
        // Arrange
        var tool = new TestTool<TestParameters, TestResult>();
        var parameters = new TestParameters { Input = "test" };

        // Act
        var result = tool.InvokeAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(Task<TestResult>));
    }

    [TestMethod]
    public void ITool_Should_SupportGetJsonSchema_When_GetJsonSchemaIsCalled()
    {
        // Arrange
        var tool = new TestTool<TestParameters, TestResult>();

        // Act
        var schema = tool.GetJsonSchema();

        // Assert
        Assert.IsNotNull(schema);
        Assert.AreEqual("{}", schema);
    }

    [TestMethod]
    public void ITool_Should_SupportDescribe_When_DescribeIsCalled()
    {
        // Arrange
        var tool = new TestTool<TestParameters, TestResult>();

        // Act
        var description = tool.Describe();

        // Assert
        Assert.IsNotNull(description);
        Assert.AreEqual("Test tool description", description);
    }
}
