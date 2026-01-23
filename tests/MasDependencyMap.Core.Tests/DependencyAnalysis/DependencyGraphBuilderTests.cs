namespace MasDependencyMap.Core.Tests.DependencyAnalysis;

using FluentAssertions;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.SolutionLoading;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class DependencyGraphBuilderTests
{
    private readonly DependencyGraphBuilder _builder;

    public DependencyGraphBuilderTests()
    {
        _builder = new DependencyGraphBuilder(NullLogger<DependencyGraphBuilder>.Instance);
    }

    [Fact]
    public async Task BuildAsync_SingleProjectNoReferences_CreatesOneVertex()
    {
        // Arrange
        var solution = new SolutionAnalysis
        {
            SolutionName = "TestSolution",
            SolutionPath = @"C:\Solutions\TestSolution.sln",
            LoaderType = "Test",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "Project1",
                    FilePath = @"C:\Projects\Project1\Project1.csproj",
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = Array.Empty<ProjectReference>()
                }
            }
        };

        // Act
        var graph = await _builder.BuildAsync(solution);

        // Assert
        graph.VertexCount.Should().Be(1);
        graph.EdgeCount.Should().Be(0);
        graph.Vertices.Should().ContainSingle(v => v.ProjectName == "Project1");
    }

    [Fact]
    public async Task BuildAsync_TwoProjectsWithReference_CreatesEdge()
    {
        // Arrange
        var project2Path = @"C:\Projects\Project2\Project2.csproj";
        var solution = new SolutionAnalysis
        {
            SolutionName = "TestSolution",
            SolutionPath = @"C:\Solutions\TestSolution.sln",
            LoaderType = "Test",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "Project1",
                    FilePath = @"C:\Projects\Project1\Project1.csproj",
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = new List<ProjectReference>
                    {
                        new ProjectReference
                        {
                            TargetName = "Project2",
                            TargetPath = project2Path,
                            Type = ReferenceType.ProjectReference
                        }
                    }
                },
                new ProjectInfo
                {
                    Name = "Project2",
                    FilePath = project2Path,
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = Array.Empty<ProjectReference>()
                }
            }
        };

        // Act
        var graph = await _builder.BuildAsync(solution);

        // Assert
        graph.VertexCount.Should().Be(2);
        graph.EdgeCount.Should().Be(1);

        var edge = graph.Edges.Should().ContainSingle().Subject;
        edge.Source.ProjectName.Should().Be("Project1");
        edge.Target.ProjectName.Should().Be("Project2");
        edge.DependencyType.Should().Be(DependencyType.ProjectReference);
    }

    [Fact]
    public async Task BuildAsync_OrphanedProject_DetectsOrphan()
    {
        // Arrange
        var solution = new SolutionAnalysis
        {
            SolutionName = "TestSolution",
            SolutionPath = @"C:\Solutions\TestSolution.sln",
            LoaderType = "Test",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "OrphanProject",
                    FilePath = @"C:\Projects\OrphanProject\OrphanProject.csproj",
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = Array.Empty<ProjectReference>()
                }
            }
        };

        // Act
        var graph = await _builder.BuildAsync(solution);

        // Assert
        var orphaned = graph.DetectOrphanedNodes().ToList();
        orphaned.Should().ContainSingle();
        orphaned.First().ProjectName.Should().Be("OrphanProject");
    }

    [Fact]
    public async Task BuildAsync_MultiSolution_CreatesUnifiedGraph()
    {
        // Arrange
        var sharedProjectPath = @"C:\Projects\SharedProject\SharedProject.csproj";

        var solution1 = new SolutionAnalysis
        {
            SolutionName = "Solution1",
            SolutionPath = @"C:\Solutions\Solution1.sln",
            LoaderType = "Test",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "Project1",
                    FilePath = @"C:\Projects\Project1\Project1.csproj",
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = new List<ProjectReference>
                    {
                        new ProjectReference
                        {
                            TargetName = "SharedProject",
                            TargetPath = sharedProjectPath,
                            Type = ReferenceType.ProjectReference
                        }
                    }
                }
            }
        };

        var solution2 = new SolutionAnalysis
        {
            SolutionName = "Solution2",
            SolutionPath = @"C:\Solutions\Solution2.sln",
            LoaderType = "Test",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "SharedProject",
                    FilePath = sharedProjectPath,
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = Array.Empty<ProjectReference>()
                }
            }
        };

        // Act
        var graph = await _builder.BuildAsync(new[] { solution1, solution2 });

        // Assert
        graph.VertexCount.Should().Be(2);
        graph.EdgeCount.Should().Be(1);

        // Verify cross-solution dependency
        var edge = graph.Edges.Should().ContainSingle().Subject;
        edge.IsCrossSolution.Should().BeTrue();
        edge.Source.SolutionName.Should().Be("Solution1");
        edge.Target.SolutionName.Should().Be("Solution2");
    }

    [Fact]
    public async Task BuildAsync_MultiSolution_NoDuplicateVertices()
    {
        // Arrange
        var sharedProjectPath = @"C:\Projects\SharedProject\SharedProject.csproj";

        var solution1 = new SolutionAnalysis
        {
            SolutionName = "Solution1",
            SolutionPath = @"C:\Solutions\Solution1.sln",
            LoaderType = "Test",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "SharedProject",
                    FilePath = sharedProjectPath,
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = Array.Empty<ProjectReference>()
                }
            }
        };

        var solution2 = new SolutionAnalysis
        {
            SolutionName = "Solution2",
            SolutionPath = @"C:\Solutions\Solution2.sln",
            LoaderType = "Test",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "SharedProject",
                    FilePath = sharedProjectPath,
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = Array.Empty<ProjectReference>()
                }
            }
        };

        // Act
        var graph = await _builder.BuildAsync(new[] { solution1, solution2 });

        // Assert
        graph.VertexCount.Should().Be(1);
        graph.Vertices.Should().ContainSingle(v => v.ProjectPath == sharedProjectPath);
    }

    [Fact]
    public async Task BuildAsync_ComplexDependencies_BuildsCorrectGraph()
    {
        // Arrange
        var corePath = @"C:\Projects\Core\Core.csproj";
        var dataPath = @"C:\Projects\Data\Data.csproj";
        var apiPath = @"C:\Projects\API\API.csproj";

        var solution = new SolutionAnalysis
        {
            SolutionName = "MultiTier",
            SolutionPath = @"C:\Solutions\MultiTier.sln",
            LoaderType = "Test",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "Core",
                    FilePath = corePath,
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = Array.Empty<ProjectReference>()
                },
                new ProjectInfo
                {
                    Name = "Data",
                    FilePath = dataPath,
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = new List<ProjectReference>
                    {
                        new ProjectReference
                        {
                            TargetName = "Core",
                            TargetPath = corePath,
                            Type = ReferenceType.ProjectReference
                        }
                    }
                },
                new ProjectInfo
                {
                    Name = "API",
                    FilePath = apiPath,
                    TargetFramework = "net8.0",
                    Language = "C#",
                    References = new List<ProjectReference>
                    {
                        new ProjectReference
                        {
                            TargetName = "Core",
                            TargetPath = corePath,
                            Type = ReferenceType.ProjectReference
                        },
                        new ProjectReference
                        {
                            TargetName = "Data",
                            TargetPath = dataPath,
                            Type = ReferenceType.ProjectReference
                        }
                    }
                }
            }
        };

        // Act
        var graph = await _builder.BuildAsync(solution);

        // Assert
        graph.VertexCount.Should().Be(3);
        graph.EdgeCount.Should().Be(3);

        // Verify Core has no dependencies (is a leaf)
        var coreNode = graph.Vertices.First(v => v.ProjectName == "Core");
        graph.GetOutEdges(coreNode).Should().BeEmpty();
        graph.GetInEdges(coreNode).Should().HaveCount(2); // Referenced by Data and API

        // Verify API has 2 dependencies
        var apiNode = graph.Vertices.First(v => v.ProjectName == "API");
        graph.GetOutEdges(apiNode).Should().HaveCount(2);
    }

    [Fact]
    public async Task BuildAsync_NullSolution_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _builder.BuildAsync((SolutionAnalysis)null!));
    }

    [Fact]
    public async Task BuildAsync_NullSolutions_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _builder.BuildAsync((IEnumerable<SolutionAnalysis>)null!));
    }
}
