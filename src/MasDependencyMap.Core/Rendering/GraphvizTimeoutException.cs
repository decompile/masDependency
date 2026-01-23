namespace MasDependencyMap.Core.Rendering;

/// <summary>
/// Exception thrown when Graphviz rendering exceeds the timeout duration.
/// The default rendering timeout is 30 seconds.
/// </summary>
public class GraphvizTimeoutException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphvizTimeoutException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public GraphvizTimeoutException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphvizTimeoutException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GraphvizTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
