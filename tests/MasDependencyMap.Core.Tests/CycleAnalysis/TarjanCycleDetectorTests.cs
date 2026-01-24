namespace MasDependencyMap.Core.Tests.CycleAnalysis;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MasDependencyMap.Core.CycleAnalysis;
using MasDependencyMap.Core.DependencyAnalysis;

public class TarjanCycleDetectorTests
{
    private readonly ILogger<TarjanCycleDetector> _logger;
    private readonly ILogger<CycleStatisticsCalculator> _statsLogger;
    private readonly ICycleStatisticsCalculator _statisticsCalculator;
    private readonly TarjanCycleDetector _detector;

    public TarjanCycleDetectorTests()
    {
        _logger = NullLogger<TarjanCycleDetector>.Instance;
        _statsLogger = NullLogger<CycleStatisticsCalculator>.Instance;
        _statisticsCalculator = new CycleStatisticsCalculator(_statsLogger);
        _detector = new TarjanCycleDetector(_logger, _statisticsCalculator);
    }

    [Fact]
    public async Task DetectCyclesAsync_GraphWithSimpleCycle_ReturnsOneCycle()
    {
        // Arrange
        var graph = CreateGraphWithSimpleCycle(); // A -> B -> C -> A

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().HaveCount(1);
        cycles[0].CycleSize.Should().Be(3);
        cycles[0].Projects.Should().Contain(p => p.ProjectName == "ProjectA");
        cycles[0].Projects.Should().Contain(p => p.ProjectName == "ProjectB");
        cycles[0].Projects.Should().Contain(p => p.ProjectName == "ProjectC");
        cycles[0].CycleId.Should().Be(1);
    }

    [Fact]
    public async Task DetectCyclesAsync_AcyclicGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = CreateAcyclicGraph(); // A -> B -> C (tree)

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCyclesAsync_EmptyGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = new DependencyGraph();

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCyclesAsync_SingleProjectGraph_ReturnsEmptyList()
    {
        // Arrange
        var graph = CreateSingleProjectGraph();

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCyclesAsync_MultipleCycles_ReturnsAllCycles()
    {
        // Arrange
        var graph = CreateGraphWithTwoCycles(); // A -> B -> A, C -> D -> E -> C

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().HaveCount(2);
        var cycle1 = cycles.FirstOrDefault(c => c.CycleSize == 2);
        var cycle2 = cycles.FirstOrDefault(c => c.CycleSize == 3);

        cycle1.Should().NotBeNull();
        cycle2.Should().NotBeNull();

        cycle1!.Projects.Should().Contain(p => p.ProjectName == "ProjectA");
        cycle1.Projects.Should().Contain(p => p.ProjectName == "ProjectB");

        cycle2!.Projects.Should().Contain(p => p.ProjectName == "ProjectC");
        cycle2.Projects.Should().Contain(p => p.ProjectName == "ProjectD");
        cycle2.Projects.Should().Contain(p => p.ProjectName == "ProjectE");
    }

    [Fact]
    public async Task DetectCyclesAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _detector.DetectCyclesAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("graph");
    }

    [Fact]
    public async Task DetectCyclesAsync_LargestCycleCalculation_CorrectStatistics()
    {
        // Arrange
        var graph = CreateGraphWith3ProjectCycleAnd5ProjectCycle();

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert
        cycles.Should().HaveCount(2);
        var largestCycle = cycles.Max(c => c.CycleSize);
        largestCycle.Should().Be(5);

        var smallestCycle = cycles.Min(c => c.CycleSize);
        smallestCycle.Should().Be(3);
    }

    [Fact]
    public async Task DetectCyclesAsync_SelfReferencingProject_ReturnsEmptyList()
    {
        // Arrange - A project that references itself should be treated as single-node SCC
        var graph = CreateSelfReferencingGraph();

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert - Self-referencing is a single-node SCC, not a cycle
        cycles.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCyclesAsync_ComplexCyclicGraph_FormsOneSCC()
    {
        // Arrange - Projects with multiple cycle paths all form ONE strongly connected component
        // This tests the understanding that Tarjan's SCC treats all mutually reachable nodes as one SCC
        var graph = CreateGraphWithComplexCycles();

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert - All 4 projects are mutually reachable, so they form 1 SCC
        cycles.Should().HaveCount(1);
        cycles[0].CycleSize.Should().Be(4);

        // Verify all projects are in the single SCC
        cycles[0].Projects.Should().Contain(p => p.ProjectName == "ProjectA");
        cycles[0].Projects.Should().Contain(p => p.ProjectName == "ProjectB");
        cycles[0].Projects.Should().Contain(p => p.ProjectName == "ProjectC");
        cycles[0].Projects.Should().Contain(p => p.ProjectName == "ProjectD");
    }

    [Fact]
    public async Task DetectCyclesAsync_MultipleSeparateCycles_CountsAllProjects()
    {
        // Arrange - Two completely separate cycles
        var graph = CreateGraphWithTwoCycles();

        // Act
        var cycles = await _detector.DetectCyclesAsync(graph);

        // Assert - Should find 2 separate cycles
        cycles.Should().HaveCount(2);

        // Count total projects across all cycles (no overlap in separate SCCs)
        var allProjectsInCycles = cycles
            .SelectMany(c => c.Projects)
            .Distinct()
            .ToList();

        // Total: 5 unique projects (A, B from first cycle; C, D, E from second cycle)
        allProjectsInCycles.Should().HaveCount(5);
    }

    [Fact]
    public async Task DetectCyclesAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var graph = CreateGraphWithSimpleCycle();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await _detector.DetectCyclesAsync(graph, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // Helper methods to create test graphs
    private DependencyGraph CreateGraphWithSimpleCycle()
    {
        var graph = new DependencyGraph();

        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = @"C:\Projects\A\A.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = @"C:\Projects\B\B.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = @"C:\Projects\C\C.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
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
        }); // Cycle

        return graph;
    }

    private DependencyGraph CreateAcyclicGraph()
    {
        var graph = new DependencyGraph();

        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = @"C:\Projects\A\A.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = @"C:\Projects\B\B.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = @"C:\Projects\C\C.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
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

        return graph;
    }

    private DependencyGraph CreateSingleProjectGraph()
    {
        var graph = new DependencyGraph();

        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = @"C:\Projects\A\A.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(projectA);
        return graph;
    }

    private DependencyGraph CreateGraphWithTwoCycles()
    {
        var graph = new DependencyGraph();

        // First cycle: A -> B -> A
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = @"C:\Projects\A\A.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = @"C:\Projects\B\B.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
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

        // Second cycle: C -> D -> E -> C
        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = @"C:\Projects\C\C.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectD = new ProjectNode
        {
            ProjectName = "ProjectD",
            ProjectPath = @"C:\Projects\D\D.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectE = new ProjectNode
        {
            ProjectName = "ProjectE",
            ProjectPath = @"C:\Projects\E\E.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(projectC);
        graph.AddVertex(projectD);
        graph.AddVertex(projectE);

        graph.AddEdge(new DependencyEdge
        {
            Source = projectC,
            Target = projectD,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectD,
            Target = projectE,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectE,
            Target = projectC,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private DependencyGraph CreateGraphWith3ProjectCycleAnd5ProjectCycle()
    {
        var graph = new DependencyGraph();

        // First cycle: 3 projects (A -> B -> C -> A)
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = @"C:\Projects\A\A.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = @"C:\Projects\B\B.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = @"C:\Projects\C\C.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
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

        // Second cycle: 5 projects (D -> E -> F -> G -> H -> D)
        var projectD = new ProjectNode
        {
            ProjectName = "ProjectD",
            ProjectPath = @"C:\Projects\D\D.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectE = new ProjectNode
        {
            ProjectName = "ProjectE",
            ProjectPath = @"C:\Projects\E\E.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectF = new ProjectNode
        {
            ProjectName = "ProjectF",
            ProjectPath = @"C:\Projects\F\F.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectG = new ProjectNode
        {
            ProjectName = "ProjectG",
            ProjectPath = @"C:\Projects\G\G.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectH = new ProjectNode
        {
            ProjectName = "ProjectH",
            ProjectPath = @"C:\Projects\H\H.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(projectD);
        graph.AddVertex(projectE);
        graph.AddVertex(projectF);
        graph.AddVertex(projectG);
        graph.AddVertex(projectH);

        graph.AddEdge(new DependencyEdge
        {
            Source = projectD,
            Target = projectE,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectE,
            Target = projectF,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectF,
            Target = projectG,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectG,
            Target = projectH,
            DependencyType = DependencyType.ProjectReference
        });

        graph.AddEdge(new DependencyEdge
        {
            Source = projectH,
            Target = projectD,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private DependencyGraph CreateSelfReferencingGraph()
    {
        var graph = new DependencyGraph();

        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = @"C:\Projects\A\A.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(projectA);

        graph.AddEdge(new DependencyEdge
        {
            Source = projectA,
            Target = projectA,
            DependencyType = DependencyType.ProjectReference
        }); // Self-reference

        return graph;
    }

    private DependencyGraph CreateGraphWithComplexCycles()
    {
        var graph = new DependencyGraph();

        // Projects with multiple cycle paths that form one large SCC
        var projectA = new ProjectNode
        {
            ProjectName = "ProjectA",
            ProjectPath = @"C:\Projects\A\A.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectB = new ProjectNode
        {
            ProjectName = "ProjectB",
            ProjectPath = @"C:\Projects\B\B.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectC = new ProjectNode
        {
            ProjectName = "ProjectC",
            ProjectPath = @"C:\Projects\C\C.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        var projectD = new ProjectNode
        {
            ProjectName = "ProjectD",
            ProjectPath = @"C:\Projects\D\D.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };

        graph.AddVertex(projectA);
        graph.AddVertex(projectB);
        graph.AddVertex(projectC);
        graph.AddVertex(projectD);

        // Create multiple cycle paths: A -> B -> A
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

        // And A -> C -> D -> A (all mutually reachable = 1 SCC)
        graph.AddEdge(new DependencyEdge
        {
            Source = projectA,
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
            Source = projectD,
            Target = projectA,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }
}
