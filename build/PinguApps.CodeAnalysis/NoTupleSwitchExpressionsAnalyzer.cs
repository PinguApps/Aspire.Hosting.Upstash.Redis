using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PinguApps.CodeAnalysis;

/// <summary>
/// Reports an error when a switch expression uses a tuple as its governing expression.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoTupleSwitchExpressionsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier for tuple switch expression usage.
    /// </summary>
    public const string DiagnosticId = "PIN0003";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Do not use tuple switch expressions",
        "Use sequential checks instead of a tuple switch expression",
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

        if (switchExpression.GoverningExpression is TupleExpressionSyntax tupleExpression)
        {
            context.ReportDiagnostic(Diagnostic.Create(_rule, tupleExpression.GetLocation()));
        }
    }
}
