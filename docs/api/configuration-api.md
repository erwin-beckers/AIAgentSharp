# Configuration API Reference

This document provides comprehensive API reference for configuring AIAgentSharp agents and components.

## Core Configuration Classes

### AgentConfiguration

Main configuration class for agent behavior and settings.

```csharp
public class AgentConfiguration
{
    public int MaxTurns { get; set; } = 40;
    public bool EnableValidation { get; set; } = true;
    public double ValidationThreshold { get; set; } = 0.7;
    public TimeSpan LlmTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan ToolTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public bool UseFunctionCalling { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableEvents { get; set; } = true;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public Dictionary<string, object?> CustomSettings { get; set; } = new();
}
```

#### Properties

- **MaxTurns**: Maximum number of turns before stopping (default: 40)
- **EnableValidation**: Whether to validate reasoning steps (default: true)
- **ValidationThreshold**: Minimum confidence for validation (default: 0.7)
- **LlmTimeout**: Timeout for LLM calls (default: 5 minutes)
- **ToolTimeout**: Timeout for tool execution (default: 2 minutes)
- **UseFunctionCalling**: Use native function calling vs JSON parsing (default: true)
- **EnableMetrics**: Enable metrics collection (default: true)
- **EnableEvents**: Enable event emission (default: true)
- **LogLevel**: Logging verbosity level (default: Information)
- **CustomSettings**: Additional custom configuration options

## Reasoning Configuration

### ChainOfThoughtConfiguration

Configuration specific to Chain of Thought reasoning.

```csharp
public class ChainOfThoughtConfiguration
{
    public int MaxSteps { get; set; } = 10;
    public bool EnableValidation { get; set; } = true;
    public double ValidationThreshold { get; set; } = 0.7;
    public bool AllowStepAddition { get; set; } = true;
    public TimeSpan StepTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public bool EnableInsights { get; set; } = true;
}
```

### TreeOfThoughtsConfiguration

Configuration for Tree of Thoughts reasoning.

```csharp
public class TreeOfThoughtsConfiguration
{
    public int MaxDepth { get; set; } = 5;
    public int MaxBranches { get; set; } = 3;
    public double PruningThreshold { get; set; } = 0.3;
    public ExplorationStrategy Strategy { get; set; } = ExplorationStrategy.BestFirst;
    public int BeamWidth { get; set; } = 2;
    public double ExplorationWeight { get; set; } = 1.4;
    public TimeSpan NodeTimeout { get; set; } = TimeSpan.FromMinutes(1);
}
```

#### Exploration Strategies

```csharp
public enum ExplorationStrategy
{
    BestFirst,
    BreadthFirst,
    DepthFirst,
    BeamSearch,
    MonteCarlo
}
```

## LLM Provider Configuration

### OpenAI Configuration

```csharp
public class OpenAIConfiguration
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o";
    public string? BaseUrl { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public string[]? Stop { get; set; }
    public double TopP { get; set; } = 1.0;
    public double FrequencyPenalty { get; set; } = 0.0;
    public double PresencePenalty { get; set; } = 0.0;
    public Dictionary<string, object?> AdditionalOptions { get; set; } = new();
}
```

### Anthropic Configuration

```csharp
public class AnthropicConfiguration
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-3-sonnet-20240229";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
    public string[]? StopSequences { get; set; }
    public double TopP { get; set; } = 1.0;
    public int TopK { get; set; } = -1;
    public Dictionary<string, object?> AdditionalOptions { get; set; } = new();
}
```

### Gemini Configuration

```csharp
public class GeminiConfiguration
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-pro";
    public double Temperature { get; set; } = 0.7;
    public int MaxOutputTokens { get; set; } = 4096;
    public int TopK { get; set; } = 40;
    public double TopP { get; set; } = 0.95;
    public string[]? StopSequences { get; set; }
    public Dictionary<string, object?> AdditionalOptions { get; set; } = new();
}
```

## Event System Configuration

### EventConfiguration

```csharp
public class EventConfiguration
{
    public bool EnableRunEvents { get; set; } = true;
    public bool EnableStepEvents { get; set; } = true;
    public bool EnableLlmEvents { get; set; } = true;
    public bool EnableToolEvents { get; set; } = true;
    public bool EnableStatusEvents { get; set; } = true;
    public bool EnableChunkEvents { get; set; } = true;
    public TimeSpan EventTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxEventQueueSize { get; set; } = 1000;
}
```

## Metrics Configuration

### MetricsConfiguration

```csharp
public class MetricsConfiguration
{
    public bool EnableExecutionTime { get; set; } = true;
    public bool EnableTokenUsage { get; set; } = true;
    public bool EnableApiCalls { get; set; } = true;
    public bool EnableErrorTracking { get; set; } = true;
    public bool EnablePerformanceMetrics { get; set; } = true;
    public TimeSpan MetricsFlushInterval { get; set; } = TimeSpan.FromMinutes(1);
    public string? MetricsOutputPath { get; set; }
}
```

## State Management Configuration

### StateConfiguration

```csharp
public class StateConfiguration
{
    public bool EnablePersistence { get; set; } = true;
    public string? StoragePath { get; set; }
    public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(1);
    public bool CompressState { get; set; } = true;
    public int MaxHistorySize { get; set; } = 1000;
    public bool EnableEncryption { get; set; } = false;
    public string? EncryptionKey { get; set; }
}
```

## Configuration Builder

### AgentConfigurationBuilder

Fluent builder for creating agent configurations.

```csharp
public class AgentConfigurationBuilder
{
    public AgentConfigurationBuilder WithMaxTurns(int maxTurns)
    public AgentConfigurationBuilder WithValidation(bool enabled, double threshold = 0.7)
    public AgentConfigurationBuilder WithTimeouts(TimeSpan llmTimeout, TimeSpan toolTimeout)
    public AgentConfigurationBuilder WithFunctionCalling(bool enabled)
    public AgentConfigurationBuilder WithMetrics(bool enabled)
    public AgentConfigurationBuilder WithEvents(bool enabled)
    public AgentConfigurationBuilder WithLogLevel(LogLevel level)
    public AgentConfigurationBuilder WithCustomSetting(string key, object? value)
    public AgentConfiguration Build()
}
```

## Configuration Examples

### Basic Agent Configuration

```csharp
var config = new AgentConfigurationBuilder()
    .WithMaxTurns(20)
    .WithValidation(enabled: true, threshold: 0.8)
    .WithTimeouts(
        llmTimeout: TimeSpan.FromMinutes(3),
        toolTimeout: TimeSpan.FromMinutes(1))
    .WithLogLevel(LogLevel.Debug)
    .Build();
```

### Advanced Configuration

```csharp
var config = new AgentConfiguration
{
    MaxTurns = 50,
    EnableValidation = true,
    ValidationThreshold = 0.75,
    LlmTimeout = TimeSpan.FromMinutes(10),
    ToolTimeout = TimeSpan.FromMinutes(5),
    UseFunctionCalling = true,
    EnableMetrics = true,
    EnableEvents = true,
    LogLevel = LogLevel.Information,
    CustomSettings = new Dictionary<string, object?>
    {
        ["experimental_features"] = true,
        ["custom_retry_count"] = 3,
        ["memory_limit_mb"] = 512
    }
};
```

### Chain of Thought Configuration

```csharp
var cotConfig = new ChainOfThoughtConfiguration
{
    MaxSteps = 15,
    EnableValidation = true,
    ValidationThreshold = 0.8,
    AllowStepAddition = true,
    StepTimeout = TimeSpan.FromMinutes(3),
    EnableInsights = true
};

var agent = new Agent(llmClient, config, cotConfig);
```

### Tree of Thoughts Configuration

```csharp
var totConfig = new TreeOfThoughtsConfiguration
{
    MaxDepth = 4,
    MaxBranches = 4,
    PruningThreshold = 0.4,
    Strategy = ExplorationStrategy.BeamSearch,
    BeamWidth = 3,
    ExplorationWeight = 1.2,
    NodeTimeout = TimeSpan.FromSeconds(90)
};
```

## Environment Variables

### Supported Environment Variables

- **LLM_API_KEY**: Default API key for LLM providers
- **AGENT_MAX_TURNS**: Default maximum turns
- **AGENT_LOG_LEVEL**: Default logging level
- **AGENT_TIMEOUT_MINUTES**: Default timeout in minutes
- **AGENT_ENABLE_METRICS**: Enable/disable metrics (true/false)
- **AGENT_STORAGE_PATH**: Default storage path for state

### Loading from Environment

```csharp
var config = AgentConfiguration.FromEnvironment();

// Or with overrides
var config = AgentConfiguration.FromEnvironment()
    .WithMaxTurns(30)
    .WithValidation(true, 0.8)
    .Build();
```

## Configuration Files

### JSON Configuration

```json
{
  "agent": {
    "maxTurns": 40,
    "enableValidation": true,
    "validationThreshold": 0.7,
    "llmTimeout": "00:05:00",
    "toolTimeout": "00:02:00",
    "useFunctionCalling": true,
    "enableMetrics": true,
    "enableEvents": true,
    "logLevel": "Information"
  },
  "llm": {
    "provider": "openai",
    "model": "gpt-4o",
    "temperature": 0.7,
    "maxTokens": 4096
  },
  "reasoning": {
    "chainOfThought": {
      "maxSteps": 10,
      "enableValidation": true,
      "validationThreshold": 0.7
    }
  }
}
```

### Loading from File

```csharp
var config = AgentConfiguration.FromFile("appsettings.json");
```

## Best Practices

1. **Start Simple**: Begin with default configurations and adjust as needed
2. **Environment-Specific**: Use different configurations for development/production
3. **Security**: Store API keys in environment variables, not configuration files
4. **Performance**: Adjust timeouts based on your use case
5. **Monitoring**: Enable metrics and events for production deployments
6. **Validation**: Use appropriate validation thresholds for your domain

## See Also

- [Agent Framework](../agent-framework.md) - Overview of agent configuration
- [Quick Start Guide](../quick-start.md) - Basic configuration examples
- [Best Practices](../best-practices/) - Configuration best practices

