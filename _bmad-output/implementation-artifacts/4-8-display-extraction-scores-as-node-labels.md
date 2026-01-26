# Story 4.8: Display Extraction Scores as Node Labels

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want extraction difficulty scores shown as labels on graph nodes,
So that I can see exact scores alongside the color-coding.

## Acceptance Criteria

**Given** Extraction scores are calculated for all projects
**When** DotGenerator.GenerateAsync() is called with scoring information
**Then** Each node label includes both project name and extraction score
**And** Label format is: "ProjectName\nScore: 23"
**And** Scores are displayed with clear formatting (no decimal places needed)
**And** Label remains readable on both colored backgrounds and when rendered as PNG/SVG
**And** Enabling/disabling score labels can be configured via visualization settings

## Tasks / Subtasks

- [x] Update IDotGenerator interface to accept showScoreLabels parameter (AC: Configuration support)
  - [x]Add optional bool showScoreLabels = false parameter to GenerateAsync method
  - [x]Update XML documentation to explain score label display behavior
  - [x]Maintain backward compatibility (default false = existing label behavior)
  - [x]Place parameter after extractionScores parameter in signature

- [x]Update DotGenerator implementation signature (AC: Accept configuration)
  - [x]Add showScoreLabels parameter to GenerateAsync method
  - [x]Pass showScoreLabels to BuildDotContent method
  - [x]Maintain existing heat map color behavior (independent from labels)
  - [x]ConfigureAwait(false) for all async operations (existing pattern)

- [x]Implement score label formatting logic (AC: Label format with scores)
  - [x]Create private method: FormatNodeLabel(string projectName, double? score, bool showScore)
  - [x]When showScore=false: Return project name only (existing behavior)
  - [x]When showScore=true AND score available: Return "ProjectName\nScore: XX" format
  - [x]When showScore=true BUT score=null: Return project name only (graceful degradation)
  - [x]Round FinalScore to integer (0 decimal places): Math.Round(score.Value)
  - [x]Use \n escape sequence for multi-line label in DOT format

- [x]Update BuildDotContent node label generation (AC: Apply score labels)
  - [x]Check if showScoreLabels=true AND scoreLookup is available
  - [x]For each node, lookup extraction score using existing GetExtractionScore helper
  - [x]Format node label using FormatNodeLabel method
  - [x]Apply label to node: node [label="...", fillcolor=...]
  - [x]Preserve existing heat map color logic (Story 4.7 - unchanged)
  - [x]Log Debug: "Applied score labels to {LabeledNodeCount} nodes"

- [x]Implement font color for label readability (AC: Readable on colored backgrounds)
  - [x]Create private method: GetFontColorForBackground(string backgroundColor)
  - [x]Return "black" for lightgreen and yellow backgrounds
  - [x]Return "white" for lightcoral background
  - [x]Return "black" for default colors (lightblue, other solution colors)
  - [x]Apply fontcolor attribute to nodes: node [label="...", fillcolor="...", fontcolor="..."]

- [x]Handle edge case: showScoreLabels=true but extractionScores=null/empty (AC: Graceful degradation)
  - [x]If showScoreLabels=true BUT scoreLookup is null, log Warning
  - [x]Warning message: "Score labels requested but no extraction scores provided, showing project names only"
  - [x]Continue with project name-only labels (no score suffix)
  - [x]Graph generation continues normally

- [x]Handle edge case: showScoreLabels=false with extractionScores provided (AC: Labels independent from colors)
  - [x]Heat map colors still apply (Story 4.7 behavior)
  - [x]Node labels show project name only (no score)
  - [x]This allows: color-coded graph WITHOUT score labels (user preference)
  - [x]Log Debug: "Heat map colors applied without score labels"

- [x]Update DI registration (AC: No changes needed)
  - [x]DotGenerator already registered as singleton in Program.cs
  - [x]No new dependencies added (same ILogger<DotGenerator>)
  - [x]Verify registration still present after implementation

- [x]Create comprehensive unit tests (AC: Test coverage)
  - [x]Extend DotGeneratorTests.cs in tests/MasDependencyMap.Core.Tests/Visualization/
  - [x]Test: GenerateAsync_WithScoreLabelsEnabled_IncludesScoresInLabels (validate label format)
  - [x]Test: GenerateAsync_WithScoreLabelsDisabled_ShowsProjectNamesOnly (backward compatibility)
  - [x]Test: GenerateAsync_WithScoreLabelsAndNoScores_ShowsProjectNamesOnly (graceful degradation)
  - [x]Test: GenerateAsync_WithScoreLabelsEnabled_RoundsToInteger (score 85.7 → "Score: 86")
  - [x]Test: GenerateAsync_WithScoreLabelsEnabled_UsesCorrectFontColor (lightgreen/yellow → black, lightcoral → white)
  - [x]Test: GenerateAsync_WithHeatMapButNoLabels_AppliesColorsOnly (colors without scores)
  - [x]Test: GenerateAsync_WithBothScoreLabelsAndHeatMap_CombinesBothFeatures (integration)
  - [x]Test: GenerateAsync_ScoreLabelFormat_MatchesExpectedPattern (regex validation: "Name\nScore: \d+")
  - [x]Use xUnit, FluentAssertions for assertions
  - [x]Test naming: {MethodName}_{Scenario}_{ExpectedResult} pattern

- [x]Validate against project-context.md rules (AC: Architecture compliance)
  - [x]Feature-based namespace: MasDependencyMap.Core.Visualization (consistent with existing)
  - [x]Async suffix on async methods (GenerateAsync maintains existing pattern)
  - [x]File-scoped namespace declarations
  - [x]ILogger injection via constructor (existing)
  - [x]ConfigureAwait(false) in library code
  - [x]XML documentation on all public APIs
  - [x]Structured logging with named placeholders

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 4.8 Score Label Display Requirements:**

This story implements **NODE LABELS with extraction scores**, building directly on Story 4.7's heat map color-coding. Together, they provide both visual (color) and precise (numeric) difficulty information.

**Epic 4 Vision (Recap):**
- Story 4.1-4.4: Individual metric calculators ✅ DONE
- Story 4.5: Combined extraction score calculator ✅ DONE
- Story 4.6: Ranked extraction candidate lists ✅ DONE
- Story 4.7: Heat map visualization with color-coded scores ✅ DONE
- **Story 4.8: Display extraction scores as node labels (THIS STORY - FINAL VISUALIZATION)**

**Story 4.8 Unique Characteristics:**

1. **Builds on Story 4.7:**
   - Story 4.7: NODE COLORS based on difficulty categories (green/yellow/red)
   - Story 4.8: NODE LABELS with precise scores (e.g., "ProjectName\nScore: 45")
   - **Sequential enhancement:** 4.7 provides visual buckets, 4.8 adds precise numbers
   - **Independent features:** Users can enable colors WITHOUT labels, or both together

2. **Label-Only Enhancement, No New Calculations:**
   - Stories 4.1-4.5: Heavy calculation logic (metrics, scoring)
   - Story 4.7: Color mapping visualization
   - Story 4.8: LABEL FORMATTING only - displays existing score data
   - **No new algorithms:** Pure DOT label formatting with readability optimizations

3. **Configurable Display:**
   - New parameter: `showScoreLabels` (default: false for backward compatibility)
   - Allows four visualization modes:
     1. Default colors, no scores (showScoreLabels=false, extractionScores=null)
     2. Heat map colors, no labels (showScoreLabels=false, extractionScores=provided)
     3. No colors, with labels (showScoreLabels=true, extractionScores=provided, but ignore colors)
     4. Heat map colors + score labels (showScoreLabels=true, extractionScores=provided) ← **Most useful**
   - **Flexibility:** Users choose their preferred visualization style

4. **Readability on Colored Backgrounds:**
   - Story 4.7 created colored nodes (lightgreen, yellow, lightcoral)
   - Story 4.8 ensures text is readable on those backgrounds
   - **Font color strategy:**
     - Light backgrounds (lightgreen, yellow) → black text
     - Dark backgrounds (lightcoral) → white text
   - **Tested rendering:** Must work in both PNG and SVG output formats

5. **Graceful Degradation:**
   - showScoreLabels=true but extractionScores=null → show project names only, log warning
   - Score available but null for specific project → show project name only for that node
   - **No failures:** Feature requests degrade to safe defaults, never break graph generation

🚨 **CRITICAL - DotGenerator Current State After Story 4.7:**

**Existing DotGenerator Implementation (from Story 4.7):**

Located at: `src/MasDependencyMap.Core/Visualization/DotGenerator.cs`

**Current GenerateAsync Signature (Story 4.7):**
```csharp
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null, // Added in Story 4.7
    CancellationToken cancellationToken = default)
```

**Current Node Label Generation (Line 358 of DotGenerator.cs):**
```csharp
builder.AppendLine($"    {escapedName} [label={escapedName}, fillcolor=\"{nodeColor}\"];");
```

**Story 4.8 Changes:**

1. **NEW Optional Parameter:**
   ```csharp
   // UPDATED signature (Story 4.8)
   public async Task<string> GenerateAsync(
       DependencyGraph graph,
       string outputDirectory,
       string solutionName,
       IReadOnlyList<CycleInfo>? cycles = null,
       IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
       int maxBreakPoints = 10,
       IReadOnlyList<ExtractionScore>? extractionScores = null,
       bool showScoreLabels = false,  // NEW PARAMETER (Story 4.8)
       CancellationToken cancellationToken = default)
   ```

2. **Updated Node Label Generation:**
   ```csharp
   // NEW: Format label with score if requested
   string labelText = FormatNodeLabel(vertex.ProjectName, score?.FinalScore, showScoreLabels);
   string escapedLabel = EscapeDotIdentifier(labelText);

   // NEW: Get appropriate font color for background
   string fontColor = GetFontColorForBackground(nodeColor);

   // UPDATED: Include fontcolor attribute for readability
   builder.AppendLine($"    {escapedName} [label={escapedLabel}, fillcolor=\"{nodeColor}\", fontcolor=\"{fontColor}\"];");
   ```

3. **Label Formatting Logic:**
   ```csharp
   private string FormatNodeLabel(string projectName, double? finalScore, bool showScore)
   {
       if (!showScore || finalScore == null)
           return projectName;  // Existing behavior

       // Round to integer (no decimal places)
       int scoreInt = (int)Math.Round(finalScore.Value);

       // Multi-line format: "ProjectName\nScore: XX"
       return $"{projectName}\\nScore: {scoreInt}";
   }
   ```

4. **Font Color for Readability:**
   ```csharp
   private string GetFontColorForBackground(string backgroundColor)
   {
       // Dark/medium backgrounds need white text
       if (backgroundColor == "lightcoral")
           return "white";

       // Light backgrounds need black text
       // lightgreen, yellow, lightblue, and other solution colors
       return "black";
   }
   ```

🚨 **CRITICAL - Graphviz DOT Label Syntax:**

**Multi-Line Label Format (from Graphviz 2.38+ docs):**

Story 4.8 uses backslash escape sequences for multi-line labels:

```dot
// DOT format syntax (what Story 4.8 will generate)
"ProjectA" [label="ProjectA\nScore: 85", fillcolor="lightgreen", fontcolor="black"];
"ProjectB" [label="ProjectB\nScore: 42", fillcolor="yellow", fontcolor="black"];
"ProjectC" [label="ProjectC\nScore: 73", fillcolor="lightcoral", fontcolor="white"];
```

**Escape Sequence Options:**
- `\n` - Centered text (RECOMMENDED for scores)
- `\l` - Left-justified text
- `\r` - Right-justified text

**Why `\n` (centered)?**
- Project names and scores are both centered → visually balanced
- Consistent with existing single-line labels (which are centered by default)
- Most intuitive for users viewing dependency graphs

**Label Escaping Rules:**

From existing `EscapeDotIdentifier` method (Epic 2):
```csharp
private static string EscapeDotIdentifier(string identifier)
{
    // Wrap in quotes and escape internal quotes
    return $"\"{identifier.Replace("\"", "\\\"")}\"";
}
```

**Story 4.8 Usage:**
```csharp
// Format label first (includes \n for multi-line)
string labelText = FormatNodeLabel("My.Project", 85.6, showScoreLabels: true);
// Result: "My.Project\nScore: 86"

// Then escape for DOT format
string escapedLabel = EscapeDotIdentifier(labelText);
// Result: "\"My.Project\\nScore: 86\""

// Apply to node
builder.AppendLine($"    \"My.Project\" [label={escapedLabel}, ...];");
```

**Key Point:** `EscapeDotIdentifier` already handles necessary escaping. The `\n` sequence will be preserved correctly in the output DOT file and interpreted as newline by Graphviz.

🚨 **CRITICAL - Font Color for Readability (Based on Graphviz Research):**

**Background Color to Font Color Mapping:**

Story 4.7 defined three heat map background colors. Story 4.8 chooses appropriate text colors:

| Background Color | RGB | Lightness | Font Color | Rationale |
|-----------------|-----|-----------|------------|-----------|
| lightgreen | #90EE90 | Light | **black** | High contrast, clear readability |
| yellow | #FFFF00 | Very Light | **black** | Standard (traffic lights), excellent contrast |
| lightcoral | #F08080 | Medium | **white** | Prevents washed-out appearance, clear on reddish tones |

**Default Solution Colors (from Story 4.7):**
```csharp
private static readonly string[] SolutionNodeColors = {
    "lightblue", "lightgreen", "lightyellow", "lightpink",
    "lightcyan", "lavender", "lightsalmon", "lightgray"
};
```

**Font Color Strategy for Solution Colors:**
- All solution colors are light shades → use **black** font color
- Consistent with heat map light backgrounds (lightgreen, yellow)

**Font Color Decision Logic:**
```csharp
private string GetFontColorForBackground(string backgroundColor)
{
    // Only lightcoral needs white text (medium-dark background)
    if (backgroundColor == "lightcoral")
        return "white";

    // All other colors (lightgreen, yellow, lightblue, etc.) use black
    return "black";
}
```

**Testing Requirements:**
- Verify black text on lightgreen: ✅ Clear, high contrast
- Verify black text on yellow: ✅ Clear, standard traffic light pattern
- Verify white text on lightcoral: ✅ Clear, avoids washed-out appearance
- Verify black text on lightblue (default): ✅ Clear
- Verify readability in both PNG and SVG output formats

🚨 **CRITICAL - Score Formatting and Rounding:**

**Score Characteristics (from ExtractionScore model):**
```csharp
public sealed record ExtractionScore(
    string ProjectName,
    string ProjectPath,
    double FinalScore,  // 0-100 range, can have decimals
    ...
)
```

**Acceptance Criteria Requirement:**
> "Scores are displayed with clear formatting (no decimal places needed)"

**Implementation:**
```csharp
private string FormatNodeLabel(string projectName, double? finalScore, bool showScore)
{
    if (!showScore || finalScore == null)
        return projectName;

    // Round to nearest integer (no decimals)
    int scoreInt = (int)Math.Round(finalScore.Value);

    // Format: "ProjectName\nScore: XX"
    return $"{projectName}\\nScore: {scoreInt}";
}
```

**Rounding Examples:**
```csharp
FinalScore = 85.3  → "Score: 85"
FinalScore = 85.5  → "Score: 86" (standard rounding)
FinalScore = 85.7  → "Score: 86"
FinalScore = 33.4  → "Score: 33" (Easy category)
FinalScore = 33.5  → "Score: 34" (Medium category - rounding affects category!)
FinalScore = 66.5  → "Score: 67" (Hard category - rounding affects category!)
```

**Important Edge Case:**
- Score 33.5 rounds to 34 (Medium), but might have been colored green (Easy ≤ 33)
- This is acceptable: Label shows precise score, color shows category
- User sees both: "This project (34) is just barely Medium, colored yellow"
- **No action needed:** This slight mismatch is informative, not a bug

**Alternative Considered:**
- Use `(int)finalScore` (truncation) instead of `Math.Round`
- **Rejected:** Standard rounding (banker's rounding) is more accurate
- Example: 85.9 should display as 86, not 85

🚨 **CRITICAL - Configuration Parameter Design:**

**Parameter Placement:**
```csharp
Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null,
    bool showScoreLabels = false,  // NEW - placed AFTER extractionScores
    CancellationToken cancellationToken = default);
```

**Why After extractionScores?**
1. **Logical grouping:** showScoreLabels is related to extractionScores (uses that data)
2. **Optional parameter order:** C# requires optional params at end of signature
3. **Backward compatibility:** Existing callers don't need to change if they don't pass extractionScores

**Default Value: false**

**Why false?**
1. **Backward compatibility:** Existing behavior (no score labels) is preserved
2. **Progressive enhancement:** Users opt-in to new feature
3. **Consistency with Story 4.7:** extractionScores=null meant "don't use scores", showScoreLabels=false means "don't show labels"

**Alternative Considered:**
- Default to `true` when extractionScores is provided
- **Rejected:** Users might want heat map colors WITHOUT labels (cleaner visualization for presentations)

**Usage Patterns:**

```csharp
// Pattern 1: Default (backward compatible)
await dotGenerator.GenerateAsync(graph, outputDir, solutionName);
// Result: Default colors, no scores

// Pattern 2: Heat map colors only (Story 4.7 usage)
await dotGenerator.GenerateAsync(graph, outputDir, solutionName,
    extractionScores: scores);
// Result: Green/yellow/red colors, no score labels

// Pattern 3: Heat map + score labels (Story 4.8 full feature)
await dotGenerator.GenerateAsync(graph, outputDir, solutionName,
    extractionScores: scores,
    showScoreLabels: true);
// Result: Green/yellow/red colors + "Score: XX" labels

// Pattern 4: Scores without colors (edge case, but supported)
await dotGenerator.GenerateAsync(graph, outputDir, solutionName,
    extractionScores: scores,
    showScoreLabels: true);
// Result: Default solution colors + "Score: XX" labels
```

### Technical Requirements

**Existing Component Enhancement:**

Story 4.8 extends `DotGenerator` (src/MasDependencyMap.Core/Visualization/DotGenerator.cs), previously enhanced by:
- Epic 2: Basic DOT generation
- Epic 3: Cycle and break-point highlighting (red/yellow edges)
- Story 4.7: Heat map node coloring (green/yellow/red nodes)

**No New Namespaces:**

All work happens in existing `MasDependencyMap.Core.Visualization` namespace:

```
src/MasDependencyMap.Core/Visualization/
├── IDotGenerator.cs              # MODIFY: Add showScoreLabels parameter
├── DotGenerator.cs               # MODIFY: Implement label formatting logic
├── IGraphvizRenderer.cs          # NO CHANGE
└── GraphvizRenderer.cs           # NO CHANGE
```

**Dependencies:**

Story 4.8 depends on Story 4.7's ExtractionScore integration:

```csharp
using MasDependencyMap.Core.ExtractionScoring;  // For ExtractionScore type (Story 4.7)

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

// NO CHANGES NEEDED for Story 4.8
```

**Interface Update (Backward Compatible):**

```csharp
// IDotGenerator.cs (BEFORE - Story 4.7)
Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null,
    CancellationToken cancellationToken = default);

// IDotGenerator.cs (AFTER - Story 4.8)
Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null,
    bool showScoreLabels = false,  // NEW OPTIONAL PARAMETER (Story 4.8)
    CancellationToken cancellationToken = default);
```

**Backward Compatibility Guarantee:**

- Optional parameter with false default
- All existing callers continue to work without changes (no score labels shown)
- CLI integration in Epic 5 will pass showScoreLabels: true
- Story 4.7 callers (if any) remain functional with color-only visualization

### Architecture Compliance

**Feature-Based Namespace (Existing):**

Story 4.8 works within existing `MasDependencyMap.Core.Visualization` namespace (established in Epic 2, enhanced in Epic 3 and Story 4.7).

**Async Pattern (Preserved):**

```csharp
// Existing pattern (maintained - no new async operations)
public async Task<string> GenerateAsync(...)
{
    // ... existing logic ...

    // NEW: Label formatting (synchronous logic, no new async calls)
    string labelText = FormatNodeLabel(vertex.ProjectName, score?.FinalScore, showScoreLabels);
    string fontColor = GetFontColorForBackground(nodeColor);

    // ... rest of existing logic ...

    await File.WriteAllTextAsync(...).ConfigureAwait(false);  // Existing
}
```

**Structured Logging (Extended):**

```csharp
// Existing logs (preserved from Story 4.7)
_logger.LogInformation("Generating DOT graph for {SolutionName} with {VertexCount} projects and {EdgeCount} dependencies", ...);
_logger.LogDebug("Applying heat map colors based on {ScoreCount} extraction scores", extractionScores.Count);

// NEW logs for Story 4.8
_logger.LogWarning(
    "Score labels requested but no extraction scores provided, showing project names only");

_logger.LogDebug(
    "Applied score labels to {LabeledNodeCount} of {TotalNodeCount} nodes",
    labeledNodeCount, totalNodeCount);

_logger.LogDebug(
    "Generated node labels with scores: {EasyCount} easy, {MediumCount} medium, {HardCount} hard",
    easyCount, mediumCount, hardCount);
```

**XML Documentation (Required):**

```csharp
/// <summary>
/// Generates a DOT format graph file from the dependency graph with optional heat map visualization and score labels.
/// </summary>
/// <param name="graph">The dependency graph to visualize.</param>
/// <param name="outputDirectory">Directory where the DOT file will be saved.</param>
/// <param name="solutionName">Name of the solution being analyzed.</param>
/// <param name="cycles">Optional cycle information for edge highlighting.</param>
/// <param name="recommendations">Optional cycle-breaking recommendations for edge highlighting.</param>
/// <param name="maxBreakPoints">Maximum number of break point edges to highlight (default 10).</param>
/// <param name="extractionScores">Optional extraction difficulty scores for heat map node coloring and score labels. When provided, nodes can be colored green (easy 0-33), yellow (medium 34-66), or red (hard 67-100) based on Story 4.7.</param>
/// <param name="showScoreLabels">When true and extractionScores are provided, node labels include extraction scores in format "ProjectName\nScore: XX". When false, labels show project names only (default behavior). Requires extractionScores to be non-null and non-empty to display scores.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Absolute path to the generated DOT file.</returns>
/// <exception cref="ArgumentNullException">If graph, outputDirectory, or solutionName is null.</exception>
/// <exception cref="IOException">If file write fails.</exception>
```

### Library/Framework Requirements

**Existing Libraries (No New Packages):**

All dependencies already installed from previous epics:
- ✅ QuikGraph v2.5.0 - Graph data structures (DependencyGraph parameter)
- ✅ Microsoft.Extensions.Logging.Console - Structured logging
- ✅ System.IO - File operations
- ✅ System.Math - Rounding operations (built-in)

**No New NuGet Packages Required for Story 4.8** ✅

**Graphviz External Dependency (Existing):**

Story 4.8 generates enhanced DOT format text files. Graphviz rendering (DOT → PNG/SVG) is handled by GraphvizRenderer (Epic 2 - Story 2.9).

- Graphviz version: 2.38+ (external tool, must be in PATH)
- Multi-line labels: `\n` escape sequence (supported since Graphviz 2.0)
- Font color support: `fontcolor` attribute (X11 color names)
- No special Graphviz features required for Story 4.8

**Label Syntax Validation:**

Graphviz supports multi-line labels using escape sequences:
- `\n` (newline, centered) ✅ Used in Story 4.8
- `\l` (newline, left-aligned)
- `\r` (newline, right-aligned)

Font color attribute:
- `fontcolor="black"` ✅ Supported since Graphviz 2.0
- `fontcolor="white"` ✅ Supported since Graphviz 2.0

### File Structure Requirements

**Files to Modify:**

```
src/MasDependencyMap.Core/Visualization/
├── IDotGenerator.cs                              # MODIFY: Add showScoreLabels parameter to interface
└── DotGenerator.cs                               # MODIFY: Implement score label logic (2 new private methods, update node generation)

tests/MasDependencyMap.Core.Tests/Visualization/
└── DotGeneratorTests.cs                          # MODIFY: Add ~8 new tests for score labels

_bmad-output/implementation-artifacts/
├── sprint-status.yaml                            # MODIFY: Update story 4-8 status from backlog to ready-for-dev
└── 4-8-display-extraction-scores-as-node-labels.md  # CREATE: This story file
```

**Files to Update (Status Only After Implementation):**

```
_bmad-output/implementation-artifacts/sprint-status.yaml      # MODIFY: Update story 4-8 from ready-for-dev to in-progress to done
```

**No New Files Created:**

Story 4.8 extends existing components, no new files needed (except this story file itself).

**No CLI Integration Yet:**

DotGenerator enhancement is ready, but CLI doesn't yet use showScoreLabels parameter. CLI integration happens in Epic 5 (Reporting):
- Story 5.1: Text report generator (may include scored visualizations)
- Story 5.3: Extraction difficulty section in reports (likely uses score labels)

For now:
- Extend DotGenerator interface and implementation
- Add comprehensive tests (including integration tests with mock scores)
- CLI integration deferred to Epic 5 (pass showScoreLabels based on user preference or config)

### Testing Requirements

**Test Class: DotGeneratorTests.cs (Existing - Extended)**

**Test Strategy:**

Extend existing DotGeneratorTests with score label scenarios:
- Story 4.8: Add ~8 new tests for score label functionality
- Story 4.7: Added ~13 tests for heat map functionality
- Existing tests: ~41 tests for cycles, break-points, multi-solution (Epic 2/3)
- Total tests after Story 4.8: ~62 tests

**New Test Coverage:**

1. **Basic Score Label Display:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithScoreLabelsEnabled_IncludesScoresInLabels()
   {
       // Arrange
       var graph = CreateGraphWithProjects("ProjectA", "ProjectB");
       var scores = new List<ExtractionScore>
       {
           CreateScore("ProjectA", 85.3),  // Should display as 85
           CreateScore("ProjectB", 42.7)   // Should display as 43
       };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var dotPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution",
               extractionScores: scores,
               showScoreLabels: true);

           // Assert
           var dotContent = await File.ReadAllTextAsync(dotPath);
           dotContent.Should().Contain("label=\"ProjectA\\nScore: 85\"");
           dotContent.Should().Contain("label=\"ProjectB\\nScore: 43\"");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

2. **Backward Compatibility (showScoreLabels=false):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithScoreLabelsDisabled_ShowsProjectNamesOnly()
   {
       // Arrange
       var graph = CreateGraphWithProjects("ProjectA");
       var scores = new List<ExtractionScore> { CreateScore("ProjectA", 85.0) };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act (showScoreLabels defaults to false)
           var dotPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution",
               extractionScores: scores);

           // Assert
           var dotContent = await File.ReadAllTextAsync(dotPath);
           dotContent.Should().Contain("label=\"ProjectA\"");
           dotContent.Should().NotContain("Score:");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

3. **Graceful Degradation (showScoreLabels=true but no scores):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithScoreLabelsEnabledButNoScores_ShowsProjectNamesOnly()
   {
       // Arrange
       var graph = CreateGraphWithProjects("ProjectA");
       var outputDir = CreateTempDirectory();

       try
       {
           // Act (showScoreLabels=true but extractionScores=null)
           var dotPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution",
               showScoreLabels: true);

           // Assert
           var dotContent = await File.ReadAllTextAsync(dotPath);
           dotContent.Should().Contain("label=\"ProjectA\"");
           dotContent.Should().NotContain("Score:");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

4. **Score Rounding:**
   ```csharp
   [Theory]
   [InlineData(85.3, "Score: 85")]
   [InlineData(85.5, "Score: 86")]  // Standard rounding
   [InlineData(85.7, "Score: 86")]
   [InlineData(0.0, "Score: 0")]
   [InlineData(100.0, "Score: 100")]
   [InlineData(33.4, "Score: 33")]  // Easy
   [InlineData(33.5, "Score: 34")]  // Medium (rounding up)
   [InlineData(66.9, "Score: 67")]  // Hard (rounding up)
   public async Task GenerateAsync_WithScoreLabelsEnabled_RoundsScoreToInteger(
       double score, string expectedLabel)
   {
       // Arrange
       var graph = CreateGraphWithProjects("Project");
       var scores = new List<ExtractionScore> { CreateScore("Project", score) };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var dotPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution",
               extractionScores: scores,
               showScoreLabels: true);

           // Assert
           var dotContent = await File.ReadAllTextAsync(dotPath);
           dotContent.Should().Contain($"label=\"Project\\n{expectedLabel}\"");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

5. **Font Color Readability:**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithScoreLabelsOnColoredBackgrounds_UsesReadableFontColors()
   {
       // Arrange
       var graph = CreateGraphWithProjects("EasyProject", "MediumProject", "HardProject");
       var scores = new List<ExtractionScore>
       {
           CreateScore("EasyProject", 25.0),    // lightgreen background
           CreateScore("MediumProject", 50.0),  // yellow background
           CreateScore("HardProject", 75.0)     // lightcoral background
       };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var dotPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution",
               extractionScores: scores,
               showScoreLabels: true);

           // Assert
           var dotContent = await File.ReadAllTextAsync(dotPath);

           // Easy: lightgreen background → black text
           dotContent.Should().Contain("\"EasyProject\" [label=\"EasyProject\\nScore: 25\", fillcolor=\"lightgreen\", fontcolor=\"black\"]");

           // Medium: yellow background → black text
           dotContent.Should().Contain("\"MediumProject\" [label=\"MediumProject\\nScore: 50\", fillcolor=\"yellow\", fontcolor=\"black\"]");

           // Hard: lightcoral background → white text
           dotContent.Should().Contain("\"HardProject\" [label=\"HardProject\\nScore: 75\", fillcolor=\"lightcoral\", fontcolor=\"white\"]");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

6. **Heat Map Without Labels (User Choice):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithHeatMapButNoLabels_AppliesColorsOnly()
   {
       // Arrange
       var graph = CreateGraphWithProjects("ProjectA");
       var scores = new List<ExtractionScore> { CreateScore("ProjectA", 25.0) };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act (extractionScores provided, showScoreLabels=false)
           var dotPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution",
               extractionScores: scores,
               showScoreLabels: false);

           // Assert
           var dotContent = await File.ReadAllTextAsync(dotPath);
           dotContent.Should().Contain("fillcolor=\"lightgreen\"");
           dotContent.Should().Contain("label=\"ProjectA\"");  // No score
           dotContent.Should().NotContain("Score:");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

7. **Combined Features (Heat Map + Labels):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithBothHeatMapAndScoreLabels_CombinesBothFeatures()
   {
       // Arrange
       var graph = CreateGraphWithProjects("ProjectA", "ProjectB");
       var scores = new List<ExtractionScore>
       {
           CreateScore("ProjectA", 25.0),  // Easy (green)
           CreateScore("ProjectB", 75.0)   // Hard (red)
       };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var dotPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution",
               extractionScores: scores,
               showScoreLabels: true);

           // Assert
           var dotContent = await File.ReadAllTextAsync(dotPath);

           // Heat map colors (Story 4.7)
           dotContent.Should().Contain("fillcolor=\"lightgreen\"");
           dotContent.Should().Contain("fillcolor=\"lightcoral\"");

           // Score labels (Story 4.8)
           dotContent.Should().Contain("Score: 25");
           dotContent.Should().Contain("Score: 75");

           // Combined
           dotContent.Should().Contain("label=\"ProjectA\\nScore: 25\", fillcolor=\"lightgreen\"");
           dotContent.Should().Contain("label=\"ProjectB\\nScore: 75\", fillcolor=\"lightcoral\"");
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

8. **Label Format Validation (Regex):**
   ```csharp
   [Fact]
   public async Task GenerateAsync_WithScoreLabels_MatchesExpectedFormat()
   {
       // Arrange
       var graph = CreateGraphWithProjects("MyProject");
       var scores = new List<ExtractionScore> { CreateScore("MyProject", 42.0) };
       var outputDir = CreateTempDirectory();

       try
       {
           // Act
           var dotPath = await _generator.GenerateAsync(
               graph, outputDir, "TestSolution",
               extractionScores: scores,
               showScoreLabels: true);

           // Assert
           var dotContent = await File.ReadAllTextAsync(dotPath);

           // Validate label format: "ProjectName\nScore: XX"
           var labelPattern = @"label=""MyProject\\nScore: \d+""";
           dotContent.Should().MatchRegex(labelPattern);
       }
       finally
       {
           CleanupTempDirectory(outputDir);
       }
   }
   ```

**Test Helper Methods (Reuse Existing + Add New):**

```csharp
// Existing helpers from Epic 2/3/Story 4.7 (reuse)
private DependencyGraph CreateGraphWithProjects(params string[] projectNames) { ... }
private IReadOnlyList<CycleInfo> CreateCycleInfo() { ... }
private ExtractionScore CreateScore(string projectName, double score) { ... }  // From Story 4.7
private string CreateTempDirectory() { return Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid()); }
private void CleanupTempDirectory(string dir) { if (Directory.Exists(dir)) Directory.Delete(dir, true); }

// No new helpers needed for Story 4.8 (reuse existing from Story 4.7)
```

### Previous Story Intelligence

**From Story 4.7 (Heat Map Visualization) - Color-Coding Pattern:**

Story 4.7 created the foundation for colored nodes. Story 4.8 builds on this by adding text labels:

```csharp
// Story 4.7 pattern: Extract score, determine color
var score = GetExtractionScore(projectName, scoreLookup);
var heatMapColor = GetNodeColorForScore(score?.FinalScore);

// Story 4.8 pattern: Also format label with score
string labelText = FormatNodeLabel(projectName, score?.FinalScore, showScoreLabels);
string fontColor = GetFontColorForBackground(heatMapColor ?? nodeColor);
```

**Pattern Reuse from Story 4.7:**

```csharp
// Story 4.7: Score lookup helper (REUSE in Story 4.8)
private ExtractionScore? GetExtractionScore(string projectName, Dictionary<string, ExtractionScore>? scoreLookup)
{
    if (scoreLookup == null)
        return null;

    if (scoreLookup.TryGetValue(projectName, out var score))
        return score;

    return null;
}

// Story 4.7: Color selection logic (PRESERVE in Story 4.8)
private string? GetNodeColorForScore(double? finalScore)
{
    if (finalScore == null) return null;
    if (finalScore <= 33) return "lightgreen";   // Easy
    if (finalScore < 67) return "yellow";        // Medium
    return "lightcoral";                         // Hard
}

// Story 4.8: NEW - Label formatting logic
private string FormatNodeLabel(string projectName, double? finalScore, bool showScore)
{
    if (!showScore || finalScore == null)
        return projectName;

    int scoreInt = (int)Math.Round(finalScore.Value);
    return $"{projectName}\\nScore: {scoreInt}";
}

// Story 4.8: NEW - Font color selection logic
private string GetFontColorForBackground(string backgroundColor)
{
    if (backgroundColor == "lightcoral")
        return "white";
    return "black";
}
```

**From Epic 3 (Cycle Detection) - DotGenerator Enhancement Pattern (Continued):**

Epic 3 added optional parameters for cycles and recommendations. Story 4.7 added extractionScores. Story 4.8 adds showScoreLabels:

```csharp
// Epic 2 (original):
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    CancellationToken cancellationToken = default)

// Epic 3 (cycle highlighting):
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,               // Added
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,  // Added
    int maxBreakPoints = 10,                               // Added
    CancellationToken cancellationToken = default)

// Story 4.7 (heat map colors):
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null,  // Added
    CancellationToken cancellationToken = default)

// Story 4.8 (score labels):
public async Task<string> GenerateAsync(
    DependencyGraph graph,
    string outputDirectory,
    string solutionName,
    IReadOnlyList<CycleInfo>? cycles = null,
    IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
    int maxBreakPoints = 10,
    IReadOnlyList<ExtractionScore>? extractionScores = null,
    bool showScoreLabels = false,                             // Added
    CancellationToken cancellationToken = default)
```

**Consistent Enhancement Pattern:**
- Each enhancement adds optional parameters
- Default values ensure backward compatibility
- Features are composable (can use independently or together)

**From Epic 2 (DOT Generation) - Label Escaping:**

DotGenerator already has label escaping logic (Epic 2 foundation):

```csharp
// Epic 2: Existing helper for escaping DOT identifiers
private static string EscapeDotIdentifier(string identifier)
{
    // Wrap in quotes and escape internal quotes
    return $"\"{identifier.Replace("\"", "\\\"")}\"";
}

// Epic 2: Existing node label generation (before Story 4.8)
builder.AppendLine($"    {escapedName} [label={escapedName}, fillcolor=\"{nodeColor}\"];");

// Story 4.8: Enhanced node label generation
string labelText = FormatNodeLabel(vertex.ProjectName, score?.FinalScore, showScoreLabels);
string escapedLabel = EscapeDotIdentifier(labelText);  // REUSE existing helper
string fontColor = GetFontColorForBackground(nodeColor);

builder.AppendLine($"    {escapedName} [label={escapedLabel}, fillcolor=\"{nodeColor}\", fontcolor=\"{fontColor}\"];");
```

### DotGenerator Current Implementation Details (After Story 4.7)

**Component Location:**

- Interface: `src/MasDependencyMap.Core/Visualization/IDotGenerator.cs`
- Implementation: `src/MasDependencyMap.Core/Visualization/DotGenerator.cs`
- Tests: `tests/MasDependencyMap.Core.Tests/Visualization/DotGeneratorTests.cs`

**Current Node Label Generation (Line 358 of DotGenerator.cs, Story 4.7):**

```csharp
// Story 4.7 implementation (TO BE ENHANCED in Story 4.8)
builder.AppendLine($"    {escapedName} [label={escapedName}, fillcolor=\"{nodeColor}\"];");
```

**Story 4.8 Changes (Line 358 area):**

```csharp
// Story 4.8 implementation (ENHANCED)

// NEW: Format label with score if requested
string labelText = FormatNodeLabel(vertex.ProjectName, score?.FinalScore, showScoreLabels);
string escapedLabel = EscapeDotIdentifier(labelText);

// NEW: Get appropriate font color for readability
string fontColor = GetFontColorForBackground(nodeColor);

// UPDATED: Include score in label and fontcolor for readability
builder.AppendLine($"    {escapedName} [label={escapedLabel}, fillcolor=\"{nodeColor}\", fontcolor=\"{fontColor}\"];");
```

**Current Heat Map Color Logic (Story 4.7 - PRESERVED):**

```csharp
// From DotGenerator.cs (Story 4.7 implementation - lines 318-346)
// Heat map mode: Apply colors based on scores
if (isHeatMapMode && scoreLookup != null)
{
    var score = GetExtractionScore(vertex.ProjectName, scoreLookup);
    if (score != null)
    {
        var heatMapColor = GetNodeColorForScore(score.FinalScore);
        if (heatMapColor != null)
        {
            nodeColor = heatMapColor;
            // ... color distribution tracking ...
        }
    }
    else
    {
        // No score found for this project - use default color
        nodeColor = isMultiSolution && solutionColorMap.ContainsKey(vertex.SolutionName)
            ? solutionColorMap[vertex.SolutionName]
            : "lightblue";
    }
}
// Default mode: color by solution
else if (isMultiSolution && solutionColorMap.ContainsKey(vertex.SolutionName))
{
    nodeColor = solutionColorMap[vertex.SolutionName];
}
else
{
    nodeColor = "lightblue";
}

// Story 4.7: Generate node (TO BE ENHANCED in Story 4.8)
builder.AppendLine($"    {escapedName} [label={escapedName}, fillcolor=\"{nodeColor}\"];");
```

**Test Coverage (Existing Foundation from Story 4.7):**

From DotGeneratorTests.cs (Story 4.7):
- ✅ Basic graph generation (Epic 2)
- ✅ Multi-solution support (Epic 2)
- ✅ Cycle highlighting (Epic 3)
- ✅ Break-point highlighting (Epic 3)
- ✅ Edge priority handling (Epic 3)
- ✅ Heat map color application (Story 4.7)
- ✅ Heat map boundary testing (Story 4.7)
- ✅ Heat map legend generation (Story 4.7)
- ✅ Backward compatibility with null scores (Story 4.7)
- ✅ Special character escaping (Epic 2)
- ✅ Edge cases (null parameters, empty graphs) (Epic 2)

Story 4.8 extends with score label tests (~8 new tests, total ~62 tests).

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 4.8 completes Epic 4's visualization enhancements, combining with Story 4.7:

```
Epic 4 (Scoring) → Story 4.7 (Heat Map Colors) + Story 4.8 (Score Labels) → Epic 5 (Reporting)

src/MasDependencyMap.Core/ExtractionScoring/
├── [Stories 4.1-4.5: Metric calculation, score generation] ✅ DONE
├── ExtractionScore.cs               # Story 4.5 (consumed by Stories 4.7-4.8) ✅ DONE
└── [Story 4.6: Ranked list generation] ✅ DONE

src/MasDependencyMap.Core/Visualization/
├── DotGenerator.cs                   # Story 4.7 (heat map colors) ✅ DONE
│                                     # Story 4.8 (score labels) ← THIS STORY
└── [Other visualization components]

Epic 5 (Future):
- Text reports will reference scored visualizations
- CSV exports may include extraction score columns
- CLI commands will wire up scores + showScoreLabels → DotGenerator
```

**Cross-Epic Integration:**

Story 4.8 completes the **bridge** between:
1. **Epic 4 (Scoring):** Produces ExtractionScore objects (Stories 4.1-4.6)
2. **Epic 2 (Visualization):** DotGenerator creates graphs (Epic 2 foundation)
3. **Story 4.7 (Heat Map):** Color-codes nodes by difficulty (green/yellow/red)
4. **Story 4.8 (Labels):** Adds precise scores to node labels ("Score: XX")
5. **Epic 5 (Reporting):** Will integrate fully-scored visualizations into reports

**Dependency Flow (Complete with Story 4.8):**

```
Story 4.5 (ExtractionScoreCalculator)
        ↓
[IReadOnlyList<ExtractionScore>]
        ↓
Story 4.7 (DotGenerator with heat map colors) ← DONE
        ↓ (same data)
Story 4.8 (DotGenerator with score labels) ← THIS STORY
        ↓
[Fully scored DOT file: colors + labels]
        ↓
Story 2.9 (GraphvizRenderer)
        ↓
[Comprehensive scored PNG/SVG visualization]
        ↓
Epic 5 (Reports, CLI integration)
```

**Impact on Existing Components:**

```
MODIFIED (Story 4.8):
- IDotGenerator.cs: Add showScoreLabels parameter
- DotGenerator.cs: Implement label formatting and font color logic

UNCHANGED (Backward compatible):
- GraphvizRenderer.cs: Still renders DOT → PNG/SVG (no changes)
- CLI commands: Don't yet pass showScoreLabels (Epic 5 integration)
- Existing callers: Continue working with default labels (project names only)
- Story 4.7 heat map: Colors work independently from labels
```

**Epic 4 Completion Status After Story 4.8:**

After Story 4.8, Epic 4 is **COMPLETE**:
1. ✅ Story 4.1: Coupling metric calculator
2. ✅ Story 4.2: Cyclomatic complexity calculator
3. ✅ Story 4.3: Technology version debt analyzer
4. ✅ Story 4.4: External API exposure detector
5. ✅ Story 4.5: Combined extraction score calculator
6. ✅ Story 4.6: Ranked extraction candidate lists
7. ✅ Story 4.7: Heat map visualization (color-coded nodes)
8. ⏳ Story 4.8: Node labels with scores (THIS STORY - FINAL VISUALIZATION)

After Story 4.8:
- Epic 4 is **COMPLETE** ✅
- Epic 5 (Reporting) can integrate all scoring/visualization features
- CLI becomes fully scoring-aware with both colors AND precise scores

**Full Visualization Capability Matrix (After Story 4.8):**

| extractionScores | showScoreLabels | Result |
|-----------------|-----------------|--------|
| null | false (default) | Default colors, no scores (Epic 2 baseline) |
| null | true | Default colors, no scores (graceful degradation) |
| provided | false (default) | Heat map colors, no labels (Story 4.7 only) |
| provided | true | **Heat map colors + score labels (Story 4.7 + 4.8 - FULL FEATURE)** |

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-4-extraction-difficulty-scoring-and-candidate-ranking.md, Story 4.8 (lines 136-151)]
- Story requirements: Display scores on node labels, format "ProjectName\nScore: XX", configurable display

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 18-51)]
- Technology stack: .NET 8.0, C# 12, Graphviz 2.38+ external dependency
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces (Visualization), async patterns, file-scoped namespaces
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 114-119)]
- Logging: Structured logging with named placeholders, ILogger<T> injection
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 282-286)]
- Graphviz integration: External tool via Process.Start, DOT format text generation

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-7-implement-heat-map-visualization-with-color-coded-scores.md]
- Story 4.7 implementation: Heat map colors (lightgreen/yellow/lightcoral), score lookup helpers, backward compatibility pattern
- Node color selection logic, extraction score lookup (GetExtractionScore helper)
- ExtractionScore model structure: FinalScore (double 0-100), ProjectName, other metrics

**Graphviz Label Formatting Research:**
- [Source: Task agent research acb22c9]
- Multi-line label syntax: `\n` escape sequence for centered newlines
- Font color readability: black for light backgrounds (lightgreen, yellow), white for dark backgrounds (lightcoral)
- Label escaping: Only double-quotes need escaping in quoted strings
- Font recommendations: Helvetica for names, Courier for scores (monospace)
- Graphviz 2.38+ compatibility: `\n` supported since Graphviz 2.0, fontcolor attribute standard

**DotGenerator Current Implementation:**
- [Source: src/MasDependencyMap.Core/Visualization/DotGenerator.cs (line 358)]
- Current node label generation: `builder.AppendLine($"    {escapedName} [label={escapedName}, fillcolor=\"{nodeColor}\"];");`
- [Source: src/MasDependencyMap.Core/Visualization/DotGenerator.cs (lines 159-237)]
- Existing helpers: BuildExtractionScoreLookup, GetExtractionScore, GetNodeColorForScore
- [Source: Epic 2 implementation]
- Label escaping: EscapeDotIdentifier method handles quote escaping

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Story 4.7 completed with code review pattern (d10e790)
- Story 4.6 completed with code review fixes (28eb11f)
- Epic 4 stories follow: implementation → code review → fixes → status update
- DotGenerator previously enhanced in Epic 3 (cycles) and Story 4.7 (heat map)

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - All tasks completed successfully without debugging required.

### Completion Notes List

✅ **Story 4.8 Implementation Complete - Score Labels Feature Added**

**Implementation Summary:**
- Added optional `showScoreLabels` parameter to `IDotGenerator.GenerateAsync()` interface
- Implemented label formatting with `FormatNodeLabel()` method that creates multi-line labels with format "ProjectName\nScore: XX"
- Implemented font color selection with `GetFontColorForBackground()` method for readability (black on light backgrounds, white on lightcoral)
- Updated node generation in `BuildDotContent()` to apply both score labels and appropriate font colors
- All nodes now include `fontcolor` attribute for consistent readability across all visualization modes

**Technical Decisions:**
- Score rounding uses `Math.Round()` for standard banker's rounding (e.g., 85.5 → 86)
- Font color strategy: black for lightgreen/yellow/lightblue, white for lightcoral
- **fontcolor applied to ALL nodes** (not just score-labeled) for consistent readability across all visualization modes
- Multi-line label uses `\n` escape sequence (Graphviz standard, centered alignment)
- Backward compatibility maintained with `showScoreLabels = false` default parameter
- Graceful degradation: if `showScoreLabels=true` but no scores provided, shows project names only with warning log

**Test Coverage:**
- Added 8 new comprehensive tests for score label functionality
- Updated 20+ existing tests to expect new `fontcolor` attribute (now applied to ALL nodes for consistent readability)
- All 385 tests pass successfully (69 DotGenerator tests + other Core tests)
- Test coverage includes: label formatting, rounding, font colors, backward compatibility, graceful degradation, combined features

**Integration Notes:**
- Story 4.8 builds on Story 4.7's heat map color-coding
- Both features work independently or together (4 visualization modes supported)
- No CLI integration yet - deferred to Epic 5 (Reporting)
- DI registration unchanged - DotGenerator already registered as singleton

**Validation:**
- All acceptance criteria satisfied (label format, readability, configuration support, score rounding)
- All tasks and subtasks completed and marked [x]
- Red-green-refactor TDD cycle followed for all new methods
- Full test suite passes (385/385 tests)
- Code follows project-context.md rules (feature-based namespace, async patterns, file-scoped namespaces, structured logging, XML documentation)

### File List

**Modified Files:**
- src/MasDependencyMap.Core/Visualization/IDotGenerator.cs (added showScoreLabels parameter with XML documentation)
- src/MasDependencyMap.Core/Visualization/DotGenerator.cs (added FormatNodeLabel, GetFontColorForBackground methods; updated GenerateAsync signature and BuildDotContent node generation; fontcolor now applied to ALL nodes for consistent readability)
- tests/MasDependencyMap.Core.Tests/Visualization/DotGeneratorTests.cs (added 8 new tests for score labels; updated 20+ existing tests to expect fontcolor attribute on all nodes)
- .claude/settings.local.json (added permissions for dotnet clean and graphviz.org web fetch used during development)
- _bmad-output/implementation-artifacts/sprint-status.yaml (status: ready-for-dev → in-progress → review)
- _bmad-output/implementation-artifacts/4-8-display-extraction-scores-as-node-labels.md (this story file)
