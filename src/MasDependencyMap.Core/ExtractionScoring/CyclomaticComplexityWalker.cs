namespace MasDependencyMap.Core.ExtractionScoring;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Walks a C# syntax tree to calculate cyclomatic complexity.
/// Counts decision points (if, loops, switch cases, operators, etc.) using McCabe's formula: CC = 1 + decision points.
/// </summary>
internal sealed class CyclomaticComplexityWalker : CSharpSyntaxWalker
{
    /// <summary>
    /// Gets the cyclomatic complexity count. Starts at 1 per McCabe's formula.
    /// </summary>
    public int Complexity { get; private set; } = 1;

    /// <summary>
    /// Visits an if statement and increments complexity.
    /// Each if/else if adds a decision point.
    /// </summary>
    public override void VisitIfStatement(IfStatementSyntax node)
    {
        Complexity++;
        base.VisitIfStatement(node);
    }

    /// <summary>
    /// Visits a for loop and increments complexity.
    /// </summary>
    public override void VisitForStatement(ForStatementSyntax node)
    {
        Complexity++;
        base.VisitForStatement(node);
    }

    /// <summary>
    /// Visits a foreach loop and increments complexity.
    /// </summary>
    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        Complexity++;
        base.VisitForEachStatement(node);
    }

    /// <summary>
    /// Visits a while loop and increments complexity.
    /// </summary>
    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        Complexity++;
        base.VisitWhileStatement(node);
    }

    /// <summary>
    /// Visits a do-while loop and increments complexity.
    /// </summary>
    public override void VisitDoStatement(DoStatementSyntax node)
    {
        Complexity++;
        base.VisitDoStatement(node);
    }

    /// <summary>
    /// Visits a switch section (case) and increments complexity.
    /// Each case in a switch statement adds a decision point.
    /// </summary>
    public override void VisitSwitchSection(SwitchSectionSyntax node)
    {
        Complexity++;
        base.VisitSwitchSection(node);
    }

    /// <summary>
    /// Visits a conditional expression (ternary operator) and increments complexity.
    /// </summary>
    public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        Complexity++;
        base.VisitConditionalExpression(node);
    }

    /// <summary>
    /// Visits a catch clause and increments complexity.
    /// Each catch block adds a decision point.
    /// </summary>
    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        Complexity++;
        base.VisitCatchClause(node);
    }

    /// <summary>
    /// Visits a binary expression and increments complexity for && and || operators.
    /// Logical AND and OR operators add decision points.
    /// </summary>
    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        if (node.Kind() == SyntaxKind.LogicalAndExpression ||
            node.Kind() == SyntaxKind.LogicalOrExpression)
        {
            Complexity++;
        }
        base.VisitBinaryExpression(node);
    }
}
