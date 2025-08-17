using System.ComponentModel.DataAnnotations;

namespace AIAgentSharp;

/// <summary>
///     Parameters for fetching a single indicator value.
/// </summary>
[ToolParams(Description = "Parameters to fetch a single indicator value.")]
public sealed class GetIndicatorParams
{
    /// <summary>
    ///     Gets or sets the trading symbol to fetch the indicator for.
    /// </summary>
    [ToolField(Description = "Trading symbol", Example = "MNQ", Required = true)]
    [Required]
    public string Symbol { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the type of indicator to fetch.
    /// </summary>
    [ToolField(Description = "Indicator type", Example = "RSI", Required = true)]
    [Required]
    public string Indicator { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the lookback period for the indicator calculation.
    /// </summary>
    [ToolField(Description = "Lookback period", Example = 14, Required = true, Minimum = 1)]
    [Range(1, int.MaxValue)]
    public int Period { get; set; }
}