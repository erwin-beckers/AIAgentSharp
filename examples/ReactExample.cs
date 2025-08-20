using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Anthropic;
using AIAgentSharp.Examples;
using AIAgentSharp.Fluent;
using AIAgentSharp.Gemini;
using AIAgentSharp.Mistral;
using AIAgentSharp.OpenAI;

namespace example;

internal class ReactExample
{
    public static async Task RunAsync(string apiKey)
    {
        // Create LLM client and travel planning tools
        // var llm = new AnthropicLlmClient(apiKey); // For Anthropic
        // var llm = new GeminiLlmClient(apiKey); // For Google Gemini
        // var llm = new MistralLlmClient(apiKey); // for Mistral
        var llm = new OpenAiLlmClient(apiKey);
        var tools = new List<ITool>
        {
            new SearchFlightsTool(),
            new SearchHotelsTool(),
            new SearchAttractionsTool(),
            new CalculateTripCostTool()
        };

        // Configure the agent using the improved fluent API
        var agent = AIAgent.Create(llm)
            .WithTools(tools)
            .WithStreaming() 
            .WithStorage(new MemoryAgentStateStore())
            .WithEventHandling(events => events
                .OnRunStarted(e => Console.WriteLine($"Starting: {e.Goal} (Agent: {e.AgentId})"))
                .OnStepStarted(e => Console.WriteLine($"Step {e.TurnIndex + 1} started (Agent: {e.AgentId})"))
                .OnLlmCallStarted(e => Console.WriteLine($"LLM call started (turn {e.TurnIndex + 1}, Agent: {e.AgentId})"))
                .OnToolCallStarted(e => Console.WriteLine($"Tool call started: {e.ToolName} (turn {e.TurnIndex + 1}, Agent: {e.AgentId})"))
                .OnStepCompleted(e => Console.WriteLine($"Step {e.TurnIndex + 1} completed - Continue: {e.Continue}, Tool: {e.ExecutedTool} (Agent: {e.AgentId})"))
                .OnRunCompleted(e => Console.WriteLine($"Run completed - Success: {e.Succeeded}, Turns: {e.TotalTurns} (Agent: {e.AgentId})"))
                .OnStatusUpdate(e => 
                {
                    Console.WriteLine();
                    Console.WriteLine($"Status Update (Turn {e.TurnIndex + 1}, Agent: {e.AgentId}): {e.StatusTitle}");

                    if (!string.IsNullOrEmpty(e.StatusDetails))
                    {
                        Console.WriteLine($"   Details: {e.StatusDetails}");
                    }

                    if (!string.IsNullOrEmpty(e.NextStepHint))
                    {
                        Console.WriteLine($"   Next: {e.NextStepHint}");
                    }

                    if (e.ProgressPct.HasValue)
                    {
                        Console.WriteLine($"   Progress: {e.ProgressPct}%");
                    }

                    Console.WriteLine();
                })
                .OnLlmCallCompleted(e => 
                {
                    if (e.Error != null)
                    {
                        Console.WriteLine($"LLM call failed (turn {e.TurnIndex + 1}, Agent: {e.AgentId}): {e.Error}");
                    }
                    else
                    {
                        Console.WriteLine($"LLM call completed (turn {e.TurnIndex + 1}, Agent: {e.AgentId}) - Action: {e.LlmMessage?.Action}");
                    }
                })
                .OnLlmChunkReceived(e => 
                {
                    if (!string.IsNullOrEmpty(e.Chunk.Content))
                    {
                        Console.Write(e.Chunk.Content);
                    }
                })
                .OnToolCallCompleted(e => 
                {
                    if (e.Success)
                    {
                        Console.WriteLine($"Tool call completed: {e.ToolName} (turn {e.TurnIndex + 1}, Agent: {e.AgentId}) in {e.ExecutionTime.TotalMilliseconds:F0}ms");
                    }
                    else
                    {
                        Console.WriteLine($"Tool call failed: {e.ToolName} (turn {e.TurnIndex + 1}, Agent: {e.AgentId}): {e.Error}");
                    }
                })
            )
            .Build();

        // Define the travel planning goal for the agent
        var goal = "Plan a 3-day trip to Paris for a couple with a budget of $2000. " +
                   "Search for flights from JFK to CDG, find a hotel for 3 nights, and suggest activities. " +
                   "Create a complete itinerary with costs and ensure it stays within budget.";

        Console.WriteLine("AI Travel Planning Agent");
        Console.WriteLine("==========================");
        Console.WriteLine($"Goal: {goal}");
        Console.WriteLine();

        // Run the agent
        var result = await agent.RunAsync("travel-planning-session", goal, tools);

        // Display the final results
        Console.WriteLine();
        Console.WriteLine("=== TRAVEL PLANNING RESULT ===");
        Console.WriteLine($"Succeeded: {result.Succeeded}");

        if (!string.IsNullOrEmpty(result.Error))
        {
            Console.WriteLine($"Error:     {result.Error}");
        }
        Console.WriteLine();
        Console.WriteLine("📋 FINAL ITINERARY:");
        Console.WriteLine(result.FinalOutput);
    }
}