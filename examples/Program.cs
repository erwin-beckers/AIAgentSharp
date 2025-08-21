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
        var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY") ?? throw new InvalidOperationException("Set LLM_API_KEY env var.");


        Console.WriteLine("---------------------------- SIMPLE STREAMING TEST --------------------------");
        await SimpleStreamingTest.RunAsync(apiKey);

        Console.WriteLine("---------------------------- RE/ACT EXAMPLE --------------------------");
        await ReactExample.RunAsync(apiKey);

        Console.WriteLine("---------------------------- CHAIN OF THOUGHTS EXAMPLE --------------------------");
        await ChainOfThoughExample.RunAsync(apiKey);

        Console.WriteLine("---------------------------- TREE OF THOUGHTS EXAMPLE --------------------------");
        await TreeOfThoughsExample.RunAsync(apiKey);

        Console.WriteLine("---------------------------- CUSTOM SCHEMA EXAMPLE --------------------------");
        await CustomSchemaExample.RunAsync(apiKey);
    }
}