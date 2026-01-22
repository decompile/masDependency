namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Analysis result from loading a .NET solution.
/// Contains all projects and their dependency relationships.
/// Supports .NET Framework 3.5+ through .NET 8+ projects (20-year version span).
/// </summary>
public class SolutionAnalysis
{
    /// <summary>
    /// Absolute path to the .sln file that was analyzed.
    /// </summary>
    public string SolutionPath { get; init; } = string.Empty;

    /// <summary>
    /// Solution name (file name without extension).
    /// Example: "SampleMonolith" from "SampleMonolith.sln"
    /// </summary>
    public string SolutionName { get; init; } = string.Empty;

    /// <summary>
    /// All projects in the solution with their dependencies.
    /// Includes metadata like target framework, language, and references.
    /// </summary>
    public IReadOnlyList<ProjectInfo> Projects { get; init; } = Array.Empty<ProjectInfo>();

    /// <summary>
    /// Loading strategy used to analyze the solution (Roslyn, MSBuild, or ProjectFile).
    /// Indicates which loader in the fallback chain successfully loaded the solution.
    /// </summary>
    public string LoaderType { get; init; } = string.Empty;
}
