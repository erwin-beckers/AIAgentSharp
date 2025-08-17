namespace AIAgentSharp;

/// <summary>
///     Optional interface for tools to control deduplication behavior.
///     If not implemented, tools use the global dedupe settings.
/// </summary>
public interface IDedupeControl
{
    /// <summary>
    ///     Whether this tool should participate in deduplication at all.
    ///     Default: true (participate in deduplication)
    /// </summary>
    bool AllowDedupe { get; }

    /// <summary>
    ///     Custom TTL for this tool's results. If null, uses the global TTL.
    ///     Default: null (use global TTL)
    /// </summary>
    TimeSpan? CustomTtl { get; }
}