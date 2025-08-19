# Installation Guide

This guide will help you install and set up AIAgentSharp in your .NET project.

## Prerequisites

- **.NET 8.0 or later** - AIAgentSharp requires .NET 8.0 or higher
- **LLM Provider API Key** - You'll need an API key from one of the supported providers:
  - OpenAI (GPT-4, GPT-3.5-turbo)
  - Anthropic (Claude)
  - Google (Gemini)
  - Mistral AI

## Available NuGet Packages

AIAgentSharp is distributed across multiple NuGet packages to allow you to include only what you need:

| Package | Description | Required |
|---------|-------------|----------|
| `AIAgentSharp` | Core framework with abstract interfaces, reasoning engines, and tool framework | ✅ Required |
| `AIAgentSharp.OpenAI` | OpenAI integration with `OpenAiLlmClient` | Optional |
| `AIAgentSharp.Anthropic` | Anthropic Claude integration with `AnthropicLlmClient` | Optional |
| `AIAgentSharp.Gemini` | Google Gemini integration with `GeminiLlmClient` | Optional |
| `AIAgentSharp.Mistral` | Mistral AI integration with `MistralLlmClient` | Optional |

## Installation Steps

### 1. Install Core Package

First, install the core AIAgentSharp package:

```bash
dotnet add package AIAgentSharp
```

### 2. Install LLM Provider Package

Choose and install one or more LLM provider packages based on your needs:

```bash
# For OpenAI
dotnet add package AIAgentSharp.OpenAI

# For Anthropic Claude
dotnet add package AIAgentSharp.Anthropic

# For Google Gemini
dotnet add package AIAgentSharp.Gemini

# For Mistral AI
dotnet add package AIAgentSharp.Mistral
```

### 3. Set Up API Keys

Set your API key as an environment variable:

```bash
# Windows
set OPENAI_API_KEY=your-api-key-here
set ANTHROPIC_API_KEY=your-api-key-here
set GOOGLE_API_KEY=your-api-key-here
set MISTRAL_API_KEY=your-api-key-here

# Linux/macOS
export OPENAI_API_KEY=your-api-key-here
export ANTHROPIC_API_KEY=your-api-key-here
export GOOGLE_API_KEY=your-api-key-here
export MISTRAL_API_KEY=your-api-key-here
```

### 4. Add Using Statements

Add the necessary using statements to your C# files:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.Tools;
using AIAgentSharp.Configuration;

// For specific LLM providers
using AIAgentSharp.OpenAI;      // For OpenAI
using AIAgentSharp.Anthropic;   // For Anthropic
using AIAgentSharp.Gemini;      // For Google
using AIAgentSharp.Mistral;     // For Mistral
```

## Project File Example

Here's an example `.csproj` file showing the typical package references:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AIAgentSharp" Version="1.0.0" />
    <PackageReference Include="AIAgentSharp.OpenAI" Version="1.0.0" />
  </ItemGroup>

</Project>
```

## Verification

To verify your installation, create a simple test program:

```csharp
using AIAgentSharp.Agents;
using AIAgentSharp.StateStores;
using AIAgentSharp.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Please set OPENAI_API_KEY environment variable");
            return;
        }

        var llm = new OpenAiLlmClient(apiKey);
        var store = new MemoryAgentStateStore();
        var agent = new Agent(llm, store);
        
        Console.WriteLine("AIAgentSharp installation verified successfully!");
    }
}
```

## Next Steps

After installation, you can:

1. **Get Started**: Follow the [Quick Start Guide](quick-start.md) to create your first agent
2. **Learn Concepts**: Read [Basic Concepts](concepts.md) to understand the framework
3. **Explore Examples**: Check out the [Examples](examples/) for practical usage patterns

## Troubleshooting

### Common Installation Issues

**Package not found**: Ensure you're using .NET 8.0 or later and have the latest NuGet package source.

**API key not found**: Verify your environment variables are set correctly and accessible to your application.

**Version conflicts**: If you encounter version conflicts, ensure all AIAgentSharp packages are using the same version.

For more help, see the [Troubleshooting Guide](troubleshooting/common-issues.md).
