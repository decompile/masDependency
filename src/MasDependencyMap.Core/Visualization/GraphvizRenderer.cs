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

    public bool IsGraphvizInstalled()
    {
        _logger.LogWarning("GraphvizRenderer.IsGraphvizInstalled is a stub implementation");
        return false; // Stub returns false for now
    }

    public Task<string> RenderToFileAsync(string dotFilePath, string outputFormat)
    {
        _logger.LogWarning("GraphvizRenderer.RenderToFileAsync is a stub implementation");
        throw new NotImplementedException(
            "Graphviz rendering will be implemented in Epic 2 Story 2-9. " +
            "This is a stub for DI container setup in Epic 1.");
    }
}
