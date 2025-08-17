namespace AIAgentSharp;

/// <summary>
///     Exception thrown when tool parameter validation fails.
/// </summary>
public class ToolValidationException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the ToolValidationException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="missing">Optional list of missing required fields.</param>
    /// <param name="fieldErrors">Optional list of field-specific validation errors.</param>
    public ToolValidationException(string message, List<string>? missing = null, List<ToolValidationError>? fieldErrors = null)
        : base(message)
    {
        Missing = missing ?? new List<string>();
        FieldErrors = fieldErrors ?? new List<ToolValidationError>();
    }

    /// <summary>
    ///     Gets the list of missing required fields.
    /// </summary>
    public List<string> Missing { get; }

    /// <summary>
    ///     Gets the list of field-specific validation errors.
    /// </summary>
    public List<ToolValidationError> FieldErrors { get; }
}