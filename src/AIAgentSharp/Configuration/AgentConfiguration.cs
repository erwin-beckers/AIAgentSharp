namespace AIAgentSharp;

/// <summary>
///     Configuration options for the agent, including timeouts, limits, and feature flags.
///     This class provides centralized configuration for all agent behavior.
/// </summary>
public sealed class AgentConfiguration
{
    /// <summary>
    ///     Maximum number of turns to keep in full detail in the prompt.
    ///     Older turns are summarized to reduce token usage.
    /// </summary>
    public int MaxRecentTurns { get; init; } = 10;

    /// <summary>
    ///     Maximum number of characters for the thoughts field in LLM responses.
    ///     Prevents excessive verbosity in agent reasoning.
    /// </summary>
    public int MaxThoughtsLength { get; init; } = 20000;

    /// <summary>
    ///     Maximum number of characters for the final output field.
    ///     Limits the size of final agent responses.
    /// </summary>
    public int MaxFinalLength { get; init; } = 50000;

    /// <summary>
    ///     Maximum number of characters for the summary field.
    ///     Controls the size of planning summaries.
    /// </summary>
    public int MaxSummaryLength { get; init; } = 40000;

    /// <summary>
    ///     Whether to enable history summarization for older turns.
    ///     When enabled, older turns are summarized to reduce prompt size.
    /// </summary>
    public bool EnableHistorySummarization { get; init; } = true;

    /// <summary>
    ///     Maximum number of tool call history records to keep per agent.
    ///     Used for loop detection and deduplication.
    /// </summary>
    public int MaxToolCallHistory { get; init; } = 20;

    /// <summary>
    ///     Number of consecutive failures before triggering loop breaker.
    ///     Prevents infinite loops from repeated tool failures.
    /// </summary>
    public int ConsecutiveFailureThreshold { get; init; } = 3;

    /// <summary>
    ///     Time threshold for dedupe staleness (results older than this won't be reused).
    ///     Prevents reuse of potentially outdated tool results.
    /// </summary>
    public TimeSpan DedupeStalenessThreshold { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Maximum number of turns for the agent to run.
    ///     Prevents infinite loops and controls resource usage.
    /// </summary>
    public int MaxTurns { get; init; } = 100;

    /// <summary>
    ///     Timeout for LLM calls.
    ///     Prevents hanging on slow LLM responses.
    /// </summary>
    public TimeSpan LlmTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Timeout for tool calls.
    ///     Prevents hanging on slow tool executions.
    /// </summary>
    public TimeSpan ToolTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Whether to use function calling instead of Re/Act pattern.
    ///     Function calling is more efficient but requires LLM support.
    /// </summary>
    public bool UseFunctionCalling { get; init; } = true;

    /// <summary>
    ///     Whether to emit public status updates for UI consumption.
    ///     Enables real-time status updates without exposing internal reasoning.
    /// </summary>
    public bool EmitPublicStatus { get; init; } = true;

    /// <summary>
    ///     Maximum size in characters for tool output in history (to prevent prompt bloat).
    ///     Large tool outputs are truncated to maintain prompt efficiency.
    /// </summary>
    public int MaxToolOutputSize { get; init; } = 2000;
}