using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Builds QuikGraph dependency graph from solution analysis.
/// Full implementation deferred to Epic 2 Story 2-5.
/// </summary>
public class DependencyGraphBuilder : IDependencyGraphBuilder
{
    private readonly ILogger<DependencyGraphBuilder> _logger;

    public DependencyGraphBuilder(ILogger<DependencyGraphBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds a dependency graph from solution analysis.
    /// This is a stub implementation that will be completed in Epic 2.
    /// </summary>
    /// <param name="solutionAnalysis">Solution analysis result from ISolutionLoader</param>
    /// <returns>Dependency graph suitable for cycle detection and visualization</returns>
    /// <exception cref="NotImplementedException">Always thrown - stub implementation deferred to Epic 2 Story 2-5</exception>
    public Task<object> BuildGraphAsync(object solutionAnalysis)
    {
        _logger.LogWarning("DependencyGraphBuilder.BuildGraphAsync is a stub implementation");
        throw new NotImplementedException(
            "Dependency graph building will be implemented in Epic 2 Story 2-5. " +
            "This is a stub for DI container setup in Epic 1.");
    }
}
