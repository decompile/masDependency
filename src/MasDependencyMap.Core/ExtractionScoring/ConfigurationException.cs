namespace MasDependencyMap.Core.ExtractionScoring;

/// <summary>
/// Exception thrown when configuration validation fails for extraction scoring weights.
/// Used to fail fast with clear error messages when weights don't sum to 1.0 or are out of valid range.
/// </summary>
public sealed class ConfigurationException : Exception
{
    /// <summary>
    /// Creates a new ConfigurationException with a message.
    /// </summary>
    /// <param name="message">Error message describing what configuration is invalid and how to fix it</param>
    public ConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new ConfigurationException with a message and inner exception.
    /// </summary>
    /// <param name="message">Error message describing what configuration is invalid</param>
    /// <param name="innerException">Original exception that caused the failure</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
