namespace AIAgentSharp;

/// <summary>
/// Represents a function call made by the LLM.
/// </summary>
public sealed class LlmFunctionCall
{
    /// <summary>
    /// Gets or sets the name of the function that was called.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments for the function call as a dictionary.
    /// </summary>
    public Dictionary<string, object> Arguments { get; init; } = new();

    /// <summary>
    /// Gets or sets the raw JSON arguments string for the function call.
    /// </summary>
    public string ArgumentsJson { get; init; } = string.Empty;
}