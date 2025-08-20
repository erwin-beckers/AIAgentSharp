# Common Issues and Solutions

This guide covers the most frequently encountered issues when working with AIAgentSharp and provides step-by-step solutions to resolve them.

## Overview

Common issues in AIAgentSharp applications typically fall into these categories:
- LLM API connectivity and authentication
- Tool execution failures
- Performance and timeout issues
- State management problems
- Configuration errors
- Memory and resource issues

## LLM Provider Issues

### 1. API Key Authentication Errors

**Problem**: `LLMAuthenticationException: Invalid API key`

**Symptoms**:
- 401 Unauthorized errors
- Authentication failed messages
- API key not found errors

**Solutions**:

```csharp
// Check API key configuration
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("OpenAI API key not found in environment variables");
}

// Verify API key format
if (!apiKey.StartsWith("sk-"))
{
    throw new InvalidOperationException("Invalid OpenAI API key format");
}
```

**Prevention**:
- Store API keys in secure environment variables
- Use Azure Key Vault or similar secure storage
- Never commit API keys to source control
- Implement API key rotation

### 2. Rate Limiting Issues

**Problem**: `LLMRateLimitException: Rate limit exceeded`

**Symptoms**:
- 429 Too Many Requests errors
- Rate limit exceeded messages
- Intermittent failures

**Solutions**:

```csharp
public class RateLimitHandler
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<DateTime> _requestTimes;
    private readonly int _maxRequestsPerMinute;
    
    public RateLimitHandler(int maxRequestsPerMinute = 60)
    {
        _semaphore = new SemaphoreSlim(1, 1);
        _requestTimes = new Queue<DateTime>();
        _maxRequestsPerMinute = maxRequestsPerMinute;
    }
    
    public async Task<T> ExecuteWithRateLimitAsync<T>(Func<Task<T>> operation)
    {
        await _semaphore.WaitAsync();
        try
        {
            await WaitForRateLimit();
            var result = await operation();
            RecordRequest();
            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task WaitForRateLimit()
    {
        var now = DateTime.UtcNow;
        var oneMinuteAgo = now.AddMinutes(-1);
        
        // Remove old requests
        while (_requestTimes.Count > 0 && _requestTimes.Peek() < oneMinuteAgo)
        {
            _requestTimes.Dequeue();
        }
        
        // Wait if rate limit exceeded
        if (_requestTimes.Count >= _maxRequestsPerMinute)
        {
            var oldestRequest = _requestTimes.Peek();
            var waitTime = oldestRequest.AddMinutes(1) - now;
            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime);
            }
        }
    }
    
    private void RecordRequest()
    {
        _requestTimes.Enqueue(DateTime.UtcNow);
    }
}
```

**Prevention**:
- Implement exponential backoff retry logic
- Use request queuing for high-volume applications
- Monitor API usage and set appropriate limits
- Consider using multiple API keys for load distribution

### 3. Token Limit Exceeded

**Problem**: `LLMTokenLimitException: Token limit exceeded`

**Symptoms**:
- 400 Bad Request errors
- Token limit exceeded messages
- Large prompts failing

**Solutions**:

```csharp
public class TokenLimitHandler
{
    private readonly int _maxTokens;
    private readonly ITokenizer _tokenizer;
    
    public TokenLimitHandler(int maxTokens = 4000, ITokenizer tokenizer = null)
    {
        _maxTokens = maxTokens;
        _tokenizer = tokenizer ?? new DefaultTokenizer();
    }
    
    public string TruncatePrompt(string prompt, int reservedTokens = 100)
    {
        var tokens = _tokenizer.Tokenize(prompt);
        var maxPromptTokens = _maxTokens - reservedTokens;
        
        if (tokens.Count <= maxPromptTokens)
        {
            return prompt;
        }
        
        // Truncate from the beginning (keep most recent content)
        var truncatedTokens = tokens.Skip(tokens.Count - maxPromptTokens).ToList();
        return _tokenizer.Detokenize(truncatedTokens);
    }
    
    public LLMRequest CreateTruncatedRequest(string prompt, int reservedTokens = 100)
    {
        var truncatedPrompt = TruncatePrompt(prompt, reservedTokens);
        
        return new LLMRequest
        {
            Prompt = truncatedPrompt,
            MaxTokens = _maxTokens,
            Temperature = 0.7f
        };
    }
}
```

**Prevention**:
- Monitor prompt length and token usage
- Implement prompt summarization for long conversations
- Use streaming responses for large outputs
- Set appropriate token limits

## Tool Execution Issues

### 1. Tool Not Found

**Problem**: `ToolNotFoundException: Tool 'tool_name' not found`

**Symptoms**:
- Tool not registered errors
- Missing tool implementations
- Tool name mismatches

**Solutions**:

```csharp
public class ToolRegistryValidator
{
    private readonly IEnumerable<ITool> _tools;
    
    public ToolRegistryValidator(IEnumerable<ITool> tools)
    {
        _tools = tools;
    }
    
    public void ValidateToolRegistration(string toolName)
    {
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        if (tool == null)
        {
            var availableTools = string.Join(", ", _tools.Select(t => t.Name));
            throw new ToolNotFoundException(
                $"Tool '{toolName}' not found. Available tools: {availableTools}");
        }
    }
    
    public void ValidateAllTools()
    {
        var duplicateNames = _tools
            .GroupBy(t => t.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
            
        if (duplicateNames.Any())
        {
            throw new InvalidOperationException(
                $"Duplicate tool names found: {string.Join(", ", duplicateNames)}");
        }
    }
}
```

**Prevention**:
- Register all tools in dependency injection
- Use consistent tool naming conventions
- Validate tool registration at startup
- Implement tool discovery mechanisms

### 2. Tool Parameter Validation Errors

**Problem**: `ToolParameterException: Invalid parameter 'param_name'`

**Symptoms**:
- Parameter validation failures
- Missing required parameters
- Invalid parameter types

**Solutions**:

```csharp
public class ToolParameterValidator
{
    public ValidationResult ValidateParameters(ToolParameters parameters, ToolDefinition definition)
    {
        var result = new ValidationResult();
        
        foreach (var param in definition.Parameters)
        {
            if (param.Required && !parameters.HasParameter(param.Name))
            {
                result.AddError($"Required parameter '{param.Name}' is missing");
                continue;
            }
            
            if (parameters.HasParameter(param.Name))
            {
                var value = parameters.GetParameter(param.Name);
                if (!IsValidType(value, param.Type))
                {
                    result.AddError($"Parameter '{param.Name}' has invalid type. Expected: {param.Type}");
                }
            }
        }
        
        return result;
    }
    
    private bool IsValidType(object value, string expectedType)
    {
        return expectedType.ToLower() switch
        {
            "string" => value is string,
            "integer" => value is int,
            "number" => value is double || value is int,
            "boolean" => value is bool,
            _ => true
        };
    }
}
```

**Prevention**:
- Define clear parameter schemas
- Implement comprehensive parameter validation
- Provide helpful error messages
- Use strongly-typed parameter classes

### 3. External API Failures

**Problem**: `ToolExecutionException: External API call failed`

**Symptoms**:
- Network connectivity issues
- External service unavailability
- Timeout errors

**Solutions**:

```csharp
public class ResilientToolExecutor
{
    private readonly IRetryPolicy _retryPolicy;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILogger _logger;
    
    public ResilientToolExecutor(ILogger logger)
    {
        _retryPolicy = new ExponentialBackoffRetryPolicy();
        _circuitBreaker = new CircuitBreaker();
        _logger = logger;
    }
    
    public async Task<ToolResult> ExecuteWithResilienceAsync(Func<Task<ToolResult>> operation)
    {
        if (_circuitBreaker.IsOpen)
        {
            return new ToolResult
            {
                Success = false,
                ErrorMessage = "Circuit breaker is open - service temporarily unavailable"
            };
        }
        
        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    _circuitBreaker.RecordFailure();
                    throw;
                }
            });
            
            _circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool execution failed after retries");
            return new ToolResult
            {
                Success = false,
                ErrorMessage = $"Tool execution failed: {ex.Message}"
            };
        }
    }
}
```

**Prevention**:
- Implement retry logic with exponential backoff
- Use circuit breakers for external services
- Monitor external API health
- Implement fallback mechanisms

## Performance Issues

### 1. Slow Response Times

**Problem**: Agent responses taking too long

**Symptoms**:
- Response times > 30 seconds
- Timeout errors
- Poor user experience

**Solutions**:

```csharp
public class PerformanceOptimizer
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, PerformanceMetrics> _metrics;
    
    public PerformanceOptimizer(ILogger logger)
    {
        _logger = logger;
        _metrics = new Dictionary<string, PerformanceMetrics>();
    }
    
    public async Task<T> ExecuteWithMonitoringAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            RecordMetrics(operationName, stopwatch.Elapsed, true);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordMetrics(operationName, stopwatch.Elapsed, false);
            throw;
        }
    }
    
    private void RecordMetrics(string operationName, TimeSpan duration, bool success)
    {
        if (!_metrics.ContainsKey(operationName))
        {
            _metrics[operationName] = new PerformanceMetrics();
        }
        
        var metrics = _metrics[operationName];
        metrics.AddExecution(duration, success);
        
        if (duration.TotalSeconds > 10)
        {
            _logger.LogWarning("Slow operation detected: {Operation} took {Duration}ms", 
                operationName, duration.TotalMilliseconds);
        }
    }
    
    public PerformanceReport GenerateReport()
    {
        return new PerformanceReport
        {
            Operations = _metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.GetSummary()
            )
        };
    }
}
```

**Prevention**:
- Monitor response times and set alerts
- Optimize prompt engineering
- Use caching for repeated requests
- Implement request prioritization

### 2. Memory Leaks

**Problem**: Memory usage increasing over time

**Symptoms**:
- OutOfMemoryException errors
- High memory usage
- Application slowdown

**Solutions**:

```csharp
public class MemoryMonitor
{
    private readonly ILogger _logger;
    private readonly long _memoryThreshold;
    private readonly Timer _monitorTimer;
    
    public MemoryMonitor(ILogger logger, long memoryThresholdMB = 500)
    {
        _logger = logger;
        _memoryThreshold = memoryThresholdMB * 1024 * 1024; // Convert to bytes
        _monitorTimer = new Timer(MonitorMemory, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }
    
    private void MonitorMemory(object state)
    {
        var currentMemory = GC.GetTotalMemory(false);
        
        if (currentMemory > _memoryThreshold)
        {
            _logger.LogWarning("High memory usage detected: {MemoryMB}MB", 
                currentMemory / (1024 * 1024));
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterGC = GC.GetTotalMemory(false);
            _logger.LogInformation("Memory after GC: {MemoryMB}MB", 
                afterGC / (1024 * 1024));
        }
    }
    
    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}
```

**Prevention**:
- Implement proper disposal patterns
- Use weak references for caching
- Monitor memory usage
- Implement memory limits

## State Management Issues

### 1. State Corruption

**Problem**: Agent state becoming inconsistent

**Symptoms**:
- Unexpected agent behavior
- State serialization errors
- Data loss

**Solutions**:

```csharp
public class StateValidator
{
    private readonly ILogger _logger;
    
    public StateValidator(ILogger logger)
    {
        _logger = logger;
    }
    
    public bool ValidateState<T>(T state) where T : class
    {
        if (state == null)
        {
            _logger.LogWarning("State is null");
            return false;
        }
        
        // Validate state properties
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(state);
            if (value == null && IsRequired(property))
            {
                _logger.LogWarning("Required property '{Property}' is null", property.Name);
                return false;
            }
        }
        
        return true;
    }
    
    private bool IsRequired(PropertyInfo property)
    {
        return property.GetCustomAttribute<RequiredAttribute>() != null;
    }
    
    public T RepairState<T>(T state) where T : class, new()
    {
        if (state == null)
        {
            _logger.LogInformation("Creating new state instance");
            return new T();
        }
        
        // Attempt to repair corrupted state
        var repaired = new T();
        var properties = typeof(T).GetProperties();
        
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(state);
                if (value != null)
                {
                    property.SetValue(repaired, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy property '{Property}'", property.Name);
            }
        }
        
        return repaired;
    }
}
```

**Prevention**:
- Implement state validation
- Use versioning for state schemas
- Implement state backup and recovery
- Monitor state consistency

### 2. State Persistence Failures

**Problem**: State not being saved or loaded correctly

**Symptoms**:
- State loss between sessions
- Serialization errors
- Storage access failures

**Solutions**:

```csharp
public class ResilientStateManager : IAgentStateManager
{
    private readonly IAgentStateManager _primaryStorage;
    private readonly IAgentStateManager _backupStorage;
    private readonly ILogger _logger;
    
    public ResilientStateManager(
        IAgentStateManager primaryStorage,
        IAgentStateManager backupStorage,
        ILogger logger)
    {
        _primaryStorage = primaryStorage;
        _backupStorage = backupStorage;
        _logger = logger;
    }
    
    public async Task SaveStateAsync<T>(string key, T state)
    {
        try
        {
            await _primaryStorage.SaveStateAsync(key, state);
            await _backupStorage.SaveStateAsync(key, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save state to primary storage, trying backup");
            try
            {
                await _backupStorage.SaveStateAsync(key, state);
            }
            catch (Exception backupEx)
            {
                _logger.LogError(backupEx, "Failed to save state to backup storage");
                throw;
            }
        }
    }
    
    public async Task<T> GetStateAsync<T>(string key)
    {
        try
        {
            return await _primaryStorage.GetStateAsync<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load state from primary storage, trying backup");
            try
            {
                var state = await _backupStorage.GetStateAsync<T>(key);
                // Restore to primary storage
                await _primaryStorage.SaveStateAsync(key, state);
                return state;
            }
            catch (Exception backupEx)
            {
                _logger.LogError(backupEx, "Failed to load state from backup storage");
                throw;
            }
        }
    }
}
```

**Prevention**:
- Implement redundant storage
- Use transaction-based saves
- Monitor storage health
- Implement automatic recovery

## Configuration Issues

### 1. Missing Configuration

**Problem**: Required configuration values not found

**Symptoms**:
- Configuration errors at startup
- Missing environment variables
- Invalid configuration values

**Solutions**:

```csharp
public class ConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    
    public ConfigurationValidator(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public void ValidateRequiredSettings()
    {
        var requiredSettings = new[]
        {
            "OpenAI:ApiKey",
            "OpenAI:Model",
            "Agent:MaxTokens",
            "Agent:Temperature"
        };
        
        var missingSettings = new List<string>();
        
        foreach (var setting in requiredSettings)
        {
            var value = _configuration[setting];
            if (string.IsNullOrEmpty(value))
            {
                missingSettings.Add(setting);
            }
        }
        
        if (missingSettings.Any())
        {
            var message = $"Missing required configuration settings: {string.Join(", ", missingSettings)}";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }
    }
    
    public void ValidateConfigurationValues()
    {
        // Validate OpenAI API key format
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey) && !apiKey.StartsWith("sk-"))
        {
            throw new InvalidOperationException("Invalid OpenAI API key format");
        }
        
        // Validate numeric values
        if (!int.TryParse(_configuration["Agent:MaxTokens"], out var maxTokens) || maxTokens <= 0)
        {
            throw new InvalidOperationException("Agent:MaxTokens must be a positive integer");
        }
        
        if (!float.TryParse(_configuration["Agent:Temperature"], out var temperature) || 
            temperature < 0 || temperature > 2)
        {
            throw new InvalidOperationException("Agent:Temperature must be between 0 and 2");
        }
    }
}
```

**Prevention**:
- Validate configuration at startup
- Use strongly-typed configuration classes
- Provide default values where appropriate
- Document required configuration

### 2. Environment-Specific Issues

**Problem**: Configuration working in one environment but not another

**Symptoms**:
- Different behavior across environments
- Environment-specific errors
- Configuration conflicts

**Solutions**:

```csharp
public class EnvironmentConfigurationManager
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    
    public EnvironmentConfigurationManager(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public void LogEnvironmentInfo()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var machineName = Environment.MachineName;
        var osVersion = Environment.OSVersion;
        
        _logger.LogInformation("Environment: {Environment}, Machine: {Machine}, OS: {OS}", 
            environment, machineName, osVersion);
        
        // Log configuration sources
        var configSources = _configuration.GetType()
            .GetProperty("Providers")
            ?.GetValue(_configuration) as IEnumerable<IConfigurationProvider>;
            
        if (configSources != null)
        {
            foreach (var source in configSources)
            {
                _logger.LogDebug("Configuration source: {Source}", source.GetType().Name);
            }
        }
    }
    
    public void ValidateEnvironmentConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        switch (environment?.ToLower())
        {
            case "development":
                ValidateDevelopmentConfiguration();
                break;
            case "staging":
                ValidateStagingConfiguration();
                break;
            case "production":
                ValidateProductionConfiguration();
                break;
            default:
                _logger.LogWarning("Unknown environment: {Environment}", environment);
                break;
        }
    }
    
    private void ValidateDevelopmentConfiguration()
    {
        // Development-specific validation
        if (string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"]))
        {
            _logger.LogWarning("OpenAI API key not configured for development environment");
        }
    }
    
    private void ValidateStagingConfiguration()
    {
        // Staging-specific validation
        if (_configuration["Agent:MaxTokens"] == "4000")
        {
            _logger.LogWarning("Using development token limit in staging environment");
        }
    }
    
    private void ValidateProductionConfiguration()
    {
        // Production-specific validation
        if (string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"]))
        {
            throw new InvalidOperationException("OpenAI API key required in production");
        }
    }
}
```

**Prevention**:
- Use environment-specific configuration files
- Validate configuration per environment
- Use configuration transforms
- Implement configuration testing

## Debugging Tools

### 1. Diagnostic Logger

```csharp
public class DiagnosticLogger
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, object> _context;
    
    public DiagnosticLogger(ILogger logger)
    {
        _logger = logger;
        _context = new Dictionary<string, object>();
    }
    
    public void AddContext(string key, object value)
    {
        _context[key] = value;
    }
    
    public void LogDiagnostic(string message, LogLevel level = LogLevel.Information)
    {
        var contextString = string.Join(", ", _context.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        _logger.Log(level, "{Message} | Context: {Context}", message, contextString);
    }
    
    public void LogPerformance(string operation, TimeSpan duration)
    {
        LogDiagnostic($"Operation '{operation}' completed in {duration.TotalMilliseconds}ms");
    }
    
    public void LogError(Exception ex, string operation)
    {
        LogDiagnostic($"Error in operation '{operation}': {ex.Message}", LogLevel.Error);
        _logger.LogError(ex, "Detailed error information for operation '{Operation}'", operation);
    }
}
```

### 2. Health Checker

```csharp
public class AgentHealthChecker
{
    private readonly ILLMClient _llmClient;
    private readonly IAgentStateManager _stateManager;
    private readonly IEnumerable<ITool> _tools;
    private readonly ILogger _logger;
    
    public AgentHealthChecker(
        ILLMClient llmClient,
        IAgentStateManager stateManager,
        IEnumerable<ITool> tools,
        ILogger logger)
    {
        _llmClient = llmClient;
        _stateManager = stateManager;
        _tools = tools;
        _logger = logger;
    }
    
    public async Task<HealthReport> CheckHealthAsync()
    {
        var report = new HealthReport();
        
        // Check LLM connectivity
        try
        {
            var testRequest = new LLMRequest { Prompt = "test", MaxTokens = 10 };
            await _llmClient.GenerateAsync(testRequest);
            report.AddComponent("LLM", HealthStatus.Healthy);
        }
        catch (Exception ex)
        {
            report.AddComponent("LLM", HealthStatus.Unhealthy, ex.Message);
        }
        
        // Check state manager
        try
        {
            await _stateManager.SaveStateAsync("health_check", new { timestamp = DateTime.UtcNow });
            await _stateManager.GetStateAsync<object>("health_check");
            report.AddComponent("StateManager", HealthStatus.Healthy);
        }
        catch (Exception ex)
        {
            report.AddComponent("StateManager", HealthStatus.Unhealthy, ex.Message);
        }
        
        // Check tools
        foreach (var tool in _tools)
        {
            try
            {
                var parameters = new ToolParameters();
                await tool.ExecuteAsync(parameters);
                report.AddComponent($"Tool_{tool.Name}", HealthStatus.Healthy);
            }
            catch (Exception ex)
            {
                report.AddComponent($"Tool_{tool.Name}", HealthStatus.Unhealthy, ex.Message);
            }
        }
        
        return report;
    }
}

public class HealthReport
{
    private readonly Dictionary<string, HealthComponent> _components = new();
    
    public void AddComponent(string name, HealthStatus status, string message = null)
    {
        _components[name] = new HealthComponent { Status = status, Message = message };
    }
    
    public bool IsHealthy => _components.Values.All(c => c.Status == HealthStatus.Healthy);
    
    public Dictionary<string, HealthComponent> Components => _components;
}

public class HealthComponent
{
    public HealthStatus Status { get; set; }
    public string Message { get; set; }
}

public enum HealthStatus
{
    Healthy,
    Unhealthy,
    Degraded
}
```

## Prevention Checklist

- [ ] Implement comprehensive error handling
- [ ] Use retry logic with exponential backoff
- [ ] Monitor performance and set alerts
- [ ] Validate configuration at startup
- [ ] Implement health checks
- [ ] Use circuit breakers for external services
- [ ] Monitor memory usage
- [ ] Implement proper logging
- [ ] Use secure configuration management
- [ ] Test error scenarios

## Next Steps

- Set up monitoring and alerting
- Implement automated health checks
- Create runbooks for common issues
- Establish escalation procedures
- Document troubleshooting procedures
- Train team on issue resolution

This guide provides a comprehensive approach to identifying and resolving common issues in AIAgentSharp applications, helping you build more robust and reliable systems.
