namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents coupling metrics for a single project in a dependency graph.
/// Coupling quantifies how connected a project is to other projects via dependencies.
/// Higher coupling indicates harder extraction (more projects to coordinate changes with).
/// </summary>
/// <param name="ProjectName">Name of the project being analyzed.</param>
/// <param name="IncomingCount">Number of projects that depend on this project (consumers). Higher values mean more projects will break if this project changes.</param>
/// <param name="OutgoingCount">Number of projects this project depends on (dependencies). Higher values mean more coupling to bring along during extraction.</param>
/// <param name="TotalScore">Weighted coupling score calculated as (IncomingCount * 2) + OutgoingCount. Incoming dependencies are weighted 2x higher because consumer projects make extraction harder.</param>
/// <param name="NormalizedScore">Coupling score normalized to 0-100 scale using linear scaling. 0 = minimal coupling (easy to extract), 100 = maximum coupling in solution (hard to extract).</param>
public sealed record CouplingMetric(
    string ProjectName,
    int IncomingCount,
    int OutgoingCount,
    int TotalScore,
    double NormalizedScore);
