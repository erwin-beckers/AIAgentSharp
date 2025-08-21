using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Examples;
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;

namespace example;

/// <summary>
///     Demonstrates the Hybrid reasoning capabilities of AIAgentSharp, which combines
///     Chain of Thought and Tree of Thoughts approaches for comprehensive problem-solving.
/// </summary>
public class HybridReasoningExample
{
    public static async Task RunAsync(ILlmClient llm)
    {
        Console.WriteLine("=== AIAgentSharp Hybrid Reasoning Example ===\n");

        // Create tools
        var tools = new List<ITool>
        {
            new SearchFlightsTool(),
            new SearchHotelsTool(),
            new SearchAttractionsTool(),
            new CalculateTripCostTool()
        };

        // Demonstrate Hybrid Reasoning
        await DemonstrateHybridReasoning(llm, tools);
    }

    private static async Task DemonstrateHybridReasoning(ILlmClient llm, List<ITool> tools)
    {
        Console.WriteLine("🔄 Hybrid Reasoning Example");
        Console.WriteLine("This demonstrates combined Chain of Thought and Tree of Thoughts reasoning.\n");

        // Configure the agent using the fluent API
        var agent = AIAgent.Create(llm)
            .WithTools(tools)
            .WithStorage(new MemoryAgentStateStore())
            .WithReasoning(ReasoningType.Hybrid, options => options
                .SetMaxDepth(3)
                .SetMaxTreeNodes(20)
                .SetExplorationStrategy(ExplorationStrategy.BestFirst)
            )
            .WithSystemMessage("You are an expert strategic consultant with deep knowledge of business strategy, market analysis, and competitive positioning. Use hybrid reasoning to provide comprehensive strategic insights.")
            .WithUserMessage("When analyzing strategic challenges, combine systematic analysis with creative exploration. Consider both structured frameworks and innovative approaches.")
            .WithMessages(messages => messages
                .AddSystemMessage("Use Chain of Thought for systematic analysis and Tree of Thoughts for exploring innovative solutions.")
                .AddAssistantMessage("I will combine structured analysis with creative exploration to provide comprehensive strategic insights.")
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
            )
            .Build();

        var goal = @"Develop a comprehensive strategic plan for a mid-sized technology company looking to expand into the European market. The company specializes in AI-powered customer service solutions and currently operates only in North America.

Key considerations:
- Market entry strategy (direct vs. partnerships vs. acquisitions)
- Competitive landscape analysis
- Regulatory compliance requirements (GDPR, etc.)
- Resource allocation and timeline
- Risk assessment and mitigation strategies
- Success metrics and KPIs

Use hybrid reasoning to provide both systematic analysis and creative strategic options.";

        Console.WriteLine($"Goal: {goal}\n");
        Console.WriteLine("Starting Hybrid reasoning...\n");

        var result = await agent.RunAsync("hybrid-strategy-agent", goal, tools);

        Console.WriteLine("\n✅ Hybrid Reasoning Complete!");
        Console.WriteLine($"Success: {result.Succeeded}");
        Console.WriteLine($"Final Output: {result.FinalOutput}");

        // Display reasoning insights if available
        if (result.State?.CurrentReasoningChain != null)
        {
            Console.WriteLine("\n🔗 Chain of Thought Analysis:");
            var chain = result.State.CurrentReasoningChain;
            Console.WriteLine($"- Total Steps: {chain.Steps.Count}");
            Console.WriteLine($"- Final Confidence: {chain.FinalConfidence:F2}");
            Console.WriteLine($"- Conclusion: {chain.FinalConclusion}");
        }

        if (result.State?.CurrentReasoningTree != null)
        {
            Console.WriteLine("\n🌳 Tree of Thoughts Exploration:");
            var tree = result.State.CurrentReasoningTree;
            Console.WriteLine($"- Total Nodes Explored: {tree.NodeCount}");
            Console.WriteLine($"- Maximum Depth Reached: {tree.CurrentMaxDepth}");
            Console.WriteLine($"- Exploration Strategy: {tree.ExplorationStrategy}");
        }

        // Display hybrid-specific metadata
        if (result.State?.ReasoningMetadata != null && result.State.ReasoningMetadata.ContainsKey("method"))
        {
            Console.WriteLine("\n🔄 Hybrid Reasoning Metadata:");
            foreach (var kvp in result.State.ReasoningMetadata)
            {
                Console.WriteLine($"- {kvp.Key}: {kvp.Value}");
            }
        }

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("Hybrid reasoning combines the systematic approach of Chain of Thought");
        Console.WriteLine("with the exploratory capabilities of Tree of Thoughts for comprehensive");
        Console.WriteLine("problem-solving and strategic analysis.");
        Console.WriteLine(new string('=', 80));
    }
}
