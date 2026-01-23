namespace MasDependencyMap.Core.Filtering;

using MasDependencyMap.Core.Configuration;
using MasDependencyMap.Core.DependencyAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Filters framework dependencies from dependency graphs using configurable blocklist and allowlist patterns.
/// Removes edges to Microsoft.*, System.*, and other framework references to focus on custom code architecture.
/// </summary>
/// <remarks>
/// The filter operates on edges, not vertices:
/// - All project nodes (vertices) are retained in the filtered graph
/// - Only dependency edges to blocked projects are removed
/// - Wildcard patterns support "*" at the end (e.g., "Microsoft.*" matches "Microsoft.Extensions.Logging")
/// - Exact patterns match the entire project name (e.g., "mscorlib" matches only "mscorlib")
/// - Matching is case-insensitive for robustness
/// - AllowList takes precedence over BlockList
/// </remarks>
public class FrameworkFilter : IFrameworkFilter
{
    private readonly FilterConfiguration _configuration;
    private readonly ILogger<FrameworkFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkFilter"/> class.
    /// </summary>
    /// <param name="configuration">The filter configuration with blocklist and allowlist patterns.</param>
    /// <param name="logger">The logger for filtering statistics and diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> or <paramref name="logger"/> is null.
    /// </exception>
    public FrameworkFilter(
        IOptions<FilterConfiguration> configuration,
        ILogger<FrameworkFilter> logger)
    {
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Filters framework dependencies from the provided dependency graph.
    /// Creates a new graph with framework dependencies removed based on configured patterns.
    /// </summary>
    /// <param name="graph">The dependency graph to filter. Must not be null.</param>
    /// <param name="cancellationToken">Cancellation token to support async operations.</param>
    /// <returns>
    /// A new <see cref="DependencyGraph"/> with framework dependencies filtered out.
    /// All vertices are retained; only edges to blocked projects are removed.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="graph"/> is null.</exception>
    public Task<DependencyGraph> FilterAsync(
        DependencyGraph graph,
        CancellationToken cancellationToken = default)
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));

        var originalEdgeCount = graph.EdgeCount;
        var filteredGraph = new DependencyGraph();

        // Add all vertices (filtering removes edges, not vertices)
        foreach (var vertex in graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            filteredGraph.AddVertex(vertex);
        }

        // Filter edges based on BlockList/AllowList
        var blockedCount = 0;
        var retainedCount = 0;

        foreach (var edge in graph.Edges)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if target project is blocked
            if (IsBlocked(edge.Target.ProjectName, _configuration.BlockList, _configuration.AllowList))
            {
                blockedCount++;
                continue; // Skip this edge
            }

            // Retain edge
            filteredGraph.AddEdge(edge);
            retainedCount++;
        }

        // Log statistics (skip logging for empty graphs to reduce noise)
        if (originalEdgeCount > 0)
        {
            var blockedPercent = (blockedCount / (double)originalEdgeCount) * 100;
            var retainedPercent = (retainedCount / (double)originalEdgeCount) * 100;

            _logger.LogInformation(
                "Filtered {BlockedCount} framework refs ({BlockedPercent:F1}%), retained {RetainedCount} custom refs ({RetainedPercent:F1}%)",
                blockedCount,
                blockedPercent,
                retainedCount,
                retainedPercent);
        }

        return Task.FromResult(filteredGraph);
    }

    /// <summary>
    /// Determines if a project name is blocked based on blocklist and allowlist patterns.
    /// </summary>
    /// <param name="projectName">The project name to check.</param>
    /// <param name="blockList">The list of blocklist patterns.</param>
    /// <param name="allowList">The list of allowlist patterns.</param>
    /// <returns>
    /// <c>true</c> if the project is blocked; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Logic:
    /// 1. Check allowlist first (takes precedence) - if matches, return false (not blocked)
    /// 2. Check blocklist second - if matches, return true (blocked)
    /// 3. Default: return false (retain - not in blocklist or allowlist)
    /// </remarks>
    private bool IsBlocked(string projectName, List<string> blockList, List<string> allowList)
    {
        // Step 1: Check AllowList first (takes precedence)
        // Filter out null patterns to prevent NullReferenceException
        if (allowList != null && allowList.Where(p => p != null).Any(pattern => MatchesPattern(projectName, pattern)))
        {
            return false; // Explicitly allowed
        }

        // Step 2: Check BlockList
        // Filter out null patterns to prevent NullReferenceException
        if (blockList != null && blockList.Where(p => p != null).Any(pattern => MatchesPattern(projectName, pattern)))
        {
            return true; // Blocked
        }

        // Step 3: Default - retain (not in BlockList or AllowList)
        return false;
    }

    /// <summary>
    /// Matches a project name against a pattern with wildcard support.
    /// </summary>
    /// <param name="name">The project name to match.</param>
    /// <param name="pattern">The pattern to match against (supports "*" wildcard at end).</param>
    /// <returns>
    /// <c>true</c> if the name matches the pattern; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Pattern matching:
    /// - Wildcard patterns (e.g., "Microsoft.*") match any project starting with "Microsoft."
    /// - Exact patterns (e.g., "mscorlib") match only the exact project name
    /// - Matching is case-insensitive
    /// </remarks>
    private static bool MatchesPattern(string name, string pattern)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pattern))
            return false;

        // Wildcard pattern (e.g., "Microsoft.*")
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // Exact match (e.g., "mscorlib")
        return name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
