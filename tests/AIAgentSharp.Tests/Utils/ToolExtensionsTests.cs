using Moq;

namespace AIAgentSharp.Tests.Utils;

[TestClass]
public class ToolExtensionsTests
{
    private Mock<ITool> _mockTool1;
    private Mock<ITool> _mockTool2;
    private Mock<ITool> _mockTool3;

    [TestInitialize]
    public void Setup()
    {
        _mockTool1 = new Mock<ITool>();
        _mockTool1.Setup(t => t.Name).Returns("tool1");

        _mockTool2 = new Mock<ITool>();
        _mockTool2.Setup(t => t.Name).Returns("tool2");

        _mockTool3 = new Mock<ITool>();
        _mockTool3.Setup(t => t.Name).Returns("tool3");
    }

    [TestMethod]
    public void RequireTool_Should_ReturnTool_When_ToolExists()
    {
        // Arrange
        var registry = new Dictionary<string, ITool>
        {
            { "tool1", _mockTool1.Object },
            { "tool2", _mockTool2.Object }
        };

        // Act
        var result = registry.RequireTool("tool1");

        // Assert
        Assert.AreEqual(_mockTool1.Object, result);
    }

    [TestMethod]
    public void RequireTool_Should_ReturnTool_When_ToolExistsWithExactCase()
    {
        // Arrange
        var registry = new Dictionary<string, ITool>
        {
            { "Tool1", _mockTool1.Object },
            { "TOOL2", _mockTool2.Object }
        };

        // Act
        var result = registry.RequireTool("Tool1");

        // Assert
        Assert.AreEqual(_mockTool1.Object, result);
    }

    [TestMethod]
    public void RequireTool_Should_ThrowKeyNotFoundException_When_ToolDoesNotExist()
    {
        // Arrange
        var registry = new Dictionary<string, ITool>
        {
            { "tool1", _mockTool1.Object },
            { "tool2", _mockTool2.Object }
        };

        // Act & Assert
        var exception = Assert.ThrowsException<KeyNotFoundException>(() => 
            registry.RequireTool("nonexistent"));

        Assert.IsTrue(exception.Message.Contains("Tool 'nonexistent' not found"));
        Assert.IsTrue(exception.Message.Contains("tool1"));
        Assert.IsTrue(exception.Message.Contains("tool2"));
    }

    [TestMethod]
    public void RequireTool_Should_ThrowKeyNotFoundException_When_RegistryIsEmpty()
    {
        // Arrange
        var registry = new Dictionary<string, ITool>();

        // Act & Assert
        var exception = Assert.ThrowsException<KeyNotFoundException>(() => 
            registry.RequireTool("anytool"));

        Assert.IsTrue(exception.Message.Contains("Tool 'anytool' not found"));
        Assert.IsTrue(exception.Message.Contains("Available: "));
    }

    [TestMethod]
    public void RequireTool_Should_ThrowKeyNotFoundException_When_RegistryIsNull()
    {
        // Arrange
        IDictionary<string, ITool>? registry = null;

        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() => 
            registry!.RequireTool("anytool"));
    }

    [TestMethod]
    public void RequireTool_Should_HandleSingleToolRegistry()
    {
        // Arrange
        var registry = new Dictionary<string, ITool>
        {
            { "singletool", _mockTool1.Object }
        };

        // Act
        var result = registry.RequireTool("singletool");

        // Assert
        Assert.AreEqual(_mockTool1.Object, result);
    }

    [TestMethod]
    public void RequireTool_Should_HandleLargeRegistry()
    {
        // Arrange
        var registry = new Dictionary<string, ITool>();
        var tools = new List<ITool>();

        for (int i = 0; i < 100; i++)
        {
            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns($"tool{i}");
            registry.Add($"tool{i}", mockTool.Object);
            tools.Add(mockTool.Object);
        }

        // Act
        var result = registry.RequireTool("tool50");

        // Assert
        Assert.AreEqual(tools[50], result);
    }

    [TestMethod]
    public void ToRegistry_Should_ConvertIEnumerableToDictionary()
    {
        // Arrange
        var tools = new List<ITool>
        {
            _mockTool1.Object,
            _mockTool2.Object,
            _mockTool3.Object
        };

        // Act
        var result = tools.ToRegistry();

        // Assert
        Assert.IsInstanceOfType(result, typeof(IDictionary<string, ITool>));
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(_mockTool1.Object, result["tool1"]);
        Assert.AreEqual(_mockTool2.Object, result["tool2"]);
        Assert.AreEqual(_mockTool3.Object, result["tool3"]);
    }

    [TestMethod]
    public void ToRegistry_Should_UseCaseInsensitiveComparer()
    {
        // Arrange
        var tools = new List<ITool>
        {
            _mockTool1.Object,
            _mockTool2.Object
        };

        // Act
        var result = tools.ToRegistry();

        // Assert
        // The dictionary uses StringComparer.OrdinalIgnoreCase, so all case variations should work
        Assert.AreEqual(_mockTool1.Object, result["TOOL1"]);
        Assert.AreEqual(_mockTool1.Object, result["Tool1"]);
        Assert.AreEqual(_mockTool1.Object, result["tool1"]);
        Assert.AreEqual(_mockTool2.Object, result["TOOL2"]);
        Assert.AreEqual(_mockTool2.Object, result["Tool2"]);
        Assert.AreEqual(_mockTool2.Object, result["tool2"]);
    }

    [TestMethod]
    public void ToRegistry_Should_HandleEmptyCollection()
    {
        // Arrange
        var tools = new List<ITool>();

        // Act
        var result = tools.ToRegistry();

        // Assert
        Assert.IsInstanceOfType(result, typeof(IDictionary<string, ITool>));
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ToRegistry_Should_HandleNullCollection()
    {
        // Arrange
        IEnumerable<ITool>? tools = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => tools!.ToRegistry());
    }

    [TestMethod]
    public void ToRegistry_Should_HandleSingleToolCollection()
    {
        // Arrange
        var tools = new List<ITool> { _mockTool1.Object };

        // Act
        var result = tools.ToRegistry();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(_mockTool1.Object, result["tool1"]);
    }

    [TestMethod]
    public void ToRegistry_Should_HandleDuplicateToolNames()
    {
        // Arrange
        var mockTool1Duplicate = new Mock<ITool>();
        mockTool1Duplicate.Setup(t => t.Name).Returns("tool1");

        var tools = new List<ITool>
        {
            _mockTool1.Object,
            mockTool1Duplicate.Object
        };

        // Act & Assert
        // Should throw because dictionary keys must be unique
        Assert.ThrowsException<ArgumentException>(() => tools.ToRegistry());
    }

    [TestMethod]
    public void ToRegistry_Should_HandleNullToolNames()
    {
        // Arrange
        var mockToolWithNullName = new Mock<ITool>();
        mockToolWithNullName.Setup(t => t.Name).Returns((string?)null);

        var tools = new List<ITool> { mockToolWithNullName.Object };

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => tools.ToRegistry());
    }

    [TestMethod]
    public void ToRegistry_Should_HandleEmptyToolNames()
    {
        // Arrange
        var mockToolWithEmptyName = new Mock<ITool>();
        mockToolWithEmptyName.Setup(t => t.Name).Returns("");

        var tools = new List<ITool> { mockToolWithEmptyName.Object };

        // Act
        var result = tools.ToRegistry();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(mockToolWithEmptyName.Object, result[""]);
    }

    [TestMethod]
    public void ToRegistry_Should_HandleWhitespaceToolNames()
    {
        // Arrange
        var mockToolWithWhitespaceName = new Mock<ITool>();
        mockToolWithWhitespaceName.Setup(t => t.Name).Returns("   ");

        var tools = new List<ITool> { mockToolWithWhitespaceName.Object };

        // Act
        var result = tools.ToRegistry();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(mockToolWithWhitespaceName.Object, result["   "]);
    }

    [TestMethod]
    public void ToRegistry_Should_HandleLargeCollection()
    {
        // Arrange
        var tools = new List<ITool>();
        for (int i = 0; i < 1000; i++)
        {
            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns($"tool{i}");
            tools.Add(mockTool.Object);
        }

        // Act
        var result = tools.ToRegistry();

        // Assert
        Assert.AreEqual(1000, result.Count);
        for (int i = 0; i < 1000; i++)
        {
            Assert.IsTrue(result.ContainsKey($"tool{i}"));
        }
    }

    [TestMethod]
    public void ToRegistry_Should_HandleDifferentCollectionTypes()
    {
        // Arrange
        var toolsArray = new ITool[] { _mockTool1.Object, _mockTool2.Object };
        var toolsList = new List<ITool> { _mockTool1.Object, _mockTool2.Object };
        var toolsEnumerable = toolsList.AsEnumerable();

        // Act
        var resultArray = toolsArray.ToRegistry();
        var resultList = toolsList.ToRegistry();
        var resultEnumerable = toolsEnumerable.ToRegistry();

        // Assert
        Assert.AreEqual(2, resultArray.Count);
        Assert.AreEqual(2, resultList.Count);
        Assert.AreEqual(2, resultEnumerable.Count);
        Assert.AreEqual(_mockTool1.Object, resultArray["tool1"]);
        Assert.AreEqual(_mockTool2.Object, resultArray["tool2"]);
    }
}
