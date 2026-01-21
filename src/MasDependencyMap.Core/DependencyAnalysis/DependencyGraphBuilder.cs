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

    public Task<object> BuildGraphAsync(object solutionAnalysis)
    {
        _logger.LogWarning("DependencyGraphBuilder.BuildGraphAsync is a stub implementation");
        throw new NotImplementedException(
            "Dependency graph building will be implemented in Epic 2 Story 2-5. " +
            "This is a stub for DI container setup in Epic 1.");
    }
}
