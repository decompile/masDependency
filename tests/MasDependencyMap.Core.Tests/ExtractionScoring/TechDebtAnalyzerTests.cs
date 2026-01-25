namespace MasDependencyMap.Core.Tests.ExtractionScoring;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.ExtractionScoring;
using MasDependencyMap.Core.DependencyAnalysis;

public class TechDebtAnalyzerTests
{
    private readonly ILogger<TechDebtAnalyzer> _logger;
    private readonly TechDebtAnalyzer _analyzer;
    private readonly string _testDataPath;

    public TechDebtAnalyzerTests()
    {
        _logger = NullLogger<TechDebtAnalyzer>.Instance;
        _analyzer = new TechDebtAnalyzer(_logger);
        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "TechDebt");
    }

    [Fact]
    public async Task AnalyzeAsync_Net35Project_Returns100Score()
    {
        // Arrange: Project targeting .NET Framework 3.5 (highest debt)
        var projectPath = Path.Combine(_testDataPath, "net35-project.csproj");
        var project = new ProjectNode
        {
            ProjectName = "net35-project",
            ProjectPath = projectPath,
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.ProjectName.Should().Be("net35-project");
        metric.ProjectPath.Should().Be(projectPath);
        metric.TargetFramework.Should().Be("net3.5");
        metric.NormalizedScore.Should().Be(100);
    }

    [Fact]
    public async Task AnalyzeAsync_Net48Project_Returns40Score()
    {
        // Arrange: Project targeting .NET Framework 4.8 (legacy but recent)
        var projectPath = Path.Combine(_testDataPath, "net48-project.csproj");
        var project = new ProjectNode
        {
            ProjectName = "net48-project",
            ProjectPath = projectPath,
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.ProjectName.Should().Be("net48-project");
        metric.ProjectPath.Should().Be(projectPath);
        metric.TargetFramework.Should().Be("net4.8");
        metric.NormalizedScore.Should().Be(40);
    }

    [Fact]
    public async Task AnalyzeAsync_NetCore31Project_Returns30Score()
    {
        // Arrange: Project targeting .NET Core 3.1 (old modern)
        var projectPath = Path.Combine(_testDataPath, "netcoreapp31-project.csproj");
        var project = new ProjectNode
        {
            ProjectName = "netcoreapp31-project",
            ProjectPath = projectPath,
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.ProjectName.Should().Be("netcoreapp31-project");
        metric.ProjectPath.Should().Be(projectPath);
        metric.TargetFramework.Should().Be("netcoreapp3.1");
        metric.NormalizedScore.Should().Be(30);
    }

    [Fact]
    public async Task AnalyzeAsync_Net60Project_Returns10Score()
    {
        // Arrange: Project targeting .NET 6 (recent modern)
        var projectPath = Path.Combine(_testDataPath, "net60-project.csproj");
        var project = new ProjectNode
        {
            ProjectName = "net60-project",
            ProjectPath = projectPath,
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.ProjectName.Should().Be("net60-project");
        metric.ProjectPath.Should().Be(projectPath);
        metric.TargetFramework.Should().Be("net6.0");
        metric.NormalizedScore.Should().Be(10);
    }

    [Fact]
    public async Task AnalyzeAsync_Net80Project_Returns0Score()
    {
        // Arrange: Project targeting .NET 8 (current, no debt)
        var projectPath = Path.Combine(_testDataPath, "net80-project.csproj");
        var project = new ProjectNode
        {
            ProjectName = "net80-project",
            ProjectPath = projectPath,
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.ProjectName.Should().Be("net80-project");
        metric.ProjectPath.Should().Be(projectPath);
        metric.TargetFramework.Should().Be("net8.0");
        metric.NormalizedScore.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_LegacyFrameworkFormat_ParsesCorrectly()
    {
        // Arrange: Legacy format <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
        var projectPath = Path.Combine(_testDataPath, "legacy-project.csproj");
        var project = new ProjectNode
        {
            ProjectName = "legacy-project",
            ProjectPath = projectPath,
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.ProjectName.Should().Be("legacy-project");
        metric.ProjectPath.Should().Be(projectPath);
        metric.TargetFramework.Should().Be("net472");
        metric.NormalizedScore.Should().Be(40);
    }

    [Fact]
    public async Task AnalyzeAsync_MultiTargeting_UsesFirstTarget()
    {
        // Arrange: Multi-targeting <TargetFrameworks>netstandard2.0;net6.0</TargetFrameworks>
        var projectPath = Path.Combine(_testDataPath, "multitarget-project.csproj");
        var project = new ProjectNode
        {
            ProjectName = "multitarget-project",
            ProjectPath = projectPath,
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.ProjectName.Should().Be("multitarget-project");
        metric.ProjectPath.Should().Be(projectPath);
        metric.TargetFramework.Should().Be("netstandard2.0");
        // netstandard2.0 now scored appropriately (added to scoring dictionary)
        metric.NormalizedScore.Should().Be(50);
    }

    [Fact]
    public async Task AnalyzeAsync_InvalidProjectFile_ReturnsFallbackScore50()
    {
        // Arrange: Invalid project path
        var project = new ProjectNode
        {
            ProjectName = "InvalidProject",
            ProjectPath = "C:\\NonExistent\\Project.csproj",
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };

        // Act
        var metric = await _analyzer.AnalyzeAsync(project);

        // Assert
        metric.ProjectName.Should().Be("InvalidProject");
        metric.TargetFramework.Should().Be("unknown");
        metric.NormalizedScore.Should().Be(50); // Neutral fallback score
    }

    [Fact]
    public async Task AnalyzeAsync_NullProject_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _analyzer.AnalyzeAsync(null!));
    }

    [Fact]
    public async Task AnalyzeAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var projectPath = Path.Combine(_testDataPath, "net80-project.csproj");
        var project = new ProjectNode
        {
            ProjectName = "net80-project",
            ProjectPath = projectPath,
            TargetFramework = string.Empty,
            SolutionName = "TestSolution"
        };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        // TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _analyzer.AnalyzeAsync(project, cts.Token));
    }
}
