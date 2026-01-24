namespace MasDependencyMap.Core.CycleAnalysis;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Represents a recommendation for breaking a circular dependency.
/// Generated from weak coupling edges identified in cycle analysis.
/// </summary>
public sealed record CycleBreakingSuggestion : IComparable<CycleBreakingSuggestion>
{
    /// <summary>
    /// Unique identifier for this cycle (matches CycleInfo.CycleId).
    /// </summary>
    public int CycleId { get; init; }

    /// <summary>
    /// Source project of the dependency edge to break.
    /// </summary>
    public ProjectNode SourceProject { get; init; }

    /// <summary>
    /// Target project of the dependency edge to break.
    /// </summary>
    public ProjectNode TargetProject { get; init; }

    /// <summary>
    /// Coupling score for this dependency edge (number of method calls).
    /// Lower scores indicate easier dependencies to break.
    /// </summary>
    public int CouplingScore { get; init; }

    /// <summary>
    /// Size of the cycle this edge belongs to (number of projects).
    /// Larger cycles have higher impact when broken.
    /// </summary>
    public int CycleSize { get; init; }

    /// <summary>
    /// Human-readable rationale explaining why this edge is recommended.
    /// Example: "Weakest link in 8-project cycle, only 3 method calls"
    /// </summary>
    public string Rationale { get; init; }

    /// <summary>
    /// Priority ranking (1 = highest priority, calculated from coupling and cycle size).
    /// Lower coupling scores get higher priority (lower rank number).
    /// </summary>
    public int Rank { get; init; }

    public CycleBreakingSuggestion(
        int cycleId,
        ProjectNode sourceProject,
        ProjectNode targetProject,
        int couplingScore,
        int cycleSize,
        string rationale)
    {
        ArgumentNullException.ThrowIfNull(sourceProject);
        ArgumentNullException.ThrowIfNull(targetProject);
        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException("Rationale cannot be empty", nameof(rationale));
        }

        CycleId = cycleId;
        SourceProject = sourceProject;
        TargetProject = targetProject;
        CouplingScore = couplingScore;
        CycleSize = cycleSize;
        Rationale = rationale;
        Rank = 0; // Set by generator after sorting
    }

    /// <summary>
    /// Natural ordering: lowest coupling score first, then largest cycle size.
    /// </summary>
    public int CompareTo(CycleBreakingSuggestion? other)
    {
        if (other == null) return 1;

        // Primary: Lowest coupling score first (easier to break)
        var couplingComparison = CouplingScore.CompareTo(other.CouplingScore);
        if (couplingComparison != 0) return couplingComparison;

        // Secondary: Largest cycle size first (higher impact)
        var cycleSizeComparison = other.CycleSize.CompareTo(CycleSize); // Reversed for descending
        if (cycleSizeComparison != 0) return cycleSizeComparison;

        // Tertiary: Alphabetical by source project name (deterministic)
        return string.Compare(
            SourceProject.ProjectName,
            other.SourceProject.ProjectName,
            StringComparison.OrdinalIgnoreCase);
    }
}
