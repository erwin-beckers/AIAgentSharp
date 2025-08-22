using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;

namespace example;

internal class SimpleStreamingTest
{
    public static async Task RunAsync(ILlmClient llm)
    {
        // Test 2: Agent with function calling disabled
        Console.WriteLine("Streaming example");
        Console.WriteLine("----------------------------------------------");
        
        // Configure the agent using the improved fluent API
        var agent = AIAgent.Create(llm)
            .WithStorage(new MemoryAgentStateStore())
            .WithStreaming(true)
            .WithSystemMessage("You are a creative storyteller with a talent for crafting engaging, imaginative narratives. Use vivid descriptions and emotional depth in your stories.")
            .WithUserMessage("When telling stories, create memorable characters, build suspense, and include unexpected twists. Make the story come alive with sensory details.")
            .WithMessages(messages => messages
                .AddSystemMessage("Focus on creating stories that are both entertaining and meaningful. Use humor and heart to connect with the audience.")
                .AddAssistantMessage("I will craft an engaging story with rich characters and an imaginative plot that captures the reader's attention.")
            )
            .WithEventHandling(events => events
                .OnRunStarted(e => Console.WriteLine($"Starting: {e.Goal} (Agent: {e.AgentId})"))
                .OnStepStarted(e => Console.WriteLine($"Step {e.TurnIndex + 1} started (Agent: {e.AgentId})"))
                .OnLlmCallStarted(e => Console.WriteLine($"LLM call started (turn {e.TurnIndex + 1}, Agent: {e.AgentId})"))
                .OnToolCallStarted(e => Console.WriteLine($"Tool call started: {e.ToolName} (turn {e.TurnIndex + 1}, Agent: {e.AgentId})"))
                .OnStepCompleted(e => Console.WriteLine($"Step {e.TurnIndex + 1} completed - Continue: {e.Continue}, Tool: {e.ExecutedTool} (Agent: {e.AgentId})"))
                .OnRunCompleted(e => Console.WriteLine($"Run completed - Success: {e.Succeeded}, Turns: {e.TotalTurns} (Agent: {e.AgentId})"))
                .OnLlmChunkReceived(e => 
                {
                    if (!string.IsNullOrEmpty(e.Chunk.Content))
                    {
                        Console.Write(e.Chunk.Content);
                    }
                })
            )
            .Build();

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
