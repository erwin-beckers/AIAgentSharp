using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Examples;
using AIAgentSharp.OpenAI;

namespace example;

internal class ReactExample
{
    public static async Task RunAsync(string[] args, string apiKey)
    {
        // Create the appropriate state store
        IAgentStateStore store = new MemoryAgentStateStore();

        // Create LLM client and travel planning tools
        // var llm = new AnthropicLlmClient(apiKey); // For Anthrophic
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

        // Configure the agent
        var config = new AgentConfiguration
        {
            MaxTurns = 40,
            UseFunctionCalling = true,
            EmitPublicStatus = true // Enable public status updates
        };
        var agent = new Agent(llm, store, config: config);

        // Subscribe to events for real-time monitoring
        agent.RunStarted += (sender, e) => Console.WriteLine($"[EVENT] Run started for {e.AgentId} with goal: {e.Goal}");
        agent.StepStarted += (sender, e) => Console.WriteLine($"[EVENT] Step {e.TurnIndex + 1} started for {e.AgentId}");
        agent.LlmCallStarted += (sender, e) => Console.WriteLine($"[EVENT] LLM call started for {e.AgentId} turn {e.TurnIndex + 1}");
        agent.ToolCallStarted += (sender, e) => Console.WriteLine($"[EVENT] Tool call started: {e.ToolName} for {e.AgentId} turn {e.TurnIndex + 1}");
        agent.StepCompleted += (sender, e) => Console.WriteLine($"[EVENT] Step {e.TurnIndex + 1} completed for {e.AgentId} - Continue: {e.Continue}, Tool: {e.ExecutedTool}");
        agent.RunCompleted += (sender, e) => Console.WriteLine($"[EVENT] Run completed for {e.AgentId} - Success: {e.Succeeded}, Turns: {e.TotalTurns}");

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