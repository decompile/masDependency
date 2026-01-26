namespace MasDependencyMap.Core.Visualization;

using System.Text;
using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Generates Graphviz DOT format files from dependency graphs.
/// Produces DOT files compatible with Graphviz 2.38+ for visualization rendering.
/// </summary>
public class DotGenerator : IDotGenerator
{
    private readonly ILogger<DotGenerator> _logger;
    // Node fill colors for solutions (lighter shades for better readability)
    private static readonly string[] SolutionNodeColors = { "lightblue", "lightgreen", "lightyellow", "lightpink", "lightcyan", "lavender", "lightsalmon", "lightgray" };

    public DotGenerator(ILogger<DotGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        IReadOnlyList<CycleInfo>? cycles = null,
        IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
        int maxBreakPoints = 10,
        IReadOnlyList<ExtractionScore>? extractionScores = null,
        bool showScoreLabels = false,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty", nameof(outputDirectory));
        if (string.IsNullOrWhiteSpace(solutionName))
            throw new ArgumentException("Solution name cannot be null or empty", nameof(solutionName));

        _logger.LogInformation(
            "Generating DOT file for {SolutionName} ({VertexCount} nodes, {EdgeCount} edges)",
            solutionName, graph.VertexCount, graph.EdgeCount);

        // Check for empty graph
        if (graph.VertexCount == 0)
        {
            _logger.LogWarning("Empty graph - no nodes or edges to visualize");
        }

        // Build DOT content
        var dotContent = BuildDotContent(graph, cycles, recommendations, maxBreakPoints, extractionScores, showScoreLabels, out bool isMultiSolution);

        // Prepare output path with multi-solution naming support
        var sanitizedSolutionName = SanitizeFileName(solutionName);
        var fileName = isMultiSolution
            ? "Ecosystem-dependencies.dot"
            : $"{sanitizedSolutionName}-dependencies.dot";

        var absoluteOutputDir = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(absoluteOutputDir);
        var filePath = Path.Combine(absoluteOutputDir, fileName);

        // Write to file
        try
        {
            await File.WriteAllTextAsync(filePath, dotContent, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);

            var fileSize = new FileInfo(filePath).Length;
            _logger.LogInformation("DOT file generated: {FilePath} ({FileSize} bytes)", filePath, fileSize);

            return filePath;
        }
        catch (IOException ex)
        {
            throw new DotGenerationException($"Failed to write DOT file to '{filePath}': {ex.Message}", ex);
        }
    }

    private HashSet<(string source, string target)> BuildCyclicEdgeSet(
        IReadOnlyList<CycleInfo> cycles,
        DependencyGraph graph)
    {
        // Use case-insensitive comparer for consistent edge matching
        var cyclicEdges = new HashSet<(string, string)>(new CaseInsensitiveEdgeComparer());

        // Pre-build all cycle project sets for O(1) lookup - more efficient than iterating edges per cycle
        var cycleProjectSets = cycles
            .Select(cycle => new HashSet<string>(
                cycle.Projects.Select(p => p.ProjectName),
                StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Iterate edges once and check against all cycles - O(E * C) with better cache locality
        foreach (var edge in graph.Edges)
        {
            foreach (var projectsInCycle in cycleProjectSets)
            {
                if (projectsInCycle.Contains(edge.Source.ProjectName) &&
                    projectsInCycle.Contains(edge.Target.ProjectName))
                {
                    cyclicEdges.Add((edge.Source.ProjectName, edge.Target.ProjectName));
                    break; // Edge found in a cycle, no need to check other cycles
                }
            }
        }

        _logger.LogDebug(
            "Identified {CyclicEdgeCount} cyclic edges across {CycleCount} cycles",
            cyclicEdges.Count,
            cycles.Count);

        return cyclicEdges;
    }

    private HashSet<(string source, string target)> BuildBreakPointEdgeSet(
        IReadOnlyList<CycleBreakingSuggestion> recommendations,
        int maxSuggestions = 10)
    {
        // Use case-insensitive comparer for consistent edge matching
        var breakPointEdges = new HashSet<(string, string)>(new CaseInsensitiveEdgeComparer());

        // Take top N recommendations - already pre-sorted by IRecommendationGenerator
        // Sorting: (1) coupling score ascending, (2) cycle size descending, (3) project name alphabetical
        // No need to re-sort here as CycleBreakingSuggestion implements IComparable correctly
        var topRecommendations = recommendations
            .Take(maxSuggestions)
            .ToList();

        foreach (var recommendation in topRecommendations)
        {
            // Extract source and target project names from recommendation
            var sourceProjectName = recommendation.SourceProject?.ProjectName;
            var targetProjectName = recommendation.TargetProject?.ProjectName;

            if (string.IsNullOrWhiteSpace(sourceProjectName) || string.IsNullOrWhiteSpace(targetProjectName))
            {
                _logger.LogWarning(
                    "Skipping recommendation with null/empty project names: {Source} -> {Target}",
                    sourceProjectName ?? "(null)",
                    targetProjectName ?? "(null)");
                continue;
            }

            breakPointEdges.Add((sourceProjectName, targetProjectName));
        }

        _logger.LogDebug(
            "Identified {BreakPointCount} break point edges from {TotalRecommendations} recommendations (top {MaxSuggestions})",
            breakPointEdges.Count,
            recommendations.Count,
            maxSuggestions);

        return breakPointEdges;
    }

    /// <summary>
    /// Builds a lookup dictionary for O(1) extraction score access by project name.
    /// Returns null if extraction scores are not provided or empty.
    /// </summary>
    private Dictionary<string, ExtractionScore>? BuildExtractionScoreLookup(IReadOnlyList<ExtractionScore>? extractionScores)
    {
        if (extractionScores == null || extractionScores.Count == 0)
        {
            if (extractionScores != null && extractionScores.Count == 0)
            {
                _logger.LogWarning("Extraction scores provided but empty, using default node colors");
            }
            return null;
        }

        _logger.LogDebug("Applying heat map colors based on {ScoreCount} extraction scores", extractionScores.Count);

        // Build case-insensitive dictionary for O(1) score lookup
        // Use manual loop with TryAdd to handle duplicate project names gracefully
        var lookup = new Dictionary<string, ExtractionScore>(extractionScores.Count, StringComparer.OrdinalIgnoreCase);
        var duplicateCount = 0;

        foreach (var score in extractionScores)
        {
            if (!lookup.TryAdd(score.ProjectName, score))
            {
                duplicateCount++;
                _logger.LogWarning(
                    "Duplicate project name '{ProjectName}' found in extraction scores (case-insensitive), keeping first occurrence",
                    score.ProjectName);
            }
        }

        if (duplicateCount > 0)
        {
            _logger.LogWarning(
                "Found {DuplicateCount} duplicate project names in extraction scores, kept first occurrences",
                duplicateCount);
        }

        return lookup;
    }

    /// <summary>
    /// Gets extraction score for a project by name, or null if not found.
    /// </summary>
    private ExtractionScore? GetExtractionScore(string projectName, Dictionary<string, ExtractionScore>? scoreLookup)
    {
        if (scoreLookup == null)
            return null;

        if (scoreLookup.TryGetValue(projectName, out var score))
            return score;

        _logger.LogDebug("No extraction score found for project {ProjectName}, using default color", projectName);
        return null;
    }

    /// <summary>
    /// Determines node fill color based on extraction difficulty score.
    /// Returns null if score is not available (fallback to solution-based color).
    /// </summary>
    private string? GetNodeColorForScore(double? finalScore)
    {
        if (finalScore == null)
            return null;

        // Heat map color mapping based on difficulty categories
        // Easy: 0-33 (inclusive upper bound)
        if (finalScore <= 33)
            return "lightgreen";

        // Medium: 34-66 (exclusive lower, exclusive upper)
        if (finalScore < 67)
            return "yellow";

        // Hard: 67-100 (inclusive lower bound)
        return "lightcoral";
    }

    /// <summary>
    /// Formats node label with optional extraction score.
    /// Returns multi-line label "ProjectName\nScore: XX" when showScore is true and score is available.
    /// Returns project name only when showScore is false or score is unavailable.
    /// </summary>
    private string FormatNodeLabel(string projectName, double? finalScore, bool showScore)
    {
        if (!showScore || finalScore == null)
            return projectName;

        // Round to nearest integer (no decimal places)
        int scoreInt = (int)Math.Round(finalScore.Value);

        // Multi-line format: "ProjectName\nScore: XX"
        // Use \n escape sequence for centered newline in DOT format
        return $"{projectName}\\nScore: {scoreInt}";
    }

    /// <summary>
    /// Determines appropriate font color for readability based on background color.
    /// Returns "white" for dark backgrounds (lightcoral), "black" for light backgrounds (lightgreen, yellow, etc.).
    /// </summary>
    private string GetFontColorForBackground(string backgroundColor)
    {
        // Only lightcoral needs white text (medium-dark background)
        if (backgroundColor == "lightcoral")
            return "white";

        // All other colors (lightgreen, yellow, lightblue, etc.) use black
        return "black";
    }

    private string BuildDotContent(DependencyGraph graph, IReadOnlyList<CycleInfo>? cycles, IReadOnlyList<CycleBreakingSuggestion>? recommendations, int maxBreakPoints, IReadOnlyList<ExtractionScore>? extractionScores, bool showScoreLabels, out bool isMultiSolution)
    {
        // Build cyclic edge set for O(1) lookup during edge generation
        var cyclicEdges = cycles != null && cycles.Count > 0
            ? BuildCyclicEdgeSet(cycles, graph)
            : new HashSet<(string, string)>();

        // Build break point edge set for O(1) lookup during edge generation
        var breakPointEdges = recommendations != null && recommendations.Count > 0
            ? BuildBreakPointEdgeSet(recommendations, maxBreakPoints)
            : new HashSet<(string, string)>();

        // Build extraction score lookup for O(1) access during node generation
        var scoreLookup = BuildExtractionScoreLookup(extractionScores);
        bool isHeatMapMode = scoreLookup != null;

        // Detect multi-solution graphs
        var uniqueSolutions = graph.Vertices
            .Select(v => v.SolutionName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();

        isMultiSolution = uniqueSolutions.Count > 1;

        // Create solution-to-color mapping for consistent coloring
        var solutionColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < uniqueSolutions.Count; i++)
        {
            solutionColorMap[uniqueSolutions[i]] = SolutionNodeColors[i % SolutionNodeColors.Length];
        }

        // Estimate capacity: ~80 chars per node + ~60 chars per edge + legends + header/footer
        // Solution legend: ~100 chars per solution
        // Dependency type legend: ~200 chars (break points, cycles, cross-solution, default)
        var estimatedCapacity = (graph.VertexCount * 80) + (graph.EdgeCount * 60) + (uniqueSolutions.Count * 100) + 200 + 300;
        var builder = new StringBuilder(estimatedCapacity);

        // Header
        builder.AppendLine("digraph dependencies {");
        builder.AppendLine("    rankdir=LR;");
        builder.AppendLine("    nodesep=0.5;");
        builder.AppendLine("    ranksep=1.0;");
        builder.AppendLine();
        builder.AppendLine("    node [shape=box, style=filled];");
        builder.AppendLine("    edge [arrowhead=normal];");
        builder.AppendLine();

        // Add legend for multi-solution graphs
        if (isMultiSolution)
        {
            builder.AppendLine("    // Legend - Solution Color Coding");
            builder.AppendLine("    subgraph cluster_legend {");
            builder.AppendLine("        label=\"Solutions\";");
            builder.AppendLine("        style=dashed;");
            builder.AppendLine("        color=gray;");
            builder.AppendLine();

            foreach (var solution in uniqueSolutions)
            {
                var color = solutionColorMap[solution];
                var legendId = $"legend_{EscapeDotIdentifier(solution).Trim('"')}";
                builder.AppendLine($"        {legendId} [label={EscapeDotIdentifier(solution)}, fillcolor=\"{color}\"];");
            }

            builder.AppendLine("    }");
            builder.AppendLine();

            _logger.LogDebug("Generated legend for {SolutionCount} solutions", uniqueSolutions.Count);
        }

        // Handle edge case: showScoreLabels=true but no scores provided
        if (showScoreLabels && scoreLookup == null)
        {
            _logger.LogWarning("Score labels requested but no extraction scores provided, showing project names only");
        }

        // Nodes with color coding (heat map mode takes precedence over solution-based colors)
        var easyCount = 0;
        var mediumCount = 0;
        var hardCount = 0;
        var labeledNodeCount = 0;

        foreach (var vertex in graph.Vertices)
        {
            var escapedName = EscapeDotIdentifier(vertex.ProjectName);
            string nodeColor;
            ExtractionScore? score = null;

            // Heat map mode: color by extraction difficulty score
            if (isHeatMapMode)
            {
                score = GetExtractionScore(vertex.ProjectName, scoreLookup);
                var heatMapColor = GetNodeColorForScore(score?.FinalScore);

                if (heatMapColor != null)
                {
                    nodeColor = heatMapColor;

                    // Track color distribution for logging
                    if (heatMapColor == "lightgreen")
                        easyCount++;
                    else if (heatMapColor == "yellow")
                        mediumCount++;
                    else if (heatMapColor == "lightcoral")
                        hardCount++;
                }
                else
                {
                    // No score found for this project - use default color
                    nodeColor = isMultiSolution && solutionColorMap.ContainsKey(vertex.SolutionName)
                        ? solutionColorMap[vertex.SolutionName]
                        : "lightblue";
                }
            }
            // Default mode: color by solution
            else if (isMultiSolution && solutionColorMap.ContainsKey(vertex.SolutionName))
            {
                nodeColor = solutionColorMap[vertex.SolutionName];
            }
            else
            {
                // Single solution or missing solution name - use default color
                nodeColor = "lightblue";
            }

            // Format label with score if requested (and score is available)
            string labelText = FormatNodeLabel(vertex.ProjectName, score?.FinalScore, showScoreLabels);
            string escapedLabel = EscapeDotIdentifier(labelText);

            // Get appropriate font color for readability
            string fontColor = GetFontColorForBackground(nodeColor);

            // Generate node with label, background color, and font color
            builder.AppendLine($"    {escapedName} [label={escapedLabel}, fillcolor=\"{nodeColor}\", fontcolor=\"{fontColor}\"];");

            // Track labeled nodes for logging
            if (showScoreLabels && score?.FinalScore != null)
                labeledNodeCount++;
        }

        if (isHeatMapMode)
        {
            _logger.LogDebug(
                "Applied heat map colors to {NodeCount} nodes: {EasyCount} easy, {MediumCount} medium, {HardCount} hard",
                easyCount + mediumCount + hardCount,
                easyCount,
                mediumCount,
                hardCount);
        }

        if (showScoreLabels && scoreLookup != null)
        {
            _logger.LogDebug(
                "Applied score labels to {LabeledNodeCount} of {TotalNodeCount} nodes",
                labeledNodeCount,
                graph.VertexCount);
        }

        builder.AppendLine();

        // Edges with break point, cycle, and cross-solution highlighting
        var breakPointEdgeCount = 0;
        var cyclicEdgeCount = 0;
        var crossSolutionCount = 0;
        foreach (var edge in graph.Edges)
        {
            var sourceEscaped = EscapeDotIdentifier(edge.Source.ProjectName);
            var targetEscaped = EscapeDotIdentifier(edge.Target.ProjectName);

            // Check if edge is a suggested break point (HIGHEST PRIORITY)
            if (breakPointEdges.Contains((edge.Source.ProjectName, edge.Target.ProjectName)))
            {
                builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"yellow\", style=\"bold\"];");
                breakPointEdgeCount++;
            }
            // Check if edge is cyclic (medium-high priority)
            else if (cyclicEdges.Contains((edge.Source.ProjectName, edge.Target.ProjectName)))
            {
                builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"red\", style=\"bold\"];");
                cyclicEdgeCount++;
            }
            // Check if edge is cross-solution (medium priority)
            else if (edge.IsCrossSolution)
            {
                // Blue color and bold style for cross-solution dependencies
                builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"blue\", style=\"bold\"];");
                crossSolutionCount++;
            }
            // Default: intra-solution, non-cyclic, not a break suggestion
            else
            {
                // Black color for normal dependencies
                builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"black\"];");
            }
        }

        if (breakPointEdgeCount > 0)
        {
            _logger.LogDebug("Applied break point highlighting: {BreakPointCount} edges marked in yellow", breakPointEdgeCount);
        }

        if (cyclicEdgeCount > 0)
        {
            _logger.LogDebug("Applied cycle highlighting: {CyclicEdgeCount} edges marked in red", cyclicEdgeCount);
        }

        if (crossSolutionCount > 0)
        {
            _logger.LogDebug("Applied cross-solution highlighting: {CrossSolutionCount} edges marked in blue", crossSolutionCount);
        }

        // Add legend when any highlighting is active (cycles OR recommendations)
        if ((cycles != null && cycles.Count > 0 && cyclicEdgeCount > 0) ||
            (recommendations != null && recommendations.Count > 0 && breakPointEdgeCount > 0))
        {
            builder.AppendLine();
            builder.AppendLine("    // Legend - Dependency Types");
            builder.AppendLine("    subgraph cluster_dependency_legend {");
            builder.AppendLine("        label=\"Dependency Types\";");
            builder.AppendLine("        style=dashed;");
            builder.AppendLine("        color=gray;");
            builder.AppendLine();

            // Break points (highest priority, show first)
            if (breakPointEdgeCount > 0)
            {
                // Show requested limit (e.g., "Top 10") rather than actual count to avoid confusion
                builder.AppendLine($"        legend_breakpoint [label=\"Yellow: Suggested Break Points (Top {maxBreakPoints})\", color=\"yellow\", style=\"bold\", shape=\"box\"];");
            }

            // Cycles (show when present)
            if (cyclicEdgeCount > 0)
            {
                builder.AppendLine("        legend_cycle [label=\"Red: Circular Dependencies\", color=\"red\", style=\"bold\", shape=\"box\"];");
            }

            // Cross-solution (show when present)
            if (crossSolutionCount > 0)
            {
                builder.AppendLine("        legend_cross [label=\"Blue: Cross-Solution\", color=\"blue\", style=\"bold\", shape=\"box\"];");
            }

            // Default (always show as baseline)
            builder.AppendLine("        legend_default [label=\"Black: Normal Dependencies\", color=\"black\", shape=\"box\"];");
            builder.AppendLine("    }");

            _logger.LogDebug("Generated dependency type legend");
        }

        // Add extraction difficulty legend when heat map mode is active
        if (isHeatMapMode)
        {
            builder.AppendLine();
            builder.AppendLine("    // Legend - Extraction Difficulty");
            builder.AppendLine("    subgraph cluster_extraction_legend {");
            builder.AppendLine("        label=\"Extraction Difficulty\";");
            builder.AppendLine("        style=dashed;");
            builder.AppendLine("        color=gray;");
            builder.AppendLine();
            builder.AppendLine("        legend_easy [label=\"Green: Easy (0-33)\", fillcolor=\"lightgreen\", style=\"filled\", shape=\"box\"];");
            builder.AppendLine("        legend_medium [label=\"Yellow: Medium (34-66)\", fillcolor=\"yellow\", style=\"filled\", shape=\"box\"];");
            builder.AppendLine("        legend_hard [label=\"Red: Hard (67-100)\", fillcolor=\"lightcoral\", style=\"filled\", shape=\"box\"];");
            builder.AppendLine();
            builder.AppendLine("        // Invisible edges to arrange legend items horizontally");
            builder.AppendLine("        legend_easy -> legend_medium -> legend_hard [style=invis];");
            builder.AppendLine("    }");

            _logger.LogDebug("Generated extraction difficulty legend");
        }

        // Footer
        builder.AppendLine("}");

        _logger.LogDebug("Generated {VertexCount} nodes and {EdgeCount} edges across {SolutionCount} solutions",
            graph.VertexCount, graph.EdgeCount, uniqueSolutions.Count);

        return builder.ToString();
    }

    private static string EscapeDotIdentifier(string identifier)
    {
        // Always quote identifiers for consistency and safety
        var escaped = identifier.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }


    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "output";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Handle edge case where all characters were invalid
        return string.IsNullOrWhiteSpace(sanitized) ? "output" : sanitized;
    }

    /// <summary>
    /// Case-insensitive equality comparer for edge tuples (source, target).
    /// Ensures consistent edge matching regardless of project name casing.
    /// </summary>
    private sealed class CaseInsensitiveEdgeComparer : IEqualityComparer<(string, string)>
    {
        public bool Equals((string, string) x, (string, string) y)
        {
            return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string, string) obj)
        {
            return HashCode.Combine(
                obj.Item1?.ToLowerInvariant() ?? string.Empty,
                obj.Item2?.ToLowerInvariant() ?? string.Empty);
        }
    }
}
