# Publish masDependencyMap for Windows x64 as a self-contained single-file executable
# This creates a standalone exe that doesn't require .NET runtime to be installed

Write-Host "Publishing masDependencyMap for Windows x64..." -ForegroundColor Cyan

# Clean previous builds
if (Test-Path "publish\win-x64") {
    Remove-Item "publish\win-x64" -Recurse -Force
}

# Publish as self-contained (NOT single-file due to MSBuildLocator limitations)
# Single-file publishing doesn't work with Microsoft.Build.Locator
dotnet publish src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output publish/win-x64 `
    /p:PublishReadyToRun=true `
    /p:DebugType=None `
    /p:DebugSymbols=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPublish successful!" -ForegroundColor Green
    Write-Host "Executable location: publish\win-x64\MasDependencyMap.CLI.exe" -ForegroundColor Green
    Write-Host "`nFile size:" -ForegroundColor Cyan
    Get-Item "publish\win-x64\MasDependencyMap.CLI.exe" | Format-Table Name, @{Name="Size (MB)";Expression={[math]::Round($_.Length / 1MB, 2)}}

    Write-Host "`nUsage:" -ForegroundColor Yellow
    Write-Host "  cd publish\win-x64"
    Write-Host "  .\MasDependencyMap.CLI.exe analyze --solution path\to\solution.sln"
    Write-Host "`nOr add publish\win-x64 to your PATH to use from anywhere:"
    Write-Host "  MasDependencyMap.CLI analyze --solution path\to\solution.sln"
} else {
    Write-Host "`nPublish failed!" -ForegroundColor Red
    exit 1
}
