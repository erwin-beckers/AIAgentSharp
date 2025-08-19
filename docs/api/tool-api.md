# Tool API Reference

This document provides comprehensive API reference for the AIAgentSharp tool framework.

## Core Interfaces

### ITool Interface

The main interface that all tools must implement.

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<ToolExecutionResult> ExecuteAsync(Dictionary<string, object?> parameters, CancellationToken ct = default);
    FunctionSpec GetFunctionSpec();
}
```

#### Properties

- **Name**: Unique identifier for the tool
- **Description**: Human-readable description of what the tool does

#### Methods

- **ExecuteAsync**: Main execution method called by the agent
- **GetFunctionSpec**: Returns the function specification for LLM function calling

## Base Classes

### BaseTool

Abstract base class that provides common functionality for tools.

```csharp
public abstract class BaseTool : ITool
{
    protected readonly ILogger _logger;
    
    protected BaseTool(ILogger logger)
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public abstract Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object?> parameters, 
        CancellationToken ct = default);
        
    public abstract FunctionSpec GetFunctionSpec();
    
    protected virtual void ValidateParameters(Dictionary<string, object?> parameters, string[] requiredParams)
    protected virtual T GetParameter<T>(Dictionary<string, object?> parameters, string key, T defaultValue = default)
}
```

## Tool Execution Result

### ToolExecutionResult Class

Represents the result of tool execution.

```csharp
public class ToolExecutionResult
{
    public bool Success { get; set; }
    public object? Output { get; set; }
    public string? Error { get; set; }
    public string TurnId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public TimeSpan? ExecutionTime { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}
```

#### Properties

- **Success**: Whether the tool execution succeeded
- **Output**: The result data from the tool
- **Error**: Error message if execution failed
- **TurnId**: Unique identifier for the turn
- **CreatedUtc**: When the result was created
- **ExecutionTime**: How long the tool took to execute
- **Metadata**: Additional metadata about the execution

## Function Specification

### FunctionSpec Class

Defines the schema for LLM function calling.

```csharp
public class FunctionSpec
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}
```

## Example Tool Implementation

```csharp
public class CalculatorTool : BaseTool
{
    public override string Name => "calculator";
    public override string Description => "Performs basic arithmetic operations";

    public CalculatorTool(ILogger logger) : base(logger) { }

    public override async Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object?> parameters, 
        CancellationToken ct = default)
    {
        try
        {
            ValidateParameters(parameters, new[] { "operation", "a", "b" });
            
            var operation = GetParameter<string>(parameters, "operation");
            var a = GetParameter<double>(parameters, "a");
            var b = GetParameter<double>(parameters, "b");

            var result = operation switch
            {
                "add" => a + b,
                "subtract" => a - b,
                "multiply" => a * b,
                "divide" => b != 0 ? a / b : throw new ArgumentException("Division by zero"),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };

            return new ToolExecutionResult
            {
                Success = true,
                Output = result,
                TurnId = Guid.NewGuid().ToString(),
                CreatedUtc = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                TurnId = Guid.NewGuid().ToString(),
                CreatedUtc = DateTimeOffset.UtcNow
            };
        }
    }

    public override FunctionSpec GetFunctionSpec()
    {
        return new FunctionSpec
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["operation"] = new { type = "string", enum = new[] { "add", "subtract", "multiply", "divide" } },
                    ["a"] = new { type = "number", description = "First number" },
                    ["b"] = new { type = "number", description = "Second number" }
                },
                ["required"] = new[] { "operation", "a", "b" }
            }
        };
    }
}
```

## Best Practices

1. **Error Handling**: Always wrap tool execution in try-catch blocks
2. **Parameter Validation**: Validate all required parameters before execution
3. **Logging**: Use the provided logger for debugging and monitoring
4. **Cancellation**: Respect the cancellation token for long-running operations
5. **Type Safety**: Use generic parameter extraction methods for type safety
6. **Function Specs**: Provide detailed function specifications for better LLM integration

## See Also

- [Tool Framework](../tool-framework.md) - Overview of the tool system
- [Tool Creation Guide](../examples/tool-creation.md) - Step-by-step tool creation
- [Agent Framework](../agent-framework.md) - How agents use tools

