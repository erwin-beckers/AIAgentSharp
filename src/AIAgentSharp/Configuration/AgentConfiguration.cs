using AIAgentSharp.Agents.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace AIAgentSharp;

/// <summary>
/// Configuration options for AI agents, providing centralized control over agent behavior,
/// performance, and resource usage. This class uses the builder pattern with init-only
/// properties for immutable configuration.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AgentConfiguration"/> provides comprehensive configuration options for all
/// aspects of agent behavior. The configuration is immutable once created, ensuring
/// consistent behavior throughout the agent's lifetime.
/// </para>
/// <para>
/// Configuration options are organized into several categories:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <strong>Performance &amp; Limits</strong>: Control resource usage and prevent infinite loops
/// </description></item>
/// <item><description>
/// <strong>Token Management</strong>: Optimize prompt size and reduce LLM costs
/// </description></item>
/// <item><description>
/// <strong>Timeouts</strong>: Prevent hanging operations and ensure responsiveness
/// </description></item>
/// <item><description>
/// <strong>Features</strong>: Enable/disable specific agent capabilities
/// </description></item>
/// <item><description>
/// <strong>Loop Detection</strong>: Prevent infinite loops and improve reliability
/// </description></item>
/// </list>
/// <para>
/// Default values are optimized for most use cases, but you can customize them based on
/// your specific requirements and constraints.
/// </para>
/// </remarks>
/// <example>
/// <para>Basic configuration with defaults:</para>
/// <code>
/// var config = new AgentConfiguration();
/// var agent = new Agent(llmClient, stateStore, config: config);
/// </code>
/// <para>Custom configuration for production use:</para>
/// <code>
/// var config = new AgentConfiguration
/// {
///     MaxTurns = 50,
///     LlmTimeout = TimeSpan.FromMinutes(2),
///     ToolTimeout = TimeSpan.FromMinutes(1),
///     UseFunctionCalling = true,
///     EmitPublicStatus = true,
///     EnableHistorySummarization = true,
///     MaxRecentTurns = 15,
///     DedupeStalenessThreshold = TimeSpan.FromMinutes(10)
/// };
/// </code>
/// <para>Configuration for resource-constrained environments:</para>
/// <code>
/// var config = new AgentConfiguration
/// {
///     MaxTurns = 20,
///     MaxThoughtsLength = 10000,
///     MaxFinalLength = 25000,
///     MaxToolOutputSize = 1000,
///     EnableHistorySummarization = true,
///     MaxRecentTurns = 5
/// };
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
public sealed class AgentConfiguration
{
    /// <summary>
    /// Gets the maximum number of recent turns to keep in full detail in the prompt.
    /// </summary>
    /// <value>
    /// The number of recent turns to preserve in full detail. Default is 10.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting controls how many of the most recent agent turns are included in full
    /// detail in the LLM prompt. Older turns beyond this limit are summarized to reduce
    /// token usage while preserving important context.
    /// </para>
    /// <para>
    /// Higher values provide more detailed context but increase token usage and costs.
    /// Lower values reduce costs but may lose important context for complex tasks.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>5-10</strong>: For simple tasks with limited context requirements
    /// - <strong>10-15</strong>: For most general-purpose agents (default)
    /// - <strong>15-20</strong>: For complex tasks requiring detailed context
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxRecentTurns = 15 };
    /// </code>
    /// </example>
    public int MaxRecentTurns { get; init; } = 10;

    /// <summary>
    /// Gets the maximum number of characters allowed for the thoughts field in LLM responses.
    /// </summary>
    /// <value>
    /// The maximum character limit for thoughts. Default is 20,000 characters.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting prevents excessive verbosity in agent reasoning by limiting the size
    /// of the thoughts field in LLM responses. This helps control token usage and
    /// ensures responses remain focused and actionable.
    /// </para>
    /// <para>
    /// The thoughts field contains the agent's internal reasoning process. While detailed
    /// thoughts can be helpful for debugging, overly verbose thoughts increase costs
    /// without necessarily improving performance.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>10,000</strong>: For simple tasks with minimal reasoning
    /// - <strong>20,000</strong>: For most general-purpose agents (default)
    /// - <strong>30,000</strong>: For complex tasks requiring detailed reasoning
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxThoughtsLength = 15000 };
    /// </code>
    /// </example>
    public int MaxThoughtsLength { get; init; } = 20000;

    /// <summary>
    /// Gets the maximum number of characters allowed for the final output field.
    /// </summary>
    /// <value>
    /// The maximum character limit for final output. Default is 50,000 characters.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting limits the size of the agent's final response to the user. This
    /// prevents extremely long responses that may be difficult to process or display
    /// in user interfaces.
    /// </para>
    /// <para>
    /// The final output is the agent's completed response to the user's request. While
    /// comprehensive responses are valuable, extremely long outputs may exceed UI
    /// constraints or user expectations.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>25,000</strong>: For concise responses suitable for chat interfaces
    /// - <strong>50,000</strong>: For most general-purpose agents (default)
    /// - <strong>100,000</strong>: For detailed reports or comprehensive analyses
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxFinalLength = 30000 };
    /// </code>
    /// </example>
    public int MaxFinalLength { get; init; } = 50000;

    /// <summary>
    /// Gets the maximum number of characters allowed for the summary field.
    /// </summary>
    /// <value>
    /// The maximum character limit for summaries. Default is 40,000 characters.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting controls the size of planning summaries generated by the agent.
    /// Summaries are used to condense older conversation history and maintain context
    /// while reducing token usage.
    /// </para>
    /// <para>
    /// Summaries should be comprehensive enough to preserve important context but
    /// concise enough to be efficient. This balance depends on the complexity of
    /// your use case.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>20,000</strong>: For simple tasks with minimal context requirements
    /// - <strong>40,000</strong>: For most general-purpose agents (default)
    /// - <strong>60,000</strong>: For complex tasks requiring detailed context preservation
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxSummaryLength = 30000 };
    /// </code>
    /// </example>
    public int MaxSummaryLength { get; init; } = 40000;

    /// <summary>
    /// Gets a value indicating whether history summarization is enabled for older turns.
    /// </summary>
    /// <value>
    /// <c>true</c> if history summarization is enabled; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled, older agent turns beyond <see cref="MaxRecentTurns"/> are summarized
    /// to reduce prompt size and token usage. This is essential for long-running
    /// conversations or complex tasks that span many turns.
    /// </para>
    /// <para>
    /// Summarization preserves important context while significantly reducing token
    /// consumption. This is especially important for cost-sensitive applications
    /// or when working with LLMs that have strict token limits.
    /// </para>
    /// <para>
    /// Disable this feature only if you need to preserve every detail of the
    /// conversation history, and be aware that this will increase token usage
    /// and may hit LLM token limits for long conversations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Disable summarization for debugging purposes
    /// var config = new AgentConfiguration { EnableHistorySummarization = false };
    /// </code>
    /// </example>
    public bool EnableHistorySummarization { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of tool call history records to keep per agent.
    /// </summary>
    /// <value>
    /// The maximum number of tool call history records. Default is 20.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting controls how many tool call records are maintained for loop detection
    /// and deduplication purposes. Tool call history is used to identify patterns that
    /// might indicate infinite loops or redundant operations.
    /// </para>
    /// <para>
    /// Higher values provide better loop detection but consume more memory. Lower values
    /// reduce memory usage but may miss some loop patterns.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>10</strong>: For simple tasks with limited tool usage
    /// - <strong>20</strong>: For most general-purpose agents (default)
    /// - <strong>30-50</strong>: For complex tasks with extensive tool usage
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxToolCallHistory = 30 };
    /// </code>
    /// </example>
    public int MaxToolCallHistory { get; init; } = 20;

    /// <summary>
    /// Gets the number of consecutive failures before triggering the loop breaker.
    /// </summary>
    /// <value>
    /// The consecutive failure threshold. Default is 3.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting determines how many consecutive tool failures are allowed before
    /// the agent's loop detection mechanism intervenes. This prevents infinite loops
    /// caused by repeatedly failing tool calls.
    /// </para>
    /// <para>
    /// When the threshold is reached, the agent will attempt to break the loop by
    /// changing its approach or reporting the issue to the user.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>2-3</strong>: For most applications (default)
    /// - <strong>4-5</strong>: For applications with potentially flaky tools
    /// - <strong>1</strong>: For applications requiring immediate failure detection
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { ConsecutiveFailureThreshold = 5 };
    /// </code>
    /// </example>
    public int ConsecutiveFailureThreshold { get; init; } = 3;

    /// <summary>
    /// Gets the time threshold for deduplication staleness.
    /// </summary>
    /// <value>
    /// The time threshold for deduplication. Default is 5 minutes.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting determines how long tool results are considered fresh for
    /// deduplication purposes. Results older than this threshold will not be
    /// reused, even if the same tool is called with identical parameters.
    /// </para>
    /// <para>
    /// This prevents the reuse of potentially outdated information while still
    /// providing the performance benefits of deduplication for recent results.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>1-2 minutes</strong>: For rapidly changing data (weather, stock prices)
    /// - <strong>5 minutes</strong>: For most general-purpose applications (default)
    /// - <strong>10-30 minutes</strong>: For relatively static data (geographic info)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration 
    /// { 
    ///     DedupeStalenessThreshold = TimeSpan.FromMinutes(2) 
    /// };
    /// </code>
    /// </example>
    public TimeSpan DedupeStalenessThreshold { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the maximum number of turns for the agent to run.
    /// </summary>
    /// <value>
    /// The maximum number of turns. Default is 100.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting prevents infinite loops and controls resource usage by limiting
    /// the total number of turns an agent can execute. Once this limit is reached,
    /// the agent will stop and return its current state.
    /// </para>
    /// <para>
    /// This is a critical safety mechanism that prevents runaway agents from
    /// consuming excessive resources or getting stuck in infinite loops.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>20-50</strong>: For simple, well-defined tasks
    /// - <strong>50-100</strong>: For most general-purpose agents (default)
    /// - <strong>100-200</strong>: For complex, multi-step tasks
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxTurns = 50 };
    /// </code>
    /// </example>
    public int MaxTurns { get; init; } = 100;

    /// <summary>
    /// Gets the timeout for LLM calls.
    /// </summary>
    /// <value>
    /// The LLM call timeout. Default is 5 minutes.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting prevents the agent from hanging indefinitely on slow LLM responses.
    /// If an LLM call exceeds this timeout, it will be cancelled and the agent will
    /// handle the failure appropriately.
    /// </para>
    /// <para>
    /// This timeout should be set based on your LLM provider's typical response times
    /// and your application's responsiveness requirements.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>1-2 minutes</strong>: For fast LLMs or real-time applications
    /// - <strong>3-5 minutes</strong>: For most general-purpose applications (default)
    /// - <strong>5-10 minutes</strong>: For complex reasoning tasks or slower LLMs
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration 
    /// { 
    ///     LlmTimeout = TimeSpan.FromMinutes(2) 
    /// };
    /// </code>
    /// </example>
    public TimeSpan LlmTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the timeout for tool calls.
    /// </summary>
    /// <value>
    /// The tool call timeout. Default is 2 minutes.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting prevents the agent from hanging indefinitely on slow tool executions.
    /// If a tool call exceeds this timeout, it will be cancelled and the agent will
    /// handle the failure appropriately.
    /// </para>
    /// <para>
    /// This timeout should be set based on your tools' typical execution times and
    /// your application's responsiveness requirements.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>30 seconds - 1 minute</strong>: For fast, local tools
    /// - <strong>1-2 minutes</strong>: For most general-purpose tools (default)
    /// - <strong>2-5 minutes</strong>: For slow external APIs or complex computations
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration 
    /// { 
    ///     ToolTimeout = TimeSpan.FromMinutes(1) 
    /// };
    /// </code>
    /// </example>
    public TimeSpan ToolTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets a value indicating whether to use function calling instead of the Re/Act pattern.
    /// </summary>
    /// <value>
    /// <c>true</c> if function calling should be used; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled, the agent will use OpenAI-style function calling when the LLM
    /// supports it. Function calling is more efficient and reliable than the Re/Act
    /// pattern but requires LLM support.
    /// </para>
    /// <para>
    /// Function calling provides:
    /// - More reliable tool selection and parameter extraction
    /// - Better performance and reduced token usage
    /// - Cleaner separation between reasoning and tool calls
    /// </para>
    /// <para>
    /// If the LLM doesn't support function calling, the agent will automatically
    /// fall back to the Re/Act pattern regardless of this setting.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Force Re/Act pattern for compatibility
    /// var config = new AgentConfiguration { UseFunctionCalling = false };
    /// </code>
    /// </example>
    public bool UseFunctionCalling { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to emit public status updates for UI consumption.
    /// </summary>
    /// <value>
    /// <c>true</c> if public status updates should be emitted; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled, the agent will emit real-time status updates that can be consumed
    /// by user interfaces to show progress and current activity. These updates are
    /// designed to be user-friendly and don't expose internal reasoning details.
    /// </para>
    /// <para>
    /// Status updates include:
    /// - Current activity description
    /// - Progress percentage
    /// - Next step hints
    /// - Error messages (when appropriate)
    /// </para>
    /// <para>
    /// This feature is essential for providing good user experience in interactive
    /// applications where users need to understand what the agent is doing.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Subscribe to status updates
    /// agent.StatusUpdate += (sender, e) => 
    /// {
    ///     Console.WriteLine($"Status: {e.StatusTitle} - Progress: {e.ProgressPct}%");
    /// };
    /// </code>
    /// </example>
    public bool EmitPublicStatus { get; init; } = true;

    /// <summary>
    /// Gets the maximum size in characters for tool output in history.
    /// </summary>
    /// <value>
    /// The maximum tool output size. Default is 2,000 characters.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting prevents prompt bloat by limiting the size of tool outputs that
    /// are included in the conversation history. Large tool outputs are truncated
    /// to maintain prompt efficiency and reduce token usage.
    /// </para>
    /// <para>
    /// Tool outputs are essential for agent reasoning but can become very large,
    /// especially for tools that return extensive data sets or detailed reports.
    /// Truncation preserves the most important information while keeping prompts
    /// manageable.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - <strong>1,000</strong>: For simple tools with minimal output
    /// - <strong>2,000</strong>: For most general-purpose tools (default)
    /// - <strong>5,000</strong>: For tools that return detailed data
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxToolOutputSize = 3000 };
    /// </code>
    /// </example>
    public int MaxToolOutputSize { get; init; } = 2000;

    /// <summary>
    /// Gets the type of reasoning to use for agent decision making.
    /// </summary>
    /// <value>
    /// The reasoning type. Default is <see cref="ReasoningType.None"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting determines the reasoning approach used by the agent for making
    /// decisions and solving problems. Different reasoning types offer different
    /// trade-offs between performance, accuracy, and computational complexity.
    /// </para>
    /// <para>
    /// Available reasoning types:
    /// - <strong>None</strong>: No reasoning (disabled)
    /// - <strong>ChainOfThought</strong>: Linear step-by-step reasoning
    /// - <strong>TreeOfThoughts</strong>: Branching exploration of multiple solution paths
    /// - <strong>Hybrid</strong>: Combination of multiple reasoning approaches
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { ReasoningType = ReasoningType.TreeOfThoughts };
    /// </code>
    /// </example>
    public ReasoningType ReasoningType { get; init; } = ReasoningType.None;

    /// <summary>
    /// Gets the maximum number of reasoning steps allowed in a Chain of Thought.
    /// </summary>
    /// <value>
    /// The maximum number of reasoning steps. Default is 10.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting limits the number of reasoning steps in a Chain of Thought
    /// to prevent excessive computation and token usage.
    /// </para>
    /// <para>
    /// Higher values allow for more detailed reasoning but increase costs and
    /// execution time. Lower values are more efficient but may limit reasoning depth.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxReasoningSteps = 15 };
    /// </code>
    /// </example>
    public int MaxReasoningSteps { get; init; } = 10;

    /// <summary>
    /// Gets the maximum depth allowed in a Tree of Thoughts.
    /// </summary>
    /// <value>
    /// The maximum tree depth. Default is 5.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting limits the depth of exploration in Tree of Thoughts reasoning
    /// to prevent exponential growth in computation.
    /// </para>
    /// <para>
    /// Higher values allow for deeper exploration but increase computational complexity.
    /// Lower values are more efficient but may miss optimal solutions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxTreeDepth = 8 };
    /// </code>
    /// </example>
    public int MaxTreeDepth { get; init; } = 5;

    /// <summary>
    /// Gets the maximum number of nodes allowed in a Tree of Thoughts.
    /// </summary>
    /// <value>
    /// The maximum number of tree nodes. Default is 50.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting limits the total number of nodes in a Tree of Thoughts
    /// to prevent excessive memory usage and computation.
    /// </para>
    /// <para>
    /// Higher values allow for more extensive exploration but increase resource usage.
    /// Lower values are more efficient but may limit solution space exploration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MaxTreeNodes = 100 };
    /// </code>
    /// </example>
    public int MaxTreeNodes { get; init; } = 50;

    /// <summary>
    /// Gets the exploration strategy for Tree of Thoughts reasoning.
    /// </summary>
    /// <value>
    /// The exploration strategy. Default is <see cref="ExplorationStrategy.BestFirst"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting determines how the Tree of Thoughts explores different solution paths.
    /// Different strategies offer different trade-offs between exploration and exploitation.
    /// </para>
    /// <para>
    /// Available strategies:
    /// - <strong>BestFirst</strong>: Explore the most promising paths first (default)
    /// - <strong>BreadthFirst</strong>: Explore all paths at the same depth before going deeper
    /// - <strong>DepthFirst</strong>: Explore one path to maximum depth before backtracking
    /// - <strong>BeamSearch</strong>: Maintain a limited set of most promising paths
    /// - <strong>MonteCarlo</strong>: Use random sampling for exploration
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { TreeExplorationStrategy = ExplorationStrategy.BeamSearch };
    /// </code>
    /// </example>
    public ExplorationStrategy TreeExplorationStrategy { get; init; } = ExplorationStrategy.BestFirst;

    /// <summary>
    /// Gets a value indicating whether to enable reasoning validation.
    /// </summary>
    /// <value>
    /// <c>true</c> if reasoning validation is enabled; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled, the agent will validate the quality and completeness of its
    /// reasoning process to ensure reliable decision making.
    /// </para>
    /// <para>
    /// Validation includes checking for logical consistency, completeness of analysis,
    /// and confidence levels in conclusions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { EnableReasoningValidation = false };
    /// </code>
    /// </example>
    public bool EnableReasoningValidation { get; init; } = true;

    /// <summary>
    /// Gets the minimum confidence threshold for accepting reasoning conclusions.
    /// </summary>
    /// <value>
    /// The minimum confidence threshold (0.0 to 1.0). Default is 0.7.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting determines the minimum confidence level required for the agent
    /// to accept and act on its reasoning conclusions.
    /// </para>
    /// <para>
    /// Higher values ensure more reliable decisions but may cause the agent to
    /// be overly cautious. Lower values allow more aggressive decision making
    /// but may lead to less reliable outcomes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration { MinReasoningConfidence = 0.8 };
    /// </code>
    /// </example>
    public double MinReasoningConfidence { get; init; } = 0.7;

    /// <summary>
    /// Gets additional messages to be included in the conversation context.
    /// </summary>
    /// <value>
    /// A list of additional messages to include alongside the existing system prompt and goal.
    /// Default is an empty list.
    /// </value>
    /// <remarks>
    /// <para>
    /// These messages will be included in the conversation context alongside the existing
    /// AIAgentSharp system prompt and the user's goal. This allows you to add custom
    /// system prompts, user messages, or assistant messages without replacing the
    /// core AIAgentSharp functionality.
    /// </para>
    /// <para>
    /// The messages are added in the order they appear in this list, after the
    /// AIAgentSharp system prompt but before the goal and conversation history.
    /// </para>
    /// <para>
    /// This is useful for:
    /// - Adding domain-specific instructions or context
    /// - Including custom system prompts for specific use cases
    /// - Providing additional context or examples
    /// - Setting tone or style preferences
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration 
    /// { 
    ///     AdditionalMessages = new List&lt;LlmMessage&gt;
    ///     {
    ///         new LlmMessage { Role = "system", Content = "You are a helpful travel assistant." },
    ///         new LlmMessage { Role = "user", Content = "Please provide detailed recommendations." }
    ///     }
    /// };
    /// </code>
    /// </example>
    public List<LlmMessage> AdditionalMessages { get; init; } = new();
}