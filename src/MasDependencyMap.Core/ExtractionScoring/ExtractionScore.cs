namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents the final extraction difficulty score for a project, combining all four metrics with configurable weights.
/// Lower scores (0-33) indicate easier extraction, higher scores (67-100) indicate harder extraction.
/// </summary>
/// <param name="ProjectName">Name of the project being scored.</param>
/// <param name="ProjectPath">Absolute path to the project file (.csproj or .vbproj).</param>
/// <param name="FinalScore">Final weighted extraction difficulty score (0-100). Calculated as weighted sum of all four metrics.</param>
/// <param name="CouplingMetric">Coupling metric details (incoming/outgoing references). May be null if dependency graph was unavailable during scoring. When null, coupling contribution to FinalScore is 0.</param>
/// <param name="ComplexityMetric">Cyclomatic complexity metric details (average complexity, method count).</param>
/// <param name="TechDebtMetric">Technology version debt metric details (target framework, debt score).</param>
/// <param name="ExternalApiMetric">External API exposure metric details (endpoint count, API types).</param>
public sealed record ExtractionScore(
    string ProjectName,
    string ProjectPath,
    double FinalScore,
    CouplingMetric? CouplingMetric,
    ComplexityMetric ComplexityMetric,
    TechDebtMetric TechDebtMetric,
    ExternalApiMetric ExternalApiMetric)
{
    /// <summary>
    /// Gets the difficulty category based on final score.
    /// Easy: 0-33, Medium: 34-66, Hard: 67-100.
    /// </summary>
    public string DifficultyCategory => FinalScore switch
    {
        <= 33 => "Easy",
        <= 66 => "Medium",
        _ => "Hard"
    };
}
