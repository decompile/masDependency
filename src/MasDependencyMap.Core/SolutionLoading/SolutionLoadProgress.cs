namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Progress information for multi-solution loading operation.
/// Used for Spectre.Console progress indicators.
/// </summary>
public class SolutionLoadProgress
{
    /// <summary>
    /// Index of currently loading solution (0-based).
    /// </summary>
    public int CurrentIndex { get; init; }

    /// <summary>
    /// Total number of solutions to load.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Name of currently loading solution file.
    /// </summary>
    public string CurrentFileName { get; init; } = string.Empty;

    /// <summary>
    /// Number of projects loaded in current solution (if available).
    /// </summary>
    public int? ProjectCount { get; init; }

    /// <summary>
    /// Elapsed time for current solution (if complete).
    /// </summary>
    public TimeSpan? ElapsedTime { get; init; }

    /// <summary>
    /// True if current solution completed successfully.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Error message if current solution failed to load.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
