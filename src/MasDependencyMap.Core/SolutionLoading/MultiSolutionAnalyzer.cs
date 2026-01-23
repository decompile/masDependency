namespace MasDependencyMap.Core.SolutionLoading;

using Microsoft.Extensions.Logging;
using System.Diagnostics;

/// <summary>
/// Loads multiple solutions sequentially and coordinates unified dependency analysis.
/// Implements graceful degradation: continues loading remaining solutions if one fails.
/// </summary>
public class MultiSolutionAnalyzer : IMultiSolutionAnalyzer
{
    private readonly ISolutionLoader _solutionLoader;
    private readonly ILogger<MultiSolutionAnalyzer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSolutionAnalyzer"/> class.
    /// </summary>
    /// <param name="solutionLoader">Solution loader to use for each solution.</param>
    /// <param name="logger">Logger for structured logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public MultiSolutionAnalyzer(
        ISolutionLoader solutionLoader,
        ILogger<MultiSolutionAnalyzer> logger)
    {
        _solutionLoader = solutionLoader ?? throw new ArgumentNullException(nameof(solutionLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads multiple solutions sequentially and returns unified analysis results.
    /// Each solution is loaded using the existing ISolutionLoader fallback chain.
    /// Implements graceful degradation: Continues loading remaining solutions if one fails.
    /// </summary>
    /// <param name="solutionPaths">Absolute paths to .sln files to analyze.</param>
    /// <param name="progress">Progress reporter for UI updates (optional).</param>
    /// <param name="cancellationToken">Cancellation token for operation.</param>
    /// <returns>Read-only list of SolutionAnalysis results, one per successfully loaded solution.</returns>
    /// <exception cref="ArgumentNullException">When solutionPaths is null.</exception>
    /// <exception cref="ArgumentException">When solutionPaths is empty or contains null/invalid paths.</exception>
    /// <exception cref="SolutionLoadException">When all solutions fail to load.</exception>
    public async Task<IReadOnlyList<SolutionAnalysis>> LoadAllAsync(
        IEnumerable<string> solutionPaths,
        IProgress<SolutionLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solutionPaths);

        var paths = solutionPaths.ToList();
        if (paths.Count == 0)
            throw new ArgumentException("No solution paths provided", nameof(solutionPaths));

        if (paths.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Solution paths cannot be null or empty", nameof(solutionPaths));

        // Validate all paths exist before starting
        var missingPaths = paths.Where(p => !File.Exists(p)).ToList();
        if (missingPaths.Any())
        {
            throw new ArgumentException(
                $"Solution files not found: {string.Join(", ", missingPaths.Select(Path.GetFileName))}",
                nameof(solutionPaths));
        }

        _logger.LogInformation("Loading {SolutionCount} solutions", paths.Count);

        var results = new List<SolutionAnalysis>();
        var errors = new List<string>();

        for (int i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            var fileName = Path.GetFileName(path);

            progress?.Report(new SolutionLoadProgress
            {
                CurrentIndex = i,
                TotalCount = paths.Count,
                CurrentFileName = fileName,
                IsComplete = false
            });

            try
            {
                var sw = Stopwatch.StartNew();
                var analysis = await _solutionLoader.LoadAsync(path, cancellationToken)
                    .ConfigureAwait(false);
                sw.Stop();

                results.Add(analysis);

                _logger.LogInformation(
                    "Loaded {FileName} ({ProjectCount} projects, {ElapsedMs}ms)",
                    fileName,
                    analysis.Projects.Count,
                    sw.ElapsedMilliseconds);

                progress?.Report(new SolutionLoadProgress
                {
                    CurrentIndex = i,
                    TotalCount = paths.Count,
                    CurrentFileName = fileName,
                    ProjectCount = analysis.Projects.Count,
                    ElapsedTime = sw.Elapsed,
                    IsComplete = true
                });
            }
            catch (Exception ex) when (ex is SolutionLoadException || ex is IOException)
            {
                var errorMsg = $"{fileName}: {ex.Message}";
                errors.Add(errorMsg);

                _logger.LogError(ex, "Failed to load solution {FileName}", fileName);

                progress?.Report(new SolutionLoadProgress
                {
                    CurrentIndex = i,
                    TotalCount = paths.Count,
                    CurrentFileName = fileName,
                    ErrorMessage = errorMsg,
                    IsComplete = true
                });

                // Continue with remaining solutions (graceful degradation)
            }
        }

        // Check if any solutions loaded successfully
        if (results.Count == 0)
        {
            var allErrors = string.Join("\n", errors);
            throw new SolutionLoadException(
                $"Failed to load all {paths.Count} solutions:\n{allErrors}");
        }

        // Log summary
        if (errors.Any())
        {
            _logger.LogWarning(
                "Loaded {SuccessCount} of {TotalCount} solutions ({FailCount} failed)",
                results.Count,
                paths.Count,
                errors.Count);
        }
        else
        {
            _logger.LogInformation(
                "Successfully loaded all {SolutionCount} solutions",
                paths.Count);
        }

        return results;
    }
}
