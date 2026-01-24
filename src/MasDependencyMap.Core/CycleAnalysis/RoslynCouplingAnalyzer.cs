namespace MasDependencyMap.Core.CycleAnalysis;

using MasDependencyMap.Core.DependencyAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using QuikGraph;

/// <summary>
/// Analyzes coupling strength using Roslyn semantic analysis to count method calls.
/// Falls back to reference count (score=1) when semantic analysis is unavailable.
/// </summary>
public sealed class RoslynCouplingAnalyzer : ICouplingAnalyzer
{
    private readonly ILogger<RoslynCouplingAnalyzer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCouplingAnalyzer"/> class.
    /// </summary>
    /// <param name="logger">Logger for progress tracking and diagnostics.</param>
    /// <exception cref="ArgumentNullException">When logger is null.</exception>
    public RoslynCouplingAnalyzer(ILogger<RoslynCouplingAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AdjacencyGraph<ProjectNode, DependencyEdge>> AnalyzeAsync(
        AdjacencyGraph<ProjectNode, DependencyEdge> graph,
        Solution? solution,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.EdgeCount == 0)
        {
            _logger.LogInformation("No dependency edges to analyze for coupling");
            return graph;
        }

        // Try Roslyn semantic analysis
        if (solution != null)
        {
            try
            {
                return await AnalyzeWithRoslynAsync(graph, solution, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Roslyn semantic analysis unavailable, using reference count fallback: {Reason}",
                    ex.Message);
            }
        }
        else
        {
            _logger.LogWarning("Roslyn solution not provided, using reference count fallback for coupling analysis");
        }

        // Fallback: Set all edges to coupling score = 1 (reference count)
        return ApplyReferenceCountFallback(graph, cancellationToken);
    }

    /// <summary>
    /// Analyzes coupling using Roslyn semantic analysis to count actual method calls.
    /// </summary>
    private async Task<AdjacencyGraph<ProjectNode, DependencyEdge>> AnalyzeWithRoslynAsync(
        AdjacencyGraph<ProjectNode, DependencyEdge> graph,
        Solution solution,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing coupling for {ProjectCount} projects", solution.ProjectIds.Count);

        // Dictionary to store method call counts per (source, target) edge
        var edgeCouplingScores = new Dictionary<(string source, string target), int>();

        // Process projects sequentially to control memory usage
        int processedCount = 0;
        foreach (var project in solution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            processedCount++;
            _logger.LogDebug(
                "Processing project {ProjectName} ({CurrentIndex}/{TotalCount})",
                project.Name,
                processedCount,
                solution.ProjectIds.Count);

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation == null)
            {
                _logger.LogWarning("Compilation unavailable for project {ProjectName}, skipping", project.Name);
                continue;
            }

            // Analyze each syntax tree in the project
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                // Use syntax walker to count method calls
                var walker = new MethodCallCounterWalker(semanticModel, compilation.Assembly);
                walker.Visit(root);

                // Aggregate results into edge coupling scores
                foreach (var (targetAssembly, count) in walker.GetCallCounts())
                {
                    var key = (project.Name, targetAssembly);
                    edgeCouplingScores[key] = edgeCouplingScores.GetValueOrDefault(key) + count;
                }
            }
        }

        // Annotate graph edges with coupling scores
        int annotatedEdgeCount = 0;
        foreach (var edge in graph.Edges)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = (edge.Source.ProjectName, edge.Target.ProjectName);

            if (edgeCouplingScores.TryGetValue(key, out int couplingScore))
            {
                edge.CouplingScore = couplingScore;
                edge.CouplingStrength = CouplingClassifier.ClassifyCouplingStrength(couplingScore);

                _logger.LogDebug(
                    "Edge {Source} → {Target}: {Score} calls ({Strength})",
                    edge.Source.ProjectName,
                    edge.Target.ProjectName,
                    edge.CouplingScore,
                    edge.CouplingStrength);

                annotatedEdgeCount++;
            }
            else
            {
                // Edge exists in graph but no method calls found in semantic analysis
                // Keep default: CouplingScore = 1, CouplingStrength = Weak
                _logger.LogDebug(
                    "Edge {Source} → {Target}: No method calls detected, using reference count",
                    edge.Source.ProjectName,
                    edge.Target.ProjectName);
            }
        }

        _logger.LogInformation(
            "Found {EdgeCount} dependency edges with coupling scores (Total edges: {TotalEdges})",
            annotatedEdgeCount,
            graph.EdgeCount);

        return graph;
    }

    /// <summary>
    /// Applies reference count fallback when Roslyn semantic analysis is unavailable.
    /// Sets all edges to coupling score = 1 (reference exists) and Weak strength.
    /// </summary>
    private AdjacencyGraph<ProjectNode, DependencyEdge> ApplyReferenceCountFallback(
        AdjacencyGraph<ProjectNode, DependencyEdge> graph,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Applying reference count fallback for {EdgeCount} edges", graph.EdgeCount);

        foreach (var edge in graph.Edges)
        {
            edge.CouplingScore = 1;
            edge.CouplingStrength = CouplingStrength.Weak;
        }

        return graph;
    }
}
