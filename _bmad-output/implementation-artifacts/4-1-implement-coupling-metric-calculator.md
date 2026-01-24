# Story 4.1: Implement Coupling Metric Calculator

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want coupling metrics calculated for each project (incoming/outgoing reference counts),
So that I can quantify how connected each project is to others.

## Acceptance Criteria

**Given** A DependencyGraph with all project dependencies
**When** CouplingMetricCalculator.CalculateAsync() is called for a project
**Then** Incoming reference count is calculated (how many projects depend on this one)
**And** Outgoing reference count is calculated (how many projects this one depends on)
**And** Total coupling score is calculated as: (incoming * 2) + outgoing (incoming weighted higher)
**And** Coupling metric is normalized to 0-100 scale for scoring algorithm
**And** ILogger logs coupling calculation progress for large solutions

## Tasks / Subtasks

- [x] Create CouplingMetric model class (AC: Store coupling metrics with normalization)
  - [x] Define CouplingMetric record with properties: ProjectName, IncomingCount, OutgoingCount, TotalScore, NormalizedScore
  - [x] Add XML documentation explaining weighting formula and normalization
  - [x] Use record type for immutability (C# 9+ pattern)
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace (new feature-based namespace)

- [x] Create ICouplingMetricCalculator interface (AC: Abstraction for DI)
  - [x] Define CalculateAsync(DependencyGraph graph, CancellationToken cancellationToken = default) method signature
  - [x] Return Task<IReadOnlyList<CouplingMetric>> for all projects in graph
  - [x] Add XML documentation with examples and exception documentation
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [x] Implement CouplingMetricCalculator class (AC: Calculate incoming/outgoing counts)
  - [x] Implement ICouplingMetricCalculator interface
  - [x] Inject ILogger<CouplingMetricCalculator> via constructor for structured logging
  - [x] Implement CalculateAsync method with graph traversal logic
  - [x] For each project (vertex) in graph, count incoming edges using graph.GetInEdges(project).Count()
  - [x] For each project (vertex) in graph, count outgoing edges using graph.GetOutEdges(project).Count()
  - [x] Calculate total score: (incoming * 2) + outgoing (incoming weighted 2x per AC)
  - [x] File-scoped namespace declaration (C# 10+ pattern)
  - [x] Async method with Async suffix per project conventions

- [x] Implement normalization to 0-100 scale (AC: Normalized metric for scoring)
  - [x] Find maximum total score across all projects in graph
  - [x] Handle edge case: max score = 0 (no dependencies) → all projects get normalized score 0
  - [x] Handle edge case: single project with dependencies → still normalize to 0-100 range
  - [x] Formula: normalizedScore = (totalScore / maxTotalScore) * 100
  - [x] Ensure normalized score is clamped to 0-100 range using Math.Clamp
  - [x] Document normalization algorithm in XML comments and code comments

- [x] Add structured logging with named placeholders (AC: Log progress for large solutions)
  - [x] Log Information: "Calculating coupling metrics for {ProjectCount} projects" at start
  - [x] Log Debug: "Project {ProjectName}: Incoming={IncomingCount}, Outgoing={OutgoingCount}, Total={TotalScore}, Normalized={NormalizedScore}" for each project
  - [x] Log Information: "Coupling calculation complete: Max total score={MaxScore}, {ProjectCount} projects analyzed" at end
  - [x] Use named placeholders, NOT string interpolation (critical project rule)
  - [x] Log level: Information for key milestones, Debug for per-project details

- [x] Register service in DI container (AC: Service integration)
  - [x] Add registration in CLI Program.cs or DI configuration
  - [x] Use services.TryAddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>() pattern
  - [x] Register in appropriate section (Epic 4: Extraction Scoring Services)
  - [x] Follow existing DI registration patterns from Epic 2 and Epic 3

- [x] Create comprehensive unit tests (AC: Test coverage)
  - [x] Create test class: tests/MasDependencyMap.Core.Tests/ExtractionScoring/CouplingMetricCalculatorTests.cs
  - [x] Test: CalculateAsync_GraphWithDependencies_CalculatesIncomingAndOutgoingCounts
  - [x] Test: CalculateAsync_GraphWithDependencies_WeightsIncomingHigherThanOutgoing (verify formula: incoming * 2 + outgoing)
  - [x] Test: CalculateAsync_GraphWithDependencies_NormalizesScoreTo100Scale
  - [x] Test: CalculateAsync_EmptyGraph_ReturnsEmptyList
  - [x] Test: CalculateAsync_SingleProjectNoDependencies_ReturnsZeroCoupling
  - [x] Test: CalculateAsync_GraphWithMaxCoupling_NormalizedScoreIs100
  - [x] Test: CalculateAsync_WeightingFormula_IncomingCountedTwiceAsHigh (additional validation test)
  - [x] Test: CalculateAsync_NullGraph_ThrowsArgumentNullException (defensive programming)
  - [x] Use xUnit, FluentAssertions pattern from project conventions
  - [x] Test naming: {MethodName}_{Scenario}_{ExpectedResult} pattern
  - [x] Arrange-Act-Assert structure

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Feature-based namespace: MasDependencyMap.Core.ExtractionScoring (NOT layer-based)
  - [x] Async suffix on all async methods (CalculateAsync)
  - [x] File-scoped namespace declarations (all files)
  - [x] ILogger injection via constructor (NOT static logger)
  - [x] Task.FromResult for synchronous async methods (no ConfigureAwait needed)
  - [x] XML documentation on all public APIs (model, interface, implementation)
  - [x] Test files mirror Core namespace structure (tests/MasDependencyMap.Core.Tests/ExtractionScoring)

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Epic 4 Foundation Story:**

This is the FIRST story in Epic 4 (Extraction Difficulty Scoring). This story creates the foundation namespace, patterns, and scoring infrastructure that Stories 4.2-4.8 will build upon.

**Epic 4 Vision:**
- Story 4.1: Coupling metrics (THIS STORY) - Foundation
- Story 4.2: Cyclomatic complexity metrics
- Story 4.3: Technology version debt metrics
- Story 4.4: External API exposure metrics
- Story 4.5: Combined extraction score calculator (uses 4.1-4.4)
- Story 4.6: Ranked extraction candidate lists
- Story 4.7: Heat map visualization with color-coded scores
- Story 4.8: Display extraction scores as node labels

**Critical Decisions for Story 4.1:**
1. **New Namespace:** `MasDependencyMap.Core.ExtractionScoring` - Feature-based per project conventions
2. **Model Pattern:** Use `record` types for immutable metric data (CouplingMetric)
3. **Calculator Pattern:** Interface + Implementation with async methods and ILogger injection
4. **DI Pattern:** Singleton services (stateless calculators)
5. **Scoring Pattern:** Always normalize to 0-100 scale for consistent scoring across Epic 4

🚨 **CRITICAL - Coupling Score Weighting Formula:**

**From Epic 4 Story 4.1 Acceptance Criteria:**

"Total coupling score is calculated as: (incoming * 2) + outgoing (incoming weighted higher)"

**Why Incoming Dependencies Are Weighted Higher (2x):**

Incoming dependencies represent projects that DEPEND ON this project. High incoming count means:
- More projects will break if you change this project
- Harder to extract because you must update many consuming projects
- Higher risk of breaking changes propagating across the codebase
- More coordination required for refactoring

Outgoing dependencies represent projects this project DEPENDS ON. High outgoing count means:
- This project is coupled to many others
- Extraction requires bringing dependencies along or replacing them
- Still important, but less impactful than incoming (1x weight)

**Formula:**
```csharp
var totalScore = (incomingCount * 2) + outgoingCount;
```

**Example:**
- Project A: 5 incoming, 3 outgoing → (5 * 2) + 3 = 13 total score
- Project B: 3 incoming, 5 outgoing → (3 * 2) + 5 = 11 total score
- Project A has higher coupling score despite same total edge count (8 vs 8)
- This correctly reflects that Project A is harder to extract (more consumers depend on it)

🚨 **CRITICAL - Normalization to 0-100 Scale:**

**From Epic 4 Story 4.1 Acceptance Criteria:**

"Coupling metric is normalized to 0-100 scale for scoring algorithm"

**Why Normalize:**
- Story 4.5 combines 4 metrics (coupling, complexity, tech debt, external APIs) using weighted average
- Each metric must be on same 0-100 scale to make weighting meaningful
- Prevents high coupling scores from dominating low complexity scores (apples-to-apples comparison)
- Consistent interpretation: 0 = minimal extraction difficulty, 100 = maximum extraction difficulty

**Normalization Algorithm:**

```csharp
// Find maximum total score across ALL projects in graph
var maxTotalScore = allCouplingMetrics.Max(m => m.TotalScore);

// Handle edge case: no dependencies at all
if (maxTotalScore == 0)
{
    // All projects have 0 coupling, normalized score is 0 for all
    normalizedScore = 0;
}
else
{
    // Linear normalization: map [0, maxTotalScore] to [0, 100]
    normalizedScore = (totalScore / (double)maxTotalScore) * 100;
}

// Clamp to 0-100 range (defensive programming)
normalizedScore = Math.Clamp(normalizedScore, 0, 100);
```

**Example:**
- Solution has 50 projects
- Maximum total score found: 80 (some highly coupled project)
- Project A total score: 40 → normalized = (40/80) * 100 = 50
- Project B total score: 80 → normalized = (80/80) * 100 = 100
- Project C total score: 0 → normalized = (0/80) * 100 = 0

**Critical Edge Cases:**
1. **Empty graph:** Return empty list (no projects to analyze)
2. **Single project, no dependencies:** Total score = 0, normalized = 0
3. **All projects have zero coupling:** Max = 0, all normalized scores = 0 (avoid divide by zero)
4. **Large solutions (100+ projects):** Normalization still works, log progress for user feedback

🚨 **CRITICAL - QuikGraph API for Edge Counting:**

**From Project Context + QuikGraph v2.5.0 Documentation:**

DependencyGraph is an `AdjacencyGraph<ProjectNode, DependencyEdge>` from QuikGraph:

```csharp
// Count incoming edges (projects that depend on this project)
var incomingCount = graph.InEdges(projectNode).Count();
// InEdges returns IEnumerable<DependencyEdge> of edges pointing TO this vertex

// Count outgoing edges (projects this project depends on)
var outgoingCount = graph.OutEdges(projectNode).Count();
// OutEdges returns IEnumerable<DependencyEdge> of edges pointing FROM this vertex

// Iterate all vertices (projects)
foreach (var project in graph.Vertices)
{
    // ... calculate metrics for each project
}
```

**QuikGraph Graph Traversal Pattern:**
- `graph.Vertices` - IEnumerable<ProjectNode> of all vertices in graph
- `graph.InEdges(vertex)` - All edges WHERE edge.Target == vertex
- `graph.OutEdges(vertex)` - All edges WHERE edge.Source == vertex
- Both return `IEnumerable<TEdge>`, use `.Count()` for count

**Defensive Programming:**
- Check if vertex exists in graph before calling InEdges/OutEdges
- Handle graphs with no edges (all counts = 0)
- Use LINQ `.Count()` for enumerable iteration (not .Length or .Count property)

### Technical Requirements

**New Namespace: MasDependencyMap.Core.ExtractionScoring:**

Epic 4 introduces extraction difficulty scoring - a new feature domain. Per project conventions, use feature-based namespace:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2
├── CycleAnalysis/               # Epic 3
├── Visualization/               # Epic 2, extended in Epic 3
└── ExtractionScoring/           # Epic 4 (NEW) - THIS STORY CREATES THIS
    ├── CouplingMetric.cs            # Model (Story 4.1)
    ├── ICouplingMetricCalculator.cs # Interface (Story 4.1)
    └── CouplingMetricCalculator.cs  # Implementation (Story 4.1)
```

**CouplingMetric Model Pattern:**

Use C# 9+ `record` for immutable data transfer objects (DTOs):

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents coupling metrics for a single project in a dependency graph.
/// Coupling quantifies how connected a project is to other projects via dependencies.
/// Higher coupling indicates harder extraction (more projects to coordinate changes with).
/// </summary>
/// <param name="ProjectName">Name of the project being analyzed.</param>
/// <param name="IncomingCount">Number of projects that depend on this project (consumers).</param>
/// <param name="OutgoingCount">Number of projects this project depends on (dependencies).</param>
/// <param name="TotalScore">Weighted coupling score: (IncomingCount * 2) + OutgoingCount. Incoming weighted higher because consumers make extraction harder.</param>
/// <param name="NormalizedScore">Coupling score normalized to 0-100 scale. 0 = minimal coupling (easy to extract), 100 = maximum coupling in solution (hard to extract).</param>
public sealed record CouplingMetric(
    string ProjectName,
    int IncomingCount,
    int OutgoingCount,
    int TotalScore,
    double NormalizedScore);
```

**Why `record` Instead of `class`:**
- Immutability: Metric data should not change after calculation
- Value equality: Compare metrics by value, not reference
- Concise syntax: Primary constructor with positional parameters
- Thread-safe: Immutable records are inherently thread-safe
- Pattern used in Epic 3 for CycleInfo, CycleBreakingSuggestion (established project pattern)

**ICouplingMetricCalculator Interface Pattern:**

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Calculates coupling metrics for projects in a dependency graph.
/// Coupling measures how connected each project is to others via incoming and outgoing dependencies.
/// Used to quantify extraction difficulty for migration planning.
/// </summary>
public interface ICouplingMetricCalculator
{
    /// <summary>
    /// Calculates coupling metrics for all projects in the dependency graph.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze. Must not be null.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// List of coupling metrics, one per project in the graph.
    /// Metrics include incoming/outgoing counts, weighted total score, and normalized 0-100 score.
    /// Returns empty list if graph has no vertices.
    /// </returns>
    /// <exception cref="ArgumentNullException">When graph is null.</exception>
    Task<IReadOnlyList<CouplingMetric>> CalculateAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default);
}
```

**Why Interface:**
- Dependency Injection: Enables testability via mocking
- Future extensibility: Could implement different coupling algorithms (weighted by method calls, by type usage, etc.)
- Consistent with project patterns: All services have I-prefix interfaces
- Epic 4.5 will consume this interface to combine metrics

**Async Pattern Even Though No I/O:**

Per project conventions, use async pattern for consistency with other calculators:
- Story 4.2 (Complexity Calculator) WILL use Roslyn semantic analysis (async I/O)
- Story 4.4 (External API Detector) WILL use Roslyn semantic analysis (async I/O)
- Story 4.1 (Coupling) is graph traversal (synchronous), but use async for consistency
- Pattern: Return `Task.FromResult()` if no async work, but maintain async signature
- Allows Epic 4.5 to call all calculators uniformly with `await`

### Architecture Compliance

**Namespace Organization (Feature-Based, NOT Layer-Based):**

✅ **CORRECT:**
- `MasDependencyMap.Core.ExtractionScoring` (feature: extraction scoring)

❌ **WRONG:**
- `MasDependencyMap.Core.Services.CouplingCalculator` (layer-based)
- `MasDependencyMap.Core.Models.CouplingMetric` (layer-based)

**File Naming and Structure:**

```
src/MasDependencyMap.Core/ExtractionScoring/
├── CouplingMetric.cs                    # One class per file, name matches class name
├── ICouplingMetricCalculator.cs         # I-prefix for interfaces
└── CouplingMetricCalculator.cs          # Descriptive implementation name
```

**Dependency Injection Registration:**

```csharp
// In Program.cs or DI configuration
services.AddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>();
```

**Lifetime:**
- Singleton: CouplingMetricCalculator is stateless (only reads graph, no mutable state)
- All Epic 4 calculators will be singletons
- Consistent with Epic 2 and Epic 3 service patterns

**Integration with Existing Components:**

Story 4.1 CONSUMES from Epic 2:
- DependencyGraph (graph structure)
- ProjectNode (vertices)
- DependencyEdge (edges)

Story 4.1 PRODUCES for Epic 4:
- CouplingMetric (model)
- ICouplingMetricCalculator (service)
- Will be consumed by Story 4.5 (ExtractionScoreCalculator)

### Library/Framework Requirements

**No New NuGet Packages Required:**

All dependencies already satisfied:
- ✅ QuikGraph v2.5.0 (installed in Epic 2) - For graph.Vertices, graph.InEdges, graph.OutEdges
- ✅ Microsoft.Extensions.Logging.Abstractions (installed in Epic 1) - For ILogger<T>
- ✅ System.Linq (built-in) - For .Count(), .Max()
- ✅ System.Threading (built-in) - For Task, CancellationToken

**QuikGraph v2.5.0 API Usage:**

```csharp
// DependencyGraph is AdjacencyGraph<ProjectNode, DependencyEdge>
public class CouplingMetricCalculator : ICouplingMetricCalculator
{
    public async Task<IReadOnlyList<CouplingMetric>> CalculateAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var metrics = new List<CouplingMetric>();

        // Iterate all vertices (projects)
        foreach (var project in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Count incoming edges (projects depending on this one)
            var incomingCount = graph.InEdges(project).Count();

            // Count outgoing edges (projects this one depends on)
            var outgoingCount = graph.OutEdges(project).Count();

            // Calculate weighted total score (incoming * 2 + outgoing)
            var totalScore = (incomingCount * 2) + outgoingCount;

            // Will normalize after collecting all scores
            metrics.Add(new CouplingMetric(
                project.ProjectName,
                incomingCount,
                outgoingCount,
                totalScore,
                NormalizedScore: 0)); // Placeholder, will normalize next
        }

        // Normalize all scores to 0-100 scale
        var maxTotalScore = metrics.Count > 0 ? metrics.Max(m => m.TotalScore) : 0;
        if (maxTotalScore == 0)
        {
            // All projects have 0 coupling, normalized scores stay 0
            return metrics.AsReadOnly();
        }

        // Recalculate with normalized scores
        var normalizedMetrics = metrics.Select(m => m with
        {
            NormalizedScore = Math.Clamp((m.TotalScore / (double)maxTotalScore) * 100, 0, 100)
        }).ToList();

        return normalizedMetrics.AsReadOnly();
    }
}
```

**C# Language Features Used:**
- `record` with positional parameters (C# 9+)
- `with` expression for record mutation (C# 9+)
- File-scoped namespaces (C# 10+)
- `ArgumentNullException.ThrowIfNull` (C# 11+, available in .NET 8)
- Primary constructors could be used for DI (C# 12, .NET 8)

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/ExtractionScoring/    # NEW DIRECTORY
├── CouplingMetric.cs                           # NEW
├── ICouplingMetricCalculator.cs                # NEW
└── CouplingMetricCalculator.cs                 # NEW

tests/MasDependencyMap.Core.Tests/ExtractionScoring/  # NEW DIRECTORY
└── CouplingMetricCalculatorTests.cs                  # NEW
```

**Files to Modify:**

```
src/MasDependencyMap.CLI/Program.cs              # MODIFY: Add DI registration
_bmad-output/implementation-artifacts/sprint-status.yaml  # MODIFY: Update story status
```

**No Integration with CLI Commands Yet:**

Story 4.1 creates the calculator but doesn't integrate it into CLI commands. That happens in Story 4.5 or later when all metrics are combined for extraction scoring.

For now:
- Create the service and register it in DI
- Full CLI integration happens in Epic 4 later stories
- Tests will validate functionality

### Testing Requirements

**Test Class: CouplingMetricCalculatorTests.cs**

```csharp
namespace MasDependencyMap.Core.Tests.ExtractionScoring;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.ExtractionScoring;
using MasDependencyMap.Core.DependencyAnalysis;

public class CouplingMetricCalculatorTests
{
    private readonly ILogger<CouplingMetricCalculator> _logger;
    private readonly CouplingMetricCalculator _calculator;

    public CouplingMetricCalculatorTests()
    {
        _logger = NullLogger<CouplingMetricCalculator>.Instance;
        _calculator = new CouplingMetricCalculator(_logger);
    }

    [Fact]
    public async Task CalculateAsync_GraphWithDependencies_CalculatesIncomingAndOutgoingCounts()
    {
        // Arrange: A -> B, C -> B (B has 2 incoming, 0 outgoing)
        var graph = CreateGraphWithDependencies();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        var projectBMetric = metrics.Single(m => m.ProjectName == "ProjectB");
        projectBMetric.IncomingCount.Should().Be(2);
        projectBMetric.OutgoingCount.Should().Be(0);
    }

    [Fact]
    public async Task CalculateAsync_GraphWithDependencies_WeightsIncomingHigherThanOutgoing()
    {
        // Arrange: A -> B (B: 1 incoming, 0 outgoing), A -> C (A: 0 incoming, 2 outgoing)
        var graph = CreateGraphForWeightingTest();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert - Verify formula: (incoming * 2) + outgoing
        var projectBMetric = metrics.Single(m => m.ProjectName == "ProjectB");
        projectBMetric.TotalScore.Should().Be((1 * 2) + 0); // = 2

        var projectAMetric = metrics.Single(m => m.ProjectName == "ProjectA");
        projectAMetric.TotalScore.Should().Be((0 * 2) + 2); // = 2
    }

    [Fact]
    public async Task CalculateAsync_GraphWithDependencies_NormalizesScoreTo100Scale()
    {
        // Arrange: Graph with known max coupling
        var graph = CreateGraphWithKnownMaxCoupling();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        var maxMetric = metrics.OrderByDescending(m => m.TotalScore).First();
        maxMetric.NormalizedScore.Should().Be(100.0);

        metrics.Should().AllSatisfy(m =>
        {
            m.NormalizedScore.Should().BeGreaterOrEqualTo(0.0);
            m.NormalizedScore.Should().BeLessOrEqualTo(100.0);
        });
    }

    [Fact]
    public async Task CalculateAsync_EmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        metrics.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateAsync_SingleProjectNoDependencies_ReturnsZeroCoupling()
    {
        // Arrange
        var graph = new DependencyGraph();
        var project = new ProjectNode("ProjectA", "ProjectA.csproj");
        graph.AddVertex(project);

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        metrics.Should().ContainSingle();
        var metric = metrics.First();
        metric.IncomingCount.Should().Be(0);
        metric.OutgoingCount.Should().Be(0);
        metric.TotalScore.Should().Be(0);
        metric.NormalizedScore.Should().Be(0);
    }

    [Fact]
    public async Task CalculateAsync_GraphWithMaxCoupling_NormalizedScoreIs100()
    {
        // Arrange: Create graph where one project has maximum coupling
        var graph = CreateGraphWithMaxCouplingProject();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        var maxCoupledProject = metrics.OrderByDescending(m => m.TotalScore).First();
        maxCoupledProject.NormalizedScore.Should().Be(100.0);
    }

    [Fact]
    public async Task CalculateAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _calculator.CalculateAsync(null!));
    }

    // Helper methods to create test graphs
    private DependencyGraph CreateGraphWithDependencies()
    {
        var graph = new DependencyGraph();
        var projectA = new ProjectNode("ProjectA", "ProjectA.csproj");
        var projectB = new ProjectNode("ProjectB", "ProjectB.csproj");
        var projectC = new ProjectNode("ProjectC", "ProjectC.csproj");

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);

        graph.AddEdge(new DependencyEdge(projectA, projectB)); // A -> B
        graph.AddEdge(new DependencyEdge(projectC, projectB)); // C -> B

        return graph;
    }

    // ... other helper methods
}
```

**Test Naming Convention:**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `CalculateAsync_GraphWithDependencies_CalculatesIncomingAndOutgoingCounts()`
- ✅ `CalculateAsync_GraphWithDependencies_WeightsIncomingHigherThanOutgoing()`
- ✅ `CalculateAsync_EmptyGraph_ReturnsEmptyList()`

**Test Coverage Checklist:**
- ✅ Incoming/outgoing count calculation
- ✅ Weighting formula validation (incoming * 2 + outgoing)
- ✅ Normalization to 0-100 scale
- ✅ Empty graph edge case
- ✅ Single project with no dependencies edge case
- ✅ Maximum coupling project gets normalized score 100
- ✅ Null graph throws ArgumentNullException
- ✅ (Optional) Large solution logging validation

### Previous Story Intelligence

**From Story 3-7 (Most Recent Completed Story) - Key Patterns to Reuse:**

1. **Record Model Pattern:**
   ```csharp
   // Story 3-5 created CycleBreakingSuggestion as record
   // Story 4-1 creates CouplingMetric as record (same pattern)
   public sealed record CouplingMetric(...)
   ```

2. **Calculator Service Pattern:**
   ```csharp
   // Epic 3 services: TarjanCycleDetector, CouplingAnalyzer, RecommendationGenerator
   // Epic 4 services: CouplingMetricCalculator, ComplexityCalculator, etc. (same pattern)
   // Interface + Implementation with ILogger injection
   ```

3. **DI Registration Pattern:**
   ```csharp
   // From Epic 2 and Epic 3
   services.AddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>();
   ```

4. **Test Structure Pattern:**
   ```csharp
   // From Story 3-7 DotGeneratorTests
   // Constructor with NullLogger setup
   // Helper methods for test graph creation
   // Arrange-Act-Assert structure
   // FluentAssertions for readable assertions
   ```

5. **Code Review Expectations:**
   - Expect 2-10 issues found in code review (based on Story 3-7 pattern)
   - Common issues: test coverage gaps, edge cases, performance optimizations, documentation improvements
   - Typical flow: Initial implementation commit → Code review fixes commit → Status update commit

**From Epic 3 (Cycle Analysis) - Similar Domain Patterns:**

Epic 3 analyzed graph structure for cycle detection and recommendations.
Epic 4 analyzes graph structure for extraction difficulty scoring.

Similar patterns:
- Graph traversal with QuikGraph APIs (InEdges, OutEdges, Vertices)
- Metric calculation and normalization
- Record models for metric data
- Calculator services with async patterns
- Integration via DI

**From Epic 2 (Dependency Graph Building) - Graph Model Understanding:**

DependencyGraph structure:
- `AdjacencyGraph<ProjectNode, DependencyEdge>` from QuikGraph
- `ProjectNode` has: ProjectName, ProjectPath, SolutionName
- `DependencyEdge` has: Source (ProjectNode), Target (ProjectNode), IsCrossSolution (bool)
- Graph methods: Vertices, InEdges(vertex), OutEdges(vertex)

### Git Intelligence Summary

**Recent Commits Pattern:**

Last 5 commits show rigorous code review process:
1. Initial "Story X.Y complete" commit with implementation
2. Follow-up "Code review fixes for Story X.Y" commit addressing 5-10 issues
3. Final "Update Story X.Y status to done" commit

**Expected File Changes for Story 4.1:**

Based on Story 3-7 pattern:
- New: `src/MasDependencyMap.Core/ExtractionScoring/CouplingMetric.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/ICouplingMetricCalculator.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/CouplingMetricCalculator.cs`
- New: `tests/MasDependencyMap.Core.Tests/ExtractionScoring/CouplingMetricCalculatorTests.cs`
- Modified: `src/MasDependencyMap.CLI/Program.cs` (DI registration)
- Modified: `_bmad-output/implementation-artifacts/sprint-status.yaml` (story status update)
- Modified: `_bmad-output/implementation-artifacts/4-1-implement-coupling-metric-calculator.md` (completion notes)

**Commit Message Pattern for Story Completion:**

```bash
git commit -m "Story 4-1 complete: Implement coupling metric calculator

- Created CouplingMetric record model with incoming/outgoing counts and normalized score
- Created ICouplingMetricCalculator interface for DI abstraction
- Implemented CouplingMetricCalculator with QuikGraph API usage for edge counting
- Implemented weighting formula: (incoming * 2) + outgoing per AC requirement
- Implemented normalization to 0-100 scale using linear scaling algorithm
- Added structured logging with named placeholders for progress tracking
- Registered service in DI container as singleton
- Created comprehensive unit tests with 7 test cases (all passing)
- Tests validate weighting formula, normalization, edge cases (empty graph, single project, max coupling)
- New namespace: MasDependencyMap.Core.ExtractionScoring (feature-based)
- All acceptance criteria satisfied
- Foundation for Epic 4 extraction difficulty scoring established

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 4.1 creates new Epic 4 namespace following established patterns:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
├── CycleAnalysis/               # Epic 3: Cycle detection
├── Visualization/               # Epic 2: DOT generation (extended in Epic 3)
├── SolutionLoading/             # Epic 2: Solution loading (implied)
└── ExtractionScoring/           # Epic 4: Extraction difficulty (NEW - THIS STORY)
    ├── CouplingMetric.cs            # Model
    ├── ICouplingMetricCalculator.cs # Interface
    └── CouplingMetricCalculator.cs  # Implementation
```

**Consistency with Existing Patterns:**
- ✅ Feature-based namespace (NOT layer-based)
- ✅ Interface + Implementation pattern (I-prefix interfaces)
- ✅ File naming matches class naming exactly
- ✅ Test namespace mirrors Core structure
- ✅ Service pattern with ILogger injection
- ✅ Singleton DI registration for stateless services
- ✅ Record model for immutable data
- ✅ Async methods with Async suffix

**Cross-Namespace Dependencies:**
- ExtractionScoring → DependencyAnalysis (uses DependencyGraph, ProjectNode, DependencyEdge)
- This is expected and acceptable (Epic 4 builds on Epic 2 infrastructure)
- Similar to Epic 3 (CycleAnalysis → DependencyAnalysis dependency)

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-4-extraction-difficulty-scoring-and-candidate-ranking.md, Story 4.1 (lines 5-20)]
- Story requirements: Coupling metrics with incoming/outgoing counts, weighted formula, normalization to 0-100 scale, logging

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 18-51)]
- Technology stack: .NET 8.0, C# 12, QuikGraph v2.5.0
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Namespace organization: Feature-based, Interface naming, Async patterns, Nullable reference types, File-scoped namespaces, Exception handling
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 101-119)]
- Dependency Injection: Constructor injection, Singleton vs Transient lifetimes
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 115-119)]
- Structured logging: Named placeholders, log levels, ILogger<T> injection
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 140-159)]
- Testing: Test organization, test naming, test frameworks

**QuikGraph v2.5.0 API:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 127-131)]
- AdjacencyGraph<TVertex, TEdge> usage, SCC algorithm, vertices and edges
- QuikGraph documentation: Graph traversal methods (Vertices, InEdges, OutEdges)

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-7-mark-suggested-break-points-in-yellow-on-visualizations.md (full file)]
- Record model pattern, service pattern, DI registration, test structure, code review expectations
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-7-mark-suggested-break-points-in-yellow-on-visualizations.md (lines 1125-1197)]
- File change patterns, commit message format, completion notes structure

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Code review pattern: Initial commit → Code review fixes → Status update
- File change pattern from Story 3-7

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

None

### Completion Notes List

**Implementation Date:** 2026-01-24

**Summary:**
Successfully implemented Epic 4 Story 4.1 - Coupling Metric Calculator. Created the foundation for extraction difficulty scoring with a robust, well-tested metric calculator that quantifies project coupling in dependency graphs.

**Key Accomplishments:**

1. **New ExtractionScoring Namespace Created** (Epic 4 Foundation)
   - Established `MasDependencyMap.Core.ExtractionScoring` namespace following feature-based organization
   - Created directory structure: `src/MasDependencyMap.Core/ExtractionScoring/`
   - Mirrored test structure: `tests/MasDependencyMap.Core.Tests/ExtractionScoring/`

2. **CouplingMetric Model Implemented**
   - Used C# 9+ `record` type for immutability and value equality
   - Comprehensive XML documentation explaining weighting formula and normalization
   - Properties: ProjectName, IncomingCount, OutgoingCount, TotalScore, NormalizedScore

3. **ICouplingMetricCalculator Interface Created**
   - Async API with cancellation support
   - Returns `Task<IReadOnlyList<CouplingMetric>>` for all projects
   - Includes XML documentation with usage examples and exception documentation

4. **CouplingMetricCalculator Implementation**
   - Uses DependencyGraph.GetInEdges() and GetOutEdges() for accurate edge counting
   - Weighting formula: `(incoming * 2) + outgoing` per AC requirements
   - Normalization: Linear scaling to 0-100 range using `Math.Clamp`
   - Structured logging with named placeholders (never string interpolation)
   - Handles edge cases: empty graphs, single projects, zero coupling scenarios

5. **DI Registration**
   - Registered as `services.TryAddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>()`
   - Added to "Epic 4: Extraction Scoring Services" section in Program.cs
   - Follows existing singleton pattern for stateless services

6. **Comprehensive Test Coverage (9 Tests, 100% Pass Rate)**
   - ✅ CalculateAsync_GraphWithDependencies_CalculatesIncomingAndOutgoingCounts
   - ✅ CalculateAsync_GraphWithDependencies_WeightsIncomingHigherThanOutgoing
   - ✅ CalculateAsync_GraphWithDependencies_NormalizesScoreTo100Scale
   - ✅ CalculateAsync_EmptyGraph_ReturnsEmptyList
   - ✅ CalculateAsync_SingleProjectNoDependencies_ReturnsZeroCoupling
   - ✅ CalculateAsync_GraphWithMaxCoupling_NormalizedScoreIs100
   - ✅ CalculateAsync_WeightingFormula_IncomingCountedTwiceAsHigh
   - ✅ CalculateAsync_NullGraph_ThrowsArgumentNullException
   - ✅ CalculateAsync_CancellationRequested_ThrowsOperationCanceledException (added during code review)

7. **Full Test Suite Validation**
   - All 270 tests passed (including 9 new tests)
   - No regressions introduced
   - Test execution time: 20 seconds
   - Code review fixes validated with additional cancellation test

**Architecture Compliance Verified:**
- ✅ Feature-based namespace (MasDependencyMap.Core.ExtractionScoring)
- ✅ File-scoped namespace declarations
- ✅ Async suffix on async methods
- ✅ ILogger<T> constructor injection
- ✅ XML documentation on all public APIs
- ✅ Test file structure mirrors Core namespace
- ✅ Record type for immutable data
- ✅ Singleton DI lifetime for stateless service
- ✅ Named placeholders in logging (no string interpolation)

**Technical Notes:**
- Used `Task.FromResult` for synchronous async method (graph traversal is CPU-bound, not I/O-bound)
- DependencyGraph wrapper required `GetInEdges()` and `GetOutEdges()` instead of direct QuikGraph API
- ProjectNode and DependencyEdge use required properties with init-only setters (C# 11 pattern)
- FluentAssertions API: `BeGreaterThanOrEqualTo` and `BeLessThanOrEqualTo` (not BeGreaterOrEqualTo)

**Foundation for Epic 4:**
This story establishes the pattern and infrastructure that Stories 4.2-4.8 will build upon:
- Metric model pattern (record types)
- Calculator interface pattern (I-prefix, async)
- DI registration pattern (singleton, TryAdd)
- Test structure pattern (helper methods, Arrange-Act-Assert)
- Normalization pattern (0-100 scale for all metrics)

**Next Steps:**
- Story 4.2: Implement Cyclomatic Complexity Metric Calculator (will follow same pattern)
- Story 4.3: Implement Technology Version Debt Metric Calculator
- Story 4.4: Implement External API Exposure Metric Calculator
- Story 4.5: Combine all metrics into unified Extraction Score Calculator

### File List

**New Files Created:**

- `src/MasDependencyMap.Core/ExtractionScoring/CouplingMetric.cs` - Record model for coupling metrics
- `src/MasDependencyMap.Core/ExtractionScoring/ICouplingMetricCalculator.cs` - Calculator interface
- `src/MasDependencyMap.Core/ExtractionScoring/CouplingMetricCalculator.cs` - Calculator implementation
- `tests/MasDependencyMap.Core.Tests/ExtractionScoring/CouplingMetricCalculatorTests.cs` - Comprehensive unit tests (9 tests including cancellation test added during code review)

**Modified Files:**

- `src/MasDependencyMap.CLI/Program.cs` - Added using directive and DI registration for ICouplingMetricCalculator
- `.claude/settings.local.json` - Added permissions for WebFetch(github.com), Bash(cd), Bash(ls)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - Updated story status from ready-for-dev → in-progress → review
- `_bmad-output/implementation-artifacts/4-1-implement-coupling-metric-calculator.md` - Marked all tasks complete, added completion notes

**Code Review Fixes Applied (2026-01-24):**

Following adversarial code review, 7 issues were identified and fixed:

HIGH Severity (3 fixed):
1. ✅ Git discrepancy - Added .claude/settings.local.json to Modified Files documentation
2. ✅ Duplicate logging - Consolidated LogDebug to single call per project with all metrics (Incoming, Outgoing, Total, Normalized)
3. ✅ Inefficient two-pass - Refactored to single-allocation algorithm using lightweight tuples for raw data, creates CouplingMetric objects only once

MEDIUM Severity (4 fixed):
4. ✅ Missing CancellationToken test - Added CalculateAsync_CancellationRequested_ThrowsOperationCanceledException test
5. ✅ Magic number 100 - Added const double NormalizedScoreScale = 100.0
6. ✅ Misleading XML docs - Updated ICouplingMetricCalculator to clarify synchronous implementation with async signature
7. ✅ Redundant logging - Removed duplicate ProjectCount from completion log, now logs only MaxScore

Test Results After Fixes:
- ✅ All 270 tests pass (269 original + 1 new cancellation test)
- ✅ 0 regressions introduced
- ✅ Code review fixes validated
