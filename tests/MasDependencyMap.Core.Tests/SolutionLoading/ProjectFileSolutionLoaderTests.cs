using FluentAssertions;
using MasDependencyMap.Core.SolutionLoading;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MasDependencyMap.Core.Tests.SolutionLoading;

/// <summary>
/// Unit tests for ProjectFileSolutionLoader.
/// Tests XML parsing of .sln and project files, reference extraction, and error handling.
/// Uses samples/SampleMonolith solution from Story 1-7 for integration testing.
/// Note: ProjectFileSolutionLoader does NOT require MSBuildLocator since it uses pure XML parsing.
/// </summary>
public class ProjectFileSolutionLoaderTests
{
    private readonly ProjectFileSolutionLoader _loader;

    public ProjectFileSolutionLoaderTests()
    {
        // Use NullLogger to avoid test output noise
        _loader = new ProjectFileSolutionLoader(NullLogger<ProjectFileSolutionLoader>.Instance);
    }

    #region CanLoad Tests

    [Fact]
    public void CanLoad_ValidSolutionPath_ReturnsTrue()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeTrue("solution file exists and has .sln extension");
    }

    [Fact]
    public void CanLoad_MissingFile_ReturnsFalse()
    {
        // Arrange
        var solutionPath = "D:\\nonexistent\\solution.sln";

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse("file does not exist");
    }

    [Fact]
    public void CanLoad_NullPath_ReturnsFalse()
    {
        // Arrange
        string? solutionPath = null;

        // Act
        var result = _loader.CanLoad(solutionPath!);

        // Assert
        result.Should().BeFalse("path is null");
    }

    [Fact]
    public void CanLoad_EmptyPath_ReturnsFalse()
    {
        // Arrange
        var solutionPath = string.Empty;

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse("path is empty");
    }

    [Fact]
    public void CanLoad_WhitespacePath_ReturnsFalse()
    {
        // Arrange
        var solutionPath = "   ";

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse("path is whitespace");
    }

    [Fact]
    public void CanLoad_NonSlnExtension_ReturnsFalse()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/Core/Core.csproj");

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse("file is not a .sln file");
    }

    #endregion

    #region LoadAsync Tests

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
        analysis.Projects.Should().HaveCount(7, "sample solution has 7 projects");
        analysis.LoaderType.Should().Be("ProjectFile", "loader type indicates XML parsing was used");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsProjectReferences()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        var servicesProject = analysis.Projects.FirstOrDefault(p => p.Name == "Services");
        servicesProject.Should().NotBeNull("Services project should exist");
        servicesProject!.References.Should().Contain(r =>
            r.TargetName == "Core" && r.Type == ReferenceType.ProjectReference,
            "Services project references Core project");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_FiltersFrameworkAssemblies()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - Framework assemblies should be filtered out
        var coreProject = analysis.Projects.FirstOrDefault(p => p.Name == "Core");
        coreProject.Should().NotBeNull("Core project should exist");
        coreProject!.References.Where(r => r.Type == ReferenceType.AssemblyReference)
            .Should().NotContain(r => r.TargetName.StartsWith("System."),
                "System.* framework assemblies should be filtered");
        coreProject.References.Where(r => r.Type == ReferenceType.AssemblyReference)
            .Should().NotContain(r => r.TargetName.StartsWith("Microsoft."),
                "Microsoft.* framework assemblies should be filtered");
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
            p.TargetFramework.Should().Match(tf => tf == "net8.0" || tf == "unknown",
                "projects should have net8.0 or unknown target framework"));
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_DeterminesLanguage()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - Sample solution is all C#
        analysis.Projects.Should().AllSatisfy(p => p.Language.Should().Be("C#",
            "all projects in sample solution are C# (.csproj files)"));
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsProjectNames()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        var projectNames = analysis.Projects.Select(p => p.Name).ToList();
        projectNames.Should().Contain("Common", "Common project exists");
        projectNames.Should().Contain("Core", "Core project exists");
        projectNames.Should().Contain("Infrastructure", "Infrastructure project exists");
        projectNames.Should().Contain("Services", "Services project exists");
        projectNames.Should().Contain("UI", "UI project exists");
        projectNames.Should().Contain("Legacy.ModuleA", "Legacy.ModuleA project exists");
        projectNames.Should().Contain("Legacy.ModuleB", "Legacy.ModuleB project exists");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_BuildsDependencyGraph()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - Verify expected dependency structure
        var coreProject = analysis.Projects.First(p => p.Name == "Core");
        coreProject.References.Should().Contain(r => r.TargetName == "Common" && r.Type == ReferenceType.ProjectReference,
            "Core depends on Common");

        var infrastructureProject = analysis.Projects.First(p => p.Name == "Infrastructure");
        infrastructureProject.References.Should().Contain(r => r.TargetName == "Core" && r.Type == ReferenceType.ProjectReference,
            "Infrastructure depends on Core");

        var servicesProject = analysis.Projects.First(p => p.Name == "Services");
        servicesProject.References.Should().Contain(r => r.TargetName == "Core" && r.Type == ReferenceType.ProjectReference,
            "Services depends on Core");
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
        await act.Should().ThrowAsync<OperationCanceledException>(
            "operation was cancelled before completion");
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
            .WithMessage("*Failed to load solution via project file parsing*",
                "solution file cannot be read");
    }

    [Fact]
    public async Task LoadAsync_SolutionWithNoProjects_ThrowsProjectFileLoadException()
    {
        // Arrange - Create empty .sln file for testing
        var tempSlnPath = Path.Combine(Path.GetTempPath(), $"EmptySolution_{Guid.NewGuid()}.sln");
        await File.WriteAllTextAsync(tempSlnPath, "Microsoft Visual Studio Solution File, Format Version 12.00\n");

        try
        {
            // Act
            Func<Task> act = async () => await _loader.LoadAsync(tempSlnPath);

            // Assert
            await act.Should().ThrowAsync<ProjectFileLoadException>()
                .WithMessage("*No valid projects found*",
                    "solution has no project entries");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempSlnPath))
                File.Delete(tempSlnPath);
        }
    }

    [Fact]
    public async Task LoadAsync_SolutionWithInvalidXml_ThrowsProjectFileLoadException()
    {
        // Arrange - Create solution with project that has invalid XML
        var tempDir = Path.Combine(Path.GetTempPath(), $"InvalidXmlTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var tempSlnPath = Path.Combine(tempDir, "TestSolution.sln");
        var tempProjPath = Path.Combine(tempDir, "TestProject.csproj");

        try
        {
            // Create solution file
            var slnContent = @"Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""TestProject"", ""TestProject.csproj"", ""{12345678-1234-1234-1234-123456789012}""
EndProject";
            await File.WriteAllTextAsync(tempSlnPath, slnContent);

            // Create invalid project XML
            await File.WriteAllTextAsync(tempProjPath, "<Project><PropertyGroup><TargetFramework>net8.0");

            // Act
            Func<Task> act = async () => await _loader.LoadAsync(tempSlnPath);

            // Assert - When all projects fail to parse, we get "All projects failed to parse" message
            await act.Should().ThrowAsync<ProjectFileLoadException>()
                .WithMessage("*All projects failed to parse*",
                    "project file has malformed XML causing all projects to fail");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadAsync_SolutionWithSolutionFolders_SkipsFolders()
    {
        // Arrange - Create solution with solution folders
        var tempDir = Path.Combine(Path.GetTempPath(), $"SolutionFolderTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var tempSlnPath = Path.Combine(tempDir, "TestSolution.sln");
        var tempProjPath = Path.Combine(tempDir, "TestProject.csproj");

        try
        {
            // Create solution file with solution folder
            var slnContent = @"Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""SolutionFolder"", ""SolutionFolder"", ""{11111111-1111-1111-1111-111111111111}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""TestProject"", ""TestProject.csproj"", ""{12345678-1234-1234-1234-123456789012}""
EndProject";
            await File.WriteAllTextAsync(tempSlnPath, slnContent);

            // Create valid project file
            var projContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
            await File.WriteAllTextAsync(tempProjPath, projContent);

            // Act
            var analysis = await _loader.LoadAsync(tempSlnPath);

            // Assert
            analysis.Projects.Should().HaveCount(1, "solution folders should be skipped");
            analysis.Projects.First().Name.Should().Be("TestProject");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadAsync_SolutionWithMultiTargeting_ExtractsFirstFramework()
    {
        // Arrange - Create project with multi-targeting
        var tempDir = Path.Combine(Path.GetTempPath(), $"MultiTargetTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var tempSlnPath = Path.Combine(tempDir, "TestSolution.sln");
        var tempProjPath = Path.Combine(tempDir, "TestProject.csproj");

        try
        {
            // Create solution file
            var slnContent = @"Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""TestProject"", ""TestProject.csproj"", ""{12345678-1234-1234-1234-123456789012}""
EndProject";
            await File.WriteAllTextAsync(tempSlnPath, slnContent);

            // Create project with TargetFrameworks (plural)
            var projContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net472;net6.0</TargetFrameworks>
  </PropertyGroup>
</Project>";
            await File.WriteAllTextAsync(tempProjPath, projContent);

            // Act
            var analysis = await _loader.LoadAsync(tempSlnPath);

            // Assert
            analysis.Projects.First().TargetFramework.Should().Be("net8.0",
                "first framework from TargetFrameworks should be extracted");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadAsync_LegacyFrameworkProject_ExtractsTargetFrameworkVersion()
    {
        // Arrange - Create legacy .NET Framework project
        var tempDir = Path.Combine(Path.GetTempPath(), $"LegacyFrameworkTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var tempSlnPath = Path.Combine(tempDir, "TestSolution.sln");
        var tempProjPath = Path.Combine(tempDir, "TestProject.csproj");

        try
        {
            // Create solution file
            var slnContent = @"Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""TestProject"", ""TestProject.csproj"", ""{12345678-1234-1234-1234-123456789012}""
EndProject";
            await File.WriteAllTextAsync(tempSlnPath, slnContent);

            // Create legacy project with TargetFrameworkVersion
            var projContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  </PropertyGroup>
</Project>";
            await File.WriteAllTextAsync(tempProjPath, projContent);

            // Act
            var analysis = await _loader.LoadAsync(tempSlnPath);

            // Assert
            analysis.Projects.First().TargetFramework.Should().Be("net472",
                "TargetFrameworkVersion v4.7.2 should be converted to net472");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion
}
