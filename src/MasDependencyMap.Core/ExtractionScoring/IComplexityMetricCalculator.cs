namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Calculates cyclomatic complexity metrics for a project using Roslyn semantic analysis.
/// Complexity measures code intricacy based on decision points (if, loops, switch, operators).
/// Used to quantify extraction difficulty for migration planning.
/// </summary>
public interface IComplexityMetricCalculator
{
    /// <summary>
    /// Calculates cyclomatic complexity metrics for a single project.
    /// Uses Roslyn to walk method syntax trees and count decision points.
    /// Falls back to neutral score (50) if Roslyn semantic analysis is unavailable.
    /// </summary>
    /// <param name="project">The project to analyze. Must not be null. ProjectPath must point to valid .csproj file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// Complexity metrics including method count, total/average complexity, and normalized 0-100 score.
    /// If Roslyn unavailable, returns metric with NormalizedScore=50 (neutral/unknown complexity).
    /// </returns>
    /// <exception cref="ArgumentNullException">When project is null.</exception>
    /// <exception cref="OperationCanceledException">When cancellation is requested.</exception>
    Task<ComplexityMetric> CalculateAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default);
}
