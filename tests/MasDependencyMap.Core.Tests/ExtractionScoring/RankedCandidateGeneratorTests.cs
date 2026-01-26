namespace MasDependencyMap.Core.Tests.ExtractionScoring;

using FluentAssertions;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class RankedCandidateGeneratorTests
{
    private readonly Mock<IExtractionScoreCalculator> _mockScoreCalculator;
    private readonly Mock<ILogger<RankedCandidateGenerator>> _mockLogger;
    private readonly RankedCandidateGenerator _generator;

    public RankedCandidateGeneratorTests()
    {
        _mockScoreCalculator = new Mock<IExtractionScoreCalculator>();
        _mockLogger = new Mock<ILogger<RankedCandidateGenerator>>();

        _generator = new RankedCandidateGenerator(
            _mockScoreCalculator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithMultipleProjects_SortsAscending()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>
        {
            CreateMockScore("ProjectA", 10),  // Easy
            CreateMockScore("ProjectB", 25),  // Easy
            CreateMockScore("ProjectC", 50),  // Medium
            CreateMockScore("ProjectD", 75),  // Hard
            CreateMockScore("ProjectE", 90)   // Hard
        };

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.AllProjects.Should().HaveCount(5);
        result.AllProjects.Should().BeInAscendingOrder(s => s.FinalScore);
        result.AllProjects[0].ProjectName.Should().Be("ProjectA");
        result.AllProjects[1].ProjectName.Should().Be("ProjectB");
        result.AllProjects[2].ProjectName.Should().Be("ProjectC");
        result.AllProjects[3].ProjectName.Should().Be("ProjectD");
        result.AllProjects[4].ProjectName.Should().Be("ProjectE");
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithEasyProjects_ReturnsTop10Easiest()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>();
        for (int i = 0; i < 15; i++)
        {
            mockScores.Add(CreateMockScore($"EasyProject{i}", i)); // Scores 0-14 (all Easy)
        }

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.EasiestCandidates.Should().HaveCount(10);
        result.EasiestCandidates.Should().BeInAscendingOrder(s => s.FinalScore);
        result.EasiestCandidates[0].ProjectName.Should().Be("EasyProject0");
        result.EasiestCandidates[9].ProjectName.Should().Be("EasyProject9");
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithHardProjects_ReturnsBottom10Hardest()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>();
        for (int i = 67; i <= 81; i++) // 15 hard projects (scores 67-81)
        {
            mockScores.Add(CreateMockScore($"HardProject{i}", i));
        }

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.HardestCandidates.Should().HaveCount(10);
        result.HardestCandidates.Should().BeInDescendingOrder(s => s.FinalScore);
        result.HardestCandidates[0].FinalScore.Should().Be(81); // Hardest first
        result.HardestCandidates[9].FinalScore.Should().Be(72);
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithFewerThan10Easy_ReturnsAll()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>
        {
            CreateMockScore("ProjectA", 10),
            CreateMockScore("ProjectB", 20),
            CreateMockScore("ProjectC", 30),
            CreateMockScore("ProjectD", 50), // Medium
            CreateMockScore("ProjectE", 70)  // Hard
        };

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.EasiestCandidates.Should().HaveCount(3); // Only 3 easy projects
        result.EasiestCandidates[0].ProjectName.Should().Be("ProjectA");
        result.EasiestCandidates[1].ProjectName.Should().Be("ProjectB");
        result.EasiestCandidates[2].ProjectName.Should().Be("ProjectC");
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithFewerThan10Hard_ReturnsAll()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>
        {
            CreateMockScore("ProjectA", 10),  // Easy
            CreateMockScore("ProjectB", 50),  // Medium
            CreateMockScore("ProjectC", 75),  // Hard
            CreateMockScore("ProjectD", 85),  // Hard
            CreateMockScore("ProjectE", 95)   // Hard
        };

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.HardestCandidates.Should().HaveCount(3); // Only 3 hard projects
        result.HardestCandidates.Should().BeInDescendingOrder(s => s.FinalScore);
        result.HardestCandidates[0].FinalScore.Should().Be(95); // Hardest first
        result.HardestCandidates[1].FinalScore.Should().Be(85);
        result.HardestCandidates[2].FinalScore.Should().Be(75);
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithNoEasyProjects_ReturnsEmptyEasiestList()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>
        {
            CreateMockScore("ProjectA", 50),  // Medium
            CreateMockScore("ProjectB", 60),  // Medium
            CreateMockScore("ProjectC", 75),  // Hard
            CreateMockScore("ProjectD", 85)   // Hard
        };

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.EasiestCandidates.Should().BeEmpty();
        result.Statistics.EasyCount.Should().Be(0);
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithNoHardProjects_ReturnsEmptyHardestList()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>
        {
            CreateMockScore("ProjectA", 10),  // Easy
            CreateMockScore("ProjectB", 20),  // Easy
            CreateMockScore("ProjectC", 50),  // Medium
            CreateMockScore("ProjectD", 60)   // Medium
        };

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.HardestCandidates.Should().BeEmpty();
        result.Statistics.HardCount.Should().Be(0);
    }

    [Fact]
    public async Task GenerateRankedListAsync_Statistics_CalculatesCorrectCounts()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>
        {
            CreateMockScore("Easy1", 10),
            CreateMockScore("Easy2", 20),
            CreateMockScore("Easy3", 30),
            CreateMockScore("Medium1", 40),
            CreateMockScore("Medium2", 50),
            CreateMockScore("Medium3", 60),
            CreateMockScore("Hard1", 70),
            CreateMockScore("Hard2", 80),
            CreateMockScore("Hard3", 90),
            CreateMockScore("Hard4", 95)
        };

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.Statistics.TotalProjects.Should().Be(10);
        result.Statistics.EasyCount.Should().Be(3);
        result.Statistics.MediumCount.Should().Be(3);
        result.Statistics.HardCount.Should().Be(4);
    }

    [Fact]
    public async Task GenerateRankedListAsync_Statistics_SumEqualsTotal()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>
        {
            CreateMockScore("ProjectA", 15),  // Easy
            CreateMockScore("ProjectB", 45),  // Medium
            CreateMockScore("ProjectC", 75)   // Hard
        };

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.Statistics.IsValid.Should().BeTrue();
        (result.Statistics.EasyCount + result.Statistics.MediumCount + result.Statistics.HardCount)
            .Should().Be(result.Statistics.TotalProjects);
    }

    [Fact]
    public async Task GenerateRankedListAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var graph = new DependencyGraph();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _generator.GenerateRankedListAsync(graph, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithBoundaryScores_CategorizesCorrectly()
    {
        // Arrange
        var graph = new DependencyGraph();

        var mockScores = new List<ExtractionScore>
        {
            CreateMockScore("Boundary33", 33.0),   // Easy (inclusive upper)
            CreateMockScore("Boundary33.1", 33.1), // Medium (exclusive lower)
            CreateMockScore("Boundary66.9", 66.9), // Medium (exclusive upper)
            CreateMockScore("Boundary67", 67.0)    // Hard (inclusive lower)
        };

        _mockScoreCalculator
            .Setup(c => c.CalculateForAllProjectsAsync(graph, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockScores);

        // Act
        var result = await _generator.GenerateRankedListAsync(graph);

        // Assert
        result.Statistics.EasyCount.Should().Be(1);   // Score 33.0
        result.Statistics.MediumCount.Should().Be(2); // Scores 33.1 and 66.9
        result.Statistics.HardCount.Should().Be(1);   // Score 67.0
    }

    [Fact]
    public void Constructor_WithNullScoreCalculator_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RankedCandidateGenerator(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RankedCandidateGenerator(_mockScoreCalculator.Object, null!));
    }

    [Fact]
    public async Task GenerateRankedListAsync_WithNullGraph_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _generator.GenerateRankedListAsync(null!));
    }

    private ExtractionScore CreateMockScore(string projectName, double score)
    {
        // Create minimal mock ExtractionScore for testing
        return new ExtractionScore(
            ProjectName: projectName,
            ProjectPath: $"/path/to/{projectName}.csproj",
            FinalScore: score,
            CouplingMetric: null,
            ComplexityMetric: new ComplexityMetric(
                ProjectName: projectName,
                ProjectPath: $"/path/to/{projectName}.csproj",
                MethodCount: 100,
                TotalComplexity: 500,
                AverageComplexity: 5.0,
                NormalizedScore: 30.0),
            TechDebtMetric: new TechDebtMetric(
                ProjectName: projectName,
                ProjectPath: $"/path/to/{projectName}.csproj",
                TargetFramework: "net8.0",
                NormalizedScore: 20.0),
            ExternalApiMetric: new ExternalApiMetric(
                ProjectName: projectName,
                ProjectPath: $"/path/to/{projectName}.csproj",
                EndpointCount: 0,
                NormalizedScore: 0.0,
                ApiTypeBreakdown: new ApiTypeBreakdown(
                    WebApiEndpoints: 0,
                    WebMethodEndpoints: 0,
                    WcfEndpoints: 0)));
    }
}
