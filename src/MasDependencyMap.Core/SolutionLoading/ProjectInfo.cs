namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Metadata and dependencies for a single project in a solution.
/// Contains project identification, target framework, and all references.
/// </summary>
public class ProjectInfo
{
    /// <summary>
    /// Project name (without file extension).
    /// Example: "MasDependencyMap.Core" from "MasDependencyMap.Core.csproj"
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Absolute path to .csproj or .vbproj file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Target framework moniker (e.g., "net8.0", "net472", "netstandard2.0").
    /// May be "unknown" if target framework cannot be determined (legacy projects).
    /// Handles .NET Framework 3.5+ through .NET 8+ (20-year span).
    /// </summary>
    public string TargetFramework { get; init; } = "unknown";

    /// <summary>
    /// Programming language (C#, Visual Basic, F#, etc.).
    /// Extracted from Roslyn Project.Language property.
    /// </summary>
    public string Language { get; init; } = string.Empty;

    /// <summary>
    /// All references (project and DLL) from this project.
    /// Includes both ProjectReferences (project-to-project) and AssemblyReferences (DLLs).
    /// </summary>
    public IReadOnlyList<ProjectReference> References { get; init; } = Array.Empty<ProjectReference>();
}
