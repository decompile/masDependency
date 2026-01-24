namespace MasDependencyMap.Core.CycleAnalysis;

using Microsoft.Extensions.Logging;
using QuikGraph;
using QuikGraph.Algorithms;
using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Detects circular dependency cycles using Tarjan's strongly connected components algorithm
/// via QuikGraph's StronglyConnectedComponentsAlgorithm.
/// </summary>
public class TarjanCycleDetector : ITarjanCycleDetector
{
    private readonly ILogger<TarjanCycleDetector> _logger;

    public TarjanCycleDetector(ILogger<TarjanCycleDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CycleInfo>> DetectCyclesAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.VertexCount == 0)
        {
            _logger.LogInformation("Empty graph provided, no cycles to detect");
            return Array.Empty<CycleInfo>();
        }

        try
        {
            _logger.LogInformation(
                "Detecting circular dependencies in {ProjectCount} projects",
                graph.VertexCount);

            // Check cancellation before expensive operation
            cancellationToken.ThrowIfCancellationRequested();

            // Use QuikGraph's built-in Tarjan algorithm via extension method
            // Run in Task.Run to enable cancellation support for CPU-bound work
            var cycles = await Task.Run(() =>
            {
                var components = new Dictionary<ProjectNode, int>();
                var underlyingGraph = graph.GetUnderlyingGraph();
                var componentCount = underlyingGraph.StronglyConnectedComponents(components);

                var cycleList = new List<CycleInfo>();
                int cycleId = 1;

                // Group by component index to get each SCC
                var componentGroups = components
                    .GroupBy(kvp => kvp.Value)
                    .Select(g => g.Select(kvp => kvp.Key).ToList())
                    .Where(component => component.Count > 1) // Filter to multi-node SCCs only
                    .ToList();

                // Check cancellation before processing results
                cancellationToken.ThrowIfCancellationRequested();

                // Create CycleInfo for each cycle
                foreach (var component in componentGroups)
                {
                    cycleList.Add(new CycleInfo(cycleId++, component));
                }

                return cycleList;
            }, cancellationToken).ConfigureAwait(false);

            // Log results
            if (cycles.Count > 0)
            {
                var stats = CalculateStatistics(cycles, graph.VertexCount);

                _logger.LogInformation(
                    "Found {CycleCount} circular dependency chains, {ProjectsInCycles} projects ({ParticipationRate:F1}%) involved in cycles, largest cycle: {LargestCycleSize} projects",
                    stats.totalCycles,
                    stats.projectsInCycles,
                    stats.participationRate,
                    stats.largestCycle);
            }
            else
            {
                _logger.LogInformation("No circular dependencies detected");
            }

            return cycles;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Cycle detection cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during cycle detection");
            throw;
        }
    }

    private (int totalCycles, int largestCycle, int projectsInCycles, double participationRate) CalculateStatistics(
        IReadOnlyList<CycleInfo> cycles,
        int totalProjectCount)
    {
        if (cycles.Count == 0)
            return (0, 0, 0, 0.0);

        int totalCycles = cycles.Count;
        int largestCycle = cycles.Max(c => c.CycleSize);

        // Count distinct projects across all cycles (project may appear in multiple cycles)
        int projectsInCycles = cycles
            .SelectMany(c => c.Projects)
            .Distinct()
            .Count();

        double participationRate = (projectsInCycles / (double)totalProjectCount) * 100.0;

        return (totalCycles, largestCycle, projectsInCycles, participationRate);
    }
}
