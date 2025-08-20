# AIAgentSharp Build and Publishing Workflow

This directory contains scripts to manage the development and publishing workflow for AIAgentSharp packages.

## Overview

The project uses `Directory.Build.props` to conditionally switch between:
- **Local project references** (for development)
- **NuGet package references** (for publishing)

## Scripts

### Development Workflow

#### `switch-to-local.ps1`
Switches back to local project references for development.
```powershell
.\scripts\switch-to-local.ps1
```

### Publishing Workflow

#### `switch-to-nuget.ps1 [version]`
Switches to NuGet package references for publishing.
```powershell
.\scripts\switch-to-nuget.ps1 1.0.8
```

#### `publish-all.ps1 [version]`
Complete automated publishing workflow:
1. Builds AIAgentSharp with local references
2. Publishes AIAgentSharp to NuGet
3. Builds all LLM packages with NuGet references
4. Publishes all LLM packages to NuGet
5. Switches back to local references

```powershell
.\scripts\publish-all.ps1 1.0.8
```

## Manual Usage

You can also control the behavior manually using MSBuild properties:

### For Development (Local References)
```powershell
dotnet build --property:UseLocalPackages=true
```

### For Publishing (NuGet References)
```powershell
dotnet build --property:UseLocalPackages=false --property:AIAgentSharpVersion=1.0.8
```

## Environment Setup

Before publishing, set your NuGet API key:
```powershell
$env:NUGET_API_KEY = "your-nuget-api-key-here"
```

## How It Works

The `Directory.Build.props` file at the solution root automatically:
- Uses local project references when `UseLocalPackages=true` (default)
- Uses NuGet package references when `UseLocalPackages=false`
- Applies to all projects in the solution

This eliminates the need to manually edit project files when switching between development and publishing modes.
