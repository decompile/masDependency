namespace MasDependencyMap.Core.Visualization;

/// <summary>
/// Exception thrown when DOT file generation fails.
/// </summary>
public class DotGenerationException : Exception
{
    public DotGenerationException(string message) : base(message)
    {
    }

    public DotGenerationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
