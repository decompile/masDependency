namespace SampleMonolith.Common;

/// <summary>
/// Common string utility extensions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if a string is null or whitespace.
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
}
