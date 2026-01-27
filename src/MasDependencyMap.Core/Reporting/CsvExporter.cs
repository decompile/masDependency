namespace MasDependencyMap.Core.Reporting;

using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using Microsoft.Extensions.Logging;

/// <summary>
/// Exports dependency analysis results to CSV files in RFC 4180 format.
/// Uses CsvHelper for compliant CSV generation with UTF-8 BOM for Excel compatibility.
/// </summary>
public sealed class CsvExporter : ICsvExporter
{
    private readonly ILogger<CsvExporter> _logger;

    /// <summary>
    /// Initializes a new instance of the CsvExporter class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    /// <exception cref="ArgumentNullException">When logger is null.</exception>
    public CsvExporter(ILogger<CsvExporter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> ExportExtractionScoresAsync(
        IReadOnlyList<ExtractionScore> scores,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionName);

        _logger.LogInformation(
            "Exporting {ScoreCount} extraction scores to CSV for solution {SolutionName}",
            scores.Count,
            solutionName);

        // Sort by extraction score ascending (easiest candidates first)
        var sortedScores = scores.OrderBy(s => s.FinalScore).ToList();

        // Start performance measurement after sorting
        var stopwatch = Stopwatch.StartNew();

        // Generate filename and ensure directory exists
        var sanitizedSolutionName = SanitizeFileName(solutionName);
        var fileName = $"{sanitizedSolutionName}-extraction-scores.csv";
        var filePath = Path.Combine(outputDirectory, fileName);

        Directory.CreateDirectory(outputDirectory);

        // Configure CsvHelper for Excel compatibility
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), // UTF-8 with BOM
            NewLine = "\r\n" // CRLF for Windows/Excel compatibility
        };

        // Write CSV using CsvHelper
        await using var writer = new StreamWriter(filePath, append: false, encoding: config.Encoding);
        await using var csv = new CsvWriter(writer, config);

        // Register ClassMap for column header customization
        csv.Context.RegisterClassMap<ExtractionScoreRecordMap>();

        // Map ExtractionScore objects to ExtractionScoreRecord POCOs
        var records = sortedScores.Select(MapToRecord).ToList();

        // Write header and data rows
        await csv.WriteRecordsAsync(records, cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        _logger.LogInformation(
            "Exported {RecordCount} extraction scores to CSV at {FilePath} in {ElapsedMs}ms",
            records.Count,
            filePath,
            stopwatch.Elapsed.TotalMilliseconds);

        return filePath;
    }

    /// <inheritdoc />
    public async Task<string> ExportCycleAnalysisAsync(
        IReadOnlyList<CycleInfo> cycles,
        IReadOnlyList<CycleBreakingSuggestion> suggestions,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        ArgumentNullException.ThrowIfNull(cycles);
        ArgumentNullException.ThrowIfNull(suggestions);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionName);

        _logger.LogInformation(
            "Exporting {CycleCount} cycles to CSV for solution {SolutionName}",
            cycles.Count,
            solutionName);

        // Create lookup dictionary for cycle → suggestion matching
        // Use first suggestion (lowest rank) if multiple suggestions exist for same cycle
        var suggestionsByCycle = suggestions
            .GroupBy(s => s.CycleId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Rank).First());

        // Sort cycles by CycleId ascending (natural ordering)
        var sortedCycles = cycles.OrderBy(c => c.CycleId).ToList();

        // Start performance measurement after sorting
        var stopwatch = Stopwatch.StartNew();

        // Generate filename and ensure directory exists
        var sanitizedSolutionName = SanitizeFileName(solutionName);
        var fileName = $"{sanitizedSolutionName}-cycle-analysis.csv";
        var filePath = Path.Combine(outputDirectory, fileName);

        Directory.CreateDirectory(outputDirectory);

        // Configure CsvHelper for Excel compatibility
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), // UTF-8 with BOM
            NewLine = "\r\n" // CRLF for Windows/Excel compatibility
        };

        // Write CSV using CsvHelper
        await using var writer = new StreamWriter(filePath, append: false, encoding: config.Encoding);
        await using var csv = new CsvWriter(writer, config);

        // Register ClassMap for column header customization
        csv.Context.RegisterClassMap<CycleAnalysisRecordMap>();

        // Map CycleInfo + CycleBreakingSuggestion objects to CycleAnalysisRecord POCOs
        var records = sortedCycles.Select(cycle =>
        {
            var suggestion = suggestionsByCycle.GetValueOrDefault(cycle.CycleId);
            return MapToRecord(cycle, suggestion);
        }).ToList();

        // Write header and data rows
        await csv.WriteRecordsAsync(records, cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        _logger.LogInformation(
            "Exported {RecordCount} cycle analysis records to CSV at {FilePath} in {ElapsedMs}ms",
            records.Count,
            filePath,
            stopwatch.Elapsed.TotalMilliseconds);

        return filePath;
    }

    /// <inheritdoc />
    public async Task<string> ExportDependencyMatrixAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionName);

        var edges = graph.Edges.ToList();
        _logger.LogInformation(
            "Exporting {EdgeCount} dependency edges to CSV for solution {SolutionName}",
            edges.Count,
            solutionName);

        // Sort by source project, then target project (both ascending)
        var sortedEdges = edges
            .OrderBy(e => e.Source.ProjectName)
            .ThenBy(e => e.Target.ProjectName)
            .ToList();

        // Start performance measurement after sorting
        var stopwatch = Stopwatch.StartNew();

        // Generate filename and ensure directory exists
        var sanitizedSolutionName = SanitizeFileName(solutionName);
        var fileName = $"{sanitizedSolutionName}-dependency-matrix.csv";
        var filePath = Path.Combine(outputDirectory, fileName);

        Directory.CreateDirectory(outputDirectory);

        // Configure CsvHelper for Excel compatibility
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), // UTF-8 with BOM
            NewLine = "\r\n" // CRLF for Windows/Excel compatibility
        };

        // Write CSV using CsvHelper
        await using var writer = new StreamWriter(filePath, append: false, encoding: config.Encoding);
        await using var csv = new CsvWriter(writer, config);

        // Register ClassMap for column header customization
        csv.Context.RegisterClassMap<DependencyMatrixRecordMap>();

        // Map DependencyEdge objects to DependencyMatrixRecord POCOs
        var records = sortedEdges.Select(MapToRecord).ToList();

        // Write header and data rows
        await csv.WriteRecordsAsync(records, cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        _logger.LogInformation(
            "Exported {RecordCount} dependency edges to CSV at {FilePath} in {ElapsedMs}ms",
            records.Count,
            filePath,
            stopwatch.Elapsed.TotalMilliseconds);

        return filePath;
    }

    /// <summary>
    /// Maps an ExtractionScore to ExtractionScoreRecord for CSV export.
    /// Handles null CouplingMetric by exporting "N/A".
    /// </summary>
    private ExtractionScoreRecord MapToRecord(ExtractionScore score)
    {
        // Log warning if CouplingMetric is null
        if (score.CouplingMetric is null)
        {
            _logger.LogWarning(
                "Project {ProjectName} has null CouplingMetric, exporting as N/A",
                score.ProjectName);
        }

        return new ExtractionScoreRecord
        {
            ProjectName = score.ProjectName,
            ExtractionScore = score.FinalScore.ToString("F1", CultureInfo.InvariantCulture),
            CouplingMetric = score.CouplingMetric?.NormalizedScore.ToString("F1", CultureInfo.InvariantCulture) ?? "N/A",
            ComplexityMetric = score.ComplexityMetric.NormalizedScore.ToString("F1", CultureInfo.InvariantCulture),
            TechDebtScore = score.TechDebtMetric.NormalizedScore.ToString("F1", CultureInfo.InvariantCulture),
            ExternalApis = score.ExternalApiMetric.EndpointCount
        };
    }

    /// <summary>
    /// Maps a CycleInfo and optional CycleBreakingSuggestion to CycleAnalysisRecord for CSV export.
    /// Handles missing suggestions by exporting "N/A" for break point and 0 for coupling score.
    /// </summary>
    private CycleAnalysisRecord MapToRecord(CycleInfo cycle, CycleBreakingSuggestion? suggestion)
    {
        // Log warning if cycle has no suggestion
        if (suggestion is null)
        {
            _logger.LogWarning(
                "Cycle {CycleId} has no breaking suggestion, exporting with N/A",
                cycle.CycleId);
        }

        // Join project names with comma-space separator
        var projectsInvolved = string.Join(", ", cycle.Projects.Select(p => p.ProjectName));

        // Format break point with arrow if suggestion exists
        var breakPoint = suggestion is not null
            ? $"{suggestion.SourceProject.ProjectName} → {suggestion.TargetProject.ProjectName}"
            : "N/A";

        // Use suggestion's coupling score, or 0 if no suggestion
        var couplingScore = suggestion?.CouplingScore ?? 0;

        return new CycleAnalysisRecord
        {
            CycleId = cycle.CycleId,
            CycleSize = cycle.CycleSize,
            ProjectsInvolved = projectsInvolved,
            SuggestedBreakPoint = breakPoint,
            CouplingScore = couplingScore
        };
    }

    /// <summary>
    /// Maps a DependencyEdge to DependencyMatrixRecord for CSV export.
    /// Formats DependencyType enum to human-readable string.
    /// </summary>
    private DependencyMatrixRecord MapToRecord(DependencyEdge edge)
    {
        return new DependencyMatrixRecord
        {
            SourceProject = edge.Source.ProjectName,
            TargetProject = edge.Target.ProjectName,
            DependencyType = FormatDependencyType(edge.DependencyType),
            CouplingScore = edge.CouplingScore
        };
    }

    private static string FormatDependencyType(DependencyType type) => type switch
    {
        DependencyType.ProjectReference => "Project Reference",
        DependencyType.BinaryReference => "Binary Reference",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown dependency type")
    };

    /// <summary>
    /// Sanitizes a file name by replacing invalid characters with underscores.
    /// Ensures cross-platform file name compatibility.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
    }
}
