# Story 3.1: Implement Tarjan's SCC Algorithm for Cycle Detection

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want to detect all circular dependency chains using Tarjan's strongly connected components algorithm,
So that I can identify which projects are involved in cycles.

## Acceptance Criteria

**Given** A filtered DependencyGraph with circular dependencies
**When** TarjanCycleDetector.DetectCyclesAsync() is called
**Then** QuikGraph's Tarjan's SCC algorithm identifies all strongly connected components
**And** Each SCC with more than 1 project is identified as a circular dependency cycle
**And** CycleInfo objects are created for each cycle containing the list of projects involved
**And** Cycle statistics are calculated: total cycles, largest cycle size, total projects in cycles
**And** ILogger logs "Found 12 circular dependency chains, 45 projects (61.6%) involved in cycles"

**Given** A graph with no cycles
**When** TarjanCycleDetector.DetectCyclesAsync() is called
**Then** An empty list of CycleInfo objects is returned
**And** ILogger logs "No circular dependencies detected"

## Tasks / Subtasks

- [x] Create TarjanCycleDetector interface and implementation (AC: QuikGraph's Tarjan algorithm usage)
  - [x] Create ITarjanCycleDetector interface in Core.CycleAnalysis namespace
  - [x] Implement TarjanCycleDetector class using QuikGraph.Algorithms.StronglyConnectedComponentsAlgorithm
  - [x] Add ILogger<TarjanCycleDetector> dependency via constructor injection
  - [x] Implement DetectCyclesAsync method accepting DependencyGraph parameter

- [x] Create CycleInfo model class (AC: Cycle data structure)
  - [x] Create CycleInfo record in Core.CycleAnalysis namespace
  - [x] Add Projects property (IReadOnlyList<ProjectNode>)
  - [x] Add CycleSize property (int, computed from Projects.Count)
  - [x] Add CycleId property (int, sequential identifier)
  - [x] Use C# record type for immutability

- [x] Implement Tarjan's SCC algorithm invocation (AC: SCC detection)
  - [x] Use QuikGraph extension method StronglyConnectedComponents (Tarjan implementation)
  - [x] Pass DependencyGraph.GetUnderlyingGraph() as input to algorithm
  - [x] Dictionary receives component mappings from extension method
  - [x] Group components by index to extract SCCs

- [x] Filter SCCs to identify circular dependencies (AC: Cycles vs single-node components)
  - [x] Filter SCCs where component.Count > 1 (exclude single-node components)
  - [x] Create CycleInfo object for each multi-node SCC
  - [x] Assign sequential CycleId (1, 2, 3, ...) to each cycle
  - [x] Store ProjectNode list for each cycle

- [x] Calculate cycle statistics (AC: Total cycles, largest cycle, participation rate)
  - [x] Count total circular dependency chains (filtered SCCs)
  - [x] Find largest cycle size using Max() on CycleSize property
  - [x] Calculate total unique projects in cycles (distinct project count across all cycles)
  - [x] Calculate participation rate: (projects in cycles / total projects) * 100

- [x] Implement structured logging (AC: Logging requirements)
  - [x] Log "Detecting circular dependencies in {ProjectCount} projects" at Information level
  - [x] Log "Found {CycleCount} circular dependency chains" at Information level
  - [x] Log "{ProjectsInCycles} projects ({ParticipationRate:F1}%) involved in cycles" at Information level
  - [x] Log "No circular dependencies detected" at Information level when no cycles found
  - [x] Use named placeholders (NOT string interpolation)

- [x] Handle edge cases (AC: Empty graphs, single-project graphs)
  - [x] Return empty list when DependencyGraph has 0 vertices
  - [x] Return empty list when DependencyGraph has 1 vertex (tested via single-project graph)
  - [x] Return empty list when graph is acyclic (tree structure)
  - [x] Validate input graph is not null

- [x] Register service in DI container (AC: Dependency injection)
  - [x] Register ITarjanCycleDetector → TarjanCycleDetector as singleton in Program.cs
  - [x] Use services.TryAddSingleton() pattern for test override support

- [x] Create comprehensive tests (AC: Algorithm correctness)
  - [x] Unit test: Graph with circular dependency (3-project cycle) → returns 1 CycleInfo
  - [x] Unit test: Graph with multiple cycles (2 separate cycles) → returns 2 CycleInfo objects
  - [x] Unit test: Acyclic graph (tree) → returns empty list
  - [x] Unit test: Empty graph → returns empty list
  - [x] Unit test: Single-project graph → returns empty list
  - [x] Unit test: Cycle statistics calculation (largest cycle, participation rate)
  - [x] Unit test: Self-referencing project (single-node SCC) → returns empty list

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Use QuikGraph's Tarjan Implementation, NOT Custom:**

**From Epic 3 Story 3.1 Architecture Requirements:**
```
TarjanCycleDetector using QuikGraph's Tarjan's SCC algorithm
```

**From Project Context (lines 270-274):**
```
🚨 Circular Dependency Detection:
- Use Tarjan's algorithm via StronglyConnectedComponentsAlgorithm<TVertex, TEdge>
- NEVER implement custom cycle detection - QuikGraph's implementation is proven
- A strongly connected component with >1 vertex IS a circular dependency
- Report ALL projects in the cycle, not just the first one found
```

**Why QuikGraph's Implementation:**
- Battle-tested, mathematically correct implementation
- Handles edge cases (self-loops, multi-edges, disconnected graphs)
- O(V + E) time complexity (optimal)
- Thread-safe when used correctly
- No need to reinvent the wheel

**Implementation Pattern:**
```csharp
public async Task<IReadOnlyList<CycleInfo>> DetectCyclesAsync(
    DependencyGraph graph,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(graph);

    // Use QuikGraph's built-in Tarjan algorithm
    var sccAlgorithm = new StronglyConnectedComponentsAlgorithm<ProjectNode, DependencyEdge>(graph);
    sccAlgorithm.Compute();

    var cycles = new List<CycleInfo>();
    int cycleId = 1;

    // Filter to multi-node SCCs (these are circular dependencies)
    foreach (var component in sccAlgorithm.Components)
    {
        if (component.Count > 1) // Cycle found
        {
            cycles.Add(new CycleInfo(
                cycleId++,
                component.ToList()));
        }
    }

    return cycles;
}
```

🚨 **CRITICAL - Namespace Organization (Feature-Based):**

**From Project Context (lines 56-59):**
```
MUST use feature-based namespaces, NOT layer-based
Pattern: MasDependencyMap.Core.{Feature}
NEVER use layer-based like MasDependencyMap.Core.Services
```

**Correct Namespace for This Story:**
```csharp
namespace MasDependencyMap.Core.CycleAnalysis;
```

**NOT:**
- ❌ `MasDependencyMap.Core.Services.CycleAnalysis`
- ❌ `MasDependencyMap.Core.Algorithms.CycleDetection`
- ❌ `MasDependencyMap.Core.Models.Cycles`

**File Structure:**
```
src/MasDependencyMap.Core/
└── CycleAnalysis/              # Feature-based namespace
    ├── ITarjanCycleDetector.cs
    ├── TarjanCycleDetector.cs
    └── CycleInfo.cs
```

🚨 **CRITICAL - Async All The Way (Even For In-Memory Operations):**

**From Project Context (lines 66-69, 295-299):**
```
ALWAYS use Async suffix for async methods, even when no sync version exists
ALL I/O operations MUST be async
Use ConfigureAwait(false) in library code (Core layer)
NEVER use .Result or .Wait() - causes deadlocks
```

**Why Async For Tarjan (Even Though It's In-Memory):**
- Consistency with codebase patterns (all Core methods are async)
- Enables future cancellation token support
- Allows future parallel processing optimizations
- Prevents accidental blocking calls in async context
- Follows .NET best practices for library code

**Implementation Pattern:**
```csharp
public async Task<IReadOnlyList<CycleInfo>> DetectCyclesAsync(
    DependencyGraph graph,
    CancellationToken cancellationToken = default)
{
    // Even though Tarjan is synchronous, wrap in Task.Run for consistency
    return await Task.Run(() =>
    {
        var sccAlgorithm = new StronglyConnectedComponentsAlgorithm<ProjectNode, DependencyEdge>(graph);
        sccAlgorithm.Compute();

        // ... process results

        return cycles;
    }, cancellationToken).ConfigureAwait(false);
}
```

**Alternative (if synchronous is acceptable):**
Since graph algorithms are CPU-bound and not I/O, you MAY implement synchronously but MUST still use async signature for consistency:
```csharp
public Task<IReadOnlyList<CycleInfo>> DetectCyclesAsync(
    DependencyGraph graph,
    CancellationToken cancellationToken = default)
{
    // Synchronous implementation wrapped in Task.FromResult
    var result = DetectCyclesInternal(graph);
    return Task.FromResult(result);
}
```

🚨 **CRITICAL - Structured Logging (Named Placeholders):**

**From Project Context (lines 115-119):**
```
Use structured logging with named placeholders:
  _logger.LogInformation("Loading {SolutionPath}", path)
NEVER use string interpolation:
  _logger.LogInformation($"Loading {path}")
```

**Correct Logging Pattern for Story 3.1:**
```csharp
// ✅ CORRECT: Named placeholders
_logger.LogInformation(
    "Detecting circular dependencies in {ProjectCount} projects",
    graph.VertexCount);

_logger.LogInformation(
    "Found {CycleCount} circular dependency chains, {ProjectsInCycles} projects ({ParticipationRate:F1}%) involved in cycles",
    cycles.Count,
    totalProjectsInCycles,
    participationRate);

_logger.LogInformation("No circular dependencies detected");

// ❌ WRONG: String interpolation
_logger.LogInformation($"Found {cycles.Count} cycles"); // DO NOT USE
```

**Why Named Placeholders:**
- Enables structured logging analysis (search by CycleCount, filter by ProjectsInCycles)
- Better performance (no string allocation before logging)
- Consistent with .NET logging best practices
- Enables logging aggregation and monitoring tools

🚨 **CRITICAL - Dependency Injection Registration:**

**From Project Context (lines 101-107, 216-224):**
```
Register services in CLI Program.cs using ServiceCollection
Core components MUST use constructor injection
Lifetime patterns: Singletons for stateless services
Use TryAdd pattern to allow test overrides
```

**Registration Pattern for Story 3.1:**
```csharp
// In Program.cs (CLI project)
services.TryAddSingleton<ITarjanCycleDetector, TarjanCycleDetector>();
```

**Why Singleton:**
- TarjanCycleDetector is stateless (no instance state between calls)
- No I/O operations (pure graph algorithm)
- Thread-safe when used correctly
- Performance: Avoid creating new instances per call

**Constructor Injection:**
```csharp
public class TarjanCycleDetector : ITarjanCycleDetector
{
    private readonly ILogger<TarjanCycleDetector> _logger;

    public TarjanCycleDetector(ILogger<TarjanCycleDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

🚨 **CRITICAL - CycleInfo Immutability (Use C# Record):**

**From Project Context (lines 54-79 - C# 12 best practices):**
```
Use file-scoped namespace declarations (C# 10+)
Nullable reference types are ENABLED
Use modern C# features
```

**CycleInfo as Immutable Record:**
```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Represents a circular dependency cycle detected by Tarjan's algorithm.
/// Contains the set of projects involved in the cycle.
/// </summary>
public sealed record CycleInfo
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

    public CycleInfo(int cycleId, IReadOnlyList<ProjectNode> projects)
    {
        CycleId = cycleId;
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));

        if (projects.Count < 2)
            throw new ArgumentException("Cycle must contain at least 2 projects", nameof(projects));
    }
}
```

**Why Record Type:**
- Value-based equality (two CycleInfo with same data are equal)
- Immutable by default (init-only properties)
- Concise syntax with primary constructor support
- Thread-safe (no mutation after construction)
- Follows modern C# 12 patterns

### Technical Requirements

**QuikGraph Tarjan's SCC Algorithm Usage:**

QuikGraph provides `StronglyConnectedComponentsAlgorithm<TVertex, TEdge>` which implements Tarjan's algorithm for finding strongly connected components.

**Algorithm Properties:**
- **Time Complexity:** O(V + E) where V = vertices (projects), E = edges (dependencies)
- **Space Complexity:** O(V) for DFS stack and component tracking
- **Output:** Dictionary mapping each vertex to its component index
- **Guarantees:** Finds ALL strongly connected components in a single pass

**Usage Pattern:**
```csharp
using QuikGraph.Algorithms;

var sccAlgorithm = new StronglyConnectedComponentsAlgorithm<ProjectNode, DependencyEdge>(graph);
sccAlgorithm.Compute();

// Access results
int componentCount = sccAlgorithm.ComponentCount;
IDictionary<ProjectNode, int> componentMap = sccAlgorithm.Components;

// Group by component index
var componentGroups = componentMap
    .GroupBy(kvp => kvp.Value)
    .Select(g => g.Select(kvp => kvp.Key).ToList())
    .Where(component => component.Count > 1) // Filter to cycles only
    .ToList();
```

**ITarjanCycleDetector Interface Design:**

```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Detects circular dependency cycles using Tarjan's strongly connected components algorithm.
/// Identifies all cycles in a dependency graph and provides cycle statistics.
/// </summary>
public interface ITarjanCycleDetector
{
    /// <summary>
    /// Detects all circular dependency cycles in the given dependency graph.
    /// Uses Tarjan's SCC algorithm to identify strongly connected components with >1 project.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <returns>
    /// List of CycleInfo objects representing detected cycles.
    /// Returns empty list if no cycles found.
    /// </returns>
    /// <exception cref="ArgumentNullException">When graph is null.</exception>
    Task<IReadOnlyList<CycleInfo>> DetectCyclesAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default);
}
```

**Cycle Statistics Calculation:**

Story 3.1 requires calculating cycle statistics (used in logging):
- **Total Cycles:** `cycles.Count`
- **Largest Cycle Size:** `cycles.Max(c => c.CycleSize)` or 0 if no cycles
- **Total Projects in Cycles:** `cycles.SelectMany(c => c.Projects).Distinct().Count()`
- **Participation Rate:** `(totalProjectsInCycles / graph.VertexCount) * 100.0`

**Implementation Pattern:**
```csharp
private (int totalCycles, int largestCycle, int projectsInCycles, double participationRate) CalculateStatistics(
    IReadOnlyList<CycleInfo> cycles,
    int totalProjectCount)
{
    if (cycles.Count == 0)
        return (0, 0, 0, 0.0);

    int totalCycles = cycles.Count;
    int largestCycle = cycles.Max(c => c.CycleSize);

    // Count distinct projects across all cycles (project may appear in multiple cycles)
    int projectsInCycles = cycles
        .SelectMany(c => c.Projects)
        .Distinct()
        .Count();

    double participationRate = (projectsInCycles / (double)totalProjectCount) * 100.0;

    return (totalCycles, largestCycle, projectsInCycles, participationRate);
}
```

**Logging Integration:**

Follow existing logging patterns from Story 2-10 (lines 1294-1306):

```csharp
// Before detection
_logger.LogInformation(
    "Detecting circular dependencies in {ProjectCount} projects",
    graph.VertexCount);

// After detection (cycles found)
if (cycles.Count > 0)
{
    var stats = CalculateStatistics(cycles, graph.VertexCount);

    _logger.LogInformation(
        "Found {CycleCount} circular dependency chains, {ProjectsInCycles} projects ({ParticipationRate:F1}%) involved in cycles",
        stats.totalCycles,
        stats.projectsInCycles,
        stats.participationRate);
}
else
{
    _logger.LogInformation("No circular dependencies detected");
}
```

**Error Handling:**

Follow error handling patterns from Story 2-10:

```csharp
public async Task<IReadOnlyList<CycleInfo>> DetectCyclesAsync(
    DependencyGraph graph,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(graph);

    if (graph.VertexCount == 0)
    {
        _logger.LogInformation("Empty graph provided, no cycles to detect");
        return Array.Empty<CycleInfo>();
    }

    try
    {
        // ... Tarjan algorithm execution
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during cycle detection");
        throw; // Re-throw after logging
    }
}
```

### Architecture Compliance

**Epic 3 Architecture Requirements (from epic-list.md lines 41-54):**

```
Epic 3: Circular Dependency Detection and Break-Point Analysis
Architecture Requirements:
- TarjanCycleDetector using QuikGraph's Tarjan's SCC algorithm
- CouplingAnalyzer for method call counting across dependency edges
- Enhanced DOT visualization with RED cycle highlighting and YELLOW break point marking
- Cycle statistics calculation (total cycles, sizes, project participation rates)
- Ranked cycle-breaking recommendations based on weakest coupling edges
```

**Story 3.1 Implements:**
- ✅ TarjanCycleDetector interface and implementation
- ✅ QuikGraph's Tarjan SCC algorithm integration
- ✅ CycleInfo model for storing cycle data
- ✅ Cycle statistics calculation
- ❌ CouplingAnalyzer (Story 3.3)
- ❌ DOT visualization enhancements (Stories 3.6, 3.7)
- ❌ Cycle-breaking recommendations (Story 3.5)

**Integration with Existing Components:**

Story 3.1 consumes:
- **DependencyGraph** (from Story 2-5): Input to Tarjan algorithm
- **ProjectNode** (from Story 2-5): Vertices in dependency graph
- **DependencyEdge** (from Story 2-5): Edges in dependency graph
- **ILogger<T>** (from Story 1-6): Structured logging
- **QuikGraph v2.5.0** (from Story 2-5): Graph data structures and Tarjan algorithm

Story 3.1 produces:
- **CycleInfo** objects: Consumed by Story 3.2 (statistics), Story 3.4 (weak edge identification), Story 3.6 (visualization)
- **ITarjanCycleDetector** interface: Used by CLI for cycle detection workflow

**Namespace Organization:**
```
src/MasDependencyMap.Core/
├── DependencyAnalysis/           # From Epic 2
│   ├── DependencyGraph.cs
│   ├── ProjectNode.cs
│   └── DependencyEdge.cs
└── CycleAnalysis/                # NEW: Epic 3 Story 3.1
    ├── ITarjanCycleDetector.cs
    ├── TarjanCycleDetector.cs
    └── CycleInfo.cs
```

**DI Integration:**
```csharp
// Existing (from Epic 2)
services.TryAddSingleton<IDependencyGraphBuilder, DependencyGraphBuilder>();

// NEW: Story 3.1
services.TryAddSingleton<ITarjanCycleDetector, TarjanCycleDetector>();
```

### Library/Framework Requirements

**QuikGraph v2.5.0 - Already Installed (Story 2-5):**

From project-context.md (lines 26, 127-130):
```
QuikGraph v2.5.0 - Graph data structures and algorithms
Use QuikGraph.Algorithms.StronglyConnectedComponentsAlgorithm for cycle detection
```

**No New NuGet Packages Required:**

All dependencies already satisfied:
- ✅ QuikGraph v2.5.0 (installed in Story 2-5)
- ✅ Microsoft.Extensions.Logging.Abstractions (installed in Story 1-6)
- ✅ Microsoft.Extensions.DependencyInjection (installed in Story 1-5)

**QuikGraph Usage Verification:**

From Story 2-5 implementation:
- DependencyGraph uses `AdjacencyGraph<ProjectNode, DependencyEdge>`
- Graph structure is compatible with QuikGraph algorithms
- Tarjan algorithm requires `IVertexAndEdgeListGraph<TVertex, TEdge>` (DependencyGraph satisfies this)

**Import Statements:**
```csharp
using QuikGraph.Algorithms;
using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.DependencyAnalysis;
```

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/
└── CycleAnalysis/                        # NEW namespace
    ├── ITarjanCycleDetector.cs           # NEW: Cycle detector interface
    ├── TarjanCycleDetector.cs            # NEW: Tarjan algorithm implementation
    └── CycleInfo.cs                      # NEW: Cycle data model

tests/MasDependencyMap.Core.Tests/
└── CycleAnalysis/                        # NEW namespace
    └── TarjanCycleDetectorTests.cs       # NEW: Comprehensive test suite
```

**Files to Modify:**

```
src/MasDependencyMap.CLI/Program.cs (register ITarjanCycleDetector in DI)
```

**Files NOT to Modify:**

```
src/MasDependencyMap.Core/DependencyAnalysis/DependencyGraph.cs (reused as-is)
src/MasDependencyMap.Core/DependencyAnalysis/ProjectNode.cs (reused as-is)
src/MasDependencyMap.Core/DependencyAnalysis/DependencyEdge.cs (reused as-is)
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

public class TarjanCycleDetectorTests
{
    private readonly ILogger<TarjanCycleDetector> _logger;
    private readonly TarjanCycleDetector _detector;

    public TarjanCycleDetectorTests()
    {
        _logger = NullLogger<TarjanCycleDetector>.Instance;
        _detector = new TarjanCycleDetector(_logger);
    }

    [Fact]
    public async Task DetectCyclesAsync_GraphWithCycle_ReturnsOneCycle()
    {
        // Arrange
        var graph = CreateGraphWithCycle(); // A -> B -> C -> A

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().HaveCount(1);
        cycles[0].CycleSize.Should().Be(3);
        cycles[0].Projects.Should().Contain(p => p.Name == "ProjectA");
        cycles[0].Projects.Should().Contain(p => p.Name == "ProjectB");
        cycles[0].Projects.Should().Contain(p => p.Name == "ProjectC");
    }

    [Fact]
    public async Task DetectCyclesAsync_AcyclicGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = CreateAcyclicGraph(); // A -> B -> C (tree)

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCyclesAsync_EmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCyclesAsync_MultipleCycles_ReturnsAllCycles()
    {
        // Arrange
        var graph = CreateGraphWithTwoCycles(); // A -> B -> A, C -> D -> C

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().HaveCount(2);
    }

    [Fact]
    public async Task DetectCyclesAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _detector.DetectCyclesAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("graph");
    }

    [Fact]
    public async Task DetectCyclesAsync_CycleStatistics_CalculatedCorrectly()
    {
        // Arrange
        var graph = CreateGraphWith3ProjectCycleAnd5ProjectCycle();

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().HaveCount(2);
        var largestCycle = cycles.Max(c => c.CycleSize);
        largestCycle.Should().Be(5);
    }

    // Helper methods to create test graphs
    private DependencyGraph CreateGraphWithCycle() { /* ... */ }
    private DependencyGraph CreateAcyclicGraph() { /* ... */ }
    private DependencyGraph CreateGraphWithTwoCycles() { /* ... */ }
}
```

**Test Naming Convention (from project-context.md lines 150-153):**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `DetectCyclesAsync_GraphWithCycle_ReturnsOneCycle()`
- ✅ `DetectCyclesAsync_AcyclicGraph_ReturnsEmptyList()`
- ✅ `DetectCyclesAsync_NullGraph_ThrowsArgumentNullException()`

**Test Categories:**
- Unit tests: Test TarjanCycleDetector with manually constructed graphs
- Integration tests: Load sample solution, verify cycle detection end-to-end
- Edge case tests: Empty graph, single project, acyclic graph

### Previous Story Intelligence

**From Story 2-10 (Multi-Solution Analysis) - Key Learnings:**

**Patterns to Reuse:**
```csharp
// Structured logging with named placeholders
_logger.LogInformation(
    "Detected {CycleCount} cycles in {ProjectCount} projects",
    cycles.Count,
    graph.VertexCount);

// ConfigureAwait(false) in library code
var cycles = await DetectCyclesInternalAsync(graph, cancellationToken)
    .ConfigureAwait(false);

// Argument validation
ArgumentNullException.ThrowIfNull(graph);
```

**DI Registration Pattern (from Story 2-10, lines 1450):**
```csharp
services.TryAddSingleton<ITarjanCycleDetector, TarjanCycleDetector>();
```

**Test Pattern (from Story 2-10, lines 1000-1123):**
```csharp
public class TarjanCycleDetectorTests
{
    private readonly ILogger<TarjanCycleDetector> _logger;
    private readonly TarjanCycleDetector _detector;

    public TarjanCycleDetectorTests()
    {
        _logger = NullLogger<TarjanCycleDetector>.Instance;
        _detector = new TarjanCycleDetector(_logger);
    }

    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

**From Story 2-5 (DependencyGraphBuilder) - Graph Construction:**

Story 2-5 established the DependencyGraph model that Story 3.1 will analyze:

**Graph Structure:**
- **Vertices:** ProjectNode objects (project name, path, target framework)
- **Edges:** DependencyEdge objects (source → target dependency)
- **Graph Type:** `AdjacencyGraph<ProjectNode, DependencyEdge>`

**Integration Point:**
```csharp
// Story 2-5 produces DependencyGraph
var graph = await graphBuilder.BuildAsync(solutions);

// Story 3.1 consumes DependencyGraph
var cycles = await cycleDetector.DetectCyclesAsync(graph);
```

**From QuikGraph Documentation:**

**Tarjan Algorithm Usage:**
```csharp
using QuikGraph.Algorithms;

var algorithm = new StronglyConnectedComponentsAlgorithm<ProjectNode, DependencyEdge>(graph);
algorithm.Compute();

// Results available in algorithm.Components
// Dictionary<ProjectNode, int> where int is component index
```

**Component Grouping:**
```csharp
var componentGroups = algorithm.Components
    .GroupBy(kvp => kvp.Value)
    .Select(g => new { ComponentId = g.Key, Nodes = g.Select(kvp => kvp.Key).ToList() })
    .Where(c => c.Nodes.Count > 1) // Filter to cycles only
    .ToList();
```

### Git Intelligence Summary

**Expected Commit Message for Story 3.1:**
```bash
git commit -m "Story 3-1 complete: Implement Tarjan's SCC algorithm for cycle detection

- Created ITarjanCycleDetector interface in Core.CycleAnalysis namespace
- Implemented TarjanCycleDetector using QuikGraph.Algorithms.StronglyConnectedComponentsAlgorithm
- Created CycleInfo record for immutable cycle data (C# 12 record type)
- Implemented cycle statistics calculation (total cycles, largest cycle, participation rate)
- Added structured logging with named placeholders per project-context.md
- Registered ITarjanCycleDetector as singleton in DI container
- Created comprehensive unit tests (7 tests) - all passing
- Handles edge cases: empty graph, acyclic graph, multiple cycles
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

**Expected Files:**
```bash
# New files
src/MasDependencyMap.Core/CycleAnalysis/ITarjanCycleDetector.cs
src/MasDependencyMap.Core/CycleAnalysis/TarjanCycleDetector.cs
src/MasDependencyMap.Core/CycleAnalysis/CycleInfo.cs
tests/MasDependencyMap.Core.Tests/CycleAnalysis/TarjanCycleDetectorTests.cs

# Modified files
src/MasDependencyMap.CLI/Program.cs

# Story tracking
_bmad-output/implementation-artifacts/3-1-implement-tarjans-scc-algorithm-for-cycle-detection.md
_bmad-output/implementation-artifacts/sprint-status.yaml
```

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 3.1 creates new namespace `MasDependencyMap.Core.CycleAnalysis` following feature-based organization:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/     # Epic 2: Graph building
├── CycleAnalysis/          # Epic 3: Cycle detection (NEW)
├── Filtering/              # Epic 2: Framework filtering
├── SolutionLoading/        # Epic 2: Solution loading
├── Visualization/          # Epic 2: DOT generation
└── Rendering/              # Epic 2: Graphviz rendering
```

**Consistency with Existing Patterns:**
- Feature-based namespaces (NOT layer-based)
- Interface + Implementation pattern (ITarjanCycleDetector, TarjanCycleDetector)
- Test namespace mirrors Core namespace (tests/MasDependencyMap.Core.Tests/CycleAnalysis)
- File naming matches class naming exactly

**No Conflicts Detected:**
- CycleAnalysis namespace is new (no existing code to conflict with)
- Uses existing DependencyGraph, ProjectNode, DependencyEdge (no modifications needed)
- Follows established DI registration pattern

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-3-circular-dependency-detection-and-break-point-analysis.md, Story 3.1 (lines 5-24)]
- Story requirements: Tarjan's SCC algorithm, cycle detection, statistics calculation

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-list.md, Epic 3 Architecture (lines 41-54)]
- TarjanCycleDetector using QuikGraph's Tarjan algorithm
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md (full file)]
- DI patterns, logging patterns, error handling strategy

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Circular Dependency Detection (lines 270-274)]
- Use QuikGraph's Tarjan implementation, NOT custom
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Namespace Organization (lines 56-59)]
- Feature-based namespaces (MasDependencyMap.Core.CycleAnalysis)
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Async Patterns (lines 66-69, 295-299)]
- Async suffix, ConfigureAwait(false), no .Result/.Wait()
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Structured Logging (lines 115-119)]
- Named placeholders, no string interpolation

**Previous Stories:**
- [Source: Story 2-5: Build Dependency Graph with QuikGraph]
- DependencyGraph, ProjectNode, DependencyEdge reused as-is
- [Source: Story 2-10: Support Multi-Solution Analysis]
- Structured logging patterns, DI registration patterns, test patterns

**QuikGraph Documentation:**
- QuikGraph.Algorithms.StronglyConnectedComponentsAlgorithm usage
- O(V + E) time complexity, Dictionary<TVertex, int> component mapping

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - Implementation completed successfully without debugging requirements

### Completion Notes List

**Implementation Summary:**
- Created ITarjanCycleDetector interface with DetectCyclesAsync method
- Implemented TarjanCycleDetector using QuikGraph's StronglyConnectedComponents extension method (Tarjan's algorithm)
- Created immutable CycleInfo record with Projects, CycleSize, and CycleId properties
- Used QuikGraph extension method on BidirectionalGraph via DependencyGraph.GetUnderlyingGraph()
- Implemented cycle statistics calculation (total cycles, largest cycle, participation rate)
- Added structured logging with named placeholders for all cycle detection operations
- Registered ITarjanCycleDetector as singleton in DI container using TryAddSingleton pattern
- Created 8 comprehensive unit tests - all passing

**Key Technical Decisions:**
- Used QuikGraph's extension method `StronglyConnectedComponents()` instead of direct algorithm class instantiation
- Accessed underlying BidirectionalGraph via `GetUnderlyingGraph()` method for QuikGraph compatibility
- Implemented synchronous algorithm logic with Task.FromResult for async signature consistency
- Used C# 12 record type for CycleInfo immutability and value semantics
- Followed feature-based namespace organization: MasDependencyMap.Core.CycleAnalysis

**Test Coverage:**
- Simple 3-project cycle detection
- Multiple independent cycles (2-project and 3-project)
- Acyclic graph (tree structure)
- Empty graph edge case
- Single-project graph edge case
- Cycle statistics calculation accuracy
- Self-referencing project (single-node SCC)
- Null graph argument validation

### File List

**New Files Created:**
- src/MasDependencyMap.Core/CycleAnalysis/ITarjanCycleDetector.cs
- src/MasDependencyMap.Core/CycleAnalysis/TarjanCycleDetector.cs
- src/MasDependencyMap.Core/CycleAnalysis/CycleInfo.cs
- tests/MasDependencyMap.Core.Tests/CycleAnalysis/TarjanCycleDetectorTests.cs

**Modified Files:**
- src/MasDependencyMap.CLI/Program.cs (added DI registration for ITarjanCycleDetector)

## Change Log

- 2026-01-24: Story 3-1 complete - Implemented Tarjan's SCC algorithm for cycle detection using QuikGraph extension method. Created ITarjanCycleDetector interface, TarjanCycleDetector implementation, and immutable CycleInfo record. Added comprehensive test coverage (8 tests, all passing). All acceptance criteria satisfied.
