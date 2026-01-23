namespace MasDependencyMap.Core.Visualization;

using System.Text;
using Microsoft.Extensions.Logging;
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

    // Edge colors for cross-solution dependencies (bold colors for visibility)
    private static readonly string[] CrossSolutionEdgeColors = { "red", "blue", "green", "purple", "orange", "brown" };

    public DotGenerator(ILogger<DotGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
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
        var dotContent = BuildDotContent(graph, out bool isMultiSolution);

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

    private string BuildDotContent(DependencyGraph graph, out bool isMultiSolution)
    {
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

        // Edges with cross-solution highlighting
        var crossSolutionCount = 0;
        foreach (var edge in graph.Edges)
        {
            var sourceEscaped = EscapeDotIdentifier(edge.Source.ProjectName);
            var targetEscaped = EscapeDotIdentifier(edge.Target.ProjectName);

            if (edge.IsCrossSolution)
            {
                // Red color and bold style for cross-solution dependencies
                builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"red\", style=\"bold\"];");
                crossSolutionCount++;
            }
            else
            {
                // Black color for intra-solution dependencies
                builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color=\"black\"];");
            }
        }

        if (crossSolutionCount > 0)
        {
            _logger.LogDebug("Applied cross-solution highlighting: {CrossSolutionCount} edges marked in red", crossSolutionCount);
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
