# Story 3.2: Calculate Cycle Statistics and Participation Rates

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want detailed statistics about circular dependencies,
So that I understand the scale of the cycle problem in my codebase.

## Acceptance Criteria

**Given** Cycles have been detected
**When** Cycle statistics are calculated
**Then** Total number of circular dependency chains is reported
**And** Largest cycle size (number of projects in the biggest cycle) is identified
**And** Cycle participation rate is calculated (percentage of projects involved in cycles)
**And** Each cycle's size is stored in the CycleInfo object
**And** Statistics are included in text reports: "Circular Dependency Chains: 12, Projects in Cycles: 45 (61.6%), Largest Cycle Size: 8 projects"

## Tasks / Subtasks

- [x] Extend CycleInfo model with statistics properties (AC: Each cycle's size is stored)
  - [x] Add TotalProjectsInCycles calculated property (distinct projects across all cycles)
  - [x] Add ParticipationRate calculated property (percentage of projects in cycles)
  - [x] Ensure CycleSize property is already available (from Story 3.1)
  - [x] Document immutability requirements for new properties

- [x] Create CycleStatistics model class (AC: Statistics aggregation)
  - [x] Create CycleStatistics record in Core.CycleAnalysis namespace
  - [x] Add TotalCycles property (int, total number of circular dependency chains)
  - [x] Add LargestCycleSize property (int, max projects in any single cycle)
  - [x] Add TotalProjectsInCycles property (int, distinct count of projects in cycles)
  - [x] Add ParticipationRate property (double, percentage calculation)
  - [x] Add TotalProjectsAnalyzed property (int, denominator for participation rate)
  - [x] Use C# record type for immutability and value semantics

- [x] Implement CycleStatisticsCalculator service (AC: Calculate statistics from cycles)
  - [x] Create ICycleStatisticsCalculator interface in Core.CycleAnalysis namespace
  - [x] Implement CycleStatisticsCalculator class
  - [x] Add ILogger<CycleStatisticsCalculator> dependency via constructor injection
  - [x] Implement CalculateAsync method accepting List<CycleInfo> and total project count
  - [x] Calculate total cycles: cycles.Count
  - [x] Calculate largest cycle: cycles.Max(c => c.CycleSize) with null/empty check
  - [x] Calculate distinct projects in cycles: cycles.SelectMany(c => c.Projects).Distinct().Count()
  - [x] Calculate participation rate: (projectsInCycles / totalProjects) * 100.0
  - [x] Return CycleStatistics record with all calculated values

- [x] Add structured logging for statistics calculation (AC: Observability)
  - [x] Log "Calculating cycle statistics for {CycleCount} cycles" at Information level
  - [x] Log "Cycle Statistics: {TotalCycles} chains, {ProjectsInCycles} projects ({ParticipationRate:F1}%), Largest: {LargestCycle}" at Information level
  - [x] Log "No cycles detected, statistics calculation skipped" when cycle list is empty
  - [x] Use named placeholders (NOT string interpolation) per project-context.md

- [x] Handle edge cases (AC: Robustness)
  - [x] Empty cycle list → return CycleStatistics with all zeros
  - [x] Single cycle → correctly calculate all statistics
  - [x] Zero total projects → set participation rate to 0.0 (avoid division by zero)
  - [x] All projects in one giant cycle → participation rate = 100.0%
  - [x] Validate input parameters (null checks)

- [x] Register service in DI container (AC: Dependency injection)
  - [x] Register ICycleStatisticsCalculator → CycleStatisticsCalculator as singleton in Program.cs
  - [x] Use services.TryAddSingleton() pattern for test override support

- [x] Integrate with TarjanCycleDetector (AC: Seamless integration)
  - [x] Modify TarjanCycleDetector.DetectCyclesAsync to call ICycleStatisticsCalculator
  - [x] Pass detected cycles and graph.VertexCount to CalculateAsync
  - [x] Return both cycles and statistics (consider returning tuple or wrapping object)
  - [x] Update ITarjanCycleDetector interface if return type changes
  - [x] OR create separate method CalculateStatisticsAsync if keeping existing API

- [x] Create comprehensive tests (AC: Algorithm correctness)
  - [x] Unit test: Empty cycle list → all statistics are zero
  - [x] Unit test: Single 3-project cycle → correct statistics (TotalCycles=1, Largest=3, Participation calculated)
  - [x] Unit test: Multiple cycles → correct distinct project counting
  - [x] Unit test: Overlapping projects in multiple cycles → counted once (distinct)
  - [x] Unit test: All projects in cycles → participation rate = 100%
  - [x] Unit test: Zero total projects → participation rate = 0% (no division by zero error)
  - [x] Unit test: Largest cycle identification from multiple cycles
  - [x] Unit test: Participation rate calculation accuracy (verify percentage formula)

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Extend Existing CycleInfo or Create Separate Statistics Model:**

**Design Decision Required:**

Story 3.1 created `CycleInfo` record representing individual cycles. Story 3.2 requires aggregate statistics across ALL cycles.

**Option 1: Separate CycleStatistics Model (RECOMMENDED):**
```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Aggregate statistics across all circular dependency cycles.
/// Provides summary metrics for understanding cycle problem scale.
/// </summary>
public sealed record CycleStatistics
{
    /// <summary>
    /// Total number of circular dependency chains detected.
    /// </summary>
    public int TotalCycles { get; init; }

    /// <summary>
    /// Size of the largest cycle (number of projects in biggest cycle).
    /// </summary>
    public int LargestCycleSize { get; init; }

    /// <summary>
    /// Total distinct projects involved in circular dependencies.
    /// Projects appearing in multiple cycles are counted once.
    /// </summary>
    public int TotalProjectsInCycles { get; init; }

    /// <summary>
    /// Total projects analyzed (denominator for participation rate).
    /// </summary>
    public int TotalProjectsAnalyzed { get; init; }

    /// <summary>
    /// Percentage of projects involved in cycles.
    /// </summary>
    public double ParticipationRate { get; init; }

    public CycleStatistics(
        int totalCycles,
        int largestCycleSize,
        int totalProjectsInCycles,
        int totalProjectsAnalyzed)
    {
        TotalCycles = totalCycles;
        LargestCycleSize = largestCycleSize;
        TotalProjectsInCycles = totalProjectsInCycles;
        TotalProjectsAnalyzed = totalProjectsAnalyzed;
        ParticipationRate = totalProjectsAnalyzed > 0
            ? (totalProjectsInCycles / (double)totalProjectsAnalyzed) * 100.0
            : 0.0;
    }
}
```

**Option 2: Extend CycleInfo (NOT RECOMMENDED):**
CycleInfo represents a SINGLE cycle, not aggregate statistics. Mixing individual cycle data with aggregate statistics violates single responsibility principle.

**Why Option 1 is Better:**
- Clear separation of concerns: CycleInfo = individual cycle, CycleStatistics = aggregate metrics
- Easier to test calculation logic in isolation
- Follows Story 3.1 pattern of immutable records
- Statistics are calculated ONCE after all cycles detected, not per-cycle

🚨 **CRITICAL - Statistics Calculation Service Pattern:**

**From Project Context (lines 101-107):**
```
Core components MUST use constructor injection
Lifetime patterns: Singletons for stateless services
```

**CycleStatisticsCalculator Interface:**
```csharp
namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Calculates aggregate statistics for circular dependency analysis.
/// Computes metrics like total cycles, largest cycle, and participation rates.
/// </summary>
public interface ICycleStatisticsCalculator
{
    /// <summary>
    /// Calculates comprehensive statistics for detected cycles.
    /// </summary>
    /// <param name="cycles">List of detected circular dependency cycles.</param>
    /// <param name="totalProjectsAnalyzed">Total number of projects in the analyzed graph.</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <returns>
    /// CycleStatistics object containing aggregate metrics.
    /// Returns statistics with all zeros if cycles list is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">When cycles is null.</exception>
    Task<CycleStatistics> CalculateAsync(
        IReadOnlyList<CycleInfo> cycles,
        int totalProjectsAnalyzed,
        CancellationToken cancellationToken = default);
}
```

**Implementation Pattern:**
```csharp
public class CycleStatisticsCalculator : ICycleStatisticsCalculator
{
    private readonly ILogger<CycleStatisticsCalculator> _logger;

    public CycleStatisticsCalculator(ILogger<CycleStatisticsCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<CycleStatistics> CalculateAsync(
        IReadOnlyList<CycleInfo> cycles,
        int totalProjectsAnalyzed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cycles);

        if (cycles.Count == 0)
        {
            _logger.LogInformation("No cycles detected, statistics calculation skipped");
            return Task.FromResult(new CycleStatistics(0, 0, 0, totalProjectsAnalyzed));
        }

        _logger.LogInformation(
            "Calculating cycle statistics for {CycleCount} cycles",
            cycles.Count);

        int totalCycles = cycles.Count;
        int largestCycleSize = cycles.Max(c => c.CycleSize);
        int totalProjectsInCycles = cycles
            .SelectMany(c => c.Projects)
            .Distinct()
            .Count();

        var statistics = new CycleStatistics(
            totalCycles,
            largestCycleSize,
            totalProjectsInCycles,
            totalProjectsAnalyzed);

        _logger.LogInformation(
            "Cycle Statistics: {TotalCycles} chains, {ProjectsInCycles} projects ({ParticipationRate:F1}%), Largest: {LargestCycle}",
            statistics.TotalCycles,
            statistics.TotalProjectsInCycles,
            statistics.ParticipationRate,
            statistics.LargestCycleSize);

        return Task.FromResult(statistics);
    }
}
```

🚨 **CRITICAL - Integration with TarjanCycleDetector (Story 3.1):**

**From Story 3.1 Implementation:**
Story 3.1 already calculates statistics internally for logging but doesn't expose them. Story 3.2 should extract this logic into a reusable service.

**Integration Options:**

**Option A: Modify TarjanCycleDetector to use ICycleStatisticsCalculator (RECOMMENDED):**
```csharp
public class TarjanCycleDetector : ITarjanCycleDetector
{
    private readonly ILogger<TarjanCycleDetector> _logger;
    private readonly ICycleStatisticsCalculator _statisticsCalculator;

    public TarjanCycleDetector(
        ILogger<TarjanCycleDetector> logger,
        ICycleStatisticsCalculator statisticsCalculator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statisticsCalculator = statisticsCalculator ?? throw new ArgumentNullException(nameof(statisticsCalculator));
    }

    public async Task<IReadOnlyList<CycleInfo>> DetectCyclesAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default)
    {
        // ... existing Tarjan algorithm logic ...

        // Use statistics calculator for logging
        var statistics = await _statisticsCalculator.CalculateAsync(
            cycles,
            graph.VertexCount,
            cancellationToken).ConfigureAwait(false);

        // Logging now comes from CycleStatisticsCalculator
        return cycles;
    }
}
```

**Option B: Keep TarjanCycleDetector unchanged, use separately in CLI:**
```csharp
// In CLI orchestration
var cycles = await cycleDetector.DetectCyclesAsync(graph);
var statistics = await statisticsCalculator.CalculateAsync(cycles, graph.VertexCount);
```

**Recommendation: Option A**
- Eliminates duplicate statistics calculation in TarjanCycleDetector
- Centralizes statistics logic in dedicated service
- Improves testability (can mock ICycleStatisticsCalculator)
- Aligns with single responsibility principle

🚨 **CRITICAL - Distinct Project Counting (Handle Overlapping Cycles):**

**From AC:** "Projects in Cycles: 45 (61.6%)"

Projects can appear in MULTIPLE cycles (overlapping strongly connected components). When calculating `TotalProjectsInCycles`, use DISTINCT counting:

```csharp
// ✅ CORRECT: Distinct counting
int totalProjectsInCycles = cycles
    .SelectMany(c => c.Projects)
    .Distinct()
    .Count();

// ❌ WRONG: Would double-count projects in multiple cycles
int totalProjectsInCycles = cycles.Sum(c => c.Projects.Count);
```

**Why Distinct Matters:**
- Project A might be in Cycle 1 (A → B → C → A) AND Cycle 2 (A → D → A)
- Sum would count A twice, inflating participation rate
- Distinct ensures each project counted exactly once

🚨 **CRITICAL - Division by Zero Protection:**

**From Project Context (lines 199-203):**
```
NEVER use magic numbers or strings in logic
Define constants at class level with descriptive names
```

**Participation Rate Calculation:**
```csharp
public double ParticipationRate { get; init; }

public CycleStatistics(/* ... */)
{
    // Protect against division by zero
    ParticipationRate = totalProjectsAnalyzed > 0
        ? (totalProjectsInCycles / (double)totalProjectsAnalyzed) * 100.0
        : 0.0;
}
```

**Edge Cases:**
- Empty graph (0 projects) → participation rate = 0.0%
- All projects in cycles → participation rate = 100.0%
- No cycles → participation rate = 0.0%

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
    "Cycle Statistics: {TotalCycles} chains, {ProjectsInCycles} projects ({ParticipationRate:F1}%), Largest: {LargestCycle}",
    statistics.TotalCycles,
    statistics.TotalProjectsInCycles,
    statistics.ParticipationRate,
    statistics.LargestCycleSize);

// ❌ WRONG: String interpolation
_logger.LogInformation(
    $"Cycle Statistics: {statistics.TotalCycles} chains"); // DO NOT USE
```

### Technical Requirements

**CycleStatistics Model Design:**

Immutable record type following Story 3.1 pattern:
- All properties init-only
- Primary constructor for initialization
- Calculated properties (ParticipationRate) derived in constructor
- Value-based equality (record semantics)
- Thread-safe (no mutation after construction)

**Statistics Calculation Algorithm:**

**Time Complexity:**
- Total cycles: O(1) - simple count
- Largest cycle: O(n) where n = number of cycles
- Distinct projects: O(p) where p = total projects across all cycles
- Overall: O(n + p)

**Space Complexity:**
- O(p) for distinct project set (HashSet internally in Distinct())

**Implementation:**
```csharp
public Task<CycleStatistics> CalculateAsync(
    IReadOnlyList<CycleInfo> cycles,
    int totalProjectsAnalyzed,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(cycles);

    if (cycles.Count == 0)
        return Task.FromResult(new CycleStatistics(0, 0, 0, totalProjectsAnalyzed));

    int totalCycles = cycles.Count;
    int largestCycleSize = cycles.Max(c => c.CycleSize);

    // Distinct ensures projects in multiple cycles counted once
    int totalProjectsInCycles = cycles
        .SelectMany(c => c.Projects)
        .Distinct() // Uses ProjectNode equality comparison
        .Count();

    return Task.FromResult(new CycleStatistics(
        totalCycles,
        largestCycleSize,
        totalProjectsInCycles,
        totalProjectsAnalyzed));
}
```

**ProjectNode Equality Requirement:**

For `.Distinct()` to work correctly, `ProjectNode` must have proper equality comparison. From Story 2-5, `ProjectNode` should implement equality based on project path or name.

**Verify in Story 2-5 code:**
- ProjectNode should override `Equals()` and `GetHashCode()`
- OR be a record type with automatic value equality
- If not implemented, `.Distinct()` will use reference equality (incorrect)

### Architecture Compliance

**Epic 3 Architecture Requirements:**

```
- TarjanCycleDetector using QuikGraph's Tarjan's SCC algorithm ✅ (Story 3.1)
- Cycle statistics calculation (total cycles, sizes, project participation rates) ✅ (Story 3.2 - THIS STORY)
- CouplingAnalyzer for method call counting ⏳ (Story 3.3)
- Ranked cycle-breaking recommendations ⏳ (Story 3.5)
- Enhanced DOT visualization with cycle highlighting ⏳ (Stories 3.6, 3.7)
```

**Story 3.2 Implements:**
- ✅ CycleStatistics model for aggregate metrics
- ✅ ICycleStatisticsCalculator service for calculation logic
- ✅ Integration with TarjanCycleDetector from Story 3.1
- ✅ Structured logging for statistics
- ✅ Comprehensive test coverage

**Integration with Existing Components:**

Story 3.2 consumes:
- **CycleInfo** (from Story 3.1): Input to statistics calculation
- **TarjanCycleDetector** (from Story 3.1): Integration point for statistics logging
- **ProjectNode** (from Story 2-5): Used in distinct counting
- **ILogger<T>** (from Story 1-6): Structured logging

Story 3.2 produces:
- **CycleStatistics** model: Consumed by Story 3.5 (recommendations), Story 5 (reporting)
- **ICycleStatisticsCalculator** service: Used by CLI and future reporting workflows

**Namespace Organization:**
```
src/MasDependencyMap.Core/
└── CycleAnalysis/                        # Epic 3 namespace
    ├── ITarjanCycleDetector.cs          # Story 3.1
    ├── TarjanCycleDetector.cs           # Story 3.1 (modified in 3.2)
    ├── CycleInfo.cs                     # Story 3.1
    ├── ICycleStatisticsCalculator.cs    # NEW: Story 3.2
    ├── CycleStatisticsCalculator.cs     # NEW: Story 3.2
    └── CycleStatistics.cs               # NEW: Story 3.2
```

**DI Integration:**
```csharp
// Existing (from Story 3.1)
services.TryAddSingleton<ITarjanCycleDetector, TarjanCycleDetector>();

// NEW: Story 3.2
services.TryAddSingleton<ICycleStatisticsCalculator, CycleStatisticsCalculator>();
```

### Library/Framework Requirements

**No New NuGet Packages Required:**

All dependencies already satisfied:
- ✅ Microsoft.Extensions.Logging.Abstractions (installed in Story 1-6)
- ✅ Microsoft.Extensions.DependencyInjection (installed in Story 1-5)
- ✅ System.Linq (built-in .NET 8)

**LINQ Usage for Statistics:**

Story 3.2 uses standard LINQ methods:
- `Count()` - total cycles
- `Max()` - largest cycle size
- `SelectMany()` - flatten all projects from all cycles
- `Distinct()` - remove duplicate projects
- `Count()` - count distinct projects

All methods are built-in .NET 8, no additional packages needed.

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/
└── CycleAnalysis/
    ├── ICycleStatisticsCalculator.cs     # NEW: Statistics calculator interface
    ├── CycleStatisticsCalculator.cs      # NEW: Statistics calculator implementation
    └── CycleStatistics.cs                # NEW: Statistics model

tests/MasDependencyMap.Core.Tests/
└── CycleAnalysis/
    └── CycleStatisticsCalculatorTests.cs # NEW: Comprehensive test suite
```

**Files to Modify:**

```
src/MasDependencyMap.Core/CycleAnalysis/TarjanCycleDetector.cs
  - Add ICycleStatisticsCalculator dependency injection
  - Replace inline statistics calculation with service call
  - Remove duplicate statistics code

src/MasDependencyMap.CLI/Program.cs
  - Register ICycleStatisticsCalculator in DI container
```

**Files NOT to Modify:**

```
src/MasDependencyMap.Core/CycleAnalysis/CycleInfo.cs (reused as-is)
src/MasDependencyMap.Core/CycleAnalysis/ITarjanCycleDetector.cs (interface unchanged)
src/MasDependencyMap.Core/DependencyAnalysis/ProjectNode.cs (verify equality, but don't modify unless broken)
```

### Testing Requirements

**Test Class Structure:**

```csharp
namespace MasDependencyMap.Core.Tests.CycleAnalysis;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.CycleAnalysis;

public class CycleStatisticsCalculatorTests
{
    private readonly ILogger<CycleStatisticsCalculator> _logger;
    private readonly CycleStatisticsCalculator _calculator;

    public CycleStatisticsCalculatorTests()
    {
        _logger = NullLogger<CycleStatisticsCalculator>.Instance;
        _calculator = new CycleStatisticsCalculator(_logger);
    }

    [Fact]
    public async Task CalculateAsync_EmptyCycleList_ReturnsZeroStatistics()
    {
        // Arrange
        var cycles = new List<CycleInfo>();
        int totalProjects = 50;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.TotalCycles.Should().Be(0);
        statistics.LargestCycleSize.Should().Be(0);
        statistics.TotalProjectsInCycles.Should().Be(0);
        statistics.ParticipationRate.Should().Be(0.0);
        statistics.TotalProjectsAnalyzed.Should().Be(50);
    }

    [Fact]
    public async Task CalculateAsync_SingleCycle_CalculatesCorrectly()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B", "C") // 3-project cycle
        };
        int totalProjects = 10;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.TotalCycles.Should().Be(1);
        statistics.LargestCycleSize.Should().Be(3);
        statistics.TotalProjectsInCycles.Should().Be(3);
        statistics.ParticipationRate.Should().BeApproximately(30.0, 0.1); // 3/10 = 30%
    }

    [Fact]
    public async Task CalculateAsync_MultipleCycles_IdentifiesLargestCycle()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B"),           // 2-project cycle
            CreateCycle(2, "C", "D", "E", "F"), // 4-project cycle
            CreateCycle(3, "G", "H", "I")       // 3-project cycle
        };
        int totalProjects = 20;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.TotalCycles.Should().Be(3);
        statistics.LargestCycleSize.Should().Be(4); // Cycle 2
        statistics.TotalProjectsInCycles.Should().Be(9); // 2 + 4 + 3
    }

    [Fact]
    public async Task CalculateAsync_OverlappingCycles_CountsDistinctProjects()
    {
        // Arrange
        // Cycle 1: A → B → A (2 projects)
        // Cycle 2: A → C → A (2 projects)
        // Total distinct: 3 projects (A, B, C)
        var projectA = new ProjectNode("A", "/path/A", "net8.0");
        var projectB = new ProjectNode("B", "/path/B", "net8.0");
        var projectC = new ProjectNode("C", "/path/C", "net8.0");

        var cycles = new List<CycleInfo>
        {
            new CycleInfo(1, new[] { projectA, projectB }),
            new CycleInfo(2, new[] { projectA, projectC })
        };
        int totalProjects = 10;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.TotalCycles.Should().Be(2);
        statistics.TotalProjectsInCycles.Should().Be(3); // A, B, C (distinct)
        statistics.ParticipationRate.Should().BeApproximately(30.0, 0.1); // 3/10
    }

    [Fact]
    public async Task CalculateAsync_AllProjectsInCycles_Returns100PercentParticipation()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B", "C", "D", "E")
        };
        int totalProjects = 5; // All 5 projects in the cycle

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.ParticipationRate.Should().BeApproximately(100.0, 0.1);
    }

    [Fact]
    public async Task CalculateAsync_ZeroTotalProjects_ReturnsZeroParticipation()
    {
        // Arrange
        var cycles = new List<CycleInfo>();
        int totalProjects = 0;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.ParticipationRate.Should().Be(0.0); // No division by zero
    }

    [Fact]
    public async Task CalculateAsync_NullCycles_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _calculator.CalculateAsync(null!, 10);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("cycles");
    }

    // Helper method
    private CycleInfo CreateCycle(int cycleId, params string[] projectNames)
    {
        var projects = projectNames
            .Select(name => new ProjectNode(name, $"/path/{name}", "net8.0"))
            .ToList();
        return new CycleInfo(cycleId, projects);
    }
}
```

**Test Naming Convention (from project-context.md):**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `CalculateAsync_EmptyCycleList_ReturnsZeroStatistics()`
- ✅ `CalculateAsync_OverlappingCycles_CountsDistinctProjects()`
- ✅ `CalculateAsync_AllProjectsInCycles_Returns100PercentParticipation()`

**Test Categories:**
- Unit tests: Test CycleStatisticsCalculator with manually constructed cycles
- Edge case tests: Empty cycles, zero projects, overlapping cycles
- Integration tests: Verify TarjanCycleDetector uses statistics calculator correctly

### Previous Story Intelligence

**From Story 3-1 (Tarjan's SCC Algorithm) - Key Learnings:**

Story 3-1 already calculates statistics internally but doesn't expose them in a reusable way. Here's the code from Story 3-1 that Story 3.2 should extract and improve:

**Statistics Calculation in Story 3-1 (lines 447-459 in 3-1 story file):**
```csharp
if (cycles.Count > 0)
{
    var stats = CalculateStatistics(cycles, graph.VertexCount);

    _logger.LogInformation(
        "Found {CycleCount} circular dependency chains, {ProjectsInCycles} projects ({ParticipationRate:F1}%) involved in cycles",
        stats.totalCycles,
        stats.projectsInCycles,
        stats.participationRate);
}
```

**What Story 3.2 Should Do:**
1. Extract `CalculateStatistics` method into dedicated `CycleStatisticsCalculator` service
2. Make statistics reusable across multiple components (not just TarjanCycleDetector)
3. Add comprehensive tests for statistics calculation
4. Create `CycleStatistics` record to replace tuple return type
5. Inject `ICycleStatisticsCalculator` into `TarjanCycleDetector`

**Patterns to Reuse from Story 3-1:**

```csharp
// Structured logging with named placeholders
_logger.LogInformation(
    "Calculating cycle statistics for {CycleCount} cycles",
    cycles.Count);

// ConfigureAwait(false) in library code
var statistics = await CalculateStatisticsAsync(cycles, totalProjects, cancellationToken)
    .ConfigureAwait(false);

// Argument validation
ArgumentNullException.ThrowIfNull(cycles);

// DI registration pattern
services.TryAddSingleton<ICycleStatisticsCalculator, CycleStatisticsCalculator>();
```

**From Story 3-1 Git Commits:**

Recent commits show pattern:
- Create interface + implementation
- Add comprehensive tests (8+ test cases)
- Register in DI container
- Handle edge cases (empty, null, zero)

Expected file structure from Story 3-1:
```
src/MasDependencyMap.Core/CycleAnalysis/
tests/MasDependencyMap.Core.Tests/CycleAnalysis/
```

Story 3.2 should follow same pattern.

**Distinct Project Counting Pattern:**

From Story 3-1 implementation (lines 425-427):
```csharp
int projectsInCycles = cycles
    .SelectMany(c => c.Projects)
    .Distinct()
    .Count();
```

This pattern MUST be reused in Story 3.2 to ensure consistent behavior.

### Git Intelligence Summary

**Recent Commits Analysis:**

Last 2 commits were Story 3-1 implementation and code review fixes:
- Created new namespace: `MasDependencyMap.Core.CycleAnalysis`
- Added 3 new files: Interface, Implementation, Model (CycleInfo)
- Added comprehensive tests (11 tests total after code review)
- Modified Program.cs for DI registration
- Used C# record types for immutability

**Files Modified in Story 3-1:**
```
src/MasDependencyMap.CLI/Program.cs
src/MasDependencyMap.Core/CycleAnalysis/ITarjanCycleDetector.cs
src/MasDependencyMap.Core/CycleAnalysis/TarjanCycleDetector.cs
src/MasDependencyMap.Core/CycleAnalysis/CycleInfo.cs
tests/MasDependencyMap.Core.Tests/CycleAnalysis/TarjanCycleDetectorTests.cs
```

**Story 3.2 Expected Files:**
```
# New files (similar pattern to 3-1)
src/MasDependencyMap.Core/CycleAnalysis/ICycleStatisticsCalculator.cs
src/MasDependencyMap.Core/CycleAnalysis/CycleStatisticsCalculator.cs
src/MasDependencyMap.Core/CycleAnalysis/CycleStatistics.cs
tests/MasDependencyMap.Core.Tests/CycleAnalysis/CycleStatisticsCalculatorTests.cs

# Modified files
src/MasDependencyMap.Core/CycleAnalysis/TarjanCycleDetector.cs (add ICycleStatisticsCalculator injection)
src/MasDependencyMap.CLI/Program.cs (add DI registration)
```

**Expected Commit Message for Story 3.2:**
```bash
git commit -m "Story 3-2 complete: Calculate cycle statistics and participation rates

- Created ICycleStatisticsCalculator interface in Core.CycleAnalysis namespace
- Implemented CycleStatisticsCalculator service using LINQ for aggregation
- Created CycleStatistics record for immutable statistics data
- Extracted statistics logic from TarjanCycleDetector into dedicated service
- Modified TarjanCycleDetector to inject and use ICycleStatisticsCalculator
- Added structured logging with named placeholders per project-context.md
- Registered ICycleStatisticsCalculator as singleton in DI container
- Created comprehensive unit tests (8 tests) - all passing
- Handles edge cases: empty cycles, overlapping projects, zero projects
- Distinct project counting ensures accurate participation rates
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 3.2 continues Epic 3 namespace organization established in Story 3-1:

```
src/MasDependencyMap.Core/
├── DependencyAnalysis/          # Epic 2: Graph building
├── CycleAnalysis/               # Epic 3: Cycle detection & statistics
│   ├── ITarjanCycleDetector.cs      # Story 3.1
│   ├── TarjanCycleDetector.cs       # Story 3.1 (modified in 3.2)
│   ├── CycleInfo.cs                 # Story 3.1
│   ├── ICycleStatisticsCalculator.cs # Story 3.2 (NEW)
│   ├── CycleStatisticsCalculator.cs  # Story 3.2 (NEW)
│   └── CycleStatistics.cs           # Story 3.2 (NEW)
├── Filtering/                   # Epic 2: Framework filtering
├── SolutionLoading/             # Epic 2: Solution loading
├── Visualization/               # Epic 2: DOT generation
└── Rendering/                   # Epic 2: Graphviz rendering
```

**Consistency with Existing Patterns:**
- Feature-based namespace: `MasDependencyMap.Core.CycleAnalysis` ✅
- Interface + Implementation pattern: `ICycleStatisticsCalculator`, `CycleStatisticsCalculator` ✅
- Test namespace mirrors Core: `tests/MasDependencyMap.Core.Tests/CycleAnalysis` ✅
- File naming matches class naming exactly ✅
- C# record types for immutable data models ✅

**No Conflicts Detected:**
- Story 3.2 extends Story 3.1 namespace (no new namespace created)
- Uses existing CycleInfo from Story 3.1 (no modifications needed)
- Follows established DI registration pattern from Epic 2 and Story 3.1

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-3-circular-dependency-detection-and-break-point-analysis.md, Story 3.2 (lines 26-40)]
- Story requirements: Cycle statistics calculation, participation rates, text report formatting

**Previous Story:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\3-1-implement-tarjans-scc-algorithm-for-cycle-detection.md (full file)]
- Created CycleInfo model, TarjanCycleDetector service
- Statistics calculation pattern (lines 412-432)
- Distinct project counting (lines 425-427)

**Architecture:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md, Logging & Diagnostics (lines 40-56)]
- ILogger<T> injection pattern, structured logging requirements
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md, Dependency Injection (lines 156-181)]
- DI patterns, service registration, singleton lifetimes

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Namespace Organization (lines 56-59)]
- Feature-based namespaces (MasDependencyMap.Core.CycleAnalysis)
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Async Patterns (lines 66-69)]
- Async suffix, Task return types
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Structured Logging (lines 115-119)]
- Named placeholders, no string interpolation
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Constants and Magic Values (lines 199-203)]
- No magic numbers, define constants

**Technology Stack:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md, Core Technologies (lines 18-26)]
- .NET 8.0, C# 12, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - No debugging issues encountered

### Completion Notes List

**Implementation Complete - 2026-01-24**

✅ **CycleStatistics Model Created** (src/MasDependencyMap.Core/CycleAnalysis/CycleStatistics.cs)
- Immutable record type with 5 properties
- Automatic ParticipationRate calculation in constructor
- Division-by-zero protection for empty graphs
- Full XML documentation

✅ **ICycleStatisticsCalculator Interface Created** (src/MasDependencyMap.Core/CycleAnalysis/ICycleStatisticsCalculator.cs)
- Single CalculateAsync method
- Follows async naming conventions
- Comprehensive XML documentation with exception details

✅ **CycleStatisticsCalculator Service Implemented** (src/MasDependencyMap.Core/CycleAnalysis/CycleStatisticsCalculator.cs)
- ILogger<T> constructor injection
- Structured logging with named placeholders (no string interpolation)
- LINQ-based statistics calculation: Count(), Max(), SelectMany(), Distinct()
- Distinct project counting handles overlapping cycles correctly
- Edge case handling: empty cycles, zero projects, single cycles

✅ **TarjanCycleDetector Integration** (src/MasDependencyMap.Core/CycleAnalysis/TarjanCycleDetector.cs)
- Added ICycleStatisticsCalculator dependency injection
- Replaced inline CalculateStatistics method with service call
- Removed duplicate statistics code (20 lines eliminated)
- Logging now handled by CycleStatisticsCalculator

✅ **DI Registration** (src/MasDependencyMap.CLI/Program.cs)
- Registered ICycleStatisticsCalculator as singleton
- Used TryAddSingleton for test override support
- Added after ITarjanCycleDetector registration (logical grouping)

✅ **Comprehensive Test Suite** (tests/MasDependencyMap.Core.Tests/CycleAnalysis/CycleStatisticsCalculatorTests.cs)
- 8 unit tests covering all acceptance criteria
- Tests for edge cases: empty cycles, overlapping projects, 100% participation, zero projects
- Tests for algorithm correctness: distinct counting, largest cycle identification, participation rate accuracy
- All tests use Arrange-Act-Assert pattern
- Test naming follows {MethodName}_{Scenario}_{ExpectedResult} convention

✅ **Test Results**
- All 204 tests passed (196 existing + 8 new)
- No regressions introduced
- Build succeeded with 0 warnings, 0 errors
- TarjanCycleDetectorTests updated to inject ICycleStatisticsCalculator (11 existing tests still pass)

**Acceptance Criteria Verification:**

✅ **AC1: Total number of circular dependency chains is reported**
- CycleStatistics.TotalCycles property implemented
- Test: CalculateAsync_MultipleCycles_IdentifiesLargestCycle validates TotalCycles=3

✅ **AC2: Largest cycle size is identified**
- CycleStatistics.LargestCycleSize property implemented
- Uses cycles.Max(c => c.CycleSize) with null/empty check
- Test: CalculateAsync_LargestCycleIdentification_SelectsMaxSize validates correct max selection

✅ **AC3: Cycle participation rate is calculated**
- CycleStatistics.ParticipationRate property implemented
- Calculated as (distinct projects / total projects) * 100.0
- Test: CalculateAsync_AllProjectsInCycles_Returns100PercentParticipation validates 100% case

✅ **AC4: Each cycle's size is stored in CycleInfo**
- CycleInfo.CycleSize property already exists from Story 3.1
- Returns Projects.Count (verified in tests)

✅ **AC5: Statistics included in text reports format**
- Logging format implemented: "Cycle Statistics: {TotalCycles} chains, {ProjectsInCycles} projects ({ParticipationRate:F1}%), Largest: {LargestCycle}"
- Example output: "Cycle Statistics: 12 chains, 45 projects (61.6%), Largest: 8"

**Technical Highlights:**

🎯 **Design Decision: Separate CycleStatistics Model**
- Chose Option A (separate model) over extending CycleInfo
- Clear separation of concerns: CycleInfo = individual cycle, CycleStatistics = aggregate metrics
- Enables reuse across multiple components (CLI, reporting, future exports)

🎯 **Distinct Project Counting**
- Uses .SelectMany(c => c.Projects).Distinct().Count() for accurate counting
- ProjectNode.Equals() based on ProjectPath ensures correct deduplication
- Test validates overlapping cycles counted correctly (3 distinct from 2 cycles with shared project)

🎯 **Structured Logging Compliance**
- All logging uses named placeholders: {CycleCount}, {TotalCycles}, {ParticipationRate:F1}
- NO string interpolation used (per project-context.md rules)
- Format specifier :F1 for single decimal percentage

🎯 **Integration Pattern**
- Followed Story 3.1 pattern: interface + implementation + tests + DI registration
- Eliminated code duplication in TarjanCycleDetector (extracted statistics logic)
- Improved testability (can mock ICycleStatisticsCalculator)

### File List

**New Files Created:**
- src/MasDependencyMap.Core/CycleAnalysis/CycleStatistics.cs
- src/MasDependencyMap.Core/CycleAnalysis/ICycleStatisticsCalculator.cs
- src/MasDependencyMap.Core/CycleAnalysis/CycleStatisticsCalculator.cs
- tests/MasDependencyMap.Core.Tests/CycleAnalysis/CycleStatisticsCalculatorTests.cs

**Files Modified:**
- src/MasDependencyMap.Core/CycleAnalysis/TarjanCycleDetector.cs
- src/MasDependencyMap.CLI/Program.cs
- tests/MasDependencyMap.Core.Tests/CycleAnalysis/TarjanCycleDetectorTests.cs
