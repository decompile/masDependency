namespace MasDependencyMap.Core.Tests.CycleAnalysis;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;
using Xunit;

public class CycleStatisticsCalculatorTests
{
    private readonly CycleStatisticsCalculator _calculator;

    public CycleStatisticsCalculatorTests()
    {
        var logger = NullLogger<CycleStatisticsCalculator>.Instance;
        _calculator = new CycleStatisticsCalculator(logger);
    }

    [Fact]
    public async Task CalculateAsync_EmptyCycleList_ReturnsZeroStatistics()
    {
        // Arrange
        var cycles = new List<CycleInfo>();
        int totalProjects = 50;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.TotalCycles.Should().Be(0);
        statistics.LargestCycleSize.Should().Be(0);
        statistics.TotalProjectsInCycles.Should().Be(0);
        statistics.ParticipationRate.Should().Be(0.0);
        statistics.TotalProjectsAnalyzed.Should().Be(50);
    }

    [Fact]
    public async Task CalculateAsync_SingleCycle_CalculatesCorrectly()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B", "C") // 3-project cycle
        };
        int totalProjects = 10;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.TotalCycles.Should().Be(1);
        statistics.LargestCycleSize.Should().Be(3);
        statistics.TotalProjectsInCycles.Should().Be(3);
        statistics.ParticipationRate.Should().BeApproximately(30.0, 0.1); // 3/10 = 30%
        statistics.TotalProjectsAnalyzed.Should().Be(10);
    }

    [Fact]
    public async Task CalculateAsync_MultipleCycles_IdentifiesLargestCycle()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B"),           // 2-project cycle
            CreateCycle(2, "C", "D", "E", "F"), // 4-project cycle
            CreateCycle(3, "G", "H", "I")       // 3-project cycle
        };
        int totalProjects = 20;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.TotalCycles.Should().Be(3);
        statistics.LargestCycleSize.Should().Be(4); // Cycle 2 is largest
        statistics.TotalProjectsInCycles.Should().Be(9); // 2 + 4 + 3 = 9
        statistics.ParticipationRate.Should().BeApproximately(45.0, 0.1); // 9/20 = 45%
    }

    [Fact]
    public async Task CalculateAsync_OverlappingCycles_CountsDistinctProjects()
    {
        // Arrange
        // Cycle 1: A → B → A (2 projects: A, B)
        // Cycle 2: A → C → A (2 projects: A, C)
        // Total distinct: 3 projects (A appears in both but counted once)
        var projectA = new ProjectNode
        {
            ProjectName = "A",
            ProjectPath = "/path/A",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var projectB = new ProjectNode
        {
            ProjectName = "B",
            ProjectPath = "/path/B",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var projectC = new ProjectNode
        {
            ProjectName = "C",
            ProjectPath = "/path/C",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var cycles = new List<CycleInfo>
        {
            new CycleInfo(1, new[] { projectA, projectB }),
            new CycleInfo(2, new[] { projectA, projectC })
        };
        int totalProjects = 10;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.TotalCycles.Should().Be(2);
        statistics.TotalProjectsInCycles.Should().Be(3); // A, B, C (distinct count)
        statistics.ParticipationRate.Should().BeApproximately(30.0, 0.1); // 3/10 = 30%
    }

    [Fact]
    public async Task CalculateAsync_AllProjectsInCycles_Returns100PercentParticipation()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B", "C", "D", "E")
        };
        int totalProjects = 5; // All 5 projects are in the cycle

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.ParticipationRate.Should().BeApproximately(100.0, 0.1);
        statistics.TotalProjectsInCycles.Should().Be(5);
        statistics.TotalProjectsAnalyzed.Should().Be(5);
    }

    [Fact]
    public async Task CalculateAsync_ZeroTotalProjects_ReturnsZeroParticipation()
    {
        // Arrange
        var cycles = new List<CycleInfo>();
        int totalProjects = 0;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.ParticipationRate.Should().Be(0.0); // No division by zero error
        statistics.TotalProjectsAnalyzed.Should().Be(0);
    }

    [Fact]
    public async Task CalculateAsync_NullCycles_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _calculator.CalculateAsync(null!, 10);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("cycles");
    }

    [Fact]
    public async Task CalculateAsync_LargestCycleIdentification_SelectsMaxSize()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B", "C"),                      // 3 projects
            CreateCycle(2, "D", "E"),                           // 2 projects
            CreateCycle(3, "F", "G", "H", "I", "J", "K", "L")   // 7 projects (largest)
        };
        int totalProjects = 15;

        // Act
        var statistics = await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        statistics.LargestCycleSize.Should().Be(7);
    }

    [Fact]
    public async Task CalculateAsync_NegativeTotalProjects_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B")
        };
        int totalProjects = -10;

        // Act
        Func<Task> act = async () => await _calculator.CalculateAsync(cycles, totalProjects);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("totalProjectsAnalyzed");
    }

    [Fact]
    public async Task CalculateAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cycles = new List<CycleInfo>
        {
            CreateCycle(1, "A", "B", "C")
        };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await _calculator.CalculateAsync(cycles, 10, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void CycleStatistics_NegativeTotalCycles_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => new CycleStatistics(-1, 5, 3, 10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("totalCycles");
    }

    [Fact]
    public void CycleStatistics_NegativeLargestCycleSize_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => new CycleStatistics(2, -5, 3, 10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("largestCycleSize");
    }

    [Fact]
    public void CycleStatistics_NegativeTotalProjectsInCycles_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => new CycleStatistics(2, 5, -3, 10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("totalProjectsInCycles");
    }

    [Fact]
    public void CycleStatistics_NegativeTotalProjectsAnalyzed_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => new CycleStatistics(2, 5, 3, -10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("totalProjectsAnalyzed");
    }

    [Fact]
    public void CycleStatistics_ProjectsInCyclesExceedsTotalProjects_ThrowsArgumentException()
    {
        // Act
        Action act = () => new CycleStatistics(2, 5, 15, 10); // 15 in cycles but only 10 total!

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("totalProjectsInCycles")
            .WithMessage("*cannot exceed total projects*");
    }

    // Helper method to create test CycleInfo objects
    private CycleInfo CreateCycle(int cycleId, params string[] projectNames)
    {
        var projects = projectNames
            .Select(name => new ProjectNode
            {
                ProjectName = name,
                ProjectPath = $"/path/{name}",
                TargetFramework = "net8.0",
                SolutionName = "TestSolution"
            })
            .ToList();

        return new CycleInfo(cycleId, projects);
    }
}
