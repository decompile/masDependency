namespace MasDependencyMap.Core.Tests.DependencyAnalysis;

using FluentAssertions;
using MasDependencyMap.Core.DependencyAnalysis;
using Xunit;

public class DependencyEdgeTests
{
    [Fact]
    public void IsCrossSolution_DifferentSolutions_ReturnsTrue()
    {
        // Arrange
        var source = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Solution1\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var target = new ProjectNode
        {
            ProjectName = "Project2",
            ProjectPath = @"C:\Solution2\Project2\Project2.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution2"
        };

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        // Act & Assert
        edge.IsCrossSolution.Should().BeTrue();
    }

    [Fact]
    public void IsCrossSolution_SameSolution_ReturnsFalse()
    {
        // Arrange
        var source = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Solution1\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var target = new ProjectNode
        {
            ProjectName = "Project2",
            ProjectPath = @"C:\Solution1\Project2\Project2.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        // Act & Assert
        edge.IsCrossSolution.Should().BeFalse();
    }

    [Fact]
    public void IsProjectReference_ProjectReferenceType_ReturnsTrue()
    {
        // Arrange
        var source = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var target = new ProjectNode
        {
            ProjectName = "Project2",
            ProjectPath = @"C:\Projects\Project2\Project2.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        // Act & Assert
        edge.IsProjectReference.Should().BeTrue();
    }

    [Fact]
    public void IsProjectReference_BinaryReferenceType_ReturnsFalse()
    {
        // Arrange
        var source = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var target = new ProjectNode
        {
            ProjectName = "External.Library",
            ProjectPath = @"C:\External\External.Library.dll",
            TargetFramework = "netstandard2.0",
            SolutionName = "External"
        };

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.BinaryReference
        };

        // Act & Assert
        edge.IsProjectReference.Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var source = new ProjectNode
        {
            ProjectName = "SourceProject",
            ProjectPath = @"C:\Projects\SourceProject\SourceProject.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var target = new ProjectNode
        {
            ProjectName = "TargetProject",
            ProjectPath = @"C:\Projects\TargetProject\TargetProject.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        // Act
        var result = edge.ToString();

        // Assert
        result.Should().Be("SourceProject -> TargetProject (ProjectReference)");
    }

    [Fact]
    public void DependencyEdge_ImplementsIEdge()
    {
        // Arrange
        var source = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var target = new ProjectNode
        {
            ProjectName = "Project2",
            ProjectPath = @"C:\Projects\Project2\Project2.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var edge = new DependencyEdge
        {
            Source = source,
            Target = target,
            DependencyType = DependencyType.ProjectReference
        };

        // Act & Assert
        edge.Source.Should().Be(source);
        edge.Target.Should().Be(target);
    }
}
