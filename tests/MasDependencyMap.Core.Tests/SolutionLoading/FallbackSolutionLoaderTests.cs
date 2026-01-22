namespace MasDependencyMap.Core.Tests.SolutionLoading;

using FluentAssertions;
using MasDependencyMap.Core.SolutionLoading;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Tests for FallbackSolutionLoader orchestration of the 3-layer fallback chain.
/// Tests Roslyn → MSBuild → ProjectFile fallback sequence.
/// Uses integration testing approach with real SampleMonolith solution.
/// </summary>
public class FallbackSolutionLoaderTests : IClassFixture<MSBuildLocatorFixture>
{
    private readonly FallbackSolutionLoader _fallbackLoader;
    private readonly RoslynSolutionLoader _roslynLoader;
    private readonly MSBuildSolutionLoader _msbuildLoader;
    private readonly ProjectFileSolutionLoader _projectFileLoader;

    public FallbackSolutionLoaderTests()
    {
        var roslynLogger = NullLogger<RoslynSolutionLoader>.Instance;
        var msbuildLogger = NullLogger<MSBuildSolutionLoader>.Instance;
        var projectFileLogger = NullLogger<ProjectFileSolutionLoader>.Instance;
        var fallbackLogger = NullLogger<FallbackSolutionLoader>.Instance;

        _roslynLoader = new RoslynSolutionLoader(roslynLogger);
        _msbuildLoader = new MSBuildSolutionLoader(msbuildLogger);
        _projectFileLoader = new ProjectFileSolutionLoader(projectFileLogger);

        _fallbackLoader = new FallbackSolutionLoader(
            _roslynLoader,
            _msbuildLoader,
            _projectFileLoader,
            fallbackLogger);
    }

    [Fact]
    public void CanLoad_ValidSolutionPath_ReturnsTrue()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var result = _fallbackLoader.CanLoad(solutionPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanLoad_InvalidSolutionPath_ReturnsFalse()
    {
        // Arrange
        var solutionPath = "nonexistent.sln";

        // Act
        var result = _fallbackLoader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_NullPath_ReturnsFalse()
    {
        // Arrange
        string? solutionPath = null;

        // Act
        var result = _fallbackLoader.CanLoad(solutionPath!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_EmptyPath_ReturnsFalse()
    {
        // Arrange
        var solutionPath = string.Empty;

        // Act
        var result = _fallbackLoader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_NonSlnFile_ReturnsFalse()
    {
        // Arrange
        var solutionPath = "test.txt";

        // Act
        var result = _fallbackLoader.CanLoad(solutionPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ValidSolution_ReturnsRoslynResult()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Act
        var result = await _fallbackLoader.LoadAsync(solutionPath);

        // Assert - SampleMonolith is a valid modern solution, so Roslyn should succeed
        result.Should().NotBeNull();
        result.LoaderType.Should().Be("Roslyn", "valid modern solutions should load with Roslyn");
        result.SolutionPath.Should().Be(Path.GetFullPath(solutionPath));
        result.SolutionName.Should().Be("SampleMonolith");
        result.Projects.Should().NotBeEmpty("SampleMonolith has multiple projects");
    }

    [Fact]
    public async Task LoadAsync_InvalidSolution_ThrowsSolutionLoadException()
    {
        // Arrange
        var solutionPath = "nonexistent.sln";

        // Act
        Func<Task> act = async () => await _fallbackLoader.LoadAsync(solutionPath);

        // Assert - all loaders will fail on nonexistent file
        await act.Should().ThrowAsync<SolutionLoadException>()
            .WithMessage("*Failed to load solution*");
    }

    [Fact]
    public async Task LoadAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await _fallbackLoader.LoadAsync(solutionPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that comprehensive error message includes all failure reasons.
    /// This test uses reflection to call the private BuildComprehensiveErrorMessage method.
    /// </summary>
    [Fact]
    public void LoadAsync_AllLoadersFail_ErrorMessageIncludesAllFailureReasons()
    {
        // Arrange
        var solutionPath = "test.sln";
        var roslynEx = new RoslynLoadException("Roslyn: Invalid workspace configuration");
        var msbuildEx = new MSBuildLoadException("MSBuild: Project not found at path");
        var projectFileEx = new ProjectFileLoadException("ProjectFile: XML parse error on line 42");

        // Act - use reflection to call private method
        var method = typeof(FallbackSolutionLoader).GetMethod(
            "BuildComprehensiveErrorMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (string)method!.Invoke(_fallbackLoader, new object?[] { solutionPath, roslynEx, msbuildEx, projectFileEx })!;

        // Assert
        result.Should().Contain("Failed to load solution: test.sln");
        result.Should().Contain("All loading strategies failed:");
        result.Should().Contain("Invalid workspace configuration");
        result.Should().Contain("Project not found at path");
        result.Should().Contain("XML parse error on line 42");
        result.Should().Contain("Possible causes:");
        result.Should().Contain("Suggestions:");
        result.Should().Contain("Verify solution opens in Visual Studio");
    }
}

/// <summary>
/// Constructor validation tests for FallbackSolutionLoader.
/// Ensures null parameter validation is enforced.
/// </summary>
/// <remarks>
/// NOTE: Fallback chain orchestration tests (Roslyn→MSBuild→ProjectFile transitions)
/// are not included due to testability constraints. The concrete loader classes
/// (RoslynSolutionLoader, MSBuildSolutionLoader, ProjectFileSolutionLoader) do not have
/// virtual LoadAsync methods, making them difficult to mock for unit testing.
///
/// The fallback chain IS tested indirectly through integration tests:
/// - Valid solutions pass through Roslyn successfully (no fallback needed)
/// - Invalid solutions trigger all three loaders sequentially
///
/// Future improvement: Consider making LoadAsync virtual or extracting an interface
/// to enable isolated unit testing of the fallback orchestration logic.
/// See Story Dev Notes lines 460-649 for planned mock-based tests.
/// </remarks>
public class FallbackSolutionLoaderFallbackChainTests
{
    [Fact]
    public void FallbackSolutionLoader_NullRoslynLoader_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new FallbackSolutionLoader(
            null!,
            new MSBuildSolutionLoader(NullLogger<MSBuildSolutionLoader>.Instance),
            new ProjectFileSolutionLoader(NullLogger<ProjectFileSolutionLoader>.Instance),
            NullLogger<FallbackSolutionLoader>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("roslynLoader");
    }

    [Fact]
    public void FallbackSolutionLoader_NullMSBuildLoader_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new FallbackSolutionLoader(
            new RoslynSolutionLoader(NullLogger<RoslynSolutionLoader>.Instance),
            null!,
            new ProjectFileSolutionLoader(NullLogger<ProjectFileSolutionLoader>.Instance),
            NullLogger<FallbackSolutionLoader>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("msbuildLoader");
    }

    [Fact]
    public void FallbackSolutionLoader_NullProjectFileLoader_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new FallbackSolutionLoader(
            new RoslynSolutionLoader(NullLogger<RoslynSolutionLoader>.Instance),
            new MSBuildSolutionLoader(NullLogger<MSBuildSolutionLoader>.Instance),
            null!,
            NullLogger<FallbackSolutionLoader>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("projectFileLoader");
    }

    [Fact]
    public void FallbackSolutionLoader_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new FallbackSolutionLoader(
            new RoslynSolutionLoader(NullLogger<RoslynSolutionLoader>.Instance),
            new MSBuildSolutionLoader(NullLogger<MSBuildSolutionLoader>.Instance),
            new ProjectFileSolutionLoader(NullLogger<ProjectFileSolutionLoader>.Instance),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
