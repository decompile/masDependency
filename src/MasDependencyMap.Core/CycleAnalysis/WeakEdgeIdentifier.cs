namespace MasDependencyMap.Core.CycleAnalysis;

using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.DependencyAnalysis;
using QuikGraph;

/// <summary>
/// Identifies weakest coupling edges within circular dependency cycles.
/// For each cycle, finds all edges with the minimum coupling score to help
/// architects determine which dependencies are easiest to break.
/// </summary>
public sealed class WeakEdgeIdentifier : IWeakEdgeIdentifier
{
    private readonly ILogger<WeakEdgeIdentifier> _logger;

    public WeakEdgeIdentifier(ILogger<WeakEdgeIdentifier> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IReadOnlyList<CycleInfo> IdentifyWeakEdges(
        IReadOnlyList<CycleInfo> cycles,
        AdjacencyGraph<ProjectNode, DependencyEdge> graph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cycles);
        ArgumentNullException.ThrowIfNull(graph);

        if (cycles.Count == 0)
        {
            _logger.LogInformation("No cycles to analyze for weak coupling edges");
            return cycles;
        }

        _logger.LogInformation("Analyzing {CycleCount} cycles for weak coupling edges", cycles.Count);

        foreach (var cycle in cycles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var edgesInCycle = GetEdgesInCycle(cycle, graph);

            if (edgesInCycle.Count == 0)
            {
                _logger.LogWarning(
                    "Cycle {CycleId} with {ProjectCount} projects has no edges - skipping weak edge analysis",
                    cycle.CycleId,
                    cycle.CycleSize);
                continue;
            }

            // Find minimum coupling score
            var minCouplingScore = edgesInCycle.Min(e => e.CouplingScore);

            // Find ALL edges with that minimum score (handles ties)
            var weakEdges = edgesInCycle
                .Where(e => e.CouplingScore == minCouplingScore)
                .ToList();

            // Populate CycleInfo
            cycle.WeakCouplingEdges = weakEdges;
            cycle.WeakCouplingScore = minCouplingScore;

            _logger.LogDebug(
                "Cycle {CycleId}: {EdgeCount} edges, min coupling = {MinScore}, {WeakEdgeCount} weak edges flagged",
                cycle.CycleId,
                edgesInCycle.Count,
                minCouplingScore,
                weakEdges.Count);
        }

        // Summary logging
        var totalWeakEdges = cycles.Sum(c => c.WeakCouplingEdges.Count);
        var avgWeakEdgesPerCycle = (double)totalWeakEdges / cycles.Count;

        _logger.LogInformation(
            "Identified {WeakEdgeCount} weak coupling edges across {CycleCount} cycles (avg {AvgPerCycle:F1} per cycle)",
            totalWeakEdges,
            cycles.Count,
            avgWeakEdgesPerCycle);

        return cycles;
    }

    /// <summary>
    /// Finds all dependency edges within a cycle.
    /// An edge is "in the cycle" if both source and target projects are cycle members.
    /// </summary>
    private IReadOnlyList<DependencyEdge> GetEdgesInCycle(
        CycleInfo cycle,
        AdjacencyGraph<ProjectNode, DependencyEdge> graph)
    {
        // Create HashSet of cycle members for O(1) lookup
        var cycleMembers = new HashSet<ProjectNode>(cycle.Projects);

        // Find all edges where BOTH source and target are in the cycle
        var edgesInCycle = graph.Edges
            .Where(edge => cycleMembers.Contains(edge.Source) &&
                           cycleMembers.Contains(edge.Target))
            .ToList();

        return edgesInCycle;
    }
}
