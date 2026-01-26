namespace MasDependencyMap.Core.Tests.ExtractionScoring;

using FluentAssertions;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Collections.Generic;

public sealed class ExtractionScoreCalculatorTests
{
    private readonly Mock<ICouplingMetricCalculator> _mockCouplingCalculator;
    private readonly Mock<IComplexityMetricCalculator> _mockComplexityCalculator;
    private readonly Mock<ITechDebtAnalyzer> _mockTechDebtAnalyzer;
    private readonly Mock<IExternalApiDetector> _mockApiDetector;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<ExtractionScoreCalculator>> _mockLogger;
    private readonly ExtractionScoreCalculator _calculator;

    public ExtractionScoreCalculatorTests()
    {
        _mockCouplingCalculator = new Mock<ICouplingMetricCalculator>();
        _mockComplexityCalculator = new Mock<IComplexityMetricCalculator>();
        _mockTechDebtAnalyzer = new Mock<ITechDebtAnalyzer>();
        _mockApiDetector = new Mock<IExternalApiDetector>();
        _mockLogger = new Mock<ILogger<ExtractionScoreCalculator>>();

        // Use real configuration (empty = defaults used)
        _configuration = new ConfigurationBuilder().Build();

        _calculator = new ExtractionScoreCalculator(
            _mockCouplingCalculator.Object,
            _mockComplexityCalculator.Object,
            _mockTechDebtAnalyzer.Object,
            _mockApiDetector.Object,
            _configuration,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CalculateAsync_WithDefaultWeights_CalculatesCorrectWeightedScore()
    {
        // Arrange
        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var graph = new DependencyGraph();
        graph.AddVertex(project);

        var couplingMetric = new CouplingMetric("TestProject", 10, 5, 25, 50);
        var complexityMetric = new ComplexityMetric("TestProject", @"D:\test\project.csproj", 20, 100, 5, 60);
        var techDebtMetric = new TechDebtMetric("TestProject", @"D:\test\project.csproj", "net472", 40);
        var apiMetric = new ExternalApiMetric("TestProject", @"D:\test\project.csproj", 8, 66,
            new ApiTypeBreakdown(8, 0, 0));

        _mockCouplingCalculator
            .Setup(c => c.CalculateAsync(It.IsAny<DependencyGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CouplingMetric> { couplingMetric });

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complexityMetric);

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(techDebtMetric);

        _mockApiDetector
            .Setup(d => d.DetectAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiMetric);

        // Act
        var score = await _calculator.CalculateAsync(project, graph);

        // Assert
        // Expected: (50 * 0.40) + (60 * 0.30) + (40 * 0.20) + (66 * 0.10)
        //         = 20 + 18 + 8 + 6.6 = 52.6
        score.FinalScore.Should().BeApproximately(52.6, 0.1);
        score.DifficultyCategory.Should().Be("Medium");
        score.ProjectName.Should().Be("TestProject");
        score.CouplingMetric.Should().Be(couplingMetric);
        score.ComplexityMetric.Should().Be(complexityMetric);
        score.TechDebtMetric.Should().Be(techDebtMetric);
        score.ExternalApiMetric.Should().Be(apiMetric);
    }

    [Fact]
    public async Task CalculateAsync_WithCustomWeights_UsesCustomWeights()
    {
        // Arrange
        var customConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ScoringWeights:CouplingWeight"] = "0.50",
                ["ScoringWeights:ComplexityWeight"] = "0.25",
                ["ScoringWeights:TechDebtWeight"] = "0.15",
                ["ScoringWeights:ExternalExposureWeight"] = "0.10"
            })
            .Build();

        var customCalculator = new ExtractionScoreCalculator(
            _mockCouplingCalculator.Object,
            _mockComplexityCalculator.Object,
            _mockTechDebtAnalyzer.Object,
            _mockApiDetector.Object,
            customConfiguration,
            _mockLogger.Object);

        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var graph = new DependencyGraph();
        graph.AddVertex(project);

        var couplingMetric = new CouplingMetric("TestProject", 10, 5, 25, 50);
        var complexityMetric = new ComplexityMetric("TestProject", @"D:\test\project.csproj", 20, 100, 5, 60);
        var techDebtMetric = new TechDebtMetric("TestProject", @"D:\test\project.csproj", "net472", 40);
        var apiMetric = new ExternalApiMetric("TestProject", @"D:\test\project.csproj", 8, 66,
            new ApiTypeBreakdown(8, 0, 0));

        _mockCouplingCalculator
            .Setup(c => c.CalculateAsync(It.IsAny<DependencyGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CouplingMetric> { couplingMetric });

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complexityMetric);

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(techDebtMetric);

        _mockApiDetector
            .Setup(d => d.DetectAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiMetric);

        // Act
        var score = await customCalculator.CalculateAsync(project, graph);

        // Assert
        // Expected: (50 * 0.50) + (60 * 0.25) + (40 * 0.15) + (66 * 0.10)
        //         = 25 + 15 + 6 + 6.6 = 52.6
        score.FinalScore.Should().BeApproximately(52.6, 0.1);
    }

    [Fact]
    public async Task CalculateForAllProjectsAsync_MultipleProjects_ReturnsScoresSortedByDifficulty()
    {
        // Arrange
        var project1 = new ProjectNode
        {
            ProjectName = "EasyProject",
            ProjectPath = @"D:\test\easy.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var project2 = new ProjectNode
        {
            ProjectName = "HardProject",
            ProjectPath = @"D:\test\hard.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var project3 = new ProjectNode
        {
            ProjectName = "MediumProject",
            ProjectPath = @"D:\test\medium.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var graph = new DependencyGraph();
        graph.AddVertex(project1);
        graph.AddVertex(project2);
        graph.AddVertex(project3);

        var couplingMetrics = new List<CouplingMetric>
        {
            new("EasyProject", 1, 1, 3, 10),
            new("HardProject", 20, 10, 50, 90),
            new("MediumProject", 5, 5, 15, 50)
        };

        _mockCouplingCalculator
            .Setup(c => c.CalculateAsync(It.IsAny<DependencyGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(couplingMetrics);

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexityMetric("EasyProject", @"D:\test\easy.csproj", 10, 20, 2, 20));

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexityMetric("HardProject", @"D:\test\hard.csproj", 100, 500, 5, 80));

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexityMetric("MediumProject", @"D:\test\medium.csproj", 50, 200, 4, 50));

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechDebtMetric("EasyProject", @"D:\test\easy.csproj", "net8.0", 0));

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechDebtMetric("HardProject", @"D:\test\hard.csproj", "net35", 100));

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechDebtMetric("MediumProject", @"D:\test\medium.csproj", "net472", 50));

        _mockApiDetector
            .Setup(d => d.DetectAsync(project1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiMetric("EasyProject", @"D:\test\easy.csproj", 0, 0,
                new ApiTypeBreakdown(0, 0, 0)));

        _mockApiDetector
            .Setup(d => d.DetectAsync(project2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiMetric("HardProject", @"D:\test\hard.csproj", 20, 100,
                new ApiTypeBreakdown(20, 0, 0)));

        _mockApiDetector
            .Setup(d => d.DetectAsync(project3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiMetric("MediumProject", @"D:\test\medium.csproj", 8, 66,
                new ApiTypeBreakdown(8, 0, 0)));

        // Act
        var scores = await _calculator.CalculateForAllProjectsAsync(graph);

        // Assert
        scores.Should().HaveCount(3);
        scores[0].ProjectName.Should().Be("EasyProject"); // Lowest score first
        scores[2].ProjectName.Should().Be("HardProject"); // Highest score last
        scores[0].DifficultyCategory.Should().Be("Easy");
        scores[2].DifficultyCategory.Should().Be("Hard");
    }

    [Theory]
    [InlineData(20, "Easy")]
    [InlineData(33, "Easy")]
    [InlineData(34, "Medium")]
    [InlineData(50, "Medium")]
    [InlineData(66, "Medium")]
    [InlineData(67, "Hard")]
    [InlineData(100, "Hard")]
    public async Task CalculateAsync_VariousScores_ReturnCorrectCategory(double score, string expectedCategory)
    {
        // Arrange
        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var graph = new DependencyGraph();
        graph.AddVertex(project);

        // Set all metrics to the same score so final weighted score equals the input score
        _mockCouplingCalculator
            .Setup(c => c.CalculateAsync(It.IsAny<DependencyGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CouplingMetric> { new("TestProject", 0, 0, 0, score) });

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexityMetric("TestProject", @"D:\test\project.csproj", 0, 0, 0, score));

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechDebtMetric("TestProject", @"D:\test\project.csproj", "net8.0", score));

        _mockApiDetector
            .Setup(d => d.DetectAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiMetric("TestProject", @"D:\test\project.csproj", 0, score,
                new ApiTypeBreakdown(0, 0, 0)));

        // Act
        var result = await _calculator.CalculateAsync(project, graph);

        // Assert - with all metrics at same score, final score should equal input score
        // (score * 0.40) + (score * 0.30) + (score * 0.20) + (score * 0.10) = score * 1.0 = score
        result.FinalScore.Should().BeApproximately(score, 0.1);
        result.DifficultyCategory.Should().Be(expectedCategory);
    }

    [Fact]
    public async Task CalculateAsync_ScoreClamping_ClampsTo0And100()
    {
        // Arrange - Create scenario where weighted score could exceed 100
        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var graph = new DependencyGraph();
        graph.AddVertex(project);

        _mockCouplingCalculator
            .Setup(c => c.CalculateAsync(It.IsAny<DependencyGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CouplingMetric> { new("TestProject", 100, 100, 300, 100) });

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexityMetric("TestProject", @"D:\test\project.csproj", 1000, 10000, 10, 100));

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechDebtMetric("TestProject", @"D:\test\project.csproj", "net11", 100));

        _mockApiDetector
            .Setup(d => d.DetectAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiMetric("TestProject", @"D:\test\project.csproj", 1000, 100,
                new ApiTypeBreakdown(1000, 0, 0)));

        // Act
        var result = await _calculator.CalculateAsync(project, graph);

        // Assert
        result.FinalScore.Should().Be(100); // Clamped to max
    }

    [Fact]
    public async Task CalculateAsync_NullProject_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _calculator.CalculateAsync(null!, null));
    }

    [Fact]
    public async Task CalculateForAllProjectsAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _calculator.CalculateForAllProjectsAsync(null!));
    }

    [Fact]
    public async Task CalculateAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var graph = new DependencyGraph();
        graph.AddVertex(project);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        _mockCouplingCalculator
            .Setup(c => c.CalculateAsync(It.IsAny<DependencyGraph>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _calculator.CalculateAsync(project, graph, cts.Token));
    }

    [Fact]
    public async Task CalculateAsync_NullGraph_ReturnsCouplingScoreZero()
    {
        // Arrange
        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexityMetric("TestProject", @"D:\test\project.csproj", 20, 100, 5, 60));

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechDebtMetric("TestProject", @"D:\test\project.csproj", "net472", 40));

        _mockApiDetector
            .Setup(d => d.DetectAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiMetric("TestProject", @"D:\test\project.csproj", 8, 66,
                new ApiTypeBreakdown(8, 0, 0)));

        // Act
        var score = await _calculator.CalculateAsync(project, null);

        // Assert
        // Expected: (0 * 0.40) + (60 * 0.30) + (40 * 0.20) + (66 * 0.10)
        //         = 0 + 18 + 8 + 6.6 = 32.6
        score.FinalScore.Should().BeApproximately(32.6, 0.1);
        score.CouplingMetric.Should().BeNull(); // No coupling metric when graph is null
    }

    [Fact]
    public async Task LoadWeights_InvalidSum_ThrowsConfigurationException()
    {
        // Arrange - Weights that don't sum to 1.0
        var invalidConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ScoringWeights:CouplingWeight"] = "0.50",
                ["ScoringWeights:ComplexityWeight"] = "0.30",
                ["ScoringWeights:TechDebtWeight"] = "0.30", // Sum = 1.10, invalid!
                ["ScoringWeights:ExternalExposureWeight"] = "0.00"
            })
            .Build();

        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var invalidCalculator = new ExtractionScoreCalculator(
            _mockCouplingCalculator.Object,
            _mockComplexityCalculator.Object,
            _mockTechDebtAnalyzer.Object,
            _mockApiDetector.Object,
            invalidConfiguration,
            _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            await invalidCalculator.CalculateAsync(project, null));

        exception.Message.Should().Contain("Weights must sum to 1.0");
        exception.Message.Should().Contain("1.10"); // The actual invalid sum
        exception.Message.Should().Contain("Update scoring-config.json");
    }

    [Fact]
    public async Task LoadWeights_NegativeWeight_ThrowsConfigurationException()
    {
        // Arrange - Negative weight
        var invalidConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ScoringWeights:CouplingWeight"] = "-0.10", // Negative!
                ["ScoringWeights:ComplexityWeight"] = "0.50",
                ["ScoringWeights:TechDebtWeight"] = "0.40",
                ["ScoringWeights:ExternalExposureWeight"] = "0.20"
            })
            .Build();

        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var invalidCalculator = new ExtractionScoreCalculator(
            _mockCouplingCalculator.Object,
            _mockComplexityCalculator.Object,
            _mockTechDebtAnalyzer.Object,
            _mockApiDetector.Object,
            invalidConfiguration,
            _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            await invalidCalculator.CalculateAsync(project, null));

        exception.Message.Should().Contain("must be between 0.0 and 1.0");
        exception.Message.Should().Contain("-0.1"); // The negative weight
    }

    [Fact]
    public async Task LoadWeights_MissingConfig_UsesDefaultWeights()
    {
        // Arrange - Use empty configuration (no scoring config = defaults)
        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var graph = new DependencyGraph();
        graph.AddVertex(project);

        _mockCouplingCalculator
            .Setup(c => c.CalculateAsync(It.IsAny<DependencyGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CouplingMetric> { new("TestProject", 0, 0, 0, 100) });

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexityMetric("TestProject", @"D:\test\project.csproj", 0, 0, 0, 0));

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechDebtMetric("TestProject", @"D:\test\project.csproj", "net8.0", 0));

        _mockApiDetector
            .Setup(d => d.DetectAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiMetric("TestProject", @"D:\test\project.csproj", 0, 0,
                new ApiTypeBreakdown(0, 0, 0)));

        // Act - Using the default calculator which has empty configuration
        var score = await _calculator.CalculateAsync(project, graph);

        // Assert - Should use default weights (0.40 for coupling)
        // Expected: 100 * 0.40 = 40
        score.FinalScore.Should().BeApproximately(40, 0.1);
    }

    [Theory]
    [InlineData(0.405, 0.30, 0.20, 0.09, true, "Within lower tolerance (sum≈0.995)")]
    [InlineData(0.405, 0.30, 0.20, 0.10, true, "Within upper tolerance (sum≈1.005)")]
    [InlineData(0.40, 0.30, 0.20, 0.08, false, "Below lower tolerance (sum=0.98)")]
    [InlineData(0.42, 0.30, 0.20, 0.10, false, "Above upper tolerance (sum=1.02)")]
    [InlineData(0.40, 0.30, 0.20, 0.10, true, "Exactly 1.0")]
    public async Task LoadWeights_ToleranceBoundary_ValidatesCorrectly(
        double coupling, double complexity, double techDebt, double exposure,
        bool shouldBeValid, string scenario)
    {
        // Arrange - Create weights that sum to test specific tolerance boundaries
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ScoringWeights:CouplingWeight"] = coupling.ToString(),
                ["ScoringWeights:ComplexityWeight"] = complexity.ToString(),
                ["ScoringWeights:TechDebtWeight"] = techDebt.ToString(),
                ["ScoringWeights:ExternalExposureWeight"] = exposure.ToString()
            })
            .Build();

        var calculator = new ExtractionScoreCalculator(
            _mockCouplingCalculator.Object,
            _mockComplexityCalculator.Object,
            _mockTechDebtAnalyzer.Object,
            _mockApiDetector.Object,
            configuration,
            _mockLogger.Object);

        var project = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"D:\test\project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        _mockComplexityCalculator
            .Setup(c => c.CalculateAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexityMetric("TestProject", @"D:\test\project.csproj", 0, 0, 0, 50));

        _mockTechDebtAnalyzer
            .Setup(a => a.AnalyzeAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechDebtMetric("TestProject", @"D:\test\project.csproj", "net8.0", 50));

        _mockApiDetector
            .Setup(d => d.DetectAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiMetric("TestProject", @"D:\test\project.csproj", 0, 50,
                new ApiTypeBreakdown(0, 0, 0)));

        // Act & Assert
        if (shouldBeValid)
        {
            // Should not throw
            var score = await calculator.CalculateAsync(project, null);
            score.Should().NotBeNull($"because {scenario}");
        }
        else
        {
            // Should throw ConfigurationException
            await Assert.ThrowsAsync<ConfigurationException>(async () =>
                await calculator.CalculateAsync(project, null));
        }
    }
}
