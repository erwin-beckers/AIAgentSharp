# Event API Reference

This document provides comprehensive API reference for the AIAgentSharp event system.

## Core Interfaces

### IEventManager Interface

The main interface for managing agent events.

```csharp
public interface IEventManager
{
    // Event subscriptions
    event EventHandler<AgentRunStartedEventArgs>? RunStarted;
    event EventHandler<AgentStepStartedEventArgs>? StepStarted;
    event EventHandler<AgentLlmCallStartedEventArgs>? LlmCallStarted;
    event EventHandler<AgentLlmCallCompletedEventArgs>? LlmCallCompleted;
    event EventHandler<AgentLlmChunkReceivedEventArgs>? LlmChunkReceived;
    event EventHandler<AgentToolCallStartedEventArgs>? ToolCallStarted;
    event EventHandler<AgentToolCallCompletedEventArgs>? ToolCallCompleted;
    event EventHandler<AgentStepCompletedEventArgs>? StepCompleted;
    event EventHandler<AgentRunCompletedEventArgs>? RunCompleted;
    event EventHandler<AgentStatusEventArgs>? StatusUpdate;

    // Event raising methods
    void RaiseRunStarted(string agentId, string goal);
    void RaiseLlmCallStarted(string agentId, int turnIndex);
    void RaiseLlmCallCompleted(string agentId, int turnIndex, ModelMessage? llmMessage, string? error = null);
    void RaiseLlmChunkReceived(string agentId, int turnIndex, LlmStreamingChunk chunk);
    void RaiseToolCallStarted(string agentId, int turnIndex, string toolName, Dictionary<string, object?> parameters);
    void RaiseToolCallCompleted(string agentId, int turnIndex, string toolName, bool success, object? output = null, string? error = null, TimeSpan? executionTime = null);
    void RaiseStepCompleted(string agentId, int turnIndex, AgentStepResult stepResult);
    void RaiseRunCompleted(string agentId, bool succeeded, string? finalOutput, string? error, int totalTurns);
    void RaiseStatusUpdate(string agentId, string statusTitle, string? statusDetails = null, string? nextStepHint = null, int? progressPct = null);
}
```

## Event Argument Classes

### AgentRunStartedEventArgs

Fired when an agent run begins.

```csharp
public class AgentRunStartedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public string Goal { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

### AgentStepStartedEventArgs

Fired when an agent step begins.

```csharp
public class AgentStepStartedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public int TurnIndex { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

### AgentLlmCallStartedEventArgs

Fired when an LLM call begins.

```csharp
public class AgentLlmCallStartedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public int TurnIndex { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

### AgentLlmCallCompletedEventArgs

Fired when an LLM call completes.

```csharp
public class AgentLlmCallCompletedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public int TurnIndex { get; set; }
    public ModelMessage? LlmMessage { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan? Duration { get; set; }
}
```

### AgentLlmChunkReceivedEventArgs

Fired when a streaming chunk is received from the LLM.

```csharp
public class AgentLlmChunkReceivedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public int TurnIndex { get; set; }
    public LlmStreamingChunk Chunk { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

### AgentToolCallStartedEventArgs

Fired when a tool call begins.

```csharp
public class AgentToolCallStartedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public int TurnIndex { get; set; }
    public string ToolName { get; set; } = "";
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

### AgentToolCallCompletedEventArgs

Fired when a tool call completes.

```csharp
public class AgentToolCallCompletedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public int TurnIndex { get; set; }
    public string ToolName { get; set; } = "";
    public bool Success { get; set; }
    public object? Output { get; set; }
    public string? Error { get; set; }
    public TimeSpan? ExecutionTime { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

### AgentStepCompletedEventArgs

Fired when an agent step completes.

```csharp
public class AgentStepCompletedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public int TurnIndex { get; set; }
    public AgentStepResult StepResult { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

### AgentRunCompletedEventArgs

Fired when an agent run completes.

```csharp
public class AgentRunCompletedEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public bool Succeeded { get; set; }
    public string? FinalOutput { get; set; }
    public string? Error { get; set; }
    public int TotalTurns { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan? TotalDuration { get; set; }
}
```

### AgentStatusEventArgs

Fired when agent status updates.

```csharp
public class AgentStatusEventArgs : EventArgs
{
    public string AgentId { get; set; } = "";
    public string StatusTitle { get; set; } = "";
    public string? StatusDetails { get; set; }
    public string? NextStepHint { get; set; }
    public int? ProgressPct { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

## Event Subscription Examples

### Basic Event Handling

```csharp
var agent = new Agent(llmClient, config);

// Subscribe to run events
agent.RunStarted += (sender, e) =>
{
    Console.WriteLine($"Agent {e.AgentId} started with goal: {e.Goal}");
};

agent.RunCompleted += (sender, e) =>
{
    Console.WriteLine($"Agent {e.AgentId} completed. Success: {e.Succeeded}");
    if (e.Succeeded)
    {
        Console.WriteLine($"Output: {e.FinalOutput}");
    }
    else
    {
        Console.WriteLine($"Error: {e.Error}");
    }
};
```

### Step Monitoring

```csharp
agent.StepStarted += (sender, e) =>
{
    Console.WriteLine($"Step {e.TurnIndex} started for agent {e.AgentId}");
};

agent.StepCompleted += (sender, e) =>
{
    var result = e.StepResult;
    Console.WriteLine($"Step {e.TurnIndex} completed. Continue: {result.ShouldContinue}");
    
    if (result.HasToolCall)
    {
        Console.WriteLine($"Tool called: {result.ToolCall?.Name}");
    }
};
```

### LLM Call Monitoring

```csharp
agent.LlmCallStarted += (sender, e) =>
{
    Console.WriteLine($"LLM call started for turn {e.TurnIndex}");
};

agent.LlmCallCompleted += (sender, e) =>
{
    if (e.Error != null)
    {
        Console.WriteLine($"LLM call failed: {e.Error}");
    }
    else
    {
        Console.WriteLine($"LLM call completed in {e.Duration?.TotalMilliseconds}ms");
    }
};
```

### Streaming Chunk Handling

```csharp
agent.LlmChunkReceived += (sender, e) =>
{
    var chunk = e.Chunk;
    if (!string.IsNullOrEmpty(chunk.Content))
    {
        Console.Write(chunk.Content); // Real-time output
    }
    
    if (chunk.IsFinal)
    {
        Console.WriteLine("\n--- LLM response complete ---");
    }
};
```

### Tool Call Monitoring

```csharp
agent.ToolCallStarted += (sender, e) =>
{
    Console.WriteLine($"Calling tool: {e.ToolName}");
    Console.WriteLine($"Parameters: {string.Join(", ", e.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
};

agent.ToolCallCompleted += (sender, e) =>
{
    if (e.Success)
    {
        Console.WriteLine($"Tool {e.ToolName} completed successfully in {e.ExecutionTime?.TotalMilliseconds}ms");
        Console.WriteLine($"Output: {e.Output}");
    }
    else
    {
        Console.WriteLine($"Tool {e.ToolName} failed: {e.Error}");
    }
};
```

### Status Updates

```csharp
agent.StatusUpdate += (sender, e) =>
{
    Console.WriteLine($"[{e.AgentId}] {e.StatusTitle}");
    
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
};
```

## Advanced Event Handling

### Event Filtering

```csharp
agent.LlmChunkReceived += (sender, e) =>
{
    // Filter out JSON content from streaming output
    if (!string.IsNullOrEmpty(e.Chunk.Content) && 
        !e.Chunk.Content.TrimStart().StartsWith("{"))
    {
        Console.Write(e.Chunk.Content);
    }
};
```

### Event Aggregation

```csharp
var stepTimes = new Dictionary<int, DateTimeOffset>();

agent.StepStarted += (sender, e) =>
{
    stepTimes[e.TurnIndex] = e.Timestamp;
};

agent.StepCompleted += (sender, e) =>
{
    if (stepTimes.TryGetValue(e.TurnIndex, out var startTime))
    {
        var duration = e.Timestamp - startTime;
        Console.WriteLine($"Step {e.TurnIndex} took {duration.TotalMilliseconds}ms");
    }
};
```

### Error Handling in Events

```csharp
agent.LlmCallCompleted += (sender, e) =>
{
    try
    {
        if (e.Error != null)
        {
            // Log error
            logger.LogError($"LLM call failed: {e.Error}");
            
            // Optionally send notification
            await notificationService.SendAlertAsync($"Agent {e.AgentId} LLM error", e.Error);
        }
    }
    catch (Exception ex)
    {
        // Handle event handler errors
        logger.LogError(ex, "Error in LLM call completed event handler");
    }
};
```

### Async Event Handlers

```csharp
agent.RunCompleted += async (sender, e) =>
{
    try
    {
        // Save results to database
        await SaveResultsAsync(e.AgentId, e.FinalOutput, e.Succeeded);
        
        // Send notifications
        await NotifyCompletionAsync(e.AgentId, e.Succeeded);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in async event handler");
    }
};
```

## Event Performance Considerations

### Event Handler Best Practices

1. **Keep Handlers Fast**: Avoid blocking operations in event handlers
2. **Use Async Carefully**: Be cautious with async operations in event handlers
3. **Handle Exceptions**: Always wrap event handler code in try-catch blocks
4. **Avoid Circular Dependencies**: Don't trigger events from within event handlers

### Memory Management

```csharp
// Unsubscribe when done to prevent memory leaks
agent.RunCompleted += OnRunCompleted;

// Later...
agent.RunCompleted -= OnRunCompleted;

// Or use weak event patterns for long-lived subscriptions
```

## Event System Configuration

### Disabling Events

```csharp
var config = new AgentConfiguration
{
    EnableEvents = false // Disable all events for performance
};
```

### Selective Event Enabling

```csharp
var eventConfig = new EventConfiguration
{
    EnableRunEvents = true,
    EnableStepEvents = true,
    EnableLlmEvents = false, // Disable LLM events
    EnableToolEvents = true,
    EnableStatusEvents = true,
    EnableChunkEvents = false // Disable chunk events
};
```

## See Also

- [Event System](../event-system.md) - Overview of the event system
- [Event Monitoring Example](../examples/event-monitoring.md) - Complete monitoring example
- [Agent Framework](../agent-framework.md) - How agents emit events

