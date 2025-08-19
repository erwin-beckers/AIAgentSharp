using AIAgentSharp.Agents.Interfaces;

namespace AIAgentSharp.Metrics;

/// <summary>
/// Defines the interface for collecting comprehensive metrics from AIAgentSharp operations.
/// This interface provides methods to record various types of metrics including performance,
/// operational, quality, and resource metrics.
/// </summary>
/// <remarks>
/// <para>
/// The metrics collector is designed to be lightweight and non-blocking, ensuring that
/// metric collection doesn't impact the performance of agent operations. All methods
/// should be thread-safe and handle exceptions gracefully.
/// </para>
/// <para>
/// Metrics are organized into several categories:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Performance Metrics</strong>: Execution times, throughput, and efficiency indicators</description></item>
/// <item><description><strong>Operational Metrics</strong>: Success rates, error rates, and system health indicators</description></item>
/// <item><description><strong>Quality Metrics</strong>: Reasoning confidence, response quality, and user satisfaction</description></item>
/// <item><description><strong>Resource Metrics</strong>: Token usage, API calls, and resource consumption</description></item>
/// </list>
/// </remarks>
public interface IMetricsCollector
{
    // Performance Metrics
    /// <summary>
    /// Records the execution time of an agent run.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <param name="totalTurns">The total number of turns executed.</param>
    void RecordAgentRunExecutionTime(string agentId, long executionTimeMs, int totalTurns);

    /// <summary>
    /// Records the execution time of an individual agent step.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="turnIndex">The zero-based index of the turn.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    void RecordAgentStepExecutionTime(string agentId, int turnIndex, long executionTimeMs);

    /// <summary>
    /// Records the execution time of an LLM call.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="turnIndex">The zero-based index of the turn.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <param name="modelName">The name of the LLM model used.</param>
    void RecordLlmCallExecutionTime(string agentId, int turnIndex, long executionTimeMs, string modelName);

    /// <summary>
    /// Records the execution time of a tool call.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="turnIndex">The zero-based index of the turn.</param>
    /// <param name="toolName">The name of the tool executed.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    void RecordToolCallExecutionTime(string agentId, int turnIndex, string toolName, long executionTimeMs);

    /// <summary>
    /// Records the execution time of reasoning operations.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="reasoningType">The type of reasoning performed.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    void RecordReasoningExecutionTime(string agentId, ReasoningType reasoningType, long executionTimeMs);

    // Operational Metrics
    /// <summary>
    /// Records the completion of an agent run with success/failure status.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="succeeded">Whether the agent run was successful.</param>
    /// <param name="totalTurns">The total number of turns executed.</param>
    /// <param name="errorType">The type of error if the run failed.</param>
    void RecordAgentRunCompletion(string agentId, bool succeeded, int totalTurns, string? errorType = null);

    /// <summary>
    /// Records the completion of an agent step with success/failure status.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="turnIndex">The zero-based index of the turn.</param>
    /// <param name="succeeded">Whether the step was successful.</param>
    /// <param name="executedTool">Whether a tool was executed in this step.</param>
    /// <param name="errorType">The type of error if the step failed.</param>
    void RecordAgentStepCompletion(string agentId, int turnIndex, bool succeeded, bool executedTool, string? errorType = null);

    /// <summary>
    /// Records the completion of an LLM call with success/failure status.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="turnIndex">The zero-based index of the turn.</param>
    /// <param name="succeeded">Whether the LLM call was successful.</param>
    /// <param name="modelName">The name of the LLM model used.</param>
    /// <param name="errorType">The type of error if the call failed.</param>
    void RecordLlmCallCompletion(string agentId, int turnIndex, bool succeeded, string modelName, string? errorType = null);

    /// <summary>
    /// Records the completion of a tool call with success/failure status.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="turnIndex">The zero-based index of the turn.</param>
    /// <param name="toolName">The name of the tool executed.</param>
    /// <param name="succeeded">Whether the tool call was successful.</param>
    /// <param name="errorType">The type of error if the call failed.</param>
    void RecordToolCallCompletion(string agentId, int turnIndex, string toolName, bool succeeded, string? errorType = null);

    /// <summary>
    /// Records a loop detection event.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="loopType">The type of loop detected.</param>
    /// <param name="consecutiveFailures">The number of consecutive failures.</param>
    void RecordLoopDetection(string agentId, string loopType, int consecutiveFailures);

    /// <summary>
    /// Records a deduplication event.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="cacheHit">Whether the result was retrieved from cache.</param>
    void RecordDeduplicationEvent(string agentId, string toolName, bool cacheHit);

    // Quality Metrics
    /// <summary>
    /// Records reasoning confidence scores.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="reasoningType">The type of reasoning performed.</param>
    /// <param name="confidenceScore">The confidence score (0.0 to 1.0).</param>
    void RecordReasoningConfidence(string agentId, ReasoningType reasoningType, double confidenceScore);

    /// <summary>
    /// Records the quality of agent responses.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="responseLength">The length of the response in characters.</param>
    /// <param name="hasFinalOutput">Whether the response includes a final output.</param>
    void RecordResponseQuality(string agentId, int responseLength, bool hasFinalOutput);

    /// <summary>
    /// Records validation results.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="validationType">The type of validation performed.</param>
    /// <param name="passed">Whether the validation passed.</param>
    /// <param name="errorMessage">The error message if validation failed.</param>
    void RecordValidation(string agentId, string validationType, bool passed, string? errorMessage = null);

    // Resource Metrics
    /// <summary>
    /// Records token usage for LLM calls.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="turnIndex">The zero-based index of the turn.</param>
    /// <param name="inputTokens">The number of input tokens.</param>
    /// <param name="outputTokens">The number of output tokens.</param>
    /// <param name="modelName">The name of the LLM model used.</param>
    void RecordTokenUsage(string agentId, int turnIndex, int inputTokens, int outputTokens, string modelName);

    /// <summary>
    /// Records API call counts.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="apiType">The type of API call (LLM, Tool, etc.).</param>
    /// <param name="modelName">The name of the model or service used.</param>
    void RecordApiCall(string agentId, string apiType, string modelName);

    /// <summary>
    /// Records state store operations.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="operationType">The type of operation (Read, Write, Delete).</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    void RecordStateStoreOperation(string agentId, string operationType, long executionTimeMs);

    // Custom Metrics
    /// <summary>
    /// Records a custom metric with a numeric value.
    /// </summary>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="tags">Optional tags for categorizing the metric.</param>
    void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Records a custom event.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="tags">Optional tags for categorizing the event.</param>
    void RecordCustomEvent(string eventName, Dictionary<string, string>? tags = null);
}
