# Reasoning Examples

This guide demonstrates AIAgentSharp's Chain of Thought (CoT) and Tree of Thoughts (ToT) reasoning capabilities.

## Chain of Thought Examples

### Basic CoT Configuration

```csharp
var config = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    MaxChainSteps = 5
};

var agent = new Agent(config, llmClient);
var result = await agent.RunAsync("Solve: If a train travels 120 km in 2 hours, what is its speed?");
```

### Multi-Step Problem Solving

```csharp
var complexProblem = @"
A company has 3 departments:
- Engineering: 40 employees, $80,000 avg salary
- Sales: 25 employees, $60,000 avg salary  
- Marketing: 15 employees, $70,000 avg salary

What is the total annual payroll cost?
";

var result = await agent.RunAsync(complexProblem);
```

## Tree of Thoughts Examples

### Basic ToT Configuration

```csharp
var config = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    MaxTreeDepth = 4,
    MaxTreeBreadth = 3,
    ExplorationStrategy = ExplorationStrategy.BestFirst
};

var agent = new Agent(config, llmClient);
```

### Vacation Planning Example

```csharp
var vacationPlanning = @"
Plan a 7-day vacation to Europe with $3000 budget.
Consider: transportation, accommodation, food, activities, weather.
";

var result = await agent.RunAsync(vacationPlanning);
```

### Business Strategy Decision

```csharp
var businessDecision = @"
A startup has $500,000 and must choose:
Option A: Mobile app (6 months, $300k, high risk)
Option B: Web platform (4 months, $200k, medium risk)
Option C: API service (3 months, $150k, low risk)

Analyze and recommend the best option.
";

var result = await agent.RunAsync(businessDecision);
```

## Advanced Patterns

### Domain-Specific Reasoning

```csharp
// Medical diagnosis
var medicalConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    ChainPromptTemplate = @"
You are a medical professional. When analyzing symptoms:
1. List relevant symptoms
2. Consider differential diagnoses
3. Recommend diagnostic tests
4. Suggest treatment options
"
};

// Legal analysis
var legalConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    ExplorationStrategy = ExplorationStrategy.BestFirst,
    TreePromptTemplate = @"
You are a legal expert. When analyzing cases:
1. Identify key legal issues
2. Research relevant precedents
3. Consider multiple arguments
4. Assess potential outcomes
"
};
```

## Real-World Examples

### Code Review

```csharp
var codeReviewPrompt = @"
Review this C# code and suggest improvements:

```csharp
public class UserService
{
    private List<User> users = new List<User>();
    
    public void AddUser(User user) { users.Add(user); }
    public User GetUser(int id) { return users.FirstOrDefault(u => u.Id == id); }
}
```

Consider: thread safety, performance, error handling, design patterns.
";

var result = await agent.RunAsync(codeReviewPrompt);
```

### Data Analysis Planning

```csharp
var dataAnalysisPrompt = @"
Plan a customer churn prediction analysis:

Dataset: 10,000 customer records with demographics, purchase history, support interactions.

Goals: Identify churn risk factors, build predictive model, recommend retention strategies.

Outline the analysis approach and methodology.
";

var result = await agent.RunAsync(dataAnalysisPrompt);
```

## Best Practices

### Choose the Right Engine

- **Chain of Thought**: Sequential problems, calculations, step-by-step procedures
- **Tree of Thoughts**: Creative problems, strategic planning, multiple options

### Configure Parameters

```csharp
var complexConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.TreeOfThoughts,
    MaxTreeDepth = 6,
    MaxTreeBreadth = 4,
    ExplorationStrategy = ExplorationStrategy.BestFirst,
    EvaluationTimeout = TimeSpan.FromMinutes(2)
};
```

### Monitor Reasoning

```csharp
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

1. **Reasoning loops**: Reduce step limits, add loop detection
2. **Poor quality**: Improve prompts, use better LLM models
3. **Performance**: Reduce exploration breadth, set timeouts

### Debugging

```csharp
var debugConfig = new AgentConfiguration
{
    ReasoningEngine = ReasoningEngine.ChainOfThought,
    EnableDetailedLogging = true,
    LogReasoningSteps = true
};
```

## Next Steps

- [Agent Framework](agent-framework.md) - Advanced configurations
- [Event System](event-system.md) - Monitoring reasoning progress
- [Best Practices](best-practices/) - Optimization tips
- [API Reference](api/) - Detailed documentation
