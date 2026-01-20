using System.ComponentModel.DataAnnotations;

namespace MasDependencyMap.Core.Configuration;

/// <summary>
/// Configuration for scoring weights used in extraction difficulty analysis.
/// All weights must be in the range [0.0, 1.0] and sum to 1.0.
/// </summary>
public sealed class ScoringConfiguration
{
    /// <summary>
    /// Weight for coupling strength metric (default: 0.40).
    /// Higher coupling indicates more dependencies to refactor.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Coupling weight must be between 0.0 and 1.0")]
    public double Coupling { get; set; } = 0.40;

    /// <summary>
    /// Weight for cyclomatic complexity metric (default: 0.30).
    /// Higher complexity indicates more difficult refactoring.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Complexity weight must be between 0.0 and 1.0")]
    public double Complexity { get; set; } = 0.30;

    /// <summary>
    /// Weight for technology debt metric (default: 0.20).
    /// Measures outdated frameworks and library versions.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "TechDebt weight must be between 0.0 and 1.0")]
    public double TechDebt { get; set; } = 0.20;

    /// <summary>
    /// Weight for external API exposure metric (default: 0.10).
    /// Measures public surface area that needs refactoring.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "ExternalExposure weight must be between 0.0 and 1.0")]
    public double ExternalExposure { get; set; } = 0.10;
}
