namespace MasDependencyMap.Core.Tests.CycleAnalysis;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuikGraph;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;

public sealed class WeakEdgeIdentifierTests
{
    private readonly ILogger<WeakEdgeIdentifier> _logger;
    private readonly WeakEdgeIdentifier _identifier;

    public WeakEdgeIdentifierTests()
    {
        _logger = NullLogger<WeakEdgeIdentifier>.Instance;
        _identifier = new WeakEdgeIdentifier(_logger);
    }

    [Fact]
    public void IdentifyWeakEdges_EmptyCycleList_ReturnsEmptyResult()
    {
        // Arrange
        var cycles = new List<CycleInfo>();
        var graph = CreateEmptyGraph();

        // Act
        var result = _identifier.IdentifyWeakEdges(cycles, graph);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void IdentifyWeakEdges_SingleCycleOneWeakEdge_EdgeFlagged()
    {
        // Arrange
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");

        var graph = new AdjacencyGraph<ProjectNode, DependencyEdge>();
        graph.AddVerticesAndEdge(CreateEdge(projectA, projectB, couplingScore: 5));  // Weak
        graph.AddVerticesAndEdge(CreateEdge(projectB, projectC, couplingScore: 10)); // Stronger
        graph.AddVerticesAndEdge(CreateEdge(projectC, projectA, couplingScore: 15)); // Strongest

        var cycle = new CycleInfo(1, new[] { projectA, projectB, projectC });

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle }, graph);

        // Assert
        var updatedCycle = result.Single();
        updatedCycle.WeakCouplingEdges.Should().HaveCount(1);
        updatedCycle.WeakCouplingEdges[0].CouplingScore.Should().Be(5);
        updatedCycle.WeakCouplingScore.Should().Be(5);
    }

    [Fact]
    public void IdentifyWeakEdges_TiedMinimumScores_AllTiedEdgesFlagged()
    {
        // Arrange
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");
        var projectD = CreateProjectNode("ProjectD");

        var graph = new AdjacencyGraph<ProjectNode, DependencyEdge>();
        graph.AddVerticesAndEdge(CreateEdge(projectA, projectB, couplingScore: 3));  // Tied weak
        graph.AddVerticesAndEdge(CreateEdge(projectB, projectC, couplingScore: 10));
        graph.AddVerticesAndEdge(CreateEdge(projectC, projectD, couplingScore: 3));  // Tied weak
        graph.AddVerticesAndEdge(CreateEdge(projectD, projectA, couplingScore: 3));  // Tied weak

        var cycle = new CycleInfo(1, new[] { projectA, projectB, projectC, projectD });

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle }, graph);

        // Assert
        var updatedCycle = result.Single();
        updatedCycle.WeakCouplingEdges.Should().HaveCount(3);
        updatedCycle.WeakCouplingEdges.Should().OnlyContain(e => e.CouplingScore == 3);
        updatedCycle.WeakCouplingScore.Should().Be(3);
    }

    [Fact]
    public void IdentifyWeakEdges_AllEdgesEqualScore_AllEdgesFlagged()
    {
        // Arrange
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");

        var graph = new AdjacencyGraph<ProjectNode, DependencyEdge>();
        graph.AddVerticesAndEdge(CreateEdge(projectA, projectB, couplingScore: 7));
        graph.AddVerticesAndEdge(CreateEdge(projectB, projectC, couplingScore: 7));
        graph.AddVerticesAndEdge(CreateEdge(projectC, projectA, couplingScore: 7));

        var cycle = new CycleInfo(1, new[] { projectA, projectB, projectC });

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle }, graph);

        // Assert
        var updatedCycle = result.Single();
        updatedCycle.WeakCouplingEdges.Should().HaveCount(3);
        updatedCycle.WeakCouplingScore.Should().Be(7);
    }

    [Fact]
    public void IdentifyWeakEdges_MultipleCycles_EachCycleAnalyzed()
    {
        // Arrange
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");
        var projectD = CreateProjectNode("ProjectD");
        var projectE = CreateProjectNode("ProjectE");

        var graph = new AdjacencyGraph<ProjectNode, DependencyEdge>();
        // Cycle 1: A → B → C → A
        graph.AddVerticesAndEdge(CreateEdge(projectA, projectB, couplingScore: 2));  // Cycle 1 min
        graph.AddVerticesAndEdge(CreateEdge(projectB, projectC, couplingScore: 5));
        graph.AddVerticesAndEdge(CreateEdge(projectC, projectA, couplingScore: 8));
        // Cycle 2: D → E → D
        graph.AddVerticesAndEdge(CreateEdge(projectD, projectE, couplingScore: 10)); // Tied min
        graph.AddVerticesAndEdge(CreateEdge(projectE, projectD, couplingScore: 10)); // Tied min

        var cycle1 = new CycleInfo(1, new[] { projectA, projectB, projectC });
        var cycle2 = new CycleInfo(2, new[] { projectD, projectE });

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle1, cycle2 }, graph);

        // Assert
        result.Should().HaveCount(2);
        result[0].WeakCouplingScore.Should().Be(2); // Min from first cycle
        result[0].WeakCouplingEdges.Should().HaveCount(1);
        result[1].WeakCouplingScore.Should().Be(10); // Min from second cycle (tied)
        result[1].WeakCouplingEdges.Should().HaveCount(2); // Two edges tied at 10
    }

    [Fact]
    public void IdentifyWeakEdges_NullCycles_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = CreateEmptyGraph();

        // Act & Assert
        var act = () => _identifier.IdentifyWeakEdges(null!, graph);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IdentifyWeakEdges_NullGraph_ThrowsArgumentNullException()
    {
        // Arrange
        var cycles = new List<CycleInfo>();

        // Act & Assert
        var act = () => _identifier.IdentifyWeakEdges(cycles, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IdentifyWeakEdges_CancellationTokenSupport_ThrowsOperationCanceledException()
    {
        // Arrange
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");

        var graph = new AdjacencyGraph<ProjectNode, DependencyEdge>();
        graph.AddVerticesAndEdge(CreateEdge(projectA, projectB, couplingScore: 5));

        var cycle = new CycleInfo(1, new[] { projectA, projectB });
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var act = () => _identifier.IdentifyWeakEdges(new[] { cycle }, graph, cts.Token);
        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void IdentifyWeakEdges_CycleWithNoEdges_SkipsCycleGracefully()
    {
        // Arrange
        var projectA = CreateProjectNode("ProjectA");
        var projectB = CreateProjectNode("ProjectB");
        var projectC = CreateProjectNode("ProjectC");

        var graph = new AdjacencyGraph<ProjectNode, DependencyEdge>();
        // Add vertices but no edges connecting A and B
        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        // Add an edge between other projects
        graph.AddVerticesAndEdge(CreateEdge(projectC, projectA, couplingScore: 5));

        var cycle = new CycleInfo(1, new[] { projectA, projectB }); // Cycle with no connecting edges

        // Act
        var result = _identifier.IdentifyWeakEdges(new[] { cycle }, graph);

        // Assert - Should skip cycle without throwing
        result.Should().HaveCount(1);
        result[0].WeakCouplingEdges.Should().BeEmpty();
        result[0].WeakCouplingScore.Should().BeNull(); // Not analyzed (no edges found)
    }

    // Helper methods
    private AdjacencyGraph<ProjectNode, DependencyEdge> CreateEmptyGraph()
    {
        return new AdjacencyGraph<ProjectNode, DependencyEdge>();
    }

    private ProjectNode CreateProjectNode(string name)
    {
        return new ProjectNode
        {
            ProjectName = name,
            ProjectPath = $"D:\\test\\{name}.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
    }

    private DependencyEdge CreateEdge(ProjectNode source, ProjectNode target, int couplingScore)
    {
        return new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference,
            CouplingScore = couplingScore,
            CouplingStrength = CouplingClassifier.ClassifyCouplingStrength(couplingScore)
        };
    }
}
