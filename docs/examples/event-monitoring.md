# Event Monitoring Example

This example demonstrates how to implement comprehensive real-time monitoring for AIAgentSharp agents using the event system. You'll learn how to track agent performance, debug issues, and gain insights into agent behavior.

## Overview

The event monitoring system provides real-time visibility into:
- Agent execution steps and progress
- Tool usage and performance
- Error tracking and debugging
- Performance metrics and analytics
- Custom business events

## Prerequisites

- AIAgentSharp installed and configured
- Basic understanding of the event system
- Familiarity with logging and monitoring concepts

## Implementation

### 1. Event Monitoring Setup

```csharp
using AIAgentSharp;
using AIAgentSharp.Events;
using AIAgentSharp.Agents;
using AIAgentSharp.Tools;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class EventMonitoringExample
{
    private readonly IEventManager _eventManager;
    private readonly ILogger<EventMonitoringExample> _logger;
    private readonly AgentMetricsCollector _metricsCollector;
    private readonly EventDashboard _dashboard;
    
    public EventMonitoringExample(
        IEventManager eventManager,
        ILogger<EventMonitoringExample> logger)
    {
        _eventManager = eventManager;
        _logger = logger;
        _metricsCollector = new AgentMetricsCollector();
        _dashboard = new EventDashboard();
        
        // Subscribe to all relevant events
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        // Agent lifecycle events
        _eventManager.Subscribe<AgentStartedEvent>(OnAgentStarted);
        _eventManager.Subscribe<AgentCompletedEvent>(OnAgentCompleted);
        _eventManager.Subscribe<AgentStepCompletedEvent>(OnStepCompleted);
        _eventManager.Subscribe<AgentErrorEvent>(OnAgentError);
        
        // Tool execution events
        _eventManager.Subscribe<ToolExecutionStartedEvent>(OnToolExecutionStarted);
        _eventManager.Subscribe<ToolExecutionCompletedEvent>(OnToolExecutionCompleted);
        _eventManager.Subscribe<ToolExecutionErrorEvent>(OnToolExecutionError);
        
        // Reasoning events
        _eventManager.Subscribe<ReasoningStartedEvent>(OnReasoningStarted);
        _eventManager.Subscribe<ReasoningCompletedEvent>(OnReasoningCompleted);
        
        // Custom business events
        _eventManager.Subscribe<CustomBusinessEvent>(OnCustomBusinessEvent);
    }
}
```

### 2. Event Handlers Implementation

```csharp
public partial class EventMonitoringExample
{
    private void OnAgentStarted(AgentStartedEvent e)
    {
        _logger.LogInformation("Agent started: {AgentId} at {Timestamp}", 
            e.AgentId, e.Timestamp);
        
        _metricsCollector.RecordAgentStart(e.AgentId, e.Timestamp);
        _dashboard.UpdateAgentStatus(e.AgentId, "Running");
        
        // Send notification to monitoring systems
        SendAlert($"Agent {e.AgentId} started execution");
    }
    
    private void OnAgentCompleted(AgentCompletedEvent e)
    {
        var duration = e.Timestamp - e.StartTime;
        _logger.LogInformation("Agent completed: {AgentId} in {Duration}ms", 
            e.AgentId, duration.TotalMilliseconds);
        
        _metricsCollector.RecordAgentCompletion(e.AgentId, duration, e.Success);
        _dashboard.UpdateAgentStatus(e.AgentId, e.Success ? "Completed" : "Failed");
        
        // Record performance metrics
        RecordPerformanceMetrics(e.AgentId, duration, e.Success);
    }
    
    private void OnStepCompleted(AgentStepCompletedEvent e)
    {
        _logger.LogDebug("Step completed: {AgentId} - Step {StepNumber}: {Description}", 
            e.AgentId, e.StepNumber, e.Description);
        
        _metricsCollector.RecordStepCompletion(e.AgentId, e.StepNumber, e.Duration);
        _dashboard.UpdateStepProgress(e.AgentId, e.StepNumber, e.Description);
        
        // Check for performance anomalies
        if (e.Duration.TotalMilliseconds > 5000) // 5 seconds threshold
        {
            _logger.LogWarning("Slow step detected: {AgentId} - Step {StepNumber} took {Duration}ms", 
                e.AgentId, e.StepNumber, e.Duration.TotalMilliseconds);
        }
    }
    
    private void OnAgentError(AgentErrorEvent e)
    {
        _logger.LogError(e.Exception, "Agent error: {AgentId} - {ErrorMessage}", 
            e.AgentId, e.ErrorMessage);
        
        _metricsCollector.RecordAgentError(e.AgentId, e.ErrorMessage, e.Exception);
        _dashboard.UpdateAgentStatus(e.AgentId, "Error");
        
        // Send critical alert
        SendCriticalAlert($"Agent {e.AgentId} encountered error: {e.ErrorMessage}");
        
        // Create incident ticket
        CreateIncidentTicket(e.AgentId, e.ErrorMessage, e.Exception);
    }
    
    private void OnToolExecutionStarted(ToolExecutionStartedEvent e)
    {
        _logger.LogDebug("Tool execution started: {AgentId} - {ToolName}", 
            e.AgentId, e.ToolName);
        
        _metricsCollector.RecordToolExecutionStart(e.AgentId, e.ToolName);
        _dashboard.UpdateToolStatus(e.AgentId, e.ToolName, "Executing");
    }
    
    private void OnToolExecutionCompleted(ToolExecutionCompletedEvent e)
    {
        _logger.LogDebug("Tool execution completed: {AgentId} - {ToolName} in {Duration}ms", 
            e.AgentId, e.ToolName, e.Duration.TotalMilliseconds);
        
        _metricsCollector.RecordToolExecutionCompletion(e.AgentId, e.ToolName, e.Duration, e.Success);
        _dashboard.UpdateToolStatus(e.AgentId, e.ToolName, e.Success ? "Completed" : "Failed");
        
        // Track tool performance
        TrackToolPerformance(e.ToolName, e.Duration, e.Success);
    }
    
    private void OnToolExecutionError(ToolExecutionErrorEvent e)
    {
        _logger.LogError(e.Exception, "Tool execution error: {AgentId} - {ToolName}: {ErrorMessage}", 
            e.AgentId, e.ToolName, e.ErrorMessage);
        
        _metricsCollector.RecordToolExecutionError(e.AgentId, e.ToolName, e.ErrorMessage);
        _dashboard.UpdateToolStatus(e.AgentId, e.ToolName, "Error");
        
        // Implement retry logic or fallback
        HandleToolError(e.AgentId, e.ToolName, e.ErrorMessage, e.Exception);
    }
    
    private void OnReasoningStarted(ReasoningStartedEvent e)
    {
        _logger.LogDebug("Reasoning started: {AgentId} - {ReasoningType}", 
            e.AgentId, e.ReasoningType);
        
        _metricsCollector.RecordReasoningStart(e.AgentId, e.ReasoningType);
    }
    
    private void OnReasoningCompleted(ReasoningCompletedEvent e)
    {
        _logger.LogDebug("Reasoning completed: {AgentId} - {ReasoningType} in {Duration}ms", 
            e.AgentId, e.ReasoningType, e.Duration.TotalMilliseconds);
        
        _metricsCollector.RecordReasoningCompletion(e.AgentId, e.ReasoningType, e.Duration);
        
        // Analyze reasoning performance
        AnalyzeReasoningPerformance(e.ReasoningType, e.Duration, e.StepsCount);
    }
    
    private void OnCustomBusinessEvent(CustomBusinessEvent e)
    {
        _logger.LogInformation("Custom business event: {AgentId} - {EventType}: {Data}", 
            e.AgentId, e.EventType, e.Data);
        
        _metricsCollector.RecordCustomEvent(e.AgentId, e.EventType, e.Data);
        _dashboard.UpdateBusinessMetrics(e.AgentId, e.EventType, e.Data);
    }
}
```

### 3. Metrics Collection and Analytics

```csharp
public class AgentMetricsCollector
{
    private readonly ConcurrentDictionary<string, AgentMetrics> _agentMetrics = new();
    private readonly ConcurrentDictionary<string, ToolMetrics> _toolMetrics = new();
    private readonly ConcurrentDictionary<string, ReasoningMetrics> _reasoningMetrics = new();
    
    public void RecordAgentStart(string agentId, DateTime timestamp)
    {
        var metrics = _agentMetrics.GetOrAdd(agentId, _ => new AgentMetrics());
        metrics.StartTime = timestamp;
        metrics.Status = AgentStatus.Running;
    }
    
    public void RecordAgentCompletion(string agentId, TimeSpan duration, bool success)
    {
        if (_agentMetrics.TryGetValue(agentId, out var metrics))
        {
            metrics.Duration = duration;
            metrics.Status = success ? AgentStatus.Completed : AgentStatus.Failed;
            metrics.Success = success;
            metrics.CompletionTime = DateTime.UtcNow;
        }
    }
    
    public void RecordStepCompletion(string agentId, int stepNumber, TimeSpan duration)
    {
        if (_agentMetrics.TryGetValue(agentId, out var metrics))
        {
            metrics.Steps.Add(new StepMetrics
            {
                StepNumber = stepNumber,
                Duration = duration,
                Timestamp = DateTime.UtcNow
            });
        }
    }
    
    public void RecordToolExecutionCompletion(string agentId, string toolName, TimeSpan duration, bool success)
    {
        var toolKey = $"{agentId}_{toolName}";
        var metrics = _toolMetrics.GetOrAdd(toolKey, _ => new ToolMetrics { ToolName = toolName });
        
        metrics.ExecutionCount++;
        metrics.TotalDuration += duration;
        metrics.SuccessCount += success ? 1 : 0;
        metrics.AverageDuration = metrics.TotalDuration.TotalMilliseconds / metrics.ExecutionCount;
    }
    
    public void RecordToolExecutionError(string agentId, string toolName, string errorMessage)
    {
        var toolKey = $"{agentId}_{toolName}";
        if (_toolMetrics.TryGetValue(toolKey, out var metrics))
        {
            metrics.ErrorCount++;
            metrics.LastError = errorMessage;
            metrics.LastErrorTime = DateTime.UtcNow;
        }
    }
    
    public AgentMetrics GetAgentMetrics(string agentId)
    {
        return _agentMetrics.TryGetValue(agentId, out var metrics) ? metrics : null;
    }
    
    public IEnumerable<ToolMetrics> GetToolMetrics()
    {
        return _toolMetrics.Values;
    }
    
    public IEnumerable<AgentMetrics> GetAllAgentMetrics()
    {
        return _agentMetrics.Values;
    }
}

public class AgentMetrics
{
    public string AgentId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public TimeSpan Duration { get; set; }
    public AgentStatus Status { get; set; }
    public bool Success { get; set; }
    public List<StepMetrics> Steps { get; set; } = new();
    public int ErrorCount { get; set; }
    public string LastError { get; set; }
}

public class ToolMetrics
{
    public string ToolName { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public double AverageDuration { get; set; }
    public string LastError { get; set; }
    public DateTime? LastErrorTime { get; set; }
}

public class StepMetrics
{
    public int StepNumber { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum AgentStatus
{
    Running,
    Completed,
    Failed,
    Error
}
```

### 4. Real-time Dashboard

```csharp
public class EventDashboard
{
    private readonly ConcurrentDictionary<string, AgentStatusInfo> _agentStatuses = new();
    private readonly ConcurrentDictionary<string, ToolStatusInfo> _toolStatuses = new();
    private readonly ConcurrentDictionary<string, object> _businessMetrics = new();
    
    public void UpdateAgentStatus(string agentId, string status)
    {
        var statusInfo = _agentStatuses.GetOrAdd(agentId, _ => new AgentStatusInfo());
        statusInfo.Status = status;
        statusInfo.LastUpdated = DateTime.UtcNow;
        
        // Broadcast to connected clients
        BroadcastAgentStatusUpdate(agentId, statusInfo);
    }
    
    public void UpdateStepProgress(string agentId, int stepNumber, string description)
    {
        if (_agentStatuses.TryGetValue(agentId, out var statusInfo))
        {
            statusInfo.CurrentStep = stepNumber;
            statusInfo.StepDescription = description;
            statusInfo.LastUpdated = DateTime.UtcNow;
            
            BroadcastStepProgressUpdate(agentId, stepNumber, description);
        }
    }
    
    public void UpdateToolStatus(string agentId, string toolName, string status)
    {
        var toolKey = $"{agentId}_{toolName}";
        var toolStatus = _toolStatuses.GetOrAdd(toolKey, _ => new ToolStatusInfo { ToolName = toolName });
        
        toolStatus.Status = status;
        toolStatus.LastUpdated = DateTime.UtcNow;
        
        BroadcastToolStatusUpdate(agentId, toolName, status);
    }
    
    public void UpdateBusinessMetrics(string agentId, string eventType, object data)
    {
        var key = $"{agentId}_{eventType}";
        _businessMetrics[key] = data;
        
        BroadcastBusinessMetricsUpdate(agentId, eventType, data);
    }
    
    public AgentStatusInfo GetAgentStatus(string agentId)
    {
        return _agentStatuses.TryGetValue(agentId, out var status) ? status : null;
    }
    
    public IEnumerable<AgentStatusInfo> GetAllAgentStatuses()
    {
        return _agentStatuses.Values;
    }
    
    public IEnumerable<ToolStatusInfo> GetAllToolStatuses()
    {
        return _toolStatuses.Values;
    }
    
    private void BroadcastAgentStatusUpdate(string agentId, AgentStatusInfo statusInfo)
    {
        // Implementation for real-time broadcasting (SignalR, WebSocket, etc.)
        Console.WriteLine($"Agent {agentId} status: {statusInfo.Status}");
    }
    
    private void BroadcastStepProgressUpdate(string agentId, int stepNumber, string description)
    {
        Console.WriteLine($"Agent {agentId} step {stepNumber}: {description}");
    }
    
    private void BroadcastToolStatusUpdate(string agentId, string toolName, string status)
    {
        Console.WriteLine($"Agent {agentId} tool {toolName}: {status}");
    }
    
    private void BroadcastBusinessMetricsUpdate(string agentId, string eventType, object data)
    {
        Console.WriteLine($"Agent {agentId} business event {eventType}: {data}");
    }
}

public class AgentStatusInfo
{
    public string AgentId { get; set; }
    public string Status { get; set; }
    public int CurrentStep { get; set; }
    public string StepDescription { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ToolStatusInfo
{
    public string ToolName { get; set; }
    public string Status { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

### 5. Custom Business Events

```csharp
public class CustomBusinessEvent : IAgentEvent
{
    public string AgentId { get; set; }
    public string EventType { get; set; }
    public object Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Example business event implementations
public class OrderProcessedEvent : CustomBusinessEvent
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
    public string CustomerId { get; set; }
}

public class DocumentAnalyzedEvent : CustomBusinessEvent
{
    public string DocumentId { get; set; }
    public string DocumentType { get; set; }
    public int PageCount { get; set; }
    public Dictionary<string, object> ExtractedData { get; set; }
}

public class CustomerInteractionEvent : CustomBusinessEvent
{
    public string CustomerId { get; set; }
    public string InteractionType { get; set; }
    public string Sentiment { get; set; }
    public int SatisfactionScore { get; set; }
}
```

### 6. Usage Example

```csharp
// Setup monitoring
var services = new ServiceCollection();

// Configure logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Register event manager and monitoring
services.AddSingleton<IEventManager, EventManager>();
services.AddSingleton<EventMonitoringExample>();

var serviceProvider = services.BuildServiceProvider();

// Create monitoring instance
var monitoring = serviceProvider.GetRequiredService<EventMonitoringExample>();

// Create an agent with monitoring
var agent = new ExampleAgent(
    llmClient,
    stateManager,
    serviceProvider.GetRequiredService<IEventManager>()
);

// Subscribe to custom business events
agent.EventManager.Subscribe<OrderProcessedEvent>(e =>
{
    Console.WriteLine($"Order processed: {e.OrderId} for ${e.Amount}");
});

// Execute agent with monitoring
var result = await agent.ExecuteAsync("Process customer order #12345");

// Get metrics after completion
var metrics = monitoring.GetAgentMetrics(agent.Id);
Console.WriteLine($"Agent completed in {metrics.Duration.TotalMilliseconds}ms");
Console.WriteLine($"Steps executed: {metrics.Steps.Count}");
Console.WriteLine($"Success: {metrics.Success}");

// Get tool performance metrics
var toolMetrics = monitoring.GetToolMetrics();
foreach (var toolMetric in toolMetrics)
{
    Console.WriteLine($"Tool {toolMetric.ToolName}: " +
                     $"{toolMetric.ExecutionCount} executions, " +
                     $"avg {toolMetric.AverageDuration:F2}ms, " +
                     $"success rate {toolMetric.SuccessCount * 100.0 / toolMetric.ExecutionCount:F1}%");
}
```

### 7. Advanced Monitoring Features

```csharp
public class AdvancedEventMonitoring
{
    private readonly IEventManager _eventManager;
    private readonly PerformanceAnalyzer _performanceAnalyzer;
    private readonly AnomalyDetector _anomalyDetector;
    private readonly AlertManager _alertManager;
    
    public AdvancedEventMonitoring(IEventManager eventManager)
    {
        _eventManager = eventManager;
        _performanceAnalyzer = new PerformanceAnalyzer();
        _anomalyDetector = new AnomalyDetector();
        _alertManager = new AlertManager();
        
        SubscribeToAdvancedEvents();
    }
    
    private void SubscribeToAdvancedEvents()
    {
        // Performance monitoring
        _eventManager.Subscribe<AgentStepCompletedEvent>(e =>
        {
            var performanceScore = _performanceAnalyzer.AnalyzeStepPerformance(e);
            if (performanceScore < 0.7) // Below 70% performance threshold
            {
                _alertManager.SendPerformanceAlert(e.AgentId, e.StepNumber, performanceScore);
            }
        });
        
        // Anomaly detection
        _eventManager.Subscribe<ToolExecutionCompletedEvent>(e =>
        {
            var isAnomaly = _anomalyDetector.DetectAnomaly(e);
            if (isAnomaly)
            {
                _alertManager.SendAnomalyAlert(e.AgentId, e.ToolName, e.Duration);
            }
        });
        
        // Business logic monitoring
        _eventManager.Subscribe<CustomBusinessEvent>(e =>
        {
            AnalyzeBusinessImpact(e);
        });
    }
    
    private void AnalyzeBusinessImpact(CustomBusinessEvent e)
    {
        // Implement business impact analysis
        // Track KPIs, SLA compliance, etc.
    }
}

public class PerformanceAnalyzer
{
    private readonly Dictionary<string, List<double>> _historicalPerformance = new();
    
    public double AnalyzeStepPerformance(AgentStepCompletedEvent e)
    {
        var key = $"{e.AgentId}_step_{e.StepNumber}";
        
        if (!_historicalPerformance.ContainsKey(key))
        {
            _historicalPerformance[key] = new List<double>();
        }
        
        var performance = CalculatePerformanceScore(e);
        _historicalPerformance[key].Add(performance);
        
        // Keep only last 100 measurements
        if (_historicalPerformance[key].Count > 100)
        {
            _historicalPerformance[key].RemoveAt(0);
        }
        
        return performance;
    }
    
    private double CalculatePerformanceScore(AgentStepCompletedEvent e)
    {
        // Implement performance scoring logic
        // Consider duration, success rate, complexity, etc.
        return 1.0 - (e.Duration.TotalMilliseconds / 10000.0); // Example scoring
    }
}

public class AnomalyDetector
{
    public bool DetectAnomaly(ToolExecutionCompletedEvent e)
    {
        // Implement anomaly detection logic
        // Consider statistical analysis, machine learning models, etc.
        
        // Simple example: detect if duration is more than 3x the average
        var averageDuration = GetAverageToolDuration(e.ToolName);
        return e.Duration.TotalMilliseconds > averageDuration * 3;
    }
    
    private double GetAverageToolDuration(string toolName)
    {
        // Implementation to get historical average duration
        return 1000.0; // Example value
    }
}

public class AlertManager
{
    public void SendPerformanceAlert(string agentId, int stepNumber, double performanceScore)
    {
        // Send alert to monitoring systems (PagerDuty, Slack, etc.)
        Console.WriteLine($"PERFORMANCE ALERT: Agent {agentId} step {stepNumber} performance: {performanceScore:P}");
    }
    
    public void SendAnomalyAlert(string agentId, string toolName, TimeSpan duration)
    {
        // Send anomaly alert
        Console.WriteLine($"ANOMALY ALERT: Agent {agentId} tool {toolName} took {duration.TotalMilliseconds}ms");
    }
}
```

## Key Features Demonstrated

### 1. Comprehensive Event Tracking
- Agent lifecycle events (start, step, completion, error)
- Tool execution events (start, completion, error)
- Reasoning engine events
- Custom business events

### 2. Real-time Monitoring
- Live dashboard updates
- Performance metrics collection
- Status tracking for agents and tools
- Business metrics monitoring

### 3. Performance Analysis
- Execution time tracking
- Success rate monitoring
- Performance scoring
- Anomaly detection

### 4. Error Handling and Alerting
- Error tracking and logging
- Critical alert notifications
- Incident ticket creation
- Retry logic implementation

### 5. Business Intelligence
- Custom business event tracking
- KPI monitoring
- SLA compliance tracking
- Business impact analysis

## Best Practices

1. **Event Granularity**: Choose appropriate event granularity - not too fine (performance impact) or too coarse (missing important details)
2. **Performance Impact**: Ensure event handling doesn't significantly impact agent performance
3. **Storage Management**: Implement proper storage and cleanup for historical metrics
4. **Alert Fatigue**: Avoid too many alerts - use appropriate thresholds and grouping
5. **Security**: Ensure sensitive data is not exposed in events
6. **Scalability**: Design the monitoring system to handle high-volume event streams

## Next Steps

- Implement persistent storage for metrics (database, time-series DB)
- Add machine learning-based anomaly detection
- Create web-based dashboard with real-time updates
- Integrate with external monitoring systems (Prometheus, Grafana)
- Add predictive analytics for performance optimization
- Implement automated incident response workflows

This example demonstrates how to build a comprehensive monitoring system that provides deep insights into agent behavior and performance while enabling proactive issue detection and resolution.
