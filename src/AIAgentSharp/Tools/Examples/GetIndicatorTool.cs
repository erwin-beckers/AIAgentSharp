namespace AIAgentSharp;

/// <summary>
///     A tool for fetching indicator values for trading symbols.
///     This is an example tool that demonstrates the BaseTool pattern.
/// </summary>
public sealed class GetIndicatorTool : BaseTool<GetIndicatorParams, object>
{
    /// <summary>
    ///     Gets the unique name of the tool.
    /// </summary>
    public override string Name => "get_indicator";

    /// <summary>
    ///     Gets the description of the tool.
    /// </summary>
    public override string Description => "Fetch a single indicator value for a symbol.";

    /// <summary>
    ///     Invokes the tool to fetch an indicator value for the specified symbol.
    /// </summary>
    /// <param name="parameters">The parameters containing symbol, indicator type, and period.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The indicator value as an object.</returns>
    public override Task<object> InvokeTypedAsync(GetIndicatorParams parameters, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Simulate fetching indicator data
        var random = new Random();
        var value = parameters.Indicator.ToUpperInvariant() switch
        {
            "RSI" => random.NextDouble() * 100, // RSI is 0-100
            "ATR" => random.NextDouble() * 10, // ATR is typically 0-10
            _ => throw new ArgumentException($"Unknown indicator: {parameters.Indicator}")
        };

        return Task.FromResult<object>(new
        {
            symbol = parameters.Symbol,
            indicator = parameters.Indicator,
            period = parameters.Period,
            value = Math.Round(value, 2)
        });
    }
}