using System.Threading.Tasks;

namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Loads solution files and discovers project dependencies.
/// Implementations use fallback chain: Roslyn → MSBuild → ProjectFile parsing.
/// </summary>
public interface ISolutionLoader
{
    /// <summary>
    /// Loads a solution file and analyzes project dependencies.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file</param>
    /// <returns>Solution analysis with project dependency information</returns>
    Task<SolutionAnalysis> LoadAsync(string solutionPath);
}
