using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp.Agents.ChainOfThought;

/// <summary>
/// Result of Chain of Thought validation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ChainValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
}