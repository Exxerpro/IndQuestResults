using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IndQuestResults.Analyzers.Analyzers;

/// <summary>
/// Analyzer that recommends using the async-specific Result API under
/// <c>IndQuestResults.Async.ResultAsync</c> instead of <c>ResultExtensions.ThenAsync</c>.
/// Encourages consistent async chaining via <c>BindAsync</c>/<c>MapAsync</c>/<c>TapAsync</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferResultAsyncAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID: Prefer ResultAsync for async chaining.
    /// </summary>
    public const string DiagnosticId = "IQR0001";

    private static readonly LocalizableString Title = "Prefer ResultAsync for async chaining";
    private static readonly LocalizableString MessageFormat = "Use ResultAsync.BindAsync instead of ResultExtensions.ThenAsync for async pipelines";
    private static readonly LocalizableString Description = "The preferred async API lives under IndQuestResults.Async.ResultAsync. Using ResultExtensions.ThenAsync can make chains less consistent.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description);

    /// <summary>
    /// Gets the diagnostics produced by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <summary>
    /// Initializes analysis and registers syntax node actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        // Do not report inside this repo's own assemblies (library, analyzers, samples, tests)
        // to avoid noisy hints while developing. Consumers will still get the diagnostic.
        // Exception: Allow test assemblies (TestAssembly) to be analyzed for testing purposes
        var asmName = context.Compilation.AssemblyName ?? string.Empty;
        if (asmName.StartsWith("IndQuestResults", System.StringComparison.OrdinalIgnoreCase) &&
            !asmName.Equals("TestAssembly", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        // Look for member invocation like expr.ThenAsync(...)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var name = memberAccess.Name.Identifier.Text;
        if (name != "ThenAsync")
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
        IMethodSymbol? symbol = symbolInfo.Symbol as IMethodSymbol;
        if (symbol is null && !symbolInfo.CandidateSymbols.IsDefaultOrEmpty)
        {
            symbol = symbolInfo.CandidateSymbols[0] as IMethodSymbol;
        }
        if (symbol is null)
        {
            return;
        }

        // Ensure it's our extension method in ResultExtensions
        if (!symbol.IsExtensionMethod)
        {
            return;
        }

        var containingType = symbol.ContainingType;
        if (containingType is null)
        {
            return;
        }

        // Check both the fully qualified name and the simple name
        var fullContaining = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var simpleName = containingType.Name;
        var namespaceName = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        
        // Check if it's ResultExtensions in IndQuestResults.Operations namespace
        if (simpleName != "ResultExtensions" || 
            (!namespaceName.Contains("IndQuestResults.Operations") && 
             !fullContaining.Contains("IndQuestResults.Operations.ResultExtensions")))
        {
            return;
        }

        // Report info diagnostic on the member name
        var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
