using System.Threading.Tasks;

namespace MasDependencyMap.Core.Visualization;

/// <summary>
/// Renders DOT files to image formats using Graphviz.
/// Wraps external Graphviz process execution.
/// </summary>
public interface IGraphvizRenderer
{
    /// <summary>
    /// Checks if Graphviz is installed and available in PATH.
    /// </summary>
    /// <returns>True if Graphviz is installed, false otherwise</returns>
    bool IsGraphvizInstalled();

    /// <summary>
    /// Renders a DOT file to the specified output format.
    /// </summary>
    /// <param name="dotFilePath">Path to input .dot file</param>
    /// <param name="outputFormat">Output format (PNG, SVG, etc.)</param>
    /// <returns>Path to rendered output file</returns>
    Task<string> RenderToFileAsync(string dotFilePath, string outputFormat);
}
