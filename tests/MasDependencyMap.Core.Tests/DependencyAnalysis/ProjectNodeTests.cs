namespace MasDependencyMap.Core.Tests.DependencyAnalysis;

using FluentAssertions;
using MasDependencyMap.Core.DependencyAnalysis;
using Xunit;

public class ProjectNodeTests
{
    [Fact]
    public void Equals_SameProjectPath_ReturnsTrue()
    {
        // Arrange
        var node1 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var node2 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        // Act & Assert
        node1.Equals(node2).Should().BeTrue();
        (node1 == node2).Should().BeTrue();
        (node1 != node2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentProjectPath_ReturnsFalse()
    {
        // Arrange
        var node1 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var node2 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project2\Project2.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        // Act & Assert
        node1.Equals(node2).Should().BeFalse();
        (node1 == node2).Should().BeFalse();
        (node1 != node2).Should().BeTrue();
    }

    [Fact]
    public void Equals_CaseInsensitivePath_ReturnsTrue()
    {
        // Arrange
        var node1 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var node2 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"c:\projects\project1\project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        // Act & Assert
        node1.Equals(node2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameProjectPath_ReturnsSameHash()
    {
        // Arrange
        var node1 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var node2 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        // Act & Assert
        node1.GetHashCode().Should().Be(node2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_CaseInsensitivePath_ReturnsSameHash()
    {
        // Arrange
        var node1 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        var node2 = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"c:\projects\project1\project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        // Act & Assert
        node1.GetHashCode().Should().Be(node2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsProjectName()
    {
        // Arrange
        var node = new ProjectNode
        {
            ProjectName = "MyProject",
            ProjectPath = @"C:\Projects\MyProject\MyProject.csproj",
            TargetFramework = "net8.0",
            SolutionName = "MySolution"
        };

        // Act
        var result = node.ToString();

        // Assert
        result.Should().Be("MyProject");
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var node = new ProjectNode
        {
            ProjectName = "Project1",
            ProjectPath = @"C:\Projects\Project1\Project1.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        // Act & Assert
        node.Equals(null).Should().BeFalse();
        (node == null).Should().BeFalse();
        (node != null).Should().BeTrue();
    }
}
