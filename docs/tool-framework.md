# Tool Framework

The Tool Framework in AIAgentSharp provides a powerful, type-safe way to create tools that agents can use to perform actions and interact with external systems.

## Overview

Tools are functions that agents can call to perform specific tasks. They provide:

- **Type Safety**: Strongly-typed parameters and return values
- **Automatic Schema Generation**: JSON schemas are automatically generated for LLM function calling
- **Validation**: Built-in parameter validation using data annotations
- **Error Handling**: Comprehensive error handling and reporting
- **Async Support**: Full async/await support for I/O operations

## Basic Tool Structure

### 1. Define Parameters

Create a parameter class with data annotations:

```csharp
using AIAgentSharp.Tools;
using System.ComponentModel.DataAnnotations;

[ToolParams(Description = "Weather lookup parameters")]
public sealed class WeatherParams
{
    [ToolField(Description = "City name", Example = "New York", Required = true)]
    [Required]
    public string City { get; set; } = default!;
    
    [ToolField(Description = "Temperature unit", Example = "Celsius")]
    public string Unit { get; set; } = "Celsius";
    
    [ToolField(Description = "Include humidity information")]
    public bool IncludeHumidity { get; set; } = false;
}
```

### 2. Create the Tool

Inherit from `BaseTool<TParams, TResult>`:

```csharp
public sealed class WeatherTool : BaseTool<WeatherParams, object>
{
    public override string Name => "get_weather";
    public override string Description => "Get current weather information for a city";

    protected override async Task<object> InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
    {
        // Your weather API logic here
        var weatherData = await GetWeatherFromApi(parameters.City, parameters.Unit, ct);
        
        var result = new
        {
            city = parameters.City,
            temperature = weatherData.Temperature,
            unit = parameters.Unit,
            description = weatherData.Description,
            humidity = parameters.IncludeHumidity ? weatherData.Humidity : null
        };
        
        return result;
    }
    
    private async Task<WeatherData> GetWeatherFromApi(string city, string unit, CancellationToken ct)
    {
        // Implementation details...
        return new WeatherData { Temperature = "22°C", Description = "Sunny", Humidity = "65%" };
    }
}
```

## Tool Attributes

### ToolParams Attribute

```csharp
[ToolParams(Description = "Tool parameter description")]
public sealed class MyParams
{
    // Parameters...
}
```

### ToolField Attribute

```csharp
[ToolField(
    Description = "Field description",
    Example = "Example value",
    Required = true,
    MinLength = 1,
    MaxLength = 100
)]
public string MyField { get; set; } = default!;
```

### Supported Data Types

AIAgentSharp supports various data types for tool parameters:

```csharp
public sealed class ComplexParams
{
    // Basic types
    public string Text { get; set; } = default!;
    public int Number { get; set; }
    public double Decimal { get; set; }
    public bool Boolean { get; set; }
    
    // Arrays
    public string[] StringArray { get; set; } = Array.Empty<string>();
    public List<int> NumberList { get; set; } = new();
    
    // Enums
    public WeatherType WeatherType { get; set; }
    
    // Complex objects
    public Address Address { get; set; } = default!;
}

public enum WeatherType
{
    Sunny,
    Cloudy,
    Rainy,
    Snowy
}

public class Address
{
    public string Street { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
}
```

## Advanced Tool Features

### Custom Validation

```csharp
public sealed class ValidatedParams
{
    [ToolField(Description = "Age must be between 0 and 150")]
    [Range(0, 150)]
    public int Age { get; set; }
    
    [ToolField(Description = "Email address")]
    [EmailAddress]
    public string Email { get; set; } = default!;
    
    [ToolField(Description = "URL")]
    [Url]
    public string Website { get; set; } = default!;
    
    [ToolField(Description = "Phone number")]
    [RegularExpression(@"^\+?[1-9]\d{1,14}$")]
    public string Phone { get; set; } = default!;
}
```

### Async Operations

```csharp
public sealed class DatabaseTool : BaseTool<QueryParams, object>
{
    public override string Name => "database_query";
    public override string Description => "Execute a database query";

    protected override async Task<object> InvokeTypedAsync(QueryParams parameters, CancellationToken ct = default)
    {
        // Simulate database operation
        await Task.Delay(100, ct); // Simulate I/O
        
        return new
        {
            query = parameters.Sql,
            results = new[] { "result1", "result2" },
            execution_time_ms = 100
        };
    }
}
```

### Error Handling

```csharp
public sealed class RobustTool : BaseTool<RobustParams, object>
{
    public override string Name => "robust_operation";
    public override string Description => "Perform a robust operation with error handling";

    protected override async Task<object> InvokeTypedAsync(RobustParams parameters, CancellationToken ct = default)
    {
        try
        {
            // Your operation here
            var result = await PerformOperation(parameters, ct);
            
            return new
            {
                success = true,
                data = result,
                timestamp = DateTime.UtcNow
            };
        }
        catch (ArgumentException ex)
        {
            throw new ToolExecutionException($"Invalid parameters: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ToolExecutionException($"Network error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new ToolExecutionException($"Unexpected error: {ex.Message}", ex);
        }
    }
}
```

## Using Tools with Agents

### Basic Usage

```csharp
// Create tools
var tools = new List<ITool>
{
    new WeatherTool(),
    new CalculatorTool(),
    new DatabaseTool()
};

// Use with agent
var agent = new Agent(llm, store);
var result = await agent.RunAsync(
    "weather-agent",
    "What's the weather in Tokyo and how many degrees is 25 Celsius in Fahrenheit?",
    tools
);
```

### Tool Selection

```csharp
// Select relevant tools based on the goal
var relevantTools = allTools.Where(tool => 
    IsToolRelevantForGoal(goal, tool)).ToList();

bool IsToolRelevantForGoal(string goal, ITool tool)
{
    var goalLower = goal.ToLower();
    var toolNameLower = tool.Name.ToLower();
    var toolDescLower = tool.Description.ToLower();
    
    return goalLower.Contains(toolNameLower) || 
           goalLower.Split(' ').Any(word => toolDescLower.Contains(word));
}
```

## Real-World Examples

### Travel Planning Tools

```csharp
[ToolParams(Description = "Flight search parameters")]
public sealed class FlightSearchParams
{
    [ToolField(Description = "Departure city", Required = true)]
    [Required]
    public string From { get; set; } = default!;
    
    [ToolField(Description = "Destination city", Required = true)]
    [Required]
    public string To { get; set; } = default!;
    
    [ToolField(Description = "Departure date (YYYY-MM-DD)", Required = true)]
    [Required]
    public string Date { get; set; } = default!;
    
    [ToolField(Description = "Maximum price in USD")]
    public decimal? MaxPrice { get; set; }
}

public sealed class FlightSearchTool : BaseTool<FlightSearchParams, object>
{
    public override string Name => "search_flights";
    public override string Description => "Search for available flights";

    protected override async Task<object> InvokeTypedAsync(FlightSearchParams parameters, CancellationToken ct = default)
    {
        // Simulate flight search
        var flights = new[]
        {
            new { airline = "Delta", price = 450, departure = "09:00", arrival = "11:30" },
            new { airline = "United", price = 380, departure = "14:15", arrival = "16:45" },
            new { airline = "American", price = 520, departure = "07:30", arrival = "10:00" }
        };
        
        var filteredFlights = flights.Where(f => 
            parameters.MaxPrice == null || f.price <= parameters.MaxPrice).ToArray();
        
        return new
        {
            from = parameters.From,
            to = parameters.To,
            date = parameters.Date,
            flights = filteredFlights,
            count = filteredFlights.Length
        };
    }
}
```

### File System Tools

```csharp
[ToolParams(Description = "File operation parameters")]
public sealed class FileOperationParams
{
    [ToolField(Description = "File path", Required = true)]
    [Required]
    public string Path { get; set; } = default!;
    
    [ToolField(Description = "Operation type", Required = true)]
    [Required]
    public string Operation { get; set; } = default!; // "read", "write", "delete"
    
    [ToolField(Description = "Content to write (for write operations)")]
    public string? Content { get; set; }
}

public sealed class FileSystemTool : BaseTool<FileOperationParams, object>
{
    public override string Name => "file_operation";
    public override string Description => "Perform file system operations";

    protected override async Task<object> InvokeTypedAsync(FileOperationParams parameters, CancellationToken ct = default)
    {
        return parameters.Operation.ToLower() switch
        {
            "read" => await ReadFile(parameters.Path, ct),
            "write" => await WriteFile(parameters.Path, parameters.Content ?? "", ct),
            "delete" => await DeleteFile(parameters.Path, ct),
            _ => throw new ArgumentException($"Unknown operation: {parameters.Operation}")
        };
    }
    
    private async Task<object> ReadFile(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");
            
        var content = await File.ReadAllTextAsync(path, ct);
        return new { operation = "read", path, content, success = true };
    }
    
    private async Task<object> WriteFile(string path, string content, CancellationToken ct)
    {
        await File.WriteAllTextAsync(path, content, ct);
        return new { operation = "write", path, success = true };
    }
    
    private object DeleteFile(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");
            
        File.Delete(path);
        return new { operation = "delete", path, success = true };
    }
}
```

## Best Practices

### 1. Tool Naming

Use clear, descriptive names:

```csharp
// Good
public override string Name => "get_weather";
public override string Name => "search_flights";
public override string Name => "calculate_distance";

// Avoid
public override string Name => "tool1";
public override string Name => "function";
```

### 2. Parameter Design

Design parameters for clarity:

```csharp
// Good - Clear parameter names
public string DepartureCity { get; set; } = default!;
public string DestinationCity { get; set; } = default!;

// Avoid - Unclear names
public string From { get; set; } = default!;
public string To { get; set; } = default!;
```

### 3. Error Handling

Provide meaningful error messages:

```csharp
protected override async Task<object> InvokeTypedAsync(MyParams parameters, CancellationToken ct = default)
{
    try
    {
        // Your logic here
    }
    catch (Exception ex)
    {
        throw new ToolExecutionException(
            $"Failed to perform operation: {ex.Message}", ex);
    }
}
```

### 4. Performance

Consider performance implications:

```csharp
// Use cancellation tokens
protected override async Task<object> InvokeTypedAsync(MyParams parameters, CancellationToken ct = default)
{
    // Check cancellation early
    ct.ThrowIfCancellationRequested();
    
    // Use cancellation in async operations
    await SomeAsyncOperation(ct);
}
```

## Testing Tools

### Unit Testing

```csharp
[Test]
public async Task WeatherTool_Should_ReturnWeatherData()
{
    // Arrange
    var tool = new WeatherTool();
    var parameters = new WeatherParams
    {
        City = "Tokyo",
        Unit = "Celsius",
        IncludeHumidity = true
    };
    
    // Act
    var result = await tool.InvokeAsync(parameters, CancellationToken.None);
    
    // Assert
    Assert.IsNotNull(result);
    // Add more specific assertions
}
```

### Integration Testing

```csharp
[Test]
public async Task Agent_Should_UseWeatherTool()
{
    // Arrange
    var tools = new List<ITool> { new WeatherTool() };
    var agent = new Agent(llm, store);
    
    // Act
    var result = await agent.RunAsync(
        "test-agent",
        "What's the weather in Paris?",
        tools
    );
    
    // Assert
    Assert.IsTrue(result.Succeeded);
    Assert.IsTrue(result.FinalOutput.Contains("weather"));
}
```

## Troubleshooting

### Common Issues

**Tool not being called**: Check that the tool name and description match what the LLM expects.

**Parameter validation errors**: Ensure all required parameters are provided and valid.

**Schema generation issues**: Verify that parameter types are supported by the framework.

For more troubleshooting help, see the [Troubleshooting Guide](troubleshooting/common-issues.md).
