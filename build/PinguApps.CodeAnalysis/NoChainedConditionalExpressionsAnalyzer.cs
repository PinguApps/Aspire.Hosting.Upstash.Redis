using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PinguApps.CodeAnalysis;

/// <summary>
/// Reports an error when a conditional expression contains another conditional expression.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoChainedConditionalExpressionsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier for chained conditional expression usage.
    /// </summary>
    public const string DiagnosticId = "PIN0002";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Do not chain conditional expressions",
        "Use at most one conditional expression in the same expression",
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
        context.RegisterSyntaxNodeAction(AnalyzeConditionalExpression, SyntaxKind.ConditionalExpression);
    }

    private static void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
    {
        ConditionalExpressionSyntax conditionalExpression = (ConditionalExpressionSyntax)context.Node;

        if (conditionalExpression.Ancestors().OfType<ConditionalExpressionSyntax>().Any())
        {
            return;
        }

        bool hasNestedConditionalExpression = conditionalExpression
            .DescendantNodes()
            .OfType<ConditionalExpressionSyntax>()
            .Any();

        if (hasNestedConditionalExpression)
        {
            context.ReportDiagnostic(Diagnostic.Create(_rule, conditionalExpression.GetLocation()));
        }
    }
}
