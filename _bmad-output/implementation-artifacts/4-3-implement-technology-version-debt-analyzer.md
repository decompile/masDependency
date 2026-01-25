# Story 4.3: Implement Technology Version Debt Analyzer

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want technology version detected from project files (.NET 3.5 vs .NET 6),
So that older frameworks contribute to extraction difficulty scores.

## Acceptance Criteria

**Given** A .csproj or .vbproj file
**When** TechDebtAnalyzer.AnalyzeAsync() is called
**Then** TargetFramework is parsed from the project file XML
**And** .NET Framework versions are identified (net35, net40, net45, net461, net472, net48)
**And** .NET Core/Modern versions are identified (netcoreapp3.1, net5.0, net6.0, net7.0, net8.0)
**And** Tech debt score is calculated: .NET 3.5 = 100 (highest debt), .NET 8 = 0 (no debt)
**And** Intermediate versions are scored proportionally (net472 = 40, net6.0 = 10)
**And** ILogger logs detected framework versions

## Tasks / Subtasks

- [x] Create TechDebtMetric model class (AC: Store tech debt metrics with framework info)
  - [x] Define TechDebtMetric record with properties: ProjectName, ProjectPath, TargetFramework, NormalizedScore
  - [x] Add XML documentation explaining tech debt calculation and version scoring
  - [x] Use record type for immutability (C# 9+ pattern, consistent with Stories 4.1 & 4.2)
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [x] Create ITechDebtAnalyzer interface (AC: Abstraction for DI)
  - [x] Define AnalyzeAsync(ProjectNode project, CancellationToken cancellationToken = default) method signature
  - [x] Return Task<TechDebtMetric> for single project analysis
  - [x] Add XML documentation with examples and exception documentation
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [x] Implement TechDebtAnalyzer class (AC: Parse project files and detect framework versions)
  - [x] Implement ITechDebtAnalyzer interface
  - [x] Inject ILogger<TechDebtAnalyzer> via constructor for structured logging
  - [x] Implement AnalyzeAsync method with XML parsing of .csproj/.vbproj files
  - [x] Parse TargetFramework element from project XML (SDK-style and legacy formats)
  - [x] Handle TargetFrameworkVersion for legacy .NET Framework projects
  - [x] Handle TargetFrameworks (plural) for multi-targeting projects (use first/lowest target)
  - [x] Implement framework version detection and categorization logic
  - [x] File-scoped namespace declaration (C# 10+ pattern)
  - [x] Async method with Async suffix and ConfigureAwait(false) per project conventions
  - [x] Use XDocument for XML parsing (System.Xml.Linq)

- [x] Implement framework version detection (AC: Identify .NET Framework and .NET Core/Modern versions)
  - [x] Parse target framework monikers (TFMs): net35, net40, net45, net461, net472, net48
  - [x] Parse modern TFMs: netcoreapp3.1, net5.0, net6.0, net7.0, net8.0, net9.0
  - [x] Handle legacy format: v4.7.2 → net472
  - [x] Handle SDK-style format: net8.0 → net8.0
  - [x] Map TFM strings to framework categories (Legacy .NET Framework vs Modern .NET)
  - [x] Extract version numbers for scoring calculation

- [x] Implement tech debt scoring algorithm (AC: Score 0-100 with proportional intermediate values)
  - [x] Define scoring thresholds: .NET 3.5 = 100, .NET 8+ = 0
  - [x] .NET Framework: net35 (100) → net48 (40) with linear interpolation
  - [x] .NET Core/Modern: netcoreapp3.1 (30) → net5.0 (20) → net6.0 (10) → net7.0/8.0/9.0+ (0)
  - [x] Use linear interpolation for intermediate versions
  - [x] Ensure normalized score is clamped to 0-100 range using Math.Clamp
  - [x] Document scoring algorithm in XML comments and code comments
  - [x] Higher normalized score = older framework = harder to extract (matches Epic 4 scoring semantics)

- [x] Implement fallback handling (AC: Graceful degradation when XML parsing fails)
  - [x] Wrap XML parsing in try-catch for XmlException, FileNotFoundException, IOException
  - [x] On exception, log warning: "Could not parse TargetFramework for {ProjectName}: {Reason}"
  - [x] Return TechDebtMetric with: TargetFramework = "unknown", NormalizedScore = 50 (neutral)
  - [x] Neutral score (50) indicates "unknown tech debt" for scoring algorithm
  - [x] Ensure fallback doesn't throw exceptions (graceful degradation)

- [x] Add structured logging with named placeholders (AC: Log framework detection)
  - [x] Log Information: "Analyzing tech debt for project {ProjectName}" at start
  - [x] Log Information: "Detected framework {TargetFramework} for {ProjectName}" after parsing
  - [x] Log Debug: "Project {ProjectName}: Framework={TargetFramework}, Score={NormalizedScore}" for results
  - [x] Log Warning: "Could not parse framework for {ProjectName}, defaulting to neutral score 50: {Reason}" on fallback
  - [x] Use named placeholders, NOT string interpolation (critical project rule)
  - [x] Log level: Information for key milestones, Debug for detailed metrics, Warning for fallback

- [x] Register service in DI container (AC: Service integration)
  - [x] Add registration in CLI Program.cs DI configuration
  - [x] Use services.AddSingleton<ITechDebtAnalyzer, TechDebtAnalyzer>() pattern
  - [x] Register in "Epic 4: Extraction Scoring Services" section (after IComplexityMetricCalculator)
  - [x] Follow existing DI registration patterns from Stories 4.1 & 4.2

- [x] Create comprehensive unit tests (AC: Test coverage)
  - [x] Create test class: tests/MasDependencyMap.Core.Tests/ExtractionScoring/TechDebtAnalyzerTests.cs
  - [x] Test: AnalyzeAsync_Net35Project_Returns100Score (highest debt)
  - [x] Test: AnalyzeAsync_Net48Project_Returns40Score (legacy but recent)
  - [x] Test: AnalyzeAsync_NetCore31Project_Returns30Score (old modern)
  - [x] Test: AnalyzeAsync_Net60Project_Returns10Score (recent modern)
  - [x] Test: AnalyzeAsync_Net80Project_Returns0Score (no debt)
  - [x] Test: AnalyzeAsync_InvalidProjectFile_ReturnsFallbackScore50 (fallback behavior)
  - [x] Test: AnalyzeAsync_LegacyFrameworkFormat_ParsesCorrectly (v4.7.2 handling)
  - [x] Test: AnalyzeAsync_MultiTargeting_UsesLowestTarget (netstandard2.0;net6.0 → net6.0)
  - [x] Test: AnalyzeAsync_NullProject_ThrowsArgumentNullException (defensive programming)
  - [x] Test: AnalyzeAsync_CancellationRequested_ThrowsOperationCanceledException (cancellation support)
  - [x] Use xUnit, FluentAssertions pattern from project conventions
  - [x] Test naming: {MethodName}_{Scenario}_{ExpectedResult} pattern
  - [x] Arrange-Act-Assert structure

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Feature-based namespace: MasDependencyMap.Core.ExtractionScoring (NOT layer-based)
  - [x] Async suffix on all async methods (AnalyzeAsync)
  - [x] File-scoped namespace declarations (all files)
  - [x] ILogger injection via constructor (NOT static logger)
  - [x] ConfigureAwait(false) in library code (Core layer)
  - [x] XML documentation on all public APIs (model, interface, implementation)
  - [x] Test files mirror Core namespace structure (tests/MasDependencyMap.Core.Tests/ExtractionScoring)

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 4.3 Tech Debt Requirements:**

This story implements technology version debt analysis, the THIRD metric in Epic 4's extraction difficulty scoring framework.

**Epic 4 Vision (Recap):**
- Story 4.1: Coupling metrics ✅ DONE
- Story 4.2: Cyclomatic complexity metrics ✅ DONE
- Story 4.3: Technology version debt metrics (THIS STORY)
- Story 4.4: External API exposure metrics
- Story 4.5: Combined extraction score calculator (uses 4.1-4.4)
- Story 4.6: Ranked extraction candidate lists
- Story 4.7: Heat map visualization with color-coded scores
- Story 4.8: Display extraction scores as node labels

**Story 4.3 Unique Challenges:**

1. **XML Parsing (Different from Stories 4.1 & 4.2):**
   - Story 4.1: Graph traversal (in-memory data)
   - Story 4.2: Roslyn semantic analysis (complex API)
   - Story 4.3: XML parsing (file I/O + XML structure understanding)
   - MUST handle both SDK-style (.NET Core+) and legacy (.NET Framework) project formats

2. **Target Framework Moniker (TFM) Parsing:**
   - SDK-style: `<TargetFramework>net8.0</TargetFramework>`
   - Legacy: `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>`
   - Multi-targeting: `<TargetFrameworks>netstandard2.0;net6.0</TargetFrameworks>` (plural!)
   - MUST correctly parse all three formats

3. **Version Scoring Strategy:**
   - Story 4.1: Relative normalization (max in solution = 100)
   - Story 4.2: Absolute normalization (industry complexity thresholds)
   - Story 4.3: **Timeline-based normalization (older = higher debt)**
   - .NET 3.5 (2008, 18 years old) = 100 debt
   - .NET 8 (2023, current) = 0 debt
   - Linear interpolation between versions

4. **Fallback Handling (Like Story 4.2):**
   - Similar to Story 4.2's Roslyn fallback
   - If XML parsing fails → default to neutral score 50
   - Graceful degradation ensures partial data availability

🚨 **CRITICAL - Target Framework Moniker (TFM) Parsing:**

**TFM Format Variations:**

The tool MUST handle a 20-YEAR version span (.NET Framework 3.5 from 2008 to .NET 8+ from 2023+).

**SDK-Style Projects (.NET Core+):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Legacy .NET Framework Projects:**
```xml
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
</Project>
```

**Multi-Targeting Projects:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net6.0;net8.0</TargetFrameworks>
  </PropertyGroup>
</Project>
```

**TFM Parsing Strategy:**

1. **Try SDK-style first:** Look for `<TargetFramework>` (singular)
2. **Try multi-targeting:** Look for `<TargetFrameworks>` (plural) → Use FIRST or LOWEST target
3. **Try legacy format:** Look for `<TargetFrameworkVersion>` → Convert v4.7.2 to net472

**TFM String Formats:**

| TFM String | Meaning | Debt Score |
|------------|---------|------------|
| `net35`, `net3.5` | .NET Framework 3.5 | 100 |
| `net40`, `net4.0` | .NET Framework 4.0 | 90 |
| `net45`, `net4.5` | .NET Framework 4.5 | 80 |
| `net451` | .NET Framework 4.5.1 | 75 |
| `net452` | .NET Framework 4.5.2 | 70 |
| `net46`, `net4.6` | .NET Framework 4.6 | 65 |
| `net461` | .NET Framework 4.6.1 | 60 |
| `net462` | .NET Framework 4.6.2 | 55 |
| `net47`, `net4.7` | .NET Framework 4.7 | 50 |
| `net471` | .NET Framework 4.7.1 | 45 |
| `net472` | .NET Framework 4.7.2 | 40 |
| `net48`, `net4.8` | .NET Framework 4.8 | 40 |
| `netcoreapp3.1` | .NET Core 3.1 (LTS) | 30 |
| `net5.0` | .NET 5 (EOL) | 20 |
| `net6.0` | .NET 6 (LTS) | 10 |
| `net7.0` | .NET 7 (EOL) | 5 |
| `net8.0` | .NET 8 (Current LTS) | 0 |
| `net9.0+` | .NET 9+ (Future) | 0 |

**Legacy Format Conversion:**

```csharp
// Legacy format: <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
// Convert: v4.7.2 → net472

string ConvertLegacyToTfm(string legacyVersion)
{
    // Remove 'v' prefix: v4.7.2 → 4.7.2
    var version = legacyVersion.TrimStart('v');

    // Remove dots: 4.7.2 → 472
    var tfm = "net" + version.Replace(".", "");

    return tfm; // net472
}
```

**Multi-Targeting Strategy:**

```csharp
// Multi-targeting: <TargetFrameworks>netstandard2.0;net6.0;net8.0</TargetFrameworks>
// Strategy: Use FIRST target (primary) OR LOWEST target (most restrictive)

string ParseMultiTargeting(string frameworks)
{
    var targets = frameworks.Split(';');

    // Option 1: Use first target (primary target)
    return targets[0]; // netstandard2.0

    // Option 2: Use lowest/oldest target (most restrictive, conservative scoring)
    // For scoring, we want to know the OLDEST framework supported (highest debt)
    // This gives a more accurate extraction difficulty assessment
    return targets.OrderBy(ParseVersion).First(); // netstandard2.0
}
```

**Recommendation:** Use **first target** approach for simplicity. Multi-targeting typically lists primary target first.

🚨 **CRITICAL - Tech Debt Scoring Algorithm:**

**Scoring Philosophy:**

Older frameworks = Higher extraction difficulty because:
1. Larger API surface area changes between old and modern .NET
2. More breaking changes to handle during migration
3. Legacy patterns (app.config, packages.config) vs modern patterns (appsettings.json, PackageReference)
4. Missing modern features (Span<T>, async streams, source generators, etc.)

**Scoring Thresholds:**

```
Framework Timeline → Tech Debt Score (0-100)
──────────────────────────────────────────────
.NET Framework 3.5 (2008) → 100 (Highest debt, 18 years old)
.NET Framework 4.0 (2010) → 90  (16 years old)
.NET Framework 4.5 (2012) → 80  (14 years old)
.NET Framework 4.6 (2015) → 65  (11 years old)
.NET Framework 4.7 (2017) → 50  (9 years old)
.NET Framework 4.7.2 (2018) → 40 (8 years old)
.NET Framework 4.8 (2019) → 40  (7 years old, last .NET Framework)
──────────────────────────────────────────────
.NET Core 3.1 (2019, LTS) → 30 (First modern .NET with good migration path)
.NET 5 (2020, EOL) → 20
.NET 6 (2021, LTS) → 10
.NET 7 (2022, EOL) → 5
.NET 8 (2023, Current LTS) → 0 (No debt)
.NET 9+ (2024+) → 0 (Future-proof)
```

**Linear Interpolation Formula:**

```csharp
public static double CalculateTechDebtScore(string targetFramework)
{
    // Map TFM to score
    var scores = new Dictionary<string, double>
    {
        // .NET Framework (Legacy)
        ["net35"] = 100, ["net3.5"] = 100,
        ["net40"] = 90, ["net4.0"] = 90,
        ["net45"] = 80, ["net4.5"] = 80,
        ["net451"] = 75,
        ["net452"] = 70,
        ["net46"] = 65, ["net4.6"] = 65,
        ["net461"] = 60,
        ["net462"] = 55,
        ["net47"] = 50, ["net4.7"] = 50,
        ["net471"] = 45,
        ["net472"] = 40,
        ["net48"] = 40, ["net4.8"] = 40,

        // .NET Core / Modern
        ["netcoreapp3.1"] = 30,
        ["net5.0"] = 20,
        ["net6.0"] = 10,
        ["net7.0"] = 5,
        ["net8.0"] = 0,
        ["net9.0"] = 0
    };

    // Normalize TFM (handle variations like "net6" vs "net6.0")
    var normalizedTfm = NormalizeTfm(targetFramework);

    // Direct lookup
    if (scores.TryGetValue(normalizedTfm, out var score))
    {
        return score;
    }

    // Fallback: Unknown framework → neutral score 50
    return 50;
}

private static string NormalizeTfm(string tfm)
{
    // Normalize: net6 → net6.0, net472 → net472
    // Handle common variations

    if (tfm.StartsWith("netcoreapp"))
        return tfm; // netcoreapp3.1

    if (tfm.StartsWith("net"))
    {
        var versionPart = tfm.Substring(3); // Remove "net" prefix

        // Already normalized (net472, net8.0)
        if (versionPart.Contains(".") || versionPart.Length > 2)
            return tfm;

        // Add .0 suffix (net6 → net6.0)
        return tfm + ".0";
    }

    return tfm;
}
```

**Why Not Use Roslyn for This?**

Story 4.2 used Roslyn for semantic analysis (method complexity). Story 4.3 uses XML parsing instead because:
1. **TargetFramework is XML metadata, not code** - No semantic analysis needed
2. **Roslyn is heavyweight** - Loads entire workspace, MSBuild integration, memory intensive
3. **XML parsing is fast and simple** - XDocument, single property read, no dependencies
4. **Works for all project types** - SDK-style, legacy, VB.NET, F#, all have TargetFramework in XML

🚨 **CRITICAL - XML Parsing with XDocument:**

**From Project Context (lines 18-51):**

Technology stack includes System.Xml.Linq (built-in .NET 8).

**XML Parsing Pattern:**

```csharp
using System.Xml.Linq;

public class TechDebtAnalyzer : ITechDebtAnalyzer
{
    private readonly ILogger<TechDebtAnalyzer> _logger;

    public TechDebtAnalyzer(ILogger<TechDebtAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<TechDebtMetric> AnalyzeAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        _logger.LogInformation("Analyzing tech debt for project {ProjectName}", project.ProjectName);

        try
        {
            // Load project XML
            var doc = await XDocument.LoadAsync(
                File.OpenRead(project.ProjectPath),
                LoadOptions.None,
                cancellationToken)
                .ConfigureAwait(false);

            // Parse TargetFramework (singular, SDK-style)
            var targetFramework = doc.Descendants("TargetFramework").FirstOrDefault()?.Value;

            // Try TargetFrameworks (plural, multi-targeting)
            if (string.IsNullOrEmpty(targetFramework))
            {
                var multiTarget = doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(multiTarget))
                {
                    // Use first target from semicolon-separated list
                    targetFramework = multiTarget.Split(';')[0];
                }
            }

            // Try TargetFrameworkVersion (legacy .NET Framework)
            if (string.IsNullOrEmpty(targetFramework))
            {
                var legacyVersion = doc.Descendants("TargetFrameworkVersion").FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(legacyVersion))
                {
                    // Convert v4.7.2 → net472
                    targetFramework = ConvertLegacyToTfm(legacyVersion);
                }
            }

            // If still not found, default to unknown
            if (string.IsNullOrEmpty(targetFramework))
            {
                _logger.LogWarning("No TargetFramework found for {ProjectName}, defaulting to neutral score 50",
                    project.ProjectName);
                return new TechDebtMetric(
                    project.ProjectName,
                    project.ProjectPath,
                    TargetFramework: "unknown",
                    NormalizedScore: 50);
            }

            // Calculate tech debt score
            var score = CalculateTechDebtScore(targetFramework);

            _logger.LogInformation("Detected framework {TargetFramework} for {ProjectName}",
                targetFramework, project.ProjectName);
            _logger.LogDebug("Project {ProjectName}: Framework={TargetFramework}, Score={NormalizedScore}",
                project.ProjectName, targetFramework, score);

            return new TechDebtMetric(
                project.ProjectName,
                project.ProjectPath,
                targetFramework,
                score);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fallback: XML parsing failed
            _logger.LogWarning("Could not parse TargetFramework for {ProjectName}, defaulting to neutral score 50: {Reason}",
                project.ProjectName, ex.Message);

            return new TechDebtMetric(
                project.ProjectName,
                project.ProjectPath,
                TargetFramework: "unknown",
                NormalizedScore: 50); // Neutral score
        }
    }
}
```

**Why Async XML Loading?**

- `XDocument.LoadAsync()` is async I/O operation (reads from file)
- Follows project convention: ALL I/O operations MUST be async
- Supports cancellation token for long-running analysis
- Consistent with Stories 4.1 & 4.2 async patterns

**XML Namespace Handling:**

Legacy .NET Framework projects use XML namespace:
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
```

XDocument handles this automatically with `Descendants()` - it searches ALL elements regardless of namespace.

### Technical Requirements

**New Namespace: MasDependencyMap.Core.ExtractionScoring (Established in Stories 4.1 & 4.2):**

Epic 4 uses the `ExtractionScoring` namespace created in Story 4.1.

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2
├── CycleAnalysis/               # Epic 3
├── Visualization/               # Epic 2, extended in Epic 3
└── ExtractionScoring/           # Epic 4
    ├── CouplingMetric.cs            # Story 4.1
    ├── ICouplingMetricCalculator.cs # Story 4.1
    ├── CouplingMetricCalculator.cs  # Story 4.1
    ├── ComplexityMetric.cs          # Story 4.2
    ├── IComplexityMetricCalculator.cs # Story 4.2
    ├── ComplexityMetricCalculator.cs # Story 4.2
    ├── CyclomaticComplexityWalker.cs # Story 4.2 (internal)
    ├── TechDebtMetric.cs            # Story 4.3 (THIS STORY)
    ├── ITechDebtAnalyzer.cs         # Story 4.3 (THIS STORY)
    └── TechDebtAnalyzer.cs          # Story 4.3 (THIS STORY)
```

**TechDebtMetric Model Pattern:**

Use C# 9+ `record` for immutable data (consistent with Stories 4.1 & 4.2):

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents technology version debt metrics for a single project.
/// Tech debt quantifies migration difficulty based on framework age and distance from modern .NET.
/// Older frameworks indicate harder extraction (more breaking changes, legacy patterns, missing modern features).
/// </summary>
/// <param name="ProjectName">Name of the project being analyzed.</param>
/// <param name="ProjectPath">Absolute path to the project file (.csproj or .vbproj).</param>
/// <param name="TargetFramework">Target framework moniker (TFM) of the project (e.g., "net8.0", "net472", "netcoreapp3.1").</param>
/// <param name="NormalizedScore">Tech debt score normalized to 0-100 scale. 0 = modern framework (easy to extract), 100 = very old framework (hard to extract).</param>
public sealed record TechDebtMetric(
    string ProjectName,
    string ProjectPath,
    string TargetFramework,
    double NormalizedScore);
```

**ITechDebtAnalyzer Interface Pattern:**

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Analyzes technology version debt for a project by parsing target framework from project files.
/// Tech debt measures migration difficulty based on framework age (older = higher debt).
/// Used to quantify extraction difficulty for migration planning.
/// </summary>
public interface ITechDebtAnalyzer
{
    /// <summary>
    /// Analyzes technology version debt for a single project.
    /// Parses TargetFramework from .csproj/.vbproj XML and calculates debt score.
    /// Falls back to neutral score (50) if parsing fails.
    /// </summary>
    /// <param name="project">The project to analyze. Must not be null. ProjectPath must point to valid .csproj or .vbproj file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// Tech debt metrics including target framework and normalized 0-100 score.
    /// If XML parsing fails, returns metric with TargetFramework="unknown", NormalizedScore=50 (neutral).
    /// </returns>
    /// <exception cref="ArgumentNullException">When project is null.</exception>
    /// <exception cref="OperationCanceledException">When cancellation is requested.</exception>
    Task<TechDebtMetric> AnalyzeAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default);
}
```

**Why Single Project Analysis (Like Story 4.2)?**

- Story 4.1 (CouplingMetricCalculator): Analyzed ENTIRE graph at once → `Task<IReadOnlyList<CouplingMetric>>`
- Story 4.2 (ComplexityMetricCalculator): Analyzes ONE project at a time → `Task<ComplexityMetric>`
- Story 4.3 (TechDebtAnalyzer): Analyzes ONE project at a time → `Task<TechDebtMetric>`

**Reasoning:**
1. **Tech debt is project-specific:** Each project has own TargetFramework, independent of others
2. **No cross-project dependencies:** Unlike coupling (relative), tech debt is absolute per project
3. **Simple XML parsing:** Fast operation, no need for batch processing
4. **Story 4.5 orchestration:** ExtractionScoreCalculator will call this for each project in a loop

**Async Pattern with File I/O:**

```csharp
public async Task<TechDebtMetric> AnalyzeAsync(
    ProjectNode project,
    CancellationToken cancellationToken = default)
{
    // Async I/O operation: Load XML from file
    var doc = await XDocument.LoadAsync(
        File.OpenRead(project.ProjectPath),
        LoadOptions.None,
        cancellationToken)
        .ConfigureAwait(false); // ConfigureAwait(false) in library code

    // ... parsing logic ...
}
```

**ConfigureAwait(false) Usage:**

Per project context (lines 296-299):
> 🚨 Async All The Way:
> - ALL I/O operations MUST be async (file, Roslyn, process execution)
> - Use ConfigureAwait(false) in library code (Core layer)

### Architecture Compliance

**Dependency Injection Registration:**

```csharp
// In Program.cs DI configuration
services.AddSingleton<ITechDebtAnalyzer, TechDebtAnalyzer>();
```

**Lifetime:**
- Singleton: TechDebtAnalyzer is stateless (only reads project files, no mutable state)
- Consistent with Stories 4.1 & 4.2 (all Epic 4 calculators are singletons)

**Integration with Existing Components:**

Story 4.3 CONSUMES from Epic 2:
- ProjectNode (vertex type, contains ProjectPath to .csproj file)

Story 4.3 PRODUCES for Epic 4:
- TechDebtMetric (model)
- ITechDebtAnalyzer (service)
- Will be consumed by Story 4.5 (ExtractionScoreCalculator)

**File Naming and Structure:**

```
src/MasDependencyMap.Core/ExtractionScoring/
├── TechDebtMetric.cs                      # One class per file, name matches class name
├── ITechDebtAnalyzer.cs                   # I-prefix for interfaces
└── TechDebtAnalyzer.cs                    # Descriptive implementation name
```

**Accessibility:**
- TechDebtMetric: `public sealed record` (part of public API)
- ITechDebtAnalyzer: `public interface` (part of public API)
- TechDebtAnalyzer: `public class` (part of public API)

### Library/Framework Requirements

**Existing .NET Libraries (Already Available):**

All dependencies built-in to .NET 8:
- ✅ System.Xml.Linq (XDocument) - Built-in .NET 8
- ✅ System.IO (File operations) - Built-in .NET 8
- ✅ Microsoft.Extensions.Logging.Abstractions - Installed in Epic 1
- ✅ System.Linq (LINQ methods) - Built-in .NET 8
- ✅ System.Threading (Task, CancellationToken) - Built-in .NET 8

**No New NuGet Packages Required for Story 4.3** ✅

**XDocument API Usage:**

```csharp
using System.Xml.Linq;

// Load XML asynchronously
var doc = await XDocument.LoadAsync(
    File.OpenRead(filePath),
    LoadOptions.None,
    cancellationToken);

// Query elements (ignores XML namespaces automatically)
var targetFramework = doc.Descendants("TargetFramework").FirstOrDefault()?.Value;
var multiTargets = doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value;
var legacyVersion = doc.Descendants("TargetFrameworkVersion").FirstOrDefault()?.Value;
```

**Key XDocument Methods:**
- `XDocument.LoadAsync()`: Async file loading
- `Descendants(string name)`: Find all descendant elements with given name (namespace-agnostic)
- `FirstOrDefault()`: Get first element or null
- `.Value`: Get element inner text content

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/ExtractionScoring/
├── TechDebtMetric.cs                             # NEW
├── ITechDebtAnalyzer.cs                          # NEW
└── TechDebtAnalyzer.cs                           # NEW

tests/MasDependencyMap.Core.Tests/ExtractionScoring/
└── TechDebtAnalyzerTests.cs                      # NEW
```

**Files to Modify:**

```
src/MasDependencyMap.CLI/Program.cs               # MODIFY: Add DI registration
_bmad-output/implementation-artifacts/sprint-status.yaml  # MODIFY: Update story status
```

**No Integration with CLI Commands Yet:**

Story 4.3 creates the analyzer but doesn't integrate it into CLI commands. That happens in Story 4.5 when all metrics are combined for extraction scoring.

For now:
- Create the service and register it in DI
- Full CLI integration happens in Epic 4 later stories
- Tests will validate functionality

### Testing Requirements

**Test Class: TechDebtAnalyzerTests.cs**

```csharp
namespace MasDependencyMap.Core.Tests.ExtractionScoring;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.ExtractionScoring;
using MasDependencyMap.Core.DependencyAnalysis;

public class TechDebtAnalyzerTests
{
    private readonly ILogger<TechDebtAnalyzer> _logger;
    private readonly TechDebtAnalyzer _analyzer;

    public TechDebtAnalyzerTests()
    {
        _logger = NullLogger<TechDebtAnalyzer>.Instance;
        _analyzer = new TechDebtAnalyzer(_logger);
    }

    [Fact]
    public async Task AnalyzeAsync_Net35Project_Returns100Score()
    {
        // Arrange: Project targeting .NET Framework 3.5 (highest debt)
        var project = CreateTestProjectWithTfm("net35");

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.TargetFramework.Should().Be("net3.5");
        metric.NormalizedScore.Should().Be(100);
    }

    [Fact]
    public async Task AnalyzeAsync_Net48Project_Returns40Score()
    {
        // Arrange: Project targeting .NET Framework 4.8 (legacy but recent)
        var project = CreateTestProjectWithTfm("net48");

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.TargetFramework.Should().Be("net4.8");
        metric.NormalizedScore.Should().Be(40);
    }

    [Fact]
    public async Task AnalyzeAsync_NetCore31Project_Returns30Score()
    {
        // Arrange: Project targeting .NET Core 3.1 (old modern)
        var project = CreateTestProjectWithTfm("netcoreapp3.1");

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.TargetFramework.Should().Be("netcoreapp3.1");
        metric.NormalizedScore.Should().Be(30);
    }

    [Fact]
    public async Task AnalyzeAsync_Net60Project_Returns10Score()
    {
        // Arrange: Project targeting .NET 6 (recent modern)
        var project = CreateTestProjectWithTfm("net6.0");

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.TargetFramework.Should().Be("net6.0");
        metric.NormalizedScore.Should().Be(10);
    }

    [Fact]
    public async Task AnalyzeAsync_Net80Project_Returns0Score()
    {
        // Arrange: Project targeting .NET 8 (current, no debt)
        var project = CreateTestProjectWithTfm("net8.0");

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.TargetFramework.Should().Be("net8.0");
        metric.NormalizedScore.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_LegacyFrameworkFormat_ParsesCorrectly()
    {
        // Arrange: Legacy format <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
        var project = CreateLegacyFormatProject("v4.7.2");

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.TargetFramework.Should().Be("net472");
        metric.NormalizedScore.Should().Be(40);
    }

    [Fact]
    public async Task AnalyzeAsync_MultiTargeting_UsesFirstTarget()
    {
        // Arrange: Multi-targeting <TargetFrameworks>netstandard2.0;net6.0</TargetFrameworks>
        var project = CreateMultiTargetProject("netstandard2.0;net6.0");

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.TargetFramework.Should().Be("netstandard2.0");
        // netstandard2.0 should have moderate debt score (compatible with both old and new)
    }

    [Fact]
    public async Task AnalyzeAsync_InvalidProjectFile_ReturnsFallbackScore50()
    {
        // Arrange: Invalid project path
        var project = new ProjectNode("InvalidProject", "C:\\NonExistent\\Project.csproj");

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.TargetFramework.Should().Be("unknown");
        metric.NormalizedScore.Should().Be(50); // Neutral fallback score
    }

    [Fact]
    public async Task AnalyzeAsync_NullProject_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _analyzer.AnalyzeAsync(null!));
    }

    [Fact]
    public async Task AnalyzeAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var project = CreateTestProjectWithTfm("net8.0");
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _analyzer.AnalyzeAsync(project, cts.Token));
    }

    // Helper methods to create test projects
    private ProjectNode CreateTestProjectWithTfm(string tfm)
    {
        // Create test .csproj file with specified TFM
        var testProjectPath = $"TestData/{tfm}-project/{tfm}-project.csproj";
        // Generate XML file with <TargetFramework>{tfm}</TargetFramework>
        return new ProjectNode($"{tfm}-project", testProjectPath);
    }

    private ProjectNode CreateLegacyFormatProject(string version)
    {
        // Create test .csproj with legacy <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
        var testProjectPath = "TestData/legacy-project/legacy.csproj";
        return new ProjectNode("legacy-project", testProjectPath);
    }

    private ProjectNode CreateMultiTargetProject(string frameworks)
    {
        // Create test .csproj with <TargetFrameworks>netstandard2.0;net6.0</TargetFrameworks>
        var testProjectPath = "TestData/multitarget-project/multitarget.csproj";
        return new ProjectNode("multitarget-project", testProjectPath);
    }
}
```

**Test Naming Convention:**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `AnalyzeAsync_Net35Project_Returns100Score()`
- ✅ `AnalyzeAsync_LegacyFrameworkFormat_ParsesCorrectly()`
- ✅ `AnalyzeAsync_MultiTargeting_UsesFirstTarget()`

**Test Coverage Checklist:**
- ✅ .NET Framework 3.5 (highest debt score 100)
- ✅ .NET Framework 4.8 (legacy but recent, score 40)
- ✅ .NET Core 3.1 (old modern, score 30)
- ✅ .NET 6 (recent LTS, score 10)
- ✅ .NET 8 (current LTS, score 0)
- ✅ Legacy format parsing (v4.7.2 → net472)
- ✅ Multi-targeting projects (use first target)
- ✅ Invalid project file fallback (score 50)
- ✅ Null project throws ArgumentNullException
- ✅ Cancellation support

**Test Data Strategy:**

**Option 1: Test Fixtures with Real Project Files** (Recommended)
- Create `tests/MasDependencyMap.Core.Tests/TestData/` directory
- Add sample .csproj files with different TFMs:
  - `net35-project/net35-project.csproj` (SDK-style with net3.5)
  - `net48-project/net48-project.csproj` (SDK-style with net4.8)
  - `legacy-project/legacy.csproj` (Legacy format with TargetFrameworkVersion)
  - `multitarget-project/multitarget.csproj` (Multi-targeting)

**Sample Test Project File (SDK-style):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Sample Test Project File (Legacy):**
```xml
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
</Project>
```

**Recommendation:** Use test fixtures with real .csproj files for integration testing.

### Previous Story Intelligence

**From Story 4.2 (Cyclomatic Complexity Calculator) - Patterns to Reuse:**

1. **Record Model Pattern:**
   ```csharp
   // Stories 4.1 & 4.2 used record models
   // Story 4.3 uses same pattern
   public sealed record TechDebtMetric(...)
   ```

2. **Analyzer Service Pattern:**
   ```csharp
   // Story 4.2: IComplexityMetricCalculator + ComplexityMetricCalculator
   // Story 4.3: ITechDebtAnalyzer + TechDebtAnalyzer (same pattern)
   // Interface + Implementation with ILogger injection
   ```

3. **DI Registration Pattern:**
   ```csharp
   // From Stories 4.1 & 4.2
   services.AddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>();
   services.AddSingleton<IComplexityMetricCalculator, ComplexityMetricCalculator>();
   // Story 4.3
   services.AddSingleton<ITechDebtAnalyzer, TechDebtAnalyzer>();
   ```

4. **Fallback Handling Pattern:**
   ```csharp
   // Story 4.2: Roslyn unavailable → score 50
   // Story 4.3: XML parsing failed → score 50
   // Same neutral fallback strategy
   ```

5. **Test Structure Pattern:**
   ```csharp
   // Constructor with NullLogger setup
   // Helper methods for test data creation
   // Arrange-Act-Assert structure
   // FluentAssertions for readable assertions
   ```

6. **Code Review Expectations (From Stories 4.1 & 4.2):**
   - Expect 5-10 issues found in code review
   - Common issues: test coverage gaps, edge cases, documentation improvements
   - Typical flow: Initial implementation commit → Code review fixes commit → Status update commit

**Key Differences from Story 4.2:**

| Aspect | Story 4.2 (Complexity) | Story 4.3 (Tech Debt) |
|--------|------------------------|----------------------|
| Primary API | Roslyn (semantic analysis) | **XDocument (XML parsing)** |
| Analysis Type | Code structure (methods) | **Project metadata (TFM)** |
| Normalization | Industry thresholds (0-7, 8-15, 16-25) | **Timeline-based (2008-2023)** |
| Fallback | Roslyn failure (missing SDK) | **XML parse failure (corrupt file)** |
| Memory | Heavy (MSBuildWorkspace) | **Lightweight (XDocument)** |
| Complexity | High (syntax walking) | **Low (single XML property)** |
| Return Type | `ComplexityMetric` | **`TechDebtMetric`** |

### Git Intelligence Summary

**Recent Commits Pattern:**

Last 5 commits show consistent code review process:
1. `5911da1` Code review fixes for Story 4-2: Implement cyclomatic complexity calculator with Roslyn
2. `cc24f3c` Code review fixes for Story 4-1: Implement coupling metric calculator
3. `78f33d1` Code review fixes for Story 3-7: Mark suggested break points in YELLOW on visualizations
4. `d8166d9` Story 3-7 complete: Mark suggested break points in YELLOW on visualizations
5. `fea9295` Update Story 3-6 status to done and document code review fixes

**Pattern:** Initial commit → Code review → Fixes commit → Status update commit

**Expected File Changes for Story 4.3:**

Based on Stories 4.1 & 4.2 pattern:
- New: `src/MasDependencyMap.Core/ExtractionScoring/TechDebtMetric.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/ITechDebtAnalyzer.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/TechDebtAnalyzer.cs`
- New: `tests/MasDependencyMap.Core.Tests/ExtractionScoring/TechDebtAnalyzerTests.cs`
- Modified: `src/MasDependencyMap.CLI/Program.cs` (DI registration)
- Modified: `_bmad-output/implementation-artifacts/sprint-status.yaml` (story status update)
- Modified: `_bmad-output/implementation-artifacts/4-3-implement-technology-version-debt-analyzer.md` (completion notes)

**Commit Message Pattern for Story Completion:**

```bash
git commit -m "Story 4-3 complete: Implement technology version debt analyzer

- Created TechDebtMetric record model with target framework and normalized score
- Created ITechDebtAnalyzer interface for DI abstraction
- Implemented TechDebtAnalyzer with XML parsing using XDocument
- Implemented TargetFramework parsing for SDK-style, legacy, and multi-targeting formats
- Implemented timeline-based scoring: .NET 3.5 (100 debt) to .NET 8 (0 debt)
- Implemented fallback handling: defaults to neutral score 50 when XML parsing fails
- Added structured logging with named placeholders for framework detection
- Registered service in DI container as singleton
- Created comprehensive unit tests with 10 test cases (all passing)
- Tests validate TFM parsing, scoring thresholds, fallback, edge cases
- New files: TechDebtMetric, ITechDebtAnalyzer, TechDebtAnalyzer, TechDebtAnalyzerTests
- All acceptance criteria satisfied
- Epic 4 Story 4.3 foundation for extraction difficulty scoring established

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 4.3 extends Epic 4 namespace created in Story 4.1, continuing Epic 4 progression:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
├── CycleAnalysis/               # Epic 3: Cycle detection
├── Visualization/               # Epic 2: DOT generation (extended in Epic 3)
└── ExtractionScoring/           # Epic 4: Extraction difficulty
    ├── CouplingMetric.cs            # Story 4.1
    ├── ICouplingMetricCalculator.cs # Story 4.1
    ├── CouplingMetricCalculator.cs  # Story 4.1
    ├── ComplexityMetric.cs          # Story 4.2
    ├── IComplexityMetricCalculator.cs # Story 4.2
    ├── ComplexityMetricCalculator.cs # Story 4.2
    ├── CyclomaticComplexityWalker.cs # Story 4.2 (internal)
    ├── TechDebtMetric.cs            # Story 4.3 (NEW)
    ├── ITechDebtAnalyzer.cs         # Story 4.3 (NEW)
    └── TechDebtAnalyzer.cs          # Story 4.3 (NEW)
```

**Consistency with Existing Patterns:**
- ✅ Feature-based namespace (NOT layer-based)
- ✅ Interface + Implementation pattern (I-prefix interfaces)
- ✅ File naming matches class naming exactly
- ✅ Test namespace mirrors Core structure
- ✅ Service pattern with ILogger injection
- ✅ Singleton DI registration for stateless services
- ✅ Record model for immutable data
- ✅ Async methods with Async suffix and ConfigureAwait(false)

**Cross-Namespace Dependencies:**
- ExtractionScoring → DependencyAnalysis (uses ProjectNode)
- This is expected and acceptable (Epic 4 builds on Epic 2 infrastructure)
- Same as Stories 4.1 & 4.2

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-4-extraction-difficulty-scoring-and-candidate-ranking.md, Story 4.3 (lines 42-58)]
- Story requirements: Parse TargetFramework from project XML, identify .NET Framework and .NET Core versions, calculate tech debt score, log framework detection

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 18-51)]
- Technology stack: .NET 8.0, C# 12, System.Xml.Linq (XDocument for XML parsing)
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces, async patterns, file-scoped namespaces, nullable reference types
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 249-260)]
- Version compatibility: Tool targets .NET 8.0 BUT analyzes projects from .NET Framework 3.5+ through .NET 8+ (20-year span)
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 296-299)]
- Async patterns: ConfigureAwait(false) in library code

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md (lines 22-39)]
- Configuration management: Microsoft.Extensions.Configuration with JSON support
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\implementation-patterns-consistency-rules.md (lines 9-19)]
- Namespace organization: Feature-based, not layer-based

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-2-implement-cyclomatic-complexity-calculator-with-roslyn.md (full file)]
- Record model pattern, analyzer interface/implementation pattern, DI registration, test structure, fallback handling
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-1-implement-coupling-metric-calculator.md (full file)]
- Epic 4 namespace establishment, calculator patterns, singleton registration

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Code review pattern: Initial commit → Code review fixes (5-10 issues) → Status update
- Recent pattern: Stories 4.1 & 4.2 both had code review fixes applied after initial implementation

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

None - implementation completed without issues requiring external debugging.

### Completion Notes List

1. ✅ Created TechDebtMetric record model with comprehensive XML documentation explaining timeline-based tech debt scoring
2. ✅ Created ITechDebtAnalyzer interface following Epic 4 patterns (consistent with Stories 4.1 & 4.2)
3. ✅ Implemented TechDebtAnalyzer with XDocument-based XML parsing for all three TFM formats:
   - SDK-style: `<TargetFramework>net8.0</TargetFramework>`
   - Legacy: `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>`
   - Multi-targeting: `<TargetFrameworks>netstandard2.0;net6.0</TargetFrameworks>`
4. ✅ Implemented timeline-based scoring algorithm: .NET 3.5 (100 debt) → .NET 8 (0 debt) with all intermediate versions
5. ✅ Fixed critical XML namespace handling issue: Used `.Where(e => e.Name.LocalName == "...")` to properly ignore XML namespaces in legacy project files
6. ✅ Implemented graceful fallback handling with neutral score 50 when XML parsing fails
7. ✅ Added comprehensive structured logging with named placeholders (Information, Debug, Warning levels)
8. ✅ Registered service in DI container as singleton (stateless service, consistent with Stories 4.1 & 4.2)
9. ✅ Created comprehensive test suite with 10 tests covering all TFM formats, edge cases, and error conditions
10. ✅ All 10 tests pass (100% pass rate), full test suite passes with 302/302 tests (no regressions)
11. ✅ Validated architecture compliance: feature-based namespace, async suffix, ConfigureAwait(false), XML docs, file-scoped namespaces

**Code Review Fixes Applied (2 High, 4 Medium issues fixed):**

12. ✅ **HIGH FIX:** Added validation to ConvertLegacyToTfm to handle invalid/empty input (prevents returning invalid "net" TFM)
13. ✅ **HIGH FIX:** Fixed potential FileStream resource leak by using `await using var stream` pattern for proper disposal
14. ✅ **MEDIUM FIX:** Added .csproj/.vbproj file extension validation with clear ArgumentException when invalid file type provided
15. ✅ **MEDIUM FIX:** Added netstandard TFM scoring to dictionary (netstandard1.x=70, netstandard2.0=50, netstandard2.1=35) for accurate tech debt assessment
16. ✅ **MEDIUM FIX:** Changed Dictionary to ImmutableDictionary for FrameworkScores lookup table (better thread safety and immutability intent)
17. ✅ **MEDIUM FIX:** Updated all tests to not pre-set TargetFramework property, matching real-world usage where analyzer discovers the framework

**Key Implementation Decisions:**
- Used `.Where(e => e.Name.LocalName == "...")` instead of `Descendants("...")` for XML element lookup to properly ignore XML namespaces in legacy .NET Framework project files. This was critical for parsing `<TargetFrameworkVersion>` in files with `xmlns="http://schemas.microsoft.com/developer/msbuild/2003"` namespace declarations.
- Used ImmutableDictionary for framework scoring lookup table to ensure thread safety and clearly communicate immutability intent.
- Added defensive validation for file extensions and legacy version format parsing to prevent subtle bugs with invalid inputs.

### File List

**New Files Created:**
- src/MasDependencyMap.Core/ExtractionScoring/TechDebtMetric.cs
- src/MasDependencyMap.Core/ExtractionScoring/ITechDebtAnalyzer.cs
- src/MasDependencyMap.Core/ExtractionScoring/TechDebtAnalyzer.cs
- tests/MasDependencyMap.Core.Tests/ExtractionScoring/TechDebtAnalyzerTests.cs
- tests/MasDependencyMap.Core.Tests/TestData/TechDebt/net35-project.csproj
- tests/MasDependencyMap.Core.Tests/TestData/TechDebt/net48-project.csproj
- tests/MasDependencyMap.Core.Tests/TestData/TechDebt/netcoreapp31-project.csproj
- tests/MasDependencyMap.Core.Tests/TestData/TechDebt/net60-project.csproj
- tests/MasDependencyMap.Core.Tests/TestData/TechDebt/net80-project.csproj
- tests/MasDependencyMap.Core.Tests/TestData/TechDebt/legacy-project.csproj
- tests/MasDependencyMap.Core.Tests/TestData/TechDebt/multitarget-project.csproj

**Modified Files:**
- src/MasDependencyMap.CLI/Program.cs (added DI registration for ITechDebtAnalyzer)
- tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj (added TestData file copying configuration)
- _bmad-output/implementation-artifacts/sprint-status.yaml (updated story status: in-progress → review)
- _bmad-output/implementation-artifacts/4-3-implement-technology-version-debt-analyzer.md (marked all tasks complete, added completion notes)
