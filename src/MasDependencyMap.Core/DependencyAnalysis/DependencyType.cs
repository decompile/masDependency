namespace MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Represents the type of dependency between two projects.
/// </summary>
public enum DependencyType
{
    /// <summary>
    /// A project-to-project reference (e.g., &lt;ProjectReference Include="..." /&gt;).
    /// This is the primary type of dependency in solution analysis.
    /// </summary>
    ProjectReference,

    /// <summary>
    /// A binary or assembly reference (e.g., &lt;Reference Include="..." /&gt; or &lt;PackageReference /&gt;).
    /// Used for DLL references and NuGet packages.
    /// </summary>
    BinaryReference
}
