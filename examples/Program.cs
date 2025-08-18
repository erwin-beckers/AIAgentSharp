using example;
using Examples;

/// <summary>
///     Main program demonstrating the usage of the Agent framework.
///     This example shows how to create an agent, configure it, subscribe to events,
///     and run it with a specific goal and tools for travel planning.
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        // Get OpenAI API key from environment variable
        var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY") ?? throw new InvalidOperationException("Set OPENAI_API_KEY env var.");

        if (args.Contains("--tot"))
        {
            await TreeOfThoughsExample.RunAsync(apiKey);
            return;
        }

        if (args.Contains("--cot"))
        {
            await ChainOfThoughExample.RunAsync(apiKey);
            return;
        }

        await ReactExample.RunAsync(args, apiKey);
    }
}