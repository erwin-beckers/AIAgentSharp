namespace AIAgentSharp;

/// <summary>
/// Defines the interface for agent state persistence, allowing agents to save and restore
/// their execution state across sessions or application restarts.
/// </summary>
/// <remarks>
/// <para>
/// Agent state stores are responsible for persisting the complete state of agent execution,
/// including the conversation history, tool execution results, and current progress. This
/// enables agents to resume their work after interruptions and maintain context across
/// multiple execution sessions.
/// </para>
/// <para>
/// The framework provides several built-in implementations:
/// - <see cref="MemoryAgentStateStore"/>: In-memory storage for testing and development
/// - <see cref="FileAgentStateStore"/>: File-based storage for simple persistence
/// </para>
/// <para>
/// For production applications, consider implementing custom state stores that integrate
/// with your existing data storage solutions (databases, cloud storage, etc.).
/// </para>
/// <para>
/// State stores should be designed to be:
/// - <strong>Thread-safe</strong>: Multiple agents may access the store concurrently
/// - <strong>Reliable</strong>: State should be persisted atomically and consistently
/// - <strong>Efficient</strong>: Large state objects should be handled efficiently
/// - <strong>Secure</strong>: Sensitive data should be protected appropriately
/// </para>
/// </remarks>
/// <example>
/// <para>Basic file-based state store implementation:</para>
/// <code>
/// public class FileAgentStateStore : IAgentStateStore
/// {
///     private readonly string _directory;
///     
///     public FileAgentStateStore(string directory)
///     {
///         _directory = directory;
///         Directory.CreateDirectory(_directory);
///     }
///     
///     public async Task&lt;AgentState?&gt; LoadAsync(string agentId, CancellationToken ct = default)
///     {
///         var filePath = Path.Combine(_directory, $"{agentId}.json");
///         if (!File.Exists(filePath)) return null;
///         
///         var json = await File.ReadAllTextAsync(filePath, ct);
///         return JsonSerializer.Deserialize&lt;AgentState&gt;(json);
///     }
///     
///     public async Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default)
///     {
///         var filePath = Path.Combine(_directory, $"{agentId}.json");
///         var json = JsonSerializer.Serialize(state);
///         await File.WriteAllTextAsync(filePath, json, ct);
///     }
/// }
/// </code>
/// </example>
public interface IAgentStateStore
{
    /// <summary>
    /// Loads the agent state for the specified agent ID.
    /// </summary>
    /// <param name="agentId">
    /// The unique identifier of the agent whose state should be loaded.
    /// This ID should be consistent across sessions for the same agent instance.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the loading operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous loading operation. The result is:
    /// - The agent state if it exists and can be loaded successfully
    /// - <c>null</c> if no state exists for the specified agent ID
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="agentId"/> is null or empty.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stored state is corrupted or cannot be deserialized.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is called by the agent framework when an agent starts execution
    /// to restore its previous state. If no state exists, the agent will start fresh.
    /// </para>
    /// <para>
    /// The returned state should contain:
    /// - The original goal and agent ID
    /// - Complete conversation history (turns)
    /// - Tool execution results and context
    /// - Any other relevant state information
    /// </para>
    /// <para>
    /// Implementations should handle:
    /// - Efficient loading of large state objects
    /// - Proper error handling for corrupted or invalid state
    /// - Graceful handling of missing state (return null)
    /// - Thread-safe access to shared storage
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Example usage in an agent:</para>
    /// <code>
    /// var state = await stateStore.LoadAsync("travel-agent-123", ct);
    /// if (state != null)
    /// {
    ///     Console.WriteLine($"Resuming agent with {state.Turns.Count} previous turns");
    ///     // Continue from where the agent left off
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Starting new agent session");
    ///     // Create new state for the agent
    /// }
    /// </code>
    /// </example>
    Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default);

    /// <summary>
    /// Saves the agent state for the specified agent ID.
    /// </summary>
    /// <param name="agentId">
    /// The unique identifier of the agent whose state should be saved.
    /// This ID should be consistent across sessions for the same agent instance.
    /// </param>
    /// <param name="state">
    /// The complete agent state to be persisted. This includes all conversation
    /// history, tool results, and current execution context.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the saving operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous saving operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="agentId"/> is null or empty, or when
    /// <paramref name="state"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the state cannot be serialized or saved due to storage issues.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is called by the agent framework after each execution step
    /// to persist the current state. This ensures that agent progress is not lost
    /// if the application is interrupted or restarted.
    /// </para>
    /// <para>
    /// The state object contains:
    /// - Complete conversation history with all turns
    /// - Tool execution results and parameters
    /// - LLM messages and responses
    /// - Metadata like creation and modification timestamps
    /// </para>
    /// <para>
    /// Implementations should ensure:
    /// - Atomic writes to prevent corruption
    /// - Efficient handling of large state objects
    /// - Proper error handling and recovery
    /// - Thread-safe access to shared storage
    /// - Backup and versioning if appropriate
    /// </para>
    /// <para>
    /// For large state objects, consider implementing compression or incremental
    /// updates to improve performance and reduce storage requirements.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Example usage in an agent step:</para>
    /// <code>
    /// // After executing a step
    /// state.Turns.Add(newTurn);
    /// state.LastModifiedUtc = DateTimeOffset.UtcNow;
    /// 
    /// await stateStore.SaveAsync("travel-agent-123", state, ct);
    /// Console.WriteLine("Agent state saved successfully");
    /// </code>
    /// </example>
    Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default);
}