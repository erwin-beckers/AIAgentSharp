using System.ComponentModel.DataAnnotations;

namespace AIAgentSharp.Tests;

// Simple math tools for testing
public class AddTool : BaseTool<AddParams, double>
{
    public override string Name => "add";
    public override string Description => "Adds two numbers together";

    protected override Task<double> InvokeTypedAsync(AddParams parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(parameters.A + parameters.B);
    }
}

public class AddParams
{
    [Required]
    public double A { get; set; }
    
    [Required]
    public double B { get; set; }
}

public class MultiplyTool : BaseTool<MultiplyParams, double>
{
    public override string Name => "multiply";
    public override string Description => "Multiplies two numbers together";

    protected override Task<double> InvokeTypedAsync(MultiplyParams parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(parameters.A * parameters.B);
    }
}

public class MultiplyParams
{
    [Required]
    public double A { get; set; }
    
    [Required]
    public double B { get; set; }
}

public class DivideTool : BaseTool<DivideParams, double>
{
    public override string Name => "divide";
    public override string Description => "Divides two numbers";

    protected override Task<double> InvokeTypedAsync(DivideParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters.B == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero");
        }
        return Task.FromResult(parameters.A / parameters.B);
    }
}

public class DivideParams
{
    [Required]
    public double A { get; set; }
    
    [Required]
    public double B { get; set; }
}

// Existing mock tools...
public class MockConcatParams
{
    [Required]
    public string[] Strings { get; set; } = Array.Empty<string>();
    
    public string Separator { get; set; } = " ";
}

public class MockConcatTool : BaseTool<MockConcatParams, string>
{
    public override string Name => "concat";
    public override string Description => "Concatenate multiple strings together";

    protected override Task<string> InvokeTypedAsync(MockConcatParams parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(string.Join(parameters.Separator, parameters.Strings));
    }
}

public class MockValidationParams
{
    [Required]
    [MinLength(1)]
    public string Input { get; set; } = string.Empty;
    
    [Required]
    public string[] Rules { get; set; } = Array.Empty<string>();
}

public class MockValidationTool : BaseTool<MockValidationParams, object>
{
    public override string Name => "validation_tool";
    public override string Description => "Validate input data with custom rules";

    protected override Task<object> InvokeTypedAsync(MockValidationParams parameters, CancellationToken cancellationToken = default)
    {
        // Simulate validation delay
        Thread.Sleep(100);
        
        if (string.IsNullOrEmpty(parameters.Input))
        {
            throw new ToolValidationException("Input cannot be empty", new List<string> { "input" });
        }
        
        return Task.FromResult<object>(new { success = true, validated_input = parameters.Input });
    }
}

public class MockExceptionParams
{
    [Required]
    public string ErrorType { get; set; } = string.Empty;
}

public class MockExceptionTool : BaseTool<MockExceptionParams, object>
{
    public override string Name => "exception_tool";
    public override string Description => "Throws different types of exceptions for testing";

    protected override Task<object> InvokeTypedAsync(MockExceptionParams parameters, CancellationToken cancellationToken = default)
    {
        switch (parameters.ErrorType.ToLower())
        {
            case "validation":
                throw new ToolValidationException("Validation error", new List<string> { "param" });
            case "argument":
                throw new ArgumentException("Argument error");
            case "invalidoperation":
                throw new InvalidOperationException("Invalid operation error");
            case "timeout":
                throw new TimeoutException("Timeout error");
            default:
                throw new Exception($"Unknown error type: {parameters.ErrorType}");
        }
    }
}

public class MockSlowParams
{
    public int DelayMs { get; set; } = 1000;
}

public class MockSlowTool : BaseTool<MockSlowParams, object>
{
    public override string Name => "slow_tool";
    public override string Description => "A tool that takes time to execute";

    protected override async Task<object> InvokeTypedAsync(MockSlowParams parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(parameters.DelayMs, cancellationToken);
        return new { result = "slow operation completed", delay_ms = parameters.DelayMs };
    }
}

public class MockCustomTtlParams
{
    public string Data { get; set; } = string.Empty;
}

public class MockCustomTtlTool : BaseTool<MockCustomTtlParams, object>, IDedupeControl
{
    public override string Name => "custom_ttl_tool";
    public override string Description => "A tool with custom TTL settings";

    public bool AllowDedupe => true;
    public TimeSpan? CustomTtl => TimeSpan.FromMinutes(10);

    protected override Task<object> InvokeTypedAsync(MockCustomTtlParams parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { result = parameters.Data, ttl_minutes = 10 });
    }
}

public class MockNonDedupedParams
{
    public string Data { get; set; } = string.Empty;
}

public class MockNonDedupedTool : BaseTool<MockNonDedupedParams, object>, IDedupeControl
{
    public override string Name => "non_deduped_tool";
    public override string Description => "A tool that doesn't allow deduplication";

    public bool AllowDedupe => false;
    public TimeSpan? CustomTtl => null;

    protected override Task<object> InvokeTypedAsync(MockNonDedupedParams parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { result = parameters.Data, dedupe_allowed = false });
    }
}

public class MockSchemaParams
{
    [Required]
    public string RequiredField { get; set; } = string.Empty;
    
    public string? OptionalField { get; set; }
}

public class MockSchemaTool : BaseTool<MockSchemaParams, object>
{
    public override string Name => "schema_tool";
    public override string Description => "A tool that provides its own schema";

    protected override Task<object> InvokeTypedAsync(MockSchemaParams parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { 
            required_field = parameters.RequiredField, 
            optional_field = parameters.OptionalField 
        });
    }
}
