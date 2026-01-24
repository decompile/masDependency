namespace MasDependencyMap.Core.CycleAnalysis;

using MasDependencyMap.Core.DependencyAnalysis;
using QuikGraph;

/// <summary>
/// Service for identifying weakest coupling edges within circular dependency cycles.
/// Analyzes cycles to find edges with the lowest coupling scores, which represent
/// the easiest dependencies to break when resolving circular dependencies.
/// </summary>
public interface IWeakEdgeIdentifier
{
    /// <summary>
    /// Identifies the weakest coupling edges within each circular dependency cycle.
    /// For each cycle, finds all edges with the minimum coupling score.
    /// </summary>
    /// <param name="cycles">Circular dependency cycles to analyze.</param>
    /// <param name="graph">Dependency graph with coupling scores (from CouplingAnalyzer).</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <returns>
    /// The input cycles with WeakCouplingEdges and WeakCouplingScore properties populated.
    /// If multiple edges have the same minimum score, all are flagged as weak edges.
    /// </returns>
    /// <exception cref="ArgumentNullException">If cycles or graph is null.</exception>
    /// <exception cref="InvalidOperationException">If coupling analysis has not been performed on the graph.</exception>
    IReadOnlyList<CycleInfo> IdentifyWeakEdges(
        IReadOnlyList<CycleInfo> cycles,
        AdjacencyGraph<ProjectNode, DependencyEdge> graph,
        CancellationToken cancellationToken = default);
}
