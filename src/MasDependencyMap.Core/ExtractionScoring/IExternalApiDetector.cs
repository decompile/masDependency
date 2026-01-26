namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Detects external API exposure for a project by scanning for web service attributes using Roslyn semantic analysis.
/// API exposure measures extraction difficulty based on public API surface area across multiple technologies:
/// - Modern ASP.NET Core Web API ([ApiController], HTTP verb attributes)
/// - Legacy ASMX web services ([WebMethod] attributes)
/// - WCF service contracts ([ServiceContract], [OperationContract] attributes)
/// Used to quantify extraction difficulty for migration planning.
/// </summary>
public interface IExternalApiDetector
{
    /// <summary>
    /// Detects external API exposure for a single project.
    /// Uses Roslyn semantic analysis to scan for API attributes across multiple technologies (WebAPI, ASMX, WCF).
    /// Falls back to 0 endpoints (no APIs) if Roslyn analysis unavailable (conservative approach).
    /// </summary>
    /// <param name="project">The project to analyze. Must not be null. ProjectPath must point to valid .csproj or .vbproj file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation. Allows cancellation of long-running semantic analysis.</param>
    /// <returns>
    /// API exposure metrics including endpoint count and normalized 0-100 score.
    /// If Roslyn analysis fails, returns metric with EndpointCount=0, NormalizedScore=0 (conservative fallback - assumes no external APIs).
    /// </returns>
    /// <exception cref="ArgumentNullException">When project is null.</exception>
    /// <exception cref="OperationCanceledException">When cancellation is requested via cancellationToken.</exception>
    Task<ExternalApiMetric> DetectAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default);
}
