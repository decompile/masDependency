using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Loads .NET solutions using Roslyn semantic analysis.
/// Provides full semantic information including target frameworks, languages, and references.
/// First loader in 3-layer fallback chain (Roslyn → MSBuild → ProjectFile).
/// Requires MSBuildLocator.RegisterDefaults() to be called before first use.
/// </summary>
public class RoslynSolutionLoader : ISolutionLoader
{
    private readonly ILogger<RoslynSolutionLoader> _logger;

    public RoslynSolutionLoader(ILogger<RoslynSolutionLoader> logger)
    {
        // Trust DI container to provide non-null logger
        _logger = logger;
    }

    /// <summary>
    /// Checks if the loader can handle the given solution file.
    /// Verifies file exists and has .sln extension.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file</param>
    /// <returns>True if file exists and is a .sln file, false otherwise</returns>
    public bool CanLoad(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            _logger.LogDebug("CanLoad returned false: solution path is null or empty");
            return false;
        }

        if (!File.Exists(solutionPath))
        {
            _logger.LogDebug("CanLoad returned false: file does not exist at {SolutionPath}", solutionPath);
            return false;
        }

        var hasSlnExtension = Path.GetExtension(solutionPath).Equals(".sln", StringComparison.OrdinalIgnoreCase);
        if (!hasSlnExtension)
        {
            _logger.LogDebug("CanLoad returned false: file does not have .sln extension at {SolutionPath}", solutionPath);
        }

        return hasSlnExtension;
    }

    /// <summary>
    /// Loads solution and extracts project dependency graph using Roslyn semantic analysis.
    /// Supports .NET Framework 3.5+ through .NET 8+ projects (20-year version span).
    /// Handles mixed C#/VB.NET solutions.
    /// Supports cancellation for long-running solution loading operations.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file</param>
    /// <param name="cancellationToken">Cancellation token to abort long-running operations</param>
    /// <returns>Complete solution analysis with all projects and dependencies</returns>
    /// <exception cref="RoslynLoadException">When Roslyn fails to load the solution</exception>
    /// <exception cref="OperationCanceledException">When operation is cancelled via token</exception>
    public async Task<SolutionAnalysis> LoadAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading solution using Roslyn: {SolutionPath}", solutionPath);

        try
        {
            // CRITICAL: MSBuildLocator.RegisterDefaults() must be called in Main() BEFORE this
            // Failure to do so causes cryptic assembly loading errors
            using var workspace = MSBuildWorkspace.Create();

            // Subscribe to diagnostics for debugging and logging warnings
            // Store handler to ensure proper cleanup and prevent memory leaks
            #pragma warning disable CS0618 // WorkspaceFailed is obsolete - see note below
            EventHandler<Microsoft.CodeAnalysis.WorkspaceDiagnosticEventArgs> workspaceFailedHandler = (sender, args) =>
            {
                _logger.LogWarning("Workspace diagnostic: {Diagnostic}", args.Diagnostic.Message);
            };
            workspace.WorkspaceFailed += workspaceFailedHandler;
            #pragma warning restore CS0618
            // NOTE: WorkspaceFailed is marked obsolete in Roslyn but there's no direct replacement
            // for capturing workspace-level diagnostics. The recommended approach is to check
            // Solution.GetDiagnostics() after loading, but that doesn't capture load-time issues.
            // We'll continue using this until Roslyn provides a non-obsolete alternative.

            try
            {
                // Load solution using Roslyn MSBuildWorkspace with cancellation support
                var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

                _logger.LogInformation("Solution loaded successfully, extracting {ProjectCount} projects", solution.Projects.Count());

                // Extract all projects and their dependencies
                var projects = new List<ProjectInfo>();
                foreach (var project in solution.Projects)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var projectInfo = await ExtractProjectInfoAsync(project, cancellationToken);
                    projects.Add(projectInfo);
                    _logger.LogInformation("Extracted project: {ProjectName} ({Language}, {TargetFramework})",
                        projectInfo.Name, projectInfo.Language, projectInfo.TargetFramework);
                }

                return new SolutionAnalysis
                {
                    SolutionPath = Path.GetFullPath(solutionPath),
                    SolutionName = Path.GetFileNameWithoutExtension(solutionPath),
                    Projects = projects,
                    LoaderType = "Roslyn"
                };
            }
            finally
            {
                // Unsubscribe to prevent memory leaks
                #pragma warning disable CS0618
                workspace.WorkspaceFailed -= workspaceFailedHandler;
                #pragma warning restore CS0618
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Roslyn failed to load solution: {SolutionPath}", solutionPath);
            throw new RoslynLoadException($"Failed to load solution at {solutionPath}", ex);
        }
    }

    /// <summary>
    /// Extracts complete project information including metadata and all references.
    /// Handles both modern .NET and legacy .NET Framework projects.
    /// </summary>
    /// <param name="project">Roslyn Project instance from workspace</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>ProjectInfo with all metadata and references</returns>
    private async Task<ProjectInfo> ExtractProjectInfoAsync(Project project, CancellationToken cancellationToken)
    {
        // Extract target framework (handles legacy and modern formats)
        var targetFramework = await ExtractTargetFrameworkAsync(project, cancellationToken);

        // Extract all references (project-to-project and assembly/DLL references)
        var references = ExtractReferences(project);

        return new ProjectInfo
        {
            Name = project.Name,
            FilePath = project.FilePath ?? string.Empty,
            TargetFramework = targetFramework,
            Language = project.Language, // "C#", "Visual Basic", "F#", etc.
            References = references
        };
    }

    /// <summary>
    /// Extracts all references from a project, differentiating project references from assembly references.
    /// Includes both ProjectReferences (project-to-project) and MetadataReferences (DLLs/assemblies).
    /// </summary>
    /// <param name="project">Roslyn Project instance</param>
    /// <returns>List of all references with type differentiation</returns>
    private List<ProjectReference> ExtractReferences(Project project)
    {
        var references = new List<ProjectReference>();

        // Extract project-to-project references
        foreach (var projectRef in project.ProjectReferences)
        {
            var targetProject = project.Solution.GetProject(projectRef.ProjectId);
            if (targetProject != null)
            {
                references.Add(new ProjectReference
                {
                    TargetName = targetProject.Name,
                    Type = ReferenceType.ProjectReference,
                    TargetPath = targetProject.FilePath
                });

                _logger.LogDebug("Project reference: {SourceProject} → {TargetProject}",
                    project.Name, targetProject.Name);
            }
        }

        // Extract assembly/DLL references (NuGet packages, third-party libraries)
        // Filter out framework assemblies (System.*, Microsoft.*, etc.) to reduce noise
        foreach (var metadataRef in project.MetadataReferences)
        {
            if (metadataRef is PortableExecutableReference portableRef && !string.IsNullOrEmpty(portableRef.FilePath))
            {
                var assemblyName = Path.GetFileNameWithoutExtension(portableRef.FilePath);

                // Skip framework assemblies that are noise in dependency analysis
                if (IsFrameworkAssembly(assemblyName))
                {
                    continue;
                }

                references.Add(new ProjectReference
                {
                    TargetName = assemblyName,
                    Type = ReferenceType.AssemblyReference,
                    TargetPath = portableRef.FilePath
                });
            }
        }

        _logger.LogDebug("Extracted {ReferenceCount} total references for project {ProjectName}",
            references.Count, project.Name);

        return references;
    }

    /// <summary>
    /// Extracts target framework from Roslyn project.
    /// Handles .NET Framework 3.5+ through .NET 8+ (20-year version span).
    /// Parses project file XML to extract TargetFramework element.
    /// Returns "unknown" if target framework cannot be determined (legacy projects).
    /// </summary>
    /// <param name="project">Roslyn Project instance</param>
    /// <param name="cancellationToken">Cancellation token for async file read</param>
    /// <returns>Target framework moniker (e.g., "net8.0", "net472") or "unknown"</returns>
    private async Task<string> ExtractTargetFrameworkAsync(Project project, CancellationToken cancellationToken)
    {
        // Parse project file XML to extract <TargetFramework> or <TargetFrameworks>
        if (string.IsNullOrEmpty(project.FilePath) || !File.Exists(project.FilePath))
        {
            _logger.LogDebug("Cannot extract target framework: project file path is null or missing for {ProjectName}", project.Name);
            return "unknown";
        }

        try
        {
            var projectXml = await File.ReadAllTextAsync(project.FilePath, cancellationToken);

            // Parse XML to find TargetFramework or TargetFrameworks element
            var targetFramework = ExtractTargetFrameworkFromXml(projectXml);

            if (!string.IsNullOrEmpty(targetFramework))
            {
                _logger.LogDebug("Extracted target framework: {TargetFramework} for project {ProjectName}",
                    targetFramework, project.Name);
                return targetFramework;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse project file for target framework: {ProjectFile}", project.FilePath);
        }

        _logger.LogDebug("Target framework could not be determined for project {ProjectName}, using 'unknown'", project.Name);
        return "unknown";
    }

    /// <summary>
    /// Extracts target framework from project file XML content.
    /// Handles both SDK-style projects (TargetFramework) and legacy projects (TargetFrameworkVersion).
    /// </summary>
    /// <param name="projectXml">Raw XML content of .csproj file</param>
    /// <returns>Target framework moniker or empty string if not found</returns>
    private string ExtractTargetFrameworkFromXml(string projectXml)
    {
        // SDK-style projects: <TargetFramework>net8.0</TargetFramework>
        var targetFrameworkMatch = System.Text.RegularExpressions.Regex.Match(
            projectXml,
            @"<TargetFramework>([^<]+)</TargetFramework>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (targetFrameworkMatch.Success)
        {
            return targetFrameworkMatch.Groups[1].Value.Trim();
        }

        // Multi-targeting: <TargetFrameworks>net8.0;net6.0</TargetFrameworks>
        var targetFrameworksMatch = System.Text.RegularExpressions.Regex.Match(
            projectXml,
            @"<TargetFrameworks>([^<]+)</TargetFrameworks>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (targetFrameworksMatch.Success)
        {
            var frameworks = targetFrameworksMatch.Groups[1].Value.Trim();
            // Return first framework for multi-targeting projects
            var firstFramework = frameworks.Split(';')[0].Trim();
            return firstFramework;
        }

        // Legacy .NET Framework projects: <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
        var legacyMatch = System.Text.RegularExpressions.Regex.Match(
            projectXml,
            @"<TargetFrameworkVersion>v?([^<]+)</TargetFrameworkVersion>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (legacyMatch.Success)
        {
            var version = legacyMatch.Groups[1].Value.Trim();
            // Convert v4.7.2 to net472 format
            return ConvertLegacyFrameworkVersion(version);
        }

        return string.Empty;
    }

    /// <summary>
    /// Converts legacy .NET Framework version (v4.7.2) to modern TFM format (net472).
    /// </summary>
    /// <param name="version">Legacy version string like "4.7.2" or "v4.7.2"</param>
    /// <returns>Modern TFM like "net472"</returns>
    private string ConvertLegacyFrameworkVersion(string version)
    {
        // Remove 'v' prefix if present
        version = version.TrimStart('v', 'V');

        // Remove dots: 4.7.2 -> 472, 3.5 -> 35
        var versionNumber = version.Replace(".", "");

        return $"net{versionNumber}";
    }

    /// <summary>
    /// Determines if an assembly is a framework assembly that should be filtered out.
    /// Framework assemblies are noise in dependency analysis and should be excluded.
    /// Story 2.6 will implement configurable filtering; this is basic filtering for now.
    /// </summary>
    /// <param name="assemblyName">Assembly name without extension</param>
    /// <returns>True if assembly is a framework assembly, false otherwise</returns>
    private bool IsFrameworkAssembly(string assemblyName)
    {
        // Filter common framework assemblies (identical logic to MSBuildSolutionLoader)
        return assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("Windows", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("WindowsBase", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase);
    }
}

