namespace MasDependencyMap.Core.CycleAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Syntax walker that counts method calls from the current project to other projects/assemblies.
/// Uses Roslyn semantic analysis to identify cross-project method invocations.
/// </summary>
internal sealed class MethodCallCounterWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    private readonly IAssemblySymbol _sourceAssembly;
    private readonly Dictionary<string, int> _callCounts;

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodCallCounterWalker"/> class.
    /// </summary>
    /// <param name="semanticModel">Semantic model for resolving method symbols.</param>
    /// <param name="sourceAssembly">The assembly being analyzed (source of calls).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when semanticModel or sourceAssembly is null.
    /// </exception>
    public MethodCallCounterWalker(SemanticModel semanticModel, IAssemblySymbol sourceAssembly)
    {
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
        _sourceAssembly = sourceAssembly ?? throw new ArgumentNullException(nameof(sourceAssembly));
        _callCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the method call counts aggregated by target assembly name.
    /// </summary>
    /// <returns>
    /// Read-only dictionary mapping target assembly name to method call count.
    /// </returns>
    public IReadOnlyDictionary<string, int> GetCallCounts() => _callCounts;

    /// <summary>
    /// Visits invocation expressions to count method calls across assembly boundaries.
    /// </summary>
    /// <param name="node">The invocation expression syntax node.</param>
    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);

        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            var targetAssembly = methodSymbol.ContainingAssembly;

            // Check if method belongs to a different assembly (cross-project call)
            if (targetAssembly != null &&
                !SymbolEqualityComparer.Default.Equals(_sourceAssembly, targetAssembly))
            {
                var targetAssemblyName = targetAssembly.Name;

                // Increment call count for this target assembly
                _callCounts[targetAssemblyName] = _callCounts.GetValueOrDefault(targetAssemblyName) + 1;
            }
        }

        // Continue walking the syntax tree
        base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// Visits object creation expressions to count constructor calls across assembly boundaries.
    /// Constructor calls (new TargetClass()) are also method invocations.
    /// </summary>
    /// <param name="node">The object creation expression syntax node.</param>
    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);

        if (symbolInfo.Symbol is IMethodSymbol constructorSymbol)
        {
            var targetAssembly = constructorSymbol.ContainingAssembly;

            // Check if constructor belongs to a different assembly
            if (targetAssembly != null &&
                !SymbolEqualityComparer.Default.Equals(_sourceAssembly, targetAssembly))
            {
                var targetAssemblyName = targetAssembly.Name;

                // Increment call count for constructor invocation
                _callCounts[targetAssemblyName] = _callCounts.GetValueOrDefault(targetAssemblyName) + 1;
            }
        }

        // Continue walking the syntax tree
        base.VisitObjectCreationExpression(node);
    }
}
