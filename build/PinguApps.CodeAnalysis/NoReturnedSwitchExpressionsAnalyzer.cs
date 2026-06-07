using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PinguApps.CodeAnalysis;

/// <summary>
/// Reports an error when a switch expression is returned directly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoReturnedSwitchExpressionsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier for returned switch expression usage.
    /// </summary>
    public const string DiagnosticId = "PIN0003";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Do not return switch expressions",
        "Use explicit branches instead of returning a switch expression",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
    }

    private static void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
    {
        SwitchExpressionSyntax switchExpression = (SwitchExpressionSyntax)context.Node;

        if (IsReturnedExpression(switchExpression))
        {
            context.ReportDiagnostic(Diagnostic.Create(_rule, switchExpression.GetLocation()));
        }
    }

    private static bool IsReturnedExpression(SwitchExpressionSyntax switchExpression)
    {
        SyntaxNode node = switchExpression;

        while (node.Parent is SyntaxNode parent)
        {
            if (parent is ReturnStatementSyntax)
            {
                return true;
            }

            if (parent is ArrowExpressionClauseSyntax)
            {
                return true;
            }

            if (parent is LambdaExpressionSyntax lambdaExpression
                && ReferenceEquals(lambdaExpression.ExpressionBody, node))
            {
                return true;
            }

            node = parent;
        }

        return false;
    }
}
