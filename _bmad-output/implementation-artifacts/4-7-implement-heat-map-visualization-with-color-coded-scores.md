# Story 4.7: Implement Heat Map Visualization with Color-Coded Scores

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want dependency graph nodes color-coded by extraction difficulty,
So that I can visually identify easy vs. hard extraction candidates.

## Acceptance Criteria

**Given** Extraction scores are calculated for all projects
**When** DotGenerator.GenerateAsync() is called with scoring information
**Then** Nodes with scores 0-33 are rendered in GREEN
**And** Nodes with scores 34-66 are rendered in YELLOW
**And** Nodes with scores 67-100 are rendered in RED
**And** Node color clearly indicates extraction difficulty at a glance
**And** Graph legend includes "Green: Easy (0-33), Yellow: Medium (34-66), Red: Hard (67-100)"

## Tasks / Subtasks

- [x] Extend IDotGenerator interface to accept extraction score data (AC: Interface update)
  - [x] Add optional parameter: IReadOnlyList<ExtractionScore>? extractionScores = null to GenerateAsync method
  - [x] Update XML documentation to explain color-coding behavior when scores provided
  - [x] Maintain backward compatibility (null scores = existing behavior with solution-based colors)
  - [x] Place in `MasDependencyMap.Core.Visualization` namespace

- [x] Update DotGenerator implementation signature (AC: Accept extraction scores)
  - [x] Add extractionScores parameter to GenerateAsync method
  - [x] Update internal BuildDotContent method to accept and use extraction scores
  - [x] Maintain existing cycle/break-point highlighting (yellow/red edges remain unchanged)
  - [x] ConfigureAwait(false) for all async operations

- [x] Implement extraction score lookup helper (AC: Map projects to scores)
  - [x] Create private method: GetExtractionScore(string projectName, IReadOnlyList<ExtractionScore>?)
  - [x] Build case-insensitive dictionary for O(1) score lookup by project name
  - [x] Handle missing scores gracefully (default to no special color if project not found)
  - [x] Log Debug when extraction scores are provided: "Applying heat map colors based on {ScoreCount} extraction scores"

- [x] Implement heat map node color selection logic (AC: Color-code by difficulty)
  - [x] Create private method: GetNodeColorForScore(double? score)
  - [x] Return "lightgreen" for scores 0-33 (Easy - use light colors for readability)
  - [x] Return "yellow" for scores 34-66 (Medium)
  - [x] Return "lightcoral" for scores 67-100 (Hard - use lightcoral instead of red for better contrast)
  - [x] Return default solution-based color when score is null (backward compatibility)
  - [x] Use Graphviz X11 color scheme (default, well-supported)

- [x] Update BuildDotContent node generation (AC: Apply heat map colors to nodes)
  - [x] Check if extractionScores parameter is provided and non-empty
  - [x] For each node, lookup extraction score using GetExtractionScore helper
  - [x] Determine node fillcolor using GetNodeColorForScore logic
  - [x] Apply fillcolor attribute: node [style=filled, fillcolor=colorValue]
  - [x] Preserve existing node label format (project name only, Story 4.8 will add score labels)
  - [x] Log Debug: "Applied heat map colors to {NodeCount} nodes"

- [x] Update legend generation for heat map mode (AC: Add extraction difficulty legend)
  - [x] Check if extraction scores were provided (heat map mode active)
  - [x] Generate heat map legend with 3 categories: "Green: Easy (0-33)", "Yellow: Medium (34-66)", "Red: Hard (67-100)"
  - [x] Place heat map legend AFTER edge type legend (maintain edge highlighting visibility)
  - [x] Use Graphviz subgraph cluster for legend organization
  - [x] Each legend entry shows color box + label text
  - [x] Maintain existing legend for edge types (yellow=break points, red=cycles, blue=cross-solution, black=default)

- [x] Handle edge case: extraction scores provided but empty (AC: Graceful degradation)
  - [x] If extractionScores.Count == 0, treat as null (use existing solution-based colors)
  - [x] Log Warning: "Extraction scores provided but empty, using default node colors"
  - [x] Continue with normal graph generation

- [x] Handle edge case: project in graph not found in scores (AC: Missing score handling)
  - [x] GetExtractionScore returns null if project not found in score dictionary
  - [x] GetNodeColorForScore returns default color for null score
  - [x] Log Debug: "No extraction score found for project {ProjectName}, using default color"
  - [x] Graph generation continues normally

- [x] Update DI registration (AC: No changes needed)
  - [x] DotGenerator already registered as singleton in Program.cs
  - [x] No new dependencies added (same ILogger<DotGenerator>)
  - [x] Verify registration still present after implementation

- [x] Create comprehensive unit tests (AC: Test coverage)
  - [x] Extend DotGeneratorTests.cs in tests/MasDependencyMap.Core.Tests/Visualization/
  - [x] Test: GenerateAsync_WithExtractionScores_AppliesHeatMapColors (validate color application)
  - [x] Test: GenerateAsync_WithEasyScore_UsesGreenColor (score 0-33 → lightgreen)
  - [x] Test: GenerateAsync_WithMediumScore_UsesYellowColor (score 34-66 → yellow)
  - [x] Test: GenerateAsync_WithHardScore_UsesRedColor (score 67-100 → lightcoral)
  - [x] Test: GenerateAsync_WithNullScores_UsesDefaultColors (backward compatibility)
  - [x] Test: GenerateAsync_WithEmptyScores_UsesDefaultColors (edge case)
  - [x] Test: GenerateAsync_WithMissingProject_UsesDefaultColor (project not in scores)
  - [x] Test: GenerateAsync_WithExtractionScores_IncludesHeatMapLegend (legend generation)
  - [x] Test: GenerateAsync_WithExtractionScores_PreservesEdgeHighlighting (cycles/break-points still work)
  - [x] Use xUnit, FluentAssertions for assertions
  - [x] Test naming: {MethodName}_{Scenario}_{ExpectedResult} pattern

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Feature-based namespace: MasDependencyMap.Core.Visualization (consistent with existing)
  - [x] Async suffix on async methods (GenerateAsync maintains existing pattern)
  - [x] File-scoped namespace declarations
  - [x] ILogger injection via constructor (existing)
  - [x] ConfigureAwait(false) in library code
  - [x] XML documentation on all public APIs
  - [x] Structured logging with named placeholders

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 4.7 Heat Map Visualization Requirements:**

This story implements **NODE COLOR-CODING** based on extraction difficulty scores, transforming the dependency graph into a heat map that shows at-a-glance which projects are easiest vs. hardest to extract.

**Epic 4 Vision (Recap):**
- Story 4.1: Coupling metrics ✅ DONE
- Story 4.2: Cyclomatic complexity metrics ✅ DONE
- Story 4.3: Technology version debt metrics ✅ DONE
- Story 4.4: External API exposure metrics ✅ DONE
- Story 4.5: Combined extraction score calculator ✅ DONE
- Story 4.6: Ranked extraction candidate lists ✅ DONE
- **Story 4.7: Heat map visualization with color-coded scores (THIS STORY - VISUALIZATION)**
- Story 4.8: Display extraction scores as node labels (consumes 4.7)

**Story 4.7 Unique Characteristics:**

1. **Enhances Existing Component:**
   - Stories 4.1-4.6: Created NEW components (calculators, analyzers, generators)
   - Story 4.7: EXTENDS existing DotGenerator (created in Epic 2/3)
   - **Backward compatible:** Existing behavior preserved when scores not provided

2. **Visual Enhancement, Not New Computation:**
   - Stories 4.1-4.5: Heavy calculation logic (Roslyn, graph algorithms, weighted scoring)
   - Story 4.6: Data transformation (sorting, filtering, statistics)
   - Story 4.7: VISUALIZATION only - consumes scores from Story 4.5/4.6
   - **No new algorithms:** Pure color mapping based on score ranges

3. **Depends on Story 4.5 Output:**
   - Story 4.7 consumes: IReadOnlyList<ExtractionScore> from ExtractionScoreCalculator
   - Story 4.6 generates ranked lists but Story 4.7 uses raw scores
   - **Flexible integration:** CLI can pass scores from either 4.5 or 4.6

4. **Preserves Existing Features:**
   - DotGenerator currently highlights: cycles (RED edges), break points (YELLOW edges), cross-solution (BLUE edges)
   - Story 4.7 adds: NODE colors based on extraction scores
   - **No conflicts:** Edge colors and node colors are independent in Graphviz
   - **Priority maintained:** Existing edge highlighting logic unchanged

5. **Prepares for Story 4.8:**
   - Story 4.7: Node COLOR-CODING (green/yellow/red)
   - Story 4.8: Node LABELS with actual scores (e.g., "ProjectName\nScore: 45")
   - **Sequential enhancement:** 4.7 provides visual buckets, 4.8 adds precise numbers

🚨 **CRITICAL - DotGenerator Current State Analysis:**

**Existing DotGenerator Implementation (from code exploration):**

Located at: `src/MasDependencyMap.Core/Visualization/DotGenerator.cs`

**Current GenerateAsync Signature:**
```csharp
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    CancellationToken cancellationToken = default)
```

**Current Features:**
1. **Edge Color Highlighting (PRESERVED):**
   - YELLOW edges: Break point recommendations (highest priority)
   - RED edges: Cyclic dependencies
   - BLUE edges: Cross-solution dependencies
   - BLACK edges: Normal dependencies (default)

2. **Node Color Scheme (TO BE ENHANCED):**
   - Currently: Nodes colored by SOLUTION (8 predefined light colors)
   - Single-solution graphs: All nodes use "lightblue"
   - Multi-solution graphs: Nodes grouped by solution with different colors

3. **Legend Generation (TO BE EXTENDED):**
   - Currently: Shows edge types (break points, cycles, cross-solution, default)
   - Multi-solution: Also shows solution color legend
   - Story 4.7: Add extraction difficulty legend

4. **Existing Helpers:**
   - Edge set optimization: HashSet for O(1) lookup of cyclic/break-point edges
   - Case-insensitive edge comparer
   - DOT format generation with Graphviz 2.38+ compatibility

**Story 4.7 Implementation Strategy:**

1. **NEW Optional Parameter:**
   ```csharp
   // UPDATED signature
   public async Task<string> GenerateAsync(
       DependencyGraph graph,
       string outputDirectory,
       string solutionName,
       IReadOnlyList<CycleInfo>? cycles = null,
       IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
       int maxBreakPoints = 10,
       IReadOnlyList<ExtractionScore>? extractionScores = null, // NEW PARAMETER
       CancellationToken cancellationToken = default)
   ```

2. **Backward Compatibility Mode:**
   ```csharp
   // If extractionScores is null or empty → Use existing solution-based node colors
   // If extractionScores is provided → Use heat map colors based on scores
   bool isHeatMapMode = extractionScores?.Any() == true;
   ```

3. **Node Color Selection Logic:**
   ```csharp
   private string GetNodeColorForScore(double? score)
   {
       if (score == null)
           return GetDefaultSolutionColor(); // Existing logic

       // Heat map color selection
       if (score <= 33)
           return "lightgreen";  // Easy (0-33)
       else if (score < 67)
           return "yellow";      // Medium (34-66)
       else
           return "lightcoral";  // Hard (67-100)
   }
   ```

4. **Integration Point in BuildDotContent:**
   ```csharp
   // Current node generation (around line 225-240):
   foreach (var vertex in graph.Vertices)
   {
       string projectName = vertex.ProjectName;

       // EXISTING: Solution-based color logic
       string solutionColor = GetSolutionColor(vertex.SolutionPath);

       // NEW: Override with heat map color if scores provided
       string nodeColor = solutionColor;
       if (isHeatMapMode)
       {
           var score = GetExtractionScore(projectName, extractionScores);
           nodeColor = GetNodeColorForScore(score);
       }

       // Apply color to node
       dotBuilder.AppendLine($"  \"{projectName}\" [style=filled, fillcolor=\"{nodeColor}\"];");
   }
   ```

🚨 **CRITICAL - Graphviz Color Specifications:**

**Supported Color Formats (from Graphviz docs):**

1. **Named Colors (X11 Color Scheme):** Default scheme, case-insensitive
   - "lightgreen", "yellow", "lightcoral", "lightblue", "red", "green"
   - Full list: https://graphviz.org/doc/info/colors.html

2. **RGB Hex:** "#RRGGBB" or "#RRGGBBAA" (with alpha)
   - Example: "#00FF00" (green), "#FFFF00" (yellow), "#FF6666" (light red)

3. **HSV:** "H S V" format (hue saturation value)
   - Example: "0.333 1.000 1.000" (green)

**Story 4.7 Color Choices (using X11 named colors):**

```csharp
// Easy (0-33): Light green for readability
"lightgreen"  // X11 color: #90EE90 (RGB: 144, 238, 144)

// Medium (34-66): Yellow (high visibility)
"yellow"      // X11 color: #FFFF00 (RGB: 255, 255, 0)

// Hard (67-100): Light coral for warning without overwhelming
"lightcoral"  // X11 color: #F08080 (RGB: 240, 128, 128)
```

**Why These Colors?**

1. **lightgreen** (Easy):
   - Positive association (green = go, safe, low risk)
   - Light variant ensures text readability on node
   - Distinct from "green" (which might be too saturated)

2. **yellow** (Medium):
   - Caution signal (yellow = proceed with care)
   - High visibility on both screen and print
   - Standard Graphviz color, widely supported

3. **lightcoral** (Hard):
   - Warning/danger signal (red hues = high risk)
   - "lightcoral" is softer than pure "red", avoids alarm overload
   - Better contrast with black text than pure red
   - Alternative considered: "indianred" (#CD5C5C), but lightcoral (#F08080) is lighter

**Alternative Color Schemes Considered:**

| Scheme | Easy | Medium | Hard | Rationale |
|--------|------|--------|------|-----------|
| **Traffic Light (chosen)** | lightgreen | yellow | lightcoral | Universal recognition, accessible |
| Heat Map RGB | #00FF00 | #FFFF00 | #FF0000 | Too saturated, poor readability |
| Pastel | palegreen | khaki | mistyrose | Too subtle, low contrast |
| Monochrome | white | lightgray | gray | No color distinction for colorblind users |

**Accessibility Consideration:**

Traffic light scheme (green/yellow/red) has ~8% male colorblindness issues (red-green). However:
- Using LIGHT variants (lightgreen, lightcoral) improves distinction
- Yellow remains distinct even with colorblindness
- Story 4.8 will add numeric labels, providing redundant information channel

🚨 **CRITICAL - Difficulty Category Boundaries (Consistent with Story 4.6):**

**Category Definitions (Must match Story 4.6 and Story 4.5):**

```csharp
// Easy: 0-33 (inclusive lower, inclusive upper)
score <= 33  // lightgreen

// Medium: 34-66 (exclusive lower on boundaries)
score > 33 && score < 67  // yellow

// Hard: 67-100 (inclusive lower, inclusive upper)
score >= 67  // lightcoral
```

**Boundary Handling:**

```csharp
// Edge cases (CRITICAL - must be consistent across Epic 4)
score = 0.0   → Easy (lightgreen)
score = 33.0  → Easy (lightgreen)     // inclusive upper bound
score = 33.1  → Medium (yellow)       // exclusive lower bound
score = 66.9  → Medium (yellow)       // exclusive upper bound
score = 67.0  → Hard (lightcoral)     // inclusive lower bound
score = 100.0 → Hard (lightcoral)
```

**Why These Boundaries?**

- Consistent with Story 4.5 (ExtractionScore.DifficultyCategory property)
- Consistent with Story 4.6 (RankedExtractionCandidates category filtering)
- 33.33% buckets provide balanced distribution
- Matches Epic 4 vision from PRD

🚨 **CRITICAL - Legend Generation:**

**Current Legend (Existing - Preserved):**

DotGenerator currently generates legends showing:
1. **Edge Types:** Break points (yellow), cycles (red), cross-solution (blue), default (black)
2. **Solution Colors** (multi-solution only): Shows which color represents which solution

**Story 4.7 Addition:**

Add **Extraction Difficulty Legend** when heat map mode is active:

```dot
subgraph cluster_extraction_legend {
  label = "Extraction Difficulty";
  style = filled;
  color = lightgrey;

  legend_easy [label="Easy (0-33)", style=filled, fillcolor=lightgreen, shape=box];
  legend_medium [label="Medium (34-66)", style=filled, fillcolor=yellow, shape=box];
  legend_hard [label="Hard (67-100)", style=filled, fillcolor=lightcoral, shape=box];

  legend_easy -> legend_medium -> legend_hard [style=invis];
}
```

**Legend Placement:**

```csharp
// Existing legend order (preserved):
1. Edge Type Legend (if cycles/break-points exist)
2. Solution Color Legend (if multi-solution)

// NEW (Story 4.7):
3. Extraction Difficulty Legend (if extraction scores provided)

// Placement: Add after existing legends, before main graph closing brace
```

**Legend Design Rationale:**

- **Subgraph cluster:** Groups legend items visually
- **Invisible edges:** Arranges legend items horizontally for compact layout
- **Shape=box:** Matches node shape, clear color display
- **Label format:** "Easy (0-33)" - both category name and score range for clarity

### Technical Requirements

**Existing Component Enhancement:**

Story 4.7 extends `DotGenerator` (src/MasDependencyMap.Core/Visualization/DotGenerator.cs), created in Epic 2 for basic visualization and enhanced in Epic 3 for cycle highlighting.

**No New Namespaces:**

All work happens in existing `MasDependencyMap.Core.Visualization` namespace:

```
src/MasDependencyMap.Core/Visualization/
├── IDotGenerator.cs              # MODIFY: Add extractionScores parameter
├── DotGenerator.cs               # MODIFY: Implement heat map color logic
├── IGraphvizRenderer.cs          # NO CHANGE
└── GraphvizRenderer.cs           # NO CHANGE
```

**Dependencies:**

Story 4.7 depends on Story 4.5's ExtractionScore model:

```csharp
using MasDependencyMap.Core.ExtractionScoring;  // For ExtractionScore type

// Constructor (existing - no changes)
public DotGenerator(ILogger<DotGenerator> logger)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**No New DI Registrations:**

DotGenerator is already registered in `Program.cs` from Epic 2:

```csharp
// Existing registration (Epic 2 - Story 2.8)
services.AddSingleton<IDotGenerator, DotGenerator>();

// NO CHANGES NEEDED for Story 4.7
```

**Interface Update (Backward Compatible):**

```csharp
// IDotGenerator.cs (BEFORE - Epic 2/3)
Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    CancellationToken cancellationToken = default);

// IDotGenerator.cs (AFTER - Story 4.7)
Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null,  // NEW OPTIONAL PARAMETER
    CancellationToken cancellationToken = default);
```

**Backward Compatibility Guarantee:**

- Optional parameter with null default
- All existing callers continue to work without changes
- CLI integration in Epic 5 will pass extraction scores
- Epic 2/3 callers (if any) remain functional

### Architecture Compliance

**Feature-Based Namespace (Existing):**

Story 4.7 works within existing `MasDependencyMap.Core.Visualization` namespace (established in Epic 2).

**Async Pattern (Preserved):**

```csharp
// Existing pattern (maintained)
public async Task<string> GenerateAsync(...)
{
    // ... existing logic ...

    // NEW: Heat map color application (synchronous logic, no new async calls)
    if (extractionScores?.Any() == true)
    {
        ApplyHeatMapColors(graph, extractionScores);
    }

    // ... rest of existing logic ...

    await File.WriteAllTextAsync(...).ConfigureAwait(false);  // Existing
}
```

**Structured Logging (Extended):**

```csharp
// Existing logs (preserved)
_logger.LogInformation("Generating DOT graph for {SolutionName} with {VertexCount} projects and {EdgeCount} dependencies", ...);

// NEW logs for Story 4.7
_logger.LogDebug(
    "Applying heat map colors based on {ScoreCount} extraction scores",
    extractionScores.Count);

_logger.LogDebug(
    "Applied heat map colors to {NodeCount} nodes: {EasyCount} easy, {MediumCount} medium, {HardCount} hard",
    nodeCount, easyCount, mediumCount, hardCount);

_logger.LogDebug(
    "No extraction score found for project {ProjectName}, using default color",
    projectName);
```

**XML Documentation (Required):**

```csharp
/// <summary>
/// Generates a DOT format graph file from the dependency graph with optional heat map visualization.
/// </summary>
/// <param name="graph">The dependency graph to visualize.</param>
/// <param name="outputDirectory">Directory where the DOT file will be saved.</param>
/// <param name="solutionName">Name of the solution being analyzed.</param>
/// <param name="cycles">Optional cycle information for edge highlighting.</param>
/// <param name="recommendations">Optional cycle-breaking recommendations for edge highlighting.</param>
/// <param name="maxBreakPoints">Maximum number of break point edges to highlight (default 10).</param>
/// <param name="extractionScores">Optional extraction difficulty scores for heat map node coloring. When provided, nodes are colored green (easy 0-33), yellow (medium 34-66), or red (hard 67-100) instead of solution-based colors.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Absolute path to the generated DOT file.</returns>
/// <exception cref="ArgumentNullException">If graph, outputDirectory, or solutionName is null.</exception>
/// <exception cref="IOException">If file write fails.</exception>
```

### Library/Framework Requirements

**Existing Libraries (No New Packages):**

All dependencies already installed in Epic 2/3:
- ✅ QuikGraph v2.5.0 - Graph data structures (DependencyGraph parameter)
- ✅ Microsoft.Extensions.Logging.Console - Structured logging
- ✅ System.IO - File operations

**No New NuGet Packages Required for Story 4.7** ✅

**Graphviz External Dependency (Existing):**

Story 4.7 generates DOT format text files. Graphviz rendering (DOT → PNG/SVG) is handled by GraphvizRenderer (Epic 2 - Story 2.9).

- Graphviz version: 2.38+ (external tool, must be in PATH)
- Color support: X11 color scheme (default in Graphviz)
- Node attributes: `style=filled`, `fillcolor=colorName`

**Color Name Validation:**

Graphviz supports 100+ X11 color names. Story 4.7 uses well-established colors:
- "lightgreen" ✅ Supported since Graphviz 2.0
- "yellow" ✅ Supported since Graphviz 1.0
- "lightcoral" ✅ Supported since Graphviz 2.0

### File Structure Requirements

**Files to Modify:**

```
src/MasDependencyMap.Core/Visualization/
├── IDotGenerator.cs                              # MODIFY: Add extractionScores parameter to interface
└── DotGenerator.cs                               # MODIFY: Implement heat map logic

tests/MasDependencyMap.Core.Tests/Visualization/
└── DotGeneratorTests.cs                          # MODIFY: Add heat map tests
```

**Files to Update (Status Only):**

```
_bmad-output/implementation-artifacts/sprint-status.yaml      # MODIFY: Update story 4-7 status
_bmad-output/implementation-artifacts/4-7-implement-heat-map-visualization-with-color-coded-scores.md  # MODIFY: Completion notes
```

**No New Files Created:**

Story 4.7 extends existing components, no new files needed.

**No CLI Integration Yet:**

DotGenerator enhancement is ready, but CLI doesn't yet pass extraction scores. CLI integration happens in Epic 5 (Reporting):
- Story 5.1: Text report generator (may integrate heat maps)
- Story 5.3: Extraction difficulty section in reports (likely uses heat maps)

For now:
- Extend DotGenerator interface and implementation
- Add comprehensive tests (including integration tests with mock scores)
- CLI integration deferred to Epic 5

### Testing Requirements

**Test Class: DotGeneratorTests.cs (Existing - Extended)**

**Test Strategy:**

Extend existing DotGeneratorTests with heat map scenarios:
- Story 4.7: Add ~10 new tests for heat map functionality
- Existing tests: ~15 tests for cycles, break-points, multi-solution (Epic 2/3)
- Total tests after Story 4.7: ~25 tests

**New Test Coverage:**

1. **Heat Map Color Application:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithEasyScore_AppliesLightGreenColor()
   {
       // Arrange
       var graph = CreateGraphWithProjects("ProjectA");
       var scores = new List<ExtractionScore>
       {
           CreateScore("ProjectA", 25.0)  // Easy (0-33)
       };

       // Act
       var dotPath = await _generator.GenerateAsync(
           graph, _outputDir, "TestSolution", extractionScores: scores);

       // Assert
       var dotContent = await File.ReadAllTextAsync(dotPath);
       dotContent.Should().Contain("\"ProjectA\" [style=filled, fillcolor=\"lightgreen\"]");
   }
   ```

2. **Boundary Testing:**
   ```csharp
   [Theory]
   [InlineData(0.0, "lightgreen")]      // Minimum easy
   [InlineData(33.0, "lightgreen")]     // Maximum easy
   [InlineData(33.1, "yellow")]         // Minimum medium
   [InlineData(66.9, "yellow")]         // Maximum medium
   [InlineData(67.0, "lightcoral")]     // Minimum hard
   [InlineData(100.0, "lightcoral")]    // Maximum hard
   public async Task GenerateAsync_WithScoreBoundaries_AppliesCorrectColor(
       double score, string expectedColor)
   {
       // Test boundary conditions
   }
   ```

3. **Backward Compatibility:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithNullScores_UsesDefaultSolutionColors()
   {
       // Arrange
       var graph = CreateGraphWithProjects("ProjectA");

       // Act (NO extractionScores parameter)
       var dotPath = await _generator.GenerateAsync(
           graph, _outputDir, "TestSolution");

       // Assert
       var dotContent = await File.ReadAllTextAsync(dotPath);
       dotContent.Should().Contain("fillcolor=\"lightblue\"");  // Default single-solution color
       dotContent.Should().NotContain("Extraction Difficulty");  // No heat map legend
   }
   ```

4. **Legend Validation:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithExtractionScores_IncludesHeatMapLegend()
   {
       // Arrange
       var graph = CreateGraphWithProjects("ProjectA", "ProjectB", "ProjectC");
       var scores = CreateScores(easy: 1, medium: 1, hard: 1);

       // Act
       var dotPath = await _generator.GenerateAsync(
           graph, _outputDir, "TestSolution", extractionScores: scores);

       // Assert
       var dotContent = await File.ReadAllTextAsync(dotPath);
       dotContent.Should().Contain("cluster_extraction_legend");
       dotContent.Should().Contain("Easy (0-33)");
       dotContent.Should().Contain("Medium (34-66)");
       dotContent.Should().Contain("Hard (67-100)");
       dotContent.Should().Contain("fillcolor=lightgreen");
       dotContent.Should().Contain("fillcolor=yellow");
       dotContent.Should().Contain("fillcolor=lightcoral");
   }
   ```

5. **Edge Highlighting Preservation:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithExtractionScoresAndCycles_PreservesCycleHighlighting()
   {
       // Arrange
       var graph = CreateGraphWithCycle();
       var cycles = CreateCycleInfo();
       var scores = CreateScores(easy: 2, medium: 0, hard: 0);

       // Act
       var dotPath = await _generator.GenerateAsync(
           graph, _outputDir, "TestSolution",
           cycles: cycles,
           extractionScores: scores);

       // Assert
       var dotContent = await File.ReadAllTextAsync(dotPath);

       // Both features work together
       dotContent.Should().Contain("fillcolor=\"lightgreen\"");  // Node color (heat map)
       dotContent.Should().Contain("color=red, style=\"bold\"");  // Edge color (cycle)
   }
   ```

6. **Edge Cases:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithEmptyScores_UsesDefaultColors()
   {
       // Empty list provided → treat as null
   }

   [Fact]
   public async Task GenerateAsync_WithMissingProjectInScores_UsesDefaultColor()
   {
       // Project in graph but not in scores → default color
   }

   [Fact]
   public async Task GenerateAsync_WithExtraScoresNotInGraph_IgnoresExtraScores()
   {
       // Scores has more projects than graph → ignore extras
   }
   ```

**Test Helper Methods (Reuse Existing):**

```csharp
// Existing helpers from Epic 2/3 (reuse)
private DependencyGraph CreateGraphWithProjects(params string[] projectNames) { ... }
private IReadOnlyList<CycleInfo> CreateCycleInfo() { ... }

// NEW helpers for Story 4.7
private ExtractionScore CreateScore(string projectName, double score)
{
    return new ExtractionScore(
        ProjectName: projectName,
        ProjectPath: $"/path/to/{projectName}.csproj",
        FinalScore: score,
        CouplingMetric: null!,
        ComplexityMetric: null!,
        TechDebtMetric: null!,
        ExternalApiMetric: null!);
}

private IReadOnlyList<ExtractionScore> CreateScores(int easy, int medium, int hard)
{
    var scores = new List<ExtractionScore>();

    // Generate easy scores (0-33)
    for (int i = 0; i < easy; i++)
        scores.Add(CreateScore($"EasyProject{i}", 10.0 + i));

    // Generate medium scores (34-66)
    for (int i = 0; i < medium; i++)
        scores.Add(CreateScore($"MediumProject{i}", 50.0 + i));

    // Generate hard scores (67-100)
    for (int i = 0; i < hard; i++)
        scores.Add(CreateScore($"HardProject{i}", 80.0 + i));

    return scores;
}
```

### Previous Story Intelligence

**From Story 4.6 (Ranked Candidate Generator) - Data Preparation Pattern:**

Story 4.6 prepared sorted/filtered lists for consumption. Story 4.7 consumes raw scores for visualization:

```csharp
// Story 4.6 pattern: Prepare data in specific format for consumers
public sealed record RankedExtractionCandidates(
    IReadOnlyList<ExtractionScore> AllProjects,
    IReadOnlyList<ExtractionScore> EasiestCandidates,
    IReadOnlyList<ExtractionScore> HardestCandidates,
    ExtractionStatistics Statistics);

// Story 4.7 pattern: Accept flexible input (raw scores, not ranked lists)
public async Task<string> GenerateAsync(
    ...,
    IReadOnlyList<ExtractionScore>? extractionScores = null,  // Raw scores
    ...)
```

**Why Story 4.7 Uses Raw Scores (Not RankedExtractionCandidates)?**

1. **Visualization needs ALL projects:** DotGenerator visualizes entire graph, not just top/bottom 10
2. **Color-coding is per-node:** Each node in graph needs its score, regardless of ranking
3. **Simplicity:** Passing `RankedExtractionCandidates.AllProjects` would work, but adds unnecessary coupling
4. **Flexibility:** CLI can pass scores from either ExtractionScoreCalculator (4.5) or RankedCandidateGenerator (4.6)

**Pattern Reuse from Story 4.6:**

```csharp
// Story 4.6: Difficulty categorization logic
var easyCandidates = allScores.Where(s => s.FinalScore <= 33).ToList();
var mediumCandidates = allScores.Where(s => s.FinalScore > 33 && s.FinalScore < 67).ToList();
var hardCandidates = allScores.Where(s => s.FinalScore >= 67).ToList();

// Story 4.7: Same logic for color selection
private string GetNodeColorForScore(double? score)
{
    if (score == null) return GetDefaultColor();

    if (score <= 33)
        return "lightgreen";  // Easy
    else if (score < 67)
        return "yellow";      // Medium
    else
        return "lightcoral";  // Hard
}
```

**From Epic 3 (Cycle Detection) - DotGenerator Enhancement Pattern:**

Epic 3 Story 3.6-3.7 enhanced DotGenerator with cycle highlighting (red edges) and break-point highlighting (yellow edges). Story 4.7 follows same enhancement pattern for NODE colors:

```csharp
// Epic 3 pattern: Optional parameters for enhanced features
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,               // Added in Epic 3
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,  // Added in Epic 3
    int maxBreakPoints = 10,
    CancellationToken cancellationToken = default)

// Story 4.7 pattern: Add another optional parameter
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null,  // NEW in Story 4.7
    CancellationToken cancellationToken = default)
```

**Backward Compatibility Strategy (Same as Epic 3):**

```csharp
// Epic 3: Check if cycles/recommendations provided
bool hasCycles = cycles?.Any() == true;
bool hasRecommendations = recommendations?.Any() == true;

if (hasCycles)
    ApplyCycleHighlighting(edge, cycles);

// Story 4.7: Check if extraction scores provided
bool hasExtractionScores = extractionScores?.Any() == true;

if (hasExtractionScores)
    ApplyHeatMapColoring(node, extractionScores);
```

**From Epic 2 (DOT Generation) - Existing Node/Edge Separation:**

DotGenerator already separates node styling from edge styling (Epic 2 foundation):

```csharp
// Epic 2 pattern: Nodes and edges are styled independently
// Node styling (fillcolor)
dotBuilder.AppendLine($"  \"{projectName}\" [style=filled, fillcolor=\"{color}\"];");

// Edge styling (color, style)
dotBuilder.AppendLine($"  \"{from}\" -> \"{to}\" [color={edgeColor}, style=\"{edgeStyle}\"];");

// Story 4.7 leverages this separation:
// - Node colors (fillcolor): Heat map based on extraction scores
// - Edge colors (color): Cycle/break-point highlighting (unchanged)
// No conflicts - independent styling systems
```

### DotGenerator Current Implementation Details

**Component Location:**

- Interface: `src/MasDependencyMap.Core/Visualization/IDotGenerator.cs`
- Implementation: `src/MasDependencyMap.Core/Visualization/DotGenerator.cs`
- Tests: `tests/MasDependencyMap.Core.Tests/Visualization/DotGeneratorTests.cs`

**Current Node Color Scheme (To Be Enhanced):**

```csharp
// From DotGenerator.cs (Epic 2/3 implementation)
private static readonly string[] SolutionNodeColors = new[]
{
    "lightblue", "lightgreen", "lightyellow", "lightcoral",
    "lightpink", "lightgray", "lightsalmon", "lightcyan"
};

private string GetSolutionColor(string? solutionPath)
{
    // Single-solution graphs
    if (string.IsNullOrEmpty(solutionPath))
        return "lightblue";

    // Multi-solution graphs: Hash solution path to color index
    int colorIndex = Math.Abs(solutionPath.GetHashCode() % SolutionNodeColors.Length);
    return SolutionNodeColors[colorIndex];
}
```

**Story 4.7 Changes:**

```csharp
// NEW method for heat map color selection
private string GetNodeColorForScore(double? score)
{
    if (score == null)
        return null;  // Signal to use default solution color

    // Heat map color mapping
    if (score <= 33)
        return "lightgreen";
    else if (score < 67)
        return "yellow";
    else
        return "lightcoral";
}

// MODIFIED node generation in BuildDotContent
private string GetNodeColor(
    string projectName,
    string? solutionPath,
    IReadOnlyList<ExtractionScore>? extractionScores)
{
    // Heat map mode takes precedence
    if (extractionScores?.Any() == true)
    {
        var score = GetExtractionScore(projectName, extractionScores);
        var heatMapColor = GetNodeColorForScore(score?.FinalScore);
        if (heatMapColor != null)
            return heatMapColor;
    }

    // Fallback to solution-based color
    return GetSolutionColor(solutionPath);
}
```

**Current Edge Color Priority (Preserved):**

```csharp
// From DotGenerator.cs (Epic 3 implementation)
// Priority order: YELLOW (break points) > RED (cycles) > BLUE (cross-solution) > BLACK (default)

private EdgeStyle GetEdgeStyle(Edge edge, ...)
{
    // 1. YELLOW: Break point recommendations (highest priority)
    if (IsBreakPoint(edge, recommendations))
        return new EdgeStyle("yellow", "bold");

    // 2. RED: Cyclic dependencies
    if (IsCyclicEdge(edge, cycles))
        return new EdgeStyle("red", "bold");

    // 3. BLUE: Cross-solution dependencies
    if (IsCrossSolution(edge))
        return new EdgeStyle("blue", "normal");

    // 4. BLACK: Default
    return new EdgeStyle("black", "normal");
}

// Story 4.7: NO CHANGES to edge styling
// Node colors (fillcolor) and edge colors (color) are independent in Graphviz
```

**Current Legend Generation (To Be Extended):**

```csharp
// From DotGenerator.cs (Epic 2/3 implementation)
private void AppendLegend(StringBuilder dotBuilder, bool hasCycles, bool hasRecommendations, bool isMultiSolution)
{
    // Edge type legend (if cycles or recommendations exist)
    if (hasCycles || hasRecommendations)
    {
        dotBuilder.AppendLine("  subgraph cluster_legend {");
        dotBuilder.AppendLine("    label = \"Edge Types\";");
        // ... yellow=break points, red=cycles, blue=cross-solution, black=default ...
        dotBuilder.AppendLine("  }");
    }

    // Solution color legend (if multi-solution)
    if (isMultiSolution)
    {
        dotBuilder.AppendLine("  subgraph cluster_solution_legend {");
        dotBuilder.AppendLine("    label = \"Solutions\";");
        // ... solution names with their colors ...
        dotBuilder.AppendLine("  }");
    }
}

// Story 4.7: ADD extraction difficulty legend
private void AppendExtractionDifficultyLegend(StringBuilder dotBuilder)
{
    dotBuilder.AppendLine("  subgraph cluster_extraction_legend {");
    dotBuilder.AppendLine("    label = \"Extraction Difficulty\";");
    dotBuilder.AppendLine("    style = filled;");
    dotBuilder.AppendLine("    color = lightgrey;");
    dotBuilder.AppendLine();
    dotBuilder.AppendLine("    legend_easy [label=\"Easy (0-33)\", style=filled, fillcolor=lightgreen, shape=box];");
    dotBuilder.AppendLine("    legend_medium [label=\"Medium (34-66)\", style=filled, fillcolor=yellow, shape=box];");
    dotBuilder.AppendLine("    legend_hard [label=\"Hard (67-100)\", style=filled, fillcolor=lightcoral, shape=box];");
    dotBuilder.AppendLine();
    dotBuilder.AppendLine("    legend_easy -> legend_medium -> legend_hard [style=invis];");
    dotBuilder.AppendLine("  }");
}
```

**Test Coverage (Existing Foundation):**

From DotGeneratorTests.cs (Epic 2/3):
- ✅ Basic graph generation
- ✅ Multi-solution support
- ✅ Cycle highlighting (red edges)
- ✅ Break-point highlighting (yellow edges)
- ✅ Edge priority handling (yellow > red > blue > black)
- ✅ Legend generation
- ✅ Special character escaping
- ✅ Edge cases (null parameters, empty graphs)

Story 4.7 extends with heat map tests (~10 new tests).

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 4.7 enhances Epic 2's visualization infrastructure to consume Epic 4's extraction scoring:

```
Epic 4 (Scoring) → Story 4.7 (Visualization) → Epic 5 (Reporting)

src/MasDependencyMap.Core/ExtractionScoring/
├── [Stories 4.1-4.5: Metric calculation, score generation]
├── ExtractionScore.cs               # Story 4.5 (consumed by Story 4.7)
└── [Story 4.6: Ranked list generation]

src/MasDependencyMap.Core/Visualization/
├── DotGenerator.cs                   # Story 4.7 (THIS STORY - ENHANCED)
└── [Other visualization components]

Epic 5 (Future):
- Text reports will reference heat map visualizations
- CSV exports may include extraction score columns
- CLI commands will wire up scores → DotGenerator
```

**Cross-Epic Integration:**

Story 4.7 is a **bridge** between:
1. **Epic 4 (Scoring):** Produces ExtractionScore objects
2. **Epic 2 (Visualization):** DotGenerator creates graphs
3. **Epic 5 (Reporting):** Will integrate scored visualizations into reports

**Dependency Flow:**

```
Story 4.5 (ExtractionScoreCalculator)
        ↓
[IReadOnlyList<ExtractionScore>]
        ↓
Story 4.7 (DotGenerator with heat map) ← THIS STORY
        ↓
[Colored DOT file]
        ↓
Story 2.9 (GraphvizRenderer)
        ↓
[Heat map PNG/SVG visualization]
        ↓
Epic 5 (Reports, CLI integration)
```

**Impact on Existing Components:**

```
MODIFIED (Story 4.7):
- IDotGenerator.cs: Add extractionScores parameter
- DotGenerator.cs: Implement heat map logic

UNCHANGED (Backward compatible):
- GraphvizRenderer.cs: Still renders DOT → PNG/SVG (no changes)
- CLI commands: Don't yet pass scores (Epic 5 integration)
- Existing callers: Continue working with default colors
```

**Epic 4 Completion Status After Story 4.7:**

After Story 4.7, Epic 4 is functionally complete:
1. ✅ Stories 4.1-4.4: Individual metric calculators
2. ✅ Story 4.5: Combined extraction score calculator
3. ✅ Story 4.6: Ranked extraction candidate lists
4. ✅ Story 4.7: Heat map visualization (THIS STORY)
5. ⏳ Story 4.8: Node labels with scores (NEXT STORY - builds on 4.7)

After Story 4.8:
- Epic 4 is COMPLETE
- Epic 5 (Reporting) can integrate all scoring/visualization features
- CLI becomes fully scoring-aware

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-4-extraction-difficulty-scoring-and-candidate-ranking.md, Story 4.7 (lines 120-135)]
- Story requirements: Color-code nodes by extraction difficulty (green/yellow/red), add legend showing score ranges

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 18-51)]
- Technology stack: .NET 8.0, C# 12, Graphviz 2.38+ external dependency
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces (Visualization), async patterns, file-scoped namespaces
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 114-119)]
- Logging: Structured logging with named placeholders, ILogger<T> injection
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 282-286)]
- Graphviz integration: External tool via Process.Start, timeout handling, error messages with download URL

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\architecture-validation-results.md (lines 43-47)]
- Visualization architecture: DotGenerator (QuikGraph.Graphviz), GraphvizRenderer (Process.Start wrapper)
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md]
- DOT generation decision: Graphviz 2.38+ compatibility, X11 color scheme support

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-6-generate-ranked-extraction-candidate-lists.md]
- ExtractionScore model structure, difficulty category boundaries (0-33, 34-66, 67-100)
- [Source: Epic 3 Stories 3.6-3.7]
- DotGenerator enhancement pattern: Optional parameters for cycles/recommendations, backward compatibility
- [Source: Epic 2 Story 2.8]
- Original DotGenerator implementation: Node/edge styling separation, solution-based colors

**DotGenerator Current Implementation:**
- [Source: Task exploration agent output]
- Current API: GenerateAsync with cycles, recommendations, maxBreakPoints parameters
- Edge color priority: YELLOW (break points) > RED (cycles) > BLUE (cross-solution) > BLACK (default)
- Node colors: Solution-based (8 light colors), single-solution uses "lightblue"
- Legend generation: Edge types, solution colors (multi-solution)
- Test coverage: 15+ tests for basic generation, cycle highlighting, legend generation

**Graphviz Color Specifications:**
- [Source: https://graphviz.org/docs/attr-types/color/]
- Supported formats: X11 named colors (default), RGB hex (#RRGGBB), HSV
- [Source: https://graphviz.org/doc/info/colors.html]
- Color names: Case-insensitive, 100+ X11 colors supported
- Heat map colors: "lightgreen" (#90EE90), "yellow" (#FFFF00), "lightcoral" (#F08080)

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Story 4-6 completed with code review pattern
- Epic 4 stories follow: implementation → code review → fixes → status update
- DotGenerator previously enhanced in Epic 3 (cycle/break-point highlighting)

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - Implementation completed successfully without debugging needs.

### Completion Notes List

✅ Successfully implemented heat map visualization with color-coded extraction difficulty scores:

**Implementation Summary:**
1. Extended IDotGenerator interface to accept optional IReadOnlyList<ExtractionScore>? parameter (backward compatible)
2. Implemented extraction score lookup helper with O(1) dictionary-based access (case-insensitive)
3. Implemented heat map color selection logic:
   - Green (lightgreen): Easy extraction (0-33 scores)
   - Yellow: Medium extraction (34-66 scores)
   - Red (lightcoral): Hard extraction (67-100 scores)
4. Updated BuildDotContent to apply heat map colors when scores provided, fallback to solution-based colors when absent
5. Added extraction difficulty legend generation (horizontal layout with color boxes)
6. Heat map mode takes precedence over solution-based colors but preserves edge highlighting (cycles/break-points)

**Testing:**
- Added 13 comprehensive unit tests covering:
  - Color application for easy/medium/hard scores
  - Boundary condition testing (0, 33, 33.1, 66.9, 67, 100)
  - Backward compatibility (null/empty scores use default colors)
  - Missing project handling (uses default color for projects without scores)
  - Legend generation verification
  - Compatibility with existing cycle/break-point highlighting
  - Mixed difficulty scenarios
  - **NEW:** Duplicate project name handling (keeps first occurrence, logs warning)
  - **NEW:** Orphaned extraction scores handling (scores for projects not in graph are ignored)
- All 54 DotGeneratorTests pass (including 41 existing + 13 new heat map tests)

**Architecture Compliance:**
- Feature-based namespace: MasDependencyMap.Core.Visualization (consistent with existing)
- File-scoped namespace declarations
- Structured logging with named placeholders
- XML documentation on all new public APIs
- ConfigureAwait(false) in library code
- No new NuGet dependencies required

**Integration:**
- CLI updated to pass null for extractionScores parameter (Epic 5 integration deferred as planned)
- DI registration unchanged (DotGenerator already registered in Program.cs)
- Graphviz X11 color scheme used (lightgreen, yellow, lightcoral) - widely supported

**Key Design Decisions:**
1. Used lightcoral instead of pure red for better readability on nodes
2. Heat map legend placed after edge type legend to maintain priority visibility
3. Case-insensitive project name matching for robustness
4. Graceful degradation when scores empty or projects missing from score list

**Code Review Fixes (Post-Implementation):**
✅ **Issue #1 (HIGH):** Fixed unhandled ArgumentException for duplicate project names
  - Changed BuildExtractionScoreLookup from .ToDictionary() to manual loop with TryAdd()
  - Now gracefully handles duplicates: keeps first occurrence, logs warning for each duplicate
  - Added aggregate warning log when duplicates found
  - Location: DotGenerator.cs:163-197

✅ **Issue #2 (HIGH):** Added missing test for duplicate extraction scores
  - Test: GenerateAsync_WithDuplicateProjectNamesInScores_UsesFirstOccurrence
  - Validates first occurrence wins, duplicate ignored, warning logged
  - Location: DotGeneratorTests.cs:2104-2141

✅ **Issue #3 (MEDIUM):** Fixed inconsistent task completion markers in story file
  - All parent tasks now marked [x] when subtasks complete
  - 10 parent tasks updated to reflect actual completion status
  - Improves story tracking and sprint status accuracy

✅ **Issue #4 (MEDIUM):** Added missing test for orphaned extraction scores
  - Test: GenerateAsync_WithOrphanedScoresNotInGraph_IgnoresExtraScores
  - Validates scores for non-existent projects are silently ignored
  - Location: DotGeneratorTests.cs:2143-2185

**Final Metrics:**
- **Test Coverage:** 54 tests, 100% pass rate
- **Code Quality:** 0 HIGH issues remaining, 0 MEDIUM issues remaining
- **Implementation Status:** ✅ COMPLETE - All acceptance criteria met, code review passed

### File List

Modified files:
- src/MasDependencyMap.Core/Visualization/IDotGenerator.cs
- src/MasDependencyMap.Core/Visualization/DotGenerator.cs
- src/MasDependencyMap.CLI/Program.cs
- tests/MasDependencyMap.Core.Tests/Visualization/DotGeneratorTests.cs

## Change Log

- 2026-01-26: Story 4.7 implemented - Added heat map visualization with color-coded extraction difficulty scores to DotGenerator. Extended IDotGenerator interface with optional extractionScores parameter (backward compatible). Implemented green/yellow/red node coloring based on extraction difficulty categories (0-33 Easy, 34-66 Medium, 67-100 Hard). Added extraction difficulty legend generation. Preserved existing cycle and break-point edge highlighting. Added 11 comprehensive unit tests. All 52 DotGeneratorTests pass. CLI updated for backward compatibility.
- 2026-01-26: Code review fixes applied - Fixed duplicate project name handling (HIGH: now uses TryAdd loop instead of ToDictionary, logs warnings). Added 2 new tests for duplicate and orphaned scores (HIGH/MEDIUM). Fixed inconsistent task completion markers (MEDIUM: 10 parent tasks updated). All 54 DotGeneratorTests pass. Ready for done status.
