namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents technology version debt metrics for a single project.
/// Tech debt quantifies migration difficulty based on framework age and distance from modern .NET.
/// Older frameworks indicate harder extraction (more breaking changes, legacy patterns, missing modern features).
/// </summary>
/// <param name="ProjectName">Name of the project being analyzed.</param>
/// <param name="ProjectPath">Absolute path to the project file (.csproj or .vbproj).</param>
/// <param name="TargetFramework">Target framework moniker (TFM) of the project (e.g., "net8.0", "net472", "netcoreapp3.1").</param>
/// <param name="NormalizedScore">Tech debt score normalized to 0-100 scale. 0 = modern framework (easy to extract), 100 = very old framework (hard to extract).</param>
public sealed record TechDebtMetric(
    string ProjectName,
    string ProjectPath,
    string TargetFramework,
    double NormalizedScore);
