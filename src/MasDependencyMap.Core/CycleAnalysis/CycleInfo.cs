namespace MasDependencyMap.Core.CycleAnalysis;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Represents a circular dependency cycle detected by Tarjan's algorithm.
/// Contains the set of projects involved in the cycle and weak coupling edge analysis.
/// </summary>
public sealed class CycleInfo
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

    /// <summary>
    /// Weakest coupling edges within this cycle (lowest coupling score).
    /// Multiple edges if tied for lowest score.
    /// Populated by WeakEdgeIdentifier service (Story 3.4).
    /// Empty list until weak edge analysis is performed.
    /// </summary>
    public IReadOnlyList<DependencyEdge> WeakCouplingEdges { get; set; } = Array.Empty<DependencyEdge>();

    /// <summary>
    /// Minimum coupling score found within this cycle.
    /// Represents the strength of the weakest dependency link.
    /// Null until weak edge analysis is performed.
    /// </summary>
    public int? WeakCouplingScore { get; set; }

    public CycleInfo(int cycleId, IReadOnlyList<ProjectNode> projects)
    {
        CycleId = cycleId;
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));

        if (projects.Count < 2)
            throw new ArgumentException("Cycle must contain at least 2 projects", nameof(projects));
    }
}
