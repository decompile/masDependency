# Story 2.6: Implement Framework Dependency Filter

Status: review

## Story

As an architect,
I want to filter out Microsoft.*/System.* framework dependencies from the graph,
So that I see only custom code architecture.

## Acceptance Criteria

**Given** A DependencyGraph with framework and custom dependencies
**When** FrameworkFilter.FilterAsync() is called with BlockList patterns from configuration
**Then** All edges to projects matching Microsoft.*, System.*, mscorlib, netstandard are removed
**And** Projects matching AllowList patterns (e.g., YourCompany.*) are retained
**And** The filtered graph contains only custom code dependencies
**And** ILogger logs the count of filtered dependencies (e.g., "Filtered 2,847 framework refs, retained 412 custom refs")
**And** Filter rules are loaded from filter-config.json with PascalCase property names

## Tasks / Subtasks

- [x] Load FilterConfiguration from existing Story 1-4 implementation (AC: Filter rules loaded from filter-config.json)
  - [x] Verify FilterConfiguration POCO exists with BlockList and AllowList properties
  - [x] Verify filter-config.json already created with Microsoft.*, System.*, mscorlib, netstandard defaults
  - [x] Verify IConfiguration registration in Program.cs from Story 1-4
  - [x] Inject IOptions<FilterConfiguration> into FrameworkFilter via DI

- [x] Create IFrameworkFilter interface (AC: Testable abstraction)
  - [x] Define IFrameworkFilter interface in Filtering namespace
  - [x] Method: Task<DependencyGraph> FilterAsync(DependencyGraph graph, CancellationToken cancellationToken)
  - [x] XML documentation with examples of BlockList/AllowList patterns
  - [x] Follow interface naming convention with I-prefix

- [x] Create FrameworkFilter implementation (AC: Filter framework dependencies)
  - [x] Define FrameworkFilter.cs implementing IFrameworkFilter
  - [x] Inject IOptions<FilterConfiguration> and ILogger<FrameworkFilter> via constructor
  - [x] Implement FilterAsync method with pattern matching logic
  - [x] Use wildcard pattern matching for Microsoft.*, System.* patterns
  - [x] Remove edges where Target project matches BlockList patterns
  - [x] Retain edges where Target project matches AllowList patterns (takes precedence over BlockList)
  - [x] Handle BinaryReference edges (DllReferences) using same filtering logic
  - [x] Create new filtered DependencyGraph (immutable approach - don't modify input graph)
  - [x] Use CancellationToken for async operations

- [x] Implement pattern matching logic (AC: Microsoft.*, System.* patterns work correctly)
  - [x] Pattern matching implemented as private method in FrameworkFilter (simpler than separate helper class)
  - [x] Support * wildcard at end of pattern (e.g., "Microsoft.*" matches "Microsoft.Extensions.Logging")
  - [x] Support exact match patterns (e.g., "mscorlib" matches only "mscorlib")
  - [x] Case-insensitive matching for robustness
  - [x] Test edge cases: "System.Core", "Microsoft.Build", "System.Xml.Linq"
  - [x] AllowList takes precedence: "YourCompany.Microsoft.Utils" allowed even if "Microsoft.*" blocked

- [x] Implement filtering statistics logging (AC: ILogger logs filtered counts)
  - [x] Count total edges before filtering
  - [x] Count edges removed by BlockList
  - [x] Count edges retained by AllowList override
  - [x] Count final edges after filtering
  - [x] Log structured message: "Filtered {BlockedCount} framework refs ({BlockedPercent:F1}%), retained {RetainedCount} custom refs ({RetainedPercent:F1}%)"
  - [x] Use Information log level for statistics (visible in verbose mode)

- [x] Handle edge cases and validation (AC: Robust filtering)
  - [x] Handle empty DependencyGraph (return empty graph)
  - [x] Handle null or empty BlockList (no filtering applied)
  - [x] Handle null or empty AllowList (no overrides)
  - [x] Handle ProjectReference edges (primary use case)
  - [x] Handle BinaryReference edges (framework DLLs)
  - [x] Preserve graph structure (vertices remain, only edges removed)
  - [x] Validate configuration at startup (Story 1-4 already implements this)

- [x] Register FrameworkFilter in DI container (AC: DI integration)
  - [x] Add services.TryAddSingleton<IFrameworkFilter, FrameworkFilter>() to Program.cs
  - [x] Ensure IOptions<FilterConfiguration> is already registered (from Story 1-4)
  - [x] Ensure ILogger<FrameworkFilter> is resolved automatically
  - [x] Follow DI registration pattern from Stories 2-1 through 2-5

- [x] Create unit tests for pattern matching (AC: Pattern matching correctness)
  - [x] Pattern matching tested via FrameworkFilter tests (no separate PatternMatcher class)
  - [x] Test wildcard pattern: "Microsoft.*" matches "Microsoft.Extensions.Logging"
  - [x] Test wildcard pattern: "System.*" matches "System.Core"
  - [x] Test exact match: "mscorlib" matches "mscorlib" only
  - [x] Test case-insensitive: "microsoft.*" matches "Microsoft.Extensions.Logging"
  - [x] Test non-match: "Microsoft.*" does NOT match "MyCompany.Microsoft"
  - [x] Test AllowList precedence: "YourCompany.*" overrides "Microsoft.*" for "YourCompany.Microsoft.Utils"
  - [x] Follow test naming convention: {MethodName}_{Scenario}_{ExpectedResult}

- [x] Create unit tests for FrameworkFilter (AC: Filtering logic correctness)
  - [x] Test FilterAsync removes System.* edges
  - [x] Test FilterAsync removes Microsoft.* edges
  - [x] Test FilterAsync removes mscorlib edges
  - [x] Test FilterAsync retains custom project edges
  - [x] Test AllowList override: YourCompany.Microsoft.* retained even if Microsoft.* blocked
  - [x] Test empty BlockList (no filtering)
  - [x] Test empty graph (returns empty graph)
  - [x] Test logging: verify structured log message with counts
  - [x] Use NullLogger<FrameworkFilter>.Instance for non-logging tests
  - [x] Use TestLogger implementation to verify logging for statistics tests

- [x] Create integration tests with SampleMonolith (AC: End-to-end validation)
  - [x] Load SampleMonolith solution and build DependencyGraph
  - [x] Apply FrameworkFilter with default BlockList
  - [x] Verify no System.*, Microsoft.*, mscorlib, or netstandard edges remain
  - [x] Verify custom project edges retained
  - [x] Verify edge count is appropriate (SampleMonolith has only custom project refs)
  - [x] Log final statistics verified via TestLogger

- [x] Update filter-config.json documentation (AC: Configuration guidance)
  - [x] Filter-config.json already well-documented from Story 1-4 with clear BlockList patterns
  - [x] AllowList precedence explained in IFrameworkFilter XML documentation
  - [x] Common patterns already present (Microsoft.*, System.*, mscorlib, netstandard)
  - [x] Case-insensitive matching documented in FrameworkFilter XML comments

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Filtering is Core to Story 2.6:**

From Epic 2 Story 2.6 (epics/epic-2.md lines 98-112):
```
Filter out Microsoft.*/System.* framework dependencies
BlockList patterns from configuration (filter-config.json)
AllowList patterns override BlockList (e.g., YourCompany.*)
Filtered graph contains only custom code dependencies
```

**Why Filtering Matters:**
- Architects don't care about framework dependencies (System.Core, Microsoft.Build, etc.)
- Visualization becomes cluttered with thousands of framework references
- Cycle detection should focus on custom code, not framework dependencies
- Extraction scoring should analyze custom project coupling, not framework usage

**Implementation Strategy:**
1. Reuse FilterConfiguration from Story 1-4 (already has BlockList, AllowList)
2. Create IFrameworkFilter abstraction for testability
3. Implement wildcard pattern matching (Microsoft.*, System.*)
4. Filter edges (not vertices) - remove dependencies to framework projects
5. AllowList takes precedence - allows YourCompany.Microsoft.* even if Microsoft.* blocked
6. Log statistics - show how many refs filtered vs retained

🚨 **CRITICAL - Story 1-4 Already Implemented Configuration:**

From Story 1-4 (1-4-implement-configuration-management-with-json-support.md):
```csharp
public sealed class FilterConfiguration
{
    public List<string> BlockList { get; set; } = new()
    {
        "Microsoft.*",
        "System.*",
        "mscorlib",
        "netstandard"
    };

    public List<string> AllowList { get; set; } = new();
}
```

**What Story 1-4 Already Provides:**
- FilterConfiguration POCO with BlockList and AllowList properties
- filter-config.json created with default Microsoft.*/System.* patterns
- IConfiguration registered in DI container
- ValidateDataAnnotations() and ValidateOnStart() for config validation
- PascalCase JSON property names (matches C# POCO properties)

**What This Story (2.6) Must Do:**
- Create IFrameworkFilter interface and FrameworkFilter implementation
- Inject IOptions<FilterConfiguration> to access configuration
- Implement pattern matching logic for wildcard patterns (Microsoft.*, System.*)
- Filter DependencyGraph edges based on BlockList/AllowList
- Log filtering statistics

🚨 **CRITICAL - Pattern Matching Logic:**

**Wildcard Pattern Matching:**
```csharp
// Pattern: "Microsoft.*"
// Matches: Microsoft.Extensions.Logging, Microsoft.Build, Microsoft.CodeAnalysis
// Does NOT match: MyCompany.Microsoft, MicrosoftTools

// Pattern: "System.*"
// Matches: System.Core, System.Linq, System.Xml
// Does NOT match: SystemUtilities, MySystem.Core

// Pattern: "mscorlib" (exact match)
// Matches: mscorlib
// Does NOT match: mscorlib.Extensions
```

**AllowList Precedence:**
```csharp
// BlockList: ["Microsoft.*", "System.*"]
// AllowList: ["YourCompany.*"]

// Edge to "Microsoft.Extensions.Logging" → BLOCKED (matches Microsoft.*)
// Edge to "YourCompany.Core" → RETAINED (matches YourCompany.*)
// Edge to "YourCompany.Microsoft.Helpers" → RETAINED (AllowList takes precedence!)
// Edge to "CustomProject" → RETAINED (doesn't match BlockList)
```

**Implementation Pattern:**
```csharp
public bool IsBlocked(string projectName, List<string> blockList, List<string> allowList)
{
    // Step 1: Check AllowList first (takes precedence)
    if (allowList.Any(pattern => MatchesPattern(projectName, pattern)))
    {
        return false; // Explicitly allowed
    }

    // Step 2: Check BlockList
    if (blockList.Any(pattern => MatchesPattern(projectName, pattern)))
    {
        return true; // Blocked
    }

    // Step 3: Default - retain (not in BlockList or AllowList)
    return false;
}

private bool MatchesPattern(string name, string pattern)
{
    if (pattern.EndsWith("*"))
    {
        var prefix = pattern.Substring(0, pattern.Length - 1);
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
    else
    {
        return name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Technical Requirements

**FrameworkFilter Implementation Pattern:**

```csharp
namespace MasDependencyMap.Core.Filtering;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Filters framework dependencies from dependency graphs using configurable patterns.
/// Removes edges to Microsoft.*, System.*, and other framework references to focus on custom code architecture.
/// </summary>
public class FrameworkFilter : IFrameworkFilter
{
    private readonly FilterConfiguration _configuration;
    private readonly ILogger<FrameworkFilter> _logger;

    public FrameworkFilter(
        IOptions<FilterConfiguration> configuration,
        ILogger<FrameworkFilter> logger)
    {
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<DependencyGraph> FilterAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default)
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));

        var originalEdgeCount = graph.EdgeCount;
        var filteredGraph = new DependencyGraph();

        // Add all vertices (filtering removes edges, not vertices)
        foreach (var vertex in graph.Vertices)
        {
            filteredGraph.AddVertex(vertex);
        }

        // Filter edges based on BlockList/AllowList
        var blockedCount = 0;
        var retainedCount = 0;

        foreach (var edge in graph.Edges)
        {
            // Check if target project is blocked
            if (IsBlocked(edge.Target.ProjectName, _configuration.BlockList, _configuration.AllowList))
            {
                blockedCount++;
                continue; // Skip this edge
            }

            // Retain edge
            filteredGraph.AddEdge(edge);
            retainedCount++;
        }

        // Log statistics
        var blockedPercent = originalEdgeCount > 0
            ? (blockedCount / (double)originalEdgeCount) * 100
            : 0;
        var retainedPercent = originalEdgeCount > 0
            ? (retainedCount / (double)originalEdgeCount) * 100
            : 0;

        _logger.LogInformation(
            "Filtered {BlockedCount} framework refs ({BlockedPercent:F1}%), retained {RetainedCount} custom refs ({RetainedPercent:F1}%)",
            blockedCount,
            blockedPercent,
            retainedCount,
            retainedPercent);

        return Task.FromResult(filteredGraph);
    }

    private bool IsBlocked(string projectName, List<string> blockList, List<string> allowList)
    {
        // AllowList takes precedence
        if (allowList != null && allowList.Any(pattern => MatchesPattern(projectName, pattern)))
        {
            return false;
        }

        // Check BlockList
        if (blockList != null && blockList.Any(pattern => MatchesPattern(projectName, pattern)))
        {
            return true;
        }

        // Default: retain (not blocked)
        return false;
    }

    private static bool MatchesPattern(string name, string pattern)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pattern))
            return false;

        // Wildcard pattern (e.g., "Microsoft.*")
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // Exact match (e.g., "mscorlib")
        return name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
```

**Key Implementation Details:**
- Inject IOptions<FilterConfiguration> to access configuration from Story 1-4
- FilterAsync creates new DependencyGraph (immutable approach)
- Add all vertices first, then filter edges
- IsBlocked checks AllowList first (precedence), then BlockList
- MatchesPattern supports wildcard (*) and exact match patterns
- Case-insensitive matching for robustness
- Structured logging with named placeholders and percentages

### Architecture Compliance

**Filtering Architecture (From core-architectural-decisions.md lines 28-39):**

Epic 2 requires IFrameworkFilter with JSON blocklist/allowlist pattern matching:
```
Configuration Management: Microsoft.Extensions.Configuration
Filter rules loaded from filter-config.json
BlockList patterns: Microsoft.*, System.*, mscorlib, netstandard
AllowList patterns: YourCompany.* (overrides BlockList)
```

**This Story's Role in Architecture:**
1. Stories 2.1-2.5: Solution loading + graph building ← DONE
2. **Story 2.6**: Framework dependency filter ← THIS STORY
3. Stories 2.7-2.9: Graphviz visualization
4. Story 2.10: Multi-solution analysis

**Dependency Analysis Flow (From core-architectural-decisions.md):**
```
CLI.AnalyzeCommand
  ↓ (via ISolutionLoader)
Core.SolutionLoading.FallbackSolutionLoader  ← Stories 2-1 to 2-4 (DONE)
  ↓ (returns SolutionAnalysis)
Core.DependencyAnalysis.DependencyGraphBuilder  ← Story 2-5 (DONE)
  ↓ (builds DependencyGraph)
Core.Filtering.FrameworkFilter  ← THIS STORY (Story 2-6)
  ↓ (filters graph)
Core.CycleDetection.TarjanCycleDetector  ← Epic 3
  ↓ (detects cycles)
Core.Scoring.ExtractionScoreCalculator  ← Epic 4
```

**Configuration Integration (From Story 1-4):**
- FilterConfiguration already exists with BlockList and AllowList properties
- IConfiguration already registered in DI container
- ValidateDataAnnotations() and ValidateOnStart() already configured
- filter-config.json already created with defaults

**Logging Strategy:**
- Inject ILogger<FrameworkFilter> via constructor
- Use Information log level for filtering statistics
- Structured logging: "Filtered {BlockedCount} framework refs ({BlockedPercent:F1}%), retained {RetainedCount} custom refs ({RetainedPercent:F1}%)"

**Error Handling Strategy:**
- No custom exceptions needed (filtering is defensive, not failing)
- Null checks for graph parameter (ArgumentNullException)
- Handle null/empty BlockList and AllowList gracefully
- Empty graph returns empty graph

### Library/Framework Requirements

**No New NuGet Packages Required:**

All required packages already installed from previous stories:
- Microsoft.Extensions.Options (from Story 1-4 for IOptions<FilterConfiguration>)
- Microsoft.Extensions.Logging.Abstractions (from Story 1-6 for ILogger<T>)
- QuikGraph 2.5.0 (from Story 2-5 for DependencyGraph)

**Existing Dependencies (No Changes):**
- FilterConfiguration POCO (from Story 1-4)
- DependencyGraph, ProjectNode, DependencyEdge (from Story 2-5)
- IConfiguration registration (from Story 1-4)

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/
└── Filtering/                              # New namespace
    ├── IFrameworkFilter.cs                 # Interface
    └── FrameworkFilter.cs                  # Implementation

tests/MasDependencyMap.Core.Tests/
└── Filtering/                              # New test namespace
    └── FrameworkFilterTests.cs             # Filter tests
```

**Files to Modify:**
```
src/MasDependencyMap.CLI/Program.cs (register FrameworkFilter in DI)
filter-config.json (add documentation comments - optional)
```

**Namespace Organization (From project-context.md):**
```csharp
namespace MasDependencyMap.Core.Filtering;
```

**File Naming:**
- FrameworkFilter.cs (matches class name exactly)
- IFrameworkFilter.cs (matches interface name exactly)
- FrameworkFilterTests.cs (matches test class name exactly)

### Testing Requirements

**Unit Test Structure:**

```csharp
namespace MasDependencyMap.Core.Tests.Filtering;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.Filtering;
using MasDependencyMap.Core.DependencyAnalysis;

public class FrameworkFilterTests
{
    private readonly ILogger<FrameworkFilter> _logger;

    public FrameworkFilterTests()
    {
        _logger = NullLogger<FrameworkFilter>.Instance;
    }

    [Fact]
    public async Task FilterAsync_SystemCoreReference_RemovesEdge()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            BlockList = new List<string> { "System.*", "Microsoft.*" },
            AllowList = new List<string>()
        };
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);

        var graph = CreateGraphWithSystemReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(0); // System.Core edge removed
        filtered.VertexCount.Should().Be(2); // Vertices retained
    }

    [Fact]
    public async Task FilterAsync_CustomProjectReference_RetainsEdge()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            BlockList = new List<string> { "System.*", "Microsoft.*" },
            AllowList = new List<string>()
        };
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);

        var graph = CreateGraphWithCustomReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(1); // Custom edge retained
    }

    [Fact]
    public async Task FilterAsync_AllowListPattern_OverridesBlockList()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            BlockList = new List<string> { "Microsoft.*" },
            AllowList = new List<string> { "YourCompany.*" }
        };
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);

        var graph = CreateGraphWithYourCompanyMicrosoftReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(1); // YourCompany.Microsoft.* allowed
    }

    private DependencyGraph CreateGraphWithSystemReference()
    {
        var graph = new DependencyGraph();
        var project1 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Test"
        };
        var systemCore = new ProjectNode
        {
            ProjectName = "System.Core",
            ProjectPath = @"C:\Program Files\dotnet\System.Core.dll",
            TargetFramework = "net8.0",
            SolutionName = "Framework"
        };

        graph.AddVertex(project1);
        graph.AddVertex(systemCore);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = systemCore,
            DependencyType = DependencyType.BinaryReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    // Additional helper methods...
}
```

**Test Naming Convention (From project-context.md):**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `FilterAsync_SystemCoreReference_RemovesEdge()`
- ✅ `FilterAsync_CustomProjectReference_RetainsEdge()`
- ✅ `FilterAsync_AllowListPattern_OverridesBlockList()`
- ❌ `Should_filter_system_references()` ← WRONG (BDD-style)

**Integration Testing:**

Use samples/SampleMonolith solution for real filtering:
1. Load solution via FallbackSolutionLoader
2. Build graph via DependencyGraphBuilder.BuildAsync()
3. Apply FrameworkFilter.FilterAsync()
4. Verify System.*, Microsoft.* edges removed
5. Verify custom project edges retained
6. Check log output for filtering statistics

**Manual Testing Checklist:**
1. Run with samples/SampleMonolith - verify framework refs filtered
2. Check logs show "Filtered X framework refs (Y%), retained Z custom refs (W%)"
3. Verify edge count reduced after filtering
4. Verify vertices remain (only edges removed)

### Previous Story Intelligence

**From Story 2-5 (DependencyGraphBuilder):**

Story 2-5 created the DependencyGraph structure that this story will filter:

**Reusable Patterns:**
```csharp
// DI Registration Pattern (from Program.cs)
services.AddSingleton<IFrameworkFilter, FrameworkFilter>();

// Constructor pattern with null validation
public FrameworkFilter(
    IOptions<FilterConfiguration> configuration,
    ILogger<FrameworkFilter> logger)
{
    _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}

// Async method pattern (no ConfigureAwait needed for Task.FromResult)
public Task<DependencyGraph> FilterAsync(
    DependencyGraph graph,
    CancellationToken cancellationToken = default)
{
    // ... implementation ...
    return Task.FromResult(filteredGraph);
}

// Structured logging pattern
_logger.LogInformation(
    "Filtered {BlockedCount} framework refs ({BlockedPercent:F1}%), retained {RetainedCount} custom refs ({RetainedPercent:F1}%)",
    blockedCount,
    blockedPercent,
    retainedCount,
    retainedPercent);
```

**Key Insights from Story 2-5:**
- DependencyGraph is a wrapper around BidirectionalGraph<ProjectNode, DependencyEdge>
- DependencyGraph has Vertices and Edges properties
- AddVertex and AddEdge methods available
- DependencyEdge has DependencyType enum (ProjectReference, BinaryReference)
- ProjectNode has ProjectName property (used for pattern matching)
- Edge filtering removes edges, not vertices

**Note in Story 2-5 AC:**
```
Edge type (ProjectReference vs. BinaryReference) is stored on each edge
(Note: BinaryReference filtering deferred to Story 2.6)
```

This confirms Story 2.6 should handle both ProjectReference and BinaryReference edges.

**From Story 1-4 (Configuration Management):**

Story 1-4 created the FilterConfiguration that this story will use:

**FilterConfiguration Structure:**
```csharp
public sealed class FilterConfiguration
{
    public List<string> BlockList { get; set; } = new()
    {
        "Microsoft.*",
        "System.*",
        "mscorlib",
        "netstandard"
    };

    public List<string> AllowList { get; set; } = new();
}
```

**What Story 1-4 Already Provides:**
- FilterConfiguration POCO with data annotations
- filter-config.json with default patterns
- IConfiguration registered in DI
- IOptions<FilterConfiguration> available for injection
- ValidateDataAnnotations() and ValidateOnStart() configured

**Key Implementation Insight:**
- Inject IOptions<FilterConfiguration> to access configuration
- Use _configuration.Value.BlockList and _configuration.Value.AllowList
- No need to load JSON manually - configuration already bound

### Git Intelligence Summary

**Recent Commit Pattern (Last 5 Commits):**

```
7b3854b Code review fixes for Story 2-5: Build dependency graph with QuikGraph
2dbf9a3 Story 2-5 complete: Build dependency graph with QuikGraph
799aeae Story 2-4 complete: Strategy pattern fallback chain with code review fixes
c04983e Code review fixes for Story 2-3: ProjectFileSolutionLoader improvements
d8d00cb Story 2-3 complete: Project file fallback loader
```

**Commit Pattern Insights:**
- Epic 2 stories committed individually
- Code review cycle is standard: implementation → review → fixes
- Story 2-6 will likely follow same pattern (Story complete → Code review fixes)

**Expected Files for Story 2.6:**
```bash
# New files
src/MasDependencyMap.Core/Filtering/IFrameworkFilter.cs
src/MasDependencyMap.Core/Filtering/FrameworkFilter.cs
tests/MasDependencyMap.Core.Tests/Filtering/FrameworkFilterTests.cs

# Modified files
src/MasDependencyMap.CLI/Program.cs (DI registration)
_bmad-output/implementation-artifacts/2-6-implement-framework-dependency-filter.md
_bmad-output/implementation-artifacts/sprint-status.yaml
```

**Suggested Commit Message Pattern:**
```bash
git commit -m "Story 2-6 complete: Implement framework dependency filter

- Created IFrameworkFilter interface with FilterAsync method
- Created FrameworkFilter implementation with pattern matching logic
- Implemented wildcard pattern matching (Microsoft.*, System.*)
- Implemented AllowList precedence over BlockList
- Integrated with FilterConfiguration from Story 1-4
- Implemented filtering statistics logging with percentages
- Registered IFrameworkFilter in DI container
- Created comprehensive unit tests ({TestCount} tests) - all passing
- Full regression suite passes ({TotalTests} tests total)
- Verified framework reference filtering with SampleMonolith
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Latest Technical Information

**Microsoft.Extensions.Options (Already Installed from Story 1-4):**

**IOptions<T> Pattern:**
- **Purpose**: Dependency injection for configuration POCOs
- **Usage**: `IOptions<FilterConfiguration>` injected via constructor
- **Access**: `_configuration.Value.BlockList` to get configuration values
- **Validation**: ValidateDataAnnotations() ensures BlockList/AllowList are valid
- **Documentation**: https://learn.microsoft.com/en-us/dotnet/core/extensions/options

**Pattern Matching Best Practices (.NET 8):**

**Wildcard Matching:**
```csharp
// Use StartsWith for wildcard patterns
if (pattern.EndsWith("*"))
{
    var prefix = pattern.Substring(0, pattern.Length - 1);
    return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
```

**Case-Insensitive Matching:**
```csharp
// Always use StringComparison.OrdinalIgnoreCase for robustness
return name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
```

**LINQ Performance:**
```csharp
// .Any() is optimized for List<T> - O(n) with short-circuit evaluation
if (blockList.Any(pattern => MatchesPattern(projectName, pattern)))
{
    return true;
}
```

**DependencyGraph Filtering Pattern:**
```csharp
// Create new graph (immutable approach)
var filteredGraph = new DependencyGraph();

// Add all vertices first
foreach (var vertex in graph.Vertices)
{
    filteredGraph.AddVertex(vertex);
}

// Filter edges
foreach (var edge in graph.Edges)
{
    if (!IsBlocked(edge.Target.ProjectName, blockList, allowList))
    {
        filteredGraph.AddEdge(edge);
    }
}
```

**Sources:**
- [Options pattern in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Configuration in .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [String.StartsWith Method | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.string.startswith)

### Project Context Reference

🔬 **Complete project rules:** See `D:\work\masDependencyMap\_bmad-output\project-context.md` for comprehensive project guidelines.

**Critical Rules for This Story:**

**1. Namespace Organization (From project-context.md lines 57-59):**
```
MUST use feature-based namespaces: MasDependencyMap.Core.Filtering
NEVER use layer-based: MasDependencyMap.Core.Services or MasDependencyMap.Core.Models
```

**2. Configuration Management (From project-context.md lines 107-113):**
```
Use IOptions<T> injection, NOT direct JsonSerializer.Deserialize<T>()
Filter rules loaded from filter-config.json with PascalCase property names
```

**3. Async/Await Pattern (From project-context.md lines 66-69):**
```
ALWAYS use Async suffix: Task<DependencyGraph> FilterAsync(...)
NO ConfigureAwait needed for Task.FromResult (code review insight from Story 2-5)
```

**4. Logging (From project-context.md lines 115-119):**
```
Use structured logging: _logger.LogInformation("Filtered {BlockedCount} framework refs", blockedCount)
NEVER string interpolation: _logger.LogInformation($"Filtered {blockedCount} framework refs")
```

**5. File-Scoped Namespaces (.NET 8 Pattern):**
```csharp
namespace MasDependencyMap.Core.Filtering;

public class FrameworkFilter : IFrameworkFilter
{
    // Implementation
}
```

**6. Nullable Reference Types (Enabled by Default in .NET 8):**
```csharp
public FrameworkFilter(IOptions<FilterConfiguration> configuration, ILogger<FrameworkFilter> logger)
{
    _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**7. Testing (From project-context.md lines 151-154):**
```
Test naming: {MethodName}_{Scenario}_{ExpectedResult}
Example: FilterAsync_SystemCoreReference_RemovesEdge()
```

**8. Filtering Requirements (From project-context.md lines 249-256):**
```
Version Compatibility: Tool analyzes .NET Framework 3.5+ through .NET 8+
Framework dependencies to filter: Microsoft.*, System.*, mscorlib, netstandard
AllowList takes precedence over BlockList
```

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-2-solution-loading-and-dependency-discovery.md, Story 2.6 (lines 97-112)]
- Story requirements: Filter framework dependencies using BlockList/AllowList patterns

**Architecture Documents:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md, Configuration Management (lines 22-38)]
- Microsoft.Extensions.Configuration with JSON support
- Filter rules from filter-config.json
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md, Logging (lines 40-56)]
- ILogger<T> injection, structured logging patterns

**Previous Stories:**
- [Source: Story 1-4: Implement Configuration Management with JSON Support]
- FilterConfiguration POCO with BlockList and AllowList properties
- filter-config.json created with defaults
- IOptions<FilterConfiguration> available for injection
- [Source: Story 2-5: Build Dependency Graph with QuikGraph]
- DependencyGraph wrapper around BidirectionalGraph
- ProjectNode, DependencyEdge, DependencyType structures
- Note: BinaryReference filtering deferred to Story 2.6

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Namespace Organization (lines 57-59)]
- Feature-based namespaces required
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Configuration (lines 107-113)]
- IOptions<T> injection pattern
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Logging (lines 115-119)]
- Structured logging with named placeholders
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Testing (lines 151-154)]
- Test naming convention

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

No debugging issues encountered. Implementation was straightforward following the patterns from Stories 1-4 and 2-5.

### Completion Notes List

✅ **Story 2-6 Implementation Complete** (Date: 2026-01-23)

**Implementation Summary:**
- Created IFrameworkFilter interface in new MasDependencyMap.Core.Filtering namespace
- Implemented FrameworkFilter with wildcard pattern matching (Microsoft.*, System.*)
- Integrated with FilterConfiguration from Story 1-4 using IOptions<T> injection
- Implemented AllowList precedence over BlockList as specified
- Added structured logging with filtering statistics (blocked count, retained count, percentages)
- Registered IFrameworkFilter in DI container using TryAddSingleton pattern

**Pattern Matching Implementation:**
- Wildcard patterns: "Microsoft.*" matches any project starting with "Microsoft."
- Exact patterns: "mscorlib" matches only exact name
- Case-insensitive matching for robustness
- AllowList checked first, then BlockList (precedence logic)

**Edge Filtering Approach:**
- Immutable approach: creates new DependencyGraph, doesn't modify input
- All vertices retained, only edges filtered
- Handles both ProjectReference and BinaryReference edge types
- Empty graph returns empty graph
- Null/empty BlockList/AllowList handled gracefully

**Testing:**
- Created 16 comprehensive unit tests in FrameworkFilterTests.cs
- Tests cover: null handling, empty graph, System.*, Microsoft.*, mscorlib, netstandard filtering
- Tests verify: AllowList precedence, case-insensitive matching, wildcard vs exact patterns
- Integration test with SampleMonolith solution validates end-to-end filtering
- All 125 tests in test suite pass (no regressions)

**Acceptance Criteria Validation:**
- ✅ All edges to projects matching Microsoft.*, System.*, mscorlib, netstandard are removed
- ✅ Projects matching AllowList patterns are retained (precedence over BlockList)
- ✅ Filtered graph contains only custom code dependencies
- ✅ ILogger logs count of filtered dependencies with percentages
- ✅ Filter rules loaded from filter-config.json with PascalCase property names

**Technical Decisions:**
- Pattern matching implemented as private methods in FrameworkFilter (simpler than separate helper class)
- Used TestLogger implementation in tests to verify logging behavior (instead of Moq)
- Integration test gracefully skips if SampleMonolith not present (CI compatibility)

### File List

**New Files Created:**
- src/MasDependencyMap.Core/Filtering/IFrameworkFilter.cs
- src/MasDependencyMap.Core/Filtering/FrameworkFilter.cs
- tests/MasDependencyMap.Core.Tests/Filtering/FrameworkFilterTests.cs

**Modified Files:**
- src/MasDependencyMap.CLI/Program.cs (added IFrameworkFilter registration and namespace import)

## Change Log

**2026-01-23:** Story 2.6 implementation completed
- Created IFrameworkFilter interface with FilterAsync method in new Filtering namespace
- Implemented FrameworkFilter with wildcard pattern matching (Microsoft.*, System.*)
- Integrated with FilterConfiguration from Story 1-4 using IOptions<T> pattern
- Implemented AllowList precedence over BlockList for flexible filtering
- Added structured logging with filtering statistics (blocked/retained counts and percentages)
- Registered IFrameworkFilter in DI container following TryAddSingleton pattern
- Created 16 comprehensive unit tests covering all edge cases and acceptance criteria
- Added integration test with SampleMonolith solution for end-to-end validation
- All 125 tests pass with no regressions
- All acceptance criteria satisfied and validated
- Story marked as "review" and ready for code review
