using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Represents token usage for a specific model.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TokenUsage
{
    public string Model { get; set; } = string.Empty;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
}