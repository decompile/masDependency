namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Exception thrown when direct project file XML parsing fails to load a solution.
/// Signals complete fallback chain exhaustion - all loaders (Roslyn, MSBuild, ProjectFile) have failed.
/// This is the last resort loader, so this exception indicates complete inability to analyze the solution.
/// Part of the 3-layer fallback strategy: Roslyn → MSBuild → ProjectFile.
/// </summary>
public class ProjectFileLoadException : SolutionLoadException
{
    /// <summary>
    /// Creates a new ProjectFileLoadException with a message.
    /// </summary>
    /// <param name="message">Error message describing why project file parsing failed</param>
    public ProjectFileLoadException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new ProjectFileLoadException with a message and inner exception.
    /// Preserves the original error for debugging while signaling complete fallback chain failure.
    /// </summary>
    /// <param name="message">Error message describing why project file parsing failed</param>
    /// <param name="innerException">Original exception from file I/O or XML parsing</param>
    public ProjectFileLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
