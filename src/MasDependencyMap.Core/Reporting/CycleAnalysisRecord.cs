namespace MasDependencyMap.Core.Reporting;

/// <summary>
/// POCO record for CSV export of cycle analysis results.
/// Maps CycleInfo and CycleBreakingSuggestion data to CSV columns with Title Case with Spaces headers.
/// </summary>
public sealed record CycleAnalysisRecord
{
    /// <summary>
    /// Unique identifier for this cycle (1-based sequential).
    /// Maps to CSV column "Cycle ID".
    /// </summary>
    public int CycleId { get; init; }

    /// <summary>
    /// Number of projects involved in this circular dependency.
    /// Maps to CSV column "Cycle Size".
    /// </summary>
    public int CycleSize { get; init; }

    /// <summary>
    /// Comma-separated list of project names in this cycle.
    /// Maps to CSV column "Projects Involved".
    /// RFC 4180: This field will be quoted automatically by CsvHelper (contains commas).
    /// </summary>
    public string ProjectsInvolved { get; init; } = string.Empty;

    /// <summary>
    /// Suggested dependency edge to break, formatted as "Source → Target".
    /// Maps to CSV column "Suggested Break Point".
    /// Value is "N/A" when no suggestion exists for this cycle.
    /// </summary>
    public string SuggestedBreakPoint { get; init; } = string.Empty;

    /// <summary>
    /// Coupling score for the suggested break point (number of method calls).
    /// Maps to CSV column "Coupling Score".
    /// Value is 0 when no suggestion exists for this cycle.
    /// </summary>
    public int CouplingScore { get; init; }
}
