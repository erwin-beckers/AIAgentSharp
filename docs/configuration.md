# Configuration

AIAgentSharp provides extensive configuration options to customize agent behavior, performance, and functionality for your specific use cases.

## Overview

The `AgentConfiguration` class allows you to configure:
- **Reasoning behavior** and strategies
- **Performance settings** and limits
- **State management** and persistence
- **Event handling** and monitoring
- **Tool usage** and function calling
- **Error handling** and recovery
- **Metrics collection** and reporting

## Basic Configuration

### Default Configuration

Create an agent with default settings:

```csharp
var agent = new Agent(llm, store);
```

### Custom Configuration

Configure specific settings:

```csharp
var config = new AgentConfiguration
{
    MaxTurns = 20,
    EnableReasoning = true,
    ReasoningType = ReasoningType.ChainOfThought,
    UseFunctionCalling = true
};

var agent = new Agent(llm, store, config: config);
```

## Reasoning Configuration

### Enable Reasoning

Configure reasoning capabilities:

```csharp
var config = new AgentConfiguration
{
    EnableReasoning = true,
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true
};
```

### Reasoning Types

Choose from different reasoning strategies:

```csharp
// Chain of Thought reasoning
var cotConfig = new AgentConfiguration
{
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 8
};

// Tree of Thoughts reasoning
var totConfig = new AgentConfiguration
{
    ReasoningType = ReasoningType.TreeOfThoughts,
    MaxReasoningSteps = 15,
    TreeOfThoughtsConfig = new TreeOfThoughtsConfiguration
    {
        MaxBranches = 5,
        ExplorationStrategy = ExplorationStrategy.BreadthFirst
    }
};
```

### Reasoning Validation

Configure reasoning validation:

```csharp
var config = new AgentConfiguration
{
    EnableReasoningValidation = true,
    ReasoningValidationThreshold = 0.7,
    RequireReasoningConfidence = true
};
```

## Performance Configuration

### Turn Limits

Control the maximum number of turns:

```csharp
var config = new AgentConfiguration
{
    MaxTurns = 30,           // Maximum turns per run
    MaxConcurrentTurns = 5   // Maximum concurrent turns
};
```

### Timeout Settings

Configure timeouts for operations:

```csharp
var config = new AgentConfiguration
{
    LlmTimeout = TimeSpan.FromMinutes(2),
    ToolTimeout = TimeSpan.FromSeconds(30),
    TotalTimeout = TimeSpan.FromMinutes(10)
};
```

### Memory Management

Configure memory usage:

```csharp
var config = new AgentConfiguration
{
    MaxHistoryLength = 50,
    EnableHistorySummarization = true,
    HistorySummarizationThreshold = 20,
    MaxMemoryUsageMB = 512
};
```

## State Management Configuration

### State Persistence

Configure state persistence behavior:

```csharp
var config = new AgentConfiguration
{
    PersistStateAfterEachTurn = true,
    PersistStateInterval = 3,        // Save every 3 turns
    EnableStateCompression = true,
    StateCompressionLevel = 6
};
```

### State Cleanup

Configure automatic state cleanup:

```csharp
var config = new AgentConfiguration
{
    EnableAutoStateCleanup = true,
    StateCleanupInterval = TimeSpan.FromHours(24),
    MaxStateAge = TimeSpan.FromDays(7),
    CleanupOldStates = true
};
```

## Tool Configuration

### Function Calling

Configure function calling behavior:

```csharp
var config = new AgentConfiguration
{
    UseFunctionCalling = true,
    PreferFunctionCalling = true,
    MaxToolCallsPerTurn = 3,
    EnableToolValidation = true
};
```

### Tool Selection

Configure tool selection strategy:

```csharp
var config = new AgentConfiguration
{
    ToolSelectionStrategy = ToolSelectionStrategy.Intelligent,
    EnableToolCaching = true,
    ToolCacheExpiration = TimeSpan.FromMinutes(30)
};
```

## Event System Configuration

### Event Handling

Configure event system behavior:

```csharp
var config = new AgentConfiguration
{
    EmitPublicStatus = true,
    EmitDetailedEvents = true,
    EventBufferSize = 1000,
    EnableEventPersistence = true
};
```

### Status Updates

Configure status update behavior:

```csharp
var config = new AgentConfiguration
{
    StatusUpdateInterval = TimeSpan.FromSeconds(2),
    EnableProgressTracking = true,
    ShowDetailedProgress = true
};
```

## Error Handling Configuration

### Error Recovery

Configure error handling and recovery:

```csharp
var config = new AgentConfiguration
{
    MaxRetries = 3,
    RetryDelay = TimeSpan.FromSeconds(1),
    EnableErrorRecovery = true,
    ContinueOnError = false
};
```

### Error Reporting

Configure error reporting:

```csharp
var config = new AgentConfiguration
{
    EnableErrorLogging = true,
    LogErrorDetails = true,
    ErrorReportingLevel = ErrorReportingLevel.Detailed
};
```

## Metrics Configuration

### Metrics Collection

Configure metrics collection:

```csharp
var config = new AgentConfiguration
{
    EnableMetricsCollection = true,
    EnableMetricsPersistence = true,
    MetricsStoragePath = "agent-metrics",
    MetricsCollectionInterval = TimeSpan.FromSeconds(5)
};
```

### Performance Thresholds

Set performance alert thresholds:

```csharp
var config = new AgentConfiguration
{
    MetricsAlertThresholds = new MetricsAlertThresholds
    {
        MaxResponseTimeMs = 5000,
        MaxErrorRate = 0.1,
        MaxTokenUsage = 10000,
        MinSuccessRate = 0.8
    }
};
```

## Security Configuration

### API Key Management

Configure secure API key handling:

```csharp
var config = new AgentConfiguration
{
    EnableSecureKeyStorage = true,
    KeyRotationInterval = TimeSpan.FromDays(30),
    MaskSensitiveData = true
};
```

### Input Validation

Configure input validation:

```csharp
var config = new AgentConfiguration
{
    EnableInputValidation = true,
    MaxInputLength = 10000,
    SanitizeInput = true,
    BlockMaliciousInput = true
};
```

## Advanced Configuration

### Custom Settings

Add custom configuration settings:

```csharp
var config = new AgentConfiguration
{
    CustomSettings = new Dictionary<string, object>
    {
        ["business_rules"] = "strict",
        ["language"] = "en-US",
        ["timezone"] = "UTC",
        ["debug_mode"] = false
    }
};
```

### Environment-Specific Configuration

Configure for different environments:

```csharp
// Development configuration
var devConfig = new AgentConfiguration
{
    MaxTurns = 50,
    EnableDetailedLogging = true,
    DebugMode = true,
    MetricsCollectionInterval = TimeSpan.FromSeconds(1)
};

// Production configuration
var prodConfig = new AgentConfiguration
{
    MaxTurns = 20,
    EnableDetailedLogging = false,
    DebugMode = false,
    MetricsCollectionInterval = TimeSpan.FromMinutes(5),
    EnableMetricsPersistence = true
};
```

## Configuration Validation

### Validate Configuration

Validate configuration before use:

```csharp
var config = new AgentConfiguration
{
    MaxTurns = 30,
    ReasoningType = ReasoningType.ChainOfThought
};

// Validate configuration
var validationResult = config.Validate();
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Configuration error: {error}");
    }
}
```

### Configuration Profiles

Use predefined configuration profiles:

```csharp
// Use predefined profile
var config = AgentConfiguration.CreateProfile(ConfigurationProfile.Production);

// Or create custom profile
var customProfile = new AgentConfiguration
{
    // Production settings
    MaxTurns = 20,
    EnableMetricsPersistence = true,
    EnableErrorRecovery = true,
    
    // Custom settings
    CustomSettings = new Dictionary<string, object>
    {
        ["environment"] = "production",
        ["version"] = "1.0.0"
    }
};
```

## Configuration Examples

### Complete Configuration Example

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.Configuration;

// Create comprehensive configuration
var config = new AgentConfiguration
{
    // Reasoning settings
    EnableReasoning = true,
    ReasoningType = ReasoningType.ChainOfThought,
    MaxReasoningSteps = 10,
    EnableReasoningValidation = true,
    
    // Performance settings
    MaxTurns = 25,
    LlmTimeout = TimeSpan.FromMinutes(2),
    ToolTimeout = TimeSpan.FromSeconds(30),
    
    // State management
    PersistStateAfterEachTurn = true,
    MaxHistoryLength = 40,
    EnableHistorySummarization = true,
    
    // Tool settings
    UseFunctionCalling = true,
    MaxToolCallsPerTurn = 3,
    
    // Event system
    EmitPublicStatus = true,
    StatusUpdateInterval = TimeSpan.FromSeconds(1),
    
    // Error handling
    MaxRetries = 3,
    EnableErrorRecovery = true,
    
    // Metrics
    EnableMetricsCollection = true,
    EnableMetricsPersistence = true,
    
    // Security
    EnableInputValidation = true,
    MaskSensitiveData = true,
    
    // Custom settings
    CustomSettings = new Dictionary<string, object>
    {
        ["application"] = "travel-planner",
        ["version"] = "2.1.0"
    }
};

// Create agent with configuration
var agent = new Agent(llm, store, config: config);
```

### Environment-Specific Configurations

```csharp
// Development configuration
public static AgentConfiguration GetDevelopmentConfig()
{
    return new AgentConfiguration
    {
        MaxTurns = 50,
        EnableDetailedLogging = true,
        DebugMode = true,
        EnableMetricsCollection = true,
        MetricsCollectionInterval = TimeSpan.FromSeconds(1),
        CustomSettings = new Dictionary<string, object>
        {
            ["environment"] = "development",
            ["log_level"] = "debug"
        }
    };
}

// Production configuration
public static AgentConfiguration GetProductionConfig()
{
    return new AgentConfiguration
    {
        MaxTurns = 20,
        EnableDetailedLogging = false,
        DebugMode = false,
        EnableMetricsCollection = true,
        EnableMetricsPersistence = true,
        MetricsCollectionInterval = TimeSpan.FromMinutes(5),
        EnableErrorRecovery = true,
        CustomSettings = new Dictionary<string, object>
        {
            ["environment"] = "production",
            ["log_level"] = "info"
        }
    };
}
```

This comprehensive configuration system allows you to fine-tune every aspect of your agents to meet your specific requirements and optimize performance for your use case.
