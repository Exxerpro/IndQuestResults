using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DebugAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DebugRule = new(
        "DEBUG001",
        "Debug diagnostic",
        "Position debug",
        "Debug",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DebugRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "Success" &&
            memberAccess.Expression is GenericNameSyntax genericName &&
            genericName.Identifier.Text == "Result")
        {
            var lineSpan = memberAccess.Expression.GetLocation().GetLineSpan();
            var column = lineSpan.StartLinePosition.Character + 1;
            
            var diagnostic = Diagnostic.Create(
                DebugRule,
                memberAccess.Expression.GetLocation(),
                $"Column: {column}");
            context.ReportDiagnostic(diagnostic);
        }
    }
}