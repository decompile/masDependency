namespace MasDependencyMap.Core.Tests.Reporting;

using System.Text;
using FluentAssertions;
using MasDependencyMap.Core.Configuration;
using MasDependencyMap.Core.DependencyAnalysis;
using MasDependencyMap.Core.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class TextReportGeneratorTests : IDisposable
{
    private readonly ILogger<TextReportGenerator> _logger;
    private readonly TextReportGenerator _generator;
    private readonly List<string> _tempDirectories;

    public TextReportGeneratorTests()
    {
        _logger = NullLogger<TextReportGenerator>.Instance;

        // Create default filter configuration for testing
        var filterConfig = new FilterConfiguration
        {
            BlockList = new List<string>
            {
                "Microsoft.*",
                "System.*",
                "mscorlib",
                "netstandard",
                "Windows.*",
                "NETCore.*",
                "FSharp.*",
                "VisualBasic.*"
            },
            AllowList = new List<string>()
        };
        var filterOptions = Options.Create(filterConfig);

        _generator = new TextReportGenerator(_logger, filterOptions);
        _tempDirectories = new List<string>();
    }

    public void Dispose()
    {
        // Cleanup temp directories
        foreach (var dir in _tempDirectories)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_NullGraph_ThrowsArgumentNullException()
    {
        // Arrange
        var outputDir = CreateTempDirectory();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _generator.GenerateAsync(null!, outputDir, "TestSolution"));
    }

    [Fact]
    public async Task GenerateAsync_EmptyOutputDirectory_ThrowsArgumentException()
    {
        // Arrange
        var graph = CreateTestGraph();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _generator.GenerateAsync(graph, "", "TestSolution"));
    }

    [Fact]
    public async Task GenerateAsync_EmptySolutionName_ThrowsArgumentException()
    {
        // Arrange
        var graph = CreateTestGraph();
        var outputDir = CreateTempDirectory();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _generator.GenerateAsync(graph, outputDir, ""));
    }

    [Fact]
    public async Task GenerateAsync_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var graph = CreateTestGraph();
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _generator.GenerateAsync(graph, nonExistentDir, "TestSolution"));
    }

    [Fact]
    public async Task GenerateAsync_ValidGraph_CreatesReportFile()
    {
        // Arrange
        var graph = CreateTestGraph(projectCount: 10, edgeCount: 20);
        var outputDir = CreateTempDirectory();
        var solutionName = "TestSolution";

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, solutionName);

        // Assert
        File.Exists(reportPath).Should().BeTrue();
        reportPath.Should().EndWith("TestSolution-analysis-report.txt");
    }

    [Fact]
    public async Task GenerateAsync_ValidGraph_ContainsHeaderWithSolutionName()
    {
        // Arrange
        var graph = CreateTestGraph(projectCount: 73);
        var outputDir = CreateTempDirectory();
        var solutionName = "MyLegacySolution";

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, solutionName);

        // Assert
        var content = await File.ReadAllTextAsync(reportPath);
        content.Should().Contain("Solution: MyLegacySolution");
        content.Should().Contain("Total Projects: 73");
        content.Should().MatchRegex(@"Analysis Date: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} UTC");
        content.Should().Contain("MasDependencyMap Analysis Report");
    }

    [Fact]
    public async Task GenerateAsync_GraphWithFrameworkRefs_ShowsCorrectStatistics()
    {
        // Arrange
        var graph = CreateTestGraphWithFrameworkRefs(
            customProjects: 10,
            frameworkProjects: 5,
            customEdges: 20,
            frameworkEdges: 30);
        var outputDir = CreateTempDirectory();

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, "TestSolution");

        // Assert
        var content = await File.ReadAllTextAsync(reportPath);
        content.Should().Contain("Total References: 50");  // 20 + 30
        content.Should().Contain("Framework References: 30");
        content.Should().Contain("Custom References: 20");
        content.Should().MatchRegex(@"Framework References: 30 \(60%\)");  // 30/50 = 60%
    }

    [Fact]
    public async Task GenerateAsync_MultiSolutionGraph_ShowsCrossSolutionReferences()
    {
        // Arrange
        var graph = CreateMultiSolutionGraph(
            solution1Projects: 5,
            solution2Projects: 5,
            crossSolutionEdges: 3);
        var outputDir = CreateTempDirectory();

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, "MultiSolution");

        // Assert
        var content = await File.ReadAllTextAsync(reportPath);
        content.Should().Contain("Cross-Solution References: 3");
    }

    [Fact]
    public async Task GenerateAsync_EmptyGraph_HandlesGracefully()
    {
        // Arrange
        var graph = new DependencyGraph();
        var outputDir = CreateTempDirectory();

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, "EmptySolution");

        // Assert
        var content = await File.ReadAllTextAsync(reportPath);
        content.Should().Contain("Total Projects: 0");
        content.Should().Contain("Total References: 0");
    }

    [Fact]
    public async Task GenerateAsync_LargeGraph_CompletesWithin10Seconds()
    {
        // Arrange
        var graph = CreateTestGraph(projectCount: 400, edgeCount: 5000);  // Simulate large solution
        var outputDir = CreateTempDirectory();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var reportPath = await _generator.GenerateAsync(graph, outputDir, "LargeSolution");
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
        File.Exists(reportPath).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_ReportHasCorrectStructure()
    {
        // Arrange
        var graph = CreateTestGraph(projectCount: 10);
        var outputDir = CreateTempDirectory();

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, "TestSolution");

        // Assert
        var content = await File.ReadAllTextAsync(reportPath);

        // Verify header section
        content.Should().Contain("MasDependencyMap Analysis Report");
        content.Should().Contain("Solution: TestSolution");

        // Verify dependency overview section
        content.Should().Contain("DEPENDENCY OVERVIEW");
        content.Should().Contain("Total References:");
        content.Should().Contain("Framework References:");
        content.Should().Contain("Custom References:");

        // Verify separators exist
        content.Should().Contain(new string('=', 80));
    }

    [Fact]
    public async Task GenerateAsync_ReturnsAbsolutePath()
    {
        // Arrange
        var graph = CreateTestGraph();
        var outputDir = CreateTempDirectory();

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, "TestSolution");

        // Assert
        Path.IsPathRooted(reportPath).Should().BeTrue();
        reportPath.Should().StartWith(outputDir);
    }

    [Fact]
    public async Task GenerateAsync_GeneratedFile_UsesUTF8Encoding()
    {
        // Arrange
        var graph = CreateTestGraph();
        var outputDir = CreateTempDirectory();

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, "TestSolution");

        // Assert
        var bytes = await File.ReadAllBytesAsync(reportPath);

        // UTF-8 without BOM should not start with BOM bytes (EF BB BF)
        // Plain text files typically don't use BOM
        if (bytes.Length >= 3)
        {
            var hasBOM = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            hasBOM.Should().BeFalse("plain text reports should use UTF-8 without BOM");
        }

        // Verify content can be read as UTF-8
        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("MasDependencyMap Analysis Report");
    }

    [Fact]
    public async Task GenerateAsync_CancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var graph = CreateTestGraph(projectCount: 100, edgeCount: 500); // Larger graph for timing
        var outputDir = CreateTempDirectory();
        var cts = new CancellationTokenSource();

        // Cancel immediately to ensure cancellation happens during file write
        cts.Cancel();

        // Act & Assert
        // TaskCanceledException inherits from OperationCanceledException, both are valid
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _generator.GenerateAsync(graph, outputDir, "TestSolution", cancellationToken: cts.Token));

        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAsync_InvalidFileNameCharacters_SanitizesFileName()
    {
        // Arrange
        var graph = CreateTestGraph();
        var outputDir = CreateTempDirectory();
        var invalidSolutionName = "My<Solution>:Test?File*";

        // Act
        var reportPath = await _generator.GenerateAsync(graph, outputDir, invalidSolutionName);

        // Assert
        File.Exists(reportPath).Should().BeTrue();
        var fileName = Path.GetFileName(reportPath);
        fileName.Should().NotContain("<");
        fileName.Should().NotContain(">");
        fileName.Should().NotContain(":");
        fileName.Should().NotContain("?");
        fileName.Should().NotContain("*");
        fileName.Should().EndWith("-analysis-report.txt");
    }

    // Helper: Create temp directory
    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "masdepmap-test-" + Guid.NewGuid());
        Directory.CreateDirectory(path);
        _tempDirectories.Add(path);
        return path;
    }

    // Helper: Create test graph
    private DependencyGraph CreateTestGraph(int projectCount = 10, int edgeCount = 15)
    {
        var graph = new DependencyGraph();

        // Add vertices
        var projects = new List<ProjectNode>();
        for (int i = 0; i < projectCount; i++)
        {
            var project = new ProjectNode
            {
                ProjectName = $"Project{i}",
                ProjectPath = $"C:\\projects\\Project{i}.csproj",
                SolutionName = "Solution1",
                TargetFramework = "net8.0"
            };
            graph.AddVertex(project);
            projects.Add(project);
        }

        // Add edges
        for (int i = 0; i < Math.Min(edgeCount, projectCount - 1); i++)
        {
            var edge = new DependencyEdge
            {
                Source = projects[i],
                Target = projects[(i + 1) % projectCount],
                DependencyType = DependencyType.ProjectReference
            };
            graph.AddEdge(edge);
        }

        return graph;
    }

    // Helper: Create graph with framework references
    private DependencyGraph CreateTestGraphWithFrameworkRefs(
        int customProjects,
        int frameworkProjects,
        int customEdges,
        int frameworkEdges)
    {
        var graph = new DependencyGraph();

        // Add custom projects
        var customProjectList = new List<ProjectNode>();
        for (int i = 0; i < customProjects; i++)
        {
            var project = new ProjectNode
            {
                ProjectName = $"CustomProject{i}",
                ProjectPath = $"C:\\projects\\CustomProject{i}.csproj",
                SolutionName = "Solution1",
                TargetFramework = "net8.0"
            };
            graph.AddVertex(project);
            customProjectList.Add(project);
        }

        // Add framework projects
        var frameworkProjectList = new List<ProjectNode>();
        for (int i = 0; i < frameworkProjects; i++)
        {
            var project = new ProjectNode
            {
                ProjectName = $"System.Framework{i}",  // Framework pattern
                ProjectPath = $"C:\\frameworks\\System.Framework{i}.dll",
                SolutionName = "Solution1",
                TargetFramework = "net8.0"
            };
            graph.AddVertex(project);
            frameworkProjectList.Add(project);
        }

        // Add custom edges (between custom projects)
        int customEdgeCount = 0;
        for (int i = 0; i < customProjectList.Count && customEdgeCount < customEdges; i++)
        {
            for (int j = i + 1; j < customProjectList.Count && customEdgeCount < customEdges; j++)
            {
                var edge = new DependencyEdge
                {
                    Source = customProjectList[i],
                    Target = customProjectList[j],
                    DependencyType = DependencyType.ProjectReference
                };
                graph.AddEdge(edge);
                customEdgeCount++;
            }
        }

        // Add framework edges (custom projects -> framework projects)
        int frameworkEdgeCount = 0;
        for (int i = 0; i < customProjectList.Count && frameworkEdgeCount < frameworkEdges; i++)
        {
            for (int j = 0; j < frameworkProjectList.Count && frameworkEdgeCount < frameworkEdges; j++)
            {
                var edge = new DependencyEdge
                {
                    Source = customProjectList[i],
                    Target = frameworkProjectList[j],
                    DependencyType = DependencyType.BinaryReference
                };
                graph.AddEdge(edge);
                frameworkEdgeCount++;
            }
        }

        return graph;
    }

    // Helper: Create multi-solution graph
    private DependencyGraph CreateMultiSolutionGraph(
        int solution1Projects,
        int solution2Projects,
        int crossSolutionEdges)
    {
        var graph = new DependencyGraph();

        var solution1ProjectList = new List<ProjectNode>();
        var solution2ProjectList = new List<ProjectNode>();

        // Add Solution1 projects
        for (int i = 0; i < solution1Projects; i++)
        {
            var project = new ProjectNode
            {
                ProjectName = $"Solution1.Project{i}",
                ProjectPath = $"C:\\projects\\Solution1\\Project{i}.csproj",
                SolutionName = "Solution1",  // Different solution name
                TargetFramework = "net8.0"
            };
            graph.AddVertex(project);
            solution1ProjectList.Add(project);
        }

        // Add Solution2 projects
        for (int i = 0; i < solution2Projects; i++)
        {
            var project = new ProjectNode
            {
                ProjectName = $"Solution2.Project{i}",
                ProjectPath = $"C:\\projects\\Solution2\\Project{i}.csproj",
                SolutionName = "Solution2",  // Different solution name
                TargetFramework = "net8.0"
            };
            graph.AddVertex(project);
            solution2ProjectList.Add(project);
        }

        // Add cross-solution edges
        for (int i = 0; i < Math.Min(crossSolutionEdges, Math.Min(solution1ProjectList.Count, solution2ProjectList.Count)); i++)
        {
            var edge = new DependencyEdge
            {
                Source = solution1ProjectList[i],
                Target = solution2ProjectList[i],
                DependencyType = DependencyType.ProjectReference
            };
            graph.AddEdge(edge);
        }

        return graph;
    }
}
