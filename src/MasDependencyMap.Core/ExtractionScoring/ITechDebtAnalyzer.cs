namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Analyzes technology version debt for a project by parsing target framework from project files.
/// Tech debt measures migration difficulty based on framework age (older = higher debt).
/// Used to quantify extraction difficulty for migration planning.
/// </summary>
public interface ITechDebtAnalyzer
{
    /// <summary>
    /// Analyzes technology version debt for a single project.
    /// Parses TargetFramework from .csproj/.vbproj XML and calculates debt score.
    /// Falls back to neutral score (50) if parsing fails.
    /// </summary>
    /// <param name="project">The project to analyze. Must not be null. ProjectPath must point to valid .csproj or .vbproj file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// Tech debt metrics including target framework and normalized 0-100 score.
    /// If XML parsing fails, returns metric with TargetFramework="unknown", NormalizedScore=50 (neutral).
    /// </returns>
    /// <exception cref="ArgumentNullException">When project is null.</exception>
    /// <exception cref="OperationCanceledException">When cancellation is requested.</exception>
    Task<TechDebtMetric> AnalyzeAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default);
}
