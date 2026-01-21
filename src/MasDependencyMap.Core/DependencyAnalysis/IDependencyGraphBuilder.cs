using System.Threading.Tasks;

namespace MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Builds dependency graph from solution analysis results.
/// Uses QuikGraph for graph data structure.
/// </summary>
public interface IDependencyGraphBuilder
{
    /// <summary>
    /// Builds a dependency graph from solution analysis.
    /// </summary>
    /// <param name="solutionAnalysis">Solution analysis result from ISolutionLoader</param>
    /// <returns>Dependency graph suitable for cycle detection and visualization</returns>
    Task<object> BuildGraphAsync(object solutionAnalysis); // object for now, will be typed in Epic 2
}
