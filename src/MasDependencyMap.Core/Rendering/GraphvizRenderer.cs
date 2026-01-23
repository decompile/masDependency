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
    /// Executes Graphviz 'dot' command with timeout and error handling.
    /// </summary>
    /// <param name="dotFilePath">Absolute path to the .dot file to render.</param>
    /// <param name="format">Output format (Png, Svg, Pdf).</param>
    /// <param name="cancellationToken">Cancellation token for timeout control.</param>
    /// <returns>Absolute path to the generated output file.</returns>
    /// <exception cref="ArgumentNullException">When dotFilePath is null.</exception>
    /// <exception cref="ArgumentException">When dotFilePath is empty or file doesn't exist.</exception>
    /// <exception cref="GraphvizNotFoundException">When Graphviz is not installed or not in PATH.</exception>
    /// <exception cref="GraphvizRenderException">When rendering fails (non-zero exit code).</exception>
    /// <exception cref="GraphvizTimeoutException">When rendering exceeds 30-second timeout.</exception>
    public async Task<string> RenderToFileAsync(
        string dotFilePath,
        GraphvizOutputFormat format,
        CancellationToken cancellationToken = default)
    {
        // Validation
        ArgumentNullException.ThrowIfNull(dotFilePath);
        if (string.IsNullOrWhiteSpace(dotFilePath))
            throw new ArgumentException("DOT file path cannot be empty", nameof(dotFilePath));

        // Normalize to absolute path
        if (!Path.IsPathRooted(dotFilePath))
            dotFilePath = Path.GetFullPath(dotFilePath);

        if (!File.Exists(dotFilePath))
            throw new ArgumentException($"DOT file not found: {dotFilePath}", nameof(dotFilePath));

        _logger.LogInformation("Rendering {Format} from {DotFile}", format, dotFilePath);

        // Create timeout cancellation source (30 seconds)
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(RenderingTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await RenderToFileInternalAsync(dotFilePath, format, linkedCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning("Graphviz rendering timeout after {Timeout}s for {DotFile}",
                RenderingTimeoutSeconds, dotFilePath);
            throw new GraphvizTimeoutException(
                $"Rendering {format} from {dotFilePath} exceeded {RenderingTimeoutSeconds} second timeout");
        }
        catch (OperationCanceledException)
        {
            // User cancellation - rethrow
            throw;
        }
    }

    private async Task<string> RenderToFileInternalAsync(
        string dotFilePath,
        GraphvizOutputFormat format,
        CancellationToken cancellationToken)
    {
        var outputPath = GetOutputPath(dotFilePath, format);
        var arguments = BuildArguments(dotFilePath, outputPath, format);

        _logger.LogDebug("Executing: dot {Arguments}", arguments);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dot",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process? process = null;
        try
        {
            process = Process.Start(processStartInfo);
        }
        catch (Win32Exception ex) when (ex.Message.Contains("The system cannot find the file specified"))
        {
            _logger.LogError("Graphviz 'dot' executable not found in PATH");
            throw new GraphvizNotFoundException();
        }

        if (process == null)
            throw new InvalidOperationException("Failed to start Graphviz process");

        try
        {
            // Setup cancellation handler to kill process on timeout/cancellation
            await using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch (InvalidOperationException)
                {
                    // Process already exited - ignore
                }
            }))
            {
                // CRITICAL: Read output streams ASYNCHRONOUSLY FIRST to prevent deadlock
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // Wait for process with cancellation support
                await Task.Run(() => process.WaitForExit(), cancellationToken)
                    .ConfigureAwait(false);

                // Await stream reads
                var output = await outputTask;
                var error = await errorTask;

                // Log output/error at debug level (verbose diagnostics)
                if (!string.IsNullOrWhiteSpace(output))
                    _logger.LogDebug("Graphviz stdout: {Output}", output);
                if (!string.IsNullOrWhiteSpace(error))
                    _logger.LogDebug("Graphviz stderr: {Error}", error);

                // Validate exit code
                if (process.ExitCode != 0)
                {
                    _logger.LogError("Graphviz rendering failed with exit code {ExitCode}: {Error}",
                        process.ExitCode, error);
                    throw new GraphvizRenderException(
                        $"Graphviz rendering failed with exit code {process.ExitCode}: {error}");
                }

                // Validate output file was created
                ValidateOutputFile(outputPath);

                var fileSize = new FileInfo(outputPath).Length;
                _logger.LogInformation("Rendered {Format}: {OutputPath} ({FileSize} bytes)",
                    format, outputPath, fileSize);

                return outputPath;
            }
        }
        finally
        {
            process?.Dispose();
        }
    }

    private string BuildArguments(string dotFilePath, string outputPath, GraphvizOutputFormat format)
    {
        var formatString = format switch
        {
            GraphvizOutputFormat.Png => "png",
            GraphvizOutputFormat.Svg => "svg",
            GraphvizOutputFormat.Pdf => "pdf",
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };

        // Quote paths for spaces and special characters (cross-platform)
        return $"-T{formatString} -o \"{outputPath}\" \"{dotFilePath}\"";
    }

    private string GetOutputPath(string dotFilePath, GraphvizOutputFormat format)
    {
        var extension = format switch
        {
            GraphvizOutputFormat.Png => ".png",
            GraphvizOutputFormat.Svg => ".svg",
            GraphvizOutputFormat.Pdf => ".pdf",
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };

        return Path.ChangeExtension(dotFilePath, extension);
    }

    private void ValidateOutputFile(string outputPath)
    {
        if (!File.Exists(outputPath))
        {
            throw new GraphvizRenderException(
                $"Graphviz completed but output file not found: {outputPath}");
        }

        var fileInfo = new FileInfo(outputPath);
        if (fileInfo.Length == 0)
        {
            throw new GraphvizRenderException(
                $"Graphviz created empty output file: {outputPath}");
        }
    }
}
