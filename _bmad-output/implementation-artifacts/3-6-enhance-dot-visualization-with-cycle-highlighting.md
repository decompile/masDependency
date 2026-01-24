# Story 3.6: Enhance DOT Visualization with Cycle Highlighting

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want circular dependencies highlighted in RED on dependency graphs,
So that I can visually identify problematic areas.

## Acceptance Criteria

**Given** Cycles have been detected in the dependency graph
**When** DotGenerator.GenerateAsync() is called with cycle information
**Then** Edges that are part of circular dependencies are rendered in RED
**And** Edges not in cycles remain in default color (black or gray)
**And** The visual distinction between cyclic and non-cyclic edges is clear
**And** Multi-cycle scenarios show all cyclic edges in RED
**And** Graph legend includes "Red: Circular Dependencies" notation

## Tasks / Subtasks

- [x] Extend IDotGenerator interface to accept cycle information (AC: DotGenerator can receive cycle data)
  - [x] Add optional `IReadOnlyList<CycleInfo>? cycles = null` parameter to GenerateAsync method
  - [x] Update XML documentation to describe cycle highlighting behavior
  - [x] Maintain backward compatibility (cycles parameter is optional, defaults to null)
  - [x] Document that when cycles is null or empty, no cycle highlighting is applied

- [x] Create helper method to build cycle edge set (AC: Fast edge lookup for cycle membership)
  - [x] Create private method `BuildCyclicEdgeSet(IReadOnlyList<CycleInfo> cycles, DependencyGraph graph)`
  - [x] Return HashSet<(string source, string target)> for O(1) edge lookup
  - [x] For each cycle, identify all edges where both source and target are in cycle's project list
  - [x] Use tuple of (source.ProjectName, target.ProjectName) as hash key
  - [x] Handle empty cycles list gracefully (return empty HashSet)
  - [x] Log Debug message: "Identified {CyclicEdgeCount} edges across {CycleCount} cycles"

- [x] Modify BuildDotContent to accept and use cycle information (AC: Edge coloring logic)
  - [x] Add cycles parameter to BuildDotContent method signature
  - [x] Build cyclic edge set at start of method using helper
  - [x] Check each edge against cyclic edge set during edge generation
  - [x] Color priority: Cycles (RED) > Cross-solution (move to different color)
  - [x] Track count of cyclic edges colored for logging

- [x] Update edge coloring logic with cycle priority (AC: RED for cycles, clear visual distinction)
  - [x] If edge is in cyclic edge set → color="red", style="bold"
  - [x] Else if edge is cross-solution → color="blue", style="bold" (changed from red to avoid conflict)
  - [x] Else (intra-solution, non-cyclic) → color="black" (default)
  - [x] Log Debug: "Applied cycle highlighting: {CyclicEdgeCount} edges marked in red"
  - [x] Ensure visual distinction is clear (bold style for both cycles and cross-solution)

- [x] Add legend entry for circular dependencies (AC: "Red: Circular Dependencies" notation)
  - [x] Check if cycles were provided and have content
  - [x] Add legend section for cycle coloring when cycles exist
  - [x] Legend entry: "Red: Circular Dependencies"
  - [x] Position after solution legend (if multi-solution graph)
  - [x] Use subgraph cluster for legend organization
  - [x] Example legend node showing RED edge

- [x] Update cross-solution legend to reflect new color (AC: Updated legend for cross-solution)
  - [x] Change cross-solution legend from "Red" to "Blue"
  - [x] Add legend entry: "Blue: Cross-Solution Dependencies"
  - [x] Ensure legend is only shown when cross-solution edges exist
  - [x] Maintain consistent legend formatting

- [x] Handle edge cases and validation (AC: Robustness)
  - [x] Null cycles parameter → no cycle highlighting (backward compatible)
  - [x] Empty cycles list → no cycle highlighting
  - [x] Cycles with no edges in graph → log Debug warning, continue
  - [x] Graph with no cycles but cycles param provided → no RED edges, valid state
  - [x] Multi-cycle graph → all cyclic edges RED regardless of which cycle

- [x] Update existing tests and add new cycle highlighting tests (AC: Test coverage)
  - [x] Update existing DotGeneratorTests to pass null for cycles (backward compatibility)
  - [x] Add test: GenerateAsync_WithCycles_EdgesInCyclesAreRed
  - [x] Add test: GenerateAsync_WithCycles_EdgesNotInCyclesAreBlack
  - [x] Add test: GenerateAsync_WithMultipleCycles_AllCyclicEdgesAreRed
  - [x] Add test: GenerateAsync_WithCycles_LegendIncludesCircularDependencies
  - [x] Add test: GenerateAsync_NullCycles_NoRedEdges (backward compatibility)
  - [x] Add test: GenerateAsync_EmptyCycles_NoRedEdges
  - [x] Add test: GenerateAsync_CyclesAndCrossSolution_CyclesTakePriority
  - [x] Verify legend content includes "Red: Circular Dependencies"

- [x] Update DI registration and integration points (AC: Service integration)
  - [x] No changes needed to DI registration (interface compatible)
  - [x] Update CLI usage to pass cycle information from TarjanCycleDetector
  - [x] Integration: After cycle detection, pass cycles to DotGenerator.GenerateAsync
  - [x] Expected workflow: DetectCycles → GenerateAsync with cycles → Render
  - [x] Document integration pattern in XML comments

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Interface Extension Pattern:**

**From Epic 3 Story 3.6 Requirements:**

The IDotGenerator interface must be extended to accept cycle information while maintaining backward compatibility:

```csharp
public interface IDotGenerator
{
    /// <summary>
    /// Generates a Graphviz DOT file from a dependency graph.
    /// Creates a directed graph with nodes representing projects and edges representing dependencies.
    /// Circular dependencies are highlighted in RED when cycle information is provided.
    /// Cross-solution dependencies are color-coded for visual distinction.
    /// </summary>
    /// <param name="graph">The dependency graph to visualize.</param>
    /// <param name="outputDirectory">Directory where the .dot file will be written.</param>
    /// <param name="solutionName">Name of the solution (used for filename generation).</param>
    /// <param name="cycles">Optional list of detected circular dependencies for highlighting.
    /// When provided, edges within cycles are rendered in RED.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Absolute path to the generated .dot file.</returns>
    /// <exception cref="ArgumentNullException">When graph, outputDirectory, or solutionName is null.</exception>
    /// <exception cref="ArgumentException">When outputDirectory or solutionName is empty.</exception>
    /// <exception cref="DotGenerationException">When DOT file generation or writing fails.</exception>
    Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        IReadOnlyList<CycleInfo>? cycles = null,
        CancellationToken cancellationToken = default);
}
```

**Why This Design:**
- Optional parameter (defaults to null) maintains backward compatibility
- Existing callers continue to work without changes
- New callers can pass cycle information for highlighting
- Clear documentation of cycle highlighting behavior
- CycleInfo from Story 3.1 provides all needed cycle data

🚨 **CRITICAL - Cyclic Edge Identification Algorithm:**

**From Epic 3 Story 3.6 Acceptance Criteria:**

"Edges that are part of circular dependencies are rendered in RED"

To identify which edges are cyclic:

```csharp
private HashSet<(string source, string target)> BuildCyclicEdgeSet(
    IReadOnlyList<CycleInfo> cycles,
    DependencyGraph graph)
{
    var cyclicEdges = new HashSet<(string, string)>();

    foreach (var cycle in cycles)
    {
        // Create set of project names in this cycle for O(1) lookup
        var projectsInCycle = new HashSet<string>(
            cycle.Projects.Select(p => p.ProjectName),
            StringComparer.OrdinalIgnoreCase);

        // Find all edges where both source and target are in this cycle
        foreach (var edge in graph.Edges)
        {
            if (projectsInCycle.Contains(edge.Source.ProjectName) &&
                projectsInCycle.Contains(edge.Target.ProjectName))
            {
                cyclicEdges.Add((edge.Source.ProjectName, edge.Target.ProjectName));
            }
        }
    }

    _logger.LogDebug(
        "Identified {CyclicEdgeCount} cyclic edges across {CycleCount} cycles",
        cyclicEdges.Count,
        cycles.Count);

    return cyclicEdges;
}
```

**Algorithm Complexity:**
- Time: O(C * E) where C = cycles, E = edges (acceptable for typical graphs)
- Space: O(cyclic edges) for HashSet (minimal)
- Per-cycle: O(P + E) where P = projects in cycle
- HashSet lookup during edge generation: O(1)

**Key Points:**
- An edge is cyclic if BOTH source AND target are in the SAME cycle
- Use HashSet for O(1) lookup during edge coloring
- Handle multiple cycles correctly (edge could be in multiple cycles, still just RED)
- Case-insensitive project name comparison (consistent with existing code)

🚨 **CRITICAL - Edge Color Priority Rules:**

**From Story 3.6 Requirements and Existing Implementation:**

The current DotGenerator uses RED for cross-solution dependencies. Story 3.6 requires RED for cycles. This creates a conflict that must be resolved:

**Design Decision: Cycles Take Priority**

Color priority (highest to lowest):
1. **Cycles: RED** (Story 3.6 - THIS STORY)
2. **Cross-solution: BLUE** (Changed from RED to avoid conflict)
3. **Default: BLACK** (Intra-solution, non-cyclic)

**Edge Coloring Logic:**

```csharp
// In BuildDotContent, during edge generation loop:
var sourceEscaped = EscapeDotIdentifier(edge.Source.ProjectName);
var targetEscaped = EscapeDotIdentifier(edge.Target.ProjectName);

// Check if edge is cyclic (highest priority)
if (cyclicEdges.Contains((edge.Source.ProjectName, edge.Target.ProjectName)))
{
    builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"red\", style=\"bold\"];");
    cyclicEdgeCount++;
}
// Check if edge is cross-solution (medium priority)
else if (edge.IsCrossSolution)
{
    builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"blue\", style=\"bold\"];");
    crossSolutionCount++;
}
// Default: intra-solution, non-cyclic
else
{
    builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"black\"];");
}
```

**Rationale:**
- Cycles are PRIMARY architectural concern (Epic 3 focus)
- Cross-solution dependencies moved to BLUE (still visually distinct)
- If edge is both cyclic AND cross-solution, RED wins (cycle concern > cross-solution concern)
- Story 3.7 will add YELLOW for break suggestions (overrides RED)

🚨 **CRITICAL - Legend Updates:**

**From Story 3.6 Acceptance Criteria:**

"Graph legend includes 'Red: Circular Dependencies' notation"

Legend must be updated to reflect new color scheme:

```csharp
// Add cycle legend when cycles are provided and detected
if (cycles != null && cycles.Count > 0 && cyclicEdgeCount > 0)
{
    builder.AppendLine();
    builder.AppendLine("    // Legend - Circular Dependencies");
    builder.AppendLine("    subgraph cluster_cycle_legend {");
    builder.AppendLine("        label=\"Dependency Types\";");
    builder.AppendLine("        style=dashed;");
    builder.AppendLine("        color=gray;");
    builder.AppendLine();
    builder.AppendLine("        legend_cycle [label=\"Red: Circular Dependencies\", color=\"red\", style=\"bold\"];");

    if (crossSolutionCount > 0)
    {
        builder.AppendLine("        legend_cross [label=\"Blue: Cross-Solution\", color=\"blue\", style=\"bold\"];");
    }

    builder.AppendLine("        legend_default [label=\"Black: Normal Dependencies\", color=\"black\"];");
    builder.AppendLine("    }");
}
```

**Legend Design:**
- Only show legend when cycles exist and cyclic edges were found
- Include all active edge types in legend (cycles, cross-solution, default)
- Use subgraph cluster for visual grouping
- Match edge styling (bold for emphasized types)

🚨 **CRITICAL - Backward Compatibility:**

**From Interface Design Requirements:**

All existing DotGenerator callers must continue to work without changes:

```csharp
// Old usage (still works - cycles defaults to null)
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName);

// New usage (with cycle highlighting)
var cycles = await cycleDetector.DetectCyclesAsync(graph);
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName, cycles);
```

**Backward Compatibility Tests:**
- Existing tests pass null for cycles parameter (explicitly or via default)
- No cycle highlighting when cycles is null or empty
- Cross-solution highlighting still works (now BLUE instead of RED)
- Legend behavior unchanged when no cycles provided

### Technical Requirements

**DOT Format Syntax:**

From Graphviz documentation (2026):
- Edge color attribute: `[color="colorname"]`
- Valid color names: "red", "blue", "black", "gray", etc.
- Edge style attribute: `[style="bold"]` for emphasis
- Combined attributes: `[color="red", style="bold"]`
- Legend: Use subgraph cluster with dashed border

**Edge Color Attributes:**
- RED: `color="red", style="bold"` (circular dependencies)
- BLUE: `color="blue", style="bold"` (cross-solution dependencies)
- BLACK: `color="black"` (normal intra-solution dependencies)

**HashSet Performance:**
- HashSet<(string, string)> for edge lookup: O(1) lookup, O(n) space
- Tuple comparison: Value equality, case-sensitive by default
- Use StringComparer.OrdinalIgnoreCase for project names (consistent with existing code)

**Algorithm Correctness:**

Edge is cyclic if:
1. Both source AND target are in the same cycle's project list
2. OR edge appears in multiple cycles (still just colored once)

Edge is NOT cyclic if:
- Only source OR only target is in a cycle (broken dependency, not cyclic)
- Neither source nor target is in any cycle

**Multi-Cycle Handling:**
- An edge could theoretically be in multiple cycles (complex dependency structures)
- Color it RED once, don't duplicate
- HashSet naturally handles deduplication

### Architecture Compliance

**Epic 3 Architecture Requirements:**

```
- TarjanCycleDetector using QuikGraph's Tarjan's SCC algorithm ✅ (Story 3.1)
- Cycle statistics calculation ✅ (Story 3.2)
- CouplingAnalyzer for method call counting ✅ (Story 3.3)
- WeakEdgeIdentifier for finding weakest edges ✅ (Story 3.4)
- Ranked cycle-breaking recommendations ✅ (Story 3.5)
- Enhanced DOT visualization with cycle highlighting ⏳ (Story 3.6 - THIS STORY)
- Mark suggested break points in YELLOW ⏳ (Story 3.7)
```

**Story 3.6 Implements:**
- ✅ IDotGenerator interface extension with optional cycles parameter
- ✅ Cyclic edge identification algorithm
- ✅ RED edge coloring for circular dependencies
- ✅ Legend entry for "Red: Circular Dependencies"
- ✅ Backward compatibility with existing callers
- ✅ Cross-solution color change from RED to BLUE (avoid conflict)
- ✅ Structured logging for cycle highlighting progress

**Integration with Existing Components:**

Story 3.6 extends:
- **DotGenerator** (from Story 2-8): Enhanced with cycle highlighting capability
- Uses DependencyGraph (Epic 2), CycleInfo (Story 3.1)

Story 3.6 produces:
- **Enhanced DOT files**: With RED edges for circular dependencies
- **Updated visualizations**: Clear visual identification of problematic cycles
- **Updated legends**: Documenting edge color meanings

**Namespace Organization:**

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
│   ├── DependencyGraph.cs           # Reused as-is
│   ├── DependencyEdge.cs            # Reused as-is
│   └── ProjectNode.cs               # Reused as-is
├── CycleAnalysis/               # Epic 3: Cycle detection
│   └── CycleInfo.cs                 # Story 3.1 - Consumed by 3.6
└── Visualization/               # Epic 2: DOT generation
    ├── IDotGenerator.cs             # MODIFIED: Add cycles parameter
    ├── DotGenerator.cs              # MODIFIED: Cycle highlighting logic
    └── DotGenerationException.cs    # Reused as-is
```

**DI Integration:**
```csharp
// No changes to DI registration - interface compatible
services.TryAddSingleton<IDotGenerator, DotGenerator>();

// CLI usage pattern changes
var cycles = await cycleDetector.DetectCyclesAsync(graph);          // Story 3.1
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName, cycles);  // Story 3.6 (NEW)
await graphvizRenderer.RenderToFileAsync(dotPath, GraphvizOutputFormat.PNG); // Existing
```

### Library/Framework Requirements

**No New NuGet Packages Required:**

All dependencies already satisfied:
- ✅ Microsoft.Extensions.Logging.Abstractions (installed in Story 1-6)
- ✅ System.Text (built-in for StringBuilder)
- ✅ System.IO (built-in for file operations)

**Graphviz DOT Format:**
- Version: Compatible with Graphviz 2.38+ (NFR requirement)
- Color syntax: Standard DOT format (`color="colorname"`)
- Style syntax: `style="bold"` for emphasized edges
- Legend syntax: `subgraph cluster_name` for grouped legend items

**C# Language Features Used:**
- Tuples: `(string source, string target)` for edge keys
- HashSet: O(1) lookup for cyclic edge membership
- Optional parameters: `cycles = null` for backward compatibility
- String interpolation: For DOT content generation (user-facing, not logging)

**Existing DotGenerator Patterns to Maintain:**
- StringBuilder for DOT content building (performance)
- EscapeDotIdentifier() for safe project name escaping
- File.WriteAllTextAsync() for async file writing
- ConfigureAwait(false) for library code
- Structured logging with named placeholders

### File Structure Requirements

**Files to Modify:**

```
src/MasDependencyMap.Core/Visualization/
├── IDotGenerator.cs                 # MODIFY: Add cycles parameter to GenerateAsync
└── DotGenerator.cs                  # MODIFY: Implement cycle highlighting logic

tests/MasDependencyMap.Core.Tests/Visualization/
└── DotGeneratorTests.cs             # MODIFY: Update existing tests, add new cycle tests
```

**No New Files Created:**

Story 3.6 enhances existing visualization infrastructure. No new classes or interfaces needed.

**Integration Points:**

```
src/MasDependencyMap.CLI/Program.cs or Commands/AnalyzeCommand.cs
  - Update DOT generation call to pass cycle information
  - Integration: var cycles = await cycleDetector.DetectCyclesAsync(graph);
  -              await dotGenerator.GenerateAsync(graph, outputDir, name, cycles);
```

### Testing Requirements

**Test Class: DotGeneratorTests.cs**

Update existing test class with new cycle highlighting tests:

```csharp
namespace MasDependencyMap.Core.Tests.Visualization;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.Visualization;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.CycleAnalysis;

public class DotGeneratorTests
{
    private readonly ILogger<DotGenerator> _logger;
    private readonly DotGenerator _generator;

    public DotGeneratorTests()
    {
        _logger = NullLogger<DotGenerator>.Instance;
        _generator = new DotGenerator(_logger);
    }

    // ========== NEW TESTS FOR STORY 3.6 ==========

    [Fact]
    public async Task GenerateAsync_WithCycles_EdgesInCyclesAreRed()
    {
        // Arrange
        var graph = CreateGraphWithCycle(); // ProjectA -> ProjectB -> ProjectC -> ProjectA
        var cycles = new List<CycleInfo>
        {
            new CycleInfo(1, graph.Vertices.ToList())
        };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().Contain("color=\"red\"");
        dotContent.Should().Contain("ProjectA\" -> \"ProjectB\" [color=\"red\", style=\"bold\"]");
        dotContent.Should().Contain("ProjectB\" -> \"ProjectC\" [color=\"red\", style=\"bold\"]");
        dotContent.Should().Contain("ProjectC\" -> \"ProjectA\" [color=\"red\", style=\"bold\"]");
    }

    [Fact]
    public async Task GenerateAsync_WithCycles_EdgesNotInCyclesAreBlack()
    {
        // Arrange
        var graph = CreateGraphWithCycleAndNonCyclicEdge(); // A->B->A (cycle), C->D (not cyclic)
        var cycleProjects = new List<ProjectNode> { graph.Vertices.ElementAt(0), graph.Vertices.ElementAt(1) };
        var cycles = new List<CycleInfo> { new CycleInfo(1, cycleProjects) };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().Contain("ProjectC\" -> \"ProjectD\" [color=\"black\"]");
        dotContent.Should().NotContain("ProjectC\" -> \"ProjectD\" [color=\"red\"");
    }

    [Fact]
    public async Task GenerateAsync_WithMultipleCycles_AllCyclicEdgesAreRed()
    {
        // Arrange
        var graph = CreateGraphWithMultipleCycles(); // Cycle 1: A->B->A, Cycle 2: C->D->C
        var cycle1 = new CycleInfo(1, new[] { graph.Vertices.ElementAt(0), graph.Vertices.ElementAt(1) });
        var cycle2 = new CycleInfo(2, new[] { graph.Vertices.ElementAt(2), graph.Vertices.ElementAt(3) });
        var cycles = new List<CycleInfo> { cycle1, cycle2 };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().Contain("ProjectA\" -> \"ProjectB\" [color=\"red\", style=\"bold\"]");
        dotContent.Should().Contain("ProjectB\" -> \"ProjectA\" [color=\"red\", style=\"bold\"]");
        dotContent.Should().Contain("ProjectC\" -> \"ProjectD\" [color=\"red\", style=\"bold\"]");
        dotContent.Should().Contain("ProjectD\" -> \"ProjectC\" [color=\"red\", style=\"bold\"]");
    }

    [Fact]
    public async Task GenerateAsync_WithCycles_LegendIncludesCircularDependencies()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().Contain("Red: Circular Dependencies");
        dotContent.Should().Contain("subgraph cluster_cycle_legend");
    }

    [Fact]
    public async Task GenerateAsync_NullCycles_NoRedEdgesBackwardCompatible()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles: null);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().NotContain("color=\"red\""); // No cycle highlighting when cycles is null
        dotContent.Should().NotContain("Circular Dependencies"); // No cycle legend
    }

    [Fact]
    public async Task GenerateAsync_EmptyCycles_NoRedEdges()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo>(); // Empty list
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().NotContain("color=\"red\"");
        dotContent.Should().NotContain("Circular Dependencies");
    }

    [Fact]
    public async Task GenerateAsync_CyclesAndCrossSolution_CyclesTakePriority()
    {
        // Arrange
        var graph = CreateGraphWithCyclicCrossSolutionEdge(); // A->B (cycle + cross-solution)
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().Contain("ProjectA\" -> \"ProjectB\" [color=\"red\""); // Cycle wins over cross-solution
        dotContent.Should().NotContain("ProjectA\" -> \"ProjectB\" [color=\"blue\"");
    }

    // ========== EXISTING TESTS (Updated to pass null for cycles) ==========

    [Fact]
    public async Task GenerateAsync_ValidGraph_CreatesDotFile()
    {
        // Existing test updated to explicitly pass cycles: null
        var graph = CreateSimpleGraph();
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles: null);

        File.Exists(dotPath).Should().BeTrue();
        dotPath.Should().EndWith(".dot");
    }

    // ... other existing tests updated similarly ...

    // ========== Helper Methods ==========

    private DependencyGraph CreateGraphWithCycle()
    {
        // ProjectA -> ProjectB -> ProjectC -> ProjectA (3-node cycle)
        var graph = new DependencyGraph();
        var projectA = new ProjectNode("ProjectA", "ProjectA.csproj") { SolutionName = "Solution1" };
        var projectB = new ProjectNode("ProjectB", "ProjectB.csproj") { SolutionName = "Solution1" };
        var projectC = new ProjectNode("ProjectC", "ProjectC.csproj") { SolutionName = "Solution1" };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);

        graph.AddEdge(new DependencyEdge(projectA, projectB));
        graph.AddEdge(new DependencyEdge(projectB, projectC));
        graph.AddEdge(new DependencyEdge(projectC, projectA));

        return graph;
    }

    private DependencyGraph CreateGraphWithCycleAndNonCyclicEdge()
    {
        // A->B->A (cycle), C->D (not cyclic)
        var graph = new DependencyGraph();
        var projectA = new ProjectNode("ProjectA", "ProjectA.csproj") { SolutionName = "Solution1" };
        var projectB = new ProjectNode("ProjectB", "ProjectB.csproj") { SolutionName = "Solution1" };
        var projectC = new ProjectNode("ProjectC", "ProjectC.csproj") { SolutionName = "Solution1" };
        var projectD = new ProjectNode("ProjectD", "ProjectD.csproj") { SolutionName = "Solution1" };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);

        graph.AddEdge(new DependencyEdge(projectA, projectB));
        graph.AddEdge(new DependencyEdge(projectB, projectA));
        graph.AddEdge(new DependencyEdge(projectC, projectD));

        return graph;
    }

    private DependencyGraph CreateGraphWithMultipleCycles()
    {
        // Cycle 1: A->B->A, Cycle 2: C->D->C
        var graph = new DependencyGraph();
        var projectA = new ProjectNode("ProjectA", "ProjectA.csproj") { SolutionName = "Solution1" };
        var projectB = new ProjectNode("ProjectB", "ProjectB.csproj") { SolutionName = "Solution1" };
        var projectC = new ProjectNode("ProjectC", "ProjectC.csproj") { SolutionName = "Solution1" };
        var projectD = new ProjectNode("ProjectD", "ProjectD.csproj") { SolutionName = "Solution1" };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);

        graph.AddEdge(new DependencyEdge(projectA, projectB));
        graph.AddEdge(new DependencyEdge(projectB, projectA));
        graph.AddEdge(new DependencyEdge(projectC, projectD));
        graph.AddEdge(new DependencyEdge(projectD, projectC));

        return graph;
    }

    private DependencyGraph CreateGraphWithCyclicCrossSolutionEdge()
    {
        // A->B (both in cycle AND cross-solution)
        var graph = new DependencyGraph();
        var projectA = new ProjectNode("ProjectA", "ProjectA.csproj") { SolutionName = "Solution1" };
        var projectB = new ProjectNode("ProjectB", "ProjectB.csproj") { SolutionName = "Solution2" }; // Different solution

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);

        graph.AddEdge(new DependencyEdge(projectA, projectB) { IsCrossSolution = true });
        graph.AddEdge(new DependencyEdge(projectB, projectA) { IsCrossSolution = true });

        return graph;
    }
}
```

**Test Naming Convention:**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `GenerateAsync_WithCycles_EdgesInCyclesAreRed()`
- ✅ `GenerateAsync_WithCycles_EdgesNotInCyclesAreBlack()`
- ✅ `GenerateAsync_WithMultipleCycles_AllCyclicEdgesAreRed()`
- ✅ `GenerateAsync_NullCycles_NoRedEdgesBackwardCompatible()`

**Test Coverage:**
- 8 new tests for Story 3.6 cycle highlighting
- All existing tests updated to pass `cycles: null` (backward compatibility)
- Edge cases: null cycles, empty cycles, multi-cycle, mixed cyclic/non-cyclic
- Priority testing: cycles override cross-solution coloring
- Legend testing: verify legend content when cycles present

### Previous Story Intelligence

**From Story 3-5 (Generate Ranked Cycle-Breaking Recommendations) - Key Learnings:**

Story 3-5 completed cycle analysis pipeline. Story 3.6 visualizes the cycle data:

1. **CycleInfo Structure** (from 3-1, extended in 3-4):
   - CycleInfo contains Projects list
   - Use Projects list to identify cyclic edges
   - Multiple cycles possible in same graph

2. **Integration Pattern:**
   ```csharp
   // Expected workflow from CLI
   var cycles = await cycleDetector.DetectCyclesAsync(graph);                             // Story 3.1
   var statistics = await statsCalculator.CalculateAsync(cycles, graph);                  // Story 3.2
   var annotatedGraph = await couplingAnalyzer.AnalyzeAsync(graph, solution);             // Story 3.3
   var cyclesWithWeakEdges = weakEdgeIdentifier.IdentifyWeakEdges(cycles, annotatedGraph); // Story 3.4
   var recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cyclesWithWeakEdges); // Story 3.5
   var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, name, cycles);        // Story 3.6 (NEW)
   ```

3. **Patterns to Reuse:**
   ```csharp
   // Argument validation
   ArgumentNullException.ThrowIfNull(graph);

   // Structured logging with named placeholders
   _logger.LogDebug(
       "Identified {CyclicEdgeCount} cyclic edges across {CycleCount} cycles",
       cyclicEdges.Count,
       cycles.Count);

   // Optional parameter for backward compatibility
   IReadOnlyList<CycleInfo>? cycles = null
   ```

4. **Code Style from 3-5:**
   - File-scoped namespaces
   - XML documentation for all public APIs
   - Structured logging (Debug for detailed progress, Information for key milestones)
   - Defensive null checks
   - ConfigureAwait(false) for async library code

**From Story 2-8 (Generate DOT Format) - Existing DotGenerator Patterns:**

Story 3.6 extends existing DotGenerator:

1. **StringBuilder Pattern:**
   - Estimate capacity upfront for performance
   - Build DOT content incrementally
   - Use AppendLine for formatted output

2. **EscapeDotIdentifier:**
   - Always quote identifiers for safety
   - Escape backslashes and quotes
   - Reuse this for all project names

3. **Color Scheme:**
   - Current: RED for cross-solution (CONFLICT with cycles)
   - Change cross-solution to BLUE in Story 3.6
   - Maintain bold style for emphasized edges

4. **Legend Pattern:**
   - Use subgraph cluster for legend grouping
   - Conditional legend (only show when needed)
   - Clear labeling with color indicators

**Expected Integration in CLI (Story 3.6 Changes):**

```csharp
// Before Story 3.6 (no cycle highlighting)
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName);

// After Story 3.6 (with cycle highlighting)
var cycles = await cycleDetector.DetectCyclesAsync(graph);
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName, cycles);
```

### Git Intelligence Summary

**Recent Commits Analysis:**

Last 5 commits show Epic 3 story pattern:
1. **f8bd5be:** Code review fixes for Story 3-5
2. **7249b33:** Story 3-5 complete
3. **ecb8f93:** Code review fixes for Story 3-4
4. **f934770:** Code review fixes for Story 3-2
5. **005b452:** Code review fixes for Story 3-3

**Pattern Observed:**
- Initial "Story X.Y complete" commit
- Follow-up "Code review fixes for Story X.Y" commit
- Suggests rigorous code review process after implementation

**Commit Message Pattern:**

Story completion commits include:
- Story number and title
- List of changes made
- Acceptance criteria satisfied
- Test coverage summary
- Co-Authored-By: Claude Sonnet 4.5

**Expected Commit Message for Story 3.6:**

```bash
git commit -m "Story 3-6 complete: Enhance DOT visualization with cycle highlighting

- Extended IDotGenerator interface with optional cycles parameter for backward compatibility
- Added BuildCyclicEdgeSet helper method for O(1) edge lookup using HashSet<(string, string)>
- Modified BuildDotContent to accept and use cycle information
- Implemented edge color priority: Cycles (RED) > Cross-solution (BLUE) > Default (BLACK)
- Changed cross-solution edge color from RED to BLUE to avoid conflict with cycle highlighting
- Added legend entry for \"Red: Circular Dependencies\" when cycles are present
- Updated cross-solution legend to reflect new BLUE color
- Added 8 comprehensive unit tests for cycle highlighting (all passing)
- Updated existing tests to pass cycles: null for backward compatibility
- Handles edge cases: null cycles, empty cycles, multi-cycle graphs
- All acceptance criteria satisfied
- Backward compatible: existing callers work without changes

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

**Files Expected to Change in Story 3.6:**

- Modified: IDotGenerator.cs (interface extension)
- Modified: DotGenerator.cs (cycle highlighting implementation)
- Modified: DotGeneratorTests.cs (new tests + updated existing tests)
- Modified: sprint-status.yaml (story status update)
- Modified: CLI integration file (pass cycles to DotGenerator)

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 3.6 enhances Epic 2 visualization infrastructure:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
│   ├── DependencyGraph.cs           # Reused as-is
│   ├── DependencyEdge.cs            # Reused as-is
│   └── ProjectNode.cs               # Reused as-is
├── CycleAnalysis/               # Epic 3: Cycle detection
│   └── CycleInfo.cs                 # Story 3.1 - Consumed by 3.6
└── Visualization/               # Epic 2: DOT generation (ENHANCED)
    ├── IDotGenerator.cs             # MODIFIED: Add cycles parameter
    ├── DotGenerator.cs              # MODIFIED: Cycle highlighting logic
    └── DotGenerationException.cs    # Reused as-is
```

**Consistency with Existing Patterns:**
- Feature-based namespace: `MasDependencyMap.Core.Visualization` ✅
- Interface + Implementation pattern: `IDotGenerator`, `DotGenerator` ✅
- File naming matches class naming exactly ✅
- Test namespace mirrors Core: `tests/MasDependencyMap.Core.Tests/Visualization` ✅
- Service pattern with ILogger injection ✅
- Singleton DI registration (no changes needed) ✅

**Cross-Namespace Dependencies:**
- Visualization → DependencyAnalysis (uses DependencyGraph, DependencyEdge, ProjectNode) ✅
- Visualization → CycleAnalysis (uses CycleInfo from Story 3.1) ✅ NEW
- This cross-namespace usage is expected and acceptable (Epic 3 builds on Epic 2)

**Interface Evolution Pattern:**
- Optional parameter added to existing interface method
- Maintains backward compatibility (existing callers unaffected)
- New callers can leverage new functionality
- Follows C# optional parameter best practices

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-3-circular-dependency-detection-and-break-point-analysis.md, Story 3.6 (lines 95-110)]
- Story requirements: Highlight cycles in RED, clear visual distinction, legend entry

**Previous Stories:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-5-generate-ranked-cycle-breaking-recommendations.md (full file)]
- Cycle analysis pipeline, CycleInfo usage, integration patterns
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-1-implement-tarjans-scc-algorithm-for-cycle-detection.md]
- CycleInfo model creation, cycle detection integration
- [Source: D:\work\masDependencyMap\src\MasDependencyMap.Core\Visualization\DotGenerator.cs:1-208]
- Existing DOT generator implementation, current color scheme, legend pattern

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Structured Logging (lines 115-119)]
- Named placeholders, no string interpolation
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, File-Scoped Namespaces (lines 76-79)]
- Use file-scoped namespace declarations (C# 10+)
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md, DOT File Generation (lines 134-155)]
- QuikGraph.Graphviz extension, color coding strategy, custom formatters

**Technology Stack:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Core Technologies (lines 20-26)]
- .NET 8.0, C# 12
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, External Tools (lines 39-41)]
- Graphviz 2.38+ (external process)

**Graphviz DOT Format:**
- [Source: https://graphviz.org/doc/info/attrs.html]
- Edge color attribute syntax, valid color names
- [Source: https://graphviz.org/docs/attrs/color/]
- Color attribute reference, style combinations
- [Source: https://graphviz.org/docs/edges/]
- Edge attribute specifications, style options

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

claude-sonnet-4-5-20250929 (Claude Sonnet 4.5)

### Debug Log References

N/A - Implementation completed successfully without issues

### Code Review Fixes Applied

**Code Review Date:** 2026-01-24
**Reviewer:** Claude Sonnet 4.5 (Adversarial Review Mode)

**Issues Fixed:**

1. ✅ **RETRACTED - Issue #1 (IsCrossSolution):** Initially flagged as bug, but investigation confirmed IsCrossSolution is correctly implemented as computed property (DependencyEdge.cs:50-53). No fix needed.

2. ✅ **Fixed - Issue #2 (Weak Test Assertions):** Enhanced GenerateAsync_WithCycles_EdgesInCyclesAreRed test to check specific edges instead of just presence of red color. Test now validates all three edges in the cycle are red.

3. ✅ **Fixed - Issue #3 (Story File Not Tracked):** Added story file to git staging area.

4. ✅ **Fixed - Issue #4 (Task Audit):** Resolved by fixing Issue #2 - test now properly validates AC.

5. ✅ **Fixed - Issue #5 (Dead Code):** Removed unused CrossSolutionEdgeColors array from DotGenerator.cs:19.

6. ✅ **Fixed - Issue #6 (Missing Test):** Added GenerateAsync_CyclesWithNoMatchingEdges_NoRedEdges test to verify behavior when cycle projects don't match graph edges.

7. ✅ **Fixed - Issue #7 (Performance):** Optimized BuildCyclicEdgeSet to pre-build cycle project sets and iterate edges once instead of per-cycle iteration. Better cache locality and performance.

**Issues Deferred (Low Priority):**

8. 🟡 **Issue #8 (Legend Styling):** Minor inconsistency between solution legend and cycle legend attributes. Not blocking, deferred for future UX improvements.

9. 🟡 **Issue #9 (Logging Levels):** Cycle highlighting uses LogDebug instead of LogInformation. Not blocking, deferred for future logging improvements.

**Test Results After Fixes:** 27/27 tests passing (added 1 new test)

### Completion Notes List

✅ **Interface Extension (Task 1):**
- Extended IDotGenerator.GenerateAsync() with optional `IReadOnlyList<CycleInfo>? cycles = null` parameter
- Added comprehensive XML documentation explaining cycle highlighting behavior
- Maintained 100% backward compatibility with existing callers

✅ **Cycle Edge Set Building (Task 2):**
- Implemented BuildCyclicEdgeSet() private helper method
- Uses HashSet<(string, string)> for O(1) edge lookup during DOT generation
- Handles empty/null cycles gracefully
- Added structured logging: "Identified {CyclicEdgeCount} cyclic edges across {CycleCount} cycles"

✅ **Edge Coloring Logic (Tasks 3-4):**
- Updated BuildDotContent() to accept cycles parameter
- Implemented color priority: Cycles (RED) > Cross-solution (BLUE) > Default (BLACK)
- Changed cross-solution edge color from RED to BLUE to avoid conflict with cycle highlighting
- Applied bold style to both cycles and cross-solution edges for visual distinction
- Added debug logging for both cycle and cross-solution highlighting

✅ **Legend Updates (Tasks 5-6):**
- Added "Red: Circular Dependencies" legend entry when cycles are detected
- Updated cross-solution legend to "Blue: Cross-Solution Dependencies"
- Legend only appears when cycles exist and cyclic edges were found
- Used subgraph cluster for proper Graphviz legend organization

✅ **Edge Cases and Robustness (Task 7):**
- Null cycles parameter → no cycle highlighting (backward compatible)
- Empty cycles list → no cycle highlighting
- Multi-cycle graphs → all cyclic edges highlighted in RED
- Case-insensitive project name comparison (consistent with existing code)

✅ **Test Coverage (Task 8):**
- Added 9 comprehensive unit tests for cycle highlighting (added 1 during code review)
- Updated CancelledToken test to include cycles parameter
- All 27 DotGenerator tests passing (increased from 26 after code review fixes)
- Full test suite: 253/253 tests passing (no regressions)
- Test coverage includes: basic cycles, multiple cycles, mixed cyclic/non-cyclic edges, legend content, backward compatibility, edge priority, cycles with no matching edges

✅ **CLI Integration (Task 9):**
- Updated Program.cs line 391 to pass cycles to DotGenerator.GenerateAsync()
- No DI registration changes needed (interface compatible)
- Integration workflow: DetectCycles → GenerateAsync with cycles → Render
- CLI builds successfully without warnings or errors

**Implementation Highlights:**
- Zero breaking changes - existing code continues to work unchanged
- O(1) edge lookup performance using HashSet
- Clear color priority hierarchy prevents ambiguity
- Comprehensive test coverage ensures correctness
- Follows all project patterns from project-context.md (file-scoped namespaces, structured logging, async/await patterns)

### File List

**Files Modified:**
- src/MasDependencyMap.Core/Visualization/IDotGenerator.cs (interface extension)
- src/MasDependencyMap.Core/Visualization/DotGenerator.cs (cycle highlighting implementation)
- tests/MasDependencyMap.Core.Tests/Visualization/DotGeneratorTests.cs (8 new tests + 1 updated test)
- src/MasDependencyMap.CLI/Program.cs (CLI integration line 391)
