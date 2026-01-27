# Story 5.4: Add Cycle-Breaking Recommendations to Text Reports

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want cycle-breaking recommendations with rationale included in text reports,
So that I can share actionable insights with my team.

## Acceptance Criteria

**Given** Cycle-breaking recommendations have been generated
**When** Text report is generated
**Then** Cycle-Breaking Recommendations section lists top 5 suggestions
**And** Each recommendation includes: source project → target project, coupling score, rationale
**And** Rationale explains why this edge should be broken (e.g., "Weakest link in 8-project cycle, only 3 method calls")
**And** Report format example: "Break: PaymentService → OrderManagement (Coupling: 3 calls) - Weakest link in 5-project cycle"
**And** Recommendations are actionable and easy for stakeholders to understand

## Tasks / Subtasks

- [x] Update TextReportGenerator.GenerateAsync to use recommendations parameter (AC: Include recommendations section)
  - [x] Check if recommendations parameter is not null and has elements
  - [x] If recommendations provided: Call AppendRecommendations(report, recommendations) helper method
  - [x] If no recommendations or empty list: Call AppendRecommendations with null to skip section
  - [x] Insert recommendations section after Extraction Difficulty Scores section (end of report before closing)

- [x] Implement AppendRecommendations private method (AC: Top 5 suggestions)
  - [x] Accept StringBuilder report and IReadOnlyList<CycleBreakingSuggestion>? recommendations parameters
  - [x] Add section header: "CYCLE-BREAKING RECOMMENDATIONS" with separator (80 '=' characters)
  - [x] Handle null/empty recommendations: Return early without section (backward compatibility)
  - [x] Take top 5 recommendations by Rank (already sorted by RecommendationGenerator)
  - [x] Add explanatory text: "Top 5 prioritized actions to reduce circular dependencies"

- [x] Format recommendations with clear actionable output (AC: Each recommendation shows required fields)
  - [x] Use 1-based ranking: "1. Break: SourceProject → TargetProject (Coupling: X calls)"
  - [x] Include rationale on same line after dash: " - {recommendation.Rationale}"
  - [x] Format coupling score with proper singular/plural: "1 call" vs "3 calls"
  - [x] Example output: "1. Break: PaymentService → OrderManagement (Coupling: 3 calls) - Weakest link in 5-project cycle"
  - [x] Use arrow symbol → for clarity (Unicode U+2192)
  - [x] Add blank line for spacing between recommendations

- [x] Handle edge cases gracefully (AC: Backward compatibility)
  - [x] If fewer than 5 recommendations: Show all available (don't pad)
  - [x] If null recommendations parameter: Skip section entirely
  - [x] If empty recommendations list: Skip section entirely
  - [x] No "No recommendations available" message (optional feature, not core)

- [x] Add comprehensive unit tests (AC: Test coverage)
  - [x] Create tests in TextReportGeneratorTests.cs (extends existing test class)
  - [x] Test: GenerateAsync_WithRecommendations_IncludesRecommendationsSection (verify section exists)
  - [x] Test: GenerateAsync_WithRecommendations_ShowsTop5Only (verify only 5 shown when 10+ available)
  - [x] Test: GenerateAsync_WithRecommendations_FormatsCorrectly (verify format matches spec)
  - [x] Test: GenerateAsync_WithNullRecommendations_DoesNotIncludeSection (null parameter)
  - [x] Test: GenerateAsync_WithEmptyRecommendations_DoesNotIncludeSection (empty list)
  - [x] Test: GenerateAsync_WithFewerThan5Recommendations_ShowsAllAvailable (edge case: 3 recommendations)
  - [x] Test: GenerateAsync_RecommendationsSection_AppearsAfterExtractionScores (section order)
  - [x] Test: GenerateAsync_WithSingleCoupling_FormatsAsSingleCall (grammatical correctness: "1 call")
  - [x] Test: GenerateAsync_WithMultipleCouplings_FormatsAsPlural (grammatical correctness: "3 calls")
  - [x] Test: GenerateAsync_RecommendationsSection_UsesCorrectSeparators (formatting consistency)
  - [x] Use helper methods: CreateTestRecommendations() for generating test data

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Existing namespace: MasDependencyMap.Core.Reporting (no new namespace)
  - [x] Private method: AppendRecommendations (helper, not public API)
  - [x] Consistent formatting: Use ReportWidth constant (80 characters)
  - [x] Structured logging: Add log message for recommendations section generation
  - [x] Performance: Single-pass LINQ Take(5) for top recommendations
  - [x] Test organization: Add tests to existing TextReportGeneratorTests.cs

- [x] Verify integration with Epic 3 CycleBreakingSuggestion model (AC: Consume recommendation data correctly)
  - [x] Use CycleBreakingSuggestion properties: Rank, SourceProject, TargetProject, CouplingScore, Rationale
  - [x] Verify CycleBreakingSuggestion namespace import: using MasDependencyMap.Core.CycleAnalysis;
  - [x] Ensure CycleBreakingSuggestion contains all necessary data (already defined in Epic 3)
  - [x] No changes to CycleBreakingSuggestion model needed (Story 5.4 is pure consumer)
  - [x] Trust Rank property from RecommendationGenerator (already sorted correctly)

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 5.4 Extends Story 5.1/5.2/5.3 Foundation:**

This story **extends** the existing `TextReportGenerator` created in Story 5.1 and extended in Stories 5.2 and 5.3. **NO new classes** are created - only the implementation of `TextReportGenerator.cs` is modified.

**Epic 5 Reporting Stack Progress:**
- ✅ Story 5.1: Text Report Generator with Summary Statistics (FOUNDATION - DONE)
- ✅ Story 5.2: Add Cycle Detection section to text reports (EXTENDS - DONE)
- ✅ Story 5.3: Add Extraction Difficulty Scoring section to text reports (EXTENDS - DONE)
- 🔨 Story 5.4: Add Cycle-Breaking Recommendations to text reports (THIS STORY - EXTENDS)
- ⏳ Story 5.5-5.7: CSV export capabilities (NEW COMPONENTS)
- ⏳ Story 5.8: Spectre.Console table formatting enhancements (CLI INTEGRATION)

**Story 5.4 Unique Characteristics:**

1. **Pure Extension of Existing Component:**
   - Story 5.1: Created ITextReportGenerator interface and TextReportGenerator implementation
   - Story 5.2: Added Cycle Detection section to TextReportGenerator
   - Story 5.3: Added Extraction Difficulty Scores section to TextReportGenerator
   - Story 5.4: Adds Cycle-Breaking Recommendations section to TextReportGenerator
   - **No new files created** - Only modifies src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs
   - **No new tests file** - Extends tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs

2. **Leverages Story 5.1's Forward-Compatible Interface:**
   - Story 5.1 interface included optional `recommendations` parameter for exactly this purpose:
   ```csharp
   Task<string> GenerateAsync(
       DependencyGraph graph,
       string outputDirectory,
       string solutionName,
       IReadOnlyList<CycleInfo>? cycles = null,
       IReadOnlyList<ExtractionScore>? extractionScores = null,
       IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,  // ← Story 5.4 uses this
       CancellationToken cancellationToken = default);
   ```
   - **Interface unchanged** - Story 5.1 designed for extensibility
   - **Implementation change only** - Activate the recommendations parameter handling

3. **Consumes Epic 3 Cycle-Breaking Recommendations:**
   - Epic 3 (Stories 3.1-3.7): All done, CycleBreakingSuggestion model exists and tested
   - Story 3.1-3.4: Detected cycles, analyzed coupling, identified weak edges
   - Story 3.5: Created RecommendationGenerator with ranked suggestions
   - Story 3.6-3.7: Visualization enhancements (parallel effort, visual representation)
   - **Story 5.4 integration point:** Consumes CycleBreakingSuggestion from Epic 3's RecommendationGenerator
   - **No Epic 3 changes needed** - Pure consumption of existing data

4. **Section Insertion Strategy:**
   ```
   Report Structure After Story 5.4:

   ================================================================================
   MasDependencyMap Analysis Report
   ================================================================================
   [Header: Solution name, date, total projects]      ← Story 5.1

   DEPENDENCY OVERVIEW                                ← Story 5.1
   [Total references, framework/custom split]         ← Story 5.1

   CYCLE DETECTION                                    ← Story 5.2
   [Circular dependency chains, participation %]      ← Story 5.2
   [Largest cycle, detailed cycle list]               ← Story 5.2

   EXTRACTION DIFFICULTY SCORES                       ← Story 5.3
   [Top 10 easiest candidates with metrics]           ← Story 5.3
   [Bottom 10 hardest candidates with complexity]     ← Story 5.3

   CYCLE-BREAKING RECOMMENDATIONS                     ← Story 5.4 NEW
   [Top 5 prioritized recommendations]                ← Story 5.4 NEW
   ================================================================================
   ```
   - **Insert after:** Extraction Difficulty Scores section (Story 5.3)
   - **Insert as:** Final content section before report closing

5. **Top 5 Recommendations Presentation:**
   - **Ranking:** Use existing Rank property from RecommendationGenerator (already sorted correctly)
   - **Format:** `{Rank}. Break: {SourceProject} → {TargetProject} (Coupling: {Score} {calls/call}) - {Rationale}`
   - **Example:** `1. Break: PaymentService → OrderManagement (Coupling: 3 calls) - Weakest link in 5-project cycle`
   - **Arrow symbol:** Use Unicode → (U+2192) for clear visual direction
   - **Coupling format:** Singular "1 call" vs plural "3 calls" for grammatical correctness
   - **Rationale:** Generated by Epic 3 RecommendationGenerator, already human-readable
   - **Limit:** Top 5 only (even if 50+ recommendations available)

6. **Backward Compatibility Requirement:**
   - If `recommendations` parameter is null: Report still generates with Stories 5.1-5.3 sections only
   - If `recommendations` parameter is empty list: No section included (skip)
   - **No breaking changes** - Existing callers without recommendations parameter continue to work
   - **Graceful degradation** - Report adapts to available data

🚨 **CRITICAL - CycleBreakingSuggestion Model from Epic 3:**

Story 5.4 consumes the `CycleBreakingSuggestion` model created in Epic 3 (Cycle Detection and Break-Point Analysis). Understanding this model is critical for correct implementation.

**CycleBreakingSuggestion Definition (from Epic 3):**

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Represents a recommendation for breaking a circular dependency.
/// Generated from weak coupling edges identified in cycle analysis.
/// </summary>
public sealed record CycleBreakingSuggestion : IComparable<CycleBreakingSuggestion>
{
    public int CycleId { get; init; }              // Unique cycle identifier
    public ProjectNode SourceProject { get; init; } // Source project of edge to break
    public ProjectNode TargetProject { get; init; } // Target project of edge to break
    public int CouplingScore { get; init; }        // Method call count (lower = easier to break)
    public int CycleSize { get; init; }            // Number of projects in cycle
    public string Rationale { get; init; }         // Human-readable explanation
    public int Rank { get; init; }                 // Priority ranking (1 = highest)
}
```

**Important Properties:**
- `Rank`: Priority ranking (1-based), calculated by RecommendationGenerator (lowest coupling + largest cycle = rank 1)
- `SourceProject.ProjectName`: Name of project depending on target (string property)
- `TargetProject.ProjectName`: Name of project being depended upon (string property)
- `CouplingScore`: Number of method calls between projects (lower = weaker coupling = easier to break)
- `CycleSize`: Size of the cycle this edge belongs to (larger cycle = higher impact when broken)
- `Rationale`: Pre-generated by RecommendationGenerator.GenerateRationale() - already human-readable

**Story 5.4 Display Strategy:**

**Recommendations Format:**
```
1. Break: NotificationService → EmailSender (Coupling: 3 calls) - Weakest link in 8-project cycle, only 3 method calls
2. Break: LoggingHelper → ConfigReader (Coupling: 5 calls) - Weakest link in large 6-project cycle, only 5 method calls
3. Break: CacheManager → DataLayer (Coupling: 7 calls) - Weakest link in 4-project cycle
4. Break: AuthService → UserManager (Coupling: 8 calls) - Weakest link in critical 12-project cycle
5. Break: ReportBuilder → ChartGenerator (Coupling: 9 calls) - Weakest link in small 3-project cycle, just 2 method calls
```

**Formatting Rules:**

1. **Ranking:**
   - Use 1-based ranking from Rank property (already set by RecommendationGenerator)
   - Format: `{Rank}. Break: ...`
   - Trust the sorting order - RecommendationGenerator already sorted by coupling (ascending) then cycle size (descending)

2. **Project Names:**
   - Format: `{SourceProject.ProjectName} → {TargetProject.ProjectName}`
   - Use Unicode arrow → (U+2192) for clear visual direction
   - Example: `PaymentService → OrderManagement`

3. **Coupling Score:**
   - Format: `(Coupling: {CouplingScore} {call/calls})`
   - Grammatical correctness:
     - `1 call` (singular)
     - `2 calls`, `3 calls`, etc. (plural)
   - Example: `(Coupling: 3 calls)` or `(Coupling: 1 call)`

4. **Rationale:**
   - Format: ` - {Rationale}`
   - Rationale already generated by Epic 3 RecommendationGenerator
   - No modification needed - use as-is
   - Example: ` - Weakest link in 8-project cycle, only 3 method calls`

**Handling Edge Cases:**

1. **Fewer than 5 recommendations:**
   - Show all available recommendations (e.g., 3 recommendations if only 3 generated)
   - Don't pad with empty entries
   - Use LINQ `.Take(5)` which gracefully handles fewer items

2. **Exactly 5 recommendations:**
   - Show all 5 recommendations

3. **More than 5 recommendations:**
   - Show only top 5 (by Rank)
   - Use LINQ `.Take(5)` to limit

4. **Null or empty recommendations:**
   - Skip section entirely (backward compatibility)
   - No "No recommendations available" message (unlike cycles section)
   - Reasoning: Cycle-breaking recommendations are optional feature, not core analysis

🚨 **CRITICAL - Report Formatting Pattern (Stories 5.1/5.2/5.3 Consistency):**

Story 5.4 must match Stories 5.1, 5.2, and 5.3 formatting conventions for visual consistency.

**Story 5.4 Section Format (Target):**

```
CYCLE-BREAKING RECOMMENDATIONS
================================================================================

Top 5 prioritized actions to reduce circular dependencies:

 1. Break: NotificationService → EmailSender (Coupling: 3 calls) - Weakest link in 8-project cycle, only 3 method calls
 2. Break: LoggingHelper → ConfigReader (Coupling: 5 calls) - Weakest link in large 6-project cycle, only 5 method calls
 3. Break: CacheManager → DataLayer (Coupling: 7 calls) - Weakest link in 4-project cycle
 4. Break: AuthService → UserManager (Coupling: 8 calls) - Weakest link in critical 12-project cycle
 5. Break: ReportBuilder → ChartGenerator (Coupling: 9 calls) - Weakest link in small 3-project cycle

================================================================================
```

**Formatting Rules (from Stories 5.1/5.2/5.3):**

1. **Section Headers:**
   - ALL CAPS section name (e.g., "CYCLE-BREAKING RECOMMENDATIONS")
   - 80 '=' characters separator line
   - Blank line after separator

2. **Explanatory Text:**
   - Single line explaining what the section contains
   - Example: "Top 5 prioritized actions to reduce circular dependencies:"
   - Blank line after explanatory text

3. **Ranking Format:**
   - Right-aligned rank with padding: ` 1.`, ` 2.`, ..., ` 5.` (space for alignment)
   - Consistent spacing for visual alignment
   - One recommendation per line

4. **Coupling Formatting:**
   - Integer coupling score (no decimal places): `Coupling: 3 calls` not `Coupling: 3.0 calls`
   - Grammatical correctness: "1 call", "2 calls"
   - Parentheses around coupling: `(Coupling: X calls)`

5. **Section Closing:**
   - Blank line before section separator
   - 80 '=' characters separator
   - Report ends after this section (final section)

**ReportWidth Constant (from Story 5.1):**

```csharp
private const int ReportWidth = 80;  // Standard terminal width for formatting

// Usage in Story 5.4:
report.AppendLine(new string('=', ReportWidth));  // Main separator
```

**Coupling Formatting Examples:**

```csharp
// Singular vs plural formatting
var couplingText = recommendation.CouplingScore == 1 ? "1 call" : $"{recommendation.CouplingScore} calls";

// Full line format
report.AppendLine($" {recommendation.Rank}. Break: {recommendation.SourceProject.ProjectName} → {recommendation.TargetProject.ProjectName} (Coupling: {couplingText}) - {recommendation.Rationale}");

// Example outputs:
" 1. Break: PaymentService → OrderManagement (Coupling: 3 calls) - Weakest link in 5-project cycle"
" 2. Break: UserService → AuthProvider (Coupling: 1 call) - Weakest link in 4-project cycle, only 1 method call"
```

🚨 **CRITICAL - Performance Considerations:**

Story 5.1 Acceptance Criteria: "File is generated within 10 seconds regardless of solution size"

Story 5.4 adds recommendations section - must maintain this performance guarantee.

**Performance Analysis:**

1. **Stories 5.1 + 5.2 + 5.3 Baseline:**
   - Header generation: O(1)
   - Dependency overview: O(edges)
   - Cycle detection section: O(projects + cycles * avg_cycle_size)
   - Extraction scores section: O(n log n) where n = project count
   - **Total:** O(edges + projects + n log n) - typically <3 seconds for 5000 edges

2. **Story 5.4 Additions:**
   - Take top 5: O(5) = O(1) - recommendations already sorted by Epic 3
   - Format each recommendation: O(5) = O(1)
   - **Total Story 5.4:** O(1) - negligible overhead

3. **Combined Performance:**
   - Stories 5.1 + 5.2 + 5.3 + 5.4: O(edges + projects + n log n)
   - Story 5.4 adds <1ms overhead (formatting 5 lines)
   - **Expected time:** <3 seconds total (well within 10-second limit)

4. **Optimization Strategies:**
   - ✅ Trust RecommendationGenerator sorting - no re-sorting needed
   - ✅ Use LINQ Take(5) for early termination
   - ✅ No nested loops or complex operations
   - ✅ Single-pass formatting

**Memory Considerations:**

```csharp
// Efficient: Take top 5 from already-sorted list
var topRecommendations = recommendations.Take(5).ToList();  // Materialize only 5 items

// No sorting needed - Epic 3 RecommendationGenerator already sorted by:
// 1. Coupling score (ascending)
// 2. Cycle size (descending)
// 3. Source project name (alphabetical)
```

**Logging Performance:**

```csharp
// Add logging for Story 5.4 section generation
_logger.LogDebug("Appending cycle-breaking recommendations section: {RecommendationCount} total, showing top {TopCount}",
    recommendations.Count,
    Math.Min(5, recommendations.Count));

// Existing Story 5.1 performance logging (unchanged):
var elapsed = DateTime.UtcNow - startTime;
_logger.LogInformation(
    "Generated text report at {FilePath} in {ElapsedMs}ms",
    filePath,
    elapsed.TotalMilliseconds);
```

**No Performance Concerns for Story 5.4:** Recommendations section adds <1ms overhead (formatting 5 pre-sorted recommendations).

### Technical Requirements

**Modified Component: TextReportGenerator.cs (from Stories 5.1, 5.2, and 5.3)**

Story 5.4 modifies the existing TextReportGenerator implementation created in Story 5.1 and extended in Stories 5.2 and 5.3:

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # UNCHANGED (interface already supports recommendations parameter)
└── TextReportGenerator.cs           # MODIFIED: Add AppendRecommendations method, use recommendations parameter

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs      # MODIFIED: Add ~9-10 new tests for recommendations section
```

**New Dependencies (Epic 3 Integration):**

Story 5.4 adds explicit dependency on Epic 3's CycleAnalysis namespace (already imported in Stories 5.2):

```csharp
using MasDependencyMap.Core.CycleAnalysis;  // For CycleBreakingSuggestion model (already present from Story 5.2 for CycleInfo)
```

**Existing Dependencies (from Stories 5.1, 5.2, and 5.3):**

All Story 5.1/5.2/5.3 dependencies remain:

```csharp
using System.Text;                              // For StringBuilder
using MasDependencyMap.Core.DependencyAnalysis; // For DependencyGraph
using MasDependencyMap.Core.CycleAnalysis;      // For CycleInfo (Story 5.2), CycleBreakingSuggestion (Story 5.4)
using MasDependencyMap.Core.ExtractionScoring;  // For ExtractionScore (Story 5.3)
using Microsoft.Extensions.Logging;             // For ILogger<T>
```

**No New NuGet Packages Required:**

All dependencies already installed from previous epics:
- ✅ Epic 1: Microsoft.Extensions.Logging.Console
- ✅ Epic 2: QuikGraph (DependencyGraph)
- ✅ Epic 3: CycleAnalysis components (CycleInfo, CycleBreakingSuggestion, RecommendationGenerator)
- ✅ Epic 4: ExtractionScoring components (ExtractionScore)
- ✅ Built-in: System.Text.StringBuilder, System.Linq

**DI Registration:**

No changes to DI registration - TextReportGenerator already registered in Story 5.1:

```csharp
// CLI Program.cs (unchanged from Story 5.1)
services.AddSingleton<ITextReportGenerator, TextReportGenerator>();
```

**Private Helper Method Pattern:**

Story 5.4 follows Story 5.1/5.2/5.3 pattern of private helper methods:

```csharp
// Story 5.1 pattern:
private void AppendHeader(StringBuilder report, string solutionName, DependencyGraph graph)
private void AppendDependencyOverview(StringBuilder report, DependencyGraph graph)

// Story 5.2 pattern:
private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo> cycles)

// Story 5.3 pattern:
private void AppendExtractionScores(StringBuilder report, IReadOnlyList<ExtractionScore> extractionScores)

// Story 5.4 pattern (SAME STYLE):
private void AppendRecommendations(StringBuilder report, IReadOnlyList<CycleBreakingSuggestion> recommendations)
private string FormatCouplingCalls(int couplingScore)  // NEW: Helper for grammatical correctness
```

**Method Organization:**

```csharp
public sealed class TextReportGenerator : ITextReportGenerator
{
    // Fields
    private readonly ILogger<TextReportGenerator> _logger;
    private readonly FilterConfiguration _filterConfiguration;
    private const int ReportWidth = 80;
    private int _totalProjects;  // Story 5.2

    // Constructor
    public TextReportGenerator(ILogger<TextReportGenerator> logger, IOptions<FilterConfiguration> filterConfiguration) { }

    // Public API (interface implementation)
    public async Task<string> GenerateAsync(...) { }

    // Private helper methods (grouped by section)
    private void AppendHeader(...)                  // Story 5.1
    private void AppendDependencyOverview(...)      // Story 5.1
    private void AppendCycleDetection(...)          // Story 5.2
    private void AppendExtractionScores(...)        // Story 5.3
    private void AppendRecommendations(...)         // Story 5.4 NEW

    // Private utility methods
    private bool IsFrameworkReference(...)          // Story 5.1
    private int CountCrossSolutionReferences(...)   // Story 5.1
    private string SanitizeFileName(...)            // Story 5.1
    private string GetComplexityLabel(...)          // Story 5.3
    private string FormatExternalApis(...)          // Story 5.3
    private string FormatCouplingCalls(...)         // Story 5.4 NEW
}
```

### Architecture Compliance

**Namespace Structure (Unchanged from Stories 5.1/5.2/5.3):**

Story 5.4 works within the existing `MasDependencyMap.Core.Reporting` namespace:

```csharp
namespace MasDependencyMap.Core.Reporting;  // File-scoped namespace (C# 10+)

using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.CycleAnalysis;      // Already present from Story 5.2
using MasDependencyMap.Core.ExtractionScoring;  // Added in Story 5.3
using Microsoft.Extensions.Logging;
```

**Structured Logging (Required):**

Add logging for recommendations section generation:

```csharp
// Inside AppendRecommendations method:
_logger.LogDebug("Appending cycle-breaking recommendations section: {TotalRecommendations} recommendations, showing top {ShowCount}",
    recommendations.Count,
    Math.Min(5, recommendations.Count));
```

**No XML Documentation Changes:**

Since Story 5.4 only adds private helper methods, no XML documentation changes required:
- ITextReportGenerator interface: Already documented in Story 5.1 with recommendations parameter
- TextReportGenerator.GenerateAsync: Already has `<inheritdoc />`
- Private methods: XML documentation not required by project-context.md

**Performance Logging (Unchanged):**

Story 5.1's existing performance logging captures Story 5.4's additions:

```csharp
// Existing Story 5.1 logging (captures total time including Stories 5.2, 5.3, and 5.4):
var elapsed = DateTime.UtcNow - startTime;
_logger.LogInformation(
    "Generated text report at {FilePath} in {ElapsedMs}ms",
    filePath,
    elapsed.TotalMilliseconds);
```

### Library/Framework Requirements

**No New Libraries Required:**

Story 5.4 uses only existing dependencies from previous epics:

**From Epic 1 (Foundation):**
- ✅ Microsoft.Extensions.Logging.Console - Structured logging
- ✅ Microsoft.Extensions.DependencyInjection - DI container (no changes)

**From Epic 2 (Dependency Analysis):**
- ✅ QuikGraph v2.5.0 - DependencyGraph data structure

**From Epic 3 (Cycle Detection and Break-Point Analysis):**
- ✅ CycleInfo model - Used by Story 5.2
- ✅ CycleBreakingSuggestion model - Used by Story 5.4 ← THIS STORY
- ✅ RecommendationGenerator - Generates recommendations consumed by this story

**From Epic 4 (Extraction Scoring):**
- ✅ ExtractionScore model - Used by Story 5.3

**Built-in .NET Libraries:**
- ✅ System.Text.StringBuilder - Efficient string building
- ✅ System.Linq - LINQ queries (Take for limiting to top 5)
- ✅ System.Collections.Generic - IReadOnlyList<T>

**Epic 3 CycleBreakingSuggestion Model (Existing):**

Story 5.4 consumes the CycleBreakingSuggestion model from Epic 3 without modifications:

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

// Created in Epic 3, Story 3.5 (Generate ranked cycle-breaking recommendations)
public sealed record CycleBreakingSuggestion : IComparable<CycleBreakingSuggestion>
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

Story 5.4 is a pure consumer - no changes to:
- ✅ RecommendationGenerator.cs (Epic 3, Story 3.5)
- ✅ TarjanCycleDetector.cs (Epic 3, Stories 3.1-3.2)
- ✅ CouplingAnalyzer.cs (Epic 3, Story 3.3)
- ✅ DotGenerator.cs (Epic 3, Stories 3.6-3.7) - visualizations

**Story 5.4 Integration Point:**

```
Epic 3: RecommendationGenerator.GenerateRecommendationsAsync()
        ↓
[IReadOnlyList<CycleBreakingSuggestion>] (sorted by rank)
        ↓
Story 5.4: TextReportGenerator.GenerateAsync(recommendations: ...)
        ↓
[Text Report with Cycle-Breaking Recommendations section]
```

### File Structure Requirements

**Files to Modify (Only 2 Files):**

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # UNCHANGED (interface already supports recommendations parameter)
└── TextReportGenerator.cs           # MODIFY: Add AppendRecommendations method, activate recommendations parameter

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs      # MODIFY: Add ~9-10 new tests for recommendations section
```

**No New Files Created:**

Story 5.4 is a pure extension of Stories 5.1, 5.2, and 5.3 - no new classes or files.

**Files to Modify Outside Core:**

```
_bmad-output/implementation-artifacts/sprint-status.yaml  # UPDATE: Story 5-4 status backlog → ready-for-dev
_bmad-output/implementation-artifacts/5-4-add-cycle-breaking-recommendations-to-text-reports.md  # CREATE: This story file
```

**No Changes to Other Components:**

```
UNCHANGED (Epics 1-4):
- All Epic 1 foundation components
- All Epic 2 dependency analysis components
- All Epic 3 cycle detection components ✅ (consumed as-is)
- All Epic 4 extraction scoring components
- CLI Program.cs (DI registration unchanged)

UNCHANGED (Epic 5):
- Story 5.1's ITextReportGenerator interface
- Story 5.1's DI registration
- Story 5.2's AppendCycleDetection method
- Story 5.3's AppendExtractionScores method
```

**Modification Details:**

1. **TextReportGenerator.cs Changes:**
   - In GenerateAsync: Add section call after AppendExtractionScores:
     ```csharp
     if (recommendations != null && recommendations.Count > 0)
     {
         AppendRecommendations(report, recommendations);
     }
     ```
   - Add new private method: `private void AppendRecommendations(StringBuilder report, IReadOnlyList<CycleBreakingSuggestion> recommendations)`
   - Add new helper method: `private string FormatCouplingCalls(int couplingScore)`
   - **Estimated lines added:** ~40-50 lines (main method + helper)

2. **TextReportGeneratorTests.cs Changes:**
   - Add helper method: `private IReadOnlyList<CycleBreakingSuggestion> CreateTestRecommendations(...)`
   - Add ~9-10 new test methods for recommendations section validation
   - **Estimated lines added:** ~200-250 lines (tests + helpers)

**Impact on Stories 5.1/5.2/5.3 Code:**

Minimal impact - only the GenerateAsync method changes:

```csharp
// Story 5.3 implementation (before Story 5.4):
public async Task<string> GenerateAsync(...)
{
    // ... validation, logging, setup ...

    _totalProjects = graph.VertexCount;
    var report = new StringBuilder(capacity: 4096);

    AppendHeader(report, solutionName, graph);
    AppendDependencyOverview(report, graph);

    if (cycles != null)
    {
        AppendCycleDetection(report, cycles);
    }

    if (extractionScores != null && extractionScores.Count > 0)
    {
        AppendExtractionScores(report, extractionScores);
    }

    // Write to file
    // ... file write, performance logging ...
}

// Story 5.4 implementation (after):
public async Task<string> GenerateAsync(...)
{
    // ... validation, logging, setup ...

    _totalProjects = graph.VertexCount;
    var report = new StringBuilder(capacity: 4096);

    AppendHeader(report, solutionName, graph);
    AppendDependencyOverview(report, graph);

    if (cycles != null)
    {
        AppendCycleDetection(report, cycles);
    }

    if (extractionScores != null && extractionScores.Count > 0)
    {
        AppendExtractionScores(report, extractionScores);
    }

    // NEW: Story 5.4 - Cycle-breaking recommendations section
    if (recommendations != null && recommendations.Count > 0)
    {
        AppendRecommendations(report, recommendations);
    }

    // Write to file
    // ... file write, performance logging ...
}
```

**Lines Changed in Existing Code:** ~5 lines (section call)
**Lines Added (New Methods):** ~40-50 lines
**Total Story 5.4 Code Changes:** ~45-55 lines in TextReportGenerator.cs

### Testing Requirements

**Test Strategy: Extend Existing Test Class**

Story 5.4 adds ~9-10 new tests to the existing `TextReportGeneratorTests.cs` extended in Stories 5.1, 5.2, and 5.3:

```
tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs
├── [Existing Story 5.1 tests: 15 tests] ✅
├── [Existing Story 5.2 tests: ~9 tests]  ✅
├── [Existing Story 5.3 tests: ~12 tests]  ✅
└── [New Story 5.4 tests: ~9-10 tests]    ← THIS STORY
```

**Total Tests After Story 5.4:** ~45-46 tests in TextReportGeneratorTests.cs

**New Test Coverage (Story 5.4):**

1. **Basic Recommendations Section Inclusion:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithRecommendations_IncludesRecommendationsSection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 20);
       var recommendations = CreateTestRecommendations(count: 10);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", recommendations: recommendations);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("CYCLE-BREAKING RECOMMENDATIONS");
           content.Should().Contain("Top 5 prioritized actions");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

2. **Top 5 Only Display:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithRecommendations_ShowsTop5Only()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 20);
       var recommendations = CreateTestRecommendations(count: 15);  // 15 recommendations
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", recommendations: recommendations);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify only top 5 shown (ranks 1-5)
           content.Should().Contain(" 1. Break:");
           content.Should().Contain(" 5. Break:");

           // Verify rank 6 NOT shown
           content.Should().NotContain(" 6. Break:");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

3. **Format Correctness:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithRecommendations_FormatsCorrectly()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var sourceProject = new ProjectNode("PaymentService", "PaymentService.csproj", "C:\\path");
       var targetProject = new ProjectNode("OrderManagement", "OrderManagement.csproj", "C:\\path");
       var recommendations = new List<CycleBreakingSuggestion>
       {
           new CycleBreakingSuggestion(
               cycleId: 1,
               sourceProject: sourceProject,
               targetProject: targetProject,
               couplingScore: 3,
               cycleSize: 5,
               rationale: "Weakest link in 5-project cycle") with { Rank = 1 }
       };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", recommendations: recommendations);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain(" 1. Break: PaymentService → OrderManagement (Coupling: 3 calls) - Weakest link in 5-project cycle");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

4. **Null Recommendations Scenario:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithNullRecommendations_DoesNotIncludeSection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", recommendations: null);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().NotContain("CYCLE-BREAKING RECOMMENDATIONS");

           // Should still have Stories 5.1/5.2/5.3 sections
           content.Should().Contain("DEPENDENCY OVERVIEW");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

5. **Empty Recommendations List:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithEmptyRecommendations_DoesNotIncludeSection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var recommendations = new List<CycleBreakingSuggestion>();  // Empty list
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", recommendations: recommendations);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().NotContain("CYCLE-BREAKING RECOMMENDATIONS");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

6. **Fewer Than 5 Recommendations:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithFewerThan5Recommendations_ShowsAllAvailable()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var recommendations = CreateTestRecommendations(count: 3);  // Only 3 recommendations
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", recommendations: recommendations);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify all 3 recommendations shown
           content.Should().Contain(" 1. Break:");
           content.Should().Contain(" 3. Break:");

           // Should not have rank 4 or higher
           content.Should().NotContain(" 4. Break:");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

7. **Section Order Verification:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_RecommendationsSection_AppearsAfterExtractionScores()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 20);
       var scores = CreateTestExtractionScores(count: 20);
       var recommendations = CreateTestRecommendations(count: 10);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: scores, recommendations: recommendations);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           var extractionScoresIndex = content.IndexOf("EXTRACTION DIFFICULTY SCORES");
           var recommendationsIndex = content.IndexOf("CYCLE-BREAKING RECOMMENDATIONS");

           extractionScoresIndex.Should().BeGreaterThan(0, "Extraction Scores should exist");
           recommendationsIndex.Should().BeGreaterThan(extractionScoresIndex,
               "Recommendations should appear after Extraction Scores");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

8. **Single Coupling Call Formatting:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithSingleCoupling_FormatsAsSingleCall()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 5);
       var sourceProject = new ProjectNode("ServiceA", "ServiceA.csproj", "C:\\path");
       var targetProject = new ProjectNode("ServiceB", "ServiceB.csproj", "C:\\path");
       var recommendations = new List<CycleBreakingSuggestion>
       {
           new CycleBreakingSuggestion(
               cycleId: 1,
               sourceProject: sourceProject,
               targetProject: targetProject,
               couplingScore: 1,  // Single call
               cycleSize: 3,
               rationale: "Weakest link in small 3-project cycle, only 1 method call") with { Rank = 1 }
       };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", recommendations: recommendations);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("(Coupling: 1 call)");  // Singular, not "1 calls"
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

9. **Multiple Couplings Formatting:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithMultipleCouplings_FormatsAsPlural()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 5);
       var sourceProject = new ProjectNode("ServiceA", "ServiceA.csproj", "C:\\path");
       var targetProject = new ProjectNode("ServiceB", "ServiceB.csproj", "C:\\path");
       var recommendations = new List<CycleBreakingSuggestion>
       {
           new CycleBreakingSuggestion(
               cycleId: 1,
               sourceProject: sourceProject,
               targetProject: targetProject,
               couplingScore: 5,  // Multiple calls
               cycleSize: 4,
               rationale: "Weakest link in 4-project cycle, only 5 method calls") with { Rank = 1 }
       };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", recommendations: recommendations);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("(Coupling: 5 calls)");  // Plural
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

10. **Formatting with Correct Separators:**
    ```csharp
    [Fact]
    public async Task GenerateAsync_RecommendationsSection_UsesCorrectSeparators()
    {
        // Arrange
        var graph = CreateTestGraph(projectCount: 20);
        var recommendations = CreateTestRecommendations(count: 10);
        var outputDir = CreateTempDirectory();

        try
        {
            // Act
            var reportPath = await _generator.GenerateAsync(
                graph, outputDir, "TestSolution", recommendations: recommendations);

            // Assert
            var content = await File.ReadAllTextAsync(reportPath);

            // Verify section separators (80 '=' characters)
            var separator = new string('=', 80);
            content.Should().Contain(separator);

            // Verify section header
            content.Should().Contain("CYCLE-BREAKING RECOMMENDATIONS");
        }
        finally
        {
            CleanupTempDirectory(outputDir);
        }
    }
    ```

**Test Helper Methods (New):**

```csharp
// Helper: Create test recommendations with configurable count
private IReadOnlyList<CycleBreakingSuggestion> CreateTestRecommendations(int count = 10)
{
    var recommendations = new List<CycleBreakingSuggestion>();
    var random = new Random(42);  // Fixed seed for reproducible tests

    for (int i = 0; i < count; i++)
    {
        var sourceProject = new ProjectNode($"SourceProject{i}", $"SourceProject{i}.csproj", "C:\\test");
        var targetProject = new ProjectNode($"TargetProject{i}", $"TargetProject{i}.csproj", "C:\\test");
        var couplingScore = i + 1;  // Sequential coupling scores (1, 2, 3, ...)
        var cycleSize = random.Next(3, 12);  // Random cycle size 3-12

        var recommendation = new CycleBreakingSuggestion(
            cycleId: i,
            sourceProject: sourceProject,
            targetProject: targetProject,
            couplingScore: couplingScore,
            cycleSize: cycleSize,
            rationale: $"Weakest link in {cycleSize}-project cycle, only {couplingScore} method calls");

        // Set rank (1-based)
        recommendations.Add(recommendation with { Rank = i + 1 });
    }

    return recommendations;
}

// Existing helpers from Stories 5.1/5.2/5.3 (reused):
private DependencyGraph CreateTestGraph(int projectCount = 10, int edgeCount = 15) { ... }
private IReadOnlyList<CycleInfo> CreateTestCycles(int cycleCount = 3) { ... }  // Story 5.2
private IReadOnlyList<ExtractionScore> CreateTestExtractionScores(int count = 20, bool randomize = true) { ... }  // Story 5.3
private string CreateTempDirectory() { ... }
private void CleanupTempDirectory(string path) { ... }
```

**Test Execution Strategy:**

1. **Run existing Stories 5.1/5.2/5.3 tests first:** Verify no regressions (~36 tests should still pass)
2. **Add Story 5.4 tests incrementally:** One test at a time during implementation
3. **Final validation:** All ~45-46 tests pass before marking story as done

**Test Coverage After Story 5.4:**

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
├── Story 5.2 tests: ~9 tests ✅ (no changes)
│   ├── Cycle section inclusion
│   ├── Cycle count accuracy
│   ├── Participation percentage
│   ├── Largest cycle size
│   ├── Detailed cycle listing
│   ├── No cycles scenarios
│   └── Section order validation
│
├── Story 5.3 tests: ~12 tests ✅ (no changes)
│   ├── Extraction section inclusion
│   ├── Top 10 easiest candidates
│   ├── Bottom 10 hardest candidates
│   ├── Metric formatting correctness
│   ├── Null/empty scores handling
│   ├── Fewer than 10 projects
│   ├── Section order validation
│   ├── Zero APIs formatting
│   ├── Complexity labels
│   └── Formatting separators
│
└── Story 5.4 tests: ~9-10 tests ← NEW
    ├── Recommendations section inclusion
    ├── Top 5 only display
    ├── Format correctness with arrow
    ├── Null recommendations handling
    ├── Empty recommendations handling
    ├── Fewer than 5 recommendations
    ├── Section order validation
    ├── Single call grammatical formatting
    ├── Multiple calls plural formatting
    └── Formatting with separators

Total: ~45-46 tests (comprehensive coverage for Epic 5 reporting through Story 5.4)
```

### Previous Story Intelligence

**From Story 5.3 (Immediate Predecessor) - Extraction Difficulty Scores Section:**

Story 5.3 extended TextReportGenerator with extraction difficulty scores section. Story 5.4 follows identical pattern:

**Key Patterns from Story 5.3:**

1. **Section Insertion Pattern:**
   ```csharp
   // Story 5.3 pattern:
   if (extractionScores != null && extractionScores.Count > 0)
   {
       AppendExtractionScores(report, extractionScores);
   }

   // Story 5.4 pattern (SAME):
   if (recommendations != null && recommendations.Count > 0)
   {
       AppendRecommendations(report, recommendations);
   }
   ```

2. **Private Helper Method Pattern:**
   ```csharp
   // Story 5.3 pattern:
   private void AppendExtractionScores(StringBuilder report, IReadOnlyList<ExtractionScore> extractionScores)

   // Story 5.4 pattern (SAME):
   private void AppendRecommendations(StringBuilder report, IReadOnlyList<CycleBreakingSuggestion> recommendations)
   ```

3. **Formatting Constants:**
   ```csharp
   private const int ReportWidth = 80;  // Established in Story 5.1

   // Story 5.4 reuses this for consistency:
   report.AppendLine(new string('=', ReportWidth));  // Section separator
   ```

4. **Number Formatting Standards:**
   ```csharp
   // Story 5.3 pattern:
   var apisText = externalApis == 0 ? "no external APIs" :
                  externalApis == 1 ? "1 API" :
                  $"{externalApis} APIs";

   // Story 5.4 pattern (SAME):
   var couplingText = couplingScore == 1 ? "1 call" : $"{couplingScore} calls";
   ```

5. **Null/Empty Handling:**
   ```csharp
   // Story 5.3 pattern: Null or empty list skips section (no message)
   if (extractionScores != null && extractionScores.Count > 0)
   {
       AppendExtractionScores(report, extractionScores);
   }

   // Story 5.4 pattern (SAME):
   if (recommendations != null && recommendations.Count > 0)
   {
       AppendRecommendations(report, recommendations);
   }
   ```

**From Story 5.3 Code Review Fixes:**

Story 5.3 underwent adversarial code review. Story 5.4 should learn from these:

1. **Null Item Validation:**
   - Story 5.3 review: Added validation to check for null items in extractionScores list
   - Story 5.4 lesson: Add similar validation for recommendations list (defensive programming)
   ```csharp
   if (recommendations.Any(r => r == null))
   {
       throw new ArgumentException("Recommendations list contains null items", nameof(recommendations));
   }
   ```

2. **Integration Testing:**
   - Story 5.3 review: Added integration test using real Epic 4 ExtractionScoreCalculator
   - Story 5.4 lesson: Add integration test using real Epic 3 RecommendationGenerator

3. **Dynamic Labels:**
   - Story 5.3 review: Changed from hardcoded "Score 0-33" to dynamic score ranges based on actual data
   - Story 5.4 lesson: No hardcoded ranges needed (recommendations already ranked 1-5)

**From Stories 5.1 and 5.2 (Foundation) - Report Generator Patterns:**

1. **Optional Parameters for Extensibility:**
   ```csharp
   // Story 5.1 interface design (forward-compatible):
   Task<string> GenerateAsync(
       DependencyGraph graph,
       string outputDirectory,
       string solutionName,
       IReadOnlyList<CycleInfo>? cycles = null,
       IReadOnlyList<ExtractionScore>? extractionScores = null,
       IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,  // ← Story 5.4 uses this
       CancellationToken cancellationToken = default);
   ```

2. **StringBuilder Pre-allocation:**
   ```csharp
   // Story 5.1 pattern: Pre-allocate capacity
   var report = new StringBuilder(capacity: 4096);

   // Story 5.4 continues this pattern (no change needed)
   ```

3. **Performance Logging:**
   ```csharp
   // Story 5.1 pattern: Track total generation time
   var startTime = DateTime.UtcNow;
   // ... generate report ...
   var elapsed = DateTime.UtcNow - startTime;
   _logger.LogInformation("Generated text report at {FilePath} in {ElapsedMs}ms", filePath, elapsed.TotalMilliseconds);

   // Story 5.4 continues this pattern (no change needed)
   ```

**From Epic 3 (Cycle Detection and Break-Point Analysis) - Data Source:**

Story 5.4 consumes cycle-breaking recommendations from Epic 3:

**Epic 3 CycleBreakingSuggestion Model (Story 3.5):**

```csharp
// Created by RecommendationGenerator (Epic 3, Story 3.5):
public sealed record CycleBreakingSuggestion : IComparable<CycleBreakingSuggestion>
{
    public int CycleId { get; init; }
    public ProjectNode SourceProject { get; init; }
    public ProjectNode TargetProject { get; init; }
    public int CouplingScore { get; init; }
    public int CycleSize { get; init; }
    public string Rationale { get; init; }
    public int Rank { get; init; }  // Priority ranking (1 = highest)
}
```

**Epic 3 Recommendation Flow:**

```
Story 3.1: Detect cycles using Tarjan's algorithm
        ↓
Story 3.2: Calculate cycle statistics
        ↓
Story 3.3: Analyze coupling strength (method call counting)
        ↓
Story 3.4: Identify weakest coupling edges within cycles
        ↓
Story 3.5: Generate ranked cycle-breaking recommendations
        ↓
[IReadOnlyList<CycleBreakingSuggestion>] (sorted by rank)
        ↓
Story 5.4: Report recommendations in text format (THIS STORY)
```

**Implementation Takeaways:**

1. ✅ **Follow Story 5.3's section generation pattern** - private helper methods, null handling
2. ✅ **Maintain Stories 5.1/5.2/5.3's formatting consistency** - 80-char separators, number formatting
3. ✅ **Reuse Story 5.1's performance optimizations** - StringBuilder pre-allocation, LINQ Take(5)
4. ✅ **Learn from Story 5.3's code review** - null item validation, integration tests
5. ✅ **Consume Epic 3's CycleBreakingSuggestion as-is** - trust Rank property, no re-sorting
6. ✅ **Complete the reporting stack** - Story 5.4 is the last text report section before CSV export

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 5.4 completes Epic 5's text reporting stack progression:

```
Epic Progression:
Epic 2 (Dependency Analysis) → Epic 3 (Cycle Detection) → Epic 4 (Extraction Scoring) → Epic 5 (Reporting)

src/MasDependencyMap.Core/DependencyAnalysis/
├── [Stories 2.1-2.5: Dependency graph building] ✅ DONE
└── DependencyGraph.cs               # Consumed by Story 5.1

src/MasDependencyMap.Core/CycleAnalysis/
├── [Stories 3.1-3.7: Cycle detection and analysis] ✅ DONE
├── CycleInfo.cs                     # Consumed by Story 5.2
└── CycleBreakingSuggestion.cs       # Consumed by Story 5.4 ← THIS STORY

src/MasDependencyMap.Core/ExtractionScoring/
├── [Stories 4.1-4.8: Scoring and visualization] ✅ DONE
└── ExtractionScore.cs               # Consumed by Story 5.3

src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # Story 5.1 (unchanged)
└── TextReportGenerator.cs           # Story 5.1 + Story 5.2 + Story 5.3 + Story 5.4 ← MODIFIED
```

**Epic 5 Reporting Stack After Story 5.4:**

```
Story 5.1 (DONE): Text Report Generator Foundation
    ↓ Creates: ITextReportGenerator, TextReportGenerator
    ↓ Sections: Header, Dependency Overview
    ↓
Story 5.2 (DONE): Add Cycle Detection Section
    ↓ Extends: TextReportGenerator
    ↓ Sections: Header, Dependency Overview, Cycle Detection
    ↓
Story 5.3 (DONE): Add Extraction Difficulty Section
    ↓ Extends: TextReportGenerator
    ↓ Sections: Header, Dependency Overview, Cycle Detection, Extraction Scores
    ↓
Story 5.4 (THIS STORY): Add Recommendations Section
    ↓ Extends: TextReportGenerator
    ↓ Sections: Header, Dependency Overview, Cycle Detection, Extraction Scores, Recommendations
    ↓
[Complete Text Report] ✅ TEXT REPORTING COMPLETE
    ↓
Stories 5.5-5.7: CSV Export (New Components)
    ↓
Story 5.8: Spectre.Console CLI Integration
```

**Cross-Epic Dependencies:**

Story 5.4 completes the integration between Epic 3 and Epic 5:

```
Epic 3 Output:
- CycleBreakingSuggestion objects (Ranked recommendations)
- Top N prioritized break points by coupling and cycle size

        ↓

Story 5.4 Integration:
- Consumes IReadOnlyList<CycleBreakingSuggestion>
- Takes top 5 by Rank (pre-sorted)
- Formats for stakeholder readability with arrow symbol

        ↓

Epic 5 Output:
- Text report with cycle-breaking recommendations section
- Human-readable actionable recommendations
- Stakeholder-ready migration guidance
```

**No Impact on Other Epics:**

```
Epic 1 (Foundation): ✅ No changes
Epic 2 (Dependency Analysis): ✅ No changes
Epic 3 (Cycle Detection): ✅ No changes (pure consumption)
Epic 4 (Extraction Scoring): ✅ No changes
Epic 6 (Future): Not yet started
```

**Epic 5 Roadmap After Story 5.4:**

After Story 5.4, Epic 5 continues with CSV export capabilities:

1. ✅ Story 5.1: Text Report Generator foundation (DONE)
2. ✅ Story 5.2: Add Cycle Detection section (DONE)
3. ✅ Story 5.3: Add Extraction Difficulty section (DONE)
4. 🔨 Story 5.4: Add Recommendations section (THIS STORY) ← TEXT REPORTING COMPLETE
5. ⏳ Story 5.5: CSV export for extraction scores (new CsvExporter component)
6. ⏳ Story 5.6: CSV export for cycle analysis
7. ⏳ Story 5.7: CSV export for dependency matrix
8. ⏳ Story 5.8: Spectre.Console table formatting (CLI layer integration)

**After Story 5.4:**
- **Text report sections:** 5/5 complete ✅ (Header, Dependency Overview, Cycle Detection, Extraction Scores, Recommendations)
- **Text reporting:** COMPLETE
- **CSV export:** Not started (Stories 5.5-5.7)
- **CLI integration:** Not started (Story 5.8)

**File Modification Summary (Story 5.4):**

```
MODIFIED (2 files):
- src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs
  - Modify GenerateAsync: Add recommendations section call
  - Add new method: AppendRecommendations
  - Add new helper: FormatCouplingCalls

- tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs
  - Add helper: CreateTestRecommendations
  - Add ~9-10 new test methods

UNCHANGED (Backward compatible):
- ITextReportGenerator.cs (interface already supports recommendations parameter)
- All Epic 1-4 components
- All Epic 5 Stories 5.1/5.2/5.3 test coverage (~36 existing tests still pass)
```

**Development Workflow Impact:**

Story 5.4 follows the same development pattern as Stories 5.1, 5.2, and 5.3:

1. **Implementation Phase:**
   - Modify TextReportGenerator.cs
   - Add AppendRecommendations private method and helper
   - Update GenerateAsync to call new method

2. **Testing Phase:**
   - Add helper methods for recommendation creation
   - Add ~9-10 new tests
   - Run all tests (existing + new)

3. **Validation Phase:**
   - Verify all ~45-46 project tests pass
   - Verify no regressions in Stories 5.1/5.2/5.3 functionality
   - Manual test: Generate report with recommendations from test solution

4. **Code Review Phase (Expected):**
   - Stories 5.1, 5.2, and 5.3 all had code review findings
   - Story 5.4 should expect similar scrutiny
   - Prepare for adversarial review focusing on:
     - Format correctness (arrow symbol, coupling calls)
     - Top 5 limiting logic (Take(5))
     - Grammatical correctness ("1 call" vs "3 calls")
     - Section order (appears after extraction scores)
     - Null item validation

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-5-comprehensive-reporting-and-data-export.md, Story 5.4 (lines 59-73)]
- Story requirements: Cycle-breaking recommendations section with top 5 suggestions, format with arrow, coupling score, rationale

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
- Number formatting: N0 for integers, grammatical correctness for units

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\5-3-add-extraction-difficulty-scoring-section-to-text-reports.md]
- Story 5.3 implementation: Extended TextReportGenerator with extraction difficulty scores section
- Helper method pattern: AppendExtractionScores (Story 5.4 follows same pattern)
- Formatting constants: ReportWidth = 80, separator patterns
- Performance: LINQ Take(N) for limiting output
- Code review learnings: Null item validation, integration tests, dynamic labels

**Epic 3 Integration:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-3-circular-dependency-detection-and-break-point-analysis.md, Story 3.5 (lines 79-94)]
- CycleBreakingSuggestion model: CycleId, SourceProject, TargetProject, CouplingScore, CycleSize, Rationale, Rank
- Story 3.5: RecommendationGenerator implementation (generates ranked recommendations)
- Rationale format: "Weakest link in {cycleSize}-project cycle, {couplingDescription}"
- Sorting: Lowest coupling first, then largest cycle size, then alphabetical

**Epic 3 Implementation Files:**
- [Source: src\MasDependencyMap.Core\CycleAnalysis\CycleBreakingSuggestion.cs (lines 1-95)]
- CycleBreakingSuggestion record with IComparable implementation
- Natural ordering: Coupling score (asc), cycle size (desc), project name (asc)
- Rank property set by RecommendationGenerator after sorting
- [Source: src\MasDependencyMap.Core\CycleAnalysis\RecommendationGenerator.cs (lines 1-123)]
- GenerateRecommendationsAsync returns ranked recommendations
- Rationale generation: Determines impact context and coupling description
- Ranks assigned during LINQ chain (1-based)

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Most recent: Story 5.3 completed with code review fixes (35baccd)
- Pattern: Story implementation → code review → fixes → status update
- Story 5.3 most recent: "Code review fixes for Story 5-3: Add extraction difficulty scoring section to text reports"

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - Implementation completed without issues

### Completion Notes List

✅ **Story 5.4 Implementation Complete**

**Implementation Summary:**
- Extended `TextReportGenerator.GenerateAsync` to activate the `recommendations` parameter (already present from Story 5.1)
- Implemented `AppendRecommendations` private method following Stories 5.2/5.3 section pattern
- Added `FormatCouplingCalls` helper method for grammatical correctness (singular "1 call" vs plural "3 calls")
- Added null item validation for defensive programming (consistent with Story 5.3 code review fixes)
- Section correctly positioned after Extraction Difficulty Scores section (final content section)

**Testing:**
- Added 12 comprehensive unit tests to existing `TextReportGeneratorTests.cs` (11 original + 1 code review fix)
- All 54 tests pass (42 existing + 12 new)
- Test coverage includes:
  - Section inclusion/exclusion (null, empty, populated lists)
  - Top 5 limiting (with 3, 5, 10+ recommendations)
  - Format correctness (arrow symbol, coupling calls, rationale)
  - Section order validation
  - Grammatical correctness (singular vs plural)
  - Null item validation
  - Separator consistency
  - **[Code Review Fix]** Integration test with real Epic 3 RecommendationGenerator

**Architecture Compliance:**
- Existing namespace: `MasDependencyMap.Core.Reporting`
- Private helper methods (no public API changes)
- Structured logging with named placeholders
- ReportWidth constant (80 characters) for consistency
- Single-pass LINQ `Take(5)` for performance
- UTF-8 encoding without BOM (plain text standard)

**Epic 3 Integration:**
- Successfully consumes `CycleBreakingSuggestion` from Epic 3
- Uses properties: `Rank`, `SourceProject.ProjectName`, `TargetProject.ProjectName`, `CouplingScore`, `Rationale`
- Trusts `Rank` property from `RecommendationGenerator` (already sorted correctly)
- No changes to Epic 3 components (pure consumer)

**Performance:**
- Top 5 recommendations: O(5) = O(1) operation (already sorted)
- No sorting needed (Epic 3 pre-sorted by coupling score, cycle size)
- Negligible overhead (<1ms for 5 line formatting)
- Total report generation remains well within 10-second budget

**Backward Compatibility:**
- Null recommendations: Report generates with Stories 5.1-5.3 sections only
- Empty recommendations: Section skipped gracefully
- No breaking changes to existing callers

**Code Review Fixes Applied:**
1. **HIGH - Incomplete File List:** Added sprint-status.yaml to File List documentation
2. **MEDIUM - Missing Integration Test:** Added integration test using real Epic 3 RecommendationGenerator to verify end-to-end flow with CycleInfo objects containing WeakCouplingEdges
3. **MEDIUM - Defensive Validation Analysis:** Investigated null SourceProject/TargetProject validation - determined Epic 3's CycleBreakingSuggestion constructor already validates these properties at construction time, making additional validation in TextReportGenerator redundant
4. **Test Coverage:** Added 1 integration test with real Epic 3 components, bringing total Story 5.4 tests to 12

### File List

**Modified:**
- `src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs` - Added AppendRecommendations and FormatCouplingCalls methods, activated recommendations parameter with null item validation
- `tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs` - Added 12 comprehensive tests for Story 5.4 (11 original + 1 code review integration test with real Epic 3 RecommendationGenerator)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - Updated story 5-4 status to "done" (via code review workflow)
- `_bmad-output/implementation-artifacts/5-4-add-cycle-breaking-recommendations-to-text-reports.md` - Updated story file with completion status and code review fixes

**No New Files Created** - Story 5.4 is a pure extension of Stories 5.1-5.3
