namespace MasDependencyMap.Core.Tests.CycleAnalysis;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;

public class RecommendationGeneratorTests
{
    private readonly ILogger<RecommendationGenerator> _logger;
    private readonly RecommendationGenerator _generator;

    public RecommendationGeneratorTests()
    {
        _logger = NullLogger<RecommendationGenerator>.Instance;
        _generator = new RecommendationGenerator(_logger);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_EmptyCycleList_ReturnsEmptyResult()
    {
        // Arrange
        var cycles = new List<CycleInfo>();

        // Act
        var result = await _generator.GenerateRecommendationsAsync(cycles);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_SingleCycleOneWeakEdge_OneRecommendationCreated()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(
            cycleId: 1,
            cycleSize: 3,
            weakEdgeCouplingScores: new[] { 5 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        result.Should().HaveCount(1);
        var recommendation = result.Single();
        recommendation.CycleId.Should().Be(1);
        recommendation.CouplingScore.Should().Be(5);
        recommendation.CycleSize.Should().Be(3);
        recommendation.Rank.Should().Be(1);
        recommendation.Rationale.Should().Contain("3-project cycle");
        recommendation.Rationale.Should().Contain("5 method calls");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_MultipleWeakEdges_AllGenerateRecommendations()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(
            cycleId: 1,
            cycleSize: 5,
            weakEdgeCouplingScores: new[] { 2, 3, 2 }); // 3 weak edges

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(r =>
        {
            r.CycleId.Should().Be(1);
            r.CycleSize.Should().Be(5);
        });
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RecommendationsRankedByCouplingScore_LowestFirst()
    {
        // Arrange
        var cycle1 = CreateCycleWithWeakEdges(1, 4, new[] { 10 });
        var cycle2 = CreateCycleWithWeakEdges(2, 6, new[] { 3 });
        var cycle3 = CreateCycleWithWeakEdges(3, 5, new[] { 7 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle1, cycle2, cycle3 });

        // Assert
        result.Should().HaveCount(3);
        result[0].CouplingScore.Should().Be(3);  // Lowest coupling first
        result[0].Rank.Should().Be(1);
        result[1].CouplingScore.Should().Be(7);
        result[1].Rank.Should().Be(2);
        result[2].CouplingScore.Should().Be(10); // Highest coupling last
        result[2].Rank.Should().Be(3);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_TiedCouplingScores_SecondarySortByCycleSize()
    {
        // Arrange
        var cycle1 = CreateCycleWithWeakEdges(1, 4, new[] { 5 }); // Same coupling, smaller cycle
        var cycle2 = CreateCycleWithWeakEdges(2, 8, new[] { 5 }); // Same coupling, larger cycle

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle1, cycle2 });

        // Assert
        result.Should().HaveCount(2);
        result[0].CouplingScore.Should().Be(5);
        result[0].CycleSize.Should().Be(8); // Larger cycle first (higher impact)
        result[1].CycleSize.Should().Be(4); // Smaller cycle second
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RationaleFormat_MatchesExpectedTemplate()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(1, 8, new[] { 3 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        var recommendation = result.Single();
        recommendation.Rationale.Should().Contain("Weakest link");
        recommendation.Rationale.Should().Contain("8-project cycle");
        recommendation.Rationale.Should().Contain("3 method calls");
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_CouplingScoreOne_RationaleUsesSingular()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(1, 3, new[] { 1 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        var recommendation = result.Single();
        recommendation.Rationale.Should().Contain("1 method call"); // Singular, not "1 method calls"
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_LargeCycle_RationaleEmphasizesImpact()
    {
        // Arrange
        var cycle = CreateCycleWithWeakEdges(1, 12, new[] { 4 });

        // Act
        var result = await _generator.GenerateRecommendationsAsync(new[] { cycle });

        // Assert
        var recommendation = result.Single();
        recommendation.Rationale.Should().Match(r => r.Contains("critical") || r.Contains("large")); // Emphasizes size
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_NullCycles_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _generator.GenerateRecommendationsAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_CancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var largeCycleSet = CreateManyCycles(100);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var act = async () => await _generator.GenerateRecommendationsAsync(largeCycleSet, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Top5Extraction_ReturnsCorrectSubset()
    {
        // Arrange
        var cycles = Enumerable.Range(1, 10)
            .Select(i => CreateCycleWithWeakEdges(i, 5, new[] { i }))
            .ToList();

        // Act
        var allRecommendations = await _generator.GenerateRecommendationsAsync(cycles);
        var top5 = allRecommendations.Take(5).ToList();

        // Assert
        allRecommendations.Should().HaveCount(10);
        top5.Should().HaveCount(5);
        top5[0].Rank.Should().Be(1);
        top5[4].Rank.Should().Be(5);
        top5.Should().BeInAscendingOrder(r => r.CouplingScore); // Lowest coupling first
    }

    // Helper methods for creating test data
    private CycleInfo CreateCycleWithWeakEdges(int cycleId, int cycleSize, int[] weakEdgeCouplingScores)
    {
        // Create cycle with projects and weak edges
        var projects = Enumerable.Range(1, cycleSize)
            .Select(i => new ProjectNode
            {
                ProjectName = $"Project{i}",
                ProjectPath = $"Project{i}.csproj",
                TargetFramework = "net8.0",
                SolutionName = "TestSolution"
            })
            .ToList();

        var cycle = new CycleInfo(cycleId, projects);

        // Create weak edges with specified coupling scores
        var weakEdges = weakEdgeCouplingScores
            .Select((score, index) => new DependencyEdge
            {
                Source = projects[index % cycleSize],
                Target = projects[(index + 1) % cycleSize],
                DependencyType = DependencyType.ProjectReference,
                CouplingScore = score
            })
            .ToList();

        cycle.WeakCouplingEdges = weakEdges;
        cycle.WeakCouplingScore = weakEdges.Min(e => e.CouplingScore);

        return cycle;
    }

    private List<CycleInfo> CreateManyCycles(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateCycleWithWeakEdges(i, 5, new[] { i }))
            .ToList();
    }
}
