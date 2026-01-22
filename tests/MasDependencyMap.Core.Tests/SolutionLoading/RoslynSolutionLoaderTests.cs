using FluentAssertions;
using MasDependencyMap.Core.SolutionLoading;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MasDependencyMap.Core.Tests.SolutionLoading;

/// <summary>
/// Unit tests for RoslynSolutionLoader.
/// Tests solution loading, project extraction, reference extraction, and error handling.
/// Uses samples/SampleMonolith solution from Story 1-7 for integration testing.
/// </summary>
public class RoslynSolutionLoaderTests : IClassFixture<MSBuildLocatorFixture>
{
    private readonly RoslynSolutionLoader _loader;

    public RoslynSolutionLoaderTests(MSBuildLocatorFixture fixture)
    {
        // Use NullLogger to avoid test output noise
        _loader = new RoslynSolutionLoader(NullLogger<RoslynSolutionLoader>.Instance);
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
    public void CanLoad_NonSlnFile_ReturnsFalse()
    {
        // Arrange - use any existing file that's not a .sln
        var solutionPath = Path.GetFullPath("README.md");

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse("file does not have .sln extension");
    }

    #endregion

    #region LoadAsync - Basic Tests

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
        analysis.Projects.Should().HaveCount(7, "sample solution has 7 projects from Story 1-7");
        analysis.LoaderType.Should().Be("Roslyn");
        analysis.SolutionPath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsProjectNames()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - verify all 7 expected projects from Story 1-7
        var projectNames = analysis.Projects.Select(p => p.Name).ToList();
        projectNames.Should().Contain("Common");
        projectNames.Should().Contain("Core");
        projectNames.Should().Contain("Infrastructure");
        projectNames.Should().Contain("Services");
        projectNames.Should().Contain("UI");
        projectNames.Should().Contain("Legacy.ModuleA");
        projectNames.Should().Contain("Legacy.ModuleB");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsProjectPaths()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        foreach (var project in analysis.Projects)
        {
            project.FilePath.Should().NotBeNullOrEmpty($"project {project.Name} should have file path");
            project.FilePath.Should().EndWith(".csproj", $"project {project.Name} should be C# project");
            File.Exists(project.FilePath).Should().BeTrue($"project file should exist for {project.Name}");
        }
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsLanguages()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        foreach (var project in analysis.Projects)
        {
            project.Language.Should().Be("C#", $"all sample projects are C# projects");
        }
    }

    #endregion

    #region LoadAsync - Reference Extraction Tests

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsProjectReferences()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - verify expected project references from Story 1-7 dependency structure
        var servicesProject = analysis.Projects.First(p => p.Name == "Services");
        var projectRefs = servicesProject.References.Where(r => r.Type == ReferenceType.ProjectReference).ToList();

        projectRefs.Should().Contain(r => r.TargetName == "Core", "Services depends on Core");
        projectRefs.Should().Contain(r => r.TargetName == "Infrastructure", "Services depends on Infrastructure");
        projectRefs.Should().Contain(r => r.TargetName == "Common", "Services depends on Common");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_FiltersFrameworkAssemblies()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        var coreProject = analysis.Projects.First(p => p.Name == "Core");
        var assemblyRefs = coreProject.References.Where(r => r.Type == ReferenceType.AssemblyReference).ToList();

        // Framework assemblies (System.*, Microsoft.*) should be filtered out to reduce noise
        var hasSystemRefs = assemblyRefs.Any(r => r.TargetName.StartsWith("System", StringComparison.OrdinalIgnoreCase));
        var hasMicrosoftRefs = assemblyRefs.Any(r => r.TargetName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase));

        hasSystemRefs.Should().BeFalse("System.* framework assemblies should be filtered out");
        hasMicrosoftRefs.Should().BeFalse("Microsoft.* framework assemblies should be filtered out");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_DifferentiatesReferenceTypes()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - verify all projects have both types properly categorized
        foreach (var project in analysis.Projects)
        {
            var projectRefs = project.References.Where(r => r.Type == ReferenceType.ProjectReference).ToList();
            var assemblyRefs = project.References.Where(r => r.Type == ReferenceType.AssemblyReference).ToList();

            // All references should be categorized
            (projectRefs.Count + assemblyRefs.Count).Should().Be(project.References.Count,
                $"all references for {project.Name} should be categorized");

            // Assembly references should have TargetPath
            foreach (var assemblyRef in assemblyRefs)
            {
                assemblyRef.TargetPath.Should().NotBeNullOrEmpty(
                    $"assembly reference {assemblyRef.TargetName} should have target path");
            }
        }
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsUIProjectReferences()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - UI depends on Services (from Story 1-7)
        var uiProject = analysis.Projects.First(p => p.Name == "UI");
        var projectRefs = uiProject.References.Where(r => r.Type == ReferenceType.ProjectReference).ToList();

        projectRefs.Should().Contain(r => r.TargetName == "Services", "UI depends on Services");
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsLegacyModuleReferences()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - From Story 1-7: Circular dependency between Legacy modules was attempted
        // but failed due to MSBuild MSB4006 error. Modern SDK-style projects do NOT allow circular refs.
        // Therefore, Legacy.ModuleA and Legacy.ModuleB exist but have NO project references to each other.
        var moduleA = analysis.Projects.First(p => p.Name == "Legacy.ModuleA");
        var moduleB = analysis.Projects.First(p => p.Name == "Legacy.ModuleB");

        moduleA.Should().NotBeNull("Legacy.ModuleA should exist in solution");
        moduleB.Should().NotBeNull("Legacy.ModuleB should exist in solution");

        // Verify both modules are loaded successfully (even without circular refs)
        var projectRefs = moduleA.References.Where(r => r.Type == ReferenceType.ProjectReference).ToList();
        projectRefs.Should().BeEmpty("Legacy.ModuleA has no project references due to MSBuild circular ref constraint");
    }

    #endregion

    #region LoadAsync - Error Handling Tests

    [Fact]
    public async Task LoadAsync_InvalidSolutionPath_ThrowsRoslynLoadException()
    {
        // Arrange
        var solutionPath = "D:\\invalid\\solution.sln";

        // Act
        Func<Task> act = async () => await _loader.LoadAsync(solutionPath);

        // Assert
        await act.Should().ThrowAsync<RoslynLoadException>()
            .WithMessage("*Failed to load solution*");
    }

    [Fact]
    public async Task LoadAsync_NonexistentFile_ThrowsRoslynLoadException()
    {
        // Arrange
        var solutionPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".sln");

        // Act
        Func<Task> act = async () => await _loader.LoadAsync(solutionPath);

        // Assert
        await act.Should().ThrowAsync<RoslynLoadException>();
    }

    [Fact]
    public async Task LoadAsync_RoslynLoadException_PreservesInnerException()
    {
        // Arrange
        var solutionPath = "D:\\invalid\\solution.sln";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RoslynLoadException>(
            async () => await _loader.LoadAsync(solutionPath));

        exception.InnerException.Should().NotBeNull("inner exception should be preserved for debugging");
    }

    #endregion

    #region Target Framework Tests

    [Fact]
    public async Task LoadAsync_SampleSolution_HandlesTargetFramework()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - target framework extraction (may be "unknown" for now, enhanced in future stories)
        foreach (var project in analysis.Projects)
        {
            project.TargetFramework.Should().NotBeNullOrEmpty(
                $"project {project.Name} should have target framework value");
        }
    }

    #endregion
}
