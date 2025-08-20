using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Examples;
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;

namespace Examples;

/// <summary>
///     Demonstrates the Tree of Thoughts (ToT) reasoning.
/// </summary>
public class TreeOfThoughsExample
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

        // Example 2: Tree of Thoughts Reasoning
        await DemonstrateTreeOfThoughtsReasoning(llm, tools);
    }

    private static async Task DemonstrateTreeOfThoughtsReasoning(ILlmClient llm, List<ITool> tools)
    {
        Console.WriteLine("🌳 Tree of Thoughts (ToT) Reasoning Example");
        Console.WriteLine("This demonstrates branching exploration of multiple solution paths.\n");

        // Configure the agent using the fluent API
        var agent = AIAgent.Create(llm)
            .WithTools(tools)
            .WithStorage(new MemoryAgentStateStore())
            .WithReasoning(ReasoningType.TreeOfThoughts, options => options
                .SetExplorationStrategy(ExplorationStrategy.BestFirst)
                .SetMaxDepth(3)
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

        var goal = @"Design an innovative marketing strategy for a new eco-friendly smartphone with the following constraints:
- Target audience: Tech-savvy millennials and Gen Z
- Budget: $500,000 for initial campaign
- Timeline: 3 months to launch
- Must differentiate from major competitors (Apple, Samsung)
- Should leverage social media and influencer marketing
- Need to emphasize sustainability and environmental impact
- Must be measurable and ROI-focused

Explore multiple approaches and provide the most promising strategy.";

        Console.WriteLine($"Goal: {goal}\n");
        Console.WriteLine("Starting Tree of Thoughts reasoning...\n");

        var result = await agent.RunAsync("tot-marketing-agent", goal, tools);

        Console.WriteLine("\n✅ Tree of Thoughts Reasoning Complete!");
        Console.WriteLine($"Success: {result.Succeeded}");
        Console.WriteLine($"Final Output: {result.FinalOutput}");

        // Display reasoning tree if available
        if (result.State?.CurrentReasoningTree != null)
        {
            Console.WriteLine("\n🌳 Reasoning Tree Analysis:");
            var tree = result.State.CurrentReasoningTree;
            Console.WriteLine($"- Total Nodes Explored: {tree.NodeCount}");
            Console.WriteLine($"- Maximum Depth Reached: {tree.CurrentMaxDepth}");
            Console.WriteLine($"- Best Path Length: {tree.BestPath.Count}");
            Console.WriteLine($"- Exploration Strategy: {tree.ExplorationStrategy}");

            Console.WriteLine("\n🛤️ Best Path Found:");

            foreach (var nodeId in tree.BestPath.Take(3)) // Show first 3 nodes
            {
                if (tree.Nodes.TryGetValue(nodeId, out var node))
                {
                    Console.WriteLine($"  Depth {node.Depth} ({node.ThoughtType}): {node.Thought.Substring(0, Math.Min(80, node.Thought.Length))}...");
                    Console.WriteLine($"    Score: {node.Score:F2}");
                }
            }
        }
    }
}