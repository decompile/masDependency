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
        _callCounts = new Dictionary<string, int>(StringComparer.Ordinal); // Assembly names are case-sensitive
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

    /// <summary>
    /// Visits member access expressions to count property getter calls across assembly boundaries.
    /// Property access like target.Property compiles to get_Property() method call.
    /// </summary>
    /// <param name="node">The member access expression syntax node.</param>
    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);

        // Check if this is a property access (property getters/setters are method calls)
        if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
        {
            var targetAssembly = propertySymbol.ContainingAssembly;

            // Check if property belongs to a different assembly
            if (targetAssembly != null &&
                !SymbolEqualityComparer.Default.Equals(_sourceAssembly, targetAssembly))
            {
                var targetAssemblyName = targetAssembly.Name;

                // Increment call count for property access (getter or setter)
                _callCounts[targetAssemblyName] = _callCounts.GetValueOrDefault(targetAssemblyName) + 1;
            }
        }

        // Continue walking the syntax tree
        base.VisitMemberAccessExpression(node);
    }

    /// <summary>
    /// Visits element access expressions to count indexer calls across assembly boundaries.
    /// Indexer usage like target[index] compiles to get_Item() or set_Item() method call.
    /// </summary>
    /// <param name="node">The element access expression syntax node.</param>
    public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);

        // Indexers resolve to property symbols (with get/set accessors)
        if (symbolInfo.Symbol is IPropertySymbol indexerSymbol)
        {
            var targetAssembly = indexerSymbol.ContainingAssembly;

            // Check if indexer belongs to a different assembly
            if (targetAssembly != null &&
                !SymbolEqualityComparer.Default.Equals(_sourceAssembly, targetAssembly))
            {
                var targetAssemblyName = targetAssembly.Name;

                // Increment call count for indexer access
                _callCounts[targetAssemblyName] = _callCounts.GetValueOrDefault(targetAssemblyName) + 1;
            }
        }

        // Continue walking the syntax tree
        base.VisitElementAccessExpression(node);
    }

    /// <summary>
    /// Visits binary expressions to count operator overload calls across assembly boundaries.
    /// Binary operators like a + b can be overloaded and compile to op_Addition() method calls.
    /// </summary>
    /// <param name="node">The binary expression syntax node.</param>
    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);

        // Operator overloads resolve to IMethodSymbol with MethodKind.UserDefinedOperator
        if (symbolInfo.Symbol is IMethodSymbol operatorSymbol)
        {
            var targetAssembly = operatorSymbol.ContainingAssembly;

            // Check if operator belongs to a different assembly
            if (targetAssembly != null &&
                !SymbolEqualityComparer.Default.Equals(_sourceAssembly, targetAssembly))
            {
                var targetAssemblyName = targetAssembly.Name;

                // Increment call count for operator overload
                _callCounts[targetAssemblyName] = _callCounts.GetValueOrDefault(targetAssemblyName) + 1;
            }
        }

        // Continue walking the syntax tree
        base.VisitBinaryExpression(node);
    }

    /// <summary>
    /// Visits cast expressions to count implicit/explicit conversion operator calls across assembly boundaries.
    /// Casts like (TargetType)value can invoke user-defined conversion operators.
    /// </summary>
    /// <param name="node">The cast expression syntax node.</param>
    public override void VisitCastExpression(CastExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);

        // Conversion operators resolve to IMethodSymbol with MethodKind.Conversion
        if (symbolInfo.Symbol is IMethodSymbol conversionSymbol)
        {
            var targetAssembly = conversionSymbol.ContainingAssembly;

            // Check if conversion operator belongs to a different assembly
            if (targetAssembly != null &&
                !SymbolEqualityComparer.Default.Equals(_sourceAssembly, targetAssembly))
            {
                var targetAssemblyName = targetAssembly.Name;

                // Increment call count for conversion operator
                _callCounts[targetAssemblyName] = _callCounts.GetValueOrDefault(targetAssemblyName) + 1;
            }
        }

        // Continue walking the syntax tree
        base.VisitCastExpression(node);
    }
}
