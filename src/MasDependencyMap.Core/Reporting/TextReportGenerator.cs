namespace MasDependencyMap.Core.Reporting;

using System.Text;
using MasDependencyMap.Core.Configuration;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Generates comprehensive text reports from solution dependency analysis results.
/// Produces stakeholder-ready reports with summary statistics, cycle detection, extraction scores, and recommendations.
/// </summary>
public sealed class TextReportGenerator : ITextReportGenerator
{
    private readonly ILogger<TextReportGenerator> _logger;
    private readonly FilterConfiguration _filterConfiguration;
    private const int ReportWidth = 80;  // Standard terminal width for formatting

    /// <summary>
    /// Initializes a new instance of the TextReportGenerator class.
    /// </summary>
    /// <param name="logger">Logger for structured logging. Must not be null.</param>
    /// <param name="filterConfiguration">Filter configuration for framework pattern detection. Must not be null.</param>
    /// <exception cref="ArgumentNullException">When logger or filterConfiguration is null.</exception>
    public TextReportGenerator(
        ILogger<TextReportGenerator> logger,
        IOptions<FilterConfiguration> filterConfiguration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _filterConfiguration = filterConfiguration?.Value ?? throw new ArgumentNullException(nameof(filterConfiguration));
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        IReadOnlyList<CycleInfo>? cycles = null,
        IReadOnlyList<ExtractionScore>? extractionScores = null,
        IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
        CancellationToken cancellationToken = default)
    {
        // Validation
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionName);

        if (!Directory.Exists(outputDirectory))
        {
            throw new DirectoryNotFoundException($"Output directory does not exist: {outputDirectory}");
        }

        _logger.LogInformation("Generating text report for solution {SolutionName}", solutionName);

        var startTime = DateTime.UtcNow;

        // Build report content
        var report = new StringBuilder(capacity: 4096);  // Pre-allocate for performance

        AppendHeader(report, solutionName, graph);
        AppendDependencyOverview(report, graph);

        // Future stories will add more sections here
        // Story 5.2: AppendCycleDetection(report, cycles);
        // Story 5.3: AppendExtractionScores(report, extractionScores);
        // Story 5.4: AppendRecommendations(report, recommendations);

        // Write to file (sanitize solution name to prevent path traversal)
        var sanitizedName = SanitizeFileName(solutionName);
        var fileName = $"{sanitizedName}-analysis-report.txt";
        var filePath = Path.Combine(outputDirectory, fileName);

        // Use UTF-8 without BOM for plain text files (standard for cross-platform text files)
        var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        await File.WriteAllTextAsync(filePath, report.ToString(), utf8WithoutBom, cancellationToken)
            .ConfigureAwait(false);

        var elapsed = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Generated text report at {FilePath} in {ElapsedMs}ms",
            filePath,
            elapsed.TotalMilliseconds);

        return filePath;
    }

    /// <summary>
    /// Appends the report header section with solution metadata.
    /// </summary>
    /// <param name="report">The StringBuilder to append to.</param>
    /// <param name="solutionName">Name of the solution being analyzed.</param>
    /// <param name="graph">The dependency graph containing project statistics.</param>
    private void AppendHeader(StringBuilder report, string solutionName, DependencyGraph graph)
    {
        var separator = new string('=', ReportWidth);

        report.AppendLine(separator);
        report.AppendLine("MasDependencyMap Analysis Report");
        report.AppendLine(separator);
        report.AppendLine();
        report.AppendLine($"Solution: {solutionName}");
        report.AppendLine($"Analysis Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine($"Total Projects: {graph.VertexCount}");
        report.AppendLine();
        report.AppendLine(separator);
        report.AppendLine();
    }

    /// <summary>
    /// Appends the dependency overview section with reference statistics.
    /// </summary>
    /// <param name="report">The StringBuilder to append to.</param>
    /// <param name="graph">The dependency graph containing dependency statistics.</param>
    private void AppendDependencyOverview(StringBuilder report, DependencyGraph graph)
    {
        report.AppendLine("DEPENDENCY OVERVIEW");
        report.AppendLine(new string('=', ReportWidth));
        report.AppendLine();

        // Calculate statistics (single-pass for performance)
        var totalReferences = graph.EdgeCount;
        var frameworkReferences = CountFrameworkReferences(graph);
        var customReferences = totalReferences - frameworkReferences;
        var crossSolutionReferences = CountCrossSolutionReferences(graph);

        // Format with percentages
        var frameworkPercentage = totalReferences > 0 ? (frameworkReferences * 100.0 / totalReferences) : 0;
        var customPercentage = totalReferences > 0 ? (customReferences * 100.0 / totalReferences) : 0;

        report.AppendLine($"Total References: {totalReferences:N0}");
        report.AppendLine($"  - Framework References: {frameworkReferences:N0} ({frameworkPercentage:F0}%)");
        report.AppendLine($"  - Custom References: {customReferences:N0} ({customPercentage:F0}%)");
        report.AppendLine();

        if (crossSolutionReferences > 0)
        {
            report.AppendLine($"Cross-Solution References: {crossSolutionReferences:N0}");
            report.AppendLine("  (References between different solution files in multi-solution analysis)");
            report.AppendLine();
        }

        report.AppendLine(new string('=', ReportWidth));
        report.AppendLine();
    }

    /// <summary>
    /// Counts framework references in the graph using configured framework patterns.
    /// Uses the same BlockList patterns as FrameworkFilter for consistency.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze.</param>
    /// <returns>The number of edges targeting framework projects.</returns>
    private int CountFrameworkReferences(DependencyGraph graph)
    {
        return graph.Edges.Count(edge =>
            IsBlockedByFrameworkFilter(edge.Target.ProjectName));
    }

    /// <summary>
    /// Determines if a project name matches framework filter patterns.
    /// Replicates the same logic as FrameworkFilter.IsBlocked() for consistency.
    /// </summary>
    /// <param name="projectName">The project name to check.</param>
    /// <returns>True if the project matches a framework pattern; otherwise, false.</returns>
    private bool IsBlockedByFrameworkFilter(string projectName)
    {
        // Check AllowList first (takes precedence)
        foreach (var allowPattern in _filterConfiguration.AllowList)
        {
            if (MatchesPattern(projectName, allowPattern))
            {
                return false; // Explicitly allowed, not a framework reference
            }
        }

        // Check BlockList
        foreach (var blockPattern in _filterConfiguration.BlockList)
        {
            if (MatchesPattern(projectName, blockPattern))
            {
                return true; // Blocked, is a framework reference
            }
        }

        return false; // Not in BlockList, not a framework reference
    }

    /// <summary>
    /// Checks if a project name matches a wildcard pattern.
    /// Supports patterns ending with "*" for prefix matching, or exact matching.
    /// </summary>
    /// <param name="projectName">The project name to check.</param>
    /// <param name="pattern">The pattern to match against (e.g., "Microsoft.*" or "mscorlib").</param>
    /// <returns>True if the project name matches the pattern; otherwise, false.</returns>
    private static bool MatchesPattern(string projectName, string pattern)
    {
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return projectName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return projectName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sanitizes a solution name for use in filenames by replacing invalid characters.
    /// Prevents path traversal and invalid filename exceptions.
    /// </summary>
    /// <param name="solutionName">The solution name to sanitize.</param>
    /// <returns>A sanitized filename-safe version of the solution name.</returns>
    private static string SanitizeFileName(string solutionName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", solutionName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Ensure result is not empty after sanitization
        return string.IsNullOrWhiteSpace(sanitized) ? "Solution" : sanitized;
    }

    /// <summary>
    /// Counts cross-solution references in the graph.
    /// A cross-solution reference occurs when source and target projects belong to different solutions.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze.</param>
    /// <returns>The number of edges crossing solution boundaries.</returns>
    private int CountCrossSolutionReferences(DependencyGraph graph)
    {
        // Count edges where source and target have different SolutionName properties
        return graph.Edges.Count(e =>
            !string.IsNullOrEmpty(e.Source.SolutionName) &&
            !string.IsNullOrEmpty(e.Target.SolutionName) &&
            !e.Source.SolutionName.Equals(e.Target.SolutionName, StringComparison.OrdinalIgnoreCase));
    }
}
