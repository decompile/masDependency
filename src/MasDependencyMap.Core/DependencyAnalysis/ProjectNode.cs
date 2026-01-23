namespace MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Represents a project as a vertex in the dependency graph.
/// Uses ProjectPath as the canonical unique identifier for graph equality.
/// </summary>
public class ProjectNode : IEquatable<ProjectNode>
{
    /// <summary>
    /// Gets the name of the project (e.g., "MasDependencyMap.Core").
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Gets the absolute path to the project file.
    /// This is the canonical unique identifier for equality comparison.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Gets the target framework of the project (e.g., "net8.0", "net472").
    /// </summary>
    public required string TargetFramework { get; init; }

    /// <summary>
    /// Gets the name of the solution this project belongs to.
    /// Used to track cross-solution dependencies in multi-solution analysis.
    /// </summary>
    public required string SolutionName { get; init; }

    /// <summary>
    /// Determines whether this ProjectNode is equal to another ProjectNode.
    /// Equality is based on ProjectPath (canonical unique identifier).
    /// </summary>
    /// <param name="other">The other ProjectNode to compare with.</param>
    /// <returns>True if the ProjectPath values are equal; otherwise, false.</returns>
    public bool Equals(ProjectNode? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return ProjectPath.Equals(other.ProjectPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether this ProjectNode is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the object is a ProjectNode with the same ProjectPath; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ProjectNode);
    }

    /// <summary>
    /// Returns a hash code based on the ProjectPath.
    /// Uses case-insensitive comparison for cross-platform compatibility.
    /// </summary>
    /// <returns>A hash code for this ProjectNode.</returns>
    public override int GetHashCode()
    {
        return ProjectPath.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a string representation of this ProjectNode for debugging.
    /// </summary>
    /// <returns>The ProjectName for easy identification in debug output.</returns>
    public override string ToString()
    {
        return ProjectName;
    }

    public static bool operator ==(ProjectNode? left, ProjectNode? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(ProjectNode? left, ProjectNode? right)
    {
        return !(left == right);
    }
}
