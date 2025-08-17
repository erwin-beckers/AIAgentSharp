namespace AIAgentSharp;

/// <summary>
///     Represents the result of a function calling operation with an LLM.
/// </summary>
public sealed class FunctionCallResult
{
    /// <summary>
    ///     Gets or sets whether the LLM made a function call.
    /// </summary>
    public bool HasFunctionCall { get; init; }

    /// <summary>
    ///     Gets or sets the name of the function that was called.
    /// </summary>
    public string? FunctionName { get; init; }

    /// <summary>
    ///     Gets or sets the JSON arguments for the function call.
    /// </summary>
    public string? FunctionArgumentsJson { get; init; }

    /// <summary>
    ///     Gets or sets the assistant's content (may be empty on function_call).
    /// </summary>
    public string? AssistantContent { get; init; }

    /// <summary>
    ///     Gets or sets the raw text fallback if no function call was made.
    /// </summary>
    public string? RawTextFallback { get; init; }
}