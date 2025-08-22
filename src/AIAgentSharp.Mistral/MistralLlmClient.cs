using System.Text;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AIAgentSharp.Mistral;

/// <summary>
/// Mistral AI LLM client implementation using direct HTTP API calls.
/// This client supports text completion, function calling, and streaming through a unified interface.
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

    /// <summary>
    /// Streams chunks from the Mistral LLM based on the provided request.
    /// This method always returns chunks, regardless of whether streaming is enabled.
    /// </summary>
    /// <param name="request">The unified request containing messages, functions, and configuration.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of LLM streaming chunks.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async IAsyncEnumerable<LlmStreamingChunk> StreamAsync(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Messages == null || !request.Messages.Any())
        {
            throw new ArgumentException("Request must contain at least one message.", nameof(request));
        }

        // Determine the actual response type based on request and available functions
        var actualResponseType = DetermineActualResponseType(request);

        // Handle different response types
        switch (actualResponseType)
        {
            case LlmResponseType.FunctionCall:
                await foreach (var chunk in HandleFunctionCallRequestSafe(request, ct))
                {
                    yield return chunk;
                }
                break;

            case LlmResponseType.Streaming:
                await foreach (var chunk in HandleStreamingRequestSafe(request, ct))
                {
                    yield return chunk;
                }
                break;

            case LlmResponseType.Text:
            default:
                await foreach (var chunk in HandleTextRequestSafe(request, ct))
                {
                    yield return chunk;
                }
                break;
        }
    }

    private LlmResponseType DetermineActualResponseType(LlmRequest request)
    {
        // Stream by default for plain text (matches OpenAI/Anthropic/Gemini behavior)
        if (request.Functions != null && request.Functions.Any())
        {
            return LlmResponseType.FunctionCall;
        }
        return LlmResponseType.Streaming;
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleTextRequestSafe(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        IAsyncEnumerable<LlmStreamingChunk> result;
        try
        {
            result = HandleTextRequest(request, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Mistral text request failed: {ex.Message}", ex);
        }

        await foreach (var chunk in result)
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleTextRequest(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var mistralRequest = new
        {
            model = _configuration.Model,
            messages = ConvertToMistralMessages(request.Messages),
            max_tokens = request.MaxTokens ?? _configuration.MaxTokens,
            temperature = request.Temperature ?? _configuration.Temperature,
            top_p = request.TopP ?? _configuration.TopP,
            stream = false
        };

        var json = JsonSerializer.Serialize(mistralRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var mistralResponse = JsonSerializer.Deserialize<MistralChatCompletionResponse>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        if (mistralResponse?.Choices?.FirstOrDefault()?.Message == null)
        {
            throw new InvalidOperationException("Invalid response from Mistral AI API");
        }

        var extractedContent = ExtractJsonFromMarkdown(mistralResponse.Choices[0].Message.Content);

        yield return new LlmStreamingChunk
        {
            Content = extractedContent,
            IsFinal = true,
            FinishReason = "stop",
            ActualResponseType = LlmResponseType.Text,
            Usage = mistralResponse.Usage != null ? new LlmUsage
            {
                InputTokens = mistralResponse.Usage.PromptTokens,
                OutputTokens = mistralResponse.Usage.CompletionTokens,
                Model = _configuration.Model,
                Provider = "Mistral"
            } : null
        };
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleFunctionCallRequestSafe(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        IAsyncEnumerable<LlmStreamingChunk> result;
        try
        {
            result = HandleFunctionCallRequest(request, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Mistral function call request failed: {ex.Message}", ex);
        }

        await foreach (var chunk in result)
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleFunctionCallRequest(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        if (!_configuration.EnableFunctionCalling)
        {
            throw new InvalidOperationException("Function calling is not enabled in the configuration");
        }

        // For Mistral, we need to include function definitions in the system message
        // since Mistral doesn't support the tools parameter in the same way as OpenAI
        var messagesList = request.Messages.ToList();
        var systemMessage = CreateSystemMessageWithFunctions(request.Functions!);
        
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

        var mistralRequest = new
        {
            model = _configuration.Model,
            messages = ConvertToMistralMessages(updatedMessages),
            max_tokens = request.MaxTokens ?? _configuration.MaxTokens,
            temperature = request.Temperature ?? _configuration.Temperature,
            top_p = request.TopP ?? _configuration.TopP,
            stream = false
        };

        var json = JsonSerializer.Serialize(mistralRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var mistralResponse = JsonSerializer.Deserialize<MistralChatCompletionResponse>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        if (mistralResponse?.Choices?.FirstOrDefault()?.Message == null)
        {
            throw new InvalidOperationException("Invalid response from Mistral AI API");
        }

        var choice = mistralResponse.Choices[0];
        var extractedContent = ExtractJsonFromMarkdown(choice.Message.Content);

        var chunk = new LlmStreamingChunk
        {
            Content = extractedContent,
            IsFinal = true,
            FinishReason = "stop",
            ActualResponseType = LlmResponseType.Text, // Default to text, will update if function call found
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
            chunk.ActualResponseType = LlmResponseType.FunctionCall;
            chunk.FunctionCall = new LlmFunctionCall
            {
                Name = functionCall.Value.FunctionName,
                ArgumentsJson = functionCall.Value.Arguments,
                Arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(functionCall.Value.Arguments) ?? new Dictionary<string, object>()
            };
        }

        yield return chunk;
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleStreamingRequestSafe(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        IAsyncEnumerable<LlmStreamingChunk> result;
        try
        {
            result = HandleStreamingRequest(request, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Mistral streaming request failed: {ex.Message}", ex);
        }

        await foreach (var chunk in result)
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleStreamingRequest(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // Respect EnableStreaming; if false, call non-streaming path
        if (!request.EnableStreaming)
        {
            await foreach (var chunk in HandleTextRequestNonStreaming(request, ct))
            {
                yield return chunk;
            }
            yield break;
        }

        var usage = new LlmUsage { Model = _configuration.Model, Provider = "Mistral" };

        var mistralRequest = new
        {
            model = _configuration.Model,
            messages = ConvertToMistralMessages(request.Messages),
            max_tokens = request.MaxTokens ?? _configuration.MaxTokens,
            temperature = request.Temperature ?? _configuration.Temperature,
            top_p = request.TopP ?? _configuration.TopP,
            stream = true
        };

        var json = JsonSerializer.Serialize(mistralRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: false);

        var frameBuilder = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            ct.ThrowIfCancellationRequested();

            // End of one SSE event
            if (line.Length == 0)
            {
                if (frameBuilder.Length == 0) continue;
                var payload = frameBuilder.ToString();
                frameBuilder.Clear();

                // Collect data: lines in the frame into a single JSON string
                var dataBuilder = new StringBuilder();
                using (var sr = new StringReader(payload))
                {
                    string? l;
                    while ((l = sr.ReadLine()) != null)
                    {
                        if (!l.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
                        var data = l.Substring(5).TrimStart();
                        if (data.Length == 0) continue;
                        if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                        {
                            // Final chunk
                            yield return new LlmStreamingChunk
                            {
                                Content = string.Empty,
                                IsFinal = true,
                                FinishReason = "stop",
                                ActualResponseType = LlmResponseType.Text,
                                Usage = usage
                            };
                            yield break;
                        }
                        dataBuilder.Append(data);
                    }
                }

                var dataJson = dataBuilder.ToString();
                if (string.IsNullOrWhiteSpace(dataJson)) continue;

                using (var doc = JsonDocument.Parse(dataJson))
                {
                    var root = doc.RootElement;

                    // Update usage if present (usually only in final event)
                    if (root.TryGetProperty("usage", out var usageElem) && usageElem.ValueKind == JsonValueKind.Object)
                    {
                        if (usageElem.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var promptTokens))
                            usage.InputTokens = promptTokens;
                        if (usageElem.TryGetProperty("completion_tokens", out var ctokens) && ctokens.TryGetInt32(out var completionTokens))
                            usage.OutputTokens = completionTokens;
                    }

                    // Extract incremental content: choices[0].delta.content (preferred), fallback to choices[0].message.content
                    if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
                    {
                        var choice0 = choices[0];
                        string? deltaText = null;

                        if (choice0.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.Object)
                        {
                            if (delta.TryGetProperty("content", out var contentElem) && contentElem.ValueKind == JsonValueKind.String)
                            {
                                deltaText = contentElem.GetString();
                            }
                        }
                        else if (choice0.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object)
                        {
                            if (message.TryGetProperty("content", out var contentElem) && contentElem.ValueKind == JsonValueKind.String)
                            {
                                deltaText = contentElem.GetString();
                            }
                        }

                        if (!string.IsNullOrEmpty(deltaText))
                        {
                            yield return new LlmStreamingChunk
                            {
                                Content = deltaText!,
                                IsFinal = false,
                                ActualResponseType = LlmResponseType.Streaming,
                                Usage = usage
                            };
                        }
                    }
                }

                continue;
            }

            // Accumulate frame lines
            frameBuilder.AppendLine(line);
        }
    }

    private async IAsyncEnumerable<LlmStreamingChunk> HandleTextRequestNonStreaming(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var mistralRequest = new
        {
            model = _configuration.Model,
            messages = ConvertToMistralMessages(request.Messages),
            max_tokens = request.MaxTokens ?? _configuration.MaxTokens,
            temperature = request.Temperature ?? _configuration.Temperature,
            top_p = request.TopP ?? _configuration.TopP,
            stream = false
        };

        var json = JsonSerializer.Serialize(mistralRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var mistralResponse = JsonSerializer.Deserialize<MistralChatCompletionResponse>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        var text = mistralResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        var usage = mistralResponse?.Usage != null ? new LlmUsage
        {
            InputTokens = mistralResponse.Usage.PromptTokens,
            OutputTokens = mistralResponse.Usage.CompletionTokens,
            Model = _configuration.Model,
            Provider = "Mistral"
        } : null;

        yield return new LlmStreamingChunk
        {
            Content = text,
            IsFinal = true,
            FinishReason = "stop",
            ActualResponseType = LlmResponseType.Text,
            Usage = usage
        };
    }

    [ExcludeFromCodeCoverage]
    private static string CreateSystemMessageWithFunctions(IEnumerable<FunctionSpec> functions)
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
