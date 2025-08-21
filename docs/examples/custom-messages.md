# Custom Messages in AIAgentSharp

AIAgentSharp now supports adding custom messages (including system prompts) alongside the existing AIAgentSharp system prompt. This allows you to enhance the agent's behavior with domain-specific instructions, context, or preferences without replacing the core AIAgentSharp functionality.

## Overview

The custom messages feature allows you to:

- Add custom system prompts for specific use cases
- Include additional context or examples
- Set tone or style preferences
- Provide domain-specific instructions
- Add conversation history or examples

## Message Order

Messages are added to the conversation in this specific order:

1. **AIAgentSharp System Prompt** (always first, never replaced)
2. **Your Custom Additional Messages** (in the order you specify)
3. **User Goal and Tool Catalog**
4. **Conversation History**

## Fluent API Methods

### Simple Message Methods

```csharp
var agent = AIAgent.Create(llm)
    .WithSystemMessage("You are an expert travel planner.")
    .WithUserMessage("Please provide detailed recommendations.")
    .WithAssistantMessage("I will provide comprehensive travel advice.")
    .Build();
```

### Multiple Messages at Once

```csharp
var agent = AIAgent.Create(llm)
    .WithMessages(
        new LlmMessage { Role = "system", Content = "You are a helpful assistant." },
        new LlmMessage { Role = "user", Content = "Please be concise." }
    )
    .Build();
```

### Fluent Message Builder

```csharp
var agent = AIAgent.Create(llm)
    .WithMessages(messages => messages
        .AddSystemMessage("You are a data analysis expert.")
        .AddUserMessage("Please provide insights and visualizations.")
        .AddAssistantMessage("I will analyze the data and provide clear insights.")
        .AddMessage("user", "Focus on actionable recommendations.")
    )
    .Build();
```

### Combining Multiple Approaches

```csharp
var agent = AIAgent.Create(llm)
    .WithSystemMessage("You are a project management expert.")
    .WithUserMessage("Help me organize my development workflow.")
    .WithMessages(messages => messages
        .AddSystemMessage("Always consider agile methodologies.")
        .AddUserMessage("Include time estimation techniques.")
    )
    .Build();
```

## Use Cases

### Domain-Specific Instructions

```csharp
var agent = AIAgent.Create(llm)
    .WithSystemMessage("You are a medical coding expert with 10+ years of experience.")
    .WithUserMessage("Always verify coding accuracy and provide rationale for your decisions.")
    .Build();
```

### Style and Tone Preferences

```csharp
var agent = AIAgent.Create(llm)
    .WithSystemMessage("You are a friendly, patient teacher.")
    .WithUserMessage("Explain concepts in simple terms and provide examples.")
    .Build();
```

### Context and Examples

```csharp
var agent = AIAgent.Create(llm)
    .WithSystemMessage("You are a Python development expert.")
    .WithUserMessage("Focus on modern Python practices (3.8+) and type hints.")
    .WithAssistantMessage("I will provide code examples with proper type annotations.")
    .Build();
```

### Security and Compliance

```csharp
var agent = AIAgent.Create(llm)
    .WithSystemMessage("You are a security-conscious developer.")
    .WithUserMessage("Always consider security implications and follow OWASP guidelines.")
    .WithMessages(messages => messages
        .AddSystemMessage("Never suggest hardcoded credentials or insecure practices.")
        .AddUserMessage("Prioritize security best practices in all recommendations.")
    )
    .Build();
```

## Best Practices

### 1. Keep Messages Concise

```csharp
// Good
.WithSystemMessage("You are a C# expert. Focus on modern patterns.")

// Avoid
.WithSystemMessage("You are a C# expert with extensive knowledge of object-oriented programming, design patterns, SOLID principles, dependency injection, async/await patterns, LINQ, Entity Framework, ASP.NET Core, and many other advanced topics...")
```

### 2. Use Specific Instructions

```csharp
// Good
.WithUserMessage("Provide code examples with error handling.")

// Avoid
.WithUserMessage("Be helpful.")
```

### 3. Combine Related Instructions

```csharp
// Good
.WithMessages(messages => messages
    .AddSystemMessage("You are a database optimization expert.")
    .AddUserMessage("Focus on query performance and indexing strategies.")
    .AddAssistantMessage("I will provide specific optimization recommendations.")
)

// Avoid
.WithSystemMessage("You are a database expert.")
.WithUserMessage("Focus on performance.")
.WithAssistantMessage("I will help with optimization.")
```

### 4. Consider Message Order

Remember that your custom messages come after the AIAgentSharp system prompt but before the user's goal. This means:

- The AIAgentSharp system prompt establishes the core behavior
- Your custom messages can refine or specialize that behavior
- The user's goal provides the specific task context

## Technical Details

### Message Storage

Custom messages are stored in the `AgentState.AdditionalMessages` property and are persisted across agent sessions when using state stores.

### Configuration

Messages can be configured through the `AgentConfiguration.AdditionalMessages` property or through the fluent API methods.

### Validation

The system validates that:
- Messages have non-empty content
- Messages have valid roles (system, user, assistant)
- Messages are not null

## Examples

See the `examples/MessageExample.cs` file for comprehensive examples of all the different ways to use custom messages.

## Migration from Previous Versions

If you're upgrading from a previous version of AIAgentSharp, no changes are required to your existing code. The new message functionality is additive and doesn't affect existing behavior.

## Limitations

- Custom messages cannot replace the AIAgentSharp system prompt
- Messages are added in the order specified
- There's no built-in deduplication of similar messages
- Message content is not validated for appropriateness or safety
