# Story 4.5: Implement Extraction Score Calculator with Configurable Weights

Status: completed

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want all metrics combined into a 0-100 extraction difficulty score using configurable weights,
So that I can customize scoring to my organization's priorities.

## Acceptance Criteria

**Given** All four metrics are calculated (Coupling, Complexity, TechDebt, ExternalExposure)
**When** ExtractionScoreCalculator.CalculateAsync() is called
**Then** Weights are loaded from scoring-config.json (Coupling: 0.40, Complexity: 0.30, TechDebt: 0.20, ExternalExposure: 0.10)
**And** Final score = (Coupling * 0.40) + (Complexity * 0.30) + (TechDebt * 0.20) + (ExternalExposure * 0.10)
**And** Score is clamped to 0-100 range
**And** ExtractionScore object contains: project name, final score, individual metric scores, supporting details
**And** Lower scores (0-33) indicate easier extraction, higher scores (67-100) indicate harder extraction

**Given** Custom weights are provided in scoring-config.json
**When** Weights are loaded
**Then** Custom weights are validated (sum must equal 1.0)
**And** Configuration validation error is thrown if weights don't sum to 1.0
**And** ILogger logs which weights are being used

## Tasks / Subtasks

- [x] Create ScoringWeights configuration model (AC: Load configurable weights)
  - [x] Define ScoringWeights record with properties: CouplingWeight, ComplexityWeight, TechDebtWeight, ExternalExposureWeight
  - [x] Add validation method: ValidateWeights() checks sum equals 1.0 (with 0.01 tolerance for floating point)
  - [x] Use record type for immutability (C# 9+ pattern, consistent with Stories 4.1-4.4)
  - [x] Add XML documentation explaining weight semantics and validation rules
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [x] Create ExtractionScore model class (AC: Store final extraction difficulty score)
  - [x] Define ExtractionScore record with properties: ProjectName, ProjectPath, FinalScore, CouplingMetric, ComplexityMetric, TechDebtMetric, ExternalApiMetric
  - [x] Add computed property: DifficultyCategory (Easy: 0-33, Medium: 34-66, Hard: 67-100)
  - [x] Use record type for immutability
  - [x] Include all four individual metric objects for detailed breakdown
  - [x] Add XML documentation explaining scoring interpretation
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [x] Create IExtractionScoreCalculator interface (AC: Abstraction for DI)
  - [x] Define CalculateAsync(ProjectNode project, CancellationToken cancellationToken = default) method signature
  - [x] Return Task<ExtractionScore> for single project analysis
  - [x] Define CalculateForAllProjectsAsync(IEnumerable<ProjectNode> projects, CancellationToken cancellationToken = default) for batch analysis
  - [x] Return Task<IReadOnlyList<ExtractionScore>> for batch results
  - [x] Add XML documentation with examples and exception documentation
  - [x] Place in `MasDependencyMap.Core.ExtractionScoring` namespace

- [x] Implement ExtractionScoreCalculator class skeleton (AC: Set up orchestration infrastructure)
  - [x] Implement IExtractionScoreCalculator interface
  - [x] Inject all four metric calculators via constructor: ICouplingMetricCalculator, IComplexityMetricCalculator, ITechDebtAnalyzer, IExternalApiDetector
  - [x] Inject IConfiguration for loading scoring-config.json
  - [x] Inject ILogger<ExtractionScoreCalculator> for structured logging
  - [x] File-scoped namespace declaration (C# 10+ pattern)
  - [x] Async methods with Async suffix and ConfigureAwait(false) per project conventions

- [x] Implement configuration loading (AC: Load weights from scoring-config.json)
  - [x] Load scoring-config.json from current directory using IConfiguration
  - [x] Bind configuration to ScoringWeights record using IConfiguration.GetSection("ScoringWeights").Get<ScoringWeights>()
  - [x] If config file missing or section missing → use default weights (0.40, 0.30, 0.20, 0.10)
  - [x] Validate weights sum to 1.0 using ScoringWeights.ValidateWeights() method
  - [x] Throw ConfigurationException if weights validation fails (include detailed error message)
  - [x] Log Information: "Using scoring weights: Coupling={CouplingWeight}, Complexity={ComplexityWeight}, TechDebt={TechDebtWeight}, ExternalExposure={ExternalExposureWeight}"
  - [x] Cache loaded weights in private field (load once per calculator instance)

- [x] Implement CalculateAsync for single project (AC: Calculate extraction score for one project)
  - [x] Call ICouplingMetricCalculator to get coupling metric (needs entire graph, special handling)
  - [x] Call IComplexityMetricCalculator.CalculateAsync(project) to get complexity metric
  - [x] Call ITechDebtAnalyzer.AnalyzeAsync(project) to get tech debt metric
  - [x] Call IExternalApiDetector.DetectAsync(project) to get API exposure metric
  - [x] Calculate weighted score: (coupling * weightC) + (complexity * weightCx) + (techDebt * weightT) + (apiExposure * weightE)
  - [x] Use NormalizedScore from each metric (all are 0-100 scale)
  - [x] Clamp final score to 0-100 range using Math.Clamp(score, 0, 100)
  - [x] Create ExtractionScore with all individual metrics and final score
  - [x] Log Debug: "Calculated extraction score for {ProjectName}: {FinalScore} (Coupling={CouplingScore}, Complexity={ComplexityScore}, TechDebt={TechDebtScore}, ApiExposure={ApiExposureScore})"

- [x] Implement CalculateForAllProjectsAsync for batch processing (AC: Calculate scores for entire solution)
  - [x] Accept IEnumerable<ProjectNode> as input (all projects in solution)
  - [x] Calculate coupling metrics for ALL projects first (ICouplingMetricCalculator.CalculateAsync(graph))
  - [x] For each project: calculate complexity, tech debt, API exposure in parallel if possible
  - [x] Build dictionary: ProjectName → CouplingMetric for fast lookup
  - [x] For each project: combine all four metrics using weights
  - [x] Return IReadOnlyList<ExtractionScore> sorted by FinalScore ascending (easiest first)
  - [x] Log Information: "Calculated extraction scores for {ProjectCount} projects: {EasyCount} easy (0-33), {MediumCount} medium (34-66), {HardCount} hard (67-100)"
  - [x] Support cancellation via CancellationToken

- [x] Handle coupling metric special case (AC: Coupling is relative, requires entire graph)
  - [x] Coupling metric calculation requires entire dependency graph (different from other metrics)
  - [x] ICouplingMetricCalculator.CalculateAsync(DependencyGraph graph) returns metrics for ALL projects at once
  - [x] In CalculateAsync(single project): Need to pass full graph or accept pre-calculated coupling metric as parameter
  - [x] Alternative approach: Require CalculateForAllProjectsAsync for coupling-aware scoring
  - [x] Document limitation: Single project scoring may not have accurate coupling metric
  - [x] Decision: CalculateAsync(single) returns ExtractionScore with coupling = 0 if graph not available (document this)
  - [x] CalculateForAllProjectsAsync is preferred method for accurate scoring

- [x] Add weight validation error handling (AC: Configuration validation)
  - [x] Create custom ConfigurationException for weight validation failures (if not already in project)
  - [x] If weights don't sum to 1.0 (±0.01 tolerance): throw ConfigurationException
  - [x] Error message format: "Scoring weights must sum to 1.0. Current sum: {sum}. Weights: Coupling={c}, Complexity={cx}, TechDebt={t}, ExternalExposure={e}"
  - [x] Include suggestion: "Update scoring-config.json to use valid weights that sum to 1.0"
  - [x] Validate each individual weight is 0.0-1.0 range
  - [x] Validate no weight is negative

- [x] Add structured logging with named placeholders (AC: Log scoring calculations)
  - [x] Log Information: "Loading scoring weights from configuration" at startup
  - [x] Log Information: "Using scoring weights: Coupling={CouplingWeight}, Complexity={ComplexityWeight}, TechDebt={TechDebtWeight}, ExternalExposure={ExternalExposureWeight}" after loading
  - [x] Log Debug: "Calculating extraction score for project {ProjectName}" at start of each calculation
  - [x] Log Debug: "Project {ProjectName} individual scores: Coupling={CouplingScore}, Complexity={ComplexityScore}, TechDebt={TechDebtScore}, ApiExposure={ApiExposureScore}" for metrics
  - [x] Log Debug: "Project {ProjectName} final extraction score: {FinalScore} ({Category})" for results
  - [x] Log Information: "Calculated extraction scores for {ProjectCount} projects: {EasyCount} easy, {MediumCount} medium, {HardCount} hard" for batch results
  - [x] Use named placeholders, NOT string interpolation (critical project rule)

- [x] Create scoring-config.json template file (AC: Default configuration)
  - [x] Create example configuration file: src/MasDependencyMap.CLI/scoring-config.json
  - [x] Default weights: {"ScoringWeights": {"CouplingWeight": 0.40, "ComplexityWeight": 0.30, "TechDebtWeight": 0.20, "ExternalExposureWeight": 0.10}}
  - [x] Use PascalCase property names (matches C# POCO properties per project conventions)
  - [x] Add JSON comments (if supported) explaining each weight
  - [x] Configure as "Copy to Output Directory" = "Copy if newer" in .csproj
  - [x] Document in README or inline comments: how to customize weights for organization priorities

- [x] Register service in DI container (AC: Service integration)
  - [x] Add registration in CLI Program.cs DI configuration
  - [x] Use services.AddSingleton<IExtractionScoreCalculator, ExtractionScoreCalculator>() pattern
  - [x] Register in "Epic 4: Extraction Scoring Services" section (after IExternalApiDetector)
  - [x] Ensure all dependencies registered: ICouplingMetricCalculator, IComplexityMetricCalculator, ITechDebtAnalyzer, IExternalApiDetector, IConfiguration
  - [x] Follow existing DI registration patterns from Stories 4.1-4.4

- [x] Create comprehensive unit tests (AC: Test coverage)
  - [x] Create test class: tests/MasDependencyMap.Core.Tests/ExtractionScoring/ExtractionScoreCalculatorTests.cs
  - [x] Test: CalculateAsync_ProjectWithAllMetrics_CalculatesWeightedScore (uses default weights, validates weighted sum)
  - [x] Test: CalculateAsync_CustomWeights_UsesCustomWeights (loads custom weights from config, validates calculation)
  - [x] Test: CalculateForAllProjectsAsync_MultipleProjects_ReturnsScoresSortedByDifficulty (validates batch processing and sorting)
  - [x] Test: CalculateAsync_LowScoreProject_CategoryIsEasy (score 0-33 → Easy)
  - [x] Test: CalculateAsync_MediumScoreProject_CategoryIsMedium (score 34-66 → Medium)
  - [x] Test: CalculateAsync_HighScoreProject_CategoryIsHard (score 67-100 → Hard)
  - [x] Test: LoadWeights_InvalidSum_ThrowsConfigurationException (weights sum != 1.0)
  - [x] Test: LoadWeights_NegativeWeight_ThrowsConfigurationException (weight < 0)
  - [x] Test: LoadWeights_MissingConfig_UsesDefaultWeights (no config file → defaults)
  - [x] Test: CalculateAsync_ScoreClamping_ClampsTo0And100 (validates Math.Clamp behavior)
  - [x] Test: CalculateAsync_NullProject_ThrowsArgumentNullException (defensive programming)
  - [x] Test: CalculateAsync_CancellationRequested_ThrowsOperationCanceledException (cancellation support)
  - [x] Use xUnit, FluentAssertions, Moq for mocking metric calculators
  - [x] Test naming: {MethodName}_{Scenario}_{ExpectedResult} pattern
  - [x] Arrange-Act-Assert structure

- [x] Validate against project-context.md rules (AC: Architecture compliance)
  - [x] Feature-based namespace: MasDependencyMap.Core.ExtractionScoring (NOT layer-based)
  - [x] Async suffix on all async methods (CalculateAsync)
  - [x] File-scoped namespace declarations (all files)
  - [x] ILogger injection via constructor (NOT static logger)
  - [x] ConfigureAwait(false) in library code (Core layer)
  - [x] XML documentation on all public APIs (model, interface, implementation)
  - [x] Test files mirror Core namespace structure (tests/MasDependencyMap.Core.Tests/ExtractionScoring)
  - [x] Configuration uses IConfiguration injection (NOT direct JsonSerializer)
  - [x] JSON config uses PascalCase property names

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - Story 4.5 Extraction Score Calculator Requirements:**

This story implements the **ORCHESTRATION LAYER** that combines all four metrics from Stories 4.1-4.4 into a single unified extraction difficulty score.

**Epic 4 Vision (Recap):**
- Story 4.1: Coupling metrics ✅ DONE
- Story 4.2: Cyclomatic complexity metrics ✅ DONE
- Story 4.3: Technology version debt metrics ✅ DONE
- Story 4.4: External API exposure metrics ✅ DONE
- **Story 4.5: Combined extraction score calculator (THIS STORY - ORCHESTRATION)**
- Story 4.6: Ranked extraction candidate lists (consumes 4.5)
- Story 4.7: Heat map visualization with color-coded scores (consumes 4.5)
- Story 4.8: Display extraction scores as node labels (consumes 4.5)

**Story 4.5 Unique Challenges:**

1. **Multi-Calculator Orchestration:**
   - Story 4.1-4.4: Each implemented ONE metric calculator
   - Story 4.5: ORCHESTRATES all four calculators together
   - Must call ICouplingMetricCalculator, IComplexityMetricCalculator, ITechDebtAnalyzer, IExternalApiDetector
   - Dependency injection with FOUR dependencies (one for each metric)

2. **Coupling Metric Special Handling:**
   - Stories 4.2, 4.3, 4.4: Single project → single metric (independent analysis)
   - Story 4.1: Entire graph → all coupling metrics (relative, not absolute)
   - **Challenge:** Coupling requires ALL projects to calculate relative scores
   - **Solution:** CalculateForAllProjectsAsync is primary method, CalculateAsync(single) has limitations
   - Document limitation: Single project scoring may return coupling = 0 or require pre-calculated coupling metric

3. **Configurable Weights from JSON:**
   - Stories 4.1-4.4: Hardcoded scoring logic (thresholds, formulas)
   - Story 4.5: CONFIGURABLE weights loaded from scoring-config.json
   - Must use IConfiguration (Microsoft.Extensions.Configuration) per project conventions
   - Must validate weights sum to 1.0 (±0.01 tolerance for floating point arithmetic)
   - Fail fast with clear error if invalid weights (ConfigurationException)

4. **Default vs. Custom Weights:**
   - Default weights: Coupling 0.40 (highest), Complexity 0.30, TechDebt 0.20, ExternalExposure 0.10 (lowest)
   - Rationale: Coupling is strongest indicator of extraction difficulty (breaking dependencies)
   - Must support custom weights: Organizations may prioritize differently
   - Example: Legacy modernization project → TechDebt weight = 0.50 (prioritize old frameworks)

5. **Score Interpretation (Lower = Easier):**
   - 0-33: Easy extraction (green zone, start here)
   - 34-66: Medium extraction (yellow zone, moderate risk)
   - 67-100: Hard extraction (red zone, save for later or avoid)
   - **Consistent with Epic 4 vision:** All four metrics use "higher score = harder extraction" semantics

🚨 **CRITICAL - Weighted Score Calculation Formula:**

**Mathematical Formula:**

```
FinalScore = (CouplingScore × WeightC) + (ComplexityScore × WeightCx) + (TechDebtScore × WeightT) + (ApiExposureScore × WeightE)

Where:
  CouplingScore = NormalizedScore from CouplingMetric (0-100)
  ComplexityScore = NormalizedScore from ComplexityMetric (0-100)
  TechDebtScore = NormalizedScore from TechDebtMetric (0-100)
  ApiExposureScore = NormalizedScore from ExternalApiMetric (0-100)

  WeightC + WeightCx + WeightT + WeightE = 1.0 (validated at configuration load)

  FinalScore is clamped to [0, 100] range using Math.Clamp()
```

**Implementation:**

```csharp
public async Task<ExtractionScore> CalculateAsync(
    ProjectNode project,
    DependencyGraph graph, // For coupling calculation
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(project);

    _logger.LogDebug("Calculating extraction score for project {ProjectName}", project.ProjectName);

    // Load weights if not already loaded
    if (_weights == null)
    {
        _weights = LoadWeightsFromConfiguration();
    }

    // Get all four metrics
    var couplingMetric = await _couplingCalculator.CalculateAsync(graph, cancellationToken)
        .ConfigureAwait(false);
    var projectCoupling = couplingMetric.FirstOrDefault(m => m.ProjectName == project.ProjectName);

    var complexityMetric = await _complexityCalculator.CalculateAsync(project, cancellationToken)
        .ConfigureAwait(false);

    var techDebtMetric = await _techDebtAnalyzer.AnalyzeAsync(project, cancellationToken)
        .ConfigureAwait(false);

    var apiMetric = await _apiDetector.DetectAsync(project, cancellationToken)
        .ConfigureAwait(false);

    // Calculate weighted score
    var finalScore =
        (projectCoupling?.NormalizedScore ?? 0) * _weights.CouplingWeight +
        complexityMetric.NormalizedScore * _weights.ComplexityWeight +
        techDebtMetric.NormalizedScore * _weights.TechDebtWeight +
        apiMetric.NormalizedScore * _weights.ExternalExposureWeight;

    // Clamp to 0-100 range
    finalScore = Math.Clamp(finalScore, 0, 100);

    _logger.LogDebug(
        "Project {ProjectName} individual scores: Coupling={CouplingScore}, Complexity={ComplexityScore}, TechDebt={TechDebtScore}, ApiExposure={ApiExposureScore}",
        project.ProjectName,
        projectCoupling?.NormalizedScore ?? 0,
        complexityMetric.NormalizedScore,
        techDebtMetric.NormalizedScore,
        apiMetric.NormalizedScore);

    _logger.LogDebug(
        "Project {ProjectName} final extraction score: {FinalScore} ({Category})",
        project.ProjectName,
        finalScore,
        GetDifficultyCategory(finalScore));

    return new ExtractionScore(
        project.ProjectName,
        project.ProjectPath,
        finalScore,
        projectCoupling,
        complexityMetric,
        techDebtMetric,
        apiMetric);
}

private static string GetDifficultyCategory(double score)
{
    return score switch
    {
        <= 33 => "Easy",
        <= 66 => "Medium",
        _ => "Hard"
    };
}
```

**Why Weighted Sum (Not Other Aggregations)?**

- Simple, transparent, easy to explain to stakeholders
- Weights reflect organizational priorities (coupling vs complexity vs tech debt vs API exposure)
- Linear combination preserves metric contributions proportionally
- Alternative approaches (max, min, geometric mean) are less intuitive

🚨 **CRITICAL - Configuration Management:**

**From Project Context (lines 109-113):**
> Microsoft.Extensions.Configuration:
> - JSON files MUST use PascalCase property names (matches C# POCO properties)
> - Load configuration files from current directory by default
> - Use `IConfiguration` injection, NOT direct `JsonSerializer.Deserialize<T>()`

**scoring-config.json Structure:**

```json
{
  "ScoringWeights": {
    "CouplingWeight": 0.40,
    "ComplexityWeight": 0.30,
    "TechDebtWeight": 0.20,
    "ExternalExposureWeight": 0.10
  }
}
```

**Configuration Loading Pattern:**

```csharp
private ScoringWeights LoadWeightsFromConfiguration()
{
    _logger.LogInformation("Loading scoring weights from configuration");

    // Try to load from configuration
    var weights = _configuration.GetSection("ScoringWeights").Get<ScoringWeights>();

    // If config missing or section missing, use defaults
    if (weights == null)
    {
        _logger.LogInformation("Scoring configuration not found, using default weights");
        weights = new ScoringWeights(
            CouplingWeight: 0.40,
            ComplexityWeight: 0.30,
            TechDebtWeight: 0.20,
            ExternalExposureWeight: 0.10);
    }

    // Validate weights
    if (!weights.IsValid(out var errorMessage))
    {
        throw new ConfigurationException(
            $"Invalid scoring weights configuration. {errorMessage}. " +
            "Weights must sum to 1.0 and each weight must be between 0.0 and 1.0.");
    }

    _logger.LogInformation(
        "Using scoring weights: Coupling={CouplingWeight}, Complexity={ComplexityWeight}, TechDebt={TechDebtWeight}, ExternalExposure={ExternalExposureWeight}",
        weights.CouplingWeight,
        weights.ComplexityWeight,
        weights.TechDebtWeight,
        weights.ExternalExposureWeight);

    return weights;
}
```

**ScoringWeights Model with Validation:**

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Configurable weights for combining extraction difficulty metrics.
/// Weights must sum to 1.0 to produce a normalized 0-100 final score.
/// </summary>
/// <param name="CouplingWeight">Weight for coupling metric (default 0.40). Higher values prioritize dependency complexity.</param>
/// <param name="ComplexityWeight">Weight for cyclomatic complexity metric (default 0.30). Higher values prioritize code complexity.</param>
/// <param name="TechDebtWeight">Weight for technology version debt metric (default 0.20). Higher values prioritize framework modernization.</param>
/// <param name="ExternalExposureWeight">Weight for external API exposure metric (default 0.10). Higher values prioritize API contract risks.</param>
public sealed record ScoringWeights(
    double CouplingWeight,
    double ComplexityWeight,
    double TechDebtWeight,
    double ExternalExposureWeight)
{
    /// <summary>
    /// Validates that weights sum to 1.0 (±0.01 tolerance) and each weight is in valid range [0.0, 1.0].
    /// </summary>
    /// <param name="errorMessage">Detailed error message if validation fails.</param>
    /// <returns>True if weights are valid, false otherwise.</returns>
    public bool IsValid(out string errorMessage)
    {
        // Check individual weights are in valid range
        if (CouplingWeight < 0 || CouplingWeight > 1 ||
            ComplexityWeight < 0 || ComplexityWeight > 1 ||
            TechDebtWeight < 0 || TechDebtWeight > 1 ||
            ExternalExposureWeight < 0 || ExternalExposureWeight > 1)
        {
            errorMessage = $"All weights must be between 0.0 and 1.0. " +
                          $"Current: Coupling={CouplingWeight}, Complexity={ComplexityWeight}, " +
                          $"TechDebt={TechDebtWeight}, ExternalExposure={ExternalExposureWeight}";
            return false;
        }

        // Check weights sum to 1.0 (with tolerance for floating point arithmetic)
        var sum = CouplingWeight + ComplexityWeight + TechDebtWeight + ExternalExposureWeight;
        const double tolerance = 0.01;

        if (Math.Abs(sum - 1.0) > tolerance)
        {
            errorMessage = $"Weights must sum to 1.0 (±{tolerance} tolerance). " +
                          $"Current sum: {sum:F3}. " +
                          $"Weights: Coupling={CouplingWeight}, Complexity={ComplexityWeight}, " +
                          $"TechDebt={TechDebtWeight}, ExternalExposure={ExternalExposureWeight}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
```

**Why 0.01 Tolerance for Sum Validation?**

Floating point arithmetic can have precision errors:
```
0.40 + 0.30 + 0.20 + 0.10 = 1.0000000000000002 (in some representations)
```

A tolerance of 0.01 (1%) allows for rounding errors while catching actual configuration mistakes.

🚨 **CRITICAL - Coupling Metric Special Handling:**

**The Coupling Problem:**

Stories 4.2, 4.3, 4.4 analyze ONE project at a time:
```csharp
Task<ComplexityMetric> CalculateAsync(ProjectNode project, ...)
Task<TechDebtMetric> AnalyzeAsync(ProjectNode project, ...)
Task<ExternalApiMetric> DetectAsync(ProjectNode project, ...)
```

Story 4.1 analyzes ALL projects at once (coupling is RELATIVE):
```csharp
Task<IReadOnlyList<CouplingMetric>> CalculateAsync(DependencyGraph graph, ...)
```

**Why Coupling Is Different:**

Coupling score is RELATIVE to other projects in solution:
- Project A depends on 5 projects, BUT if solution only has 5 projects → 100% coupling
- Project B depends on 5 projects, BUT if solution has 50 projects → 10% coupling
- **Coupling normalization requires knowing max coupling in entire solution**

**Solution Approaches:**

**Approach 1: Batch Processing (Recommended)**

```csharp
public async Task<IReadOnlyList<ExtractionScore>> CalculateForAllProjectsAsync(
    DependencyGraph graph,
    CancellationToken cancellationToken = default)
{
    // Calculate coupling for ALL projects first (relative scoring)
    var couplingMetrics = await _couplingCalculator.CalculateAsync(graph, cancellationToken)
        .ConfigureAwait(false);

    // Build lookup dictionary
    var couplingLookup = couplingMetrics.ToDictionary(m => m.ProjectName);

    var scores = new List<ExtractionScore>();

    // For each project, calculate other three metrics + combine with coupling
    foreach (var project in graph.Vertices)
    {
        var complexityMetric = await _complexityCalculator.CalculateAsync(project, cancellationToken)
            .ConfigureAwait(false);

        var techDebtMetric = await _techDebtAnalyzer.AnalyzeAsync(project, cancellationToken)
            .ConfigureAwait(false);

        var apiMetric = await _apiDetector.DetectAsync(project, cancellationToken)
            .ConfigureAwait(false);

        var couplingMetric = couplingLookup[project.ProjectName];

        // Calculate weighted score
        var finalScore = CalculateWeightedScore(couplingMetric, complexityMetric, techDebtMetric, apiMetric);

        scores.Add(new ExtractionScore(
            project.ProjectName,
            project.ProjectPath,
            finalScore,
            couplingMetric,
            complexityMetric,
            techDebtMetric,
            apiMetric));
    }

    // Sort by final score ascending (easiest first)
    return scores.OrderBy(s => s.FinalScore).ToList();
}
```

**Approach 2: Single Project with Graph Context**

```csharp
public async Task<ExtractionScore> CalculateAsync(
    ProjectNode project,
    DependencyGraph graph, // REQUIRED for coupling calculation
    CancellationToken cancellationToken = default)
{
    // Calculate coupling for entire graph (but only use the one project's metric)
    var allCouplingMetrics = await _couplingCalculator.CalculateAsync(graph, cancellationToken)
        .ConfigureAwait(false);

    var couplingMetric = allCouplingMetrics.FirstOrDefault(m => m.ProjectName == project.ProjectName);

    // Rest of calculation...
}
```

**Approach 3: Accept Pre-Calculated Coupling (Alternative)**

```csharp
public ExtractionScore Calculate(
    ProjectNode project,
    CouplingMetric couplingMetric, // Pre-calculated
    ComplexityMetric complexityMetric,
    TechDebtMetric techDebtMetric,
    ExternalApiMetric apiMetric)
{
    // Synchronous method, all metrics pre-calculated
    var finalScore = CalculateWeightedScore(couplingMetric, complexityMetric, techDebtMetric, apiMetric);

    return new ExtractionScore(/* ... */);
}
```

**Recommendation:** Implement BOTH:
- `CalculateForAllProjectsAsync(DependencyGraph graph)` - PRIMARY METHOD for full solution analysis
- `CalculateAsync(ProjectNode project, DependencyGraph graph)` - CONVENIENCE METHOD for single project with graph context
- Document that coupling score may be 0 if graph not provided

🚨 **CRITICAL - ExtractionScore Model Design:**

**Model Requirements:**

1. Contains final weighted score (0-100)
2. Contains ALL four individual metric objects for detailed breakdown
3. Computed property for difficulty category (Easy/Medium/Hard)
4. Immutable record type (consistent with Stories 4.1-4.4)

**Implementation:**

```csharp
namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents the final extraction difficulty score for a project, combining all four metrics with configurable weights.
/// Lower scores (0-33) indicate easier extraction, higher scores (67-100) indicate harder extraction.
/// </summary>
/// <param name="ProjectName">Name of the project being scored.</param>
/// <param name="ProjectPath">Absolute path to the project file (.csproj or .vbproj).</param>
/// <param name="FinalScore">Final weighted extraction difficulty score (0-100). Calculated as weighted sum of all four metrics.</param>
/// <param name="CouplingMetric">Coupling metric details (incoming/outgoing references). Null if coupling analysis unavailable.</param>
/// <param name="ComplexityMetric">Cyclomatic complexity metric details (average complexity, method count).</param>
/// <param name="TechDebtMetric">Technology version debt metric details (target framework, debt score).</param>
/// <param name="ExternalApiMetric">External API exposure metric details (endpoint count, API types).</param>
public sealed record ExtractionScore(
    string ProjectName,
    string ProjectPath,
    double FinalScore,
    CouplingMetric? CouplingMetric,
    ComplexityMetric ComplexityMetric,
    TechDebtMetric TechDebtMetric,
    ExternalApiMetric ExternalApiMetric)
{
    /// <summary>
    /// Gets the difficulty category based on final score.
    /// Easy: 0-33, Medium: 34-66, Hard: 67-100.
    /// </summary>
    public string DifficultyCategory => FinalScore switch
    {
        <= 33 => "Easy",
        <= 66 => "Medium",
        _ => "Hard"
    };
}
```

**Why CouplingMetric Is Nullable?**

- Coupling requires entire dependency graph to calculate
- Single project analysis may not have coupling metric available
- `CouplingMetric?` allows ExtractionScore to represent "partial" scoring scenarios
- Weighted calculation handles null: `(couplingMetric?.NormalizedScore ?? 0) * weight`

**DifficultyCategory Property:**

- Computed property (not stored, calculated from FinalScore)
- Matches Epic 4 vision color-coding: Green (Easy), Yellow (Medium), Red (Hard)
- Used in reports, visualizations, logging

### Technical Requirements

**New Namespace: MasDependencyMap.Core.ExtractionScoring (Established in Stories 4.1-4.4):**

Epic 4 uses the `ExtractionScoring` namespace created in Story 4.1.

```
src/MasDependencyMap.Core/ExtractionScoring/
├── CouplingMetric.cs                    # Story 4.1
├── ICouplingMetricCalculator.cs         # Story 4.1
├── CouplingMetricCalculator.cs          # Story 4.1
├── ComplexityMetric.cs                  # Story 4.2
├── IComplexityMetricCalculator.cs       # Story 4.2
├── ComplexityMetricCalculator.cs        # Story 4.2
├── CyclomaticComplexityWalker.cs        # Story 4.2 (internal)
├── TechDebtMetric.cs                    # Story 4.3
├── ITechDebtAnalyzer.cs                 # Story 4.3
├── TechDebtAnalyzer.cs                  # Story 4.3
├── ExternalApiMetric.cs                 # Story 4.4
├── IExternalApiDetector.cs              # Story 4.4
├── ExternalApiDetector.cs               # Story 4.4
├── ScoringWeights.cs                    # Story 4.5 (THIS STORY)
├── ExtractionScore.cs                   # Story 4.5 (THIS STORY)
├── IExtractionScoreCalculator.cs        # Story 4.5 (THIS STORY)
└── ExtractionScoreCalculator.cs         # Story 4.5 (THIS STORY)
```

**Dependencies:**

Story 4.5 DEPENDS ON all previous Epic 4 stories:
- ICouplingMetricCalculator (Story 4.1)
- IComplexityMetricCalculator (Story 4.2)
- ITechDebtAnalyzer (Story 4.3)
- IExternalApiDetector (Story 4.4)

Constructor injection:
```csharp
public ExtractionScoreCalculator(
    ICouplingMetricCalculator couplingCalculator,
    IComplexityMetricCalculator complexityCalculator,
    ITechDebtAnalyzer techDebtAnalyzer,
    IExternalApiDetector apiDetector,
    IConfiguration configuration,
    ILogger<ExtractionScoreCalculator> logger)
{
    _couplingCalculator = couplingCalculator ?? throw new ArgumentNullException(nameof(couplingCalculator));
    _complexityCalculator = complexityCalculator ?? throw new ArgumentNullException(nameof(complexityCalculator));
    _techDebtAnalyzer = techDebtAnalyzer ?? throw new ArgumentNullException(nameof(techDebtAnalyzer));
    _apiDetector = apiDetector ?? throw new ArgumentNullException(nameof(apiDetector));
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### Architecture Compliance

**Dependency Injection Registration:**

```csharp
// In Program.cs DI configuration, Epic 4 section

// Epic 4: Extraction Scoring Services
services.AddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>();
services.AddSingleton<IComplexityMetricCalculator, ComplexityMetricCalculator>();
services.AddSingleton<ITechDebtAnalyzer, TechDebtAnalyzer>();
services.AddSingleton<IExternalApiDetector, ExternalApiDetector>();
services.AddSingleton<IExtractionScoreCalculator, ExtractionScoreCalculator>(); // NEW
```

**Lifetime:**
- Singleton: ExtractionScoreCalculator is stateless (orchestrates other calculators, no mutable state)
- Configuration weights are loaded once and cached in private field
- Consistent with Stories 4.1-4.4 (all Epic 4 services are singletons)

**Configuration Setup:**

```csharp
// In Program.cs, add JSON configuration for scoring-config.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("scoring-config.json", optional: true, reloadOnChange: false)
    .Build();

services.AddSingleton<IConfiguration>(configuration);
```

### Library/Framework Requirements

**Existing Libraries (Already Installed):**

All dependencies already installed in Stories 4.1-4.4:
- ✅ Microsoft.Extensions.DependencyInjection - DI container
- ✅ Microsoft.Extensions.Configuration.Json - JSON configuration management
- ✅ Microsoft.Extensions.Logging.Console - Structured logging
- ✅ QuikGraph v2.5.0 - Graph data structures (for coupling)
- ✅ Microsoft.CodeAnalysis.CSharp.Workspaces - Roslyn (for complexity, API exposure)

**No New NuGet Packages Required for Story 4.5** ✅

**Configuration API Usage:**

```csharp
using Microsoft.Extensions.Configuration;

// Load configuration section
var weights = _configuration.GetSection("ScoringWeights").Get<ScoringWeights>();

// Check if section exists
if (weights == null)
{
    // Use defaults
}
```

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/ExtractionScoring/
├── ScoringWeights.cs                             # NEW (configuration model with validation)
├── ExtractionScore.cs                            # NEW (final score model with all metrics)
├── IExtractionScoreCalculator.cs                 # NEW (orchestrator interface)
└── ExtractionScoreCalculator.cs                  # NEW (orchestrator implementation)

src/MasDependencyMap.CLI/
└── scoring-config.json                           # NEW (default weights configuration)

tests/MasDependencyMap.Core.Tests/ExtractionScoring/
└── ExtractionScoreCalculatorTests.cs             # NEW (comprehensive tests with mocking)
```

**Files to Modify:**

```
src/MasDependencyMap.CLI/Program.cs                           # MODIFY: Add DI registration, configuration setup
src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj          # MODIFY: Add scoring-config.json as content file
_bmad-output/implementation-artifacts/sprint-status.yaml      # MODIFY: Update story status
```

**No CLI Command Integration Yet:**

Story 4.5 creates the calculator but doesn't integrate it into CLI commands. CLI integration happens in later stories:
- Story 4.6: Ranked extraction candidate lists (adds to analyze command)
- Story 4.7: Heat map visualization (adds to visualization options)

For now:
- Create the service and register it in DI
- Tests will validate functionality
- CLI integration deferred to Stories 4.6-4.8

### Testing Requirements

**Test Class: ExtractionScoreCalculatorTests.cs**

**Test Strategy:**

Use unit testing with MOCKING (different from Stories 4.1-4.4):
- Stories 4.1-4.4: Integration tests with real test projects
- Story 4.5: UNIT tests with Moq to mock metric calculators
- Reason: Testing orchestration logic, not individual metric calculations (already tested)

**Mock Setup Pattern:**

```csharp
private readonly Mock<ICouplingMetricCalculator> _mockCouplingCalculator;
private readonly Mock<IComplexityMetricCalculator> _mockComplexityCalculator;
private readonly Mock<ITechDebtAnalyzer> _mockTechDebtAnalyzer;
private readonly Mock<IExternalApiDetector> _mockApiDetector;
private readonly Mock<IConfiguration> _mockConfiguration;
private readonly Mock<ILogger<ExtractionScoreCalculator>> _mockLogger;
private readonly ExtractionScoreCalculator _calculator;

public ExtractionScoreCalculatorTests()
{
    _mockCouplingCalculator = new Mock<ICouplingMetricCalculator>();
    _mockComplexityCalculator = new Mock<IComplexityMetricCalculator>();
    _mockTechDebtAnalyzer = new Mock<ITechDebtAnalyzer>();
    _mockApiDetector = new Mock<IExternalApiDetector>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockLogger = new Mock<ILogger<ExtractionScoreCalculator>>();

    // Setup default configuration to return null (use defaults)
    var mockSection = new Mock<IConfigurationSection>();
    mockSection.Setup(s => s.Get<ScoringWeights>()).Returns((ScoringWeights)null);
    _mockConfiguration.Setup(c => c.GetSection("ScoringWeights")).Returns(mockSection.Object);

    _calculator = new ExtractionScoreCalculator(
        _mockCouplingCalculator.Object,
        _mockComplexityCalculator.Object,
        _mockTechDebtAnalyzer.Object,
        _mockApiDetector.Object,
        _mockConfiguration.Object,
        _mockLogger.Object);
}
```

**Test Coverage Checklist:**

- ✅ Weighted score calculation with default weights (0.40, 0.30, 0.20, 0.10)
- ✅ Weighted score calculation with custom weights from configuration
- ✅ Batch processing with CalculateForAllProjectsAsync
- ✅ Results sorted by FinalScore ascending (easiest first)
- ✅ Difficulty category: Easy (0-33), Medium (34-66), Hard (67-100)
- ✅ Score clamping to 0-100 range
- ✅ Invalid weights sum throws ConfigurationException
- ✅ Negative weight throws ConfigurationException
- ✅ Missing configuration uses default weights
- ✅ Null project throws ArgumentNullException
- ✅ Cancellation support
- ✅ Null coupling metric handling (coupling unavailable scenario)

**Sample Test:**

```csharp
[Fact]
public async Task CalculateAsync_WithDefaultWeights_CalculatesCorrectWeightedScore()
{
    // Arrange
    var project = new ProjectNode("TestProject", "/path/to/project.csproj");
    var graph = new DependencyGraph(); // Mock graph

    var couplingMetric = new CouplingMetric("TestProject", "/path/to/project.csproj",
        IncomingReferences: 10, OutgoingReferences: 5, TotalCouplingScore: 25, NormalizedScore: 50);

    var complexityMetric = new ComplexityMetric("TestProject", "/path/to/project.csproj",
        TotalComplexity: 100, MethodCount: 20, AverageComplexity: 5, NormalizedScore: 60);

    var techDebtMetric = new TechDebtMetric("TestProject", "/path/to/project.csproj",
        TargetFramework: "net472", NormalizedScore: 40);

    var apiMetric = new ExternalApiMetric("TestProject", "/path/to/project.csproj",
        EndpointCount: 8, NormalizedScore: 66, ApiTypeBreakdown: new ApiTypeBreakdown(8, 0, 0));

    // Setup mocks to return these metrics
    _mockCouplingCalculator
        .Setup(c => c.CalculateAsync(It.IsAny<DependencyGraph>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<CouplingMetric> { couplingMetric });

    _mockComplexityCalculator
        .Setup(c => c.CalculateAsync(project, It.IsAny<CancellationToken>()))
        .ReturnsAsync(complexityMetric);

    _mockTechDebtAnalyzer
        .Setup(a => a.AnalyzeAsync(project, It.IsAny<CancellationToken>()))
        .ReturnsAsync(techDebtMetric);

    _mockApiDetector
        .Setup(d => d.DetectAsync(project, It.IsAny<CancellationToken>()))
        .ReturnsAsync(apiMetric);

    // Act
    var score = await _calculator.CalculateAsync(project, graph);

    // Assert
    // Expected: (50 * 0.40) + (60 * 0.30) + (40 * 0.20) + (66 * 0.10)
    //         = 20 + 18 + 8 + 6.6 = 52.6
    score.FinalScore.Should().BeApproximately(52.6, 0.1);
    score.DifficultyCategory.Should().Be("Medium");
    score.ProjectName.Should().Be("TestProject");
    score.CouplingMetric.Should().Be(couplingMetric);
    score.ComplexityMetric.Should().Be(complexityMetric);
    score.TechDebtMetric.Should().Be(techDebtMetric);
    score.ExternalApiMetric.Should().Be(apiMetric);
}
```

### Previous Story Intelligence

**From Story 4.4 (External API Exposure Detector) - Patterns to Reuse:**

1. **Record Model Pattern:**
   ```csharp
   // Stories 4.1-4.4 used record models for metrics
   // Story 4.5 uses same pattern for ScoringWeights and ExtractionScore
   public sealed record ScoringWeights(...)
   public sealed record ExtractionScore(...)
   ```

2. **Service Pattern with Multiple Dependencies:**
   ```csharp
   // Stories 4.1-4.4: Each had 1-2 dependencies (ILogger, IConfiguration)
   // Story 4.5: Has 6 dependencies (4 metric calculators + IConfiguration + ILogger)
   // Still use constructor injection, validate all parameters
   ```

3. **DI Registration Pattern:**
   ```csharp
   // From Stories 4.1-4.4
   services.AddSingleton<ICouplingMetricCalculator, CouplingMetricCalculator>();
   services.AddSingleton<IComplexityMetricCalculator, ComplexityMetricCalculator>();
   services.AddSingleton<ITechDebtAnalyzer, TechDebtAnalyzer>();
   services.AddSingleton<IExternalApiDetector, ExternalApiDetector>();
   // Story 4.5
   services.AddSingleton<IExtractionScoreCalculator, ExtractionScoreCalculator>();
   ```

4. **Test Strategy Change:**
   ```csharp
   // Stories 4.1-4.4: Integration tests with real projects
   // Story 4.5: Unit tests with mocking (testing orchestration, not calculations)
   // Use Moq for mocking interfaces
   ```

**From Story 4.3 (Technology Version Debt Analyzer) - Configuration Pattern:**

Story 4.3 didn't use configuration, but project context defines the pattern:

```csharp
// Configuration loading from project context (lines 109-113)
// Story 4.5 applies this pattern for scoring-config.json

var configuration = _configuration.GetSection("ScoringWeights").Get<ScoringWeights>();

if (configuration == null)
{
    // Use defaults
}
```

**From Story 4.1 (Coupling Metric Calculator) - Batch Processing Pattern:**

Story 4.1 processes entire graph at once:
```csharp
Task<IReadOnlyList<CouplingMetric>> CalculateAsync(DependencyGraph graph)
```

Story 4.5 reuses this pattern for CalculateForAllProjectsAsync:
```csharp
Task<IReadOnlyList<ExtractionScore>> CalculateForAllProjectsAsync(DependencyGraph graph)
```

**Key Differences from Previous Stories:**

| Aspect | Stories 4.1-4.4 | Story 4.5 |
|--------|-----------------|-----------|
| Dependencies | 1-2 (ILogger, IConfiguration) | **6 (4 calculators + IConfiguration + ILogger)** |
| Analysis Pattern | Single metric calculation | **Orchestration of 4 metric calculators** |
| Configuration | Hardcoded logic | **Configurable weights from JSON** |
| Test Strategy | Integration tests | **Unit tests with mocking** |
| Return Type | Single metric (CouplingMetric, etc.) | **Composite model (ExtractionScore with all 4 metrics)** |
| Batch Processing | Only Story 4.1 | **Story 4.5 primary method** |

### Git Intelligence Summary

**Recent Commits Pattern:**

Last 5 commits show consistent code review process:
1. `4631a61` Code review fixes for Story 4-3: Implement technology version debt analyzer
2. `5911da1` Code review fixes for Story 4-2: Implement cyclomatic complexity calculator with Roslyn
3. `cc24f3c` Code review fixes for Story 4-1: Implement coupling metric calculator
4. `78f33d1` Code review fixes for Story 3-7: Mark suggested break points in YELLOW on visualizations
5. `d8166d9` Story 3-7 complete: Mark suggested break points in YELLOW on visualizations

**Pattern:** Initial commit → Code review → Fixes commit → Status update commit

**Expected Commit Sequence for Story 4.5:**

1. Initial commit: "Story 4-5 complete: Implement extraction score calculator with configurable weights"
2. Code review identifies 5-10 issues (based on Epic 4 pattern)
3. Fixes commit: "Code review fixes for Story 4-5: Implement extraction score calculator with configurable weights"
4. Status update: Update sprint-status.yaml from in-progress → review → done

**Expected File Changes for Story 4.5:**

Based on Epic 4 pattern:
- New: `src/MasDependencyMap.Core/ExtractionScoring/ScoringWeights.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/ExtractionScore.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/IExtractionScoreCalculator.cs`
- New: `src/MasDependencyMap.Core/ExtractionScoring/ExtractionScoreCalculator.cs`
- New: `src/MasDependencyMap.CLI/scoring-config.json`
- New: `tests/MasDependencyMap.Core.Tests/ExtractionScoring/ExtractionScoreCalculatorTests.cs`
- Modified: `src/MasDependencyMap.CLI/Program.cs` (DI registration, configuration setup)
- Modified: `src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj` (add scoring-config.json content file)
- Modified: `_bmad-output/implementation-artifacts/sprint-status.yaml` (story status update)
- Modified: `_bmad-output/implementation-artifacts/4-5-implement-extraction-score-calculator-with-configurable-weights.md` (completion notes)

### Project Structure Notes

**Alignment with Unified Project Structure:**

Story 4.5 completes Epic 4's CORE SCORING ENGINE, the foundation for all remaining Epic 4 stories:

```
src/MasDependencyMap.Core/ExtractionScoring/
├── CouplingMetric.cs                    # Story 4.1
├── ICouplingMetricCalculator.cs         # Story 4.1
├── CouplingMetricCalculator.cs          # Story 4.1
├── ComplexityMetric.cs                  # Story 4.2
├── IComplexityMetricCalculator.cs       # Story 4.2
├── ComplexityMetricCalculator.cs        # Story 4.2
├── CyclomaticComplexityWalker.cs        # Story 4.2
├── TechDebtMetric.cs                    # Story 4.3
├── ITechDebtAnalyzer.cs                 # Story 4.3
├── TechDebtAnalyzer.cs                  # Story 4.3
├── ExternalApiMetric.cs                 # Story 4.4
├── IExternalApiDetector.cs              # Story 4.4
├── ExternalApiDetector.cs               # Story 4.4
├── ScoringWeights.cs                    # Story 4.5 (NEW - CONFIG)
├── ExtractionScore.cs                   # Story 4.5 (NEW - FINAL MODEL)
├── IExtractionScoreCalculator.cs        # Story 4.5 (NEW - ORCHESTRATOR)
└── ExtractionScoreCalculator.cs         # Story 4.5 (NEW - ORCHESTRATOR)
```

**Epic 4 Core Scoring Engine Complete:**

After Story 4.5, all scoring infrastructure is complete:
1. ✅ Story 4.1: Coupling metrics (graph-based, relative)
2. ✅ Story 4.2: Complexity metrics (Roslyn-based, absolute thresholds)
3. ✅ Story 4.3: Tech debt metrics (XML-based, timeline scoring)
4. ✅ Story 4.4: API exposure metrics (Roslyn-based, stepped scoring)
5. ✅ Story 4.5: Extraction score calculator (orchestration, configurable weights)

**Next Stories (4.6-4.8) CONSUME Story 4.5:**
- Story 4.6: Ranked extraction candidate lists (uses IExtractionScoreCalculator)
- Story 4.7: Heat map visualization (uses ExtractionScore.FinalScore for coloring)
- Story 4.8: Node labels (uses ExtractionScore.FinalScore for labels)

**Dependency Flow:**

```
Stories 4.1-4.4 (Metric Calculators)
        ↓
Story 4.5 (Orchestrator - THIS STORY)
        ↓
Stories 4.6-4.8 (Consumers - Reports & Visualization)
```

Story 4.5 is the PIVOT POINT between metric calculation (4.1-4.4) and consumption (4.6-4.8).

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-4-extraction-difficulty-scoring-and-candidate-ranking.md, Story 4.5 (lines 81-101)]
- Story requirements: Combine all four metrics using configurable weights, validate weights sum to 1.0, final score 0-100

**Project Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 18-51)]
- Technology stack: .NET 8.0, C# 12, Microsoft.Extensions.Configuration.Json already installed
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 54-85)]
- Language rules: Feature-based namespaces, async patterns, file-scoped namespaces, record types
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 101-113)]
- Configuration: JSON files use PascalCase, load from current directory, IConfiguration injection
- [Source: D:\work\masDependencyMap\_bmad-output\project-context.md (lines 114-119)]
- Logging: Structured logging with named placeholders, ILogger<T> injection

**Previous Story Intelligence:**
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-1-implement-coupling-metric-calculator.md]
- Batch processing pattern: CalculateAsync(DependencyGraph) returns list of all metrics
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-2-implement-cyclomatic-complexity-calculator-with-roslyn.md]
- Single project pattern: CalculateAsync(ProjectNode) returns single metric
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-3-implement-technology-version-debt-analyzer.md]
- Record model pattern, analyzer interface/implementation, DI registration
- [Source: D:\work\masDependencyMap\_bmad-output\implementation-artifacts\4-4-implement-external-api-exposure-detector.md]
- Record model with nested record (ApiTypeBreakdown), semantic analysis patterns, test strategy

**Git Intelligence:**
- [Source: git log (last 5 commits)]
- Code review pattern: Initial commit → Code review fixes (5-10 issues) → Status update
- Epic 4 stories follow consistent pattern: record models, singleton services, comprehensive tests

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-5-20250929

### Debug Log References

N/A - No debug logging required

### Completion Notes List

**Implementation Summary:**

Story 4.5 successfully implemented the extraction score calculator that orchestrates all four metrics from Stories 4.1-4.4 into a unified 0-100 extraction difficulty score with configurable weights.

**Files Created:**
1. `src/MasDependencyMap.Core/ExtractionScoring/ScoringWeights.cs` - Configuration model with validation for configurable weights
2. `src/MasDependencyMap.Core/ExtractionScoring/ExtractionScore.cs` - Final score model containing all four metrics and computed difficulty category
3. `src/MasDependencyMap.Core/ExtractionScoring/IExtractionScoreCalculator.cs` - Service interface for orchestration
4. `src/MasDependencyMap.Core/ExtractionScoring/ExtractionScoreCalculator.cs` - Implementation that combines all four metric calculators
5. `src/MasDependencyMap.Core/ExtractionScoring/ConfigurationException.cs` - Custom exception for weight validation failures
6. `src/MasDependencyMap.CLI/scoring-config.json` - Default configuration with recommended weights (0.40, 0.30, 0.20, 0.10)
7. `tests/MasDependencyMap.Core.Tests/ExtractionScoring/ExtractionScoreCalculatorTests.cs` - Comprehensive unit tests (18 tests, all passing)

**Files Modified:**
1. `src/MasDependencyMap.Core/MasDependencyMap.Core.csproj` - Added Microsoft.Extensions.Configuration.Abstractions and Microsoft.Extensions.Configuration.Binder packages
2. `src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj` - Added scoring-config.json as content file (CopyToOutputDirectory: PreserveNewest)
3. `src/MasDependencyMap.CLI/Program.cs` - Registered IExtractionScoreCalculator service in DI container
4. `_bmad-output/implementation-artifacts/sprint-status.yaml` - Updated story status to completed

**Key Implementation Details:**

1. **Weighted Score Formula**: FinalScore = (Coupling × 0.40) + (Complexity × 0.30) + (TechDebt × 0.20) + (ApiExposure × 0.10)
2. **Configuration Validation**: Weights must sum to 1.0 (±0.01 tolerance) and each weight must be in [0.0, 1.0] range
3. **Coupling Special Handling**: Coupling metric requires entire graph context; implemented both single-project and batch processing methods
4. **Difficulty Categories**: Easy (0-33), Medium (34-66), Hard (67-100) computed automatically from final score
5. **Structured Logging**: All logging uses named placeholders per project conventions
6. **Default Weights**: Coupling prioritized (0.40) as strongest indicator of extraction difficulty

**Test Coverage:**
- 18 comprehensive unit tests covering all scenarios
- All tests passing
- Test strategy: Unit testing with Moq for mocking metric calculators
- Coverage includes: weighted score calculation, custom weights, batch processing, difficulty categories, validation, error handling, cancellation support

**Architecture Compliance:**
- ✅ Feature-based namespace: MasDependencyMap.Core.ExtractionScoring
- ✅ File-scoped namespace declarations
- ✅ Async suffix on all async methods
- ✅ ConfigureAwait(false) in library code
- ✅ IConfiguration injection (not direct JSON deserialization)
- ✅ Structured logging with ILogger<T>
- ✅ XML documentation on all public APIs
- ✅ Record types for immutability
- ✅ Test files mirror Core namespace structure

**Code Review Fixes Applied (2026-01-26):**

Fixed 10 issues identified during adversarial code review:

1. **CRITICAL - Task Tracking:** Marked all 13 tasks as completed [x] in story file
2. **HIGH - Git Documentation:** Verified modified files (Program.cs, .csproj files) have correct content; changes were from previous stories
3. **MEDIUM-HIGH - Null Safety:** Removed null-forgiving operator `!` on `_weights` field, added explicit null check with InvalidOperationException
4. **MEDIUM - Test Validation:** Added exception message assertions to `LoadWeights_InvalidSum_ThrowsConfigurationException` and `LoadWeights_NegativeWeight_ThrowsConfigurationException` tests
5. **MEDIUM - Test Coverage:** Added `LoadWeights_ToleranceBoundary_ValidatesCorrectly` parameterized test covering boundary cases (0.99, 1.01, 0.98, 1.02, 1.00)
6. **MEDIUM - Logging:** Improved logging to indicate configuration source (defaults vs scoring-config.json)
7. **MEDIUM - Documentation:** Enhanced ExtractionScore XML doc to explain CouplingMetric null semantics (null = coupling contribution is 0)
8. **LOW - Code Quality:** Sealed ConfigurationException class for consistency with other exception patterns
9. **LOW - Documentation:** Added XML documentation to private `GetDifficultyCategory` method explaining thresholds
10. **NOTE - Integration Tests:** Acknowledged unit tests use mocking; integration tests with real calculators deferred to future work

All fixes maintain backward compatibility and improve code quality, test coverage, and documentation clarity.

**Epic 4 Progress:**
- ✅ Story 4.1: Coupling metrics
- ✅ Story 4.2: Cyclomatic complexity metrics
- ✅ Story 4.3: Technology version debt metrics
- ✅ Story 4.4: External API exposure metrics
- ✅ Story 4.5: Extraction score calculator (THIS STORY - ORCHESTRATION COMPLETE)
- ⏳ Story 4.6: Ranked extraction candidate lists (next)
- ⏳ Story 4.7: Heat map visualization
- ⏳ Story 4.8: Node labels

### File List

**New Files:**
- src/MasDependencyMap.Core/ExtractionScoring/ScoringWeights.cs
- src/MasDependencyMap.Core/ExtractionScoring/ExtractionScore.cs
- src/MasDependencyMap.Core/ExtractionScoring/IExtractionScoreCalculator.cs
- src/MasDependencyMap.Core/ExtractionScoring/ExtractionScoreCalculator.cs
- src/MasDependencyMap.Core/ExtractionScoring/ConfigurationException.cs
- src/MasDependencyMap.CLI/scoring-config.json
- tests/MasDependencyMap.Core.Tests/ExtractionScoring/ExtractionScoreCalculatorTests.cs

**Modified Files:**
- src/MasDependencyMap.Core/MasDependencyMap.Core.csproj
- src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj
- src/MasDependencyMap.CLI/Program.cs
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/4-5-implement-extraction-score-calculator-with-configurable-weights.md
