# Error Handling Best Practices

This guide covers comprehensive error handling strategies for AIAgentSharp applications. Proper error handling is crucial for building robust, production-ready agents that can gracefully handle failures and provide meaningful feedback.

## Overview

Effective error handling in AIAgentSharp involves:
- Anticipating and handling various failure scenarios
- Implementing retry mechanisms with exponential backoff
- Providing meaningful error messages and logging
- Graceful degradation when services are unavailable
- Monitoring and alerting for critical errors

## Error Categories

### 1. LLM Provider Errors
- API rate limiting
- Authentication failures
- Network connectivity issues
- Model unavailability
- Token limit exceeded

### 2. Tool Execution Errors
- External API failures
- Invalid parameters
- Timeout errors
- Resource exhaustion
- Authentication issues

### 3. Agent Logic Errors
- Invalid state transitions
- Reasoning engine failures
- Memory/state corruption
- Configuration errors

### 4. System Errors
- Memory exhaustion
- Disk space issues
- Network connectivity problems
- Database connection failures

## Implementation Strategies

### 1. Comprehensive Exception Handling

```csharp
public class RobustAgent : Agent
{
    private readonly ILogger<RobustAgent> _logger;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ICircuitBreaker _circuitBreaker;
    
    public RobustAgent(
        ILLMClient llmClient,
        IAgentStateManager stateManager,
        IEventManager eventManager,
        ILogger<RobustAgent> logger)
        : base(llmClient, stateManager, eventManager)
    {
        _logger = logger;
        _retryPolicy = new ExponentialBackoffRetryPolicy();
        _circuitBreaker = new CircuitBreaker();
    }
    
    public override async Task<string> ExecuteAsync(string prompt)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
            }
            
            // Check circuit breaker before execution
            if (_circuitBreaker.IsOpen)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
            
            // Execute with retry policy
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    return await base.ExecuteAsync(prompt);
                }
                catch (Exception ex)
                {
                    _circuitBreaker.RecordFailure();
                    throw;
                }
            });
        }
        catch (LLMRateLimitException ex)
        {
            _logger.LogWarning("Rate limit exceeded: {Message}", ex.Message);
            await HandleRateLimitError(ex);
            throw;
        }
        catch (LLMAuthenticationException ex)
        {
            _logger.LogError("Authentication failed: {Message}", ex.Message);
            await HandleAuthenticationError(ex);
            throw;
        }
        catch (ToolExecutionException ex)
        {
            _logger.LogError("Tool execution failed: {ToolName} - {Message}", 
                ex.ToolName, ex.Message);
            await HandleToolError(ex);
            throw;
        }
        catch (CircuitBreakerOpenException ex)
        {
            _logger.LogWarning("Circuit breaker open: {Message}", ex.Message);
            return await HandleCircuitBreakerOpen(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during agent execution");
            await HandleUnexpectedError(ex);
            throw;
        }
    }
}
```

### 2. Retry Policies

```csharp
public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
}

public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly double _backoffMultiplier;
    
    public ExponentialBackoffRetryPolicy(
        int maxRetries = 3,
        TimeSpan baseDelay = default,
        double backoffMultiplier = 2.0)
    {
        _maxRetries = maxRetries;
        _baseDelay = baseDelay == default ? TimeSpan.FromSeconds(1) : baseDelay;
        _backoffMultiplier = backoffMultiplier;
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        var lastException = default(Exception);
        
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsRetryableException(ex) && attempt < _maxRetries)
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);
                await Task.Delay(delay);
            }
        }
        
        throw lastException ?? new InvalidOperationException("Operation failed after all retries");
    }
    
    private bool IsRetryableException(Exception ex)
    {
        return ex is LLMRateLimitException ||
               ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException;
    }
    
    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = _baseDelay * Math.Pow(_backoffMultiplier, attempt);
        var jitter = Random.Shared.NextDouble() * 0.1; // 10% jitter
        return TimeSpan.FromMilliseconds(delay.TotalMilliseconds * (1 + jitter));
    }
}
```

### 3. Circuit Breaker Pattern

```csharp
public interface ICircuitBreaker
{
    bool IsOpen { get; }
    void RecordSuccess();
    void RecordFailure();
}

public class CircuitBreaker : ICircuitBreaker
{
    private readonly object _lock = new object();
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime;
    
    public CircuitBreaker(int failureThreshold = 5, TimeSpan resetTimeout = default)
    {
        _failureThreshold = failureThreshold;
        _resetTimeout = resetTimeout == default ? TimeSpan.FromMinutes(1) : resetTimeout;
    }
    
    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    if (DateTime.UtcNow - _lastFailureTime > _resetTimeout)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                    }
                }
                return _state == CircuitBreakerState.Open;
            }
        }
    }
    
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
        }
    }
    
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
            }
        }
    }
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
```

### 4. Tool Error Handling

```csharp
public abstract class RobustTool : ITool
{
    private readonly ILogger _logger;
    private readonly IRetryPolicy _retryPolicy;
    
    protected RobustTool(ILogger logger)
    {
        _logger = logger;
        _retryPolicy = new ExponentialBackoffRetryPolicy();
    }
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public async Task<ToolResult> ExecuteAsync(ToolParameters parameters)
    {
        try
        {
            // Validate parameters
            ValidateParameters(parameters);
            
            // Execute with retry policy
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    return await ExecuteCoreAsync(parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tool execution failed: {ToolName}", Name);
                    throw new ToolExecutionException(Name, ex.Message, ex);
                }
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Parameter validation failed: {ToolName} - {Message}", 
                Name, ex.Message);
            return new ToolResult
            {
                Success = false,
                ErrorMessage = $"Parameter validation failed: {ex.Message}"
            };
        }
        catch (ToolExecutionException ex)
        {
            _logger.LogError(ex, "Tool execution failed: {ToolName}", Name);
            return new ToolResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in tool: {ToolName}", Name);
            return new ToolResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred"
            };
        }
    }
    
    protected abstract Task<ToolResult> ExecuteCoreAsync(ToolParameters parameters);
    
    protected virtual void ValidateParameters(ToolParameters parameters)
    {
        // Override in derived classes to add parameter validation
    }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class ToolExecutionException : Exception
{
    public string ToolName { get; }
    
    public ToolExecutionException(string toolName, string message, Exception innerException = null)
        : base(message, innerException)
    {
        ToolName = toolName;
    }
}
```

### 5. LLM Client Error Handling

```csharp
public class RobustLLMClient : ILLMClient
{
    private readonly ILLMClient _innerClient;
    private readonly ILogger<RobustLLMClient> _logger;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ICircuitBreaker _circuitBreaker;
    
    public RobustLLMClient(
        ILLMClient innerClient,
        ILogger<RobustLLMClient> logger)
    {
        _innerClient = innerClient;
        _logger = logger;
        _retryPolicy = new ExponentialBackoffRetryPolicy();
        _circuitBreaker = new CircuitBreaker();
    }
    
    public async Task<LLMResponse> GenerateAsync(LLMRequest request)
    {
        try
        {
            if (_circuitBreaker.IsOpen)
            {
                throw new CircuitBreakerOpenException("LLM circuit breaker is open");
            }
            
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var response = await _innerClient.GenerateAsync(request);
                    _circuitBreaker.RecordSuccess();
                    return response;
                }
                catch (Exception ex)
                {
                    _circuitBreaker.RecordFailure();
                    throw;
                }
            });
        }
        catch (LLMRateLimitException ex)
        {
            _logger.LogWarning("Rate limit exceeded: {Message}", ex.Message);
            await HandleRateLimit(request);
            throw;
        }
        catch (LLMAuthenticationException ex)
        {
            _logger.LogError("Authentication failed: {Message}", ex.Message);
            await HandleAuthenticationFailure();
            throw;
        }
        catch (LLMTokenLimitException ex)
        {
            _logger.LogWarning("Token limit exceeded: {Message}", ex.Message);
            return await HandleTokenLimit(request, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected LLM error");
            throw;
        }
    }
    
    private async Task HandleRateLimit(LLMRequest request)
    {
        // Implement rate limit handling (e.g., queue request, use fallback model)
        await Task.Delay(TimeSpan.FromSeconds(60)); // Wait before retry
    }
    
    private async Task HandleAuthenticationFailure()
    {
        // Implement authentication failure handling (e.g., refresh token, alert admin)
        _logger.LogCritical("LLM authentication failed - manual intervention required");
    }
    
    private async Task<LLMResponse> HandleTokenLimit(LLMRequest request, LLMTokenLimitException ex)
    {
        // Implement token limit handling (e.g., truncate input, split request)
        var truncatedRequest = TruncateRequest(request, ex.MaxTokens);
        return await _innerClient.GenerateAsync(truncatedRequest);
    }
    
    private LLMRequest TruncateRequest(LLMRequest request, int maxTokens)
    {
        // Implementation to truncate request to fit within token limits
        return request;
    }
}
```

### 6. Error Recovery Strategies

```csharp
public class ErrorRecoveryManager
{
    private readonly ILogger<ErrorRecoveryManager> _logger;
    private readonly IEventManager _eventManager;
    
    public ErrorRecoveryManager(ILogger<ErrorRecoveryManager> logger, IEventManager eventManager)
    {
        _logger = logger;
        _eventManager = eventManager;
    }
    
    public async Task<bool> TryRecoverAsync(Exception exception, AgentContext context)
    {
        try
        {
            switch (exception)
            {
                case LLMRateLimitException ex:
                    return await HandleRateLimitRecovery(ex, context);
                    
                case ToolExecutionException ex:
                    return await HandleToolRecovery(ex, context);
                    
                case StateCorruptionException ex:
                    return await HandleStateRecovery(ex, context);
                    
                default:
                    return await HandleGenericRecovery(exception, context);
            }
        }
        catch (Exception recoveryEx)
        {
            _logger.LogError(recoveryEx, "Error recovery failed");
            return false;
        }
    }
    
    private async Task<bool> HandleRateLimitRecovery(LLMRateLimitException ex, AgentContext context)
    {
        _logger.LogInformation("Attempting rate limit recovery");
        
        // Wait for rate limit to reset
        await Task.Delay(TimeSpan.FromMinutes(1));
        
        // Retry with exponential backoff
        return true;
    }
    
    private async Task<bool> HandleToolRecovery(ToolExecutionException ex, AgentContext context)
    {
        _logger.LogInformation("Attempting tool recovery: {ToolName}", ex.ToolName);
        
        // Try alternative tool or fallback strategy
        var fallbackResult = await TryFallbackTool(ex.ToolName, context);
        
        if (fallbackResult)
        {
            _eventManager.Publish(new ToolRecoveryEvent
            {
                ToolName = ex.ToolName,
                RecoveryStrategy = "FallbackTool",
                Success = true
            });
        }
        
        return fallbackResult;
    }
    
    private async Task<bool> HandleStateRecovery(StateCorruptionException ex, AgentContext context)
    {
        _logger.LogWarning("Attempting state recovery");
        
        // Restore from backup or reset state
        await context.StateManager.ResetStateAsync(context.AgentId);
        
        return true;
    }
    
    private async Task<bool> HandleGenericRecovery(Exception ex, AgentContext context)
    {
        _logger.LogInformation("Attempting generic recovery");
        
        // Implement generic recovery strategies
        return false;
    }
    
    private async Task<bool> TryFallbackTool(string failedToolName, AgentContext context)
    {
        // Implementation to try alternative tools
        return false;
    }
}
```

### 7. Error Monitoring and Alerting

```csharp
public class ErrorMonitoringService
{
    private readonly ILogger<ErrorMonitoringService> _logger;
    private readonly IEventManager _eventManager;
    private readonly IAlertService _alertService;
    private readonly Dictionary<string, ErrorStats> _errorStats = new();
    
    public ErrorMonitoringService(
        ILogger<ErrorMonitoringService> logger,
        IEventManager eventManager,
        IAlertService alertService)
    {
        _logger = logger;
        _eventManager = eventManager;
        _alertService = alertService;
        
        SubscribeToErrorEvents();
    }
    
    private void SubscribeToErrorEvents()
    {
        _eventManager.Subscribe<AgentErrorEvent>(OnAgentError);
        _eventManager.Subscribe<ToolExecutionErrorEvent>(OnToolError);
        _eventManager.Subscribe<LLMErrorEvent>(OnLLMError);
    }
    
    private void OnAgentError(AgentErrorEvent e)
    {
        RecordError("Agent", e.AgentId, e.ErrorMessage);
        
        if (ShouldSendAlert("Agent", e.AgentId))
        {
            _alertService.SendAlert(AlertLevel.Critical, 
                $"Agent {e.AgentId} error: {e.ErrorMessage}");
        }
    }
    
    private void OnToolError(ToolExecutionErrorEvent e)
    {
        RecordError("Tool", e.ToolName, e.ErrorMessage);
        
        if (ShouldSendAlert("Tool", e.ToolName))
        {
            _alertService.SendAlert(AlertLevel.Warning,
                $"Tool {e.ToolName} error: {e.ErrorMessage}");
        }
    }
    
    private void OnLLMError(LLMErrorEvent e)
    {
        RecordError("LLM", "LLM", e.ErrorMessage);
        
        if (ShouldSendAlert("LLM", "LLM"))
        {
            _alertService.SendAlert(AlertLevel.Critical,
                $"LLM error: {e.ErrorMessage}");
        }
    }
    
    private void RecordError(string category, string source, string errorMessage)
    {
        var key = $"{category}_{source}";
        
        if (!_errorStats.ContainsKey(key))
        {
            _errorStats[key] = new ErrorStats();
        }
        
        var stats = _errorStats[key];
        stats.ErrorCount++;
        stats.LastError = errorMessage;
        stats.LastErrorTime = DateTime.UtcNow;
        
        // Reset counter if more than 1 hour has passed
        if (DateTime.UtcNow - stats.FirstErrorTime > TimeSpan.FromHours(1))
        {
            stats.ErrorCount = 1;
            stats.FirstErrorTime = DateTime.UtcNow;
        }
    }
    
    private bool ShouldSendAlert(string category, string source)
    {
        var key = $"{category}_{source}";
        
        if (_errorStats.TryGetValue(key, out var stats))
        {
            // Send alert if more than 5 errors in the last hour
            return stats.ErrorCount >= 5 && 
                   DateTime.UtcNow - stats.FirstErrorTime <= TimeSpan.FromHours(1);
        }
        
        return false;
    }
}

public class ErrorStats
{
    public int ErrorCount { get; set; }
    public string LastError { get; set; }
    public DateTime LastErrorTime { get; set; }
    public DateTime FirstErrorTime { get; set; } = DateTime.UtcNow;
}

public enum AlertLevel
{
    Info,
    Warning,
    Critical
}
```

## Best Practices Summary

### 1. Defensive Programming
- Always validate inputs and parameters
- Implement proper null checks
- Use strong typing to prevent runtime errors
- Add comprehensive logging at all levels

### 2. Graceful Degradation
- Provide fallback mechanisms for critical services
- Implement circuit breakers to prevent cascade failures
- Use default values when external services are unavailable
- Maintain core functionality even when optional features fail

### 3. Retry Strategies
- Use exponential backoff with jitter
- Implement appropriate retry limits
- Distinguish between retryable and non-retryable errors
- Consider different retry strategies for different error types

### 4. Monitoring and Alerting
- Track error rates and patterns
- Set up appropriate alerting thresholds
- Monitor system health and performance
- Implement automated incident response

### 5. Error Classification
- Categorize errors by severity and impact
- Implement different handling strategies for different error types
- Use appropriate logging levels for different error categories
- Provide meaningful error messages to users

### 6. State Management
- Implement proper state validation
- Use transactions where appropriate
- Implement rollback mechanisms
- Maintain data consistency during error recovery

### 7. Testing Error Scenarios
- Test all error handling paths
- Simulate various failure scenarios
- Verify retry mechanisms work correctly
- Test error recovery procedures

## Common Anti-patterns to Avoid

1. **Swallowing Exceptions**: Never catch exceptions without proper handling
2. **Generic Exception Handling**: Avoid catching all exceptions with a single handler
3. **Infinite Retries**: Always implement retry limits and backoff strategies
4. **Silent Failures**: Always log errors and provide meaningful feedback
5. **Ignoring Resource Cleanup**: Ensure proper disposal of resources even during errors
6. **Hard-coded Error Messages**: Use configurable error messages and localization

## Next Steps

- Implement comprehensive error handling in your agents
- Set up monitoring and alerting systems
- Create error recovery procedures
- Test error scenarios thoroughly
- Document error handling strategies for your team

This guide provides a foundation for building robust, error-resistant AIAgentSharp applications that can handle failures gracefully and maintain high availability.
