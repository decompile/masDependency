namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Exception thrown when Roslyn fails to load a solution.
/// Signals to the fallback chain that MSBuildSolutionLoader or ProjectFileSolutionLoader should be tried.
/// Part of the 3-layer fallback strategy: Roslyn → MSBuild → ProjectFile.
/// </summary>
public class RoslynLoadException : SolutionLoadException
{
    /// <summary>
    /// Creates a new RoslynLoadException with a message.
    /// </summary>
    /// <param name="message">Error message describing why Roslyn failed</param>
    public RoslynLoadException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new RoslynLoadException with a message and inner exception.
    /// Preserves the original error for debugging while signaling fallback needed.
    /// </summary>
    /// <param name="message">Error message describing why Roslyn failed</param>
    /// <param name="innerException">Original exception from Roslyn/MSBuildWorkspace</param>
    public RoslynLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
