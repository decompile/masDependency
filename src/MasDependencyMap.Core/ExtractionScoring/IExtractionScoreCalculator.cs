namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Calculates final extraction difficulty scores by combining all four metrics with configurable weights.
/// Orchestrates ICouplingMetricCalculator, IComplexityMetricCalculator, ITechDebtAnalyzer, and IExternalApiDetector.
/// Used to produce unified 0-100 extraction difficulty scores for migration candidate ranking.
/// </summary>
public interface IExtractionScoreCalculator
{
    /// <summary>
    /// Calculates extraction score for a single project by combining all four metrics.
    /// Note: Coupling metric requires entire graph context. If graph is not available or
    /// coupling cannot be calculated, coupling score will be 0.
    /// For accurate coupling-aware scoring, use CalculateForAllProjectsAsync instead.
    /// </summary>
    /// <param name="project">The project to score. Must not be null.</param>
    /// <param name="graph">The dependency graph containing the project. Required for coupling calculation. May be null but coupling score will be 0.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// Extraction score with final weighted score (0-100) and all individual metric details.
    /// Final score = (Coupling × WeightC) + (Complexity × WeightCx) + (TechDebt × WeightT) + (ApiExposure × WeightE).
    /// Weights loaded from scoring-config.json or defaults (0.40, 0.30, 0.20, 0.10).
    /// </returns>
    /// <exception cref="ArgumentNullException">When project is null.</exception>
    /// <exception cref="ConfigurationException">When scoring weights are invalid (don't sum to 1.0 or out of range).</exception>
    /// <exception cref="OperationCanceledException">When cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// var calculator = serviceProvider.GetRequiredService&lt;IExtractionScoreCalculator&gt;();
    /// var score = await calculator.CalculateAsync(projectNode, dependencyGraph);
    /// Console.WriteLine($"{score.ProjectName}: {score.FinalScore:F1} ({score.DifficultyCategory})");
    /// </code>
    /// </example>
    Task<ExtractionScore> CalculateAsync(
        ProjectNode project,
        DependencyGraph? graph,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates extraction scores for all projects in a dependency graph.
    /// This is the preferred method for accurate scoring because coupling metrics
    /// are relative and require the entire graph context.
    /// Results are sorted by FinalScore ascending (easiest extractions first).
    /// </summary>
    /// <param name="graph">The dependency graph containing all projects to score. Must not be null.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// Read-only list of extraction scores sorted by FinalScore ascending (easiest first).
    /// Each score includes final weighted score and all individual metric details.
    /// Returns empty list if graph has no vertices.
    /// </returns>
    /// <exception cref="ArgumentNullException">When graph is null.</exception>
    /// <exception cref="ConfigurationException">When scoring weights are invalid.</exception>
    /// <exception cref="OperationCanceledException">When cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// var calculator = serviceProvider.GetRequiredService&lt;IExtractionScoreCalculator&gt;();
    /// var scores = await calculator.CalculateForAllProjectsAsync(dependencyGraph);
    ///
    /// // Show easiest extraction candidates first
    /// foreach (var score in scores.Take(5))
    /// {
    ///     Console.WriteLine($"{score.ProjectName}: {score.FinalScore:F1} ({score.DifficultyCategory})");
    /// }
    /// </code>
    /// </example>
    Task<IReadOnlyList<ExtractionScore>> CalculateForAllProjectsAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default);
}
