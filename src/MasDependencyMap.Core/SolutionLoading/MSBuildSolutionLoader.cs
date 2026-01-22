namespace MasDependencyMap.Core.SolutionLoading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

/// <summary>
/// Loads .NET solutions using MSBuild project references without full semantic analysis.
/// Fallback loader when RoslynSolutionLoader fails.
/// Falls back to ProjectFileSolutionLoader if MSBuild also fails.
/// </summary>
public class MSBuildSolutionLoader : ISolutionLoader
{
    private readonly ILogger<MSBuildSolutionLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MSBuildSolutionLoader"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    public MSBuildSolutionLoader(ILogger<MSBuildSolutionLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines whether this loader can load the specified solution file.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>True if the file exists and has a .sln extension; otherwise, false.</returns>
    public bool CanLoad(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return false;

        if (!File.Exists(solutionPath))
            return false;

        return Path.GetExtension(solutionPath).Equals(".sln", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads a solution using MSBuild project references without full semantic analysis.
    /// This is a fallback method used when Roslyn semantic analysis fails.
    /// </summary>
    /// <param name="solutionPath">The absolute path to the .sln file.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Complete solution analysis with project dependencies extracted via MSBuild.</returns>
    /// <exception cref="MSBuildLoadException">When MSBuild fails to load the solution.</exception>
    public async Task<SolutionAnalysis> LoadAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Using MSBuild fallback loader for solution: {SolutionPath}", solutionPath);

        try
        {
            // CRITICAL: MSBuildLocator.RegisterDefaults() must be called in Main() BEFORE this
            using var workspace = MSBuildWorkspace.Create();

            // Subscribe to diagnostics for debugging
            // Store handler to ensure proper cleanup and prevent memory leaks
#pragma warning disable CS0618 // WorkspaceFailed is obsolete but no alternative exists yet
            EventHandler<Microsoft.CodeAnalysis.WorkspaceDiagnosticEventArgs> workspaceFailedHandler = (sender, args) =>
            {
                _logger.LogWarning("MSBuild workspace diagnostic: {Diagnostic}", args.Diagnostic.Message);
            };

            workspace.WorkspaceFailed += workspaceFailedHandler;
#pragma warning restore CS0618

            try
            {
                // Load solution without full semantic analysis
                var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

                // Extract projects
                var projects = new List<ProjectInfo>();
                foreach (var project in solution.Projects)
                {
                    _logger.LogInformation("Extracting MSBuild project metadata: {ProjectName}", project.Name);
                    projects.Add(await ExtractProjectInfoAsync(project, cancellationToken));
                }

                _logger.LogInformation("MSBuild successfully loaded solution: {SolutionPath} with {ProjectCount} projects", solutionPath, projects.Count);

                return new SolutionAnalysis
                {
                    SolutionPath = Path.GetFullPath(solutionPath),
                    SolutionName = Path.GetFileNameWithoutExtension(solutionPath),
                    Projects = projects,
                    LoaderType = "MSBuild"
                };
            }
            finally
            {
                // Unsubscribe to prevent memory leak
#pragma warning disable CS0618
                workspace.WorkspaceFailed -= workspaceFailedHandler;
#pragma warning restore CS0618
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MSBuild solution loading cancelled: {SolutionPath}", solutionPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MSBuild failed to load solution: {SolutionPath}", solutionPath);
            throw new MSBuildLoadException($"Failed to load solution via MSBuild at {solutionPath}", ex);
        }
    }

    private async Task<ProjectInfo> ExtractProjectInfoAsync(Project project, CancellationToken cancellationToken)
    {
        // Extract target framework (may be limited without full semantic analysis)
        var targetFramework = await ExtractTargetFrameworkAsync(project, cancellationToken);

        // Extract project references
        var references = new List<ProjectReference>();

        // Add project-to-project references
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
            }
        }

        // Add assembly/DLL references (filtered)
        foreach (var metadataRef in project.MetadataReferences)
        {
            if (metadataRef is PortableExecutableReference portableRef && portableRef.FilePath != null)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(portableRef.FilePath);

                // Filter framework assemblies (same as RoslynSolutionLoader)
                if (!IsFrameworkAssembly(assemblyName))
                {
                    references.Add(new ProjectReference
                    {
                        TargetName = assemblyName,
                        Type = ReferenceType.AssemblyReference,
                        TargetPath = portableRef.FilePath
                    });
                }
            }
        }

        return new ProjectInfo
        {
            Name = project.Name,
            FilePath = project.FilePath ?? string.Empty,
            TargetFramework = targetFramework,
            Language = DetermineLanguage(project.FilePath),
            References = references
        };
    }

    private async Task<string> ExtractTargetFrameworkAsync(Project project, CancellationToken cancellationToken)
    {
        // Try to extract target framework from project file
        // MSBuild mode may have limited access compared to Roslyn
        // Reuse same XML parsing logic from RoslynSolutionLoader Story 2-1

        if (string.IsNullOrEmpty(project.FilePath) || !File.Exists(project.FilePath))
        {
            return "unknown";
        }

        try
        {
            var projectXml = await File.ReadAllTextAsync(project.FilePath, cancellationToken);

            // Modern SDK-style: <TargetFramework>net8.0</TargetFramework>
            var tfMatch = System.Text.RegularExpressions.Regex.Match(projectXml, @"<TargetFramework>(.*?)</TargetFramework>");
            if (tfMatch.Success)
            {
                return tfMatch.Groups[1].Value.Trim();
            }

            // Multi-targeting: <TargetFrameworks>net8.0;net472</TargetFrameworks>
            var tfsMatch = System.Text.RegularExpressions.Regex.Match(projectXml, @"<TargetFrameworks>(.*?)</TargetFrameworks>");
            if (tfsMatch.Success)
            {
                var frameworks = tfsMatch.Groups[1].Value.Split(';');
                return frameworks.FirstOrDefault()?.Trim() ?? "unknown";
            }

            // Legacy .NET Framework: <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
            var tfvMatch = System.Text.RegularExpressions.Regex.Match(projectXml, @"<TargetFrameworkVersion>v(.*?)</TargetFrameworkVersion>");
            if (tfvMatch.Success)
            {
                var version = tfvMatch.Groups[1].Value.Replace(".", "");
                return $"net{version}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract target framework from project file: {ProjectPath}", project.FilePath);
        }

        return "unknown";
    }

    private string DetermineLanguage(string? projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath))
            return "Unknown";

        var extension = Path.GetExtension(projectFilePath).ToLowerInvariant();
        return extension switch
        {
            ".csproj" => "C#",
            ".vbproj" => "Visual Basic",
            ".fsproj" => "F#",
            _ => "Unknown"
        };
    }

    private bool IsFrameworkAssembly(string assemblyName)
    {
        // Filter common framework assemblies (identical logic to RoslynSolutionLoader)
        return assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("Windows", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("WindowsBase", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase);
    }
}
