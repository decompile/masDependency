# Story 5.6: Implement CSV Export for Cycle Analysis

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want cycle analysis details exported to CSV,
So that I can share cycle information with my team for tracking.

## Acceptance Criteria

**Given** Cycles have been detected
**When** CsvExporter.ExportCycleAnalysisAsync() is called
**Then** CsvHelper library generates {SolutionName}-cycle-analysis.csv in RFC 4180 format
**And** CSV columns include: "Cycle ID", "Cycle Size", "Projects Involved", "Suggested Break Point", "Coupling Score"
**And** Each row represents one cycle
**And** Projects Involved column lists all project names in the cycle (comma-separated within quotes)
**And** Suggested Break Point shows "SourceProject → TargetProject"
**And** CSV is RFC 4180 compliant and opens correctly in Excel/Google Sheets

## Tasks / Subtasks

- [x] Extend ICsvExporter interface (AC: Add method signature)
  - [x] Add ExportCycleAnalysisAsync method to ICsvExporter
  - [x] Define parameters: cycles list, suggestions list, output directory, solution name
  - [x] Add XML documentation for the new method
  - [x] Return Task<string> with path to generated CSV file

- [x] Create CycleAnalysisRecord POCO (AC: CSV column mapping)
  - [x] Define properties matching CSV columns with Title Case with Spaces
  - [x] Map CycleId → "Cycle ID" (integer)
  - [x] Map CycleSize → "Cycle Size" (integer)
  - [x] Map ProjectsInvolved → "Projects Involved" (comma-separated string)
  - [x] Map SuggestedBreakPoint → "Suggested Break Point" (format: "Source → Target")
  - [x] Map CouplingScore → "Coupling Score" (integer)

- [x] Create CycleAnalysisRecordMap ClassMap (AC: Header customization)
  - [x] Use ClassMap<CycleAnalysisRecord> for column header mapping
  - [x] Set column headers with exact casing per AC
  - [x] Define column order: Cycle ID, Cycle Size, Projects Involved, Suggested Break Point, Coupling Score
  - [x] Ensure RFC 4180 compliance

- [x] Implement ExportCycleAnalysisAsync in CsvExporter (AC: CSV generation)
  - [x] Add method implementation to existing CsvExporter class
  - [x] Validate input parameters (null checks, empty checks)
  - [x] Match cycles with suggestions using CycleId
  - [x] Map CycleInfo + CycleBreakingSuggestion to CycleAnalysisRecord
  - [x] Format "Projects Involved" as comma-separated list
  - [x] Format "Suggested Break Point" as "Source → Target"
  - [x] Handle cycles without suggestions (use "N/A" for break point and score)
  - [x] Sort cycles by CycleId ascending for consistency
  - [x] Configure CsvHelper with UTF-8 BOM and InvariantCulture
  - [x] Register CycleAnalysisRecordMap ClassMap
  - [x] Write CSV using CsvHelper.WriteRecordsAsync
  - [x] Log export success with record count and elapsed time
  - [x] Return absolute path to generated CSV file

- [x] Handle edge cases (AC: Graceful degradation)
  - [x] Empty cycles list: Create CSV with headers only
  - [x] Cycle without suggestion: Export "N/A" for Suggested Break Point and Coupling Score
  - [x] Large cycle with many projects: Handle long Projects Involved string (CSV quoting)
  - [x] Multiple suggestions for same cycle: Use first (lowest coupling) suggestion

- [x] Add comprehensive unit tests (AC: Test coverage)
  - [x] Test: ExportCycleAnalysisAsync_ValidCycles_GeneratesValidCsv (basic export)
  - [x] Test: ExportCycleAnalysisAsync_SortsByCycleId_Ascending (sorting validation)
  - [x] Test: ExportCycleAnalysisAsync_ColumnHeaders_UseTitleCaseWithSpaces (header validation)
  - [x] Test: ExportCycleAnalysisAsync_ProjectsInvolved_CommaSeparated (formatting validation)
  - [x] Test: ExportCycleAnalysisAsync_SuggestedBreakPoint_FormattedCorrectly (arrow format)
  - [x] Test: ExportCycleAnalysisAsync_CycleWithoutSuggestion_ExportsNA (null handling)
  - [x] Test: ExportCycleAnalysisAsync_UTF8WithBOM_OpensInExcel (encoding validation)
  - [x] Test: ExportCycleAnalysisAsync_EmptyCycles_CreatesEmptyCsv (edge case)
  - [x] Test: ExportCycleAnalysisAsync_LargeCycle_HandlesLongProjectList (RFC 4180 quoting)
  - [x] Test: ExportCycleAnalysisAsync_CancellationToken_CancelsOperation (cancellation support)

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Namespace: MasDependencyMap.Core.Reporting (existing, consistent with Story 5.5)
  - [x] File-scoped namespace declaration
  - [x] Async all the way: Use ConfigureAwait(false) in library code
  - [x] Structured logging with named placeholders
  - [x] XML documentation for public APIs (interface method)
  - [x] No XML documentation for private helpers

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 5.6 EXTENDS Existing CSV Infrastructure:**

This story **extends** the CSV export infrastructure created in Story 5.5, NOT a new component.

**Epic 5 Reporting Stack Progress:**
- ✅ Story 5.1: Text Report Generator with Summary Statistics (FOUNDATION - DONE)
- ✅ Story 5.2: Add Cycle Detection section to text reports (EXTENDS - DONE)
- ✅ Story 5.3: Add Extraction Difficulty Scoring section to text reports (EXTENDS - DONE)
- ✅ Story 5.4: Add Cycle-Breaking Recommendations to text reports (EXTENDS - DONE)
- ✅ Story 5.5: CSV export for extraction difficulty scores (NEW COMPONENT - DONE)
- 🔨 Story 5.6: CSV export for cycle analysis (EXTENDS CsvExporter - THIS STORY)
- ⏳ Story 5.7: CSV export for dependency matrix (EXTENDS CsvExporter)
- ⏳ Story 5.8: Spectre.Console table formatting enhancements (CLI INTEGRATION)

**Story 5.6 Unique Characteristics:**

1. **Extends Existing Component:**
   ```
   Story 5.5: Created ICsvExporter interface and CsvExporter implementation
   Story 5.6: Adds ExportCycleAnalysisAsync method to both ← THIS STORY
   Story 5.7: Adds ExportDependencyMatrixAsync method to both
   ```

2. **Interface Extension Pattern:**
   ```csharp
   public interface ICsvExporter
   {
       // Story 5.5 (existing):
       Task<string> ExportExtractionScoresAsync(...);

       // Story 5.6 (NEW METHOD):
       Task<string> ExportCycleAnalysisAsync(
           IReadOnlyList<CycleInfo> cycles,
           IReadOnlyList<CycleBreakingSuggestion> suggestions,
           string outputDirectory,
           string solutionName,
           CancellationToken cancellationToken = default);

       // Story 5.7 will add:
       // Task<string> ExportDependencyMatrixAsync(...);
   }
   ```

3. **Consumes Epic 3 Cycle Analysis:**
   - Epic 3 (Stories 3.1-3.7): All done, CycleInfo and CycleBreakingSuggestion models exist
   - Story 3.1: Implemented Tarjan's SCC algorithm for cycle detection
   - Story 3.2: Calculated cycle statistics and participation rates
   - Story 3.3: Implemented coupling strength analysis via method call counting
   - Story 3.4: Identified weakest coupling edges within cycles
   - Story 3.5: Generated ranked cycle-breaking recommendations
   - **Story 5.6 integration point:** Consumes IReadOnlyList<CycleInfo> and IReadOnlyList<CycleBreakingSuggestion>
   - **No Epic 3 changes needed** - Pure consumption of existing data

4. **CSV File Structure:**
   ```
   Cycle ID,Cycle Size,Projects Involved,Suggested Break Point,Coupling Score
   1,3,"ProjectA, ProjectB, ProjectC","ProjectA → ProjectB",5
   2,5,"ProjectD, ProjectE, ProjectF, ProjectG, ProjectH","ProjectE → ProjectF",3
   3,2,"ProjectI, ProjectJ","ProjectI → ProjectJ",12
   ```
   - **Header row:** Title Case with Spaces (Excel-friendly)
   - **Data rows:** One row per cycle
   - **Sorting:** By Cycle ID ascending (natural ordering)
   - **Projects Involved:** Comma-separated list, quoted per RFC 4180
   - **Suggested Break Point:** Arrow format (Source → Target)
   - **Null handling:** "N/A" for cycles without suggestions

5. **Matching Cycles with Suggestions:**
   ```csharp
   // Create lookup dictionary for fast matching
   var suggestionsByCycle = suggestions
       .GroupBy(s => s.CycleId)
       .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Rank).First());

   // Map each cycle to record
   foreach (var cycle in cycles.OrderBy(c => c.CycleId))
   {
       var suggestion = suggestionsByCycle.GetValueOrDefault(cycle.CycleId);

       var record = new CycleAnalysisRecord
       {
           CycleId = cycle.CycleId,
           CycleSize = cycle.CycleSize,
           ProjectsInvolved = string.Join(", ", cycle.Projects.Select(p => p.ProjectName)),
           SuggestedBreakPoint = suggestion != null
               ? $"{suggestion.SourceProject.ProjectName} → {suggestion.TargetProject.ProjectName}"
               : "N/A",
           CouplingScore = suggestion?.CouplingScore ?? 0
       };
   }
   ```

6. **RFC 4180 Quoting for Projects Involved:**
   - Projects Involved contains commas (separator between project names)
   - RFC 4180 requires quoting fields containing commas
   - CsvHelper automatically handles this when writing
   - Example: `"ProjectA, ProjectB, ProjectC"` (entire field quoted)
   - Resulting CSV: `1,3,"ProjectA, ProjectB, ProjectC","ProjectA → ProjectB",5`

🚨 **CRITICAL - CycleInfo and CycleBreakingSuggestion Data Models (from Epic 3):**

Story 5.6 consumes the cycle analysis models created in Epic 3. Understanding these models is critical for correct CSV mapping.

**CycleInfo Definition (from Epic 3, Story 3.1-3.2):**

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

public sealed class CycleInfo
{
    /// <summary>
    /// Unique identifier for this cycle (1-based sequential).
    /// </summary>
    public int CycleId { get; init; }

    /// <summary>
    /// List of projects involved in this circular dependency.
    /// </summary>
    public IReadOnlyList<ProjectNode> Projects { get; init; }

    /// <summary>
    /// Number of projects in this cycle.
    /// </summary>
    public int CycleSize => Projects.Count;

    /// <summary>
    /// Weakest coupling edges within this cycle (lowest coupling score).
    /// Populated by WeakEdgeIdentifier service (Story 3.4).
    /// </summary>
    public IReadOnlyList<DependencyEdge> WeakCouplingEdges { get; set; }

    /// <summary>
    /// Minimum coupling score found within this cycle.
    /// Null until weak edge analysis is performed.
    /// </summary>
    public int? WeakCouplingScore { get; set; }
}
```

**CycleBreakingSuggestion Definition (from Epic 3, Story 3.5):**

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

public sealed record CycleBreakingSuggestion : IComparable<CycleBreakingSuggestion>
{
    /// <summary>
    /// Unique identifier for this cycle (matches CycleInfo.CycleId).
    /// </summary>
    public int CycleId { get; init; }

    /// <summary>
    /// Source project of the dependency edge to break.
    /// </summary>
    public ProjectNode SourceProject { get; init; }

    /// <summary>
    /// Target project of the dependency edge to break.
    /// </summary>
    public ProjectNode TargetProject { get; init; }

    /// <summary>
    /// Coupling score for this dependency edge (number of method calls).
    /// </summary>
    public int CouplingScore { get; init; }

    /// <summary>
    /// Size of the cycle this edge belongs to (number of projects).
    /// </summary>
    public int CycleSize { get; init; }

    /// <summary>
    /// Human-readable rationale explaining why this edge is recommended.
    /// </summary>
    public string Rationale { get; init; }

    /// <summary>
    /// Priority ranking (1 = highest priority).
    /// </summary>
    public int Rank { get; init; }
}
```

**CSV Column Mapping:**

| CSV Column            | Source Property                                    | Format           | Null Handling |
|-----------------------|----------------------------------------------------|------------------|---------------|
| Cycle ID              | CycleInfo.CycleId                                  | Integer          | N/A           |
| Cycle Size            | CycleInfo.CycleSize                                | Integer          | N/A           |
| Projects Involved     | CycleInfo.Projects (joined with ", ")             | Quoted string    | N/A           |
| Suggested Break Point | Suggestion.SourceProject → TargetProject           | Arrow format     | "N/A" if null |
| Coupling Score        | Suggestion.CouplingScore                           | Integer          | 0 if no suggestion |

**CycleAnalysisRecord POCO for CSV Mapping:**

```csharp
namespace MasDependencyMap.Core.Reporting;

/// <summary>
/// POCO record for CSV export of cycle analysis results.
/// Maps CycleInfo and CycleBreakingSuggestion data to CSV columns with Title Case with Spaces headers.
/// </summary>
public sealed record CycleAnalysisRecord
{
    public int CycleId { get; init; }
    public int CycleSize { get; init; }
    public string ProjectsInvolved { get; init; } = string.Empty;
    public string SuggestedBreakPoint { get; init; } = string.Empty;
    public int CouplingScore { get; init; }
}
```

**Mapping Logic:**

```csharp
private CycleAnalysisRecord MapToRecord(CycleInfo cycle, CycleBreakingSuggestion? suggestion)
{
    // Join project names with comma-space separator
    var projectsInvolved = string.Join(", ", cycle.Projects.Select(p => p.ProjectName));

    // Format break point with arrow if suggestion exists
    var breakPoint = suggestion != null
        ? $"{suggestion.SourceProject.ProjectName} → {suggestion.TargetProject.ProjectName}"
        : "N/A";

    // Use suggestion's coupling score, or 0 if no suggestion
    var couplingScore = suggestion?.CouplingScore ?? 0;

    return new CycleAnalysisRecord
    {
        CycleId = cycle.CycleId,
        CycleSize = cycle.CycleSize,
        ProjectsInvolved = projectsInvolved,
        SuggestedBreakPoint = breakPoint,
        CouplingScore = couplingScore
    };
}
```

🚨 **CRITICAL - CsvHelper Configuration Pattern (Consistent with Story 5.5):**

Story 5.6 uses the SAME CsvHelper configuration as Story 5.5 for consistency.

**CsvHelper Configuration for Story 5.6:**

```csharp
public async Task<string> ExportCycleAnalysisAsync(
    IReadOnlyList<CycleInfo> cycles,
    IReadOnlyList<CycleBreakingSuggestion> suggestions,
    string outputDirectory,
    string solutionName,
    CancellationToken cancellationToken = default)
{
    // Validate inputs
    ArgumentNullException.ThrowIfNull(cycles);
    ArgumentNullException.ThrowIfNull(suggestions);
    ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
    ArgumentException.ThrowIfNullOrWhiteSpace(solutionName);

    // Create lookup dictionary for cycle → suggestion matching
    var suggestionsByCycle = suggestions
        .GroupBy(s => s.CycleId)
        .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Rank).First());

    // Sort cycles by CycleId ascending (natural ordering)
    var sortedCycles = cycles.OrderBy(c => c.CycleId).ToList();

    // Generate filename and ensure directory exists
    var sanitizedSolutionName = SanitizeFileName(solutionName);
    var fileName = $"{sanitizedSolutionName}-cycle-analysis.csv";
    var filePath = Path.Combine(outputDirectory, fileName);

    Directory.CreateDirectory(outputDirectory);

    // Configure CsvHelper for Excel compatibility (SAME AS STORY 5.5)
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), // UTF-8 with BOM
        NewLine = "\r\n" // CRLF for Windows/Excel compatibility
    };

    // Write CSV using CsvHelper
    await using var writer = new StreamWriter(filePath, append: false, encoding: config.Encoding);
    await using var csv = new CsvWriter(writer, config);

    // Register ClassMap for column header customization
    csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();

    // Map CycleInfo + CycleBreakingSuggestion objects to CycleAnalysisRecord POCOs
    var records = sortedCycles.Select(cycle =>
    {
        var suggestion = suggestionsByCycle.GetValueOrDefault(cycle.CycleId);
        return MapToRecord(cycle, suggestion);
    }).ToList();

    // Write header and data rows
    await csv.WriteRecordsAsync(records, cancellationToken).ConfigureAwait(false);

    // Log success
    _logger.LogInformation(
        "Exported {RecordCount} cycle analysis records to CSV at {FilePath}",
        records.Count,
        filePath);

    return filePath;
}
```

**ClassMap for Column Header Customization:**

```csharp
using CsvHelper.Configuration;

namespace MasDependencyMap.Core.Reporting;

/// <summary>
/// CsvHelper ClassMap for CycleAnalysisRecord to customize column headers with Title Case with Spaces.
/// </summary>
public sealed class CycleAnalysisRecordMap : ClassMap<CycleAnalysisRecord>
{
    public CycleAnalysisRecordMap()
    {
        // Map properties to CSV columns with Title Case with Spaces headers
        Map(m => m.CycleId).Name("Cycle ID").Index(0);
        Map(m => m.CycleSize).Name("Cycle Size").Index(1);
        Map(m => m.ProjectsInvolved).Name("Projects Involved").Index(2);
        Map(m => m.SuggestedBreakPoint).Name("Suggested Break Point").Index(3);
        Map(m => m.CouplingScore).Name("Coupling Score").Index(4);
    }
}
```

**Key Implementation Points:**

1. **Cycle-Suggestion Matching:**
   - Create dictionary lookup: `suggestionsByCycle[cycleId]`
   - Handle cycles without suggestions: Use "N/A" for break point
   - Multiple suggestions per cycle: Use first (lowest rank = highest priority)

2. **Projects Involved Formatting:**
   - Join project names with ", " separator
   - CsvHelper automatically quotes field (contains commas)
   - Example: `"ProjectA, ProjectB, ProjectC"`

3. **Break Point Formatting:**
   - Use Unicode arrow: `→` (U+2192)
   - Format: `"{SourceProject.ProjectName} → {TargetProject.ProjectName}"`
   - Example: `"PaymentService → OrderManagement"`
   - Null handling: `"N/A"` when no suggestion exists

4. **Sorting:**
   - Sort by CycleId ascending for natural ordering
   - Ensures CSV rows match the order cycles were detected

### Technical Requirements

**Modified Components: ICsvExporter and CsvExporter**

Story 5.6 extends the CSV export infrastructure created in Story 5.5:

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # Story 5.1 (unchanged)
├── TextReportGenerator.cs           # Stories 5.1-5.4 (unchanged)
├── ICsvExporter.cs                  # Story 5.5 created, Story 5.6 MODIFIES ← THIS STORY
├── CsvExporter.cs                   # Story 5.5 created, Story 5.6 MODIFIES ← THIS STORY
├── ExtractionScoreRecord.cs         # Story 5.5 (unchanged)
├── ExtractionScoreRecordMap.cs      # Story 5.5 (unchanged)
├── CycleAnalysisRecord.cs           # NEW: Story 5.6 POCO for CSV mapping ← THIS STORY
└── CycleAnalysisRecordMap.cs        # NEW: Story 5.6 CsvHelper ClassMap ← THIS STORY

tests/MasDependencyMap.Core.Tests/Reporting/
└── CsvExporterTests.cs              # Story 5.5 created, Story 5.6 EXTENDS ← THIS STORY (~10 new tests)
```

**Existing Dependencies (from Story 5.5):**

All Story 5.5 dependencies remain, plus new Epic 3 cycle analysis dependencies:

```csharp
using System.Text;                                // UTF8Encoding
using MasDependencyMap.Core.CycleAnalysis;        // CycleInfo, CycleBreakingSuggestion (Epic 3)
using MasDependencyMap.Core.DependencyAnalysis;   // ProjectNode (Epic 2)
using Microsoft.Extensions.Logging;               // ILogger<T>
using CsvHelper;                                  // CsvWriter
using CsvHelper.Configuration;                    // CsvConfiguration, ClassMap<T>
using System.Globalization;                       // CultureInfo.InvariantCulture
```

**No New NuGet Packages Required:**

All dependencies already installed:
- ✅ Epic 1: Microsoft.Extensions.Logging.Console
- ✅ Epic 5: CsvHelper v33.1.0 (installed in Story 5.5)
- ✅ Built-in: System.Text, System.IO, System.Linq, System.Globalization

**Interface Extension:**

Story 5.6 adds one method to ICsvExporter:

```csharp
namespace MasDependencyMap.Core.Reporting;

public interface ICsvExporter
{
    // Story 5.5 (existing):
    Task<string> ExportExtractionScoresAsync(
        IReadOnlyList<ExtractionScore> scores,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default);

    // Story 5.6 (NEW):
    /// <summary>
    /// Exports cycle analysis results to CSV file.
    /// Includes cycle details and suggested break points for each cycle.
    /// </summary>
    /// <param name="cycles">List of detected circular dependency cycles.</param>
    /// <param name="suggestions">List of cycle-breaking suggestions.</param>
    /// <param name="outputDirectory">Directory where CSV file will be created.</param>
    /// <param name="solutionName">Solution name used for CSV filename.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Absolute path to the generated CSV file.</returns>
    /// <exception cref="ArgumentNullException">When cycles, suggestions, or paths are null.</exception>
    /// <exception cref="ArgumentException">When outputDirectory or solutionName is empty.</exception>
    /// <exception cref="IOException">When file write operation fails.</exception>
    Task<string> ExportCycleAnalysisAsync(
        IReadOnlyList<CycleInfo> cycles,
        IReadOnlyList<CycleBreakingSuggestion> suggestions,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default);

    // Story 5.7 will add:
    // Task<string> ExportDependencyMatrixAsync(...);
}
```

**Implementation Class Extension:**

```csharp
namespace MasDependencyMap.Core.Reporting;

public sealed class CsvExporter : ICsvExporter
{
    // Fields (unchanged from Story 5.5)
    private readonly ILogger<CsvExporter> _logger;

    // Constructor (unchanged from Story 5.5)
    public CsvExporter(ILogger<CsvExporter> logger) { ... }

    // Story 5.5 method (unchanged)
    public async Task<string> ExportExtractionScoresAsync(...) { ... }

    // Story 5.6 NEW method
    public async Task<string> ExportCycleAnalysisAsync(
        IReadOnlyList<CycleInfo> cycles,
        IReadOnlyList<CycleBreakingSuggestion> suggestions,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    // Private helper methods
    private CycleAnalysisRecord MapToRecord(CycleInfo cycle, CycleBreakingSuggestion? suggestion) { ... }
    private string SanitizeFileName(string fileName) { ... } // Existing from Story 5.5
}
```

### Architecture Compliance

**Namespace Structure:**

Story 5.6 adds components to the existing `MasDependencyMap.Core.Reporting` namespace:

```csharp
namespace MasDependencyMap.Core.Reporting;  // File-scoped namespace (C# 10+)

using MasDependencyMap.Core.CycleAnalysis;        // For CycleInfo, CycleBreakingSuggestion
using MasDependencyMap.Core.DependencyAnalysis;   // For ProjectNode
using Microsoft.Extensions.Logging;               // For ILogger<T>
using CsvHelper;                                  // For CsvWriter
using CsvHelper.Configuration;                    // For CsvConfiguration, ClassMap<T>
using System.Globalization;                       // For CultureInfo.InvariantCulture
```

**Structured Logging (Required):**

Add logging for cycle CSV export operations:

```csharp
// Start of export:
_logger.LogInformation("Exporting {CycleCount} cycles to CSV for solution {SolutionName}",
    cycles.Count,
    solutionName);

// Warning for cycle without suggestion:
_logger.LogWarning("Cycle {CycleId} has no breaking suggestion, exporting with N/A",
    cycle.CycleId);

// Success:
_logger.LogInformation(
    "Exported {RecordCount} cycle analysis records to CSV at {FilePath}",
    records.Count,
    filePath);

// Error:
_logger.LogError(ex, "Failed to export cycle analysis to CSV for solution {SolutionName}",
    solutionName);
```

**XML Documentation (Required for Public APIs):**

ICsvExporter interface method requires XML documentation:

```csharp
/// <summary>
/// Exports cycle analysis results to CSV file.
/// Includes cycle details and suggested break points for each cycle.
/// </summary>
/// <param name="cycles">List of detected circular dependency cycles.</param>
/// <param name="suggestions">List of cycle-breaking suggestions.</param>
/// <param name="outputDirectory">Directory where CSV file will be created.</param>
/// <param name="solutionName">Solution name used for CSV filename.</param>
/// <param name="cancellationToken">Cancellation token for async operation.</param>
/// <returns>Absolute path to the generated CSV file.</returns>
/// <exception cref="ArgumentNullException">When cycles, suggestions, or paths are null.</exception>
/// <exception cref="ArgumentException">When outputDirectory or solutionName is empty.</exception>
/// <exception cref="IOException">When file write operation fails.</exception>
Task<string> ExportCycleAnalysisAsync(...);
```

**Async All The Way (Required):**

All I/O operations must be async with ConfigureAwait(false):

```csharp
// File write:
await using var writer = new StreamWriter(filePath, append: false, encoding: config.Encoding);
await using var csv = new CsvWriter(writer, config);

// CSV write:
await csv.WriteRecordsAsync(records, cancellationToken).ConfigureAwait(false);
```

### Library/Framework Requirements

**CsvHelper v33.1.0 (Already Installed in Story 5.5):**

Story 5.6 uses the same CsvHelper configuration as Story 5.5:

**From Epic 3 (Cycle Analysis):**
- ✅ CycleInfo model - Consumed by Story 5.6 ← THIS STORY
- ✅ CycleBreakingSuggestion model - Consumed by Story 5.6 ← THIS STORY
- ✅ Tarjan's SCC algorithm - Generated cycles
- ✅ Weak edge identification - Generated suggestions

**From Epic 5 (Story 5.5):**
- ✅ ICsvExporter interface - Extended by Story 5.6 ← THIS STORY
- ✅ CsvExporter class - Extended by Story 5.6 ← THIS STORY
- ✅ CsvHelper v33.1.0 - Reused for cycle CSV export

**Built-in .NET Libraries:**
- ✅ System.Text - UTF8Encoding
- ✅ System.IO - File and directory operations
- ✅ System.Linq - LINQ queries for matching and mapping
- ✅ System.Globalization - CultureInfo.InvariantCulture

**Epic 3 Cycle Analysis Models (Existing):**

Story 5.6 consumes the cycle analysis models from Epic 3 without modifications:

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

// Created in Epic 3, Stories 3.1-3.2
public sealed class CycleInfo
{
    public int CycleId { get; init; }
    public IReadOnlyList<ProjectNode> Projects { get; init; }
    public int CycleSize => Projects.Count;
    public IReadOnlyList<DependencyEdge> WeakCouplingEdges { get; set; }
    public int? WeakCouplingScore { get; set; }
}

// Created in Epic 3, Story 3.5
public sealed record CycleBreakingSuggestion
{
    public int CycleId { get; init; }
    public ProjectNode SourceProject { get; init; }
    public ProjectNode TargetProject { get; init; }
    public int CouplingScore { get; init; }
    public int CycleSize { get; init; }
    public string Rationale { get; init; }
    public int Rank { get; init; }
}
```

**No Changes to Epic 3 Components:**

Story 5.6 is a pure consumer - no changes to:
- ✅ TarjanCycleDetector.cs (Epic 3, Story 3.1)
- ✅ CycleStatisticsCalculator.cs (Epic 3, Story 3.2)
- ✅ CouplingAnalyzer.cs (Epic 3, Story 3.3)
- ✅ WeakEdgeIdentifier.cs (Epic 3, Story 3.4)
- ✅ CycleBreakingRecommendationGenerator.cs (Epic 3, Story 3.5)

**Story 5.6 Integration Point:**

```
Epic 3: CycleBreakingRecommendationGenerator.GenerateRecommendations()
        ↓
[IReadOnlyList<CycleInfo> cycles]
[IReadOnlyList<CycleBreakingSuggestion> suggestions]
        ↓
Story 5.6: CsvExporter.ExportCycleAnalysisAsync(cycles, suggestions, ...)
        ↓
[CSV File: {SolutionName}-cycle-analysis.csv]
```

### File Structure Requirements

**Files to Modify (2 Existing Files):**

```
src/MasDependencyMap.Core/Reporting/
├── ICsvExporter.cs                  # MODIFY: Add ExportCycleAnalysisAsync method
└── CsvExporter.cs                   # MODIFY: Implement ExportCycleAnalysisAsync method
```

**Files to Create (2 New Files):**

```
src/MasDependencyMap.Core/Reporting/
├── CycleAnalysisRecord.cs           # NEW: POCO for CSV mapping
└── CycleAnalysisRecordMap.cs        # NEW: CsvHelper ClassMap for column headers

tests/MasDependencyMap.Core.Tests/Reporting/
└── CsvExporterTests.cs              # MODIFY: Add ~10 tests for cycle export
```

**No Changes to CLI Registration:**

CsvExporter already registered in DI container (Story 5.5). No changes needed:
```csharp
// CLI Program.cs (unchanged from Story 5.5):
services.AddSingleton<ICsvExporter, CsvExporter>();  // Story 5.5 registration
```

**No Changes to Other Components:**

```
UNCHANGED (Epics 1-4):
- All Epic 1 foundation components
- All Epic 2 dependency analysis components
- All Epic 3 cycle detection components ✅ (consumed as-is)
- All Epic 4 extraction scoring components

UNCHANGED (Epic 5 Stories 5.1-5.5):
- ITextReportGenerator interface
- TextReportGenerator implementation
- ExtractionScoreRecord and ExtractionScoreRecordMap (Story 5.5)
- All test report generation tests
```

**File Modification Details:**

1. **ICsvExporter.cs (MODIFY):**
   - Add ExportCycleAnalysisAsync method signature
   - Add XML documentation for the new method
   - **Estimated lines added:** ~15-20 lines

2. **CsvExporter.cs (MODIFY):**
   - Implement ExportCycleAnalysisAsync method
   - Add MapToRecord private helper for cycle mapping
   - Reuse existing SanitizeFileName helper
   - **Estimated lines added:** ~80-100 lines

3. **CycleAnalysisRecord.cs (NEW):**
   - POCO record for CSV mapping
   - Properties with Title Case with Spaces mapping
   - All integer properties (no string formatting needed)
   - **Estimated lines:** ~15-20 lines

4. **CycleAnalysisRecordMap.cs (NEW):**
   - CsvHelper ClassMap<CycleAnalysisRecord>
   - Column header customization with Title Case with Spaces
   - Column order specification (Index property)
   - **Estimated lines:** ~15-20 lines

5. **CsvExporterTests.cs (MODIFY):**
   - Add ~10 comprehensive unit tests for cycle export
   - Add helper methods for test data creation (CycleInfo, CycleBreakingSuggestion)
   - **Estimated lines added:** ~300-350 lines

**Total Story 5.6 Code:**
- New production code: ~110-140 lines
- Modified production code (interface + impl): ~95-120 lines
- New test code: ~300-350 lines
- **Total:** ~505-610 lines

### Testing Requirements

**Test Strategy: Extend Existing CsvExporterTests**

Story 5.6 adds ~10 new tests to the existing `CsvExporterTests.cs` test class:

```
tests/MasDependencyMap.Core.Tests/Reporting/
└── CsvExporterTests.cs              # Story 5.5: ~15 tests, Story 5.6: ~10 new tests (~25 total)
```

**New Test Coverage (Story 5.6):**

1. **Basic CSV Export:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_ValidCycles_GeneratesValidCsv()
   {
       // Arrange
       var cycles = CreateTestCycles(count: 5);
       var suggestions = CreateTestSuggestions(cycles);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               cycles, suggestions, outputDir, "TestSolution");

           // Assert
           File.Exists(filePath).Should().BeTrue();
           var content = await File.ReadAllTextAsync(filePath);
           content.Should().Contain("Cycle ID,Cycle Size,Projects Involved");

           // Parse CSV and verify row count
           using var reader = new StreamReader(filePath);
           using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
           var records = csv.GetRecords<CycleAnalysisRecord>().ToList();
           records.Should().HaveCount(5);
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

2. **Sorting Validation:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_SortsByCycleId_Ascending()
   {
       // Arrange
       var cycles = new List<CycleInfo>
       {
           CreateCycle(cycleId: 3, projectCount: 2),
           CreateCycle(cycleId: 1, projectCount: 5),
           CreateCycle(cycleId: 2, projectCount: 3)
       };
       var suggestions = CreateTestSuggestions(cycles);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               cycles, suggestions, outputDir, "TestSolution");

           // Assert
           using var reader = new StreamReader(filePath);
           using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
           var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

           // Verify ascending order by CycleId
           records[0].CycleId.Should().Be(1);
           records[1].CycleId.Should().Be(2);
           records[2].CycleId.Should().Be(3);
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

3. **Column Header Validation:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_ColumnHeaders_UseTitleCaseWithSpaces()
   {
       // Arrange
       var cycles = CreateTestCycles(count: 1);
       var suggestions = CreateTestSuggestions(cycles);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               cycles, suggestions, outputDir, "TestSolution");

           // Assert
           var headerLine = await File.ReadLinesAsync(filePath).FirstAsync();

           headerLine.Should().Contain("Cycle ID");
           headerLine.Should().Contain("Cycle Size");
           headerLine.Should().Contain("Projects Involved");
           headerLine.Should().Contain("Suggested Break Point");
           headerLine.Should().Contain("Coupling Score");

           // Verify exact order
           var expectedHeader = "Cycle ID,Cycle Size,Projects Involved,Suggested Break Point,Coupling Score";
           headerLine.Should().Be(expectedHeader);
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

4. **Projects Involved Formatting:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_ProjectsInvolved_CommaSeparated()
   {
       // Arrange
       var projects = new List<ProjectNode>
       {
           new("ProjectA", "pathA"),
           new("ProjectB", "pathB"),
           new("ProjectC", "pathC")
       };
       var cycle = new CycleInfo(1, projects);
       var suggestion = CreateSuggestion(cycleId: 1, cycle.Projects[0], cycle.Projects[1], couplingScore: 5);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               new[] { cycle }, new[] { suggestion }, outputDir, "TestSolution");

           // Assert
           using var reader = new StreamReader(filePath);
           using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
           var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

           records[0].ProjectsInvolved.Should().Be("ProjectA, ProjectB, ProjectC");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

5. **Suggested Break Point Formatting:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_SuggestedBreakPoint_FormattedCorrectly()
   {
       // Arrange
       var projects = CreateTestProjects(3);
       var cycle = new CycleInfo(1, projects);
       var suggestion = CreateSuggestion(cycleId: 1, projects[0], projects[1], couplingScore: 5);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               new[] { cycle }, new[] { suggestion }, outputDir, "TestSolution");

           // Assert
           using var reader = new StreamReader(filePath);
           using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
           var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

           // Verify arrow format: "Source → Target"
           records[0].SuggestedBreakPoint.Should().Be($"{projects[0].ProjectName} → {projects[1].ProjectName}");
           records[0].SuggestedBreakPoint.Should().Contain("→");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

6. **Cycle Without Suggestion:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_CycleWithoutSuggestion_ExportsNA()
   {
       // Arrange
       var cycle = CreateCycle(cycleId: 1, projectCount: 2);
       var suggestions = new List<CycleBreakingSuggestion>(); // Empty - no suggestions
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               new[] { cycle }, suggestions, outputDir, "TestSolution");

           // Assert
           using var reader = new StreamReader(filePath);
           using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
           var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

           records[0].SuggestedBreakPoint.Should().Be("N/A");
           records[0].CouplingScore.Should().Be(0);
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

7. **UTF-8 with BOM Validation:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_UTF8WithBOM_OpensInExcel()
   {
       // Arrange
       var cycles = CreateTestCycles(count: 3);
       var suggestions = CreateTestSuggestions(cycles);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               cycles, suggestions, outputDir, "TestSolution");

           // Assert - Check for UTF-8 BOM
           var bytes = await File.ReadAllBytesAsync(filePath);
           bytes.Should().HaveCountGreaterThan(3);

           // UTF-8 BOM: 0xEF, 0xBB, 0xBF
           bytes[0].Should().Be(0xEF);
           bytes[1].Should().Be(0xBB);
           bytes[2].Should().Be(0xBF);
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

8. **Empty Cycles List:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_EmptyCycles_CreatesEmptyCsv()
   {
       // Arrange
       var cycles = new List<CycleInfo>();
       var suggestions = new List<CycleBreakingSuggestion>();
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               cycles, suggestions, outputDir, "TestSolution");

           // Assert
           File.Exists(filePath).Should().BeTrue();
           var lines = await File.ReadAllLinesAsync(filePath);
           lines.Should().HaveCount(1);  // Header only
           lines[0].Should().Contain("Cycle ID");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

9. **Large Cycle with Many Projects:**
   ```csharp
   [Fact]
   public async Task ExportCycleAnalysisAsync_LargeCycle_HandlesLongProjectList()
   {
       // Arrange
       var projects = CreateTestProjects(20);  // 20 projects in one cycle
       var cycle = new CycleInfo(1, projects);
       var suggestion = CreateSuggestion(cycleId: 1, projects[0], projects[1], couplingScore: 5);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var filePath = await _exporter.ExportCycleAnalysisAsync(
               new[] { cycle }, new[] { suggestion }, outputDir, "TestSolution");

           // Assert
           using var reader = new StreamReader(filePath);
           using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
           var records = csv.GetRecords<CycleAnalysisRecord>().ToList();

           // Verify all 20 projects are in the list
           var projectNames = records[0].ProjectsInvolved.Split(", ");
           projectNames.Should().HaveCount(20);

           // Verify RFC 4180 quoting (field should be quoted because it contains commas)
           var rawContent = await File.ReadAllTextAsync(filePath);
           rawContent.Should().Contain("\"Project0, Project1,");  // Quoted field
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

10. **Cancellation Token Support:**
    ```csharp
    [Fact]
    public async Task ExportCycleAnalysisAsync_CancellationToken_CancelsOperation()
    {
        // Arrange
        var cycles = CreateTestCycles(count: 100);
        var suggestions = CreateTestSuggestions(cycles);
        var outputDir = CreateTempDirectory();
        var cts = new CancellationTokenSource();
        cts.Cancel();  // Cancel immediately

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _exporter.ExportCycleAnalysisAsync(
                    cycles, suggestions, outputDir, "TestSolution", cts.Token));
        }
        finally
        {
            CleanupTempDirectory(outputDir);
        }
    }
    ```

**Test Helper Methods:**

```csharp
// Helper: Create test cycles
private IReadOnlyList<CycleInfo> CreateTestCycles(int count)
{
    var cycles = new List<CycleInfo>();
    for (int i = 0; i < count; i++)
    {
        var projects = CreateTestProjects(3 + i % 5);  // Varying cycle sizes
        cycles.Add(new CycleInfo(i + 1, projects));
    }
    return cycles;
}

private CycleInfo CreateCycle(int cycleId, int projectCount)
{
    var projects = CreateTestProjects(projectCount);
    return new CycleInfo(cycleId, projects);
}

private IReadOnlyList<ProjectNode> CreateTestProjects(int count)
{
    var projects = new List<ProjectNode>();
    for (int i = 0; i < count; i++)
    {
        projects.Add(new ProjectNode($"Project{i}", $"path{i}"));
    }
    return projects;
}

// Helper: Create test suggestions
private IReadOnlyList<CycleBreakingSuggestion> CreateTestSuggestions(IReadOnlyList<CycleInfo> cycles)
{
    var suggestions = new List<CycleBreakingSuggestion>();
    foreach (var cycle in cycles)
    {
        if (cycle.Projects.Count >= 2)
        {
            suggestions.Add(CreateSuggestion(
                cycle.CycleId,
                cycle.Projects[0],
                cycle.Projects[1],
                couplingScore: 5));
        }
    }
    return suggestions;
}

private CycleBreakingSuggestion CreateSuggestion(
    int cycleId,
    ProjectNode source,
    ProjectNode target,
    int couplingScore)
{
    var rationale = $"Weakest link in {cycleId}-project cycle, only {couplingScore} method calls";
    return new CycleBreakingSuggestion(cycleId, source, target, couplingScore, cycleId, rationale)
    {
        Rank = cycleId
    };
}

// Reuse from Story 5.5 tests:
private string CreateTempDirectory() { ... }
private void CleanupTempDirectory(string path) { ... }
```

**Test Execution Strategy:**

1. **Run existing Story 5.5 tests first:** Verify no regressions (~15 tests should still pass)
2. **Add Story 5.6 tests incrementally:** One test at a time during implementation
3. **Final validation:** All ~25 tests pass (15 existing + 10 new) before marking story as done

**Test Coverage After Story 5.6:**

```
Epic 5 Test Coverage:
├── TextReportGeneratorTests.cs: 54 tests ✅ (Stories 5.1-5.4, no changes)
└── CsvExporterTests.cs: ~25 tests (15 from Story 5.5 + 10 from Story 5.6)

Total: ~79 tests (comprehensive coverage for Epic 5 through Story 5.6)
```

### Previous Story Intelligence

**From Story 5.5 (Immediate Predecessor) - CSV Export for Extraction Scores:**

Story 5.5 created the CSV export infrastructure. Story 5.6 extends it with cycle analysis export.

**Key Patterns to Reuse from Story 5.5:**

1. **File Naming Pattern:**
   ```csharp
   // Story 5.5 pattern:
   var fileName = $"{sanitizedSolutionName}-extraction-scores.csv";

   // Story 5.6 pattern (SAME STYLE):
   var fileName = $"{sanitizedSolutionName}-cycle-analysis.csv";
   ```

2. **Sanitization Pattern:**
   ```csharp
   // Story 5.5 pattern - reuse existing SanitizeFileName helper:
   private string SanitizeFileName(string fileName)
   {
       var invalidChars = Path.GetInvalidFileNameChars();
       return new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
   }
   ```

3. **CsvHelper Configuration:**
   ```csharp
   // Story 5.5 pattern - use IDENTICAL configuration:
   var config = new CsvConfiguration(CultureInfo.InvariantCulture)
   {
       HasHeaderRecord = true,
       Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
       NewLine = "\r\n"
   };
   ```

4. **Logging Pattern:**
   ```csharp
   // Story 5.5 pattern:
   _logger.LogInformation("Exported {RecordCount} extraction scores to CSV at {FilePath}", records.Count, filePath);

   // Story 5.6 pattern (SAME STYLE):
   _logger.LogInformation("Exported {RecordCount} cycle analysis records to CSV at {FilePath}", records.Count, filePath);
   ```

5. **ClassMap Registration Pattern:**
   ```csharp
   // Story 5.5 pattern:
   csv.Context.RegisterClassMap<ExtractionScoreRecordMap>();

   // Story 5.6 pattern (SAME):
   csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();
   ```

**Implementation Differences from Story 5.5:**

1. **Two Input Lists vs One:**
   ```
   Story 5.5: Single input list (IReadOnlyList<ExtractionScore>)
   Story 5.6: Two input lists (cycles + suggestions) - requires matching
   ```

2. **Data Joining:**
   ```
   Story 5.5: Direct mapping (ExtractionScore → ExtractionScoreRecord)
   Story 5.6: Join operation (CycleInfo + CycleBreakingSuggestion → CycleAnalysisRecord)
   ```

3. **Null Handling:**
   ```
   Story 5.5: CouplingMetric can be null → export "N/A"
   Story 5.6: Suggestion can be missing for cycle → export "N/A" for break point
   ```

4. **String Formatting:**
   ```
   Story 5.5: Decimal formatting with "F1" (1 decimal place)
   Story 5.6: Arrow formatting for break point, comma-separated list for projects
   ```

**From Epic 3 (Cycle Analysis) - Data Source:**

Story 5.6 consumes cycle analysis results from Epic 3:

**Epic 3 Cycle Analysis Flow:**

```
Story 3.1: Detect cycles using Tarjan's SCC algorithm
        ↓
Story 3.2: Calculate cycle statistics (size, participation rates)
        ↓
Story 3.3: Analyze coupling strength (method call counting)
        ↓
Story 3.4: Identify weakest coupling edges within cycles
        ↓
Story 3.5: Generate ranked cycle-breaking recommendations
        ↓
[IReadOnlyList<CycleInfo> cycles]
[IReadOnlyList<CycleBreakingSuggestion> suggestions]
        ↓
Story 5.6: Export cycle analysis to CSV (THIS STORY)
```

**Implementation Takeaways:**

1. ✅ **Extend ICsvExporter interface** - Add ExportCycleAnalysisAsync method
2. ✅ **Reuse CsvHelper configuration** - Same as Story 5.5 for consistency
3. ✅ **Follow Story 5.5's file naming pattern** - Same sanitization and path resolution
4. ✅ **Reuse logging pattern** - Same structure as Story 5.5
5. ✅ **Consume Epic 3's cycle models as-is** - No modifications to cycle detection
6. ✅ **Handle missing suggestions** - Export "N/A" for cycles without break points
7. ✅ **Join cycles and suggestions** - Use CycleId for matching
8. ✅ **Format Projects Involved** - Comma-separated list, quoted per RFC 4180
9. ✅ **Format Break Point** - Arrow notation (Source → Target)

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 5.6 extends the CSV export component created in Story 5.5:

```
Epic Progression:
Epic 2 (Dependency Analysis) → Epic 3 (Cycle Detection) → Epic 4 (Extraction Scoring) → Epic 5 (Reporting)

src/MasDependencyMap.Core/CycleAnalysis/
├── [Stories 3.1-3.7: Cycle detection and analysis] ✅ DONE
├── CycleInfo.cs                     # Consumed by Story 5.6 ← THIS STORY
└── CycleBreakingSuggestion.cs       # Consumed by Story 5.6 ← THIS STORY

src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # Story 5.1 (unchanged)
├── TextReportGenerator.cs           # Stories 5.1-5.4 (unchanged)
├── ICsvExporter.cs                  # Story 5.5 created, Story 5.6 EXTENDS ← THIS STORY
├── CsvExporter.cs                   # Story 5.5 created, Story 5.6 EXTENDS ← THIS STORY
├── ExtractionScoreRecord.cs         # Story 5.5 (unchanged)
├── ExtractionScoreRecordMap.cs      # Story 5.5 (unchanged)
├── CycleAnalysisRecord.cs           # Story 5.6 ← NEW
└── CycleAnalysisRecordMap.cs        # Story 5.6 ← NEW
```

**Epic 5 Reporting Stack After Story 5.6:**

```
Story 5.1 (DONE): Text Report Generator Foundation
    ↓ Creates: ITextReportGenerator, TextReportGenerator
    ↓ Output: {SolutionName}-analysis-report.txt
    ↓
Story 5.2 (DONE): Add Cycle Detection Section
    ↓ Extends: TextReportGenerator
    ↓
Story 5.3 (DONE): Add Extraction Difficulty Section
    ↓ Extends: TextReportGenerator
    ↓
Story 5.4 (DONE): Add Recommendations Section
    ↓ Extends: TextReportGenerator
    ↓
[Complete Text Report] ✅ TEXT REPORTING COMPLETE
    ↓
Story 5.5 (DONE): CSV Export for Extraction Scores
    ↓ Creates: ICsvExporter, CsvExporter
    ↓ Output: {SolutionName}-extraction-scores.csv
    ↓
Story 5.6 (THIS STORY): CSV Export for Cycle Analysis
    ↓ Extends: ICsvExporter, CsvExporter
    ↓ Output: {SolutionName}-cycle-analysis.csv
    ↓
Story 5.7: CSV Export for Dependency Matrix
    ↓ Extends: ICsvExporter, CsvExporter
    ↓ Output: {SolutionName}-dependency-matrix.csv
    ↓
Story 5.8: Spectre.Console CLI Integration
    ↓ Enhances: CLI layer with formatted tables
```

**Cross-Epic Dependencies:**

Story 5.6 completes the integration between Epic 3 and Epic 5:

```
Epic 3 Output:
- CycleInfo objects (detected cycles)
- CycleBreakingSuggestion objects (ranked recommendations)
- Includes: Cycle ID, projects, weak edges, coupling scores

        ↓

Story 5.6 Integration:
- Consumes IReadOnlyList<CycleInfo>
- Consumes IReadOnlyList<CycleBreakingSuggestion>
- Matches cycles with suggestions using CycleId
- Maps to CSV columns with Title Case with Spaces
- Exports RFC 4180 CSV with UTF-8 BOM

        ↓

Epic 5 Output:
- CSV file ready for Excel/Google Sheets
- All cycles with break point suggestions
- Stakeholder-ready cycle tracking data
```

**No Impact on Other Epics:**

```
Epic 1 (Foundation): ✅ No changes
Epic 2 (Dependency Analysis): ✅ No changes
Epic 3 (Cycle Detection): ✅ No changes (pure consumption)
Epic 4 (Extraction Scoring): ✅ No changes
Epic 6 (Future): Not yet started
```

**Epic 5 Roadmap After Story 5.6:**

After Story 5.6, Epic 5 continues with dependency matrix CSV export:

1. ✅ Story 5.1: Text Report Generator foundation (DONE)
2. ✅ Story 5.2: Add Cycle Detection section (DONE)
3. ✅ Story 5.3: Add Extraction Difficulty section (DONE)
4. ✅ Story 5.4: Add Recommendations section (DONE) ← TEXT REPORTING COMPLETE
5. ✅ Story 5.5: CSV export for extraction scores (DONE) ← FIRST CSV EXPORT
6. 🔨 Story 5.6: CSV export for cycle analysis (THIS STORY) ← SECOND CSV EXPORT
7. ⏳ Story 5.7: CSV export for dependency matrix (extends CsvExporter)
8. ⏳ Story 5.8: Spectre.Console table formatting (CLI layer integration)

**After Story 5.6:**
- **Text reporting:** Complete (Stories 5.1-5.4) ✅
- **CSV export:** 2/3 complete (Stories 5.5-5.6)
- **Remaining CSV exports:** Story 5.7 will extend ICsvExporter one more time
- **CLI integration:** Story 5.8 (final story)

**File Creation Summary (Story 5.6):**

```
NEW (2 files):
- src/MasDependencyMap.Core/Reporting/CycleAnalysisRecord.cs
  - POCO for CSV mapping with Title Case properties

- src/MasDependencyMap.Core/Reporting/CycleAnalysisRecordMap.cs
  - CsvHelper ClassMap for column header customization

MODIFIED (3 files):
- src/MasDependencyMap.Core/Reporting/ICsvExporter.cs
  - Add ExportCycleAnalysisAsync method signature

- src/MasDependencyMap.Core/Reporting/CsvExporter.cs
  - Implement ExportCycleAnalysisAsync method
  - Add MapToRecord helper for cycle mapping

- tests/MasDependencyMap.Core.Tests/Reporting/CsvExporterTests.cs
  - Add ~10 comprehensive tests for cycle export functionality

UNCHANGED (Backward compatible):
- All Epic 1-4 components
- All Epic 5 Stories 5.1-5.5 components
- CLI DI registration (no changes needed)
```

**Development Workflow Impact:**

Story 5.6 follows an extension pattern (similar to Stories 5.2-5.4, but for CSV instead of text):

**Story 5.6 Pattern (Interface + Class Extension):**
1. **Design Phase:**
   - Add method to ICsvExporter interface
   - Design CycleAnalysisRecord POCO
   - Design CycleAnalysisRecordMap ClassMap

2. **Implementation Phase:**
   - Modify ICsvExporter.cs to add method signature with XML docs
   - Modify CsvExporter.cs to implement method
   - Create CycleAnalysisRecord.cs POCO
   - Create CycleAnalysisRecordMap.cs ClassMap

3. **Testing Phase:**
   - Extend CsvExporterTests.cs with new tests
   - Add helper methods for cycle test data creation
   - Add ~10 tests covering all scenarios

4. **Validation Phase:**
   - Run all tests (existing + new)
   - Verify CSV opens correctly in Excel
   - Verify UTF-8 BOM detection
   - Verify RFC 4180 quoting for Projects Involved
   - Manual test with real cycle data

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-5-comprehensive-reporting-and-data-export.md, Story 5.6 (lines 91-106)]
- Story requirements: CSV export with CsvHelper, cycle details, break point suggestions, RFC 4180 format, UTF-8 with BOM

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces (Reporting), async patterns, file-scoped namespaces, XML documentation
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 114-119)]
- Logging: Structured logging with named placeholders, ILogger<T> injection
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 132-137)]
- CsvHelper: CultureInfo.InvariantCulture, Title Case with Spaces headers, UTF-8 with BOM, POCO classes

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\5-5-implement-csv-export-for-extraction-difficulty-scores.md]
- Story 5.5 implementation: Created ICsvExporter interface and CsvExporter class
- CsvHelper configuration: UTF-8 with BOM, InvariantCulture, CRLF line endings
- ClassMap pattern: Register with csv.Context.RegisterClassMap<T>()
- File naming: {sanitizedSolutionName}-{export-type}.csv
- Logging pattern: Record count + file path + elapsed time

**Epic 3 Integration:**
- [Source: D:\work\masDependencyMap\src\MasDependencyMap.Core\CycleAnalysis\CycleInfo.cs (lines 1-49)]
- CycleInfo model: CycleId, Projects list, CycleSize, WeakCouplingEdges, WeakCouplingScore
- [Source: D:\work\masDependencyMap\src\MasDependencyMap\Core\CycleAnalysis\CycleBreakingSuggestion.cs (lines 1-95)]
- CycleBreakingSuggestion model: CycleId, SourceProject, TargetProject, CouplingScore, CycleSize, Rationale, Rank
- Natural ordering: Lowest coupling first, then largest cycle size

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Most recent: Story 5.4 code review fixes (b927a41)
- Pattern: Story implementation → code review → fixes → status update
- Recent work: All text reporting complete (Stories 5.1-5.4), CSV export started (Story 5.5)

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-5-20250929

### Debug Log References

### Completion Notes List

✅ **Story 5.6 Implementation Complete**

**Implementation Summary:**
- Extended ICsvExporter interface with ExportCycleAnalysisAsync method
- Created CycleAnalysisRecord POCO for CSV mapping with Title Case column headers
- Created CycleAnalysisRecordMap ClassMap for CsvHelper header customization
- Implemented ExportCycleAnalysisAsync in CsvExporter class with:
  - Cycle-suggestion matching using dictionary lookup by CycleId
  - Projects Involved formatting as comma-separated list with RFC 4180 quoting
  - Suggested Break Point formatting with Unicode arrow (→)
  - "N/A" handling for cycles without suggestions
  - Sorting by CycleId ascending for natural ordering
  - UTF-8 with BOM encoding for Excel compatibility

**Testing Results:**
- Added 11 comprehensive unit tests covering all scenarios (including multi-suggestion edge case)
- All 26 CsvExporter tests pass (15 existing + 11 new)
- Test coverage includes: basic export, sorting, headers, formatting, edge cases, encoding, cancellation, multi-suggestion selection
- Verified RFC 4180 compliance with quoting for comma-separated project lists
- Verified UTF-8 BOM for Excel compatibility

**Architecture Compliance:**
- File-scoped namespaces (C# 10+)
- Async all the way with ConfigureAwait(false)
- Structured logging with named placeholders
- XML documentation for public APIs
- Consistent with Story 5.5 patterns

**Integration Points:**
- Consumes Epic 3 cycle analysis models (CycleInfo, CycleBreakingSuggestion)
- No changes to Epic 3 components - pure consumption
- Extends existing CSV export infrastructure from Story 5.5

**Code Review Fixes Applied (2026-01-27):**
1. ✅ Fixed XML documentation in ICsvExporter.cs - Corrected exception types (ArgumentException for whitespace validation)
2. ✅ Added missing test for multi-suggestion edge case - Validates lowest rank selection when multiple suggestions exist for same cycle
3. ✅ Fixed test helper CreateSuggestion - Now passes correct cycleSize instead of cycleId to match CycleBreakingSuggestion constructor
4. ✅ All 26 tests pass after fixes

**Date:** 2026-01-27

### File List

**New Files:**
- src/MasDependencyMap.Core/Reporting/CycleAnalysisRecord.cs
- src/MasDependencyMap.Core/Reporting/CycleAnalysisRecordMap.cs

**Modified Files:**
- src/MasDependencyMap.Core/Reporting/ICsvExporter.cs
- src/MasDependencyMap.Core/Reporting/CsvExporter.cs
- tests/MasDependencyMap.Core.Tests/Reporting/CsvExporterTests.cs
