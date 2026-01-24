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
    /// Calculated as (TotalProjectsInCycles / TotalProjectsAnalyzed) * 100.
    /// Returns 0.0 if TotalProjectsAnalyzed is zero (division by zero protection).
    /// </summary>
    public double ParticipationRate { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CycleStatistics"/> record.
    /// Automatically calculates the ParticipationRate based on provided values.
    /// </summary>
    /// <param name="totalCycles">Total number of circular dependency chains.</param>
    /// <param name="largestCycleSize">Size of the largest cycle.</param>
    /// <param name="totalProjectsInCycles">Total distinct projects in cycles.</param>
    /// <param name="totalProjectsAnalyzed">Total projects analyzed.</param>
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

        // Division by zero protection: if no projects analyzed, participation rate is 0.0%
        ParticipationRate = totalProjectsAnalyzed > 0
            ? (totalProjectsInCycles / (double)totalProjectsAnalyzed) * 100.0
            : 0.0;
    }
}
