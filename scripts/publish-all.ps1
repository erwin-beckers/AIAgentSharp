# Complete publishing workflow for all packages
# Usage: .\scripts\publish-all.ps1 [version]

param(
    [string]$Version = "1.0.11"
)

Write-Host "Starting complete publishing workflow..." -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow

# Step 1: Update all project versions
Write-Host "Step 1: Updating project versions to $Version..." -ForegroundColor Cyan
(Get-Content src\AIAgentSharp\AIAgentSharp.csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content src\AIAgentSharp\AIAgentSharp.csproj
(Get-Content src\AIAgentSharp.OpenAI\AIAgentSharp.OpenAI.csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content src\AIAgentSharp.OpenAI\AIAgentSharp.OpenAI.csproj
(Get-Content src\AIAgentSharp.Anthropic\AIAgentSharp.Anthropic.csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content src\AIAgentSharp.Anthropic\AIAgentSharp.Anthropic.csproj
(Get-Content src\AIAgentSharp.Gemini\AIAgentSharp.Gemini.csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content src\AIAgentSharp.Gemini\AIAgentSharp.Gemini.csproj
(Get-Content src\AIAgentSharp.Mistral\AIAgentSharp.Mistral.csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content src\AIAgentSharp.Mistral\AIAgentSharp.Mistral.csproj

# Step 2: Build and pack AIAgentSharp (using local references)
Write-Host "Step 2: Building and packing AIAgentSharp..." -ForegroundColor Cyan
dotnet pack src\AIAgentSharp\AIAgentSharp.csproj --configuration Release --output nupkg

# Step 3: Publish AIAgentSharp to NuGet
Write-Host "Step 3: Publishing AIAgentSharp to NuGet..." -ForegroundColor Cyan
if ($env:NUGET_API_KEY) {
    dotnet nuget push nupkg\AIAgentSharp.$Version.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
    Write-Host "Waiting 10 minutes for NuGet indexing..." -ForegroundColor Yellow
    Write-Host "NuGet indexing can take up to 10 minutes. Please be patient..." -ForegroundColor Yellow
    Start-Sleep -Seconds 600
} else {
    Write-Host "WARNING: NUGET_API_KEY not set. Skipping NuGet publish." -ForegroundColor Red
}

# Step 4: Build and pack LLM packages with NuGet references
Write-Host "Step 4: Building and packing LLM packages with NuGet references..." -ForegroundColor Cyan

# OpenAI
dotnet pack src\AIAgentSharp.OpenAI\AIAgentSharp.OpenAI.csproj --property:UseLocalPackages=false --property:AIAgentSharpVersion=$Version --configuration Release --output nupkg

# Anthropic
dotnet pack src\AIAgentSharp.Anthropic\AIAgentSharp.Anthropic.csproj --property:UseLocalPackages=false --property:AIAgentSharpVersion=$Version --configuration Release --output nupkg

# Gemini
dotnet pack src\AIAgentSharp.Gemini\AIAgentSharp.Gemini.csproj --property:UseLocalPackages=false --property:AIAgentSharpVersion=$Version --configuration Release --output nupkg

# Mistral
dotnet pack src\AIAgentSharp.Mistral\AIAgentSharp.Mistral.csproj --property:UseLocalPackages=false --property:AIAgentSharpVersion=$Version --configuration Release --output nupkg

# Step 5: Publish all LLM packages to NuGet
Write-Host "Step 5: Publishing LLM packages to NuGet..." -ForegroundColor Cyan
if ($env:NUGET_API_KEY) {
    dotnet nuget push nupkg\AIAgentSharp.OpenAI.$Version.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
    dotnet nuget push nupkg\AIAgentSharp.Anthropic.$Version.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
    dotnet nuget push nupkg\AIAgentSharp.Gemini.$Version.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
    dotnet nuget push nupkg\AIAgentSharp.Mistral.$Version.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
} else {
    Write-Host "WARNING: NUGET_API_KEY not set. Skipping NuGet publish." -ForegroundColor Red
}

Write-Host "Publishing workflow completed successfully!" -ForegroundColor Green
Write-Host "All packages have been built and published (if API key was provided)." -ForegroundColor Yellow
Write-Host "Development environment continues to use local references." -ForegroundColor Yellow
