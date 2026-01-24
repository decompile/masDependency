namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Classifies coupling strength based on method call count thresholds.
/// Provides consistent classification logic for dependency edges.
/// </summary>
public static class CouplingClassifier
{
    private const int WeakCouplingMaxCalls = 5;
    private const int MediumCouplingMaxCalls = 20;

    /// <summary>
    /// Classifies coupling strength based on the number of method calls.
    /// </summary>
    /// <param name="methodCallCount">Number of method calls from source to target project.</param>
    /// <returns>
    /// CouplingStrength classification:
    /// - Weak: 1-5 calls
    /// - Medium: 6-20 calls
    /// - Strong: 21+ calls
    /// </returns>
    public static CouplingStrength ClassifyCouplingStrength(int methodCallCount)
    {
        return methodCallCount switch
        {
            <= WeakCouplingMaxCalls => CouplingStrength.Weak,
            <= MediumCouplingMaxCalls => CouplingStrength.Medium,
            _ => CouplingStrength.Strong
        };
    }
}
