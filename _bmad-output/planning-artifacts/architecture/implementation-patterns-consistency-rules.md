# Implementation Patterns & Consistency Rules

## Pattern Categories Defined

**Critical Conflict Points Identified:** 14 areas where AI agents could make different implementation choices that would cause inconsistencies or conflicts in the codebase.

## Naming Patterns

**Namespace Organization:**
- **Rule:** Organize namespaces by feature/domain, not by layer
- **Pattern:** `MasDependencyMap.Core.{Feature}`
- **Examples:**
  - `MasDependencyMap.Core.SolutionLoading`
  - `MasDependencyMap.Core.DependencyAnalysis`
  - `MasDependencyMap.Core.CycleDetection`
  - `MasDependencyMap.Core.Scoring`
  - `MasDependencyMap.Core.Visualization`
  - `MasDependencyMap.Core.Reporting`
- **Rationale:** Clear separation of domain concepts, aligns with 5-7 estimated components, easier testing organization

**Interface & Class Naming:**
- **Rule:** Use I-prefix for interfaces, descriptive names for implementations
- **Pattern:** `I{Concept}` with `{Technology}{Concept}` implementations
- **Examples:**
  - `ISolutionLoader` with `RoslynSolutionLoader`, `MSBuildSolutionLoader`, `ProjectFileSolutionLoader`
  - `IGraphvizRenderer` with `GraphvizRenderer`
  - `ICycleDetector` with `TarjanCycleDetector`
- **Anti-Pattern:** ❌ `SolutionLoader` (interface) with `SolutionLoaderImpl` (implementation)

**Method Naming:**
- **Rule:** Always use Async suffix for async methods
- **Pattern:** `{Action}Async` returning `Task<T>` or `Task`
- **Examples:**
  - `Task<SolutionAnalysis> LoadAsync(string solutionPath)`
  - `Task<string> RenderToFileAsync(string dotFilePath, GraphvizOutputFormat format)`
  - `Task ValidateConfigurationAsync()`
- **Anti-Pattern:** ❌ `Task<SolutionAnalysis> Load(string solutionPath)` (missing Async suffix)

**File Naming:**
- **Rule:** File names match class names exactly
- **Pattern:** `{ClassName}.cs`
- **Examples:**
  - `RoslynSolutionLoader.cs`
  - `CycleDetector.cs`
  - `ExtractionScoreCalculator.cs`
- **Anti-Pattern:** ❌ `roslyn-solution-loader.cs` or `roslyn_solution_loader.cs`

**Configuration JSON Naming:**
- **Rule:** PascalCase for all JSON property names
- **Pattern:** Match C# POCO property names exactly
- **Example:**
```json
{
  "FrameworkFilters": {
    "BlockList": ["Microsoft.*", "System.*"],
    "AllowList": ["YourCompany.*"]
  },
  "ScoringWeights": {
    "Coupling": 0.40,
    "Complexity": 0.30,
    "TechDebt": 0.20,
    "ExternalExposure": 0.10
  }
}
```
- **Anti-Pattern:** ❌ `"blockList"` or `"block_list"`

**CSV Column Naming:**
- **Rule:** Title Case with Spaces for stakeholder-ready output
- **Pattern:** Human-readable column headers
- **Examples:**
  - `"Project Name"`, `"Extraction Score"`, `"Coupling Metric"`
  - `"Incoming References"`, `"Outgoing References"`, `"Cycle Count"`
  - `"Technology Version"`, `"External APIs"`
- **Rationale:** Excel/Google Sheets friendly, no manual reformatting needed (NFR18)
- **Anti-Pattern:** ❌ `"project_name"` or `"ExtractionScore"`

## Structure Patterns

**Test Organization:**
- **Rule:** Tests mirror Core namespace structure in separate tests/ folder
- **Pattern:** `tests/MasDependencyMap.Core.Tests/{Feature}/{ClassNameTests.cs}`
- **Examples:**
  - `tests/MasDependencyMap.Core.Tests/SolutionLoading/RoslynSolutionLoaderTests.cs`
  - `tests/MasDependencyMap.Core.Tests/CycleDetection/CycleDetectorTests.cs`
  - `tests/MasDependencyMap.Core.Tests/Scoring/ExtractionScoreCalculatorTests.cs`
- **Rationale:** Clear separation between production and test code, easy to find corresponding tests
- **Anti-Pattern:** ❌ Flat structure in tests/ or co-located test files

**Test Class Naming:**
- **Rule:** Tests suffix for test classes
- **Pattern:** `{ClassName}Tests`
- **Examples:**
  - `public class RoslynSolutionLoaderTests`
  - `public class CycleDetectorTests`
  - `public class GraphvizRendererTests`
- **Anti-Pattern:** ❌ `RoslynSolutionLoaderTest` (singular) or `RoslynSolutionLoaderSpec`

**Test Method Naming:**
- **Rule:** MethodName_Scenario_ExpectedResult
- **Pattern:** `{MethodName}_{Scenario}_{ExpectedResult}`
- **Examples:**
  - `LoadAsync_ValidSolutionPath_ReturnsAnalysis()`
  - `LoadAsync_InvalidPath_ThrowsFileNotFoundException()`
  - `RenderToFileAsync_GraphvizNotInstalled_ThrowsGraphvizNotFoundException()`
  - `CalculateScore_HighCoupling_ReturnsHighDifficultyScore()`
- **Rationale:** Clear what's being tested, readable in test runners, standard .NET convention
- **Anti-Pattern:** ❌ `Should_return_analysis_when_path_is_valid()` or `TestLoadAsync()`

**Configuration File Locations:**
- **Rule:** User-specified via CLI with sensible defaults
- **Default Behavior:** Look for `filter-config.json` and `scoring-config.json` in current directory
- **Override:** `--config path/to/config.json` via CLI argument
- **Examples:**
  - Default: `./filter-config.json`, `./scoring-config.json`
  - Custom: `--config ./configs/my-analysis-config.json`
- **Rationale:** Supports multiple analysis scenarios, version-controlled configs, different profiles

**Output File Naming:**
- **Rule:** Solution name prefix, no timestamps in filenames
- **Pattern:** `{SolutionName}-{OutputType}.{Extension}`
- **Examples:**
  - `MyLegacy-dependencies.dot`
  - `MyLegacy-dependencies.png`
  - `MyLegacy-analysis-report.txt`
  - `MyLegacy-extraction-scores.csv`
  - `MyLegacy-cycle-analysis.csv`
- **Organization:** User creates timestamped output folders: `--output ./analysis-2026-01-18`
- **Rationale:** Clean filenames, easy to find by solution name, folder provides run context
- **Anti-Pattern:** ❌ `MyLegacy-dependencies-2026-01-18-143022.dot` (timestamp in filename)

## Format Patterns

**Error Message Format:**
- **Rule:** Structured messages with Spectre.Console markup
- **Pattern:** Three-part structure with visual hierarchy
- **Format:**
```csharp
console.MarkupLine("[red]Error:[/] {ErrorMessage}");
console.MarkupLine("[dim]Reason:[/] {DetailedReason}");
console.MarkupLine("[dim]Suggestion:[/] {RemediationSteps}");
```
- **Example:**
```csharp
console.MarkupLine("[red]Error:[/] Could not load solution at [yellow]{0}[/]", path);
console.MarkupLine("[dim]Reason:[/] Roslyn workspace failed to open solution file");
console.MarkupLine("[dim]Suggestion:[/] Verify .NET SDK version matches solution target framework");
```
- **Rationale:** Visual hierarchy, colored output, clear remediation steps (NFR14)
- **Anti-Pattern:** ❌ Plain `Console.WriteLine("ERROR: ...")` without structure or color

**Logging Message Templates:**
- **Rule:** Structured logging with named placeholders
- **Pattern:** Use named placeholders `{PropertyName}`, not string interpolation
- **Examples:**
```csharp
_logger.LogInformation("Loading solution from {SolutionPath} with {ProjectCount} projects", path, count);
_logger.LogWarning("Roslyn failed for {SolutionPath}, falling back to MSBuild. Reason: {FailureReason}", path, reason);
_logger.LogError("All loaders failed for {SolutionPath}. Last error: {ErrorMessage}", path, error);
```
- **Rationale:** Enables structured logging properties, better for searching/aggregation, more performant than string interpolation
- **Anti-Pattern:** ❌ `_logger.LogInformation($"Loading solution from {path} with {count} projects")`

**Exception Handling:**
- **Rule:** Custom exception hierarchy for domain errors
- **Pattern:** Base exception per domain area with specific implementations
- **Example Hierarchy:**
```csharp
// Base exceptions
public class SolutionLoadException : Exception { }
public class GraphvizException : Exception { }
public class ConfigurationException : Exception { }

// Specific exceptions
public class RoslynLoadException : SolutionLoadException { }
public class MSBuildLoadException : SolutionLoadException { }
public class ProjectFileLoadException : SolutionLoadException { }
public class GraphvizNotFoundException : GraphvizException { }
public class GraphvizRenderException : GraphvizException { }
```
- **Usage in Fallback Chain:**
```csharp
try {
    return await _roslynLoader.LoadAsync(path);
}
catch (RoslynLoadException ex) {
    _logger.LogWarning("Roslyn failed, trying MSBuild: {Error}", ex.Message);
    try {
        return await _msbuildLoader.LoadAsync(path);
    }
    catch (MSBuildLoadException ex2) {
        _logger.LogWarning("MSBuild failed, trying project file parsing: {Error}", ex2.Message);
        return await _projectFileLoader.LoadAsync(path);
    }
}
```
- **Rationale:** Catch specific exception types in fallback chain, preserve context, standard .NET pattern

## Communication Patterns

**Progress Reporting:**
- **Rule:** Use Spectre.Console.Progress for long-running operations
- **Pattern:** Wrap operations in progress context with descriptive tasks
- **Example:**
```csharp
await AnsiConsole.Progress()
    .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
    .StartAsync(async ctx =>
    {
        var loadTask = ctx.AddTask("Loading solutions", maxValue: solutionPaths.Count);
        foreach (var path in solutionPaths)
        {
            await LoadSolutionAsync(path);
            loadTask.Increment(1);
        }
    });
```
- **Rationale:** Clear visual feedback, percentage completion, meets NFR17 requirements

**Logging Levels:**
- **Rule:** Use appropriate log levels consistently
- **Pattern:**
  - `LogError`: Unrecoverable errors that block operation
  - `LogWarning`: Recoverable errors, fallback triggered
  - `LogInformation`: Major milestones (solution loaded, cycle detected) - verbose mode only
  - `LogDebug`: Detailed diagnostics - verbose mode only
- **Example:**
```csharp
_logger.LogError("Failed to load solution {SolutionPath}: {Error}", path, error);
_logger.LogWarning("Roslyn loader failed, falling back to MSBuild for {SolutionPath}", path);
_logger.LogInformation("Successfully loaded {ProjectCount} projects from {SolutionPath}", count, path);
_logger.LogDebug("Parsing project file at {ProjectPath}", projectPath);
```

## Enforcement Guidelines

**All AI Agents MUST:**

1. **Follow namespace organization by feature** - Never organize by layer (Models/, Services/)
2. **Use I-prefix for all interfaces** - Never create interfaces without I-prefix
3. **Always use Async suffix** - Never create async methods without Async suffix
4. **Use PascalCase in JSON configs** - Never use camelCase or snake_case
5. **Use Title Case with Spaces in CSV headers** - Never use code-style naming in CSV exports
6. **Mirror namespace structure in tests** - Never use flat test organization
7. **Use MethodName_Scenario_ExpectedResult for test methods** - Never use BDD-style or unclear naming
8. **Use Spectre.Console markup for user errors** - Never use plain Console.WriteLine for errors
9. **Use structured logging with named placeholders** - Never use string interpolation in log messages
10. **Create custom exceptions for domain errors** - Never use generic exceptions when domain-specific ones apply
11. **File names match class names exactly** - Never use kebab-case or snake_case for file names
12. **Solution name prefix for output files** - Never include timestamps in output filenames

**Pattern Enforcement:**

- **During Code Review:** Verify naming conventions, namespace organization, test structure
- **During Testing:** Ensure test names follow pattern, tests are in correct locations
- **During Implementation:** Check that error messages use Spectre.Console markup, logging uses structured templates
- **Pattern Violations:** Document in PR comments, request changes before merge

**Pattern Documentation:**

- This architecture document is the single source of truth for patterns
- Project context file (when created) will reference these patterns
- README should link to this document for pattern guidance

## Pattern Examples

**Good Examples:**

**Namespace & Class Organization:**
```csharp
namespace MasDependencyMap.Core.SolutionLoading;

public interface ISolutionLoader
{
    Task<SolutionAnalysis> LoadAsync(string solutionPath);
}

public class RoslynSolutionLoader : ISolutionLoader
{
    private readonly ILogger<RoslynSolutionLoader> _logger;

    public async Task<SolutionAnalysis> LoadAsync(string solutionPath)
    {
        _logger.LogInformation("Loading solution from {SolutionPath}", solutionPath);
        // Implementation
    }
}
```

**Test Organization:**
```csharp
// File: tests/MasDependencyMap.Core.Tests/SolutionLoading/RoslynSolutionLoaderTests.cs
namespace MasDependencyMap.Core.Tests.SolutionLoading;

public class RoslynSolutionLoaderTests
{
    [Fact]
    public async Task LoadAsync_ValidSolutionPath_ReturnsAnalysis()
    {
        // Arrange
        var loader = new RoslynSolutionLoader(/* dependencies */);

        // Act
        var result = await loader.LoadAsync("test.sln");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoadAsync_InvalidPath_ThrowsFileNotFoundException()
    {
        // Test implementation
    }
}
```

**Error Handling with Markup:**
```csharp
try
{
    var result = await _graphvizRenderer.RenderToFileAsync(dotFile, OutputFormat.Png);
}
catch (GraphvizNotFoundException ex)
{
    console.MarkupLine("[red]Error:[/] Graphviz is not installed or not in PATH");
    console.MarkupLine("[dim]Reason:[/] Could not find 'dot' executable");
    console.MarkupLine("[dim]Suggestion:[/] Install Graphviz from https://graphviz.org or add to PATH");
    return 1;
}
```

**Configuration JSON:**
```json
{
  "FrameworkFilters": {
    "BlockList": [
      "Microsoft.*",
      "System.*",
      "mscorlib",
      "netstandard"
    ],
    "AllowList": [
      "YourCompany.*"
    ]
  },
  "ScoringWeights": {
    "Coupling": 0.40,
    "Complexity": 0.30,
    "TechDebt": 0.20,
    "ExternalExposure": 0.10
  }
}
```

**Anti-Patterns:**

**❌ Layer-based namespace organization:**
```csharp
namespace MasDependencyMap.Core.Services;  // Wrong - organized by layer
namespace MasDependencyMap.Core.Models;    // Wrong - organized by layer
```

**❌ Missing interface prefix:**
```csharp
public interface SolutionLoader { }  // Wrong - no I-prefix
```

**❌ Missing Async suffix:**
```csharp
public async Task<SolutionAnalysis> Load(string path) { }  // Wrong - no Async
```

**❌ String interpolation in logging:**
```csharp
_logger.LogInformation($"Loading {path}");  // Wrong - use placeholders
```

**❌ camelCase in JSON config:**
```json
{
  "frameworkFilters": {  // Wrong - should be PascalCase
    "blockList": []      // Wrong - should be PascalCase
  }
}
```

**❌ Code-style CSV headers:**
```csv
ProjectName,ExtractionScore,CouplingMetric  // Wrong - use Title Case with Spaces
```

**❌ Flat test organization:**
```
tests/MasDependencyMap.Core.Tests/
  RoslynSolutionLoaderTests.cs  // Wrong - should mirror namespace structure
  CycleDetectorTests.cs          // Wrong - should be in subdirectories
```
