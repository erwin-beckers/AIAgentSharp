using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
///     Represents a validation error for a specific field in tool parameters.
/// </summary>
[ExcludeFromCodeCoverage]
public class ToolValidationError
{
    /// <summary>
    ///     Initializes a new instance of the ToolValidationError class.
    /// </summary>
    /// <param name="field">The name of the field that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    public ToolValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }

    /// <summary>
    ///     Gets the name of the field that failed validation.
    /// </summary>
    public string Field { get; }

    /// <summary>
    ///     Gets the validation error message.
    /// </summary>
    public string Message { get; }
}