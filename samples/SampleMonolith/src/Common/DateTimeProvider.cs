namespace SampleMonolith.Common;

/// <summary>
/// Provides current date and time (testable abstraction).
/// </summary>
public class DateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    public virtual DateTime UtcNow => DateTime.UtcNow;
}
