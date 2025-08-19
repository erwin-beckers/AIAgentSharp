namespace AIAgentSharp;

/// <summary>
/// Defines the type of response expected from the LLM.
/// </summary>
public enum LlmResponseType
{
    /// <summary>
    /// Regular text completion response.
    /// </summary>
    Text,

    /// <summary>
    /// Function calling response with structured function calls.
    /// </summary>
    FunctionCall,

    /// <summary>
    /// Streaming response with real-time text chunks.
    /// </summary>
    Streaming,

    /// <summary>
    /// Auto-detect the best response type based on available functions and configuration.
    /// </summary>
    Auto
}