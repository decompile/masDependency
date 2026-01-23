namespace MasDependencyMap.Core.Tests.Visualization;

using FluentAssertions;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.Visualization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class DotGeneratorTests
{
    private readonly DotGenerator _generator;

    public DotGeneratorTests()
    {
        _generator = new DotGenerator(NullLogger<DotGenerator>.Instance);
    }

    [Fact]
    public async Task GenerateAsync_SimpleGraph_GeneratesValidDotFile()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, solutionName);

            // Assert
            filePath.Should().NotBeNullOrEmpty();
            File.Exists(filePath).Should().BeTrue();

            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Contain("digraph dependencies {");
            content.Should().Contain("}");
            content.Should().Contain("ProjectA");
            content.Should().Contain("ProjectB");
            content.Should().Contain("->");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _generator.GenerateAsync(
            null!,
            "output",
            "solution");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("graph");
    }

    [Fact]
    public async Task GenerateAsync_EmptyOutputDirectory_ThrowsArgumentException()
    {
        // Arrange
        var graph = CreateSimpleGraph();

        // Act
        Func<Task> act = async () => await _generator.GenerateAsync(
            graph,
            "",
            "solution");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("outputDirectory");
    }

    [Fact]
    public async Task GenerateAsync_NullOutputDirectory_ThrowsArgumentException()
    {
        // Arrange
        var graph = CreateSimpleGraph();

        // Act
        Func<Task> act = async () => await _generator.GenerateAsync(
            graph,
            null!,
            "solution");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("outputDirectory");
    }

    [Fact]
    public async Task GenerateAsync_EmptySolutionName_ThrowsArgumentException()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = Path.GetTempPath();

        // Act
        Func<Task> act = async () => await _generator.GenerateAsync(
            graph,
            outputDir,
            "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("solutionName");
    }

    [Fact]
    public async Task GenerateAsync_NullSolutionName_ThrowsArgumentException()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = Path.GetTempPath();

        // Act
        Func<Task> act = async () => await _generator.GenerateAsync(
            graph,
            outputDir,
            null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("solutionName");
    }

    [Fact]
    public async Task GenerateAsync_ProjectNameWithSpaces_EscapesCorrectly()
    {
        // Arrange
        var graph = CreateGraphWithSpecialChars();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, "Test");
            var content = await File.ReadAllTextAsync(filePath);

            // Assert
            content.Should().Contain("\"My Project\"");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_ProjectNameWithQuotes_EscapesCorrectly()
    {
        // Arrange
        var graph = new DependencyGraph();
        var node = new ProjectNode
        {
            ProjectName = "Project\"Quote\"",
            ProjectPath = "/path/to/project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        graph.AddVertex(node);

        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, "Test");
            var content = await File.ReadAllTextAsync(filePath);

            // Assert
            content.Should().Contain("\\\"");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_CrossSolutionEdges_AppliesColorCoding()
    {
        // Arrange
        var graph = CreateCrossSolutionGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, "Test");
            var content = await File.ReadAllTextAsync(filePath);

            // Assert
            content.Should().Contain("[color=");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_SameSolutionEdges_NoColorAttribute()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, "Test");
            var content = await File.ReadAllTextAsync(filePath);

            // Assert - same-solution edges should have black color (intra-solution dependencies)
            var lines = content.Split('\n');
            var edgeLine = lines.First(l => l.Contains("->") && l.Contains("ProjectA") && l.Contains("ProjectB"));
            edgeLine.Should().Contain("[color=\"black\"]");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_OutputDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid(), "nested", "path");

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, "Test");

            // Assert
            Directory.Exists(outputDir).Should().BeTrue();
            File.Exists(filePath).Should().BeTrue();
        }
        finally
        {
            try
            {
                var topDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(outputDir))!));
                if (Directory.Exists(topDir))
                    Directory.Delete(topDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_InvalidCharsInSolutionName_SanitizesFilename()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "Test<>Solution|Name";

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, solutionName);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            Path.GetFileName(filePath).Should().NotContain("<");
            Path.GetFileName(filePath).Should().NotContain(">");
            Path.GetFileName(filePath).Should().NotContain("|");
            Path.GetFileName(filePath).Should().EndWith("-dependencies.dot");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_EmptyGraph_GeneratesValidDotFile()
    {
        // Arrange
        var graph = new DependencyGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, "Empty");
            var content = await File.ReadAllTextAsync(filePath);

            // Assert
            content.Should().Contain("digraph dependencies {");
            content.Should().Contain("}");
            content.Should().NotContain("->");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_LargeGraph_CompletesSuccessfully()
    {
        // Arrange
        var graph = CreateLargeGraph(100, 200);
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, "Large");

            // Assert
            File.Exists(filePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Contain("digraph dependencies {");
            var fileInfo = new FileInfo(filePath);
            fileInfo.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_GraphvizCompatibleSyntax_ContainsRequiredAttributes()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, "Test");
            var content = await File.ReadAllTextAsync(filePath);

            // Assert - Graphviz 2.38+ compatible syntax
            content.Should().Contain("rankdir=LR");
            content.Should().Contain("nodesep=0.5");
            content.Should().Contain("ranksep=1.0");
            content.Should().Contain("node [shape=box, style=filled]");
            content.Should().Contain("edge [arrowhead=normal]");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_ReturnsAbsolutePath()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = "relative-path";
        var solutionName = "Test";

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, solutionName);

            // Assert
            Path.IsPathRooted(filePath).Should().BeTrue();
        }
        finally
        {
            try
            {
                var absoluteDir = Path.GetFullPath(outputDir);
                if (Directory.Exists(absoluteDir))
                    Directory.Delete(absoluteDir, true);
            }
            catch
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var graph = CreateSimpleGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act
            Func<Task> act = async () => await _generator.GenerateAsync(
                graph,
                outputDir,
                "Test",
                cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_MultiSolutionGraph_GeneratesEcosystemFilename()
    {
        // Arrange - create multi-solution graph with projects from different solutions
        var graph = CreateMultiSolutionGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "Solution1"; // Any name, should be ignored for multi-solution

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, solutionName);

            // Assert
            filePath.Should().NotBeNullOrEmpty();
            Path.GetFileName(filePath).Should().Be("Ecosystem-dependencies.dot", "multi-solution graphs should use 'Ecosystem-dependencies.dot' naming");

            // Verify file content includes legend
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Contain("subgraph cluster_legend", "multi-solution graphs should include legend");
            content.Should().Contain("Solutions", "legend should show solution names");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_SingleSolutionGraph_GeneratesSolutionNamedFile()
    {
        // Arrange - create single-solution graph
        var graph = CreateSimpleGraph();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "MySolution";

        try
        {
            // Act
            var filePath = await _generator.GenerateAsync(graph, outputDir, solutionName);

            // Assert
            filePath.Should().NotBeNullOrEmpty();
            Path.GetFileName(filePath).Should().Be("MySolution-dependencies.dot", "single-solution graphs should use '{SolutionName}-dependencies.dot' naming");
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private DependencyGraph CreateSimpleGraph()
    {
        var graph = new DependencyGraph();
        var nodeA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "/path/to/ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
        var nodeB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "/path/to/ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(nodeA);
        graph.AddVertex(nodeB);
        graph.AddEdge(new DependencyEdge
        {
            Source = nodeA,
            Target = nodeB,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private DependencyGraph CreateGraphWithSpecialChars()
    {
        var graph = new DependencyGraph();
        var node = new ProjectNode
        {
            ProjectName = "My Project",
            ProjectPath = "/path/to/My Project.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(node);
        return graph;
    }

    private DependencyGraph CreateCrossSolutionGraph()
    {
        var graph = new DependencyGraph();
        var nodeA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "/path/to/ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var nodeB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "/path/to/ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution2"
        };

        graph.AddVertex(nodeA);
        graph.AddVertex(nodeB);
        graph.AddEdge(new DependencyEdge
        {
            Source = nodeA,
            Target = nodeB,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private DependencyGraph CreateMultiSolutionGraph()
    {
        // Create graph with projects from 3 different solutions
        var graph = new DependencyGraph();

        var nodeA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "/path/to/ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var nodeB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "/path/to/ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution2"
        };
        var nodeC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = "/path/to/ProjectC.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution3"
        };

        graph.AddVertex(nodeA);
        graph.AddVertex(nodeB);
        graph.AddVertex(nodeC);

        // Add cross-solution edges
        graph.AddEdge(new DependencyEdge
        {
            Source = nodeA,
            Target = nodeB,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = nodeB,
            Target = nodeC,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private DependencyGraph CreateLargeGraph(int nodeCount, int edgeCount)
    {
        var graph = new DependencyGraph();
        var nodes = new List<ProjectNode>();

        // Create nodes
        for (int i = 0; i < nodeCount; i++)
        {
            var node = new ProjectNode
            {
                ProjectName = $"Project{i}",
                ProjectPath = $"/path/to/Project{i}.csproj",
                TargetFramework = "net8.0",
                SolutionName = "TestSolution"
            };
            graph.AddVertex(node);
            nodes.Add(node);
        }

        // Create edges
        var random = new Random(42); // Fixed seed for deterministic tests
        for (int i = 0; i < edgeCount; i++)
        {
            var source = nodes[random.Next(nodeCount)];
            var target = nodes[random.Next(nodeCount)];

            if (source != target)
            {
                try
                {
                    graph.AddEdge(new DependencyEdge
                    {
                        Source = source,
                        Target = target,
                        DependencyType = DependencyType.ProjectReference
                    });
                }
                catch (InvalidOperationException)
                {
                    // Edge already exists, skip
                }
            }
        }

        return graph;
    }
}
