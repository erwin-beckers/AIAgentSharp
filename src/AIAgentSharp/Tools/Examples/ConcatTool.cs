using System.Collections;
using System.Text.Json;

namespace AIAgentSharp;

/// <summary>
///     A tool for concatenating strings with a separator.
///     This tool demonstrates manual implementation of the tool interfaces without using BaseTool.
/// </summary>
public sealed class ConcatTool : ITool, IToolIntrospect, IFunctionSchemaProvider
{
    /// <summary>
    ///     Gets the description of the tool.
    /// </summary>
    public string Description => "Concatenate strings with a separator.";

    /// <summary>
    ///     Gets the JSON schema for the tool's parameters.
    /// </summary>
    /// <returns>A JSON schema object for the tool's parameters.</returns>
    public object GetJsonSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                items = new
                {
                    type = "array",
                    description = "Array of strings to concatenate",
                    items = new { type = "string" },
                    example = new[] { "hello", "world" }
                },
                sep = new
                {
                    type = "string",
                    description = "Separator string",
                    example = ", "
                }
            },
            required = new[] { "items" },
            additionalProperties = false
        };
    }

    /// <summary>
    ///     Gets the unique name of the tool.
    /// </summary>
    public string Name => "concat";

    /// <summary>
    ///     Invokes the tool to concatenate strings with a separator.
    /// </summary>
    /// <param name="parameters">The parameters containing items to concatenate and optional separator.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The concatenated result as an object.</returns>
    public Task<object?> InvokeAsync(Dictionary<string, object?> parameters, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Extract items parameter
        if (!parameters.TryGetValue("items", out var itemsObj))
        {
            throw new ArgumentException("Missing required parameter: items");
        }

        // Extract separator parameter (optional)
        parameters.TryGetValue("sep", out var sepObj);
        var separator = sepObj is string str ? str : ", ";

        // Convert items to string array, handling various input types
        var items = new List<string>();

        if (itemsObj == null)
        {
            // Handle null items
            items.Add("");
        }
        else if (itemsObj is string[] stringArray)
        {
            items.AddRange(stringArray);
        }
        else if (itemsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in jsonElement.EnumerateArray())
            {
                items.Add(element.ToString());
            }
        }
        else if (itemsObj is IEnumerable<object> objectEnumerable)
        {
            foreach (var item in objectEnumerable)
            {
                items.Add(item?.ToString() ?? "");
            }
        }
        else if (itemsObj is string singleString)
        {
            items.Add(singleString);
        }
        else if (itemsObj is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                items.Add(item?.ToString() ?? "");
            }
        }
        else if (itemsObj is int intValue)
        {
            // Handle single integer
            items.Add(intValue.ToString());
        }
        else
        {
            // Try to convert to string as fallback
            items.Add(itemsObj.ToString() ?? "");
        }

        var result = string.Join(separator, items);
        return Task.FromResult<object?>(new { result });
    }

    /// <summary>
    ///     Gets a concise description of the tool and its parameters.
    /// </summary>
    /// <returns>A JSON string describing the tool and its parameters.</returns>
    public string Describe()
    {
        return JsonSerializer.Serialize(new
        {
            name = "concat",
            description = "Concatenate strings with a separator.",
            @params = new
            {
                type = "object",
                properties = new
                {
                    items = new
                    {
                        type = "array",
                        description = "Array of strings to concatenate",
                        items = new { type = "string" },
                        example = new[] { "hello", "world" }
                    },
                    sep = new
                    {
                        type = "string",
                        description = "Separator string",
                        example = ", "
                    }
                },
                required = new[] { "items" },
                additionalProperties = false
            }
        }, JsonUtil.JsonOptions);
    }
}