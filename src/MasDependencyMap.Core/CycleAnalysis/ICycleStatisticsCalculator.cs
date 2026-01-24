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
