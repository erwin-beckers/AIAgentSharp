using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Attribute for marking and configuring properties in tool parameter classes.
///     This attribute provides metadata for tool parameter properties and is used
///     by the schema generation system to create comprehensive tool descriptions.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property)]
public sealed class ToolFieldAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the description of the property.
    ///     This description is used in tool documentation and schema generation.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Gets or sets an example value for the property.
    ///     This example is used in tool documentation to show expected values.
    /// </summary>
    public object? Example { get; init; }

    /// <summary>
    ///     Gets or sets whether the property is required.
    ///     If not specified, this is inferred from nullability and DataAnnotations.
    /// </summary>
    public bool Required { get; init; }

    /// <summary>
    ///     Gets or sets the minimum length for string properties.
    ///     Default is -1 (no minimum).
    /// </summary>
    public int MinLength { get; init; } = -1;

    /// <summary>
    ///     Gets or sets the maximum length for string properties.
    ///     Default is -1 (no maximum).
    /// </summary>
    public int MaxLength { get; init; } = -1;

    /// <summary>
    ///     Gets or sets the minimum value for numeric properties.
    ///     Default is double.NaN (no minimum).
    /// </summary>
    public double Minimum { get; init; } = double.NaN;

    /// <summary>
    ///     Gets or sets the maximum value for numeric properties.
    ///     Default is double.NaN (no maximum).
    /// </summary>
    public double Maximum { get; init; } = double.NaN;

    /// <summary>
    ///     Gets or sets the regex pattern for string validation.
    ///     Used for validating string properties against a specific pattern.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    ///     Gets or sets the format for the property (e.g., email, uri, date-time).
    ///     This is used in schema generation to specify the expected format.
    /// </summary>
    public string? Format { get; init; }
}