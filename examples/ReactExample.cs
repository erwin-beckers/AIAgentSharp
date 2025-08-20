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

        // Configure the agent using the fluent API
        var agent = AIAgent.Create(llm)
            .WithTools(tools)
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

        // Subscribe to public status updates
        agent.StatusUpdate += (sender, e) =>
        {
            Console.WriteLine();
            Console.WriteLine($"STATUS UPDATE (Turn {e.TurnIndex + 1}): {e.StatusTitle}");

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
        };

        // Subscribe to LLM call completion events
        agent.LlmCallCompleted += (sender, e) =>
        {
            if (e.Error != null)
            {
                Console.WriteLine($"[EVENT] LLM call failed for {e.AgentId} turn {e.TurnIndex + 1}: {e.Error}");
            }
            else
            {
                Console.WriteLine($"[EVENT] LLM call completed for {e.AgentId} turn {e.TurnIndex + 1} - Action: {e.LlmMessage?.Action}");
            }
        };

        // Subscribe to streaming chunks for real-time display (only works when UseFunctionCalling = false)
        agent.LlmChunkReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Chunk.Content))
            {
                Console.Write(e.Chunk.Content);
            }
        };

        // Subscribe to tool call completion events
        agent.ToolCallCompleted += (sender, e) =>
        {
            if (e.Success)
            {
                Console.WriteLine($"[EVENT] Tool call completed: {e.ToolName} for {e.AgentId} turn {e.TurnIndex + 1} in {e.ExecutionTime.TotalMilliseconds:F0}ms");
            }
            else
            {
                Console.WriteLine($"[EVENT] Tool call failed: {e.ToolName} for {e.AgentId} turn {e.TurnIndex + 1}: {e.Error}");
            }
        };

        // Define the travel planning goal for the agent
        var goal = "Plan a 3-day trip to Paris for a couple with a budget of $2000. " +
                   "Search for flights from JFK to CDG, find a hotel for 3 nights, and suggest activities. " +
                   "Create a complete itinerary with costs and ensure it stays within budget.";

        Console.WriteLine("🌍 AI Travel Planning Agent");
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