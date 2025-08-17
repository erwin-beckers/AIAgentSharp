

namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Builds messages for LLM communication, including prompt construction and history management.
/// </summary>
public interface IMessageBuilder
{
    /// <summary>
    /// Builds the complete message set for LLM communication.
    /// </summary>
    /// <param name="state">Current agent state</param>
    /// <param name="tools">Available tools</param>
    /// <returns>Collection of LLM messages</returns>
    IEnumerable<LlmMessage> BuildMessages(AgentState state, IDictionary<string, ITool> tools);
}
