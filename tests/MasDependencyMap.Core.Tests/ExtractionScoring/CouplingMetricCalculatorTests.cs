namespace MasDependencyMap.Core.Tests.ExtractionScoring;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.ExtractionScoring;
using Xunit;

public class CouplingMetricCalculatorTests
{
    private readonly ILogger<CouplingMetricCalculator> _logger;
    private readonly CouplingMetricCalculator _calculator;

    public CouplingMetricCalculatorTests()
    {
        _logger = NullLogger<CouplingMetricCalculator>.Instance;
        _calculator = new CouplingMetricCalculator(_logger);
    }

    [Fact]
    public async Task CalculateAsync_GraphWithDependencies_CalculatesIncomingAndOutgoingCounts()
    {
        // Arrange: A -> B, C -> B (B has 2 incoming, 0 outgoing)
        var graph = CreateGraphWithDependencies();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        var projectBMetric = metrics.Single(m => m.ProjectName == "ProjectB");
        projectBMetric.IncomingCount.Should().Be(2);
        projectBMetric.OutgoingCount.Should().Be(0);
    }

    [Fact]
    public async Task CalculateAsync_GraphWithDependencies_WeightsIncomingHigherThanOutgoing()
    {
        // Arrange: A -> B, A -> C (A: 0 incoming, 2 outgoing), (B: 1 incoming, 0 outgoing)
        var graph = CreateGraphForWeightingTest();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert - Verify formula: (incoming * 2) + outgoing
        var projectAMetric = metrics.Single(m => m.ProjectName == "ProjectA");
        projectAMetric.TotalScore.Should().Be((0 * 2) + 2); // = 2

        var projectBMetric = metrics.Single(m => m.ProjectName == "ProjectB");
        projectBMetric.TotalScore.Should().Be((1 * 2) + 0); // = 2
    }

    [Fact]
    public async Task CalculateAsync_GraphWithDependencies_NormalizesScoreTo100Scale()
    {
        // Arrange: Graph with known max coupling
        var graph = CreateGraphWithKnownMaxCoupling();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        var maxMetric = metrics.OrderByDescending(m => m.TotalScore).First();
        maxMetric.NormalizedScore.Should().Be(100.0);

        metrics.Should().AllSatisfy(m =>
        {
            m.NormalizedScore.Should().BeGreaterThanOrEqualTo(0.0);
            m.NormalizedScore.Should().BeLessThanOrEqualTo(100.0);
        });
    }

    [Fact]
    public async Task CalculateAsync_EmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        metrics.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateAsync_SingleProjectNoDependencies_ReturnsZeroCoupling()
    {
        // Arrange
        var graph = new DependencyGraph();
        var project = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        graph.AddVertex(project);

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        metrics.Should().ContainSingle();
        var metric = metrics.First();
        metric.IncomingCount.Should().Be(0);
        metric.OutgoingCount.Should().Be(0);
        metric.TotalScore.Should().Be(0);
        metric.NormalizedScore.Should().Be(0);
    }

    [Fact]
    public async Task CalculateAsync_GraphWithMaxCoupling_NormalizedScoreIs100()
    {
        // Arrange: Create graph where one project has maximum coupling
        var graph = CreateGraphWithMaxCouplingProject();

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert
        var maxCoupledProject = metrics.OrderByDescending(m => m.TotalScore).First();
        maxCoupledProject.NormalizedScore.Should().Be(100.0);
    }

    [Fact]
    public async Task CalculateAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _calculator.CalculateAsync(null!));
    }

    [Fact]
    public async Task CalculateAsync_WeightingFormula_IncomingCountedTwiceAsHigh()
    {
        // Arrange: Project with 3 incoming and 2 outgoing
        var graph = new DependencyGraph();
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");
        var projectD = CreateProjectNode("ProjectD");
        var projectE = CreateProjectNode("ProjectE");
        var projectF = CreateProjectNode("ProjectF");

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);
        graph.AddVertex(projectE);
        graph.AddVertex(projectF);

        graph.AddEdge(CreateDependencyEdge(projectB, projectA)); // B -> A (incoming for A)
        graph.AddEdge(CreateDependencyEdge(projectC, projectA)); // C -> A (incoming for A)
        graph.AddEdge(CreateDependencyEdge(projectD, projectA)); // D -> A (incoming for A)
        graph.AddEdge(CreateDependencyEdge(projectA, projectE)); // A -> E (outgoing from A)
        graph.AddEdge(CreateDependencyEdge(projectA, projectF)); // A -> F (outgoing from A)

        // Act
        var metrics = await _calculator.CalculateAsync(graph);

        // Assert - Verify formula: (3 * 2) + 2 = 8
        var projectAMetric = metrics.Single(m => m.ProjectName == "ProjectA");
        projectAMetric.IncomingCount.Should().Be(3);
        projectAMetric.OutgoingCount.Should().Be(2);
        projectAMetric.TotalScore.Should().Be((3 * 2) + 2, "because formula is (incoming * 2) + outgoing");
        projectAMetric.TotalScore.Should().Be(8);
    }

    [Fact]
    public async Task CalculateAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange: Create a large graph to ensure cancellation can be detected
        var graph = new DependencyGraph();
        for (int i = 0; i < 50; i++)
        {
            var project = CreateProjectNode($"Project{i}");
            graph.AddVertex(project);
        }

        // Add some edges
        var vertices = graph.Vertices.ToList();
        for (int i = 0; i < vertices.Count - 1; i++)
        {
            graph.AddEdge(CreateDependencyEdge(vertices[i], vertices[i + 1]));
        }

        // Create cancellation token that is already cancelled
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _calculator.CalculateAsync(graph, cts.Token));
    }

    // Helper methods to create test objects

    private ProjectNode CreateProjectNode(string name)
    {
        return new ProjectNode
        {
            ProjectName = name,
            ProjectPath = $"{name}.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
    }

    private DependencyEdge CreateDependencyEdge(ProjectNode source, ProjectNode target)
    {
        return new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };
    }

    private DependencyGraph CreateGraphWithDependencies()
    {
        var graph = new DependencyGraph();
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);

        graph.AddEdge(CreateDependencyEdge(projectA, projectB)); // A -> B
        graph.AddEdge(CreateDependencyEdge(projectC, projectB)); // C -> B

        return graph;
    }

    private DependencyGraph CreateGraphForWeightingTest()
    {
        var graph = new DependencyGraph();
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);

        graph.AddEdge(CreateDependencyEdge(projectA, projectB)); // A -> B
        graph.AddEdge(CreateDependencyEdge(projectA, projectC)); // A -> C

        return graph;
    }

    private DependencyGraph CreateGraphWithKnownMaxCoupling()
    {
        var graph = new DependencyGraph();
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");
        var projectD = CreateProjectNode("ProjectD");

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);

        // ProjectB has maximum coupling: 2 incoming, 1 outgoing = (2*2)+1 = 5
        graph.AddEdge(CreateDependencyEdge(projectA, projectB)); // A -> B (incoming for B)
        graph.AddEdge(CreateDependencyEdge(projectC, projectB)); // C -> B (incoming for B)
        graph.AddEdge(CreateDependencyEdge(projectB, projectD)); // B -> D (outgoing from B)

        return graph;
    }

    private DependencyGraph CreateGraphWithMaxCouplingProject()
    {
        var graph = new DependencyGraph();
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");
        var projectD = CreateProjectNode("ProjectD");
        var projectE = CreateProjectNode("ProjectE");

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);
        graph.AddVertex(projectE);

        graph.AddEdge(CreateDependencyEdge(projectA, projectB));
        graph.AddEdge(CreateDependencyEdge(projectC, projectB));
        graph.AddEdge(CreateDependencyEdge(projectD, projectB));
        graph.AddEdge(CreateDependencyEdge(projectB, projectE));

        // ProjectB: 3 incoming, 1 outgoing = (3*2)+1 = 7 (max score)

        return graph;
    }
}
