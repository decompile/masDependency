# Troubleshooting Guide

Common issues and solutions for masDependencyMap.

## Table of Contents

1. [Graphviz Not Found](#graphviz-not-found)
2. [MSBuild and Roslyn Errors](#msbuild-and-roslyn-errors)
3. [JSON Configuration Errors](#json-configuration-errors)
4. [Solution Loading Failures](#solution-loading-failures)
5. [Performance Issues](#performance-issues)
6. [Project Loading Errors](#project-loading-errors)
7. [Output File Issues](#output-file-issues)
8. [Command-Line Parsing Errors](#command-line-parsing-errors)

---

## Graphviz Not Found

### Symptom

```
Error: Graphviz not found
Reason: 'dot' executable not in PATH
Suggestion: Install Graphviz from https://graphviz.org/download/
```

### Cause

The tool cannot find the `dot` executable (part of Graphviz) in the system PATH. Graphviz is required for generating visual dependency graphs.

### Solution

#### Windows

1. **Download Graphviz installer**
   - Visit [graphviz.org/download](https://graphviz.org/download/)
   - Download the Windows installer (stable release recommended)

2. **Install Graphviz**
   - Run the installer
   - Install to `C:\Program Files\Graphviz` (default location)
   - Note the installation path

3. **Add Graphviz to PATH**
   - Open **System Properties** → **Environment Variables**
   - Under **System Variables**, find **Path** and click **Edit**
   - Click **New** and add `C:\Program Files\Graphviz\bin`
   - Click **OK** to save changes

4. **Restart terminal**
   - Close all terminal windows
   - Open new terminal
   - Run verification command

5. **Verify installation**
   ```bash
   dot -version
   ```
   Expected output:
   ```
   dot - graphviz version 2.xx.x (...)
   ```

**Troubleshooting:**
- If `dot -version` fails after adding to PATH, verify the bin directory path is correct
- Try full path: `C:\Program Files\Graphviz\bin\dot.exe -version`
- Restart computer if PATH changes don't take effect

#### macOS

1. **Install via Homebrew** (recommended)
   ```bash
   brew install graphviz
   ```

2. **Verify installation**
   ```bash
   dot -version
   ```

**Alternative: Download installer**
- Visit [graphviz.org/download](https://graphviz.org/download/)
- Download macOS package
- Install and add to PATH if necessary

#### Linux (Ubuntu/Debian)

1. **Install via apt**
   ```bash
   sudo apt-get update
   sudo apt-get install graphviz
   ```

2. **Verify installation**
   ```bash
   dot -version
   ```

#### Linux (Fedora/RHEL/CentOS)

1. **Install via yum/dnf**
   ```bash
   sudo yum install graphviz
   # or
   sudo dnf install graphviz
   ```

2. **Verify installation**
   ```bash
   dot -version
   ```

### Verification After Installation

Run the analysis command again:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln
```

Expected result: PNG and SVG files generated successfully.

---

## MSBuild and Roslyn Errors

The tool uses a fallback chain for loading solutions:
1. **Roslyn** - Full semantic analysis (preferred)
2. **MSBuild** - Project reference analysis (fallback)
3. **Project File** - XML parsing (last resort)

### Symptom 1: MSBuildLocator Exception

```
Error: Could not load type 'Microsoft.Build.Locator.MSBuildLocator'
Reason: MSBuild integration components not found
```

### Cause

.NET 8 SDK not installed or not in PATH.

### Solution

1. **Check .NET SDK installation**
   ```bash
   dotnet --version
   ```

2. **If not installed or wrong version**
   - Download .NET 8 SDK from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
   - Install the SDK (not just runtime)
   - Verify: `dotnet --version` should show 8.0.x

3. **Restart terminal** after installation

4. **Re-run analysis**

### Symptom 2: Roslyn Fallback Warning

```
Warning: Roslyn failed, falling back to MSBuild
Reason: Unable to load workspace semantically
```

### Cause

This is **NORMAL** fallback behavior, not an error. Roslyn requires full .NET SDK and may fail for:
- Old project formats (.NET Framework 3.5-4.x)
- Missing SDK references
- Corrupted project files

### Solution

**No action required** - this is expected behavior:

1. **MSBuild fallback still provides accurate dependency analysis**
   - Dependency graph will be complete
   - Only semantic analysis features unavailable
   - Cyclomatic complexity might be approximated

2. **To see why Roslyn failed (optional)**
   ```bash
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --verbose
   ```

3. **Verbose output shows**
   - Specific Roslyn error
   - Which loader succeeded (MSBuild or ProjectFile)
   - Individual project loading details

**When to investigate:**
- All projects fail to load (even with MSBuild fallback)
- Analysis results clearly wrong (missing dependencies)

### Symptom 3: Old Project Format Errors

```
Error: Failed to load project MyLegacy.csproj
Reason: Unsupported project format
```

### Cause

Very old .NET Framework project formats (pre-3.5) or corrupted project files.

### Solution

1. **Verify project format**
   - Open .csproj file in text editor
   - Check for valid XML structure
   - Check for `<TargetFramework>` or `<TargetFrameworkVersion>`

2. **Check .NET Framework SDK installation**
   - Old projects require .NET Framework SDKs
   - Install from [Visual Studio Installer](https://visualstudio.microsoft.com/downloads/)
   - Select ".NET Framework development tools"

3. **Use verbose mode to see details**
   ```bash
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --verbose
   ```

4. **Try manual project repair**
   - Open solution in Visual Studio
   - Right-click project → Reload Project
   - Build solution to verify it loads correctly

### Symptom 4: Assembly Loading Errors

```
Error: Could not load file or assembly 'Microsoft.CodeAnalysis'
```

### Cause

Conflicting Roslyn versions or corrupted NuGet cache.

### Solution

1. **Clear NuGet cache**
   ```bash
   dotnet nuget locals all --clear
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Rebuild tool**
   ```bash
   dotnet build
   ```

4. **Re-run analysis**

---

## JSON Configuration Errors

### Symptom: JSON Syntax Error

```
Error: JSON syntax error in filter-config.json
Location: Line 5, Position 12
Details: ',' expected
```

### Cause

Invalid JSON syntax in configuration file.

### Solution

1. **Open file in JSON-aware editor**
   - VS Code, Visual Studio, or any editor with JSON syntax highlighting
   - Navigate to line and position from error message

2. **Use online JSON validator**
   - Go to [jsonlint.com](https://jsonlint.com)
   - Paste your JSON and validate
   - Follow error messages to fix syntax

3. **Check common JSON errors**

   **Missing comma:**
   ```json
   {
     "FrameworkFilters": {
       "BlockList": [
         "Microsoft.*"
         "System.*"  // ERROR: Missing comma
       ]
     }
   }
   ```

   **Fixed:**
   ```json
   {
     "FrameworkFilters": {
       "BlockList": [
         "Microsoft.*",
         "System.*"  // Added comma
       ]
     }
   }
   ```

   **Extra comma:**
   ```json
   {
     "FrameworkFilters": {
       "BlockList": [
         "Microsoft.*",
         "System.*",  // ERROR: Extra comma after last item
       ]
     }
   }
   ```

   **Fixed:**
   ```json
   {
     "FrameworkFilters": {
       "BlockList": [
         "Microsoft.*",
         "System.*"  // Removed trailing comma
       ]
     }
   }
   ```

   **Unquoted property names:**
   ```json
   {
     FrameworkFilters: {  // ERROR: Not quoted
       BlockList: []
     }
   }
   ```

   **Fixed:**
   ```json
   {
     "FrameworkFilters": {  // Quoted
       "BlockList": []
     }
   }
   ```

   **Single quotes instead of double quotes:**
   ```json
   {
     'FrameworkFilters': {  // ERROR: Single quotes
       'BlockList': []
     }
   }
   ```

   **Fixed:**
   ```json
   {
     "FrameworkFilters": {  // Double quotes
       "BlockList": []
     }
   }
   ```

### Symptom: Weight Validation Error

```
Error: Configuration validation failed
Details: Scoring weights must sum to 1.0
Actual sum: 0.95
```

### Cause

Scoring weights don't add up to 1.0.

### Solution

1. **Calculate current sum**
   ```
   Coupling + Complexity + TechDebt + ExternalExposure = ?
   ```

2. **Adjust weights to sum to 1.0**

   **Before (sum = 0.95):**
   ```json
   {
     "ScoringWeights": {
       "Coupling": 0.40,
       "Complexity": 0.25,
       "TechDebt": 0.20,
       "ExternalExposure": 0.10
     }
   }
   ```

   **After (sum = 1.0):**
   ```json
   {
     "ScoringWeights": {
       "Coupling": 0.40,
       "Complexity": 0.25,
       "TechDebt": 0.20,
       "ExternalExposure": 0.15
     }
   }
   ```

3. **Verify sum**
   ```
   0.40 + 0.25 + 0.20 + 0.15 = 1.0 ✓
   ```

### Symptom: Property Name Error

```
Error: Configuration validation failed
Reason: Property 'frameworkFilters' not recognized
```

### Cause

Property names must use PascalCase, not camelCase.

### Solution

**Use correct property names:**

- ✓ `FrameworkFilters` (not `frameworkFilters`)
- ✓ `BlockList` (not `blockList`)
- ✓ `AllowList` (not `allowList`)
- ✓ `ScoringWeights` (not `scoringWeights`)
- ✓ `Coupling` (not `coupling`)
- ✓ `TechDebt` (not `techDebt`)

**Wrong (camelCase):**
```json
{
  "frameworkFilters": {
    "blockList": [],
    "allowList": []
  }
}
```

**Correct (PascalCase):**
```json
{
  "FrameworkFilters": {
    "BlockList": [],
    "AllowList": []
  }
}
```

---

## Solution Loading Failures

### Symptom: Solution File Not Found

```
Error: Solution file not found
Reason: No file exists at D:\path\to\solution.sln
Suggestion: Verify the path and try again
```

### Cause

Incorrect file path or file doesn't exist.

### Solution

1. **Verify file path**
   ```bash
   # Windows
   dir "D:\path\to\solution.sln"

   # macOS/Linux
   ls -l /path/to/solution.sln
   ```

2. **Use absolute path**
   ```bash
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution D:\Projects\MyApp\MyApp.sln
   ```

3. **Use relative path from current directory**
   ```bash
   cd D:\Projects\MyApp
   dotnet run --project D:\work\masDependencyMap\src\MasDependencyMap.CLI -- analyze --solution .\MyApp.sln
   ```

4. **Check file extension**
   - Must be `.sln` file
   - NOT `.csproj` or other project file types

### Symptom: Partial Solution Loading

```
Warning: 45/50 projects loaded successfully
```

### Cause

Some projects failed to load due to:
- Missing .NET SDKs (e.g., .NET Framework 4.5 SDK not installed)
- Corrupted project files
- Unsupported project types (e.g., C++, database projects)

### Solution

**This is EXPECTED and analysis continues with available projects:**

1. **Use --verbose to see which projects failed**
   ```bash
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --verbose
   ```

2. **Verbose output shows**
   - Which projects failed
   - Why each project failed (missing SDK, invalid format, etc.)
   - Which fallback loader succeeded

3. **Install missing SDKs (if needed)**
   - Open Visual Studio Installer
   - Install required .NET Framework SDKs
   - Re-run analysis

4. **Accept partial results**
   - Tool provides analysis for successfully loaded projects
   - Dependency graph may be incomplete but still useful
   - Missing projects won't appear in visualization

**Expected scenarios:**
- Large solutions: 95%+ load success is normal
- Legacy solutions: 80-90% load success acceptable
- Mixed-language solutions: C# projects load, C++/other types skipped

### Symptom: All Projects Fail to Load

```
Error: No projects loaded from solution
Reason: All project load attempts failed
```

### Cause

Major issue with solution file or environment setup.

### Solution

1. **Verify solution opens in Visual Studio**
   - Open solution in Visual Studio
   - Ensure projects load correctly
   - Build solution to verify it works

2. **Check .NET SDK installation**
   ```bash
   dotnet --version
   # Should show 8.0.x
   ```

3. **Check solution file format**
   - Open .sln file in text editor
   - Verify it's valid Visual Studio solution format
   - Check for corruption (invalid characters, truncated content)

4. **Run with --verbose for diagnostics**
   ```bash
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --verbose
   ```

5. **Create minimal test solution**
   ```bash
   dotnet new sln -n TestSolution
   dotnet new classlib -n TestProject
   dotnet sln TestSolution.sln add TestProject/TestProject.csproj
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution TestSolution.sln
   ```
   - If test solution works, issue is with original solution
   - If test solution fails, environment setup issue

---

## Performance Issues

### Symptom: Slow Analysis (>5 minutes for 50 projects)

### Expected Performance

- **Small solutions (< 10 projects)**: < 1 minute
- **Medium solutions (10-50 projects)**: 1-5 minutes
- **Large solutions (50-100 projects)**: 5-15 minutes
- **Very large solutions (100-400+ projects)**: 15-30+ minutes

### Causes and Solutions

#### Cause 1: Roslyn Semantic Analysis (CPU-intensive)

**Solution:**
- Roslyn performs deep semantic analysis (memory/CPU intensive)
- MSBuild fallback is faster but less detailed
- For speed-critical analysis, accept MSBuild fallback

#### Cause 2: Competing Processes

**Solution:**
1. **Close Visual Studio** (frees memory and CPU)
2. **Close other CPU-intensive applications**
3. **Wait for background tasks to complete** (Windows Update, antivirus scans)

#### Cause 3: Large Projects with Many Files

**Solution:**
- Sequential processing (by design) minimizes memory but slower
- Expected: ~5 minutes per 50 projects, ~30 minutes per 400 projects
- Use `--verbose` to see per-project progress

#### Cause 4: Network Drives or Slow Disks

**Solution:**
1. **Copy solution to local disk** (SSD preferred)
2. **Avoid network/mapped drives** (UNC paths)
3. **Use local analysis** then copy results

**Performance Tip:**
```bash
# Fast: Local SSD
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution C:\Projects\MySolution.sln

# Slow: Network drive
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution \\server\share\MySolution.sln
```

### Symptom: High Memory Usage (>4GB)

### Expected Memory Usage

- **Typical**: < 1GB for most solutions
- **Large solutions**: 1-4GB
- **Very large solutions**: 4GB+ (acceptable)

### Causes and Solutions

#### Cause: Roslyn Semantic Models (Memory-intensive)

**Solution:**
1. **Close Visual Studio** before running analysis (releases gigabytes)
2. **Run analysis sequentially** on multiple solutions (not parallel)
3. **Accept MSBuild fallback** if memory constrained

#### Cause: Multiple Analyses Running Simultaneously

**Solution:**
- Run analyses **one at a time**
- Sequential processing minimizes memory footprint

**Wrong (parallel):**
```bash
# DON'T DO THIS - causes high memory usage
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution Solution1.sln &
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution Solution2.sln &
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution Solution3.sln &
```

**Correct (sequential):**
```bash
# DO THIS - processes one at a time
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution Solution1.sln
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution Solution2.sln
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution Solution3.sln
```

---

## Project Loading Errors

### Symptom: "SDK Not Found" Errors

```
Error: Failed to load project MyProject.csproj
Reason: .NET SDK 'Microsoft.NET.Sdk' not found
```

### Cause

Project targets SDK version not installed.

### Solution

1. **Install required SDK**
   - Download from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
   - Install specific SDK version from error message
   - Re-run analysis

2. **Check global.json (if present)**
   ```json
   {
     "sdk": {
       "version": "8.0.100"  // Specific version required
     }
   }
   ```
   - Comment out version constraint to use latest installed SDK
   - Or install exact version specified

### Symptom: "Package Not Restored" Errors

```
Warning: NuGet packages not restored for MyProject.csproj
```

### Cause

NuGet packages not restored before analysis.

### Solution

1. **Restore packages**
   ```bash
   dotnet restore MySolution.sln
   ```

2. **Re-run analysis**
   ```bash
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln
   ```

---

## Output File Issues

### Symptom: PNG/SVG Not Generated

### Cause

Graphviz not installed or not in PATH.

### Solution

See [Graphviz Not Found](#graphviz-not-found) section above.

### Symptom: Output Files Overwritten

### Cause

Multiple analyses to same output directory overwrite previous results.

### Solution

**Use separate output directories:**

```bash
# Analysis 1
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution Solution1.sln --output ./analysis-solution1

# Analysis 2
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution Solution2.sln --output ./analysis-solution2
```

**Or use timestamps:**

```bash
# PowerShell
$timestamp = Get-Date -Format "yyyy-MM-dd-HHmmss"
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --output "./analysis-$timestamp"

# Bash
timestamp=$(date +%Y-%m-%d-%H%M%S)
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --output "./analysis-$timestamp"
```

### Symptom: Permission Denied Writing Output

```
Error: Access denied writing file
Reason: File is open in another process or directory is read-only
```

### Solution

1. **Close files in other applications** (Excel, image viewers)
2. **Check directory permissions** (write access required)
3. **Use different output directory**
4. **Run with elevated permissions** (if necessary)

---

## Command-Line Parsing Errors

### Symptom: "Required option missing"

```
Error: --solution is required
Reason: The analyze command requires a solution file path
Suggestion: Use --solution path/to/your.sln
```

### Cause

Missing required `--solution` parameter.

### Solution

```bash
# Wrong
dotnet run --project src/MasDependencyMap.CLI -- analyze

# Correct
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln
```

### Symptom: "Unrecognized option"

```
Error: Unrecognized command or argument '--unknown-option'
```

### Cause

Invalid command-line option.

### Solution

1. **Check spelling and dashes**
   ```bash
   # Wrong: Single dash
   dotnet run --project src/MasDependencyMap.CLI -- analyze -solution MySolution.sln

   # Correct: Double dash
   dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln
   ```

2. **Use --help to see available options**
   ```bash
   dotnet run --project src/MasDependencyMap.CLI -- analyze --help
   ```

3. **Valid options:**
   - `--solution` (required)
   - `--output` (optional)
   - `--config` (optional)
   - `--reports` (optional)
   - `--format` (optional)
   - `--verbose` (optional)

---

## Getting Help

### Enable Verbose Logging

For any issue, start with verbose mode:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --verbose
```

**Verbose mode shows:**
- Solution loading strategy (Roslyn/MSBuild/ProjectFile)
- Individual project loading progress and errors
- Configuration loading and validation
- Framework filter matches
- Detailed error messages and stack traces

### Report Issues

When reporting bugs, include:

1. **Command used** (exact command-line arguments)
2. **Verbose output** (use `--verbose` flag)
3. **System information:**
   - .NET SDK version: `dotnet --version`
   - Graphviz version: `dot -version`
   - Operating system and version
4. **Solution characteristics** (if possible):
   - Number of projects
   - .NET Framework versions targeted
   - Any unusual project types

**Example Bug Report:**

```
Command:
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --verbose

Output:
[Paste verbose output here]

Environment:
- .NET SDK: 8.0.100
- Graphviz: 2.50.0
- OS: Windows 11 Pro 22H2

Solution Info:
- 50 projects
- Mix of .NET Framework 4.8 and .NET 6
- Standard class libraries and web projects
```

### Additional Resources

- **[User Guide](user-guide.md)** - Complete command-line reference
- **[Configuration Guide](configuration-guide.md)** - JSON configuration details
- **[README](../README.md)** - Quick start and installation

### Common Quick Fixes

**Before reporting an issue, try these:**

1. **Restart terminal** (PATH changes, SDK installations)
2. **Clear NuGet cache** (`dotnet nuget locals all --clear`)
3. **Rebuild tool** (`dotnet build`)
4. **Verify Graphviz installation** (`dot -version`)
5. **Verify .NET SDK** (`dotnet --version` should show 8.0.x)
6. **Try sample solution** (if sample works, issue is with your solution)

```bash
# Quick validation test
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution samples/SampleMonolith/SampleMonolith.sln
```

If sample solution works but yours doesn't, the issue is likely solution-specific (not tool issue).

---

## Platform-Specific Issues

### Windows

**Long Path Issues:**
```
Error: Path too long
```

**Solution:**
- Enable long paths: `Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1`
- Restart system
- Or use shorter output paths

**PATH Environment Variable:**
- Use semicolon separator: `C:\Path1;C:\Path2`
- Enclose paths with spaces in quotes
- Restart terminal after changes

### macOS

**Homebrew Graphviz Issues:**

If `brew install graphviz` fails:

```bash
# Update Homebrew
brew update

# Reinstall Graphviz
brew reinstall graphviz

# Verify
which dot
dot -version
```

**Permission Issues:**

```bash
# Fix permissions
sudo chown -R $(whoami) /usr/local/bin/dotnet
```

### Linux

**Missing Dependencies:**

Ubuntu/Debian:
```bash
sudo apt-get install -y dotnet-sdk-8.0 graphviz libgdiplus
```

Fedora/RHEL:
```bash
sudo dnf install -y dotnet-sdk-8.0 graphviz libgdiplus
```

**Font Rendering Issues:**

If graphs have missing fonts:
```bash
sudo apt-get install fonts-liberation
fc-cache -fv
```

---

For additional help, see [User Guide](user-guide.md) or [Configuration Guide](configuration-guide.md).
