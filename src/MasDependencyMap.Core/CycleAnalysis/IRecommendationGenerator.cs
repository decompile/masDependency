namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Generates ranked cycle-breaking recommendations from cycles with identified weak edges.
/// Recommendations are sorted by coupling score (lowest first) for architect decision-making.
/// </summary>
public interface IRecommendationGenerator
{
    /// <summary>
    /// Generates ranked cycle-breaking recommendations from cycles with weak edges.
    /// </summary>
    /// <param name="cycles">Cycles with WeakCouplingEdges populated by WeakEdgeIdentifier.</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <returns>
    /// Ranked list of cycle-breaking recommendations, sorted by coupling score (lowest first),
    /// then by cycle size (largest first), then alphabetically by project name.
    /// </returns>
    Task<IReadOnlyList<CycleBreakingSuggestion>> GenerateRecommendationsAsync(
        IReadOnlyList<CycleInfo> cycles,
        CancellationToken cancellationToken = default);
}
