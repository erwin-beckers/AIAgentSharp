namespace AIAgentSharp;

/// <summary>
///     Defines the interface for tools that can provide introspection information.
/// </summary>
public interface IToolIntrospect
{
    /// <summary>
    ///     Gets the unique name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Returns a concise JSON-ish description of the tool, including params schema.
    ///     Keep it one line if possible.
    /// </summary>
    /// <returns>A JSON string describing the tool and its parameters.</returns>
    string Describe();
}