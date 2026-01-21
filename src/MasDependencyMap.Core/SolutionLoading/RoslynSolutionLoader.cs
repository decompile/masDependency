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

    public Task<SolutionAnalysis> LoadAsync(string solutionPath)
    {
        _logger.LogWarning("RoslynSolutionLoader is a stub implementation");
        throw new NotImplementedException(
            "Solution loading will be implemented in Epic 2 Story 2-1. " +
            "This is a stub for DI container setup in Epic 1.");
    }
}
