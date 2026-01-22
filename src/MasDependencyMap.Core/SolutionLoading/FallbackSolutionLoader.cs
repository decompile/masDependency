namespace MasDependencyMap.Core.SolutionLoading;

using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates solution loading with automatic fallback chain.
/// Tries RoslynSolutionLoader first, falls back to MSBuildSolutionLoader,
/// then ProjectFileSolutionLoader if all else fails.
/// Part of 3-layer fallback strategy: Roslyn → MSBuild → ProjectFile.
/// </summary>
public class FallbackSolutionLoader : ISolutionLoader
{
    private readonly RoslynSolutionLoader _roslynLoader;
    private readonly MSBuildSolutionLoader _msbuildLoader;
    private readonly ProjectFileSolutionLoader _projectFileLoader;
    private readonly ILogger<FallbackSolutionLoader> _logger;

    /// <summary>
    /// Creates a new FallbackSolutionLoader with all three loader implementations.
    /// </summary>
    /// <param name="roslynLoader">Primary loader using Roslyn semantic analysis</param>
    /// <param name="msbuildLoader">Secondary loader using MSBuild project references</param>
    /// <param name="projectFileLoader">Tertiary loader using direct XML parsing</param>
    /// <param name="logger">Logger for structured logging of fallback transitions</param>
    public FallbackSolutionLoader(
        RoslynSolutionLoader roslynLoader,
        MSBuildSolutionLoader msbuildLoader,
        ProjectFileSolutionLoader projectFileLoader,
        ILogger<FallbackSolutionLoader> logger)
    {
        _roslynLoader = roslynLoader ?? throw new ArgumentNullException(nameof(roslynLoader));
        _msbuildLoader = msbuildLoader ?? throw new ArgumentNullException(nameof(msbuildLoader));
        _projectFileLoader = projectFileLoader ?? throw new ArgumentNullException(nameof(projectFileLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if the loader can handle the given solution file.
    /// Delegates to the first loader (all loaders have same CanLoad logic).
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file</param>
    /// <returns>True if file exists and is a .sln file, false otherwise</returns>
    public bool CanLoad(string solutionPath)
    {
        // Delegate to first loader - all three have same CanLoad logic
        return _roslynLoader.CanLoad(solutionPath);
    }

    /// <summary>
    /// Loads solution using automatic fallback chain.
    /// Tries Roslyn first, falls back to MSBuild on RoslynLoadException,
    /// then falls back to ProjectFile on MSBuildLoadException.
    /// Throws comprehensive error if all three loaders fail.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file</param>
    /// <param name="cancellationToken">Cancellation token to abort long-running operations</param>
    /// <returns>Complete solution analysis from first successful loader</returns>
    /// <exception cref="SolutionLoadException">When all loaders fail to load the solution</exception>
    /// <exception cref="OperationCanceledException">When operation is cancelled via token</exception>
    public async Task<SolutionAnalysis> LoadAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting solution analysis with fallback chain: {SolutionPath}", solutionPath);

        RoslynLoadException? roslynException = null;
        MSBuildLoadException? msbuildException = null;
        ProjectFileLoadException? projectFileException = null;

        // Try Roslyn (best fidelity - full semantic analysis)
        try
        {
            _logger.LogInformation("Attempting solution load with Roslyn semantic analysis...");
            var result = await _roslynLoader.LoadAsync(solutionPath, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully loaded solution using Roslyn");
            return result;
        }
        catch (RoslynLoadException ex)
        {
            roslynException = ex;
            _logger.LogWarning(ex, "Roslyn semantic analysis failed, falling back to MSBuild: {SolutionPath}", solutionPath);
        }

        // Try MSBuild (medium fidelity - MSBuild project references)
        try
        {
            _logger.LogInformation("Attempting solution load with MSBuild workspace...");
            var result = await _msbuildLoader.LoadAsync(solutionPath, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully loaded solution using MSBuild (Roslyn failed)");
            return result;
        }
        catch (MSBuildLoadException ex)
        {
            msbuildException = ex;
            _logger.LogWarning(ex, "MSBuild workspace failed, falling back to ProjectFile parser: {SolutionPath}", solutionPath);
        }

        // Try ProjectFile parser (low fidelity - direct XML parsing, last resort)
        try
        {
            _logger.LogInformation("Attempting solution load with direct XML parsing...");
            var result = await _projectFileLoader.LoadAsync(solutionPath, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully loaded solution using ProjectFile parser (Roslyn and MSBuild failed)");
            return result;
        }
        catch (ProjectFileLoadException ex)
        {
            projectFileException = ex;
            _logger.LogError(ex, "ProjectFile parser failed - all loaders exhausted: {SolutionPath}", solutionPath);
        }

        // All loaders failed - create comprehensive error message
        var errorMessage = BuildComprehensiveErrorMessage(
            solutionPath,
            roslynException,
            msbuildException,
            projectFileException);

        _logger.LogError("Complete solution loading failure for: {SolutionPath}", solutionPath);
        throw new SolutionLoadException(errorMessage, projectFileException);
    }

    /// <summary>
    /// Builds comprehensive error message when all loaders fail.
    /// Aggregates all failure reasons and provides remediation steps.
    /// </summary>
    /// <param name="solutionPath">Path to the solution that failed to load</param>
    /// <param name="roslynEx">Exception from Roslyn loader</param>
    /// <param name="msbuildEx">Exception from MSBuild loader</param>
    /// <param name="projectFileEx">Exception from ProjectFile loader</param>
    /// <returns>Comprehensive error message with all failure details and suggestions</returns>
    private string BuildComprehensiveErrorMessage(
        string solutionPath,
        RoslynLoadException? roslynEx,
        MSBuildLoadException? msbuildEx,
        ProjectFileLoadException? projectFileEx)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Failed to load solution: {solutionPath}");
        sb.AppendLine();
        sb.AppendLine("All loading strategies failed:");
        sb.AppendLine();

        if (roslynEx != null)
        {
            sb.AppendLine($"1. Roslyn semantic analysis: {roslynEx.Message}");
        }

        if (msbuildEx != null)
        {
            sb.AppendLine($"2. MSBuild workspace: {msbuildEx.Message}");
        }

        if (projectFileEx != null)
        {
            sb.AppendLine($"3. Direct XML parsing: {projectFileEx.Message}");
        }

        sb.AppendLine();
        sb.AppendLine("Possible causes:");
        sb.AppendLine("- Solution file is corrupted or invalid");
        sb.AppendLine("- Project files have syntax errors");
        sb.AppendLine("- Missing .NET SDK or MSBuild installation");
        sb.AppendLine("- Incompatible solution format");
        sb.AppendLine();
        sb.AppendLine("Suggestions:");
        sb.AppendLine("- Verify solution opens in Visual Studio");
        sb.AppendLine("- Run 'dotnet build' on solution to check for errors");
        sb.AppendLine("- Check solution file encoding (UTF-8 expected)");

        return sb.ToString();
    }
}
