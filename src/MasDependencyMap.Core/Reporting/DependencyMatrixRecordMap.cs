namespace MasDependencyMap.Core.Reporting;

using CsvHelper.Configuration;

/// <summary>
/// CsvHelper ClassMap for DependencyMatrixRecord to customize column headers with Title Case with Spaces.
/// Defines column order and header names for dependency matrix CSV export.
/// </summary>
public sealed class DependencyMatrixRecordMap : ClassMap<DependencyMatrixRecord>
{
    /// <summary>
    /// Configures the CSV column mappings for dependency matrix export.
    /// Headers use Title Case with Spaces for Excel compatibility.
    /// </summary>
    public DependencyMatrixRecordMap()
    {
        // Map properties to CSV columns with Title Case with Spaces headers
        Map(m => m.SourceProject).Name("Source Project").Index(0);
        Map(m => m.TargetProject).Name("Target Project").Index(1);
        Map(m => m.DependencyType).Name("Dependency Type").Index(2);
        Map(m => m.CouplingScore).Name("Coupling Score").Index(3);
    }
}
