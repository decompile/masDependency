namespace MasDependencyMap.Core.Tests.CycleAnalysis;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;
using QuikGraph;
using Xunit;

public class RoslynCouplingAnalyzerTests
{
    private readonly RoslynCouplingAnalyzer _analyzer;

    public RoslynCouplingAnalyzerTests()
    {
        var logger = NullLogger<RoslynCouplingAnalyzer>.Instance;
        _analyzer = new RoslynCouplingAnalyzer(logger);
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyGraph_ReturnsEmptyResult()
    {
        // Arrange
        var graph = new AdjacencyGraph<ProjectNode, DependencyEdge>();

        // Act
        var result = await _analyzer.AnalyzeAsync(graph, solution: null);

        // Assert
        result.EdgeCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _analyzer.AnalyzeAsync(null!, solution: null);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("graph");
    }

    [Fact]
    public async Task AnalyzeAsync_RoslynUnavailable_FallsBackToReferenceCount()
    {
        // Arrange
        var graph = CreateGraphWithMultipleEdges();

        // Act - Pass null solution to trigger fallback
        var result = await _analyzer.AnalyzeAsync(graph, solution: null);

        // Assert - All edges should have coupling score = 1 (reference count fallback)
        result.Edges.Should().OnlyContain(edge => edge.CouplingScore == 1);
        result.Edges.Should().OnlyContain(edge => edge.CouplingStrength == CouplingStrength.Weak);
    }

    [Fact]
    public void CouplingClassifier_WeakCoupling_ClassifiedCorrectly()
    {
        // Arrange - Test boundary values for weak coupling (1-5 calls)

        // Act & Assert
        CouplingClassifier.ClassifyCouplingStrength(1).Should().Be(CouplingStrength.Weak);
        CouplingClassifier.ClassifyCouplingStrength(3).Should().Be(CouplingStrength.Weak);
        CouplingClassifier.ClassifyCouplingStrength(5).Should().Be(CouplingStrength.Weak);
    }

    [Fact]
    public void CouplingClassifier_MediumCoupling_ClassifiedCorrectly()
    {
        // Arrange - Test boundary values for medium coupling (6-20 calls)

        // Act & Assert
        CouplingClassifier.ClassifyCouplingStrength(6).Should().Be(CouplingStrength.Medium);
        CouplingClassifier.ClassifyCouplingStrength(10).Should().Be(CouplingStrength.Medium);
        CouplingClassifier.ClassifyCouplingStrength(20).Should().Be(CouplingStrength.Medium);
    }

    [Fact]
    public void CouplingClassifier_StrongCoupling_ClassifiedCorrectly()
    {
        // Arrange - Test boundary values for strong coupling (21+ calls)

        // Act & Assert
        CouplingClassifier.ClassifyCouplingStrength(21).Should().Be(CouplingStrength.Strong);
        CouplingClassifier.ClassifyCouplingStrength(50).Should().Be(CouplingStrength.Strong);
        CouplingClassifier.ClassifyCouplingStrength(100).Should().Be(CouplingStrength.Strong);
    }

    [Fact]
    public void DependencyEdge_DefaultCouplingProperties_SetCorrectly()
    {
        // Arrange & Act
        var source = new ProjectNode
        {
            ProjectName = "SourceProject",
            ProjectPath = "C:\\Source\\Source.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target = new ProjectNode
        {
            ProjectName = "TargetProject",
            ProjectPath = "C:\\Target\\Target.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        // Assert - Default values should be set
        edge.CouplingScore.Should().Be(1); // Default reference count
        edge.CouplingStrength.Should().Be(CouplingStrength.Weak); // Default classification
    }

    [Fact]
    public void DependencyEdge_SetCouplingProperties_UpdatesCorrectly()
    {
        // Arrange
        var source = new ProjectNode
        {
            ProjectName = "SourceProject",
            ProjectPath = "C:\\Source\\Source.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target = new ProjectNode
        {
            ProjectName = "TargetProject",
            ProjectPath = "C:\\Target\\Target.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        // Act
        edge.CouplingScore = 25;
        edge.CouplingStrength = CouplingStrength.Strong;

        // Assert
        edge.CouplingScore.Should().Be(25);
        edge.CouplingStrength.Should().Be(CouplingStrength.Strong);
    }

    [Fact]
    public void DependencyEdge_ToString_IncludesCouplingInformation()
    {
        // Arrange
        var source = new ProjectNode
        {
            ProjectName = "SourceProject",
            ProjectPath = "C:\\Source\\Source.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target = new ProjectNode
        {
            ProjectName = "TargetProject",
            ProjectPath = "C:\\Target\\Target.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference,
            CouplingScore = 15,
            CouplingStrength = CouplingStrength.Medium
        };

        // Act
        var result = edge.ToString();

        // Assert
        result.Should().Contain("SourceProject");
        result.Should().Contain("TargetProject");
        result.Should().Contain("15 calls");
        result.Should().Contain("Medium");
    }

    [Fact]
    public async Task AnalyzeAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var graph = CreateGraphWithMultipleEdges();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await _analyzer.AnalyzeAsync(graph, solution: null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void CouplingClassifier_NegativeInput_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Action act = () => CouplingClassifier.ClassifyCouplingStrength(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CouplingClassifier_ZeroCalls_ReturnsWeak()
    {
        // Act
        var result = CouplingClassifier.ClassifyCouplingStrength(0);

        // Assert
        result.Should().Be(CouplingStrength.Weak);
    }

    // Helper method to create a graph with multiple edges for testing
    private static AdjacencyGraph<ProjectNode, DependencyEdge> CreateGraphWithMultipleEdges()
    {
        var graph = new AdjacencyGraph<ProjectNode, DependencyEdge>();

        // Create project nodes
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "C:\\ProjectA\\ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "C:\\ProjectB\\ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = "C:\\ProjectC\\ProjectC.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var projectD = new ProjectNode
        {
            ProjectName = "ProjectD",
            ProjectPath = "C:\\ProjectD\\ProjectD.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        // Add vertices
        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);

        // Add edges
        graph.AddEdge(new DependencyEdge
        {
            Source = projectA,
            Target = projectB,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectB,
            Target = projectC,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectC,
            Target = projectD,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectA,
            Target = projectC,
            DependencyType = DependencyType.BinaryReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectB,
            Target = projectD,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }
}
