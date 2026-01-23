namespace MasDependencyMap.Core.Rendering;

/// <summary>
/// Provides abstraction for Graphviz detection and DOT file rendering operations.
/// Enables testing and platform-agnostic visualization generation.
/// </summary>
public interface IGraphvizRenderer
{
    /// <summary>
    /// Checks if Graphviz is installed and accessible via PATH.
    /// Executes 'dot -V' command and verifies successful version output.
    /// </summary>
    /// <returns>True if Graphviz is installed and functional, false otherwise.</returns>
    Task<bool> IsGraphvizInstalledAsync();

    /// <summary>
    /// Renders a DOT file to the specified output format (PNG, SVG, PDF).
    /// Requires Graphviz to be installed (check with IsGraphvizInstalled first).
    /// </summary>
    /// <param name="dotFilePath">Absolute path to the .dot file to render.</param>
    /// <param name="format">Output format (Png, Svg, Pdf).</param>
    /// <param name="cancellationToken">Cancellation token for timeout control.</param>
    /// <returns>Absolute path to the generated output file.</returns>
    /// <exception cref="GraphvizNotFoundException">When Graphviz is not installed or not in PATH.</exception>
    /// <exception cref="FileNotFoundException">When DOT file does not exist.</exception>
    /// <exception cref="InvalidOperationException">When rendering fails.</exception>
    Task<string> RenderToFileAsync(
        string dotFilePath,
        GraphvizOutputFormat format,
        CancellationToken cancellationToken = default);
}
