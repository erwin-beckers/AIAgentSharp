# Testing Guidelines

This guide outlines the testing standards and best practices for AIAgentSharp. Comprehensive testing is crucial for maintaining code quality, preventing regressions, and ensuring reliable AI agent behavior.

## Testing Philosophy

- **Test-Driven Development (TDD)**: Write tests before implementing features when possible
- **Comprehensive Coverage**: Aim for high test coverage across all components
- **Realistic Testing**: Test with realistic data and scenarios
- **Performance Testing**: Include performance benchmarks for critical paths
- **Integration Testing**: Test component interactions and end-to-end workflows

## Test Categories

### 1. Unit Tests

Unit tests verify individual components in isolation.

```csharp
[TestFixture]
public class AgentTests
{
    private Mock<ILLMClient> _llmClientMock;
    private Mock<ILogger<Agent>> _loggerMock;
    private Agent _agent;

    [SetUp]
    public void Setup()
    {
        _llmClientMock = new Mock<ILLMClient>();
        _loggerMock = new Mock<ILogger<Agent>>();
        _agent = new Agent(_llmClientMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var input = "test input";
        var expectedResponse = "test response";
        
        _llmClientMock.Setup(x => x.GenerateResponseAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Output, Is.EqualTo(expectedResponse));
    }

    [Test]
    public async Task ExecuteAsync_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _agent.ExecuteAsync(null));
        
        Assert.That(exception.ParamName, Is.EqualTo("input"));
    }

    [Test]
    public async Task ExecuteAsync_WhenLLMClientFails_ThrowsAgentExecutionException()
    {
        // Arrange
        var input = "test input";
        _llmClientMock.Setup(x => x.GenerateResponseAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<AgentExecutionException>(
            async () => await _agent.ExecuteAsync(input));
        
        Assert.That(exception.InnerException, Is.InstanceOf<HttpRequestException>());
    }
}
```

### 2. Integration Tests

Integration tests verify component interactions and external dependencies.

```csharp
[TestFixture]
public class AgentIntegrationTests
{
    private TestServer _server;
    private HttpClient _client;
    private IServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public void Setup()
    {
        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ILLMClient, MockLLMClient>();
                services.AddSingleton<IToolRegistry, ToolRegistry>();
                services.AddSingleton<IAgentFactory, AgentFactory>();
            });

        _server = new TestServer(builder);
        _client = _server.CreateClient();
        _serviceProvider = _server.Services;
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    [Test]
    public async Task AgentExecution_WithToolIntegration_CompletesSuccessfully()
    {
        // Arrange
        var agentFactory = _serviceProvider.GetRequiredService<IAgentFactory>();
        var agent = await agentFactory.CreateAgentAsync("test-agent");
        
        var toolRegistry = _serviceProvider.GetRequiredService<IToolRegistry>();
        await toolRegistry.RegisterToolAsync(new WeatherTool());

        // Act
        var result = await agent.ExecuteAsync("What's the weather in London?");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Output, Is.Not.Empty);
    }

    [Test]
    public async Task AgentState_WithPersistence_RetainsDataBetweenExecutions()
    {
        // Arrange
        var agentFactory = _serviceProvider.GetRequiredService<IAgentFactory>();
        var agent = await agentFactory.CreateAgentAsync("state-test-agent");

        // Act
        await agent.ExecuteAsync("Remember that I like pizza");
        var result = await agent.ExecuteAsync("What do I like?");

        // Assert
        Assert.That(result.Output, Contains.Substring("pizza"));
    }
}

public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAIAgentSharp();
        services.AddSingleton<ILLMClient, MockLLMClient>();
        services.AddSingleton<IStateManager, InMemoryStateManager>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseAIAgentSharp();
    }
}
```

### 3. End-to-End Tests

E2E tests verify complete workflows from user input to final output.

```csharp
[TestFixture]
public class AgentE2ETests
{
    private TestServer _server;
    private HttpClient _client;

    [OneTimeSetUp]
    public void Setup()
    {
        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>();

        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    [Test]
    public async Task TravelPlanningAgent_CompleteWorkflow_ReturnsTravelPlan()
    {
        // Arrange
        var request = new
        {
            destination = "Paris",
            startDate = "2024-06-01",
            endDate = "2024-06-07",
            budget = 5000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agents/travel-planning", request);
        var result = await response.Content.ReadFromJsonAsync<AgentResult>();

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Output, Contains.Substring("Paris"));
        Assert.That(result.Output, Contains.Substring("2024-06-01"));
    }

    [Test]
    public async Task AgentAPI_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new { invalidField = "test" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agents/travel-planning", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
```

### 4. Performance Tests

Performance tests verify system performance under various conditions.

```csharp
[TestFixture]
public class AgentPerformanceTests
{
    private IAgent _agent;
    private Stopwatch _stopwatch;

    [SetUp]
    public void Setup()
    {
        var llmClient = new MockLLMClient();
        _agent = new Agent(llmClient);
        _stopwatch = new Stopwatch();
    }

    [Test]
    public async Task AgentExecution_UnderLoad_CompletesWithinTimeLimit()
    {
        // Arrange
        var inputs = Enumerable.Range(1, 100)
            .Select(i => $"Test input {i}")
            .ToArray();

        var tasks = inputs.Select(input => _agent.ExecuteAsync(input));
        _stopwatch.Start();

        // Act
        var results = await Task.WhenAll(tasks);
        _stopwatch.Stop();

        // Assert
        Assert.That(_stopwatch.ElapsedMilliseconds, Is.LessThan(30000)); // 30 seconds
        Assert.That(results.All(r => r.IsSuccess), Is.True);
    }

    [Test]
    public async Task AgentMemory_WithLargeState_DoesNotExceedMemoryLimit()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var largeInput = new string('x', 10000);

        // Act
        for (int i = 0; i < 100; i++)
        {
            await _agent.ExecuteAsync(largeInput);
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        Assert.That(memoryIncrease, Is.LessThan(50 * 1024 * 1024)); // 50MB limit
    }

    [Test]
    public async Task AgentThroughput_ConcurrentRequests_HandlesLoad()
    {
        // Arrange
        var concurrentTasks = 10;
        var requestsPerTask = 10;
        var semaphore = new SemaphoreSlim(concurrentTasks);

        // Act
        var tasks = Enumerable.Range(1, concurrentTasks * requestsPerTask)
            .Select(async i =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await _agent.ExecuteAsync($"Request {i}");
                }
                finally
                {
                    semaphore.Release();
                }
            });

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.That(results.All(r => r.IsSuccess), Is.True);
    }
}
```

## Mocking Strategies

### 1. LLM Client Mocking

```csharp
public class MockLLMClient : ILLMClient
{
    private readonly Dictionary<string, string> _responses;
    private readonly Random _random;

    public MockLLMClient()
    {
        _responses = new Dictionary<string, string>
        {
            { "weather", "The weather is sunny with a temperature of 22°C." },
            { "travel", "I can help you plan your trip. Here are some suggestions..." },
            { "default", "I understand your request. Let me help you with that." }
        };
        _random = new Random();
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Simulate network delay
        await Task.Delay(_random.Next(100, 500), cancellationToken);

        // Return appropriate response based on prompt content
        if (prompt.Contains("weather", StringComparison.OrdinalIgnoreCase))
            return _responses["weather"];
        
        if (prompt.Contains("travel", StringComparison.OrdinalIgnoreCase))
            return _responses["travel"];
        
        return _responses["default"];
    }

    public async Task<string> GenerateResponseAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        return await GenerateResponseAsync(request.Prompt, cancellationToken);
    }
}
```

### 2. Tool Mocking

```csharp
public class MockWeatherTool : ITool
{
    public string Name => "get_weather";
    public string Description => "Get current weather information for a location";

    public async Task<ToolResult> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate API call

        var location = parameters?.ToString() ?? "Unknown";
        var temperature = new Random().Next(10, 30);
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy" };
        var condition = conditions[new Random().Next(conditions.Length)];

        return new ToolResult
        {
            IsSuccess = true,
            Output = $"Weather in {location}: {temperature}°C, {condition}",
            Data = new { Location = location, Temperature = temperature, Condition = condition }
        };
    }
}
```

### 3. State Manager Mocking

```csharp
public class MockStateManager : IStateManager
{
    private readonly Dictionary<string, object> _state;

    public MockStateManager()
    {
        _state = new Dictionary<string, object>();
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _state.TryGetValue(key, out var value) ? (T)value : default(T);
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _state[key] = value;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _state.Remove(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _state.ContainsKey(key);
    }
}
```

## Test Data Management

### 1. Test Data Factories

```csharp
public static class TestDataFactory
{
    public static AgentRequest CreateAgentRequest(string input = "test input")
    {
        return new AgentRequest
        {
            Input = input,
            Context = new Dictionary<string, object>
            {
                { "userId", "test-user" },
                { "sessionId", Guid.NewGuid().ToString() }
            }
        };
    }

    public static AgentState CreateAgentState(string agentId = "test-agent")
    {
        return new AgentState
        {
            AgentId = agentId,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                { "conversationHistory", new List<string>() },
                { "preferences", new Dictionary<string, object>() }
            }
        };
    }

    public static ToolExecutionRequest CreateToolRequest(string toolName = "test_tool")
    {
        return new ToolExecutionRequest
        {
            ToolName = toolName,
            Parameters = new Dictionary<string, object>
            {
                { "param1", "value1" },
                { "param2", 42 }
            }
        };
    }
}
```

### 2. Data-Driven Tests

```csharp
[TestFixture]
public class DataDrivenTests
{
    [TestCaseSource(nameof(GetTestInputs))]
    public async Task AgentExecution_WithVariousInputs_HandlesCorrectly(string input, bool expectedSuccess)
    {
        // Arrange
        var agent = new Agent(new MockLLMClient());

        // Act
        var result = await agent.ExecuteAsync(input);

        // Assert
        Assert.That(result.IsSuccess, Is.EqualTo(expectedSuccess));
    }

    private static IEnumerable<TestCaseData> GetTestInputs()
    {
        yield return new TestCaseData("Hello, how are you?", true);
        yield return new TestCaseData("", false);
        yield return new TestCaseData(null, false);
        yield return new TestCaseData("What's the weather like?", true);
        yield return new TestCaseData("Help me plan a trip", true);
    }
}
```

## Test Configuration

### 1. Test Settings

```json
// appsettings.test.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AIAgentSharp": {
    "LLM": {
      "Provider": "Mock",
      "Timeout": 5000
    },
    "State": {
      "Provider": "InMemory",
      "ConnectionString": ""
    },
    "Tools": {
      "MaxExecutionTime": 10000
    }
  }
}
```

### 2. Test Categories

```csharp
[TestFixture]
[Category("Unit")]
public class UnitTests { }

[TestFixture]
[Category("Integration")]
public class IntegrationTests { }

[TestFixture]
[Category("Performance")]
public class PerformanceTests { }

[TestFixture]
[Category("E2E")]
public class E2ETests { }
```

## Continuous Integration

### 1. Test Pipeline

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run unit tests
      run: dotnet test --filter Category=Unit --no-build --verbosity normal
    
    - name: Run integration tests
      run: dotnet test --filter Category=Integration --no-build --verbosity normal
    
    - name: Run performance tests
      run: dotnet test --filter Category=Performance --no-build --verbosity normal
    
    - name: Generate coverage report
      run: dotnet test --collect:"XPlat Code Coverage" --results-directory coverage
    
    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: coverage/coverage.cobertura.xml
```

### 2. Test Reporting

```csharp
[TestFixture]
public class TestReporting
{
    [Test]
    public async Task AgentExecution_WithMetrics_ReportsCorrectly()
    {
        // Arrange
        var metricsCollector = new MetricsCollector();
        var agent = new Agent(new MockLLMClient(), metricsCollector);

        // Act
        var result = await agent.ExecuteAsync("test input");

        // Assert
        var metrics = metricsCollector.GetMetrics();
        Assert.That(metrics.ExecutionCount, Is.EqualTo(1));
        Assert.That(metrics.AverageExecutionTime, Is.GreaterThan(0));
        Assert.That(metrics.SuccessRate, Is.EqualTo(1.0));
    }
}
```

## Best Practices

### 1. Test Organization

- Group related tests in the same test class
- Use descriptive test method names
- Follow the Arrange-Act-Assert pattern
- Keep tests independent and isolated

### 2. Test Data

- Use realistic test data
- Avoid hardcoded values when possible
- Use test data factories for complex objects
- Clean up test data after tests

### 3. Performance Considerations

- Mock external dependencies to avoid network calls
- Use appropriate timeouts for async operations
- Monitor memory usage in performance tests
- Test with realistic load scenarios

### 4. Error Testing

- Test both success and failure scenarios
- Verify exception types and messages
- Test edge cases and boundary conditions
- Ensure proper error handling and logging

### 5. Maintenance

- Keep tests up to date with code changes
- Refactor tests when they become brittle
- Remove obsolete tests
- Document complex test scenarios

## Tools and Utilities

### 1. Test Helpers

```csharp
public static class TestHelpers
{
    public static async Task<T> WaitForConditionAsync<T>(
        Func<Task<T>> condition,
        TimeSpan timeout,
        TimeSpan interval)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < timeout)
        {
            var result = await condition();
            if (result != null)
                return result;
                
            await Task.Delay(interval);
        }
        
        throw new TimeoutException($"Condition not met within {timeout}");
    }

    public static async Task AssertEventuallyAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout = default,
        string message = null)
    {
        timeout = timeout == default ? TimeSpan.FromSeconds(5) : timeout;
        
        var result = await WaitForConditionAsync(condition, timeout, TimeSpan.FromMilliseconds(100));
        Assert.That(result, Is.True, message);
    }
}
```

### 2. Test Attributes

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class RetryTestAttribute : TestAttribute, ITestAction
{
    private readonly int _maxRetries;

    public RetryTestAttribute(int maxRetries = 3)
    {
        _maxRetries = maxRetries;
    }

    public void BeforeTest(TestDetails testDetails)
    {
        // Setup before test
    }

    public void AfterTest(TestDetails testDetails)
    {
        if (testDetails.Result.Status == TestStatus.Failed && 
            testDetails.Result.AssertionResults.Count < _maxRetries)
        {
            // Retry logic
        }
    }

    public ActionTargets Targets => ActionTargets.Test;
}
```

Following these testing guidelines ensures that AIAgentSharp maintains high quality and reliability through comprehensive testing coverage.
