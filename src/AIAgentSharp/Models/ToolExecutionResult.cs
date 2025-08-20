using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Represents the result of a tool execution, including success status, output, and metadata.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ToolExecutionResult
{
    /// <summary>
    ///     Gets or sets whether the tool execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the output from the tool execution.
    /// </summary>
    public object? Output { get; set; }

    /// <summary>
    ///     Gets or sets the error message if the tool execution failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    ///     Gets or sets the name of the tool that was executed.
    /// </summary>
    public string Tool { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the parameters that were passed to the tool.
    /// </summary>
    public Dictionary<string, object?> Params { get; set; } = new();

    /// <summary>
    ///     Gets or sets the unique identifier for this tool execution, used for idempotency.
    /// </summary>
    public string TurnId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the time taken to execute the tool.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this result was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}