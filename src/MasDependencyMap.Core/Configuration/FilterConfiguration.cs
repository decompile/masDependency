using System.Collections.Generic;

namespace MasDependencyMap.Core.Configuration;

/// <summary>
/// Configuration for filtering framework dependencies.
/// Provides default blocklist/allowlist patterns when config file is missing.
/// </summary>
public sealed class FilterConfiguration
{
    /// <summary>
    /// List of namespace patterns to block from dependency analysis.
    /// Supports wildcard patterns (e.g., "Microsoft.*").
    /// </summary>
    public List<string> BlockList { get; set; } = new()
    {
        "Microsoft.*",
        "System.*",
        "mscorlib",
        "netstandard"
    };

    /// <summary>
    /// List of namespace patterns to allow even if they match the blocklist.
    /// Allowlist takes precedence over blocklist.
    /// </summary>
    public List<string> AllowList { get; set; } = new();
}
