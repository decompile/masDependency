# Story 3.4: Identify Weakest Coupling Edges Within Cycles

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want to identify the weakest coupling edges within circular dependencies,
So that I know which dependencies are easiest to break.

## Acceptance Criteria

**Given** Cycles have been detected and coupling analysis is complete
**When** Weak coupling edges within cycles are identified
**Then** For each cycle, the edge with the lowest coupling score is flagged
**And** Multiple weak edges (tied for lowest score) are all flagged
**And** Weak edges are stored in CycleInfo objects with their coupling scores
**And** ILogger logs "Identified 18 weak coupling edges across 12 cycles (avg 1.5 per cycle)"

## Tasks / Subtasks

- [x] Extend CycleInfo model to store weak coupling edges (AC: Store weak edges)
  - [x] Add WeakCouplingEdges property (IReadOnlyList<DependencyEdge>)
  - [x] Add WeakCouplingScore property (int, the lowest score found in cycle)
  - [x] Update CycleInfo constructor to initialize collections
  - [x] Ensure backward compatibility with existing cycle detection code

- [x] Create IWeakEdgeIdentifier interface (AC: Service contract)
  - [x] Create IWeakEdgeIdentifier interface in Core.CycleAnalysis namespace
  - [x] Define IdentifyWeakEdgesAsync method accepting cycles and annotated graph
  - [x] Return updated cycles with weak edges populated
  - [x] Include CancellationToken parameter for long-running operations

- [x] Implement WeakEdgeIdentifier service (AC: Identify weakest edges in cycles)
  - [x] Create WeakEdgeIdentifier class in Core.CycleAnalysis namespace
  - [x] Inject ILogger<WeakEdgeIdentifier> via constructor
  - [x] For each cycle, find all edges within the cycle
  - [x] Identify minimum coupling score among cycle edges
  - [x] Flag ALL edges with that minimum score (handle ties)
  - [x] Populate CycleInfo.WeakCouplingEdges with flagged edges
  - [x] Handle edge cases: cycles with no edges, equal coupling scores

- [x] Implement cycle edge extraction logic (AC: Find edges within cycle)
  - [x] Given a cycle (list of ProjectNode vertices)
  - [x] Find all DependencyEdge objects connecting cycle members
  - [x] Use graph.Edges.Where() to filter edges within cycle
  - [x] Handle bidirectional edges if present
  - [x] Exclude edges outside the cycle (important for accuracy)

- [x] Implement minimum score identification (AC: Find lowest coupling)
  - [x] Extract CouplingScore from all edges in cycle
  - [x] Use LINQ .Min() to find minimum score
  - [x] Handle empty collections gracefully
  - [x] Return minimum score for logging and storage

- [x] Implement tie handling for multiple weak edges (AC: Flag all tied edges)
  - [x] Find all edges where CouplingScore equals minimum
  - [x] Store ALL matching edges in WeakCouplingEdges list
  - [x] Do not arbitrarily pick one - architects need all options
  - [x] Log count of tied edges per cycle at Debug level

- [x] Add structured logging for analysis results (AC: Observability)
  - [x] Log "Analyzing {CycleCount} cycles for weak coupling edges" at Information level
  - [x] Log "Cycle {CycleIndex}: {EdgeCount} edges, min coupling = {MinScore}" at Debug level
  - [x] Log "Identified {WeakEdgeCount} weak coupling edges across {CycleCount} cycles (avg {AvgPerCycle} per cycle)" at Information level
  - [x] Use named placeholders, NOT string interpolation
  - [x] Calculate average weak edges per cycle for summary log

- [x] Handle edge cases and validation (AC: Robustness)
  - [x] Handle cycles with zero edges (should not happen, but validate)
  - [x] Handle cycles where all edges have same coupling score (all flagged as weak)
  - [x] Rely on DependencyEdge default CouplingScore of 1 (no explicit validation needed)

- [x] Register service in DI container (AC: Dependency injection)
  - [x] Register IWeakEdgeIdentifier → WeakEdgeIdentifier as singleton
  - [x] Use services.TryAddSingleton() for test override support
  - [x] Ensure registration after ICouplingAnalyzer (dependency order)

- [x] Create comprehensive tests (AC: Algorithm correctness)
  - [x] Unit test: Empty cycle list → returns empty result
  - [x] Unit test: Single cycle with one weak edge → edge flagged
  - [x] Unit test: Cycle with multiple edges, one minimum → correct edge flagged
  - [x] Unit test: Cycle with tied minimum scores → all tied edges flagged
  - [x] Unit test: Multiple cycles → each cycle's weak edges identified
  - [x] Unit test: Cycle with all equal scores → all edges flagged
  - [x] Unit test: Null cycles → throws ArgumentNullException
  - [x] Unit test: Null graph → throws ArgumentNullException
  - [x] Unit test: Cancellation token support
  - [x] Unit test: Cycle with no edges → gracefully skipped (WeakCouplingScore remains null)

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - CycleInfo Extension Pattern:**

**From Story 3-1 (Tarjan's SCC Algorithm):**

`CycleInfo` was created in Story 3.1 to represent circular dependency cycles. Story 3.4 MUST extend this class to include weak coupling edge information:

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Represents a circular dependency cycle with weak coupling edge analysis.
/// </summary>
public sealed class CycleInfo
{
    /// <summary>
    /// Unique identifier for this cycle (1-based sequential).
    /// </summary>
    public int CycleId { get; init; }

    /// <summary>
    /// Projects involved in the circular dependency.
    /// </summary>
    public IReadOnlyList<ProjectNode> Projects { get; init; }

    /// <summary>
    /// Total number of projects in this cycle.
    /// </summary>
    public int CycleSize => Projects.Count;

    /// <summary>
    /// Weakest coupling edges within this cycle (lowest coupling score).
    /// Multiple edges if tied for lowest score.
    /// Populated by WeakEdgeIdentifier service (Story 3.4).
    /// </summary>
    public IReadOnlyList<DependencyEdge> WeakCouplingEdges { get; set; }

    /// <summary>
    /// Minimum coupling score found within this cycle.
    /// Represents the strength of the weakest dependency link.
    /// Null until weak edge analysis is performed.
    /// </summary>
    public int? WeakCouplingScore { get; set; }

    public CycleInfo(int cycleId, IReadOnlyList<ProjectNode> projects)
    {
        CycleId = cycleId;
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));
        WeakCouplingEdges = Array.Empty<DependencyEdge>(); // Initialize to empty, populated later
    }

    // Existing methods from Story 3.1...
}
```

**Why Mutable Properties:**
- CycleInfo created in Story 3.1 (cycle detection phase)
- Coupling analysis happens in Story 3.3 (after cycles detected)
- Weak edge identification happens in Story 3.4 (after coupling analysis)
- Mutating existing CycleInfo objects is more efficient than rebuilding

🚨 **CRITICAL - Algorithm for Finding Edges Within Cycle:**

**Cycle Representation:**
- CycleInfo contains a list of ProjectNode objects (vertices in the cycle)
- Need to find all DependencyEdge objects connecting these vertices

**Algorithm:**
```csharp
private IReadOnlyList<DependencyEdge> GetEdgesInCycle(
    CycleInfo cycle,
    AdjacencyGraph<ProjectNode, DependencyEdge> graph)
{
    // Create HashSet of cycle members for O(1) lookup
    var cycleMembers = new HashSet<ProjectNode>(cycle.Projects);

    // Find all edges where BOTH source and target are in the cycle
    var edgesInCycle = graph.Edges
        .Where(edge => cycleMembers.Contains(edge.Source) &&
                       cycleMembers.Contains(edge.Target))
        .ToList();

    return edgesInCycle;
}
```

**Why This Works:**
- Cycles are **strongly connected components** - all vertices are mutually reachable
- An edge is "in the cycle" if both endpoints are cycle members
- Using HashSet for O(1) membership checking (performance optimization)
- LINQ .Where() efficiently filters graph.Edges collection

**Edge Case: No Edges Found**
- Should not happen for valid strongly connected components
- If it does, log warning and skip cycle (defensive programming)

🚨 **CRITICAL - Handling Ties in Minimum Coupling:**

**From Epic 3 Story 3.4 Acceptance Criteria:**
"Multiple weak edges (tied for lowest score) are all flagged"

**Tie Handling Pattern:**
```csharp
public IReadOnlyList<CycleInfo> IdentifyWeakEdges(
    IReadOnlyList<CycleInfo> cycles,
    AdjacencyGraph<ProjectNode, DependencyEdge> graph,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(cycles);
    ArgumentNullException.ThrowIfNull(graph);

    _logger.LogInformation("Analyzing {CycleCount} cycles for weak coupling edges", cycles.Count);

    foreach (var cycle in cycles)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var edgesInCycle = GetEdgesInCycle(cycle, graph);

        if (edgesInCycle.Count == 0)
        {
            _logger.LogWarning(
                "Cycle with {ProjectCount} projects has no edges - skipping weak edge analysis",
                cycle.CycleSize);
            continue;
        }

        // Find minimum coupling score
        var minCouplingScore = edgesInCycle.Min(e => e.CouplingScore);

        // Find ALL edges with that minimum score (handles ties)
        var weakEdges = edgesInCycle
            .Where(e => e.CouplingScore == minCouplingScore)
            .ToList();

        // Populate CycleInfo
        cycle.WeakCouplingEdges = weakEdges;
        cycle.WeakCouplingScore = minCouplingScore;

        _logger.LogDebug(
            "Cycle {CycleIndex}: {EdgeCount} edges, min coupling = {MinScore}, {WeakEdgeCount} weak edges flagged",
            cycles.IndexOf(cycle) + 1,
            edgesInCycle.Count,
            minCouplingScore,
            weakEdges.Count);
    }

    // Summary logging
    var totalWeakEdges = cycles.Sum(c => c.WeakCouplingEdges.Count);
    var avgWeakEdgesPerCycle = cycles.Count > 0 ? (double)totalWeakEdges / cycles.Count : 0;

    _logger.LogInformation(
        "Identified {WeakEdgeCount} weak coupling edges across {CycleCount} cycles (avg {AvgPerCycle:F1} per cycle)",
        totalWeakEdges,
        cycles.Count,
        avgWeakEdgesPerCycle);

    return cycles;
}
```

**Key Points:**
- Uses `.Where(e => e.CouplingScore == minCouplingScore)` to find ALL ties
- Does not use `.First()` or arbitrary selection
- Architects need visibility into all weak options for informed decisions

🚨 **CRITICAL - Dependency on Story 3.3 Coupling Data:**

**From Story 3-3 Implementation:**

Story 3.4 depends on Story 3.3 completing successfully:
- DependencyEdge MUST have CouplingScore property populated
- CouplingScore defaults to 1 if Roslyn unavailable (fallback)
- Story 3.4 assumes all edges have valid coupling scores (minimum of 1)
- No explicit validation needed since DependencyEdge.CouplingScore defaults to 1

**Integration Order in CLI:**
```csharp
// In Program.cs or AnalyzeCommand
var solution = await solutionLoader.LoadAsync(solutionPath);           // Epic 2
var graph = await graphBuilder.BuildAsync(solution);                   // Epic 2
var cycles = await cycleDetector.DetectCyclesAsync(graph);             // Story 3.1
var annotatedGraph = await couplingAnalyzer.AnalyzeAsync(graph, solution); // Story 3.3
var cyclesWithWeakEdges = await weakEdgeIdentifier.IdentifyWeakEdgesAsync(cycles, annotatedGraph); // Story 3.4 (NEW)
```

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
    "Analyzing {CycleCount} cycles for weak coupling edges",
    cycles.Count);

_logger.LogDebug(
    "Cycle {CycleIndex}: {EdgeCount} edges, min coupling = {MinScore}",
    cycleIndex,
    edgesInCycle.Count,
    minCouplingScore);

_logger.LogInformation(
    "Identified {WeakEdgeCount} weak coupling edges across {CycleCount} cycles (avg {AvgPerCycle:F1} per cycle)",
    totalWeakEdges,
    cycles.Count,
    avgWeakEdgesPerCycle);

// ❌ WRONG: String interpolation
_logger.LogInformation($"Analyzing {cycles.Count} cycles"); // DO NOT USE
```

**Formatting Specifiers:**
- `{AvgPerCycle:F1}` formats average to 1 decimal place
- Follows structured logging best practices for searchability

### Technical Requirements

**Algorithm Complexity:**

**Time Complexity:**
- Per cycle: O(e) where e = number of edges in graph (filtering)
- Per solution: O(c * e) where c = cycles, e = edges
- Finding minimum: O(edges_in_cycle)
- Overall: O(c * e) worst case, acceptable for typical solutions

**Space Complexity:**
- O(c * w) where c = cycles, w = weak edges per cycle
- Typically w is small (1-3 edges per cycle)
- Total memory footprint minimal

**Optimization Strategies:**
1. Use HashSet for cycle member lookup (O(1) instead of O(n))
2. Reuse cycle member HashSet across multiple lookups if needed
3. Early exit if edgesInCycle is empty
4. Process cycles sequentially (no parallel processing needed - fast operation)

**Edge Cases to Handle:**

1. **Empty Cycle List:**
   - Input: cycles.Count == 0
   - Output: Return empty list, log "No cycles to analyze"

2. **Cycle with No Edges:**
   - Should not happen in valid SCC
   - Log warning and skip cycle
   - Don't throw exception (graceful degradation)

3. **All Edges Have Same Coupling Score:**
   - Input: Cycle where all edges have CouplingScore = 5
   - Output: ALL edges flagged as weak
   - This is correct behavior - architects see all options

4. **Single Edge in Cycle:**
   - Input: 2-project cycle with 1 edge
   - Output: That edge is flagged as weak (minimum by default)

5. **Bidirectional Edges:**
   - Input: Project A → B and B → A both in cycle
   - Output: Both edges analyzed, both could be weak if tied

6. **Large Cycles:**
   - Input: 50-project cycle with hundreds of edges
   - Output: Algorithm still O(e), performance acceptable
   - Cancellation token allows user to abort if needed

### Architecture Compliance

**Epic 3 Architecture Requirements:**

```
- TarjanCycleDetector using QuikGraph's Tarjan's SCC algorithm ✅ (Story 3.1)
- Cycle statistics calculation ✅ (Story 3.2)
- CouplingAnalyzer for method call counting ✅ (Story 3.3)
- WeakEdgeIdentifier for finding weakest edges ⏳ (Story 3.4 - THIS STORY)
- Ranked cycle-breaking recommendations ⏳ (Story 3.5)
- Enhanced DOT visualization with cycle highlighting ⏳ (Stories 3.6, 3.7)
```

**Story 3.4 Implements:**
- ✅ IWeakEdgeIdentifier service for weak edge identification
- ✅ WeakEdgeIdentifier implementation with tie handling
- ✅ CycleInfo extension with weak edge storage
- ✅ Structured logging for analysis results
- ✅ Edge extraction algorithm for cycles
- ✅ Integration with Story 3.3 coupling data

**Integration with Existing Components:**

Story 3.4 consumes:
- **CycleInfo** (from Story 3.1): Extended with weak edge properties
- **DependencyGraph** (from Story 2-5): Graph to query for edges
- **DependencyEdge** (from Story 2-5): Edges with coupling scores (from Story 3.3)
- **ProjectNode** (from Story 2-5): Vertices in cycles
- **ILogger<T>** (from Story 1-6): Structured logging

Story 3.4 produces:
- **CycleInfo with WeakCouplingEdges**: Consumed by Story 3.5 (recommendations), Stories 3.6-3.7 (visualization)
- **Weak edge data**: Used in reporting (Epic 5) and visualization

**Namespace Organization:**

```
src/MasDependencyMap.Core/
├── CycleAnalysis/                        # Epic 3 namespace
│   ├── ITarjanCycleDetector.cs          # Story 3.1
│   ├── TarjanCycleDetector.cs           # Story 3.1
│   ├── CycleInfo.cs                     # Story 3.1 - MODIFIED: Add weak edge properties
│   ├── ICycleStatisticsCalculator.cs    # Story 3.2
│   ├── CycleStatisticsCalculator.cs     # Story 3.2
│   ├── CycleStatistics.cs               # Story 3.2
│   ├── ICouplingAnalyzer.cs             # Story 3.3
│   ├── RoslynCouplingAnalyzer.cs        # Story 3.3
│   ├── MethodCallCounterWalker.cs       # Story 3.3
│   ├── CouplingStrength.cs              # Story 3.3
│   ├── IWeakEdgeIdentifier.cs           # NEW: Story 3.4
│   └── WeakEdgeIdentifier.cs            # NEW: Story 3.4
└── DependencyAnalysis/                   # Epic 2 namespace
    ├── DependencyEdge.cs                 # Reused (with coupling from 3.3)
    └── ProjectNode.cs                    # Reused as-is
```

**DI Integration:**
```csharp
// Existing (from Epic 2 and Stories 3.1-3.3)
services.TryAddSingleton<ITarjanCycleDetector, TarjanCycleDetector>();
services.TryAddSingleton<ICycleStatisticsCalculator, CycleStatisticsCalculator>();
services.TryAddSingleton<ICouplingAnalyzer, RoslynCouplingAnalyzer>();

// NEW: Story 3.4
services.TryAddSingleton<IWeakEdgeIdentifier, WeakEdgeIdentifier>();
```

### Library/Framework Requirements

**No New NuGet Packages Required:**

All dependencies already satisfied:
- ✅ QuikGraph v2.5.0 (installed in Epic 2) - AdjacencyGraph, vertices, edges
- ✅ Microsoft.Extensions.Logging.Abstractions (installed in Story 1-6)
- ✅ Microsoft.Extensions.DependencyInjection (installed in Story 1-5)

**QuikGraph APIs Used:**
- `AdjacencyGraph<ProjectNode, DependencyEdge>` - Graph structure
- `graph.Edges` - Enumerable of all edges in graph
- `IEdge<T>.Source` and `IEdge<T>.Target` - Edge endpoints

**LINQ APIs Used:**
- `.Where()` - Filtering edges in cycle
- `.Min()` - Finding minimum coupling score
- `.Sum()` - Calculating total weak edges
- `.ToList()` - Materializing collections

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/
└── CycleAnalysis/
    ├── IWeakEdgeIdentifier.cs              # NEW: Weak edge identifier interface
    └── WeakEdgeIdentifier.cs               # NEW: Weak edge identifier implementation

tests/MasDependencyMap.Core.Tests/
└── CycleAnalysis/
    └── WeakEdgeIdentifierTests.cs          # NEW: Comprehensive test suite
```

**Files to Modify:**

```
src/MasDependencyMap.Core/CycleAnalysis/CycleInfo.cs
  - Add WeakCouplingEdges property (IReadOnlyList<DependencyEdge>)
  - Add WeakCouplingScore property (int)
  - Update XML documentation

src/MasDependencyMap.CLI/Program.cs
  - Register IWeakEdgeIdentifier in DI container
  - Integrate weak edge identification into CLI workflow (after coupling analysis)
```

### Testing Requirements

**Test Class Structure:**

```csharp
namespace MasDependencyMap.Core.Tests.CycleAnalysis;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using QuikGraph;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;

public class WeakEdgeIdentifierTests
{
    private readonly ILogger<WeakEdgeIdentifier> _logger;
    private readonly WeakEdgeIdentifier _identifier;

    public WeakEdgeIdentifierTests()
    {
        _logger = NullLogger<WeakEdgeIdentifier>.Instance;
        _identifier = new WeakEdgeIdentifier(_logger);
    }

    [Fact]
    public void IdentifyWeakEdges_EmptyCycleList_ReturnsEmptyResult()
    {
        // Arrange
        var cycles = new List<CycleInfo>();
        var graph = CreateEmptyGraph();

        // Act
        var result = _identifier.IdentifyWeakEdges(cycles, graph);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void IdentifyWeakEdges_SingleCycleOneWeakEdge_EdgeFlagged()
    {
        // Arrange
        var (cycle, graph) = CreateCycleWithEdges(
            edgeCouplingScores: new[] { 5, 10, 15 }); // 5 is minimum

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle }, graph);

        // Assert
        var updatedCycle = result.Single();
        updatedCycle.WeakCouplingEdges.Should().HaveCount(1);
        updatedCycle.WeakCouplingEdges[0].CouplingScore.Should().Be(5);
        updatedCycle.WeakCouplingScore.Should().Be(5);
    }

    [Fact]
    public void IdentifyWeakEdges_TiedMinimumScores_AllTiedEdgesFlagged()
    {
        // Arrange
        var (cycle, graph) = CreateCycleWithEdges(
            edgeCouplingScores: new[] { 3, 10, 3, 15, 3 }); // Three edges tied at 3

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle }, graph);

        // Assert
        var updatedCycle = result.Single();
        updatedCycle.WeakCouplingEdges.Should().HaveCount(3);
        updatedCycle.WeakCouplingEdges.Should().OnlyContain(e => e.CouplingScore == 3);
        updatedCycle.WeakCouplingScore.Should().Be(3);
    }

    [Fact]
    public void IdentifyWeakEdges_AllEdgesEqualScore_AllEdgesFlagged()
    {
        // Arrange
        var (cycle, graph) = CreateCycleWithEdges(
            edgeCouplingScores: new[] { 7, 7, 7, 7 }); // All equal

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle }, graph);

        // Assert
        var updatedCycle = result.Single();
        updatedCycle.WeakCouplingEdges.Should().HaveCount(4);
        updatedCycle.WeakCouplingScore.Should().Be(7);
    }

    [Fact]
    public void IdentifyWeakEdges_MultipleCycles_EachCycleAnalyzed()
    {
        // Arrange
        var cycle1 = CreateCycleWithEdges(new[] { 2, 5, 8 }).cycle;
        var cycle2 = CreateCycleWithEdges(new[] { 10, 12, 10 }).cycle;
        var graph = MergeGraphs(cycle1, cycle2);

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle1, cycle2 }, graph);

        // Assert
        result.Should().HaveCount(2);
        result[0].WeakCouplingScore.Should().Be(2); // Min from first cycle
        result[1].WeakCouplingScore.Should().Be(10); // Min from second cycle (tied)
        result[1].WeakCouplingEdges.Should().HaveCount(2); // Two edges tied at 10
    }

    [Fact]
    public void IdentifyWeakEdges_NullCycles_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = CreateEmptyGraph();

        // Act & Assert
        var act = () => _identifier.IdentifyWeakEdges(null, graph);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IdentifyWeakEdges_NullGraph_ThrowsArgumentNullException()
    {
        // Arrange
        var cycles = new List<CycleInfo>();

        // Act & Assert
        var act = () => _identifier.IdentifyWeakEdges(cycles, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IdentifyWeakEdges_CancellationTokenSupport_ThrowsOperationCanceledException()
    {
        // Arrange
        var largeCycleSet = CreateManyCycles(100);
        var graph = CreateLargeGraph();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var act = () => _identifier.IdentifyWeakEdges(largeCycleSet, graph, cts.Token);
        act.Should().Throw<OperationCanceledException>();
    }

    // Helper methods for creating test data
    private AdjacencyGraph<ProjectNode, DependencyEdge> CreateEmptyGraph() { /* ... */ }
    private (CycleInfo cycle, AdjacencyGraph<ProjectNode, DependencyEdge> graph) CreateCycleWithEdges(int[] edgeCouplingScores) { /* ... */ }
    private AdjacencyGraph<ProjectNode, DependencyEdge> MergeGraphs(params CycleInfo[] cycles) { /* ... */ }
}
```

**Test Naming Convention:**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `IdentifyWeakEdges_EmptyCycleList_ReturnsEmptyResult()`
- ✅ `IdentifyWeakEdges_TiedMinimumScores_AllTiedEdgesFlagged()`
- ✅ `IdentifyWeakEdges_MultipleCycles_EachCycleAnalyzed()`

### Previous Story Intelligence

**From Story 3-3 (Coupling Strength Analysis) - Key Learnings:**

Story 3-3 established the coupling analysis pattern:
1. Extend existing models (DependencyEdge) with new properties
2. Create service to populate those properties
3. Use structured logging with named placeholders
4. Handle edge cases and nulls gracefully
5. Comprehensive unit tests (8+ tests)
6. Register as singleton in DI

**Patterns to Reuse:**
```csharp
// Argument validation
ArgumentNullException.ThrowIfNull(cycles);
ArgumentNullException.ThrowIfNull(graph);

// Structured logging with named placeholders
_logger.LogInformation(
    "Analyzing {CycleCount} cycles for weak coupling edges",
    cycles.Count);

// Cancellation token support
cancellationToken.ThrowIfCancellationRequested();

// DI registration
services.TryAddSingleton<IWeakEdgeIdentifier, WeakEdgeIdentifier>();
```

**From Story 3-2 (Cycle Statistics) - Integration Pattern:**

Story 3-2 showed how to work with CycleInfo collections:
- Iterate through cycles collection
- Calculate aggregate statistics
- Log summary information
- Return enriched cycle data

**From Story 3-1 (Tarjan's SCC) - CycleInfo Pattern:**

Story 3-1 created CycleInfo with ProjectsInCycle list. Story 3.4 extends this:
- Add new properties to existing domain models
- Keep properties mutable for post-processing
- Initialize collections to empty (not null)
- Use IReadOnlyList for immutable external access

Expected integration in CLI:
```csharp
// In Program.cs or AnalyzeCommand
var solution = await solutionLoader.LoadAsync(solutionPath);                     // Epic 2
var graph = await graphBuilder.BuildAsync(solution);                             // Epic 2
var cycles = await cycleDetector.DetectCyclesAsync(graph);                       // Story 3.1
var statistics = await statsCalculator.CalculateAsync(cycles, graph);            // Story 3.2
var annotatedGraph = await couplingAnalyzer.AnalyzeAsync(graph, solution);       // Story 3.3
var cyclesWithWeakEdges = weakEdgeIdentifier.IdentifyWeakEdges(cycles, annotatedGraph); // Story 3.4 (NEW)
var suggestions = await suggestionGenerator.GenerateAsync(cyclesWithWeakEdges);  // Story 3.5 (future)
```

### Git Intelligence Summary

**Recent Commits Analysis:**

Last 5 commits show pattern for Epic 3 stories:
1. **f934770:** Code review fixes for Story 3-2
2. **005b452:** Code review fixes for Story 3-3
3. **b0a32ee:** Story 3-3 complete
4. **53cc115:** Story 3-2 complete
5. **741e6be:** Code review fixes for Story 3-1

**Commit Message Pattern:**

Story completion commits include:
- Story number and description
- List of new interfaces/classes created
- Key technical details
- Acceptance criteria satisfied
- Co-Authored-By: Claude Sonnet 4.5

**Expected Commit Message for Story 3.4:**
```bash
git commit -m "Story 3-4 complete: Identify weakest coupling edges within cycles

- Created IWeakEdgeIdentifier interface in Core.CycleAnalysis namespace
- Implemented WeakEdgeIdentifier service with tie handling
- Extended CycleInfo with WeakCouplingEdges and WeakCouplingScore properties
- Algorithm finds all edges within cycle using HashSet for O(1) member lookup
- Identifies minimum coupling score and flags ALL tied edges (no arbitrary selection)
- Handles edge cases: empty cycles, all equal scores, single edge
- Structured logging for analysis progress and summary statistics
- Registered IWeakEdgeIdentifier as singleton in DI container
- Created comprehensive unit tests (8+ tests) - all passing
- Validates coupling data exists before analysis
- CancellationToken support for long-running operations
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 3.4 continues Epic 3 namespace organization:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
│   ├── DependencyEdge.cs            # Reused (with coupling from 3.3)
│   └── ProjectNode.cs               # Reused as-is
├── CycleAnalysis/               # Epic 3: Cycle detection, statistics, coupling, weak edges
│   ├── ITarjanCycleDetector.cs      # Story 3.1
│   ├── TarjanCycleDetector.cs       # Story 3.1
│   ├── CycleInfo.cs                 # Story 3.1 - MODIFIED: Add weak edge properties
│   ├── ICycleStatisticsCalculator.cs # Story 3.2
│   ├── CycleStatisticsCalculator.cs  # Story 3.2
│   ├── CycleStatistics.cs           # Story 3.2
│   ├── ICouplingAnalyzer.cs         # Story 3.3
│   ├── RoslynCouplingAnalyzer.cs    # Story 3.3
│   ├── MethodCallCounterWalker.cs   # Story 3.3
│   ├── CouplingStrength.cs          # Story 3.3
│   ├── IWeakEdgeIdentifier.cs       # Story 3.4 (NEW)
│   └── WeakEdgeIdentifier.cs        # Story 3.4 (NEW)
└── SolutionLoading/             # Epic 2: Solution loading
```

**Consistency with Existing Patterns:**
- Feature-based namespace: `MasDependencyMap.Core.CycleAnalysis` ✅
- Interface + Implementation pattern: `IWeakEdgeIdentifier`, `WeakEdgeIdentifier` ✅
- Test namespace mirrors Core: `tests/MasDependencyMap.Core.Tests/CycleAnalysis` ✅
- File naming matches class naming exactly ✅
- Service pattern with ILogger injection ✅
- Singleton DI registration ✅

**Cross-Namespace Dependencies:**
- CycleAnalysis → DependencyAnalysis (uses DependencyEdge, ProjectNode)
- CycleAnalysis → CycleAnalysis (extends CycleInfo from Story 3.1)
- This is expected and acceptable (Epic 3 builds on itself)

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-3-circular-dependency-detection-and-break-point-analysis.md, Story 3.4 (lines 64-78)]
- Story requirements: Identify weakest coupling edges, handle ties, store in CycleInfo, log summary

**Previous Stories:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-3-implement-coupling-strength-analysis-via-method-call-counting.md (full file)]
- Coupling analysis pattern, DependencyEdge extension, structured logging
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-1-implement-tarjans-scc-algorithm-for-cycle-detection.md]
- CycleInfo model creation, cycle detection integration
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-2-calculate-cycle-statistics-and-participation-rates.md]
- Working with cycle collections, aggregate statistics

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Structured Logging (lines 115-119)]
- Named placeholders, no string interpolation
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Namespace Organization (lines 56-60)]
- Feature-based namespace organization
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Interface & Class Naming (lines 61-65)]
- I-prefix for interfaces, descriptive implementation names
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\implementation-patterns-consistency-rules.md, Test Method Naming (lines 99-108)]
- MethodName_Scenario_ExpectedResult pattern

**Technology Stack:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Core Technologies (lines 20-26)]
- .NET 8.0, C# 12, QuikGraph v2.5.0
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md, Dependency Injection (lines 157-181)]
- Full DI throughout Core and CLI layers

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - Clean implementation with no debugging required

### Completion Notes List

**Implementation Summary:**

✅ **CycleInfo Model Extended** (src/MasDependencyMap.Core/CycleAnalysis/CycleInfo.cs)
- Changed from `record` to `sealed class` for proper mutability semantics
- Added WeakCouplingEdges property (IReadOnlyList<DependencyEdge>) initialized to empty array
- Added WeakCouplingScore property (int?) defaulting to null (distinguishes "not analyzed" from "zero score")
- Properties are mutable to allow post-processing after cycle detection
- Maintains backward compatibility with existing code

✅ **IWeakEdgeIdentifier Interface Created** (src/MasDependencyMap.Core/CycleAnalysis/IWeakEdgeIdentifier.cs)
- Defines IdentifyWeakEdges method signature
- Accepts cycles, annotated graph, and cancellation token
- Returns updated cycles with weak edge data populated
- Comprehensive XML documentation

✅ **WeakEdgeIdentifier Service Implemented** (src/MasDependencyMap.Core/CycleAnalysis/WeakEdgeIdentifier.cs)
- Core algorithm: GetEdgesInCycle() uses HashSet for O(1) cycle member lookup
- Finds all edges where both source and target are in the cycle
- Identifies minimum coupling score using LINQ .Min()
- Flags ALL edges tied for minimum score (no arbitrary selection)
- Populates CycleInfo.WeakCouplingEdges and WeakCouplingScore properties
- Gracefully handles cycles with no edges (logs warning, continues)
- Structured logging with named placeholders (Information and Debug levels)
- Summary log: "Identified X weak coupling edges across Y cycles (avg Z per cycle)"

✅ **Comprehensive Test Suite** (tests/MasDependencyMap.Core.Tests/CycleAnalysis/WeakEdgeIdentifierTests.cs)
- 9 unit tests covering all scenarios
- Test cases: empty cycles, single weak edge, multiple tied edges, all equal scores
- Edge cases: null arguments, cancellation token, cycles with no edges
- Helper methods for creating test data (CreateProjectNode, CreateEdge)
- All tests passing (232 total tests in full suite)

✅ **DI Registration** (src/MasDependencyMap.CLI/Program.cs:144)
- Registered IWeakEdgeIdentifier → WeakEdgeIdentifier as singleton
- Used TryAddSingleton() for test override support
- Positioned after ICouplingAnalyzer (correct dependency order)

**Technical Decisions:**
- Used synchronous IdentifyWeakEdges() instead of async (no I/O operations)
- Chose mutable properties on CycleInfo for efficiency (avoid rebuilding objects)
- HashSet optimization for O(1) cycle member lookup in GetEdgesInCycle()
- LINQ .Where() for tie handling ensures all weak edges are captured

**Test Results:**
- All 9 new tests passed
- Full regression suite passed (232/232 tests)
- No warnings or errors during build

**Acceptance Criteria Validated:**
✅ For each cycle, edge with lowest coupling score is flagged
✅ Multiple weak edges (tied for lowest score) are all flagged
✅ Weak edges stored in CycleInfo objects with coupling scores
✅ Structured logging: "Identified X weak coupling edges across Y cycles (avg Z per cycle)"

### File List

**New Files:**
- src/MasDependencyMap.Core/CycleAnalysis/IWeakEdgeIdentifier.cs
- src/MasDependencyMap.Core/CycleAnalysis/WeakEdgeIdentifier.cs
- tests/MasDependencyMap.Core.Tests/CycleAnalysis/WeakEdgeIdentifierTests.cs

**Modified Files:**
- src/MasDependencyMap.Core/CycleAnalysis/CycleInfo.cs (changed to sealed class, added WeakCouplingEdges and nullable WeakCouplingScore properties)
- src/MasDependencyMap.CLI/Program.cs (registered IWeakEdgeIdentifier in DI container at line 144)
- .gitignore (added output/ and samples/sample-output/ to exclude test artifacts)
