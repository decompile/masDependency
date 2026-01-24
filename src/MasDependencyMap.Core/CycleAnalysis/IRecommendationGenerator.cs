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
    /// <param name="cycles">List of cycles with WeakCouplingEdges populated (from Story 3-4). Empty or null weak edges are skipped.</param>
    /// <param name="cancellationToken">Token to cancel long-running operations.</param>
    /// <returns>
    /// Ranked list of cycle-breaking recommendations with Rank property assigned (1 = highest priority).
    /// Sorted by: (1) coupling score ascending, (2) cycle size descending, (3) project name alphabetical.
    /// Returns empty list if no cycles have weak edges.
    /// </returns>
    /// <exception cref="ArgumentNullException">When cycles is null.</exception>
    /// <exception cref="OperationCanceledException">When cancellation requested.</exception>
    Task<IReadOnlyList<CycleBreakingSuggestion>> GenerateRecommendationsAsync(
        IReadOnlyList<CycleInfo> cycles,
        CancellationToken cancellationToken = default);
}
