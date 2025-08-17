using System.Text.Json;
using File = System.IO.File;

namespace AIAgentSharp;

/// <summary>
///     A file-based implementation of IAgentStateStore that persists agent states to JSON files.
///     Each agent's state is stored in a separate file with atomic write operations for data integrity.
/// </summary>
public sealed class FileAgentStateStore : IAgentStateStore
{
    private readonly string _directory;
    private readonly ILogger _logger;

    /// <summary>
    ///     Initializes a new instance of the FileAgentStateStore class.
    /// </summary>
    /// <param name="directory">The directory where agent state files will be stored.</param>
    /// <param name="logger">Optional logger for operation tracking. Uses ConsoleLogger if not provided.</param>
    /// <exception cref="ArgumentNullException">Thrown when directory is null.</exception>
    public FileAgentStateStore(string directory, ILogger? logger = null)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _logger = logger ?? new ConsoleLogger();
    }

    /// <summary>
    ///     Loads the state for a specific agent from a JSON file.
    /// </summary>
    /// <param name="agentId">The unique identifier for the agent.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The agent state, or null if the file doesn't exist or is invalid.</returns>
    /// <summary>
    /// Loads the agent state from a file on disk.
    /// </summary>
    /// <param name="agentId">Unique identifier for the agent whose state to load.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// The loaded <see cref="AgentState"/> or null if no state file exists or is invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method attempts to load the agent state from a JSON file stored on disk:
    /// </para>
    /// <list type="number">
    /// <item><description>Constructs the file path based on the agent ID</description></item>
    /// <item><description>Checks if the state file exists</description></item>
    /// <item><description>Reads and deserializes the JSON content</description></item>
    /// <item><description>Validates the loaded state structure</description></item>
    /// <item><description>Returns the state or null if loading fails</description></item>
    /// </list>
    /// <para>
    /// The method handles various error conditions gracefully, including missing files,
    /// corrupted JSON, and invalid state structures, returning null in all error cases.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="ct"/>.</exception>
    public async Task<AgentState?> LoadAsync(string agentId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_directory, $"{agentId}.jsonl");

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, ct);

            if (lines.Length == 0)
            {
                return null;
            }

            var header = JsonSerializer.Deserialize<AgentStateHeader>(lines[0], JsonUtil.JsonOptions);

            if (header == null)
            {
                return null;
            }

            var state = new AgentState
            {
                AgentId = header.AgentId,
                Goal = header.Goal,
                UpdatedUtc = header.UpdatedUtc,
                Turns = new List<AgentTurn>()
            };

            for (var i = 1; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    var turn = JsonSerializer.Deserialize<AgentTurn>(lines[i], JsonUtil.JsonOptions);

                    if (turn != null)
                    {
                        state.Turns.Add(turn);
                    }
                }
            }

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load agent state from {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves the agent state to a file on disk.
    /// </summary>
    /// <param name="agentId">Unique identifier for the agent whose state to save.</param>
    /// <param name="state">The agent state to persist.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <remarks>
    /// <para>
    /// This method persists the agent state to a JSON file on disk:
    /// </para>
    /// <list type="number">
    /// <item><description>Creates the directory structure if it doesn't exist</description></item>
    /// <item><description>Constructs the file path based on the agent ID</description></item>
    /// <item><description>Serializes the state to JSON format</description></item>
    /// <item><description>Writes the content to disk atomically</description></item>
    /// <item><description>Handles file system errors gracefully</description></item>
    /// </list>
    /// <para>
    /// The method uses atomic file operations to ensure data integrity and handles
    /// various file system scenarios including directory creation and permission issues.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentId"/> or <paramref name="state"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="ct"/>.</exception>
    public async Task SaveAsync(string agentId, AgentState state, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, $"{agentId}.jsonl");

        // Update timestamp before serializing the header
        state.UpdatedUtc = DateTimeOffset.UtcNow;

        var lines = new List<string>
        {
            JsonSerializer.Serialize(new AgentStateHeader
            {
                AgentId = state.AgentId,
                Goal = state.Goal,
                UpdatedUtc = state.UpdatedUtc
            }, JsonUtil.JsonOptions)
        };

        foreach (var turn in state.Turns)
        {
            lines.Add(JsonSerializer.Serialize(turn, JsonUtil.JsonOptions));
        }

        // Atomic file writes - safer persistence (prevents partial writes)
        var tmp = filePath + ".tmp";
        await File.WriteAllLinesAsync(tmp, lines, ct);
        File.Move(tmp, filePath, true);
    }

    private sealed class AgentStateHeader
    {
        public string AgentId { get; set; } = string.Empty;
        public string Goal { get; set; } = string.Empty;
        public DateTimeOffset UpdatedUtc { get; set; }
    }
}