# Event System

AIAgentSharp provides a comprehensive event system that allows you to monitor agent activity in real-time, track performance, and respond to various lifecycle events.

## Overview

The event system enables:

- **Real-time Monitoring**: Track agent progress and status
- **Performance Analytics**: Monitor metrics and resource usage
- **Debugging**: Understand agent behavior and decision-making
- **Integration**: Connect with external monitoring systems
- **Custom Logic**: Respond to specific events with custom actions

## Available Events

### Agent Lifecycle Events

```csharp
// Agent started
agent.RunStarted += (sender, e) =>
{
    Console.WriteLine($"Agent {e.AgentId} started with goal: {e.Goal}");
    Console.WriteLine($"Timestamp: {e.Timestamp}");
};

// Agent completed
agent.RunCompleted += (sender, e) =>
{
    Console.WriteLine($"Agent {e.AgentId} completed in {e.TotalTurns} turns");
    Console.WriteLine($"Success: {e.Succeeded}");
    Console.WriteLine($"Duration: {e.Duration}");
};
```

### Status Update Events

```csharp
agent.StatusUpdate += (sender, e) =>
{
    Console.WriteLine($"Agent: {e.AgentId}");
    Console.WriteLine($"Status: {e.StatusTitle}");
    Console.WriteLine($"Details: {e.StatusDetails}");
    Console.WriteLine($"Progress: {e.ProgressPct}%");
    Console.WriteLine($"Timestamp: {e.Timestamp}");
};
```

### Tool Events

```csharp
// Tool call started
agent.ToolCallStarted += (sender, e) =>
{
    Console.WriteLine($"Tool: {e.ToolName}");
    Console.WriteLine($"Parameters: {e.Parameters}");
    Console.WriteLine($"Turn: {e.TurnNumber}");
    Console.WriteLine($"Timestamp: {e.Timestamp}");
};

// Tool call completed
agent.ToolCallCompleted += (sender, e) =>
{
    Console.WriteLine($"Tool: {e.ToolName}");
    Console.WriteLine($"Result: {e.Result}");
    Console.WriteLine($"Duration: {e.Duration}");
    Console.WriteLine($"Success: {e.Succeeded}");
};
```

### Reasoning Events

```csharp
// Reasoning started
agent.ReasoningStarted += (sender, e) =>
{
    Console.WriteLine($"Reasoning started: {e.ReasoningType}");
    Console.WriteLine($"Goal: {e.Goal}");
    Console.WriteLine($"Context: {e.Context}");
};

// Reasoning completed
agent.ReasoningCompleted += (sender, e) =>
{
    Console.WriteLine($"Reasoning completed");
    Console.WriteLine($"Steps: {e.ReasoningChain.Steps.Count}");
    Console.WriteLine($"Confidence: {e.ReasoningChain.FinalConfidence:F2}");
    Console.WriteLine($"Duration: {e.Duration}");
};

// Individual reasoning step completed
agent.ReasoningStepCompleted += (sender, e) =>
{
    Console.WriteLine($"Step {e.Step.StepNumber} completed: {e.Step.StepType}");
    Console.WriteLine($"Reasoning: {e.Step.Reasoning}");
    Console.WriteLine($"Confidence: {e.Step.Confidence:F2}");
    Console.WriteLine($"Insights: {string.Join(", ", e.Step.Insights)}");
};
```

### LLM Events

```csharp
// LLM call started
agent.LlmCallStarted += (sender, e) =>
{
    Console.WriteLine($"LLM call started");
    Console.WriteLine($"Model: {e.Model}");
    Console.WriteLine($"Messages: {e.Messages.Count}");
    Console.WriteLine($"Max tokens: {e.MaxTokens}");
};

// LLM call completed
agent.LlmCallCompleted += (sender, e) =>
{
    Console.WriteLine($"LLM call completed");
    Console.WriteLine($"Response: {e.Response}");
    Console.WriteLine($"Usage: {e.Usage.TotalTokens} tokens");
    Console.WriteLine($"Duration: {e.Duration}");
};

// LLM streaming chunks
agent.LlmChunkReceived += (sender, e) =>
{
    Console.Write(e.Chunk.Content); // Print chunks as they arrive
    if (e.Chunk.IsFinal)
    {
        Console.WriteLine(); // New line when complete
    }
};
```

## Metrics Events

### Real-time Metrics Updates

```csharp
agent.Metrics.MetricsUpdated += (sender, e) =>
{
    var metrics = e.Metrics;
    
    // Performance metrics
    Console.WriteLine($"Total Agent Runs: {metrics.Performance.TotalAgentRuns}");
    Console.WriteLine($"Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}");
    Console.WriteLine($"Average Duration: {metrics.Performance.AverageAgentRunDuration:F2}s");
    
    // Resource metrics
    Console.WriteLine($"Total Tokens: {metrics.Resources.TotalTokens:N0}");
    Console.WriteLine($"Average Tokens per Run: {metrics.Resources.AverageTokensPerRun:F0}");
    Console.WriteLine($"Total Cost: ${metrics.Resources.TotalCost:F2}");
    
    // Reasoning metrics
    Console.WriteLine($"Reasoning Success Rate: {metrics.Reasoning.ReasoningSuccessRate:P2}");
    Console.WriteLine($"Average Reasoning Steps: {metrics.Reasoning.AverageReasoningSteps:F1}");
    Console.WriteLine($"Average Confidence: {metrics.Reasoning.AverageConfidence:F2}");
    
    // Tool metrics
    Console.WriteLine($"Total Tool Calls: {metrics.Tools.TotalToolCalls}");
    Console.WriteLine($"Tool Success Rate: {metrics.Tools.ToolCallSuccessRate:P2}");
    Console.WriteLine($"Average Tool Duration: {metrics.Tools.AverageToolCallDuration:F2}s");
};
```

## Event Arguments

### AgentRunStartedEventArgs

```csharp
public class AgentRunStartedEventArgs : EventArgs
{
    public string AgentId { get; }
    public string Goal { get; }
    public DateTime Timestamp { get; }
    public IDictionary<string, ITool> Tools { get; }
}
```

### AgentRunCompletedEventArgs

```csharp
public class AgentRunCompletedEventArgs : EventArgs
{
    public string AgentId { get; }
    public bool Succeeded { get; }
    public string? FinalOutput { get; }
    public string? ErrorMessage { get; }
    public AgentErrorType ErrorType { get; }
    public int TotalTurns { get; }
    public TimeSpan Duration { get; }
    public DateTime Timestamp { get; }
}
```

### AgentStatusUpdateEventArgs

```csharp
public class AgentStatusUpdateEventArgs : EventArgs
{
    public string AgentId { get; }
    public string StatusTitle { get; }
    public string StatusDetails { get; }
    public double? ProgressPct { get; }
    public DateTime Timestamp { get; }
}
```

### AgentToolCallStartedEventArgs

```csharp
public class AgentToolCallStartedEventArgs : EventArgs
{
    public string AgentId { get; }
    public string ToolName { get; }
    public Dictionary<string, object?> Parameters { get; }
    public int TurnNumber { get; }
    public DateTime Timestamp { get; }
}
```

### AgentToolCallCompletedEventArgs

```csharp
public class AgentToolCallCompletedEventArgs : EventArgs
{
    public string AgentId { get; }
    public string ToolName { get; }
    public object? Result { get; }
    public bool Succeeded { get; }
    public string? ErrorMessage { get; }
    public TimeSpan Duration { get; }
    public DateTime Timestamp { get; }
}
```

## Event Handling Patterns

### Console Logging

```csharp
public class ConsoleEventLogger
{
    public void SubscribeToEvents(Agent agent)
    {
        agent.RunStarted += OnRunStarted;
        agent.StatusUpdate += OnStatusUpdate;
        agent.RunCompleted += OnRunCompleted;
        agent.ToolCallStarted += OnToolCallStarted;
        agent.ToolCallCompleted += OnToolCallCompleted;
        agent.ReasoningStarted += OnReasoningStarted;
        agent.ReasoningCompleted += OnReasoningCompleted;
    }
    
    private void OnRunStarted(object? sender, AgentRunStartedEventArgs e)
    {
        Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] 🚀 Agent {e.AgentId} started");
        Console.WriteLine($"   Goal: {e.Goal}");
        Console.WriteLine($"   Tools: {e.Tools.Count}");
    }
    
    private void OnStatusUpdate(object? sender, AgentStatusUpdateEventArgs e)
    {
        var progress = e.ProgressPct.HasValue ? $" ({e.ProgressPct:F0}%)" : "";
        Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] 📊 {e.StatusTitle}: {e.StatusDetails}{progress}");
    }
    
    private void OnRunCompleted(object? sender, AgentRunCompletedEventArgs e)
    {
        var status = e.Succeeded ? "✅" : "❌";
        Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] {status} Agent {e.AgentId} completed");
        Console.WriteLine($"   Duration: {e.Duration.TotalSeconds:F2}s");
        Console.WriteLine($"   Turns: {e.TotalTurns}");
        if (!e.Succeeded)
        {
            Console.WriteLine($"   Error: {e.ErrorMessage}");
        }
    }
    
    private void OnToolCallStarted(object? sender, AgentToolCallStartedEventArgs e)
    {
        Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] 🔧 Tool {e.ToolName} called");
        Console.WriteLine($"   Parameters: {string.Join(", ", e.Parameters.Select(kv => $"{kv.Key}={kv.Value}"))}");
    }
    
    private void OnToolCallCompleted(object? sender, AgentToolCallCompletedEventArgs e)
    {
        var status = e.Succeeded ? "✅" : "❌";
        Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] {status} Tool {e.ToolName} completed");
        Console.WriteLine($"   Duration: {e.Duration.TotalSeconds:F2}s");
        if (!e.Succeeded)
        {
            Console.WriteLine($"   Error: {e.ErrorMessage}");
        }
    }
    
    private void OnReasoningStarted(object? sender, AgentReasoningStartedEventArgs e)
    {
        Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] 🧠 Reasoning started: {e.ReasoningType}");
    }
    
    private void OnReasoningCompleted(object? sender, AgentReasoningCompletedEventArgs e)
    {
        Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] 🧠 Reasoning completed");
        Console.WriteLine($"   Steps: {e.ReasoningChain.Steps.Count}");
        Console.WriteLine($"   Confidence: {e.ReasoningChain.FinalConfidence:F2}");
        Console.WriteLine($"   Duration: {e.Duration.TotalSeconds:F2}s");
    }
}

// Usage
var logger = new ConsoleEventLogger();
logger.SubscribeToEvents(agent);
```

### File Logging

```csharp
public class FileEventLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new object();
    
    public FileEventLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
    }
    
    public void SubscribeToEvents(Agent agent)
    {
        agent.RunStarted += OnRunStarted;
        agent.RunCompleted += OnRunCompleted;
        agent.StatusUpdate += OnStatusUpdate;
    }
    
    private void OnRunStarted(object? sender, AgentRunStartedEventArgs e)
    {
        Log($"AGENT_STARTED|{e.AgentId}|{e.Goal}|{e.Timestamp:yyyy-MM-dd HH:mm:ss}");
    }
    
    private void OnRunCompleted(object? sender, AgentRunCompletedEventArgs e)
    {
        Log($"AGENT_COMPLETED|{e.AgentId}|{e.Succeeded}|{e.TotalTurns}|{e.Duration.TotalSeconds:F2}|{e.Timestamp:yyyy-MM-dd HH:mm:ss}");
    }
    
    private void OnStatusUpdate(object? sender, AgentStatusUpdateEventArgs e)
    {
        Log($"STATUS_UPDATE|{e.AgentId}|{e.StatusTitle}|{e.StatusDetails}|{e.ProgressPct}|{e.Timestamp:yyyy-MM-dd HH:mm:ss}");
    }
    
    private void Log(string message)
    {
        lock (_lock)
        {
            File.AppendAllText(_logFilePath, message + Environment.NewLine);
        }
    }
}
```

### Database Logging

```csharp
public class DatabaseEventLogger
{
    private readonly IDbConnection _connection;
    
    public DatabaseEventLogger(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public void SubscribeToEvents(Agent agent)
    {
        agent.RunStarted += OnRunStarted;
        agent.RunCompleted += OnRunCompleted;
        agent.ToolCallStarted += OnToolCallStarted;
        agent.ToolCallCompleted += OnToolCallCompleted;
    }
    
    private void OnRunStarted(object? sender, AgentRunStartedEventArgs e)
    {
        var sql = @"
            INSERT INTO AgentRuns (AgentId, Goal, StartedAt, ToolsCount)
            VALUES (@AgentId, @Goal, @StartedAt, @ToolsCount)";
        
        _connection.Execute(sql, new
        {
            e.AgentId,
            e.Goal,
            StartedAt = e.Timestamp,
            ToolsCount = e.Tools.Count
        });
    }
    
    private void OnRunCompleted(object? sender, AgentRunCompletedEventArgs e)
    {
        var sql = @"
            UPDATE AgentRuns 
            SET CompletedAt = @CompletedAt, Succeeded = @Succeeded, 
                TotalTurns = @TotalTurns, Duration = @Duration,
                ErrorMessage = @ErrorMessage, ErrorType = @ErrorType
            WHERE AgentId = @AgentId AND StartedAt = @StartedAt";
        
        _connection.Execute(sql, new
        {
            e.AgentId,
            CompletedAt = e.Timestamp,
            e.Succeeded,
            e.TotalTurns,
            Duration = e.Duration.TotalSeconds,
            e.ErrorMessage,
            ErrorType = e.ErrorType.ToString()
        });
    }
    
    private void OnToolCallStarted(object? sender, AgentToolCallStartedEventArgs e)
    {
        var sql = @"
            INSERT INTO ToolCalls (AgentId, ToolName, Parameters, TurnNumber, StartedAt)
            VALUES (@AgentId, @ToolName, @Parameters, @TurnNumber, @StartedAt)";
        
        _connection.Execute(sql, new
        {
            e.AgentId,
            e.ToolName,
            Parameters = JsonSerializer.Serialize(e.Parameters),
            e.TurnNumber,
            StartedAt = e.Timestamp
        });
    }
    
    private void OnToolCallCompleted(object? sender, AgentToolCallCompletedEventArgs e)
    {
        var sql = @"
            UPDATE ToolCalls 
            SET CompletedAt = @CompletedAt, Succeeded = @Succeeded,
                Result = @Result, Duration = @Duration, ErrorMessage = @ErrorMessage
            WHERE AgentId = @AgentId AND ToolName = @ToolName AND StartedAt = @StartedAt";
        
        _connection.Execute(sql, new
        {
            e.AgentId,
            e.ToolName,
            CompletedAt = e.Timestamp,
            e.Succeeded,
            Result = JsonSerializer.Serialize(e.Result),
            Duration = e.Duration.TotalSeconds,
            e.ErrorMessage
        });
    }
}
```

### Metrics Dashboard

```csharp
public class MetricsDashboard
{
    private readonly Dictionary<string, object> _metrics = new();
    private readonly object _lock = new object();
    
    public void SubscribeToEvents(Agent agent)
    {
        agent.Metrics.MetricsUpdated += OnMetricsUpdated;
        agent.RunCompleted += OnRunCompleted;
    }
    
    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        lock (_lock)
        {
            _metrics["LastUpdated"] = DateTime.UtcNow;
            _metrics["TotalRuns"] = e.Metrics.Performance.TotalAgentRuns;
            _metrics["SuccessRate"] = e.Metrics.Operational.AgentRunSuccessRate;
            _metrics["TotalTokens"] = e.Metrics.Resources.TotalTokens;
            _metrics["AverageTokens"] = e.Metrics.Resources.AverageTokensPerRun;
            _metrics["TotalCost"] = e.Metrics.Resources.TotalCost;
        }
    }
    
    private void OnRunCompleted(object? sender, AgentRunCompletedEventArgs e)
    {
        lock (_lock)
        {
            if (!_metrics.ContainsKey("RecentRuns"))
            {
                _metrics["RecentRuns"] = new List<object>();
            }
            
            var recentRuns = (List<object>)_metrics["RecentRuns"];
            recentRuns.Add(new
            {
                e.AgentId,
                e.Succeeded,
                e.TotalTurns,
                e.Duration.TotalSeconds,
                e.Timestamp
            });
            
            // Keep only last 100 runs
            if (recentRuns.Count > 100)
            {
                recentRuns.RemoveAt(0);
            }
        }
    }
    
    public Dictionary<string, object> GetMetrics()
    {
        lock (_lock)
        {
            return new Dictionary<string, object>(_metrics);
        }
    }
}
```

## Best Practices

### 1. Event Subscription

```csharp
// Subscribe to events early
var agent = new Agent(llm, store);
SubscribeToEvents(agent); // Do this before running the agent

// Unsubscribe when done
private void SubscribeToEvents(Agent agent)
{
    agent.RunStarted += OnRunStarted;
    agent.RunCompleted += OnRunCompleted;
    // ... other events
}

private void UnsubscribeFromEvents(Agent agent)
{
    agent.RunStarted -= OnRunStarted;
    agent.RunCompleted -= OnRunCompleted;
    // ... other events
}
```

### 2. Performance Considerations

```csharp
// Use async event handlers for I/O operations
private async void OnRunCompleted(object? sender, AgentRunCompletedEventArgs e)
{
    await LogToDatabaseAsync(e); // Don't block the main thread
}

// Use lightweight handlers for real-time updates
private void OnStatusUpdate(object? sender, AgentStatusUpdateEventArgs e)
{
    // Keep this fast - just update UI or simple logging
    UpdateProgressBar(e.ProgressPct);
}
```

### 3. Error Handling

```csharp
private void OnToolCallCompleted(object? sender, AgentToolCallCompletedEventArgs e)
{
    try
    {
        // Your event handling logic
        ProcessToolResult(e);
    }
    catch (Exception ex)
    {
        // Log the error but don't let it crash the agent
        Console.WriteLine($"Error in tool call event handler: {ex.Message}");
    }
}
```

### 4. Event Filtering

```csharp
// Only log events for specific agents
private void OnRunStarted(object? sender, AgentRunStartedEventArgs e)
{
    if (e.AgentId.StartsWith("production-"))
    {
        LogToProductionSystem(e);
    }
}

// Only log errors
private void OnRunCompleted(object? sender, AgentRunCompletedEventArgs e)
{
    if (!e.Succeeded)
    {
        LogError(e);
    }
}
```

## Troubleshooting

### Common Issues

**Events not firing**: Ensure you subscribe to events before running the agent.

**Performance issues**: Use async event handlers and avoid blocking operations.

**Memory leaks**: Unsubscribe from events when the agent is disposed.

**Missing events**: Check that the agent configuration enables the events you need.

For more troubleshooting help, see the [Troubleshooting Guide](troubleshooting/common-issues.md).
