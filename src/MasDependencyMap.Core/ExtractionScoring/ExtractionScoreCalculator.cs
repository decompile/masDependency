namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Calculates final extraction difficulty scores by combining all four metrics with configurable weights.
/// Orchestrates coupling, complexity, tech debt, and API exposure calculators.
/// Weights are loaded from scoring-config.json or use defaults (0.40, 0.30, 0.20, 0.10).
/// </summary>
public sealed class ExtractionScoreCalculator : IExtractionScoreCalculator
{
    private readonly ICouplingMetricCalculator _couplingCalculator;
    private readonly IComplexityMetricCalculator _complexityCalculator;
    private readonly ITechDebtAnalyzer _techDebtAnalyzer;
    private readonly IExternalApiDetector _apiDetector;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExtractionScoreCalculator> _logger;
    private ScoringWeights? _weights;

    /// <summary>
    /// Initializes a new instance of the ExtractionScoreCalculator class.
    /// </summary>
    /// <param name="couplingCalculator">Calculator for coupling metrics. Must not be null.</param>
    /// <param name="complexityCalculator">Calculator for complexity metrics. Must not be null.</param>
    /// <param name="techDebtAnalyzer">Analyzer for tech debt metrics. Must not be null.</param>
    /// <param name="apiDetector">Detector for API exposure metrics. Must not be null.</param>
    /// <param name="configuration">Configuration provider for loading scoring weights. Must not be null.</param>
    /// <param name="logger">Logger for structured logging. Must not be null.</param>
    /// <exception cref="ArgumentNullException">When any parameter is null.</exception>
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

    /// <inheritdoc />
    public async Task<ExtractionScore> CalculateAsync(
        ProjectNode project,
        DependencyGraph? graph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        _logger.LogDebug("Calculating extraction score for project {ProjectName}", project.ProjectName);

        // Load weights if not already loaded
        _weights ??= LoadWeightsFromConfiguration();

        // Get coupling metric (requires graph context)
        CouplingMetric? couplingMetric = null;
        if (graph != null)
        {
            var couplingMetrics = await _couplingCalculator.CalculateAsync(graph, cancellationToken)
                .ConfigureAwait(false);
            couplingMetric = couplingMetrics.FirstOrDefault(m => m.ProjectName == project.ProjectName);
        }

        // Get other three metrics (project-specific)
        var complexityMetric = await _complexityCalculator.CalculateAsync(project, cancellationToken)
            .ConfigureAwait(false);

        var techDebtMetric = await _techDebtAnalyzer.AnalyzeAsync(project, cancellationToken)
            .ConfigureAwait(false);

        var apiMetric = await _apiDetector.DetectAsync(project, cancellationToken)
            .ConfigureAwait(false);

        // Calculate weighted score
        var finalScore = CalculateWeightedScore(couplingMetric, complexityMetric, techDebtMetric, apiMetric);

        _logger.LogDebug(
            "Project {ProjectName} individual scores: Coupling={CouplingScore}, Complexity={ComplexityScore}, TechDebt={TechDebtScore}, ApiExposure={ApiExposureScore}",
            project.ProjectName,
            couplingMetric?.NormalizedScore ?? 0,
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
            couplingMetric,
            complexityMetric,
            techDebtMetric,
            apiMetric);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExtractionScore>> CalculateForAllProjectsAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);

        _logger.LogDebug("Calculating extraction scores for all projects in graph");

        // Load weights if not already loaded
        _weights ??= LoadWeightsFromConfiguration();

        // Calculate coupling for ALL projects first (relative scoring requires full graph)
        var couplingMetrics = await _couplingCalculator.CalculateAsync(graph, cancellationToken)
            .ConfigureAwait(false);

        // Build lookup dictionary for fast coupling metric retrieval
        var couplingLookup = couplingMetrics.ToDictionary(m => m.ProjectName);

        var scores = new List<ExtractionScore>();

        // For each project, calculate other three metrics + combine with coupling
        foreach (var project in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var complexityMetric = await _complexityCalculator.CalculateAsync(project, cancellationToken)
                .ConfigureAwait(false);

            var techDebtMetric = await _techDebtAnalyzer.AnalyzeAsync(project, cancellationToken)
                .ConfigureAwait(false);

            var apiMetric = await _apiDetector.DetectAsync(project, cancellationToken)
                .ConfigureAwait(false);

            var couplingMetric = couplingLookup.GetValueOrDefault(project.ProjectName);

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
        var sortedScores = scores.OrderBy(s => s.FinalScore).ToList();

        // Count difficulty categories for summary logging
        var easyCount = sortedScores.Count(s => s.DifficultyCategory == "Easy");
        var mediumCount = sortedScores.Count(s => s.DifficultyCategory == "Medium");
        var hardCount = sortedScores.Count(s => s.DifficultyCategory == "Hard");

        _logger.LogInformation(
            "Calculated extraction scores for {ProjectCount} projects: {EasyCount} easy, {MediumCount} medium, {HardCount} hard",
            sortedScores.Count,
            easyCount,
            mediumCount,
            hardCount);

        return sortedScores;
    }

    private ScoringWeights LoadWeightsFromConfiguration()
    {
        _logger.LogInformation("Loading scoring weights from configuration section 'ScoringWeights'");

        // Try to load from configuration
        var weights = _configuration.GetSection("ScoringWeights").Get<ScoringWeights>();
        var isUsingDefaults = false;

        // If config missing or section missing, use defaults
        if (weights == null)
        {
            _logger.LogInformation("Configuration section 'ScoringWeights' not found in scoring-config.json, using default weights (0.40, 0.30, 0.20, 0.10)");
            weights = new ScoringWeights(
                CouplingWeight: 0.40,
                ComplexityWeight: 0.30,
                TechDebtWeight: 0.20,
                ExternalExposureWeight: 0.10);
            isUsingDefaults = true;
        }

        // Validate weights
        if (!weights.IsValid(out var errorMessage))
        {
            throw new ConfigurationException(
                $"Invalid scoring weights configuration. {errorMessage} " +
                "Update scoring-config.json to use valid weights that sum to 1.0.");
        }

        _logger.LogInformation(
            "Using scoring weights from {Source}: Coupling={CouplingWeight}, Complexity={ComplexityWeight}, TechDebt={TechDebtWeight}, ExternalExposure={ExternalExposureWeight}",
            isUsingDefaults ? "defaults" : "scoring-config.json",
            weights.CouplingWeight,
            weights.ComplexityWeight,
            weights.TechDebtWeight,
            weights.ExternalExposureWeight);

        return weights;
    }

    private double CalculateWeightedScore(
        CouplingMetric? couplingMetric,
        ComplexityMetric complexityMetric,
        TechDebtMetric techDebtMetric,
        ExternalApiMetric apiMetric)
    {
        // Weights must be loaded before calling this method
        if (_weights is null)
        {
            throw new InvalidOperationException("Scoring weights have not been loaded. Call LoadWeightsFromConfiguration first.");
        }

        var finalScore =
            (couplingMetric?.NormalizedScore ?? 0) * _weights.CouplingWeight +
            complexityMetric.NormalizedScore * _weights.ComplexityWeight +
            techDebtMetric.NormalizedScore * _weights.TechDebtWeight +
            apiMetric.NormalizedScore * _weights.ExternalExposureWeight;

        // Clamp to 0-100 range
        return Math.Clamp(finalScore, 0, 100);
    }

    /// <summary>
    /// Gets the difficulty category for a given extraction score.
    /// Categorizes scores into three ranges: Easy (0-33), Medium (34-66), or Hard (67-100).
    /// </summary>
    /// <param name="score">The extraction score to categorize (0-100 range).</param>
    /// <returns>Category string: "Easy", "Medium", or "Hard".</returns>
    private static string GetDifficultyCategory(double score)
    {
        return score switch
        {
            <= 33 => "Easy",
            <= 66 => "Medium",
            _ => "Hard"
        };
    }
}
