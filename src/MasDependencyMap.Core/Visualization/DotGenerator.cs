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

        // Build DOT content
        var dotContent = BuildDotContent(graph);

        // Prepare output path
        var sanitizedSolutionName = SanitizeFileName(solutionName);
        var fileName = $"{sanitizedSolutionName}-dependencies.dot";
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

    private string BuildDotContent(DependencyGraph graph)
    {
        // Estimate capacity: ~50 chars per node + ~40 chars per edge + 200 for header/footer
        var estimatedCapacity = Math.Max((graph.VertexCount * 50) + (graph.EdgeCount * 40) + 200, 1000);
        var builder = new StringBuilder(estimatedCapacity);

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
            _logger.LogDebug("Applied cross-solution color coding: {CrossSolutionCount} edges", crossSolutionCount);
        }

        // Footer
        builder.AppendLine("}");

        _logger.LogDebug("Generated {VertexCount} nodes and {EdgeCount} edges",
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
        ArgumentNullException.ThrowIfNull(solutionName);

        // Use stable hash algorithm (character-based) to ensure same solution
        // gets same color across platforms, .NET versions, and application runs
        // This is critical for deterministic DOT file generation in CI/CD
        int hash = 0;
        foreach (char c in solutionName)
        {
            hash = unchecked((hash * 31) + char.ToUpperInvariant(c));
        }

        var index = Math.Abs(hash) % SolutionColors.Length;
        return SolutionColors[index];
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
