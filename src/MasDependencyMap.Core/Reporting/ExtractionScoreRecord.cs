namespace MasDependencyMap.Core.Reporting;

/// <summary>
/// POCO record for CSV export of extraction difficulty scores.
/// Maps ExtractionScore data to CSV columns with Title Case with Spaces headers.
/// All numeric fields are formatted as strings for precise decimal control.
/// </summary>
public sealed record ExtractionScoreRecord
{
    /// <summary>
    /// Project name.
    /// Maps to CSV column: "Project Name"
    /// </summary>
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>
    /// Final extraction difficulty score (0-100), formatted with 1 decimal place.
    /// Maps to CSV column: "Extraction Score"
    /// </summary>
    public string ExtractionScore { get; init; } = string.Empty;

    /// <summary>
    /// Coupling metric normalized score (0-100), formatted with 1 decimal place.
    /// Value is "N/A" when coupling metric is unavailable.
    /// Maps to CSV column: "Coupling Metric"
    /// </summary>
    public string CouplingMetric { get; init; } = string.Empty;

    /// <summary>
    /// Complexity metric normalized score (0-100), formatted with 1 decimal place.
    /// Maps to CSV column: "Complexity Metric"
    /// </summary>
    public string ComplexityMetric { get; init; } = string.Empty;

    /// <summary>
    /// Tech debt metric normalized score (0-100), formatted with 1 decimal place.
    /// Maps to CSV column: "Tech Debt Score"
    /// </summary>
    public string TechDebtScore { get; init; } = string.Empty;

    /// <summary>
    /// Number of external API endpoints detected.
    /// Maps to CSV column: "External APIs"
    /// </summary>
    public int ExternalApis { get; init; }
}
