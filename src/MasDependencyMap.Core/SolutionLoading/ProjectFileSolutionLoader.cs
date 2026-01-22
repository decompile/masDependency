namespace MasDependencyMap.Core.SolutionLoading;

using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

/// <summary>
/// Loads .NET solutions using direct XML parsing of .sln and project files.
/// Last fallback loader when both RoslynSolutionLoader and MSBuildSolutionLoader fail.
/// Does not depend on Roslyn or MSBuild APIs - uses pure System.Xml.Linq for parsing.
/// Part of 3-layer fallback strategy: Roslyn → MSBuild → ProjectFile.
/// </summary>
public class ProjectFileSolutionLoader : ISolutionLoader
{
    private readonly ILogger<ProjectFileSolutionLoader> _logger;
    private const string SolutionFolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

    /// <summary>
    /// Creates a new ProjectFileSolutionLoader with logging support.
    /// </summary>
    /// <param name="logger">Logger for structured logging of parsing operations</param>
    public ProjectFileSolutionLoader(ILogger<ProjectFileSolutionLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines if this loader can handle the given solution path.
    /// Checks that path is valid and file exists with .sln extension.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file</param>
    /// <returns>True if the solution file exists and has .sln extension</returns>
    public bool CanLoad(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return false;

        if (!File.Exists(solutionPath))
            return false;

        return Path.GetExtension(solutionPath).Equals(".sln", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads solution analysis using direct XML parsing of .sln and project files.
    /// This is the last resort fallback when both Roslyn and MSBuild loaders fail.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Solution analysis with project dependency information</returns>
    /// <exception cref="ProjectFileLoadException">When solution file cannot be read or all projects fail to parse</exception>
    public async Task<SolutionAnalysis> LoadAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Last resort: Using project file parser for solution: {SolutionPath}", solutionPath);
        _logger.LogWarning("Both Roslyn and MSBuild loaders failed - falling back to direct XML parsing");

        try
        {
            // Check cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            // Parse solution file to extract project paths
            var projectPaths = ParseSolutionFile(solutionPath);

            if (!projectPaths.Any())
            {
                throw new ProjectFileLoadException($"No valid projects found in solution file: {solutionPath}");
            }

            _logger.LogInformation("Found {ProjectCount} projects in solution file", projectPaths.Count());

            // Parse each project file
            var projects = new List<ProjectInfo>();
            var failedProjects = new List<string>();

            foreach (var projectPath in projectPaths)
            {
                try
                {
                    _logger.LogInformation("Parsing project file: {ProjectPath}", projectPath);
                    var projectInfo = await ParseProjectFileAsync(projectPath, cancellationToken);
                    projects.Add(projectInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse project file: {ProjectPath}", projectPath);
                    failedProjects.Add(projectPath);
                }
            }

            if (projects.Count == 0)
            {
                throw new ProjectFileLoadException($"All projects failed to parse in solution: {solutionPath}");
            }

            if (failedProjects.Any())
            {
                _logger.LogWarning("Successfully parsed {SuccessCount}/{TotalCount} projects. Failed: {FailedCount}",
                    projects.Count, projectPaths.Count(), failedProjects.Count);
            }

            return new SolutionAnalysis
            {
                SolutionPath = Path.GetFullPath(solutionPath),
                SolutionName = Path.GetFileNameWithoutExtension(solutionPath),
                Projects = projects,
                LoaderType = "ProjectFile"
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Project file solution loading cancelled: {SolutionPath}", solutionPath);
            throw;
        }
        catch (ProjectFileLoadException)
        {
            // Already logged, re-throw as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project file parser failed to load solution: {SolutionPath}", solutionPath);
            throw new ProjectFileLoadException($"Failed to load solution via project file parsing at {solutionPath}", ex);
        }
    }

    /// <summary>
    /// Parses a .sln file to extract project file paths.
    /// Skips solution folder entries and only includes .csproj, .vbproj, .fsproj files.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file</param>
    /// <returns>List of absolute paths to project files</returns>
    private IEnumerable<string> ParseSolutionFile(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? string.Empty;
        var projectPaths = new List<string>();

        var lines = File.ReadAllLines(solutionPath);
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("Project(\""))
            {
                // Parse: Project("{GUID}") = "Name", "Path\To\Project.csproj", "{GUID}"
                var parts = line.Split(new[] { '\"' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 6)
                {
                    var projectGuid = parts[1]; // Project type GUID is in 2nd quoted section
                    var projectPath = parts[5]; // Path is in 6th quoted section (0-indexed: 0,1,2,3,4,5)

                    // Skip solution folders: {2150E333-8FDC-42A3-9474-1A3956D46DE8}
                    if (projectGuid.Equals(SolutionFolderGuid, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Resolve relative path to absolute
                    var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, projectPath));

                    // Only include .csproj, .vbproj, .fsproj
                    var ext = Path.GetExtension(fullPath).ToLowerInvariant();
                    if (ext == ".csproj" || ext == ".vbproj" || ext == ".fsproj")
                    {
                        projectPaths.Add(fullPath);
                    }
                }
            }
        }

        return projectPaths;
    }

    /// <summary>
    /// Parses a project file as XML to extract metadata and dependencies.
    /// Handles both SDK-style (.NET Core/5+) and legacy (.NET Framework) formats.
    /// </summary>
    /// <param name="projectPath">Absolute path to the project file</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Project information with references and metadata</returns>
    /// <exception cref="ProjectFileLoadException">When project file XML is invalid or cannot be read</exception>
    private async Task<ProjectInfo> ParseProjectFileAsync(string projectPath, CancellationToken cancellationToken)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? string.Empty;

        XDocument projectXml;
        try
        {
            using var stream = File.OpenRead(projectPath);
            projectXml = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Failed to parse project XML: {ProjectPath}", projectPath);
            throw new ProjectFileLoadException($"Invalid XML in project file: {projectPath}", ex);
        }

        // Handle both SDK-style (no namespace) and legacy (with namespace)
        var ns = projectXml.Root?.GetDefaultNamespace() ?? XNamespace.None;

        // Check for missing SDK references and log warning
        if (projectXml.Root?.Attribute("Sdk") == null)
        {
            // This might be a legacy .NET Framework project (which is fine)
            // Or a SDK-style project with missing SDK reference (warn about this)
            var targetFramework = projectXml.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value;
            if (!string.IsNullOrWhiteSpace(targetFramework) && targetFramework.StartsWith("net") && !targetFramework.StartsWith("net4"))
            {
                _logger.LogWarning("SDK-style project missing Sdk attribute: {ProjectPath}", projectPath);
            }
        }

        // Extract target framework
        var targetFrameworkValue = ExtractTargetFramework(projectXml, ns);

        // Extract project references
        var references = new List<ProjectReference>();

        // ProjectReference elements
        var projectRefs = projectXml.Descendants(ns + "ProjectReference");
        foreach (var projectRef in projectRefs)
        {
            var include = projectRef.Attribute("Include")?.Value;
            if (!string.IsNullOrEmpty(include))
            {
                var refPath = Path.GetFullPath(Path.Combine(projectDirectory, include));
                var refName = Path.GetFileNameWithoutExtension(refPath);

                references.Add(new ProjectReference
                {
                    TargetName = refName,
                    Type = ReferenceType.ProjectReference,
                    TargetPath = refPath
                });
            }
        }

        // PackageReference elements (NuGet packages)
        var packageRefs = projectXml.Descendants(ns + "PackageReference");
        foreach (var packageRef in packageRefs)
        {
            var packageName = packageRef.Attribute("Include")?.Value;
            if (!string.IsNullOrEmpty(packageName) && !IsFrameworkAssembly(packageName))
            {
                references.Add(new ProjectReference
                {
                    TargetName = packageName,
                    Type = ReferenceType.AssemblyReference,
                    TargetPath = null
                });
            }
        }

        // Reference elements (assembly references - legacy format)
        var assemblyRefs = projectXml.Descendants(ns + "Reference");
        foreach (var assemblyRef in assemblyRefs)
        {
            var include = assemblyRef.Attribute("Include")?.Value;
            if (!string.IsNullOrEmpty(include))
            {
                // Handle "AssemblyName, Version=..., Culture=..." format
                var assemblyName = include.Split(',')[0].Trim();

                if (!IsFrameworkAssembly(assemblyName))
                {
                    references.Add(new ProjectReference
                    {
                        TargetName = assemblyName,
                        Type = ReferenceType.AssemblyReference,
                        TargetPath = null
                    });
                }
            }
        }

        return new ProjectInfo
        {
            Name = Path.GetFileNameWithoutExtension(projectPath),
            FilePath = projectPath,
            TargetFramework = targetFrameworkValue,
            Language = DetermineLanguage(projectPath),
            References = references
        };
    }

    /// <summary>
    /// Extracts target framework from project XML.
    /// Handles TargetFramework (SDK-style), TargetFrameworks (multi-targeting), and TargetFrameworkVersion (legacy).
    /// </summary>
    /// <param name="projectXml">Project XML document</param>
    /// <param name="ns">XML namespace (may be None for SDK-style projects)</param>
    /// <returns>Target framework string (e.g., "net8.0", "net472")</returns>
    private string ExtractTargetFramework(XDocument projectXml, XNamespace ns)
    {
        // Modern SDK-style: <TargetFramework>net8.0</TargetFramework>
        var tfElement = projectXml.Descendants(ns + "TargetFramework").FirstOrDefault();
        if (tfElement != null && !string.IsNullOrWhiteSpace(tfElement.Value))
        {
            return tfElement.Value.Trim();
        }

        // Multi-targeting: <TargetFrameworks>net8.0;net472</TargetFrameworks>
        var tfsElement = projectXml.Descendants(ns + "TargetFrameworks").FirstOrDefault();
        if (tfsElement != null && !string.IsNullOrWhiteSpace(tfsElement.Value))
        {
            var frameworks = tfsElement.Value.Split(';');
            return frameworks.FirstOrDefault()?.Trim() ?? "unknown";
        }

        // Legacy .NET Framework: <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
        var tfvElement = projectXml.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault();
        if (tfvElement != null && !string.IsNullOrWhiteSpace(tfvElement.Value))
        {
            var version = tfvElement.Value.Trim().TrimStart('v').Replace(".", "");
            return $"net{version}";
        }

        return "unknown";
    }

    /// <summary>
    /// Determines programming language from project file extension.
    /// </summary>
    /// <param name="projectFilePath">Path to the project file</param>
    /// <returns>Language name (C#, Visual Basic, F#, or Unknown)</returns>
    private string DetermineLanguage(string projectFilePath)
    {
        var extension = Path.GetExtension(projectFilePath).ToLowerInvariant();
        return extension switch
        {
            ".csproj" => "C#",
            ".vbproj" => "Visual Basic",
            ".fsproj" => "F#",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Checks if an assembly is a framework assembly that should be filtered out.
    /// Framework assemblies (System.*, Microsoft.*, etc.) are not useful for dependency analysis.
    /// Consistent with RoslynSolutionLoader and MSBuildSolutionLoader filtering.
    /// </summary>
    /// <param name="assemblyName">Name of the assembly to check</param>
    /// <returns>True if this is a framework assembly that should be filtered</returns>
    private bool IsFrameworkAssembly(string assemblyName)
    {
        return assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase);
    }
}
