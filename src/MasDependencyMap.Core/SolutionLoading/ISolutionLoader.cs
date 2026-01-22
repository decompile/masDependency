namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Loads .NET solution files and extracts project dependency information.
/// Implementations may use different loading strategies (Roslyn, MSBuild, XML parsing).
/// Part of 3-layer fallback chain: Roslyn → MSBuild → ProjectFile.
/// </summary>
public interface ISolutionLoader
{
    /// <summary>
    /// Checks if the loader can handle the given solution file.
    /// Used by fallback chain to determine which loader to try.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file</param>
    /// <returns>True if loader can process this solution, false otherwise</returns>
    bool CanLoad(string solutionPath);

    /// <summary>
    /// Loads solution and extracts project dependency graph.
    /// Performs semantic analysis to discover all project references and assembly dependencies.
    /// Supports cancellation for long-running solution analysis operations.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file</param>
    /// <param name="cancellationToken">Cancellation token to abort long-running operations</param>
    /// <returns>Complete solution analysis with all projects and dependencies</returns>
    /// <exception cref="SolutionLoadException">When solution cannot be loaded by this loader</exception>
    /// <exception cref="OperationCanceledException">When operation is cancelled via token</exception>
    Task<SolutionAnalysis> LoadAsync(string solutionPath, CancellationToken cancellationToken = default);
}
