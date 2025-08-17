namespace AIAgentSharp;

/// <summary>
///     Defines the interface for a tool that can be called by an agent.
/// </summary>
public interface ITool
{
    /// <summary>
    ///     Gets the unique name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Invokes the tool with the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters to pass to the tool.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default);
}