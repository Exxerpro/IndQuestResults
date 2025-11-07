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
        if (asmName.StartsWith("IndQuestResults", StringComparison.OrdinalIgnoreCase) &&
            !asmName.Equals("TestAssembly", StringComparison.OrdinalIgnoreCase))
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

        // Check if exception is passed as a parameter (not just used in arguments)
        // We need to check if the exception is passed as the exception parameter,
        // not just if it's used in string concatenation (e.g., ex.Message)
        var exceptionPassedAsParameter = false;
        
        // First, try to determine which parameter position corresponds to the exception parameter
        // by checking the method signature
        var exceptionParameterIndex = -1;
        
        if (methodSymbol.Parameters.Any(p => p.Name == "exception"))
        {
            exceptionParameterIndex = methodSymbol.Parameters
                .Select((p, i) => new { p, i })
                .FirstOrDefault(x => x.p.Name == "exception")?.i ?? -1;
        }
        else if (methodSymbol.Parameters.Length == 1 && methodSymbol.Parameters[0].Type.Name == "Exception")
        {
            exceptionParameterIndex = 0;
        }
        else if (methodSymbol.Parameters.Length >= 2)
        {
            // Check if any parameter is of type Exception (works for 2+ parameter methods)
            var exceptionParam = methodSymbol.Parameters
                .Select((p, i) => new { p, i })
                .FirstOrDefault(x => x.p.Type.Name == "Exception");
            if (exceptionParam != null)
            {
                exceptionParameterIndex = exceptionParam.i;
            }
        }

        // Check if exception is passed as a parameter
        if (exceptionParameterIndex >= 0 && exceptionParameterIndex < arguments.Count)
        {
            var exceptionArg = arguments[exceptionParameterIndex];
            if (exceptionArg.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.Text == exceptionVariable)
            {
                exceptionPassedAsParameter = true;
            }
        }

        // Check for named parameter "exception"
        if (!exceptionPassedAsParameter)
        {
            exceptionPassedAsParameter = arguments.Any(arg =>
                arg.NameColon != null &&
                arg.NameColon.Name.Identifier.Text == "exception" &&
                arg.Expression is IdentifierNameSyntax exprIdentifier &&
                exprIdentifier.Identifier.Text == exceptionVariable);
        }

        // Also check if exception is used anywhere in the arguments (e.g., ex.Message in string concatenation)
        // This is used to determine if we should report the diagnostic
        var exceptionUsedInArguments = arguments.Any(arg =>
        {
            // Check for direct identifier usage: ex
            if (arg.Expression.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Any(id => id.Identifier.Text == exceptionVariable))
            {
                return true;
            }
            
            // Check for member access: ex.Message, ex.ToString(), etc.
            if (arg.Expression.DescendantNodesAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .Any(memberAccess => memberAccess.Expression is IdentifierNameSyntax identifier &&
                                    identifier.Identifier.Text == exceptionVariable))
            {
                return true;
            }
            
            return false;
        });

        // Check if method signature supports exception parameter
        // WithFailure has multiple overloads - check ALL overloads, not just the resolved one
        var hasExceptionParameter = methodSymbol.Parameters.Any(p => p.Name == "exception");

        // Also check if the method has 1+ parameters with Exception type
        // This covers single-parameter methods, 2-parameter methods (e.g., WithFailure(string, Exception)),
        // and 3+ parameter methods (e.g., WithFailure(string, T, Exception))
        var parameterCount = methodSymbol.Parameters.Length;
        var hasExceptionOverload = hasExceptionParameter ||
                                   (parameterCount == 1 && methodSymbol.Parameters[0].Type.Name == "Exception") ||
                                   (parameterCount >= 2 && methodSymbol.Parameters.Any(p => p.Type.Name == "Exception"));

        // If the resolved overload doesn't support exception, check all overloads
        if (!hasExceptionOverload)
        {
            // Get all overloads of WithFailure from the containing type
            var allOverloads = containingType.GetMembers("WithFailure")
                .OfType<IMethodSymbol>()
                .ToList();

            // Check if any overload has exception parameter
            hasExceptionOverload = allOverloads.Any(overload =>
            {
                var hasExceptionParam = overload.Parameters.Any(p => p.Name == "exception");
                var paramCount = overload.Parameters.Length;
                return hasExceptionParam ||
                       (paramCount == 1 && overload.Parameters[0].Type.Name == "Exception") ||
                       (paramCount >= 2 && overload.Parameters.Any(p => p.Type.Name == "Exception"));
            });
        }

        // If the method supports exception parameter but it's not provided, report diagnostic
        // Report IQR301 when:
        // 1. The method supports an exception parameter
        // 2. The exception is not passed as a parameter
        // 3. The exception is used in arguments (e.g., ex.Message in string concatenation)
        // Note: We only report when exception is actually used in arguments to avoid false positives
        // when exception variable exists but is never used
        if (hasExceptionOverload && !exceptionPassedAsParameter && exceptionUsedInArguments)
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
        if (asmName.StartsWith("IndQuestResults", StringComparison.OrdinalIgnoreCase) &&
            !asmName.Equals("TestAssembly", StringComparison.OrdinalIgnoreCase))
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

        // Check if exception variable is used in the catch block
        // Check both direct identifier usage and member access (e.g., ex.Message)
        var exceptionUsed = catchClause.Block?.DescendantNodes()
            .Any(node =>
            {
                // Direct identifier usage: ex
                if (node is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == exceptionVariable)
                {
                    return true;
                }
                
                // Member access: ex.Message, ex.ToString(), etc.
                if (node is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is IdentifierNameSyntax memberIdentifier &&
                    memberIdentifier.Identifier.Text == exceptionVariable)
                {
                    return true;
                }
                
                return false;
            }) ?? false;

        if (exceptionUsed && !hasWithFailure)
        {
            // Exception is used but not passed to WithFailure - report diagnostic
            // This is IQR302: exception is used but not passed to WithFailure
            // Report on the try block to match test expectations
            var tryStatement = catchClause.FirstAncestorOrSelf<TryStatementSyntax>();
            var location = tryStatement?.TryKeyword.GetLocation() ?? catchClause.GetLocation();
            var diagnostic = Diagnostic.Create(
                ExceptionNotPreservedRule,
                location);
            context.ReportDiagnostic(diagnostic);
        }
        else if (exceptionUsed && hasWithFailure)
        {
            // Check if exception is passed to WithFailure
            var withFailureInvocations = catchClause.Block?.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => inv.Expression is MemberAccessExpressionSyntax memberAccess &&
                             memberAccess.Name.Identifier.Text == "WithFailure")
                .ToList() ?? [];

            var exceptionPassedToWithFailure = withFailureInvocations.Any(inv =>
            {
                var args = inv.ArgumentList.Arguments;
                
                // Check for named parameter "exception"
                if (args.Any(arg =>
                    arg.NameColon != null &&
                    arg.NameColon.Name.Identifier.Text == "exception" &&
                    arg.Expression is IdentifierNameSyntax namedIdentifier &&
                    namedIdentifier.Identifier.Text == exceptionVariable))
                {
                    return true;
                }
                
                // Check if exception is passed as a direct identifier (as a parameter, not just used in arguments)
                // We need to check if it's passed as the exception parameter, not just used in string concatenation
                var symbolInfo = context.SemanticModel.GetSymbolInfo(inv, context.CancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    // Find the exception parameter index
                    var exceptionParamIndex = -1;
                    if (methodSymbol.Parameters.Any(p => p.Name == "exception"))
                    {
                        exceptionParamIndex = methodSymbol.Parameters
                            .Select((p, i) => new { p, i })
                            .FirstOrDefault(x => x.p.Name == "exception")?.i ?? -1;
                    }
                    else if (methodSymbol.Parameters.Length == 1 && methodSymbol.Parameters[0].Type.Name == "Exception")
                    {
                        exceptionParamIndex = 0;
                    }
                    else if (methodSymbol.Parameters.Length >= 2)
                    {
                        // Check if any parameter is of type Exception (works for 2+ parameter methods)
                        var exceptionParam = methodSymbol.Parameters
                            .Select((p, i) => new { p, i })
                            .FirstOrDefault(x => x.p.Type.Name == "Exception");
                        if (exceptionParam != null)
                        {
                            exceptionParamIndex = exceptionParam.i;
                        }
                    }
                    
                    // Check if exception is passed at the exception parameter position
                    if (exceptionParamIndex >= 0 && exceptionParamIndex < args.Count)
                    {
                        var exceptionArg = args[exceptionParamIndex];
                        if (exceptionArg.Expression is IdentifierNameSyntax identifier &&
                            identifier.Identifier.Text == exceptionVariable)
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            });

            // Only report IQR302 if exception is used but not passed to WithFailure
            // AND the exception is not used in WithFailure arguments (which would trigger IQR301)
            // Check if exception is used in WithFailure arguments
            var exceptionUsedInWithFailureArgs = withFailureInvocations.Any(inv =>
                inv.ArgumentList.Arguments.Any(arg =>
                    arg.Expression.DescendantNodesAndSelf()
                        .OfType<IdentifierNameSyntax>()
                        .Any(id => id.Identifier.Text == exceptionVariable)));

            // If exception is used in WithFailure arguments, IQR301 will be reported by AnalyzeInvocation
            // So we should not report IQR302 here
            if (!exceptionPassedToWithFailure && !exceptionUsedInWithFailureArgs)
            {
                // Report on the try block to match test expectations
                var tryStatement = catchClause.FirstAncestorOrSelf<TryStatementSyntax>();
                var location = tryStatement?.TryKeyword.GetLocation() ?? catchClause.GetLocation();
                var diagnostic = Diagnostic.Create(
                    ExceptionNotPreservedRule,
                    location);
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
}
