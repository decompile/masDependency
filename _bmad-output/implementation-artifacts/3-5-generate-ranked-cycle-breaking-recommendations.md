# Story 3.5: Generate Ranked Cycle-Breaking Recommendations

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want ranked recommendations for which dependencies to break first,
So that I can make data-driven decisions about cycle resolution.

## Acceptance Criteria

**Given** Weak coupling edges have been identified
**When** Cycle-breaking recommendations are generated
**Then** CycleBreakingSuggestion objects are created for each weak edge
**And** Suggestions include: source project, target project, coupling score, rationale
**And** Rationale explains why this edge is recommended (e.g., "Weakest link in 8-project cycle, only 3 method calls")
**And** Suggestions are ranked by coupling score (lowest first)
**And** Top 5 cycle-breaking recommendations are prominently featured in reports

## Tasks / Subtasks

- [x] Create CycleBreakingSuggestion model (AC: Data structure for recommendations)
  - [x] Create CycleBreakingSuggestion record in Core.CycleAnalysis namespace
  - [x] Add SourceProject property (ProjectNode)
  - [x] Add TargetProject property (ProjectNode)
  - [x] Add CouplingScore property (int, from DependencyEdge)
  - [x] Add Rationale property (string, human-readable explanation)
  - [x] Add CycleSize property (int, size of cycle this edge belongs to)
  - [x] Add CycleId property (int, reference to parent cycle)
  - [x] Add comprehensive XML documentation for all properties
  - [x] Implement IComparable<CycleBreakingSuggestion> for natural sorting by coupling score

- [x] Create IRecommendationGenerator interface (AC: Service contract)
  - [x] Create IRecommendationGenerator interface in Core.CycleAnalysis namespace
  - [x] Define GenerateRecommendationsAsync method accepting cycles with weak edges
  - [x] Return IReadOnlyList<CycleBreakingSuggestion> ranked by coupling score
  - [x] Include CancellationToken parameter for long-running operations
  - [x] Add XML documentation describing contract

- [x] Implement RecommendationGenerator service (AC: Generate recommendations from weak edges)
  - [x] Create RecommendationGenerator class in Core.CycleAnalysis namespace
  - [x] Inject ILogger<RecommendationGenerator> via constructor
  - [x] Iterate through all cycles with weak edges populated
  - [x] For each weak edge in each cycle, create CycleBreakingSuggestion
  - [x] Generate contextual rationale for each suggestion
  - [x] Rank all suggestions by coupling score (lowest first)
  - [x] Handle edge cases: empty cycles, no weak edges, ties in coupling scores

- [x] Implement rationale generation logic (AC: Helpful explanations)
  - [x] Generate rationale template: "Weakest link in {cycleSize}-project cycle, only {couplingScore} method calls"
  - [x] Handle special cases for coupling score = 1: "single method call" vs "method calls"
  - [x] Include cycle size context to help architect prioritize
  - [x] For large cycles (>5 projects), emphasize high impact potential
  - [x] For small cycles (2-3 projects), emphasize simplicity of fix
  - [x] Ensure rationale is actionable and non-technical (architect-friendly)

- [x] Implement recommendation ranking (AC: Lowest coupling first)
  - [x] Sort recommendations by CouplingScore ascending (lowest = easiest to break)
  - [x] Use LINQ .OrderBy() for primary sort
  - [x] Add secondary sort by CycleSize descending (break largest cycles first if tied)
  - [x] Add tertiary sort by SourceProject.Name for deterministic ordering
  - [x] Return IReadOnlyList to prevent external modification

- [x] Add structured logging for recommendation generation (AC: Observability)
  - [x] Log "Generating cycle-breaking recommendations from {CycleCount} cycles" at Information level
  - [x] Log "Cycle {CycleId}: {WeakEdgeCount} weak edges found" at Debug level
  - [x] Log "Generated {RecommendationCount} cycle-breaking recommendations" at Information level
  - [x] Log "Top recommendation: {SourceProject} → {TargetProject} (score: {Score})" at Information level
  - [x] Use named placeholders, NOT string interpolation
  - [x] Include summary statistics in final log

- [x] Handle edge cases and validation (AC: Robustness)
  - [x] Handle empty cycle list → return empty recommendations list
  - [x] Handle cycles with no weak edges → skip cycle gracefully
  - [x] Handle null cycles or null graph → throw ArgumentNullException
  - [x] Handle cancellation token → check periodically in loops
  - [x] Validate all cycles have WeakCouplingEdges populated (defensive)

- [x] Register service in DI container (AC: Dependency injection)
  - [x] Register IRecommendationGenerator → RecommendationGenerator as singleton
  - [x] Use services.TryAddSingleton() for test override support
  - [x] Ensure registration after IWeakEdgeIdentifier (dependency order)

- [x] Create comprehensive tests (AC: Algorithm correctness)
  - [x] Unit test: Empty cycle list → returns empty recommendations
  - [x] Unit test: Single cycle with one weak edge → one recommendation created
  - [x] Unit test: Multiple weak edges → all generate recommendations
  - [x] Unit test: Recommendations ranked by coupling score (lowest first)
  - [x] Unit test: Tied coupling scores → secondary sort by cycle size
  - [x] Unit test: Rationale format matches expected template
  - [x] Unit test: Special rationale for coupling score = 1
  - [x] Unit test: Large cycle (>5 projects) rationale emphasizes impact
  - [x] Unit test: Null cycles → throws ArgumentNullException
  - [x] Unit test: Cancellation token support
  - [x] Unit test: Top 5 recommendations extraction

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - CycleBreakingSuggestion Model:**

**From Epic 3 Story 3.5 Acceptance Criteria:**

The recommendation model must include all data needed for architect decision-making and reporting:

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Represents a recommendation for breaking a circular dependency.
/// Generated from weak coupling edges identified in cycle analysis.
/// </summary>
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
    /// Lower scores indicate easier dependencies to break.
    /// </summary>
    public int CouplingScore { get; init; }

    /// <summary>
    /// Size of the cycle this edge belongs to (number of projects).
    /// Larger cycles have higher impact when broken.
    /// </summary>
    public int CycleSize { get; init; }

    /// <summary>
    /// Human-readable rationale explaining why this edge is recommended.
    /// Example: "Weakest link in 8-project cycle, only 3 method calls"
    /// </summary>
    public string Rationale { get; init; }

    /// <summary>
    /// Priority ranking (1 = highest priority, calculated from coupling and cycle size).
    /// Lower coupling scores get higher priority (lower rank number).
    /// </summary>
    public int Rank { get; init; }

    public CycleBreakingSuggestion(
        int cycleId,
        ProjectNode sourceProject,
        ProjectNode targetProject,
        int couplingScore,
        int cycleSize,
        string rationale)
    {
        if (sourceProject == null) throw new ArgumentNullException(nameof(sourceProject));
        if (targetProject == null) throw new ArgumentNullException(nameof(targetProject));
        if (string.IsNullOrWhiteSpace(rationale)) throw new ArgumentException("Rationale cannot be empty", nameof(rationale));

        CycleId = cycleId;
        SourceProject = sourceProject;
        TargetProject = targetProject;
        CouplingScore = couplingScore;
        CycleSize = cycleSize;
        Rationale = rationale;
        Rank = 0; // Set by generator after sorting
    }

    /// <summary>
    /// Natural ordering: lowest coupling score first, then largest cycle size.
    /// </summary>
    public int CompareTo(CycleBreakingSuggestion? other)
    {
        if (other == null) return 1;

        // Primary: Lowest coupling score first (easier to break)
        var couplingComparison = CouplingScore.CompareTo(other.CouplingScore);
        if (couplingComparison != 0) return couplingComparison;

        // Secondary: Largest cycle size first (higher impact)
        var cycleSizeComparison = other.CycleSize.CompareTo(CycleSize); // Reversed for descending
        if (cycleSizeComparison != 0) return cycleSizeComparison;

        // Tertiary: Alphabetical by source project name (deterministic)
        return string.Compare(
            SourceProject.ProjectName,
            other.SourceProject.ProjectName,
            StringComparison.OrdinalIgnoreCase);
    }
}
```

**Why This Design:**
- `record` type for immutability and value equality
- `IComparable` for natural sorting in collections
- `Rank` property set after sorting for reporting (1 = top recommendation)
- Validation in constructor ensures data integrity
- All properties required for architect decision-making and Epic 5 reporting

🚨 **CRITICAL - Rationale Generation Pattern:**

**From Epic 3 Story 3.5 Acceptance Criteria:**

Rationale must be human-readable and actionable, explaining WHY this edge is recommended:

```csharp
private string GenerateRationale(DependencyEdge edge, CycleInfo cycle)
{
    var couplingScore = edge.CouplingScore;
    var cycleSize = cycle.CycleSize;

    // Determine impact level based on cycle size
    string impactContext = cycleSize switch
    {
        >= 10 => $"critical {cycleSize}-project cycle",
        >= 6 => $"large {cycleSize}-project cycle",
        >= 4 => $"{cycleSize}-project cycle",
        _ => $"small {cycleSize}-project cycle"
    };

    // Format coupling description
    string couplingDescription = couplingScore switch
    {
        1 => "only 1 method call",
        2 => "just 2 method calls",
        <= 5 => $"only {couplingScore} method calls",
        _ => $"{couplingScore} method calls"
    };

    // Build rationale
    return $"Weakest link in {impactContext}, {couplingDescription}";
}
```

**Example Rationales:**
- Coupling=1, Cycle=8: "Weakest link in large 8-project cycle, only 1 method call"
- Coupling=3, Cycle=12: "Weakest link in critical 12-project cycle, only 3 method calls"
- Coupling=2, Cycle=3: "Weakest link in small 3-project cycle, just 2 method calls"
- Coupling=7, Cycle=5: "Weakest link in 5-project cycle, 7 method calls"

**Key Points:**
- Emphasizes "weakest link" (easy to break)
- Includes cycle size for impact context
- Uses friendly language ("only", "just") for low coupling scores
- Differentiates between small/large/critical cycles
- Actionable for architects without deep technical knowledge

🚨 **CRITICAL - Recommendation Ranking Algorithm:**

**From Epic 3 Story 3.5 Acceptance Criteria:**

"Suggestions are ranked by coupling score (lowest first)"

**Sorting Strategy:**

```csharp
public async Task<IReadOnlyList<CycleBreakingSuggestion>> GenerateRecommendationsAsync(
    IReadOnlyList<CycleInfo> cycles,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(cycles);

    _logger.LogInformation(
        "Generating cycle-breaking recommendations from {CycleCount} cycles",
        cycles.Count);

    var recommendations = new List<CycleBreakingSuggestion>();

    foreach (var cycle in cycles)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (cycle.WeakCouplingEdges == null || cycle.WeakCouplingEdges.Count == 0)
        {
            _logger.LogDebug(
                "Cycle {CycleId}: No weak edges identified, skipping",
                cycle.CycleId);
            continue;
        }

        _logger.LogDebug(
            "Cycle {CycleId}: {WeakEdgeCount} weak edges found",
            cycle.CycleId,
            cycle.WeakCouplingEdges.Count);

        // Generate recommendation for each weak edge
        foreach (var edge in cycle.WeakCouplingEdges)
        {
            var rationale = GenerateRationale(edge, cycle);

            var suggestion = new CycleBreakingSuggestion(
                cycleId: cycle.CycleId,
                sourceProject: edge.Source,
                targetProject: edge.Target,
                couplingScore: edge.CouplingScore,
                cycleSize: cycle.CycleSize,
                rationale: rationale);

            recommendations.Add(suggestion);
        }
    }

    // Rank recommendations: lowest coupling first, then largest cycle, then alphabetical
    var rankedRecommendations = recommendations
        .OrderBy(r => r.CouplingScore)           // Primary: Lowest coupling first
        .ThenByDescending(r => r.CycleSize)      // Secondary: Largest cycle first
        .ThenBy(r => r.SourceProject.ProjectName) // Tertiary: Alphabetical
        .ToList();

    // Assign rank numbers (1-based)
    for (int i = 0; i < rankedRecommendations.Count; i++)
    {
        var updated = rankedRecommendations[i] with { Rank = i + 1 };
        rankedRecommendations[i] = updated;
    }

    _logger.LogInformation(
        "Generated {RecommendationCount} cycle-breaking recommendations",
        rankedRecommendations.Count);

    if (rankedRecommendations.Count > 0)
    {
        var top = rankedRecommendations[0];
        _logger.LogInformation(
            "Top recommendation: {SourceProject} → {TargetProject} (coupling: {Score}, cycle size: {CycleSize})",
            top.SourceProject.ProjectName,
            top.TargetProject.ProjectName,
            top.CouplingScore,
            top.CycleSize);
    }

    return rankedRecommendations;
}
```

**Ranking Strategy Rationale:**
1. **Primary Sort (Coupling Score):** Lower scores = easier to break → Higher priority
2. **Secondary Sort (Cycle Size):** Larger cycles = higher impact → Prefer breaking big cycles
3. **Tertiary Sort (Project Name):** Deterministic ordering for tied recommendations

**Top 5 Extraction:**
```csharp
// In reporting or visualization code (Epic 5, Stories 3.6-3.7)
var top5 = recommendations.Take(5).ToList();
```

🚨 **CRITICAL - Integration with Story 3-4 Output:**

**From Story 3-4 Implementation:**

Story 3.5 directly depends on Story 3-4's weak edge identification:

```csharp
// Expected workflow integration in CLI (Program.cs or AnalyzeCommand)
var solution = await solutionLoader.LoadAsync(solutionPath);                           // Epic 2
var graph = await graphBuilder.BuildAsync(solution);                                   // Epic 2
var cycles = await cycleDetector.DetectCyclesAsync(graph);                             // Story 3.1
var annotatedGraph = await couplingAnalyzer.AnalyzeAsync(graph, solution);             // Story 3.3
var cyclesWithWeakEdges = weakEdgeIdentifier.IdentifyWeakEdges(cycles, annotatedGraph); // Story 3.4
var recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cyclesWithWeakEdges); // Story 3.5 (NEW)
```

**Input Validation:**
- Assumes all cycles have WeakCouplingEdges populated by Story 3-4
- Gracefully skips cycles where WeakCouplingEdges is null or empty
- No explicit validation needed since Story 3-4 guarantees data integrity

**Data Flow:**
1. Story 3-1: Detects cycles → CycleInfo with Projects
2. Story 3-3: Annotates graph with coupling scores → DependencyEdge.CouplingScore
3. Story 3-4: Identifies weak edges → CycleInfo.WeakCouplingEdges populated
4. **Story 3-5 (THIS STORY):** Generates recommendations → CycleBreakingSuggestion list
5. Story 3-6, 3-7: Visualizes recommendations → Yellow edges in DOT graph
6. Epic 5: Reports recommendations → Text/CSV export

🚨 **CRITICAL - Structured Logging (Named Placeholders):**

**From Project Context (lines 115-119):**

```
Use structured logging with named placeholders
NEVER use string interpolation in log messages
```

**Correct Logging Pattern:**

```csharp
// ✅ CORRECT: Named placeholders
_logger.LogInformation(
    "Generating cycle-breaking recommendations from {CycleCount} cycles",
    cycles.Count);

_logger.LogDebug(
    "Cycle {CycleId}: {WeakEdgeCount} weak edges found",
    cycle.CycleId,
    cycle.WeakCouplingEdges.Count);

_logger.LogInformation(
    "Generated {RecommendationCount} cycle-breaking recommendations",
    rankedRecommendations.Count);

_logger.LogInformation(
    "Top recommendation: {SourceProject} → {TargetProject} (coupling: {Score}, cycle size: {CycleSize})",
    top.SourceProject.ProjectName,
    top.TargetProject.ProjectName,
    top.CouplingScore,
    top.CycleSize);

// ❌ WRONG: String interpolation
_logger.LogInformation($"Generating recommendations from {cycles.Count} cycles"); // DO NOT USE
```

### Technical Requirements

**Algorithm Complexity:**

**Time Complexity:**
- Per cycle: O(w) where w = weak edges per cycle (typically 1-3)
- Per solution: O(c * w) where c = cycles
- Sorting: O(n log n) where n = total recommendations
- Overall: O(c * w + n log n), dominated by sorting for large solution
- Acceptable for typical solutions (hundreds of recommendations)

**Space Complexity:**
- O(n) where n = total recommendations
- Each recommendation is lightweight (references + strings)
- Total memory footprint minimal

**Optimization Strategies:**
1. Use single LINQ chain for multi-level sorting (efficient)
2. Avoid materializing intermediate collections
3. Generate rationale on-demand (not cached)
4. Process cycles sequentially (fast operation, no parallelization needed)

**Edge Cases to Handle:**

1. **Empty Cycle List:**
   - Input: cycles.Count == 0
   - Output: Return empty list, log "No cycles to analyze"

2. **Cycles with No Weak Edges:**
   - Input: cycle.WeakCouplingEdges.Count == 0
   - Output: Skip cycle, log Debug message
   - NOT an error (valid state if cycle has no edges)

3. **All Recommendations Have Same Coupling Score:**
   - Input: All weak edges have CouplingScore = 5
   - Output: Secondary sort by CycleSize (largest first)
   - Tertiary sort by ProjectName (alphabetical)

4. **Single Recommendation:**
   - Input: 1 cycle with 1 weak edge
   - Output: 1 recommendation with Rank = 1

5. **Tied Recommendations:**
   - Input: Multiple edges with same coupling score and cycle size
   - Output: Tertiary sort by SourceProject.ProjectName ensures deterministic ordering

6. **Large Number of Recommendations:**
   - Input: 50 cycles with average 2 weak edges each = 100 recommendations
   - Output: All ranked, top 5 featured prominently in reports

### Architecture Compliance

**Epic 3 Architecture Requirements:**

```
- TarjanCycleDetector using QuikGraph's Tarjan's SCC algorithm ✅ (Story 3.1)
- Cycle statistics calculation ✅ (Story 3.2)
- CouplingAnalyzer for method call counting ✅ (Story 3.3)
- WeakEdgeIdentifier for finding weakest edges ✅ (Story 3.4)
- Ranked cycle-breaking recommendations ⏳ (Story 3.5 - THIS STORY)
- Enhanced DOT visualization with cycle highlighting ⏳ (Stories 3.6, 3.7)
```

**Story 3.5 Implements:**
- ✅ CycleBreakingSuggestion model for recommendation data
- ✅ IRecommendationGenerator service for generating recommendations
- ✅ RecommendationGenerator implementation with ranking algorithm
- ✅ Rationale generation with context-aware messaging
- ✅ Multi-level sorting (coupling, cycle size, project name)
- ✅ Structured logging for recommendation generation
- ✅ Integration with Story 3.4 weak edge data

**Integration with Existing Components:**

Story 3.5 consumes:
- **CycleInfo** (from Story 3.1): Extended with WeakCouplingEdges (from Story 3.4)
- **DependencyEdge** (from Story 2-5): Edges with coupling scores (from Story 3.3)
- **ProjectNode** (from Story 2-5): Source and target project references
- **ILogger<T>** (from Story 1-6): Structured logging

Story 3.5 produces:
- **CycleBreakingSuggestion list**: Consumed by Epic 5 (reporting), Stories 3.6-3.7 (visualization)
- **Ranked recommendations**: Used in CLI output, text reports, CSV exports
- **Top 5 recommendations**: Featured prominently in reports and visualizations

**Namespace Organization:**

```
src/MasDependencyMap.Core/
└── CycleAnalysis/                        # Epic 3 namespace
    ├── ITarjanCycleDetector.cs          # Story 3.1
    ├── TarjanCycleDetector.cs           # Story 3.1
    ├── CycleInfo.cs                     # Story 3.1 (extended in 3.4)
    ├── ICycleStatisticsCalculator.cs    # Story 3.2
    ├── CycleStatisticsCalculator.cs     # Story 3.2
    ├── CycleStatistics.cs               # Story 3.2
    ├── ICouplingAnalyzer.cs             # Story 3.3
    ├── RoslynCouplingAnalyzer.cs        # Story 3.3
    ├── MethodCallCounterWalker.cs       # Story 3.3
    ├── CouplingStrength.cs              # Story 3.3
    ├── IWeakEdgeIdentifier.cs           # Story 3.4
    ├── WeakEdgeIdentifier.cs            # Story 3.4
    ├── CycleBreakingSuggestion.cs       # NEW: Story 3.5
    ├── IRecommendationGenerator.cs      # NEW: Story 3.5
    └── RecommendationGenerator.cs       # NEW: Story 3.5
```

**DI Integration:**
```csharp
// Existing (from Stories 3.1-3.4)
services.TryAddSingleton<ITarjanCycleDetector, TarjanCycleDetector>();
services.TryAddSingleton<ICycleStatisticsCalculator, CycleStatisticsCalculator>();
services.TryAddSingleton<ICouplingAnalyzer, RoslynCouplingAnalyzer>();
services.TryAddSingleton<IWeakEdgeIdentifier, WeakEdgeIdentifier>();

// NEW: Story 3.5
services.TryAddSingleton<IRecommendationGenerator, RecommendationGenerator>();
```

### Library/Framework Requirements

**No New NuGet Packages Required:**

All dependencies already satisfied:
- ✅ Microsoft.Extensions.Logging.Abstractions (installed in Story 1-6)
- ✅ Microsoft.Extensions.DependencyInjection (installed in Story 1-5)

**LINQ APIs Used:**
- `.OrderBy()` - Primary sort by coupling score
- `.ThenByDescending()` - Secondary sort by cycle size (descending)
- `.ThenBy()` - Tertiary sort by project name (alphabetical)
- `.ToList()` - Materializing sorted collection
- `.Take(5)` - Extracting top 5 recommendations for reports

**String Formatting:**
- String interpolation for rationale generation (user-facing, not logging)
- `StringComparison.OrdinalIgnoreCase` for case-insensitive sorting

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/
└── CycleAnalysis/
    ├── CycleBreakingSuggestion.cs           # NEW: Recommendation model
    ├── IRecommendationGenerator.cs          # NEW: Recommendation generator interface
    └── RecommendationGenerator.cs           # NEW: Recommendation generator implementation

tests/MasDependencyMap.Core.Tests/
└── CycleAnalysis/
    └── RecommendationGeneratorTests.cs      # NEW: Comprehensive test suite
```

**Files to Modify:**

```
src/MasDependencyMap.CLI/Program.cs
  - Register IRecommendationGenerator in DI container
  - Integrate recommendation generation into CLI workflow (after weak edge identification)
```

### Testing Requirements

**Test Class Structure:**

```csharp
namespace MasDependencyMap.Core.Tests.CycleAnalysis;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;

public class RecommendationGeneratorTests
{
    private readonly ILogger<RecommendationGenerator> _logger;
    private readonly RecommendationGenerator _generator;

    public RecommendationGeneratorTests()
    {
        _logger = NullLogger<RecommendationGenerator>.Instance;
        _generator = new RecommendationGenerator(_logger);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_EmptyCycleList_ReturnsEmptyResult()
    {
        // Arrange
        var cycles = new List<CycleInfo>();

        // Act
        var result = await _generator.GenerateRecommendationsAsync(cycles);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_SingleCycleOneWeakEdge_OneRecommendationCreated()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(
            cycleId: 1,
            cycleSize: 3,
            weakEdgeCouplingScores: new[] { 5 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        result.Should().HaveCount(1);
        var recommendation = result.Single();
        recommendation.CycleId.Should().Be(1);
        recommendation.CouplingScore.Should().Be(5);
        recommendation.CycleSize.Should().Be(3);
        recommendation.Rank.Should().Be(1);
        recommendation.Rationale.Should().Contain("3-project cycle");
        recommendation.Rationale.Should().Contain("5 method calls");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_MultipleWeakEdges_AllGenerateRecommendations()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(
            cycleId: 1,
            cycleSize: 5,
            weakEdgeCouplingScores: new[] { 2, 3, 2 }); // 3 weak edges

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(r =>
        {
            r.CycleId.Should().Be(1);
            r.CycleSize.Should().Be(5);
        });
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RecommendationsRankedByCouplingScore_LowestFirst()
    {
        // Arrange
        var cycle1 = CreateCycleWithWeakEdges(1, 4, new[] { 10 });
        var cycle2 = CreateCycleWithWeakEdges(2, 6, new[] { 3 });
        var cycle3 = CreateCycleWithWeakEdges(3, 5, new[] { 7 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle1, cycle2, cycle3 });

        // Assert
        result.Should().HaveCount(3);
        result[0].CouplingScore.Should().Be(3);  // Lowest coupling first
        result[0].Rank.Should().Be(1);
        result[1].CouplingScore.Should().Be(7);
        result[1].Rank.Should().Be(2);
        result[2].CouplingScore.Should().Be(10); // Highest coupling last
        result[2].Rank.Should().Be(3);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_TiedCouplingScores_SecondarySortByCycleSize()
    {
        // Arrange
        var cycle1 = CreateCycleWithWeakEdges(1, 4, new[] { 5 }); // Same coupling, smaller cycle
        var cycle2 = CreateCycleWithWeakEdges(2, 8, new[] { 5 }); // Same coupling, larger cycle

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle1, cycle2 });

        // Assert
        result.Should().HaveCount(2);
        result[0].CouplingScore.Should().Be(5);
        result[0].CycleSize.Should().Be(8); // Larger cycle first (higher impact)
        result[1].CycleSize.Should().Be(4); // Smaller cycle second
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RationaleFormat_MatchesExpectedTemplate()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(1, 8, new[] { 3 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        var recommendation = result.Single();
        recommendation.Rationale.Should().Contain("Weakest link");
        recommendation.Rationale.Should().Contain("8-project cycle");
        recommendation.Rationale.Should().Contain("3 method calls");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_CouplingScoreOne_RationaleUsesSingular()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(1, 3, new[] { 1 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        var recommendation = result.Single();
        recommendation.Rationale.Should().Contain("1 method call"); // Singular, not "1 method calls"
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_LargeCycle_RationaleEmphasizesImpact()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(1, 12, new[] { 4 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        var recommendation = result.Single();
        recommendation.Rationale.Should().Contain("critical").Or.Contain("large"); // Emphasizes size
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_NullCycles_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _generator.GenerateRecommendationsAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_CancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var largeCycleSet = CreateManyCycles(100);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var act = async () => await _generator.GenerateRecommendationsAsync(largeCycleSet, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Top5Extraction_ReturnsCorrectSubset()
    {
        // Arrange
        var cycles = Enumerable.Range(1, 10)
            .Select(i => CreateCycleWithWeakEdges(i, 5, new[] { i }))
            .ToList();

        // Act
        var allRecommendations = await _generator.GenerateRecommendationsAsync(cycles);
        var top5 = allRecommendations.Take(5).ToList();

        // Assert
        allRecommendations.Should().HaveCount(10);
        top5.Should().HaveCount(5);
        top5[0].Rank.Should().Be(1);
        top5[4].Rank.Should().Be(5);
        top5.Should().BeInAscendingOrder(r => r.CouplingScore); // Lowest coupling first
    }

    // Helper methods for creating test data
    private CycleInfo CreateCycleWithWeakEdges(int cycleId, int cycleSize, int[] weakEdgeCouplingScores)
    {
        // Create cycle with projects and weak edges
        var projects = Enumerable.Range(1, cycleSize)
            .Select(i => new ProjectNode($"Project{i}", $"Project{i}.csproj"))
            .ToList();

        var cycle = new CycleInfo(cycleId, projects);

        // Create weak edges with specified coupling scores
        var weakEdges = weakEdgeCouplingScores
            .Select((score, index) => new DependencyEdge(
                projects[index % cycleSize],
                projects[(index + 1) % cycleSize])
            {
                CouplingScore = score
            })
            .ToList();

        cycle.WeakCouplingEdges = weakEdges;
        cycle.WeakCouplingScore = weakEdges.Min(e => e.CouplingScore);

        return cycle;
    }

    private List<CycleInfo> CreateManyCycles(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateCycleWithWeakEdges(i, 5, new[] { i }))
            .ToList();
    }
}
```

**Test Naming Convention:**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `GenerateRecommendationsAsync_EmptyCycleList_ReturnsEmptyResult()`
- ✅ `GenerateRecommendationsAsync_RecommendationsRankedByCouplingScore_LowestFirst()`
- ✅ `GenerateRecommendationsAsync_TiedCouplingScores_SecondarySortByCycleSize()`
- ✅ `GenerateRecommendationsAsync_RationaleFormat_MatchesExpectedTemplate()`

### Previous Story Intelligence

**From Story 3-4 (Identify Weakest Coupling Edges) - Key Learnings:**

Story 3-4 established the weak edge identification pattern that Story 3.5 builds on:
1. Input validation with ArgumentNullException.ThrowIfNull()
2. Graceful handling of edge cases (empty collections, null data)
3. Structured logging with Information and Debug levels
4. Summary statistics logging at the end
5. Comprehensive unit tests (9+ tests)
6. Singleton DI registration

**Patterns to Reuse:**
```csharp
// Argument validation
ArgumentNullException.ThrowIfNull(cycles);

// Structured logging with named placeholders
_logger.LogInformation(
    "Generating cycle-breaking recommendations from {CycleCount} cycles",
    cycles.Count);

// Cancellation token support
cancellationToken.ThrowIfCancellationRequested();

// DI registration
services.TryAddSingleton<IRecommendationGenerator, RecommendationGenerator>();
```

**From Story 3-3 (Coupling Strength Analysis) - Model Extension Pattern:**

Story 3-3 showed how to extend existing models with new data:
- DependencyEdge extended with CouplingScore
- Properties mutable for post-processing
- Service populates properties after creation

Story 3-5 follows similar pattern:
- CycleBreakingSuggestion is NEW model (not extension)
- Created from existing data (CycleInfo + DependencyEdge)
- Immutable `record` type (unlike mutable CycleInfo)

**From Story 3-2 (Cycle Statistics) - Multi-Level Sorting:**

Story 3-2 showed aggregate statistics calculation. Story 3.5 extends this:
- Multi-level sorting with LINQ (.OrderBy, .ThenByDescending, .ThenBy)
- Summary statistics logging
- Top N extraction for reporting

Expected integration in CLI:
```csharp
// In Program.cs or AnalyzeCommand
var solution = await solutionLoader.LoadAsync(solutionPath);                           // Epic 2
var graph = await graphBuilder.BuildAsync(solution);                                   // Epic 2
var cycles = await cycleDetector.DetectCyclesAsync(graph);                             // Story 3.1
var statistics = await statsCalculator.CalculateAsync(cycles, graph);                  // Story 3.2
var annotatedGraph = await couplingAnalyzer.AnalyzeAsync(graph, solution);             // Story 3.3
var cyclesWithWeakEdges = weakEdgeIdentifier.IdentifyWeakEdges(cycles, annotatedGraph); // Story 3.4
var recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cyclesWithWeakEdges); // Story 3.5 (NEW)

// Use recommendations in reporting and visualization
var top5 = recommendations.Take(5).ToList();
await dotGenerator.GenerateAsync(graph, cycles, recommendations); // Stories 3.6-3.7
await textReporter.GenerateAsync(statistics, recommendations);    // Epic 5
```

### Git Intelligence Summary

**Recent Commits Analysis:**

Last 5 commits show Epic 3 story pattern:
1. **ecb8f93:** Code review fixes for Story 3-4
2. **f934770:** Code review fixes for Story 3-2
3. **005b452:** Code review fixes for Story 3-3
4. **b0a32ee:** Story 3-3 complete
5. **53cc115:** Story 3-2 complete

**Pattern Observed:**
- Initial "Story X.Y complete" commit
- Follow-up "Code review fixes for Story X.Y" commit
- Suggests rigorous code review process after implementation

**Commit Message Pattern:**

Story completion commits include:
- Story number and title
- List of new interfaces/classes created
- Key technical details and algorithm description
- Acceptance criteria satisfied
- Test coverage summary
- Co-Authored-By: Claude Sonnet 4.5

**Expected Commit Message for Story 3.5:**
```bash
git commit -m "Story 3-5 complete: Generate ranked cycle-breaking recommendations

- Created CycleBreakingSuggestion record model with IComparable<T> for natural sorting
- Created IRecommendationGenerator interface in Core.CycleAnalysis namespace
- Implemented RecommendationGenerator service with ranking algorithm
- Multi-level sorting: coupling score (lowest first), cycle size (largest first), project name (alphabetical)
- Rationale generation with context-aware messaging (critical/large/small cycle emphasis)
- Special handling for coupling score = 1 (singular "method call")
- Structured logging for generation progress and top recommendation
- Handles edge cases: empty cycles, no weak edges, all equal scores, ties
- Registered IRecommendationGenerator as singleton in DI container
- Created comprehensive unit tests (11+ tests) - all passing
- CancellationToken support for long-running operations
- Rank property assigned after sorting (1-based, 1 = highest priority)
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

**Files Changed in Recent Commits:**

Story 3-4 pattern (from ecb8f93):
- New: Interface, Implementation, Tests
- Modified: DI registration in Program.cs
- Modified: Domain model extension (CycleInfo)
- Modified: Story file, sprint-status.yaml

Story 3-5 expected pattern:
- New: CycleBreakingSuggestion.cs, IRecommendationGenerator.cs, RecommendationGenerator.cs, RecommendationGeneratorTests.cs
- Modified: Program.cs (DI registration)
- Modified: sprint-status.yaml (story status update)

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 3.5 continues Epic 3 namespace organization:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
│   ├── DependencyEdge.cs            # Reused (with coupling from 3.3)
│   └── ProjectNode.cs               # Reused as-is
├── CycleAnalysis/               # Epic 3: Cycle detection, statistics, coupling, weak edges, recommendations
│   ├── ITarjanCycleDetector.cs      # Story 3.1
│   ├── TarjanCycleDetector.cs       # Story 3.1
│   ├── CycleInfo.cs                 # Story 3.1 (extended in 3.4)
│   ├── ICycleStatisticsCalculator.cs # Story 3.2
│   ├── CycleStatisticsCalculator.cs  # Story 3.2
│   ├── CycleStatistics.cs           # Story 3.2
│   ├── ICouplingAnalyzer.cs         # Story 3.3
│   ├── RoslynCouplingAnalyzer.cs    # Story 3.3
│   ├── MethodCallCounterWalker.cs   # Story 3.3
│   ├── CouplingStrength.cs          # Story 3.3
│   ├── IWeakEdgeIdentifier.cs       # Story 3.4
│   ├── WeakEdgeIdentifier.cs        # Story 3.4
│   ├── CycleBreakingSuggestion.cs   # Story 3.5 (NEW)
│   ├── IRecommendationGenerator.cs  # Story 3.5 (NEW)
│   └── RecommendationGenerator.cs   # Story 3.5 (NEW)
└── SolutionLoading/             # Epic 2: Solution loading
```

**Consistency with Existing Patterns:**
- Feature-based namespace: `MasDependencyMap.Core.CycleAnalysis` ✅
- Interface + Implementation pattern: `IRecommendationGenerator`, `RecommendationGenerator` ✅
- Model as separate file: `CycleBreakingSuggestion.cs` ✅
- Test namespace mirrors Core: `tests/MasDependencyMap.Core.Tests/CycleAnalysis` ✅
- File naming matches class naming exactly ✅
- Service pattern with ILogger injection ✅
- Singleton DI registration ✅

**Cross-Namespace Dependencies:**
- CycleAnalysis → DependencyAnalysis (uses DependencyEdge, ProjectNode) ✅
- CycleAnalysis → CycleAnalysis (uses CycleInfo from Story 3.1, extended in 3.4) ✅
- This is expected and acceptable (Epic 3 builds on itself)

**New Model Design:**
- `CycleBreakingSuggestion` is a `record` type (immutable, value equality)
- Implements `IComparable<T>` for natural sorting
- Separate from domain models (not extending existing types)
- Pure data transfer object for recommendation output

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-3-circular-dependency-detection-and-break-point-analysis.md, Story 3.5 (lines 79-94)]
- Story requirements: Generate recommendations from weak edges, rank by coupling score, include rationale

**Previous Stories:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-4-identify-weakest-coupling-edges-within-cycles.md (full file)]
- Weak edge identification pattern, CycleInfo extension, structured logging, test patterns
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-3-implement-coupling-strength-analysis-via-method-call-counting.md]
- Coupling analysis pattern, DependencyEdge extension
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-2-calculate-cycle-statistics-and-participation-rates.md]
- Statistics calculation, aggregate data patterns
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-1-implement-tarjans-scc-algorithm-for-cycle-detection.md]
- CycleInfo model creation, cycle detection integration

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Structured Logging (lines 115-119)]
- Named placeholders, no string interpolation
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Namespace Organization (lines 56-60)]
- Feature-based namespace organization
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Interface & Class Naming (lines 61-65)]
- I-prefix for interfaces, descriptive implementation names
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, File-Scoped Namespaces (lines 76-79)]
- Use file-scoped namespace declarations (C# 10+)

**Technology Stack:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Core Technologies (lines 20-26)]
- .NET 8.0, C# 12
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Infrastructure (lines 31-35)]
- Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging

**Testing Patterns:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Test Method Naming (lines 150-154)]
- MethodName_Scenario_ExpectedResult pattern
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Test Organization (lines 140-145)]
- Tests mirror Core namespace structure

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (full file)]
- Complete project rules and patterns for AI agents

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - Story prepared for implementation

### Completion Notes List

**Implementation Summary:**

✅ **CycleBreakingSuggestion Record Model Created** (src/MasDependencyMap.Core/CycleAnalysis/CycleBreakingSuggestion.cs)
- Implemented as sealed record type for immutability and value equality
- All required properties: CycleId, SourceProject, TargetProject, CouplingScore, CycleSize, Rationale, Rank
- Constructor validation: ArgumentNullException for null projects, ArgumentException for empty rationale
- Implemented IComparable<CycleBreakingSuggestion> with three-level sorting:
  1. Primary: Coupling score (lowest first - easier to break)
  2. Secondary: Cycle size (largest first - higher impact)
  3. Tertiary: Project name (alphabetical - deterministic)
- Comprehensive XML documentation for all properties

✅ **IRecommendationGenerator Interface Created** (src/MasDependencyMap.Core/CycleAnalysis/IRecommendationGenerator.cs)
- Single method: GenerateRecommendationsAsync accepting cycles and cancellation token
- Returns IReadOnlyList<CycleBreakingSuggestion> ranked by coupling score
- Complete XML documentation describing contract and return value ordering

✅ **RecommendationGenerator Service Implemented** (src/MasDependencyMap.Core/CycleAnalysis/RecommendationGenerator.cs)
- Constructor injection of ILogger<RecommendationGenerator>
- Argument validation: ArgumentNullException.ThrowIfNull(cycles)
- Iterates through all cycles with weak edges populated
- Generates CycleBreakingSuggestion for each weak edge in each cycle
- Context-aware rationale generation via GenerateRationale() private method
- Multi-level sorting using LINQ: .OrderBy(coupling), .ThenByDescending(size), .ThenBy(name)
- Rank assignment after sorting (1-based: Rank = i + 1)
- Structured logging with named placeholders (Information and Debug levels)
- Summary log includes total recommendation count and top recommendation details

✅ **Rationale Generation Logic Implemented**
- Switch expression for impact context based on cycle size:
  - >= 10: "critical X-project cycle"
  - >= 6: "large X-project cycle"
  - >= 4: "X-project cycle"
  - < 4: "small X-project cycle"
- Switch expression for coupling description:
  - 1: "only 1 method call" (singular)
  - 2: "just 2 method calls"
  - <= 5: "only X method calls"
  - > 5: "X method calls"
- Template: "Weakest link in {impact}, {coupling}"
- Architect-friendly, actionable language emphasizing ease of breaking

✅ **Edge Case Handling**
- Empty cycle list → returns empty recommendations
- Cycles with null or empty WeakCouplingEdges → skipped with Debug log
- Null cycles argument → throws ArgumentNullException
- Cancellation token checked in foreach loop → throws OperationCanceledException
- Tied coupling scores → secondary sort by cycle size (deterministic ordering)

✅ **DI Registration** (src/MasDependencyMap.CLI/Program.cs:145)
- Registered IRecommendationGenerator → RecommendationGenerator as singleton
- Used services.TryAddSingleton() for test override support
- Positioned after IWeakEdgeIdentifier (correct dependency order)

✅ **Comprehensive Test Suite Created** (tests/MasDependencyMap.Core.Tests/CycleAnalysis/RecommendationGeneratorTests.cs)
- 11 unit tests covering all scenarios (243 total tests in suite, all passing)
- Test coverage:
  1. Empty cycle list → empty recommendations
  2. Single cycle one weak edge → one recommendation with rank 1
  3. Multiple weak edges → all generate recommendations
  4. Recommendations ranked by coupling (lowest first)
  5. Tied coupling scores → secondary sort by cycle size
  6. Rationale format matches template
  7. Coupling score = 1 → singular "1 method call"
  8. Large cycle (12 projects) → rationale emphasizes impact (critical/large)
  9. Null cycles → throws ArgumentNullException
  10. Cancellation token → throws OperationCanceledException
  11. Top 5 extraction → correct subset with proper ranking
- Helper methods: CreateCycleWithWeakEdges, CreateManyCycles
- Uses object initializer syntax for ProjectNode and DependencyEdge (required properties)

✅ **Acceptance Criteria Validated:**
- ✅ CycleBreakingSuggestion objects created for each weak edge
- ✅ Suggestions include: source project, target project, coupling score, rationale
- ✅ Rationale explains why edge is recommended (context-aware messaging)
- ✅ Suggestions ranked by coupling score (lowest first)
- ✅ Top 5 recommendations can be extracted via .Take(5)

**Technical Decisions:**
- Used async method signature for GenerateRecommendationsAsync for consistency with interface pattern
- Returned Task.FromResult for synchronous implementation (no I/O operations)
- Chose sealed record for CycleBreakingSuggestion (immutability, value equality)
- Used multi-level LINQ sorting for efficient ranking algorithm
- Generated rationale on-demand (not cached) for memory efficiency

**Test Results:**
- All 243 tests passing (11 new tests + 232 existing tests)
- Build successful with 0 warnings, 0 errors
- Full regression suite passed

### Code Review Fixes Applied

**Code Review Date:** 2026-01-24
**Reviewer:** Claude Sonnet 4.5 (Adversarial Code Review Agent)
**Issues Found:** 7 (0 High, 4 Medium, 3 Low)
**Issues Fixed:** 7 (all issues resolved)

**MEDIUM Issues Fixed:**
1. ✅ Removed unnecessary async/await - Changed to Task.FromResult without await keyword (RecommendationGenerator.cs:96)
2. ✅ Fixed log levels - Changed LogInformation to LogDebug per project-context.md (lines 27, 81)
3. ✅ Optimized record mutation - Used LINQ Select to assign Rank during sorting chain (lines 68-74)
4. ✅ Enhanced XML documentation - Added comprehensive param/exception docs (IRecommendationGenerator.cs:9-20)

**LOW Issues Fixed:**
5. ✅ Removed null-forgiving operator - Used pragma suppress in test (RecommendationGeneratorTests.cs:164)
6. ✅ Added edge case tests - Zero coupling score, large dataset performance (RecommendationGeneratorTests.cs:201-225)
7. ✅ Added CompareTo null comment - Clarified why null returns 1 (CycleBreakingSuggestion.cs:79)

**Post-Review Test Results:**
- All 245 tests passing (243 original + 2 new edge case tests)
- Build successful with 0 warnings, 0 errors
- Performance test validates <1 second for 1000 recommendations

### File List

**New Files Created:**
- src/MasDependencyMap.Core/CycleAnalysis/CycleBreakingSuggestion.cs
- src/MasDependencyMap.Core/CycleAnalysis/IRecommendationGenerator.cs
- src/MasDependencyMap.Core/CycleAnalysis/RecommendationGenerator.cs
- tests/MasDependencyMap.Core.Tests/CycleAnalysis/RecommendationGeneratorTests.cs

**Modified Files:**
- src/MasDependencyMap.CLI/Program.cs (line 145: DI registration)
- _bmad-output/implementation-artifacts/sprint-status.yaml (status updates)
- _bmad-output/implementation-artifacts/3-5-generate-ranked-cycle-breaking-recommendations.md (task checkboxes, completion notes, code review fixes)
