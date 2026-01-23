namespace MasDependencyMap.Core.DependencyAnalysis;

using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.SolutionLoading;

/// <summary>
/// Builds QuikGraph dependency graphs from solution analysis results.
/// Supports single-solution and multi-solution graph construction with cross-solution dependency detection.
/// </summary>
public class DependencyGraphBuilder : IDependencyGraphBuilder
{
    private readonly ILogger<DependencyGraphBuilder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyGraphBuilder"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging of graph building operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public DependencyGraphBuilder(ILogger<DependencyGraphBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds a dependency graph from a single solution analysis.
    /// </summary>
    /// <param name="solution">The solution analysis containing projects and dependencies.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A dependency graph with all projects and dependencies.</returns>
    public Task<DependencyGraph> BuildAsync(
        SolutionAnalysis solution,
        CancellationToken cancellationToken = default)
    {
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));

        _logger.LogInformation(
            "Building dependency graph for solution: {SolutionName} ({ProjectCount} projects)",
            solution.SolutionName,
            solution.Projects.Count);

        var graph = new DependencyGraph();
        var projectNodeMap = new Dictionary<string, ProjectNode>(StringComparer.OrdinalIgnoreCase);

        // Phase 1: Add all vertices (projects)
        foreach (var project in solution.Projects)
        {
            var node = new ProjectNode
            {
                ProjectName = project.Name,
                ProjectPath = project.FilePath,
                TargetFramework = project.TargetFramework,
                SolutionName = solution.SolutionName
            };

            graph.AddVertex(node);
            projectNodeMap[project.FilePath] = node;
        }

        // Phase 2: Add edges (dependencies)
        foreach (var project in solution.Projects)
        {
            var sourceNode = projectNodeMap[project.FilePath];

            foreach (var reference in project.References)
            {
                // Only process ProjectReferences for now (BinaryReferences will be filtered in Story 2.6)
                if (reference.Type == ReferenceType.ProjectReference && reference.TargetPath != null)
                {
                    if (projectNodeMap.TryGetValue(reference.TargetPath, out var targetNode))
                    {
                        var edge = new DependencyEdge
                        {
                            Source = sourceNode,
                            Target = targetNode,
                            DependencyType = DependencyType.ProjectReference
                        };

                        graph.AddEdge(edge);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Project reference target not found in solution: {SourceProject} -> {TargetPath}",
                            project.Name,
                            reference.TargetPath);
                    }
                }
            }
        }

        _logger.LogInformation(
            "Graph built: {VertexCount} vertices, {EdgeCount} edges",
            graph.VertexCount,
            graph.EdgeCount);

        // Validation: Detect orphaned nodes
        var orphaned = graph.DetectOrphanedNodes();
        if (orphaned.Any())
        {
            _logger.LogInformation(
                "Found {OrphanedCount} orphaned nodes (no dependencies): {OrphanedNodes}",
                orphaned.Count,
                string.Join(", ", orphaned.Select(n => n.ProjectName)));
        }

        return Task.FromResult(graph);
    }

    /// <summary>
    /// Builds a unified dependency graph from multiple solution analyses.
    /// Merges projects from all solutions and detects cross-solution dependencies.
    /// </summary>
    /// <param name="solutions">The collection of solution analyses to merge.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A unified dependency graph containing all projects across all solutions.</returns>
    public Task<DependencyGraph> BuildAsync(
        IEnumerable<SolutionAnalysis> solutions,
        CancellationToken cancellationToken = default)
    {
        if (solutions == null)
            throw new ArgumentNullException(nameof(solutions));

        var solutionsList = solutions.ToList();

        _logger.LogInformation(
            "Building unified dependency graph for {SolutionCount} solutions",
            solutionsList.Count);

        var graph = new DependencyGraph();
        var projectNodeCache = new Dictionary<string, ProjectNode>(StringComparer.OrdinalIgnoreCase);

        // Phase 1: Create all vertices from all solutions
        foreach (var solution in solutionsList)
        {
            foreach (var project in solution.Projects)
            {
                // Avoid duplicate vertices (same project in multiple solutions)
                if (!projectNodeCache.ContainsKey(project.FilePath))
                {
                    var node = new ProjectNode
                    {
                        ProjectName = project.Name,
                        ProjectPath = project.FilePath,
                        TargetFramework = project.TargetFramework,
                        SolutionName = solution.SolutionName
                    };

                    projectNodeCache[project.FilePath] = node;
                    graph.AddVertex(node);
                }
            }
        }

        // Phase 2: Create all edges
        var crossSolutionCount = 0;

        foreach (var solution in solutionsList)
        {
            foreach (var project in solution.Projects)
            {
                var sourceNode = projectNodeCache[project.FilePath];

                // Add ProjectReference edges
                foreach (var reference in project.References)
                {
                    if (reference.Type == ReferenceType.ProjectReference && reference.TargetPath != null)
                    {
                        if (projectNodeCache.TryGetValue(reference.TargetPath, out var targetNode))
                        {
                            var edge = new DependencyEdge
                            {
                                Source = sourceNode,
                                Target = targetNode,
                                DependencyType = DependencyType.ProjectReference
                            };

                            graph.AddEdge(edge);

                            // Log cross-solution dependencies
                            if (edge.IsCrossSolution)
                            {
                                crossSolutionCount++;
                                _logger.LogInformation(
                                    "Cross-solution dependency: {Source} ({SourceSolution}) -> {Target} ({TargetSolution})",
                                    sourceNode.ProjectName,
                                    sourceNode.SolutionName,
                                    targetNode.ProjectName,
                                    targetNode.SolutionName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Project reference target not found across solutions: {SourceProject} -> {TargetPath}",
                                project.Name,
                                reference.TargetPath);
                        }
                    }
                }
            }
        }

        _logger.LogInformation(
            "Unified graph built: {VertexCount} vertices, {EdgeCount} edges, {CrossSolutionCount} cross-solution dependencies",
            graph.VertexCount,
            graph.EdgeCount,
            crossSolutionCount);

        // Validation: Detect orphaned nodes
        var orphaned = graph.DetectOrphanedNodes();
        if (orphaned.Any())
        {
            _logger.LogInformation(
                "Found {OrphanedCount} orphaned nodes (no dependencies): {OrphanedNodes}",
                orphaned.Count,
                string.Join(", ", orphaned.Select(n => n.ProjectName)));
        }

        return Task.FromResult(graph);
    }
}
