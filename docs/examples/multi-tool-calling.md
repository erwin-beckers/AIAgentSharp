# Multi-Tool Calling Guide

AIAgentSharp supports advanced multi-tool calling, allowing the LLM to execute multiple tools in a single response. This feature dramatically improves efficiency by reducing the number of LLM round-trips required for complex tasks.

## 🎯 Overview

Multi-tool calling allows the LLM to:
- Call multiple tools simultaneously in one response
- Reduce latency by minimizing LLM round-trips
- Execute parallel operations efficiently
- Handle complex workflows in fewer steps

## 🚀 Basic Usage

### Setting Up an Agent with Multiple Tools

```csharp
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;

var agent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(
        new SearchFlightsTool(),
        new SearchHotelsTool(),
        new SearchAttractionsTool(),
        new CalculateTripCostTool()
    )
    .WithReasoning(ReasoningType.ChainOfThought)
    .WithStorage(new MemoryAgentStateStore())
    .Build();
```

### Executing Multi-Tool Calls

```csharp
var result = await agent.RunAsync("travel-agent", 
    "Plan a 5-day business trip to Tokyo for 3 people with a $8000 budget", 
    agent.Tools);

// The agent might execute multiple tools in a single turn:
// 1. SearchFlightsTool
// 2. SearchHotelsTool
// 3. SearchAttractionsTool
// All in one LLM response!
```

## 📋 How It Works

### LLM Response Format

When the LLM decides to call multiple tools, it returns a JSON response with this structure:

```json
{
  "thoughts": "I need to search for flights, hotels, and activities to plan this trip",
  "action": "multi_tool_call",
  "action_input": {
    "tool_calls": [
      {
        "tool": "search_flights",
        "params": {
          "departureDate": "2024-05-01",
          "destination": "Tokyo",
          "origin": "New York",
          "passengers": 3
        }
      },
      {
        "tool": "search_hotels",
        "params": {
          "checkInDate": "2024-05-01",
          "checkOutDate": "2024-05-06",
          "city": "Tokyo",
          "guests": 3
        }
      },
      {
        "tool": "search_attractions",
        "params": {
          "category": "team building",
          "city": "Tokyo",
          "maxPrice": 200
        }
      }
    ]
  }
}
```

### Execution Flow

1. **LLM Analysis**: The LLM analyzes the task and determines which tools to call
2. **Multi-Tool Response**: LLM returns a `multi_tool_call` action with multiple `tool_calls`
3. **Parallel Execution**: AIAgentSharp executes all tools (can be done in parallel)
4. **Result Aggregation**: All tool results are collected and provided to the LLM
5. **Continuation**: The LLM uses all results to continue or complete the task

## 🔧 Advanced Examples

### Travel Planning with Multi-Tool Calling

```csharp
using AIAgentSharp.Fluent;
using AIAgentSharp.OpenAI;
using AIAgentSharp.Examples;

// Create a comprehensive travel planning agent
var travelAgent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(tools => tools
        .Add(new SearchFlightsTool())
        .Add(new SearchHotelsTool())
        .Add(new SearchAttractionsTool())
        .Add(new CalculateTripCostTool())
        .Add(new SearchRestaurantsTool())
    )
    .WithReasoning(ReasoningType.ChainOfThought, options => options
        .SetMaxDepth(10)
    )
    .WithEventHandling(events => events
        .OnToolCallStarted(e => Console.WriteLine($"🔧 Executing: {e.ToolName}"))
        .OnStepCompleted(e => Console.WriteLine($"✅ Step completed with {e.ToolCallResults?.Count ?? 0} tools"))
    )
    .Build();

// Complex planning request that will trigger multi-tool calling
var goal = @"Plan a comprehensive 5-day business trip to Tokyo for a team of 3 people:
- Budget: $8000 total
- Must include 2 business meetings in different locations
- Team members have dietary restrictions (vegetarian, gluten-free, seafood allergy)
- Need to accommodate different arrival times
- Must include team building activities
- Should optimize for productivity and cost-effectiveness";

var result = await travelAgent.RunAsync("travel-planner", goal, travelAgent.Tools);
```

### E-commerce Multi-Tool Example

```csharp
var ecommerceAgent = AIAgent.Create(new OpenAiLlmClient(apiKey))
    .WithTools(
        new SearchProductsTool(),
        new CheckInventoryTool(),
        new GetPricingTool(),
        new CalculateShippingTool(),
        new ApplyDiscountsTool()
    )
    .Build();

// Request that triggers multiple tools
var result = await ecommerceAgent.RunAsync("shopping-assistant",
    "Find me a laptop under $1500, check if it's in stock, calculate shipping to New York, and apply any available discounts",
    ecommerceAgent.Tools);
```

## 📊 Monitoring Multi-Tool Execution

### Event Handling

```csharp
var agent = AIAgent.Create(llm)
    .WithTools(tools)
    .WithEventHandling(events => events
        .OnStepStarted(e => Console.WriteLine($"Step {e.TurnIndex + 1} started"))
        .OnToolCallStarted(e => Console.WriteLine($"🔧 Tool: {e.ToolName}"))
        .OnToolCallCompleted(e => 
        {
            Console.WriteLine($"✅ Tool completed: {e.ToolName}");
            if (e.Error != null)
                Console.WriteLine($"❌ Error: {e.Error}");
        })
        .OnStepCompleted(e => 
        {
            var toolCount = e.ToolCallResults?.Count ?? 0;
            Console.WriteLine($"📋 Step completed with {toolCount} tools executed");
            
            // Log each tool result
            if (e.ToolCallResults != null)
            {
                foreach (var toolResult in e.ToolCallResults)
                {
                    Console.WriteLine($"   - {toolResult.ToolName}: {(toolResult.IsSuccess ? "Success" : "Failed")}");
                }
            }
        })
    )
    .Build();
```

### Accessing Multi-Tool Results

```csharp
var result = await agent.RunAsync("agent-id", "Your complex goal", tools);

// Access the current turn's tool execution results
if (result.State?.CurrentTurn?.ToolExecutionResults != null)
{
    Console.WriteLine($"Tools executed in final turn: {result.State.CurrentTurn.ToolExecutionResults.Count}");
    
    foreach (var toolResult in result.State.CurrentTurn.ToolExecutionResults)
    {
        Console.WriteLine($"Tool: {toolResult.ToolName}");
        Console.WriteLine($"Success: {toolResult.IsSuccess}");
        Console.WriteLine($"Result: {toolResult.Result}");
        if (toolResult.Error != null)
            Console.WriteLine($"Error: {toolResult.Error}");
    }
}

// Access metrics for the entire run
var metrics = agent.Metrics.GetMetrics();
Console.WriteLine($"Total tool calls: {metrics.Performance.TotalToolCalls}");
Console.WriteLine($"Tool success rate: {metrics.Operational.ToolCallSuccessRate:P2}");
```

## 🎯 Best Practices

### 1. Design Tools for Parallelization

```csharp
// Good - Independent tools that can run in parallel
public class SearchFlightsTool : BaseTool<FlightParams, FlightResult> { }
public class SearchHotelsTool : BaseTool<HotelParams, HotelResult> { }
public class GetWeatherTool : BaseTool<WeatherParams, WeatherResult> { }

// Less ideal - Tools that depend on each other
public class GetUserIdTool : BaseTool<UserParams, string> { }
public class GetUserOrdersTool : BaseTool<UserIdParams, Order[]> { } // Depends on GetUserIdTool
```

### 2. Handle Tool Dependencies

For tools that depend on each other, the LLM will typically call them in separate turns:

```csharp
// Turn 1: Get user information
// Turn 2: Use user ID to get orders (after receiving result from Turn 1)
var result = await agent.RunAsync("order-agent", 
    "Get all orders for user john.doe@email.com", 
    new[] { new GetUserIdTool(), new GetUserOrdersTool() });
```

### 3. Optimize for Multi-Tool Scenarios

```csharp
// Provide clear, comprehensive goals that benefit from multi-tool calling
var goal = @"Research and compare these 3 products:
1. iPhone 15 Pro
2. Samsung Galaxy S24
3. Google Pixel 8
Include pricing, availability, reviews, and shipping options for each.";

// This will likely trigger:
// - SearchProductTool for iPhone 15 Pro
// - SearchProductTool for Samsung Galaxy S24  
// - SearchProductTool for Google Pixel 8
// All in a single LLM response
```

### 4. Error Handling

```csharp
.WithEventHandling(events => events
    .OnToolCallCompleted(e => 
    {
        if (e.Error != null)
        {
            Console.WriteLine($"❌ Tool {e.ToolName} failed: {e.Error}");
            // The agent will continue with other tools and adapt
        }
    })
)
```

## 🔍 Troubleshooting

### Common Issues

1. **Tools Not Called Together**: Ensure tools are independent and can run in parallel
2. **JSON Parsing Errors**: Verify your LLM supports the multi-tool calling format
3. **Performance Issues**: Monitor tool execution times and optimize slow tools

### Debugging

Enable detailed logging to see multi-tool execution:

```csharp
var config = new AgentConfiguration
{
    EmitPublicStatus = true,
    EnableDetailedLogging = true
};

// Or with fluent API:
var agent = AIAgent.Create(llm)
    .WithConfig(config)
    .WithTools(tools)
    .Build();
```

## 🚀 Benefits

- **Reduced Latency**: Fewer LLM round-trips for complex tasks
- **Improved Efficiency**: Parallel tool execution when possible
- **Better User Experience**: Faster task completion
- **Cost Optimization**: Fewer LLM API calls
- **Enhanced Reasoning**: LLM can see all tool results together for better decision making

## 🔗 Related Documentation

- [Tool Framework](../tool-framework.md) - Creating and configuring tools
- [Fluent API](../fluent-api.md) - Modern agent configuration
- [Event System](../event-system.md) - Monitoring and debugging
- [Performance Best Practices](../best-practices/performance.md) - Optimizing agent performance
