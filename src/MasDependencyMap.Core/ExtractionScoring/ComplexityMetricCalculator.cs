namespace MasDependencyMap.Core.ExtractionScoring;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using MasDependencyMap.Core.DependencyAnalysis;

/// <summary>
/// Calculates cyclomatic complexity metrics for projects using Roslyn semantic analysis.
/// Walks method syntax trees to count decision points and calculates complexity scores.
/// Falls back to neutral score (50) when Roslyn is unavailable.
/// </summary>
public sealed class ComplexityMetricCalculator : IComplexityMetricCalculator
{
    private readonly ILogger<ComplexityMetricCalculator> _logger;

    // Normalization thresholds based on industry standards
    // NIST235: 1-10 low, Microsoft CA1502: 11-24 moderate, 25+ excessive
    // Miller's Law: 1-7 ideal
    private const double LowComplexityThreshold = 7.0;
    private const double MediumComplexityThreshold = 15.0;
    private const double HighComplexityThreshold = 25.0;
    private const double VeryHighComplexityRange = 10.0;
    private const double NormalizedScoreScale = 100.0;
    private const double NeutralFallbackScore = 50.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplexityMetricCalculator"/> class.
    /// </summary>
    /// <param name="logger">Logger for progress and diagnostic output.</param>
    public ComplexityMetricCalculator(ILogger<ComplexityMetricCalculator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ComplexityMetric> CalculateAsync(
        ProjectNode project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        _logger.LogInformation("Calculating cyclomatic complexity for project {ProjectName}", project.ProjectName);

        try
        {
            // CRITICAL: MSBuildLocator.RegisterDefaults() must be called in Program.Main BEFORE this
            using var workspace = MSBuildWorkspace.Create();

            // Load project using Roslyn
            var roslynProject = await workspace.OpenProjectAsync(project.ProjectPath, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var methodCount = 0;
            var totalComplexity = 0;

            // Analyze each document (source file) in project
            foreach (var document in roslynProject.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxRoot == null) continue;

                // Find all executable code blocks: methods, constructors, properties, local functions
                var executableNodes = syntaxRoot.DescendantNodes()
                    .Where(node => node is MethodDeclarationSyntax
                                   or ConstructorDeclarationSyntax
                                   or PropertyDeclarationSyntax
                                   or LocalFunctionStatementSyntax);

                foreach (var node in executableNodes)
                {
                    methodCount++;

                    // Walk syntax tree to calculate complexity
                    var walker = new CyclomaticComplexityWalker();
                    walker.Visit(node);

                    totalComplexity += walker.Complexity;

                    var nodeName = node switch
                    {
                        MethodDeclarationSyntax method => method.Identifier.Text,
                        ConstructorDeclarationSyntax ctor => $".ctor({ctor.ParameterList.Parameters.Count} params)",
                        PropertyDeclarationSyntax prop => $"Property {prop.Identifier.Text}",
                        LocalFunctionStatementSyntax local => $"LocalFunction {local.Identifier.Text}",
                        _ => "Unknown"
                    };

                    _logger.LogDebug("{NodeType} {NodeName} in {FileName}: Complexity={Complexity}",
                        node.GetType().Name, nodeName, document.Name, walker.Complexity);
                }
            }

            // Calculate average complexity
            var avgComplexity = methodCount > 0 ? (double)totalComplexity / methodCount : 0.0;

            // Normalize to 0-100 scale
            var normalizedScore = NormalizeComplexity(avgComplexity);

            _logger.LogDebug("Project {ProjectName}: Methods={MethodCount}, Total={TotalComplexity}, Average={AverageComplexity:F2}, Normalized={NormalizedScore:F2}",
                project.ProjectName, methodCount, totalComplexity, avgComplexity, normalizedScore);

            _logger.LogInformation("Complexity calculation complete for {ProjectName}", project.ProjectName);

            return new ComplexityMetric(
                project.ProjectName,
                project.ProjectPath,
                methodCount,
                totalComplexity,
                avgComplexity,
                normalizedScore);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fallback: Roslyn unavailable
            _logger.LogWarning("Roslyn semantic analysis unavailable for {ProjectName}, defaulting to neutral score 50: {Reason}",
                project.ProjectName, ex.Message);

            return new ComplexityMetric(
                project.ProjectName,
                project.ProjectPath,
                MethodCount: 0,
                TotalComplexity: 0,
                AverageComplexity: 0.0,
                NormalizedScore: NeutralFallbackScore);
        }
    }

    /// <summary>
    /// Normalizes average cyclomatic complexity to 0-100 scale using industry thresholds.
    /// Uses linear interpolation between threshold boundaries:
    /// - 0-7: Low complexity (0-33 normalized)
    /// - 8-15: Medium complexity (34-66 normalized)
    /// - 16-25: High complexity (67-90 normalized)
    /// - 26+: Very high complexity (91-100 normalized, capped at 100)
    /// </summary>
    /// <param name="avgComplexity">Average cyclomatic complexity per method.</param>
    /// <returns>Normalized score in 0-100 range. Higher = more complex = harder to extract.</returns>
    internal static double NormalizeComplexity(double avgComplexity)
    {
        // Handle edge case: no complexity
        if (avgComplexity <= 0)
        {
            return 0.0;
        }

        // Low complexity: 0-7 → 0-33
        if (avgComplexity <= LowComplexityThreshold)
        {
            return (avgComplexity / LowComplexityThreshold) * 33.0;
        }

        // Medium complexity: 8-15 → 34-66
        if (avgComplexity <= MediumComplexityThreshold)
        {
            return 33.0 + ((avgComplexity - LowComplexityThreshold) / (MediumComplexityThreshold - LowComplexityThreshold)) * 33.0;
        }

        // High complexity: 16-25 → 67-90
        if (avgComplexity <= HighComplexityThreshold)
        {
            return 66.0 + ((avgComplexity - MediumComplexityThreshold) / (HighComplexityThreshold - MediumComplexityThreshold)) * 24.0;
        }

        // Very high complexity: 26+ → 91-100 (capped at 100)
        var veryHighScore = 90.0 + ((avgComplexity - HighComplexityThreshold) / VeryHighComplexityRange) * VeryHighComplexityRange;
        return Math.Clamp(veryHighScore, 0.0, NormalizedScoreScale);
    }
}
