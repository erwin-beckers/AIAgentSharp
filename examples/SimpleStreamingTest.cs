using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Fluent;
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
        
        // Configure the agent using the fluent API
        var agent = AIAgent.Create(llm)
            .WithStorage(new MemoryAgentStateStore())
            .WithEventHandling(events => events
                .OnRunStarted(e => Console.WriteLine($"[EVENT] Run started for {e.AgentId} with goal: {e.Goal}"))
                .OnStepStarted(e => Console.WriteLine($"[EVENT] Step {e.TurnIndex + 1} started for {e.AgentId}"))
                .OnLlmCallStarted(e => Console.WriteLine($"[EVENT] LLM call started for {e.AgentId} turn {e.TurnIndex + 1}"))
                .OnToolCallStarted(e => Console.WriteLine($"[EVENT] Tool call started: {e.ToolName} for {e.AgentId} turn {e.TurnIndex + 1}"))
                .OnStepCompleted(e => Console.WriteLine($"[EVENT] Step {e.TurnIndex + 1} completed for {e.AgentId} - Continue: {e.Continue}, Tool: {e.ExecutedTool}"))
                .OnRunCompleted(e => Console.WriteLine($"[EVENT] Run completed for {e.AgentId} - Success: {e.Succeeded}, Turns: {e.TotalTurns}"))
            )
            .Build();

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
