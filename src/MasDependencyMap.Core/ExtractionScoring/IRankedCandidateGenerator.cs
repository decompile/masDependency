namespace MasDependencyMap.Core.ExtractionScoring;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Generates ranked lists of extraction candidates sorted by difficulty score.
/// Identifies easiest and hardest extraction candidates for migration planning.
/// </summary>
public interface IRankedCandidateGenerator
{
    /// <summary>
    /// Generates a ranked list of extraction candidates from the dependency graph.
    /// Projects are sorted by extraction score ascending (easiest first).
    /// Top 10 easiest and bottom 10 hardest candidates are identified for quick reference.
    /// </summary>
    /// <param name="graph">The dependency graph containing all projects to rank. If the graph contains no projects, returns an empty result with all counts set to zero.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A ranked list of extraction candidates with statistics. Returns empty lists if graph contains no projects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <example>
    /// <code>
    /// var ranked = await generator.GenerateRankedListAsync(dependencyGraph);
    /// Console.WriteLine($"Total projects: {ranked.Statistics.TotalProjects}");
    /// Console.WriteLine($"Easy candidates: {ranked.Statistics.EasyCount}");
    /// Console.WriteLine($"Easiest project: {ranked.EasiestCandidates.First().ProjectName}");
    /// </code>
    /// </example>
    Task<RankedExtractionCandidates> GenerateRankedListAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default);
}
