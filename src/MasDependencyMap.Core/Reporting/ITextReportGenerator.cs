namespace MasDependencyMap.Core.Reporting;

using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Generates comprehensive text reports from solution dependency analysis results.
/// Reports include dependency statistics, cycle detection results, extraction scores, and recommendations.
/// </summary>
public interface ITextReportGenerator
{
    /// <summary>
    /// Generates a comprehensive text report from dependency analysis results.
    /// </summary>
    /// <param name="graph">The dependency graph containing all projects and references. Must not be null.</param>
    /// <param name="outputDirectory">Directory where the report file will be written. Must exist.</param>
    /// <param name="solutionName">Name of the solution being analyzed. Used in filename and header.</param>
    /// <param name="cycles">Optional cycle detection results. If provided, includes Cycle Detection section (Story 5.2).</param>
    /// <param name="extractionScores">Optional extraction difficulty scores. If provided, includes Extraction Difficulty section (Story 5.3).</param>
    /// <param name="recommendations">Optional cycle-breaking recommendations. If provided, includes Recommendations section (Story 5.4).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Absolute path to the generated report file (e.g., "C:\output\MySolution-analysis-report.txt").</returns>
    /// <exception cref="ArgumentNullException">When graph, outputDirectory, or solutionName is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">When outputDirectory does not exist.</exception>
    /// <exception cref="IOException">When file write operation fails.</exception>
    Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        IReadOnlyList<CycleInfo>? cycles = null,
        IReadOnlyList<ExtractionScore>? extractionScores = null,
        IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
        CancellationToken cancellationToken = default);
}
