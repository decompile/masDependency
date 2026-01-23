namespace MasDependencyMap.Core.Tests.DependencyAnalysis;

using FluentAssertions;
using MasDependencyMap.Core.DependencyAnalysis;
using Xunit;

public class DependencyGraphTests
{
    [Fact]
    public void Constructor_CreatesEmptyGraph()
    {
        // Act
        var graph = new DependencyGraph();

        // Assert
        graph.VertexCount.Should().Be(0);
        graph.EdgeCount.Should().Be(0);
        graph.Vertices.Should().BeEmpty();
        graph.Edges.Should().BeEmpty();
    }

    [Fact]
    public void AddVertex_AddsNewVertex_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        var node = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"C:\Projects\TestProject.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        // Act
        var result = graph.AddVertex(node);

        // Assert
        result.Should().BeTrue();
        graph.VertexCount.Should().Be(1);
        graph.Vertices.Should().Contain(node);
    }

    [Fact]
    public void AddVertex_AddsDuplicateVertex_ReturnsFalse()
    {
        // Arrange
        var graph = new DependencyGraph();
        var node = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"C:\Projects\TestProject.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(node);

        // Act
        var result = graph.AddVertex(node);

        // Assert
        result.Should().BeFalse();
        graph.VertexCount.Should().Be(1);
    }

    [Fact]
    public void AddVertex_NullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => graph.AddVertex(null!));
    }

    [Fact]
    public void AddEdge_AddsNewEdge_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        var source = new ProjectNode
        {
            ProjectName = "Source",
            ProjectPath = @"C:\Projects\Source.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target = new ProjectNode
        {
            ProjectName = "Target",
            ProjectPath = @"C:\Projects\Target.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(source);
        graph.AddVertex(target);

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        // Act
        var result = graph.AddEdge(edge);

        // Assert
        result.Should().BeTrue();
        graph.EdgeCount.Should().Be(1);
        graph.Edges.Should().Contain(edge);
    }

    [Fact]
    public void AddEdge_NullEdge_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => graph.AddEdge(null!));
    }

    [Fact]
    public void GetOutEdges_ReturnsOutgoingEdges()
    {
        // Arrange
        var graph = new DependencyGraph();
        var source = new ProjectNode
        {
            ProjectName = "Source",
            ProjectPath = @"C:\Projects\Source.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target1 = new ProjectNode
        {
            ProjectName = "Target1",
            ProjectPath = @"C:\Projects\Target1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target2 = new ProjectNode
        {
            ProjectName = "Target2",
            ProjectPath = @"C:\Projects\Target2.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(source);
        graph.AddVertex(target1);
        graph.AddVertex(target2);

        var edge1 = new DependencyEdge
        {
            Source = source,
            Target = target1,
            DependencyType = DependencyType.ProjectReference
        };
        var edge2 = new DependencyEdge
        {
            Source = source,
            Target = target2,
            DependencyType = DependencyType.ProjectReference
        };

        graph.AddEdge(edge1);
        graph.AddEdge(edge2);

        // Act
        var outEdges = graph.GetOutEdges(source).ToList();

        // Assert
        outEdges.Should().HaveCount(2);
        outEdges.Should().Contain(edge1);
        outEdges.Should().Contain(edge2);
    }

    [Fact]
    public void GetInEdges_ReturnsIncomingEdges()
    {
        // Arrange
        var graph = new DependencyGraph();
        var target = new ProjectNode
        {
            ProjectName = "Target",
            ProjectPath = @"C:\Projects\Target.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var source1 = new ProjectNode
        {
            ProjectName = "Source1",
            ProjectPath = @"C:\Projects\Source1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var source2 = new ProjectNode
        {
            ProjectName = "Source2",
            ProjectPath = @"C:\Projects\Source2.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(target);
        graph.AddVertex(source1);
        graph.AddVertex(source2);

        var edge1 = new DependencyEdge
        {
            Source = source1,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };
        var edge2 = new DependencyEdge
        {
            Source = source2,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        graph.AddEdge(edge1);
        graph.AddEdge(edge2);

        // Act
        var inEdges = graph.GetInEdges(target).ToList();

        // Assert
        inEdges.Should().HaveCount(2);
        inEdges.Should().Contain(edge1);
        inEdges.Should().Contain(edge2);
    }

    [Fact]
    public void IsOutEdgesEmpty_NoOutgoingEdges_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        var node = new ProjectNode
        {
            ProjectName = "LeafNode",
            ProjectPath = @"C:\Projects\LeafNode.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(node);

        // Act
        var result = graph.IsOutEdgesEmpty(node);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsOutEdgesEmpty_HasOutgoingEdges_ReturnsFalse()
    {
        // Arrange
        var graph = new DependencyGraph();
        var source = new ProjectNode
        {
            ProjectName = "Source",
            ProjectPath = @"C:\Projects\Source.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target = new ProjectNode
        {
            ProjectName = "Target",
            ProjectPath = @"C:\Projects\Target.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(source);
        graph.AddVertex(target);

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        graph.AddEdge(edge);

        // Act
        var result = graph.IsOutEdgesEmpty(source);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInEdgesEmpty_NoIncomingEdges_ReturnsTrue()
    {
        // Arrange
        var graph = new DependencyGraph();
        var node = new ProjectNode
        {
            ProjectName = "RootNode",
            ProjectPath = @"C:\Projects\RootNode.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(node);

        // Act
        var result = graph.IsInEdgesEmpty(node);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInEdgesEmpty_HasIncomingEdges_ReturnsFalse()
    {
        // Arrange
        var graph = new DependencyGraph();
        var source = new ProjectNode
        {
            ProjectName = "Source",
            ProjectPath = @"C:\Projects\Source.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target = new ProjectNode
        {
            ProjectName = "Target",
            ProjectPath = @"C:\Projects\Target.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(source);
        graph.AddVertex(target);

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        graph.AddEdge(edge);

        // Act
        var result = graph.IsInEdgesEmpty(target);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DetectOrphanedNodes_NoOrphans_ReturnsEmpty()
    {
        // Arrange
        var graph = new DependencyGraph();
        var source = new ProjectNode
        {
            ProjectName = "Source",
            ProjectPath = @"C:\Projects\Source.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var target = new ProjectNode
        {
            ProjectName = "Target",
            ProjectPath = @"C:\Projects\Target.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(source);
        graph.AddVertex(target);

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        graph.AddEdge(edge);

        // Act
        var orphaned = graph.DetectOrphanedNodes().ToList();

        // Assert
        orphaned.Should().BeEmpty();
    }

    [Fact]
    public void DetectOrphanedNodes_HasOrphans_ReturnsOrphanedNodes()
    {
        // Arrange
        var graph = new DependencyGraph();
        var connected1 = new ProjectNode
        {
            ProjectName = "Connected1",
            ProjectPath = @"C:\Projects\Connected1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var connected2 = new ProjectNode
        {
            ProjectName = "Connected2",
            ProjectPath = @"C:\Projects\Connected2.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var orphan = new ProjectNode
        {
            ProjectName = "Orphan",
            ProjectPath = @"C:\Projects\Orphan.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(connected1);
        graph.AddVertex(connected2);
        graph.AddVertex(orphan);

        var edge = new DependencyEdge
        {
            Source = connected1,
            Target = connected2,
            DependencyType = DependencyType.ProjectReference
        };

        graph.AddEdge(edge);

        // Act
        var orphaned = graph.DetectOrphanedNodes().ToList();

        // Assert
        orphaned.Should().ContainSingle();
        orphaned.First().ProjectName.Should().Be("Orphan");
    }

    [Fact]
    public void GetUnderlyingGraph_ReturnsWrappedGraph()
    {
        // Arrange
        var graph = new DependencyGraph();
        var node = new ProjectNode
        {
            ProjectName = "TestProject",
            ProjectPath = @"C:\Projects\TestProject.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(node);

        // Act
        var underlyingGraph = graph.GetUnderlyingGraph();

        // Assert
        underlyingGraph.Should().NotBeNull();
        underlyingGraph.VertexCount.Should().Be(1);
        underlyingGraph.Vertices.Should().Contain(node);
    }

    [Fact]
    public void GetOutEdges_NullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => graph.GetOutEdges(null!));
    }

    [Fact]
    public void GetInEdges_NullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => graph.GetInEdges(null!));
    }

    [Fact]
    public void IsOutEdgesEmpty_NullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => graph.IsOutEdgesEmpty(null!));
    }

    [Fact]
    public void IsInEdgesEmpty_NullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => graph.IsInEdgesEmpty(null!));
    }
}
