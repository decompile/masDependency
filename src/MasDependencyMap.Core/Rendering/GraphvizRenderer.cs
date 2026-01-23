using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MasDependencyMap.Core.Rendering;

/// <summary>
/// Detects Graphviz installation and renders DOT files to images via 'dot' command.
/// Uses Process.Start() for cross-platform compatibility (Windows, Linux, macOS).
/// </summary>
public class GraphvizRenderer : IGraphvizRenderer
{
    private readonly ILogger<GraphvizRenderer> _logger;
    private const int DetectionTimeoutSeconds = 5;
    private const int RenderingTimeoutSeconds = 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphvizRenderer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">When logger is null.</exception>
    public GraphvizRenderer(ILogger<GraphvizRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if Graphviz is installed and accessible via PATH.
    /// Executes 'dot -V' command and verifies successful version output.
    /// </summary>
    /// <returns>True if Graphviz is installed and functional, false otherwise.</returns>
    public async Task<bool> IsGraphvizInstalledAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dot",
                Arguments = "-V",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start Graphviz detection process");
                return false;
            }

            // Read stderr asynchronously BEFORE waiting (prevents deadlock if buffer fills)
            var stderrTask = process.StandardError.ReadToEndAsync();

            // Wait for exit with timeout
            if (!process.WaitForExit(DetectionTimeoutSeconds * 1000))
            {
                process.Kill();
                process.Dispose(); // Explicit disposal after kill
                _logger.LogWarning("Graphviz detection timed out after {Timeout} seconds", DetectionTimeoutSeconds);
                return false;
            }

            // Read version from stderr (Graphviz outputs to stderr, not stdout!)
            var versionOutput = await stderrTask;

            // Verify exit code and version string
            if (process.ExitCode == 0 &&
                versionOutput.Contains("graphviz version", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Graphviz detected: {Version}", versionOutput.Trim());
                return true;
            }

            _logger.LogWarning("Graphviz process exited with code {ExitCode}, output: {Output}",
                process.ExitCode, versionOutput);
            return false;
        }
        catch (Win32Exception ex) when (ex.Message.Contains("The system cannot find the file specified"))
        {
            _logger.LogWarning("Graphviz 'dot' executable not found in PATH");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during Graphviz detection");
            return false;
        }
    }

    /// <summary>
    /// Renders a DOT file to the specified output format (PNG, SVG, PDF).
    /// This implementation will be completed in Story 2.9.
    /// </summary>
    /// <param name="dotFilePath">Absolute path to the .dot file to render.</param>
    /// <param name="format">Output format (Png, Svg, Pdf).</param>
    /// <param name="cancellationToken">Cancellation token for timeout control.</param>
    /// <returns>Absolute path to the generated output file.</returns>
    /// <exception cref="NotImplementedException">This method will be implemented in Story 2.9.</exception>
    public Task<string> RenderToFileAsync(
        string dotFilePath,
        GraphvizOutputFormat format,
        CancellationToken cancellationToken = default)
    {
        // IMPLEMENTATION DEFERRED TO STORY 2.9
        throw new NotImplementedException("Rendering will be implemented in Story 2.9");
    }
}
