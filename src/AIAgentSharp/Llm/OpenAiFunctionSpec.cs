namespace AIAgentSharp;

/// <summary>
///     Represents a function specification for OpenAI-style function calling.
/// </summary>
public sealed class OpenAiFunctionSpec
{
    /// <summary>
    ///     Gets or sets the name of the function.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    ///     Gets or sets the description of the function.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    ///     Gets or sets the JSON schema for the function's parameters.
    /// </summary>
    public object ParametersSchema { get; init; } = new { };
}