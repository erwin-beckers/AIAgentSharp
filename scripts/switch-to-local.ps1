# Switch back to local project references for development
# Usage: .\scripts\switch-to-local.ps1

Write-Host "Switching back to local project references for development..." -ForegroundColor Green

# Build with UseLocalPackages=true to use local project references
dotnet build --property:UseLocalPackages=true

Write-Host "Build completed with local project references." -ForegroundColor Green
Write-Host "You can now continue development with local references." -ForegroundColor Yellow
