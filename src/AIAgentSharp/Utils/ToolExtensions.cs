namespace AIAgentSharp;

/// <summary>
///     Provides extension methods for working with tool collections and registries.
/// </summary>
public static class ToolExtensions
{
    /// <summary>
    ///     Gets a tool from a registry by name, throwing an exception if not found.
    /// </summary>
    /// <param name="registry">The tool registry to search.</param>
    /// <param name="name">The name of the tool to find.</param>
    /// <returns>The found tool.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the tool is not found in the registry.</exception>
    public static ITool RequireTool(this IDictionary<string, ITool> registry, string name)
    {
        if (registry.TryGetValue(name, out var tool))
        {
            return tool;
        }
        throw new KeyNotFoundException($"Tool '{name}' not found. Available: {string.Join(", ", registry.Keys)}");
    }

    /// <summary>
    ///     Converts a collection of tools into a dictionary registry for efficient lookup.
    /// </summary>
    /// <param name="tools">The collection of tools to convert.</param>
    /// <returns>A dictionary mapping tool names to tool instances.</returns>
    public static IDictionary<string, ITool> ToRegistry(this IEnumerable<ITool> tools)
    {
        return tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }
}