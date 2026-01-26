namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Configurable weights for combining extraction difficulty metrics.
/// Weights must sum to 1.0 to produce a normalized 0-100 final score.
/// </summary>
/// <param name="CouplingWeight">Weight for coupling metric (default 0.40). Higher values prioritize dependency complexity.</param>
/// <param name="ComplexityWeight">Weight for cyclomatic complexity metric (default 0.30). Higher values prioritize code complexity.</param>
/// <param name="TechDebtWeight">Weight for technology version debt metric (default 0.20). Higher values prioritize framework modernization.</param>
/// <param name="ExternalExposureWeight">Weight for external API exposure metric (default 0.10). Higher values prioritize API contract risks.</param>
public sealed record ScoringWeights(
    double CouplingWeight,
    double ComplexityWeight,
    double TechDebtWeight,
    double ExternalExposureWeight)
{
    /// <summary>
    /// Validates that weights sum to 1.0 (±0.01 tolerance) and each weight is in valid range [0.0, 1.0].
    /// </summary>
    /// <param name="errorMessage">Detailed error message if validation fails.</param>
    /// <returns>True if weights are valid, false otherwise.</returns>
    public bool IsValid(out string errorMessage)
    {
        // Check individual weights are in valid range
        if (CouplingWeight < 0 || CouplingWeight > 1 ||
            ComplexityWeight < 0 || ComplexityWeight > 1 ||
            TechDebtWeight < 0 || TechDebtWeight > 1 ||
            ExternalExposureWeight < 0 || ExternalExposureWeight > 1)
        {
            errorMessage = $"All weights must be between 0.0 and 1.0. " +
                          $"Current: Coupling={CouplingWeight}, Complexity={ComplexityWeight}, " +
                          $"TechDebt={TechDebtWeight}, ExternalExposure={ExternalExposureWeight}";
            return false;
        }

        // Check weights sum to 1.0 (with tolerance for floating point arithmetic)
        var sum = CouplingWeight + ComplexityWeight + TechDebtWeight + ExternalExposureWeight;
        const double tolerance = 0.01;

        if (Math.Abs(sum - 1.0) > tolerance)
        {
            errorMessage = $"Weights must sum to 1.0 (±{tolerance} tolerance). " +
                          $"Current sum: {sum:F3}. " +
                          $"Weights: Coupling={CouplingWeight}, Complexity={ComplexityWeight}, " +
                          $"TechDebt={TechDebtWeight}, ExternalExposure={ExternalExposureWeight}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
