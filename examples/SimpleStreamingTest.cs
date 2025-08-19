using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.OpenAI;

namespace example;

internal class SimpleStreamingTest
{
    public static async Task RunAsync(string apiKey)
    {
        var llm = new OpenAiLlmClient(apiKey);
        // Test 2: Agent with function calling disabled
        Console.WriteLine("Streaming example");
        Console.WriteLine("----------------------------------------------");
        
        var store = new MemoryAgentStateStore();
        var config = new AgentConfiguration
        {
            MaxTurns = 2,
            UseFunctionCalling = false, // This should enable streaming
            EmitPublicStatus = true
        };
        
        var agent = new Agent(llm, store, config: config);

        // Subscribe to events to see streaming
        agent.LlmCallStarted += (sender, e) => Console.WriteLine($"[EVENT] LLM call started for turn {e.TurnIndex + 1}");
        agent.LlmCallCompleted += (sender, e) => Console.WriteLine($"[EVENT] LLM call completed for turn {e.TurnIndex + 1}");
        
        // Subscribe to streaming chunks for real-time display
        agent.LlmChunkReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Chunk.Content))
            {
                Console.Write(e.Chunk.Content); // Print streaming content in real-time
            }
        };

        var goal = "Tell me a short story about a cat.";

        Console.WriteLine($"Goal: {goal}");
        Console.WriteLine("Assistant: ");
        
        var result = await agent.RunAsync("streaming-test", goal, new List<ITool>());

        Console.WriteLine();
        Console.WriteLine("=== FINAL RESULT ===");
        Console.WriteLine($"Succeeded: {result.Succeeded}");
        Console.WriteLine($"Final Output: {result.FinalOutput}");
    }
}
