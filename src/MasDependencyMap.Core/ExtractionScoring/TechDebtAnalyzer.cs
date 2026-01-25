namespace MasDependencyMap.Core.ExtractionScoring;

using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Analyzes technology version debt for projects by parsing target framework from project files.
/// Implements timeline-based scoring where older frameworks receive higher debt scores.
/// </summary>
public class TechDebtAnalyzer : ITechDebtAnalyzer
{
    private readonly ILogger<TechDebtAnalyzer> _logger;

    // Framework version debt scores (timeline-based: older = higher debt)
    private static readonly ImmutableDictionary<string, double> FrameworkScores = new Dictionary<string, double>
    {
        // .NET Framework (Legacy) - 2008-2019
        ["net35"] = 100, ["net3.5"] = 100,
        ["net40"] = 90, ["net4.0"] = 90,
        ["net45"] = 80, ["net4.5"] = 80,
        ["net451"] = 75,
        ["net452"] = 70,
        ["net46"] = 65, ["net4.6"] = 65,
        ["net461"] = 60,
        ["net462"] = 55,
        ["net47"] = 50, ["net4.7"] = 50,
        ["net471"] = 45,
        ["net472"] = 40,
        ["net48"] = 40, ["net4.8"] = 40,

        // .NET Standard (Cross-platform compatibility) - 2016-2019
        ["netstandard1.0"] = 70, ["netstandard1.1"] = 70, ["netstandard1.2"] = 70,
        ["netstandard1.3"] = 70, ["netstandard1.4"] = 70, ["netstandard1.5"] = 70, ["netstandard1.6"] = 70,
        ["netstandard2.0"] = 50,
        ["netstandard2.1"] = 35,

        // .NET Core / Modern - 2019-present
        ["netcoreapp3.1"] = 30,
        ["net5.0"] = 20,
        ["net6.0"] = 10,
        ["net7.0"] = 5,
        ["net8.0"] = 0,
        ["net9.0"] = 0
    }.ToImmutableDictionary();

    private const double NeutralFallbackScore = 50.0;

    public TechDebtAnalyzer(ILogger<TechDebtAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TechDebtMetric> AnalyzeAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        // Validate project file extension
        if (!project.ProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) &&
            !project.ProjectPath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"ProjectPath must point to a .csproj or .vbproj file. Received: {project.ProjectPath}",
                nameof(project));
        }

        _logger.LogInformation("Analyzing tech debt for project {ProjectName}", project.ProjectName);

        try
        {
            // Load project XML with proper resource disposal
            await using var stream = File.OpenRead(project.ProjectPath);
            var doc = await XDocument.LoadAsync(
                stream,
                LoadOptions.None,
                cancellationToken)
                .ConfigureAwait(false);

            // Parse TargetFramework (singular, SDK-style)
            // Use LocalName to ignore XML namespaces
            var targetFramework = doc.Descendants()
                .Where(e => e.Name.LocalName == "TargetFramework")
                .FirstOrDefault()?.Value;

            // Try TargetFrameworks (plural, multi-targeting)
            if (string.IsNullOrEmpty(targetFramework))
            {
                var multiTarget = doc.Descendants()
                    .Where(e => e.Name.LocalName == "TargetFrameworks")
                    .FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(multiTarget))
                {
                    // Use first target from semicolon-separated list
                    targetFramework = multiTarget.Split(';')[0];
                }
            }

            // Try TargetFrameworkVersion (legacy .NET Framework)
            if (string.IsNullOrEmpty(targetFramework))
            {
                var legacyVersion = doc.Descendants()
                    .Where(e => e.Name.LocalName == "TargetFrameworkVersion")
                    .FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(legacyVersion))
                {
                    // Convert v4.7.2 → net472
                    targetFramework = ConvertLegacyToTfm(legacyVersion);
                }
            }

            // If still not found, default to unknown
            if (string.IsNullOrEmpty(targetFramework))
            {
                _logger.LogWarning("No TargetFramework found for {ProjectName}, defaulting to neutral score {Score}",
                    project.ProjectName, NeutralFallbackScore);
                return new TechDebtMetric(
                    project.ProjectName,
                    project.ProjectPath,
                    TargetFramework: "unknown",
                    NormalizedScore: NeutralFallbackScore);
            }

            // Calculate tech debt score
            var score = CalculateTechDebtScore(targetFramework);

            _logger.LogInformation("Detected framework {TargetFramework} for {ProjectName}",
                targetFramework, project.ProjectName);
            _logger.LogDebug("Project {ProjectName}: Framework={TargetFramework}, Score={NormalizedScore}",
                project.ProjectName, targetFramework, score);

            return new TechDebtMetric(
                project.ProjectName,
                project.ProjectPath,
                targetFramework,
                score);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fallback: XML parsing failed
            _logger.LogWarning("Could not parse TargetFramework for {ProjectName}, defaulting to neutral score {Score}: {Reason}",
                project.ProjectName, NeutralFallbackScore, ex.Message);

            return new TechDebtMetric(
                project.ProjectName,
                project.ProjectPath,
                TargetFramework: "unknown",
                NormalizedScore: NeutralFallbackScore); // Neutral score
        }
    }

    /// <summary>
    /// Converts legacy .NET Framework version format to TFM.
    /// Example: v4.7.2 → net472
    /// </summary>
    private static string ConvertLegacyToTfm(string legacyVersion)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(legacyVersion))
            return "unknown";

        // Remove 'v' prefix: v4.7.2 → 4.7.2
        var version = legacyVersion.TrimStart('v');

        // Validate after removing 'v' prefix
        if (string.IsNullOrWhiteSpace(version))
            return "unknown";

        // Remove dots: 4.7.2 → 472
        var tfm = "net" + version.Replace(".", "");

        return tfm; // net472
    }

    /// <summary>
    /// Calculates tech debt score for a target framework moniker.
    /// Timeline-based scoring: older frameworks = higher debt.
    /// </summary>
    private static double CalculateTechDebtScore(string targetFramework)
    {
        // Normalize TFM (handle variations like "net6" vs "net6.0")
        var normalizedTfm = NormalizeTfm(targetFramework);

        // Direct lookup
        if (FrameworkScores.TryGetValue(normalizedTfm, out var score))
        {
            return score;
        }

        // Fallback: Unknown framework → neutral score
        return NeutralFallbackScore;
    }

    /// <summary>
    /// Normalizes TFM strings to handle common variations.
    /// Examples: net6 → net6.0, net472 → net472
    /// </summary>
    private static string NormalizeTfm(string tfm)
    {
        if (string.IsNullOrEmpty(tfm))
            return tfm;

        // Handle netcoreapp as-is
        if (tfm.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
            return tfm.ToLowerInvariant();

        if (tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            var versionPart = tfm.Substring(3); // Remove "net" prefix

            // Already normalized (net472, net8.0)
            if (versionPart.Contains('.') || versionPart.Length > 2)
                return tfm.ToLowerInvariant();

            // Add .0 suffix (net6 → net6.0)
            return (tfm + ".0").ToLowerInvariant();
        }

        return tfm.ToLowerInvariant();
    }
}
