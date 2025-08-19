# Tool Creation Guide

This guide shows you how to create custom tools for AIAgentSharp agents, from simple tools to complex integrations.

## Overview

Tools in AIAgentSharp are strongly-typed, self-documenting functions that agents can call to perform specific tasks. They provide:
- **Type safety** with compile-time validation
- **Automatic schema generation** for LLM function calling
- **Built-in validation** and error handling
- **Async support** for long-running operations
- **Extensibility** for complex integrations

## Basic Tool Structure

### Simple Tool

Create a basic tool with minimal configuration:

```csharp
using AIAgentSharp.Tools;
using System.ComponentModel.DataAnnotations;

[ToolParams(Description = "Calculate the sum of two numbers")]
public sealed class AddParams
{
    [ToolField(Description = "First number", Example = "5")]
    [Required]
    public double A { get; set; }
    
    [ToolField(Description = "Second number", Example = "3")]
    [Required]
    public double B { get; set; }
}

public sealed class AddTool : BaseTool<AddParams, double>
{
    public override string Name => "add_numbers";
    public override string Description => "Add two numbers together";

    protected override async Task<double> InvokeTypedAsync(AddParams parameters, CancellationToken ct = default)
    {
        return parameters.A + parameters.B;
    }
}
```

### Using the Tool

```csharp
var tools = new List<ITool> { new AddTool() };
var agent = new Agent(llm, store);

var result = await agent.RunAsync("calculator", "What is 15 + 27?", tools);
```

## Advanced Tool Features

### Complex Parameters

Create tools with complex parameter structures:

```csharp
[ToolParams(Description = "Search for products with filters")]
public sealed class ProductSearchParams
{
    [ToolField(Description = "Search query", Example = "laptop")]
    [Required]
    public string Query { get; set; } = default!;
    
    [ToolField(Description = "Minimum price")]
    public decimal? MinPrice { get; set; }
    
    [ToolField(Description = "Maximum price")]
    public decimal? MaxPrice { get; set; }
    
    [ToolField(Description = "Product categories", Example = "electronics,computers")]
    public List<string> Categories { get; set; } = new();
    
    [ToolField(Description = "Sort order", Example = "price_asc")]
    public string SortBy { get; set; } = "relevance";
    
    [ToolField(Description = "Maximum results to return")]
    [Range(1, 100)]
    public int MaxResults { get; set; } = 20;
}

public sealed class ProductSearchTool : BaseTool<ProductSearchParams, object>
{
    public override string Name => "search_products";
    public override string Description => "Search for products in the catalog";

    protected override async Task<object> InvokeTypedAsync(ProductSearchParams parameters, CancellationToken ct = default)
    {
        // Simulate product search
        var products = new List<object>
        {
            new { id = 1, name = "Gaming Laptop", price = 1299.99, category = "electronics" },
            new { id = 2, name = "Office Laptop", price = 799.99, category = "electronics" },
            new { id = 3, name = "Student Laptop", price = 599.99, category = "electronics" }
        };

        // Apply filters
        var filtered = products.Where(p => 
            p.GetType().GetProperty("name")?.GetValue(p)?.ToString()?.Contains(parameters.Query, StringComparison.OrdinalIgnoreCase) == true
        ).Take(parameters.MaxResults);

        return new { products = filtered.ToList(), total = filtered.Count() };
    }
}
```

### Async Operations

Create tools that perform async operations:

```csharp
[ToolParams(Description = "Get weather information for a location")]
public sealed class WeatherParams
{
    [ToolField(Description = "City name", Example = "New York")]
    [Required]
    public string City { get; set; } = default!;
    
    [ToolField(Description = "Country code", Example = "US")]
    public string? Country { get; set; }
    
    [ToolField(Description = "Temperature unit", Example = "Celsius")]
    public string Unit { get; set; } = "Celsius";
}

public sealed class WeatherTool : BaseTool<WeatherParams, object>
{
    private readonly HttpClient _httpClient;

    public WeatherTool(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override string Name => "get_weather";
    public override string Description => "Get current weather for a location";

    protected override async Task<object> InvokeTypedAsync(WeatherParams parameters, CancellationToken ct = default)
    {
        try
        {
            // Simulate API call
            await Task.Delay(100, ct); // Simulate network delay
            
            var weather = new
            {
                city = parameters.City,
                country = parameters.Country ?? "Unknown",
                temperature = 22.5,
                unit = parameters.Unit,
                description = "Partly cloudy",
                humidity = 65,
                wind_speed = 12.3,
                timestamp = DateTime.UtcNow
            };

            return weather;
        }
        catch (Exception ex)
        {
            throw new ToolExecutionException($"Failed to get weather for {parameters.City}: {ex.Message}");
        }
    }
}
```

### Error Handling

Implement robust error handling in tools:

```csharp
public sealed class DatabaseQueryTool : BaseTool<QueryParams, object>
{
    private readonly IDbConnection _connection;

    public DatabaseQueryTool(IDbConnection connection)
    {
        _connection = connection;
    }

    public override string Name => "query_database";
    public override string Description => "Execute a database query";

    protected override async Task<object> InvokeTypedAsync(QueryParams parameters, CancellationToken ct = default)
    {
        try
        {
            // Validate query for security
            if (parameters.Query.Contains("DROP") || parameters.Query.Contains("DELETE"))
            {
                throw new ToolExecutionException("Destructive operations are not allowed");
            }

            // Execute query
            var result = await _connection.QueryAsync(parameters.Query, ct);
            return new { data = result, count = result.Count() };
        }
        catch (SqlException ex)
        {
            throw new ToolExecutionException($"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new ToolExecutionException($"Query execution failed: {ex.Message}");
        }
    }
}
```

## Tool Validation

### Parameter Validation

Use data annotations for automatic validation:

```csharp
[ToolParams(Description = "Send email with validation")]
public sealed class EmailParams
{
    [ToolField(Description = "Recipient email address")]
    [Required]
    [EmailAddress]
    public string To { get; set; } = default!;
    
    [ToolField(Description = "Email subject")]
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = default!;
    
    [ToolField(Description = "Email body")]
    [Required]
    [StringLength(10000)]
    public string Body { get; set; } = default!;
    
    [ToolField(Description = "Priority level")]
    [Range(1, 5)]
    public int Priority { get; set; } = 3;
}
```

### Custom Validation

Implement custom validation logic:

```csharp
public sealed class CustomValidationTool : BaseTool<CustomParams, object>
{
    public override string Name => "custom_validation";
    public override string Description => "Tool with custom validation";

    protected override async Task<object> InvokeTypedAsync(CustomParams parameters, CancellationToken ct = default)
    {
        // Custom validation
        if (parameters.Age < 18)
        {
            throw new ToolExecutionException("User must be 18 or older");
        }

        if (parameters.Budget <= 0)
        {
            throw new ToolExecutionException("Budget must be greater than zero");
        }

        // Process the request
        return new { status = "validated", processed = true };
    }
}
```

## Tool Integration

### External API Integration

Create tools that integrate with external services:

```csharp
public sealed class PaymentTool : BaseTool<PaymentParams, object>
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentTool> _logger;

    public PaymentTool(IPaymentService paymentService, ILogger<PaymentTool> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public override string Name => "process_payment";
    public override string Description => "Process a payment transaction";

    protected override async Task<object> InvokeTypedAsync(PaymentParams parameters, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing payment for amount: {Amount}", parameters.Amount);

            var result = await _paymentService.ProcessPaymentAsync(
                parameters.Amount,
                parameters.Currency,
                parameters.CardToken,
                ct
            );

            return new
            {
                transaction_id = result.TransactionId,
                status = result.Status,
                amount = result.Amount,
                timestamp = result.Timestamp
            };
        }
        catch (PaymentException ex)
        {
            _logger.LogError(ex, "Payment processing failed");
            throw new ToolExecutionException($"Payment failed: {ex.Message}");
        }
    }
}
```

### File System Tools

Create tools for file operations:

```csharp
public sealed class FileReadTool : BaseTool<FileReadParams, object>
{
    public override string Name => "read_file";
    public override string Description => "Read content from a file";

    protected override async Task<object> InvokeTypedAsync(FileReadParams parameters, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(parameters.FilePath))
            {
                throw new ToolExecutionException($"File not found: {parameters.FilePath}");
            }

            var content = await File.ReadAllTextAsync(parameters.FilePath, ct);
            
            return new
            {
                file_path = parameters.FilePath,
                content = content,
                size_bytes = content.Length,
                last_modified = File.GetLastWriteTime(parameters.FilePath)
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw new ToolExecutionException($"Access denied to file: {parameters.FilePath}");
        }
        catch (Exception ex)
        {
            throw new ToolExecutionException($"Failed to read file: {ex.Message}");
        }
    }
}
```

## Tool Testing

### Unit Testing Tools

Test your tools thoroughly:

```csharp
[TestClass]
public class AddToolTests
{
    private AddTool _tool = null!;

    [TestInitialize]
    public void Setup()
    {
        _tool = new AddTool();
    }

    [TestMethod]
    public async Task InvokeAsync_WithValidParams_ReturnsCorrectSum()
    {
        // Arrange
        var parameters = new AddParams { A = 5, B = 3 };

        // Act
        var result = await _tool.InvokeAsync(parameters);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(8.0, result.Result);
    }

    [TestMethod]
    public async Task InvokeAsync_WithNegativeNumbers_ReturnsCorrectSum()
    {
        // Arrange
        var parameters = new AddParams { A = -5, B = 3 };

        // Act
        var result = await _tool.InvokeAsync(parameters);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(-2.0, result.Result);
    }
}
```

### Integration Testing

Test tools with real dependencies:

```csharp
[TestClass]
public class WeatherToolIntegrationTests
{
    private WeatherTool _tool = null!;
    private HttpClient _httpClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _httpClient = new HttpClient();
        _tool = new WeatherTool(_httpClient);
    }

    [TestMethod]
    public async Task InvokeAsync_WithValidCity_ReturnsWeatherData()
    {
        // Arrange
        var parameters = new WeatherParams { City = "London", Unit = "Celsius" };

        // Act
        var result = await _tool.InvokeAsync(parameters);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Result);
        
        var weather = result.Result as dynamic;
        Assert.AreEqual("London", weather.city);
        Assert.AreEqual("Celsius", weather.unit);
    }
}
```

## Best Practices

### 1. Tool Design

- **Keep tools focused** on a single responsibility
- **Use descriptive names** and descriptions
- **Provide examples** in parameter descriptions
- **Handle errors gracefully** with meaningful messages

### 2. Parameter Design

- **Use appropriate data types** for parameters
- **Add validation** with data annotations
- **Provide defaults** where appropriate
- **Document constraints** clearly

### 3. Error Handling

- **Throw `ToolExecutionException`** for tool-specific errors
- **Log errors** for debugging
- **Provide user-friendly** error messages
- **Handle timeouts** and cancellation

### 4. Performance

- **Use async operations** for I/O-bound tasks
- **Implement caching** where appropriate
- **Handle rate limits** for external APIs
- **Optimize for common use cases**

### 5. Security

- **Validate all inputs** thoroughly
- **Sanitize data** before processing
- **Use secure connections** for external APIs
- **Implement proper authentication** where needed

## Complete Example

Here's a complete example of a production-ready tool:

```csharp
using AIAgentSharp.Tools;
using System.ComponentModel.DataAnnotations;

[ToolParams(Description = "Search for flights with advanced filters")]
public sealed class FlightSearchParams
{
    [ToolField(Description = "Departure airport code", Example = "JFK")]
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Origin { get; set; } = default!;
    
    [ToolField(Description = "Destination airport code", Example = "LAX")]
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Destination { get; set; } = default!;
    
    [ToolField(Description = "Departure date", Example = "2024-05-15")]
    [Required]
    public DateTime DepartureDate { get; set; }
    
    [ToolField(Description = "Return date (optional for one-way)")]
    public DateTime? ReturnDate { get; set; }
    
    [ToolField(Description = "Number of passengers")]
    [Range(1, 9)]
    public int Passengers { get; set; } = 1;
    
    [ToolField(Description = "Maximum price per passenger")]
    [Range(0, 10000)]
    public decimal? MaxPrice { get; set; }
    
    [ToolField(Description = "Preferred airlines", Example = "Delta,American")]
    public List<string> PreferredAirlines { get; set; } = new();
}

public sealed class FlightSearchTool : BaseTool<FlightSearchParams, object>
{
    private readonly IFlightService _flightService;
    private readonly ILogger<FlightSearchTool> _logger;

    public FlightSearchTool(IFlightService flightService, ILogger<FlightSearchTool> logger)
    {
        _flightService = flightService;
        _logger = logger;
    }

    public override string Name => "search_flights";
    public override string Description => "Search for available flights with pricing";

    protected override async Task<object> InvokeTypedAsync(FlightSearchParams parameters, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Searching flights from {Origin} to {Destination} on {Date}", 
                parameters.Origin, parameters.Destination, parameters.DepartureDate);

            // Validate dates
            if (parameters.DepartureDate < DateTime.Today)
            {
                throw new ToolExecutionException("Departure date cannot be in the past");
            }

            if (parameters.ReturnDate.HasValue && parameters.ReturnDate <= parameters.DepartureDate)
            {
                throw new ToolExecutionException("Return date must be after departure date");
            }

            // Search flights
            var flights = await _flightService.SearchFlightsAsync(
                parameters.Origin,
                parameters.Destination,
                parameters.DepartureDate,
                parameters.ReturnDate,
                parameters.Passengers,
                parameters.MaxPrice,
                parameters.PreferredAirlines,
                ct
            );

            return new
            {
                search_criteria = new
                {
                    origin = parameters.Origin,
                    destination = parameters.Destination,
                    departure_date = parameters.DepartureDate,
                    return_date = parameters.ReturnDate,
                    passengers = parameters.Passengers
                },
                flights = flights.Select(f => new
                {
                    airline = f.Airline,
                    flight_number = f.FlightNumber,
                    departure_time = f.DepartureTime,
                    arrival_time = f.ArrivalTime,
                    price_per_passenger = f.PricePerPassenger,
                    total_price = f.PricePerPassenger * parameters.Passengers,
                    stops = f.NumberOfStops
                }),
                total_flights = flights.Count,
                search_timestamp = DateTime.UtcNow
            };
        }
        catch (FlightServiceException ex)
        {
            _logger.LogError(ex, "Flight search failed");
            throw new ToolExecutionException($"Flight search failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during flight search");
            throw new ToolExecutionException("An unexpected error occurred during flight search");
        }
    }
}
```

This comprehensive tool creation guide provides everything you need to build robust, production-ready tools for your AIAgentSharp agents.
