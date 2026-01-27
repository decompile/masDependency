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
    private int _totalProjects;  // Used for cycle participation percentage calculation

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

        // Store total projects for use in helper methods
        _totalProjects = graph.VertexCount;

        // Build report content
        var report = new StringBuilder(capacity: 4096);  // Pre-allocate for performance

        AppendHeader(report, solutionName, graph);
        AppendDependencyOverview(report, graph);

        // Story 5.2: Cycle detection section
        if (cycles != null)
        {
            AppendCycleDetection(report, cycles);
        }

        // Story 5.3: Extraction difficulty scoring section
        if (extractionScores != null && extractionScores.Count > 0)
        {
            // Validate no null items in the list (defensive programming)
            if (extractionScores.Any(s => s == null))
            {
                throw new ArgumentException("Extraction scores list contains null items", nameof(extractionScores));
            }
            AppendExtractionScores(report, extractionScores);
        }

        // Future stories will add more sections here
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
    /// Appends the cycle detection section showing circular dependency analysis.
    /// Displays total cycles, project participation statistics, largest cycle, and detailed cycle listings.
    /// </summary>
    /// <param name="report">The StringBuilder to append to.</param>
    /// <param name="cycles">The list of detected cycles. Empty list shows "no cycles" message.</param>
    private void AppendCycleDetection(StringBuilder report, IReadOnlyList<CycleInfo> cycles)
    {
        report.AppendLine("CYCLE DETECTION");
        report.AppendLine(new string('=', ReportWidth));
        report.AppendLine();

        // Handle empty cycles list - no circular dependencies detected
        if (cycles.Count == 0)
        {
            report.AppendLine("No circular dependencies detected");
            report.AppendLine();
            report.AppendLine(new string('=', ReportWidth));
            report.AppendLine();

            _logger.LogDebug("No cycles detected, showing no-cycles message");
            return;
        }

        // Calculate statistics
        var totalCycles = cycles.Count;
        var uniqueProjects = cycles.SelectMany(c => c.Projects).Select(p => p.ProjectName).Distinct().Count();
        var largestCycleSize = cycles.Max(c => c.Projects.Count);
        var participationPercentage = _totalProjects > 0 ? (uniqueProjects * 100.0 / _totalProjects) : 0.0;

        _logger.LogDebug(
            "Appending cycle detection section: {CycleCount} cycles, {UniqueProjects} unique projects",
            cycles.Count,
            uniqueProjects);

        // Display summary statistics
        report.AppendLine($"Circular Dependency Chains: {totalCycles:N0}");
        report.AppendLine($"Projects in Cycles: {uniqueProjects:N0} ({participationPercentage:F1}%)");
        report.AppendLine($"Largest Cycle Size: {largestCycleSize} projects");
        report.AppendLine();

        // Display detailed cycle information
        report.AppendLine("Detailed Cycle Information:");
        report.AppendLine(new string('-', ReportWidth));
        report.AppendLine();

        for (int i = 0; i < cycles.Count; i++)
        {
            var cycle = cycles[i];
            report.AppendLine($"Cycle {i + 1}: {cycle.Projects.Count} projects");

            foreach (var project in cycle.Projects)
            {
                report.AppendLine($"  - {project.ProjectName}");
            }

            // Add blank line between cycles (but not after last cycle)
            if (i < cycles.Count - 1)
            {
                report.AppendLine();
            }
        }

        // Section closing
        report.AppendLine();
        report.AppendLine(new string('=', ReportWidth));
        report.AppendLine();
    }

    /// <summary>
    /// Appends the extraction difficulty scoring section showing ranked candidates for extraction.
    /// Displays top 10 easiest and bottom 10 hardest candidates with their metrics.
    /// </summary>
    /// <param name="report">The StringBuilder to append to.</param>
    /// <param name="extractionScores">The list of extraction scores for all projects.</param>
    private void AppendExtractionScores(StringBuilder report, IReadOnlyList<ExtractionScore> extractionScores)
    {
        report.AppendLine("EXTRACTION DIFFICULTY SCORES");
        report.AppendLine(new string('=', ReportWidth));
        report.AppendLine();

        _logger.LogDebug(
            "Appending extraction scores section: {ScoreCount} projects, showing top {TopCount}/bottom {BottomCount}",
            extractionScores.Count,
            Math.Min(10, extractionScores.Count),
            Math.Min(10, extractionScores.Count));

        // Top 10 easiest candidates (lowest scores)
        var easiestCandidates = extractionScores
            .OrderBy(s => s.FinalScore)
            .Take(10)
            .ToList();

        // Show actual score range instead of hardcoded 0-33 to avoid misleading stakeholders
        var easiestMin = (int)easiestCandidates.Min(s => s.FinalScore);
        var easiestMax = (int)easiestCandidates.Max(s => s.FinalScore);
        report.AppendLine($"Easiest Candidates (Scores {easiestMin}-{easiestMax})");
        report.AppendLine("These projects have minimal dependencies and low complexity, making them ideal");
        report.AppendLine("candidates for extraction.");
        report.AppendLine();

        for (int i = 0; i < easiestCandidates.Count; i++)
        {
            var score = easiestCandidates[i];
            var rank = i + 1;
            var incomingRefs = score.CouplingMetric?.IncomingCount ?? 0;
            var outgoingRefs = score.CouplingMetric?.OutgoingCount ?? 0;
            var externalApis = score.ExternalApiMetric.EndpointCount;

            var incomingText = $"{incomingRefs} incoming";
            var outgoingText = $"{outgoingRefs} outgoing";
            var apisText = FormatExternalApis(externalApis);

            report.AppendLine($" {rank,2}. {score.ProjectName} (Score: {score.FinalScore:F0}) - {incomingText}, {outgoingText}, {apisText}");
        }

        report.AppendLine();

        // Bottom 10 hardest candidates (highest scores)
        var hardestCandidates = extractionScores
            .OrderByDescending(s => s.FinalScore)
            .Take(10)
            .ToList();

        // Show actual score range instead of hardcoded 67-100 to avoid misleading stakeholders
        var hardestMin = (int)hardestCandidates.Min(s => s.FinalScore);
        var hardestMax = (int)hardestCandidates.Max(s => s.FinalScore);
        report.AppendLine($"Hardest Candidates (Scores {hardestMin}-{hardestMax})");
        report.AppendLine("These projects have high coupling, complexity, or technical debt, requiring");
        report.AppendLine("significant refactoring effort.");
        report.AppendLine();

        for (int i = 0; i < hardestCandidates.Count; i++)
        {
            var score = hardestCandidates[i];
            var rank = i + 1;
            var couplingScore = score.CouplingMetric?.NormalizedScore ?? 0;
            var complexityScore = score.ComplexityMetric.NormalizedScore;
            var techDebtScore = score.TechDebtMetric.NormalizedScore;

            var couplingLabel = GetComplexityLabel(couplingScore, "coupling");
            var complexityLabel = GetComplexityLabel(complexityScore, "complexity");
            var techDebtText = $"Tech debt ({techDebtScore:F0})";

            report.AppendLine($" {rank,2}. {score.ProjectName} (Score: {score.FinalScore:F0}) - {couplingLabel}, {complexityLabel}, {techDebtText}");
        }

        // Section closing
        report.AppendLine();
        report.AppendLine(new string('=', ReportWidth));
        report.AppendLine();
    }

    /// <summary>
    /// Formats external API count with grammatical correctness.
    /// </summary>
    /// <param name="count">The number of external APIs.</param>
    /// <returns>Formatted string: "no external APIs", "1 API", or "N APIs".</returns>
    private static string FormatExternalApis(int count)
    {
        return count switch
        {
            0 => "no external APIs",
            1 => "1 API",
            _ => $"{count} APIs"
        };
    }

    /// <summary>
    /// Gets a complexity label for a metric value.
    /// </summary>
    /// <param name="metric">The metric value (0-100 scale).</param>
    /// <param name="metricName">The name of the metric (e.g., "coupling", "complexity").</param>
    /// <returns>Formatted string: "High coupling (75)", "Moderate complexity (45)", etc.</returns>
    private static string GetComplexityLabel(double metric, string metricName)
    {
        var level = metric switch
        {
            >= 60 => "High",
            >= 30 => "Moderate",
            _ => "Low"
        };
        return $"{level} {metricName} ({metric:F0})";
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
