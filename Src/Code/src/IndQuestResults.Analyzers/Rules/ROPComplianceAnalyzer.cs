using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IndQuestResults.Analyzers.Rules;

/// <summary>
/// Analyzer for Railway-Oriented Programming (ROP) compliance violations.
/// Detects usage of ArgumentNullException.ThrowIfNull in extension methods that should return Result failures.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ROPComplianceAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID: ROP violation - ArgumentNullException.ThrowIfNull in extension methods.
    /// </summary>
    public const string ROPViolationThrowIfNullId = "IQR201";

    /// <summary>
    /// Diagnostic ID: ROP violation - throw statement in Result-returning methods.
    /// </summary>
    public const string ROPViolationThrowStatementId = "IQR202";

    private static readonly LocalizableString ThrowIfNullTitle = "ROP violation: ArgumentNullException.ThrowIfNull in extension method";
    private static readonly LocalizableString ThrowIfNullMessageFormat = "Extension methods should return Result failures instead of throwing ArgumentNullException. Replace with: if ({0} is null) {{ return Result<T>.WithFailure(\"{0} cannot be null\"); }}";
    private static readonly LocalizableString ThrowIfNullDescription = "Railway-Oriented Programming requires that methods return Result failures instead of throwing exceptions for control flow. Extension methods should handle null parameters gracefully.";

    private static readonly LocalizableString ThrowStatementTitle = "ROP violation: throw statement in Result-returning method";
    private static readonly LocalizableString ThrowStatementMessageFormat = "Result-returning methods should return Result failures instead of throwing exceptions. Replace throw with: return Result<T>.WithFailure(\"...\");";
    private static readonly LocalizableString ThrowStatementDescription = "Railway-Oriented Programming requires that methods return Result failures instead of throwing exceptions for control flow.";

    private const string Category = "ROP Compliance";

    private static readonly DiagnosticDescriptor ThrowIfNullRule = new(
        ROPViolationThrowIfNullId,
        ThrowIfNullTitle,
        ThrowIfNullMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ThrowIfNullDescription);

    private static readonly DiagnosticDescriptor ThrowStatementRule = new(
        ROPViolationThrowStatementId,
        ThrowStatementTitle,
        ThrowStatementMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ThrowStatementDescription);

    /// <summary>
    /// Gets the diagnostics produced by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ThrowIfNullRule, ThrowStatementRule);

    /// <summary>
    /// Initializes analysis and registers syntax node actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself (to avoid noise during development)
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

        // Check if it's ArgumentNullException.ThrowIfNull
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var name = memberAccess.Name.Identifier.Text;
        if (name != "ThrowIfNull")
        {
            return;
        }

        // Check if it's ArgumentNullException.ThrowIfNull
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var containingType = methodSymbol.ContainingType;
        if (containingType?.Name != "ArgumentNullException" ||
            containingType.ContainingNamespace?.ToDisplayString() != "System")
        {
            return;
        }

        // Check if we're in an extension method that returns Result
        var containingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod == null)
        {
            return;
        }

        // Check if it's an extension method
        if (!containingMethod.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
        {
            return;
        }

        // Check if first parameter has 'this' modifier (extension method)
        var firstParam = containingMethod.ParameterList.Parameters.FirstOrDefault();
        if (firstParam == null || !firstParam.Modifiers.Any(m => m.IsKind(SyntaxKind.ThisKeyword)))
        {
            return;
        }

        // Check if method returns Result or Result<T>
        var returnType = context.SemanticModel.GetTypeInfo(containingMethod.ReturnType).Type;
        if (!IsResultType(returnType))
        {
            return;
        }

        // Get the parameter name from ThrowIfNull argument
        var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (argument?.Expression is IdentifierNameSyntax identifier)
        {
            var paramName = identifier.Identifier.Text;
            var diagnostic = Diagnostic.Create(
                ThrowIfNullRule,
                invocation.GetLocation(),
                paramName);
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            var diagnostic = Diagnostic.Create(
                ThrowIfNullRule,
                invocation.GetLocation(),
                "parameter");
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
        // Exception: Allow test assemblies (TestAssembly) to be analyzed for testing purposes
        var asmName = context.Compilation.AssemblyName ?? string.Empty;
        if (asmName.StartsWith("IndQuestResults", System.StringComparison.OrdinalIgnoreCase) &&
            !asmName.Equals("TestAssembly", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.Node is not ThrowStatementSyntax throwStatement)
        {
            return;
        }

        // Check if we're in a method that returns Result
        var containingMethod = throwStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod == null)
        {
            return;
        }

        // Check if method returns Result or Result<T>
        var returnType = context.SemanticModel.GetTypeInfo(containingMethod.ReturnType).Type;
        if (!IsResultType(returnType))
        {
            return;
        }

        // Skip if it's rethrowing (throw; without expression)
        if (throwStatement.Expression == null)
        {
            return;
        }

        // Skip if it's in a catch block (rethrowing is acceptable)
        var catchClause = throwStatement.FirstAncestorOrSelf<CatchClauseSyntax>();
        if (catchClause != null)
        {
            return;
        }

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            ThrowStatementRule,
            throwStatement.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsResultType(ITypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        // Check for Result or Result<T>
        if (type.Name == "Result" &&
            type.ContainingNamespace?.ToDisplayString()?.Contains("IndQuestResults") == true)
        {
            return true;
        }

        // Check for Task<Result<T>> - unwrap the Task
        if (type.Name == "Task" && type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var taskTypeArg = namedType.TypeArguments.FirstOrDefault();
            if (taskTypeArg?.Name == "Result" &&
                taskTypeArg.ContainingNamespace?.ToDisplayString()?.Contains("IndQuestResults") == true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIndQuestResultsLibrary(SyntaxNodeAnalysisContext context)
    {
        var assemblyName = context.Compilation.AssemblyName ?? string.Empty;
        return assemblyName.StartsWith("IndQuestResults", System.StringComparison.OrdinalIgnoreCase);
    }
}

