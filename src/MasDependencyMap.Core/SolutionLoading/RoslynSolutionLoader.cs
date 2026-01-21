using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Loads solutions using Roslyn semantic analysis.
/// Full implementation deferred to Epic 2 Story 2-1.
/// </summary>
public class RoslynSolutionLoader : ISolutionLoader
{
    private readonly ILogger<RoslynSolutionLoader> _logger;

    public RoslynSolutionLoader(ILogger<RoslynSolutionLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads a solution file and analyzes project dependencies.
    /// This is a stub implementation that will be completed in Epic 2.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file</param>
    /// <returns>Solution analysis with project dependency information</returns>
    /// <exception cref="NotImplementedException">Always thrown - stub implementation deferred to Epic 2 Story 2-1</exception>
    public Task<SolutionAnalysis> LoadAsync(string solutionPath)
    {
        _logger.LogInformation("Attempting to load solution from {SolutionPath}", solutionPath);
        _logger.LogWarning("RoslynSolutionLoader is a stub implementation");
        throw new NotImplementedException(
            "Solution loading will be implemented in Epic 2 Story 2-1. " +
            "This is a stub for DI container setup in Epic 1.");
    }
}
