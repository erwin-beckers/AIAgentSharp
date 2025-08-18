# AIAgentSharp Multi-Provider Architecture

## Overview

AIAgentSharp now supports a modular, multi-provider architecture where different LLM providers are implemented as separate NuGet packages. This approach provides several benefits:

- **Modularity**: Users only include the providers they need
- **Maintainability**: Each provider can be updated independently
- **Extensibility**: Easy to add new providers without modifying the core framework
- **Dependency Management**: Each provider manages its own dependencies

## Current Packages

### Core Package
- **AIAgentSharp**: Core framework with interfaces, models, and agent logic
- **Dependencies**: None (pure framework)

### Provider Packages
- **AIAgentSharp.OpenAI**: OpenAI integration with GPT models
- **Dependencies**: OpenAI SDK, AIAgentSharp

## Package Structure

```
AIAgentSharp/
├── src/
│   ├── AIAgentSharp/                    # Core framework
│   │   ├── AIAgentSharp.csproj
│   │   ├── Llm/
│   │   │   ├── ILlmClient.cs           # Base LLM interface
│   │   │   ├── IFunctionCallingLlmClient.cs  # Function calling interface
│   │   │   ├── FunctionCallResult.cs   # Function call result model
│   │   │   └── OpenAiFunctionSpec.cs   # OpenAI-specific function spec
│   │   └── ...
│   └── AIAgentSharp.OpenAI/            # OpenAI provider
│       ├── AIAgentSharp.OpenAI.csproj
│       ├── OpenAiLlmClient.cs          # OpenAI implementation
│       ├── OpenAiConfiguration.cs      # OpenAI-specific config
│       └── README.md                   # Provider documentation
├── tests/
│   ├── AIAgentSharp.Tests/             # Core framework tests
│   └── AIAgentSharp.OpenAI.Tests/      # OpenAI provider tests
└── examples/
    └── Example.csproj                  # Updated to use new packages
```

## Adding a New LLM Provider

### Step 1: Create Provider Package

Create a new project in `src/AIAgentSharp.{ProviderName}/`:

```bash
mkdir -p src/AIAgentSharp.Anthropic
```

### Step 2: Create Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PackageId>AIAgentSharp.Anthropic</PackageId>
    <Version>1.0.0</Version>
    <Authors>AIAgentSharp Contributors</Authors>
    <Description>Anthropic integration for AIAgentSharp - LLM-powered agents with Claude models.</Description>
    <PackageTags>ai,agent,llm,anthropic,claude,automation</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/erwin-beckers/AIAgentSharp</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <InternalsVisibleTo>AIAgentSharp.Anthropic.Tests</InternalsVisibleTo>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Anthropic.SDK" Version="0.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../AIAgentSharp/AIAgentSharp.csproj" />
  </ItemGroup>
</Project>
```

### Step 3: Implement LLM Client

```csharp
using AIAgentSharp;
using Anthropic.SDK;

namespace AIAgentSharp.Anthropic;

public sealed class AnthropicLlmClient : ILlmClient, IFunctionCallingLlmClient
{
    private readonly AnthropicClient _client;
    private readonly string _model;
    private readonly ILogger _logger;
    
    public AnthropicConfiguration Configuration { get; } = null!;

    public AnthropicLlmClient(string apiKey, string model = "claude-3-sonnet-20240229", ILogger? logger = null)
        : this(apiKey, new AnthropicConfiguration { Model = model }, logger)
    {
    }

    public AnthropicLlmClient(string apiKey, AnthropicConfiguration configuration, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _client = new AnthropicClient(apiKey);
        _model = configuration.Model;
        _logger = logger ?? new ConsoleLogger();
        Configuration = configuration;
    }

    public async Task<string> CompleteAsync(IEnumerable<LlmMessage> messages, CancellationToken ct = default)
    {
        // Implement Anthropic API calls
        // Map AIAgentSharp messages to Anthropic format
        // Return response
    }

    public async Task<FunctionCallResult> CompleteWithFunctionsAsync(
        IEnumerable<LlmMessage> messages,
        IEnumerable<OpenAiFunctionSpec> functions,
        CancellationToken ct = default)
    {
        // Implement Anthropic function calling
        // Map OpenAI function specs to Anthropic format
        // Return function call result
    }
}
```

### Step 4: Create Configuration Class

```csharp
namespace AIAgentSharp.Anthropic;

public sealed class AnthropicConfiguration
{
    public string Model { get; init; } = "claude-3-sonnet-20240229";
    public int MaxTokens { get; init; } = 4000;
    public float Temperature { get; init; } = 0.1f;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public int MaxRetries { get; init; } = 3;
    public bool EnableFunctionCalling { get; init; } = true;

    public static AnthropicConfiguration CreateForAgentReasoning()
    {
        return new AnthropicConfiguration
        {
            Model = "claude-3-sonnet-20240229",
            Temperature = 0.1f,
            MaxTokens = 4000,
            EnableFunctionCalling = true,
            MaxRetries = 3,
            RequestTimeout = TimeSpan.FromMinutes(2)
        };
    }
}
```

### Step 5: Create Tests

```csharp
using AIAgentSharp.Anthropic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIAgentSharp.Anthropic.Tests;

[TestClass]
public class AnthropicLlmClientTests
{
    private const string TestApiKey = "test-api-key";

    [TestMethod]
    public void Constructor_WithApiKey_ShouldCreateClient()
    {
        var client = new AnthropicLlmClient(TestApiKey);
        Assert.IsNotNull(client);
        Assert.AreEqual("claude-3-sonnet-20240229", client.Configuration.Model);
    }

    // Add more tests...
}
```

### Step 6: Update Solution

Add the new projects to `AIAgentSharp.sln`:

```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "AIAgentSharp.Anthropic", "src\AIAgentSharp.Anthropic\AIAgentSharp.Anthropic.csproj", "{NEW-GUID-1}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "AIAgentSharp.Anthropic.Tests", "tests\AIAgentSharp.Anthropic.Tests\AIAgentSharp.Anthropic.Tests.csproj", "{NEW-GUID-2}"
EndProject
```

### Step 7: Create Documentation

Create a `README.md` for the provider package with:
- Installation instructions
- Quick start examples
- Configuration options
- Model support
- Error handling
- Performance optimization tips

## Usage Examples

### Basic Usage

```csharp
using AIAgentSharp;
using AIAgentSharp.Agents;
using AIAgentSharp.OpenAI;  // or AIAgentSharp.Anthropic
using AIAgentSharp.StateStores;

// Create provider-specific client
var llm = new OpenAiLlmClient("your-openai-api-key");
// or var llm = new AnthropicLlmClient("your-anthropic-api-key");

// Create agent (same for all providers)
var store = new MemoryAgentStateStore();
var agent = new Agent(llm, store);

// Run agent
var result = await agent.RunAsync("my-agent", "Hello, how are you?", new List<ITool>());
```

### Advanced Configuration

```csharp
// OpenAI with custom configuration
var openAiConfig = OpenAiConfiguration.CreateForAgentReasoning();
var openAiClient = new OpenAiLlmClient("your-api-key", openAiConfig);

// Anthropic with custom configuration
var anthropicConfig = AnthropicConfiguration.CreateForAgentReasoning();
var anthropicClient = new AnthropicLlmClient("your-api-key", anthropicConfig);
```

## Provider Comparison

| Feature | OpenAI | Anthropic | Cohere | Local |
|---------|--------|-----------|--------|-------|
| Function Calling | ✅ | ✅ | ❌ | ❌ |
| Streaming | ✅ | ✅ | ✅ | ✅ |
| Cost Optimization | ✅ | ✅ | ✅ | ✅ |
| Enterprise Support | ✅ | ✅ | ✅ | ✅ |
| Custom Endpoints | ✅ | ✅ | ✅ | ✅ |

## Best Practices

### 1. Provider Selection

- **OpenAI**: Best for function calling and tool use
- **Anthropic**: Best for reasoning and analysis
- **Cohere**: Best for cost efficiency
- **Local**: Best for privacy and offline use

### 2. Configuration Optimization

```csharp
// For reasoning tasks
var config = OpenAiConfiguration.CreateForAgentReasoning();

// For creative tasks
var config = OpenAiConfiguration.CreateForCreativeTasks();

// For cost efficiency
var config = OpenAiConfiguration.CreateForCostEfficiency();
```

### 3. Error Handling

```csharp
try
{
    var result = await agent.RunAsync("agent-id", "Hello", tools);
}
catch (InvalidOperationException ex)
{
    // Handle provider-specific errors
    Console.WriteLine($"LLM API error: {ex.Message}");
}
catch (OperationCanceledException)
{
    // Handle timeouts and cancellations
    Console.WriteLine("Request was cancelled or timed out");
}
```

### 4. Logging

```csharp
var logger = new ConsoleLogger();
var llm = new OpenAiLlmClient("your-api-key", logger: logger);

// Or use your own logger implementation
var customLogger = new MyCustomLogger();
var llm = new OpenAiLlmClient("your-api-key", logger: customLogger);
```

## Future Providers

Planned provider packages:

- **AIAgentSharp.Anthropic**: Claude models
- **AIAgentSharp.Cohere**: Cohere models
- **AIAgentSharp.Ollama**: Local models via Ollama
- **AIAgentSharp.LMStudio**: Local models via LM Studio
- **AIAgentSharp.Azure**: Azure OpenAI
- **AIAgentSharp.AWS**: Amazon Bedrock

## Contributing

To add a new provider:

1. Follow the step-by-step guide above
2. Ensure all tests pass
3. Add comprehensive documentation
4. Update this architecture document
5. Submit a pull request

## Migration Guide

### From Single Package to Multi-Package

If you're migrating from the old single-package approach:

1. **Update Dependencies**:
   ```xml
   <!-- Old -->
   <PackageReference Include="AIAgentSharp" Version="1.0.0" />
   
   <!-- New -->
   <PackageReference Include="AIAgentSharp" Version="1.0.0" />
   <PackageReference Include="AIAgentSharp.OpenAI" Version="1.0.0" />
   ```

2. **Update Using Statements**:
   ```csharp
   // Old
   using AIAgentSharp;
   
   // New
   using AIAgentSharp;
   using AIAgentSharp.OpenAI;  // Add provider-specific namespace
   ```

3. **Update Client Creation**:
   ```csharp
   // Old
   var llm = new OpenAiLlmClient(apiKey);
   
   // New (same, but from different package)
   var llm = new OpenAiLlmClient(apiKey);
   ```

The API remains the same, so existing code should work without changes once the new packages are referenced.
