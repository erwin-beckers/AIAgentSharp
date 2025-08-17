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