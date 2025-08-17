namespace AIAgentSharp;

/// <summary>
///     Defines the interface for tools that can provide function calling schemas.
/// </summary>
public interface IFunctionSchemaProvider
{
    /// <summary>
    ///     Gets the unique name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the description of the tool.
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     Returns a JSON-serializable object representing parameters schema (OpenAI-compatible).
    /// </summary>
    /// <returns>A JSON schema object for the tool's parameters.</returns>
    object GetJsonSchema();
}