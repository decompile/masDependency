namespace MasDependencyMap.Core.Reporting;

using CsvHelper.Configuration;

/// <summary>
/// CsvHelper ClassMap for ExtractionScoreRecord to customize column headers with Title Case with Spaces.
/// Defines the exact column order and header names for RFC 4180 compliant CSV export.
/// </summary>
public sealed class ExtractionScoreRecordMap : ClassMap<ExtractionScoreRecord>
{
    /// <summary>
    /// Initializes the column mappings with Title Case with Spaces headers.
    /// </summary>
    public ExtractionScoreRecordMap()
    {
        // Map properties to CSV columns with Title Case with Spaces headers
        Map(m => m.ProjectName).Name("Project Name").Index(0);
        Map(m => m.ExtractionScore).Name("Extraction Score").Index(1);
        Map(m => m.CouplingMetric).Name("Coupling Metric").Index(2);
        Map(m => m.ComplexityMetric).Name("Complexity Metric").Index(3);
        Map(m => m.TechDebtScore).Name("Tech Debt Score").Index(4);
        Map(m => m.ExternalApis).Name("External APIs").Index(5);
    }
}
