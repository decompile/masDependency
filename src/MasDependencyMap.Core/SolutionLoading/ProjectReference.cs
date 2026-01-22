namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Represents a dependency from one project to another project or DLL.
/// Distinguishes between project-to-project references and external assembly references.
/// </summary>
public class ProjectReference
{
    /// <summary>
    /// Target project or assembly name.
    /// For ProjectReferences: project name (e.g., "Core")
    /// For AssemblyReferences: DLL name without extension (e.g., "System.Text.Json")
    /// </summary>
    public string TargetName { get; init; } = string.Empty;

    /// <summary>
    /// Type of reference (ProjectReference or AssemblyReference).
    /// Determines whether this is an internal dependency or external library.
    /// </summary>
    public ReferenceType Type { get; init; }

    /// <summary>
    /// Full path to referenced item.
    /// For ProjectReferences: absolute path to .csproj file
    /// For AssemblyReferences: absolute path to .dll file
    /// May be null if path cannot be resolved.
    /// </summary>
    public string? TargetPath { get; init; }
}

/// <summary>
/// Distinguishes between project-to-project references and external assembly references.
/// Used for filtering framework dependencies and building dependency graphs.
/// </summary>
public enum ReferenceType
{
    /// <summary>
    /// Project-to-project reference (ProjectReference in .csproj).
    /// These are internal dependencies within the solution.
    /// Example: Services project referencing Core project
    /// </summary>
    ProjectReference,

    /// <summary>
    /// DLL/assembly reference (Reference or PackageReference in .csproj).
    /// These are external dependencies (NuGet packages, framework DLLs, third-party libraries).
    /// Example: Microsoft.Extensions.Logging, System.Text.Json
    /// </summary>
    AssemblyReference
}
