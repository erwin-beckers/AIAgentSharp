# Metrics and Monitoring

AIAgentSharp provides comprehensive metrics collection and monitoring capabilities to track agent performance, resource usage, and operational health.

## Overview

The metrics system in AIAgentSharp enables you to:
- **Track performance** metrics in real-time
- **Monitor resource usage** (tokens, API calls, etc.)
- **Analyze agent behavior** and success rates
- **Identify bottlenecks** and optimization opportunities
- **Set up alerts** for critical issues
- **Generate reports** for business intelligence

## Metrics Categories

### Performance Metrics

Track agent execution performance:

```csharp
var metrics = agent.Metrics.GetMetrics();
Console.WriteLine($"Total Agent Runs: {metrics.Performance.TotalAgentRuns}");
Console.WriteLine($"Average Response Time: {metrics.Performance.AverageResponseTimeMs}ms");
Console.WriteLine($"Total Tokens Used: {metrics.Performance.TotalTokens:N0}");
```

### Operational Metrics

Monitor agent operational health:

```csharp
Console.WriteLine($"Success Rate: {metrics.Operational.AgentRunSuccessRate:P2}");
Console.WriteLine($"Error Rate: {metrics.Operational.AgentRunErrorRate:P2}");
Console.WriteLine($"Active Agents: {metrics.Operational.ActiveAgents}");
```

### Resource Metrics

Track resource consumption:

```csharp
Console.WriteLine($"Total API Calls: {metrics.Resources.TotalApiCalls:N0}");
Console.WriteLine($"Total Tokens: {metrics.Resources.TotalTokens:N0}");
Console.WriteLine($"Memory Usage: {metrics.Resources.MemoryUsageMB:F2}MB");
```

## Real-time Monitoring

### Event-based Metrics

Subscribe to real-time metrics updates:

```csharp
// Subscribe to metrics updates
agent.Metrics.MetricsUpdated += (sender, e) =>
{
    Console.WriteLine($"Latest success rate: {e.Metrics.Operational.AgentRunSuccessRate:P2}");
    Console.WriteLine($"Total tokens used: {e.Metrics.Resources.TotalTokens:N0}");
};
```

### Custom Metrics

Add custom metrics for your specific needs:

```csharp
// Add custom metric
agent.Metrics.AddCustomMetric("business_value", 100.0);

// Retrieve custom metrics
var customMetrics = agent.Metrics.GetCustomMetrics();
foreach (var metric in customMetrics)
{
    Console.WriteLine($"{metric.Key}: {metric.Value}");
}
```

## Metrics Collection

### Automatic Collection

Metrics are automatically collected for:
- **Agent runs** (success/failure, duration)
- **API calls** (count, tokens, response time)
- **Tool usage** (calls, success rate)
- **Reasoning steps** (count, confidence)
- **Memory usage** (current, peak)

### Manual Collection

Add custom metrics manually:

```csharp
// Track custom business metrics
agent.Metrics.RecordCustomMetric("user_satisfaction", 4.5);
agent.Metrics.RecordCustomMetric("task_complexity", "high");
agent.Metrics.RecordCustomMetric("processing_time_ms", 1250);
```

## Metrics Storage

### In-Memory Storage

Default storage for development:

```csharp
// Metrics are stored in memory by default
var metrics = agent.Metrics.GetMetrics();
```

### Persistent Storage

Store metrics for long-term analysis:

```csharp
// Configure persistent metrics storage
var config = new AgentConfiguration
{
    EnableMetricsPersistence = true,
    MetricsStoragePath = "metrics-data"
};

var agent = new Agent(llm, store, config: config);
```

### Custom Storage

Implement custom metrics storage:

```csharp
public class DatabaseMetricsStorage : IMetricsStorage
{
    public async Task SaveMetricsAsync(AgentMetrics metrics, CancellationToken ct = default)
    {
        // Save to database
    }

    public async Task<AgentMetrics> LoadMetricsAsync(CancellationToken ct = default)
    {
        // Load from database
    }
}
```

## Performance Monitoring

### Response Time Tracking

Monitor agent response times:

```csharp
var metrics = agent.Metrics.GetMetrics();
Console.WriteLine($"Average Response Time: {metrics.Performance.AverageResponseTimeMs}ms");
Console.WriteLine($"Min Response Time: {metrics.Performance.MinResponseTimeMs}ms");
Console.WriteLine($"Max Response Time: {metrics.Performance.MaxResponseTimeMs}ms");
```

### Token Usage Monitoring

Track token consumption:

```csharp
Console.WriteLine($"Total Input Tokens: {metrics.Resources.TotalInputTokens:N0}");
Console.WriteLine($"Total Output Tokens: {metrics.Resources.TotalOutputTokens:N0}");
Console.WriteLine($"Average Tokens per Run: {metrics.Resources.AverageTokensPerRun:F1}");
```

### API Call Monitoring

Monitor API usage and costs:

```csharp
Console.WriteLine($"Total API Calls: {metrics.Resources.TotalApiCalls:N0}");
Console.WriteLine($"Successful API Calls: {metrics.Resources.SuccessfulApiCalls:N0}");
Console.WriteLine($"Failed API Calls: {metrics.Resources.FailedApiCalls:N0}");
```

## Alerting and Thresholds

### Set Performance Thresholds

Configure alerts for performance issues:

```csharp
var config = new AgentConfiguration
{
    MetricsAlertThresholds = new MetricsAlertThresholds
    {
        MaxResponseTimeMs = 5000,
        MaxErrorRate = 0.1,
        MaxTokenUsage = 10000
    }
};
```

### Custom Alert Handlers

Handle metric alerts:

```csharp
agent.Metrics.MetricsAlert += (sender, e) =>
{
    Console.WriteLine($"ALERT: {e.AlertType} - {e.Message}");
    
    switch (e.AlertType)
    {
        case MetricsAlertType.HighResponseTime:
            // Handle slow response
            break;
        case MetricsAlertType.HighErrorRate:
            // Handle high error rate
            break;
        case MetricsAlertType.HighTokenUsage:
            // Handle high token usage
            break;
    }
};
```

## Reporting and Analytics

### Generate Reports

Create comprehensive reports:

```csharp
var report = agent.Metrics.GenerateReport();
Console.WriteLine($"=== Agent Performance Report ===");
Console.WriteLine($"Period: {report.StartTime} to {report.EndTime}");
Console.WriteLine($"Total Runs: {report.TotalRuns}");
Console.WriteLine($"Success Rate: {report.SuccessRate:P2}");
Console.WriteLine($"Average Response Time: {report.AverageResponseTimeMs}ms");
Console.WriteLine($"Total Tokens: {report.TotalTokens:N0}");
```

### Export Metrics

Export metrics for external analysis:

```csharp
// Export as JSON
var jsonMetrics = agent.Metrics.ExportAsJson();

// Export as CSV
var csvMetrics = agent.Metrics.ExportAsCsv();

// Export for specific time period
var periodMetrics = agent.Metrics.ExportForPeriod(
    DateTime.Now.AddDays(-7), 
    DateTime.Now
);
```

## Integration with External Systems

### Prometheus Integration

Export metrics for Prometheus:

```csharp
var prometheusMetrics = agent.Metrics.ExportForPrometheus();
// Send to Prometheus endpoint
```

### Grafana Dashboards

Create dashboards with metrics:

```csharp
// Metrics can be queried via API or exported
var dashboardData = agent.Metrics.GetDashboardData();
```

### Logging Integration

Integrate with logging systems:

```csharp
// Log metrics to your logging system
var metrics = agent.Metrics.GetMetrics();
logger.LogInformation("Agent metrics: {@Metrics}", metrics);
```

## Best Practices

### 1. Monitor Key Metrics

Track these essential metrics:
- **Success rate** - Overall agent effectiveness
- **Response time** - Performance and user experience
- **Token usage** - Cost management
- **Error rate** - System health
- **Memory usage** - Resource management

### 2. Set Appropriate Thresholds

Configure realistic thresholds:
```csharp
var thresholds = new MetricsAlertThresholds
{
    MaxResponseTimeMs = 3000,    // 3 seconds
    MaxErrorRate = 0.05,         // 5% error rate
    MaxTokenUsage = 5000,        // 5000 tokens per run
    MinSuccessRate = 0.85        // 85% success rate
};
```

### 3. Regular Monitoring

- **Set up automated monitoring** dashboards
- **Review metrics regularly** (daily/weekly)
- **Investigate anomalies** promptly
- **Track trends** over time

### 4. Performance Optimization

Use metrics to optimize:
- **Identify slow operations** and optimize them
- **Reduce token usage** through prompt optimization
- **Improve success rates** through better error handling
- **Scale resources** based on usage patterns

## Examples

### Complete Monitoring Setup

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.OpenAI;

// Create agent with monitoring
var config = new AgentConfiguration
{
    EnableMetricsPersistence = true,
    MetricsStoragePath = "agent-metrics",
    MetricsAlertThresholds = new MetricsAlertThresholds
    {
        MaxResponseTimeMs = 5000,
        MaxErrorRate = 0.1,
        MinSuccessRate = 0.8
    }
};

var agent = new Agent(llm, store, config: config);

// Subscribe to metrics events
agent.Metrics.MetricsUpdated += (sender, e) =>
{
    Console.WriteLine($"Metrics updated: {e.Metrics.Operational.AgentRunSuccessRate:P2} success rate");
};

agent.Metrics.MetricsAlert += (sender, e) =>
{
    Console.WriteLine($"ALERT: {e.AlertType} - {e.Message}");
};

// Run agent and monitor
var result = await agent.RunAsync("monitored-agent", "Complete task", tools);

// Generate report
var report = agent.Metrics.GenerateReport();
Console.WriteLine($"Task completed with {report.SuccessRate:P2} success rate");
```

### Custom Metrics Example

```csharp
// Track business-specific metrics
agent.Metrics.RecordCustomMetric("customer_satisfaction", 4.8);
agent.Metrics.RecordCustomMetric("task_completion_time", 45.2);
agent.Metrics.RecordCustomMetric("cost_per_task", 0.15);

// Retrieve and analyze
var customMetrics = agent.Metrics.GetCustomMetrics();
var avgSatisfaction = customMetrics
    .Where(m => m.Key == "customer_satisfaction")
    .Average(m => Convert.ToDouble(m.Value));

Console.WriteLine($"Average customer satisfaction: {avgSatisfaction:F1}/5.0");
```

This comprehensive metrics and monitoring system provides the insights you need to optimize agent performance, manage costs, and ensure reliable operation in production environments.
