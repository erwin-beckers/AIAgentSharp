using AIAgentSharp.Agents;
using AIAgentSharp;

namespace AIAgentSharp.Fluent;

/// <summary>
/// Static factory class for creating AIAgent instances using the fluent API.
/// </summary>
public static class AIAgent
{
    /// <summary>
    /// Creates a new AIAgentBuilder instance for fluent configuration.
    /// </summary>
    /// <returns>A new AIAgentBuilder instance</returns>
    public static AIAgentBuilder Create()
    {
        return new AIAgentBuilder();
    }

    /// <summary>
    /// Creates a new AIAgentBuilder instance with the specified LLM client.
    /// </summary>
    /// <param name="llmClient">The LLM client to use</param>
    /// <returns>A new AIAgentBuilder instance configured with the LLM client</returns>
    public static AIAgentBuilder Create(ILlmClient llmClient)
    {
        return new AIAgentBuilder().WithLlm(llmClient);
    }
}
