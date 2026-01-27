# Story 5.2: Add Cycle Detection Section to Text Reports

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want cycle detection results included in text reports,
So that I can see all circular dependencies and statistics in one place.

## Acceptance Criteria

**Given** Cycles have been detected
**When** Text report is generated
**Then** Cycle Detection section includes: total circular dependency chains count
**And** Projects in cycles count and percentage (e.g., "45 projects (61.6%)")
**And** Largest cycle size is reported
**And** Each cycle is listed with the projects involved
**And** Report is formatted clearly: "Circular Dependency Chains: 12"

**Given** No cycles are detected
**When** Text report is generated
**Then** Cycle Detection section shows "No circular dependencies detected"

## Tasks / Subtasks

- [x] Update TextReportGenerator.GenerateAsync to use cycles parameter (AC: Include cycle detection section)
  - [x] Check if cycles parameter is not null and has elements
  - [x] If cycles provided: Call AppendCycleDetection(report, cycles) helper method
  - [x] If no cycles or empty list: Call AppendCycleDetection(report, null) to show "No cycles" message
  - [x] Insert cycle section after Dependency Overview section (before future Story 5.3 extraction scores)

- [x] Implement AppendCycleDetection private method (AC: Total circular dependency chains count)
  - [x] Accept StringBuilder report and IReadOnlyList<CycleInfo>? cycles parameters
  - [x] Add section header: "CYCLE DETECTION" with separator (80 '=' characters)
  - [x] Handle null/empty cycles: Show "No circular dependencies detected" message
  - [x] For non-empty cycles: Calculate total cycles count from cycles.Count
  - [x] Format: "Circular Dependency Chains: {count:N0}" with thousands separator

- [x] Calculate and display cycle participation statistics (AC: Projects in cycles count and percentage)
  - [x] Extract unique projects from all cycles using cycles.SelectMany(c => c.Projects).Select(p => p.ProjectName).Distinct()
  - [x] Count total unique projects participating in cycles
  - [x] Get total projects from graph.VertexCount (available in class scope via _totalProjects field)
  - [x] Calculate participation percentage: (projectsInCycles / totalProjects) * 100.0
  - [x] Format: "Projects in Cycles: {count:N0} ({percentage:F1}%)" - using F1 for one decimal place
  - [x] Handle edge case: If totalProjects is 0, show 0.0% to avoid division by zero

- [x] Display largest cycle size (AC: Largest cycle size is reported)
  - [x] Calculate largest cycle: cycles.Max(c => c.Projects.Count)
  - [x] Format: "Largest Cycle Size: {size} projects"
  - [x] Add blank line for spacing before cycle list

- [x] List each cycle with projects involved (AC: Each cycle is listed)
  - [x] Add subheader: "Detailed Cycle Information:" with separator
  - [x] Iterate through cycles with index: for (int i = 0; i < cycles.Count; i++)
  - [x] Format each cycle: "Cycle {i+1}: {cycleSize} projects"
  - [x] Indent project list with "  - {projectName}" for each project in cycle
  - [x] Use Projects property from CycleInfo (IReadOnlyList<ProjectNode>)
  - [x] Add blank line between cycles for readability

- [x] Add comprehensive unit tests (AC: Test coverage)
  - [x] Create tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs updates
  - [x] Test: GenerateAsync_WithCycles_IncludesCycleDetectionSection (verify section exists)
  - [x] Test: GenerateAsync_WithCycles_ShowsCorrectCycleCount (verify count display)
  - [x] Test: GenerateAsync_WithCycles_ShowsProjectParticipationPercentage (verify percentage calc)
  - [x] Test: GenerateAsync_WithCycles_ShowsLargestCycleSize (verify max size)
  - [x] Test: GenerateAsync_WithCycles_ListsAllCyclesWithProjects (verify detailed list)
  - [x] Test: GenerateAsync_WithNullCycles_DoesNotIncludeCycleSection (null cycles parameter)
  - [x] Test: GenerateAsync_WithEmptyCycles_ShowsNoCyclesMessage (empty list)
  - [x] Test: GenerateAsync_WithMultipleCycles_FormatsCorrectly (formatting validation)
  - [x] Test: GenerateAsync_CycleSection_AppearsAfterDependencyOverview (section order)
  - [x] Use helper methods: CreateTestCycles() helper method

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Existing namespace: MasDependencyMap.Core.Reporting (no new namespace)
  - [x] Private method: AppendCycleDetection (helper, not public API)
  - [x] Consistent formatting: Use ReportWidth constant (80 characters)
  - [x] Structured logging: Add log message for cycle section generation
  - [x] Performance: Single-pass LINQ queries for statistics (no nested loops)
  - [x] Test organization: Add tests to existing TextReportGeneratorTests.cs

- [x] Verify integration with Epic 3 CycleInfo model (AC: Consume cycle data correctly)
  - [x] Use CycleInfo.Projects property (IReadOnlyList<ProjectNode>)
  - [x] Verify CycleInfo namespace import: using MasDependencyMap.Core.CycleAnalysis;
  - [x] Ensure CycleInfo contains all necessary data (already defined in Epic 3)
  - [x] No changes to CycleInfo model needed (Story 5.2 is pure consumer)

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 5.2 Extends Story 5.1 Foundation:**

This story **extends** the existing `TextReportGenerator` created in Story 5.1. **NO new classes** are created - only the implementation of `TextReportGenerator.cs` is modified.

**Epic 5 Reporting Stack Progress:**
- ✅ Story 5.1: Text Report Generator with Summary Statistics (FOUNDATION - DONE)
- 🔨 Story 5.2: Add Cycle Detection section to text reports (THIS STORY - EXTENDS)
- ⏳ Story 5.3: Add Extraction Difficulty Scoring section to text reports (FUTURE - EXTENDS)
- ⏳ Story 5.4: Add Cycle-Breaking Recommendations to text reports (FUTURE - EXTENDS)
- ⏳ Story 5.5-5.7: CSV export capabilities (NEW COMPONENTS)
- ⏳ Story 5.8: Spectre.Console table formatting enhancements (CLI INTEGRATION)

**Story 5.2 Unique Characteristics:**

1. **Pure Extension of Existing Component:**
   - Story 5.1: Created ITextReportGenerator interface and TextReportGenerator implementation
   - Story 5.2: Modifies ONLY TextReportGenerator.GenerateAsync implementation
   - **No new files created** - Only modifies src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs
   - **No new tests file** - Extends tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs

2. **Leverages Story 5.1's Forward-Compatible Interface:**
   - Story 5.1 interface included optional `cycles` parameter for exactly this purpose:
   ```csharp
   Task<string> GenerateAsync(
       DependencyGraph graph,
       string outputDirectory,
       string solutionName,
       IReadOnlyList<CycleInfo>? cycles = null,          // ← Story 5.2 uses this
       IReadOnlyList<ExtractionScore>? extractionScores = null,
       IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
       CancellationToken cancellationToken = default);
   ```
   - **Interface unchanged** - Story 5.1 designed for extensibility
   - **Implementation change only** - Activate the cycles parameter handling

3. **Consumes Epic 3 Cycle Detection Results:**
   - Epic 3 (Stories 3.1-3.7): All done, CycleInfo model exists and tested
   - Story 3.2: Created CycleStatistics record with TotalCycles, TotalProjectsInCycles, ProjectParticipationRate
   - Story 3.6: Enhanced dot visualization with cycle highlighting (parallel effort)
   - **Story 5.2 integration point:** Consumes CycleInfo from Epic 3's `StronglyConnectedComponentsAlgorithm`
   - **No Epic 3 changes needed** - Pure consumption of existing data

4. **Section Insertion Strategy:**
   ```
   Report Structure After Story 5.2:

   ================================================================================
   MasDependencyMap Analysis Report
   ================================================================================
   [Header: Solution name, date, total projects] ← Story 5.1

   DEPENDENCY OVERVIEW                           ← Story 5.1
   [Total references, framework/custom split]    ← Story 5.1

   CYCLE DETECTION                               ← Story 5.2 NEW
   [Circular dependency chains, participation %] ← Story 5.2 NEW
   [Largest cycle, detailed cycle list]          ← Story 5.2 NEW

   [Future: EXTRACTION DIFFICULTY SCORES]        ← Story 5.3
   [Future: CYCLE-BREAKING RECOMMENDATIONS]      ← Story 5.4
   ================================================================================
   ```
   - **Insert after:** Dependency Overview section
   - **Insert before:** Future extraction scores section (Story 5.3)

5. **Backward Compatibility Requirement:**
   - If `cycles` parameter is null: Report still generates with Story 5.1 sections only
   - If `cycles` parameter is empty list: Show "No circular dependencies detected"
   - **No breaking changes** - Existing callers without cycles parameter continue to work
   - **Graceful degradation** - Report adapts to available data

🚨 **CRITICAL - CycleInfo Model from Epic 3:**

Story 5.2 consumes the `CycleInfo` model created in Epic 3 (Cycle Detection). Understanding this model is critical for correct implementation.

**CycleInfo Definition (from Epic 3):**

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Represents a single strongly connected component (circular dependency).
/// </summary>
public sealed record CycleInfo(
    int CycleId,
    IReadOnlyList<string> ProjectsInCycle);
```

**Important Properties:**
- `CycleId`: Unique identifier for the cycle (1-based index)
- `ProjectsInCycle`: List of project names participating in this cycle
- **Count of projects:** Use `ProjectsInCycle.Count` to get cycle size

**CycleStatistics Record (from Epic 3, Story 3.2):**

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Summary statistics about circular dependencies in the dependency graph.
/// </summary>
public sealed record CycleStatistics(
    int TotalCycles,
    int TotalProjectsInCycles,
    double ProjectParticipationRate,
    int LargestCycleSize);
```

**Story 5.2 Calculation Strategy:**

Story 5.2 receives `IReadOnlyList<CycleInfo>` but needs to calculate statistics similar to CycleStatistics. Two approaches:

**Approach 1: Recalculate from CycleInfo list (RECOMMENDED for Story 5.2):**
```csharp
private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo>? cycles)
{
    // Calculate statistics from cycles list
    var totalCycles = cycles.Count;
    var uniqueProjects = cycles.SelectMany(c => c.ProjectsInCycle).Distinct().Count();
    var largestCycleSize = cycles.Max(c => c.ProjectsInCycle.Count);
    var participationPercentage = (_totalProjects > 0) ? (uniqueProjects * 100.0 / _totalProjects) : 0;

    // Format and append to report
    report.AppendLine($"Circular Dependency Chains: {totalCycles:N0}");
    report.AppendLine($"Projects in Cycles: {uniqueProjects:N0} ({participationPercentage:F1}%)");
    report.AppendLine($"Largest Cycle Size: {largestCycleSize} projects");
    // ...
}
```

**Approach 2: Accept CycleStatistics parameter (NOT for Story 5.2):**
- Would require interface change to add `CycleStatistics? statistics` parameter
- Breaks Story 5.1's forward-compatible design
- **Rejected:** Story 5.2 should work within Story 5.1's interface design

**Why Recalculate?**
1. **Interface compatibility:** Story 5.1 designed interface with `IReadOnlyList<CycleInfo>`, not CycleStatistics
2. **Simplicity:** Caller provides raw cycle data, report generator calculates presentation statistics
3. **Consistency:** Stories 5.3-5.4 will use same pattern (raw data in, formatted report out)
4. **Performance:** Statistics calculation is O(n) single-pass, acceptable for reporting

**Total Projects Count Source:**

Problem: AppendCycleDetection needs _totalProjects to calculate participation percentage, but it's a private helper method.

**Solution: Store graph.VertexCount in class field during GenerateAsync:**

```csharp
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    // ...
    CancellationToken cancellationToken = default)
{
    // Validation
    ArgumentNullException.ThrowIfNull(graph);
    // ...

    _logger.LogInformation("Generating text report for solution {SolutionName}", solutionName);

    // Store total projects for use in helper methods
    _totalProjects = graph.VertexCount;  // NEW: Store for cycle participation calculation

    var startTime = DateTime.UtcNow;
    var report = new StringBuilder(capacity: 4096);

    AppendHeader(report, solutionName, graph);
    AppendDependencyOverview(report, graph);

    // NEW: Story 5.2 - Add cycle detection section
    if (cycles != null)
    {
        AppendCycleDetection(report, cycles);
    }

    // Future: Story 5.3, 5.4 sections
    // ...
}

// Add class field:
private int _totalProjects;  // NEW: Used for cycle participation percentage calculation
```

**Alternative Approaches (Rejected):**
- Pass graph to AppendCycleDetection: Unnecessary coupling, only need vertex count
- Pass total projects as parameter: Creates parameter bloat for helper methods
- **Chosen approach:** Store in class field during GenerateAsync, use in helpers

🚨 **CRITICAL - Report Formatting Pattern (Story 5.1 Consistency):**

Story 5.2 must match Story 5.1's formatting conventions for visual consistency.

**Story 5.1 Section Format (Reference):**

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

**Story 5.2 Section Format (Target):**

```
CYCLE DETECTION
================================================================================

Circular Dependency Chains: 12
Projects in Cycles: 45 (61.6%)
Largest Cycle Size: 8 projects

Detailed Cycle Information:
--------------------------------------------------------------------------------
Cycle 1: 5 projects
  - PaymentService
  - OrderManagement
  - CustomerService
  - NotificationService
  - InventoryTracking

Cycle 2: 3 projects
  - UserAuthentication
  - ProfileManagement
  - SessionManager

[... more cycles ...]

================================================================================
```

**Formatting Rules (from Story 5.1):**

1. **Section Headers:**
   - ALL CAPS section name (e.g., "CYCLE DETECTION")
   - 80 '=' characters separator line
   - Blank line after separator

2. **Statistics Format:**
   - One statistic per line
   - Use `{value:N0}` for integers (thousands separator)
   - Use `{percentage:F1}%` for percentages (one decimal place)
   - No indentation for main statistics

3. **Subsection Headers:**
   - Title Case with colon (e.g., "Detailed Cycle Information:")
   - 80 '-' characters separator line (NOT '=', to distinguish from main sections)
   - Blank line after separator

4. **Lists:**
   - Two-space indentation for list items ("  - Item")
   - Blank line between groups (cycles) for readability
   - Consistent indentation throughout

5. **Section Closing:**
   - Blank line before section separator
   - 80 '=' characters separator
   - Blank line after separator (ready for next section)

**ReportWidth Constant (from Story 5.1):**

```csharp
private const int ReportWidth = 80;  // Standard terminal width for formatting

// Usage in Story 5.2:
report.AppendLine(new string('=', ReportWidth));  // Main separator
report.AppendLine(new string('-', ReportWidth));  // Subsection separator
```

**Number Formatting Examples:**

```csharp
// Integer with thousands separator
report.AppendLine($"Circular Dependency Chains: {totalCycles:N0}");
// Output: "Circular Dependency Chains: 12" or "Circular Dependency Chains: 1,234"

// Percentage with one decimal place
report.AppendLine($"Projects in Cycles: {uniqueProjects:N0} ({participationPercentage:F1}%)");
// Output: "Projects in Cycles: 45 (61.6%)" or "Projects in Cycles: 245 (23.4%)"

// No formatting for size (simple integer)
report.AppendLine($"Largest Cycle Size: {largestCycleSize} projects");
// Output: "Largest Cycle Size: 8 projects"
```

**Cycle List Format Details:**

```csharp
// Subsection header
report.AppendLine("Detailed Cycle Information:");
report.AppendLine(new string('-', ReportWidth));
report.AppendLine();

// Iterate through cycles
for (int i = 0; i < cycles.Count; i++)
{
    var cycle = cycles[i];
    report.AppendLine($"Cycle {i + 1}: {cycle.ProjectsInCycle.Count} projects");

    // List projects with indentation
    foreach (var projectName in cycle.ProjectsInCycle)
    {
        report.AppendLine($"  - {projectName}");
    }

    // Blank line between cycles (but not after last cycle)
    if (i < cycles.Count - 1)
    {
        report.AppendLine();
    }
}

// Section closing
report.AppendLine();
report.AppendLine(new string('=', ReportWidth));
report.AppendLine();
```

🚨 **CRITICAL - No Cycles Scenario Handling:**

Story 5.2 Acceptance Criteria explicitly requires:
> **Given** No cycles are detected
> **When** Text report is generated
> **Then** Cycle Detection section shows "No circular dependencies detected"

**Two Scenarios to Handle:**

1. **Cycles parameter is null:**
   - Caller didn't detect cycles or didn't pass data
   - **Strategy:** Don't call AppendCycleDetection at all (skip section)
   - **Reasoning:** If caller doesn't provide cycle data, don't include section
   - **Backward compatibility:** Story 5.1 callers without cycles parameter continue to work

2. **Cycles parameter is empty list (Count == 0):**
   - Caller detected cycles, result is zero cycles
   - **Strategy:** Call AppendCycleDetection, show "No circular dependencies detected" message
   - **Reasoning:** Caller explicitly ran cycle detection and found none

**Implementation Pattern:**

```csharp
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    // ...
    CancellationToken cancellationToken = default)
{
    // ... header, dependency overview ...

    // Story 5.2: Cycle detection section
    if (cycles != null)  // Check for null
    {
        AppendCycleDetection(report, cycles);  // Handle empty list inside method
    }

    // Future: Story 5.3, 5.4 sections
    // ...
}

private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo> cycles)
{
    report.AppendLine("CYCLE DETECTION");
    report.AppendLine(new string('=', ReportWidth));
    report.AppendLine();

    // Handle empty cycles list
    if (cycles.Count == 0)
    {
        report.AppendLine("No circular dependencies detected");
        report.AppendLine();
        report.AppendLine(new string('=', ReportWidth));
        report.AppendLine();
        return;  // Early exit, don't show statistics
    }

    // Normal flow: cycles exist
    // ... calculate and display statistics ...
}
```

**No Cycles Report Format:**

```
CYCLE DETECTION
================================================================================

No circular dependencies detected

================================================================================
```

**Rationale:**
- Simple, clear message
- Maintains section structure (header, content, separator)
- No confusing "0 cycles" or "0.0%" statistics
- Stakeholder-friendly: Good news is immediately obvious

🚨 **CRITICAL - Performance Considerations:**

Story 5.1 Acceptance Criteria: "File is generated within 10 seconds regardless of solution size"

Story 5.2 adds cycle detection section - must maintain this performance guarantee.

**Performance Analysis:**

1. **Story 5.1 Baseline:**
   - Header generation: O(1)
   - Dependency overview: O(edges) - single-pass graph.Edges enumeration
   - File write: O(report size)
   - **Total:** O(edges) - typically completes in <1 second for 5000 edges

2. **Story 5.2 Additions:**
   - Unique projects calculation: `cycles.SelectMany(c => c.ProjectsInCycle).Distinct().Count()`
     - O(cycles * avg_cycle_size + unique_projects) - typically O(projects)
   - Largest cycle: `cycles.Max(c => c.ProjectsInCycle.Count)`
     - O(cycles) - single-pass enumeration
   - Cycle listing: O(cycles * avg_cycle_size) - iterate through all projects in all cycles
   - **Total Story 5.2:** O(projects + cycles * avg_cycle_size)

3. **Combined Performance:**
   - Story 5.1 + Story 5.2: O(edges + projects + cycles * avg_cycle_size)
   - For typical legacy solution (from NFR1-NFR2):
     - Projects: 50-400
     - Edges: 1000-5000
     - Cycles: 0-50
     - Avg cycle size: 3-10 projects
   - **Worst case:** 400 projects + 5000 edges + 50 cycles * 10 projects = ~6000 operations
   - **Expected time:** <2 seconds (well within 10-second limit)

4. **Optimization Strategies (Already Applied):**
   - ✅ Single-pass LINQ queries (no nested loops)
   - ✅ StringBuilder with pre-allocated capacity (4096 bytes)
   - ✅ No redundant graph traversals
   - ✅ Efficient Distinct() using HashSet internally

**Memory Considerations:**

```csharp
// Efficient: Single-pass enumeration with immediate disposal
var uniqueProjects = cycles
    .SelectMany(c => c.ProjectsInCycle)
    .Distinct()
    .Count();  // ← Executes query, disposes intermediate collections

// Inefficient (AVOID): Materializing intermediate collections
var allProjects = cycles.SelectMany(c => c.ProjectsInCycle).ToList();  // ← Creates list
var uniqueProjects = allProjects.Distinct().ToList();  // ← Creates another list
var count = uniqueProjects.Count;  // ← Only need count, lists are waste
```

**Logging Performance:**

```csharp
// Add logging for Story 5.2 section generation
_logger.LogDebug("Appending cycle detection section: {CycleCount} cycles found", cycles.Count);

// Existing Story 5.1 performance logging (unchanged):
var elapsed = DateTime.UtcNow - startTime;
_logger.LogInformation(
    "Generated text report at {FilePath} in {ElapsedMs}ms",
    filePath,
    elapsed.TotalMilliseconds);
```

**No Performance Concerns for Story 5.2:** Cycle detection section adds negligible overhead (<100ms for typical solutions).

### Technical Requirements

**Modified Component: TextReportGenerator.cs (from Story 5.1)**

Story 5.2 modifies the existing TextReportGenerator implementation created in Story 5.1:

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # UNCHANGED (interface already supports cycles parameter)
└── TextReportGenerator.cs           # MODIFIED: Add AppendCycleDetection method, use cycles parameter

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs      # MODIFIED: Add ~7-9 new tests for cycle section
```

**New Dependencies (Epic 3 Integration):**

Story 5.2 adds explicit dependency on Epic 3's CycleAnalysis namespace:

```csharp
using MasDependencyMap.Core.CycleAnalysis;  // For CycleInfo model
```

**Existing Dependencies (from Story 5.1):**

All Story 5.1 dependencies remain:

```csharp
using System.Text;                              // For StringBuilder
using MasDependencyMap.Core.DependencyAnalysis; // For DependencyGraph
using MasDependencyMap.Core.ExtractionScoring;  // For future Story 5.3
using Microsoft.Extensions.Logging;             // For ILogger<T>
```

**No New NuGet Packages Required:**

All dependencies already installed from previous epics:
- ✅ Epic 1: Microsoft.Extensions.Logging.Console
- ✅ Epic 2: QuikGraph (DependencyGraph)
- ✅ Epic 3: CycleAnalysis components (CycleInfo, CycleStatistics)
- ✅ Built-in: System.Text.StringBuilder, System.Linq

**DI Registration:**

No changes to DI registration - TextReportGenerator already registered in Story 5.1:

```csharp
// CLI Program.cs (unchanged from Story 5.1)
services.AddSingleton<ITextReportGenerator, TextReportGenerator>();
```

**Class Field Addition:**

Add private field to store total projects count for cycle participation calculation:

```csharp
public sealed class TextReportGenerator : ITextReportGenerator
{
    private readonly ILogger<TextReportGenerator> _logger;
    private const int ReportWidth = 80;  // Existing from Story 5.1
    private int _totalProjects;          // NEW: Story 5.2 - for cycle participation %

    // ... rest of implementation
}
```

### Architecture Compliance

**Namespace Structure (Unchanged from Story 5.1):**

Story 5.2 works within the existing `MasDependencyMap.Core.Reporting` namespace:

```csharp
namespace MasDependencyMap.Core.Reporting;  // File-scoped namespace (C# 10+)

using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.CycleAnalysis;  // NEW: Story 5.2 dependency
using Microsoft.Extensions.Logging;
```

**Private Helper Method Pattern (Story 5.1 Consistency):**

Story 5.2 follows Story 5.1's pattern of private helper methods for section generation:

```csharp
// Story 5.1 pattern:
private void AppendHeader(StringBuilder report, string solutionName, DependencyGraph graph)
private void AppendDependencyOverview(StringBuilder report, DependencyGraph graph)

// Story 5.2 pattern (SAME STYLE):
private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo> cycles)

// Future:
// Story 5.3: private void AppendExtractionScores(...)
// Story 5.4: private void AppendRecommendations(...)
```

**Method Organization:**

```csharp
public sealed class TextReportGenerator : ITextReportGenerator
{
    // Fields
    private readonly ILogger<TextReportGenerator> _logger;
    private const int ReportWidth = 80;
    private int _totalProjects;

    // Constructor
    public TextReportGenerator(ILogger<TextReportGenerator> logger) { }

    // Public API (interface implementation)
    public async Task<string> GenerateAsync(...) { }

    // Private helper methods (grouped by section)
    private void AppendHeader(...)                  // Story 5.1
    private void AppendDependencyOverview(...)      // Story 5.1
    private void AppendCycleDetection(...)          // Story 5.2 NEW
    // Future: AppendExtractionScores(...)          // Story 5.3
    // Future: AppendRecommendations(...)            // Story 5.4

    // Private utility methods
    private bool IsFrameworkReference(...)          // Story 5.1
    private int CountCrossSolutionReferences(...)   // Story 5.1
    private string SanitizeFileName(...)            // Story 5.1
}
```

**Structured Logging (Required):**

Add logging for cycle detection section generation:

```csharp
// Inside AppendCycleDetection method:
if (cycles.Count == 0)
{
    _logger.LogDebug("No cycles detected, showing no-cycles message");
}
else
{
    _logger.LogDebug("Appending cycle detection section: {CycleCount} cycles, {UniqueProjects} unique projects",
        cycles.Count,
        uniqueProjects);
}
```

**No XML Documentation Changes:**

Since Story 5.2 only adds a private helper method, no XML documentation changes required:
- ITextReportGenerator interface: Already documented in Story 5.1 with cycles parameter
- TextReportGenerator.GenerateAsync: Already has `<inheritdoc />`
- Private methods: XML documentation not required by project-context.md

**Performance Logging (Unchanged):**

Story 5.1's existing performance logging captures Story 5.2's additions:

```csharp
// Existing Story 5.1 logging (captures total time including Story 5.2):
var elapsed = DateTime.UtcNow - startTime;
_logger.LogInformation(
    "Generated text report at {FilePath} in {ElapsedMs}ms",
    filePath,
    elapsed.TotalMilliseconds);
```

### Library/Framework Requirements

**No New Libraries Required:**

Story 5.2 uses only existing dependencies from previous epics:

**From Epic 1 (Foundation):**
- ✅ Microsoft.Extensions.Logging.Console - Structured logging
- ✅ Microsoft.Extensions.DependencyInjection - DI container (no changes)

**From Epic 2 (Dependency Analysis):**
- ✅ QuikGraph v2.5.0 - DependencyGraph data structure

**From Epic 3 (Cycle Detection):**
- ✅ CycleInfo model - Already created in Epic 3, Story 3.1
- ✅ CycleStatistics record - Reference for statistics calculation pattern

**Built-in .NET Libraries:**
- ✅ System.Text.StringBuilder - Efficient string building
- ✅ System.Linq - LINQ queries for statistics (SelectMany, Distinct, Max, Count)
- ✅ System.Collections.Generic - IReadOnlyList<T>

**Epic 3 CycleInfo Model (Existing):**

Story 5.2 consumes the CycleInfo model from Epic 3 without modifications:

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

// Created in Epic 3, Story 3.1 (Tarjan's SCC algorithm)
public sealed record CycleInfo(
    int CycleId,
    IReadOnlyList<string> ProjectsInCycle);
```

**No Changes to Epic 3 Components:**

Story 5.2 is a pure consumer - no changes to:
- ✅ CycleDetector.cs (Epic 3, Story 3.1)
- ✅ CycleStatistics.cs (Epic 3, Story 3.2)
- ✅ CouplingAnalyzer.cs (Epic 3, Story 3.3-3.5)
- ✅ DotVisualizer.cs (Epic 3, Story 3.6-3.7)

**Story 5.2 Integration Point:**

```
Epic 3: CycleDetector.DetectCyclesAsync()
        ↓
[IReadOnlyList<CycleInfo>]
        ↓
Story 5.2: TextReportGenerator.GenerateAsync(cycles: ...)
        ↓
[Text Report with Cycle Detection section]
```

### File Structure Requirements

**Files to Modify (Only 2 Files):**

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # UNCHANGED (interface already supports cycles parameter)
└── TextReportGenerator.cs           # MODIFY: Add AppendCycleDetection method, activate cycles parameter

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs      # MODIFY: Add ~7-9 new tests for cycle detection section
```

**No New Files Created:**

Story 5.2 is a pure extension of Story 5.1 - no new classes or files.

**Files to Modify Outside Core:**

```
_bmad-output/implementation-artifacts/sprint-status.yaml  # UPDATE: Story 5-2 status backlog → ready-for-dev
_bmad-output/implementation-artifacts/5-2-add-cycle-detection-section-to-text-reports.md  # CREATE: This story file
```

**No Changes to Other Components:**

```
UNCHANGED (Epic 1-4):
- All Epic 1 foundation components
- All Epic 2 dependency analysis components
- All Epic 3 cycle detection components ✅ (consumed as-is)
- All Epic 4 extraction scoring components
- CLI Program.cs (DI registration unchanged)

UNCHANGED (Epic 5):
- Story 5.1's ITextReportGenerator interface
- Story 5.1's DI registration
```

**Modification Details:**

1. **TextReportGenerator.cs Changes:**
   - Add private field: `private int _totalProjects;`
   - In GenerateAsync: Store `_totalProjects = graph.VertexCount;`
   - In GenerateAsync: Add section call after AppendDependencyOverview:
     ```csharp
     if (cycles != null)
     {
         AppendCycleDetection(report, cycles);
     }
     ```
   - Add new private method: `private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo> cycles)`
   - **Estimated lines added:** ~50-60 lines (method implementation + logging)

2. **TextReportGeneratorTests.cs Changes:**
   - Add helper method: `private IReadOnlyList<CycleInfo> CreateTestCycles(...)`
   - Add ~7-9 new test methods for cycle section validation
   - **Estimated lines added:** ~150-200 lines (tests + helpers)

**Impact on Story 5.1 Code:**

Minimal impact - only the GenerateAsync method changes:

```csharp
// Story 5.1 implementation (before Story 5.2):
public async Task<string> GenerateAsync(...)
{
    // ... validation, logging, setup ...

    var report = new StringBuilder(capacity: 4096);

    AppendHeader(report, solutionName, graph);
    AppendDependencyOverview(report, graph);

    // Write to file
    // ... file write, performance logging ...
}

// Story 5.2 implementation (after):
public async Task<string> GenerateAsync(...)
{
    // ... validation, logging, setup ...

    _totalProjects = graph.VertexCount;  // NEW: Store for cycle participation %
    var report = new StringBuilder(capacity: 4096);

    AppendHeader(report, solutionName, graph);
    AppendDependencyOverview(report, graph);

    // NEW: Story 5.2 - Cycle detection section
    if (cycles != null)
    {
        AppendCycleDetection(report, cycles);
    }

    // Future: Story 5.3, 5.4 sections

    // Write to file
    // ... file write, performance logging ...
}
```

**Lines Changed in Existing Code:** ~5 lines (field add + section call)
**Lines Added (New Method):** ~50-60 lines
**Total Story 5.2 Code Changes:** ~55-65 lines in TextReportGenerator.cs

### Testing Requirements

**Test Strategy: Extend Existing Test Class**

Story 5.2 adds ~7-9 new tests to the existing `TextReportGeneratorTests.cs` created in Story 5.1:

```
tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs
├── [Existing Story 5.1 tests: 15 tests] ✅
└── [New Story 5.2 tests: ~7-9 tests]    ← THIS STORY
```

**Total Tests After Story 5.2:** ~22-24 tests in TextReportGeneratorTests.cs

**New Test Coverage (Story 5.2):**

1. **Basic Cycle Section Inclusion:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithCycles_IncludesCycleDetectionSection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var cycles = CreateTestCycles(cycleCount: 3, avgSize: 4);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("CYCLE DETECTION");
           content.Should().Contain("Circular Dependency Chains:");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

2. **Cycle Count Accuracy:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithCycles_ShowsCorrectCycleCount()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 20);
       var cycles = CreateTestCycles(cycleCount: 12, avgSize: 5);  // 12 cycles
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("Circular Dependency Chains: 12");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

3. **Project Participation Percentage:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithCycles_ShowsProjectParticipationPercentage()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 73);  // Total projects
       var cycles = new List<CycleInfo>
       {
           new CycleInfo(1, new[] { "Project1", "Project2", "Project3" }),
           new CycleInfo(2, new[] { "Project4", "Project5" }),
           new CycleInfo(3, new[] { "Project1", "Project6", "Project7" })  // Project1 in 2 cycles
       };
       // Unique projects in cycles: 1,2,3,4,5,6,7 = 7 projects
       // Percentage: 7/73 * 100 = 9.6%

       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("Projects in Cycles: 7 (9.6%)");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

4. **Largest Cycle Size:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithCycles_ShowsLargestCycleSize()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 20);
       var cycles = new List<CycleInfo>
       {
           new CycleInfo(1, new[] { "P1", "P2", "P3" }),           // Size 3
           new CycleInfo(2, new[] { "P4", "P5", "P6", "P7", "P8" }), // Size 5
           new CycleInfo(3, new[] { "P9", "P10" })                 // Size 2
       };
       // Largest cycle: 5 projects

       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("Largest Cycle Size: 5 projects");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

5. **Detailed Cycle Listing:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithCycles_ListsAllCyclesWithProjects()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var cycles = new List<CycleInfo>
       {
           new CycleInfo(1, new[] { "PaymentService", "OrderManagement", "CustomerService" }),
           new CycleInfo(2, new[] { "UserAuth", "ProfileMgmt" })
       };

       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify cycle headers
           content.Should().Contain("Cycle 1: 3 projects");
           content.Should().Contain("Cycle 2: 2 projects");

           // Verify project names
           content.Should().Contain("  - PaymentService");
           content.Should().Contain("  - OrderManagement");
           content.Should().Contain("  - CustomerService");
           content.Should().Contain("  - UserAuth");
           content.Should().Contain("  - ProfileMgmt");

           // Verify detailed section header
           content.Should().Contain("Detailed Cycle Information:");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

6. **No Cycles Scenario (Null Parameter):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithNullCycles_DoesNotIncludeCycleSection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: null);  // No cycles parameter

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().NotContain("CYCLE DETECTION");
           content.Should().NotContain("Circular Dependency Chains:");

           // Should still have Story 5.1 sections
           content.Should().Contain("DEPENDENCY OVERVIEW");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

7. **No Cycles Detected (Empty List):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithEmptyCycles_ShowsNoCyclesMessage()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var cycles = new List<CycleInfo>();  // Empty list (0 cycles detected)
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("CYCLE DETECTION");
           content.Should().Contain("No circular dependencies detected");

           // Should NOT contain statistics
           content.Should().NotContain("Circular Dependency Chains:");
           content.Should().NotContain("Projects in Cycles:");
           content.Should().NotContain("Largest Cycle Size:");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

8. **Section Order Verification:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_CycleSection_AppearsAfterDependencyOverview()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var cycles = CreateTestCycles(cycleCount: 2);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           var dependencyOverviewIndex = content.IndexOf("DEPENDENCY OVERVIEW");
           var cycleDetectionIndex = content.IndexOf("CYCLE DETECTION");

           dependencyOverviewIndex.Should().BeGreaterThan(0, "Dependency Overview should exist");
           cycleDetectionIndex.Should().BeGreaterThan(dependencyOverviewIndex,
               "Cycle Detection should appear after Dependency Overview");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

9. **Formatting Validation (Multiple Cycles):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithMultipleCycles_FormatsCorrectlyWithSeparators()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 20);
       var cycles = CreateTestCycles(cycleCount: 5, avgSize: 4);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify section separators (80 '=' characters)
           var separator = new string('=', 80);
           content.Should().Contain(separator);

           // Verify subsection separator (80 '-' characters)
           var subseparator = new string('-', 80);
           content.Should().Contain(subseparator);

           // Verify cycle count in header
           content.Should().Contain("Circular Dependency Chains: 5");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

**Test Helper Methods (New):**

```csharp
// Helper: Create test cycles with configurable size
private IReadOnlyList<CycleInfo> CreateTestCycles(int cycleCount = 3, int avgSize = 4)
{
    var cycles = new List<CycleInfo>();

    for (int i = 0; i < cycleCount; i++)
    {
        var projects = new List<string>();
        var size = avgSize + (i % 2 == 0 ? 1 : -1);  // Vary size slightly

        for (int j = 0; j < size; j++)
        {
            projects.Add($"Project{i * avgSize + j}");
        }

        cycles.Add(new CycleInfo(i + 1, projects));
    }

    return cycles;
}

// Helper: Create specific cycle for testing
private CycleInfo CreateCycle(int cycleId, params string[] projectNames)
{
    return new CycleInfo(cycleId, projectNames);
}

// Existing helpers from Story 5.1 (reused):
private DependencyGraph CreateTestGraph(int projectCount = 10, int edgeCount = 15) { ... }
private string CreateTempDirectory() { ... }
private void CleanupTempDirectory(string path) { ... }
```

**Test Execution Strategy:**

1. **Run existing Story 5.1 tests first:** Verify no regressions (15 tests should still pass)
2. **Add Story 5.2 tests incrementally:** One test at a time during implementation
3. **Final validation:** All ~22-24 tests pass before marking story as done

**Test Coverage After Story 5.2:**

```
TextReportGeneratorTests.cs:
├── Story 5.1 tests: 15 tests ✅ (no changes)
│   ├── Basic report generation
│   ├── Header content validation
│   ├── Dependency overview statistics
│   ├── Multi-solution scenarios
│   ├── Null parameter validation
│   ├── Performance tests
│   └── UTF-8 encoding verification
│
└── Story 5.2 tests: ~7-9 tests ← NEW
    ├── Cycle section inclusion
    ├── Cycle count accuracy
    ├── Participation percentage
    ├── Largest cycle size
    ├── Detailed cycle listing
    ├── No cycles (null parameter)
    ├── No cycles (empty list)
    ├── Section order validation
    └── Formatting validation

Total: ~22-24 tests (comprehensive coverage for Epic 5 reporting)
```

### Previous Story Intelligence

**From Story 5.1 (Immediate Predecessor) - Report Generator Foundation:**

Story 5.1 created the TextReportGenerator foundation that Story 5.2 extends:

**Key Patterns from Story 5.1:**

1. **Optional Parameters for Extensibility:**
   ```csharp
   // Story 5.1 interface design (forward-compatible):
   Task<string> GenerateAsync(
       DependencyGraph graph,
       string outputDirectory,
       string solutionName,
       IReadOnlyList<CycleInfo>? cycles = null,          // ← Story 5.2 uses this
       IReadOnlyList<ExtractionScore>? extractionScores = null,
       IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
       CancellationToken cancellationToken = default);
   ```

2. **Private Helper Method Pattern:**
   ```csharp
   // Story 5.1 established this pattern:
   private void AppendHeader(StringBuilder report, string solutionName, DependencyGraph graph)
   private void AppendDependencyOverview(StringBuilder report, DependencyGraph graph)

   // Story 5.2 follows same pattern:
   private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo> cycles)
   ```

3. **Formatting Constants:**
   ```csharp
   private const int ReportWidth = 80;  // Standard terminal width

   // Story 5.2 reuses this for consistency:
   report.AppendLine(new string('=', ReportWidth));  // Section separator
   report.AppendLine(new string('-', ReportWidth));  // Subsection separator
   ```

4. **Number Formatting Standards:**
   ```csharp
   // Story 5.1 pattern (Story 5.2 follows):
   report.AppendLine($"Total References: {totalReferences:N0}");        // Thousands separator
   report.AppendLine($"  - Framework: {frameworkRefs:N0} ({percent:F0}%)"); // Integer percentage

   // Story 5.2 pattern:
   report.AppendLine($"Circular Dependency Chains: {totalCycles:N0}");
   report.AppendLine($"Projects in Cycles: {uniqueProjects:N0} ({percent:F1}%)"); // One decimal
   ```

5. **Section Structure Pattern:**
   ```
   Story 5.1 Section Format:
   SECTION NAME
   ================================================================================

   [Statistics and content]

   ================================================================================

   Story 5.2 follows identical pattern for consistency.
   ```

6. **Performance Optimization:**
   ```csharp
   // Story 5.1 pattern: StringBuilder with pre-allocated capacity
   var report = new StringBuilder(capacity: 4096);

   // Story 5.1 pattern: Single-pass LINQ queries
   var frameworkReferences = graph.Edges.Count(e => IsFrameworkReference(e));

   // Story 5.2 follows: Single-pass unique projects count
   var uniqueProjects = cycles.SelectMany(c => c.ProjectsInCycle).Distinct().Count();
   ```

**From Story 5.1 Code Review Fixes:**

Story 5.1 underwent adversarial code review that identified critical issues. Story 5.2 should learn from these:

1. **Security: Path Sanitization Pattern:**
   ```csharp
   // Story 5.1 fix: Sanitize user input for filenames
   private string SanitizeFileName(string fileName)
   {
       var invalidChars = Path.GetInvalidFileNameChars();
       return invalidChars.Aggregate(fileName, (current, c) => current.Replace(c, '_'));
   }

   // Story 5.2 lesson: Sanitize any user-provided data used in output
   // (Not applicable to Story 5.2 - cycles data is internal, not user-provided)
   ```

2. **Architecture: Centralized Configuration:**
   ```csharp
   // Story 5.1 fix: Use IOptions<FilterConfiguration> instead of hardcoded patterns
   // Story 5.2 lesson: Reuse existing configuration/patterns rather than duplicating
   ```

3. **Test Coverage: Explicit Verification:**
   ```csharp
   // Story 5.1 added missing tests for:
   // - UTF-8 encoding verification
   // - Cancellation token support
   // - Invalid filename character handling

   // Story 5.2 lesson: Test ALL acceptance criteria explicitly
   // - Test null cycles parameter
   // - Test empty cycles list
   // - Test cycle count accuracy
   // - Test percentage calculation
   ```

**From Epic 3 (Cycle Detection) - Data Source:**

Story 5.2 consumes cycle detection results from Epic 3:

**Epic 3 CycleInfo Model (Story 3.1):**

```csharp
// Created by Tarjan's SCC algorithm (Epic 3, Story 3.1):
public sealed record CycleInfo(
    int CycleId,
    IReadOnlyList<string> ProjectsInCycle);
```

**Epic 3 CycleStatistics Pattern (Story 3.2):**

```csharp
// Epic 3 calculated statistics for CLI display:
public sealed record CycleStatistics(
    int TotalCycles,
    int TotalProjectsInCycles,
    double ProjectParticipationRate,
    int LargestCycleSize);

// Story 5.2 pattern: Similar calculations for text report
```

**Epic 3 Cycle Detection Flow:**

```
Story 3.1: Tarjan's SCC algorithm
        ↓
[IReadOnlyList<CycleInfo>]
        ↓
Story 3.2: Calculate CycleStatistics
        ↓
Story 3.6: Visualize cycles in DOT format (parallel)
        ↓
Story 5.2: Report cycles in text format (THIS STORY)
```

**From Epic 4 (Extraction Scoring) - Future Integration Pattern:**

Story 5.2 follows the same integration pattern that Story 5.3 will use:

```csharp
// Story 5.2 pattern (cycles):
if (cycles != null)
{
    AppendCycleDetection(report, cycles);
}

// Story 5.3 pattern (extraction scores):
if (extractionScores != null)
{
    AppendExtractionScores(report, extractionScores);
}

// Story 5.4 pattern (recommendations):
if (recommendations != null)
{
    AppendRecommendations(report, recommendations);
}
```

**Implementation Takeaways:**

1. ✅ **Follow Story 5.1's section generation pattern** - private helper methods
2. ✅ **Maintain Story 5.1's formatting consistency** - 80-char separators, number formatting
3. ✅ **Reuse Story 5.1's performance optimizations** - single-pass LINQ, pre-allocated StringBuilder
4. ✅ **Learn from Story 5.1's code review** - explicit test coverage, no hardcoded patterns
5. ✅ **Consume Epic 3's CycleInfo as-is** - no modifications to cycle detection logic
6. ✅ **Set pattern for Stories 5.3-5.4** - optional parameter handling, section insertion

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 5.2 continues Epic 5's reporting stack progression:

```
Epic Progression:
Epic 2 (Dependency Analysis) → Epic 3 (Cycle Detection) → Epic 4 (Extraction Scoring) → Epic 5 (Reporting)

src/MasDependencyMap.Core/DependencyAnalysis/
├── [Stories 2.1-2.5: Dependency graph building] ✅ DONE
└── DependencyGraph.cs               # Consumed by Story 5.1

src/MasDependencyMap.Core/CycleAnalysis/
├── [Stories 3.1-3.7: Cycle detection and analysis] ✅ DONE
├── CycleInfo.cs                     # Consumed by Story 5.2 ← THIS STORY
└── CycleStatistics.cs               # Reference for statistics pattern

src/MasDependencyMap.Core/ExtractionScoring/
├── [Stories 4.1-4.8: Scoring and visualization] ✅ DONE
└── ExtractionScore.cs               # Will be consumed by Story 5.3

src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # Story 5.1 (unchanged)
└── TextReportGenerator.cs           # Story 5.1 + Story 5.2 ← MODIFIED
```

**Epic 5 Reporting Stack After Story 5.2:**

```
Story 5.1 (DONE): Text Report Generator Foundation
    ↓ Creates: ITextReportGenerator, TextReportGenerator
    ↓ Sections: Header, Dependency Overview
    ↓
Story 5.2 (THIS STORY): Add Cycle Detection Section
    ↓ Extends: TextReportGenerator
    ↓ Sections: Header, Dependency Overview, Cycle Detection
    ↓
Story 5.3 (FUTURE): Add Extraction Difficulty Section
    ↓ Extends: TextReportGenerator
    ↓ Sections: Header, Dependency Overview, Cycle Detection, Extraction Scores
    ↓
Story 5.4 (FUTURE): Add Recommendations Section
    ↓ Extends: TextReportGenerator
    ↓ Sections: Header, Dependency Overview, Cycle Detection, Extraction Scores, Recommendations
    ↓
[Complete Text Report]
```

**Cross-Epic Dependencies:**

Story 5.2 completes the integration between Epic 3 and Epic 5:

```
Epic 3 Output:
- CycleInfo objects (List of cycles)
- CycleStatistics summary

        ↓

Story 5.2 Integration:
- Consumes IReadOnlyList<CycleInfo>
- Calculates report-specific statistics
- Formats for stakeholder readability

        ↓

Epic 5 Output:
- Text report with cycle detection section
- Human-readable cycle information
- Stakeholder-ready insights
```

**No Impact on Other Epics:**

```
Epic 1 (Foundation): ✅ No changes
Epic 2 (Dependency Analysis): ✅ No changes
Epic 3 (Cycle Detection): ✅ No changes (pure consumption)
Epic 4 (Extraction Scoring): ✅ No changes
Epic 6 (Future): Not yet started
```

**Epic 5 Roadmap After Story 5.2:**

After Story 5.2, Epic 5 continues with additional report sections and CSV export:

1. ✅ Story 5.1: Text Report Generator foundation (DONE)
2. 🔨 Story 5.2: Add Cycle Detection section (THIS STORY)
3. ⏳ Story 5.3: Add Extraction Difficulty section (extends TextReportGenerator)
4. ⏳ Story 5.4: Add Recommendations section (extends TextReportGenerator)
5. ⏳ Story 5.5: CSV export for extraction scores (new CsvExporter component)
6. ⏳ Story 5.6: CSV export for cycle analysis
7. ⏳ Story 5.7: CSV export for dependency matrix
8. ⏳ Story 5.8: Spectre.Console table formatting (CLI layer integration)

**After Story 5.2:**
- **Text report sections:** 3/5 complete (Header, Dependency Overview, Cycle Detection)
- **Remaining sections:** Extraction Scores (5.3), Recommendations (5.4)
- **CSV export:** Not started (Stories 5.5-5.7)
- **CLI integration:** Not started (Story 5.8)

**File Modification Summary (Story 5.2):**

```
MODIFIED (2 files):
- src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs
  - Add private field: _totalProjects
  - Modify GenerateAsync: Add cycle section call
  - Add new method: AppendCycleDetection

- tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs
  - Add helper: CreateTestCycles
  - Add ~7-9 new test methods

UNCHANGED (Backward compatible):
- ITextReportGenerator.cs (interface already supports cycles parameter)
- All Epic 1-4 components
- All Epic 5 Story 5.1 test coverage (15 existing tests still pass)
```

**Development Workflow Impact:**

Story 5.2 follows the same development pattern as Story 5.1:

1. **Implementation Phase:**
   - Modify TextReportGenerator.cs
   - Add AppendCycleDetection private method
   - Update GenerateAsync to call new method

2. **Testing Phase:**
   - Add helper methods for cycle creation
   - Add ~7-9 new tests
   - Run all tests (existing + new)

3. **Validation Phase:**
   - Verify all 400+ project tests pass
   - Verify no regressions in Story 5.1 functionality
   - Manual test: Generate report with cycles from test solution

4. **Code Review Phase (Expected):**
   - Story 5.1 had 5 critical issues found in code review
   - Story 5.2 should expect similar scrutiny
   - Prepare for adversarial review focusing on:
     - Percentage calculation edge cases
     - Null/empty cycle handling
     - Formatting consistency
     - Performance (LINQ query efficiency)

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-5-comprehensive-reporting-and-data-export.md, Story 5.2 (lines 22-41)]
- Story requirements: Cycle detection section with statistics, project participation percentage, detailed cycle listing, "no cycles" handling

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces (Reporting), async patterns, file-scoped namespaces, XML documentation
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 114-119)]
- Logging: Structured logging with named placeholders, ILogger<T> injection

**Architecture Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\implementation-patterns-consistency-rules.md]
- Namespace organization: Feature-based (MasDependencyMap.Core.Reporting), NOT layer-based
- Test organization: Mirror namespace structure in tests/
- Private helper methods: No XML documentation required

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\5-1-implement-text-report-generator-with-summary-statistics.md]
- Story 5.1 implementation: TextReportGenerator foundation with optional parameters for extensibility
- Helper method pattern: AppendHeader, AppendDependencyOverview (Story 5.2 follows same pattern)
- Formatting constants: ReportWidth = 80, separator patterns
- Performance: StringBuilder pre-allocation (4096 capacity), single-pass LINQ queries

**Epic 3 Integration:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-3-circular-dependency-detection-and-break-point-analysis.md]
- CycleInfo model: CycleId, ProjectsInCycle (IReadOnlyList<string>)
- CycleStatistics pattern: TotalCycles, TotalProjectsInCycles, ProjectParticipationRate, LargestCycleSize
- Story 3.1: Tarjan's SCC algorithm implementation
- Story 3.2: Cycle statistics calculation (reference for Story 5.2 calculations)

**Existing Code Patterns:**
- [Source: D:\work\masDependencyMap\src\MasDependencyMap.Core\Reporting\TextReportGenerator.cs (lines 1-150)]
- File-scoped namespace: `namespace MasDependencyMap.Core.Reporting;`
- Constructor injection: ILogger<TextReportGenerator>
- Private helper methods: AppendHeader, AppendDependencyOverview
- Number formatting: N0 for integers, F0 or F1 for percentages
- Section separators: 80 '=' characters for main sections, 80 '-' characters for subsections

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Recent pattern: Story 5.1 completed with text report generator foundation
- Commit pattern: Story implementation → code review → fixes → status update
- Story 5.1 most recent: d8f8b98 "Story 5-1: Implement text report generator with summary statistics"

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - No blocking issues encountered during implementation.

### Completion Notes List

✅ **Implementation Complete - All Acceptance Criteria Met**

**Core Implementation:**
- Added private field `_totalProjects` to TextReportGenerator for cycle participation percentage calculation
- Modified GenerateAsync to store `_totalProjects = graph.VertexCount` and call AppendCycleDetection when cycles parameter is provided
- Implemented AppendCycleDetection private method with complete cycle detection section formatting

**Cycle Detection Section Features:**
- Summary statistics: Total cycles count, unique projects in cycles with percentage, largest cycle size
- Detailed cycle listing: Each cycle numbered with project count and indented project names
- "No cycles" message handling for empty cycles list
- Null cycles parameter handling (section not included)
- Consistent 80-character separator formatting (= for main sections, - for subsections)

**Epic 3 CycleInfo Integration:**
- Corrected integration to use actual CycleInfo.Projects property (IReadOnlyList<ProjectNode>) instead of story notes reference to ProjectsInCycle
- Properly access ProjectNode.ProjectName for display in report
- Added CycleAnalysis namespace import

**Test Coverage (9 new tests added):**
- GenerateAsync_WithCycles_IncludesCycleDetectionSection
- GenerateAsync_WithCycles_ShowsCorrectCycleCount
- GenerateAsync_WithCycles_ShowsProjectParticipationPercentage (9.6% validation)
- GenerateAsync_WithCycles_ShowsLargestCycleSize
- GenerateAsync_WithCycles_ListsAllCyclesWithProjects
- GenerateAsync_WithNullCycles_DoesNotIncludeCycleSection
- GenerateAsync_WithEmptyCycles_ShowsNoCyclesMessage
- GenerateAsync_CycleSection_AppearsAfterDependencyOverview
- GenerateAsync_WithMultipleCycles_FormatsCorrectlyWithSeparators
- Helper method: CreateTestCycles() for generating test cycle data

**Architecture Compliance:**
- File-scoped namespace: MasDependencyMap.Core.Reporting
- Private helper method pattern consistent with Story 5.1
- Structured logging with Debug level messages
- Single-pass LINQ queries for performance
- ReportWidth constant (80 chars) for formatting consistency
- No new files created (extended existing TextReportGenerator.cs)

**Test Results (Original Implementation):**
- All 24 TextReportGenerator tests passed
- All 409 project tests passed (no regressions)
- Performance: Large graph test (400 projects, 5000 edges) completes within 10 seconds

**Code Review Fixes Applied:**
After adversarial code review, the following issues were identified and fixed:

**HIGH SEVERITY (1 issue fixed):**
1. ✅ Incomplete File Documentation - Updated File List to document all changed files including IDE config, test debris, and .gitignore changes

**MEDIUM SEVERITY (5 issues fixed):**
2. ✅ Test Debris Cleanup - Deleted TestReportDemo.cs, test-text-report.csx, and test-output/ directory from repository root
3. ✅ IDE Configuration File - Removed .claude/settings.local.json from git tracking (matches .gitignore pattern on line 98)
4. ✅ Missing Integration Test - Added GenerateAsync_WithRealCycleDetector_ProducesCorrectReport using TarjanCycleDetector from Epic 3
5. ✅ Missing Edge Case Test (Zero Projects) - Added GenerateAsync_WithEmptyGraphButCycles_HandlesGracefully to test division by zero protection
6. ✅ Missing Edge Case Test (Null Names) - Added GenerateAsync_WithNullProjectNameInCycle_HandlesWithoutCrashing to document defensive behavior

**LOW SEVERITY (1 issue fixed):**
7. ✅ Large Cycle Test - Added GenerateAsync_WithVeryLargeCycle_FormatsCorrectlyAndPerformsWell (150 projects in single cycle)

**Test Results (After Code Review Fixes):**
- All 28 TextReportGenerator tests passed (24 original + 4 new code review tests)
- All 413 project tests passed (no regressions)
- Integration test validates Epic 3 CycleInfo compatibility
- Edge case tests document defensive programming for impossible scenarios
- Performance test confirms large cycles (150 projects) complete within 10 second budget

**Report Format Example:**
```
CYCLE DETECTION
================================================================================

Circular Dependency Chains: 12
Projects in Cycles: 45 (61.6%)
Largest Cycle Size: 8 projects

Detailed Cycle Information:
--------------------------------------------------------------------------------

Cycle 1: 5 projects
  - PaymentService
  - OrderManagement
  - CustomerService
  - NotificationService
  - InventoryTracking

Cycle 2: 3 projects
  - UserAuthentication
  - ProfileManagement
  - SessionManager

================================================================================
```

### File List

**Modified Files:**
- src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs (Added _totalProjects field, AppendCycleDetection method, cycle section call in GenerateAsync)
- tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs (Added 9 original + 4 code review tests = 13 total new tests, CreateTestCycles helper, CycleAnalysis namespace import)
- .gitignore (Added test-output/ and test-*/ patterns to ignore test artifacts)
- _bmad-output/implementation-artifacts/5-2-add-cycle-detection-section-to-text-reports.md (This story file - marked tasks complete, added completion notes and code review fixes)
- _bmad-output/implementation-artifacts/sprint-status.yaml (Updated story status: ready-for-dev → in-progress → review → done)

**Files Removed During Code Review:**
- .claude/settings.local.json (Removed from git tracking - should be ignored per .gitignore line 98)
- TestReportDemo.cs (Deleted - manual test debris from root directory)
- test-text-report.csx (Deleted - demo script from root directory)
- test-output/SampleMonolith-dependencies.dot (Deleted via directory pattern in .gitignore)

**No New Files Created** (Story 5.2 extends existing Story 5.1 components)
