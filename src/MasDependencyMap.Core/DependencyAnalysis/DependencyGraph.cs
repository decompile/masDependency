namespace MasDependencyMap.Core.DependencyAnalysis;

using QuikGraph;

/// <summary>
/// Wraps QuikGraph's BidirectionalGraph to provide a domain-specific API for dependency graph operations.
/// Encapsulates graph structure and provides helper methods for analysis and validation.
/// Uses BidirectionalGraph to support both incoming and outgoing edge queries.
/// </summary>
public class DependencyGraph
{
    private readonly BidirectionalGraph<ProjectNode, DependencyEdge> _graph;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyGraph"/> class.
    /// Creates an empty directed bidirectional graph.
    /// </summary>
    public DependencyGraph()
    {
        _graph = new BidirectionalGraph<ProjectNode, DependencyEdge>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyGraph"/> class with an existing graph.
    /// </summary>
    /// <param name="graph">The QuikGraph BidirectionalGraph to wrap.</param>
    public DependencyGraph(BidirectionalGraph<ProjectNode, DependencyEdge> graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <summary>
    /// Gets all vertices (project nodes) in the graph.
    /// </summary>
    public IEnumerable<ProjectNode> Vertices => _graph.Vertices;

    /// <summary>
    /// Gets all edges (dependencies) in the graph.
    /// </summary>
    public IEnumerable<DependencyEdge> Edges => _graph.Edges;

    /// <summary>
    /// Gets the total number of vertices (projects) in the graph.
    /// </summary>
    public int VertexCount => _graph.VertexCount;

    /// <summary>
    /// Gets the total number of edges (dependencies) in the graph.
    /// </summary>
    public int EdgeCount => _graph.EdgeCount;

    /// <summary>
    /// Adds a vertex (project) to the graph.
    /// </summary>
    /// <param name="node">The project node to add.</param>
    /// <returns>True if the vertex was added; false if it already exists.</returns>
    public bool AddVertex(ProjectNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return _graph.AddVertex(node);
    }

    /// <summary>
    /// Adds an edge (dependency) to the graph.
    /// The source and target vertices must already exist in the graph.
    /// </summary>
    /// <param name="edge">The dependency edge to add.</param>
    /// <returns>True if the edge was added; false if it already exists.</returns>
    public bool AddEdge(DependencyEdge edge)
    {
        if (edge == null)
            throw new ArgumentNullException(nameof(edge));

        return _graph.AddEdge(edge);
    }

    /// <summary>
    /// Gets all outgoing edges from a project (projects that this project depends on).
    /// </summary>
    /// <param name="node">The project node.</param>
    /// <returns>An enumerable of outgoing edges.</returns>
    public IEnumerable<DependencyEdge> GetOutEdges(ProjectNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return _graph.OutEdges(node);
    }

    /// <summary>
    /// Gets all incoming edges to a project (projects that depend on this project).
    /// </summary>
    /// <param name="node">The project node.</param>
    /// <returns>An enumerable of incoming edges.</returns>
    public IEnumerable<DependencyEdge> GetInEdges(ProjectNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return _graph.InEdges(node);
    }

    /// <summary>
    /// Checks if a project has any outgoing edges (depends on other projects).
    /// </summary>
    /// <param name="node">The project node to check.</param>
    /// <returns>True if the project has no outgoing edges; otherwise, false.</returns>
    public bool IsOutEdgesEmpty(ProjectNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return _graph.IsOutEdgesEmpty(node);
    }

    /// <summary>
    /// Checks if a project has any incoming edges (is depended upon by other projects).
    /// </summary>
    /// <param name="node">The project node to check.</param>
    /// <returns>True if the project has no incoming edges; otherwise, false.</returns>
    public bool IsInEdgesEmpty(ProjectNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return _graph.IsInEdgesEmpty(node);
    }

    /// <summary>
    /// Detects orphaned nodes (projects with no dependencies and no dependents).
    /// These are projects that are completely isolated in the graph.
    /// </summary>
    /// <returns>A list of orphaned project nodes.</returns>
    public IReadOnlyList<ProjectNode> DetectOrphanedNodes()
    {
        return Vertices.Where(v => _graph.IsOutEdgesEmpty(v) && _graph.IsInEdgesEmpty(v)).ToList();
    }

    /// <summary>
    /// Gets the underlying QuikGraph instance for advanced graph algorithm operations.
    /// </summary>
    /// <returns>The wrapped BidirectionalGraph instance.</returns>
    public BidirectionalGraph<ProjectNode, DependencyEdge> GetUnderlyingGraph()
    {
        return _graph;
    }
}
