namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;
using Microsoft.Extensions.Logging;

/// <summary>
/// Generates ranked lists of extraction candidates sorted by difficulty score.
/// Thin orchestration layer over IExtractionScoreCalculator that performs sorting, filtering, and statistics.
/// </summary>
public class RankedCandidateGenerator : IRankedCandidateGenerator
{
    private readonly IExtractionScoreCalculator _scoreCalculator;
    private readonly ILogger<RankedCandidateGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RankedCandidateGenerator"/> class.
    /// </summary>
    /// <param name="scoreCalculator">The extraction score calculator to use for scoring projects.</param>
    /// <param name="logger">The logger for structured logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when scoreCalculator or logger is null.</exception>
    public RankedCandidateGenerator(
        IExtractionScoreCalculator scoreCalculator,
        ILogger<RankedCandidateGenerator> logger)
    {
        _scoreCalculator = scoreCalculator ?? throw new ArgumentNullException(nameof(scoreCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RankedExtractionCandidates> GenerateRankedListAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);

        _logger.LogInformation(
            "Calculating extraction scores for {ProjectCount} projects",
            graph.VertexCount);

        // Get all scores (already sorted ascending by ExtractionScoreCalculator)
        var allScores = await _scoreCalculator.CalculateForAllProjectsAsync(graph, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Extraction scores calculated, sorting and categorizing projects");

        // Categorize by difficulty (keep as IEnumerable to avoid premature materialization)
        var easyCandidates = allScores.Where(s => s.FinalScore <= 33);
        var mediumCandidates = allScores.Where(s => s.FinalScore > 33 && s.FinalScore < 67);
        var hardCandidates = allScores.Where(s => s.FinalScore >= 67);

        // Materialize only what we need
        var easyCandidatesList = easyCandidates.ToList();
        var mediumCandidatesList = mediumCandidates.ToList();
        var hardCandidatesList = hardCandidates.ToList();

        // Take top 10 easiest (already sorted ascending, so take first 10)
        var top10Easiest = easyCandidatesList.Count <= 10
            ? easyCandidatesList
            : easyCandidatesList.GetRange(0, 10);

        // Take bottom 10 hardest (need to reverse sort for hardest first, then take 10)
        var bottom10Hardest = hardCandidatesList
            .OrderByDescending(s => s.FinalScore)
            .Take(10)
            .ToList();

        _logger.LogDebug("Identified {EasyCount} easy candidates (scores 0-33)", easyCandidatesList.Count);
        _logger.LogDebug("Identified {MediumCount} medium candidates (scores 34-66)", mediumCandidatesList.Count);
        _logger.LogDebug("Identified {HardCount} hard candidates (scores 67-100)", hardCandidatesList.Count);

        // Only compute expensive string operations when Debug logging is enabled
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Top 10 easiest candidates: {CandidateNames}",
                string.Join(", ", top10Easiest.Select(s => s.ProjectName)));

            _logger.LogDebug(
                "Bottom 10 hardest candidates: {CandidateNames}",
                string.Join(", ", bottom10Hardest.Select(s => s.ProjectName)));
        }

        var statistics = new ExtractionStatistics(
            TotalProjects: allScores.Count,
            EasyCount: easyCandidatesList.Count,
            MediumCount: mediumCandidatesList.Count,
            HardCount: hardCandidatesList.Count);

        if (!statistics.IsValid)
        {
            _logger.LogWarning(
                "Statistics validation failed: {EasyCount} + {MediumCount} + {HardCount} != {TotalProjects}",
                statistics.EasyCount,
                statistics.MediumCount,
                statistics.HardCount,
                statistics.TotalProjects);
        }

        _logger.LogInformation(
            "Generated ranked extraction candidates: {TotalProjects} total projects, {EasyCount} easy (0-33), {MediumCount} medium (34-66), {HardCount} hard (67-100)",
            statistics.TotalProjects,
            statistics.EasyCount,
            statistics.MediumCount,
            statistics.HardCount);

        return new RankedExtractionCandidates(
            AllProjects: allScores,
            EasiestCandidates: top10Easiest,
            HardestCandidates: bottom10Hardest,
            Statistics: statistics);
    }
}
