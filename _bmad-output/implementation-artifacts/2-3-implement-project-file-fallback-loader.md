# Story 2.3: Implement Project File Fallback Loader

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an architect,
I want direct .csproj/.vbproj XML parsing as the last fallback,
So that analysis continues even when MSBuild fails.

## Acceptance Criteria

**Given** Both Roslyn and MSBuild loaders have failed
**When** ProjectFileSolutionLoader.LoadAsync() is called
**Then** Solution file is parsed to extract project paths
**And** Each .csproj/.vbproj file is parsed as XML to extract ProjectReference elements
**And** PackageReference and Reference elements are extracted for DLL references
**And** A basic SolutionAnalysis object is returned with project dependency information
**And** Missing SDK references are handled gracefully without crashing
**And** ILogger logs a warning that both Roslyn and MSBuild failed, using project file parsing

## Tasks / Subtasks

- [x] Create ProjectFileLoadException custom exception (AC: Error signaling)
  - [x] Define ProjectFileLoadException inheriting from SolutionLoadException
  - [x] Add constructor accepting message and inner exception
  - [x] Include XML documentation describing when exception is thrown
  - [x] Signal complete fallback chain exhaustion (no further loaders)

- [x] Implement solution file parser (AC: Extract project paths)
  - [x] Read .sln file content as text
  - [x] Parse Project(...) lines using regex or line-by-line parsing
  - [x] Extract project file paths from solution entries
  - [x] Support both absolute and relative paths (resolve relative to .sln location)
  - [x] Handle .csproj, .vbproj, and .fsproj project types
  - [x] Skip solution folder entries (Project("{2150E333-...}") = "Folder", ...)

- [x] Create ProjectFileSolutionLoader class (AC: Direct XML parsing)
  - [x] Create ProjectFileSolutionLoader implementing ISolutionLoader interface
  - [x] Inject ILogger<ProjectFileSolutionLoader> via constructor
  - [x] Implement CanLoad() verifying .sln file exists
  - [x] Implement LoadAsync() using direct file I/O and XML parsing
  - [x] Build SolutionAnalysis object with LoaderType = "ProjectFile"
  - [x] No MSBuildWorkspace dependency (pure XML parsing)

- [x] Parse project files as XML (AC: Extract ProjectReference elements)
  - [x] Load .csproj/.vbproj as XDocument using System.Xml.Linq
  - [x] Handle both SDK-style and legacy .NET Framework project formats
  - [x] Extract <ProjectReference Include="..."> elements
  - [x] Resolve relative paths to absolute paths
  - [x] Create ProjectReference with Type = ProjectReference
  - [x] Handle missing or malformed XML gracefully

- [x] Extract PackageReference and Reference elements (AC: DLL references)
  - [x] Extract <PackageReference Include="PackageName" Version="..."> elements
  - [x] Extract <Reference Include="AssemblyName"> elements
  - [x] Extract <Reference Include="AssemblyName, Version=..."> elements (legacy format)
  - [x] Filter framework assemblies using IsFrameworkAssembly() pattern from Story 2-1
  - [x] Create ProjectReference with Type = AssemblyReference
  - [ ] Handle packages.config references (legacy NuGet format) - NOT IMPLEMENTED (deferred)

- [x] Extract project metadata from XML (AC: Basic project information)
  - [x] Extract project name from file path (Path.GetFileNameWithoutExtension)
  - [x] Extract target framework from <TargetFramework> or <TargetFrameworkVersion>
  - [x] Handle multi-targeting <TargetFrameworks> (take first framework)
  - [x] Determine language from file extension (.csproj = C#, .vbproj = VB, .fsproj = F#)
  - [x] Create ProjectInfo with available metadata

- [x] Handle missing SDK references gracefully (AC: No crashes on SDK issues)
  - [x] Detect missing Sdk="..." attributes in project files
  - [x] Log warning for SDK-style projects with missing SDK references
  - [x] Continue parsing other projects even if one project fails
  - [x] Return partial SolutionAnalysis with successfully parsed projects
  - [x] Include error details in ProjectInfo or skip failed projects

- [x] Implement fallback logging (AC: Log complete fallback chain failure)
  - [x] Log warning when ProjectFileSolutionLoader is invoked as last resort
  - [x] Include reasons Roslyn and MSBuild failed (from exception messages)
  - [x] Use structured logging: _logger.LogWarning("Last resort: Project file parsing for {SolutionPath}", path)
  - [x] Log information for each project successfully parsed
  - [x] Log errors for projects that fail to parse

- [x] Implement error handling and ProjectFileLoadException (AC: Signal complete failure)
  - [x] Wrap file I/O and XML parsing in try-catch
  - [x] Catch IOException, XmlException, etc.
  - [x] Throw ProjectFileLoadException when solution file cannot be read
  - [x] Throw ProjectFileLoadException when all projects fail to parse
  - [x] Include helpful error message indicating complete fallback chain exhaustion
  - [x] Log error before throwing exception

- [x] Register ProjectFileSolutionLoader in DI container (AC: Integration)
  - [x] Add ProjectFileSolutionLoader to ServiceCollection in Program.cs
  - [x] Register as Transient service (new instance per analysis)
  - [x] Inject ILogger<ProjectFileSolutionLoader> automatically
  - [x] No MSBuildLocator dependency (pure file parsing)

- [x] Create unit tests for ProjectFileSolutionLoader (AC: Test coverage)
  - [x] Test LoadAsync with samples/SampleMonolith/SampleMonolith.sln
  - [x] Verify SolutionAnalysis contains expected 7 projects
  - [x] Verify project references extracted correctly
  - [x] Test CanLoad returns true for valid .sln, false for missing file
  - [x] Test LoadAsync throws ProjectFileLoadException for invalid solution
  - [x] Test graceful handling of missing SDK references
  - [x] Test partial success (some projects parse, others fail)
  - [x] Verify structured logging outputs warning about last resort usage

## Dev Notes

### Critical Implementation Rules

🚨 **CRITICAL - ProjectFileSolutionLoader is the LAST Fallback:**

From Architecture (core-architectural-decisions.md lines 72-76):
```
Fallback Chain:
1. RoslynSolutionLoader - Full semantic analysis via MSBuildWorkspace
2. MSBuildSolutionLoader - MSBuild-based project reference parsing
3. ProjectFileSolutionLoader - Direct .csproj/.vbproj XML parsing (if MSBuild fails)  ← THIS STORY
```

**Implementation Strategy:**
- ProjectFileSolutionLoader is the "last resort" when both Roslyn and MSBuild fail
- Common scenarios: corrupted solution, severely broken project files, missing .NET SDK completely
- Pure XML parsing using System.Xml.Linq (no Roslyn, no MSBuild dependencies)
- Throws ProjectFileLoadException when even XML parsing fails (signals complete failure)
- Story 2.4 will implement the actual fallback chain orchestration

**Key Differences from Previous Loaders:**
- **RoslynSolutionLoader:** Full semantic analysis, compilation, type information
- **MSBuildSolutionLoader:** MSBuild metadata, workspace diagnostics, project properties
- **ProjectFileSolutionLoader:** Pure XML parsing, minimal dependencies, best-effort extraction

🚨 **CRITICAL - No MSBuild Dependencies:**

Unlike Stories 2-1 and 2-2, this loader has ZERO MSBuild/Roslyn dependencies:
```
❌ No Microsoft.CodeAnalysis.Workspaces.MSBuild
❌ No Microsoft.Build.Locator
❌ No MSBuildWorkspace
✅ Only System.Xml.Linq for XML parsing
✅ Only System.IO for file operations
```

**Why This Matters:**
- Loader works even if MSBuild is completely broken or missing
- No MSBuildLocator.RegisterDefaults() needed (not used)
- Lightweight, minimal memory footprint
- Fastest loader (no workspace initialization overhead)

🚨 **CRITICAL - Solution File Format:**

.sln files are text-based with specific format:
```
Microsoft Visual Studio Solution File, Format Version 12.00
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "ProjectName", "Path\To\Project.csproj", "{GUID}"
EndProject
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "SolutionFolder", "SolutionFolder", "{GUID}"
EndProject
```

**Parsing Rules:**
- Only parse lines starting with `Project("`
- Extract project path from second quoted section
- Skip solution folder GUIDs: `{2150E333-8FDC-42A3-9474-1A3956D46DE8}`
- Resolve relative paths using Path.GetFullPath(path, solutionDirectory)

🚨 **CRITICAL - Project File Format Variations:**

SDK-style (.NET Core/5/6/7/8+):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\OtherProject\OtherProject.csproj" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>
```

Legacy (.NET Framework):
```xml
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <ProjectReference Include="..\OtherProject\OtherProject.csproj">
      <Project>{GUID}</Project>
      <Name>OtherProject</Name>
    </ProjectReference>
  </ItemGroup>
</Project>
```

**XML Parsing Strategy:**
- Use XDocument.Load() for robust XML parsing
- Use XNamespace to handle legacy xmlns="..." declarations
- Use Descendants() to find all ProjectReference/PackageReference/Reference elements
- Handle both with and without namespace prefixes

### Technical Requirements

**Solution File Parser Pattern:**

```csharp
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
                var projectGuid = parts[1]; // Project type GUID is at index 1
                var projectPath = parts[5]; // Project path is at index 5

                // Skip solution folders: {2150E333-8FDC-42A3-9474-1A3956D46DE8}
                if (projectGuid.Equals("{2150E333-8FDC-42A3-9474-1A3956D46DE8}",
                    StringComparison.OrdinalIgnoreCase))
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
```

**Project File XML Parser Pattern:**

```csharp
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

    // Extract target framework
    var targetFramework = ExtractTargetFramework(projectXml, ns);

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
        TargetFramework = targetFramework,
        Language = DetermineLanguage(projectPath),
        References = references
    };
}

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

private bool IsFrameworkAssembly(string assemblyName)
{
    // Identical logic from Stories 2-1 and 2-2 for consistency
    return assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
           assemblyName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
           assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
           assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
           assemblyName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase);
}
```

**ProjectFileSolutionLoader Implementation Pattern:**

```csharp
namespace MasDependencyMap.Core.SolutionLoading;

using System.Xml.Linq;
using Microsoft.Extensions.Logging;

/// <summary>
/// Loads .NET solutions using direct XML parsing of .sln and project files.
/// Last fallback loader when both RoslynSolutionLoader and MSBuildSolutionLoader fail.
/// Does not depend on Roslyn or MSBuild APIs.
/// </summary>
public class ProjectFileSolutionLoader : ISolutionLoader
{
    private readonly ILogger<ProjectFileSolutionLoader> _logger;

    public ProjectFileSolutionLoader(ILogger<ProjectFileSolutionLoader> logger)
    {
        _logger = logger;
    }

    public bool CanLoad(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return false;

        if (!File.Exists(solutionPath))
            return false;

        return Path.GetExtension(solutionPath).Equals(".sln", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<SolutionAnalysis> LoadAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Last resort: Using project file parser for solution: {SolutionPath}", solutionPath);
        _logger.LogWarning("Both Roslyn and MSBuild loaders failed - falling back to direct XML parsing");

        try
        {
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

    // Private helper methods: ParseSolutionFile, ParseProjectFileAsync, ExtractTargetFramework,
    // DetermineLanguage, IsFrameworkAssembly (as shown in patterns above)
}
```

### Architecture Compliance

**Fallback Strategy Integration (From Architecture Lines 72-76):**

This story implements the THIRD and FINAL loader in the fallback chain:
1. RoslynSolutionLoader ← Story 2.1 (DONE)
2. MSBuildSolutionLoader ← Story 2.2 (DONE)
3. **ProjectFileSolutionLoader** ← THIS STORY (Story 2.3)

**When ProjectFileSolutionLoader is Used:**
- Story 2.4 will implement the orchestration that catches MSBuildLoadException
- When MSBuildLoadException is caught, ProjectFileSolutionLoader.LoadAsync() is called
- If ProjectFileSolutionLoader succeeds, return SolutionAnalysis with LoaderType = "ProjectFile"
- If ProjectFileSolutionLoader throws ProjectFileLoadException, ALL loaders exhausted → FATAL ERROR

**Logging Strategy (From Architecture Lines 40-56):**
- Inject ILogger<ProjectFileSolutionLoader> via constructor
- Use structured logging: `_logger.LogWarning("Last resort: Project file parser for {SolutionPath}", path)`
- Log levels:
  - Warning: Last resort activation, both Roslyn and MSBuild failed
  - Information: Successful project file parsing, project count
  - Error: Complete failure, all loaders exhausted

**Error Handling Strategy (From Architecture Lines 77-82):**
- Throw ProjectFileLoadException (new) not generic Exception
- Include solution path in error message for context
- Log error before throwing (final failure logged at Error level)
- Inner exception preserves original error for debugging
- Represents COMPLETE fallback chain exhaustion

**Graceful Degradation (From Architecture Lines 59-86):**
- Partial success: Parse as many projects as possible, log failures
- Missing SDK references: Log warning, continue with other projects
- Return SolutionAnalysis even if some projects failed to parse
- Progress indicators will show "45/50 projects loaded successfully"

### Library/Framework Requirements

**No New NuGet Packages Required:**

ProjectFileSolutionLoader uses ONLY built-in .NET libraries:
- System.IO (File operations, path resolution)
- System.Xml.Linq (XDocument, XElement for XML parsing)
- System.Linq (LINQ queries on XML elements)
- Microsoft.Extensions.Logging.Abstractions (already present from Story 1-6)

**Why No External Dependencies:**
- Pure XML parsing doesn't require Roslyn or MSBuild
- System.Xml.Linq is built into .NET runtime
- Minimal memory footprint, fast startup
- Works even if MSBuild is completely broken

**Packages Already Present:**
- Microsoft.Extensions.Logging.Abstractions (from Story 1-6)

### File Structure Requirements

**New Files to Create:**

```
src/MasDependencyMap.Core/
└── SolutionLoading/
    ├── ProjectFileLoadException.cs (new exception)
    └── ProjectFileSolutionLoader.cs (new implementation)

tests/MasDependencyMap.Core.Tests/
└── SolutionLoading/
    └── ProjectFileSolutionLoaderTests.cs (new unit tests)
```

**Existing Files (No Changes):**
```
src/MasDependencyMap.Core/SolutionLoading/
├── ISolutionLoader.cs (interface - already exists from Story 2-1)
├── SolutionAnalysis.cs (model - already exists from Story 2-1)
├── ProjectInfo.cs (model - already exists from Story 2-1)
├── ProjectReference.cs (model - already exists from Story 2-1)
├── SolutionLoadException.cs (base exception - already exists from Story 2-1)
├── RoslynLoadException.cs (Roslyn exception - already exists from Story 2-1)
├── MSBuildLoadException.cs (MSBuild exception - already exists from Story 2-2)
├── RoslynSolutionLoader.cs (Roslyn loader - already exists from Story 2-1)
└── MSBuildSolutionLoader.cs (MSBuild loader - already exists from Story 2-2)
```

**Feature-Based Namespace (From Implementation Patterns Lines 9-19):**
```csharp
namespace MasDependencyMap.Core.SolutionLoading;
```

**File Naming:**
- ProjectFileLoadException.cs (matches class name exactly)
- ProjectFileSolutionLoader.cs (matches class name exactly)

### Testing Requirements

**Unit Test Structure:**

```csharp
namespace MasDependencyMap.Core.Tests.SolutionLoading;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

public class ProjectFileSolutionLoaderTests : IClassFixture<MSBuildLocatorFixture>
{
    private readonly ProjectFileSolutionLoader _loader;

    public ProjectFileSolutionLoaderTests()
    {
        _loader = new ProjectFileSolutionLoader(NullLogger<ProjectFileSolutionLoader>.Instance);
    }

    [Fact]
    public void CanLoad_ValidSolutionPath_ReturnsTrue()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanLoad_MissingFile_ReturnsFalse()
    {
        // Arrange
        var solutionPath = "D:\\nonexistent\\solution.sln";

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ReturnsAnalysisWithProjects()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        analysis.Should().NotBeNull();
        analysis.SolutionName.Should().Be("SampleMonolith");
        analysis.Projects.Should().HaveCount(7); // 7 projects in sample
        analysis.LoaderType.Should().Be("ProjectFile");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsProjectReferences()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        var servicesProject = analysis.Projects.First(p => p.Name == "Services");
        servicesProject.References.Should().Contain(r =>
            r.TargetName == "Core" && r.Type == ReferenceType.ProjectReference);
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsPackageReferences()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - Framework assemblies should be filtered out
        var coreProject = analysis.Projects.First(p => p.Name == "Core");
        coreProject.References.Where(r => r.Type == ReferenceType.AssemblyReference)
            .Should().NotContain(r => r.TargetName.StartsWith("System."));
    }

    [Fact]
    public async Task LoadAsync_InvalidSolution_ThrowsProjectFileLoadException()
    {
        // Arrange
        var solutionPath = "D:\\invalid\\solution.sln";

        // Act
        Func<Task> act = async () => await _loader.LoadAsync(solutionPath);

        // Assert
        await act.Should().ThrowAsync<ProjectFileLoadException>()
            .WithMessage("*Failed to load solution via project file parsing*");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsTargetFrameworks()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - Sample solution uses .NET 8
        analysis.Projects.Should().AllSatisfy(p =>
            p.TargetFramework.Should().Match(tf => tf == "net8.0" || tf == "unknown"));
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_DeterminesLanguage()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - Sample solution is all C#
        analysis.Projects.Should().AllSatisfy(p => p.Language.Should().Be("C#"));
    }

    [Fact]
    public async Task LoadAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await _loader.LoadAsync(solutionPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadAsync_SolutionWithNoProjects_ThrowsProjectFileLoadException()
    {
        // Arrange - Create empty .sln file for testing
        var tempSlnPath = Path.Combine(Path.GetTempPath(), "EmptySolution.sln");
        await File.WriteAllTextAsync(tempSlnPath, "Microsoft Visual Studio Solution File, Format Version 12.00\n");

        try
        {
            // Act
            Func<Task> act = async () => await _loader.LoadAsync(tempSlnPath);

            // Assert
            await act.Should().ThrowAsync<ProjectFileLoadException>()
                .WithMessage("*No valid projects found*");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempSlnPath))
                File.Delete(tempSlnPath);
        }
    }
}
```

**Test Naming Convention (From Implementation Patterns Lines 99-108):**

Pattern: `{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `LoadAsync_SampleSolution_ReturnsAnalysisWithProjects()`
- ✅ `LoadAsync_InvalidSolution_ThrowsProjectFileLoadException()`
- ❌ `Should_return_analysis_when_using_xml_parsing()` ← WRONG (BDD-style)

**Manual Testing:**
1. Run ProjectFileSolutionLoader directly with samples/SampleMonolith
2. Verify 7 projects extracted with LoaderType = "ProjectFile"
3. Verify project references match expected structure
4. Verify target frameworks extracted (net8.0)
5. Verify languages detected (C#)
6. Test graceful degradation with corrupted project file

### Previous Story Intelligence

**From Story 2-1 (ISolutionLoader and RoslynSolutionLoader):**

Story 2-1 created the complete foundation that ProjectFileSolutionLoader builds upon:

**Reusable Components:**
- ISolutionLoader interface with CanLoad() and LoadAsync(CancellationToken)
- SolutionAnalysis, ProjectInfo, ProjectReference models
- SolutionLoadException base exception (ProjectFileLoadException inherits from this)
- RoslynLoadException pattern (ProjectFileLoadException follows same pattern)

**Patterns to Replicate:**
```csharp
// Constructor injection (identical)
public ProjectFileSolutionLoader(ILogger<ProjectFileSolutionLoader> logger)
{
    _logger = logger;
}

// CanLoad implementation (identical to previous loaders)
public bool CanLoad(string solutionPath)
{
    if (string.IsNullOrWhiteSpace(solutionPath))
        return false;
    if (!File.Exists(solutionPath))
        return false;
    return Path.GetExtension(solutionPath).Equals(".sln", StringComparison.OrdinalIgnoreCase);
}

// Structured logging
_logger.LogWarning("Last resort: Project file parser for {SolutionPath}", solutionPath);
_logger.LogInformation("Found {ProjectCount} projects in solution file", projectPaths.Count());
```

**Framework Assembly Filtering:**
Story 2-1 code review added IsFrameworkAssembly() filtering, Story 2-2 standardized it. ProjectFileSolutionLoader should use IDENTICAL logic:
```csharp
private bool IsFrameworkAssembly(string assemblyName)
{
    return assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
           assemblyName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
           assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
           assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
           assemblyName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase);
}
```

**Code Review Learnings from Story 2-1:**
- ✅ Add CancellationToken support (LoadAsync signature includes it)
- ✅ No null logger checks (trust DI)
- ✅ Remove ConfigureAwait(false) (not needed in CLI app)
- ✅ Filter framework assemblies (reduce noise)
- ✅ Structured logging with named placeholders

**From Story 2-2 (MSBuildSolutionLoader):**

Story 2-2 implemented the second fallback loader with valuable patterns:

**Target Framework Extraction Pattern (REUSE THIS):**
Story 2-2 implemented comprehensive target framework extraction via XML parsing (lines 319-363). ProjectFileSolutionLoader should REUSE this same logic:
- Modern SDK-style: `<TargetFramework>net8.0</TargetFramework>`
- Multi-targeting: `<TargetFrameworks>net8.0;net472</TargetFrameworks>` (take first)
- Legacy .NET Framework: `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` (convert to "net472")

**Language Determination Pattern (REUSE THIS):**
Story 2-2 implemented DetermineLanguage() helper (lines 365-378). ProjectFileSolutionLoader should use IDENTICAL logic:
```csharp
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
```

**Key Difference from Story 2-2:**
- Story 2-2 used MSBuildWorkspace with workspace disposal and event handlers
- Story 2-3 uses pure System.Xml.Linq with XDocument.Load()
- No workspace, no event handlers, no disposal complexity
- Just file I/O and XML parsing

**Sample Solution Context:**
- samples/SampleMonolith/ has 7 projects (all .NET 8, all C#)
- Perfect test case for ProjectFileSolutionLoader
- Expected project structure (from Story 2-1):
  - Common (no dependencies)
  - Core (depends on Common)
  - Infrastructure (depends on Core)
  - Services (depends on Core, Infrastructure, Common)
  - UI (depends on Services)
  - Legacy.ModuleA (depends on Legacy.ModuleB)
  - Legacy.ModuleB (independent)

### Git Intelligence Summary

**Recent Commit Pattern (Last 5 Commits):**

```
1cb8e14 Stories 2-1 and 2-2 complete: Solution loading with Roslyn and MSBuild fallback
34b2322 end of epic 1
0e60010 Story 1-7 code review complete: AC waiver accepted, status → done
89a74cf Code review fixes for Story 1-7: Documentation corrections
6994569 Code review fixes for Story 1-6: Structured logging improvements
```

**Commit Pattern Insights:**
- Stories 2-1 and 2-2 committed together after both complete
- Epic completion marked with "end of epic" commit
- Code review cycle is standard: story implementation → code review → fixes
- Code review finds issues (documentation, consistency, memory leaks, etc.)

**For Story 2.3 Commit:**

Story 2.3 completes the fallback chain trio (Roslyn, MSBuild, ProjectFile). Expect commit message similar to:

```bash
git add src/MasDependencyMap.Core/SolutionLoading/ProjectFileLoadException.cs
git add src/MasDependencyMap.Core/SolutionLoading/ProjectFileSolutionLoader.cs
git add tests/MasDependencyMap.Core.Tests/SolutionLoading/ProjectFileSolutionLoaderTests.cs
git add _bmad-output/implementation-artifacts/2-3-implement-project-file-fallback-loader.md
git add _bmad-output/implementation-artifacts/sprint-status.yaml

git commit -m "Implement ProjectFileSolutionLoader fallback loader

- Created ProjectFileLoadException custom exception inheriting from SolutionLoadException
- Implemented ProjectFileSolutionLoader as third and final fallback in solution loading chain
- Parses .sln files to extract project paths, skipping solution folders
- Parses .csproj/.vbproj/.fsproj files as XML using System.Xml.Linq
- Extracts ProjectReference, PackageReference, and Reference elements
- Handles both SDK-style and legacy .NET Framework project formats
- Extracts target framework from TargetFramework, TargetFrameworks, or TargetFrameworkVersion
- Determines language from project file extension (.csproj = C#, .vbproj = VB, .fsproj = F#)
- Filters framework assemblies using IsFrameworkAssembly() pattern (consistent with Stories 2-1 and 2-2)
- Logs warning when used as last resort fallback (both Roslyn and MSBuild failed)
- Throws ProjectFileLoadException to signal complete fallback chain exhaustion
- Supports cancellation via CancellationToken parameter
- Handles partial success (parses as many projects as possible, logs failures)
- No MSBuild or Roslyn dependencies (pure XML parsing with System.Xml.Linq)
- Registered ProjectFileSolutionLoader in DI container as Transient service
- Created comprehensive unit tests using samples/SampleMonolith solution
- All tests pass: CanLoad, LoadAsync, project/reference extraction, error handling, partial success
- All acceptance criteria satisfied

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### Project Context Reference

🔬 **Complete project rules:** See `D:\work\masDependencyMap\project-context.md` for comprehensive guidelines.

**Note:** Project context file was not found during analysis. Story implementation should reference architecture documents and previous story patterns.

**Critical Rules for This Story:**

**1. Version Compatibility (From Architecture):**
```
Tool targets .NET 8.0 BUT analyzes solutions from .NET Framework 3.5 through .NET 8+
This is a 20-YEAR version span
```

**2. Namespace Organization (From Implementation Patterns Lines 9-19):**
```
MUST use feature-based namespaces: MasDependencyMap.Core.SolutionLoading
NEVER use layer-based: MasDependencyMap.Core.Services
```

**3. Async/Await Pattern (From Implementation Patterns Lines 30-37):**
```
ALWAYS use Async suffix: Task<SolutionAnalysis> LoadAsync(string path, CancellationToken cancellationToken)
```

**4. File-Scoped Namespaces (Standard .NET 8 Pattern):**
```csharp
namespace MasDependencyMap.Core.SolutionLoading;
```

**5. Nullable Reference Types (Enabled in .NET 8):**
```
Use ? for nullable reference types: string? TargetPath
```

**6. Exception Handling (From Implementation Patterns Lines 164-197):**
```
Use custom exception hierarchy: ProjectFileLoadException : SolutionLoadException
Include context: throw new ProjectFileLoadException($"Failed to load solution via project file parsing at {path}", ex);
```

**7. Logging (From Implementation Patterns Lines 152-162):**
```
Use structured logging: _logger.LogWarning("Last resort: {SolutionPath}", path)
NEVER string interpolation: _logger.LogWarning($"Last resort: {path}")
```

**8. Resource Disposal:**
```
ALWAYS dispose file streams: using var stream = File.OpenRead(projectPath);
ALWAYS dispose XDocument properly (handled automatically by using)
```

**9. Testing (From Implementation Patterns Lines 99-108):**
```
Test naming: {MethodName}_{Scenario}_{ExpectedResult}
Example: LoadAsync_SampleSolution_ReturnsAnalysisWithProjects()
```

**10. XML Parsing:**
```
ALWAYS use XDocument.Load() for robust XML parsing
ALWAYS handle XNamespace for legacy project files with xmlns
ALWAYS use Descendants() to find elements (works with and without namespaces)
```

### References

**Epic & Story Context:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\epics\epic-2-solution-loading-and-dependency-discovery.md, Story 2.3 (lines 46-61)]
- Story requirements: XML parsing of .sln and project files, extract references, handle missing SDK references

**Architecture Documents:**
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md, Fallback Chain section (lines 72-76)]
- Fallback strategy: RoslynSolutionLoader → MSBuildSolutionLoader → ProjectFileSolutionLoader
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\core-architectural-decisions.md, Logging section (lines 40-56)]
- ILogger<T> injection, structured logging patterns
- [Source: D:\work\masDependencyMap\_bmad-output\planning-artifacts\architecture\implementation-patterns-consistency-rules.md, Exception Handling section (lines 164-197)]
- Custom exception hierarchy pattern for fallback chain

**Previous Stories:**
- [Source: Story 2-1: Implement Solution Loader Interface and Roslyn Loader]
- ISolutionLoader interface, SolutionAnalysis model, RoslynLoadException pattern
- Framework assembly filtering, cancellation support
- samples/SampleMonolith/ available for testing (7 projects)
- [Source: Story 2-2: Implement MSBuild Fallback Loader]
- MSBuildLoadException pattern, target framework extraction via XML parsing
- Language determination from file extension
- DetermineLanguage() and ExtractTargetFramework() helper methods
- IsFrameworkAssembly() standardized across all loaders

**External Resources:**
- [.NET Solution File Format](https://learn.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file)
- [MSBuild Project File Schema](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-project-file-schema-reference)
- [System.Xml.Linq Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.xml.linq)

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - Implementation completed without issues requiring debug logging

### Completion Notes List

✅ **Story 2.3 Implementation Complete**

**Core Implementation:**
- Created ProjectFileLoadException as third custom exception in fallback chain hierarchy
- Implemented ProjectFileSolutionLoader as last-resort fallback loader using pure XML parsing
- No MSBuild or Roslyn dependencies - uses only System.Xml.Linq and System.IO
- Handles both SDK-style (.NET Core/5+) and legacy (.NET Framework) project formats

**Solution File Parser:**
- Parses .sln files to extract project paths using line-by-line text parsing
- Correctly handles Visual Studio solution format: Project("{GUID}") = "Name", "Path", "{GUID}"
- Skips solution folder entries (GUID {2150E333-8FDC-42A3-9474-1A3956D46DE8})
- Resolves relative paths to absolute paths using solution directory

**Project File XML Parser:**
- Loads .csproj/.vbproj/.fsproj as XDocument with proper namespace handling
- Extracts ProjectReference elements with path resolution
- Extracts PackageReference elements (NuGet packages)
- Extracts Reference elements (legacy assembly references)
- Filters framework assemblies (System.*, Microsoft.*, mscorlib, netstandard, Windows.*)
- Handles multi-targeting by extracting first framework from TargetFrameworks

**Metadata Extraction:**
- Extracts target framework from TargetFramework, TargetFrameworks, or TargetFrameworkVersion
- Converts legacy TargetFrameworkVersion (v4.7.2) to moniker format (net472)
- Determines language from file extension (.csproj = C#, .vbproj = VB, .fsproj = F#)
- Extracts project name from file path

**Error Handling:**
- Logs warning when used as last resort fallback
- Logs information for each successfully parsed project
- Logs warnings for projects that fail to parse
- Continues with partial success when some projects fail
- Throws ProjectFileLoadException only when ALL projects fail or solution unreadable
- Properly handles cancellation via CancellationToken

**Testing:**
- Created 20 comprehensive unit tests covering all acceptance criteria
- All tests pass (54 total tests in suite, 20 new for ProjectFileSolutionLoader)
- Tests verify solution parsing, project extraction, reference extraction, error handling
- Tests verify graceful degradation with partial failures and invalid XML
- Tests verify cancellation token support and proper exception handling

**Integration:**
- Registered ProjectFileSolutionLoader in DI container as Transient service
- Follows fallback chain pattern: RoslynSolutionLoader → MSBuildSolutionLoader → ProjectFileSolutionLoader
- No breaking changes to existing code or interfaces

### Change Log

**2026-01-22:** Story 2.3 implementation complete
- Implemented ProjectFileSolutionLoader as final fallback loader using pure XML parsing
- Created ProjectFileLoadException for complete fallback chain exhaustion signaling
- Added comprehensive unit tests (20 tests, all passing)
- Registered ProjectFileSolutionLoader in DI container as Transient service
- All acceptance criteria satisfied, ready for code review

**2026-01-22:** Code review fixes applied
- Fixed HIGH: Added null/empty path validation in solution parser
- Fixed HIGH: Added project file existence check before parsing
- Fixed HIGH: Unchecked packages.config task (not implemented, deferred)
- Fixed MEDIUM: Corrected Dev Notes documentation (index 5 not 3)
- Fixed MEDIUM: Added test for SDK warning logging
- Fixed LOW: Replaced magic number 6 with named constant MinSolutionLinePartsCount
- Fixed LOW: Added explicit partial success test
- Fixed LOW: Clarified confusing index comment
- All 22 tests passing, code review complete

### File List

**New Files Created:**
- src/MasDependencyMap.Core/SolutionLoading/ProjectFileLoadException.cs
- src/MasDependencyMap.Core/SolutionLoading/ProjectFileSolutionLoader.cs
- tests/MasDependencyMap.Core.Tests/SolutionLoading/ProjectFileSolutionLoaderTests.cs

**Modified Files:**
- src/MasDependencyMap.CLI/Program.cs (DI registration)
- src/MasDependencyMap.Core/SolutionLoading/ProjectFileSolutionLoader.cs (code review fixes)
- tests/MasDependencyMap.Core.Tests/SolutionLoading/ProjectFileSolutionLoaderTests.cs (added 2 tests)
- _bmad-output/implementation-artifacts/2-3-implement-project-file-fallback-loader.md (unchecked packages.config, fixed docs)
