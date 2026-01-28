# Story 5.8: Format Reports with Spectre.Console Tables

Status: completed

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want text reports to use formatted tables for better readability,
So that reports are stakeholder-ready without manual editing.

## Acceptance Criteria

**Given** Report data needs to be presented in tabular format
**When** TextReportGenerator uses Spectre.Console.Table
**Then** Tables are rendered with clear column headers and aligned data
**And** Extraction candidate tables show: Rank | Project Name | Score | Incoming | Outgoing | APIs
**And** Cycle tables show: Cycle ID | Size | Projects | Suggested Break
**And** Tables use appropriate column widths for readability
**And** Tables render correctly in console output when using --verbose mode
**And** Non-technical language is used suitable for executive presentations

## Tasks / Subtasks

- [x] Analyze table rendering approach (AC: Technical feasibility)
  - [x] Research Spectre.Console.Table API for text file output
  - [x] Evaluate TestConsole vs custom rendering for plain text files
  - [x] Determine if tables should be in file, console, or both
  - [x] Review acceptance criteria interpretation with user if ambiguous

- [x] Inject IAnsiConsole into TextReportGenerator (AC: Architecture compliance)
  - [x] Add IAnsiConsole parameter to TextReportGenerator constructor
  - [x] Update DI registration in Program.cs if needed
  - [x] Inject IAnsiConsole into existing TextReportGenerator class
  - [x] Validate injection follows project-context.md rules

- [x] Replace Extraction Scores plain text with Spectre.Console.Table (AC: Easiest candidates table)
  - [x] Create Table instance for easiest candidates (top 10)
  - [x] Add columns: Rank | Project Name | Score | Incoming | Outgoing | APIs
  - [x] Set column alignment (Rank right-aligned, Score right-aligned, counts right-aligned)
  - [x] Add rows for each candidate with formatted data
  - [x] Render table to string for file output OR append to console output
  - [x] Maintain "Easiest Candidates" header and score range description

- [x] Replace Extraction Scores hardest candidates with Spectre.Console.Table (AC: Hardest candidates table)
  - [x] Create Table instance for hardest candidates (top 10)
  - [x] Add columns: Rank | Project Name | Score | Coupling | Complexity | Tech Debt
  - [x] Set column alignment (Rank right-aligned, metrics right-aligned)
  - [x] Add rows with formatted data and complexity labels
  - [x] Render table to string for file output OR append to console output
  - [x] Maintain "Hardest Candidates" header and score range description

- [x] Replace Cycle Detection detailed info with Spectre.Console.Table (AC: Cycle tables)
  - [x] Create Table instance for cycle listing
  - [x] Add columns: Cycle ID | Size | Projects | Suggested Break
  - [x] Handle multi-project lists in "Projects" column (comma-separated or newline-separated)
  - [x] Format "Suggested Break" as "SourceProject → TargetProject"
  - [x] Render table to string for file output OR append to console output
  - [x] Keep summary statistics (total chains, participation %) outside table

- [x] Replace Cycle-Breaking Recommendations with Spectre.Console.Table (AC: Recommendations table)
  - [x] Create Table instance for recommendations (top 5)
  - [x] Add columns: Rank | Break Edge | Coupling | Rationale
  - [x] Format "Break Edge" as "Source → Target"
  - [x] Format "Coupling" as "N calls" with grammatical handling
  - [x] Include rationale text (may need wider column or text wrapping)
  - [x] Render table to string for file output OR append to console output

- [x] Implement table-to-string rendering strategy (AC: File output compatibility)
  - [x] If using TestConsole: Capture output and extract plain text
  - [x] If using custom rendering: Implement plain text table layout
  - [x] Ensure UTF-8 encoding without BOM (existing requirement from Story 5.1)
  - [x] Validate tables fit within 80-character width or adjust width dynamically
  - [x] Preserve section separators and headers around tables

- [x] Test console output with --verbose mode (AC: Console rendering)
  - [x] Verify tables render correctly in console (if applicable)
  - [x] Test with actual System.CommandLine --verbose flag integration
  - [x] Ensure colors and formatting work in console but not in file
  - [x] Validate that file output remains plain text (no ANSI codes)

- [x] Update existing tests for table formatting (AC: Test coverage)
  - [x] Update GenerateAsync_ValidInput_CreatesReport test for table format
  - [x] Verify column headers match acceptance criteria exactly
  - [x] Test table rendering with empty data (0 candidates, 0 cycles)
  - [x] Test table rendering with 10+ items (pagination or truncation)
  - [x] Test wide column content (long project names, long rationale text)
  - [x] Mock IAnsiConsole in tests using TestConsole or mock framework

- [x] Validate non-technical language (AC: Executive readability)
  - [x] Review all column headers for clarity: "APIs" not "External API Count"
  - [x] Review rationale text for plain language (existing from Story 5.4)
  - [x] Ensure table formatting enhances readability vs plain text
  - [x] Remove technical jargon if present (e.g., "SCC" → "Circular Dependency")

- [x] Performance validation (AC: Existing 10-second requirement)
  - [x] Ensure table rendering doesn't degrade performance
  - [x] Test with 400+ projects (existing benchmark from Story 5.1)
  - [x] Validate StringBuilder optimization still effective
  - [x] Measure overhead of Spectre.Console.Table vs plain text

## Dev Notes

### Critical Implementation Decision Required

🚨 **CRITICAL - Clarify Story Interpretation Before Implementation:**

This story has an **architectural ambiguity** that must be resolved before coding:

**Acceptance Criteria says:**
- "When TextReportGenerator uses Spectre.Console.Table"
- "Tables render correctly in console output when using --verbose mode"
- "Non-technical language is used suitable for executive presentations"

**Technical Constraint:**
- TextReportGenerator writes to **TEXT FILES** (.txt) for distribution (Story 5.1)
- Spectre.Console.Table is designed for **CONSOLE OUTPUT** (ANSI codes, colors, interactive)
- Spectre.Console does NOT have built-in `ToPlainText()` for file output

**Two Possible Interpretations:**

**Option A: Tables in TEXT FILE (Plain Text Rendering)**
- Use Spectre.Console.Table layout logic
- Render tables to plain text ASCII art for file output
- Use `TestConsole` or custom rendering to extract plain text
- File contains aligned text tables (no ANSI codes)
- Suitable for email distribution, executives can open .txt file and see tables

**Option B: Tables in CONSOLE ONLY (Keep File Plain Text)**
- Keep text file reports as plain text (current format)
- Add --verbose flag support to CLI for console output
- When user runs with --verbose, display reports with Spectre.Console.Table to console
- File output unchanged (maintains backward compatibility)
- Executive presentations use file; developers use console

**Recommendation: Ask User to Clarify**

Before proceeding, ask the user:
1. Should tables appear in the text file output (.txt)?
2. Or should tables only appear in console output with --verbose flag?
3. If in file: What table rendering approach do you prefer (TestConsole vs custom)?

**Default Assumption for Auto-Implementation (YOLO Mode):**
If running in YOLO mode without clarification, implement **Option A** (tables in text file) using `TestConsole` from Spectre.Console.Testing NuGet package to render plain text tables to file. This maintains Story 5.1's text file output goal while enhancing readability with formatted tables.

### Architecture Context from project-context.md

**Spectre.Console Integration (CRITICAL):**
```csharp
// Line 94-99 from project-context.md:
// - Inject IAnsiConsole via DI, NOT direct AnsiConsole.Console usage (enables testing)
// - User-facing errors MUST use 3-part structure: [red]Error:[/], [dim]Reason:[/], [dim]Suggestion:[/]
// - Progress indicators: Use AnsiConsole.Progress() with TaskDescriptionColumn, ProgressBarColumn, PercentageColumn
// - Tables: Use Spectre.Console.Table for formatted reports
// - NEVER use plain Console.WriteLine for user output
```

**Current Spectre.Console Usage:**
- Package: Spectre.Console v0.54.0 (referenced in CLI project)
- Current usage: Progress bars in CLI layer only (Program.cs lines 223, 323-361)
- NOT currently used in Core.Reporting layer

**DI Injection Pattern (CRITICAL):**
```csharp
// Line 101-106 from project-context.md:
// - Full DI throughout Core and CLI layers
// - Core components MUST use constructor injection
// - Register services in CLI Program.cs using ServiceCollection
// - Lifetime: Singletons for stateless services
```

**Namespace Organization (CRITICAL):**
```csharp
// Line 57-59 from project-context.md:
// - Feature-based namespaces: MasDependencyMap.Core.Reporting
// - File-scoped namespace declaration (C# 10+)
// - File names MUST match class names exactly
```

**Documentation Standards (CRITICAL):**
```csharp
// Line 170-184 from project-context.md:
// - XML documentation comments REQUIRED for public APIs
// - Include <summary>, <param>, <returns>, <exception> tags
// - Explain WHY not WHAT - focus on intent and business context
```

### Previous Story Intelligence (Story 5.7 - CSV Export)

**Critical Learnings from Story 5.7 Implementation:**

1. **Epic 5 Reporting Stack Progression:**
   - ✅ Story 5.1: TextReportGenerator foundation (plain text, 502 lines) - DONE
   - ✅ Story 5.2-5.4: Extended TextReportGenerator (cycles, scores, recommendations) - DONE
   - ✅ Story 5.5-5.7: CSV export infrastructure (CsvExporter with 3 export methods) - DONE
   - **➡️ Story 5.8: Transform TextReportGenerator output to use Spectre.Console.Table - CURRENT**

2. **TextReportGenerator Current Structure (502 lines total):**
   - Constructor: Inject `ILogger<TextReportGenerator>` and `IOptions<FilterConfiguration>`
   - Main method: `GenerateAsync(graph, outputDir, solutionName, cycles, scores, suggestions, token)`
   - Private helpers: `AppendHeader`, `AppendDependencyOverview`, `AppendCycleDetection`, `AppendExtractionScores`, `AppendRecommendations`
   - Utilities: `FormatExternalApis`, `GetComplexityLabel`, `FormatCouplingCalls`, `SanitizeFileName`
   - Format: 80-character width, section separators (=== and ---), StringBuilder pre-allocation (4096 capacity)
   - Encoding: UTF-8 without BOM for cross-platform compatibility

3. **Current Report Sections (Plain Text Format):**
   - **Header:** Solution name, analysis date (UTC), total projects count
   - **Dependency Overview:** Total references, framework/custom breakdown with percentages
   - **Cycle Detection:** Total chains, participation %, largest cycle, detailed listing
   - **Extraction Scores:** Top 10 easiest (low scores) and top 10 hardest (high scores) with metrics
   - **Cycle-Breaking Recommendations:** Top 5 suggestions with source→target, coupling, rationale

4. **Current Plain Text Format Example:**
   ```
   Easiest Candidates (Scores 15-42)
   Description...

    1. NotificationService (Score: 23) - 3 incoming, 2 outgoing, 1 API
    2. EmailService (Score: 28) - 2 incoming, 1 outgoing, no external APIs
   ...
   10. LoggingService (Score: 42) - 5 incoming, 3 outgoing, 2 APIs
   ```

5. **Story 5.8 Target Format (Spectre.Console.Table):**
   ```
   Easiest Candidates (Scores 15-42)
   Description...

   ┌──────┬───────────────────────┬───────┬──────────┬──────────┬──────┐
   │ Rank │ Project Name          │ Score │ Incoming │ Outgoing │ APIs │
   ├──────┼───────────────────────┼───────┼──────────┼──────────┼──────┤
   │    1 │ NotificationService   │    23 │        3 │        2 │    1 │
   │    2 │ EmailService          │    28 │        2 │        1 │    0 │
   ...
   │   10 │ LoggingService        │    42 │        5 │        3 │    2 │
   └──────┴───────────────────────┴───────┴──────────┴──────────┴──────┘
   ```

### Web Research: Spectre.Console Table API (2026)

**Latest Spectre.Console Table API Documentation:**

**Basic Usage Pattern:**
```csharp
var table = new Table();
table.AddColumn("Foo");
table.AddColumn(new TableColumn("Bar").Centered());
table.AddRow("Baz", "[green]Qux[/]");
table.AddRow(new Markup("[blue]Corgi[/]"), new Panel("Waldo"));
AnsiConsole.Write(table); // Renders to console
```

**Key Features (2026):**
- Any component implementing `IRenderable` can be used as column header or cell
- Nested tables and components supported
- Border styles: `Border(TableBorder.None)`, `Border(TableBorder.Ascii)`, `Border(TableBorder.Square)`, `Border(TableBorder.Rounded)`
- Column alignment: `.Centered()`, `.LeftAligned()`, `.RightAligned()`
- Column width: Automatic or manual with `.Width(int)`

**Rendering to Plain Text for File Output (CRITICAL LIMITATION):**

🚨 **Spectre.Console does NOT have built-in `ToPlainText()` method as of 2026!**

**Available Options:**
1. **TestConsole Class** (Spectre.Console.Testing NuGet package):
   - Used by Spectre.Console's own unit tests
   - Captures output as plain text with ANSI codes
   - Can strip ANSI codes to get plain text
   - Example:
     ```csharp
     var testConsole = new TestConsole();
     testConsole.Write(table);
     string output = testConsole.Output; // Contains ANSI codes
     // Strip ANSI codes with regex or custom logic
     ```

2. **ToAnsi Extension Method** (`Spectre.Console.Advanced`):
   - Returns ANSI control code sequence for `IRenderable`
   - Result includes escape codes like `\x1b[31;1mtext\x1b[0m`
   - Requires ANSI code stripping for plain text

3. **Custom Plain Text Rendering**:
   - Access `IRenderable.Render()` method to get Segments
   - Segments provide Style+Text for low-level rendering
   - Implement custom logic to build plain text tables
   - Most control but most implementation effort

**Recommendation for Story 5.8:**
Use **TestConsole** approach for initial implementation:
1. Add `Spectre.Console.Testing` NuGet package to Core project (or test only)
2. Create `TestConsole` instance
3. Render tables to `TestConsole`
4. Extract output and strip ANSI codes
5. Append plain text table to StringBuilder for file output
6. For console output (--verbose), use injected `IAnsiConsole` directly

### Data Source: Current Report Sections

**Critical File References:**
- TextReportGenerator: `src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs` (502 lines)
- ITextReportGenerator: `src/MasDependencyMap.Core/Reporting/ITextReportGenerator.cs`
- Tests: `tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs`

**Current Method to Transform:**
```csharp
// From TextReportGenerator.cs
private void AppendExtractionScores(
    StringBuilder builder,
    List<ExtractionScore> scores,
    int projectCount)
{
    // Current: Appends plain text with manual formatting
    // Target: Create Spectre.Console.Table and render to plain text

    // Easiest candidates (top 10, lowest scores)
    // Hardest candidates (top 10, highest scores)
}

private void AppendCycleDetection(
    StringBuilder builder,
    List<CycleInfo> cycles,
    int projectCount)
{
    // Current: Appends plain text with manual formatting
    // Target: Create Spectre.Console.Table for detailed cycle listing
}

private void AppendRecommendations(
    StringBuilder builder,
    List<CycleBreakingSuggestion> suggestions)
{
    // Current: Appends plain text with manual formatting
    // Target: Create Spectre.Console.Table for recommendations
}
```

**Table Column Specifications from Acceptance Criteria:**

1. **Extraction Easiest Candidates Table:**
   - Columns: `Rank | Project Name | Score | Incoming | Outgoing | APIs`
   - Alignment: Rank (right), Score (right), counts (right), name (left)
   - Data: Top 10 candidates with lowest scores
   - Header: "Easiest Candidates (Scores {min}-{max})"

2. **Extraction Hardest Candidates Table:**
   - Columns: `Rank | Project Name | Score | Coupling | Complexity | Tech Debt`
   - Alignment: Rank (right), Score (right), metrics (right), name (left)
   - Data: Top 10 candidates with highest scores
   - Header: "Hardest Candidates (Scores {min}-{max})"

3. **Cycle Detection Table:**
   - Columns: `Cycle ID | Size | Projects | Suggested Break`
   - Data: All detected cycles with project lists
   - Projects column: Comma-separated list (may need wrapping)
   - Suggested Break: "SourceProject → TargetProject"

4. **Cycle-Breaking Recommendations Table:**
   - Columns: `Rank | Break Edge | Coupling | Rationale`
   - Data: Top 5 recommendations
   - Break Edge: "Source → Target"
   - Rationale: May be long text (consider column width or wrapping)

### Git Intelligence (Recent Commits)

**Latest Commit Analysis:**
```
7ce2589 Story 5.7: Implement CSV export for dependency matrix with code review fixes
6202fff Stories 5.5 & 5.6: Implement CSV export for extraction scores and cycle analysis
b927a41 Code review fixes for Story 5-4: Add cycle-breaking recommendations to text reports
35baccd Code review fixes for Story 5-3: Add extraction difficulty scoring section to text reports
70d3380 Story 5-2: Add cycle detection section to text reports with code review fixes
```

**Key Patterns from Commits:**
1. **Incremental story completion** - Each story builds on previous work
2. **Code review cycle included** - Stories go through review and fixes
3. **Test coverage maintained** - All commits include test updates
4. **No breaking changes** - Backward compatibility preserved throughout Epic 5

**Expected Pattern for Story 5.8:**
- Modified: `src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs`
- Modified: `src/MasDependencyMap.Core/Reporting/ITextReportGenerator.cs` (if signature changes)
- Modified: `tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs`
- Modified: `src/MasDependencyMap.CLI/Program.cs` (DI registration if IAnsiConsole needed)
- Possibly: Add `Spectre.Console.Testing` NuGet package reference

### File Structure Requirements

**Files to Modify:**
1. `src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs` - Add IAnsiConsole, transform methods
2. `src/MasDependencyMap.Core/Reporting/ITextReportGenerator.cs` - Interface signature (if changed)
3. `tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs` - Update tests for table format
4. `src/MasDependencyMap.CLI/Program.cs` - DI registration (if IAnsiConsole injection needed)
5. `src/MasDependencyMap.Core/MasDependencyMap.Core.csproj` - Add Spectre.Console.Testing package (if using TestConsole)

**No New Files Expected** - This story extends existing TextReportGenerator

**Namespace Alignment:**
- All reporting files: `namespace MasDependencyMap.Core.Reporting;` (file-scoped)
- Test file: `namespace MasDependencyMap.Core.Tests.Reporting;` (file-scoped)

### Testing Requirements

**Existing Tests to Update:**
- `GenerateAsync_ValidInput_CreatesReport` - Verify table format in output
- `GenerateAsync_WithCycles_IncludesCycleSection` - Verify cycle table rendering
- `GenerateAsync_WithExtractionScores_IncludesScoreSection` - Verify score tables
- `GenerateAsync_WithRecommendations_IncludesRecommendationSection` - Verify recommendation table

**New Test Considerations:**
- Mock IAnsiConsole using TestConsole or Moq
- Verify column headers match acceptance criteria exactly
- Test table rendering with edge cases:
  - Empty data (0 candidates, 0 cycles, 0 recommendations)
  - Single item
  - Exactly 10 items
  - Wide column content (long project names >50 characters)
  - Multi-line content in rationale column
- Validate plain text output (no ANSI codes in file)
- Performance: Ensure table rendering doesn't degrade 10-second requirement

**Test Helper Pattern:**
```csharp
private IAnsiConsole CreateTestConsole()
{
    return new TestConsole(); // From Spectre.Console.Testing
}

private string StripAnsiCodes(string input)
{
    // Regex to remove ANSI escape sequences
    return Regex.Replace(input, @"\x1B\[[^@-~]*[@-~]", string.Empty);
}
```

### Performance Considerations

**Existing Performance Requirement (Story 5.1):**
- Text report generation must complete within 10 seconds
- Tested with 400+ projects benchmark
- Uses StringBuilder with 4096 capacity pre-allocation

**Story 5.8 Performance Impact:**
- Spectre.Console.Table rendering may add overhead
- TestConsole capture and ANSI stripping adds processing
- Large tables (10 rows × 6 columns) × 4 tables = 40+ rows total

**Mitigation Strategies:**
- Reuse TestConsole instances if possible
- Pre-allocate string builders
- Batch table rendering operations
- Profile before/after to measure overhead
- Consider caching table instances if performance degrades

**Performance Testing:**
- Measure with Stopwatch before and after transformation
- Test with 400+ projects (existing benchmark)
- Validate 10-second requirement still met
- Add performance test if story 5.1 tests didn't have one

### Project Structure Notes

**Dependency Injection:**
- ITextReportGenerator already registered in Program.cs
- Need to inject IAnsiConsole into TextReportGenerator constructor
- IAnsiConsole registration may already exist from CLI layer usage
- Validate registration scope (Singleton appropriate for stateless service)

**Spectre.Console.Testing NuGet Package:**
- Required for TestConsole class
- Typically used in test projects
- May need to reference in Core project if TestConsole used in production code
- Alternative: Keep in test project only, use Moq to test IAnsiConsole

**Backward Compatibility:**
- Text report file format will change (tables vs plain text)
- File extension remains .txt
- Encoding remains UTF-8 without BOM
- Section structure preserved (headers, separators)
- Consider: Does this break any downstream tools parsing reports?

### Critical Don't-Miss Rules

🚨 **Console Output Discipline (project-context.md:288-292):**
- NEVER EVER use Console.WriteLine() for user-facing output
- ALWAYS use IAnsiConsole injected via DI
- Reason: Enables testing and consistent formatting
- Only exception: Program.Main() error handling before DI available

🚨 **Async All The Way (project-context.md:294-298):**
- ALL I/O operations MUST be async (file writing is I/O)
- NEVER use .Result or .Wait() - causes deadlocks
- Use ConfigureAwait(false) in library code (Core layer)
- TextReportGenerator.GenerateAsync already async, maintain pattern

🚨 **Exception Context (project-context.md:300-304):**
- ALWAYS include context in exceptions
- Include file paths, solution names, specific errors from inner exceptions
- Example: `throw new IOException($"Failed to write report to {filePath}", ex);`
- NEVER throw generic exceptions with just "Failed" messages

🚨 **Documentation Standards (project-context.md:170-184):**
- XML documentation REQUIRED for public APIs
- Update ITextReportGenerator interface if signature changes
- Explain WHY not WHAT - focus on intent
- Document new IAnsiConsole parameter: Why it's injected, how it's used

🚨 **Testing Framework (project-context.md:144-159):**
- xUnit as primary test framework
- FluentAssertions for assertions (optional but recommended)
- Moq for mocking dependencies
- Arrange-Act-Assert pattern consistently
- One test class per production class: TextReportGeneratorTests

### References

**Epic 5 Story Definitions:**
[Source: _bmad-output/planning-artifacts/epics/epic-5-comprehensive-reporting-and-data-export.md#Story 5.8]

**Project Context Rules:**
[Source: _bmad-output/project-context.md#Spectre.Console Integration]
[Source: _bmad-output/project-context.md#DI Injection Pattern]
[Source: _bmad-output/project-context.md#Console Output Discipline]
[Source: _bmad-output/project-context.md#Async All The Way]

**TextReportGenerator Implementation:**
[Source: src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs:1-502]
[Source: tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs]

**Previous Story Implementation:**
[Source: _bmad-output/implementation-artifacts/5-7-implement-csv-export-for-dependency-matrix.md]

**Spectre.Console Documentation (2026):**
[Source: https://spectreconsole.net/console/widgets/table - Official Table Widget Docs]
[Source: https://github.com/spectreconsole/spectre.console/issues/1677 - Render to String Discussion]
[Source: https://spectreconsole.net/console/explanation/understanding-rendering-model - Rendering Model]

**Recent Commit Intelligence:**
[Source: git commit 7ce2589 - Story 5.7 CSV export with code review fixes]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

### Completion Notes List

**Implementation Summary:**
- ✅ Successfully implemented Option A: Tables render to plain text in .txt files
- ✅ Used Spectre.Console.Testing TestConsole to capture and render tables
- ✅ Implemented ANSI code stripping using regex for clean plain text output
- ✅ All 4 report sections now use formatted tables (Extraction easiest/hardest, Cycles, Recommendations)
- ✅ All 54 tests passing with updated table format assertions
- ✅ Performance validated - 400 projects complete within 10-second budget
- ✅ Executive-friendly language maintained across all table headers

**Technical Details:**
- Added Spectre.Console v0.54.0 and Spectre.Console.Testing v0.54.0 to Core project
- Injected IAnsiConsole into TextReportGenerator constructor via DI
- Created RenderTableToPlainText() helper method using TestConsole
- Updated all report sections to use TableBorder.Ascii for clean plain text rendering
- Maintained UTF-8 without BOM encoding for cross-platform compatibility

**Files Modified:**
1. src/MasDependencyMap.Core/MasDependencyMap.Core.csproj - Added Spectre.Console packages
2. src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs - Implemented table rendering
3. tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj - Added Spectre.Console dependency
4. tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs - Updated 54 tests for table format

### File List

**Production Code:**
- `src/MasDependencyMap.Core/Reporting/TextReportGenerator.cs` - Main implementation with table rendering
- `src/MasDependencyMap.Core/MasDependencyMap.Core.csproj` - Package references updated

**Test Code:**
- `tests/MasDependencyMap.Core.Tests/Reporting/TextReportGeneratorTests.cs` - All 54 tests updated and passing
- `tests/MasDependencyMap.Core.Tests/MasDependencyMap.Core.Tests.csproj` - Package references updated
