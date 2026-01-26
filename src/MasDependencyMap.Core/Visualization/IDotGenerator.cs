namespace MasDependencyMap.Core.Visualization;

using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Provides abstraction for generating Graphviz DOT format files from dependency graphs.
/// Enables testing and separates DOT generation logic from rendering concerns.
/// </summary>
public interface IDotGenerator
{
    /// <summary>
    /// Generates a Graphviz DOT file from a dependency graph.
    /// Creates a directed graph with nodes representing projects and edges representing dependencies.
    /// Circular dependencies are highlighted in RED when cycle information is provided.
    /// Suggested break points are highlighted in YELLOW (takes priority over cycle highlighting).
    /// Cross-solution dependencies are color-coded in BLUE for visual distinction.
    /// When extraction scores are provided, nodes are colored green (easy 0-33), yellow (medium 34-66), or red (hard 67-100) instead of solution-based colors.
    /// </summary>
    /// <param name="graph">The dependency graph to visualize.</param>
    /// <param name="outputDirectory">Directory where the .dot file will be written.</param>
    /// <param name="solutionName">Name of the solution (used for filename generation).</param>
    /// <param name="cycles">Optional list of detected circular dependencies for highlighting.
    /// When provided, edges within cycles are rendered in RED. When null or empty, no cycle highlighting is applied.</param>
    /// <param name="recommendations">Optional list of cycle-breaking recommendations for highlighting.
    /// When provided, top N suggested break point edges are rendered in YELLOW (where N = maxBreakPoints).
    /// YELLOW takes priority over RED if edge is both cyclic and a break suggestion.</param>
    /// <param name="maxBreakPoints">Maximum number of break point edges to highlight in YELLOW (default: 10).
    /// Limits visual clutter by showing only the highest priority recommendations.</param>
    /// <param name="extractionScores">Optional extraction difficulty scores for heat map node coloring.
    /// When provided, nodes are colored green (easy 0-33), yellow (medium 34-66), or red (hard 67-100) instead of solution-based colors.
    /// When null or empty, existing solution-based node coloring is used.</param>
    /// <param name="showScoreLabels">When true and extractionScores are provided, node labels include extraction scores in format "ProjectName\nScore: XX".
    /// When false, labels show project names only (default behavior). Requires extractionScores to be non-null and non-empty to display scores.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Absolute path to the generated .dot file.</returns>
    /// <exception cref="ArgumentNullException">When graph, outputDirectory, or solutionName is null.</exception>
    /// <exception cref="ArgumentException">When outputDirectory or solutionName is empty.</exception>
    /// <exception cref="DotGenerationException">When DOT file generation or writing fails.</exception>
    Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        IReadOnlyList<CycleInfo>? cycles = null,
        IReadOnlyList<CycleBreakingSuggestion>? recommendations = null,
        int maxBreakPoints = 10,
        IReadOnlyList<ExtractionScore>? extractionScores = null,
        bool showScoreLabels = false,
        CancellationToken cancellationToken = default);
}
