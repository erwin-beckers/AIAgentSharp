using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace AIAgentSharp.Gemini;

/// <summary>
/// Google Gemini LLM client with first-class streaming support (SSE).
/// Mirrors OpenAI/Anthropic: single StreamAsync; text => streaming; tools => non-streaming.
/// </summary>
public sealed class GeminiLlmClient : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiConfiguration Configuration { get; }

    public GeminiLlmClient(string apiKey, string model = "gemini-2.5-flash")
        : this(apiKey, new GeminiConfiguration { Model = model })
    {
    }

    public GeminiLlmClient(string apiKey, GeminiConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        _apiKey = apiKey;
        _model = configuration.Model;
        _httpClient = new HttpClient { Timeout = configuration.RequestTimeout };
        Configuration = configuration;
    }

    internal GeminiLlmClient(HttpClient httpClient, string apiKey, string model = "gemini-2.5-flash")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _model = model;
        Configuration = new GeminiConfiguration { Model = model };
    }

    /// <summary>
    /// Streams chunks from Gemini. Text uses SSE; function calls use non-streaming call.
    /// </summary>
    public async IAsyncEnumerable<LlmStreamingChunk> StreamAsync(LlmRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.Messages == null || !request.Messages.Any())
            throw new ArgumentException("Request must contain at least one message.", nameof(request));

        var contents = ConvertToGeminiContents(request.Messages.ToList());

        var genConfig = new GeminiGenerationConfig
        {
            MaxOutputTokens = request.MaxTokens ?? Configuration.MaxTokens,
            Temperature = (float?)(request.Temperature ?? Configuration.Temperature),
            TopP = (float?)(request.TopP ?? Configuration.TopP),
            TopK = Configuration.TopK
        };

        var geminiRequest = new GeminiGenerateContentRequest
        {
            Contents = contents,
            GenerationConfig = genConfig
        };

        var usage = new LlmUsage { Model = _model, Provider = "Gemini" };

        // Tools => single non-streaming call (like OpenAI client)
        if (request.Functions != null && request.Functions.Any())
        {
            geminiRequest.Tools = ConvertToGeminiTools(request.Functions);

            var response = await _httpClient.PostAsJsonAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}",
                geminiRequest,
                cancellationToken: ct);

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Empty response from Gemini API.");

            if (body.UsageMetadata != null)
            {
                usage.InputTokens = body.UsageMetadata.PromptTokenCount ?? 0;
                usage.OutputTokens = body.UsageMetadata.CandidatesTokenCount ?? 0;
            }

            var candidate = body.Candidates.FirstOrDefault();
            var text = candidate?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;

            if (candidate?.FunctionCall != null)
            {
                yield return new LlmStreamingChunk
                {
                    Content = text,
                    IsFinal = true,
                    FinishReason = "tool_calls",
                    ActualResponseType = LlmResponseType.FunctionCall,
                    FunctionCall = new LlmFunctionCall
                    {
                        Name = candidate.FunctionCall.Name,
                        ArgumentsJson = JsonSerializer.Serialize(candidate.FunctionCall.Args),
                        Arguments = candidate.FunctionCall.Args != null
                            ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(candidate.FunctionCall.Args)) ?? new Dictionary<string, object>()
                            : new Dictionary<string, object>()
                    },
                    Usage = usage
                };
                yield break;
            }

            yield return new LlmStreamingChunk
            {
                Content = text,
                IsFinal = true,
                FinishReason = candidate?.FinishReason ?? "stop",
                ActualResponseType = LlmResponseType.Text,
                Usage = usage
            };
            yield break;
        }

        // Text => respect EnableStreaming; SSE when true, otherwise non-streaming
        if (request.EnableStreaming)
        {
            await foreach (var chunk in StreamSseAsync(geminiRequest, usage, ct))
            {
                yield return chunk;
            }
        }
        else
        {
            var resp = await _httpClient.PostAsJsonAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}",
                geminiRequest,
                cancellationToken: ct);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(cancellationToken: ct);
            var text = body?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
            if (body?.UsageMetadata != null)
            {
                usage.InputTokens = body.UsageMetadata.PromptTokenCount ?? 0;
                usage.OutputTokens = body.UsageMetadata.CandidatesTokenCount ?? 0;
            }
            yield return new LlmStreamingChunk
            {
                Content = text,
                IsFinal = true,
                FinishReason = "stop",
                ActualResponseType = LlmResponseType.Text,
                Usage = usage
            };
        }
    }

    private async IAsyncEnumerable<LlmStreamingChunk> StreamSseAsync(GeminiGenerateContentRequest geminiRequest, LlmUsage usage, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:streamGenerateContent?alt=sse&key={_apiKey}")
        {
            Content = JsonContent.Create(geminiRequest)
        };
        req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: false);

        var frame = new StringBuilder();
        var finishReason = "stop";

        string? line;
        var lineCount = 0;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            ct.ThrowIfCancellationRequested();
            lineCount++;


            // Empty line => end of one SSE event
            if (line.Length == 0)
            {
                if (frame.Length == 0) continue;

                var payload = frame.ToString();
                frame.Clear();

                var jsonBuilder = new StringBuilder();
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
                            yield return new LlmStreamingChunk
                            {
                                Content = string.Empty,
                                IsFinal = true,
                                FinishReason = finishReason,
                                ActualResponseType = LlmResponseType.Text,
                                Usage = usage
                            };
                            yield break;
                        }

                        jsonBuilder.Append(data);
                    }
                }

                var json = jsonBuilder.ToString();
                if (string.IsNullOrWhiteSpace(json)) continue;

                GeminiGenerateContentResponse? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(json, JsonOptions);
                }
                catch
                {
                    continue; // ignore malformed fragments
                }
                if (chunk == null) continue;

                var candidate = chunk.Candidates.FirstOrDefault();
                var parts = candidate?.Content?.Parts;
                if (parts != null)
                {
                    // Gemini may send multiple parts per event. Emit each text part.
                    foreach (var part in parts)
                    {
                        var text = part.Text;
                        if (!string.IsNullOrEmpty(text))
                        {
                            yield return new LlmStreamingChunk
                            {
                                Content = text,
                                IsFinal = false,
                                ActualResponseType = LlmResponseType.Streaming,
                                Usage = usage
                            };
                        }
                    }
                }

                if (chunk.UsageMetadata != null)
                {
                    usage.InputTokens = chunk.UsageMetadata.PromptTokenCount ?? 0;
                    usage.OutputTokens = chunk.UsageMetadata.CandidatesTokenCount ?? 0;
                }

                // Track finish reason if provided
                if (!string.IsNullOrEmpty(candidate?.FinishReason))
                {
                    finishReason = candidate!.FinishReason!;
                }

                continue;
            }

            // Accumulate current SSE frame
            frame.AppendLine(line);
        }

        // Graceful close without [DONE]
        yield return new LlmStreamingChunk
        {
            Content = string.Empty,
            IsFinal = true,
            FinishReason = finishReason,
            ActualResponseType = LlmResponseType.Text,
            Usage = usage
        };
    }

    [ExcludeFromCodeCoverage]
    private static List<GeminiContent> ConvertToGeminiContents(List<LlmMessage> messages)
    {
        return messages.Select(m => new GeminiContent
        {
            Role = MapRole(m.Role),
            Parts = new List<GeminiPart> { new GeminiPart { Text = m.Content } }
        }).ToList();
    }

    [ExcludeFromCodeCoverage]
    private static string MapRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "assistant" => "model",
            "model" => "model",
            "user" => "user",
            "system" => "user",
            _ => "user"
        };
    }

    [ExcludeFromCodeCoverage]
    private static List<GeminiTool> ConvertToGeminiTools(IEnumerable<FunctionSpec> functions)
    {
        return new List<GeminiTool>
        {
            new GeminiTool
            {
                FunctionDeclarations = functions.Select(f => new GeminiFunctionDeclaration
                {
                    Name = f.Name,
                    Description = f.Description,
                    Parameters = TransformSchemaForGemini(f.ParametersSchema)
                }).ToList()
            }
        };
    }

    private static object? TransformSchemaForGemini(object? schema)
    {
        if (schema is null) return null;
        var json = JsonSerializer.Serialize(schema);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return TransformJsonElement(element);
    }

    [ExcludeFromCodeCoverage]
    private static object? TransformJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => TransformJsonElement(p.Value)!),
            JsonValueKind.Array => element.EnumerateArray().Select(TransformJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private sealed class GeminiGenerateContentRequest
    {
        public List<GeminiContent> Contents { get; set; } = new();
        public List<GeminiTool>? Tools { get; set; }
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiContent
    {
        public string Role { get; set; } = string.Empty;
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private sealed class GeminiPart
    {
        public string? Text { get; set; }
    }

    private sealed class GeminiTool
    {
        public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();
    }

    private sealed class GeminiFunctionDeclaration
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public object? Parameters { get; set; }
    }

    private sealed class GeminiGenerationConfig
    {
        public int? MaxOutputTokens { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? TopK { get; set; }
    }

    private sealed class GeminiGenerateContentResponse
    {
        public List<GeminiCandidate> Candidates { get; set; } = new();
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public GeminiFunctionCall? FunctionCall { get; set; }
        public string? FinishReason { get; set; }
    }

    private sealed class GeminiFunctionCall
    {
        public string Name { get; set; } = string.Empty;
        public object? Args { get; set; }
    }

    private sealed class GeminiUsageMetadata
    {
        public int? PromptTokenCount { get; set; }
        public int? CandidatesTokenCount { get; set; }
        public int? TotalTokenCount { get; set; }
    }
}