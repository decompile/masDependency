namespace MasDependencyMap.Core.DependencyAnalysis;

using QuikGraph;

/// <summary>
/// Represents a dependency edge in the dependency graph.
/// Implements QuikGraph's IEdge interface for graph algorithm compatibility.
/// </summary>
public class DependencyEdge : IEdge<ProjectNode>
{
    /// <summary>
    /// Gets the source project (the project that has the dependency).
    /// </summary>
    public required ProjectNode Source { get; init; }

    /// <summary>
    /// Gets the target project (the project being depended upon).
    /// </summary>
    public required ProjectNode Target { get; init; }

    /// <summary>
    /// Gets the type of dependency (ProjectReference or BinaryReference).
    /// </summary>
    public required DependencyType DependencyType { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a project reference dependency.
    /// </summary>
    public bool IsProjectReference => DependencyType == DependencyType.ProjectReference;

    /// <summary>
    /// Gets a value indicating whether this dependency crosses solution boundaries.
    /// True when the source and target projects belong to different solutions.
    /// </summary>
    public bool IsCrossSolution =>
        !string.IsNullOrEmpty(Source.SolutionName) &&
        !string.IsNullOrEmpty(Target.SolutionName) &&
        !Source.SolutionName.Equals(Target.SolutionName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns a string representation of this edge for debugging.
    /// </summary>
    /// <returns>A string showing the source, target, and dependency type.</returns>
    public override string ToString()
    {
        return $"{Source.ProjectName} -> {Target.ProjectName} ({DependencyType})";
    }
}
