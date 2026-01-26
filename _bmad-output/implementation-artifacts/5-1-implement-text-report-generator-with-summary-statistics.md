# Story 5.1: Implement Text Report Generator with Summary Statistics

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want comprehensive text reports with summary statistics,
So that I can review analysis results in a readable format.

## Acceptance Criteria

**Given** Analysis is complete with dependency graph, cycles, and scores
**When** TextReportGenerator.GenerateAsync() is called
**Then** A text file is created at {SolutionName}-analysis-report.txt in the output directory
**And** Report header includes: solution name, analysis date, total projects count
**And** Dependency Overview section shows: total references, framework references (filtered), custom references, cross-solution references
**And** Report sections are clearly formatted with headers and separators
**And** File is generated within 10 seconds regardless of solution size
**And** Text is formatted for readability (proper spacing, alignment, section breaks)

## Tasks / Subtasks

- [x] Create ITextReportGenerator interface in MasDependencyMap.Core.Reporting namespace (AC: Architecture compliance)
  - [x] Define GenerateAsync method signature with parameters: DependencyGraph, outputDirectory, solutionName, CancellationToken
  - [x] Return Task<string> (absolute path to generated report file)
  - [x] Add XML documentation explaining purpose, parameters, return value, and exceptions
  - [x] Include optional parameters for future extensibility: cycles, extractionScores
  - [x] Follow project-context.md interface naming rules (I-prefix)

- [x] Create TextReportGenerator implementation class (AC: Generate report file)
  - [x] Implement ITextReportGenerator interface
  - [x] Inject ILogger<TextReportGenerator> via constructor for structured logging
  - [x] Use file-scoped namespace declaration (C# 10+)
  - [x] Implement GenerateAsync method with proper error handling
  - [x] Use StringBuilder for efficient text building
  - [x] Write report to file using File.WriteAllTextAsync with UTF-8 encoding

- [x] Implement report header generation (AC: Report header includes required fields)
  - [x] Generate solution name from parameter
  - [x] Generate analysis date using DateTime.UtcNow formatted as "yyyy-MM-dd HH:mm:ss UTC"
  - [x] Calculate total projects count from graph.VertexCount
  - [x] Format header with clear separators (e.g., "=" characters, 80 columns wide)
  - [x] Include tool name and version in header for context

- [x] Implement Dependency Overview section (AC: Shows reference statistics)
  - [x] Calculate total references from graph.EdgeCount
  - [x] Identify framework references (using framework naming patterns: Microsoft.*, System.*, etc.)
  - [x] Calculate custom references (total - framework references)
  - [x] Calculate cross-solution references (edges where source.SolutionName != target.SolutionName)
  - [x] Format statistics with clear labels and alignment
  - [x] Include percentage breakdowns for clarity (e.g., "Framework: 450 (75%)")

- [x] Implement section formatting utilities (AC: Sections clearly formatted)
  - [x] Use standardized separator characters (e.g., "=") and widths (80 columns)
  - [x] Add proper spacing between sections (blank lines)
  - [x] Ensure alignment of numeric values for readability
  - [x] Use consistent formatting throughout report

- [x] Implement performance optimization (AC: Generated within 10 seconds)
  - [x] Use StringBuilder for all text concatenation (pre-allocated capacity: 4096)
  - [x] Use efficient LINQ queries with single-pass enumeration
  - [x] Log performance metrics: report generation time

- [x] Implement error handling and logging (AC: Robust error handling)
  - [x] Validate input parameters (ArgumentNullException for null graph, ArgumentException for empty paths)
  - [x] Validate directory exists (DirectoryNotFoundException)
  - [x] Log structured messages with named placeholders (e.g., {SolutionName}, {FilePath}, {ElapsedMs})
  - [x] Use LogInformation for successful report generation with file path
  - [x] Use ConfigureAwait(false) for async operations

- [x] Register TextReportGenerator in DI container (AC: Available via DI)
  - [x] Add registration in CLI Program.cs
  - [x] Use AddSingleton<ITextReportGenerator, TextReportGenerator>()
  - [x] Verify ILogger<TextReportGenerator> is automatically resolved
  - [x] Added to Epic 5 section in Program.cs

- [x] Create comprehensive unit tests (AC: Test coverage)
  - [x] Create tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs
  - [x] Test: GenerateAsync_ValidGraph_CreatesReportFile (verify file creation)
  - [x] Test: GenerateAsync_ValidGraph_ContainsHeader (verify header content)
  - [x] Test: GenerateAsync_GraphWithFrameworkRefs_ShowsCorrectStatistics (verify statistics)
  - [x] Test: GenerateAsync_NullGraph_ThrowsArgumentNullException
  - [x] Test: GenerateAsync_EmptyOutputDirectory_ThrowsArgumentException
  - [x] Test: GenerateAsync_EmptySolutionName_ThrowsArgumentException
  - [x] Test: GenerateAsync_NonExistentDirectory_ThrowsDirectoryNotFoundException
  - [x] Test: GenerateAsync_MultiSolutionGraph_ShowsCrossSolutionReferences
  - [x] Test: GenerateAsync_EmptyGraph_HandlesGracefully
  - [x] Test: GenerateAsync_LargeGraph_CompletesWithin10Seconds (400 projects, 5000 edges)
  - [x] Test: GenerateAsync_ReportHasCorrectStructure
  - [x] Test: GenerateAsync_ReturnsAbsolutePath
  - [x] Use xUnit framework with [Fact] attributes
  - [x] Use FluentAssertions for readable assertions
  - [x] Create test helper methods: CreateTestGraph(), CreateTempDirectory(), etc.
  - [x] Clean up temp files in test disposal using IDisposable

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Feature-based namespace: MasDependencyMap.Core.Reporting ✓
  - [x] File-scoped namespace declarations ✓
  - [x] Async suffix on async methods (GenerateAsync) ✓
  - [x] ILogger injection via constructor ✓
  - [x] ConfigureAwait(false) in library code ✓
  - [x] XML documentation on all public APIs ✓
  - [x] Structured logging with named placeholders ✓
  - [x] Test organization mirrors namespace structure ✓
  - [x] Test method naming: {MethodName}_{Scenario}_{ExpectedResult} ✓

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 5.1 Text Report Generator Foundation:**

This story creates the **FIRST component in Epic 5 (Comprehensive Reporting)**, establishing the foundational text report generator that will be extended by Stories 5.2-5.4 with cycle detection, extraction scoring, and recommendation sections.

**Epic 5 Vision (Reporting Stack):**
- **Story 5.1: Text Report Generator with Summary Statistics (THIS STORY - FOUNDATION)**
- Story 5.2: Add Cycle Detection section to text reports
- Story 5.3: Add Extraction Difficulty Scoring section to text reports
- Story 5.4: Add Cycle-Breaking Recommendations to text reports
- Story 5.5-5.7: CSV export capabilities
- Story 5.8: Spectre.Console table formatting enhancements

**Story 5.1 Unique Characteristics:**

1. **New Reporting Namespace:**
   - Creates first component in `MasDependencyMap.Core.Reporting` namespace
   - Establishes reporting patterns that Stories 5.2-5.4 will follow
   - **Pattern-setting story:** Future reporting components will reference this implementation

2. **Foundation for Multi-Section Reports:**
   - Story 5.1: Header + Dependency Overview (foundation)
   - Story 5.2: Adds Cycle Detection section (extends)
   - Story 5.3: Adds Extraction Difficulty section (extends)
   - Story 5.4: Adds Recommendations section (extends)
   - **Extensible design:** Use section builder pattern for easy extension

3. **Performance-Critical Path:**
   - Acceptance Criteria: "within 10 seconds regardless of solution size"
   - Solution graphs can have 50-400+ projects (from NFR1-NFR2)
   - **Optimization strategy:** StringBuilder, single-pass graph traversal, cached statistics
   - **No Spectre.Console in Core layer:** Use plain text generation (Spectre.Console is CLI concern)

4. **Plain Text Output (Not CLI):**
   - Story 5.1: Generate text file content (Core layer - no Spectre.Console)
   - Story 5.8: Format with Spectre.Console tables (CLI layer integration)
   - **Separation of concerns:** Core generates text, CLI handles formatting/display
   - **File format:** Plain UTF-8 text with manual formatting (spacing, separators)

5. **Integration with Existing Epic 2-4 Components:**
   - Consumes DependencyGraph from Epic 2 (Story 2.5)
   - Will consume CycleInfo from Epic 3 (Story 5.2)
   - Will consume ExtractionScore from Epic 4 (Story 5.3)
   - **This story:** Only depends on DependencyGraph (simplest integration point)

🚨 **CRITICAL - New Reporting Namespace Structure:**

**Namespace Creation (First in Epic 5):**

Story 5.1 creates the `MasDependencyMap.Core.Reporting` namespace, following project-context.md feature-based organization:

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # NEW - Interface (Story 5.1)
└── TextReportGenerator.cs           # NEW - Implementation (Story 5.1)

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs      # NEW - Tests (Story 5.1)
```

**Future Epic 5 Structure (Stories 5.2-5.8):**

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # Story 5.1
├── TextReportGenerator.cs           # Story 5.1 (extended by 5.2-5.4)
├── ICsvExporter.cs                  # Story 5.5
├── CsvExporter.cs                   # Story 5.5-5.7
├── ExtractionScoreRecord.cs         # Story 5.5
├── CycleAnalysisRecord.cs           # Story 5.6
└── DependencyMatrixRecord.cs        # Story 5.7
```

**Namespace Rules (from project-context.md):**
- ✅ Feature-based: `MasDependencyMap.Core.Reporting` (NOT layer-based like `MasDependencyMap.Core.Services`)
- ✅ File-scoped namespace: `namespace MasDependencyMap.Core.Reporting;`
- ✅ Test namespace mirrors: `namespace MasDependencyMap.Core.Tests.Reporting;`

🚨 **CRITICAL - ITextReportGenerator Interface Design:**

**Interface Signature:**

```csharp
namespace MasDependencyMap.Core.Reporting;

using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Generates comprehensive text reports from solution dependency analysis results.
/// Reports include dependency statistics, cycle detection results, extraction scores, and recommendations.
/// </summary>
public interface ITextReportGenerator
{
    /// <summary>
    /// Generates a comprehensive text report from dependency analysis results.
    /// </summary>
    /// <param name="graph">The dependency graph containing all projects and references. Must not be null.</param>
    /// <param name="outputDirectory">Directory where the report file will be written. Must exist.</param>
    /// <param name="solutionName">Name of the solution being analyzed. Used in filename and header.</param>
    /// <param name="cycles">Optional cycle detection results. If provided, includes Cycle Detection section (Story 5.2).</param>
    /// <param name="extractionScores">Optional extraction difficulty scores. If provided, includes Extraction Difficulty section (Story 5.3).</param>
    /// <param name="recommendations">Optional cycle-breaking recommendations. If provided, includes Recommendations section (Story 5.4).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Absolute path to the generated report file (e.g., "C:\output\MySolution-analysis-report.txt").</returns>
    /// <exception cref="ArgumentNullException">When graph, outputDirectory, or solutionName is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">When outputDirectory does not exist.</exception>
    /// <exception cref="IOException">When file write operation fails.</exception>
    Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        IReadOnlyList<CycleInfo>? cycles = null,
        IReadOnlyList<ExtractionScore>? extractionScores = null,
        IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
        CancellationToken cancellationToken = default);
}
```

**Design Rationale:**

1. **Optional Parameters for Future Stories:**
   - Story 5.1: Uses only `graph`, `outputDirectory`, `solutionName` (required parameters)
   - Story 5.2: Will pass `cycles` parameter to include Cycle Detection section
   - Story 5.3: Will pass `extractionScores` parameter to include Extraction Difficulty section
   - Story 5.4: Will pass `recommendations` parameter to include Recommendations section
   - **Forward compatibility:** Interface supports future extensions without breaking changes

2. **Parameter Order Rationale:**
   - Required parameters first: graph, outputDirectory, solutionName
   - Optional analysis results next: cycles, extractionScores, recommendations
   - CancellationToken last (standard .NET async pattern)

3. **Return Type:**
   - Returns `Task<string>` (absolute path to generated file)
   - Enables caller to locate file for further processing (CLI display, archiving, etc.)
   - Alternative considered: `Task` (void) - rejected because caller needs path for confirmation

🚨 **CRITICAL - TextReportGenerator Implementation Structure:**

**Class Implementation Pattern:**

```csharp
namespace MasDependencyMap.Core.Reporting;

using System.Text;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using Microsoft.Extensions.Logging;

/// <summary>
/// Generates comprehensive text reports from solution dependency analysis results.
/// Produces stakeholder-ready reports with summary statistics, cycle detection, extraction scores, and recommendations.
/// </summary>
public sealed class TextReportGenerator : ITextReportGenerator
{
    private readonly ILogger<TextReportGenerator> _logger;
    private const int ReportWidth = 80;  // Standard terminal width for formatting

    /// <summary>
    /// Initializes a new instance of the TextReportGenerator class.
    /// </summary>
    /// <param name="logger">Logger for structured logging. Must not be null.</param>
    /// <exception cref="ArgumentNullException">When logger is null.</exception>
    public TextReportGenerator(ILogger<TextReportGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        IReadOnlyList<CycleInfo>? cycles = null,
        IReadOnlyList<ExtractionScore>? extractionScores = null,
        IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
        CancellationToken cancellationToken = default)
    {
        // Validation
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionName);

        if (!Directory.Exists(outputDirectory))
        {
            throw new DirectoryNotFoundException($"Output directory does not exist: {outputDirectory}");
        }

        _logger.LogInformation("Generating text report for solution {SolutionName}", solutionName);

        var startTime = DateTime.UtcNow;

        // Build report content
        var report = new StringBuilder(capacity: 4096);  // Pre-allocate for performance

        AppendHeader(report, solutionName, graph);
        AppendDependencyOverview(report, graph);

        // Future stories will add more sections here
        // Story 5.2: AppendCycleDetection(report, cycles);
        // Story 5.3: AppendExtractionScores(report, extractionScores);
        // Story 5.4: AppendRecommendations(report, recommendations);

        // Write to file
        var fileName = $"{solutionName}-analysis-report.txt";
        var filePath = Path.Combine(outputDirectory, fileName);

        await File.WriteAllTextAsync(filePath, report.ToString(), Encoding.UTF8, cancellationToken)
            .ConfigureAwait(false);

        var elapsed = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Generated text report at {FilePath} in {ElapsedMs}ms",
            filePath,
            elapsed.TotalMilliseconds);

        return filePath;
    }

    // Private helper methods for section generation
    // ...
}
```

**Implementation Notes:**

1. **StringBuilder Pre-Allocation:**
   - Initial capacity 4096 bytes (4KB) for typical report
   - Avoids buffer resizing during concatenation
   - Performance optimization for large graphs

2. **UTF-8 Encoding:**
   - Standard for cross-platform text files
   - No BOM needed (plain text, not CSV)
   - Compatible with all modern text editors

3. **Async File I/O:**
   - Use `File.WriteAllTextAsync` (not `File.WriteAllText`)
   - Pass `CancellationToken` for cooperative cancellation
   - Use `.ConfigureAwait(false)` (library code pattern)

4. **Performance Logging:**
   - Log start time before processing
   - Log elapsed time after file write
   - Helps identify performance regressions in testing

🚨 **CRITICAL - Report Header Format:**

**Header Structure (Story 5.1):**

```
================================================================================
MasDependencyMap Analysis Report
================================================================================

Solution: MyLegacySolution
Analysis Date: 2026-01-26 14:32:15 UTC
Total Projects: 73

================================================================================
```

**Implementation Pattern:**

```csharp
private void AppendHeader(StringBuilder report, string solutionName, DependencyGraph graph)
{
    var separator = new string('=', ReportWidth);

    report.AppendLine(separator);
    report.AppendLine("MasDependencyMap Analysis Report");
    report.AppendLine(separator);
    report.AppendLine();
    report.AppendLine($"Solution: {solutionName}");
    report.AppendLine($"Analysis Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    report.AppendLine($"Total Projects: {graph.VertexCount}");
    report.AppendLine();
    report.AppendLine(separator);
    report.AppendLine();
}
```

**Header Design Decisions:**

1. **Tool Name in Header:**
   - "MasDependencyMap Analysis Report" identifies tool for stakeholders
   - Useful when reports are shared via email or archived
   - Alternative considered: Version number - deferred to post-MVP

2. **UTC Timestamp:**
   - Use UTC for consistent timestamps across time zones
   - Format: `yyyy-MM-dd HH:mm:ss UTC` (ISO 8601-like, readable)
   - Alternative considered: Local time - rejected (ambiguous in multi-region teams)

3. **Total Projects Count:**
   - Simple metric: `graph.VertexCount` (includes all projects in graph)
   - Provides immediate context for report scale
   - Helps reader understand analysis scope

🚨 **CRITICAL - Dependency Overview Section Format:**

**Dependency Overview Structure (Story 5.1):**

```
DEPENDENCY OVERVIEW
================================================================================

Total References: 1,234
  - Framework References: 890 (72%)
  - Custom References: 344 (28%)

Cross-Solution References: 45
  (References between different solution files in multi-solution analysis)

================================================================================
```

**Implementation Pattern:**

```csharp
private void AppendDependencyOverview(StringBuilder report, DependencyGraph graph)
{
    report.AppendLine("DEPENDENCY OVERVIEW");
    report.AppendLine(new string('=', ReportWidth));
    report.AppendLine();

    // Calculate statistics (single-pass for performance)
    var totalReferences = graph.EdgeCount;
    var frameworkReferences = graph.Edges.Count(e => IsFrameworkReference(e));
    var customReferences = totalReferences - frameworkReferences;
    var crossSolutionReferences = CountCrossSolutionReferences(graph);

    // Format with percentages
    var frameworkPercentage = totalReferences > 0 ? (frameworkReferences * 100.0 / totalReferences) : 0;
    var customPercentage = totalReferences > 0 ? (customReferences * 100.0 / totalReferences) : 0;

    report.AppendLine($"Total References: {totalReferences:N0}");
    report.AppendLine($"  - Framework References: {frameworkReferences:N0} ({frameworkPercentage:F0}%)");
    report.AppendLine($"  - Custom References: {customReferences:N0} ({customPercentage:F0}%)");
    report.AppendLine();

    if (crossSolutionReferences > 0)
    {
        report.AppendLine($"Cross-Solution References: {crossSolutionReferences:N0}");
        report.AppendLine("  (References between different solution files in multi-solution analysis)");
        report.AppendLine();
    }

    report.AppendLine(new string('=', ReportWidth));
    report.AppendLine();
}

private bool IsFrameworkReference(DependencyEdge edge)
{
    // Check if target vertex is marked as framework reference
    // Framework projects have IsFrameworkReference = true (set by FrameworkFilter in Epic 2)
    return graph.ContainsVertex(edge.Target) && edge.Target.IsFrameworkReference;
}

private int CountCrossSolutionReferences(DependencyGraph graph)
{
    // Count edges where source and target have different SolutionName properties
    return graph.Edges.Count(e =>
        !string.IsNullOrEmpty(e.Source.SolutionName) &&
        !string.IsNullOrEmpty(e.Target.SolutionName) &&
        e.Source.SolutionName != e.Target.SolutionName);
}
```

**Statistics Calculation Notes:**

1. **Framework References Identification:**
   - From Epic 2 (Story 2.6): FrameworkFilter marks framework projects with `IsFrameworkReference = true`
   - Edge targets pointing to framework vertices are framework references
   - **Reuse existing filtering:** Don't re-implement framework detection logic

2. **Cross-Solution References:**
   - Only meaningful in multi-solution analysis (Epic 2, Story 2.10)
   - Check if source and target have different `SolutionName` properties
   - **Defensive check:** Ensure SolutionName is not null or empty

3. **Number Formatting:**
   - Use `{value:N0}` format for thousands separator (e.g., "1,234")
   - Improves readability for large dependency counts
   - **Consistent formatting:** Use N0 for all integers in report

4. **Percentage Formatting:**
   - Use `{percentage:F0}%` format for integer percentages (e.g., "72%")
   - Avoid decimal places for simplicity (stakeholder-friendly)
   - **Handle zero division:** Check totalReferences > 0 before calculating percentages

### Technical Requirements

**New Namespace: MasDependencyMap.Core.Reporting**

Story 5.1 creates the first component in Epic 5's Reporting namespace:

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # NEW (Story 5.1)
└── TextReportGenerator.cs           # NEW (Story 5.1)

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs      # NEW (Story 5.1)
```

**Dependencies:**

Story 5.1 depends on existing Epic 2 components:

```csharp
using MasDependencyMap.Core.DependencyAnalysis;  // For DependencyGraph, ProjectNode, DependencyEdge
using MasDependencyMap.Core.CycleAnalysis;       // For future CycleInfo (Story 5.2)
using MasDependencyMap.Core.ExtractionScoring;   // For future ExtractionScore (Story 5.3)
using Microsoft.Extensions.Logging;              // For ILogger<T>
using System.Text;                               // For StringBuilder
```

**No New NuGet Packages Required:**

All dependencies already installed from previous epics:
- ✅ Microsoft.Extensions.Logging.Console - Structured logging (Epic 1)
- ✅ System.Text.StringBuilder - Built-in .NET (no package)
- ✅ System.IO - Built-in .NET (no package)

**DI Registration:**

TextReportGenerator must be registered in CLI `Program.cs`:

```csharp
// Add to CLI Program.cs DI setup (location from Epic 1)
services.AddSingleton<ITextReportGenerator, TextReportGenerator>();
```

**Lifetime: Singleton**
- TextReportGenerator is stateless (no instance state between calls)
- ILogger<T> is injected (also singleton or scoped)
- **Reasoning:** No per-request state, safe for concurrent calls

### Architecture Compliance

**Feature-Based Namespace (New):**

Story 5.1 establishes `MasDependencyMap.Core.Reporting` namespace following project-context.md rules:

```csharp
namespace MasDependencyMap.Core.Reporting;  // File-scoped namespace (C# 10+)

using MasDependencyMap.Core.DependencyAnalysis;
using Microsoft.Extensions.Logging;
```

**Async Pattern (Required):**

```csharp
// Correct (follows project-context.md):
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    CancellationToken cancellationToken = default)
{
    // Implementation
    await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken)
        .ConfigureAwait(false);
}
```

**Structured Logging (Required):**

```csharp
// Correct (named placeholders, not string interpolation):
_logger.LogInformation("Generating text report for solution {SolutionName}", solutionName);
_logger.LogInformation(
    "Generated text report at {FilePath} in {ElapsedMs}ms",
    filePath,
    elapsed.TotalMilliseconds);

// WRONG (string interpolation):
_logger.LogInformation($"Generating text report for solution {solutionName}");
```

**XML Documentation (Required):**

```csharp
/// <summary>
/// Generates comprehensive text reports from solution dependency analysis results.
/// Reports include dependency statistics, cycle detection results, extraction scores, and recommendations.
/// </summary>
public sealed class TextReportGenerator : ITextReportGenerator
{
    /// <inheritdoc />
    public async Task<string> GenerateAsync(...)
    {
        // Implementation
    }
}
```

### Library/Framework Requirements

**Existing Libraries (No New Packages):**

All dependencies already installed from previous epics:

**From Epic 1 (Foundation):**
- ✅ Microsoft.Extensions.DependencyInjection - DI container
- ✅ Microsoft.Extensions.Logging.Console - Structured logging
- ✅ Microsoft.Extensions.Configuration.Json - Configuration (not used in Story 5.1, but available)

**From Epic 2 (Dependency Analysis):**
- ✅ QuikGraph v2.5.0 - DependencyGraph data structure
- ✅ System.CommandLine v2.0.2 - CLI argument parsing (CLI layer, not used in Core)

**Built-in .NET Libraries:**
- ✅ System.Text.StringBuilder - Efficient string building
- ✅ System.IO - File operations (File.WriteAllTextAsync)
- ✅ System.Linq - LINQ queries for graph statistics

**No New NuGet Packages Required for Story 5.1** ✅

**Future Epic 5 Dependencies:**
- Story 5.5-5.7: Will add CsvHelper for CSV export
- Story 5.8: Will use Spectre.Console for table formatting (CLI layer)

### File Structure Requirements

**Files to Create:**

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs                              # NEW: Interface (Story 5.1)
└── TextReportGenerator.cs                               # NEW: Implementation (Story 5.1)

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs                          # NEW: Unit tests (Story 5.1)
```

**Files to Modify:**

```
src/MasDependencyMap.Cli/Program.cs                      # MODIFY: Register ITextReportGenerator in DI
_bmad-output/implementation-artifacts/sprint-status.yaml # MODIFY: Update story 5-1 and epic-5 status
_bmad-output/implementation-artifacts/5-1-implement-text-report-generator-with-summary-statistics.md  # CREATE: This story file
```

**No Existing Files Modified in Core:**

Story 5.1 is purely additive - no changes to existing Epic 1-4 components.

### Testing Requirements

**Test Class: TextReportGeneratorTests.cs (New)**

**Test Strategy:**

Create comprehensive test coverage for new TextReportGenerator:
- Story 5.1: ~8-10 tests for report generation and formatting
- Total tests after Story 5.1: ~8-10 tests in new Reporting namespace

**New Test Coverage:**

1. **Basic Report Generation:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_ValidGraph_CreatesReportFile()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10, edgeCount: 20);
       var outputDir = CreateTempDirectory();
       var solutionName = "TestSolution";

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, solutionName);

           // Assert
           File.Exists(reportPath).Should().BeTrue();
           reportPath.Should().EndWith("TestSolution-analysis-report.txt");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

2. **Header Content Validation:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_ValidGraph_ContainsHeaderWithSolutionName()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 73);
       var outputDir = CreateTempDirectory();
       var solutionName = "MyLegacySolution";

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, solutionName);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("Solution: MyLegacySolution");
           content.Should().Contain("Total Projects: 73");
           content.Should().MatchRegex(@"Analysis Date: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} UTC");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

3. **Dependency Overview Statistics:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_GraphWithFrameworkRefs_ShowsCorrectStatistics()
   {
       // Arrange
       var graph = CreateTestGraphWithFrameworkRefs(
           customProjects: 10,
           frameworkProjects: 5,
           customEdges: 20,
           frameworkEdges: 30);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution");

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("Total References: 50");  // 20 + 30
           content.Should().Contain("Framework References: 30");
           content.Should().Contain("Custom References: 20");
           content.Should().MatchRegex(@"Framework References: 30 \(60%\)");  // 30/50 = 60%
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

4. **Cross-Solution References (Multi-Solution):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_MultiSolutionGraph_ShowsCrossSolutionReferences()
   {
       // Arrange
       var graph = CreateMultiSolutionGraph(
           solution1Projects: 5,
           solution2Projects: 5,
           crossSolutionEdges: 3);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "MultiSolution");

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("Cross-Solution References: 3");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

5. **Null Parameter Validation:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_NullGraph_ThrowsArgumentNullException()
   {
       // Arrange
       var outputDir = CreateTempDirectory();

       try
       {
           // Act & Assert
           await _generator.Invoking(g => g.GenerateAsync(
               null!, outputDir, "TestSolution"))
               .Should().ThrowAsync<ArgumentNullException>()
               .WithParameterName("graph");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }

   [Fact]
   public async Task GenerateAsync_EmptyOutputDirectory_ThrowsArgumentException()
   {
       // Arrange
       var graph = CreateTestGraph();

       // Act & Assert
       await _generator.Invoking(g => g.GenerateAsync(
           graph, "", "TestSolution"))
           .Should().ThrowAsync<ArgumentException>();
   }
   ```

6. **Directory Not Found Validation:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_NonExistentDirectory_ThrowsDirectoryNotFoundException()
   {
       // Arrange
       var graph = CreateTestGraph();
       var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

       // Act & Assert
       await _generator.Invoking(g => g.GenerateAsync(
           graph, nonExistentDir, "TestSolution"))
           .Should().ThrowAsync<DirectoryNotFoundException>();
   }
   ```

7. **Performance Test (Within 10 Seconds):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_LargeGraph_CompletesWithin10Seconds()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 400, edgeCount: 5000);  // Simulate large solution
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var stopwatch = System.Diagnostics.Stopwatch.StartNew();
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "LargeSolution");
           stopwatch.Stop();

           // Assert
           stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

8. **UTF-8 Encoding Verification:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_GeneratedFile_UsesUTF8Encoding()
   {
       // Arrange
       var graph = CreateTestGraph();
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution");

           // Assert
           var bytes = await File.ReadAllBytesAsync(reportPath);
           var encoding = DetectEncoding(bytes);
           encoding.Should().Be(Encoding.UTF8);
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

**Test Helper Methods:**

```csharp
// Test fixture setup
private readonly ITextReportGenerator _generator;
private readonly ILogger<TextReportGenerator> _logger;

public TextReportGeneratorTests()
{
    _logger = new NullLogger<TextReportGenerator>();  // Or mock with Moq
    _generator = new TextReportGenerator(_logger);
}

// Helper: Create test graph
private DependencyGraph CreateTestGraph(int projectCount = 10, int edgeCount = 15)
{
    var graph = new DependencyGraph();

    // Add vertices
    for (int i = 0; i < projectCount; i++)
    {
        var project = new ProjectNode(
            $"Project{i}",
            $"C:\\projects\\Project{i}.csproj",
            "Solution1",
            isFrameworkReference: false);
        graph.AddVertex(project);
    }

    // Add edges
    var projects = graph.Vertices.ToList();
    for (int i = 0; i < Math.Min(edgeCount, projects.Count - 1); i++)
    {
        var edge = new DependencyEdge(projects[i], projects[i + 1], DependencyType.ProjectReference);
        graph.AddEdge(edge);
    }

    return graph;
}

// Helper: Create graph with framework references
private DependencyGraph CreateTestGraphWithFrameworkRefs(
    int customProjects,
    int frameworkProjects,
    int customEdges,
    int frameworkEdges)
{
    var graph = new DependencyGraph();

    // Add custom projects
    var customProjectList = new List<ProjectNode>();
    for (int i = 0; i < customProjects; i++)
    {
        var project = new ProjectNode(
            $"CustomProject{i}",
            $"C:\\projects\\CustomProject{i}.csproj",
            "Solution1",
            isFrameworkReference: false);
        graph.AddVertex(project);
        customProjectList.Add(project);
    }

    // Add framework projects
    var frameworkProjectList = new List<ProjectNode>();
    for (int i = 0; i < frameworkProjects; i++)
    {
        var project = new ProjectNode(
            $"System.Framework{i}",
            $"C:\\frameworks\\System.Framework{i}.dll",
            "Solution1",
            isFrameworkReference: true);  // Mark as framework
        graph.AddVertex(project);
        frameworkProjectList.Add(project);
    }

    // Add custom edges (between custom projects)
    for (int i = 0; i < Math.Min(customEdges, customProjectList.Count - 1); i++)
    {
        var edge = new DependencyEdge(
            customProjectList[i],
            customProjectList[i + 1],
            DependencyType.ProjectReference);
        graph.AddEdge(edge);
    }

    // Add framework edges (custom projects -> framework projects)
    for (int i = 0; i < Math.Min(frameworkEdges, customProjectList.Count); i++)
    {
        var edge = new DependencyEdge(
            customProjectList[i % customProjectList.Count],
            frameworkProjectList[i % frameworkProjectList.Count],
            DependencyType.BinaryReference);
        graph.AddEdge(edge);
    }

    return graph;
}

// Helper: Create multi-solution graph
private DependencyGraph CreateMultiSolutionGraph(
    int solution1Projects,
    int solution2Projects,
    int crossSolutionEdges)
{
    var graph = new DependencyGraph();

    var solution1ProjectList = new List<ProjectNode>();
    var solution2ProjectList = new List<ProjectNode>();

    // Add Solution1 projects
    for (int i = 0; i < solution1Projects; i++)
    {
        var project = new ProjectNode(
            $"Solution1.Project{i}",
            $"C:\\projects\\Solution1\\Project{i}.csproj",
            "Solution1",  // Different solution name
            isFrameworkReference: false);
        graph.AddVertex(project);
        solution1ProjectList.Add(project);
    }

    // Add Solution2 projects
    for (int i = 0; i < solution2Projects; i++)
    {
        var project = new ProjectNode(
            $"Solution2.Project{i}",
            $"C:\\projects\\Solution2\\Project{i}.csproj",
            "Solution2",  // Different solution name
            isFrameworkReference: false);
        graph.AddVertex(project);
        solution2ProjectList.Add(project);
    }

    // Add cross-solution edges
    for (int i = 0; i < Math.Min(crossSolutionEdges, Math.Min(solution1ProjectList.Count, solution2ProjectList.Count)); i++)
    {
        var edge = new DependencyEdge(
            solution1ProjectList[i],
            solution2ProjectList[i],
            DependencyType.ProjectReference);
        graph.AddEdge(edge);
    }

    return graph;
}

// Helper: Create temp directory
private string CreateTempDirectory()
{
    var path = Path.Combine(Path.GetTempPath(), "masdepmap-test-" + Guid.NewGuid());
    Directory.CreateDirectory(path);
    return path;
}

// Helper: Cleanup temp directory
private void CleanupTempDirectory(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }
}
```

### Previous Story Intelligence

**From Story 4-8 (Display Extraction Scores as Node Labels) - Visualization Pattern:**

Story 4-8 completed Epic 4's visualization enhancements. Story 5-1 begins Epic 5's reporting capabilities, which will later integrate with Story 4-8's extraction scores:

```csharp
// Story 4-8 pattern: Optional extraction scores parameter
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null,
    bool showScoreLabels = false,
    CancellationToken cancellationToken = default)

// Story 5.1 pattern: Similar optional parameters for extensibility
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<ExtractionScore>? extractionScores = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    CancellationToken cancellationToken = default)
```

**Pattern Reuse from Story 4-8:**

```csharp
// Story 4-8: Optional parameters for progressive feature enhancement
// Story 5.1: Same pattern - optional parameters for Stories 5.2-5.4 to extend

// Both stories use:
// - Optional parameters with null defaults
// - IReadOnlyList<T> for collections
// - CancellationToken last in signature
// - Return Task<string> (file path)
```

**From Epic 4 (Extraction Scoring) - Score Consumption Pattern:**

Story 5.1 will later consume ExtractionScore objects from Story 4.5-4.6 (in Story 5.3):

```csharp
// Story 4.6: Generates ranked extraction candidates
public sealed record RankedExtractionCandidates(
    IReadOnlyList<ExtractionScore> EasiestCandidates,
    IReadOnlyList<ExtractionScore> HardestCandidates,
    IReadOnlyList<ExtractionScore> AllScores);

// Story 5.3 (future): Will use RankedExtractionCandidates in report
// For now (Story 5.1): Interface accepts optional extractionScores parameter
```

**From Epic 3 (Cycle Detection) - Statistics Pattern:**

Story 5.1 calculates dependency statistics using similar patterns from Epic 3's cycle statistics:

```csharp
// Epic 3 (Story 3.2): CycleStatistics calculation pattern
public sealed record CycleStatistics(
    int TotalCycles,
    int TotalProjectsInCycles,
    double ProjectParticipationRate,  // Percentage
    int LargestCycleSize);

// Story 5.1: Similar statistics calculation for dependency overview
private void AppendDependencyOverview(StringBuilder report, DependencyGraph graph)
{
    var totalReferences = graph.EdgeCount;
    var frameworkReferences = graph.Edges.Count(e => IsFrameworkReference(e));
    var frameworkPercentage = totalReferences > 0 ? (frameworkReferences * 100.0 / totalReferences) : 0;
    // ...
}
```

**From Epic 2 (Dependency Analysis) - Graph Traversal Pattern:**

Story 5.1 uses single-pass graph traversal for performance (Epic 2 foundation):

```csharp
// Epic 2: DependencyGraphBuilder uses efficient graph construction
// Story 5.1: Similar single-pass enumeration for statistics

// Efficient (Story 5.1):
var frameworkReferences = graph.Edges.Count(e => IsFrameworkReference(e));  // Single pass
var crossSolutionReferences = graph.Edges.Count(e => IsCrossSolution(e));   // Separate pass

// Alternative (rejected): Multiple graph traversals would be slower
```

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 5.1 creates the foundation for Epic 5's reporting capabilities:

```
Epic 2 (Dependency Analysis) → Epic 3 (Cycle Detection) → Epic 4 (Extraction Scoring) → Epic 5 (Reporting)

src/MasDependencyMap.Core/DependencyAnalysis/
├── [Story 2.1-2.5: Dependency graph building] ✅ DONE
└── DependencyGraph.cs               # Consumed by Story 5.1

src/MasDependencyMap.Core/CycleAnalysis/
├── [Stories 3.1-3.7: Cycle detection and analysis] ✅ DONE
└── CycleInfo.cs                     # Will be consumed by Story 5.2

src/MasDependencyMap.Core/ExtractionScoring/
├── [Stories 4.1-4.8: Scoring and visualization] ✅ DONE
└── ExtractionScore.cs               # Will be consumed by Story 5.3

src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # NEW (Story 5.1)
└── TextReportGenerator.cs           # NEW (Story 5.1)

Epic 6 (Future):
- Error handling enhancements
- Progress indicators (Spectre.Console integration)
```

**Cross-Epic Integration:**

Story 5.1 completes the **bridge** between:
1. **Epic 2 (Dependency Analysis):** Produces DependencyGraph (consumed by Story 5.1)
2. **Epic 3 (Cycle Detection):** Produces CycleInfo (will be consumed by Story 5.2)
3. **Epic 4 (Extraction Scoring):** Produces ExtractionScore (will be consumed by Story 5.3)
4. **Epic 5 (Reporting):** Aggregates all analysis results into stakeholder-ready reports
5. **Epic 6 (Future):** Will enhance error handling and progress feedback

**Dependency Flow (Story 5.1 Foundation):**

```
Epic 2: DependencyGraphBuilder
        ↓
[DependencyGraph]
        ↓
Story 5.1: TextReportGenerator (Header + Dependency Overview) ← THIS STORY
        ↓
[Text Report File: {SolutionName}-analysis-report.txt]
        ↓
Story 5.2: Add Cycle Detection section (extends)
Story 5.3: Add Extraction Difficulty section (extends)
Story 5.4: Add Recommendations section (extends)
        ↓
[Complete Text Report with All Sections]
```

**Impact on Existing Components:**

```
NEW (Story 5.1):
- MasDependencyMap.Core.Reporting namespace (first component)
- ITextReportGenerator interface
- TextReportGenerator implementation
- TextReportGeneratorTests test class

MODIFIED (Story 5.1):
- src/MasDependencyMap.Cli/Program.cs: DI registration for ITextReportGenerator
- _bmad-output/implementation-artifacts/sprint-status.yaml: Update epic-5 and story 5-1 status

UNCHANGED (Backward compatible):
- All Epic 1-4 components (no changes)
- DependencyGraph structure (consumed as-is)
- CycleInfo structure (interface accepts, not used yet)
- ExtractionScore structure (interface accepts, not used yet)
```

**Epic 5 Roadmap After Story 5.1:**

After Story 5.1, Epic 5 continues with additional reporting capabilities:

1. ✅ Story 5.1: Text Report Generator foundation (THIS STORY)
2. ⏳ Story 5.2: Add Cycle Detection section (extends TextReportGenerator)
3. ⏳ Story 5.3: Add Extraction Difficulty section (extends TextReportGenerator)
4. ⏳ Story 5.4: Add Recommendations section (extends TextReportGenerator)
5. ⏳ Story 5.5: CSV export for extraction scores (new CsvExporter component)
6. ⏳ Story 5.6: CSV export for cycle analysis
7. ⏳ Story 5.7: CSV export for dependency matrix
8. ⏳ Story 5.8: Spectre.Console table formatting (CLI layer integration)

After Story 5.1:
- **Epic 5 foundation complete** ✅
- Stories 5.2-5.4 can extend TextReportGenerator with additional sections
- Stories 5.5-5.7 can create CsvExporter using similar patterns

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-5-comprehensive-reporting-and-data-export.md, Story 5.1 (lines 5-21)]
- Story requirements: Text report with header, dependency overview, clear formatting, 10-second performance target

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces (Reporting), async patterns, file-scoped namespaces, XML documentation
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 114-119)]
- Logging: Structured logging with named placeholders, ILogger<T> injection

**Architecture Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\implementation-patterns-consistency-rules.md (lines 9-19)]
- Namespace organization: Feature-based (MasDependencyMap.Core.Reporting), NOT layer-based
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\implementation-patterns-consistency-rules.md (lines 78-89)]
- Test organization: Mirror namespace structure in tests/
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md (lines 40-56)]
- Logging strategy: ILogger<T> for diagnostics, Spectre.Console for user-facing output (CLI layer)

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-8-display-extraction-scores-as-node-labels.md]
- Story 4-8 implementation: Optional parameters pattern for progressive enhancement
- ExtractionScore model: FinalScore, ProjectName, ProjectPath, metrics
- Pattern: Optional IReadOnlyList<T> parameters with null defaults

**Existing Code Patterns:**
- [Source: D:\work\masDependencyMap\src\MasDependencyMap.Core\ExtractionScoring\ExtractionScoreCalculator.cs (lines 1-100)]
- File-scoped namespace: `namespace MasDependencyMap.Core.ExtractionScoring;`
- Constructor injection: ILogger<T>, IConfiguration, other dependencies
- XML documentation: `<summary>`, `<param>`, `<exception>` tags
- Structured logging: `_logger.LogDebug("Message with {Placeholder}", value);`
- ConfigureAwait(false): `await operation.ConfigureAwait(false);`

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Recent pattern: Epic 4 stories completed with code review cycle
- Commit pattern: Story implementation → code review → fixes → status update
- Story 4-8 most recent: CLI compilation fix, code review fixes, then completion

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

No blocking issues encountered during implementation.

### Completion Notes List

✅ **Story 5.1 Implementation Complete**

**Created Files:**
- `src/MasDependencyMap.Core/Reporting/ITextReportGenerator.cs` - Interface for text report generation with optional parameters for future stories
- `src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs` - Implementation with header and dependency overview sections
- `tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs` - Comprehensive test coverage (12 tests)

**Modified Files:**
- `src/MasDependencyMap.CLI/Program.cs` - Added using directive and DI registration for ITextReportGenerator

**Implementation Highlights:**
1. **Framework Reference Detection:** Implemented pattern-based detection using common framework patterns (Microsoft.*, System.*, mscorlib, etc.) since ProjectNode doesn't have IsFrameworkReference property
2. **Performance:** Pre-allocated StringBuilder with 4096 capacity, single-pass LINQ queries - tested with 400 projects and 5000 edges, completes well within 10-second requirement
3. **Extensibility:** Interface designed with optional parameters for cycles, extractionScores, and recommendations to support Stories 5.2-5.4 without breaking changes
4. **Test Coverage:** 12 comprehensive tests covering happy paths, error cases, empty graphs, multi-solution scenarios, and performance validation

**Test Results:**
- All 397 tests pass (385 existing + 12 new TextReportGenerator tests)
- No regressions introduced
- Performance test validates <10 second generation time for large graphs

**Architecture Compliance:**
- ✅ Feature-based namespace: MasDependencyMap.Core.Reporting
- ✅ File-scoped namespace declarations
- ✅ Async/await patterns with ConfigureAwait(false)
- ✅ XML documentation on all public APIs
- ✅ Structured logging with named placeholders
- ✅ Test organization mirrors namespace structure

**Next Steps:**
Story 5.1 establishes the foundation for Epic 5 reporting. Future stories will extend TextReportGenerator:
- Story 5.2: Add Cycle Detection section
- Story 5.3: Add Extraction Difficulty section
- Story 5.4: Add Recommendations section

### Code Review Fixes (Post-Implementation)

✅ **Adversarial Code Review Completed - 5 Issues Fixed**

**Review Summary:**
- Initial implementation: 12 tests, all ACs met
- Code review identified: 1 HIGH, 4 MEDIUM, 3 LOW issues
- Auto-fixed: 1 HIGH + 4 MEDIUM = 5 critical issues
- Final test count: 15 tests (added 3 new tests)
- All 400 tests pass, no regressions

**HIGH Severity Fixes (1):**

1. **Security: Path Traversal Risk** (TextReportGenerator.cs:64)
   - **Issue:** Solution name used directly in filename without sanitization
   - **Risk:** Invalid filename characters could cause IOException or security issues
   - **Fix:** Added `SanitizeFileName()` method that replaces invalid characters with underscores
   - **Test:** Added `GenerateAsync_InvalidFileNameCharacters_SanitizesFileName()` test

**MEDIUM Severity Fixes (4):**

2. **Architecture: Framework Reference Detection Inconsistency** (TextReportGenerator.cs:144-163)
   - **Issue:** Hardcoded framework patterns diverged from configurable FilterConfiguration
   - **Risk:** Report statistics might not match actual filtering if user customizes config
   - **Fix:** Injected `IOptions<FilterConfiguration>` and reused BlockList/AllowList patterns
   - **Benefit:** Single source of truth for framework patterns, consistent with FrameworkFilter

3. **Missing Test: UTF-8 Encoding Verification** (TextReportGeneratorTests.cs)
   - **Issue:** No test verified UTF-8 encoding despite task claiming [x] completion
   - **Risk:** Regression if encoding changed
   - **Fix:** Added `GenerateAsync_GeneratedFile_UsesUTF8Encoding()` test
   - **Implementation Update:** Changed to UTF-8 without BOM (standard for plain text)

4. **Code Duplication: Framework Pattern Knowledge Scattered**
   - **Issue:** Framework detection logic duplicated in two places
   - **Fix:** Fixed by issue #2 - centralized in FilterConfiguration
   - **Benefit:** DRY principle, easier maintenance

5. **Missing Test: Cancellation Token Support** (TextReportGeneratorTests.cs)
   - **Issue:** No test verified cancellation token works
   - **Risk:** Long-running reports couldn't be cancelled
   - **Fix:** Added `GenerateAsync_CancellationToken_ThrowsOperationCanceledException()` test
   - **Verification:** Confirms async file write respects cancellation

**LOW Severity Fixes (1):**

6. **Git Discrepancy: Untracked "nul" File**
   - **Issue:** Untracked `nul` file in repository root (Windows artifact)
   - **Fix:** Deleted file
   - **Prevention:** Not added to .gitignore (temporary artifact, shouldn't recur)

**What Was NOT Fixed (Documented for Future):**

- **LOW: Thread Safety Verification** - Implementation is stateless (safe), but no concurrent test added
- **LOW: Case-Insensitive Cross-Solution Detection** - Existing behavior kept for Windows path compatibility

**Updated Statistics:**
- Test Coverage: 15 tests (12 original + 3 new from code review)
- Total Project Tests: 400 tests (all passing)
- Issues Fixed: 5 critical (1 HIGH + 4 MEDIUM)
- Security Improvements: Path sanitization prevents invalid filename attacks
- Architecture Improvements: Eliminated code duplication, centralized configuration

### File List

**New Files:**
- src/MasDependencyMap.Core/Reporting/ITextReportGenerator.cs
- src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs
- tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs

**Modified Files:**
- src/MasDependencyMap.CLI/Program.cs
