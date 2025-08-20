# Testing Best Practices

This guide covers comprehensive testing strategies for AIAgentSharp applications. Testing AI agents presents unique challenges due to their non-deterministic nature, external dependencies, and complex reasoning processes.

## Overview

Effective testing in AIAgentSharp involves:
- Unit testing individual components
- Integration testing for tool interactions
- End-to-end testing for complete workflows
- Performance and load testing
- Security testing for vulnerabilities
- Mocking and stubbing external dependencies

## Testing Strategy

### 1. Testing Pyramid
```
    /\
   /  \     E2E Tests (Few)
  /____\    Integration Tests (Some)
 /______\   Unit Tests (Many)
```

### 2. Test Categories
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **End-to-End Tests**: Test complete workflows
- **Performance Tests**: Test scalability and performance
- **Security Tests**: Test security vulnerabilities

## Implementation Strategies

### 1. Unit Testing Framework

```csharp
[TestFixture]
public class AgentUnitTests
{
    private Mock<ILLMClient> _mockLLMClient;
    private Mock<IAgentStateManager> _mockStateManager;
    private Mock<IEventManager> _mockEventManager;
    private TestAgent _agent;
    
    [SetUp]
    public void Setup()
    {
        _mockLLMClient = new Mock<ILLMClient>();
        _mockStateManager = new Mock<IAgentStateManager>();
        _mockEventManager = new Mock<IEventManager>();
        
        _agent = new TestAgent(
            _mockLLMClient.Object,
            _mockStateManager.Object,
            _mockEventManager.Object
        );
    }
    
    [Test]
    public async Task ExecuteAsync_ValidPrompt_ReturnsExpectedResponse()
    {
        // Arrange
        var prompt = "What is 2 + 2?";
        var expectedResponse = "2 + 2 equals 4";
        
        _mockLLMClient
            .Setup(x => x.GenerateAsync(It.IsAny<LLMRequest>()))
            .ReturnsAsync(new LLMResponse { Content = expectedResponse });
        
        // Act
        var result = await _agent.ExecuteAsync(prompt);
        
        // Assert
        Assert.That(result, Is.EqualTo(expectedResponse));
        _mockLLMClient.Verify(x => x.GenerateAsync(It.IsAny<LLMRequest>()), Times.Once);
    }
    
    [Test]
    public async Task ExecuteAsync_EmptyPrompt_ThrowsArgumentException()
    {
        // Arrange
        var prompt = "";
        
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(
            async () => await _agent.ExecuteAsync(prompt)
        );
        
        Assert.That(exception.Message, Does.Contain("Prompt cannot be empty"));
    }
    
    [Test]
    public async Task ExecuteAsync_LLMError_ThrowsException()
    {
        // Arrange
        var prompt = "Test prompt";
        var expectedException = new LLMException("API error");
        
        _mockLLMClient
            .Setup(x => x.GenerateAsync(It.IsAny<LLMRequest>()))
            .ThrowsAsync(expectedException);
        
        // Act & Assert
        var exception = Assert.ThrowsAsync<LLMException>(
            async () => await _agent.ExecuteAsync(prompt)
        );
        
        Assert.That(exception.Message, Is.EqualTo("API error"));
    }
    
    [Test]
    public async Task ExecuteAsync_StateManagerCalled_StateSaved()
    {
        // Arrange
        var prompt = "Test prompt";
        var response = "Test response";
        
        _mockLLMClient
            .Setup(x => x.GenerateAsync(It.IsAny<LLMRequest>()))
            .ReturnsAsync(new LLMResponse { Content = response });
        
        // Act
        await _agent.ExecuteAsync(prompt);
        
        // Assert
        _mockStateManager.Verify(
            x => x.SaveStateAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Once
        );
    }
}

public class TestAgent : Agent
{
    public TestAgent(
        ILLMClient llmClient,
        IAgentStateManager stateManager,
        IEventManager eventManager)
        : base(llmClient, stateManager, eventManager)
    {
    }
    
    public override async Task<string> ExecuteAsync(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        }
        
        var request = new LLMRequest { Prompt = prompt };
        var response = await LLMClient.GenerateAsync(request);
        
        await StateManager.SaveStateAsync("test_state", new { prompt, response = response.Content });
        
        return response.Content;
    }
}
```

### 2. Tool Testing

```csharp
[TestFixture]
public class ToolTests
{
    private Mock<ILogger<TestTool>> _mockLogger;
    private TestTool _tool;
    
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<TestTool>>();
        _tool = new TestTool(_mockLogger.Object);
    }
    
    [Test]
    public async Task ExecuteAsync_ValidParameters_ReturnsSuccess()
    {
        // Arrange
        var parameters = new ToolParameters();
        parameters.SetString("input", "test input");
        
        // Act
        var result = await _tool.ExecuteAsync(parameters);
        
        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.ToString(), Does.Contain("test input"));
    }
    
    [Test]
    public async Task ExecuteAsync_MissingRequiredParameter_ReturnsError()
    {
        // Arrange
        var parameters = new ToolParameters();
        // Missing required "input" parameter
        
        // Act
        var result = await _tool.ExecuteAsync(parameters);
        
        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("input"));
    }
    
    [Test]
    public async Task ExecuteAsync_ExceptionThrown_ReturnsError()
    {
        // Arrange
        var parameters = new ToolParameters();
        parameters.SetString("input", "error");
        
        // Act
        var result = await _tool.ExecuteAsync(parameters);
        
        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("error"));
    }
}

public class TestTool : ITool
{
    private readonly ILogger _logger;
    
    public TestTool(ILogger logger)
    {
        _logger = logger;
    }
    
    public string Name => "test_tool";
    public string Description => "A test tool for unit testing";
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        try
        {
            var input = parameters.GetString("input");
            
            if (string.IsNullOrEmpty(input))
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Required parameter 'input' is missing"
                };
            }
            
            if (input == "error")
            {
                throw new InvalidOperationException("Test error");
            }
            
            return new ToolResult
            {
                Success = true,
                Data = $"Processed: {input}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing test tool");
            return new ToolResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
```

### 3. Integration Testing

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
            .UseStartup<TestStartup>();
        
        _server = new TestServer(builder);
        _client = _server.CreateClient();
        _serviceProvider = _server.Host.Services;
    }
    
    [OneTimeTearDown]
    public void Cleanup()
    {
        _server?.Dispose();
        _client?.Dispose();
    }
    
    [Test]
    public async Task AgentWithTools_CompleteWorkflow_Succeeds()
    {
        // Arrange
        var agent = _serviceProvider.GetRequiredService<TestAgent>();
        var prompt = "Search for weather in New York and summarize the results";
        
        // Act
        var result = await agent.ExecuteAsync(prompt);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("New York"));
        Assert.That(result, Does.Contain("weather"));
    }
    
    [Test]
    public async Task AgentStateManagement_StatePersisted_CanBeRetrieved()
    {
        // Arrange
        var agent = _serviceProvider.GetRequiredService<TestAgent>();
        var stateManager = _serviceProvider.GetRequiredService<IAgentStateManager>();
        var prompt = "Test state persistence";
        
        // Act
        await agent.ExecuteAsync(prompt);
        var savedState = await stateManager.GetStateAsync<AgentState>("test_agent");
        
        // Assert
        Assert.That(savedState, Is.Not.Null);
        Assert.That(savedState.LastPrompt, Is.EqualTo(prompt));
    }
    
    [Test]
    public async Task EventSystem_EventsPublished_CanBeSubscribed()
    {
        // Arrange
        var eventManager = _serviceProvider.GetRequiredService<IEventManager>();
        var events = new List<IAgentEvent>();
        
        eventManager.Subscribe<AgentStartedEvent>(e => events.Add(e));
        eventManager.Subscribe<AgentCompletedEvent>(e => events.Add(e));
        
        var agent = _serviceProvider.GetRequiredService<TestAgent>();
        
        // Act
        await agent.ExecuteAsync("Test event publishing");
        
        // Assert
        Assert.That(events, Has.Count.GreaterThan(0));
        Assert.That(events.Any(e => e is AgentStartedEvent), Is.True);
        Assert.That(events.Any(e => e is AgentCompletedEvent), Is.True);
    }
}

public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILLMClient, MockLLMClient>();
        services.AddSingleton<IAgentStateManager, InMemoryStateManager>();
        services.AddSingleton<IEventManager, EventManager>();
        services.AddSingleton<TestAgent>();
        
        // Register test tools
        services.AddSingleton<ITool, MockWeatherTool>();
        services.AddSingleton<ITool, MockSearchTool>();
    }
    
    public void Configure(IApplicationBuilder app)
    {
        // Configure test application
    }
}
```

### 4. End-to-End Testing

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
        _server?.Dispose();
        _client?.Dispose();
    }
    
    [Test]
    public async Task AgentAPI_CompleteWorkflow_ReturnsExpectedResult()
    {
        // Arrange
        var request = new
        {
            prompt = "What is the weather like in London today?",
            agentId = "weather_agent"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/agent/execute", request);
        
        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        
        var result = await response.Content.ReadFromJsonAsync<AgentResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Does.Contain("London"));
    }
    
    [Test]
    public async Task AgentAPI_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            prompt = "", // Invalid empty prompt
            agentId = "test_agent"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/agent/execute", request);
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task AgentAPI_ConcurrentRequests_HandlesLoad()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var request = new { prompt = "Test concurrent requests", agentId = "test_agent" };
        
        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.PostAsJsonAsync("/api/agent/execute", request));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.That(responses.All(r => r.IsSuccessStatusCode), Is.True);
    }
}
```

### 5. Performance Testing

```csharp
[TestFixture]
public class AgentPerformanceTests
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
        _server?.Dispose();
        _client?.Dispose();
    }
    
    [Test]
    public async Task AgentExecution_ResponseTime_WithinAcceptableRange()
    {
        // Arrange
        var request = new { prompt = "Simple test prompt", agentId = "test_agent" };
        var stopwatch = new Stopwatch();
        
        // Act
        stopwatch.Start();
        var response = await _client.PostAsJsonAsync("/api/agent/execute", request);
        stopwatch.Stop();
        
        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000)); // 5 seconds max
    }
    
    [Test]
    public async Task AgentExecution_Throughput_HandlesConcurrentRequests()
    {
        // Arrange
        var request = new { prompt = "Test throughput", agentId = "test_agent" };
        var concurrentRequests = 20;
        var stopwatch = new Stopwatch();
        
        // Act
        stopwatch.Start();
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => _client.PostAsJsonAsync("/api/agent/execute", request))
            .ToArray();
        
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var throughput = concurrentRequests / (stopwatch.ElapsedMilliseconds / 1000.0);
        
        Assert.That(successCount, Is.GreaterThan(concurrentRequests * 0.9)); // 90% success rate
        Assert.That(throughput, Is.GreaterThan(10)); // 10 requests per second minimum
    }
    
    [Test]
    public async Task AgentMemoryUsage_UnderLoad_StaysWithinLimits()
    {
        // Arrange
        var request = new { prompt = "Memory test", agentId = "test_agent" };
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act
        for (int i = 0; i < 100; i++)
        {
            await _client.PostAsJsonAsync("/api/agent/execute", request);
        }
        
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        
        // Assert
        Assert.That(memoryIncrease, Is.LessThan(50 * 1024 * 1024)); // 50MB max increase
    }
}
```

### 6. Mock Implementations

```csharp
public class MockLLMClient : ILLMClient
{
    private readonly Dictionary<string, string> _responses;
    
    public MockLLMClient()
    {
        _responses = new Dictionary<string, string>
        {
            { "weather", "The weather in {0} is sunny with a temperature of 22°C." },
            { "search", "Here are the search results for '{0}': [Mock results]" },
            { "default", "I understand you're asking about '{0}'. Here's what I found." }
        };
    }
    
    public async Task<LLMResponse> GenerateAsync(LLMRequest request)
    {
        await Task.Delay(100); // Simulate network delay
        
        var prompt = request.Prompt.ToLower();
        string response;
        
        if (prompt.Contains("weather"))
        {
            var location = ExtractLocation(prompt);
            response = string.Format(_responses["weather"], location);
        }
        else if (prompt.Contains("search"))
        {
            var query = ExtractQuery(prompt);
            response = string.Format(_responses["search"], query);
        }
        else
        {
            response = string.Format(_responses["default"], request.Prompt);
        }
        
        return new LLMResponse
        {
            Content = response,
            TokensUsed = response.Length / 4, // Rough estimate
            Model = "gpt-4-mock"
        };
    }
    
    private string ExtractLocation(string prompt)
    {
        // Simple location extraction for testing
        var match = Regex.Match(prompt, @"in (\w+)");
        return match.Success ? match.Groups[1].Value : "Unknown";
    }
    
    private string ExtractQuery(string prompt)
    {
        // Simple query extraction for testing
        var match = Regex.Match(prompt, @"for ['""]([^'""]+)['""]");
        return match.Success ? match.Groups[1].Value : "general query";
    }
}

public class MockWeatherTool : ITool
{
    public string Name => "get_weather";
    public string Description => "Get weather information for a location";
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        await Task.Delay(50); // Simulate API delay
        
        var location = parameters.GetString("location");
        
        return new ToolResult
        {
            Success = true,
            Data = new
            {
                location = location,
                temperature = 22,
                condition = "sunny",
                humidity = 65,
                wind_speed = 10
            }
        };
    }
}

public class MockSearchTool : ITool
{
    public string Name => "search_web";
    public string Description => "Search the web for information";
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        await Task.Delay(100); // Simulate API delay
        
        var query = parameters.GetString("query");
        
        return new ToolResult
        {
            Success = true,
            Data = new
            {
                query = query,
                results = new[]
                {
                    new { title = "Mock Result 1", url = "https://example1.com", snippet = "Mock snippet 1" },
                    new { title = "Mock Result 2", url = "https://example2.com", snippet = "Mock snippet 2" }
                }
            }
        };
    }
}
```

### 7. Test Data Management

```csharp
public class TestDataFactory
{
    public static LLMRequest CreateLLMRequest(string prompt = "Test prompt")
    {
        return new LLMRequest
        {
            Prompt = prompt,
            MaxTokens = 1000,
            Temperature = 0.7f
        };
    }
    
    public static ToolParameters CreateToolParameters(Dictionary<string, object> parameters = null)
    {
        var toolParams = new ToolParameters();
        
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                switch (param.Value)
                {
                    case string s:
                        toolParams.SetString(param.Key, s);
                        break;
                    case int i:
                        toolParams.SetInt(param.Key, i);
                        break;
                    case double d:
                        toolParams.SetDouble(param.Key, d);
                        break;
                    case bool b:
                        toolParams.SetBool(param.Key, b);
                        break;
                }
            }
        }
        
        return toolParams;
    }
    
    public static AgentState CreateAgentState(string agentId = "test_agent")
    {
        return new AgentState
        {
            AgentId = agentId,
            LastPrompt = "Test prompt",
            LastResponse = "Test response",
            ExecutionCount = 1,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public static IEnumerable<TestCaseData> GetTestPrompts()
    {
        yield return new TestCaseData("What is 2 + 2?", "4");
        yield return new TestCaseData("What is the weather like?", "weather information");
        yield return new TestCaseData("Search for information", "search results");
    }
}

[TestFixture]
public class DataDrivenTests
{
    [TestCaseSource(typeof(TestDataFactory), nameof(TestDataFactory.GetTestPrompts))]
    public async Task Agent_WithTestPrompts_ReturnsExpectedResults(string prompt, string expectedContent)
    {
        // Arrange
        var mockLLMClient = new Mock<ILLMClient>();
        mockLLMClient
            .Setup(x => x.GenerateAsync(It.IsAny<LLMRequest>()))
            .ReturnsAsync(new LLMResponse { Content = expectedContent });
        
        var agent = new TestAgent(
            mockLLMClient.Object,
            new Mock<IAgentStateManager>().Object,
            new Mock<IEventManager>().Object
        );
        
        // Act
        var result = await agent.ExecuteAsync(prompt);
        
        // Assert
        Assert.That(result, Does.Contain(expectedContent));
    }
}
```

## Best Practices Summary

### 1. Test Organization
- Organize tests by component and functionality
- Use descriptive test names that explain the scenario
- Group related tests using test fixtures
- Separate unit, integration, and E2E tests

### 2. Mocking Strategy
- Mock external dependencies (LLM APIs, external tools)
- Use realistic mock responses
- Test error scenarios with mocks
- Avoid over-mocking internal components

### 3. Test Data Management
- Use factories for creating test data
- Implement data-driven tests for multiple scenarios
- Clean up test data after tests
- Use realistic but safe test data

### 4. Performance Testing
- Set realistic performance expectations
- Test under various load conditions
- Monitor resource usage during tests
- Use performance benchmarks

### 5. Error Testing
- Test all error scenarios
- Verify error messages and handling
- Test retry mechanisms
- Test circuit breaker patterns

### 6. Security Testing
- Test input validation
- Test authentication and authorization
- Test for common vulnerabilities
- Test data sanitization

## Testing Checklist

- [ ] Unit tests for all components
- [ ] Integration tests for component interactions
- [ ] End-to-end tests for complete workflows
- [ ] Performance tests for scalability
- [ ] Security tests for vulnerabilities
- [ ] Mock implementations for external dependencies
- [ ] Test data factories and helpers
- [ ] Error scenario testing
- [ ] Load testing for concurrent requests
- [ ] Memory usage testing

## Common Testing Anti-patterns

1. **Testing Implementation Details**: Focus on behavior, not implementation
2. **Over-Mocking**: Only mock external dependencies
3. **Flaky Tests**: Ensure tests are deterministic
4. **Slow Tests**: Optimize test performance
5. **Incomplete Coverage**: Test all code paths
6. **Hard-coded Test Data**: Use factories and builders

## Next Steps

- Set up automated testing pipeline
- Implement continuous integration
- Add code coverage reporting
- Create performance benchmarks
- Set up test environments
- Document testing procedures

This guide provides a comprehensive approach to testing AIAgentSharp applications, ensuring reliability, performance, and security while maintaining code quality and developer productivity.
