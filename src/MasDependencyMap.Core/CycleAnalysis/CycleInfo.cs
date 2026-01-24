namespace MasDependencyMap.Core.CycleAnalysis;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Represents a circular dependency cycle detected by Tarjan's algorithm.
/// Contains the set of projects involved in the cycle.
/// </summary>
public sealed record CycleInfo
{
    /// <summary>
    /// Unique identifier for this cycle (1-based sequential).
    /// </summary>
    public int CycleId { get; init; }

    /// <summary>
    /// List of projects involved in this circular dependency.
    /// </summary>
    public IReadOnlyList<ProjectNode> Projects { get; init; }

    /// <summary>
    /// Number of projects in this cycle.
    /// </summary>
    public int CycleSize => Projects.Count;

    public CycleInfo(int cycleId, IReadOnlyList<ProjectNode> projects)
    {
        CycleId = cycleId;
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));

        if (projects.Count < 2)
            throw new ArgumentException("Cycle must contain at least 2 projects", nameof(projects));
    }
}
