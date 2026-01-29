# Publishing Guide

How to publish masDependencyMap as standalone executables for distribution.

## Overview

Publishing creates standalone executables that don't require users to have the .NET SDK installed or use `dotnet run`. The application includes the .NET runtime and all dependencies.

**Important:** Due to Microsoft.Build.Locator requirements, single-file publishing is not supported. The published output includes the exe and required DLLs.

## Quick Start

### Windows x64

```powershell
.\publish-win-x64.ps1
```

Output: `publish/win-x64/MasDependencyMap.CLI.exe` (~132 MB total)

### Linux x64

```bash
chmod +x publish-linux-x64.sh
./publish-linux-x64.sh
```

Output: `publish/linux-x64/MasDependencyMap.CLI` (~132 MB total)

### macOS (manual)

```bash
dotnet publish src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj \
    --configuration Release \
    --runtime osx-x64 \
    --self-contained true \
    --output publish/osx-x64 \
    /p:PublishReadyToRun=true
```

## Publish Scripts Explained

### publish-win-x64.ps1

Publishes for Windows 64-bit with the following optimizations:
- **Self-contained**: Includes .NET 8 runtime (~120 MB)
- **ReadyToRun**: Ahead-of-time compilation for faster startup
- **No debug symbols**: Smaller output size
- **Platform-specific**: Optimized for Windows x64

### publish-linux-x64.sh

Same optimizations as Windows, but for Linux x64 platforms.

### publish-trimmed.ps1

**Experimental**: Attempts to reduce size with IL trimming.

**Warning:** Trimming may break functionality if code uses reflection. Test thoroughly before distributing trimmed builds.

## Manual Publishing

### Framework-Dependent (Requires .NET 8 Runtime)

Smaller output (~5 MB) but requires users to install .NET 8 runtime:

```bash
dotnet publish src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj \
    --configuration Release \
    --output publish/framework-dependent
```

### Self-Contained for Specific Platform

Replace `<runtime>` with target platform:
- `win-x64` - Windows 64-bit
- `win-arm64` - Windows ARM64
- `linux-x64` - Linux 64-bit
- `linux-arm64` - Linux ARM64 (Raspberry Pi, etc.)
- `osx-x64` - macOS Intel
- `osx-arm64` - macOS Apple Silicon

```bash
dotnet publish src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj \
    --configuration Release \
    --runtime <runtime> \
    --self-contained true \
    --output publish/<runtime>
```

## Distribution

### Zip Archive

Create a zip file for distribution:

**Windows:**
```powershell
Compress-Archive -Path publish\win-x64\* -DestinationPath masDependencyMap-win-x64-v1.0.0.zip
```

**Linux/macOS:**
```bash
cd publish/linux-x64
tar -czf ../masDependencyMap-linux-x64-v1.0.0.tar.gz *
```

### Installation Instructions for Users

Include these instructions with your distribution:

**Windows:**
1. Extract `masDependencyMap-win-x64-v1.0.0.zip` to desired location
2. Add the extracted folder to your PATH, or navigate to it in terminal
3. Run: `MasDependencyMap.CLI.exe analyze --solution path\to\solution.sln`

**Linux/macOS:**
1. Extract archive: `tar -xzf masDependencyMap-linux-x64-v1.0.0.tar.gz`
2. Make executable: `chmod +x MasDependencyMap.CLI`
3. Add to PATH or use directly: `./MasDependencyMap.CLI analyze --solution path/to/solution.sln`

## Adding to PATH

### Windows

**Option 1: User PATH (Recommended)**
1. Open System Properties → Environment Variables
2. Under "User variables", select "Path" and click "Edit"
3. Click "New" and add the full path to `publish\win-x64`
4. Click OK, restart terminal
5. Test: `MasDependencyMap.CLI --version`

**Option 2: PowerShell Profile**
```powershell
# Add to $PROFILE
$env:PATH += ";C:\path\to\publish\win-x64"
```

### Linux/macOS

Add to `~/.bashrc`, `~/.zshrc`, or equivalent:

```bash
export PATH="$PATH:/path/to/publish/linux-x64"
```

Reload: `source ~/.bashrc` or restart terminal.

Test: `MasDependencyMap.CLI --version`

## Output Size Comparison

| Publish Type | Windows x64 | Linux x64 | Notes |
|-------------|-------------|-----------|-------|
| Framework-Dependent | ~5 MB | ~5 MB | Requires .NET 8 runtime |
| Self-Contained | ~132 MB | ~132 MB | No runtime required |
| Self-Contained + Trimmed | ~80-100 MB | ~80-100 MB | May break functionality |
| Single-File | ❌ Not supported | ❌ Not supported | MSBuildLocator incompatible |

## Why Not Single-File?

The application uses Microsoft.Build.Locator which requires native DLLs to be accessible as separate files. Single-file publishing bundles all DLLs into the exe, causing MSBuildLocator to fail with `DllNotFoundException`.

**Alternatives Considered:**
1. ✅ **Current approach**: Self-contained with separate DLLs (~132 MB, works reliably)
2. ❌ Single-file with `IncludeAllContentForSelfExtract`: Still fails with MSBuildLocator
3. ❌ Remove MSBuildLocator: Loses fallback loader capabilities for older .NET projects

## Troubleshooting

### "dot command not found" (Graphviz)

The published app doesn't include Graphviz. Users must install separately:
- Windows: `choco install graphviz` or download from graphviz.org
- Linux: `sudo apt install graphviz`
- macOS: `brew install graphviz`

### Configuration Files

The published output includes `scoring-config.json`. Users can customize this file or add `filter-config.json` in the same directory as the exe.

### Missing DLLs

If users report missing DLL errors:
1. Verify they extracted the entire publish folder (not just the exe)
2. Check antivirus didn't quarantine DLLs
3. Ensure they're running the exe from its original folder

### Performance

First run is slower due to JIT compilation. Subsequent runs are faster. Consider using ReadyToRun (enabled in publish scripts) for faster startup.

## GitHub Releases

Automated release workflow (`.github/workflows/release.yml`):

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x

      - name: Publish Windows x64
        run: |
          dotnet publish src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj \
            --configuration Release \
            --runtime win-x64 \
            --self-contained true \
            --output publish/win-x64 \
            /p:PublishReadyToRun=true

      - name: Publish Linux x64
        run: |
          dotnet publish src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj \
            --configuration Release \
            --runtime linux-x64 \
            --self-contained true \
            --output publish/linux-x64 \
            /p:PublishReadyToRun=true

      - name: Create Archives
        run: |
          cd publish/win-x64 && zip -r ../../masDependencyMap-win-x64.zip * && cd ../..
          cd publish/linux-x64 && tar -czf ../../masDependencyMap-linux-x64.tar.gz * && cd ../..

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            masDependencyMap-win-x64.zip
            masDependencyMap-linux-x64.tar.gz
```

## See Also

- [User Guide](user-guide.md) - Command-line reference
- [Configuration Guide](configuration-guide.md) - Configuration file format
- [README](../README.md) - Installation and quick start
