# User Guide

Complete command-line reference for masDependencyMap.

## Overview

masDependencyMap is a .NET dependency analysis tool that helps architects and developers understand project dependencies, detect circular dependencies, and identify microservice extraction candidates.

**When to Use This Tool:**
- Analyzing legacy .NET solutions for refactoring opportunities
- Detecting circular dependencies that block modularization
- Planning microservice extraction from monolithic applications
- Understanding dependency complexity in large codebases
- Generating documentation for solution architecture

## Command-Line Interface

**Note:** This guide shows simplified command syntax (`masdependencymap`). During development, use:
```bash
dotnet run --project src/MasDependencyMap.CLI -- [options] [command]
```

Once published/installed, you can use the shorter `masdependencymap` syntax shown in examples.

### Global Options

The following options are available for all commands:

```bash
masdependencymap [options] [command]
# Or during development:
dotnet run --project src/MasDependencyMap.CLI -- [options] [command]
```

**Options:**
- `--version` - Display version information
- `--help` - Display help information

### analyze Command

The `analyze` command performs dependency analysis on a .NET solution file.

**Full Syntax:**

```bash
masdependencymap analyze --solution <path> [options]
```

#### Required Parameters

##### --solution <path>

Path to the .sln file to analyze.

- **Type**: File path (absolute or relative)
- **Required**: Yes
- **Example**: `--solution MySolution.sln`
- **Example**: `--solution D:\Projects\MyApp\MyApp.sln`

#### Optional Parameters

##### --output <directory>

Output directory for generated files.

- **Type**: Directory path (absolute or relative)
- **Required**: No
- **Default**: Current directory
- **Example**: `--output ./analysis-results`
- **Example**: `--output D:\Reports\DependencyAnalysis`

**Generated Files:**
- `{SolutionName}-dependencies.dot` - Graphviz DOT format with heat map colors and score labels
- `{SolutionName}-dependencies.png` - PNG visualization (if --format includes png)
- `{SolutionName}-dependencies.svg` - SVG visualization (if --format includes svg)
- `{SolutionName}-analysis-report.txt` - Formatted text report with tables
- `{SolutionName}-extraction-scores.csv` - Extraction difficulty scores
- `{SolutionName}-cycle-analysis.csv` - Circular dependency analysis (if cycles detected)
- `{SolutionName}-dependency-matrix.csv` - Full dependency matrix with coupling scores

##### --config <file>

Path to configuration file (filter-config.json or scoring-config.json).

- **Type**: File path (absolute or relative)
- **Required**: No
- **Default**: Looks for `filter-config.json` and `scoring-config.json` in current directory
- **Example**: `--config ./custom-filter-config.json`
- **Note**: Configuration files are automatically loaded from current directory if they exist

##### --reports <type>

Types of reports to generate.

- **Type**: String (text|csv|all)
- **Required**: No
- **Default**: all
- **Example**: `--reports text` - Console output only
- **Example**: `--reports csv` - CSV files only
- **Example**: `--reports all` - Both console and CSV output

**Report Types:**
- **text**: Console output with formatted tables and progress indicators
- **csv**: CSV files for extraction scores, cycles, and dependency matrix
- **all**: Both text and CSV output

##### --format <type>

Graph visualization output format.

- **Type**: String (png|svg|both)
- **Required**: No
- **Default**: both
- **Example**: `--format png` - PNG only
- **Example**: `--format svg` - SVG only
- **Example**: `--format both` - Both PNG and SVG

**Format Comparison:**
- **PNG**: Raster format, good for quick viewing and embedding in documents
- **SVG**: Vector format, scalable without quality loss, good for high-resolution displays

##### --verbose

Enable detailed logging for troubleshooting.

- **Type**: Boolean flag
- **Required**: No
- **Default**: false (only show warnings and errors)
- **Example**: `--verbose`

**Verbose Mode Shows:**
- Solution loading strategy used (Roslyn/MSBuild/ProjectFile)
- Individual project loading progress with success/failure details
- Configuration loading and validation details
- Framework filter pattern matching
- Detailed error messages with stack traces

**When to Use Verbose Mode:**
- Troubleshooting solution loading failures
- Understanding why certain projects are excluded
- Diagnosing configuration issues
- Reporting bugs (include verbose output in issue reports)

## Usage Examples

### Basic Analysis

Analyze a solution with default settings:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln
```

**Expected Output:**
- Console output with project statistics
- Dependency graphs (PNG and SVG) in current directory
- CSV exports in current directory

### Specify Output Directory

Generate all output files in a specific directory:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --output ./reports
```

**Result:**
- Creates `./reports` directory if it doesn't exist
- All output files written to `./reports/`

### Custom Configuration

Use custom filter configuration to exclude specific dependencies:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --config ./my-filter-config.json
```

**Use Cases:**
- Custom framework exclusion patterns
- Company-specific internal framework handling
- Project-specific dependency filtering

See [Configuration Guide](configuration-guide.md) for configuration file format.

### Generate Only Text Reports

Skip CSV export and only show console output:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --reports text
```

**When to Use:**
- Quick analysis without persistent files
- CI/CD pipelines that only need pass/fail status
- Initial exploration of solution structure

### Generate Only CSV Files

Skip console output and generate only CSV files:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --reports csv
```

**When to Use:**
- Automated analysis with downstream processing
- Excel-based analysis workflows
- Batch processing of multiple solutions

### PNG Only for Faster Generation

Generate only PNG visualization (SVG generation is slower):

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --format png
```

**Performance:**
- PNG generation is faster than SVG
- Use PNG for quick iteration during analysis

### SVG Only for High Quality

Generate only SVG visualization:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --format svg
```

**Use Cases:**
- High-resolution presentations
- Print-quality documentation
- Web-based interactive visualizations

### Verbose Mode for Troubleshooting

Enable detailed logging to diagnose issues:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze --solution MySolution.sln --verbose
```

**Output Includes:**
- Project loading details (which loader succeeded: Roslyn/MSBuild/ProjectFile)
- Configuration loading and validation
- Framework filter matches
- Detailed error messages

### Combined Options

Realistic example combining multiple options:

```bash
dotnet run --project src/MasDependencyMap.CLI -- analyze \
  --solution D:\Projects\LegacyApp\LegacyApp.sln \
  --output D:\Reports\LegacyApp-Analysis \
  --config D:\Configs\company-filters.json \
  --reports all \
  --format both \
  --verbose
```

**This Command:**
1. Analyzes `LegacyApp.sln` from `D:\Projects\LegacyApp\`
2. Writes output to `D:\Reports\LegacyApp-Analysis\`
3. Uses custom filters from `D:\Configs\company-filters.json`
4. Generates both text and CSV reports
5. Generates both PNG and SVG visualizations
6. Shows verbose logging for troubleshooting

## Understanding Output

### Console Output

The tool displays progress indicators and summary information:

**Loading Phase:**
```
✓ Configuration loaded successfully
  Blocklist patterns: 5
  Allowlist patterns: 2
  Scoring weights: C=0.40, Cx=0.25, TD=0.20, EE=0.15

Parsed Options:
  Solution: D:\Projects\MyApp\MyApp.sln
  Output: current directory
  Config: none
  Reports: all
  Format: both
  Verbose: false
```

**Analysis Phase:**
```
✓ Analysis command received successfully!
```

**With --verbose Flag:**
```
Loading solution: D:\Projects\MyApp\MyApp.sln
Roslyn semantic analysis started...
Project loaded: MyApp.Core
Project loaded: MyApp.Services
Project loaded: MyApp.Web
Dependency graph built: 3 projects, 5 dependencies
Cycle detection completed: 0 cycles found
```

### Generated Files

#### Dependency Graphs

**{SolutionName}-dependencies.dot**
- Graphviz DOT format source file
- Can be edited manually and re-rendered
- Useful for customizing graph appearance

**{SolutionName}-dependencies.png**
- Raster image (default resolution)
- Quick viewing in image viewers
- Suitable for embedding in documents

**{SolutionName}-dependencies.svg**
- Vector image (scalable)
- High-quality rendering at any zoom level
- Suitable for web and print

**Graph Features:**
- **Nodes**: Projects in the solution with extraction scores as labels
- **Node Colors**: Heat map for extraction difficulty
  - Green (lightgreen): Easy extraction (scores 0-33)
  - Yellow: Medium difficulty (scores 34-66)
  - Red (lightcoral): Hard to extract (scores 67-100)
- **Score Labels**: Each node displays `"ProjectName\nScore: 42"`
- **Edge Colors**:
  - Black: Normal dependencies
  - Red: Circular dependencies
  - Yellow: Recommended cycle-breaking points (top 10)
  - Blue: Cross-solution dependencies (multi-solution analysis)
- **Legends**: Automatic legends for extraction difficulty and dependency types

#### CSV Exports

**{SolutionName}-extraction-scores.csv**

Extraction difficulty scores for each project.

**Columns:**
- `Project Name` - Project identifier
- `Extraction Score` - Overall difficulty score (0-100, higher = harder)
- `Coupling Score` - Number of dependencies
- `Complexity Score` - Cyclomatic complexity
- `Tech Debt Score` - Framework version age
- `External Exposure Score` - Public API surface area

**{SolutionName}-cycle-analysis.csv**

Circular dependency analysis (generated only if cycles detected).

**Columns:**
- `Cycle ID` - Unique identifier for each cycle
- `Size` - Number of projects in cycle
- `Projects` - Comma-separated project names
- `Suggested Break` - Recommended edge to break (`Source → Target`)
- `Coupling` - Method call count for suggested break edge
- `Rationale` - Explanation of why this break point is recommended

**{SolutionName}-dependency-matrix.csv**

Full project-to-project dependency matrix with coupling scores.

**Columns:**
- `Source Project` - Project with outgoing dependency
- `Target Project` - Project being depended upon
- `Coupling Score` - Number of method calls from source to target (when available)

**Format:** RFC 4180 CSV with UTF-8 BOM for Excel compatibility, sorted by source then target project

### Text Reports

Text reports are saved to `{SolutionName}-analysis-report.txt` with formatted tables.

**File Format:**
- UTF-8 encoding without BOM
- 80-character width for readability
- Formatted using Spectre.Console Table (plain text ASCII art in file)

**Report Sections:**

1. **Header**: Solution name, analysis date (UTC), total projects
2. **Dependency Overview**: Total references, framework/custom breakdown with percentages
3. **Extraction Difficulty Scores**:
   - Easiest Candidates (top 10 with lowest scores)
   - Hardest Candidates (top 10 with highest scores)
4. **Cycle Detection** (if cycles found): Total chains, participation %, largest cycle, detailed listing
5. **Cycle-Breaking Recommendations** (if applicable): Top 5 suggestions with coupling and rationale

**Example Table (Easiest Candidates):**
```
+------------------------------------------------------------------------------+
|   Rank | Project Name           |   Score |   Incoming |   Outgoing |   APIs |
|--------+------------------------+---------+------------+------------+--------|
|      1 | Legacy.ModuleB         |       6 |          0 |          1 |      0 |
|      2 | Legacy.ModuleA         |      11 |          1 |          0 |      0 |
|      3 | UI                     |      21 |          0 |          4 |      0 |
+------------------------------------------------------------------------------+
```

**Console Output with --verbose:**
- Same formatted tables displayed in terminal with colors
- Uses injected IAnsiConsole for proper rendering

## Error Handling

The tool uses a consistent 3-part error format:

```
Error: [What failed]
Reason: [Why it failed]
Suggestion: [How to fix it]
```

**Example:**

```
Error: Solution file not found
Reason: No file exists at D:\Projects\MyApp\MyApp.sln
Suggestion: Verify the path and try again
```

**Common Errors:**
- **Graphviz not found**: Install Graphviz and add to PATH (see [Troubleshooting Guide](troubleshooting.md))
- **JSON syntax error**: Fix configuration file syntax (see [Configuration Guide](configuration-guide.md))
- **Solution loading failed**: Check .NET SDK installation and project format compatibility

See the [Troubleshooting Guide](troubleshooting.md) for detailed solutions to common issues.

## Performance Expectations

**Analysis Time:**
- Small solutions (< 10 projects): < 1 minute
- Medium solutions (10-50 projects): 1-5 minutes
- Large solutions (50-100 projects): 5-15 minutes
- Very large solutions (100-400+ projects): 15-30+ minutes

**Memory Usage:**
- Typical: < 1GB for most solutions
- Large solutions: 1-4GB
- Roslyn semantic analysis is memory-intensive

**Optimization Tips:**
- Close Visual Studio before running analysis (releases memory)
- Run analysis sequentially on multiple solutions (not parallel)
- Use `--reports text` for faster analysis without CSV overhead

## Advanced Usage

### Analyzing Multiple Solutions

Run analysis on multiple solutions sequentially:

```bash
# Windows
for %s in (*.sln) do dotnet run --project src\MasDependencyMap.CLI -- analyze --solution %s --output reports\%~ns

# macOS/Linux
for sln in *.sln; do
  dotnet run --project src/MasDependencyMap.CLI -- analyze --solution "$sln" --output "reports/$(basename "$sln" .sln)"
done
```

### CI/CD Integration

Example GitHub Actions workflow:

```yaml
- name: Analyze Dependencies
  run: |
    dotnet run --project src/MasDependencyMap.CLI -- analyze \
      --solution MySolution.sln \
      --output ./dependency-analysis \
      --reports csv \
      --verbose

- name: Upload Analysis Results
  uses: actions/upload-artifact@v3
  with:
    name: dependency-analysis
    path: ./dependency-analysis
```

### PowerShell Wrapper Script

Example PowerShell script for convenient analysis:

```powershell
# analyze-solution.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$SolutionPath
)

$outputDir = "analysis-$(Get-Date -Format 'yyyy-MM-dd-HHmmss')"
dotnet run --project src/MasDependencyMap.CLI -- analyze `
    --solution $SolutionPath `
    --output $outputDir `
    --reports all `
    --format both `
    --verbose

Write-Host "Analysis complete. Results in: $outputDir"
```

## Getting Help

### Built-in Help

```bash
# Show all available commands
dotnet run --project src/MasDependencyMap.CLI -- --help

# Show analyze command help
dotnet run --project src/MasDependencyMap.CLI -- analyze --help

# Show version
dotnet run --project src/MasDependencyMap.CLI -- --version
```

### Documentation

- **[Configuration Guide](configuration-guide.md)** - JSON configuration reference
- **[Troubleshooting Guide](troubleshooting.md)** - Common issues and solutions
- **[README](../README.md)** - Quick start and installation

### Support

If you need assistance:
1. Check the [Troubleshooting Guide](troubleshooting.md) for common issues
2. Run analysis with `--verbose` flag for detailed diagnostics
3. Open an issue on GitHub with verbose output and system information

**Include in Bug Reports:**
- Command used (exact arguments)
- Verbose output (`--verbose` flag)
- .NET SDK version (`dotnet --version`)
- Graphviz version (`dot -version`)
- Operating system and version
