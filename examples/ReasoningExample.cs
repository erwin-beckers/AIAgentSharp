using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Examples;
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

        // Create state store
        var store = new MemoryAgentStateStore();

        // Create tools
        var tools = new List<ITool>
        {
            new SearchFlightsTool(),
            new SearchHotelsTool(),
            new SearchAttractionsTool(),
            new CalculateTripCostTool()
        };

        // Example 1: Chain of Thought Reasoning
        await DemonstrateChainOfThoughtReasoning(llm, store, tools);
    }

    private static async Task DemonstrateChainOfThoughtReasoning(ILlmClient llm, IAgentStateStore store, List<ITool> tools)
    {
        Console.WriteLine("🔗 Chain of Thought (CoT) Reasoning Example");
        Console.WriteLine("This demonstrates step-by-step reasoning for complex problem solving.\n");

        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.ChainOfThought,
            MaxReasoningSteps = 8,
            EnableReasoningValidation = true,
            MinReasoningConfidence = 0.7,
            MaxTurns = 20,
            UseFunctionCalling = true,
            EmitPublicStatus = true
        };

        var agent = new Agent(llm, store, config: config);

        // Subscribe to events for monitoring
        agent.StatusUpdate += (sender, e) =>
        {
            if (e.StatusTitle?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.WriteLine($"🤔 {e.StatusTitle}: {e.StatusDetails}");
            }
        };

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