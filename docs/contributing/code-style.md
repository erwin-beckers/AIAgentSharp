# Code Style Guide

This guide outlines the coding standards and conventions used in AIAgentSharp. Following these guidelines ensures consistency, readability, and maintainability across the codebase.

## General Principles

- **Readability**: Code should be self-documenting and easy to understand
- **Consistency**: Follow established patterns throughout the codebase
- **Maintainability**: Write code that's easy to modify and extend
- **Performance**: Consider performance implications, but prioritize clarity

## C# Coding Standards

### Naming Conventions

#### Classes and Interfaces

```csharp
// ✅ Good - PascalCase for classes
public class AgentManager
public class OpenAILLMClient
public class ToolExecutionResult

// ✅ Good - Interface names start with 'I'
public interface IAgent
public interface ILLMClient
public interface ITool

// ❌ Bad - Incorrect casing
public class agentManager
public class openai_llm_client
public interface tool
```

#### Methods and Properties

```csharp
// ✅ Good - PascalCase for public members
public class Agent
{
    public string Name { get; set; }
    public async Task<AgentResult> ExecuteAsync(string input)
    public bool IsActive { get; private set; }
    
    // Private members can use camelCase
    private string _internalState;
    private async Task<bool> ValidateInputAsync(string input)
}

// ❌ Bad - Incorrect casing
public string name { get; set; }
public async Task<AgentResult> execute_async(string input)
```

#### Variables and Parameters

```csharp
// ✅ Good - camelCase for variables and parameters
public async Task<AgentResult> ExecuteAsync(string userInput, CancellationToken cancellationToken)
{
    var result = await ProcessInputAsync(userInput);
    var isValid = ValidateResult(result);
    
    if (isValid)
    {
        return result;
    }
    
    throw new InvalidOperationException("Invalid result");
}

// ❌ Bad - Incorrect casing
public async Task<AgentResult> ExecuteAsync(string UserInput, CancellationToken CancellationToken)
{
    var Result = await ProcessInputAsync(UserInput);
    var IsValid = ValidateResult(Result);
}
```

#### Constants and Fields

```csharp
// ✅ Good - PascalCase for constants, camelCase for private fields
public class Configuration
{
    public const string DefaultApiEndpoint = "https://api.openai.com/v1";
    public const int MaxRetryAttempts = 3;
    
    private readonly string _apiKey;
    private readonly ILogger<Configuration> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
}

// ❌ Bad - Incorrect casing
public const string default_api_endpoint = "https://api.openai.com/v1";
private readonly string ApiKey;
```

### Code Organization

#### File Structure

```csharp
// 1. Using statements
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// 2. Namespace declaration
namespace AIAgentSharp.Core
{
    // 3. Class declaration
    public class Agent : IAgent
    {
        // 4. Constants and static fields
        private const int DefaultTimeout = 30000;
        private static readonly SemaphoreSlim _lock = new(1, 1);
        
        // 5. Instance fields
        private readonly ILLMClient _llmClient;
        private readonly ILogger<Agent> _logger;
        private readonly List<ITool> _tools;
        
        // 6. Properties
        public string Name { get; }
        public AgentState State { get; private set; }
        
        // 7. Constructor
        public Agent(ILLMClient llmClient, ILogger<Agent> logger)
        {
            _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tools = new List<ITool>();
        }
        
        // 8. Public methods
        public async Task<AgentResult> ExecuteAsync(string input, CancellationToken cancellationToken = default)
        {
            // Implementation
        }
        
        // 9. Private methods
        private async Task<AgentResult> ProcessInputAsync(string input)
        {
            // Implementation
        }
    }
}
```

#### Method Organization

```csharp
public class ToolExecutor
{
    // Public methods first
    public async Task<ToolResult> ExecuteAsync(ITool tool, object parameters)
    {
        ValidateTool(tool);
        ValidateParameters(parameters);
        
        var result = await ExecuteToolInternalAsync(tool, parameters);
        await LogExecutionAsync(tool, parameters, result);
        
        return result;
    }
    
    // Private methods after public methods
    private void ValidateTool(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));
    }
    
    private void ValidateParameters(object parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));
    }
    
    private async Task<ToolResult> ExecuteToolInternalAsync(ITool tool, object parameters)
    {
        // Implementation
    }
    
    private async Task LogExecutionAsync(ITool tool, object parameters, ToolResult result)
    {
        // Implementation
    }
}
```

### Formatting

#### Indentation and Spacing

```csharp
// ✅ Good - 4 spaces for indentation
public class Example
{
    public void Method()
    {
        if (condition)
        {
            DoSomething();
        }
        else
        {
            DoSomethingElse();
        }
    }
}

// ❌ Bad - Inconsistent indentation
public class Example
{
  public void Method()
  {
      if (condition)
    {
        DoSomething();
    }
  }
}
```

#### Line Breaks

```csharp
// ✅ Good - Break long lines for readability
public async Task<AgentResult> ExecuteAsync(
    string input,
    CancellationToken cancellationToken = default,
    IProgress<AgentProgress> progress = null)
{
    // Implementation
}

// ✅ Good - Break long method calls
var result = await _llmClient.GenerateResponseAsync(
    prompt: userPrompt,
    maxTokens: 1000,
    temperature: 0.7,
    cancellationToken: cancellationToken);

// ❌ Bad - Too long lines
public async Task<AgentResult> ExecuteAsync(string input, CancellationToken cancellationToken = default, IProgress<AgentProgress> progress = null)
{
    var result = await _llmClient.GenerateResponseAsync(prompt: userPrompt, maxTokens: 1000, temperature: 0.7, cancellationToken: cancellationToken);
}
```

#### Braces

```csharp
// ✅ Good - Always use braces for control structures
if (condition)
{
    DoSomething();
}

for (int i = 0; i < items.Length; i++)
{
    ProcessItem(items[i]);
}

// ❌ Bad - Missing braces
if (condition)
    DoSomething();

for (int i = 0; i < items.Length; i++)
    ProcessItem(items[i]);
```

### Error Handling

#### Exception Handling

```csharp
// ✅ Good - Specific exception handling
public async Task<AgentResult> ExecuteAsync(string input)
{
    try
    {
        ValidateInput(input);
        var result = await ProcessInputAsync(input);
        return result;
    }
    catch (ArgumentNullException ex)
    {
        _logger.LogError(ex, "Input validation failed");
        throw;
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Network error during execution");
        throw new AgentExecutionException("Failed to execute agent due to network error", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during agent execution");
        throw new AgentExecutionException("Unexpected error during agent execution", ex);
    }
}

// ❌ Bad - Generic exception handling
public async Task<AgentResult> ExecuteAsync(string input)
{
    try
    {
        return await ProcessInputAsync(input);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error");
        throw;
    }
}
```

#### Null Checking

```csharp
// ✅ Good - Use null-conditional operators and null-coalescing
public void ProcessData(string input, ILogger logger)
{
    var processedInput = input?.Trim() ?? string.Empty;
    var logLevel = logger?.IsEnabled(LogLevel.Debug) == true ? LogLevel.Debug : LogLevel.Information;
    
    if (string.IsNullOrEmpty(processedInput))
    {
        throw new ArgumentException("Input cannot be null or empty", nameof(input));
    }
}

// ✅ Good - Use ArgumentNullException.ThrowIfNull (C# 11+)
public void ProcessData(string input, ILogger logger)
{
    ArgumentNullException.ThrowIfNull(input);
    ArgumentNullException.ThrowIfNull(logger);
    
    // Process data
}

// ❌ Bad - Manual null checks
public void ProcessData(string input, ILogger logger)
{
    if (input == null)
        throw new ArgumentNullException(nameof(input));
    if (logger == null)
        throw new ArgumentNullException(nameof(logger));
}
```

### Async/Await Patterns

#### Async Method Naming

```csharp
// ✅ Good - Async methods end with "Async"
public async Task<AgentResult> ExecuteAsync(string input)
public async Task<bool> ValidateInputAsync(string input)
public async Task SaveStateAsync(AgentState state)

// ❌ Bad - Missing "Async" suffix
public async Task<AgentResult> Execute(string input)
public async Task<bool> ValidateInput(string input)
```

#### Async/Await Usage

```csharp
// ✅ Good - Proper async/await usage
public async Task<AgentResult> ExecuteAsync(string input)
{
    var validatedInput = await ValidateInputAsync(input);
    var result = await ProcessInputAsync(validatedInput);
    await SaveResultAsync(result);
    return result;
}

// ❌ Bad - Blocking calls in async methods
public async Task<AgentResult> ExecuteAsync(string input)
{
    var validatedInput = ValidateInputAsync(input).Result; // Blocking!
    var result = ProcessInputAsync(validatedInput).Result; // Blocking!
    return result;
}
```

#### Cancellation Token Support

```csharp
// ✅ Good - Support cancellation tokens
public async Task<AgentResult> ExecuteAsync(
    string input,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    var result = await ProcessInputAsync(input, cancellationToken);
    await SaveResultAsync(result, cancellationToken);
    
    return result;
}

// ❌ Bad - No cancellation support
public async Task<AgentResult> ExecuteAsync(string input)
{
    var result = await ProcessInputAsync(input);
    await SaveResultAsync(result);
    return result;
}
```

### Documentation

#### XML Documentation

```csharp
/// <summary>
/// Executes the agent with the specified input and returns the result.
/// </summary>
/// <param name="input">The input string to process.</param>
/// <param name="cancellationToken">Optional cancellation token.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains
/// the agent execution result.
/// </returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="input"/> is null.
/// </exception>
/// <exception cref="AgentExecutionException">
/// Thrown when the agent execution fails.
/// </exception>
public async Task<AgentResult> ExecuteAsync(
    string input,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

#### Inline Comments

```csharp
// ✅ Good - Explain complex logic
public async Task<AgentResult> ExecuteAsync(string input)
{
    // Validate and preprocess input
    var processedInput = await PreprocessInputAsync(input);
    
    // Execute reasoning chain with fallback
    AgentResult result;
    try
    {
        result = await ExecuteReasoningChainAsync(processedInput);
    }
    catch (ReasoningException)
    {
        // Fallback to simple processing if reasoning fails
        result = await ExecuteSimpleProcessingAsync(processedInput);
    }
    
    return result;
}

// ❌ Bad - Obvious comments
public async Task<AgentResult> ExecuteAsync(string input)
{
    // Process input
    var processedInput = await PreprocessInputAsync(input);
    
    // Get result
    var result = await ProcessInputAsync(processedInput);
    
    // Return result
    return result;
}
```

### Performance Considerations

#### Memory Management

```csharp
// ✅ Good - Use using statements for disposable objects
public async Task<string> ReadFileAsync(string path)
{
    using var stream = File.OpenRead(path);
    using var reader = new StreamReader(stream);
    return await reader.ReadToEndAsync();
}

// ✅ Good - Use object pooling for frequently allocated objects
public class AgentPool
{
    private readonly ObjectPool<Agent> _agentPool;
    
    public async Task<AgentResult> ExecuteAsync(string input)
    {
        var agent = _agentPool.Get();
        try
        {
            return await agent.ExecuteAsync(input);
        }
        finally
        {
            _agentPool.Return(agent);
        }
    }
}

// ❌ Bad - Not disposing resources
public async Task<string> ReadFileAsync(string path)
{
    var stream = File.OpenRead(path);
    var reader = new StreamReader(stream);
    return await reader.ReadToEndAsync();
    // Stream and reader are not disposed!
}
```

#### LINQ Usage

```csharp
// ✅ Good - Use LINQ efficiently
public IEnumerable<string> GetActiveAgentNames()
{
    return _agents
        .Where(agent => agent.IsActive)
        .Select(agent => agent.Name)
        .OrderBy(name => name);
}

// ✅ Good - Use async LINQ for IAsyncEnumerable
public async IAsyncEnumerable<string> GetActiveAgentNamesAsync()
{
    await foreach (var agent in _agentsAsync)
    {
        if (agent.IsActive)
        {
            yield return agent.Name;
        }
    }
}

// ❌ Bad - Inefficient LINQ usage
public List<string> GetActiveAgentNames()
{
    var result = new List<string>();
    foreach (var agent in _agents)
    {
        if (agent.IsActive)
        {
            result.Add(agent.Name);
        }
    }
    result.Sort();
    return result;
}
```

## Testing Standards

### Test Naming

```csharp
// ✅ Good - Descriptive test names
[Test]
public async Task ExecuteAsync_WithValidInput_ReturnsSuccessResult()

[Test]
public async Task ExecuteAsync_WithNullInput_ThrowsArgumentNullException()

[Test]
public async Task ExecuteAsync_WhenLLMClientFails_ThrowsAgentExecutionException()

// ❌ Bad - Unclear test names
[Test]
public async Task Test1()

[Test]
public async Task ExecuteTest()

[Test]
public async Task TestNullInput()
```

### Test Structure

```csharp
// ✅ Good - Arrange-Act-Assert pattern
[Test]
public async Task ExecuteAsync_WithValidInput_ReturnsSuccessResult()
{
    // Arrange
    var llmClient = new Mock<ILLMClient>();
    var agent = new Agent(llmClient.Object);
    var input = "test input";
    
    llmClient.Setup(x => x.GenerateResponseAsync(It.IsAny<string>()))
        .ReturnsAsync("test response");
    
    // Act
    var result = await agent.ExecuteAsync(input);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Output, Is.EqualTo("test response"));
}
```

## EditorConfig

The project includes an `.editorconfig` file that enforces these coding standards:

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx,vb,vbx}]
indent_style = space
indent_size = 4
end_of_line = crlf

# C# files
[*.cs]
# New line preferences
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true

# Indentation preferences
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_indent_labels = flush_left

# Space preferences
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_parentheses = false
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_around_binary_operators = before_and_after
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_between_method_call_empty_parameter_list_parentheses = false

# Wrapping preferences
csharp_preserve_single_line_statements = true
csharp_preserve_single_line_blocks = true
```

## Code Review Checklist

Before submitting code for review, ensure:

- [ ] Code follows naming conventions
- [ ] Proper error handling is implemented
- [ ] Async/await patterns are used correctly
- [ ] XML documentation is provided for public APIs
- [ ] Unit tests are written and passing
- [ ] Code is formatted according to EditorConfig
- [ ] No compiler warnings or errors
- [ ] Performance considerations are addressed
- [ ] Security best practices are followed

## Tools and Automation

### Code Analysis

```bash
# Run code analysis
dotnet build --verbosity normal

# Run style checks
dotnet format --verify-no-changes

# Run security analysis
dotnet list package --vulnerable
```

### IDE Integration

Most IDEs will automatically apply these standards when the `.editorconfig` file is present. For manual formatting:

- **Visual Studio**: Edit → Advanced → Format Document
- **VS Code**: Shift + Alt + F
- **Rider**: Ctrl + Alt + L

Following these coding standards ensures that AIAgentSharp maintains high code quality and consistency across all contributions.
