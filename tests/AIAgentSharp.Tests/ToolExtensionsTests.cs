namespace AIAgentSharp.Tests;

[TestClass]
public sealed class ToolExtensionsTests
{
    [TestMethod]
    public void RequireTool_ExistingTool_ReturnsTool()
    {
        // Arrange
        var tool = new TestTool("test-tool");
        var registry = new Dictionary<string, ITool> { { "test-tool", tool } };

        // Act
        var result = registry.RequireTool("test-tool");

        // Assert
        Assert.AreEqual(tool, result);
    }

    [TestMethod]
    public void RequireTool_ExistingToolCaseInsensitive_ReturnsTool()
    {
        // Arrange
        var tool = new TestTool("test-tool");
        var registry = new Dictionary<string, ITool> { { "test-tool", tool } };

        // Act & Assert
        Assert.ThrowsException<KeyNotFoundException>(() => registry.RequireTool("TEST-TOOL"));
    }

    [TestMethod]
    public void RequireTool_NonExistentTool_ThrowsKeyNotFoundException()
    {
        // Arrange
        var registry = new Dictionary<string, ITool>();

        // Act & Assert
        Assert.ThrowsException<KeyNotFoundException>(() => registry.RequireTool("non-existent-tool"));
    }

    [TestMethod]
    public void RequireTool_EmptyRegistry_ThrowsKeyNotFoundException()
    {
        // Arrange
        var registry = new Dictionary<string, ITool>();

        // Act & Assert
        Assert.ThrowsException<KeyNotFoundException>(() => registry.RequireTool("any-tool"));
    }

    [TestMethod]
    public void RequireTool_NullRegistry_ThrowsNullReferenceException()
    {
        // Arrange
        Dictionary<string, ITool>? registry = null;

        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() => registry!.RequireTool("any-tool"));
    }

    [TestMethod]
    public void ToRegistry_EmptyEnumerable_ReturnsEmptyRegistry()
    {
        // Arrange
        var tools = Enumerable.Empty<ITool>();

        // Act
        var registry = tools.ToRegistry();

        // Assert
        Assert.IsNotNull(registry);
        Assert.AreEqual(0, registry.Count);
    }

    [TestMethod]
    public void ToRegistry_SingleTool_ReturnsRegistryWithTool()
    {
        // Arrange
        var tool = new TestTool("test-tool");
        var tools = new[] { tool };

        // Act
        var registry = tools.ToRegistry();

        // Assert
        Assert.IsNotNull(registry);
        Assert.AreEqual(1, registry.Count);
        Assert.IsTrue(registry.ContainsKey("test-tool"));
        Assert.AreEqual(tool, registry["test-tool"]);
    }

    [TestMethod]
    public void ToRegistry_MultipleTools_ReturnsRegistryWithAllTools()
    {
        // Arrange
        var tool1 = new TestTool("tool-1");
        var tool2 = new TestTool("tool-2");
        var tool3 = new TestTool("tool-3");
        var tools = new[] { tool1, tool2, tool3 };

        // Act
        var registry = tools.ToRegistry();

        // Assert
        Assert.IsNotNull(registry);
        Assert.AreEqual(3, registry.Count);
        Assert.IsTrue(registry.ContainsKey("tool-1"));
        Assert.IsTrue(registry.ContainsKey("tool-2"));
        Assert.IsTrue(registry.ContainsKey("tool-3"));
        Assert.AreEqual(tool1, registry["tool-1"]);
        Assert.AreEqual(tool2, registry["tool-2"]);
        Assert.AreEqual(tool3, registry["tool-3"]);
    }

    [TestMethod]
    public void ToRegistry_DuplicateToolNames_ThrowsArgumentException()
    {
        // Arrange
        var tool1 = new TestTool("duplicate-tool");
        var tool2 = new TestTool("duplicate-tool");
        var tools = new[] { tool1, tool2 };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => tools.ToRegistry());
    }

    [TestMethod]
    public void ToRegistry_CaseInsensitiveDuplicateNames_ThrowsArgumentException()
    {
        // Arrange
        var tool1 = new TestTool("Test-Tool");
        var tool2 = new TestTool("test-tool");
        var tools = new[] { tool1, tool2 };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => tools.ToRegistry());
    }

    [TestMethod]
    public void ToRegistry_NullEnumerable_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<ITool>? tools = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => tools!.ToRegistry());
    }

    [TestMethod]
    public void ToRegistry_ListOfTools_ReturnsRegistryWithAllTools()
    {
        // Arrange
        var tool1 = new TestTool("list-tool-1");
        var tool2 = new TestTool("list-tool-2");
        var tools = new List<ITool> { tool1, tool2 };

        // Act
        var registry = tools.ToRegistry();

        // Assert
        Assert.IsNotNull(registry);
        Assert.AreEqual(2, registry.Count);
        Assert.IsTrue(registry.ContainsKey("list-tool-1"));
        Assert.IsTrue(registry.ContainsKey("list-tool-2"));
        Assert.AreEqual(tool1, registry["list-tool-1"]);
        Assert.AreEqual(tool2, registry["list-tool-2"]);
    }

    [TestMethod]
    public void ToRegistry_ArrayOfTools_ReturnsRegistryWithAllTools()
    {
        // Arrange
        var tool1 = new TestTool("array-tool-1");
        var tool2 = new TestTool("array-tool-2");
        var tools = new ITool[] { tool1, tool2 };

        // Act
        var registry = tools.ToRegistry();

        // Assert
        Assert.IsNotNull(registry);
        Assert.AreEqual(2, registry.Count);
        Assert.IsTrue(registry.ContainsKey("array-tool-1"));
        Assert.IsTrue(registry.ContainsKey("array-tool-2"));
        Assert.AreEqual(tool1, registry["array-tool-1"]);
        Assert.AreEqual(tool2, registry["array-tool-2"]);
    }

    [TestMethod]
    public void ToRegistry_RegistryIsCaseInsensitive()
    {
        // Arrange
        var tool = new TestTool("CaseSensitiveTool");
        var tools = new[] { tool };

        // Act
        var registry = tools.ToRegistry();

        // Assert
        Assert.IsTrue(registry.ContainsKey("CaseSensitiveTool"));
        Assert.IsTrue(registry.ContainsKey("casesensitivetool"));
        Assert.IsTrue(registry.ContainsKey("CASESENSITIVETOOL"));
        Assert.AreEqual(tool, registry["CaseSensitiveTool"]);
        Assert.AreEqual(tool, registry["casesensitivetool"]);
        Assert.AreEqual(tool, registry["CASESENSITIVETOOL"]);
    }

    [TestMethod]
    public void RequireTool_RegistryIsCaseSensitive()
    {
        // Arrange
        var tool = new TestTool("CaseSensitiveTool");
        var registry = new Dictionary<string, ITool> { { "CaseSensitiveTool", tool } };

        // Act & Assert
        Assert.AreEqual(tool, registry.RequireTool("CaseSensitiveTool"));
        Assert.ThrowsException<KeyNotFoundException>(() => registry.RequireTool("casesensitivetool"));
        Assert.ThrowsException<KeyNotFoundException>(() => registry.RequireTool("CASESENSITIVETOOL"));
    }

    private sealed class TestTool : ITool
    {
        public TestTool(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
        {
            return Task.FromResult<object?>(new { result = $"Test result for {Name}" });
        }
    }
}