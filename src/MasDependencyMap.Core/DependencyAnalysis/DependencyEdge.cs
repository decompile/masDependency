namespace MasDependencyMap.Core.DependencyAnalysis;

using MasDependencyMap.Core.CycleAnalysis;
using QuikGraph;

/// <summary>
/// Represents a dependency edge in the dependency graph with coupling strength metrics.
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
    /// Gets or sets the coupling score (number of method calls from Source to Target).
    /// Defaults to 1 (reference count) if semantic analysis is unavailable.
    /// Value is mutable to allow annotation after graph construction.
    /// </summary>
    public int CouplingScore { get; set; } = 1;

    /// <summary>
    /// Gets or sets the classification of coupling strength based on method call count.
    /// Weak (1-5), Medium (6-20), Strong (21+).
    /// Defaults to Weak. Value is mutable to allow annotation after graph construction.
    /// </summary>
    public CouplingStrength CouplingStrength { get; set; } = CouplingStrength.Weak;

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
    /// <returns>A string showing the source, target, dependency type, and coupling information.</returns>
    public override string ToString()
    {
        return $"{Source.ProjectName} -> {Target.ProjectName} ({DependencyType}, {CouplingScore} calls, {CouplingStrength})";
    }
}
