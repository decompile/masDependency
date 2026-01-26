# Story 4.6: Generate Ranked Extraction Candidate Lists

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want all projects ranked by extraction difficulty score (easiest first),
So that I know exactly where to start my migration.

## Acceptance Criteria

**Given** Extraction scores are calculated for all projects
**When** Ranked extraction candidate list is generated
**Then** Projects are sorted by extraction score (ascending: lowest score first)
**And** Top 10 easiest candidates are identified (scores 0-33)
**And** Bottom 10 hardest candidates are identified (scores 67-100)
**And** Each candidate includes: project name, score, coupling metric, complexity metric, tech debt, API exposure
**And** Ranked list is available for text reports and CSV export
**And** ILogger logs "Generated ranked extraction candidates: 73 total projects, 18 easy (0-33), 31 medium (34-66), 24 hard (67-100)"

## Tasks / Subtasks

- [x] Create IRankedCandidateGenerator interface (AC: Abstraction for DI)
  - [x] Define GenerateRankedListAsync(DependencyGraph graph, CancellationToken cancellationToken = default) method signature
  - [x] Return Task<RankedExtractionCandidates> containing all ranked projects with metadata
  - [x] Add XML documentation with examples and exception documentation
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace (consistent with Epic 4)

- [x] Create RankedExtractionCandidates model class (AC: Store ranked list with metadata)
  - [x] Define RankedExtractionCandidates record with properties: AllProjects (sorted), EasiestCandidates (top 10, 0-33), HardestCandidates (bottom 10, 67-100), Statistics (counts by category)
  - [x] Use record type for immutability (C# 9+ pattern)
  - [x] Include ExtractionStatistics nested record: TotalProjects, EasyCount, MediumCount, HardCount
  - [x] Add XML documentation explaining structure and purpose
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [x] Implement RankedCandidateGenerator class skeleton (AC: Set up generator infrastructure)
  - [x] Implement IRankedCandidateGenerator interface
  - [x] Inject IExtractionScoreCalculator via constructor (from Story 4.5)
  - [x] Inject ILogger<RankedCandidateGenerator> for structured logging
  - [x] File-scoped namespace declaration (C# 10+ pattern)
  - [x] Async methods with Async suffix and ConfigureAwait(false) per project conventions

- [x] Implement GenerateRankedListAsync core logic (AC: Generate sorted ranked list)
  - [x] Call IExtractionScoreCalculator.CalculateForAllProjectsAsync(graph) to get all extraction scores
  - [x] Scores are already sorted ascending by ExtractionScoreCalculator (easiest first)
  - [x] Separate projects by difficulty category: Easy (0-33), Medium (34-66), Hard (67-100)
  - [x] Count projects in each category for statistics
  - [x] Log Information: "Calculating extraction scores for {ProjectCount} projects" at start
  - [x] Log Information: "Generated ranked extraction candidates: {TotalProjects} total, {EasyCount} easy (0-33), {MediumCount} medium (34-66), {HardCount} hard (67-100)" at end
  - [x] Support cancellation via CancellationToken

- [x] Implement top 10 easiest candidate selection (AC: Identify top 10 easiest)
  - [x] Filter AllProjects where FinalScore <= 33 (Easy category)
  - [x] Take first 10 projects from filtered list (already sorted ascending)
  - [x] If fewer than 10 easy projects exist, take all available
  - [x] Store in EasiestCandidates property
  - [x] Log Debug: "Top 10 easiest candidates: {CandidateNames}" with comma-separated project names

- [x] Implement bottom 10 hardest candidate selection (AC: Identify bottom 10 hardest)
  - [x] Filter AllProjects where FinalScore >= 67 (Hard category)
  - [x] Sort filtered list descending by score (hardest first)
  - [x] Take first 10 projects from descending list
  - [x] If fewer than 10 hard projects exist, take all available
  - [x] Store in HardestCandidates property
  - [x] Log Debug: "Bottom 10 hardest candidates: {CandidateNames}" with comma-separated project names

- [x] Implement extraction statistics calculation (AC: Provide category counts)
  - [x] Count projects where FinalScore <= 33 (Easy)
  - [x] Count projects where FinalScore > 33 AND FinalScore < 67 (Medium)
  - [x] Count projects where FinalScore >= 67 (Hard)
  - [x] Create ExtractionStatistics record with counts
  - [x] Include in RankedExtractionCandidates result
  - [x] Verify TotalProjects = EasyCount + MediumCount + HardCount

- [x] Add structured logging with named placeholders (AC: Log ranking process)
  - [x] Log Information: "Calculating extraction scores for {ProjectCount} projects" at start
  - [x] Log Debug: "Extraction scores calculated, sorting and categorizing projects" during processing
  - [x] Log Debug: "Identified {EasyCount} easy candidates (scores 0-33)" for easy category
  - [x] Log Debug: "Identified {MediumCount} medium candidates (scores 34-66)" for medium category
  - [x] Log Debug: "Identified {HardCount} hard candidates (scores 67-100)" for hard category
  - [x] Log Information: "Generated ranked extraction candidates: {TotalProjects} total, {EasyCount} easy (0-33), {MediumCount} medium (34-66), {HardCount} hard (67-100)" at completion
  - [x] Use named placeholders, NOT string interpolation (critical project rule)

- [x] Register service in DI container (AC: Service integration)
  - [x] Add registration in CLI Program.cs DI configuration
  - [x] Use services.AddSingleton<IRankedCandidateGenerator, RankedCandidateGenerator>() pattern
  - [x] Register in "Epic 4: Extraction Scoring Services" section (after IExtractionScoreCalculator)
  - [x] Ensure dependency IExtractionScoreCalculator is already registered (Story 4.5)
  - [x] Follow existing DI registration patterns from Stories 4.1-4.5

- [x] Create comprehensive unit tests (AC: Test coverage)
  - [x] Create test class: tests/MasDependencyMap.Core.Tests/ExtractionScoring/RankedCandidateGeneratorTests.cs
  - [x] Test: GenerateRankedListAsync_WithMultipleProjects_SortsAscending (validates sorting by score ascending)
  - [x] Test: GenerateRankedListAsync_WithEasyProjects_ReturnsTop10Easiest (scores 0-33, top 10 selection)
  - [x] Test: GenerateRankedListAsync_WithHardProjects_ReturnsBottom10Hardest (scores 67-100, bottom 10 selection)
  - [x] Test: GenerateRankedListAsync_WithFewerThan10Easy_ReturnsAll (edge case: <10 easy projects)
  - [x] Test: GenerateRankedListAsync_WithFewerThan10Hard_ReturnsAll (edge case: <10 hard projects)
  - [x] Test: GenerateRankedListAsync_WithNoEasyProjects_ReturnsEmptyEasiestList (edge case: no easy projects)
  - [x] Test: GenerateRankedListAsync_WithNoHardProjects_ReturnsEmptyHardestList (edge case: no hard projects)
  - [x] Test: GenerateRankedListAsync_Statistics_CalculatesCorrectCounts (validates category counts)
  - [x] Test: GenerateRankedListAsync_Statistics_SumEqualsTotal (validates EasyCount + MediumCount + HardCount = TotalProjects)
  - [x] Test: GenerateRankedListAsync_CancellationRequested_ThrowsOperationCanceledException (cancellation support)
  - [x] Use xUnit, FluentAssertions, Moq for mocking IExtractionScoreCalculator
  - [x] Test naming: {MethodName}_{Scenario}_{ExpectedResult} pattern
  - [x] Arrange-Act-Assert structure

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Feature-based namespace: MasDependencyMap.Core.ExtractionScoring (NOT layer-based)
  - [x] Async suffix on all async methods (GenerateRankedListAsync)
  - [x] File-scoped namespace declarations (all files)
  - [x] ILogger injection via constructor (NOT static logger)
  - [x] ConfigureAwait(false) in library code (Core layer)
  - [x] XML documentation on all public APIs (model, interface, implementation)
  - [x] Test files mirror Core namespace structure (tests/MasDependencyMap.Core.Tests/ExtractionScoring)

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 4.6 Ranked Candidate Generator Requirements:**

This story implements the **RANKED LIST GENERATOR** that consumes Story 4.5's ExtractionScoreCalculator to produce sorted, filtered lists of extraction candidates for reporting and decision-making.

**Epic 4 Vision (Recap):**
- Story 4.1: Coupling metrics ✅ DONE
- Story 4.2: Cyclomatic complexity metrics ✅ DONE
- Story 4.3: Technology version debt metrics ✅ DONE
- Story 4.4: External API exposure metrics ✅ DONE
- Story 4.5: Combined extraction score calculator ✅ DONE
- **Story 4.6: Ranked extraction candidate lists (THIS STORY - LIST GENERATOR)**
- Story 4.7: Heat map visualization with color-coded scores (consumes 4.5)
- Story 4.8: Display extraction scores as node labels (consumes 4.5)

**Story 4.6 Unique Characteristics:**

1. **Thin Orchestration Layer:**
   - Stories 4.1-4.4: Heavy metric calculation logic (Roslyn analysis, graph algorithms, XML parsing)
   - Story 4.5: Orchestration of 4 calculators, configurable weights, validation
   - Story 4.6: SIMPLE wrapper around Story 4.5 - sorting, filtering, statistics
   - **Minimal business logic:** Most work already done by ExtractionScoreCalculator

2. **Single Dependency:**
   - Story 4.5: 6 dependencies (4 metric calculators + IConfiguration + ILogger)
   - Story 4.6: 2 dependencies (IExtractionScoreCalculator + ILogger)
   - **Clean separation:** Ranking logic completely decoupled from metric calculation

3. **No Configuration Required:**
   - Stories 4.1-4.4: Hardcoded thresholds
   - Story 4.5: scoring-config.json with weight validation
   - Story 4.6: NO configuration needed (uses defaults from Story 4.5)

4. **No New Algorithms:**
   - Stories 4.1-4.4: Complex algorithms (Tarjan's, Roslyn walking, semantic analysis)
   - Story 4.5: Weighted sum calculation, validation
   - Story 4.6: LINQ sorting and filtering only
   - **Pure data transformation:** No complex computation

5. **Prepares Data for Consumption:**
   - Epic 5 (Reporting): Will consume RankedExtractionCandidates for text reports and CSV export
   - Story 4.7: Will use RankedExtractionCandidates for heat map coloring
   - Story 4.8: Will use RankedExtractionCandidates for node labels
   - **This story is the bridge between scoring (Epic 4) and reporting (Epic 5)**

🚨 **CRITICAL - RankedExtractionCandidates Model Design:**

**Model Requirements:**

1. Contains ALL projects sorted by score ascending (easiest first)
2. Contains top 10 easiest candidates (score 0-33) for quick reference
3. Contains bottom 10 hardest candidates (score 67-100) for risk awareness
4. Contains statistics (counts by category) for summary reporting
5. Immutable record type (consistent with Epic 4 pattern)

**Implementation:**

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents a ranked list of extraction candidates sorted by difficulty score,
/// with top/bottom candidates highlighted and statistics summarized.
/// </summary>
/// <param name="AllProjects">All projects sorted by extraction score ascending (easiest first).</param>
/// <param name="EasiestCandidates">Top 10 easiest extraction candidates (scores 0-33). May contain fewer than 10 if not enough easy projects.</param>
/// <param name="HardestCandidates">Bottom 10 hardest extraction candidates (scores 67-100). May contain fewer than 10 if not enough hard projects.</param>
/// <param name="Statistics">Summary statistics by difficulty category.</param>
public sealed record RankedExtractionCandidates(
    IReadOnlyList<ExtractionScore> AllProjects,
    IReadOnlyList<ExtractionScore> EasiestCandidates,
    IReadOnlyList<ExtractionScore> HardestCandidates,
    ExtractionStatistics Statistics);

/// <summary>
/// Summary statistics for extraction candidates by difficulty category.
/// </summary>
/// <param name="TotalProjects">Total number of projects analyzed.</param>
/// <param name="EasyCount">Number of projects with scores 0-33 (easy extraction).</param>
/// <param name="MediumCount">Number of projects with scores 34-66 (medium extraction).</param>
/// <param name="HardCount">Number of projects with scores 67-100 (hard extraction).</param>
public sealed record ExtractionStatistics(
    int TotalProjects,
    int EasyCount,
    int MediumCount,
    int HardCount)
{
    /// <summary>
    /// Validates that category counts sum to total projects.
    /// </summary>
    public bool IsValid => EasyCount + MediumCount + HardCount == TotalProjects;
}
```

**Why Separate Properties for Top/Bottom 10?**

- **Quick access:** Reports and visualizations can show top 10 without filtering
- **Clear intent:** Explicitly identifies "start here" (easiest) and "avoid or do last" (hardest)
- **Consistent with acceptance criteria:** Epic 4.6 specifically requires identifying top 10 easy and bottom 10 hard

**Why Nested ExtractionStatistics Record?**

- **Cohesion:** Statistics are semantically grouped
- **Validation:** IsValid property ensures data integrity
- **Reusability:** Statistics can be used independently in reports

🚨 **CRITICAL - Ranking Logic Implementation:**

**Sorting Strategy:**

Story 4.5's ExtractionScoreCalculator.CalculateForAllProjectsAsync() **already returns projects sorted ascending** (easiest first). Story 4.6 does NOT need to re-sort:

```csharp
// From Story 4.5 (ExtractionScoreCalculator.cs)
public async Task<IReadOnlyList<ExtractionScore>> CalculateForAllProjectsAsync(...)
{
    // ... calculation logic ...

    // Sort by final score ascending (easiest first)
    return scores.OrderBy(s => s.FinalScore).ToList();
}
```

**Story 4.6 Implementation:**

```csharp
public async Task<RankedExtractionCandidates> GenerateRankedListAsync(
    DependencyGraph graph,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(graph);

    _logger.LogInformation(
        "Calculating extraction scores for {ProjectCount} projects",
        graph.VertexCount);

    // Get all scores (already sorted ascending by ExtractionScoreCalculator)
    var allScores = await _scoreCalculator.CalculateForAllProjectsAsync(graph, cancellationToken)
        .ConfigureAwait(false);

    _logger.LogDebug("Extraction scores calculated, sorting and categorizing projects");

    // Categorize by difficulty
    var easyCandidates = allScores.Where(s => s.FinalScore <= 33).ToList();
    var mediumCandidates = allScores.Where(s => s.FinalScore > 33 && s.FinalScore < 67).ToList();
    var hardCandidates = allScores.Where(s => s.FinalScore >= 67).ToList();

    // Take top 10 easiest (already sorted ascending, so take first 10)
    var top10Easiest = easyCandidates.Take(10).ToList();

    // Take bottom 10 hardest (need to reverse sort for hardest first, then take 10)
    var bottom10Hardest = hardCandidates.OrderByDescending(s => s.FinalScore).Take(10).ToList();

    _logger.LogDebug("Identified {EasyCount} easy candidates (scores 0-33)", easyCandidates.Count);
    _logger.LogDebug("Identified {MediumCount} medium candidates (scores 34-66)", mediumCandidates.Count);
    _logger.LogDebug("Identified {HardCount} hard candidates (scores 67-100)", hardCandidates.Count);

    var statistics = new ExtractionStatistics(
        TotalProjects: allScores.Count,
        EasyCount: easyCandidates.Count,
        MediumCount: mediumCandidates.Count,
        HardCount: hardCandidates.Count);

    if (!statistics.IsValid)
    {
        _logger.LogWarning(
            "Statistics validation failed: {EasyCount} + {MediumCount} + {HardCount} != {TotalProjects}",
            statistics.EasyCount,
            statistics.MediumCount,
            statistics.HardCount,
            statistics.TotalProjects);
    }

    _logger.LogInformation(
        "Generated ranked extraction candidates: {TotalProjects} total, {EasyCount} easy (0-33), {MediumCount} medium (34-66), {HardCount} hard (67-100)",
        statistics.TotalProjects,
        statistics.EasyCount,
        statistics.MediumCount,
        statistics.HardCount);

    return new RankedExtractionCandidates(
        AllProjects: allScores,
        EasiestCandidates: top10Easiest,
        HardestCandidates: bottom10Hardest,
        Statistics: statistics);
}
```

**Key Implementation Notes:**

1. **No Re-Sorting:** AllProjects comes pre-sorted from Story 4.5
2. **LINQ Filtering:** Use Where() for category separation
3. **Top 10 Easiest:** Take(10) on already-sorted list
4. **Bottom 10 Hardest:** OrderByDescending + Take(10) to get worst offenders
5. **Edge Cases:** Take(10) safely handles <10 items
6. **Statistics Validation:** IsValid property catches logic errors

🚨 **CRITICAL - Difficulty Category Boundaries:**

**Category Definitions (Consistent with Epic 4):**

```csharp
// Easy: 0-33 (inclusive lower, inclusive upper)
s.FinalScore <= 33

// Medium: 34-66 (exclusive lower, exclusive upper on boundaries)
s.FinalScore > 33 && s.FinalScore < 67

// Hard: 67-100 (inclusive lower, inclusive upper)
s.FinalScore >= 67
```

**Why These Boundaries?**

- Consistent with Story 4.5 ExtractionScore.DifficultyCategory property
- Matches Epic 4 vision color-coding: Green (Easy), Yellow (Medium), Red (Hard)
- 33.33% buckets provide balanced distribution

**Edge Case: Exactly 33 or 67?**

- Score = 33.0 → Easy (inclusive upper bound)
- Score = 33.1 → Medium (exclusive lower bound)
- Score = 66.9 → Medium (exclusive upper bound)
- Score = 67.0 → Hard (inclusive lower bound)

**Implementation uses <= and >= to handle boundaries correctly.**

🚨 **CRITICAL - Top 10 vs. Bottom 10 Selection:**

**Top 10 Easiest (Start Here):**

```csharp
// Filter to easy category (0-33)
var easyCandidates = allScores.Where(s => s.FinalScore <= 33).ToList();

// Take first 10 (already sorted ascending, so lowest scores first)
var top10Easiest = easyCandidates.Take(10).ToList();
```

**Bottom 10 Hardest (Avoid or Do Last):**

```csharp
// Filter to hard category (67-100)
var hardCandidates = allScores.Where(s => s.FinalScore >= 67).ToList();

// Sort descending (hardest first) and take 10
var bottom10Hardest = hardCandidates.OrderByDescending(s => s.FinalScore).Take(10).ToList();
```

**Why Different Sorting for Bottom 10?**

- Easiest 10: Already ascending order (0, 5, 10, 15, 20, 25, 28, 30, 32, 33)
- Hardest 10: Need descending order (100, 95, 92, 88, 85, 82, 78, 75, 70, 68)
- **Hardest 10 shows "worst offenders first"** for risk awareness

**Edge Case: Fewer Than 10?**

```csharp
// If only 5 easy projects exist, Take(10) returns 5 (no exception)
var top10Easiest = easyCandidates.Take(10).ToList();

// If only 3 hard projects exist, Take(10) returns 3 (no exception)
var bottom10Hardest = hardCandidates.OrderByDescending(s => s.FinalScore).Take(10).ToList();
```

**LINQ Take() is safe with fewer items - returns all available.**

### Technical Requirements

**New Namespace: MasDependencyMap.Core.ExtractionScoring (Established in Stories 4.1-4.5):**

Epic 4 continues using the `ExtractionScoring` namespace created in Story 4.1.

```
src/MasDependencyMap.Core/ExtractionScoring/
├── CouplingMetric.cs                            # Story 4.1
├── ICouplingMetricCalculator.cs                 # Story 4.1
├── CouplingMetricCalculator.cs                  # Story 4.1
├── ComplexityMetric.cs                          # Story 4.2
├── IComplexityMetricCalculator.cs               # Story 4.2
├── ComplexityMetricCalculator.cs                # Story 4.2
├── CyclomaticComplexityWalker.cs                # Story 4.2
├── TechDebtMetric.cs                            # Story 4.3
├── ITechDebtAnalyzer.cs                         # Story 4.3
├── TechDebtAnalyzer.cs                          # Story 4.3
├── ExternalApiMetric.cs                         # Story 4.4
├── IExternalApiDetector.cs                      # Story 4.4
├── ExternalApiDetector.cs                       # Story 4.4
├── ScoringWeights.cs                            # Story 4.5
├── ExtractionScore.cs                           # Story 4.5
├── IExtractionScoreCalculator.cs                # Story 4.5
├── ExtractionScoreCalculator.cs                 # Story 4.5
├── ConfigurationException.cs                    # Story 4.5
├── RankedExtractionCandidates.cs                # Story 4.6 (THIS STORY)
├── ExtractionStatistics.cs                      # Story 4.6 (THIS STORY - nested in RankedExtractionCandidates)
├── IRankedCandidateGenerator.cs                 # Story 4.6 (THIS STORY)
└── RankedCandidateGenerator.cs                  # Story 4.6 (THIS STORY)
```

**Dependencies:**

Story 4.6 DEPENDS ON Story 4.5:
- IExtractionScoreCalculator (Story 4.5)

Constructor injection:
```csharp
public RankedCandidateGenerator(
    IExtractionScoreCalculator scoreCalculator,
    ILogger<RankedCandidateGenerator> logger)
{
    _scoreCalculator = scoreCalculator ?? throw new ArgumentNullException(nameof(scoreCalculator));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### Architecture Compliance

**Dependency Injection Registration:**

```csharp
// In Program.cs DI configuration, Epic 4 section

// Epic 4: Extraction Scoring Services
services.AddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>();
services.AddSingleton<IComplexityMetricCalculator, ComplexityMetricCalculator>();
services.AddSingleton<ITechDebtAnalyzer, TechDebtAnalyzer>();
services.AddSingleton<IExternalApiDetector, ExternalApiDetector>();
services.AddSingleton<IExtractionScoreCalculator, ExtractionScoreCalculator>();
services.AddSingleton<IRankedCandidateGenerator, RankedCandidateGenerator>(); // NEW
```

**Lifetime:**
- Singleton: RankedCandidateGenerator is stateless (thin wrapper around IExtractionScoreCalculator)
- Consistent with Stories 4.1-4.5 (all Epic 4 services are singletons)

### Library/Framework Requirements

**Existing Libraries (Already Installed):**

All dependencies already installed in Stories 4.1-4.5:
- ✅ Microsoft.Extensions.DependencyInjection - DI container
- ✅ Microsoft.Extensions.Logging.Console - Structured logging
- ✅ QuikGraph v2.5.0 - Graph data structures (DependencyGraph parameter)

**No New NuGet Packages Required for Story 4.6** ✅

**LINQ Usage:**

```csharp
using System.Linq;

// Filtering
var easyCandidates = allScores.Where(s => s.FinalScore <= 33).ToList();

// Taking first N
var top10Easiest = easyCandidates.Take(10).ToList();

// Sorting descending
var bottom10Hardest = hardCandidates.OrderByDescending(s => s.FinalScore).Take(10).ToList();
```

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/ExtractionScoring/
├── RankedExtractionCandidates.cs                 # NEW (model with nested ExtractionStatistics)
├── IRankedCandidateGenerator.cs                  # NEW (generator interface)
└── RankedCandidateGenerator.cs                   # NEW (generator implementation)

tests/MasDependencyMap.Core.Tests/ExtractionScoring/
└── RankedCandidateGeneratorTests.cs              # NEW (comprehensive tests with mocking)
```

**Files to Modify:**

```
src/MasDependencyMap.CLI/Program.cs                           # MODIFY: Add DI registration
_bmad-output/implementation-artifacts/sprint-status.yaml      # MODIFY: Update story status
```

**No CLI Command Integration Yet:**

Story 4.6 creates the generator but doesn't integrate it into CLI commands. CLI integration happens in Epic 5 (Reporting):
- Story 5.1: Text report generator (uses RankedExtractionCandidates)
- Story 5.3: Extraction difficulty scoring section in reports (uses RankedExtractionCandidates)
- Story 5.5: CSV export for extraction difficulty scores (uses RankedExtractionCandidates)

Story 4.7 (heat map) and 4.8 (node labels) will also consume RankedExtractionCandidates for visualization.

For now:
- Create the service and register it in DI
- Tests will validate functionality
- CLI integration deferred to Epic 5

### Testing Requirements

**Test Class: RankedCandidateGeneratorTests.cs**

**Test Strategy:**

Use unit testing with MOCKING (consistent with Story 4.5):
- Story 4.6: UNIT tests with Moq to mock IExtractionScoreCalculator
- Reason: Testing orchestration logic (sorting, filtering, statistics), not score calculation

**Mock Setup Pattern:**

```csharp
private readonly Mock<IExtractionScoreCalculator> _mockScoreCalculator;
private readonly Mock<ILogger<RankedCandidateGenerator>> _mockLogger;
private readonly RankedCandidateGenerator _generator;

public RankedCandidateGeneratorTests()
{
    _mockScoreCalculator = new Mock<IExtractionScoreCalculator>();
    _mockLogger = new Mock<ILogger<RankedCandidateGenerator>>();

    _generator = new RankedCandidateGenerator(
        _mockScoreCalculator.Object,
        _mockLogger.Object);
}
```

**Test Coverage Checklist:**

- ✅ Sorting validation (already sorted by Story 4.5, verify preserved)
- ✅ Top 10 easiest selection (scores 0-33)
- ✅ Bottom 10 hardest selection (scores 67-100)
- ✅ Edge case: Fewer than 10 easy projects
- ✅ Edge case: Fewer than 10 hard projects
- ✅ Edge case: No easy projects (empty EasiestCandidates)
- ✅ Edge case: No hard projects (empty HardestCandidates)
- ✅ Statistics calculation (correct counts)
- ✅ Statistics validation (sum equals total)
- ✅ Cancellation support

**Sample Test:**

```csharp
[Fact]
public async Task GenerateRankedListAsync_WithMultipleProjects_ReturnsCorrectRanking()
{
    // Arrange
    var graph = new DependencyGraph(); // Mock graph

    var mockScores = new List<ExtractionScore>
    {
        CreateMockScore("ProjectA", 10),  // Easy
        CreateMockScore("ProjectB", 25),  // Easy
        CreateMockScore("ProjectC", 50),  // Medium
        CreateMockScore("ProjectD", 75),  // Hard
        CreateMockScore("ProjectE", 90)   // Hard
    };

    _mockScoreCalculator
        .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockScores);

    // Act
    var result = await _generator.GenerateRankedListAsync(graph);

    // Assert
    result.AllProjects.Should().HaveCount(5);
    result.AllProjects.Should().BeInAscendingOrder(s => s.FinalScore);
    result.EasiestCandidates.Should().HaveCount(2); // ProjectA, ProjectB
    result.HardestCandidates.Should().HaveCount(2); // ProjectE (90), ProjectD (75) - descending
    result.Statistics.TotalProjects.Should().Be(5);
    result.Statistics.EasyCount.Should().Be(2);
    result.Statistics.MediumCount.Should().Be(1);
    result.Statistics.HardCount.Should().Be(2);
    result.Statistics.IsValid.Should().BeTrue();
}

private ExtractionScore CreateMockScore(string projectName, double score)
{
    // Create minimal mock ExtractionScore for testing
    return new ExtractionScore(
        ProjectName: projectName,
        ProjectPath: $"/path/to/{projectName}.csproj",
        FinalScore: score,
        CouplingMetric: null,
        ComplexityMetric: null!,
        TechDebtMetric: null!,
        ExternalApiMetric: null!);
}
```

### Previous Story Intelligence

**From Story 4.5 (Extraction Score Calculator) - Patterns to Reuse:**

1. **Record Model Pattern:**
   ```csharp
   // Story 4.5 used nested records (ExtractionScore contains 4 metric records)
   // Story 4.6 uses same pattern (RankedExtractionCandidates contains ExtractionStatistics record)
   public sealed record RankedExtractionCandidates(...)
   public sealed record ExtractionStatistics(...)
   ```

2. **Single Dependency Pattern:**
   ```csharp
   // Story 4.5: 6 dependencies (4 calculators + IConfiguration + ILogger)
   // Story 4.6: 2 dependencies (IExtractionScoreCalculator + ILogger)
   // Constructor injection with validation
   public RankedCandidateGenerator(
       IExtractionScoreCalculator scoreCalculator,
       ILogger<RankedCandidateGenerator> logger)
   ```

3. **DI Registration Pattern:**
   ```csharp
   // From Story 4.5
   services.AddSingleton<IExtractionScoreCalculator, ExtractionScoreCalculator>();
   // Story 4.6
   services.AddSingleton<IRankedCandidateGenerator, RankedCandidateGenerator>();
   ```

4. **Test Strategy Consistency:**
   ```csharp
   // Story 4.5: Unit tests with mocking (4 metric calculators)
   // Story 4.6: Unit tests with mocking (IExtractionScoreCalculator)
   // Both test orchestration, not underlying calculation
   ```

5. **Batch Processing Pattern:**
   ```csharp
   // Story 4.5: CalculateForAllProjectsAsync(DependencyGraph) returns sorted list
   // Story 4.6: GenerateRankedListAsync(DependencyGraph) consumes sorted list
   // No re-sorting needed in Story 4.6
   ```

**From Story 4.1 (Coupling Metric Calculator) - Statistics Pattern:**

Story 4.1 calculated statistics for coupling metrics. Story 4.6 applies similar pattern for extraction difficulty:

```csharp
// Story 4.1 pattern (adapted for Story 4.6)
var easyCount = allScores.Count(s => s.FinalScore <= 33);
var mediumCount = allScores.Count(s => s.FinalScore > 33 && s.FinalScore < 67);
var hardCount = allScores.Count(s => s.FinalScore >= 67);

_logger.LogInformation(
    "Generated ranked extraction candidates: {TotalProjects} total, {EasyCount} easy (0-33), {MediumCount} medium (34-66), {HardCount} hard (67-100)",
    allScores.Count,
    easyCount,
    mediumCount,
    hardCount);
```

**Key Differences from Previous Stories:**

| Aspect | Stories 4.1-4.5 | Story 4.6 |
|--------|-----------------|-----------|
| Complexity | Heavy calculation logic | **Thin wrapper (LINQ filtering)** |
| Dependencies | Multiple (4-6 dependencies) | **Single (IExtractionScoreCalculator + ILogger)** |
| Algorithm | Complex (Tarjan's, Roslyn, weighted sum) | **Simple (LINQ sorting/filtering)** |
| Configuration | Thresholds or JSON config | **No configuration needed** |
| Test Strategy | Integration or unit with mocking | **Unit tests with mocking** |
| Purpose | Calculate metrics | **Prepare for consumption (reports, visualization)** |

### Git Intelligence Summary

**Recent Commits Pattern:**

Last 5 commits show consistent code review process:
1. `85e7dc8` Code review fixes for Story 4-5: Implement extraction score calculator with configurable weights
2. `4631a61` Code review fixes for Story 4-3: Implement technology version debt analyzer
3. `5911da1` Code review fixes for Story 4-2: Implement cyclomatic complexity calculator with Roslyn
4. `cc24f3c` Code review fixes for Story 4-1: Implement coupling metric calculator
5. `78f33d1` Code review fixes for Story 3-7: Mark suggested break points in YELLOW on visualizations

**Pattern:** Initial commit → Code review → Fixes commit → Status update commit

**Expected Commit Sequence for Story 4.6:**

1. Initial commit: "Story 4-6 complete: Generate ranked extraction candidate lists"
2. Code review identifies 5-10 issues (based on Epic 4 pattern)
3. Fixes commit: "Code review fixes for Story 4-6: Generate ranked extraction candidate lists"
4. Status update: Update sprint-status.yaml from in-progress → review → done

**Expected File Changes for Story 4.6:**

Based on Epic 4 pattern (Story 4.6 is SIMPLER than previous stories):
- New: `src/MasDependencyMap.Core/ExtractionScoring/RankedExtractionCandidates.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/IRankedCandidateGenerator.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/RankedCandidateGenerator.cs`
- New: `tests/MasDependencyMap.Core.Tests/ExtractionScoring/RankedCandidateGeneratorTests.cs`
- Modified: `src/MasDependencyMap.CLI/Program.cs` (DI registration only)
- Modified: `_bmad-output/implementation-artifacts/sprint-status.yaml` (story status update)
- Modified: `_bmad-output/implementation-artifacts/4-6-generate-ranked-extraction-candidate-lists.md` (completion notes)

**Fewer files than previous Epic 4 stories:**
- No configuration files (Story 4.5 had scoring-config.json)
- No .csproj modifications (no new packages)
- No new exception classes (reuses existing)

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 4.6 completes Epic 4's CONSUMER-FACING LAYER, preparing data for Epic 5 (Reporting) and Stories 4.7-4.8 (Visualization):

```
src/MasDependencyMap.Core/ExtractionScoring/
├── [Stories 4.1-4.5: Metric calculation and scoring infrastructure]
├── RankedExtractionCandidates.cs        # Story 4.6 (NEW - CONSUMPTION MODEL)
├── IRankedCandidateGenerator.cs         # Story 4.6 (NEW - GENERATOR)
└── RankedCandidateGenerator.cs          # Story 4.6 (NEW - GENERATOR)
```

**Epic 4 Transition Point:**

After Story 4.6, Epic 4 transitions from calculation to consumption:
1. ✅ Stories 4.1-4.4: Metric calculators (input: projects, output: individual metrics)
2. ✅ Story 4.5: Score calculator (input: graph, output: combined scores)
3. ✅ Story 4.6: Ranked list generator (input: graph, output: sorted/filtered lists) **← THIS STORY**
4. ⏳ Story 4.7: Heat map visualization (input: RankedExtractionCandidates, output: colored DOT)
5. ⏳ Story 4.8: Node labels (input: RankedExtractionCandidates, output: labeled DOT)

**Epic 5 (Reporting) Will Consume Story 4.6:**
- Story 5.1: Text report generator (consumes RankedExtractionCandidates.AllProjects)
- Story 5.3: Extraction difficulty section (consumes RankedExtractionCandidates.EasiestCandidates, HardestCandidates, Statistics)
- Story 5.5: CSV export (consumes RankedExtractionCandidates.AllProjects for full export)

**Dependency Flow:**

```
Stories 4.1-4.4 (Individual Metrics)
        ↓
Story 4.5 (Combined Score Calculator)
        ↓
Story 4.6 (Ranked List Generator - THIS STORY)
        ↓
Stories 4.7-4.8 (Visualization) + Epic 5 (Reporting)
```

Story 4.6 is the FINAL PREPARATION LAYER before consumption by reports and visualizations.

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-4-extraction-difficulty-scoring-and-candidate-ranking.md, Story 4.6 (lines 103-118)]
- Story requirements: Sort projects by extraction score ascending, identify top 10 easiest and bottom 10 hardest, provide statistics

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 18-51)]
- Technology stack: .NET 8.0, C# 12, LINQ for filtering/sorting
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces, async patterns, file-scoped namespaces, record types
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 114-119)]
- Logging: Structured logging with named placeholders, ILogger<T> injection

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-5-implement-extraction-score-calculator-with-configurable-weights.md]
- ExtractionScore model structure, batch processing pattern (CalculateForAllProjectsAsync returns sorted list)
- Record model pattern, DI registration pattern, unit testing with mocking

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Code review pattern: Initial commit → Code review fixes (5-10 issues) → Status update
- Epic 4 stories follow consistent pattern: record models, singleton services, comprehensive tests

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

No debug issues encountered during implementation.

### Completion Notes List

✅ **Story 4.6 Implementation Complete**

**Summary:**
Implemented RankedCandidateGenerator service that generates ranked lists of extraction candidates sorted by difficulty score. This is a thin orchestration layer over IExtractionScoreCalculator (Story 4.5) that performs sorting, filtering, and statistics calculation.

**Components Implemented:**
1. **RankedExtractionCandidates Record Model** (src/MasDependencyMap.Core/ExtractionScoring/RankedExtractionCandidates.cs)
   - Sealed record with AllProjects (sorted ascending), EasiestCandidates (top 10, 0-33), HardestCandidates (bottom 10, 67-100), Statistics
   - Nested ExtractionStatistics record with validation (IsValid property)
   - Full XML documentation

2. **IRankedCandidateGenerator Interface** (src/MasDependencyMap.Core/ExtractionScoring/IRankedCandidateGenerator.cs)
   - Single method: GenerateRankedListAsync(DependencyGraph, CancellationToken)
   - Returns Task<RankedExtractionCandidates>
   - Comprehensive XML documentation with usage examples

3. **RankedCandidateGenerator Implementation** (src/MasDependencyMap.Core/ExtractionScoring/RankedCandidateGenerator.cs)
   - Constructor injection: IExtractionScoreCalculator, ILogger<RankedCandidateGenerator>
   - Leverages pre-sorted output from Story 4.5 (already ascending)
   - LINQ filtering for categories: Easy (≤33), Medium (34-66), Hard (≥67)
   - Top 10 easiest: Take(10) on easy candidates (already sorted)
   - Bottom 10 hardest: OrderByDescending + Take(10) on hard candidates
   - Statistics calculation with validation
   - Structured logging with named placeholders (6 log statements)
   - ConfigureAwait(false) for library code

4. **DI Registration** (src/MasDependencyMap.CLI/Program.cs)
   - Added services.TryAddSingleton<IRankedCandidateGenerator, RankedCandidateGenerator>() after IExtractionScoreCalculator
   - Follows Epic 4 pattern (singleton lifetime for stateless service)

5. **Comprehensive Unit Tests** (tests/MasDependencyMap.Core.Tests/ExtractionScoring/RankedCandidateGeneratorTests.cs)
   - 14 tests covering all functionality
   - Moq for mocking IExtractionScoreCalculator
   - Edge cases: <10 easy/hard, no easy/hard, boundary scores (33, 67)
   - Validation: sorting, filtering, statistics, cancellation
   - All tests pass ✅

**Test Results:**
- RankedCandidateGeneratorTests: 14/14 tests passed
- Full test suite: 351/351 tests passed (no regressions)

**Key Design Decisions:**
1. **No Re-Sorting:** Leveraged Story 4.5's pre-sorted output (already ascending) for efficiency
2. **Separate Top/Bottom Properties:** EasiestCandidates and HardestCandidates for quick access without filtering
3. **Statistics Validation:** IsValid property on ExtractionStatistics ensures data integrity
4. **Minimal Business Logic:** Thin wrapper around Story 4.5 - pure LINQ filtering and sorting
5. **No Configuration Required:** Unlike Stories 4.3-4.5, no JSON config needed

**Architecture Compliance:**
- ✅ Feature-based namespace (MasDependencyMap.Core.ExtractionScoring)
- ✅ File-scoped namespace declarations
- ✅ Async suffix on all async methods
- ✅ ILogger<T> injection (not static logger)
- ✅ ConfigureAwait(false) in library code
- ✅ XML documentation on all public APIs
- ✅ Structured logging with named placeholders
- ✅ Record types for immutability

**Integration Points:**
- Consumes: IExtractionScoreCalculator (Story 4.5)
- Will be consumed by: Epic 5 (Reporting), Story 4.7 (Heat Maps), Story 4.8 (Node Labels)

### File List

**New Files Created:**
- src/MasDependencyMap.Core/ExtractionScoring/RankedExtractionCandidates.cs
- src/MasDependencyMap.Core/ExtractionScoring/IRankedCandidateGenerator.cs
- src/MasDependencyMap.Core/ExtractionScoring/RankedCandidateGenerator.cs
- tests/MasDependencyMap.Core.Tests/ExtractionScoring/RankedCandidateGeneratorTests.cs

**Modified Files:**
- src/MasDependencyMap.CLI/Program.cs (DI registration)
- _bmad-output/implementation-artifacts/sprint-status.yaml (story status)
- _bmad-output/implementation-artifacts/4-6-generate-ranked-extraction-candidate-lists.md (completion notes)

### Code Review Fixes Applied

**Performance Optimizations:**
1. Added `IsEnabled(LogLevel.Debug)` check before expensive string.Join operations to avoid allocations when debug logging disabled
2. Optimized ToList() calls - use GetRange for subsetting existing lists instead of creating new allocations
3. Deferred LINQ materialization to avoid unnecessary intermediate collections

**Documentation Improvements:**
4. Added difficulty category boundary documentation to RankedExtractionCandidates XML comments (33.0 inclusive, 67.0 inclusive)
5. Clarified HardestCandidates sorting order as descending (hardest first) in XML documentation
6. Documented empty graph behavior in IRankedCandidateGenerator interface

**Bug Fixes:**
7. Fixed sprint-status.yaml invalid status "completed" → "done" for story 4-5

**Git Hygiene:**
8. Separated commits for stories 4-4, 4-5, and 4-6 to maintain clean git history
