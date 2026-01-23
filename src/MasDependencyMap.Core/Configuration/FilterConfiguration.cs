using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MasDependencyMap.Core.Configuration;

/// <summary>
/// Configuration for filtering framework dependencies.
/// Provides default blocklist/allowlist patterns when config file is missing.
/// </summary>
public sealed class FilterConfiguration : IValidatableObject
{
    /// <summary>
    /// List of namespace patterns to block from dependency analysis.
    /// Supports wildcard patterns (e.g., "Microsoft.*").
    /// </summary>
    public List<string> BlockList { get; set; } = new()
    {
        "Microsoft.*",
        "System.*",
        "mscorlib",
        "netstandard"
    };

    /// <summary>
    /// List of namespace patterns to allow even if they match the blocklist.
    /// Allowlist takes precedence over blocklist.
    /// </summary>
    public List<string> AllowList { get; set; } = new();

    /// <summary>
    /// Validates the filter configuration for invalid patterns.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();

        // Validate BlockList patterns
        if (BlockList != null)
        {
            for (int i = 0; i < BlockList.Count; i++)
            {
                var pattern = BlockList[i];
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    errors.Add(new ValidationResult(
                        $"BlockList[{i}] is null or empty. Remove invalid patterns.",
                        new[] { nameof(BlockList) }));
                }
                else if (pattern == "*")
                {
                    errors.Add(new ValidationResult(
                        $"BlockList[{i}] is just '*' which blocks everything. Use more specific patterns like 'Microsoft.*'.",
                        new[] { nameof(BlockList) }));
                }
                else if (pattern.Contains('*') && !pattern.EndsWith("*"))
                {
                    errors.Add(new ValidationResult(
                        $"BlockList[{i}] contains wildcard in middle or start: '{pattern}'. Only trailing wildcards are supported (e.g., 'Microsoft.*').",
                        new[] { nameof(BlockList) }));
                }
            }
        }

        // Validate AllowList patterns
        if (AllowList != null)
        {
            for (int i = 0; i < AllowList.Count; i++)
            {
                var pattern = AllowList[i];
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    errors.Add(new ValidationResult(
                        $"AllowList[{i}] is null or empty. Remove invalid patterns.",
                        new[] { nameof(AllowList) }));
                }
                else if (pattern == "*")
                {
                    errors.Add(new ValidationResult(
                        $"AllowList[{i}] is just '*' which allows everything. Use more specific patterns like 'YourCompany.*'.",
                        new[] { nameof(AllowList) }));
                }
                else if (pattern.Contains('*') && !pattern.EndsWith("*"))
                {
                    errors.Add(new ValidationResult(
                        $"AllowList[{i}] contains wildcard in middle or start: '{pattern}'. Only trailing wildcards are supported (e.g., 'YourCompany.*').",
                        new[] { nameof(AllowList) }));
                }
            }
        }

        return errors;
    }
}
