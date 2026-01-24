namespace MasDependencyMap.Core.Tests.Visualization;

using FluentAssertions;
using MasDependencyMap.Core.CycleAnalysis;
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
                cycles: null,
                recommendations: null,
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

    // ========== NEW TESTS FOR STORY 3.6: CYCLE HIGHLIGHTING ==========

    [Fact]
    public async Task GenerateAsync_WithCycles_EdgesInCyclesAreRed()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo>
        {
            new CycleInfo(1, graph.Vertices.ToList())
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - Verify that ALL edges in the cycle are colored red
            dotContent.Should().Contain("\"ProjectA\" -> \"ProjectB\" [color=\"red\", style=\"bold\"]");
            dotContent.Should().Contain("\"ProjectB\" -> \"ProjectC\" [color=\"red\", style=\"bold\"]");
            dotContent.Should().Contain("\"ProjectC\" -> \"ProjectA\" [color=\"red\", style=\"bold\"]");
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
    public async Task GenerateAsync_WithCycles_EdgesNotInCyclesAreBlack()
    {
        // Arrange
        var graph = CreateGraphWithCycleAndNonCyclicEdge();
        var cycleProjects = new List<ProjectNode> { graph.Vertices.ElementAt(0), graph.Vertices.ElementAt(1) };
        var cycles = new List<CycleInfo> { new CycleInfo(1, cycleProjects) };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert
            dotContent.Should().Contain("ProjectC\" -> \"ProjectD\" [color=\"black\"]");
            dotContent.Should().NotContain("ProjectC\" -> \"ProjectD\" [color=\"red\"");
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
    public async Task GenerateAsync_WithMultipleCycles_AllCyclicEdgesAreRed()
    {
        // Arrange
        var graph = CreateGraphWithMultipleCycles();
        var cycle1 = new CycleInfo(1, new[] { graph.Vertices.ElementAt(0), graph.Vertices.ElementAt(1) });
        var cycle2 = new CycleInfo(2, new[] { graph.Vertices.ElementAt(2), graph.Vertices.ElementAt(3) });
        var cycles = new List<CycleInfo> { cycle1, cycle2 };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - All cyclic edges should be red
            dotContent.Should().Contain("ProjectA\" -> \"ProjectB\" [color=\"red\", style=\"bold\"]");
            dotContent.Should().Contain("ProjectB\" -> \"ProjectA\" [color=\"red\", style=\"bold\"]");
            dotContent.Should().Contain("ProjectC\" -> \"ProjectD\" [color=\"red\", style=\"bold\"]");
            dotContent.Should().Contain("ProjectD\" -> \"ProjectC\" [color=\"red\", style=\"bold\"]");
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
    public async Task GenerateAsync_WithCycles_LegendIncludesCircularDependencies()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert
            dotContent.Should().Contain("Red: Circular Dependencies");
            dotContent.Should().Contain("subgraph cluster_dependency_legend");
            dotContent.Should().Contain("Dependency Types");
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
    public async Task GenerateAsync_NullCycles_NoRedEdges()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles: null);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - No cycle highlighting when cycles is null
            dotContent.Should().NotContain("color=\"red\"");
            dotContent.Should().NotContain("Circular Dependencies");
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
    public async Task GenerateAsync_EmptyCycles_NoRedEdges()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo>(); // Empty list
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert
            dotContent.Should().NotContain("color=\"red\"");
            dotContent.Should().NotContain("Circular Dependencies");
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
    public async Task GenerateAsync_CyclesAndCrossSolution_CyclesTakePriority()
    {
        // Arrange
        var graph = CreateGraphWithCyclicCrossSolutionEdge();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - Cycle wins over cross-solution (red not blue)
            dotContent.Should().Contain("ProjectA\" -> \"ProjectB\" [color=\"red\"");
            dotContent.Should().NotContain("ProjectA\" -> \"ProjectB\" [color=\"blue\"");
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
    public async Task GenerateAsync_CyclesWithNoMatchingEdges_NoRedEdges()
    {
        // Arrange - Create graph with edges A->B, but cycles containing C->D (no matching edges)
        var graph = new DependencyGraph();
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "/path/to/ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "/path/to/ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = "/path/to/ProjectC.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectD = new ProjectNode
        {
            ProjectName = "ProjectD",
            ProjectPath = "/path/to/ProjectD.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddEdge(new DependencyEdge
        {
            Source = projectA,
            Target = projectB,
            DependencyType = DependencyType.ProjectReference
        });

        // Cycles contain C and D, but graph only has edge A->B
        var cycles = new List<CycleInfo> { new CycleInfo(1, new[] { projectC, projectD }) };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - No red edges because cycle projects don't match graph edges
            dotContent.Should().NotContain("color=\"red\"");
            dotContent.Should().Contain("\"ProjectA\" -> \"ProjectB\" [color=\"black\"]");
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

    // ========== NEW TESTS FOR STORY 3.7: BREAK POINT HIGHLIGHTING ==========

    [Fact]
    public async Task GenerateAsync_WithRecommendations_BreakPointEdgesAreYellow()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion(
                cycleId: 1,
                sourceProject: graph.Vertices.ElementAt(0),
                targetProject: graph.Vertices.ElementAt(1),
                couplingScore: 3,
                cycleSize: 3,
                rationale: "Weakest link in cycle")
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert
            dotContent.Should().Contain("color=\"yellow\"");
            dotContent.Should().Contain("\"ProjectA\" -> \"ProjectB\" [color=\"yellow\", style=\"bold\"]");
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
    public async Task GenerateAsync_WithRecommendations_EdgesNotInRecommendationsUseDefaultColor()
    {
        // Arrange
        var graph = CreateGraphWithCycleAndNonCyclicEdge();
        var cycleProjects = new List<ProjectNode> { graph.Vertices.ElementAt(0), graph.Vertices.ElementAt(1) };
        var cycles = new List<CycleInfo> { new CycleInfo(1, cycleProjects) };
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion(
                cycleId: 1,
                sourceProject: graph.Vertices.ElementAt(0),
                targetProject: graph.Vertices.ElementAt(1),
                couplingScore: 3,
                cycleSize: 2,
                rationale: "Weakest link")
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - Non-recommended edges use their appropriate colors
            dotContent.Should().Contain("\"ProjectC\" -> \"ProjectD\" [color=\"black\"]");  // Non-cyclic edge
            dotContent.Should().Contain("\"ProjectB\" -> \"ProjectA\"");  // Cyclic but not recommended
            dotContent.Should().NotContain("\"ProjectB\" -> \"ProjectA\" [color=\"yellow\"");  // Should be RED not YELLOW
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
    public async Task GenerateAsync_WithRecommendationsAndCycles_YellowOverridesRed()
    {
        // CRITICAL TEST: Validates color priority - YELLOW > RED
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion(
                cycleId: 1,
                sourceProject: graph.Vertices.ElementAt(0),  // ProjectA
                targetProject: graph.Vertices.ElementAt(1),  // ProjectB
                couplingScore: 3,
                cycleSize: 3,
                rationale: "Weakest link in cycle")
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - YELLOW wins over RED for recommended edge
            dotContent.Should().Contain("\"ProjectA\" -> \"ProjectB\" [color=\"yellow\", style=\"bold\"]");
            dotContent.Should().NotContain("\"ProjectA\" -> \"ProjectB\" [color=\"red\"");

            // Other cyclic edges (not recommended) should still be RED
            dotContent.Should().Contain("\"ProjectB\" -> \"ProjectC\" [color=\"red\", style=\"bold\"]");
            dotContent.Should().Contain("\"ProjectC\" -> \"ProjectA\" [color=\"red\", style=\"bold\"]");
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
    public async Task GenerateAsync_WithRecommendations_LegendIncludesSuggestedBreakPoints()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion(
                cycleId: 1,
                sourceProject: graph.Vertices.ElementAt(0),
                targetProject: graph.Vertices.ElementAt(1),
                couplingScore: 3,
                cycleSize: 3,
                rationale: "Weakest link")
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert
            dotContent.Should().Contain("Yellow: Suggested Break Points");
            dotContent.Should().Contain("subgraph cluster_dependency_legend");
            dotContent.Should().Contain("color=\"yellow\"");
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
    public async Task GenerateAsync_NullRecommendations_NoYellowEdges()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act - Story 3.6 usage pattern (cycles but no recommendations)
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations: null);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - No YELLOW highlighting when recommendations is null
            dotContent.Should().NotContain("color=\"yellow\"");
            dotContent.Should().NotContain("Suggested Break Points");
            // But RED cycle highlighting should still work
            dotContent.Should().Contain("color=\"red\"");
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
    public async Task GenerateAsync_EmptyRecommendations_NoYellowEdges()
    {
        // Arrange
        var graph = CreateGraphWithCycle();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var recommendations = new List<CycleBreakingSuggestion>(); // Empty list
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert
            dotContent.Should().NotContain("color=\"yellow\"");
            dotContent.Should().NotContain("Suggested Break Points");
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
    public async Task GenerateAsync_MoreThan10Recommendations_OnlyTop10AreYellow()
    {
        // CRITICAL TEST: Validates top 10 limit to avoid visual clutter
        // Arrange
        var graph = CreateGraphWithManyEdges();
        var cycles = new List<CycleInfo> { new CycleInfo(1, graph.Vertices.ToList()) };
        var recommendations = new List<CycleBreakingSuggestion>();

        // Create 15 recommendations with varying coupling scores
        for (int i = 0; i < 15 && i < graph.Vertices.Count() - 1; i++)
        {
            recommendations.Add(new CycleBreakingSuggestion(
                cycleId: 1,
                sourceProject: graph.Vertices.ElementAt(i),
                targetProject: graph.Vertices.ElementAt(i + 1),
                couplingScore: i + 1,  // Vary coupling scores
                cycleSize: 15,
                rationale: $"Weak link {i}"));
        }

        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles, recommendations);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - Count YELLOW edges (should be exactly 10, not 15)
            // Count only edge lines (with "->") that have color="yellow", not legend entries
            var yellowEdgeMatches = System.Text.RegularExpressions.Regex.Matches(dotContent, @"->\s+\S+\s+\[color=""yellow""");
            yellowEdgeMatches.Count.Should().Be(10, "Only top 10 recommendations should be marked in yellow");

            // Legend should indicate "Top"
            dotContent.Should().ContainAny("Top 10", "top 10");
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
    public async Task GenerateAsync_RecommendationsWithNoCycles_YellowOnlyBreakPoints()
    {
        // Arrange - Graph with recommendations but no cycles parameter
        var graph = CreateGraphWithCycle();
        var recommendations = new List<CycleBreakingSuggestion>
        {
            new CycleBreakingSuggestion(
                cycleId: 1,
                sourceProject: graph.Vertices.ElementAt(0),
                targetProject: graph.Vertices.ElementAt(1),
                couplingScore: 3,
                cycleSize: 3,
                rationale: "Weakest link")
        };
        var outputDir = Path.Combine(Path.GetTempPath(), "dot-test-" + Guid.NewGuid());
        var solutionName = "TestSolution";

        try
        {
            // Act - Recommendations without cycles
            var dotPath = await _generator.GenerateAsync(graph, outputDir, solutionName, cycles: null, recommendations);
            var dotContent = await File.ReadAllTextAsync(dotPath);

            // Assert - YELLOW edges present, no RED edges
            dotContent.Should().Contain("color=\"yellow\"");
            dotContent.Should().NotContain("color=\"red\"");
            dotContent.Should().Contain("Yellow: Suggested Break Points");
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

    // ========== HELPER METHODS ==========

    private DependencyGraph CreateGraphWithManyEdges()
    {
        // Create graph with 15+ nodes for testing top 10 limit
        var graph = new DependencyGraph();

        for (int i = 0; i < 15; i++)
        {
            var project = new ProjectNode
            {
                ProjectName = $"Project{i}",
                ProjectPath = $"/path/to/Project{i}.csproj",
                TargetFramework = "net8.0",
                SolutionName = "Solution1"
            };
            graph.AddVertex(project);
        }

        // Create a chain of edges
        for (int i = 0; i < 14; i++)
        {
            graph.AddEdge(new DependencyEdge
            {
                Source = graph.Vertices.ElementAt(i),
                Target = graph.Vertices.ElementAt(i + 1),
                DependencyType = DependencyType.ProjectReference
            });
        }

        // Close the cycle
        graph.AddEdge(new DependencyEdge
        {
            Source = graph.Vertices.ElementAt(14),
            Target = graph.Vertices.ElementAt(0),
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    // ========== HELPER METHODS ==========

    private DependencyGraph CreateGraphWithCycle()
    {
        // ProjectA -> ProjectB -> ProjectC -> ProjectA (3-node cycle)
        var graph = new DependencyGraph();
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "/path/to/ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "/path/to/ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = "/path/to/ProjectC.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);

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
            Target = projectA,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private DependencyGraph CreateGraphWithCycleAndNonCyclicEdge()
    {
        // A->B->A (cycle), C->D (not cyclic)
        var graph = new DependencyGraph();
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "/path/to/ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "/path/to/ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = "/path/to/ProjectC.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectD = new ProjectNode
        {
            ProjectName = "ProjectD",
            ProjectPath = "/path/to/ProjectD.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);

        graph.AddEdge(new DependencyEdge
        {
            Source = projectA,
            Target = projectB,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = projectB,
            Target = projectA,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = projectC,
            Target = projectD,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private DependencyGraph CreateGraphWithMultipleCycles()
    {
        // Cycle 1: A->B->A, Cycle 2: C->D->C
        var graph = new DependencyGraph();
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "/path/to/ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "/path/to/ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = "/path/to/ProjectC.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectD = new ProjectNode
        {
            ProjectName = "ProjectD",
            ProjectPath = "/path/to/ProjectD.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);

        graph.AddEdge(new DependencyEdge
        {
            Source = projectA,
            Target = projectB,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = projectB,
            Target = projectA,
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
            Source = projectD,
            Target = projectC,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private DependencyGraph CreateGraphWithCyclicCrossSolutionEdge()
    {
        // A->B (both in cycle AND cross-solution)
        var graph = new DependencyGraph();
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = "/path/to/ProjectA.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution1"
        };
        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = "/path/to/ProjectB.csproj",
            TargetFramework = "net8.0",
            SolutionName = "Solution2" // Different solution
        };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);

        graph.AddEdge(new DependencyEdge
        {
            Source = projectA,
            Target = projectB,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = projectB,
            Target = projectA,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
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
