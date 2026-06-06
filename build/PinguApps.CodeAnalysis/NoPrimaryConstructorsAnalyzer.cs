using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PinguApps.CodeAnalysis;

/// <summary>
/// Reports an error when a type declaration uses a primary constructor.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoPrimaryConstructorsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier for primary constructor usage.
    /// </summary>
    public const string DiagnosticId = "PIN0001";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Do not use primary constructors",
        "Use a traditional constructor instead of a primary constructor",
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
        context.RegisterSyntaxNodeAction(
            AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is TypeDeclarationSyntax { ParameterList: { } parameterList })
        {
            context.ReportDiagnostic(Diagnostic.Create(_rule, parameterList.GetLocation()));
        }
    }
}
