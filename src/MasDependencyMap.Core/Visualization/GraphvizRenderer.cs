using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MasDependencyMap.Core.Visualization;

/// <summary>
/// Renders DOT files using Graphviz external process.
/// Full implementation deferred to Epic 2 Story 2-9.
/// </summary>
public class GraphvizRenderer : IGraphvizRenderer
{
    private readonly ILogger<GraphvizRenderer> _logger;

    public GraphvizRenderer(ILogger<GraphvizRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if Graphviz is installed and available in PATH.
    /// This is a stub implementation that will be completed in Epic 2.
    /// </summary>
    /// <returns>Always returns false - stub implementation</returns>
    public bool IsGraphvizInstalled()
    {
        _logger.LogWarning("GraphvizRenderer.IsGraphvizInstalled is a stub implementation");
        return false; // Stub returns false for now
    }

    /// <summary>
    /// Renders a DOT file to the specified output format.
    /// This is a stub implementation that will be completed in Epic 2.
    /// </summary>
    /// <param name="dotFilePath">Path to input .dot file</param>
    /// <param name="outputFormat">Output format (PNG, SVG, etc.)</param>
    /// <returns>Path to rendered output file</returns>
    /// <exception cref="NotImplementedException">Always thrown - stub implementation deferred to Epic 2 Story 2-9</exception>
    public Task<string> RenderToFileAsync(string dotFilePath, string outputFormat)
    {
        _logger.LogWarning("GraphvizRenderer.RenderToFileAsync is a stub implementation");
        throw new NotImplementedException(
            "Graphviz rendering will be implemented in Epic 2 Story 2-9. " +
            "This is a stub for DI container setup in Epic 1.");
    }
}
