# Basic Agent Example

This example demonstrates how to create a simple AI agent that can respond to questions and perform basic tasks.

## Prerequisites

- AIAgentSharp packages installed (see [Installation Guide](../installation.md))
- LLM provider API key configured

## Simple Q&A Agent

Create a basic agent that can answer questions:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Set up your LLM client
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        var llm = new OpenAiLlmClient(apiKey);
        
        // 2. Create a state store
        var store = new MemoryAgentStateStore();
        
        // 3. Create the agent
        var agent = new Agent(llm, store);
        
        // 4. Run the agent
        var result = await agent.RunAsync(
            agentId: "qa-agent",
            goal: "What is the capital of France and what is it known for?",
            tools: new List<ITool>() // No tools for simple Q&A
        );
        
        // 5. Display the result
        Console.WriteLine($"Success: {result.Succeeded}");
        Console.WriteLine($"Response: {result.FinalOutput}");
    }
}
```

## Agent with Event Monitoring

Add real-time monitoring to track agent progress:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        var llm = new OpenAiLlmClient(apiKey);
        var store = new MemoryAgentStateStore();
        var agent = new Agent(llm, store);
        
        // Subscribe to events
        agent.RunStarted += (sender, e) => 
            Console.WriteLine($"🚀 Agent {e.AgentId} started with goal: {e.Goal}");
        
        agent.StatusUpdate += (sender, e) => 
            Console.WriteLine($"📊 Status: {e.StatusTitle} - {e.StatusDetails}");
        
        agent.RunCompleted += (sender, e) => 
            Console.WriteLine($"✅ Agent completed in {e.TotalTurns} turns");
        
        // Run agent
        var result = await agent.RunAsync(
            "monitored-agent",
            "Explain the concept of machine learning in simple terms",
            new List<ITool>()
        );
        
        // Display results
        Console.WriteLine($"\n📝 Final Response:");
        Console.WriteLine(result.FinalOutput);
    }
}
```

## Agent with Configuration

Configure the agent for specific behavior:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;
using AIAgentSharp.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        var llm = new OpenAiLlmClient(apiKey);
        var store = new MemoryAgentStateStore();
        
        // Configuration
        var config = new AgentConfiguration
        {
            MaxTurns = 3,                    // Limit conversation turns
            MaxTokens = 2000,                // Limit response size
            Temperature = 0.7,               // Creativity level
            EmitPublicStatus = true,         // Enable status events
            EnableLoopDetection = true       // Prevent infinite loops
        };
        
        var agent = new Agent(llm, store, config: config);
        
        // Subscribe to events
        agent.StatusUpdate += (sender, e) => 
            Console.WriteLine($"🔄 {e.StatusTitle}: {e.StatusDetails}");
        
        // Run agent
        var result = await agent.RunAsync(
            "configured-agent",
            "Write a short poem about artificial intelligence",
            new List<ITool>()
        );
        
        Console.WriteLine($"\n🎭 AI Poem:");
        Console.WriteLine(result.FinalOutput);
    }
}
```

## Multi-Turn Conversation

Create an agent that maintains conversation context:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        var llm = new OpenAiLlmClient(apiKey);
        var store = new MemoryAgentStateStore();
        var agent = new Agent(llm, store);
        
        // Conversation topics
        var topics = new[]
        {
            "What is artificial intelligence?",
            "How does machine learning work?",
            "What are the main types of machine learning?",
            "Can you give me an example of supervised learning?"
        };
        
        // Run multi-turn conversation
        foreach (var topic in topics)
        {
            Console.WriteLine($"\n🤖 Question: {topic}");
            
            var result = await agent.RunAsync(
                "conversation-agent",
                topic,
                new List<ITool>()
            );
            
            Console.WriteLine($"💬 Answer: {result.FinalOutput}");
            Console.WriteLine(new string('-', 50));
        }
        
        // Display conversation history
        var state = await store.GetStateAsync("conversation-agent");
        if (state?.ConversationHistory != null)
        {
            Console.WriteLine("\n📚 Conversation History:");
            foreach (var turn in state.ConversationHistory)
            {
                Console.WriteLine($"Turn {turn.TurnNumber}:");
                Console.WriteLine($"  Q: {turn.UserInput}");
                Console.WriteLine($"  A: {turn.AgentResponse}");
                Console.WriteLine();
            }
        }
    }
}
```

## Error Handling

Add robust error handling to your agent:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        var llm = new OpenAiLlmClient(apiKey);
        var store = new MemoryAgentStateStore();
        var agent = new Agent(llm, store);
        
        try
        {
            var result = await agent.RunAsync(
                "error-handling-agent",
                "Perform a complex calculation that might fail",
                new List<ITool>()
            );
            
            if (result.Succeeded)
            {
                Console.WriteLine($"✅ Success: {result.FinalOutput}");
            }
            else
            {
                Console.WriteLine($"❌ Agent failed: {result.ErrorMessage}");
                Console.WriteLine($"Error type: {result.ErrorType}");
                
                // Handle specific error types
                switch (result.ErrorType)
                {
                    case AgentErrorType.MaxTurnsExceeded:
                        Console.WriteLine("💡 Try breaking down your request into smaller parts");
                        break;
                    case AgentErrorType.LlmError:
                        Console.WriteLine("💡 Check your API key and network connection");
                        break;
                    case AgentErrorType.ToolError:
                        Console.WriteLine("💡 There was an issue with a tool execution");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Unexpected error: {ex.Message}");
        }
    }
}
```

## Performance Monitoring

Monitor agent performance and metrics:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        var llm = new OpenAiLlmClient(apiKey);
        var store = new MemoryAgentStateStore();
        var agent = new Agent(llm, store);
        
        // Subscribe to metrics updates
        agent.Metrics.MetricsUpdated += (sender, e) =>
        {
            var metrics = e.Metrics;
            Console.WriteLine($"📈 Performance Update:");
            Console.WriteLine($"  Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}");
            Console.WriteLine($"  Average Tokens: {metrics.Resources.AverageTokensPerRun:F0}");
            Console.WriteLine($"  Total Runs: {metrics.Performance.TotalAgentRuns}");
        };
        
        // Run multiple agents to build metrics
        var goals = new[]
        {
            "What is the weather like?",
            "Explain quantum computing",
            "Write a haiku about programming",
            "What are the benefits of exercise?"
        };
        
        foreach (var goal in goals)
        {
            Console.WriteLine($"\n🎯 Running agent with goal: {goal}");
            
            var result = await agent.RunAsync(
                $"agent-{Guid.NewGuid()}",
                goal,
                new List<ITool>()
            );
            
            Console.WriteLine($"Result: {(result.Succeeded ? "✅ Success" : "❌ Failed")}");
        }
        
        // Display final metrics
        var finalMetrics = agent.Metrics.GetMetrics();
        Console.WriteLine($"\n📊 Final Metrics:");
        Console.WriteLine($"  Total Agent Runs: {finalMetrics.Performance.TotalAgentRuns}");
        Console.WriteLine($"  Success Rate: {finalMetrics.Operational.AgentRunSuccessRate:P2}");
        Console.WriteLine($"  Total Tokens Used: {finalMetrics.Resources.TotalTokens:N0}");
        Console.WriteLine($"  Average Tokens per Run: {finalMetrics.Resources.AverageTokensPerRun:F0}");
    }
}
```

## Next Steps

Now that you have a basic understanding of agents, explore:

1. **[Tool Framework](../tool-framework.md)** - Add tools to make your agent more capable
2. **[Reasoning Engines](../reasoning-engines.md)** - Enable advanced reasoning capabilities
3. **[Event System](../event-system.md)** - Learn more about monitoring and events
4. **[Configuration](../configuration.md)** - Explore advanced configuration options

## Troubleshooting

If you encounter issues:

- **API Key Issues**: Ensure your environment variable is set correctly
- **Network Issues**: Check your internet connection and API provider status
- **Rate Limits**: Consider adding delays between requests for high-volume usage

For more help, see the [Troubleshooting Guide](../troubleshooting/common-issues.md).
