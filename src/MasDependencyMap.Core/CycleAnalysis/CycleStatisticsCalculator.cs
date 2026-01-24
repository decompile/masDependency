namespace MasDependencyMap.Core.CycleAnalysis;

using Microsoft.Extensions.Logging;

/// <summary>
/// Calculates aggregate statistics for circular dependency cycles.
/// Uses LINQ for efficient computation of total cycles, largest cycle size,
/// distinct project counting, and participation rates.
/// </summary>
public class CycleStatisticsCalculator : ICycleStatisticsCalculator
{
    private readonly ILogger<CycleStatisticsCalculator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CycleStatisticsCalculator"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging of statistics calculations.</param>
    /// <exception cref="ArgumentNullException">When logger is null.</exception>
    public CycleStatisticsCalculator(ILogger<CycleStatisticsCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Calculates comprehensive statistics for detected cycles.
    /// Computes total cycles, largest cycle size, distinct project count,
    /// and participation rate (percentage of projects in cycles).
    /// </summary>
    /// <param name="cycles">List of detected circular dependency cycles.</param>
    /// <param name="totalProjectsAnalyzed">Total number of projects in the analyzed graph.</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <returns>
    /// CycleStatistics object containing aggregate metrics.
    /// Returns statistics with all zeros if cycles list is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">When cycles is null.</exception>
    public Task<CycleStatistics> CalculateAsync(
        IReadOnlyList<CycleInfo> cycles,
        int totalProjectsAnalyzed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cycles);

        // Edge case: no cycles detected
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

        // CRITICAL: Use Distinct() to count each project only once
        // Projects can appear in multiple cycles (overlapping SCCs)
        int totalProjectsInCycles = cycles
            .SelectMany(c => c.Projects)
            .Distinct() // Uses ProjectNode.Equals() based on ProjectPath
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
