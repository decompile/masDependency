namespace MasDependencyMap.Core.Filtering;

using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Filters framework dependencies from dependency graphs using configurable patterns.
/// Removes edges to Microsoft.*, System.*, and other framework references to focus on custom code architecture.
/// </summary>
/// <remarks>
/// The filter uses blocklist and allowlist patterns loaded from configuration:
/// - Blocklist patterns (e.g., "Microsoft.*", "System.*") identify framework dependencies to remove
/// - Allowlist patterns (e.g., "YourCompany.*") override the blocklist to retain specific dependencies
/// - Wildcard matching is case-insensitive and supports "*" at the end of patterns
/// </remarks>
/// <example>
/// Example usage:
/// <code>
/// var filter = serviceProvider.GetRequiredService&lt;IFrameworkFilter&gt;();
/// var filteredGraph = await filter.FilterAsync(originalGraph, cancellationToken);
/// // filteredGraph now contains only custom code dependencies
/// </code>
/// </example>
public interface IFrameworkFilter
{
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
    /// <remarks>
    /// Filtering logic:
    /// 1. AllowList patterns are checked first (take precedence)
    /// 2. BlockList patterns are checked second
    /// 3. If neither matches, the edge is retained (default: keep)
    ///
    /// Statistics about filtered vs. retained edges are logged at Information level.
    /// </remarks>
    Task<DependencyGraph> FilterAsync(DependencyGraph graph, CancellationToken cancellationToken = default);
}
