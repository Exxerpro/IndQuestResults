using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IndQuestResults.Analyzers.Rules;

/// <summary>
/// Analyzer for exception handling compliance in Result operations.
/// Detects missing exception parameters in WithFailure calls and ensures exceptions are properly preserved.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExceptionHandlingComplianceAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID: Missing exception parameter in WithFailure call.
    /// </summary>
    public const string MissingExceptionParameterId = "IQR301";

    /// <summary>
    /// Diagnostic ID: Exception not preserved in catch block.
    /// </summary>
    public const string ExceptionNotPreservedId = "IQR302";

    private static readonly LocalizableString MissingExceptionTitle = "Missing exception parameter in WithFailure call";
    private static readonly LocalizableString MissingExceptionMessageFormat = "WithFailure call in catch block should include exception parameter to preserve stack trace. Use: Result<T>.WithFailure(\"...\", default, ex)";
    private static readonly LocalizableString MissingExceptionDescription = "Exception handling in Result operations should preserve the exception object for better debugging. Always pass the exception to WithFailure in catch blocks.";

    private static readonly LocalizableString ExceptionNotPreservedTitle = "Exception not preserved in catch block";
    private static readonly LocalizableString ExceptionNotPreservedMessageFormat = "Catch block should preserve exception in Result failure. Use: Result<T>.WithFailure(\"...\", default, ex)";
    private static readonly LocalizableString ExceptionNotPreservedDescription = "All catch blocks in Result-returning methods should preserve exceptions by passing them to WithFailure.";

    private const string Category = "Exception Handling";

    private static readonly DiagnosticDescriptor MissingExceptionRule = new(
        MissingExceptionParameterId,
        MissingExceptionTitle,
        MissingExceptionMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: MissingExceptionDescription);

    private static readonly DiagnosticDescriptor ExceptionNotPreservedRule = new(
        ExceptionNotPreservedId,
        ExceptionNotPreservedTitle,
        ExceptionNotPreservedMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ExceptionNotPreservedDescription);

    /// <summary>
    /// Gets the diagnostics produced by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingExceptionRule, ExceptionNotPreservedRule);

    /// <summary>
    /// Initializes analysis and registers syntax node actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
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

        // Check if it's WithFailure call
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var name = memberAccess.Name.Identifier.Text;
        if (name != "WithFailure")
        {
            return;
        }

        // Check if it's Result.WithFailure or Result<T>.WithFailure
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var containingType = methodSymbol.ContainingType;
        if (containingType?.Name != "Result" ||
            containingType.ContainingNamespace?.ToDisplayString()?.Contains("IndQuestResults") != true)
        {
            return;
        }

        // Check if we're in a catch block
        var catchClause = invocation.FirstAncestorOrSelf<CatchClauseSyntax>();
        if (catchClause == null)
        {
            return;
        }

        // Check if catch block has an exception variable
        var exceptionVariable = catchClause.Declaration?.Identifier.ValueText;
        if (string.IsNullOrEmpty(exceptionVariable))
        {
            // catch (Exception) without variable name - check if exception is used
            return;
        }

        // Check if WithFailure call includes exception parameter
        var arguments = invocation.ArgumentList.Arguments;
        
        // Check if any argument references the exception variable
        var hasExceptionArgument = arguments.Any(arg =>
        {
            if (arg.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.Text == exceptionVariable)
            {
                return true;
            }
            
            // Check for named argument with exception
            if (arg.NameColon != null &&
                arg.NameColon.Name.Identifier.Text == "exception" &&
                arg.Expression is IdentifierNameSyntax exprIdentifier &&
                exprIdentifier.Identifier.Text == exceptionVariable)
            {
                return true;
            }
            
            return false;
        });

        // Check if method signature supports exception parameter
        // WithFailure has multiple overloads - check if any parameter is named "exception"
        var hasExceptionParameter = methodSymbol.Parameters.Any(p => p.Name == "exception");
        
        // Also check if the method has 3+ parameters (error, value, exception pattern)
        // or if it's a single-parameter overload that takes Exception
        var parameterCount = methodSymbol.Parameters.Length;
        var hasExceptionOverload = hasExceptionParameter || 
                                   (parameterCount == 1 && methodSymbol.Parameters[0].Type.Name == "Exception");

        if (hasExceptionOverload && !hasExceptionArgument)
        {
            var diagnostic = Diagnostic.Create(
                MissingExceptionRule,
                invocation.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
        // Exception: Allow test assemblies (TestAssembly) to be analyzed for testing purposes
        var asmName = context.Compilation.AssemblyName ?? string.Empty;
        if (asmName.StartsWith("IndQuestResults", System.StringComparison.OrdinalIgnoreCase) &&
            !asmName.Equals("TestAssembly", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.Node is not CatchClauseSyntax catchClause)
        {
            return;
        }

        // Check if catch block has an exception variable
        var exceptionVariable = catchClause.Declaration?.Identifier.ValueText;
        if (string.IsNullOrEmpty(exceptionVariable))
        {
            return;
        }

        // Check if we're in a method that returns Result
        var containingMethod = catchClause.FirstAncestorOrSelf<MethodDeclarationSyntax>();
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

        // Check if catch block contains a return statement with WithFailure
        var returnStatements = catchClause.Block?.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .ToList();

        if (returnStatements == null || returnStatements.Count == 0)
        {
            return;
        }

        // Check if any return statement uses WithFailure
        var hasWithFailure = returnStatements.Any(returnStmt =>
        {
            if (returnStmt.Expression is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            return memberAccess.Name.Identifier.Text == "WithFailure";
        });

        if (!hasWithFailure)
        {
            // Check if exception variable is used in the catch block
            var exceptionUsed = catchClause.Block?.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any(id => id.Identifier.Text == exceptionVariable) ?? false;

            if (exceptionUsed)
            {
                var diagnostic = Diagnostic.Create(
                    ExceptionNotPreservedRule,
                    catchClause.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
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

