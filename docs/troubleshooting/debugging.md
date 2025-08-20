# Debugging Guide

This guide provides comprehensive debugging techniques and tools for AIAgentSharp applications. Debugging AI agents presents unique challenges due to their non-deterministic nature and complex interactions.

## Overview

Effective debugging in AIAgentSharp involves:
- Understanding the agent execution flow
- Identifying bottlenecks and performance issues
- Tracing tool execution and LLM interactions
- Analyzing state changes and reasoning processes
- Using debugging tools and techniques

## Debugging Strategy

### 1. Debugging Pyramid
```
    /\
   /  \     Production Debugging (Remote)
  /____\    Integration Debugging (Local)
 /______\   Unit Debugging (Isolated)
```

### 2. Debugging Categories
- **Unit Debugging**: Debug individual components
- **Integration Debugging**: Debug component interactions
- **Production Debugging**: Debug live systems
- **Performance Debugging**: Debug performance issues

## Debugging Tools and Techniques

### 1. Enhanced Logging

```csharp
public class DebugLogger
{
    private readonly ILogger _logger;
    private readonly bool _enableDebugMode;
    private readonly Dictionary<string, object> _context;
    
    public DebugLogger(ILogger logger, bool enableDebugMode = false)
    {
        _logger = logger;
        _enableDebugMode = enableDebugMode;
        _context = new Dictionary<string, object>();
    }
    
    public void SetContext(string key, object value)
    {
        _context[key] = value;
    }
    
    public void LogDebug(string message, params object[] args)
    {
        if (_enableDebugMode)
        {
            var contextString = string.Join(", ", _context.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            _logger.LogDebug($"{message} | Context: {contextString}", args);
        }
    }
    
    public void LogExecutionStep(string stepName, object data = null)
    {
        LogDebug("Execution step: {StepName}", stepName);
        if (data != null)
        {
            LogDebug("Step data: {@Data}", data);
        }
    }
    
    public void LogToolExecution(string toolName, ToolParameters parameters, ToolResult result)
    {
        LogDebug("Tool execution: {ToolName}", toolName);
        LogDebug("Tool parameters: {@Parameters}", parameters);
        LogDebug("Tool result: {@Result}", result);
    }
    
    public void LogLLMRequest(LLMRequest request, LLMResponse response)
    {
        LogDebug("LLM request: {@Request}", request);
        LogDebug("LLM response: {@Response}", response);
    }
    
    public void LogStateChange(string stateKey, object oldState, object newState)
    {
        LogDebug("State change: {StateKey}", stateKey);
        LogDebug("Old state: {@OldState}", oldState);
        LogDebug("New state: {@NewState}", newState);
    }
}
```

### 2. Execution Tracer

```csharp
public class ExecutionTracer
{
    private readonly List<ExecutionStep> _steps;
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch;
    
    public ExecutionTracer(ILogger logger)
    {
        _steps = new List<ExecutionStep>();
        _logger = logger;
        _stopwatch = new Stopwatch();
    }
    
    public void StartTrace(string operationName)
    {
        _stopwatch.Restart();
        _logger.LogInformation("Starting execution trace: {Operation}", operationName);
    }
    
    public void AddStep(string stepName, object data = null)
    {
        var step = new ExecutionStep
        {
            Name = stepName,
            Timestamp = DateTime.UtcNow,
            ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds,
            Data = data
        };
        
        _steps.Add(step);
        _logger.LogDebug("Execution step: {StepName} at {Elapsed}ms", 
            stepName, step.ElapsedMilliseconds);
    }
    
    public void EndTrace()
    {
        _stopwatch.Stop();
        var totalTime = _stopwatch.ElapsedMilliseconds;
        
        _logger.LogInformation("Execution trace completed in {TotalTime}ms", totalTime);
        
        // Log step breakdown
        foreach (var step in _steps)
        {
            _logger.LogDebug("Step: {StepName} - {Elapsed}ms", 
                step.Name, step.ElapsedMilliseconds);
        }
        
        // Generate performance report
        var report = GeneratePerformanceReport();
        _logger.LogInformation("Performance report: {@Report}", report);
    }
    
    private PerformanceReport GeneratePerformanceReport()
    {
        var stepGroups = _steps.GroupBy(s => s.Name)
            .Select(g => new StepPerformance
            {
                StepName = g.Key,
                Count = g.Count(),
                TotalTime = g.Sum(s => s.ElapsedMilliseconds),
                AverageTime = g.Average(s => s.ElapsedMilliseconds)
            })
            .OrderByDescending(s => s.TotalTime)
            .ToList();
        
        return new PerformanceReport
        {
            TotalSteps = _steps.Count,
            TotalTime = _stopwatch.ElapsedMilliseconds,
            StepBreakdown = stepGroups
        };
    }
    
    public List<ExecutionStep> GetSteps() => _steps.ToList();
}

public class ExecutionStep
{
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public object Data { get; set; }
}

public class PerformanceReport
{
    public int TotalSteps { get; set; }
    public long TotalTime { get; set; }
    public List<StepPerformance> StepBreakdown { get; set; }
}

public class StepPerformance
{
    public string StepName { get; set; }
    public int Count { get; set; }
    public long TotalTime { get; set; }
    public double AverageTime { get; set; }
}
```

### 3. State Inspector

```csharp
public class StateInspector
{
    private readonly IAgentStateManager _stateManager;
    private readonly ILogger _logger;
    
    public StateInspector(IAgentStateManager stateManager, ILogger logger)
    {
        _stateManager = stateManager;
        _logger = logger;
    }
    
    public async Task<StateSnapshot> CaptureStateSnapshot(string agentId)
    {
        var snapshot = new StateSnapshot
        {
            AgentId = agentId,
            Timestamp = DateTime.UtcNow,
            StateData = new Dictionary<string, object>()
        };
        
        try
        {
            // Capture all state keys for this agent
            var stateKeys = await GetStateKeys(agentId);
            
            foreach (var key in stateKeys)
            {
                try
                {
                    var state = await _stateManager.GetStateAsync<object>(key);
                    snapshot.StateData[key] = state;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to capture state for key: {Key}", key);
                    snapshot.StateData[key] = $"Error: {ex.Message}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture state snapshot for agent: {AgentId}", agentId);
        }
        
        return snapshot;
    }
    
    public async Task<List<StateChange>> TrackStateChanges(string agentId, TimeSpan duration)
    {
        var changes = new List<StateChange>();
        var startTime = DateTime.UtcNow;
        
        // This would typically be implemented with event subscriptions
        // For now, we'll simulate state change tracking
        
        _logger.LogInformation("Tracking state changes for agent {AgentId} for {Duration}", 
            agentId, duration);
        
        return changes;
    }
    
    public void AnalyzeStateConsistency(StateSnapshot snapshot)
    {
        _logger.LogInformation("Analyzing state consistency for agent: {AgentId}", snapshot.AgentId);
        
        foreach (var kvp in snapshot.StateData)
        {
            var state = kvp.Value;
            if (state == null)
            {
                _logger.LogWarning("Null state found for key: {Key}", kvp.Key);
                continue;
            }
            
            // Check for common state issues
            CheckStateValidity(kvp.Key, state);
        }
    }
    
    private void CheckStateValidity(string key, object state)
    {
        // Check for circular references
        if (HasCircularReferences(state))
        {
            _logger.LogWarning("Circular reference detected in state key: {Key}", key);
        }
        
        // Check for large objects
        var size = EstimateObjectSize(state);
        if (size > 1024 * 1024) // 1MB
        {
            _logger.LogWarning("Large state object detected: {Key} - {Size}MB", 
                key, size / (1024 * 1024));
        }
        
        // Check for invalid data types
        if (state is IDictionary dict)
        {
            foreach (var item in dict.Keys)
            {
                if (item == null)
                {
                    _logger.LogWarning("Null key found in state dictionary: {Key}", key);
                }
            }
        }
    }
    
    private bool HasCircularReferences(object obj)
    {
        var visited = new HashSet<object>();
        return HasCircularReferences(obj, visited);
    }
    
    private bool HasCircularReferences(object obj, HashSet<object> visited)
    {
        if (obj == null) return false;
        if (visited.Contains(obj)) return true;
        
        visited.Add(obj);
        
        // Check properties for circular references
        var properties = obj.GetType().GetProperties();
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (HasCircularReferences(value, visited))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore properties that can't be accessed
            }
        }
        
        visited.Remove(obj);
        return false;
    }
    
    private long EstimateObjectSize(object obj)
    {
        if (obj == null) return 0;
        
        // Simple size estimation
        if (obj is string str) return str.Length * 2; // UTF-16
        if (obj is byte[] bytes) return bytes.Length;
        if (obj is int) return 4;
        if (obj is long) return 8;
        if (obj is double) return 8;
        if (obj is bool) return 1;
        
        // For complex objects, estimate based on properties
        var properties = obj.GetType().GetProperties();
        long size = 0;
        
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                size += EstimateObjectSize(value);
            }
            catch
            {
                // Ignore properties that can't be accessed
            }
        }
        
        return size;
    }
    
    private async Task<List<string>> GetStateKeys(string agentId)
    {
        // This would typically be implemented based on the specific state manager
        // For now, return a mock list
        return new List<string> { $"{agentId}_state", $"{agentId}_context", $"{agentId}_history" };
    }
}

public class StateSnapshot
{
    public string AgentId { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> StateData { get; set; }
}

public class StateChange
{
    public string Key { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 4. Tool Debugger

```csharp
public class ToolDebugger
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, ToolExecutionHistory> _executionHistory;
    
    public ToolDebugger(ILogger logger)
    {
        _logger = logger;
        _executionHistory = new Dictionary<string, ToolExecutionHistory>();
    }
    
    public async Task<ToolResult> DebugToolExecution(ITool tool, ToolParameters parameters)
    {
        var toolName = tool.Name;
        var executionId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("Starting tool execution debug: {ToolName} - {ExecutionId}", 
            toolName, executionId);
        
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Log input parameters
            LogToolInput(toolName, parameters);
            
            // Execute tool
            var result = await tool.ExecuteAsync(parameters);
            
            stopwatch.Stop();
            
            // Log output
            LogToolOutput(toolName, result, stopwatch.Elapsed);
            
            // Record execution history
            RecordExecution(toolName, parameters, result, stopwatch.Elapsed, true);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Tool execution failed: {ToolName} - {ExecutionId}", 
                toolName, executionId);
            
            // Record failed execution
            RecordExecution(toolName, parameters, null, stopwatch.Elapsed, false);
            
            throw;
        }
    }
    
    private void LogToolInput(string toolName, ToolParameters parameters)
    {
        _logger.LogDebug("Tool input - {ToolName}: {@Parameters}", toolName, parameters);
        
        // Validate parameters
        var validationResult = ValidateToolParameters(parameters);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Tool parameter validation failed: {ToolName} - {Errors}", 
                toolName, string.Join(", ", validationResult.Errors));
        }
    }
    
    private void LogToolOutput(string toolName, ToolResult result, TimeSpan duration)
    {
        _logger.LogDebug("Tool output - {ToolName}: {@Result} (Duration: {Duration}ms)", 
            toolName, result, duration.TotalMilliseconds);
        
        if (!result.Success)
        {
            _logger.LogWarning("Tool execution failed: {ToolName} - {Error}", 
                toolName, result.ErrorMessage);
        }
        
        if (duration.TotalSeconds > 10)
        {
            _logger.LogWarning("Slow tool execution: {ToolName} took {Duration}ms", 
                toolName, duration.TotalMilliseconds);
        }
    }
    
    private void RecordExecution(string toolName, ToolParameters parameters, 
        ToolResult result, TimeSpan duration, bool success)
    {
        if (!_executionHistory.ContainsKey(toolName))
        {
            _executionHistory[toolName] = new ToolExecutionHistory();
        }
        
        var history = _executionHistory[toolName];
        history.AddExecution(new ToolExecutionRecord
        {
            Timestamp = DateTime.UtcNow,
            Parameters = parameters,
            Result = result,
            Duration = duration,
            Success = success
        });
    }
    
    public ToolExecutionReport GenerateReport(string toolName)
    {
        if (!_executionHistory.ContainsKey(toolName))
        {
            return new ToolExecutionReport { ToolName = toolName };
        }
        
        var history = _executionHistory[toolName];
        return history.GenerateReport();
    }
    
    public List<ToolExecutionReport> GenerateAllReports()
    {
        return _executionHistory.Keys.Select(GenerateReport).ToList();
    }
    
    private ValidationResult ValidateToolParameters(ToolParameters parameters)
    {
        var result = new ValidationResult();
        
        // Basic validation - check for null parameters
        if (parameters == null)
        {
            result.AddError("Parameters cannot be null");
            return result;
        }
        
        // Add more validation logic as needed
        return result;
    }
}

public class ToolExecutionHistory
{
    private readonly List<ToolExecutionRecord> _executions;
    private const int MaxHistorySize = 1000;
    
    public ToolExecutionHistory()
    {
        _executions = new List<ToolExecutionRecord>();
    }
    
    public void AddExecution(ToolExecutionRecord record)
    {
        _executions.Add(record);
        
        // Keep only recent executions
        if (_executions.Count > MaxHistorySize)
        {
            _executions.RemoveAt(0);
        }
    }
    
    public ToolExecutionReport GenerateReport()
    {
        if (!_executions.Any())
        {
            return new ToolExecutionReport();
        }
        
        var successfulExecutions = _executions.Where(e => e.Success).ToList();
        var failedExecutions = _executions.Where(e => !e.Success).ToList();
        
        return new ToolExecutionReport
        {
            TotalExecutions = _executions.Count,
            SuccessfulExecutions = successfulExecutions.Count,
            FailedExecutions = failedExecutions.Count,
            SuccessRate = (double)successfulExecutions.Count / _executions.Count,
            AverageDuration = successfulExecutions.Any() 
                ? successfulExecutions.Average(e => e.Duration.TotalMilliseconds) 
                : 0,
            MaxDuration = _executions.Max(e => e.Duration.TotalMilliseconds),
            MinDuration = _executions.Min(e => e.Duration.TotalMilliseconds),
            RecentExecutions = _executions.TakeLast(10).ToList()
        };
    }
}

public class ToolExecutionRecord
{
    public DateTime Timestamp { get; set; }
    public ToolParameters Parameters { get; set; }
    public ToolResult Result { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}

public class ToolExecutionReport
{
    public string ToolName { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDuration { get; set; }
    public double MaxDuration { get; set; }
    public double MinDuration { get; set; }
    public List<ToolExecutionRecord> RecentExecutions { get; set; } = new();
}
```

### 5. LLM Interaction Debugger

```csharp
public class LLMDebugger
{
    private readonly ILogger _logger;
    private readonly List<LLMInteraction> _interactions;
    
    public LLMDebugger(ILogger logger)
    {
        _logger = logger;
        _interactions = new List<LLMInteraction>();
    }
    
    public async Task<LLMResponse> DebugLLMRequest(ILLMClient llmClient, LLMRequest request)
    {
        var interactionId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("Starting LLM interaction debug: {InteractionId}", interactionId);
        
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Log request details
            LogLLMRequest(request, interactionId);
            
            // Execute request
            var response = await llmClient.GenerateAsync(request);
            
            stopwatch.Stop();
            
            // Log response details
            LogLLMResponse(response, stopwatch.Elapsed, interactionId);
            
            // Record interaction
            RecordInteraction(request, response, stopwatch.Elapsed, true, interactionId);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "LLM interaction failed: {InteractionId}", interactionId);
            
            // Record failed interaction
            RecordInteraction(request, null, stopwatch.Elapsed, false, interactionId);
            
            throw;
        }
    }
    
    private void LogLLMRequest(LLMRequest request, string interactionId)
    {
        _logger.LogDebug("LLM request - {InteractionId}: {@Request}", interactionId, request);
        
        // Analyze prompt
        AnalyzePrompt(request.Prompt, interactionId);
        
        // Check token usage
        var estimatedTokens = EstimateTokenCount(request.Prompt);
        _logger.LogDebug("Estimated tokens: {Tokens} for interaction {InteractionId}", 
            estimatedTokens, interactionId);
    }
    
    private void LogLLMResponse(LLMResponse response, TimeSpan duration, string interactionId)
    {
        _logger.LogDebug("LLM response - {InteractionId}: {@Response} (Duration: {Duration}ms)", 
            interactionId, response, duration.TotalMilliseconds);
        
        // Analyze response
        AnalyzeResponse(response, interactionId);
        
        if (duration.TotalSeconds > 30)
        {
            _logger.LogWarning("Slow LLM response: {InteractionId} took {Duration}ms", 
                interactionId, duration.TotalMilliseconds);
        }
    }
    
    private void AnalyzePrompt(string prompt, string interactionId)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            _logger.LogWarning("Empty prompt detected: {InteractionId}", interactionId);
            return;
        }
        
        // Check prompt length
        if (prompt.Length > 10000)
        {
            _logger.LogWarning("Very long prompt detected: {InteractionId} - {Length} characters", 
                interactionId, prompt.Length);
        }
        
        // Check for potential issues
        if (prompt.Contains("password") || prompt.Contains("secret"))
        {
            _logger.LogWarning("Sensitive content detected in prompt: {InteractionId}", interactionId);
        }
        
        // Check for malformed prompts
        if (prompt.Contains("undefined") || prompt.Contains("null"))
        {
            _logger.LogWarning("Potential malformed prompt: {InteractionId}", interactionId);
        }
    }
    
    private void AnalyzeResponse(LLMResponse response, string interactionId)
    {
        if (response == null)
        {
            _logger.LogWarning("Null response received: {InteractionId}", interactionId);
            return;
        }
        
        if (string.IsNullOrEmpty(response.Content))
        {
            _logger.LogWarning("Empty response content: {InteractionId}", interactionId);
        }
        
        // Check for potential issues in response
        if (response.Content.Contains("I don't know") || response.Content.Contains("I cannot"))
        {
            _logger.LogInformation("LLM expressed uncertainty: {InteractionId}", interactionId);
        }
        
        // Check response length
        if (response.Content.Length > 5000)
        {
            _logger.LogInformation("Very long response: {InteractionId} - {Length} characters", 
                interactionId, response.Content.Length);
        }
    }
    
    private int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token
        return text?.Length / 4 ?? 0;
    }
    
    private void RecordInteraction(LLMRequest request, LLMResponse response, 
        TimeSpan duration, bool success, string interactionId)
    {
        var interaction = new LLMInteraction
        {
            Id = interactionId,
            Timestamp = DateTime.UtcNow,
            Request = request,
            Response = response,
            Duration = duration,
            Success = success
        };
        
        _interactions.Add(interaction);
        
        // Keep only recent interactions
        if (_interactions.Count > 1000)
        {
            _interactions.RemoveAt(0);
        }
    }
    
    public LLMInteractionReport GenerateReport()
    {
        if (!_interactions.Any())
        {
            return new LLMInteractionReport();
        }
        
        var successfulInteractions = _interactions.Where(i => i.Success).ToList();
        var failedInteractions = _interactions.Where(i => !i.Success).ToList();
        
        return new LLMInteractionReport
        {
            TotalInteractions = _interactions.Count,
            SuccessfulInteractions = successfulInteractions.Count,
            FailedInteractions = failedInteractions.Count,
            SuccessRate = (double)successfulInteractions.Count / _interactions.Count,
            AverageDuration = successfulInteractions.Any() 
                ? successfulInteractions.Average(i => i.Duration.TotalMilliseconds) 
                : 0,
            MaxDuration = _interactions.Max(i => i.Duration.TotalMilliseconds),
            MinDuration = _interactions.Min(i => i.Duration.TotalMilliseconds),
            RecentInteractions = _interactions.TakeLast(10).ToList()
        };
    }
}

public class LLMInteraction
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public LLMRequest Request { get; set; }
    public LLMResponse Response { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}

public class LLMInteractionReport
{
    public int TotalInteractions { get; set; }
    public int SuccessfulInteractions { get; set; }
    public int FailedInteractions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDuration { get; set; }
    public double MaxDuration { get; set; }
    public double MinDuration { get; set; }
    public List<LLMInteraction> RecentInteractions { get; set; } = new();
}
```

## Debugging Workflows

### 1. Performance Debugging

```csharp
public class PerformanceDebugger
{
    private readonly ILogger _logger;
    private readonly ExecutionTracer _tracer;
    private readonly PerformanceProfiler _profiler;
    
    public PerformanceDebugger(ILogger logger)
    {
        _logger = logger;
        _tracer = new ExecutionTracer(logger);
        _profiler = new PerformanceProfiler(logger);
    }
    
    public async Task<T> DebugPerformanceAsync<T>(string operationName, Func<Task<T>> operation)
    {
        _tracer.StartTrace(operationName);
        _profiler.StartProfiling();
        
        try
        {
            _tracer.AddStep("Operation started");
            var result = await operation();
            _tracer.AddStep("Operation completed", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _tracer.AddStep("Operation failed", ex);
            throw;
        }
        finally
        {
            _profiler.StopProfiling();
            _tracer.EndTrace();
            
            // Generate performance report
            var report = _profiler.GenerateReport();
            _logger.LogInformation("Performance report: {@Report}", report);
        }
    }
    
    public void AnalyzeBottlenecks(List<ExecutionStep> steps)
    {
        var slowSteps = steps.Where(s => s.ElapsedMilliseconds > 1000).ToList();
        
        if (slowSteps.Any())
        {
            _logger.LogWarning("Bottlenecks detected:");
            foreach (var step in slowSteps.OrderByDescending(s => s.ElapsedMilliseconds))
            {
                _logger.LogWarning("  {StepName}: {Elapsed}ms", 
                    step.Name, step.ElapsedMilliseconds);
            }
        }
        
        // Analyze step patterns
        var stepGroups = steps.GroupBy(s => s.Name)
            .Select(g => new { Name = g.Key, Count = g.Count(), AvgTime = g.Average(s => s.ElapsedMilliseconds) })
            .OrderByDescending(x => x.AvgTime)
            .ToList();
        
        _logger.LogInformation("Step performance analysis:");
        foreach (var group in stepGroups)
        {
            _logger.LogInformation("  {StepName}: {Count} executions, avg {AvgTime:F2}ms", 
                group.Name, group.Count, group.AvgTime);
        }
    }
}

public class PerformanceProfiler
{
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch;
    private readonly List<ProfilingPoint> _points;
    
    public PerformanceProfiler(ILogger logger)
    {
        _logger = logger;
        _stopwatch = new Stopwatch();
        _points = new List<ProfilingPoint>();
    }
    
    public void StartProfiling()
    {
        _stopwatch.Restart();
        _points.Clear();
    }
    
    public void AddPoint(string name, object data = null)
    {
        _points.Add(new ProfilingPoint
        {
            Name = name,
            Timestamp = _stopwatch.Elapsed,
            Data = data
        });
    }
    
    public void StopProfiling()
    {
        _stopwatch.Stop();
    }
    
    public PerformanceProfile GenerateReport()
    {
        return new PerformanceProfile
        {
            TotalDuration = _stopwatch.Elapsed,
            Points = _points.ToList()
        };
    }
}

public class ProfilingPoint
{
    public string Name { get; set; }
    public TimeSpan Timestamp { get; set; }
    public object Data { get; set; }
}

public class PerformanceProfile
{
    public TimeSpan TotalDuration { get; set; }
    public List<ProfilingPoint> Points { get; set; } = new();
}
```

### 2. Memory Debugging

```csharp
public class MemoryDebugger
{
    private readonly ILogger _logger;
    private readonly MemoryMonitor _monitor;
    
    public MemoryDebugger(ILogger logger)
    {
        _logger = logger;
        _monitor = new MemoryMonitor(logger);
    }
    
    public void StartMemoryTracking()
    {
        _monitor.StartTracking();
        _logger.LogInformation("Memory tracking started");
    }
    
    public void StopMemoryTracking()
    {
        var report = _monitor.StopTracking();
        _logger.LogInformation("Memory tracking stopped. Report: {@Report}", report);
    }
    
    public void CheckMemoryLeaks()
    {
        var memoryInfo = _monitor.GetMemoryInfo();
        
        if (memoryInfo.MemoryUsage > 500 * 1024 * 1024) // 500MB
        {
            _logger.LogWarning("High memory usage detected: {MemoryMB}MB", 
                memoryInfo.MemoryUsage / (1024 * 1024));
        }
        
        if (memoryInfo.GCCollections > 10)
        {
            _logger.LogWarning("Frequent garbage collection detected: {Collections} collections", 
                memoryInfo.GCCollections);
        }
    }
    
    public void ForceGarbageCollection()
    {
        var beforeMemory = GC.GetTotalMemory(false);
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var afterMemory = GC.GetTotalMemory(false);
        var freedMemory = beforeMemory - afterMemory;
        
        _logger.LogInformation("Garbage collection freed {FreedMB}MB", 
            freedMemory / (1024 * 1024));
    }
}

public class MemoryMonitor
{
    private readonly ILogger _logger;
    private readonly List<MemorySnapshot> _snapshots;
    private readonly Timer _timer;
    private bool _isTracking;
    
    public MemoryMonitor(ILogger logger)
    {
        _logger = logger;
        _snapshots = new List<MemorySnapshot>();
        _timer = new Timer(CaptureSnapshot, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public void StartTracking()
    {
        _isTracking = true;
        _snapshots.Clear();
        _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30)); // Capture every 30 seconds
    }
    
    public MemoryTrackingReport StopTracking()
    {
        _isTracking = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        
        return GenerateReport();
    }
    
    private void CaptureSnapshot(object state)
    {
        if (!_isTracking) return;
        
        var snapshot = new MemorySnapshot
        {
            Timestamp = DateTime.UtcNow,
            MemoryUsage = GC.GetTotalMemory(false),
            GCCollections = GC.CollectionCount(0)
        };
        
        _snapshots.Add(snapshot);
        
        if (snapshot.MemoryUsage > 100 * 1024 * 1024) // 100MB
        {
            _logger.LogWarning("High memory usage snapshot: {MemoryMB}MB", 
                snapshot.MemoryUsage / (1024 * 1024));
        }
    }
    
    public MemoryInfo GetMemoryInfo()
    {
        return new MemoryInfo
        {
            MemoryUsage = GC.GetTotalMemory(false),
            GCCollections = GC.CollectionCount(0),
            Snapshots = _snapshots.Count
        };
    }
    
    private MemoryTrackingReport GenerateReport()
    {
        if (!_snapshots.Any())
        {
            return new MemoryTrackingReport();
        }
        
        return new MemoryTrackingReport
        {
            StartTime = _snapshots.First().Timestamp,
            EndTime = _snapshots.Last().Timestamp,
            InitialMemory = _snapshots.First().MemoryUsage,
            FinalMemory = _snapshots.Last().MemoryUsage,
            PeakMemory = _snapshots.Max(s => s.MemoryUsage),
            AverageMemory = _snapshots.Average(s => s.MemoryUsage),
            Snapshots = _snapshots.ToList()
        };
    }
}

public class MemorySnapshot
{
    public DateTime Timestamp { get; set; }
    public long MemoryUsage { get; set; }
    public int GCCollections { get; set; }
}

public class MemoryInfo
{
    public long MemoryUsage { get; set; }
    public int GCCollections { get; set; }
    public int Snapshots { get; set; }
}

public class MemoryTrackingReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long InitialMemory { get; set; }
    public long FinalMemory { get; set; }
    public long PeakMemory { get; set; }
    public double AverageMemory { get; set; }
    public List<MemorySnapshot> Snapshots { get; set; } = new();
}
```

## Debugging Best Practices

### 1. Systematic Approach
- Start with the most likely cause
- Use binary search to isolate issues
- Document findings and solutions
- Use version control for debugging changes

### 2. Logging Strategy
- Use appropriate log levels
- Include context in log messages
- Use structured logging
- Implement log rotation

### 3. Performance Debugging
- Profile before optimizing
- Measure baseline performance
- Identify bottlenecks
- Test optimizations

### 4. Memory Debugging
- Monitor memory usage
- Check for memory leaks
- Use memory profiling tools
- Implement memory limits

### 5. Production Debugging
- Use remote debugging tools
- Implement health checks
- Monitor system metrics
- Set up alerting

## Debugging Checklist

- [ ] Enable debug logging
- [ ] Set up execution tracing
- [ ] Implement state inspection
- [ ] Add tool debugging
- [ ] Configure LLM debugging
- [ ] Set up performance monitoring
- [ ] Implement memory tracking
- [ ] Create debugging reports
- [ ] Document debugging procedures
- [ ] Train team on debugging tools

## Common Debugging Anti-patterns

1. **Debugging in Production**: Use staging environments
2. **Over-Logging**: Use appropriate log levels
3. **Ignoring Context**: Always include relevant context
4. **Not Documenting**: Document debugging findings
5. **Rushing to Solutions**: Take time to understand the problem
6. **Not Testing Fixes**: Always test debugging changes

## Next Steps

- Set up debugging infrastructure
- Implement debugging tools
- Create debugging runbooks
- Train team on debugging
- Establish debugging procedures
- Monitor debugging effectiveness

This guide provides a comprehensive approach to debugging AIAgentSharp applications, helping you identify and resolve issues quickly and effectively.
