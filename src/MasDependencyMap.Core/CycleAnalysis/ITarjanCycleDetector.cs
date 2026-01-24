namespace MasDependencyMap.Core.CycleAnalysis;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Detects circular dependency cycles using Tarjan's strongly connected components algorithm.
/// Identifies all cycles in a dependency graph and provides cycle statistics.
/// </summary>
public interface ITarjanCycleDetector
{
    /// <summary>
    /// Detects all circular dependency cycles in the given dependency graph.
    /// Uses Tarjan's SCC algorithm to identify strongly connected components with >1 project.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <returns>
    /// List of CycleInfo objects representing detected cycles.
    /// Returns empty list if no cycles found.
    /// </returns>
    /// <exception cref="ArgumentNullException">When graph is null.</exception>
    Task<IReadOnlyList<CycleInfo>> DetectCyclesAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default);
}
