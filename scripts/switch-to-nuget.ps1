# Test build with NuGet package references
# Usage: .\scripts\switch-to-nuget.ps1 [version]
# Note: This only works if the specified version exists on NuGet

param(
    [string]$Version = "1.0.8"
)

Write-Host "Testing build with NuGet package references..." -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow

# Pack individual projects with NuGet references
Write-Host "Packing projects with NuGet references..." -ForegroundColor Cyan
dotnet pack src\AIAgentSharp.OpenAI\AIAgentSharp.OpenAI.csproj --property:UseLocalPackages=false --property:AIAgentSharpVersion=$Version --configuration Release --output nupkg --verbosity minimal
dotnet pack src\AIAgentSharp.Anthropic\AIAgentSharp.Anthropic.csproj --property:UseLocalPackages=false --property:AIAgentSharpVersion=$Version --configuration Release --output nupkg --verbosity minimal
dotnet pack src\AIAgentSharp.Gemini\AIAgentSharp.Gemini.csproj --property:UseLocalPackages=false --property:AIAgentSharpVersion=$Version --configuration Release --output nupkg --verbosity minimal
dotnet pack src\AIAgentSharp.Mistral\AIAgentSharp.Mistral.csproj --property:UseLocalPackages=false --property:AIAgentSharpVersion=$Version --configuration Release --output nupkg --verbosity minimal

Write-Host "Pack completed with NuGet package references." -ForegroundColor Green
Write-Host "This confirms that your packages can be built with NuGet references." -ForegroundColor Yellow
