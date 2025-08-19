# Quick Start Guide

Get up and running with AIAgentSharp in minutes! This guide will walk you through creating your first AI agent.

## Prerequisites

- .NET 8.0 or later installed
- AIAgentSharp packages installed (see [Installation Guide](installation.md))
- LLM provider API key configured

## Your First Agent

Let's create a simple agent that can respond to questions:

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
        
        // 2. Create a state store (for persistence)
        var store = new MemoryAgentStateStore();
        
        // 3. Create the agent
        var agent = new Agent(llm, store);
        
        // 4. Run the agent
        var result = await agent.RunAsync(
            agentId: "my-first-agent",
            goal: "What is the capital of France?",
            tools: new List<ITool>() // No tools for this simple example
        );
        
        // 5. Display the result
        Console.WriteLine($"Success: {result.Succeeded}");
        Console.WriteLine($"Response: {result.FinalOutput}");
    }
}
```

## Adding Tools

Tools allow your agent to perform actions. Here's how to add a simple calculator tool:

```csharp
using AIAgentSharp.Tools;
using System.ComponentModel.DataAnnotations;

// Define the tool parameters
[ToolParams(Description = "Calculator parameters")]
public sealed class CalculatorParams
{
    [ToolField(Description = "First number", Required = true)]
    [Required]
    public double A { get; set; }
    
    [ToolField(Description = "Second number", Required = true)]
    [Required]
    public double B { get; set; }
    
    [ToolField(Description = "Operation to perform", Required = true)]
    [Required]
    public string Operation { get; set; } = default!;
}

// Create the tool
public sealed class CalculatorTool : BaseTool<CalculatorParams, object>
{
    public override string Name => "calculator";
    public override string Description => "Perform basic arithmetic operations";

    protected override async Task<object> InvokeTypedAsync(CalculatorParams parameters, CancellationToken ct = default)
    {
        return parameters.Operation.ToLower() switch
        {
            "add" => parameters.A + parameters.B,
            "subtract" => parameters.A - parameters.B,
            "multiply" => parameters.A * parameters.B,
            "divide" => parameters.A / parameters.B,
            _ => throw new ArgumentException($"Unknown operation: {parameters.Operation}")
        };
    }
}

// Use the tool with your agent
var tools = new List<ITool> { new CalculatorTool() };
var result = await agent.RunAsync(
    "calculator-agent",
    "What is 15 plus 27?",
    tools
);
```

## Adding Event Monitoring

Monitor your agent's progress in real-time:

```csharp
// Subscribe to events
agent.StatusUpdate += (sender, e) => 
    Console.WriteLine($"Status: {e.StatusTitle} - {e.StatusDetails}");

agent.ToolCallStarted += (sender, e) => 
    Console.WriteLine($"Tool called: {e.ToolName}");

agent.RunCompleted += (sender, e) => 
    Console.WriteLine($"Agent completed in {e.TotalTurns} turns");

// Run the agent
var result = await agent.RunAsync("monitored-agent", "Your goal here", tools);
```

## Using Different LLM Providers

AIAgentSharp supports multiple LLM providers:

```csharp
// OpenAI
using AIAgentSharp.OpenAI;
var llm = new OpenAiLlmClient(apiKey);

// Anthropic Claude
using AIAgentSharp.Anthropic;
var llm = new AnthropicLlmClient(apiKey);

// Google Gemini
using AIAgentSharp.Gemini;
var llm = new GeminiLlmClient(apiKey);

// Mistral AI
using AIAgentSharp.Mistral;
var llm = new MistralLlmClient(apiKey);
```

## Advanced Configuration

Configure your agent with advanced options:

```csharp
var config = new AgentConfiguration
{
    MaxTurns = 10,                    // Maximum conversation turns
    ReasoningType = ReasoningType.ChainOfThought,  // Enable reasoning
    MaxReasoningSteps = 5,            // Maximum reasoning steps
    EnableReasoningValidation = true, // Validate reasoning
    UseFunctionCalling = true,        // Enable function calling
    EmitPublicStatus = true,          // Emit status events
    MaxTokens = 4000,                 // Maximum tokens per response
    Temperature = 0.7                 // Response creativity (0.0-1.0)
};

var agent = new Agent(llm, store, config: config);
```

## Complete Example

Here's a complete example combining all the concepts:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;
using AIAgentSharp.Tools;
using AIAgentSharp.Configuration;
using System.ComponentModel.DataAnnotations;

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
            MaxTurns = 5,
            ReasoningType = ReasoningType.ChainOfThought,
            UseFunctionCalling = true,
            EmitPublicStatus = true
        };
        
        // Create agent
        var agent = new Agent(llm, store, config: config);
        
        // Subscribe to events
        agent.StatusUpdate += (sender, e) => 
            Console.WriteLine($"🔄 {e.StatusTitle}: {e.StatusDetails}");
        
        agent.ToolCallStarted += (sender, e) => 
            Console.WriteLine($"🔧 Using tool: {e.ToolName}");
        
        agent.RunCompleted += (sender, e) => 
            Console.WriteLine($"✅ Completed in {e.TotalTurns} turns");
        
        // Create tools
        var tools = new List<ITool> { new CalculatorTool() };
        
        // Run agent
        Console.WriteLine("🤖 Starting AI Agent...");
        var result = await agent.RunAsync(
            "demo-agent",
            "Calculate the area of a circle with radius 5, then add 10 to the result",
            tools
        );
        
        // Display results
        Console.WriteLine($"\n📊 Results:");
        Console.WriteLine($"Success: {result.Succeeded}");
        Console.WriteLine($"Final Output: {result.FinalOutput}");
        
        if (result.State?.CurrentReasoningChain != null)
        {
            var chain = result.State.CurrentReasoningChain;
            Console.WriteLine($"Reasoning Steps: {chain.Steps.Count}");
            Console.WriteLine($"Confidence: {chain.FinalConfidence:F2}");
        }
    }
}

// Calculator tool implementation (from above)
[ToolParams(Description = "Calculator parameters")]
public sealed class CalculatorParams
{
    [ToolField(Description = "First number", Required = true)]
    [Required]
    public double A { get; set; }
    
    [ToolField(Description = "Second number", Required = true)]
    [Required]
    public double B { get; set; }
    
    [ToolField(Description = "Operation to perform", Required = true)]
    [Required]
    public string Operation { get; set; } = default!;
}

public sealed class CalculatorTool : BaseTool<CalculatorParams, object>
{
    public override string Name => "calculator";
    public override string Description => "Perform basic arithmetic operations";

    protected override async Task<object> InvokeTypedAsync(CalculatorParams parameters, CancellationToken ct = default)
    {
        return parameters.Operation.ToLower() switch
        {
            "add" => parameters.A + parameters.B,
            "subtract" => parameters.A - parameters.B,
            "multiply" => parameters.A * parameters.B,
            "divide" => parameters.A / parameters.B,
            _ => throw new ArgumentException($"Unknown operation: {parameters.Operation}")
        };
    }
}
```

## Next Steps

Now that you have a basic understanding, explore:

1. **[Tool Framework](tool-framework.md)** - Learn to create more sophisticated tools
2. **[Reasoning Engines](reasoning-engines.md)** - Understand Chain of Thought and Tree of Thoughts
3. **[Examples](examples/)** - See real-world usage patterns
4. **[API Reference](api/)** - Detailed API documentation

## Troubleshooting

If you encounter issues:

- **API Key Issues**: Ensure your environment variable is set correctly
- **Package Issues**: Verify all AIAgentSharp packages are the same version
- **LLM Errors**: Check your API key and provider status

For more help, see the [Troubleshooting Guide](troubleshooting/common-issues.md).
