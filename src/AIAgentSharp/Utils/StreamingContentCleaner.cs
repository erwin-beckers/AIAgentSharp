using System.Text;

namespace AIAgentSharp.Utils;

/// <summary>
/// Stateful utility class for cleaning up streaming JSON content to extract only user-facing text.
/// Uses a simple state machine to handle character-by-character streaming.
/// </summary>
public class StreamingContentCleaner
{
    private enum State
    {
        LookingForField,
        InFieldName,
        LookingForColon,
        LookingForQuote,
        InFieldValue,
        InEscape
    }

    private State _currentState = State.LookingForField;
    private readonly StringBuilder _fieldName = new StringBuilder();
    private readonly StringBuilder _output = new StringBuilder();
    private bool _isTargetField = false;

    /// <summary>
    /// Processes a chunk of streaming content and returns cleaned content if available.
    /// Outputs content immediately as it's processed, without buffering.
    /// </summary>
    /// <param name="chunk">The chunk of content to process</param>
    /// <returns>Cleaned content from the chunk, or empty string if no content to output</returns>
    public string ProcessChunk(string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
            return string.Empty;
        //Console.WriteLine($"ProcessChunk:{chunk}");
        var result = new StringBuilder();
        
        foreach (char c in chunk)
        {
            var output = ProcessCharacter(c);
            if (output.HasValue)
            {
                result.Append(output.Value);
            }
        }
        
        return result.ToString();
    }

    private char? ProcessCharacter(char c)
    {
        switch (_currentState)
        {
            case State.LookingForField:
                if (c == '"')
                {
                    _currentState = State.InFieldName;
                    _fieldName.Clear();
                }
                return null;

            case State.InFieldName:
                if (c == '"')
                {
                    // End of field name
                    var fieldName = _fieldName.ToString();
                    _isTargetField = fieldName.Equals("reasoning", StringComparison.OrdinalIgnoreCase) ||
                                   fieldName.Equals("thoughts", StringComparison.OrdinalIgnoreCase);
                    _currentState = State.LookingForColon;
                }
                else
                {
                    _fieldName.Append(c);
                }
                return null;

            case State.LookingForColon:
                if (c == ':')
                {
                    _currentState = State.LookingForQuote;
                }
                else if (!char.IsWhiteSpace(c))
                {
                    // Not a field value, go back to looking for field
                    _currentState = State.LookingForField;
                    _isTargetField = false;
                }
                return null;

            case State.LookingForQuote:
                if (c == '"')
                {
                    _currentState = State.InFieldValue;
                }
                else if (!char.IsWhiteSpace(c))
                {
                    // Not a string value, go back to looking for field
                    _currentState = State.LookingForField;
                    _isTargetField = false;
                }
                return null;

            case State.InFieldValue:
                if (c == '\\')
                {
                    _currentState = State.InEscape;
                    return null;
                }
                else if (c == '"')
                {
                    // End of field value
                    _currentState = State.LookingForField;
                    _isTargetField = false;
                    return null;
                }
                else if (_isTargetField)
                {
                    return c; // Output this character immediately
                }
                return null;

            case State.InEscape:
                _currentState = State.InFieldValue;
                if (_isTargetField)
                {
                    // Convert escaped sequences to their actual characters
                    return c switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '\\' => '\\',
                        '"' => '"',
                        _ => c // Return the character as-is for unknown escapes
                    };
                }
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Flushes any remaining content in the buffer.
    /// </summary>
    /// <returns>Any remaining cleaned content</returns>
    public string Flush()
    {
        return string.Empty;
    }

    /// <summary>
    /// Resets the cleaner state for a new streaming session.
    /// </summary>
    public void Reset()
    {
        _currentState = State.LookingForField;
        _fieldName.Clear();
        _output.Clear();
        _isTargetField = false;
    }
}
