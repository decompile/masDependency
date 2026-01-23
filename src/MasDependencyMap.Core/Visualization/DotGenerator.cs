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
    private static readonly string[] SolutionColors = { "red", "blue", "green", "purple", "orange", "brown" };

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

        try
        {
            // Build DOT content
            var dotContent = BuildDotContent(graph);

            // Prepare output path
            var sanitizedSolutionName = SanitizeFileName(solutionName);
            var fileName = $"{sanitizedSolutionName}-dependencies.dot";
            var absoluteOutputDir = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(absoluteOutputDir);
            var filePath = Path.Combine(absoluteOutputDir, fileName);

            // Write to file
            await File.WriteAllTextAsync(filePath, dotContent, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);

            var fileSize = new FileInfo(filePath).Length;
            _logger.LogInformation("DOT file generated: {FilePath} ({FileSize} bytes)", filePath, fileSize);

            return filePath;
        }
        catch (IOException ex)
        {
            throw new DotGenerationException($"Failed to write DOT file to {outputDirectory}: {ex.Message}", ex);
        }
    }

    private string BuildDotContent(DependencyGraph graph)
    {
        var builder = new StringBuilder();

        // Header
        builder.AppendLine("digraph dependencies {");
        builder.AppendLine("    rankdir=LR;");
        builder.AppendLine("    nodesep=0.5;");
        builder.AppendLine("    ranksep=1.0;");
        builder.AppendLine();
        builder.AppendLine("    node [shape=box, style=filled, fillcolor=lightblue];");
        builder.AppendLine("    edge [color=black, arrowhead=normal];");
        builder.AppendLine();

        // Nodes
        foreach (var vertex in graph.Vertices)
        {
            var escapedName = EscapeDotIdentifier(vertex.ProjectName);
            builder.AppendLine($"    {escapedName} [label={escapedName}];");
        }

        builder.AppendLine();

        // Edges with cross-solution color coding
        var crossSolutionCount = 0;
        foreach (var edge in graph.Edges)
        {
            var sourceEscaped = EscapeDotIdentifier(edge.Source.ProjectName);
            var targetEscaped = EscapeDotIdentifier(edge.Target.ProjectName);

            if (edge.IsCrossSolution)
            {
                var color = GetSolutionColor(edge.Source.SolutionName);
                builder.AppendLine($"    {sourceEscaped} -> {targetEscaped} [color={color}];");
                crossSolutionCount++;
            }
            else
            {
                builder.AppendLine($"    {sourceEscaped} -> {targetEscaped};");
            }
        }

        if (crossSolutionCount > 0)
        {
            _logger.LogInformation("Applied cross-solution color coding: {CrossSolutionCount} edges", crossSolutionCount);
        }

        // Footer
        builder.AppendLine("}");

        _logger.LogInformation("Generated {VertexCount} nodes and {EdgeCount} edges",
            graph.VertexCount, graph.EdgeCount);

        return builder.ToString();
    }

    private static string EscapeDotIdentifier(string identifier)
    {
        // Always quote identifiers for consistency and safety
        var escaped = identifier.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    private static string GetSolutionColor(string solutionName)
    {
        var hash = Math.Abs(solutionName.GetHashCode());
        return SolutionColors[hash % SolutionColors.Length];
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
