namespace AIAgentSharp;

/// <summary>
///     Defines a strongly-typed tool with generic parameter and result types.
/// </summary>
/// <typeparam name="TParams">The type of the tool's parameters.</typeparam>
/// <typeparam name="TResult">The type of the tool's result.</typeparam>
public interface ITool<TParams, TResult> : ITool, IToolSchema
{
    /// <summary>
    ///     Gets the parameter type for this tool.
    /// </summary>
    Type IToolSchema.ParameterType => typeof(TParams);

    /// <summary>
    ///     Gets the result type for this tool.
    /// </summary>
    Type IToolSchema.ResultType => typeof(TResult);
}