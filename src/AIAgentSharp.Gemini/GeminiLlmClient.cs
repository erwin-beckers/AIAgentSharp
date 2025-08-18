using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AIAgentSharp.Gemini;

/// <summary>
/// Google Gemini LLM client implementation that integrates with the Gemini API.
/// This client supports both regular chat completion and function calling via tools.
/// </summary>
/// <remarks>
/// This client uses the official Gemini API to access Gemini models.
/// You need a Gemini API key for authentication.
/// 
/// To get an API key:
/// 1. Go to the Google AI Studio (https://makersuite.google.com/app/apikey)
/// 2. Create a new API key
/// 3. Use this API key with the constructor
/// 
/// API Documentation: https://ai.google.dev/gemini-api/docs
/// </remarks>
public sealed class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    
    /// <summary>
    /// Gets the Gemini configuration used by this client.
    /// </summary>
    public GeminiConfiguration Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the GeminiLlmClient class with API key and model.
    /// </summary>
    /// <param name="apiKey">The Gemini API key from Google AI Studio for authentication.</param>
    /// <param name="model">The model to use for completions. Defaults to "gemini-1.5-flash".</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
    public GeminiLlmClient(string apiKey, string model = "gemini-1.5-flash")
        : this(apiKey, new GeminiConfiguration { Model = model })
    {
    }

    /// <summary>
    /// Initializes a new instance of the GeminiLlmClient class with API key and configuration.
    /// </summary>
    /// <param name="apiKey">The Gemini API key from Google AI Studio for authentication.</param>
    /// <param name="configuration">The Gemini configuration settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey or configuration is null or empty.</exception>
    public GeminiLlmClient(string apiKey, GeminiConfiguration configuration)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _apiKey = apiKey;
        _model = configuration.Model;
        _httpClient = new HttpClient();
        
        // Store configuration for use in completion methods
        Configuration = configuration;
    }

    /// <summary>
    /// Internal constructor for testing purposes.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="model">The model to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient is null.</exception>
    internal GeminiLlmClient(HttpClient httpClient, string apiKey, string model = "gemini-1.5-flash")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _model = model;
        Configuration = new GeminiConfiguration { Model = model };
    }

    /// <summary>
    /// Sends a collection of messages to the LLM and returns the generated response.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the LLM completion.</returns>
    /// <exception cref="ArgumentNullException">Thrown when messages is null.</exception>
    /// <exception cref="ArgumentException">Thrown when messages is empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<LlmCompletionResult> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        var messageList = messages.ToList();
        if (messageList.Count == 0)
        {
            throw new ArgumentException("Messages cannot be empty.", nameof(messages));
        }

        // Convert AIAgentSharp messages to Gemini messages
        var geminiContents = ConvertToGeminiContents(messageList);

        var request = new GeminiGenerateContentRequest
        {
            Contents = geminiContents,
            GenerationConfig = new GeminiGenerationConfig
            {
                MaxOutputTokens = Configuration.MaxTokens,
                Temperature = Configuration.Temperature,
                TopP = Configuration.TopP,
                TopK = Configuration.TopK
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}",
            request,
            cancellationToken: ct);

        response.EnsureSuccessStatusCode();

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(cancellationToken: ct);
        
        if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text == null)
        {
            throw new InvalidOperationException("Invalid response from Gemini API");
        }

        var content = geminiResponse.Candidates[0].Content.Parts[0].Text;

        // Extract JSON from markdown code blocks if present
        var extractedContent = ExtractJsonFromMarkdown(content);

        return new LlmCompletionResult
        {
            Content = extractedContent,
            Usage = new LlmUsage
            {
                InputTokens = geminiResponse.UsageMetadata?.PromptTokenCount ?? 0,
                OutputTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0,
                Model = _model,
                Provider = "Gemini"
            }
        };
    }

    /// <summary>
    /// Completes a conversation with function calling capabilities.
    /// </summary>
    /// <param name="messages">The conversation messages to send to the LLM.</param>
    /// <param name="functions">The available functions that the LLM can call.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the function calling operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages,
        IEnumerable<OpenAiFunctionSpec> functions,
        CancellationToken ct = default)
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        if (functions == null)
        {
            throw new ArgumentNullException(nameof(functions));
        }

        var messageList = messages.ToList();
        if (messageList.Count == 0)
        {
            throw new ArgumentException("Messages cannot be empty.", nameof(messages));
        }

        // Convert AIAgentSharp messages to Gemini messages
        var geminiContents = ConvertToGeminiContents(messageList);
        
        // Convert OpenAI function specs to Gemini tools
        var geminiTools = ConvertToGeminiTools(functions);

        var request = new GeminiGenerateContentRequest
        {
            Contents = geminiContents,
            Tools = geminiTools,
            GenerationConfig = new GeminiGenerationConfig
            {
                MaxOutputTokens = Configuration.MaxTokens,
                Temperature = Configuration.Temperature,
                TopP = Configuration.TopP,
                TopK = Configuration.TopK
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}",
            request,
            cancellationToken: ct);

        response.EnsureSuccessStatusCode();

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(cancellationToken: ct);
        
        if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text == null)
        {
            throw new InvalidOperationException("Invalid response from Gemini API");
        }

        var candidate = geminiResponse.Candidates[0];
        var content = candidate.Content.Parts[0].Text;

        // Extract JSON from markdown code blocks if present
        var extractedContent = ExtractJsonFromMarkdown(content);

        var result = new FunctionCallResult
        {
            AssistantContent = extractedContent,
            Usage = new LlmUsage
            {
                InputTokens = geminiResponse.UsageMetadata?.PromptTokenCount ?? 0,
                OutputTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0,
                Model = _model,
                Provider = "Gemini"
            }
        };

        // Check for function calls
        if (candidate.FunctionCall != null)
        {
            result = new FunctionCallResult
            {
                HasFunctionCall = true,
                FunctionName = candidate.FunctionCall.Name,
                FunctionArgumentsJson = JsonSerializer.Serialize(candidate.FunctionCall.Args),
                AssistantContent = extractedContent,
                Usage = new LlmUsage
                {
                    InputTokens = geminiResponse.UsageMetadata?.PromptTokenCount ?? 0,
                    OutputTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0,
                    Model = _model,
                    Provider = "Gemini"
                }
            };
        }

        return result;
    }

    private static List<GeminiContent> ConvertToGeminiContents(IEnumerable<LlmMessage> messages)
    {
        var geminiContents = new List<GeminiContent>();
        
        foreach (var message in messages)
        {
            var role = message.Role.ToLowerInvariant() switch
            {
                "system" => "user", // Gemini doesn't have system role, convert to user
                "user" => "user",
                "assistant" => "model",
                "tool_result" => "user", // Convert tool results to user messages
                _ => "user" // Default to user for unknown roles
            };

            geminiContents.Add(new GeminiContent
            {
                Role = role,
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = message.Content }
                }
            });
        }

        return geminiContents;
    }

    private static List<GeminiTool> ConvertToGeminiTools(IEnumerable<OpenAiFunctionSpec> functions)
    {
        var geminiTools = new List<GeminiTool>();
        
        foreach (var function in functions)
        {
            // Transform the schema to be compatible with Gemini API
            var geminiSchema = TransformSchemaForGemini(function.ParametersSchema);
            
            var geminiTool = new GeminiTool
            {
                FunctionDeclarations = new List<GeminiFunctionDeclaration>
                {
                    new GeminiFunctionDeclaration
                    {
                        Name = function.Name,
                        Description = function.Description,
                        Parameters = geminiSchema
                    }
                }
            };
            
            geminiTools.Add(geminiTool);
        }

        return geminiTools;
    }

    private static string ExtractJsonFromMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Check if content is wrapped in markdown code blocks
        var trimmedContent = content.Trim();
        
        // Pattern for ```json ... ``` or ``` ... ```
        var jsonBlockPattern = @"^```(?:json)?\s*\n(.*?)\n```\s*$";
        var match = System.Text.RegularExpressions.Regex.Match(trimmedContent, jsonBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        
        // If no markdown blocks found, return the original content
        return content;
    }

    private static object TransformSchemaForGemini(object schema)
    {
        // Convert to JSON and back to manipulate the schema
        var json = JsonSerializer.Serialize(schema);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        
        return TransformJsonElementForGemini(element);
    }

    private static object TransformJsonElementForGemini(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    // Skip additionalProperties as Gemini doesn't support it
                    if (property.Name == "additionalProperties")
                        continue;
                    
                    obj[property.Name] = TransformJsonElementForGemini(property.Value);
                }
                return obj;
                
            case JsonValueKind.Array:
                // Handle union types (e.g., ["string", "null"]) - Gemini doesn't support these
                // Convert to the first non-null type
                var array = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(TransformJsonElementForGemini(item));
                }
                
                // If this is a type array (union type), return the first non-null type
                if (array.Count > 0 && array.All(x => x is string))
                {
                    var typeArray = array.Cast<string>().ToList();
                    var firstNonNullType = typeArray.FirstOrDefault(t => t != "null");
                    return firstNonNullType ?? "string"; // Default to string if all are null
                }
                
                return array;
                
            case JsonValueKind.String:
                return element.GetString()!;
                
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                return element.GetDouble();
                
            case JsonValueKind.True:
                return true;
                
            case JsonValueKind.False:
                return false;
                
            default:
                return element.ToString();
        }
    }

    // Internal DTOs for Gemini API
    private class GeminiGenerateContentRequest
    {
        public List<GeminiContent> Contents { get; set; } = new();
        public List<GeminiTool>? Tools { get; set; }
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private class GeminiContent
    {
        public string Role { get; set; } = string.Empty;
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }

    private class GeminiTool
    {
        public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();
    }

    private class GeminiFunctionDeclaration
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public object? Parameters { get; set; }
    }

    private class GeminiGenerationConfig
    {
        public int? MaxOutputTokens { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? TopK { get; set; }
    }

    private class GeminiGenerateContentResponse
    {
        public List<GeminiCandidate> Candidates { get; set; } = new();
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent Content { get; set; } = new();
        public GeminiFunctionCall? FunctionCall { get; set; }
    }

    private class GeminiFunctionCall
    {
        public string Name { get; set; } = string.Empty;
        public object? Args { get; set; }
    }

    private class GeminiUsageMetadata
    {
        public int? PromptTokenCount { get; set; }
        public int? CandidatesTokenCount { get; set; }
        public int? TotalTokenCount { get; set; }
    }
}
