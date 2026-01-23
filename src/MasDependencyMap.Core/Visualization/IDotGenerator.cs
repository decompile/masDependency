namespace MasDependencyMap.Core.Visualization;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Provides abstraction for generating Graphviz DOT format files from dependency graphs.
/// Enables testing and separates DOT generation logic from rendering concerns.
/// </summary>
public interface IDotGenerator
{
    /// <summary>
    /// Generates a Graphviz DOT file from a dependency graph.
    /// Creates a directed graph with nodes representing projects and edges representing dependencies.
    /// Cross-solution dependencies are color-coded for visual distinction.
    /// </summary>
    /// <param name="graph">The dependency graph to visualize.</param>
    /// <param name="outputDirectory">Directory where the .dot file will be written.</param>
    /// <param name="solutionName">Name of the solution (used for filename generation).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Absolute path to the generated .dot file.</returns>
    /// <exception cref="ArgumentNullException">When graph, outputDirectory, or solutionName is null.</exception>
    /// <exception cref="ArgumentException">When outputDirectory or solutionName is empty.</exception>
    /// <exception cref="DotGenerationException">When DOT file generation or writing fails.</exception>
    Task<string> GenerateAsync(
        DependencyGraph graph,
        string outputDirectory,
        string solutionName,
        CancellationToken cancellationToken = default);
}
