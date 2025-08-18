using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.Interfaces;
using AIAgentSharp.Examples;
using AIAgentSharp.OpenAI;

namespace Examples;

/// <summary>
/// Demonstrates the advanced reasoning capabilities of AIAgentSharp including
/// Chain of Thought (CoT) and Tree of Thoughts (ToT) reasoning.
/// </summary>
public class ReasoningExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== AIAgentSharp Advanced Reasoning Example ===\n");

        // Get API key from environment
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Please set OPENAI_API_KEY environment variable");
            return;
        }

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

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // Example 2: Tree of Thoughts Reasoning
        await DemonstrateTreeOfThoughtsReasoning(llm, store, tools);

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // Example 3: Hybrid Reasoning
        await DemonstrateHybridReasoning(llm, store, tools);
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

        Console.WriteLine($"\n✅ Chain of Thought Reasoning Complete!");
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

    private static async Task DemonstrateTreeOfThoughtsReasoning(ILlmClient llm, IAgentStateStore store, List<ITool> tools)
    {
        Console.WriteLine("🌳 Tree of Thoughts (ToT) Reasoning Example");
        Console.WriteLine("This demonstrates branching exploration of multiple solution paths.\n");

        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.TreeOfThoughts,
            MaxTreeDepth = 6,
            MaxTreeNodes = 80,
            TreeExplorationStrategy = ExplorationStrategy.BestFirst,
            EnableReasoningValidation = true,
            MinReasoningConfidence = 0.6,
            MaxTurns = 25,
            UseFunctionCalling = true,
            EmitPublicStatus = true
        };

        var agent = new Agent(llm, store, config: config);

        // Subscribe to events for monitoring
        agent.StatusUpdate += (sender, e) =>
        {
            if (e.StatusTitle?.Contains("exploring", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.WriteLine($"🌱 {e.StatusTitle}: {e.StatusDetails}");
            }
        };

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

        Console.WriteLine($"\n✅ Tree of Thoughts Reasoning Complete!");
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

    private static async Task DemonstrateHybridReasoning(ILlmClient llm, IAgentStateStore store, List<ITool> tools)
    {
        Console.WriteLine("🔄 Hybrid Reasoning Example");
        Console.WriteLine("This demonstrates combining multiple reasoning approaches for optimal results.\n");

        var config = new AgentConfiguration
        {
            ReasoningType = ReasoningType.Hybrid,
            MaxReasoningSteps = 6,
            MaxTreeDepth = 4,
            MaxTreeNodes = 50,
            TreeExplorationStrategy = ExplorationStrategy.BeamSearch,
            EnableReasoningValidation = true,
            MinReasoningConfidence = 0.65,
            MaxTurns = 30,
            UseFunctionCalling = true,
            EmitPublicStatus = true
        };

        var agent = new Agent(llm, store, config: config);

        // Subscribe to events for monitoring
        agent.StatusUpdate += (sender, e) =>
        {
            if (e.StatusTitle?.Contains("reasoning", StringComparison.OrdinalIgnoreCase) == true ||
                e.StatusTitle?.Contains("exploring", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.WriteLine($"🔄 {e.StatusTitle}: {e.StatusDetails}");
            }
        };

        var goal = @"Develop a comprehensive solution for a startup facing multiple challenges:
- Rapidly growing user base (10,000+ users) but limited server capacity
- Need to implement new features while maintaining system stability
- Budget constraints: $50,000 for infrastructure improvements
- Team of 5 developers with varying skill levels
- Must maintain 99.9% uptime during transition
- Should plan for future scalability (100,000+ users)

Provide a detailed technical and business strategy that addresses all concerns.";

        Console.WriteLine($"Goal: {goal}\n");
        Console.WriteLine("Starting Hybrid reasoning...\n");

        var result = await agent.RunAsync("hybrid-startup-agent", goal, tools);

        Console.WriteLine($"\n✅ Hybrid Reasoning Complete!");
        Console.WriteLine($"Success: {result.Succeeded}");
        Console.WriteLine($"Final Output: {result.FinalOutput}");

        // Display hybrid reasoning results
        if (result.State?.ReasoningMetadata != null)
        {
            Console.WriteLine("\n🔄 Hybrid Reasoning Analysis:");
            var metadata = result.State.ReasoningMetadata;
            
            if (metadata.TryGetValue("reasoning_type", out var reasoningType))
                Console.WriteLine($"- Reasoning Type: {reasoningType}");
            
            if (metadata.TryGetValue("approaches_used", out var approaches))
                Console.WriteLine($"- Approaches Used: {approaches}");
            
            if (metadata.TryGetValue("steps_completed", out var steps))
                Console.WriteLine($"- CoT Steps Completed: {steps}");
            
            if (metadata.TryGetValue("nodes_explored", out var nodes))
                Console.WriteLine($"- ToT Nodes Explored: {nodes}");
        }
    }
}
