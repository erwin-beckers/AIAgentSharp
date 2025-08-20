using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
/// Represents a function specification that can be used for function calling across different LLM providers.
/// This is a provider-agnostic representation of function definitions.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FunctionSpec
{
    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the function.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the JSON schema for the function parameters.
    /// This should be a valid JSON Schema object.
    /// </summary>
    public object ParametersSchema { get; init; } = new { };
}
