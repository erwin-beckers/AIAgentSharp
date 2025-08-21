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
    public static async Task RunAsync(ILlmClient llm)
    {
        Console.WriteLine("=== AIAgentSharp Advanced Reasoning Example ===\n");

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

        // Track streaming content for each turn
        var streamingContent = new Dictionary<int, string>();

        // Configure the agent using the improved fluent API
        var agent = AIAgent.Create(llm)
            .WithTools(tools)
            .WithStorage(new MemoryAgentStateStore())
            .WithReasoning(ReasoningType.ChainOfThought, options => options
                .SetMaxDepth(8)
            )
            .WithSystemMessage("You are an expert travel analyst with deep knowledge of travel planning and cost optimization. Use systematic reasoning to break down complex travel planning tasks.")
            .WithUserMessage("When analyzing travel options, consider factors like convenience, cost-effectiveness, and traveler preferences. Always explain your reasoning process step by step.")
            .WithMessages(messages => messages
                .AddSystemMessage("Focus on logical step-by-step analysis. Consider multiple alternatives and evaluate trade-offs carefully.")
                .AddAssistantMessage("I will use systematic reasoning to analyze travel planning challenges and provide well-structured solutions.")
            )
            .WithEventHandling(events => events
                .OnRunStarted(e => Console.WriteLine($"Starting: {e.Goal} (Agent: {e.AgentId})"))
                .OnStepStarted(e => Console.WriteLine($"Step {e.TurnIndex + 1} started (Agent: {e.AgentId})"))
                .OnLlmCallStarted(e => 
                {
                    Console.WriteLine($"LLM call started (turn {e.TurnIndex + 1}, Agent: {e.AgentId})");
                })
                .OnLlmChunkReceived(e => 
                {
                    Console.Write(e.Chunk.Content);
                })
                .OnLlmCallCompleted(e => 
                {
                    Console.WriteLine(); // Add newline after streaming completes
                    Console.WriteLine($"LLM call completed (turn {e.TurnIndex + 1}, Agent: {e.AgentId})");
                })
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