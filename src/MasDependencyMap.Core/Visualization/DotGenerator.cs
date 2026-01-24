namespace MasDependencyMap.Core.Visualization;

using System.Text;
using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;

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
        var dotContent = BuildDotContent(graph, cycles, recommendations, out bool isMultiSolution);

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
        var cyclicEdges = new HashSet<(string, string)>();

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
        var breakPointEdges = new HashSet<(string, string)>();

        // Take top N recommendations to avoid visual clutter
        var topRecommendations = recommendations
            .OrderBy(r => r.CouplingScore)  // Lowest coupling score first (weakest links)
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

    private string BuildDotContent(DependencyGraph graph, IReadOnlyList<CycleInfo>? cycles, IReadOnlyList<CycleBreakingSuggestion>? recommendations, out bool isMultiSolution)
    {
        // Build cyclic edge set for O(1) lookup during edge generation
        var cyclicEdges = cycles != null && cycles.Count > 0
            ? BuildCyclicEdgeSet(cycles, graph)
            : new HashSet<(string, string)>();

        // Build break point edge set for O(1) lookup during edge generation
        var breakPointEdges = recommendations != null && recommendations.Count > 0
            ? BuildBreakPointEdgeSet(recommendations)
            : new HashSet<(string, string)>();

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

        // Estimate capacity: ~80 chars per node + ~60 chars per edge + legend + 300 for header/footer
        var estimatedCapacity = (graph.VertexCount * 80) + (graph.EdgeCount * 60) + (uniqueSolutions.Count * 100) + 300;
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

        // Nodes with color coding based on solution
        foreach (var vertex in graph.Vertices)
        {
            var escapedName = EscapeDotIdentifier(vertex.ProjectName);

            if (isMultiSolution && solutionColorMap.ContainsKey(vertex.SolutionName))
            {
                var color = solutionColorMap[vertex.SolutionName];
                builder.AppendLine($"    {escapedName} [label={escapedName}, fillcolor=\"{color}\"];");
            }
            else
            {
                // Single solution or missing solution name - use default color
                builder.AppendLine($"    {escapedName} [label={escapedName}, fillcolor=\"lightblue\"];");
            }
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
                var topN = Math.Min(breakPointEdgeCount, 10);
                builder.AppendLine($"        legend_breakpoint [label=\"Yellow: Suggested Break Points (Top {topN})\", color=\"yellow\", style=\"bold\", shape=\"box\"];");
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
}
