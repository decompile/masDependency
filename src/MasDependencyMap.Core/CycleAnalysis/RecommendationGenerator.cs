namespace MasDependencyMap.Core.CycleAnalysis;

using MasDependencyMap.Core.DependencyAnalysis;
using Microsoft.Extensions.Logging;

/// <summary>
/// Generates ranked cycle-breaking recommendations from cycles with identified weak edges.
/// </summary>
public sealed class RecommendationGenerator : IRecommendationGenerator
{
    private readonly ILogger<RecommendationGenerator> _logger;

    public RecommendationGenerator(ILogger<RecommendationGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates ranked cycle-breaking recommendations from cycles with weak edges.
    /// </summary>
    public Task<IReadOnlyList<CycleBreakingSuggestion>> GenerateRecommendationsAsync(
        IReadOnlyList<CycleInfo> cycles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cycles);

        _logger.LogDebug(
            "Generating cycle-breaking recommendations from {CycleCount} cycles",
            cycles.Count);

        var recommendations = new List<CycleBreakingSuggestion>();

        foreach (var cycle in cycles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (cycle.WeakCouplingEdges == null || cycle.WeakCouplingEdges.Count == 0)
            {
                _logger.LogDebug(
                    "Cycle {CycleId}: No weak edges identified, skipping",
                    cycle.CycleId);
                continue;
            }

            _logger.LogDebug(
                "Cycle {CycleId}: {WeakEdgeCount} weak edges found",
                cycle.CycleId,
                cycle.WeakCouplingEdges.Count);

            // Generate recommendation for each weak edge
            foreach (var edge in cycle.WeakCouplingEdges)
            {
                var rationale = GenerateRationale(edge, cycle);

                var suggestion = new CycleBreakingSuggestion(
                    cycleId: cycle.CycleId,
                    sourceProject: edge.Source,
                    targetProject: edge.Target,
                    couplingScore: edge.CouplingScore,
                    cycleSize: cycle.CycleSize,
                    rationale: rationale);

                recommendations.Add(suggestion);
            }
        }

        // Rank recommendations: lowest coupling first, then largest cycle, then alphabetical
        // Assign rank numbers during LINQ chain to avoid re-creating record objects
        var rankedRecommendations = recommendations
            .OrderBy(r => r.CouplingScore)           // Primary: Lowest coupling first
            .ThenByDescending(r => r.CycleSize)      // Secondary: Largest cycle first
            .ThenBy(r => r.SourceProject.ProjectName) // Tertiary: Alphabetical
            .Select((r, index) => r with { Rank = index + 1 }) // Assign rank (1-based)
            .ToList();

        _logger.LogDebug(
            "Generated {RecommendationCount} cycle-breaking recommendations",
            rankedRecommendations.Count);

        if (rankedRecommendations.Count > 0)
        {
            var top = rankedRecommendations[0];
            _logger.LogInformation(
                "Top recommendation: {SourceProject} → {TargetProject} (coupling: {Score}, cycle size: {CycleSize})",
                top.SourceProject.ProjectName,
                top.TargetProject.ProjectName,
                top.CouplingScore,
                top.CycleSize);
        }

        return Task.FromResult<IReadOnlyList<CycleBreakingSuggestion>>(rankedRecommendations);
    }

    /// <summary>
    /// Generates human-readable rationale explaining why this edge is recommended for breaking.
    /// </summary>
    private string GenerateRationale(DependencyEdge edge, CycleInfo cycle)
    {
        var couplingScore = edge.CouplingScore;
        var cycleSize = cycle.CycleSize;

        // Determine impact level based on cycle size
        string impactContext = cycleSize switch
        {
            >= 10 => $"critical {cycleSize}-project cycle",
            >= 6 => $"large {cycleSize}-project cycle",
            >= 4 => $"{cycleSize}-project cycle",
            _ => $"small {cycleSize}-project cycle"
        };

        // Format coupling description
        string couplingDescription = couplingScore switch
        {
            1 => "only 1 method call",
            2 => "just 2 method calls",
            <= 5 => $"only {couplingScore} method calls",
            _ => $"{couplingScore} method calls"
        };

        // Build rationale
        return $"Weakest link in {impactContext}, {couplingDescription}";
    }
}
