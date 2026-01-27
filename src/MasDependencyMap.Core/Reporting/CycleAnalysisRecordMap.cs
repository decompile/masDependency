namespace MasDependencyMap.Core.Reporting;

using CsvHelper.Configuration;

/// <summary>
/// CsvHelper ClassMap for CycleAnalysisRecord to customize column headers with Title Case with Spaces.
/// Defines column order and header names for cycle analysis CSV export.
/// </summary>
public sealed class CycleAnalysisRecordMap : ClassMap<CycleAnalysisRecord>
{
    /// <summary>
    /// Configures the CSV column mappings for cycle analysis export.
    /// Headers use Title Case with Spaces for Excel compatibility.
    /// </summary>
    public CycleAnalysisRecordMap()
    {
        // Map properties to CSV columns with Title Case with Spaces headers
        Map(m => m.CycleId).Name("Cycle ID").Index(0);
        Map(m => m.CycleSize).Name("Cycle Size").Index(1);
        Map(m => m.ProjectsInvolved).Name("Projects Involved").Index(2);
        Map(m => m.SuggestedBreakPoint).Name("Suggested Break Point").Index(3);
        Map(m => m.CouplingScore).Name("Coupling Score").Index(4);
    }
}
