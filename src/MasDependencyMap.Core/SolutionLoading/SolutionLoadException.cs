namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Base exception for all solution loading errors.
/// Used by fallback chain to signal that a loader failed and next loader should be tried.
/// </summary>
public class SolutionLoadException : Exception
{
    /// <summary>
    /// Creates a new SolutionLoadException with a message.
    /// </summary>
    /// <param name="message">Error message describing what failed</param>
    public SolutionLoadException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new SolutionLoadException with a message and inner exception.
    /// </summary>
    /// <param name="message">Error message describing what failed</param>
    /// <param name="innerException">Original exception that caused the failure</param>
    public SolutionLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
