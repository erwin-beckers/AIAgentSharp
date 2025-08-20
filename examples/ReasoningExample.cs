using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Examples;
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;

namespace example;

/// <summary>
///     Demonstrates the advanced reasoning capabilities of AIAgentSharp including
///     Chain of Thought (CoT) and Tree of Thoughts (ToT) reasoning.
/// </summary>
public class ChainOfThoughExample
{
    public static async Task RunAsync(string apiKey)
    {
        Console.WriteLine("=== AIAgentSharp Advanced Reasoning Example ===\n");

        // Create LLM client
        var llm = new OpenAiLlmClient(apiKey);

        // Create tools
        var tools = new List<ITool>
        {
            new SearchFlightsTool(),
            new SearchHotelsTool(),
            new SearchAttractionsTool(),
            new CalculateTripCostTool()
        };

        // Example 1: Chain of Thought Reasoning
        await DemonstrateChainOfThoughtReasoning(llm, tools);
    }

    private static async Task DemonstrateChainOfThoughtReasoning(ILlmClient llm, List<ITool> tools)
    {
        Console.WriteLine("🔗 Chain of Thought (CoT) Reasoning Example");
        Console.WriteLine("This demonstrates step-by-step reasoning for complex problem solving.\n");

        // Configure the agent using the fluent API
        var agent = AIAgent.Create(llm)
            .WithTools(tools)
            .WithStorage(new MemoryAgentStateStore())
            .WithReasoning(ReasoningType.ChainOfThought, options => options
                .SetMaxDepth(8)
            )
            .WithEventHandling(events => events
                .OnRunStarted(e => Console.WriteLine($"[EVENT] Run started for {e.AgentId} with goal: {e.Goal}"))
                .OnStepStarted(e => Console.WriteLine($"[EVENT] Step {e.TurnIndex + 1} started for {e.AgentId}"))
                .OnLlmCallStarted(e => Console.WriteLine($"[EVENT] LLM call started for {e.AgentId} turn {e.TurnIndex + 1}"))
                .OnToolCallStarted(e => Console.WriteLine($"[EVENT] Tool call started: {e.ToolName} for {e.AgentId} turn {e.TurnIndex + 1}"))
                .OnStepCompleted(e => Console.WriteLine($"[EVENT] Step {e.TurnIndex + 1} completed for {e.AgentId} - Continue: {e.Continue}, Tool: {e.ExecutedTool}"))
                .OnRunCompleted(e => Console.WriteLine($"[EVENT] Run completed for {e.AgentId} - Success: {e.Succeeded}, Turns: {e.TotalTurns}"))
            )
            .Build();

        var goal = @"Plan a complex 5-day business trip to Tokyo for a team of 3 people with the following requirements:
- Budget: $8000 total
- Must include 2 business meetings in different locations
- Team members have different dietary restrictions (vegetarian, gluten-free, seafood allergy)
- Need to accommodate different arrival times
- Must include team building activities
- Should optimize for productivity and cost-effectiveness

Please provide a detailed plan with specific recommendations.";

        Console.WriteLine($"Goal: {goal}\n");
        Console.WriteLine("Starting Chain of Thought reasoning...\n");

        var result = await agent.RunAsync("cot-travel-agent", goal, tools);

        Console.WriteLine("\n✅ Chain of Thought Reasoning Complete!");
        Console.WriteLine($"Success: {result.Succeeded}");
        Console.WriteLine($"Final Output: {result.FinalOutput}");

        // Display reasoning chain if available
        if (result.State?.CurrentReasoningChain != null)
        {
            Console.WriteLine("\n📋 Reasoning Chain Analysis:");
            var chain = result.State.CurrentReasoningChain;
            Console.WriteLine($"- Total Steps: {chain.Steps.Count}");
            Console.WriteLine($"- Final Confidence: {chain.FinalConfidence:F2}");
            Console.WriteLine($"- Total Execution Time: {chain.TotalExecutionTimeMs}ms");

            Console.WriteLine("\n🔍 Key Reasoning Steps:");

            foreach (var step in chain.Steps.Take(3)) // Show first 3 steps
            {
                Console.WriteLine($"  Step {step.StepNumber} ({step.StepType}): {step.Reasoning.Substring(0, Math.Min(100, step.Reasoning.Length))}...");
                Console.WriteLine($"    Confidence: {step.Confidence:F2}");
            }
        }
    }
}