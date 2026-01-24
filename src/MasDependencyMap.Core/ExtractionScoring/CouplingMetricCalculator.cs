namespace MasDependencyMap.Core.ExtractionScoring;

using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Calculates coupling metrics for projects in a dependency graph.
/// Uses QuikGraph APIs to count incoming and outgoing edges, applies weighting formula,
/// and normalizes scores to 0-100 scale for extraction difficulty comparison.
/// </summary>
public sealed class CouplingMetricCalculator : ICouplingMetricCalculator
{
    private const double NormalizedScoreScale = 100.0;
    private readonly ILogger<CouplingMetricCalculator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CouplingMetricCalculator"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging of calculation progress.</param>
    public CouplingMetricCalculator(ILogger<CouplingMetricCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CouplingMetric>> CalculateAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var projectCount = graph.VertexCount;
        _logger.LogInformation("Calculating coupling metrics for {ProjectCount} projects", projectCount);

        if (projectCount == 0)
        {
            _logger.LogInformation("Graph contains no projects, returning empty metrics list");
            return Task.FromResult<IReadOnlyList<CouplingMetric>>(Array.Empty<CouplingMetric>());
        }

        // First pass: Calculate raw coupling scores and find maximum
        var rawMetrics = new List<(string ProjectName, int IncomingCount, int OutgoingCount, int TotalScore)>(projectCount);
        var maxTotalScore = 0;

        foreach (var project in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Count incoming edges (projects that depend on this one)
            var incomingCount = graph.GetInEdges(project).Count();

            // Count outgoing edges (projects this one depends on)
            var outgoingCount = graph.GetOutEdges(project).Count();

            // Calculate weighted total score: incoming * 2 + outgoing
            // Incoming weighted 2x because consumer dependencies make extraction harder
            var totalScore = (incomingCount * 2) + outgoingCount;

            rawMetrics.Add((project.ProjectName, incomingCount, outgoingCount, totalScore));

            // Track maximum score for normalization
            if (totalScore > maxTotalScore)
            {
                maxTotalScore = totalScore;
            }
        }

        // Second pass: Create final metrics with normalized scores
        var metrics = new List<CouplingMetric>(projectCount);

        foreach (var (projectName, incomingCount, outgoingCount, totalScore) in rawMetrics)
        {
            // Linear normalization: map [0, maxTotalScore] to [0, 100]
            var normalizedScore = maxTotalScore == 0
                ? 0.0
                : Math.Clamp((totalScore / (double)maxTotalScore) * NormalizedScoreScale, 0, NormalizedScoreScale);

            _logger.LogDebug(
                "Project {ProjectName}: Incoming={IncomingCount}, Outgoing={OutgoingCount}, Total={TotalScore}, Normalized={NormalizedScore:F2}",
                projectName,
                incomingCount,
                outgoingCount,
                totalScore,
                normalizedScore);

            metrics.Add(new CouplingMetric(
                projectName,
                incomingCount,
                outgoingCount,
                totalScore,
                normalizedScore));
        }

        _logger.LogInformation(
            "Coupling calculation complete: Max total score={MaxScore}",
            maxTotalScore);

        return Task.FromResult<IReadOnlyList<CouplingMetric>>(metrics.AsReadOnly());
    }
}
