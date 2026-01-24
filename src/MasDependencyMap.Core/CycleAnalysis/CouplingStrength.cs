namespace MasDependencyMap.Core.CycleAnalysis;

/// <summary>
/// Classification of coupling strength based on method call count thresholds.
/// Used to categorize dependency edges by the intensity of coupling.
/// </summary>
public enum CouplingStrength
{
    /// <summary>
    /// Weak coupling: 1-5 method calls.
    /// Indicates minimal interaction between projects.
    /// </summary>
    Weak = 0,

    /// <summary>
    /// Medium coupling: 6-20 method calls.
    /// Indicates moderate interaction between projects.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Strong coupling: 21+ method calls.
    /// Indicates intensive interaction between projects that may be difficult to break.
    /// </summary>
    Strong = 2
}
