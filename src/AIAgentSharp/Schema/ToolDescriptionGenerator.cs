using System.Reflection;
using System.Text.Json;

namespace AIAgentSharp;

/// <summary>
///     Generates tool descriptions in JSON format for use in agent prompts and documentation.
///     This class creates standardized tool descriptions that include the tool name, description,
///     and parameter schema in a format suitable for LLM consumption.
/// </summary>
public static class ToolDescriptionGenerator
{
    /// <summary>
    ///     Builds a JSON description of a tool with its parameters.
    /// </summary>
    /// <typeparam name="TParams">The type of the tool's parameters.</typeparam>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="toolDescription">Optional description of the tool. If not provided, will use the ToolParamsAttribute description or generate a default.</param>
    /// <returns>A JSON string containing the tool description with name, description, and parameter schema.</returns>
    public static string Build<TParams>(string toolName, string? toolDescription = null)
    {
        var paramsType = typeof(TParams);
        var paramsAttr = paramsType.GetCustomAttribute<ToolParamsAttribute>();

        var description = toolDescription ?? paramsAttr?.Description ?? $"Parameters for {toolName}";
        var schema = SchemaGenerator.Generate<TParams>();

        var result = new
        {
            name = toolName,
            description,
            @params = schema
        };

        return JsonSerializer.Serialize(result, JsonUtil.JsonOptions);
    }
}