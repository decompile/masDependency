namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Represents cyclomatic complexity metrics for a single project.
/// Complexity quantifies code complexity based on decision points (if, loops, switch, operators).
/// Higher complexity indicates harder extraction (more intricate logic to understand and refactor).
/// </summary>
/// <param name="ProjectName">Name of the project being analyzed.</param>
/// <param name="ProjectPath">Absolute path to the project file (.csproj).</param>
/// <param name="MethodCount">Total number of methods analyzed in the project.</param>
/// <param name="TotalComplexity">Sum of cyclomatic complexity across all methods.</param>
/// <param name="AverageComplexity">Average cyclomatic complexity per method (TotalComplexity / MethodCount).</param>
/// <param name="NormalizedScore">Complexity score normalized to 0-100 scale using industry thresholds. 0 = simple code (easy to extract), 100 = very complex code (hard to extract).</param>
public sealed record ComplexityMetric(
    string ProjectName,
    string ProjectPath,
    int MethodCount,
    int TotalComplexity,
    double AverageComplexity,
    double NormalizedScore);
