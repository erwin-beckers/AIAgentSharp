# AIAgentSharp Code Coverage Script
# This script runs the test suite with coverage collection and displays results

Write-Host "Running AIAgentSharp tests with code coverage..." -ForegroundColor Green

# Clean previous coverage results
if (Test-Path "./coverage") {
    Remove-Item -Recurse -Force "./coverage"
    Write-Host "Cleaned previous coverage results" -ForegroundColor Yellow
}

# Run tests with coverage
Write-Host "Running tests with coverage collection..." -ForegroundColor Cyan
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "Tests completed successfully!" -ForegroundColor Green
    
    # Find coverage file
    $coverageFiles = Get-ChildItem -Path "./coverage" -Recurse -Filter "*.xml"
    
    if ($coverageFiles.Count -gt 0) {
        $coverageFile = $coverageFiles[0].FullName
        Write-Host "Coverage report generated: $coverageFile" -ForegroundColor Green
        Write-Host "Coverage files location: ./coverage/" -ForegroundColor Cyan
        
        # Display file size
        $fileSize = (Get-Item $coverageFile).Length
        Write-Host "Coverage file size: $([math]::Round($fileSize / 1KB, 2)) KB" -ForegroundColor Cyan
        
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Upload to Codecov: The coverage will be automatically uploaded on push/PR"
        Write-Host "  2. View coverage: Check the Codecov badge in the README"
        Write-Host "  3. Local analysis: Use tools like ReportGenerator for detailed HTML reports"
    } else {
        Write-Host "No coverage files found" -ForegroundColor Yellow
    }
} else {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}
