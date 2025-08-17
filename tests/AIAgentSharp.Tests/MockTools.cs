using AIAgentSharp;
using System.ComponentModel.DataAnnotations;

namespace AIAgentSharp.Tests;

// Parameter classes for mock tools
public class MockConcatParams
{
    [Required]
    public string[] Strings { get; set; } = Array.Empty<string>();
}

public class MockValidationParams
{
    [Required]
    [MinLength(1)]
    public string Input { get; set; } = string.Empty;
    
    [Required]
    public string[] Rules { get; set; } = Array.Empty<string>();
}

public class MockExceptionParams
{
    public bool ShouldThrow { get; set; }
    
    public string ExceptionMessage { get; set; } = "Something went wrong";
}

public class MockSlowParams
{
    public int DelayMs { get; set; } = 1000;
}

public class MockCustomTtlParams
{
    [Required]
    public string Input { get; set; } = string.Empty;
}

public class MockNonDedupedParams
{
    [Required]
    public string Input { get; set; } = string.Empty;
}

public class MockSchemaParams
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public int? Age { get; set; }
    
    public string? Email { get; set; }
}

/// <summary>
/// Mock tool for testing purposes - concatenates strings
/// </summary>
public class MockConcatTool : BaseTool<MockConcatParams, string>
{
    public override string Name => "concat";
    public override string Description => "Concatenate multiple strings together";

    protected override async Task<string> InvokeTypedAsync(MockConcatParams parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate some work
        return string.Join("", parameters.Strings);
    }
}

/// <summary>
/// Mock tool for testing purposes - validates parameters
/// </summary>
public class MockValidationTool : BaseTool<MockValidationParams, object>
{
    public override string Name => "validate_input";
    public override string Description => "Validate input data with custom rules";

    protected override async Task<object> InvokeTypedAsync(MockValidationParams parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        return new { input = parameters.Input, rules = parameters.Rules, validated = true };
    }
}

/// <summary>
/// Mock tool for testing purposes - throws exceptions
/// </summary>
public class MockExceptionTool : BaseTool<MockExceptionParams, object>
{
    public override string Name => "exception_tool";
    public override string Description => "Tool that throws exceptions for testing error handling";

    protected override async Task<object> InvokeTypedAsync(MockExceptionParams parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        
        if (parameters.ShouldThrow)
        {
            throw new Exception(parameters.ExceptionMessage);
        }
        
        return new { success = true, message = "Operation completed successfully" };
    }
}

/// <summary>
/// Mock tool for testing purposes - slow execution
/// </summary>
public class MockSlowTool : BaseTool<MockSlowParams, object>
{
    public override string Name => "slow_tool";
    public override string Description => "Tool that takes a long time to execute for timeout testing";

    protected override async Task<object> InvokeTypedAsync(MockSlowParams parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(parameters.DelayMs, cancellationToken);
        return new { completed = true, delay = parameters.DelayMs };
    }
}

/// <summary>
/// Mock tool for testing purposes - custom TTL
/// </summary>
public class MockCustomTtlTool : BaseTool<MockCustomTtlParams, object>, IDedupeControl
{
    public override string Name => "custom-ttl-tool";
    public override string Description => "Tool with custom TTL for deduplication testing";

    public bool AllowDedupe => true;
    public TimeSpan? CustomTtl => TimeSpan.FromMinutes(10);

    protected override async Task<object> InvokeTypedAsync(MockCustomTtlParams parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken);
        return new { result = $"Processed: {parameters.Input}", timestamp = DateTime.UtcNow };
    }
}

/// <summary>
/// Mock tool for testing purposes - no deduplication
/// </summary>
public class MockNonDedupedTool : BaseTool<MockNonDedupedParams, object>, IDedupeControl
{
    public override string Name => "non-deduped-tool";
    public override string Description => "Tool that doesn't use deduplication";

    public bool AllowDedupe => false;
    public TimeSpan? CustomTtl => null;

    protected override async Task<object> InvokeTypedAsync(MockNonDedupedParams parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken);
        return new { result = $"Processed: {parameters.Input}", timestamp = DateTime.UtcNow };
    }
}

/// <summary>
/// Mock tool for testing purposes - schema validation
/// </summary>
public class MockSchemaTool : BaseTool<MockSchemaParams, object>
{
    public override string Name => "schema_tool";
    public override string Description => "Tool for testing schema validation";

    protected override async Task<object> InvokeTypedAsync(MockSchemaParams parameters, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        
        if (string.IsNullOrEmpty(parameters.Name))
        {
            throw new Exception("Name is required");
        }
        
        return new { name = parameters.Name, age = parameters.Age, email = parameters.Email };
    }
}
