namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents a ranked list of extraction candidates sorted by difficulty score,
/// with top/bottom candidates highlighted and statistics summarized.
/// <para>
/// Difficulty categories use the following score boundaries:
/// - Easy: 0-33 (inclusive: scores &lt;= 33.0)
/// - Medium: 34-66 (exclusive boundaries: scores &gt; 33.0 and &lt; 67.0)
/// - Hard: 67-100 (inclusive: scores &gt;= 67.0)
/// </para>
/// </summary>
/// <param name="AllProjects">All projects sorted by extraction score ascending (easiest first).</param>
/// <param name="EasiestCandidates">Top 10 easiest extraction candidates (scores 0-33), in ascending order. May contain fewer than 10 if not enough easy projects.</param>
/// <param name="HardestCandidates">Bottom 10 hardest extraction candidates (scores 67-100), sorted descending by score (hardest first). May contain fewer than 10 if not enough hard projects.</param>
/// <param name="Statistics">Summary statistics by difficulty category.</param>
public sealed record RankedExtractionCandidates(
    IReadOnlyList<ExtractionScore> AllProjects,
    IReadOnlyList<ExtractionScore> EasiestCandidates,
    IReadOnlyList<ExtractionScore> HardestCandidates,
    ExtractionStatistics Statistics);

/// <summary>
/// Summary statistics for extraction candidates by difficulty category.
/// </summary>
/// <param name="TotalProjects">Total number of projects analyzed.</param>
/// <param name="EasyCount">Number of projects with scores 0-33 (easy extraction).</param>
/// <param name="MediumCount">Number of projects with scores 34-66 (medium extraction).</param>
/// <param name="HardCount">Number of projects with scores 67-100 (hard extraction).</param>
public sealed record ExtractionStatistics(
    int TotalProjects,
    int EasyCount,
    int MediumCount,
    int HardCount)
{
    /// <summary>
    /// Validates that category counts sum to total projects.
    /// </summary>
    public bool IsValid => EasyCount + MediumCount + HardCount == TotalProjects;
}
