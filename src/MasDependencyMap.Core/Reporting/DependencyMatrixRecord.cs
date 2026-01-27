namespace MasDependencyMap.Core.Reporting;

/// <summary>
/// POCO record for CSV export of dependency matrix.
/// Maps DependencyEdge data to CSV columns with Title Case with Spaces headers.
/// </summary>
public sealed record DependencyMatrixRecord
{
    /// <summary>
    /// Source project name (project that has the dependency).
    /// Maps to CSV column "Source Project".
    /// </summary>
    public string SourceProject { get; init; } = string.Empty;

    /// <summary>
    /// Target project name (project being depended upon).
    /// Maps to CSV column "Target Project".
    /// </summary>
    public string TargetProject { get; init; } = string.Empty;

    /// <summary>
    /// Dependency type formatted as human-readable string.
    /// Maps to CSV column "Dependency Type".
    /// Values: "Project Reference" or "Binary Reference".
    /// </summary>
    public string DependencyType { get; init; } = string.Empty;

    /// <summary>
    /// Coupling score (number of method calls from source to target).
    /// Maps to CSV column "Coupling Score".
    /// Defaults to 1 when semantic analysis is unavailable.
    /// </summary>
    public int CouplingScore { get; init; }
}
