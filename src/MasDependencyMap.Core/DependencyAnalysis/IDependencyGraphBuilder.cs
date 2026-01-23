namespace MasDependencyMap.Core.DependencyAnalysis;

using MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Builds dependency graphs from solution analysis results.
/// Supports single-solution and multi-solution graph construction.
/// </summary>
public interface IDependencyGraphBuilder
{
    /// <summary>
    /// Builds a dependency graph from a single solution analysis.
    /// </summary>
    /// <param name="solution">The solution analysis containing projects and dependencies.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A dependency graph with all projects and dependencies.</returns>
    Task<DependencyGraph> BuildAsync(SolutionAnalysis solution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a unified dependency graph from multiple solution analyses.
    /// Merges projects from all solutions and detects cross-solution dependencies.
    /// </summary>
    /// <param name="solutions">The collection of solution analyses to merge.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A unified dependency graph containing all projects across all solutions.</returns>
    Task<DependencyGraph> BuildAsync(IEnumerable<SolutionAnalysis> solutions, CancellationToken cancellationToken = default);
}
