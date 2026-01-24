# Story 3.7: Mark Suggested Break Points in YELLOW on Visualizations

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want suggested cycle break points marked in YELLOW on dependency graphs,
So that I can immediately see where to focus my refactoring efforts.

## Acceptance Criteria

**Given** Cycle-breaking recommendations have been generated
**When** DotGenerator.GenerateAsync() is called with recommendations
**Then** Edges identified as break point suggestions are rendered in YELLOW
**And** YELLOW edges represent the weakest coupling within cycles
**And** If an edge is both cyclic (RED) and a break suggestion, it renders as YELLOW (suggestion takes priority)
**And** Graph legend includes "Yellow: Suggested Break Points" notation
**And** Top 10 suggested break points are marked (not all weak edges, to avoid visual clutter)
**And** The visualization clearly guides the architect to specific edges that should be broken

## Tasks / Subtasks

- [x]Extend IDotGenerator interface to accept recommendation information (AC: DotGenerator can receive recommendation data)
  - [x]Add optional `IReadOnlyList<CycleBreakingSuggestion>? recommendations = null` parameter to GenerateAsync method
  - [x]Update XML documentation to describe break point highlighting behavior
  - [x]Maintain backward compatibility (recommendations parameter is optional, defaults to null)
  - [x]Document that when recommendations is null or empty, no break point highlighting is applied
  - [x]Document that YELLOW takes priority over RED (break suggestion > cycle membership)

- [x]Create helper method to build break point edge set (AC: Fast edge lookup for break point membership)
  - [x]Create private method `BuildBreakPointEdgeSet(IReadOnlyList<CycleBreakingSuggestion> recommendations, int maxSuggestions = 10)`
  - [x]Return HashSet<(string source, string target)> for O(1) edge lookup
  - [x]Take top N recommendations (default 10) to avoid visual clutter
  - [x]Extract source project name and target project name from each CycleBreakingSuggestion
  - [x]Use tuple of (source.ProjectName, target.ProjectName) as hash key
  - [x]Handle empty recommendations list gracefully (return empty HashSet)
  - [x]Log Debug message: "Identified {BreakPointCount} break point edges from {TotalRecommendations} recommendations (top {MaxSuggestions})"

- [x]Modify BuildDotContent to accept and use recommendation information (AC: Edge coloring logic with YELLOW priority)
  - [x]Add recommendations parameter to BuildDotContent method signature
  - [x]Build break point edge set at start of method using helper
  - [x]Check each edge against break point edge set during edge generation
  - [x]Color priority: Break Points (YELLOW) > Cycles (RED) > Cross-solution (BLUE) > Default (BLACK)
  - [x]Track count of break point edges colored for logging

- [x]Update edge coloring logic with break point priority (AC: YELLOW for break points, overrides RED)
  - [x]If edge is in break point edge set → color="yellow", style="bold" (HIGHEST PRIORITY)
  - [x]Else if edge is in cyclic edge set → color="red", style="bold"
  - [x]Else if edge is cross-solution → color="blue", style="bold"
  - [x]Else (intra-solution, non-cyclic) → color="black" (default)
  - [x]Log Debug: "Applied break point highlighting: {BreakPointCount} edges marked in yellow"
  - [x]Ensure YELLOW clearly overrides RED for edges that are both cyclic and suggested break points

- [x]Add legend entry for suggested break points (AC: "Yellow: Suggested Break Points" notation)
  - [x]Check if recommendations were provided and have content
  - [x]Add legend section for break point coloring when recommendations exist
  - [x]Legend entry: "Yellow: Suggested Break Points" or "Yellow: Suggested Break Points (Top N)"
  - [x]Position in legend: Break Points > Cycles > Cross-Solution > Default
  - [x]Use subgraph cluster for legend organization (consistent with existing legend)
  - [x]Example legend node showing YELLOW edge

- [x]Handle edge cases and validation (AC: Robustness)
  - [x]Null recommendations parameter → no break point highlighting (backward compatible)
  - [x]Empty recommendations list → no break point highlighting
  - [x]Recommendations with no edges in graph → log Debug warning, continue
  - [x]Graph with no recommendations but recommendations param provided → no YELLOW edges, valid state
  - [x]More than 10 recommendations → only mark top 10 (highest priority by coupling score)
  - [x]CycleBreakingSuggestion with null/empty source or target → skip with warning log

- [x]Update existing tests and add new break point highlighting tests (AC: Test coverage)
  - [x]Update existing DotGeneratorTests to pass null for recommendations (backward compatibility)
  - [x]Add test: GenerateAsync_WithRecommendations_BreakPointEdgesAreYellow
  - [x]Add test: GenerateAsync_WithRecommendations_EdgesNotInRecommendationsUseDefaultColor
  - [x]Add test: GenerateAsync_WithRecommendationsAndCycles_YellowOverridesRed (CRITICAL: priority test)
  - [x]Add test: GenerateAsync_WithRecommendations_LegendIncludesSuggestedBreakPoints
  - [x]Add test: GenerateAsync_NullRecommendations_NoYellowEdges (backward compatibility)
  - [x]Add test: GenerateAsync_EmptyRecommendations_NoYellowEdges
  - [x]Add test: GenerateAsync_MoreThan10Recommendations_OnlyTop10AreYellow
  - [x]Add test: GenerateAsync_RecommendationsWithNoCycles_YellowOnlyBreakPoints
  - [x]Verify legend content includes "Yellow: Suggested Break Points"

- [x]Update DI registration and integration points (AC: Service integration)
  - [x]No changes needed to DI registration (interface compatible)
  - [x]Update CLI usage to pass recommendations from CycleBreakingRecommendationGenerator
  - [x]Integration: After recommendations generated, pass recommendations to DotGenerator.GenerateAsync
  - [x]Expected workflow: DetectCycles → AnalyzeCoupling → GenerateRecommendations → GenerateAsync with cycles + recommendations → Render
  - [x]Document integration pattern in XML comments

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Interface Extension Pattern:**

**From Epic 3 Story 3.7 Requirements:**

The IDotGenerator interface must be extended to accept cycle-breaking recommendation information while maintaining backward compatibility:

```csharp
public interface IDotGenerator
{
    /// <summary>
    /// Generates a Graphviz DOT file from a dependency graph.
    /// Creates a directed graph with nodes representing projects and edges representing dependencies.
    /// Circular dependencies are highlighted in RED when cycle information is provided.
    /// Suggested break points are highlighted in YELLOW (takes priority over cycle highlighting).
    /// Cross-solution dependencies are color-coded in BLUE for visual distinction.
    /// </summary>
    /// <param name="graph">The dependency graph to visualize.</param>
    /// <param name="outputDirectory">Directory where the .dot file will be written.</param>
    /// <param name="solutionName">Name of the solution (used for filename generation).</param>
    /// <param name="cycles">Optional list of detected circular dependencies for highlighting.
    /// When provided, edges within cycles are rendered in RED.</param>
    /// <param name="recommendations">Optional list of cycle-breaking recommendations for highlighting.
    /// When provided, top 10 suggested break point edges are rendered in YELLOW.
    /// YELLOW takes priority over RED if edge is both cyclic and a break suggestion.</param>
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
        IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
        CancellationToken cancellationToken = default);
}
```

**Why This Design:**
- Optional parameter (defaults to null) maintains backward compatibility
- Existing callers continue to work without changes (Story 3.6 callers pass cycles only)
- New callers can pass both cycles AND recommendations for complete visualization
- Clear documentation of color priority: YELLOW (recommendations) > RED (cycles) > BLUE (cross-solution) > BLACK (default)
- CycleBreakingSuggestion from Story 3.5 provides all needed recommendation data

🚨 **CRITICAL - Break Point Edge Identification Algorithm:**

**From Epic 3 Story 3.7 Acceptance Criteria:**

"Edges identified as break point suggestions are rendered in YELLOW"
"Top 10 suggested break points are marked (not all weak edges, to avoid visual clutter)"

To identify which edges are break points:

```csharp
private HashSet<(string source, string target)> BuildBreakPointEdgeSet(
    IReadOnlyList<CycleBreakingSuggestion> recommendations,
    int maxSuggestions = 10)
{
    var breakPointEdges = new HashSet<(string, string)>(StringComparer.OrdinalIgnoreCase);

    // Take top N recommendations to avoid visual clutter
    var topRecommendations = recommendations
        .OrderBy(r => r.CouplingScore)  // Lowest coupling score first (weakest links)
        .Take(maxSuggestions)
        .ToList();

    foreach (var recommendation in topRecommendations)
    {
        // Extract source and target project names from recommendation
        var sourceProject = recommendation.SourceProject;
        var targetProject = recommendation.TargetProject;

        if (string.IsNullOrWhiteSpace(sourceProject) || string.IsNullOrWhiteSpace(targetProject))
        {
            _logger.LogWarning(
                "Skipping recommendation with null/empty project names: {Source} -> {Target}",
                sourceProject ?? "(null)",
                targetProject ?? "(null)");
            continue;
        }

        breakPointEdges.Add((sourceProject, targetProject));
    }

    _logger.LogDebug(
        "Identified {BreakPointCount} break point edges from {TotalRecommendations} recommendations (top {MaxSuggestions})",
        breakPointEdges.Count,
        recommendations.Count,
        maxSuggestions);

    return breakPointEdges;
}
```

**Algorithm Design:**
- CycleBreakingSuggestion already ranked by coupling score (lowest = weakest = best to break)
- Take top 10 by default (configurable via parameter)
- Use HashSet<(string, string)> for O(1) lookup during edge coloring
- Case-insensitive project name comparison (consistent with existing code)
- Handle null/empty project names defensively with warning log

**Key Points:**
- Recommendations are PRE-SORTED by coupling score (Story 3.5 implementation)
- Only mark top N to prevent visual clutter (AC requirement)
- Use tuple (source, target) matching DependencyEdge structure
- Defensive programming: validate project names before adding to set

🚨 **CRITICAL - Edge Color Priority Rules (UPDATED FOR STORY 3.7):**

**From Story 3.7 Requirements:**

Story 3.6 established: Cycles (RED) > Cross-solution (BLUE) > Default (BLACK)
Story 3.7 adds YELLOW as HIGHEST priority for break point suggestions.

**NEW Color Priority (highest to lowest):**
1. **Break Points: YELLOW** (Story 3.7 - THIS STORY) - HIGHEST PRIORITY
2. **Cycles: RED** (Story 3.6)
3. **Cross-solution: BLUE** (Story 3.6, changed from RED)
4. **Default: BLACK** (Intra-solution, non-cyclic, not a break suggestion)

**Edge Coloring Logic (UPDATED):**

```csharp
// In BuildDotContent, during edge generation loop:
var sourceEscaped = EscapeDotIdentifier(edge.Source.ProjectName);
var targetEscaped = EscapeDotIdentifier(edge.Target.ProjectName);

// Check if edge is a suggested break point (HIGHEST PRIORITY)
if (breakPointEdges.Contains((edge.Source.ProjectName, edge.Target.ProjectName)))
{
    builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"yellow\", style=\"bold\"];");
    breakPointEdgeCount++;
}
// Check if edge is cyclic (medium-high priority)
else if (cyclicEdges.Contains((edge.Source.ProjectName, edge.Target.ProjectName)))
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
// Default: intra-solution, non-cyclic, not a break suggestion
else
{
    builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"black\"];");
}
```

**Rationale:**
- **YELLOW overrides RED**: Break suggestions are PRIMARY action items, more important than just knowing edge is cyclic
- If an edge is both cyclic (RED) AND a break suggestion (YELLOW), show YELLOW (architect needs to know "this is where to act")
- This matches AC requirement: "If an edge is both cyclic (RED) and a break suggestion, it renders as YELLOW (suggestion takes priority)"
- Story 3.6 established RED for cycles, BLUE for cross-solution
- Story 3.7 adds YELLOW as new highest priority

🚨 **CRITICAL - Legend Updates for Story 3.7:**

**From Story 3.7 Acceptance Criteria:**

"Graph legend includes 'Yellow: Suggested Break Points' notation"

Legend must be updated to reflect new color scheme with YELLOW as highest priority:

```csharp
// Add legend when any highlighting is active
if ((cycles != null && cycles.Count > 0 && cyclicEdgeCount > 0) ||
    (recommendations != null && recommendations.Any() && breakPointEdgeCount > 0))
{
    builder.AppendLine();
    builder.AppendLine("    // Legend - Dependency Types");
    builder.AppendLine("    subgraph cluster_dependency_legend {");
    builder.AppendLine("        label=\"Dependency Types\";");
    builder.AppendLine("        style=dashed;");
    builder.AppendLine("        color=gray;");
    builder.AppendLine();

    // Break points (highest priority, show first)
    if (breakPointEdgeCount > 0)
    {
        var topN = Math.Min(breakPointEdgeCount, 10);
        builder.AppendLine($"        legend_breakpoint [label=\"Yellow: Suggested Break Points (Top {topN})\", color=\"yellow\", style=\"bold\"];");
    }

    // Cycles (show when present)
    if (cyclicEdgeCount > 0)
    {
        builder.AppendLine("        legend_cycle [label=\"Red: Circular Dependencies\", color=\"red\", style=\"bold\"];");
    }

    // Cross-solution (show when present)
    if (crossSolutionCount > 0)
    {
        builder.AppendLine("        legend_cross [label=\"Blue: Cross-Solution\", color=\"blue\", style=\"bold\"];");
    }

    // Default (always show as baseline)
    builder.AppendLine("        legend_default [label=\"Black: Normal Dependencies\", color=\"black\"];");

    builder.AppendLine("    }");
}
```

**Legend Design:**
- Show legend when ANY highlighting is active (cycles OR recommendations)
- Order by priority: Break Points (YELLOW) > Cycles (RED) > Cross-Solution (BLUE) > Default (BLACK)
- Include count in break point legend: "Top N" to clarify not all weak edges are shown
- Match edge styling (bold for emphasized types)
- Maintain consistency with Story 3.6 legend structure

🚨 **CRITICAL - Backward Compatibility (Story 3.6 → 3.7):**

**From Interface Design Requirements:**

All existing DotGenerator callers (including Story 3.6 callers) must continue to work without changes:

```csharp
// Story 2-8 usage (still works - cycles and recommendations default to null)
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName);

// Story 3-6 usage (still works - recommendations defaults to null)
var cycles = await cycleDetector.DetectCyclesAsync(graph);
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName, cycles);

// Story 3-7 NEW usage (with full visualization: cycles + recommendations)
var cycles = await cycleDetector.DetectCyclesAsync(graph);
var recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cycles, graph);
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
```

**Backward Compatibility Tests:**
- Existing tests pass null for recommendations parameter (explicitly or via default)
- No break point highlighting when recommendations is null or empty
- Cycle highlighting still works (RED edges) when cycles provided but recommendations is null
- Cross-solution highlighting still works (BLUE edges) in all scenarios
- Legend behavior adapts to what's provided (cycles only, recommendations only, or both)

🚨 **CRITICAL - Top 10 Limit (Avoid Visual Clutter):**

**From Story 3.7 Acceptance Criteria:**

"Top 10 suggested break points are marked (not all weak edges, to avoid visual clutter)"

**Why Only Top 10:**
- Large codebases could have dozens or hundreds of weak edges
- Marking all would create visual noise and defeat the purpose
- Top 10 provides focused, actionable guidance
- CycleBreakingSuggestion is already ranked by coupling score (Story 3.5)
- Weakest 10 edges are the best candidates for breaking cycles

**Implementation:**
```csharp
var topRecommendations = recommendations
    .OrderBy(r => r.CouplingScore)  // Lowest coupling = weakest = best to break
    .Take(10)  // CRITICAL: Only top 10 to avoid clutter
    .ToList();
```

**Configuration Consideration:**
- Default to 10 (matches AC requirement)
- Could make configurable via parameter in future (maxSuggestions parameter already in helper method)
- For MVP: hardcode 10 as per AC

### Technical Requirements

**Graphviz DOT Format - YELLOW Color (2026):**

From latest Graphviz documentation (2026):
- Edge color attribute: `[color="yellow"]` or `[color=yellow]` (both valid)
- Valid color names: "yellow", "gold", "orange" (yellow is standard)
- Edge style attribute: `[style="bold"]` for emphasis
- Combined attributes: `[color="yellow", style="bold"]` or `[color=yellow, style=bold]`
- Reference: https://graphviz.org/docs/edges/, https://graphviz.org/docs/attrs/color/

**Edge Color Attributes (UPDATED FOR STORY 3.7):**
- YELLOW: `color="yellow", style="bold"` (suggested break points - HIGHEST PRIORITY)
- RED: `color="red", style="bold"` (circular dependencies)
- BLUE: `color="blue", style="bold"` (cross-solution dependencies)
- BLACK: `color="black"` (normal intra-solution dependencies)

**HashSet Performance:**
- HashSet<(string, string)> for break point edge lookup: O(1) lookup, O(n) space
- Tuple comparison: Value equality with StringComparer.OrdinalIgnoreCase for project names
- Consistent with existing cyclicEdges HashSet from Story 3.6

**CycleBreakingSuggestion Model (from Story 3.5):**

Story 3.5 created CycleBreakingSuggestion model. Story 3.7 consumes it:

```csharp
public sealed record CycleBreakingSuggestion : IComparable<CycleBreakingSuggestion>
{
    public int CycleId { get; init; }                    // Which cycle this edge is part of
    public ProjectNode SourceProject { get; init; }      // Project dependency is from (NOT string!)
    public ProjectNode TargetProject { get; init; }      // Project dependency points to (NOT string!)
    public int CouplingScore { get; init; }              // Number of method calls (lower = weaker = better to break)
    public int CycleSize { get; init; }                  // Size of the cycle
    public string Rationale { get; init; }               // Human-readable reason for suggestion
    public int Rank { get; init; }                       // Priority ranking (1 = highest priority)
}
```

**Algorithm Correctness:**

Edge is a break point suggestion if:
1. Edge appears in top 10 CycleBreakingSuggestion objects (sorted by CouplingScore)
2. Edge's (source, target) tuple matches (recommendation.SourceProject, recommendation.TargetProject)

Edge coloring priority:
1. If in break point set → YELLOW (regardless of cycle or cross-solution status)
2. Else if in cyclic set → RED
3. Else if cross-solution → BLUE
4. Else → BLACK

**Ranking and Sorting:**
- CycleBreakingSuggestion objects from Story 3.5 are already ranked by coupling score
- OrderBy(r => r.CouplingScore) ensures lowest coupling (weakest edges) are first
- Take(10) gets the top 10 weakest edges (best candidates for breaking)
- These are the edges that should be YELLOW

### Architecture Compliance

**Epic 3 Architecture Requirements:**

```
- TarjanCycleDetector using QuikGraph's Tarjan's SCC algorithm ✅ (Story 3.1)
- Cycle statistics calculation ✅ (Story 3.2)
- CouplingAnalyzer for method call counting ✅ (Story 3.3)
- WeakEdgeIdentifier for finding weakest edges ✅ (Story 3.4)
- Ranked cycle-breaking recommendations ✅ (Story 3.5)
- Enhanced DOT visualization with cycle highlighting ✅ (Story 3.6)
- Mark suggested break points in YELLOW ⏳ (Story 3.7 - THIS STORY)
```

**Story 3.7 Implements:**
- ✅ IDotGenerator interface extension with optional recommendations parameter
- ✅ Break point edge identification algorithm (top 10 from recommendations)
- ✅ YELLOW edge coloring for suggested break points
- ✅ Color priority: YELLOW > RED > BLUE > BLACK
- ✅ Legend entry for "Yellow: Suggested Break Points (Top N)"
- ✅ Backward compatibility with existing callers (Story 2-8, 3-6)
- ✅ Visual clutter prevention (only top 10 marked, not all weak edges)
- ✅ Structured logging for break point highlighting progress

**Integration with Existing Components:**

Story 3.7 extends:
- **DotGenerator** (from Story 2-8, extended in Story 3-6): Enhanced with break point highlighting capability
- Uses DependencyGraph (Epic 2), CycleInfo (Story 3.1), CycleBreakingSuggestion (Story 3.5)

Story 3.7 produces:
- **Enhanced DOT files**: With YELLOW edges for top 10 suggested break points
- **Complete visualizations**: Cycles (RED), Break Points (YELLOW), Cross-Solution (BLUE), Normal (BLACK)
- **Comprehensive legends**: Documenting all edge color meanings in priority order
- **Actionable architect guidance**: Clear visual focus on where to refactor

**Namespace Organization:**

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
│   ├── DependencyGraph.cs           # Reused as-is
│   ├── DependencyEdge.cs            # Reused as-is
│   └── ProjectNode.cs               # Reused as-is
├── CycleAnalysis/               # Epic 3: Cycle detection and recommendations
│   ├── CycleInfo.cs                 # Story 3.1 - Consumed by 3.6 and 3.7
│   └── CycleBreakingSuggestion.cs   # Story 3.5 - Consumed by 3.7
└── Visualization/               # Epic 2: DOT generation (ENHANCED)
    ├── IDotGenerator.cs             # MODIFIED: Add recommendations parameter
    ├── DotGenerator.cs              # MODIFIED: Break point highlighting logic
    └── DotGenerationException.cs    # Reused as-is
```

**DI Integration:**
```csharp
// No changes to DI registration - interface compatible
services.TryAddSingleton<IDotGenerator, DotGenerator>();

// CLI usage pattern for Story 3.7
var cycles = await cycleDetector.DetectCyclesAsync(graph);                                        // Story 3.1
var recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cycles, graph);  // Story 3.5
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);  // Story 3.7 (NEW)
await graphvizRenderer.RenderToFileAsync(dotPath, GraphvizOutputFormat.PNG);                     // Existing
```

### Library/Framework Requirements

**No New NuGet Packages Required:**

All dependencies already satisfied:
- ✅ Microsoft.Extensions.Logging.Abstractions (installed in Story 1-6)
- ✅ System.Text (built-in for StringBuilder)
- ✅ System.IO (built-in for file operations)
- ✅ System.Linq (built-in for OrderBy, Take)

**Graphviz DOT Format:**
- Version: Compatible with Graphviz 2.38+ (NFR requirement)
- Color syntax: Standard DOT format (`color="yellow"` for break points)
- Style syntax: `style="bold"` for emphasized edges
- Legend syntax: `subgraph cluster_name` for grouped legend items
- Reference: https://graphviz.org/docs/edges/

**C# Language Features Used:**
- Tuples: `(string source, string target)` for edge keys (existing pattern from 3.6)
- HashSet: O(1) lookup for break point edge membership (existing pattern from 3.6)
- Optional parameters: `recommendations = null` for backward compatibility
- LINQ: `OrderBy`, `Take` for top N recommendations
- String interpolation: For DOT content generation (user-facing, not logging)

**Existing DotGenerator Patterns to Maintain:**
- StringBuilder for DOT content building (performance)
- EscapeDotIdentifier() for safe project name escaping
- File.WriteAllTextAsync() for async file writing
- ConfigureAwait(false) for library code
- Structured logging with named placeholders
- HashSet edge sets for O(1) lookup (established in Story 3.6)
- Color priority checking with if/else if chain (established in Story 3.6)

### File Structure Requirements

**Files to Modify:**

```
src/MasDependencyMap.Core/Visualization/
├── IDotGenerator.cs                 # MODIFY: Add recommendations parameter to GenerateAsync
└── DotGenerator.cs                  # MODIFY: Implement break point highlighting logic

tests/MasDependencyMap.Core.Tests/Visualization/
└── DotGeneratorTests.cs             # MODIFY: Update existing tests, add new break point tests

src/MasDependencyMap.CLI/
└── Program.cs or Commands/AnalyzeCommand.cs  # MODIFY: Pass recommendations to DotGenerator
```

**No New Files Created:**

Story 3.7 enhances existing visualization infrastructure. No new classes or interfaces needed.

**Integration Points:**

```
src/MasDependencyMap.CLI/Program.cs or Commands/AnalyzeCommand.cs
  - Update DOT generation call to pass both cycles and recommendations
  - Integration: var cycles = await cycleDetector.DetectCyclesAsync(graph);
  -              var recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cycles, graph);
  -              await dotGenerator.GenerateAsync(graph, outputDir, name, cycles, recommendations);
```

### Testing Requirements

**Test Class: DotGeneratorTests.cs**

Update existing test class with new break point highlighting tests:

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

    // ========== NEW TESTS FOR STORY 3.7 ==========

    [Fact]
    public async Task GenerateAsync_WithRecommendations_BreakPointEdgesAreYellow()
    {
        // Arrange
        var graph = CreateGraphWithCycle(); // ProjectA -> ProjectB -> ProjectC -> ProjectA
        var cycles = CreateCyclesForGraph(graph);
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion
            {
                SourceProject = "ProjectA",
                TargetProject = "ProjectB",
                CouplingScore = 3,  // Weakest edge
                Rationale = "Weakest link in cycle",
                CycleId = 1
            }
        };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().Contain("color=\"yellow\"");
        dotContent.Should().Contain("ProjectA\" -> \"ProjectB\" [color=\"yellow\", style=\"bold\"]");
    }

    [Fact]
    public async Task GenerateAsync_WithRecommendations_EdgesNotInRecommendationsUseDefaultColor()
    {
        // Arrange
        var graph = CreateGraphWithCycleAndNonCyclicEdge(); // A->B->A (cycle), C->D (not cyclic)
        var cycleProjects = new List<ProjectNode> { graph.Vertices.ElementAt(0), graph.Vertices.ElementAt(1) };
        var cycles = new List<CycleInfo> { new CycleInfo(1, cycleProjects) };
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion
            {
                SourceProject = "ProjectA",
                TargetProject = "ProjectB",
                CouplingScore = 3,
                Rationale = "Weakest link",
                CycleId = 1
            }
        };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert - Non-recommended edges use their appropriate colors
        dotContent.Should().Contain("ProjectC\" -> \"ProjectD\" [color=\"black\"]");  // Non-cyclic edge
        dotContent.Should().Contain("ProjectB\" -> \"ProjectA\"");  // Cyclic but not recommended
        dotContent.Should().NotContain("ProjectB\" -> \"ProjectA\" [color=\"yellow\"");  // Should be RED not YELLOW
    }

    [Fact]
    public async Task GenerateAsync_WithRecommendationsAndCycles_YellowOverridesRed()
    {
        // CRITICAL TEST: Validates color priority - YELLOW > RED
        // Arrange
        var graph = CreateGraphWithCycle(); // ProjectA -> ProjectB -> ProjectC -> ProjectA (all RED)
        var cycles = CreateCyclesForGraph(graph);
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion
            {
                SourceProject = "ProjectA",
                TargetProject = "ProjectB",
                CouplingScore = 3,  // This edge is BOTH cyclic (would be RED) AND recommended (should be YELLOW)
                Rationale = "Weakest link in cycle",
                CycleId = 1
            }
        };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert - YELLOW wins over RED for recommended edge
        dotContent.Should().Contain("ProjectA\" -> \"ProjectB\" [color=\"yellow\", style=\"bold\"]");
        dotContent.Should().NotContain("ProjectA\" -> \"ProjectB\" [color=\"red\"");

        // Other cyclic edges (not recommended) should still be RED
        dotContent.Should().Contain("ProjectB\" -> \"ProjectC\" [color=\"red\", style=\"bold\"]");
        dotContent.Should().Contain("ProjectC\" -> \"ProjectA\" [color=\"red\", style=\"bold\"]");
    }

    [Fact]
    public async Task GenerateAsync_WithRecommendations_LegendIncludesSuggestedBreakPoints()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = CreateCyclesForGraph(graph);
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion { SourceProject = "ProjectA", TargetProject = "ProjectB", CouplingScore = 3 }
        };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().Contain("Yellow: Suggested Break Points");
        dotContent.Should().Contain("subgraph cluster_dependency_legend");
        dotContent.Should().Contain("color=\"yellow\"");
    }

    [Fact]
    public async Task GenerateAsync_NullRecommendations_NoYellowEdgesBackwardCompatible()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = CreateCyclesForGraph(graph);
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act - Story 3.6 usage pattern (cycles but no recommendations)
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations: null);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert - No YELLOW highlighting when recommendations is null
        dotContent.Should().NotContain("color=\"yellow\"");
        dotContent.Should().NotContain("Suggested Break Points");
        // But RED cycle highlighting should still work
        dotContent.Should().Contain("color=\"red\"");
    }

    [Fact]
    public async Task GenerateAsync_EmptyRecommendations_NoYellowEdges()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = CreateCyclesForGraph(graph);
        var recommendations = new List<CycleBreakingSuggestion>(); // Empty list
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert
        dotContent.Should().NotContain("color=\"yellow\"");
        dotContent.Should().NotContain("Suggested Break Points");
    }

    [Fact]
    public async Task GenerateAsync_MoreThan10Recommendations_OnlyTop10AreYellow()
    {
        // CRITICAL TEST: Validates top 10 limit to avoid visual clutter
        // Arrange
        var graph = CreateGraphWithManyEdges(); // Graph with 15+ edges
        var cycles = CreateCyclesForGraph(graph);
        var recommendations = CreateRecommendations(15); // 15 recommendations (all valid edges)
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert - Count YELLOW edges (should be exactly 10, not 15)
        var yellowEdgeCount = Regex.Matches(dotContent, @"color=""yellow""").Count;
        yellowEdgeCount.Should().Be(10, "Only top 10 recommendations should be marked in yellow");

        // Legend should indicate "Top 10"
        dotContent.Should().ContainAny("Top 10", "top 10");
    }

    [Fact]
    public async Task GenerateAsync_RecommendationsWithNoCycles_YellowOnlyBreakPoints()
    {
        // Arrange - Graph with recommendations but no cycles parameter
        var graph = CreateGraphWithCycle();
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion { SourceProject = "ProjectA", TargetProject = "ProjectB", CouplingScore = 3 }
        };
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        // Act - Recommendations without cycles
        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles: null, recommendations);
        var dotContent = await File.ReadAllTextAsync(dotPath);

        // Assert - YELLOW edges present, no RED edges
        dotContent.Should().Contain("color=\"yellow\"");
        dotContent.Should().NotContain("color=\"red\"");
        dotContent.Should().Contain("Yellow: Suggested Break Points");
    }

    // ========== EXISTING TESTS (Updated to pass null for recommendations) ==========

    [Fact]
    public async Task GenerateAsync_ValidGraph_CreatesDotFile()
    {
        // Existing test updated to explicitly pass cycles: null, recommendations: null
        var graph = CreateSimpleGraph();
        var outputDir = Path.GetTempPath();
        var solutionName = "TestSolution";

        var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles: null, recommendations: null);

        File.Exists(dotPath).Should().BeTrue();
        dotPath.Should().EndWith(".dot");
    }

    // ... other existing tests updated to include recommendations: null ...

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

    private List<CycleInfo> CreateCyclesForGraph(DependencyGraph graph)
    {
        return new List<CycleInfo>
        {
            new CycleInfo(1, graph.Vertices.ToList())
        };
    }

    private DependencyGraph CreateGraphWithManyEdges()
    {
        // Create graph with 15+ edges for testing top 10 limit
        var graph = new DependencyGraph();
        // ... implementation with many edges ...
        return graph;
    }

    private List<CycleBreakingSuggestion> CreateRecommendations(int count)
    {
        // Create recommendations with varying coupling scores
        var recommendations = new List<CycleBreakingSuggestion>();
        for (int i = 0; i < count; i++)
        {
            recommendations.Add(new CycleBreakingSuggestion
            {
                SourceProject = $"Project{i}",
                TargetProject = $"Project{i + 1}",
                CouplingScore = i + 1,  // Vary coupling scores
                Rationale = $"Weak link {i}",
                CycleId = 1
            });
        }
        return recommendations;
    }
}
```

**Test Naming Convention:**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `GenerateAsync_WithRecommendations_BreakPointEdgesAreYellow()`
- ✅ `GenerateAsync_WithRecommendationsAndCycles_YellowOverridesRed()` (CRITICAL)
- ✅ `GenerateAsync_MoreThan10Recommendations_OnlyTop10AreYellow()` (CRITICAL)
- ✅ `GenerateAsync_NullRecommendations_NoYellowEdgesBackwardCompatible()`

**Test Coverage:**
- 9 new tests for Story 3.7 break point highlighting
- All existing tests updated to pass `recommendations: null` (backward compatibility)
- Edge cases: null recommendations, empty recommendations, >10 recommendations, no cycles with recommendations
- Priority testing: YELLOW overrides RED for edges that are both cyclic and recommended (CRITICAL)
- Limit testing: only top 10 marked even when more recommendations exist (CRITICAL)
- Legend testing: verify legend content when recommendations present

### Previous Story Intelligence

**From Story 3-6 (Enhance DOT Visualization with Cycle Highlighting) - Key Learnings:**

Story 3-6 established cycle highlighting pattern. Story 3.7 extends it with break point highlighting:

1. **Color Priority Pattern Established:**
   ```csharp
   // Story 3.6 established: if/else if chain for color priority
   // Cycles (RED) > Cross-solution (BLUE) > Default (BLACK)

   // Story 3.7 extends: Add YELLOW as HIGHEST priority
   // Break Points (YELLOW) > Cycles (RED) > Cross-solution (BLUE) > Default (BLACK)
   ```

2. **HashSet Edge Set Pattern (REUSE):**
   ```csharp
   // Story 3.6 created cyclicEdges HashSet for O(1) lookup
   private HashSet<(string source, string target)> BuildCyclicEdgeSet(...)

   // Story 3.7 creates similar breakPointEdges HashSet
   private HashSet<(string source, string target)> BuildBreakPointEdgeSet(...)

   // Both use same pattern: HashSet<(string, string)> with StringComparer.OrdinalIgnoreCase
   ```

3. **Legend Pattern (EXTEND):**
   ```csharp
   // Story 3.6 created subgraph cluster_cycle_legend
   // Story 3.7 extends to cluster_dependency_legend with all types
   // Order by priority: Break Points > Cycles > Cross-Solution > Default
   ```

4. **Integration Pattern from Story 3.6:**
   ```csharp
   // Story 3.6 workflow:
   var cycles = await cycleDetector.DetectCyclesAsync(graph);
   var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, name, cycles);

   // Story 3.7 extends:
   var cycles = await cycleDetector.DetectCyclesAsync(graph);
   var recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cycles, graph);
   var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, name, cycles, recommendations);
   ```

5. **Code Style from Story 3.6 (MAINTAIN):**
   - File-scoped namespaces
   - XML documentation for all public APIs
   - Structured logging (Debug for detailed progress, Information for key milestones)
   - Defensive null checks with ArgumentNullException.ThrowIfNull
   - ConfigureAwait(false) for async library code
   - StringBuilder for DOT content building
   - EscapeDotIdentifier for all project names

6. **Code Review Lessons from Story 3.6:**
   - Test assertions must be specific (check actual edge content, not just presence of color)
   - Validate test coverage against ALL acceptance criteria
   - Remove unused code (dead code identified in review)
   - Add edge case tests (e.g., cycles with no matching edges)
   - Optimize performance where possible (BuildCyclicEdgeSet was optimized in review)

**From Story 3-5 (Generate Ranked Cycle-Breaking Recommendations) - CycleBreakingSuggestion Model:**

Story 3.5 created the model that Story 3.7 consumes:

1. **CycleBreakingSuggestion Properties:**
   ```csharp
   public class CycleBreakingSuggestion
   {
       public string SourceProject { get; set; }      // CRITICAL: Used for edge matching
       public string TargetProject { get; set; }      // CRITICAL: Used for edge matching
       public int CouplingScore { get; set; }         // CRITICAL: Used for ranking (lower = better)
       public string Rationale { get; set; }          // Human-readable explanation
       public int CycleId { get; set; }               // Which cycle this edge belongs to
   }
   ```

2. **Pre-Sorted by Coupling Score:**
   - Story 3.5 already sorts recommendations by CouplingScore (lowest first)
   - Story 3.7 can rely on this: just take top 10 from the list
   - No need to re-sort in DotGenerator

3. **Edge Matching:**
   - Match (SourceProject, TargetProject) tuple from CycleBreakingSuggestion
   - With (Source.ProjectName, Target.ProjectName) tuple from DependencyEdge
   - Case-insensitive comparison (StringComparer.OrdinalIgnoreCase)

**Expected Integration in CLI (Story 3.7 Changes):**

```csharp
// Before Story 3.7 (Story 3.6 integration)
var cycles = await cycleDetector.DetectCyclesAsync(graph);
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName, cycles);

// After Story 3.7 (with break point highlighting)
var cycles = await cycleDetector.DetectCyclesAsync(graph);
var annotatedGraph = await couplingAnalyzer.AnalyzeAsync(graph, solution);
var recommendations = await recommendationGenerator.GenerateRecommendationsAsync(cycles, annotatedGraph);
var dotPath = await dotGenerator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
```

### Git Intelligence Summary

**Recent Commits Analysis:**

Last 5 commits show Epic 3 story completion pattern:
1. **fea9295:** Update Story 3-6 status to done and document code review fixes
2. **4f1fe29:** Code review fixes for Story 3-6: Enhance DOT visualization with cycle highlighting
3. **f8bd5be:** Code review fixes for Story 3-5: Generate ranked cycle-breaking recommendations
4. **7249b33:** Story 3-5 complete: Generate ranked cycle-breaking recommendations
5. **ecb8f93:** Code review fixes for Story 3-4: Identify weakest coupling edges within cycles

**Pattern Observed:**
- Initial "Story X.Y complete" commit
- Follow-up "Code review fixes for Story X.Y" commit
- Final "Update Story X.Y status to done" commit
- Suggests rigorous code review process after implementation
- Code review finds 5-9 issues per story (ranging from critical to minor)

**Commit Message Pattern for Story Completion:**

```bash
git commit -m "Story 3-7 complete: Mark suggested break points in YELLOW on visualizations

- Extended IDotGenerator interface with optional recommendations parameter for backward compatibility
- Added BuildBreakPointEdgeSet helper method for O(1) edge lookup using HashSet<(string, string)>
- Implemented top 10 limit to avoid visual clutter (OrderBy coupling score, Take 10)
- Modified BuildDotContent to accept and use recommendation information
- Implemented edge color priority: Break Points (YELLOW) > Cycles (RED) > Cross-solution (BLUE) > Default (BLACK)
- YELLOW overrides RED for edges that are both cyclic and recommended (highest priority)
- Added legend entry for \"Yellow: Suggested Break Points (Top N)\" when recommendations are present
- Updated legend to show all dependency types in priority order
- Added 9 comprehensive unit tests for break point highlighting (all passing)
- Updated existing tests to pass recommendations: null for backward compatibility
- Handles edge cases: null recommendations, empty recommendations, >10 recommendations
- All acceptance criteria satisfied
- Backward compatible: Story 2-8 and Story 3-6 callers work without changes

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

**Files Expected to Change in Story 3.7:**

- Modified: `src/MasDependencyMap.Core/Visualization/IDotGenerator.cs` (interface extension)
- Modified: `src/MasDependencyMap.Core/Visualization/DotGenerator.cs` (break point highlighting implementation)
- Modified: `tests/MasDependencyMap.Core.Tests/Visualization/DotGeneratorTests.cs` (9 new tests + updated existing tests)
- Modified: `src/MasDependencyMap.CLI/Program.cs` (CLI integration - pass recommendations)
- Modified: `D:\work\masDependencyMap\_bmad-output\implementation-artifacts\sprint-status.yaml` (story status update to ready-for-dev, then in-progress, then review, then done)

**Code Review Expectations (Based on Story 3-6 Review):**

Expect code review to find:
- Test assertion improvements (check specific edges, not just color presence)
- Edge case coverage gaps (e.g., recommendations with no matching edges, null project names)
- Performance optimizations (optimize HashSet building if needed)
- Dead code removal (unused variables or methods)
- Documentation improvements (XML comments, code comments)
- Minor issues like logging levels, legend styling consistency

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 3.7 enhances Epic 2 visualization infrastructure (extended in Story 3.6):

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
│   ├── DependencyGraph.cs           # Reused as-is
│   ├── DependencyEdge.cs            # Reused as-is
│   └── ProjectNode.cs               # Reused as-is
├── CycleAnalysis/               # Epic 3: Cycle detection and recommendations
│   ├── CycleInfo.cs                 # Story 3.1 - Consumed by 3.6 and 3.7
│   └── CycleBreakingSuggestion.cs   # Story 3.5 - Consumed by 3.7 (NEW)
└── Visualization/               # Epic 2: DOT generation (ENHANCED AGAIN)
    ├── IDotGenerator.cs             # MODIFIED: Add recommendations parameter
    ├── DotGenerator.cs              # MODIFIED: Break point highlighting logic
    └── DotGenerationException.cs    # Reused as-is
```

**Consistency with Existing Patterns:**
- Feature-based namespace: `MasDependencyMap.Core.Visualization` ✅
- Interface + Implementation pattern: `IDotGenerator`, `DotGenerator` ✅
- File naming matches class naming exactly ✅
- Test namespace mirrors Core: `tests/MasDependencyMap.Core.Tests/Visualization` ✅
- Service pattern with ILogger injection ✅
- Singleton DI registration (no changes needed) ✅
- Optional parameter pattern for interface extension (established in Story 3.6) ✅
- HashSet edge set pattern for O(1) lookup (established in Story 3.6, reused in 3.7) ✅

**Cross-Namespace Dependencies:**
- Visualization → DependencyAnalysis (uses DependencyGraph, DependencyEdge, ProjectNode) ✅
- Visualization → CycleAnalysis (uses CycleInfo from Story 3.1, CycleBreakingSuggestion from Story 3.5) ✅ NEW
- This cross-namespace usage is expected and acceptable (Epic 3 builds on Epic 2)

**Interface Evolution Pattern (Story 2-8 → 3-6 → 3-7):**
- Story 2-8: `GenerateAsync(graph, outputDir, solutionName)`
- Story 3-6: `GenerateAsync(graph, outputDir, solutionName, cycles = null)`
- Story 3-7: `GenerateAsync(graph, outputDir, solutionName, cycles = null, recommendations = null)`
- Pattern: Optional parameters added incrementally, maintaining backward compatibility at each step
- All three versions work: 2-8 callers, 3-6 callers, and new 3-7 callers

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-3-circular-dependency-detection-and-break-point-analysis.md, Story 3.7 (lines 111-127)]
- Story requirements: Mark break points in YELLOW, YELLOW overrides RED, top 10 limit, legend entry

**Previous Stories:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-6-enhance-dot-visualization-with-cycle-highlighting.md (full file)]
- Cycle highlighting implementation, color priority pattern, HashSet edge set pattern, legend pattern
- Code review fixes, lessons learned, test patterns
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-5-generate-ranked-cycle-breaking-recommendations.md]
- CycleBreakingSuggestion model, ranking by coupling score, integration patterns
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-1-implement-tarjans-scc-algorithm-for-cycle-detection.md]
- CycleInfo model, cycle detection integration

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Structured Logging (lines 115-119)]
- Named placeholders, no string interpolation
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, File-Scoped Namespaces (lines 76-79)]
- Use file-scoped namespace declarations (C# 10+)
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Async/Await Patterns (lines 67-72)]
- Async suffix for all async methods, ConfigureAwait(false) for library code
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\architecture-validation-results.md]
- Complete architecture validation, requirements coverage, implementation readiness

**Technology Stack:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Core Technologies (lines 20-26)]
- .NET 8.0, C# 12, QuikGraph v2.5.0
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, External Tools (lines 39-41)]
- Graphviz 2.38+ (external process)

**Graphviz DOT Format (2026):**
- [Source: https://graphviz.org/docs/edges/]
- Edge attributes, color syntax, style options
- [Source: https://graphviz.org/docs/attrs/color/]
- Color attribute reference, valid color names including "yellow"
- [Source: https://graphviz.org/docs/attr-types/style/]
- Style attribute options: bold, dashed, dotted, solid, invis

**Testing Patterns:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Test Method Naming (lines 150-154)]
- MethodName_Scenario_ExpectedResult pattern
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Test Organization (lines 140-145)]
- Tests mirror Core namespace structure

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (full file)]
- Complete project rules and patterns for AI agents
- Critical implementation rules, version compatibility, console output discipline

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

None

### Code Review - Fixes Applied

**Review Date:** 2026-01-24
**Reviewer:** Claude Sonnet 4.5 (Adversarial Code Review Mode)
**Issues Found:** 10 (2 High, 5 Medium, 3 Low)
**Issues Fixed:** 7 (All High and Medium severity)

**HIGH SEVERITY FIXES:**
1. **Removed redundant sorting logic** - BuildBreakPointEdgeSet was re-sorting pre-sorted recommendations. Removed `.OrderBy()` call and added comment explaining recommendations are already sorted by IRecommendationGenerator.
2. **Fixed sorting to respect IComparable implementation** - By removing OrderBy, now correctly uses the 3-tier sorting (coupling score → cycle size → project name) from CycleBreakingSuggestion.CompareTo().

**MEDIUM SEVERITY FIXES:**
3. **Fixed legend wording confusion** - Changed legend from "Top {topN}" (actual colored edge count) to "Top {maxBreakPoints}" (requested limit) to avoid user confusion when some recommendations don't have matching edges in the graph.
4. **Made maxBreakPoints configurable** - Added `int maxBreakPoints = 10` parameter to `IDotGenerator.GenerateAsync()` allowing callers to request different limits (default remains 10 per AC requirement).
5. **Fixed dev notes documentation mismatch** - Corrected CycleBreakingSuggestion model documentation showing ProjectNode properties instead of string properties.
6. **Added case-insensitive edge comparer** - Created `CaseInsensitiveEdgeComparer` class and used it in both `BuildBreakPointEdgeSet()` and `BuildCyclicEdgeSet()` to prevent edge lookup failures due to project name casing differences.
7. **Fixed StringBuilder capacity underestimate** - Added 200 chars for dependency type legend size to capacity estimate to avoid mid-operation resizing.

**LOW SEVERITY (NOT FIXED - Optional improvements):**
8. Missing test for triple-condition edge (cross-solution + cyclic + break point) - Test coverage gap
9. CLI defensive null check missing on dotFilePath - Minor defensive coding improvement
10. Missing ConfigureAwait documentation comment - Code documentation enhancement

### Completion Notes List

**Initial Implementation:**
- Extended IDotGenerator interface with optional `recommendations` parameter for backward compatibility
- Added BuildBreakPointEdgeSet helper method for O(1) edge lookup using HashSet<(string, string)>
- Implemented top 10 limit to avoid visual clutter
- Modified BuildDotContent to accept and use recommendation information
- Implemented edge color priority: Break Points (YELLOW) > Cycles (RED) > Cross-solution (BLUE) > Default (BLACK)
- YELLOW overrides RED for edges that are both cyclic and recommended (highest priority)
- Added legend entry for "Yellow: Suggested Break Points (Top N)" when recommendations are present
- Updated legend to show all dependency types in priority order (cluster_dependency_legend)
- Updated CLI integration to call IRecommendationGenerator and pass results to DotGenerator
- Added 8 comprehensive unit tests for break point highlighting (all passing)
- Updated 1 existing test to pass recommendations: null for backward compatibility
- Fixed 1 existing test that was checking for old legend name (cluster_cycle_legend → cluster_dependency_legend)
- Handles edge cases: null recommendations, empty recommendations, >10 recommendations
- All 261 tests pass
- All acceptance criteria satisfied
- Backward compatible: Story 2-8 and Story 3-6 callers work without changes

**Code Review Fixes (2026-01-24):**
- Removed redundant `.OrderBy()` in BuildBreakPointEdgeSet - recommendations are pre-sorted by IRecommendationGenerator
- Fixed sorting to respect CycleBreakingSuggestion.CompareTo() 3-tier logic (coupling → cycle size → name)
- Added `int maxBreakPoints = 10` parameter to GenerateAsync for configurability
- Fixed legend to show requested limit "Top {maxBreakPoints}" instead of actual colored count
- Added CaseInsensitiveEdgeComparer class to prevent edge lookup failures from casing differences
- Applied case-insensitive comparer to both BuildBreakPointEdgeSet and BuildCyclicEdgeSet
- Fixed StringBuilder capacity estimate to include dependency type legend size (+200 chars)
- Corrected dev notes documentation showing ProjectNode properties (not string) in CycleBreakingSuggestion
- Updated test to use named parameter for cancellationToken after adding maxBreakPoints parameter
- All 261 tests still pass after fixes

### File List

**Modified:**
- src/MasDependencyMap.Core/Visualization/IDotGenerator.cs
- src/MasDependencyMap.Core/Visualization/DotGenerator.cs
- src/MasDependencyMap.CLI/Program.cs
- tests/MasDependencyMap.Core.Tests/Visualization/DotGeneratorTests.cs
- _bmad-output/implementation-artifacts/sprint-status.yaml
