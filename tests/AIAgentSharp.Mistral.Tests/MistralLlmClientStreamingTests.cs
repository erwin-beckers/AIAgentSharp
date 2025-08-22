using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AIAgentSharp.Mistral.Tests;

[TestClass]
public class MistralLlmClientStreamingTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    [TestMethod]
    [Ignore]
    public async Task StreamAsync_Should_ReturnDeltas_When_EnableStreamingTrue()
    {
        var sse = new StringBuilder();
        sse.AppendLine("data: {\"choices\":[{\"delta\":{\"content\":\"hel\"}}]}");
        sse.AppendLine();
        sse.AppendLine("data: {\"choices\":[{\"delta\":{\"content\":\"lo\"}}]}");
        sse.AppendLine();
        sse.AppendLine("data: [DONE]");
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(sse.ToString()));

        var handler = new FakeHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
            return resp;
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.mistral.ai/v1/") };
        var client = new MistralLlmClient(new MistralConfiguration { ApiKey = "x", Model = "mistral-large-latest" });
        // Inject via reflection
        typeof(MistralLlmClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(client, http);

        var req = new LlmRequest
        {
            Messages = new[] { new LlmMessage { Role = "user", Content = "hi" } },
            EnableStreaming = true
        };

        var chunks = new List<LlmStreamingChunk>();
        await foreach (var ch in client.StreamAsync(req))
        {
            chunks.Add(ch);
        }

        Assert.IsTrue(chunks.Any(c => c.Content == "hel"));
        Assert.IsTrue(chunks.Any(c => c.Content == "lo"));
        Assert.IsTrue(chunks.Last().IsFinal);
    }

    [TestMethod]
    public async Task StreamAsync_Should_ReturnSingle_When_EnableStreamingFalse()
    {
        var body = new
        {
            choices = new[] { new { message = new { content = "hello" }, finish_reason = "stop" } },
            usage = new { prompt_tokens = 1, completion_tokens = 2 }
        };
        var json = JsonSerializer.Serialize(body);
        var handler = new FakeHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.mistral.ai/v1/") };
        var client = new MistralLlmClient(new MistralConfiguration { ApiKey = "x", Model = "mistral-large-latest" });
        typeof(MistralLlmClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(client, http);

        var req = new LlmRequest
        {
            Messages = new[] { new LlmMessage { Role = "user", Content = "hi" } },
            EnableStreaming = false
        };

        var chunks = new List<LlmStreamingChunk>();
        await foreach (var ch in client.StreamAsync(req)) chunks.Add(ch);

        Assert.AreEqual(1, chunks.Count);
        Assert.IsTrue(chunks[0].IsFinal);
        Assert.AreEqual("hello", chunks[0].Content);
        Assert.AreEqual(2, chunks[0].Usage!.OutputTokens);
    }
}


