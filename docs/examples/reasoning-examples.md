# Reasoning Examples

This guide demonstrates how to use AIAgentSharp's advanced reasoning capabilities, including Chain of Thought (CoT) and Tree of Thoughts (ToT) reasoning engines.

## Overview

AIAgentSharp provides two powerful reasoning engines:

- **Chain of Thought (CoT)**: Sequential reasoning that breaks down complex problems into logical steps
- **Tree of Thoughts (ToT)**: Parallel exploration of multiple reasoning paths to find optimal solutions

## Chain of Thought Examples

### Basic Chain of Thought

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.Agents.ChainOfThought;
using AIAgentSharp.Configuration;
using AIAgentSharp.Llm;
using AIAgentSharp.OpenAI;

// Configure the agent with Chain of Thought reasoning
var config = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    MaxChainSteps = 5,
    ChainStepTimeout = TimeSpan.FromSeconds(30)
};

var agent = new Agent(config, llmClient);

// Example: Mathematical problem solving
var result = await agent.RunAsync("Solve: If a train travels 120 km in 2 hours, what is its speed in km/h?");
```

### Multi-Step Problem Solving

```csharp
// Complex problem requiring multiple reasoning steps
var complexProblem = @"
A company has 3 departments:
- Engineering: 40 employees, average salary $80,000
- Sales: 25 employees, average salary $60,000  
- Marketing: 15 employees, average salary $70,000

What is the total annual payroll cost for the company?
If the company wants to give everyone a 5% raise, what will be the new total cost?
";

var result = await agent.RunAsync(complexProblem);
```

### Chain of Thought with Custom Prompts

```csharp
// Custom chain prompt for specific domain
var customConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    ChainPromptTemplate = @"
You are a financial analyst. When solving problems:
1. First, identify the key financial metrics involved
2. Break down the calculation into clear steps
3. Show your work for each step
4. Provide a final answer with units
5. Double-check your calculations
"
};

var financialAgent = new Agent(customConfig, llmClient);
```

## Tree of Thoughts Examples

### Basic Tree of Thoughts

```csharp
using AIAgentSharp.Agents.TreeOfThoughts;

var config = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    MaxTreeDepth = 4,
    MaxTreeBreadth = 3,
    ExplorationStrategy = ExplorationStrategy.BestFirst
};

var agent = new Agent(config, llmClient);

// Example: Planning a vacation
var vacationPlanning = @"
Plan a 7-day vacation to Europe with a budget of $3000.
Consider:
- Transportation costs
- Accommodation
- Food and activities
- Weather and season
- Must-see attractions
";
```

### Tree of Thoughts with Different Strategies

```csharp
// Breadth-first exploration
var breadthFirstConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    ExplorationStrategy = ExplorationStrategy.BreadthFirst,
    MaxTreeDepth = 3,
    MaxTreeBreadth = 5
};

// Depth-first exploration
var depthFirstConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    ExplorationStrategy = ExplorationStrategy.DepthFirst,
    MaxTreeDepth = 6,
    MaxTreeBreadth = 2
};

// Monte Carlo exploration
var monteCarloConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    ExplorationStrategy = ExplorationStrategy.MonteCarlo,
    MaxTreeDepth = 4,
    MaxTreeBreadth = 3,
    MonteCarloIterations = 100
};
```

### Complex Decision Making

```csharp
// Business strategy decision
var businessDecision = @"
A tech startup has $500,000 in funding and must decide between:
Option A: Develop a mobile app (6 months, $300k, high market risk)
Option B: Build a web platform (4 months, $200k, medium market risk)
Option C: Create an API service (3 months, $150k, low market risk)

Consider:
- Time to market
- Resource requirements
- Market competition
- Revenue potential
- Technical complexity
- Team expertise

Provide a detailed analysis with recommendations.
";

var result = await agent.RunAsync(businessDecision);
```

## Advanced Reasoning Patterns

### Hybrid Reasoning

```csharp
// Combine CoT and ToT for complex problems
var hybridConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    MaxChainSteps = 8,
    EnableTreeExploration = true,
    TreeExplorationDepth = 3
};

var hybridAgent = new Agent(hybridConfig, llmClient);
```

### Domain-Specific Reasoning

```csharp
// Medical diagnosis reasoning
var medicalConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    ChainPromptTemplate = @"
You are a medical professional. When analyzing symptoms:
1. List all relevant symptoms
2. Consider differential diagnoses
3. Evaluate likelihood of each diagnosis
4. Recommend diagnostic tests
5. Suggest treatment options
6. Consider contraindications and risks
"
};

// Legal reasoning
var legalConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    ExplorationStrategy = ExplorationStrategy.BestFirst,
    TreePromptTemplate = @"
You are a legal expert. When analyzing cases:
1. Identify key legal issues
2. Research relevant precedents
3. Consider multiple legal arguments
4. Evaluate strength of each argument
5. Assess potential outcomes
6. Recommend legal strategy
"
};
```

## Real-World Examples

### Code Review and Refactoring

```csharp
var codeReviewPrompt = @"
Review this C# code and suggest improvements:

```csharp
public class UserService
{
    private List<User> users = new List<User>();
    
    public void AddUser(User user)
    {
        users.Add(user);
    }
    
    public User GetUser(int id)
    {
        return users.FirstOrDefault(u => u.Id == id);
    }
}
```

Consider:
- Thread safety
- Performance
- Error handling
- Design patterns
- Best practices
";

var result = await agent.RunAsync(codeReviewPrompt);
```

### Data Analysis Planning

```csharp
var dataAnalysisPrompt = @"
Plan a data analysis project for customer churn prediction:

Dataset: 10,000 customer records with:
- Demographics
- Purchase history
- Support interactions
- Account activity

Goals:
- Identify churn risk factors
- Build predictive model
- Recommend retention strategies

Outline the analysis approach, methodology, and expected outcomes.
";

var result = await agent.RunAsync(dataAnalysisPrompt);
```

## Best Practices

### 1. Choose the Right Reasoning Engine

- **Use Chain of Thought** for:
  - Sequential problem solving
  - Mathematical calculations
  - Step-by-step procedures
  - Debugging and troubleshooting

- **Use Tree of Thoughts** for:
  - Creative problem solving
  - Strategic planning
  - Decision making with multiple options
  - Exploring alternative approaches

### 2. Configure Parameters Appropriately

```csharp
// For complex problems, increase limits
var complexConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    MaxTreeDepth = 6,
    MaxTreeBreadth = 4,
    ExplorationStrategy = ExplorationStrategy.BestFirst,
    EvaluationTimeout = TimeSpan.FromMinutes(2)
};
```

### 3. Use Domain-Specific Prompts

```csharp
// Customize prompts for your specific use case
var domainConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    ChainPromptTemplate = @"
You are an expert in [YOUR_DOMAIN]. When solving problems:
1. [DOMAIN_SPECIFIC_STEP_1]
2. [DOMAIN_SPECIFIC_STEP_2]
3. [DOMAIN_SPECIFIC_STEP_3]
"
};
```

### 4. Monitor and Debug Reasoning

```csharp
// Subscribe to reasoning events
agent.ReasoningStepCompleted += (sender, e) =>
{
    Console.WriteLine($"Step {e.StepNumber}: {e.Reasoning}");
};

agent.TreeExplorationCompleted += (sender, e) =>
{
    Console.WriteLine($"Explored {e.NodesExplored} nodes");
    Console.WriteLine($"Best path: {e.BestPath}");
};
```

## Troubleshooting

### Common Issues

1. **Reasoning gets stuck in loops**
   - Reduce `MaxChainSteps` or `MaxTreeDepth`
   - Add loop detection logic
   - Use more specific prompts

2. **Poor reasoning quality**
   - Improve prompt templates
   - Increase context window
   - Use better LLM models

3. **Performance issues**
   - Reduce exploration breadth
   - Set appropriate timeouts
   - Use caching for repeated reasoning

### Debugging Tips

```csharp
// Enable detailed logging
var debugConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    EnableDetailedLogging = true,
    LogReasoningSteps = true
};

// Monitor reasoning progress
agent.ReasoningStarted += (sender, e) =>
{
    Console.WriteLine("Reasoning started...");
};

agent.ReasoningCompleted += (sender, e) =>
{
    Console.WriteLine($"Reasoning completed in {e.Duration}");
    Console.WriteLine($"Steps taken: {e.StepsCount}");
};
```

## Next Steps

- Explore the [Agent Framework](agent-framework.md) for more advanced configurations
- Learn about [Event System](event-system.md) for monitoring reasoning progress
- Check [Best Practices](best-practices/) for optimization tips
- Review [API Reference](api/) for detailed method documentation
