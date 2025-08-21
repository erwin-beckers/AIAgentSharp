using example;
using Examples;
using AIAgentSharp;
using AIAgentSharp.Anthropic;
using AIAgentSharp.Gemini;
using AIAgentSharp.Mistral;
using AIAgentSharp.OpenAI;

/// <summary>
///     Main program demonstrating the usage of the Agent framework.
///     This example shows how to create an agent, configure it, subscribe to events,
///     and run it with a specific goal and tools for travel planning.
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        // Get API key from environment variable
        var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? throw new InvalidOperationException("Set LLM_API_KEY env var.");

        // Create LLM client - change this line to switch between providers
        ILlmClient llm = CreateLlmClient(apiKey);

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
    /// <param name="apiKey">The API key for the LLM provider</param>
    /// <returns>An LLM client instance</returns>
    private static ILlmClient CreateLlmClient(string apiKey)
    {
        // Uncomment the provider you want to use:

        // OpenAI (GPT-4, GPT-3.5, etc.)
        return new OpenAiLlmClient(apiKey);

        // Anthropic (Claude)
        // return new AnthropicLlmClient(apiKey);

        // Google Gemini
        //  return new GeminiLlmClient(apiKey);

        // Mistral AI
        // return new MistralLlmClient(apiKey);
    }
}