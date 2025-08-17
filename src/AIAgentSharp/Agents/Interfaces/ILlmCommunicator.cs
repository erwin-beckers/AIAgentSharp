

namespace AIAgentSharp.Agents.Interfaces;

/// <summary>
/// Handles communication with LLM providers, including function calling and JSON parsing.
/// </summary>
public interface ILlmCommunicator
{
    /// <summary>
    /// Calls the LLM with function calling support.
    /// </summary>
    Task<FunctionCallResult> CallWithFunctionsAsync(IEnumerable<LlmMessage> messages, List<OpenAiFunctionSpec> functionSpecs, string agentId, int turnIndex, CancellationToken ct);

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
    ModelMessage NormalizeFunctionCallToReact(FunctionCallResult functionResult, int turnIndex);
}
