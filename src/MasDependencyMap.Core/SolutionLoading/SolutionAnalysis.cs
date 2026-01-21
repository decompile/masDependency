using System.Collections.Generic;

namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Represents the result of solution analysis.
/// Will be expanded in Epic 2 with full project dependency information.
/// </summary>
public class SolutionAnalysis
{
    /// <summary>
    /// List of projects found in the solution.
    /// Minimal structure for MVP DI setup.
    /// </summary>
    public List<string> ProjectNames { get; set; } = new();
}
