namespace AIAgentSharp;

/// <summary>
///     Defines the schema information for a tool, including parameter and result types.
/// </summary>
public interface IToolSchema
{
    /// <summary>
    ///     Gets the type of the tool's parameters.
    /// </summary>
    Type ParameterType { get; }

    /// <summary>
    ///     Gets the type of the tool's result.
    /// </summary>
    Type ResultType { get; }
}