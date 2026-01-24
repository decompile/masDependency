namespace MasDependencyMap.Core.CycleAnalysis;

using MasDependencyMap.Core.DependencyAnalysis;
using Microsoft.CodeAnalysis;
using QuikGraph;

/// <summary>
/// Analyzes coupling strength between projects by counting method calls across dependency edges.
/// Uses Roslyn semantic analysis to measure actual code-level coupling.
/// </summary>
public interface ICouplingAnalyzer
{
    /// <summary>
    /// Analyzes coupling strength for all edges in the dependency graph by counting method calls.
    /// Annotates each DependencyEdge with coupling score and strength classification.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze.</param>
    /// <param name="solution">Roslyn solution for semantic analysis (can be null for fallback).</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <returns>
    /// The same graph instance with edges annotated with coupling scores and strength classifications.
    /// If Roslyn analysis fails, falls back to reference count (coupling score = 1).
    /// </returns>
    /// <exception cref="ArgumentNullException">When graph is null.</exception>
    /// <exception cref="OperationCanceledException">When operation is cancelled.</exception>
    Task<AdjacencyGraph<ProjectNode, DependencyEdge>> AnalyzeAsync(
        AdjacencyGraph<ProjectNode, DependencyEdge> graph,
        Solution? solution,
        CancellationToken cancellationToken = default);
}
