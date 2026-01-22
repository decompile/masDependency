namespace MasDependencyMap.Core.Tests.SolutionLoading;

using FluentAssertions;
using MasDependencyMap.Core.SolutionLoading;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class MSBuildSolutionLoaderTests : IClassFixture<MSBuildLocatorFixture>
{
    private readonly MSBuildSolutionLoader _loader;

    public MSBuildSolutionLoaderTests()
    {
        _loader = new MSBuildSolutionLoader(NullLogger<MSBuildSolutionLoader>.Instance);
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
    public void CanLoad_EmptyPath_ReturnsFalse()
    {
        // Arrange
        var solutionPath = "";

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_NullPath_ReturnsFalse()
    {
        // Arrange
        string solutionPath = null!;

        // Act
        var result = _loader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_NonSlnExtension_ReturnsFalse()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/src/Common/Common.csproj");

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
        analysis.LoaderType.Should().Be("MSBuild");
        analysis.SolutionPath.Should().NotBeNullOrEmpty();
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
        servicesProject.Should().NotBeNull();
        servicesProject!.References.Should().Contain(r =>
            r.TargetName == "Core" && r.Type == ReferenceType.ProjectReference);
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ExtractsDllReferences()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert - Framework assemblies should be filtered out
        var coreProject = analysis.Projects.FirstOrDefault(p => p.Name == "Core");
        coreProject.Should().NotBeNull();
        coreProject!.References.Where(r => r.Type == ReferenceType.AssemblyReference)
            .Should().NotContain(r => r.TargetName.StartsWith("System."));
    }

    [Fact]
    public async Task LoadAsync_InvalidSolutionPath_ThrowsMSBuildLoadException()
    {
        // Arrange
        var solutionPath = "D:\\invalid\\solution.sln";

        // Act
        Func<Task> act = async () => await _loader.LoadAsync(solutionPath);

        // Assert
        await act.Should().ThrowAsync<MSBuildLoadException>()
            .WithMessage("*Failed to load solution via MSBuild*");
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
    public async Task LoadAsync_SampleSolution_ExtractsProjectFilePaths()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        analysis.Projects.Should().AllSatisfy(p =>
        {
            p.FilePath.Should().NotBeNullOrEmpty();
            p.FilePath.Should().EndWith(".csproj");
        });
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ProjectsHaveNames()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        analysis.Projects.Should().AllSatisfy(p => p.Name.Should().NotBeNullOrEmpty());
        analysis.Projects.Select(p => p.Name).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task LoadAsync_SampleSolution_ServicesProjectHasMultipleReferences()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var analysis = await _loader.LoadAsync(solutionPath);

        // Assert
        var servicesProject = analysis.Projects.FirstOrDefault(p => p.Name == "Services");
        servicesProject.Should().NotBeNull();
        servicesProject!.References.Where(r => r.Type == ReferenceType.ProjectReference)
            .Should().HaveCountGreaterThanOrEqualTo(2); // Services depends on Core, Infrastructure, Common
    }
}
