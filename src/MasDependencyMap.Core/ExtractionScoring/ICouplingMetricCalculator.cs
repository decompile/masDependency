namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Calculates coupling metrics for projects in a dependency graph.
/// Coupling measures how connected each project is to others via incoming and outgoing dependencies.
/// Used to quantify extraction difficulty for migration planning.
/// </summary>
public interface ICouplingMetricCalculator
{
    /// <summary>
    /// Calculates coupling metrics for all projects in the dependency graph.
    /// Incoming dependencies are weighted 2x higher than outgoing dependencies because
    /// consumer projects make extraction significantly harder (more breakage risk).
    /// Note: Current implementation is synchronous (graph traversal is CPU-bound) but uses
    /// async signature for consistency with other Epic 4 metric calculators.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze. Must not be null.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation. Checked during graph traversal.</param>
    /// <returns>
    /// List of coupling metrics, one per project in the graph.
    /// Metrics include incoming/outgoing counts, weighted total score, and normalized 0-100 score.
    /// Returns empty list if graph has no vertices.
    /// </returns>
    /// <exception cref="ArgumentNullException">When graph is null.</exception>
    /// <exception cref="OperationCanceledException">When cancellation is requested via cancellationToken.</exception>
    /// <example>
    /// <code>
    /// var calculator = serviceProvider.GetRequiredService&lt;ICouplingMetricCalculator&gt;();
    /// var metrics = await calculator.CalculateAsync(dependencyGraph);
    ///
    /// foreach (var metric in metrics.OrderBy(m => m.NormalizedScore))
    /// {
    ///     Console.WriteLine($"{metric.ProjectName}: Score={metric.NormalizedScore:F1}, In={metric.IncomingCount}, Out={metric.OutgoingCount}");
    /// }
    /// </code>
    /// </example>
    Task<IReadOnlyList<CouplingMetric>> CalculateAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default);
}
