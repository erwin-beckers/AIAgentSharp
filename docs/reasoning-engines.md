# Reasoning Engines

AIAgentSharp provides advanced reasoning capabilities through Chain of Thought (CoT) and Tree of Thoughts (ToT) reasoning engines, enabling agents to think through complex problems step-by-step.

## Overview

Reasoning engines allow agents to:

- **Break down complex problems** into manageable steps
- **Evaluate multiple approaches** before making decisions
- **Validate their reasoning** for accuracy and completeness
- **Build confidence** in their conclusions
- **Provide transparency** into the decision-making process

## Reasoning Types

### Chain of Thought (CoT)

Chain of Thought reasoning breaks down problems into sequential steps, with each step building on the previous one.

```csharp
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 5,
    EnableReasoningValidation = true
};

var agent = new Agent(llm, store, config: config);
```

**When to use CoT:**
- Linear problem-solving
- Step-by-step analysis
- Mathematical reasoning
- Logical deduction
- Process planning

### Tree of Thoughts (ToT)

Tree of Thoughts explores multiple solution paths simultaneously, evaluating and pruning branches to find the best approach.

```csharp
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true
};

var agent = new Agent(llm, store, config: config);
```

**When to use ToT:**
- Creative problem-solving
- Multiple solution approaches
- Decision-making with trade-offs
- Complex planning scenarios
- Optimization problems

## Chain of Thought Reasoning

### How It Works

Chain of Thought reasoning follows this process:

1. **Analysis**: Understand the problem and identify key components
2. **Planning**: Create a step-by-step plan to solve the problem
3. **Execution**: Execute each step in sequence
4. **Evaluation**: Assess the quality and completeness of the solution
5. **Validation**: Verify the final answer

### Example

```csharp
var result = await agent.RunAsync(
    "math-agent",
    "If a train travels 120 km in 2 hours, and then 180 km in 3 hours, what is the average speed for the entire journey?",
    tools
);

// The agent will reason through:
// 1. Calculate total distance: 120 + 180 = 300 km
// 2. Calculate total time: 2 + 3 = 5 hours
// 3. Calculate average speed: 300 ÷ 5 = 60 km/h
// 4. Validate: This makes sense as it's between the two speeds
```

### Accessing Reasoning Chain

```csharp
if (result.State?.CurrentReasoningChain != null)
{
    var chain = result.State.CurrentReasoningChain;
    
    Console.WriteLine($"Reasoning Steps: {chain.Steps.Count}");
    Console.WriteLine($"Final Confidence: {chain.FinalConfidence:F2}");
    
    foreach (var step in chain.Steps)
    {
        Console.WriteLine($"Step {step.StepNumber}: {step.StepType}");
        Console.WriteLine($"Reasoning: {step.Reasoning}");
        Console.WriteLine($"Confidence: {step.Confidence:F2}");
        Console.WriteLine($"Insights: {string.Join(", ", step.Insights)}");
        Console.WriteLine();
    }
}
```

## Tree of Thoughts Reasoning

### How It Works

Tree of Thoughts reasoning follows this process:

1. **Exploration**: Generate multiple possible approaches
2. **Evaluation**: Score each approach based on criteria
3. **Pruning**: Remove low-scoring branches
4. **Expansion**: Develop promising branches further
5. **Selection**: Choose the best final approach

### Example

```csharp
var result = await agent.RunAsync(
    "planning-agent",
    "Plan a weekend trip to Paris with a budget of $1000, considering weather, attractions, and transportation",
    tools
);

// The agent will explore multiple approaches:
// Approach 1: Budget hotel + public transport + free attractions
// Approach 2: Mid-range hotel + mix of transport + some paid attractions
// Approach 3: Luxury experience with guided tours
// Then evaluate each based on budget, experience quality, and feasibility
```

### Configuration Options

```csharp
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxReasoningSteps = 15,           // Maximum exploration depth
    EnableReasoningValidation = true, // Validate reasoning quality
    TreeOfThoughtsConfig = new TreeOfThoughtsConfiguration
    {
        MaxBranches = 5,              // Maximum branches to explore
        ExplorationTemperature = 0.8, // Creativity in exploration
        EvaluationTemperature = 0.3   // Consistency in evaluation
    }
};
```

## Reasoning Step Types

Both reasoning engines use these step types:

```csharp
public enum ReasoningStepType
{
    Analysis,      // Understanding the problem
    Planning,      // Creating a solution plan
    Decision,      // Making choices between options
    Observation,   // Gathering information
    Evaluation,    // Assessing solution quality
    Synthesis      // Combining insights
}
```

## Reasoning Validation

Enable reasoning validation to ensure quality:

```csharp
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    EnableReasoningValidation = true,
    MaxReasoningSteps = 5
};
```

Validation checks:
- **Completeness**: All necessary steps completed
- **Logical Flow**: Steps follow logically
- **Accuracy**: Calculations and facts are correct
- **Relevance**: Steps address the original goal

## Custom Reasoning Engines

Create custom reasoning engines by implementing `IReasoningEngine`:

```csharp
public class CustomReasoningEngine : IReasoningEngine
{
    public async Task<ReasoningResult> ReasonAsync(
        string goal,
        string context,
        IDictionary<string, ITool> tools,
        CancellationToken cancellationToken = default)
    {
        // Your custom reasoning logic here
        
        return new ReasoningResult
        {
            Succeeded = true,
            FinalOutput = "Custom reasoning result",
            ReasoningChain = new ReasoningChain
            {
                Steps = new List<ReasoningStep>
                {
                    new ReasoningStep
                    {
                        StepNumber = 1,
                        StepType = ReasoningStepType.Analysis,
                        Reasoning = "Custom analysis step",
                        Confidence = 0.8,
                        Insights = new List<string> { "Custom insight" }
                    }
                },
                FinalConfidence = 0.8
            }
        };
    }
}

// Use custom reasoning engine
var reasoningManager = new ReasoningManager(llm, logger, eventManager, statusManager, metricsCollector);
reasoningManager.RegisterEngine(ReasoningType.Custom, new CustomReasoningEngine());
```

## Monitoring Reasoning

### Event Subscription

```csharp
// Subscribe to reasoning events
agent.ReasoningStarted += (sender, e) =>
{
    Console.WriteLine($"Reasoning started: {e.ReasoningType}");
    Console.WriteLine($"Goal: {e.Goal}");
};

agent.ReasoningCompleted += (sender, e) =>
{
    Console.WriteLine($"Reasoning completed");
    Console.WriteLine($"Steps: {e.ReasoningChain.Steps.Count}");
    Console.WriteLine($"Confidence: {e.ReasoningChain.FinalConfidence:F2}");
};

agent.ReasoningStepCompleted += (sender, e) =>
{
    Console.WriteLine($"Step {e.Step.StepNumber} completed: {e.Step.StepType}");
    Console.WriteLine($"Confidence: {e.Step.Confidence:F2}");
};
```

### Metrics

```csharp
// Access reasoning metrics
var metrics = agent.Metrics.GetMetrics();
Console.WriteLine($"Reasoning Success Rate: {metrics.Reasoning.ReasoningSuccessRate:P2}");
Console.WriteLine($"Average Reasoning Steps: {metrics.Reasoning.AverageReasoningSteps:F1}");
Console.WriteLine($"Average Confidence: {metrics.Reasoning.AverageConfidence:F2}");
```

## Best Practices

### 1. Choose the Right Reasoning Type

```csharp
// For linear problems
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 5
};

// For complex decision-making
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxReasoningSteps = 10
};
```

### 2. Set Appropriate Limits

```csharp
// Balance between thoroughness and performance
var config = new AgentConfiguration
{
    MaxReasoningSteps = 8,  // Not too few, not too many
    EnableReasoningValidation = true
};
```

### 3. Monitor Performance

```csharp
// Track reasoning performance
agent.ReasoningCompleted += (sender, e) =>
{
    if (e.ReasoningChain.FinalConfidence < 0.7)
    {
        Console.WriteLine("Warning: Low confidence reasoning");
    }
    
    if (e.ReasoningChain.Steps.Count > 10)
    {
        Console.WriteLine("Warning: Many reasoning steps");
    }
};
```

### 4. Validate Results

```csharp
// Always check reasoning quality
if (result.State?.CurrentReasoningChain != null)
{
    var chain = result.State.CurrentReasoningChain;
    
    if (chain.FinalConfidence < 0.6)
    {
        // Consider re-running with different parameters
        Console.WriteLine("Low confidence result, consider retry");
    }
}
```

## Troubleshooting

### Common Issues

**Low confidence**: Increase reasoning steps or enable validation.

**Too many steps**: Reduce `MaxReasoningSteps` or use simpler reasoning.

**Inconsistent results**: Enable reasoning validation and check LLM model.

**Slow performance**: Use faster LLM models or reduce reasoning complexity.

### Performance Optimization

```csharp
// Optimize for speed
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 3,
    EnableReasoningValidation = false
};

// Optimize for quality
var config = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxReasoningSteps = 15,
    EnableReasoningValidation = true
};
```

For more troubleshooting help, see the [Troubleshooting Guide](troubleshooting/common-issues.md).
