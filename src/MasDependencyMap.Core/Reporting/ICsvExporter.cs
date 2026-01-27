namespace MasDependencyMap.Core.Reporting;

using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Provides CSV export functionality for dependency analysis results.
/// Exports data in RFC 4180 format with UTF-8 BOM for Excel compatibility.
/// </summary>
public interface ICsvExporter
{
    /// <summary>
    /// Exports extraction difficulty scores to CSV file.
    /// Includes all metrics: final score, coupling, complexity, tech debt, external APIs.
    /// Results are sorted by extraction score ascending (easiest candidates first).
    /// </summary>
    /// <param name="scores">List of extraction scores to export.</param>
    /// <param name="outputDirectory">Directory where CSV file will be created.</param>
    /// <param name="solutionName">Solution name used for CSV filename.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Absolute path to the generated CSV file.</returns>
    /// <exception cref="ArgumentNullException">When scores is null.</exception>
    /// <exception cref="ArgumentException">When outputDirectory or solutionName is empty.</exception>
    /// <exception cref="IOException">When file write operation fails.</exception>
    Task<string> ExportExtractionScoresAsync(
        IReadOnlyList<ExtractionScore> scores,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports cycle analysis results to CSV file.
    /// Includes cycle details and suggested break points for each cycle.
    /// </summary>
    /// <param name="cycles">List of detected circular dependency cycles.</param>
    /// <param name="suggestions">List of cycle-breaking suggestions.</param>
    /// <param name="outputDirectory">Directory where CSV file will be created.</param>
    /// <param name="solutionName">Solution name used for CSV filename.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Absolute path to the generated CSV file.</returns>
    /// <exception cref="ArgumentNullException">When cycles or suggestions are null.</exception>
    /// <exception cref="ArgumentException">When outputDirectory or solutionName is empty or whitespace.</exception>
    /// <exception cref="IOException">When file write operation fails.</exception>
    Task<string> ExportCycleAnalysisAsync(
        IReadOnlyList<CycleInfo> cycles,
        IReadOnlyList<CycleBreakingSuggestion> suggestions,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports full dependency matrix to CSV file.
    /// Includes all dependency edges with coupling scores.
    /// Results are sorted by source project, then target project (both ascending).
    /// </summary>
    /// <param name="graph">Dependency graph containing all edges to export.</param>
    /// <param name="outputDirectory">Directory where CSV file will be created.</param>
    /// <param name="solutionName">Solution name used for CSV filename.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Absolute path to the generated CSV file.</returns>
    /// <exception cref="ArgumentNullException">When graph is null.</exception>
    /// <exception cref="ArgumentException">When outputDirectory or solutionName is empty or whitespace.</exception>
    /// <exception cref="IOException">When file write operation fails.</exception>
    Task<string> ExportDependencyMatrixAsync(
        DependencyAnalysis.DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default);
}
