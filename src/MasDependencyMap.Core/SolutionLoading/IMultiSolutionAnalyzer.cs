namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Analyzes multiple solutions simultaneously and builds a unified dependency graph.
/// Enables cross-solution dependency detection and ecosystem-wide visualization.
/// </summary>
public interface IMultiSolutionAnalyzer
{
    /// <summary>
    /// Loads multiple solutions sequentially and returns unified analysis results.
    /// Each solution is loaded using the existing ISolutionLoader fallback chain.
    /// Implements graceful degradation: Continues loading remaining solutions if one fails.
    /// </summary>
    /// <param name="solutionPaths">Absolute paths to .sln files to analyze.</param>
    /// <param name="progress">Progress reporter for UI updates (optional).</param>
    /// <param name="cancellationToken">Cancellation token for operation.</param>
    /// <returns>Read-only list of SolutionAnalysis results, one per successfully loaded solution.</returns>
    /// <exception cref="ArgumentNullException">When solutionPaths is null.</exception>
    /// <exception cref="ArgumentException">When solutionPaths is empty or contains null/invalid paths.</exception>
    /// <exception cref="SolutionLoadException">When all solutions fail to load.</exception>
    Task<IReadOnlyList<SolutionAnalysis>> LoadAllAsync(
        IEnumerable<string> solutionPaths,
        IProgress<SolutionLoadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
