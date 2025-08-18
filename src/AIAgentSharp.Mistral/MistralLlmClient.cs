using System.Text;
using System.Text.Json;

namespace AIAgentSharp.Mistral;

/// <summary>
/// Mistral AI LLM client implementation using direct HTTP API calls.
/// Note: This implementation uses HttpClient directly since the official Mistral AI .NET SDK is not yet available on NuGet.
/// </summary>
public class MistralLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly MistralConfiguration _configuration;

    public MistralLlmClient(MistralConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_configuration.ApiBaseUrl ?? "https://api.mistral.ai/v1/"),
            Timeout = _configuration.RequestTimeout
        };

        if (!string.IsNullOrEmpty(_configuration.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration.ApiKey);
        }

        if (!string.IsNullOrEmpty(_configuration.OrganizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("Mistral-Organization", _configuration.OrganizationId);
        }
    }

    public MistralLlmClient(string apiKey, MistralConfiguration? configuration = null)
        : this(configuration ?? MistralConfiguration.Create(apiKey))
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey));
        }
    }

    public async Task<LlmCompletionResult> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _configuration.Model,
            messages = ConvertToMistralMessages(messages),
            max_tokens = _configuration.MaxTokens,
            temperature = _configuration.Temperature,
            top_p = _configuration.TopP,
            stream = _configuration.EnableStreaming
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var mistralResponse = JsonSerializer.Deserialize<MistralChatCompletionResponse>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        if (mistralResponse?.Choices?.FirstOrDefault()?.Message == null)
        {
            throw new InvalidOperationException("Invalid response from Mistral AI API");
        }

        return new LlmCompletionResult
        {
            Content = ExtractJsonFromMarkdown(mistralResponse.Choices[0].Message.Content),
            Usage = mistralResponse.Usage != null ? new LlmUsage
            {
                InputTokens = mistralResponse.Usage.PromptTokens,
                OutputTokens = mistralResponse.Usage.CompletionTokens,
                Model = _configuration.Model,
                Provider = "Mistral"
            } : null
        };
    }

    public async Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages,
        IEnumerable<OpenAiFunctionSpec> functions,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.EnableFunctionCalling)
        {
            throw new InvalidOperationException("Function calling is not enabled in the configuration");
        }

        // For Mistral, we need to include function definitions in the system message
        // since Mistral doesn't support the tools parameter in the same way as OpenAI
        var messagesList = messages.ToList();
        var systemMessage = CreateSystemMessageWithFunctions(functions);
        
        // Add or update system message
        var updatedMessages = new List<LlmMessage>();
        var hasSystemMessage = false;
        
        foreach (var message in messagesList)
        {
            if (message.Role.ToLowerInvariant() == "system")
            {
                updatedMessages.Add(new LlmMessage { Role = "system", Content = systemMessage });
                hasSystemMessage = true;
            }
            else
            {
                updatedMessages.Add(message);
            }
        }
        
        if (!hasSystemMessage)
        {
            updatedMessages.Insert(0, new LlmMessage { Role = "system", Content = systemMessage });
        }

        var request = new
        {
            model = _configuration.Model,
            messages = ConvertToMistralMessages(updatedMessages),
            max_tokens = _configuration.MaxTokens,
            temperature = _configuration.Temperature,
            top_p = _configuration.TopP,
            stream = _configuration.EnableStreaming
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var mistralResponse = JsonSerializer.Deserialize<MistralChatCompletionResponse>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        if (mistralResponse?.Choices?.FirstOrDefault()?.Message == null)
        {
            throw new InvalidOperationException("Invalid response from Mistral AI API");
        }

        var choice = mistralResponse.Choices[0];
        var result = new FunctionCallResult
        {
            AssistantContent = ExtractJsonFromMarkdown(choice.Message.Content),
            Usage = mistralResponse.Usage != null ? new LlmUsage
            {
                InputTokens = mistralResponse.Usage.PromptTokens,
                OutputTokens = mistralResponse.Usage.CompletionTokens,
                Model = _configuration.Model,
                Provider = "Mistral"
            } : null
        };

        // Try to parse function calls from the response content
        var functionCall = ParseFunctionCallFromContent(choice.Message.Content);
        if (functionCall.HasValue)
        {
            result = new FunctionCallResult
            {
                HasFunctionCall = true,
                FunctionName = functionCall.Value.FunctionName,
                FunctionArgumentsJson = functionCall.Value.Arguments,
                AssistantContent = ExtractJsonFromMarkdown(choice.Message.Content),
                Usage = mistralResponse.Usage != null ? new LlmUsage
                {
                    InputTokens = mistralResponse.Usage.PromptTokens,
                    OutputTokens = mistralResponse.Usage.CompletionTokens,
                    Model = _configuration.Model,
                    Provider = "Mistral"
                } : null
            };
        }

        return result;
    }

    private static string CreateSystemMessageWithFunctions(IEnumerable<OpenAiFunctionSpec> functions)
    {
        var functionDefinitions = functions.Select(f => $@"
Function: {f.Name}
Description: {f.Description}
Parameters: {JsonSerializer.Serialize(f.ParametersSchema, new JsonSerializerOptions { WriteIndented = true })}
").ToList();

        return $@"You are a helpful AI assistant. You have access to the following functions:

{string.Join("\n", functionDefinitions)}

When you need to call a function, respond with a JSON object in this format:
{{
  ""function"": ""function_name"",
  ""arguments"": {{
    ""param1"": ""value1"",
    ""param2"": ""value2""
  }}
}}

Always respond with valid JSON when calling functions.";
    }

    private static (string FunctionName, string Arguments)? ParseFunctionCallFromContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        // First, check if content contains backticks and extract JSON from markdown
        var processedContent = content.Trim();
        if (processedContent.Contains("```") || processedContent.Contains("`"))
        {
            processedContent = ExtractJsonFromMarkdown(content);
            if (string.IsNullOrEmpty(processedContent))
                return null;
        }

        // Try to parse the processed content as JSON
        try
        {
            var jsonDoc = JsonDocument.Parse(processedContent);
            var root = jsonDoc.RootElement;

            // Pattern 1: { "function": "name", "arguments": { ... } }
            if (root.TryGetProperty("function", out var functionNameProp) &&
                root.TryGetProperty("arguments", out var argumentsProp))
            {
                return (functionNameProp.GetString()!, argumentsProp.GetRawText());
            }

            // Pattern 2: { "action": "tool_call", "tool_name"|"tool": "name", "parameters"|"params": { ... } }
            if (root.TryGetProperty("action", out var actionProp) &&
                string.Equals(actionProp.GetString(), "tool_call", StringComparison.OrdinalIgnoreCase))
            {
                // Try root-level tool name
                if (TryGetFunctionName(root, out var fnName) && TryGetArguments(root, out var fnArgs))
                {
                    return (fnName!, fnArgs!);
                }

                // Or under action_input
                if (root.TryGetProperty("action_input", out var actionInput) &&
                    actionInput.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetFunctionName(actionInput, out fnName) && TryGetArguments(actionInput, out fnArgs))
                    {
                        return (fnName!, fnArgs!);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // If the processed content fails to parse, try the original content as fallback
            try
            {
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Pattern 1: { "function": "name", "arguments": { ... } }
                if (root.TryGetProperty("function", out var functionNameProp) &&
                    root.TryGetProperty("arguments", out var argumentsProp))
                {
                    return (functionNameProp.GetString()!, argumentsProp.GetRawText());
                }

                // Pattern 2: { "action": "tool_call", "tool_name"|"tool": "name", "parameters"|"params": { ... } }
                if (root.TryGetProperty("action", out var actionProp) &&
                    string.Equals(actionProp.GetString(), "tool_call", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryGetFunctionName(root, out var fnName) && TryGetArguments(root, out var fnArgs))
                    {
                        return (fnName!, fnArgs!);
                    }
                    if (root.TryGetProperty("action_input", out var actionInput) && actionInput.ValueKind == JsonValueKind.Object)
                    {
                        if (TryGetFunctionName(actionInput, out fnName) && TryGetArguments(actionInput, out fnArgs))
                        {
                            return (fnName!, fnArgs!);
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        return null;
    }

    private static bool TryGetFunctionName(JsonElement container, out string? functionName)
    {
        functionName = null;
        if (container.TryGetProperty("tool_name", out var toolName) && toolName.ValueKind == JsonValueKind.String)
        {
            functionName = toolName.GetString();
            return !string.IsNullOrEmpty(functionName);
        }
        if (container.TryGetProperty("tool", out var tool) && tool.ValueKind == JsonValueKind.String)
        {
            functionName = tool.GetString();
            return !string.IsNullOrEmpty(functionName);
        }
        return false;
    }

    private static bool TryGetArguments(JsonElement container, out string? argumentsJson)
    {
        argumentsJson = null;
        if (container.TryGetProperty("parameters", out var parameters) &&
            (parameters.ValueKind == JsonValueKind.Object || parameters.ValueKind == JsonValueKind.Array))
        {
            argumentsJson = parameters.GetRawText();
            return true;
        }
        if (container.TryGetProperty("params", out var @params) &&
            (@params.ValueKind == JsonValueKind.Object || @params.ValueKind == JsonValueKind.Array))
        {
            argumentsJson = @params.GetRawText();
            return true;
        }
        return false;
    }

    private static string ExtractJsonFromMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Check if content is wrapped in markdown code blocks
        var trimmedContent = content.Trim();
        
        // Pattern for ```json ... ``` or ``` ... ``` - more flexible matching
        var jsonBlockPattern = @"```(?:json)?\s*\n?(.*?)\n?```";
        var match = System.Text.RegularExpressions.Regex.Match(trimmedContent, jsonBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        
        // Alternative pattern for cases where the regex might not match exactly
        // Look for content that starts with ```json and ends with ```
        if (trimmedContent.StartsWith("```json"))
        {
            var startIndex = trimmedContent.IndexOf('\n');
            if (startIndex > 0)
            {
                var endIndex = trimmedContent.LastIndexOf("```");
                if (endIndex > startIndex)
                {
                    var jsonContent = trimmedContent.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                    try
                    {
                        // Validate that it's actually valid JSON
                        JsonDocument.Parse(jsonContent);
                        return jsonContent;
                    }
                    catch
                    {
                        // Not valid JSON, continue to next method
                    }
                }
                else
                {
                    // No closing ``` found, extract everything after the first newline
                    var jsonContent = trimmedContent.Substring(startIndex + 1).Trim();
                    try
                    {
                        // Validate that it's actually valid JSON
                        JsonDocument.Parse(jsonContent);
                        return jsonContent;
                    }
                    catch
                    {
                        // Not valid JSON, continue to next method
                    }
                }
            }
        }
        
        // Try to find any JSON-like content that might be surrounded by backticks
        // This handles cases where the content might have partial markdown formatting
        var backtickPattern = @"`([^`]+)`";
        var backtickMatches = System.Text.RegularExpressions.Regex.Matches(trimmedContent, backtickPattern);
        
        foreach (System.Text.RegularExpressions.Match backtickMatch in backtickMatches)
        {
            var potentialJson = backtickMatch.Groups[1].Value.Trim();
            if (potentialJson.StartsWith("{") && potentialJson.EndsWith("}"))
            {
                try
                {
                    // Validate that it's actually valid JSON
                    JsonDocument.Parse(potentialJson);
                    return potentialJson;
                }
                catch
                {
                    // Not valid JSON, continue to next match
                }
            }
        }
        
        // If no markdown blocks found, return the original content
        return content;
    }

    private static List<MistralMessage> ConvertToMistralMessages(IEnumerable<LlmMessage> messages)
    {
        return messages.Select(m => new MistralMessage
        {
            Role = ConvertRole(m.Role),
            Content = m.Content
        }).ToList();
    }

    private static string ConvertRole(string role)
    {
        return role.ToLower() switch
        {
            "user" => "user",
            "assistant" => "assistant",
            "system" => "system",
            _ => "user"
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    // Mistral API response models
    private class MistralChatCompletionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public List<MistralChoice> Choices { get; set; } = new();
        public MistralUsage? Usage { get; set; }
    }

    private class MistralChoice
    {
        public int Index { get; set; }
        public MistralMessage Message { get; set; } = new();
        public string FinishReason { get; set; } = string.Empty;
    }

    private class MistralMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class MistralUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
