namespace AIAgentSharp;

/// <summary>
///     Attribute for marking parameter classes used by tools.
///     This attribute provides metadata for tool parameter classes and is used
///     by the schema generation system to create better tool descriptions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ToolParamsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the description of the parameter class.
    ///     This description is used in tool documentation and schema generation.
    /// </summary>
    public string? Description { get; init; }
}