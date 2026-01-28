# Story 5.3: Add Extraction Difficulty Scoring Section to Text Reports

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want extraction difficulty scores included in text reports,
So that I can see ranked candidates and their metrics.

## Acceptance Criteria

**Given** Extraction scores are calculated
**When** Text report is generated
**Then** Extraction Difficulty Scores section includes top 10 easiest candidates
**And** Each candidate shows: rank, project name, score, incoming refs, outgoing refs, external APIs
**And** Bottom 10 hardest candidates are listed with their complexity indicators
**And** Report format example: "1. NotificationService (Score: 23) - 3 incoming, 2 outgoing, no external APIs"
**And** Score ranges are explained: "Easiest Candidates (Score 0-33)", "Hardest Candidates (Score 67-100)"

## Tasks / Subtasks

- [x] Update TextReportGenerator.GenerateAsync to use extractionScores parameter (AC: Include extraction scores section)
  - [x] Check if extractionScores parameter is not null and has elements
  - [x] If scores provided: Call AppendExtractionScores(report, extractionScores) helper method
  - [x] If no scores or empty list: Call AppendExtractionScores(report, null) to skip section
  - [x] Insert extraction scores section after Cycle Detection section (before future Story 5.4 recommendations)

- [x] Implement AppendExtractionScores private method (AC: Top 10 easiest candidates)
  - [x] Accept StringBuilder report and IReadOnlyList<ExtractionScore>? extractionScores parameters
  - [x] Add section header: "EXTRACTION DIFFICULTY SCORES" with separator (80 '=' characters)
  - [x] Handle null/empty scores: Return early without section (backward compatibility)
  - [x] Sort scores ascending (low score = easy extraction) and take top 10
  - [x] Add subsection header: "Easiest Candidates (Score 0-33)"
  - [x] Format each candidate with rank, name, score, and metrics

- [x] Format easiest candidates with detailed metrics (AC: Each candidate shows metrics)
  - [x] Use 1-based ranking: "1. ProjectName (Score: X)"
  - [x] Include incoming references count from ExtractionScore.CouplingMetric.IncomingCount
  - [x] Include outgoing references count from ExtractionScore.CouplingMetric.OutgoingCount
  - [x] Include external API count from ExtractionScore.ExternalApiMetric.EndpointCount
  - [x] Format: "1. NotificationService (Score: 23) - 3 incoming, 2 outgoing, no external APIs"
  - [x] Handle zero counts: "no external APIs" instead of "0 external APIs"
  - [x] Use "1 API" singular vs "2 APIs" plural for grammatical correctness

- [x] Display bottom 10 hardest candidates (AC: Bottom 10 hardest candidates)
  - [x] Sort scores descending (high score = hard extraction) and take top 10
  - [x] Add subsection header: "Hardest Candidates (Score 67-100)"
  - [x] Format each candidate with rank, name, score, and complexity indicators
  - [x] Include coupling metric, complexity metric, tech debt score as complexity indicators
  - [x] Format: "1. LegacyCore (Score: 89) - High coupling (75), High complexity (82), Tech debt (12)"
  - [x] Blank line for spacing between sections

- [x] Add comprehensive unit tests (AC: Test coverage)
  - [x] Create tests in TextReportGeneratorTests.cs (extends existing test class)
  - [x] Test: GenerateAsync_WithExtractionScores_IncludesExtractionSection (verify section exists)
  - [x] Test: GenerateAsync_WithExtractionScores_ShowsTop10Easiest (verify top 10 ascending)
  - [x] Test: GenerateAsync_WithExtractionScores_ShowsBottom10Hardest (verify top 10 descending)
  - [x] Test: GenerateAsync_WithExtractionScores_FormatsMetricsCorrectly (verify detailed format)
  - [x] Test: GenerateAsync_WithNullScores_DoesNotIncludeExtractionSection (null parameter)
  - [x] Test: GenerateAsync_WithEmptyScores_DoesNotIncludeExtractionSection (empty list)
  - [x] Test: GenerateAsync_WithFewerThan10Scores_ShowsAllAvailable (edge case: 5 projects)
  - [x] Test: GenerateAsync_ExtractionSection_AppearsAfterCycleDetection (section order)
  - [x] Test: GenerateAsync_WithZeroExternalApis_FormatsAsNoApis (grammatical correctness)
  - [x] Test: GenerateAsync_HardestCandidates_ShowsComplexityLabels (verify complexity indicators)
  - [x] Test: GenerateAsync_WithExtractionScores_FormatsWithCorrectSeparators (formatting consistency)
  - [x] Test: GenerateAsync_WithLargeExtractionScoresList_CompletesWithinPerformanceBudget (performance validation)
  - [x] Use helper methods: CreateTestExtractionScores() for generating test data

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Existing namespace: MasDependencyMap.Core.Reporting (no new namespace)
  - [x] Private method: AppendExtractionScores (helper, not public API)
  - [x] Consistent formatting: Use ReportWidth constant (80 characters)
  - [x] Structured logging: Add log message for extraction scores section generation
  - [x] Performance: Single-pass LINQ queries for sorting/filtering (no nested loops)
  - [x] Test organization: Add tests to existing TextReportGeneratorTests.cs

- [x] Verify integration with Epic 4 ExtractionScore model (AC: Consume extraction data correctly)
  - [x] Use ExtractionScore properties: ProjectName, FinalScore (not Score), CouplingMetric, ComplexityMetric, TechDebtMetric, ExternalApiMetric
  - [x] Verify ExtractionScore namespace import: using MasDependencyMap.Core.ExtractionScoring;
  - [x] Ensure ExtractionScore contains all necessary data (already defined in Epic 4)
  - [x] No changes to ExtractionScore model needed (Story 5.3 is pure consumer)
  - [x] Use CouplingMetric.NormalizedScore, ComplexityMetric.NormalizedScore, TechDebtMetric.NormalizedScore for hardest candidates complexity indicators

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 5.3 Extends Story 5.2 Foundation:**

This story **extends** the existing `TextReportGenerator` created in Story 5.1 and extended in Story 5.2. **NO new classes** are created - only the implementation of `TextReportGenerator.cs` is modified.

**Epic 5 Reporting Stack Progress:**
- ✅ Story 5.1: Text Report Generator with Summary Statistics (FOUNDATION - DONE)
- ✅ Story 5.2: Add Cycle Detection section to text reports (EXTENDS - DONE)
- 🔨 Story 5.3: Add Extraction Difficulty Scoring section to text reports (THIS STORY - EXTENDS)
- ⏳ Story 5.4: Add Cycle-Breaking Recommendations to text reports (FUTURE - EXTENDS)
- ⏳ Story 5.5-5.7: CSV export capabilities (NEW COMPONENTS)
- ⏳ Story 5.8: Spectre.Console table formatting enhancements (CLI INTEGRATION)

**Story 5.3 Unique Characteristics:**

1. **Pure Extension of Existing Component:**
   - Story 5.1: Created ITextReportGenerator interface and TextReportGenerator implementation
   - Story 5.2: Added Cycle Detection section to TextReportGenerator
   - Story 5.3: Adds Extraction Difficulty Scores section to TextReportGenerator
   - **No new files created** - Only modifies src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs
   - **No new tests file** - Extends tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs

2. **Leverages Story 5.1's Forward-Compatible Interface:**
   - Story 5.1 interface included optional `extractionScores` parameter for exactly this purpose:
   ```csharp
   Task<string> GenerateAsync(
       DependencyGraph graph,
       string outputDirectory,
       string solutionName,
       IReadOnlyList<CycleInfo>? cycles = null,
       IReadOnlyList<ExtractionScore>? extractionScores = null,  // ← Story 5.3 uses this
       IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
       CancellationToken cancellationToken = default);
   ```
   - **Interface unchanged** - Story 5.1 designed for extensibility
   - **Implementation change only** - Activate the extractionScores parameter handling

3. **Consumes Epic 4 Extraction Scoring Results:**
   - Epic 4 (Stories 4.1-4.8): All done, ExtractionScore model exists and tested
   - Story 4.5: Created ExtractionScoreCalculator with configurable weights
   - Story 4.6: Generated ranked extraction candidate lists
   - Story 4.7-4.8: Heat map visualization (parallel effort, visual representation)
   - **Story 5.3 integration point:** Consumes ExtractionScore from Epic 4's scoring system
   - **No Epic 4 changes needed** - Pure consumption of existing data

4. **Section Insertion Strategy:**
   ```
   Report Structure After Story 5.3:

   ================================================================================
   MasDependencyMap Analysis Report
   ================================================================================
   [Header: Solution name, date, total projects]      ← Story 5.1

   DEPENDENCY OVERVIEW                                ← Story 5.1
   [Total references, framework/custom split]         ← Story 5.1

   CYCLE DETECTION                                    ← Story 5.2
   [Circular dependency chains, participation %]      ← Story 5.2
   [Largest cycle, detailed cycle list]               ← Story 5.2

   EXTRACTION DIFFICULTY SCORES                       ← Story 5.3 NEW
   [Top 10 easiest candidates with metrics]           ← Story 5.3 NEW
   [Bottom 10 hardest candidates with complexity]     ← Story 5.3 NEW

   [Future: CYCLE-BREAKING RECOMMENDATIONS]           ← Story 5.4
   ================================================================================
   ```
   - **Insert after:** Cycle Detection section (Story 5.2)
   - **Insert before:** Future recommendations section (Story 5.4)

5. **Top 10 + Bottom 10 Presentation:**
   - **Easiest Candidates:** Sort scores ascending, take top 10
     - Low scores (0-33) indicate easy extraction
     - Show: rank, name, score, incoming/outgoing refs, external APIs
     - Focus: Quantitative metrics for decision-making
   - **Hardest Candidates:** Sort scores descending, take top 10
     - High scores (67-100) indicate hard extraction
     - Show: rank, name, score, complexity indicators (coupling, complexity, tech debt)
     - Focus: Complexity indicators to explain difficulty
   - **Score Range Labels:** Guide stakeholders on interpreting scores

6. **Backward Compatibility Requirement:**
   - If `extractionScores` parameter is null: Report still generates with Stories 5.1-5.2 sections only
   - If `extractionScores` parameter is empty list: No section included (skip)
   - **No breaking changes** - Existing callers without extractionScores parameter continue to work
   - **Graceful degradation** - Report adapts to available data

🚨 **CRITICAL - ExtractionScore Model from Epic 4:**

Story 5.3 consumes the `ExtractionScore` model created in Epic 4 (Extraction Scoring). Understanding this model is critical for correct implementation.

**ExtractionScore Definition (from Epic 4):**

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents extraction difficulty score and supporting metrics for a project.
/// </summary>
public sealed record ExtractionScore(
    string ProjectName,
    int Score,                      // 0-100: Lower = easier extraction
    int IncomingReferences,         // Projects depending on this one
    int OutgoingReferences,         // Projects this one depends on
    int ExternalApiCount,           // Public APIs exposed
    double CouplingMetric,          // 0-100: Weighted coupling strength
    double ComplexityMetric,        // 0-100: Cyclomatic complexity normalized
    double TechDebtScore);          // 0-100: Technology version debt
```

**Important Properties:**
- `ProjectName`: Name of the project being scored
- `Score`: Overall extraction difficulty (0-100, lower = easier)
- `IncomingReferences`: Count of projects that depend on this project
- `OutgoingReferences`: Count of projects this project depends on
- `ExternalApiCount`: Number of public APIs exposed by this project
- `CouplingMetric`: Weighted coupling strength (0-100)
- `ComplexityMetric`: Normalized cyclomatic complexity (0-100)
- `TechDebtScore`: Technology version debt score (0-100)

**Story 5.3 Display Strategy:**

**Easiest Candidates Format:**
```
1. NotificationService (Score: 23) - 3 incoming, 2 outgoing, no external APIs
2. EmailSender (Score: 28) - 1 incoming, 4 outgoing, 1 API
3. LoggingHelper (Score: 31) - 5 incoming, 1 outgoing, no external APIs
```

**Hardest Candidates Format:**
```
1. LegacyCore (Score: 89) - High coupling (45), High complexity (78), Tech debt (12)
2. DataAccessLayer (Score: 85) - High coupling (52), Moderate complexity (45), Tech debt (8)
3. AuthenticationManager (Score: 82) - Moderate coupling (38), High complexity (67), Tech debt (15)
```

**Formatting Rules:**

1. **Easiest Candidates:**
   - Focus: Quantitative dependency metrics
   - Format: `{Rank}. {ProjectName} (Score: {Score}) - {Incoming} incoming, {Outgoing} outgoing, {APIs}`
   - Special cases:
     - `no external APIs` instead of `0 external APIs`
     - `1 API` (singular) vs `2 APIs` (plural)
   - Reasoning: Stakeholders care about "how connected is this?" for easy extractions

2. **Hardest Candidates:**
   - Focus: Complexity indicators explaining difficulty
   - Format: `{Rank}. {ProjectName} (Score: {Score}) - {CouplingLabel}, {ComplexityLabel}, Tech debt ({TechDebtScore})`
   - Complexity labels:
     - Coupling: High (>60), Moderate (30-60), Low (<30)
     - Complexity: High (>60), Moderate (30-60), Low (<30)
   - Reasoning: Stakeholders need to understand "why is this hard?" for difficult extractions

**Score Range Explanation:**

Story 5.3 acceptance criteria: "Score ranges are explained"

```
Easiest Candidates (Score 0-33)
These projects have minimal dependencies and low complexity, making them ideal candidates for extraction.

[... list of top 10 easiest ...]

Hardest Candidates (Score 67-100)
These projects have high coupling, complexity, or technical debt, requiring significant refactoring effort.

[... list of top 10 hardest ...]
```

**Handling Edge Cases:**

1. **Fewer than 10 projects:**
   - Show all available projects (e.g., 5 easiest, 5 hardest if only 5 projects total)
   - Don't pad with empty entries

2. **Exactly 10 projects:**
   - Top 10 easiest = all projects
   - Bottom 10 hardest = all projects (same list, reversed)
   - Still show both sections for consistency

3. **Null or empty scores:**
   - Skip section entirely (backward compatibility)
   - No "No scores calculated" message (unlike cycles section)
   - Reasoning: Extraction scoring is optional feature, not core analysis

🚨 **CRITICAL - Report Formatting Pattern (Story 5.1/5.2 Consistency):**

Story 5.3 must match Stories 5.1 and 5.2 formatting conventions for visual consistency.

**Story 5.3 Section Format (Target):**

```
EXTRACTION DIFFICULTY SCORES
================================================================================

Easiest Candidates (Score 0-33)
These projects have minimal dependencies and low complexity, making them ideal
candidates for extraction.

 1. NotificationService (Score: 23) - 3 incoming, 2 outgoing, no external APIs
 2. EmailSender (Score: 28) - 1 incoming, 4 outgoing, 1 API
 3. LoggingHelper (Score: 31) - 5 incoming, 1 outgoing, no external APIs
 4. CacheManager (Score: 32) - 2 incoming, 3 outgoing, no external APIs
 5. ConfigurationReader (Score: 33) - 4 incoming, 2 outgoing, 2 APIs
 6. DateTimeHelper (Score: 33) - 6 incoming, 1 outgoing, no external APIs
 7. StringUtilities (Score: 34) - 8 incoming, 0 outgoing, no external APIs
 8. ValidationEngine (Score: 35) - 3 incoming, 5 outgoing, 1 API
 9. ReportBuilder (Score: 36) - 2 incoming, 4 outgoing, 3 APIs
10. MetricsCollector (Score: 37) - 4 incoming, 3 outgoing, no external APIs

Hardest Candidates (Score 67-100)
These projects have high coupling, complexity, or technical debt, requiring
significant refactoring effort.

 1. LegacyCore (Score: 89) - High coupling (45), High complexity (78), Tech debt (12)
 2. DataAccessLayer (Score: 85) - High coupling (52), Moderate complexity (45), Tech debt (8)
 3. AuthenticationManager (Score: 82) - Moderate coupling (38), High complexity (67), Tech debt (15)
 4. BusinessRulesEngine (Score: 78) - High coupling (62), Moderate complexity (48), Tech debt (6)
 5. ReportingFramework (Score: 75) - Moderate coupling (45), High complexity (71), Tech debt (10)
 6. WorkflowOrchestrator (Score: 72) - High coupling (68), Moderate complexity (52), Tech debt (4)
 7. IntegrationHub (Score: 70) - Moderate coupling (42), High complexity (65), Tech debt (9)
 8. PaymentGateway (Score: 69) - Moderate coupling (51), Moderate complexity (58), Tech debt (11)
 9. SecurityFramework (Score: 68) - High coupling (71), Moderate complexity (44), Tech debt (7)
10. MessagingInfrastructure (Score: 67) - Moderate coupling (47), High complexity (63), Tech debt (13)

================================================================================
```

**Formatting Rules (from Story 5.1/5.2):**

1. **Section Headers:**
   - ALL CAPS section name (e.g., "EXTRACTION DIFFICULTY SCORES")
   - 80 '=' characters separator line
   - Blank line after separator

2. **Subsection Headers:**
   - Title Case with score range (e.g., "Easiest Candidates (Score 0-33)")
   - Explanatory text (1-2 lines, wrapped at ~80 chars)
   - Blank line before list

3. **Ranking Format:**
   - Right-aligned rank with padding: ` 1.`, ` 2.`, ..., `10.` (space for alignment)
   - Consistent spacing for visual alignment of project names
   - One candidate per line

4. **Metric Formatting:**
   - Use integer scores (no decimal places): `Score: 23` not `Score: 23.4`
   - Grammatical correctness: "no external APIs", "1 API", "2 APIs"
   - Complexity labels in parentheses: `High coupling (45)`

5. **Section Closing:**
   - Blank line before section separator
   - 80 '=' characters separator
   - Blank line after separator (ready for next section)

**ReportWidth Constant (from Story 5.1):**

```csharp
private const int ReportWidth = 80;  // Standard terminal width for formatting

// Usage in Story 5.3:
report.AppendLine(new string('=', ReportWidth));  // Main separator
```

**Number Formatting Examples:**

```csharp
// Integer score (no decimals)
report.AppendLine($" {rank,2}. {projectName} (Score: {score:N0})");
// Output: " 1. NotificationService (Score: 23)"

// Reference counts with grammatical correctness
var incomingText = $"{incomingRefs} incoming";
var outgoingText = $"{outgoingRefs} outgoing";
var apisText = externalApis == 0 ? "no external APIs" :
               externalApis == 1 ? "1 API" :
               $"{externalApis} APIs";
report.AppendLine($" {rank,2}. {projectName} (Score: {score}) - {incomingText}, {outgoingText}, {apisText}");
```

**Complexity Label Formatting:**

```csharp
// Complexity label generation for hardest candidates
private string GetComplexityLabel(double metric, string metricName)
{
    var level = metric switch
    {
        >= 60 => "High",
        >= 30 => "Moderate",
        _ => "Low"
    };
    return $"{level} {metricName} ({metric:F0})";
}

// Usage:
var couplingLabel = GetComplexityLabel(score.CouplingMetric, "coupling");
var complexityLabel = GetComplexityLabel(score.ComplexityMetric, "complexity");
var techDebtText = $"Tech debt ({score.TechDebtScore:F0})";

report.AppendLine($" {rank,2}. {projectName} (Score: {score.Score}) - {couplingLabel}, {complexityLabel}, {techDebtText}");
```

🚨 **CRITICAL - Performance Considerations:**

Story 5.1 Acceptance Criteria: "File is generated within 10 seconds regardless of solution size"

Story 5.3 adds extraction scores section - must maintain this performance guarantee.

**Performance Analysis:**

1. **Story 5.1 + 5.2 Baseline:**
   - Header generation: O(1)
   - Dependency overview: O(edges)
   - Cycle detection section: O(projects + cycles * avg_cycle_size)
   - **Total:** O(edges + projects) - typically <2 seconds for 5000 edges

2. **Story 5.3 Additions:**
   - Sort scores ascending: O(n log n) where n = project count
   - Take top 10: O(10) = O(1)
   - Sort scores descending: O(n log n)
   - Take top 10: O(10) = O(1)
   - Format each candidate: O(20) = O(1) for 20 total candidates (10 + 10)
   - **Total Story 5.3:** O(n log n) where n = project count

3. **Combined Performance:**
   - Story 5.1 + 5.2 + 5.3: O(edges + projects + n log n)
   - For typical legacy solution:
     - Projects: 50-400
     - Edges: 1000-5000
     - Sorting: 400 * log(400) ≈ 3200 operations
   - **Worst case:** 5000 edges + 400 projects + 3200 sort operations = ~8600 operations
   - **Expected time:** <2 seconds (well within 10-second limit)

4. **Optimization Strategies:**
   - ✅ Use LINQ OrderBy (optimized quicksort, O(n log n))
   - ✅ Take(10) early termination (don't materialize full sorted list)
   - ✅ Single sort operation per list (ascending/descending)
   - ✅ No nested loops or redundant sorts

**Memory Considerations:**

```csharp
// Efficient: Sort and take top 10 in single query
var easiestCandidates = extractionScores
    .OrderBy(s => s.Score)
    .Take(10)
    .ToList();  // Materialize only top 10

// Efficient: Reverse sort for hardest
var hardestCandidates = extractionScores
    .OrderByDescending(s => s.Score)
    .Take(10)
    .ToList();  // Materialize only top 10

// Inefficient (AVOID): Sorting full list unnecessarily
var allSorted = extractionScores.OrderBy(s => s.Score).ToList();  // ← Creates full list
var easiest = allSorted.Take(10);  // ← Already have full list in memory
```

**Logging Performance:**

```csharp
// Add logging for Story 5.3 section generation
_logger.LogDebug("Appending extraction scores section: {ScoreCount} projects, showing top/bottom 10",
    extractionScores.Count);

// Existing Story 5.1 performance logging (unchanged):
var elapsed = DateTime.UtcNow - startTime;
_logger.LogInformation(
    "Generated text report at {FilePath} in {ElapsedMs}ms",
    filePath,
    elapsed.TotalMilliseconds);
```

**No Performance Concerns for Story 5.3:** Extraction scores section adds negligible overhead (<50ms for 400 projects).

### Technical Requirements

**Modified Component: TextReportGenerator.cs (from Stories 5.1 and 5.2)**

Story 5.3 modifies the existing TextReportGenerator implementation created in Story 5.1 and extended in Story 5.2:

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # UNCHANGED (interface already supports extractionScores parameter)
└── TextReportGenerator.cs           # MODIFIED: Add AppendExtractionScores method, use extractionScores parameter

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs      # MODIFIED: Add ~9-11 new tests for extraction scores section
```

**New Dependencies (Epic 4 Integration):**

Story 5.3 adds explicit dependency on Epic 4's ExtractionScoring namespace:

```csharp
using MasDependencyMap.Core.ExtractionScoring;  // For ExtractionScore model
```

**Existing Dependencies (from Stories 5.1 and 5.2):**

All Story 5.1/5.2 dependencies remain:

```csharp
using System.Text;                              // For StringBuilder
using MasDependencyMap.Core.DependencyAnalysis; // For DependencyGraph
using MasDependencyMap.Core.CycleAnalysis;      // For CycleInfo (Story 5.2)
using Microsoft.Extensions.Logging;             // For ILogger<T>
```

**No New NuGet Packages Required:**

All dependencies already installed from previous epics:
- ✅ Epic 1: Microsoft.Extensions.Logging.Console
- ✅ Epic 2: QuikGraph (DependencyGraph)
- ✅ Epic 3: CycleAnalysis components (CycleInfo)
- ✅ Epic 4: ExtractionScoring components (ExtractionScore)
- ✅ Built-in: System.Text.StringBuilder, System.Linq

**DI Registration:**

No changes to DI registration - TextReportGenerator already registered in Story 5.1:

```csharp
// CLI Program.cs (unchanged from Story 5.1)
services.AddSingleton<ITextReportGenerator, TextReportGenerator>();
```

**Private Helper Method Pattern:**

Story 5.3 follows Story 5.1/5.2 pattern of private helper methods:

```csharp
// Story 5.1 pattern:
private void AppendHeader(StringBuilder report, string solutionName, DependencyGraph graph)
private void AppendDependencyOverview(StringBuilder report, DependencyGraph graph)

// Story 5.2 pattern:
private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo> cycles)

// Story 5.3 pattern (SAME STYLE):
private void AppendExtractionScores(StringBuilder report, IReadOnlyList<ExtractionScore> extractionScores)
private string GetComplexityLabel(double metric, string metricName)  // NEW: Helper for complexity labels
private string FormatExternalApis(int count)  // NEW: Helper for grammatical correctness

// Future:
// Story 5.4: private void AppendRecommendations(...)
```

**Method Organization:**

```csharp
public sealed class TextReportGenerator : ITextReportGenerator
{
    // Fields
    private readonly ILogger<TextReportGenerator> _logger;
    private const int ReportWidth = 80;
    private int _totalProjects;  // Story 5.2

    // Constructor
    public TextReportGenerator(ILogger<TextReportGenerator> logger) { }

    // Public API (interface implementation)
    public async Task<string> GenerateAsync(...) { }

    // Private helper methods (grouped by section)
    private void AppendHeader(...)                  // Story 5.1
    private void AppendDependencyOverview(...)      // Story 5.1
    private void AppendCycleDetection(...)          // Story 5.2
    private void AppendExtractionScores(...)        // Story 5.3 NEW
    // Future: AppendRecommendations(...)           // Story 5.4

    // Private utility methods
    private bool IsFrameworkReference(...)          // Story 5.1
    private int CountCrossSolutionReferences(...)   // Story 5.1
    private string SanitizeFileName(...)            // Story 5.1
    private string GetComplexityLabel(...)          // Story 5.3 NEW
    private string FormatExternalApis(...)          // Story 5.3 NEW
}
```

### Architecture Compliance

**Namespace Structure (Unchanged from Stories 5.1/5.2):**

Story 5.3 works within the existing `MasDependencyMap.Core.Reporting` namespace:

```csharp
namespace MasDependencyMap.Core.Reporting;  // File-scoped namespace (C# 10+)

using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.ExtractionScoring;  // NEW: Story 5.3 dependency
using Microsoft.Extensions.Logging;
```

**Structured Logging (Required):**

Add logging for extraction scores section generation:

```csharp
// Inside AppendExtractionScores method:
_logger.LogDebug("Appending extraction scores section: {ScoreCount} projects, showing top {TopCount}/bottom {BottomCount}",
    extractionScores.Count,
    Math.Min(10, extractionScores.Count),
    Math.Min(10, extractionScores.Count));
```

**No XML Documentation Changes:**

Since Story 5.3 only adds private helper methods, no XML documentation changes required:
- ITextReportGenerator interface: Already documented in Story 5.1 with extractionScores parameter
- TextReportGenerator.GenerateAsync: Already has `<inheritdoc />`
- Private methods: XML documentation not required by project-context.md

**Performance Logging (Unchanged):**

Story 5.1's existing performance logging captures Story 5.3's additions:

```csharp
// Existing Story 5.1 logging (captures total time including Stories 5.2 and 5.3):
var elapsed = DateTime.UtcNow - startTime;
_logger.LogInformation(
    "Generated text report at {FilePath} in {ElapsedMs}ms",
    filePath,
    elapsed.TotalMilliseconds);
```

### Library/Framework Requirements

**No New Libraries Required:**

Story 5.3 uses only existing dependencies from previous epics:

**From Epic 1 (Foundation):**
- ✅ Microsoft.Extensions.Logging.Console - Structured logging
- ✅ Microsoft.Extensions.DependencyInjection - DI container (no changes)

**From Epic 2 (Dependency Analysis):**
- ✅ QuikGraph v2.5.0 - DependencyGraph data structure

**From Epic 3 (Cycle Detection):**
- ✅ CycleInfo model - Used by Story 5.2

**From Epic 4 (Extraction Scoring):**
- ✅ ExtractionScore model - Used by Story 5.3 ← THIS STORY
- ✅ ExtractionScoreCalculator - Generates scores consumed by this story

**Built-in .NET Libraries:**
- ✅ System.Text.StringBuilder - Efficient string building
- ✅ System.Linq - LINQ queries for sorting/filtering (OrderBy, OrderByDescending, Take)
- ✅ System.Collections.Generic - IReadOnlyList<T>

**Epic 4 ExtractionScore Model (Existing):**

Story 5.3 consumes the ExtractionScore model from Epic 4 without modifications:

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

// Created in Epic 4, Story 4.5 (Extraction score calculator)
public sealed record ExtractionScore(
    string ProjectName,
    int Score,                      // 0-100: Lower = easier
    int IncomingReferences,
    int OutgoingReferences,
    int ExternalApiCount,
    double CouplingMetric,
    double ComplexityMetric,
    double TechDebtScore);
```

**No Changes to Epic 4 Components:**

Story 5.3 is a pure consumer - no changes to:
- ✅ ExtractionScoreCalculator.cs (Epic 4, Stories 4.1-4.5)
- ✅ HeatMapVisualizer.cs (Epic 4, Stories 4.7-4.8)
- ✅ CandidateRanker.cs (Epic 4, Story 4.6)

**Story 5.3 Integration Point:**

```
Epic 4: ExtractionScoreCalculator.CalculateScoresAsync()
        ↓
[IReadOnlyList<ExtractionScore>]
        ↓
Story 5.3: TextReportGenerator.GenerateAsync(extractionScores: ...)
        ↓
[Text Report with Extraction Difficulty Scores section]
```

### File Structure Requirements

**Files to Modify (Only 2 Files):**

```
src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # UNCHANGED (interface already supports extractionScores parameter)
└── TextReportGenerator.cs           # MODIFY: Add AppendExtractionScores method and helpers, activate extractionScores parameter

tests/MasDependencyMap.Core.Tests/Reporting/
└── TextReportGeneratorTests.cs      # MODIFY: Add ~9-11 new tests for extraction scores section
```

**No New Files Created:**

Story 5.3 is a pure extension of Stories 5.1 and 5.2 - no new classes or files.

**Files to Modify Outside Core:**

```
_bmad-output/implementation-artifacts/sprint-status.yaml  # UPDATE: Story 5-3 status backlog → ready-for-dev
_bmad-output/implementation-artifacts/5-3-add-extraction-difficulty-scoring-section-to-text-reports.md  # CREATE: This story file
```

**No Changes to Other Components:**

```
UNCHANGED (Epic 1-4):
- All Epic 1 foundation components
- All Epic 2 dependency analysis components
- All Epic 3 cycle detection components
- All Epic 4 extraction scoring components ✅ (consumed as-is)
- CLI Program.cs (DI registration unchanged)

UNCHANGED (Epic 5):
- Story 5.1's ITextReportGenerator interface
- Story 5.1's DI registration
- Story 5.2's AppendCycleDetection method
```

**Modification Details:**

1. **TextReportGenerator.cs Changes:**
   - In GenerateAsync: Add section call after AppendCycleDetection:
     ```csharp
     if (extractionScores != null && extractionScores.Count > 0)
     {
         AppendExtractionScores(report, extractionScores);
     }
     ```
   - Add new private method: `private void AppendExtractionScores(StringBuilder report, IReadOnlyList<ExtractionScore> extractionScores)`
   - Add new helper methods: `private string GetComplexityLabel(double metric, string metricName)`
   - Add new helper methods: `private string FormatExternalApis(int count)`
   - **Estimated lines added:** ~80-100 lines (main method + helpers)

2. **TextReportGeneratorTests.cs Changes:**
   - Add helper method: `private IReadOnlyList<ExtractionScore> CreateTestExtractionScores(...)`
   - Add ~9-11 new test methods for extraction scores section validation
   - **Estimated lines added:** ~200-250 lines (tests + helpers)

**Impact on Stories 5.1/5.2 Code:**

Minimal impact - only the GenerateAsync method changes:

```csharp
// Story 5.2 implementation (before Story 5.3):
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

    // Write to file
    // ... file write, performance logging ...
}

// Story 5.3 implementation (after):
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

    // NEW: Story 5.3 - Extraction scores section
    if (extractionScores != null && extractionScores.Count > 0)
    {
        AppendExtractionScores(report, extractionScores);
    }

    // Future: Story 5.4 recommendations section

    // Write to file
    // ... file write, performance logging ...
}
```

**Lines Changed in Existing Code:** ~5 lines (section call)
**Lines Added (New Methods):** ~80-100 lines
**Total Story 5.3 Code Changes:** ~85-105 lines in TextReportGenerator.cs

### Testing Requirements

**Test Strategy: Extend Existing Test Class**

Story 5.3 adds ~9-11 new tests to the existing `TextReportGeneratorTests.cs` extended in Stories 5.1 and 5.2:

```
tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs
├── [Existing Story 5.1 tests: 15 tests] ✅
├── [Existing Story 5.2 tests: ~9 tests]  ✅
└── [New Story 5.3 tests: ~9-11 tests]    ← THIS STORY
```

**Total Tests After Story 5.3:** ~33-35 tests in TextReportGeneratorTests.cs

**New Test Coverage (Story 5.3):**

1. **Basic Extraction Scores Section Inclusion:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithExtractionScores_IncludesExtractionSection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 20);
       var scores = CreateTestExtractionScores(count: 20);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: scores);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().Contain("EXTRACTION DIFFICULTY SCORES");
           content.Should().Contain("Easiest Candidates");
           content.Should().Contain("Hardest Candidates");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

2. **Top 10 Easiest Candidates Display:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithExtractionScores_ShowsTop10Easiest()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 50);
       var scores = CreateTestExtractionScores(count: 50, randomize: true);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: scores);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify top 10 easiest are sorted ascending by score
           var sortedScores = scores.OrderBy(s => s.Score).Take(10).ToList();
           foreach (var score in sortedScores)
           {
               content.Should().Contain(score.ProjectName);
           }

           // Verify rank numbering 1-10
           content.Should().Contain(" 1. ");
           content.Should().Contain("10. ");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

3. **Bottom 10 Hardest Candidates Display:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithExtractionScores_ShowsBottom10Hardest()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 50);
       var scores = CreateTestExtractionScores(count: 50, randomize: true);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: scores);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify bottom 10 hardest are sorted descending by score
           var hardestScores = scores.OrderByDescending(s => s.Score).Take(10).ToList();
           foreach (var score in hardestScores)
           {
               content.Should().Contain(score.ProjectName);
           }

           // Verify subsection header
           content.Should().Contain("Hardest Candidates (Score 67-100)");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

4. **Metric Formatting Correctness:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithExtractionScores_FormatsMetricsCorrectly()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var scores = new List<ExtractionScore>
       {
           new ExtractionScore("NotificationService", 23, 3, 2, 0, 15.5, 20.3, 5.2),
           new ExtractionScore("EmailSender", 28, 1, 4, 1, 18.2, 22.5, 6.1)
       };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: scores);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify easiest candidates format
           content.Should().Contain(" 1. NotificationService (Score: 23) - 3 incoming, 2 outgoing, no external APIs");
           content.Should().Contain(" 2. EmailSender (Score: 28) - 1 incoming, 4 outgoing, 1 API");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

5. **Null Scores Scenario:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithNullScores_DoesNotIncludeExtractionSection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: null);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().NotContain("EXTRACTION DIFFICULTY SCORES");
           content.Should().NotContain("Easiest Candidates");

           // Should still have Stories 5.1/5.2 sections
           content.Should().Contain("DEPENDENCY OVERVIEW");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

6. **Empty Scores List:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithEmptyScores_DoesNotIncludeExtractionSection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 10);
       var scores = new List<ExtractionScore>();  // Empty list
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: scores);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);
           content.Should().NotContain("EXTRACTION DIFFICULTY SCORES");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

7. **Fewer Than 10 Projects:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithFewerThan10Scores_ShowsAllAvailable()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 5);
       var scores = CreateTestExtractionScores(count: 5);  // Only 5 projects
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: scores);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify all 5 projects shown in both sections
           content.Should().Contain("EXTRACTION DIFFICULTY SCORES");

           // Count how many ranks appear
           for (int i = 1; i <= 5; i++)
           {
               content.Should().Contain($" {i}. ");
           }

           // Should not have rank 6 or higher
           content.Should().NotContain(" 6. ");
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
   public async Task GenerateAsync_ExtractionSection_AppearsAfterCycleDetection()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 20);
       var cycles = CreateTestCycles(cycleCount: 3);
       var scores = CreateTestExtractionScores(count: 20);
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", cycles: cycles, extractionScores: scores);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           var cycleDetectionIndex = content.IndexOf("CYCLE DETECTION");
           var extractionScoresIndex = content.IndexOf("EXTRACTION DIFFICULTY SCORES");

           cycleDetectionIndex.Should().BeGreaterThan(0, "Cycle Detection should exist");
           extractionScoresIndex.Should().BeGreaterThan(cycleDetectionIndex,
               "Extraction Scores should appear after Cycle Detection");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

9. **Zero External APIs Formatting:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithZeroExternalApis_FormatsAsNoApis()
   {
       // Arrange
       var graph = CreateTestGraph(projectCount: 5);
       var scores = new List<ExtractionScore>
       {
           new ExtractionScore("Service1", 20, 2, 1, 0, 10.0, 15.0, 3.0),  // 0 APIs
           new ExtractionScore("Service2", 25, 3, 2, 1, 12.0, 18.0, 4.0)   // 1 API
       };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var reportPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution", extractionScores: scores);

           // Assert
           var content = await File.ReadAllTextAsync(reportPath);

           // Verify grammatical correctness
           content.Should().Contain("no external APIs");  // Not "0 external APIs"
           content.Should().Contain("1 API");            // Singular, not "1 APIs"
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

10. **Complexity Labels for Hardest Candidates:**
    ```csharp
    [Fact]
    public async Task GenerateAsync_HardestCandidates_ShowsComplexityLabels()
    {
        // Arrange
        var graph = CreateTestGraph(projectCount: 10);
        var scores = new List<ExtractionScore>
        {
            new ExtractionScore("LegacyCore", 89, 15, 20, 5, 75.5, 82.3, 12.1),  // High coupling, high complexity
            new ExtractionScore("DataLayer", 85, 12, 18, 3, 52.2, 45.8, 8.4)     // High coupling, moderate complexity
        };
        var outputDir = CreateTempDirectory();

        try
        {
            // Act
            var reportPath = await _generator.GenerateAsync(
                graph, outputDir, "TestSolution", extractionScores: scores);

            // Assert
            var content = await File.ReadAllTextAsync(reportPath);

            // Verify hardest candidates section exists
            content.Should().Contain("Hardest Candidates");

            // Verify complexity labels
            content.Should().Contain("High coupling");
            content.Should().Contain("High complexity");
            content.Should().Contain("Moderate complexity");
            content.Should().Contain("Tech debt");
        }
        finally
        {
            CleanupTempDirectory(outputDir);
        }
    }
    ```

11. **Formatting Validation (Separators):**
    ```csharp
    [Fact]
    public async Task GenerateAsync_WithExtractionScores_FormatsWithCorrectSeparators()
    {
        // Arrange
        var graph = CreateTestGraph(projectCount: 20);
        var scores = CreateTestExtractionScores(count: 20);
        var outputDir = CreateTempDirectory();

        try
        {
            // Act
            var reportPath = await _generator.GenerateAsync(
                graph, outputDir, "TestSolution", extractionScores: scores);

            // Assert
            var content = await File.ReadAllTextAsync(reportPath);

            // Verify section separators (80 '=' characters)
            var separator = new string('=', 80);
            content.Should().Contain(separator);

            // Verify section header
            content.Should().Contain("EXTRACTION DIFFICULTY SCORES");
        }
        finally
        {
            CleanupTempDirectory(outputDir);
        }
    }
    ```

**Test Helper Methods (New):**

```csharp
// Helper: Create test extraction scores with configurable count and randomization
private IReadOnlyList<ExtractionScore> CreateTestExtractionScores(int count = 20, bool randomize = true)
{
    var scores = new List<ExtractionScore>();
    var random = new Random(42);  // Fixed seed for reproducible tests

    for (int i = 0; i < count; i++)
    {
        var projectName = $"Project{i}";
        var score = randomize ? random.Next(0, 100) : i * 5;
        var incoming = random.Next(0, 10);
        var outgoing = random.Next(0, 10);
        var apis = random.Next(0, 5);
        var coupling = random.Next(0, 100);
        var complexity = random.Next(0, 100);
        var techDebt = random.Next(0, 20);

        scores.Add(new ExtractionScore(
            projectName,
            score,
            incoming,
            outgoing,
            apis,
            coupling,
            complexity,
            techDebt));
    }

    return scores;
}

// Existing helpers from Stories 5.1/5.2 (reused):
private DependencyGraph CreateTestGraph(int projectCount = 10, int edgeCount = 15) { ... }
private IReadOnlyList<CycleInfo> CreateTestCycles(int cycleCount = 3) { ... }  // Story 5.2
private string CreateTempDirectory() { ... }
private void CleanupTempDirectory(string path) { ... }
```

**Test Execution Strategy:**

1. **Run existing Stories 5.1/5.2 tests first:** Verify no regressions (~24 tests should still pass)
2. **Add Story 5.3 tests incrementally:** One test at a time during implementation
3. **Final validation:** All ~33-35 tests pass before marking story as done

**Test Coverage After Story 5.3:**

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
└── Story 5.3 tests: ~9-11 tests ← NEW
    ├── Extraction section inclusion
    ├── Top 10 easiest candidates
    ├── Bottom 10 hardest candidates
    ├── Metric formatting correctness
    ├── Null/empty scores handling
    ├── Fewer than 10 projects
    ├── Section order validation
    ├── Zero APIs grammatical formatting
    ├── Complexity labels for hardest
    └── Formatting with separators

Total: ~33-35 tests (comprehensive coverage for Epic 5 reporting through Story 5.3)
```

### Previous Story Intelligence

**From Story 5.2 (Immediate Predecessor) - Cycle Detection Section:**

Story 5.2 extended TextReportGenerator with cycle detection section. Story 5.3 follows identical pattern:

**Key Patterns from Story 5.2:**

1. **Section Insertion Pattern:**
   ```csharp
   // Story 5.2 pattern:
   if (cycles != null)
   {
       AppendCycleDetection(report, cycles);
   }

   // Story 5.3 pattern (SAME):
   if (extractionScores != null && extractionScores.Count > 0)
   {
       AppendExtractionScores(report, extractionScores);
   }
   ```

2. **Private Helper Method Pattern:**
   ```csharp
   // Story 5.2 pattern:
   private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo> cycles)

   // Story 5.3 pattern (SAME):
   private void AppendExtractionScores(StringBuilder report, IReadOnlyList<ExtractionScore> extractionScores)
   ```

3. **Formatting Constants:**
   ```csharp
   private const int ReportWidth = 80;  // Established in Story 5.1

   // Story 5.3 reuses this for consistency:
   report.AppendLine(new string('=', ReportWidth));  // Section separator
   ```

4. **Number Formatting Standards:**
   ```csharp
   // Story 5.2 pattern:
   report.AppendLine($"Circular Dependency Chains: {totalCycles:N0}");  // Thousands separator
   report.AppendLine($"Projects in Cycles: {uniqueProjects:N0} ({percent:F1}%)");  // One decimal

   // Story 5.3 pattern (SAME):
   report.AppendLine($" {rank,2}. {projectName} (Score: {score:N0})");  // No decimals for scores
   ```

5. **Null/Empty Handling:**
   ```csharp
   // Story 5.2 pattern: Null parameter skips section, empty list shows message
   if (cycles != null)
   {
       AppendCycleDetection(report, cycles);
   }

   // Story 5.3 pattern: Null or empty list skips section (no message)
   if (extractionScores != null && extractionScores.Count > 0)
   {
       AppendExtractionScores(report, extractionScores);
   }
   ```

**From Story 5.2 Code Review Fixes:**

Story 5.2 underwent adversarial code review. Story 5.3 should learn from these:

1. **Test Debris Management:**
   - Story 5.2 review: Identified and removed test debris files (TestReportDemo.cs, test-text-report.csx)
   - Story 5.3 lesson: Don't commit temporary test files to repository

2. **Integration Testing:**
   - Story 5.2 review: Added integration test using real Epic 3 CycleDetector
   - Story 5.3 lesson: Add integration test using real Epic 4 ExtractionScoreCalculator

3. **Edge Case Testing:**
   - Story 5.2 review: Added tests for zero projects (division by zero), null project names
   - Story 5.3 lesson: Test edge cases like fewer than 10 projects, zero external APIs

4. **Large Dataset Testing:**
   - Story 5.2 review: Added test for very large cycle (150 projects)
   - Story 5.3 lesson: Test with large score lists (e.g., 400 projects)

**From Story 5.1 (Foundation) - Report Generator Patterns:**

Story 5.1 created the TextReportGenerator foundation:

1. **Optional Parameters for Extensibility:**
   ```csharp
   // Story 5.1 interface design (forward-compatible):
   Task<string> GenerateAsync(
       DependencyGraph graph,
       string outputDirectory,
       string solutionName,
       IReadOnlyList<CycleInfo>? cycles = null,
       IReadOnlyList<ExtractionScore>? extractionScores = null,  // ← Story 5.3 uses this
       IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
       CancellationToken cancellationToken = default);
   ```

2. **StringBuilder Pre-allocation:**
   ```csharp
   // Story 5.1 pattern: Pre-allocate capacity
   var report = new StringBuilder(capacity: 4096);

   // Story 5.3 continues this pattern (no change needed)
   ```

3. **Performance Logging:**
   ```csharp
   // Story 5.1 pattern: Track total generation time
   var startTime = DateTime.UtcNow;
   // ... generate report ...
   var elapsed = DateTime.UtcNow - startTime;
   _logger.LogInformation("Generated text report at {FilePath} in {ElapsedMs}ms", filePath, elapsed.TotalMilliseconds);

   // Story 5.3 continues this pattern (no change needed)
   ```

**From Epic 4 (Extraction Scoring) - Data Source:**

Story 5.3 consumes extraction scoring results from Epic 4:

**Epic 4 ExtractionScore Model (Stories 4.5-4.6):**

```csharp
// Created by ExtractionScoreCalculator (Epic 4, Story 4.5):
public sealed record ExtractionScore(
    string ProjectName,
    int Score,                      // 0-100: Lower = easier
    int IncomingReferences,
    int OutgoingReferences,
    int ExternalApiCount,
    double CouplingMetric,
    double ComplexityMetric,
    double TechDebtScore);
```

**Epic 4 Scoring Flow:**

```
Story 4.1: Implement coupling metric calculator
        ↓
Story 4.2: Implement cyclomatic complexity calculator
        ↓
Story 4.3: Implement technology version debt analyzer
        ↓
Story 4.4: Implement external API exposure detector
        ↓
Story 4.5: Implement extraction score calculator (combines all metrics)
        ↓
[IReadOnlyList<ExtractionScore>]
        ↓
Story 4.6: Generate ranked extraction candidate lists
        ↓
Story 5.3: Report scores in text format (THIS STORY)
```

**Implementation Takeaways:**

1. ✅ **Follow Story 5.2's section generation pattern** - private helper methods, null handling
2. ✅ **Maintain Stories 5.1/5.2's formatting consistency** - 80-char separators, number formatting
3. ✅ **Reuse Story 5.1's performance optimizations** - StringBuilder pre-allocation, single-pass LINQ
4. ✅ **Learn from Story 5.2's code review** - integration tests, edge case tests, no test debris
5. ✅ **Consume Epic 4's ExtractionScore as-is** - no modifications to scoring logic
6. ✅ **Set pattern for Story 5.4** - optional parameter handling, section insertion order

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 5.3 continues Epic 5's reporting stack progression:

```
Epic Progression:
Epic 2 (Dependency Analysis) → Epic 3 (Cycle Detection) → Epic 4 (Extraction Scoring) → Epic 5 (Reporting)

src/MasDependencyMap.Core/DependencyAnalysis/
├── [Stories 2.1-2.5: Dependency graph building] ✅ DONE
└── DependencyGraph.cs               # Consumed by Story 5.1

src/MasDependencyMap.Core/CycleAnalysis/
├── [Stories 3.1-3.7: Cycle detection and analysis] ✅ DONE
└── CycleInfo.cs                     # Consumed by Story 5.2

src/MasDependencyMap.Core/ExtractionScoring/
├── [Stories 4.1-4.8: Scoring and visualization] ✅ DONE
└── ExtractionScore.cs               # Consumed by Story 5.3 ← THIS STORY

src/MasDependencyMap.Core/Reporting/
├── ITextReportGenerator.cs          # Story 5.1 (unchanged)
└── TextReportGenerator.cs           # Story 5.1 + Story 5.2 + Story 5.3 ← MODIFIED
```

**Epic 5 Reporting Stack After Story 5.3:**

```
Story 5.1 (DONE): Text Report Generator Foundation
    ↓ Creates: ITextReportGenerator, TextReportGenerator
    ↓ Sections: Header, Dependency Overview
    ↓
Story 5.2 (DONE): Add Cycle Detection Section
    ↓ Extends: TextReportGenerator
    ↓ Sections: Header, Dependency Overview, Cycle Detection
    ↓
Story 5.3 (THIS STORY): Add Extraction Difficulty Section
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

Story 5.3 completes the integration between Epic 4 and Epic 5:

```
Epic 4 Output:
- ExtractionScore objects (List of scored projects)
- Ranked candidate lists (top/bottom candidates)

        ↓

Story 5.3 Integration:
- Consumes IReadOnlyList<ExtractionScore>
- Sorts for top 10 easiest / bottom 10 hardest
- Formats for stakeholder readability

        ↓

Epic 5 Output:
- Text report with extraction difficulty scores section
- Human-readable extraction candidate rankings
- Stakeholder-ready migration insights
```

**No Impact on Other Epics:**

```
Epic 1 (Foundation): ✅ No changes
Epic 2 (Dependency Analysis): ✅ No changes
Epic 3 (Cycle Detection): ✅ No changes
Epic 4 (Extraction Scoring): ✅ No changes (pure consumption)
Epic 6 (Future): Not yet started
```

**Epic 5 Roadmap After Story 5.3:**

After Story 5.3, Epic 5 continues with recommendations section and CSV export:

1. ✅ Story 5.1: Text Report Generator foundation (DONE)
2. ✅ Story 5.2: Add Cycle Detection section (DONE)
3. 🔨 Story 5.3: Add Extraction Difficulty section (THIS STORY)
4. ⏳ Story 5.4: Add Recommendations section (extends TextReportGenerator)
5. ⏳ Story 5.5: CSV export for extraction scores (new CsvExporter component)
6. ⏳ Story 5.6: CSV export for cycle analysis
7. ⏳ Story 5.7: CSV export for dependency matrix
8. ⏳ Story 5.8: Spectre.Console table formatting (CLI layer integration)

**After Story 5.3:**
- **Text report sections:** 4/5 complete (Header, Dependency Overview, Cycle Detection, Extraction Scores)
- **Remaining sections:** Recommendations (5.4)
- **CSV export:** Not started (Stories 5.5-5.7)
- **CLI integration:** Not started (Story 5.8)

**File Modification Summary (Story 5.3):**

```
MODIFIED (2 files):
- src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs
  - Modify GenerateAsync: Add extraction scores section call
  - Add new method: AppendExtractionScores
  - Add new helpers: GetComplexityLabel, FormatExternalApis

- tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs
  - Add helper: CreateTestExtractionScores
  - Add ~9-11 new test methods

UNCHANGED (Backward compatible):
- ITextReportGenerator.cs (interface already supports extractionScores parameter)
- All Epic 1-4 components
- All Epic 5 Stories 5.1/5.2 test coverage (~24 existing tests still pass)
```

**Development Workflow Impact:**

Story 5.3 follows the same development pattern as Stories 5.1 and 5.2:

1. **Implementation Phase:**
   - Modify TextReportGenerator.cs
   - Add AppendExtractionScores private method and helpers
   - Update GenerateAsync to call new method

2. **Testing Phase:**
   - Add helper methods for extraction score creation
   - Add ~9-11 new tests
   - Run all tests (existing + new)

3. **Validation Phase:**
   - Verify all ~430+ project tests pass
   - Verify no regressions in Stories 5.1/5.2 functionality
   - Manual test: Generate report with extraction scores from test solution

4. **Code Review Phase (Expected):**
   - Stories 5.1 and 5.2 both had code review findings
   - Story 5.3 should expect similar scrutiny
   - Prepare for adversarial review focusing on:
     - Sorting logic (ascending vs descending)
     - Top 10 vs bottom 10 selection correctness
     - Formatting consistency with Stories 5.1/5.2
     - Grammatical correctness (API vs APIs)
     - Performance (LINQ query efficiency)

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-5-comprehensive-reporting-and-data-export.md, Story 5.3 (lines 42-57)]
- Story requirements: Extraction difficulty scores section with top 10 easiest/bottom 10 hardest, metrics display, score ranges explained

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
- Number formatting: N0 for integers, F1 for percentages

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\5-2-add-cycle-detection-section-to-text-reports.md]
- Story 5.2 implementation: Extended TextReportGenerator with cycle detection section
- Helper method pattern: AppendCycleDetection (Story 5.3 follows same pattern)
- Formatting constants: ReportWidth = 80, separator patterns
- Performance: Single-pass LINQ queries, no nested loops
- Code review learnings: Integration tests, edge case tests, no test debris

**Epic 4 Integration:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-4-extraction-difficulty-scoring-and-candidate-ranking.md]
- ExtractionScore model: ProjectName, Score, IncomingReferences, OutgoingReferences, ExternalApiCount, CouplingMetric, ComplexityMetric, TechDebtScore
- Story 4.5: ExtractionScoreCalculator implementation (generates scores)
- Story 4.6: Ranked extraction candidate lists (top/bottom candidates)

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Most recent: Story 5.2 completed with code review fixes (70d3380)
- Pattern: Story implementation → code review → fixes → status update
- Story 5.2 most recent: "Story 5-2: Add cycle detection section to text reports with code review fixes"

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

### Completion Notes List

- ✅ Implemented AppendExtractionScores private method in TextReportGenerator.cs
- ✅ Added helper methods FormatExternalApis and GetComplexityLabel for formatting consistency
- ✅ Integrated with Epic 4 ExtractionScore model using nested metric objects (CouplingMetric, ComplexityMetric, TechDebtMetric, ExternalApiMetric)
- ✅ Updated GenerateAsync to conditionally call AppendExtractionScores when extractionScores parameter is provided
- ✅ Added 12 comprehensive unit tests to TextReportGeneratorTests.cs (total: 40 tests passing initially)
- ✅ All initial tests passing (40 passed, 0 failed, 0 skipped)
- ✅ Performance validated: Large extraction scores list (400 projects) completes within 10-second budget
- ✅ Section insertion order: Extraction Difficulty Scores appears after Cycle Detection section as designed

**Code Review Fixes Applied:**
- ✅ Added null item validation in GenerateAsync to prevent crashes if list contains null ExtractionScore objects (ArgumentException thrown)
- ✅ Fixed potentially misleading range labels: Changed from hardcoded "Score 0-33" and "Score 67-100" to dynamic "Scores {min}-{max}" based on actual data
- ✅ Added integration test GenerateAsync_WithRealExtractionScoreCalculator_ProducesCorrectReport using real Epic 4 ExtractionScoreCalculator
- ✅ Added test GenerateAsync_WithNullItemInExtractionScores_ThrowsArgumentException to validate null item handling
- ✅ Updated test assertion to match dynamic range labels (removed hardcoded "Score 67-100" check)
- ✅ Final test count: 42 tests (40 original + 2 code review fixes), all passing

**Key Implementation Notes:**
- ExtractionScore model uses nested metric objects with NormalizedScore properties, not flat structure
- FinalScore property (not Score) represents the overall extraction difficulty (0-100)
- Null-safe handling for CouplingMetric (can be null if dependency graph unavailable)
- Null item validation prevents crashes from malformed input lists
- Dynamic score ranges accurately reflect actual data instead of misleading stakeholders with hardcoded ranges
- Grammatical correctness: "no external APIs", "1 API", "2 APIs" formatting
- Complexity labels: High (≥60), Moderate (30-60), Low (<30)

### File List

**Modified Files:**
- src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs
- tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs

**No New Files Created** (Story 5.3 extends existing components)
