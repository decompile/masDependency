namespace MasDependencyMap.Core.Rendering;

/// <summary>
/// Exception thrown when Graphviz rendering fails.
/// Indicates that the Graphviz 'dot' process returned a non-zero exit code
/// or failed to produce the expected output file.
/// </summary>
public class GraphvizRenderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphvizRenderException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public GraphvizRenderException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphvizRenderException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GraphvizRenderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
