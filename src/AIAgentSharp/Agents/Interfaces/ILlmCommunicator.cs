

namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Handles communication with LLM providers, including function calling and JSON parsing.
/// </summary>
public interface ILlmCommunicator
{
    /// <summary>
    /// Calls the LLM with function calling support using the unified interface.
    /// </summary>
    Task<LlmResponse> CallWithFunctionsAsync(IEnumerable<LlmMessage> messages, List<FunctionSpec> functionSpecs, string agentId, int turnIndex, CancellationToken ct);

    /// <summary>
    /// Calls the LLM and parses the JSON response.
    /// </summary>
    Task<ModelMessage?> CallLlmAndParseAsync(IEnumerable<LlmMessage> messages, string agentId, int turnIndex, string turnId, AgentState state, CancellationToken ct);

    /// <summary>
    /// Parses a JSON response from the LLM.
    /// </summary>
    Task<ModelMessage?> ParseJsonResponse(string llmRaw, int turnIndex, string turnId, AgentState state, CancellationToken ct);

    /// <summary>
    /// Normalizes a function call result to Re/Act format.
    /// </summary>
    ModelMessage NormalizeFunctionCallToReact(LlmResponse functionResult, int turnIndex);

    /// <summary>
    /// Gets the underlying LLM client for direct access when needed.
    /// </summary>
    ILlmClient GetLlmClient();
}
