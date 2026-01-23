namespace MasDependencyMap.Core.Rendering;

/// <summary>
/// Output formats supported by Graphviz renderer.
/// Used to specify the desired image format when rendering DOT files.
/// </summary>
public enum GraphvizOutputFormat
{
    /// <summary>PNG raster format (portable, widely supported).</summary>
    Png,

    /// <summary>SVG vector format (scalable, web-friendly).</summary>
    Svg,

    /// <summary>PDF vector format (print-ready).</summary>
    Pdf
}
