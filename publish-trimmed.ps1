# Publish masDependencyMap with aggressive trimming for minimal size
# WARNING: Trimmed builds may have runtime issues if reflection is used incorrectly
# Test thoroughly before distributing

Write-Host "Publishing masDependencyMap (trimmed) for Windows x64..." -ForegroundColor Cyan
Write-Host "This creates the smallest possible executable but requires testing." -ForegroundColor Yellow

# Clean previous builds
if (Test-Path "publish\win-x64-trimmed") {
    Remove-Item "publish\win-x64-trimmed" -Recurse -Force
}

# Publish with trimming and ReadyToRun (NOT single-file due to MSBuildLocator)
dotnet publish src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output publish/win-x64-trimmed `
    /p:PublishTrimmed=true `
    /p:TrimMode=partial `
    /p:PublishReadyToRun=true `
    /p:DebugType=None `
    /p:DebugSymbols=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPublish successful!" -ForegroundColor Green
    Write-Host "Executable location: publish\win-x64-trimmed\MasDependencyMap.CLI.exe" -ForegroundColor Green
    Write-Host "`nFile size:" -ForegroundColor Cyan
    Get-Item "publish\win-x64-trimmed\MasDependencyMap.CLI.exe" | Format-Table Name, @{Name="Size (MB)";Expression={[math]::Round($_.Length / 1MB, 2)}}

    Write-Host "`nWARNING: This is a trimmed build. Test thoroughly!" -ForegroundColor Yellow
    Write-Host "Run the tests to verify functionality:" -ForegroundColor Yellow
    Write-Host "  .\publish\win-x64-trimmed\MasDependencyMap.CLI.exe analyze --solution samples\SampleMonolith\SampleMonolith.sln --output test-output"
} else {
    Write-Host "`nPublish failed!" -ForegroundColor Red
    exit 1
}
