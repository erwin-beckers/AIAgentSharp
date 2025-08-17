namespace AIAgentSharp;

/// <summary>
///     Represents a request to call a specific tool with parameters.
/// </summary>
public sealed class ToolCallRequest
{
    /// <summary>
    ///     Gets or sets the name of the tool to call.
    /// </summary>
    public string Tool { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the parameters to pass to the tool.
    /// </summary>
    public Dictionary<string, object?> Params { get; set; } = new();

    /// <summary>
    ///     Gets or sets the unique identifier for this tool call, used for idempotency.
    /// </summary>
    public string TurnId { get; set; } = string.Empty;
}