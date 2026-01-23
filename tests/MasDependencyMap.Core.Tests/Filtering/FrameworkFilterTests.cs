namespace MasDependencyMap.Core.Tests.Filtering;

using FluentAssertions;
using MasDependencyMap.Core.Configuration;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.Filtering;
using MasDependencyMap.Core.SolutionLoading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class FrameworkFilterTests
{
    private readonly ILogger<FrameworkFilter> _logger;

    public FrameworkFilterTests()
    {
        _logger = NullLogger<FrameworkFilter>.Instance;
    }

    [Fact]
    public async Task FilterAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => filter.FilterAsync(null!));
    }

    [Fact]
    public async Task FilterAsync_EmptyGraph_ReturnsEmptyGraph()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var emptyGraph = new DependencyGraph();

        // Act
        var result = await filter.FilterAsync(emptyGraph);

        // Assert
        result.VertexCount.Should().Be(0);
        result.EdgeCount.Should().Be(0);
    }

    [Fact]
    public async Task FilterAsync_SystemCoreReference_RemovesEdge()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithSystemReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(0, "System.Core edge should be removed");
        filtered.VertexCount.Should().Be(2, "Vertices should be retained");
        filtered.Vertices.Should().Contain(v => v.ProjectName == "Project1");
        filtered.Vertices.Should().Contain(v => v.ProjectName == "System.Core");
    }

    [Fact]
    public async Task FilterAsync_MicrosoftBuildReference_RemovesEdge()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithMicrosoftReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(0, "Microsoft.Build edge should be removed");
        filtered.VertexCount.Should().Be(2, "Vertices should be retained");
    }

    [Fact]
    public async Task FilterAsync_MscorlibReference_RemovesEdge()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithMscorlibReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(0, "mscorlib edge should be removed");
        filtered.VertexCount.Should().Be(2, "Vertices should be retained");
    }

    [Fact]
    public async Task FilterAsync_NetstandardReference_RemovesEdge()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithNetstandardReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(0, "netstandard edge should be removed");
        filtered.VertexCount.Should().Be(2, "Vertices should be retained");
    }

    [Fact]
    public async Task FilterAsync_CustomProjectReference_RetainsEdge()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithCustomReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(1, "Custom edge should be retained");
        filtered.VertexCount.Should().Be(2, "All vertices should be retained");
        var edge = filtered.Edges.Single();
        edge.Source.ProjectName.Should().Be("Project1");
        edge.Target.ProjectName.Should().Be("CustomLibrary");
    }

    [Fact]
    public async Task FilterAsync_AllowListPattern_OverridesBlockList()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            BlockList = new List<string> { "Microsoft.*" },
            AllowList = new List<string> { "YourCompany.*" }
        };
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithYourCompanyMicrosoftReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(1, "YourCompany.Microsoft.* should be allowed despite Microsoft.* blocklist");
        var edge = filtered.Edges.Single();
        edge.Target.ProjectName.Should().Be("YourCompany.Microsoft.Utils");
    }

    [Fact]
    public async Task FilterAsync_EmptyBlockList_NoFilteringApplied()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            BlockList = new List<string>(),
            AllowList = new List<string>()
        };
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithSystemReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(1, "No filtering should occur with empty blocklist");
    }

    [Fact]
    public async Task FilterAsync_MixedReferences_FiltersOnlyFramework()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithMixedReferences();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(2, "Only custom edges should remain");
        filtered.Edges.Should().AllSatisfy(e =>
        {
            e.Target.ProjectName.Should().NotStartWith("System.");
            e.Target.ProjectName.Should().NotStartWith("Microsoft.");
        });
    }

    [Fact]
    public async Task FilterAsync_CaseInsensitiveMatching_RemovesEdge()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            BlockList = new List<string> { "microsoft.*" }, // lowercase pattern
            AllowList = new List<string>()
        };
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithMicrosoftReference(); // Microsoft.Build (PascalCase)

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(0, "Case-insensitive matching should remove Microsoft.Build");
    }

    [Fact]
    public async Task FilterAsync_BinaryReferenceEdge_RemovesEdge()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithBinaryReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(0, "BinaryReference edges should also be filtered");
    }

    [Fact]
    public async Task FilterAsync_ProjectReferenceEdge_RemovesEdge()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);
        var graph = CreateGraphWithProjectReference();

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(0, "ProjectReference edges should also be filtered");
    }

    [Fact]
    public async Task FilterAsync_WildcardPattern_MatchesCorrectly()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            BlockList = new List<string> { "System.*" },
            AllowList = new List<string>()
        };
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);

        // Create graph with various System.* references
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var systemCore = CreateProjectNode("System.Core");
        var systemLinq = CreateProjectNode("System.Linq");
        var systemXml = CreateProjectNode("System.Xml.Linq");
        var systemUtilities = CreateProjectNode("SystemUtilities"); // Should NOT match

        graph.AddVertex(project1);
        graph.AddVertex(systemCore);
        graph.AddVertex(systemLinq);
        graph.AddVertex(systemXml);
        graph.AddVertex(systemUtilities);

        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = systemCore,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = systemLinq,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = systemXml,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = systemUtilities,
            DependencyType = DependencyType.ProjectReference
        });

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(1, "Only SystemUtilities edge should remain");
        filtered.Edges.Single().Target.ProjectName.Should().Be("SystemUtilities");
    }

    [Fact]
    public async Task FilterAsync_ExactMatchPattern_MatchesOnlyExact()
    {
        // Arrange
        var config = new FilterConfiguration
        {
            BlockList = new List<string> { "mscorlib" }, // Exact match, no wildcard
            AllowList = new List<string>()
        };
        var options = Options.Create(config);
        var filter = new FrameworkFilter(options, _logger);

        // Create graph with mscorlib and mscorlib.Extensions
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var mscorlib = CreateProjectNode("mscorlib");
        var mscorlibExtensions = CreateProjectNode("mscorlib.Extensions");

        graph.AddVertex(project1);
        graph.AddVertex(mscorlib);
        graph.AddVertex(mscorlibExtensions);

        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = mscorlib,
            DependencyType = DependencyType.BinaryReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = mscorlibExtensions,
            DependencyType = DependencyType.BinaryReference
        });

        // Act
        var filtered = await filter.FilterAsync(graph);

        // Assert
        filtered.EdgeCount.Should().Be(1, "Only mscorlib should be blocked, not mscorlib.Extensions");
        filtered.Edges.Single().Target.ProjectName.Should().Be("mscorlib.Extensions");
    }

    // Helper methods to create test graphs

    private static FilterConfiguration CreateDefaultFilterConfiguration()
    {
        return new FilterConfiguration
        {
            BlockList = new List<string> { "Microsoft.*", "System.*", "mscorlib", "netstandard" },
            AllowList = new List<string>()
        };
    }

    private static ProjectNode CreateProjectNode(string name)
    {
        return new ProjectNode
        {
            ProjectName = name,
            ProjectPath = $@"C:\Projects\{name}\{name}.csproj",
            TargetFramework = "net8.0",
            SolutionName = "TestSolution"
        };
    }

    private static DependencyGraph CreateGraphWithSystemReference()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var systemCore = CreateProjectNode("System.Core");

        graph.AddVertex(project1);
        graph.AddVertex(systemCore);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = systemCore,
            DependencyType = DependencyType.BinaryReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    private static DependencyGraph CreateGraphWithMicrosoftReference()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var microsoftBuild = CreateProjectNode("Microsoft.Build");

        graph.AddVertex(project1);
        graph.AddVertex(microsoftBuild);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = microsoftBuild,
            DependencyType = DependencyType.BinaryReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    private static DependencyGraph CreateGraphWithMscorlibReference()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var mscorlib = CreateProjectNode("mscorlib");

        graph.AddVertex(project1);
        graph.AddVertex(mscorlib);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = mscorlib,
            DependencyType = DependencyType.BinaryReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    private static DependencyGraph CreateGraphWithNetstandardReference()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var netstandard = CreateProjectNode("netstandard");

        graph.AddVertex(project1);
        graph.AddVertex(netstandard);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = netstandard,
            DependencyType = DependencyType.BinaryReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    private static DependencyGraph CreateGraphWithCustomReference()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var customLibrary = CreateProjectNode("CustomLibrary");

        graph.AddVertex(project1);
        graph.AddVertex(customLibrary);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = customLibrary,
            DependencyType = DependencyType.ProjectReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    private static DependencyGraph CreateGraphWithYourCompanyMicrosoftReference()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var yourCompanyMicrosoft = CreateProjectNode("YourCompany.Microsoft.Utils");

        graph.AddVertex(project1);
        graph.AddVertex(yourCompanyMicrosoft);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = yourCompanyMicrosoft,
            DependencyType = DependencyType.ProjectReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    private static DependencyGraph CreateGraphWithMixedReferences()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var systemCore = CreateProjectNode("System.Core");
        var microsoftBuild = CreateProjectNode("Microsoft.Build");
        var customLib1 = CreateProjectNode("CustomLibrary1");
        var customLib2 = CreateProjectNode("CustomLibrary2");

        graph.AddVertex(project1);
        graph.AddVertex(systemCore);
        graph.AddVertex(microsoftBuild);
        graph.AddVertex(customLib1);
        graph.AddVertex(customLib2);

        // Framework edges (should be filtered)
        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = systemCore,
            DependencyType = DependencyType.BinaryReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = microsoftBuild,
            DependencyType = DependencyType.BinaryReference
        });

        // Custom edges (should be retained)
        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = customLib1,
            DependencyType = DependencyType.ProjectReference
        });
        graph.AddEdge(new DependencyEdge
        {
            Source = project1,
            Target = customLib2,
            DependencyType = DependencyType.ProjectReference
        });

        return graph;
    }

    private static DependencyGraph CreateGraphWithBinaryReference()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var systemCore = CreateProjectNode("System.Core");

        graph.AddVertex(project1);
        graph.AddVertex(systemCore);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = systemCore,
            DependencyType = DependencyType.BinaryReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    private static DependencyGraph CreateGraphWithProjectReference()
    {
        var graph = new DependencyGraph();
        var project1 = CreateProjectNode("Project1");
        var systemCore = CreateProjectNode("System.Core");

        graph.AddVertex(project1);
        graph.AddVertex(systemCore);

        var edge = new DependencyEdge
        {
            Source = project1,
            Target = systemCore,
            DependencyType = DependencyType.ProjectReference
        };
        graph.AddEdge(edge);

        return graph;
    }

    // Integration test with real solution

    [Fact]
    public async Task FilterAsync_SampleMonolithSolution_FiltersFrameworkDependencies()
    {
        // Arrange
        var config = CreateDefaultFilterConfiguration();
        var options = Options.Create(config);
        var logger = new TestLogger<FrameworkFilter>();
        var filter = new FrameworkFilter(options, logger);

        // Load SampleMonolith solution
        var solutionPath = Path.GetFullPath("samples/SampleMonolith/SampleMonolith.sln");

        // Skip test if SampleMonolith doesn't exist (CI environment)
        if (!File.Exists(solutionPath))
        {
            // Test is skipped in environments without SampleMonolith
            return;
        }

        var fallbackLoader = new FallbackSolutionLoader(
            new RoslynSolutionLoader(NullLogger<RoslynSolutionLoader>.Instance),
            new MSBuildSolutionLoader(NullLogger<MSBuildSolutionLoader>.Instance),
            new ProjectFileSolutionLoader(NullLogger<ProjectFileSolutionLoader>.Instance),
            NullLogger<FallbackSolutionLoader>.Instance);

        var solution = await fallbackLoader.LoadAsync(solutionPath, CancellationToken.None);
        var graphBuilder = new DependencyGraphBuilder(NullLogger<DependencyGraphBuilder>.Instance);
        var originalGraph = await graphBuilder.BuildAsync(solution);

        // Act
        var filteredGraph = await filter.FilterAsync(originalGraph);

        // Assert
        filteredGraph.VertexCount.Should().Be(originalGraph.VertexCount, "All vertices should be retained");

        // SampleMonolith only contains ProjectReferences between custom projects (no framework refs in graph)
        // So edge count should be the same or less (if there were any framework refs, they'd be filtered)
        filteredGraph.EdgeCount.Should().BeLessThanOrEqualTo(originalGraph.EdgeCount, "Edge count should not increase");

        // Verify no System.* or Microsoft.* edges remain in the filtered graph
        foreach (var edge in filteredGraph.Edges)
        {
            edge.Target.ProjectName.Should().NotStartWith("System.", "System.* dependencies should be filtered");
            edge.Target.ProjectName.Should().NotStartWith("Microsoft.", "Microsoft.* dependencies should be filtered");
            edge.Target.ProjectName.Should().NotBe("mscorlib", "mscorlib should be filtered");
            edge.Target.ProjectName.Should().NotBe("netstandard", "netstandard should be filtered");
        }

        // Verify all edges are custom project references
        filteredGraph.Edges.Should().OnlyContain(e =>
            !e.Target.ProjectName.StartsWith("System.") &&
            !e.Target.ProjectName.StartsWith("Microsoft.") &&
            e.Target.ProjectName != "mscorlib" &&
            e.Target.ProjectName != "netstandard",
            "Only custom project edges should remain");

        // Verify logging occurred
        logger.LoggedMessages.Should().ContainSingle(msg => msg.Contains("Filtered") && msg.Contains("framework refs"));
    }

    // Test logger to capture log messages
    private class TestLogger<T> : ILogger<T>
    {
        public List<string> LoggedMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add(formatter(state, exception));
        }
    }
}
