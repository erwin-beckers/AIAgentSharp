using example;
using Examples;
using AIAgentSharp;
using AIAgentSharp.Anthropic;
using AIAgentSharp.Gemini;
using AIAgentSharp.Mistral;
using AIAgentSharp.OpenAI;

public enum LLMType
{
    OpenAi,
    Antrophic,
    Gemini,
    Mistral
}
/// <summary>
///     Main program demonstrating the usage of the Agent framework.
///     This example shows how to create an agent, configure it, subscribe to events,
///     and run it with a specific goal and tools for travel planning.
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        // Create LLM client - change this line to switch between providers
        var llm = CreateLlmClient(LLMType.OpenAi);

        Console.WriteLine("---------------------------- SIMPLE STREAMING TEST --------------------------");
        await SimpleStreamingTest.RunAsync(llm);

        Console.WriteLine("---------------------------- RE/ACT EXAMPLE --------------------------");
        await ReactExample.RunAsync(llm);

        Console.WriteLine("---------------------------- CHAIN OF THOUGHTS EXAMPLE --------------------------");
        await ChainOfThoughExample.RunAsync(llm);

        Console.WriteLine("---------------------------- TREE OF THOUGHTS EXAMPLE --------------------------");
        await TreeOfThoughsExample.RunAsync(llm);

        Console.WriteLine("---------------------------- CUSTOM SCHEMA EXAMPLE --------------------------");
        await CustomSchemaExample.RunAsync(llm);

        Console.WriteLine("---------------------------- HYBRID REASONING EXAMPLE --------------------------");
        await HybridReasoningExample.RunAsync(llm);
    }

    /// <summary>
    /// Creates an LLM client based on the specified provider.
    /// Change the return statement to switch between different LLM providers.
    /// </summary>
    /// <returns>An LLM client instance</returns>
    private static ILlmClient CreateLlmClient(LLMType llmType)
    {
        switch (llmType)
        {

            case LLMType.Antrophic:
                {
                    var apiKey = Environment.GetEnvironmentVariable("ANTROPHIC_API_KEY") ?? throw new InvalidOperationException("Set ANTROPHIC_API_KEY env var.");

                    return new AnthropicLlmClient(apiKey);
                }
            case LLMType.Gemini:
                {
                    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new InvalidOperationException("Set GEMINI_API_KEY env var.");
                    return new GeminiLlmClient(apiKey);
                }
            case LLMType.Mistral:
                {
                    var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY") ?? throw new InvalidOperationException("Set MISTRAL_API_KEY env var.");
                    return new MistralLlmClient(apiKey);
                }
            case LLMType.OpenAi:
            default:
                {
                    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("Set OPENAI_API_KEY env var.");
                    return new OpenAiLlmClient(apiKey);
                }
        }
    }
}