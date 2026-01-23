using FluentAssertions;
using MasDependencyMap.Core.SolutionLoading;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MasDependencyMap.Core.Tests.SolutionLoading;

/// <summary>
/// Unit tests for MultiSolutionAnalyzer.
/// Tests multi-solution loading, progress reporting, error handling, and graceful degradation.
/// </summary>
public class MultiSolutionAnalyzerTests
{
    private readonly Mock<ISolutionLoader> _mockSolutionLoader;
    private readonly MultiSolutionAnalyzer _analyzer;

    public MultiSolutionAnalyzerTests()
    {
        _mockSolutionLoader = new Mock<ISolutionLoader>();
        _analyzer = new MultiSolutionAnalyzer(
            _mockSolutionLoader.Object,
            NullLogger<MultiSolutionAnalyzer>.Instance);
    }

    #region LoadAllAsync - Success Scenarios

    [Fact]
    public async Task LoadAllAsync_MultipleSolutions_ReturnsAllAnalyses()
    {
        // Arrange
        var paths = new[] { "Solution1.sln", "Solution2.sln" };

        // Create temp files to satisfy File.Exists check
        foreach (var path in paths)
        {
            File.WriteAllText(path, "");
        }

        var analysis1 = new SolutionAnalysis
        {
            SolutionPath = "Solution1.sln",
            SolutionName = "Solution1",
            Projects = Array.Empty<ProjectInfo>(),
            LoaderType = "Roslyn"
        };

        var analysis2 = new SolutionAnalysis
        {
            SolutionPath = "Solution2.sln",
            SolutionName = "Solution2",
            Projects = Array.Empty<ProjectInfo>(),
            LoaderType = "Roslyn"
        };

        _mockSolutionLoader
            .SetupSequence(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis1)
            .ReturnsAsync(analysis2);

        try
        {
            // Act
            var results = await _analyzer.LoadAllAsync(paths);

            // Assert
            results.Should().HaveCount(2);
            results[0].SolutionPath.Should().Be("Solution1.sln");
            results[1].SolutionPath.Should().Be("Solution2.sln");
        }
        finally
        {
            // Cleanup
            foreach (var path in paths)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task LoadAllAsync_SingleSolution_ReturnsSingleAnalysis()
    {
        // Arrange
        var paths = new[] { "SingleSolution.sln" };
        File.WriteAllText(paths[0], "");

        var analysis = new SolutionAnalysis
        {
            SolutionPath = "SingleSolution.sln",
            SolutionName = "SingleSolution",
            Projects = Array.Empty<ProjectInfo>(),
            LoaderType = "Roslyn"
        };

        _mockSolutionLoader
            .Setup(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        try
        {
            // Act
            var results = await _analyzer.LoadAllAsync(paths);

            // Assert
            results.Should().ContainSingle();
            results[0].SolutionName.Should().Be("SingleSolution");
        }
        finally
        {
            if (File.Exists(paths[0]))
                File.Delete(paths[0]);
        }
    }

    #endregion

    #region LoadAllAsync - Graceful Degradation

    [Fact]
    public async Task LoadAllAsync_OneSolutionFails_ContinuesWithRemaining()
    {
        // Arrange
        var paths = new[] { "Solution1.sln", "Solution2.sln", "Solution3.sln" };

        foreach (var path in paths)
        {
            File.WriteAllText(path, "");
        }

        var analysis1 = new SolutionAnalysis
        {
            SolutionPath = "Solution1.sln",
            SolutionName = "Solution1",
            Projects = Array.Empty<ProjectInfo>(),
            LoaderType = "Roslyn"
        };

        var analysis3 = new SolutionAnalysis
        {
            SolutionPath = "Solution3.sln",
            SolutionName = "Solution3",
            Projects = Array.Empty<ProjectInfo>(),
            LoaderType = "Roslyn"
        };

        _mockSolutionLoader
            .SetupSequence(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis1)
            .ThrowsAsync(new SolutionLoadException("Solution2 failed"))
            .ReturnsAsync(analysis3);

        try
        {
            // Act
            var results = await _analyzer.LoadAllAsync(paths);

            // Assert
            results.Should().HaveCount(2);
            results[0].SolutionName.Should().Be("Solution1");
            results[1].SolutionName.Should().Be("Solution3");
        }
        finally
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task LoadAllAsync_AllSolutionsFail_ThrowsSolutionLoadException()
    {
        // Arrange
        var paths = new[] { "Solution1.sln", "Solution2.sln" };

        foreach (var path in paths)
        {
            File.WriteAllText(path, "");
        }

        _mockSolutionLoader
            .Setup(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SolutionLoadException("Load failed"));

        try
        {
            // Act
            Func<Task> act = async () => await _analyzer.LoadAllAsync(paths);

            // Assert
            await act.Should().ThrowAsync<SolutionLoadException>()
                .WithMessage("Failed to load all*");
        }
        finally
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    #endregion

    #region LoadAllAsync - Validation Tests

    [Fact]
    public async Task LoadAllAsync_NullSolutionPaths_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _analyzer.LoadAllAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("solutionPaths");
    }

    [Fact]
    public async Task LoadAllAsync_EmptySolutionPaths_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _analyzer.LoadAllAsync(Array.Empty<string>());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("solutionPaths")
            .WithMessage("No solution paths provided*");
    }

    [Fact]
    public async Task LoadAllAsync_NullPathInList_ThrowsArgumentException()
    {
        // Arrange
        var paths = new string[] { "Valid.sln", null! };

        // Act
        Func<Task> act = async () => await _analyzer.LoadAllAsync(paths);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("solutionPaths")
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public async Task LoadAllAsync_MissingFile_ThrowsArgumentException()
    {
        // Arrange
        var paths = new[] { "NonExistent1.sln", "NonExistent2.sln" };

        // Act
        Func<Task> act = async () => await _analyzer.LoadAllAsync(paths);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Solution files not found*")
            .WithParameterName("solutionPaths");
    }

    #endregion

    #region LoadAllAsync - Progress Reporting

    [Fact]
    public async Task LoadAllAsync_WithProgress_ReportsEachSolution()
    {
        // Arrange
        var paths = new[] { "Solution1.sln", "Solution2.sln" };

        foreach (var path in paths)
        {
            File.WriteAllText(path, "");
        }

        var analysis1 = new SolutionAnalysis
        {
            SolutionPath = "Solution1.sln",
            SolutionName = "Solution1",
            Projects = new List<ProjectInfo> { new ProjectInfo { Name = "Proj1", FilePath = "proj1.csproj", TargetFramework = "net8.0", References = Array.Empty<ProjectReference>() } },
            LoaderType = "Roslyn"
        };

        var analysis2 = new SolutionAnalysis
        {
            SolutionPath = "Solution2.sln",
            SolutionName = "Solution2",
            Projects = new List<ProjectInfo> { new ProjectInfo { Name = "Proj2", FilePath = "proj2.csproj", TargetFramework = "net8.0", References = Array.Empty<ProjectReference>() } },
            LoaderType = "Roslyn"
        };

        _mockSolutionLoader
            .SetupSequence(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis1)
            .ReturnsAsync(analysis2);

        var progressReports = new List<SolutionLoadProgress>();
        var progress = new Progress<SolutionLoadProgress>(p => progressReports.Add(p));

        try
        {
            // Act
            await _analyzer.LoadAllAsync(paths, progress);

            // Assert
            progressReports.Count.Should().BeGreaterThanOrEqualTo(2);
            progressReports.Should().Contain(p => p.CurrentFileName == "Solution1.sln");
            progressReports.Should().Contain(p => p.CurrentFileName == "Solution2.sln");
            progressReports.Where(p => p.IsComplete).Should().HaveCount(2);
        }
        finally
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task LoadAllAsync_WithProgressAndFailure_ReportsError()
    {
        // Arrange
        var paths = new[] { "Solution1.sln", "FailingSolution.sln" };

        foreach (var path in paths)
        {
            File.WriteAllText(path, "");
        }

        var analysis1 = new SolutionAnalysis
        {
            SolutionPath = "Solution1.sln",
            SolutionName = "Solution1",
            Projects = Array.Empty<ProjectInfo>(),
            LoaderType = "Roslyn"
        };

        _mockSolutionLoader
            .SetupSequence(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis1)
            .ThrowsAsync(new SolutionLoadException("Expected failure"));

        var progressReports = new List<SolutionLoadProgress>();
        var progress = new Progress<SolutionLoadProgress>(p => progressReports.Add(p));

        try
        {
            // Act
            await _analyzer.LoadAllAsync(paths, progress);

            // Assert
            var errorReport = progressReports.FirstOrDefault(p => !string.IsNullOrEmpty(p.ErrorMessage));
            errorReport.Should().NotBeNull();
            errorReport!.ErrorMessage.Should().Contain("Expected failure");
        }
        finally
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullSolutionLoader_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new MultiSolutionAnalyzer(null!, NullLogger<MultiSolutionAnalyzer>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("solutionLoader");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new MultiSolutionAnalyzer(_mockSolutionLoader.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion
}
