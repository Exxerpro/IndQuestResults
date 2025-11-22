using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IndQuestResults.Analyzers.Rules;

/// <summary>
/// Analyzer for common Result&lt;T&gt; pattern violations and best practice enforcement.
///
/// TODO v2.0: Add support for modern C# collection patterns:
/// - Collection expressions: [1, 2, 3] syntax
/// - Collection initializers with target-typed new: new() { 1, 2, 3 }
/// - List patterns in switch expressions: list is [var first, .., var last]
/// - Spread operator: [..array1, ..array2]
///
/// TODO v2.0: Enhanced reference type null safety:
/// - Detect Result&lt;T&gt; where T is a reference type
/// - Require both IsSuccess AND null checks for reference types
/// - Report warnings (not errors) when only IsSuccess is checked
///
/// TODO v2.0: Pattern matching enhancements:
/// - Extended property patterns: { IsSuccess: true, Value.Length: > 0 }
/// - Relational patterns with IsSuccess
/// - List patterns for collection results
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ResultPatternAnalyzer : DiagnosticAnalyzer
{
    // Diagnostic IDs
    public const string UnhandledResultId = "IQR001";
    public const string DirectValueAccessId = "IQR002";
    public const string ThrowingInResultId = "IQR003";
    public const string NullResultId = "IQR004";
    public const string MixingPatternsId = "IQR005";

    // Diagnostic descriptors
    private static readonly DiagnosticDescriptor UnhandledResultRule = new(
        UnhandledResultId,
        "Unhandled Result<T>",
        "Result<T> return value is not handled. Use IsSuccess check, Match, or Map",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Result<T> values must be checked for success/failure before use.");

    private static readonly DiagnosticDescriptor DirectValueAccessRule = new(
        DirectValueAccessId,
        "Direct Value access without success check",
        "Accessing Result.Value without checking IsSuccess first",
        "Safety",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Always check IsSuccess before accessing Value to prevent null reference exceptions.");

    private static readonly DiagnosticDescriptor ThrowingInResultRule = new(
        ThrowingInResultId,
        "Exception thrown in Result operation",
        "Avoid throwing exceptions in Result operations. Return Result.WithFailure instead",
        "Pattern",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Result pattern is designed to avoid exceptions. Use Result.WithFailure for errors.");

    private static readonly DiagnosticDescriptor NullResultRule = new(
        NullResultId,
        "Null Result<T> detected",
        "Result<T> should never be null. Use Result.WithFailure for error cases",
        "Safety",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Returning null Result<T> breaks the pattern. Always return a valid Result instance.");

    private static readonly DiagnosticDescriptor MixingPatternsRule = new(
        MixingPatternsId,
        "Mixing Result<T> with nullable return",
        "Method returns both Result<T> and null. Use consistent Result pattern",
        "Pattern",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Maintain consistency by always returning Result<T> instead of mixing with nullable.");

    /// <summary>
    /// Gets the set of diagnostics that this analyzer can produce.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            UnhandledResultRule,
            DirectValueAccessRule,
            ThrowingInResultRule,
            NullResultRule,
            MixingPatternsRule);

    /// <summary>
    /// Initializes analyzer actions and registers syntax node handlers.
    /// </summary>
    /// <param name="context">The analysis context used to register actions.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Check for unhandled Result<T> returns
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        
        // Check for direct Value access
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        
        // Check for throw statements in Result methods
        context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
        
        // Check for null returns in Result methods
        context.RegisterSyntaxNodeAction(AnalyzeReturnStatement, SyntaxKind.ReturnStatement);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
        if (IsIndQuestResultsLibrary(context))
            return;
            
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        
        if (symbolInfo.Symbol is not IMethodSymbol method)
            return;

        // Check if method returns Result<T>
        if (!IsResultType(method.ReturnType))
            return;

        // Check if the result is being used
        if (invocation.Parent is ExpressionStatementSyntax)
        {
            // Result is discarded - this is a problem
            var diagnostic = Diagnostic.Create(
                UnhandledResultRule,
                invocation.GetLocation(),
                method.Name);
            context.ReportDiagnostic(diagnostic);
        }
        else if (invocation.Parent is AwaitExpressionSyntax awaitExpr && 
                 awaitExpr.Parent is ExpressionStatementSyntax)
        {
            // Await result is discarded - this is a problem, point to await
            var diagnostic = Diagnostic.Create(
                UnhandledResultRule,
                awaitExpr.GetLocation(),
                method.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
        if (IsIndQuestResultsLibrary(context))
            return;
            
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        
        // Check if accessing .Value on a Result<T>
        if (memberAccess.Name.Identifier.Text != "Value")
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
        if (!IsResultType(typeInfo.Type))
            return;

        // Check if there's an IsSuccess check in scope
        if (!HasSuccessCheckInScope(memberAccess, context.SemanticModel))
        {
            var diagnostic = Diagnostic.Create(
                DirectValueAccessRule,
                memberAccess.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
        if (IsIndQuestResultsLibrary(context))
            return;
            
        var throwStatement = (ThrowStatementSyntax)context.Node;
        
        // Find containing method
        var method = throwStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method == null)
            return;

        // Check if method returns Result<T>
        var returnType = context.SemanticModel.GetTypeInfo(method.ReturnType).Type;
        if (!IsResultType(returnType))
            return;

        // Check if this is in a try-catch (which might be ok)
        var tryStatement = throwStatement.FirstAncestorOrSelf<TryStatementSyntax>();
        if (tryStatement != null)
            return;

        var diagnostic = Diagnostic.Create(
            ThrowingInResultRule,
            throwStatement.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeReturnStatement(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
        if (IsIndQuestResultsLibrary(context))
            return;
            
        var returnStatement = (ReturnStatementSyntax)context.Node;
        if (returnStatement.Expression == null)
            return;

        // Check if returning null
        if (returnStatement.Expression.IsKind(SyntaxKind.NullLiteralExpression) ||
            returnStatement.Expression.IsKind(SyntaxKind.DefaultLiteralExpression))
        {
            // Find containing method
            var method = returnStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return;

            // Check if method returns Result<T>
            var returnType = context.SemanticModel.GetTypeInfo(method.ReturnType).Type;
            if (IsResultType(returnType))
            {
                var diagnostic = Diagnostic.Create(
                    NullResultRule,
                    returnStatement.Expression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsResultType(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        // Check for Result or Result<T>
        if (type.Name == "Result" && 
            type.ContainingNamespace?.ToString()?.Contains("IndQuestResults") == true)
        {
            return true;
        }

        // Check for Task<Result<T>> - unwrap the Task
        if (type.Name == "Task" && type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var taskTypeArg = namedType.TypeArguments.FirstOrDefault();
            if (taskTypeArg?.Name == "Result" && 
                taskTypeArg.ContainingNamespace?.ToString()?.Contains("IndQuestResults") == true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIndQuestResultsLibrary(SyntaxNodeAnalysisContext context)
    {
        // Check if we're analyzing the IndQuestResults library itself
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        return assemblyName?.Contains("IndQuestResults") == true;
    }

    private static bool HasSuccessCheckInScope(SyntaxNode node, SemanticModel semanticModel)
    {
        // Check for Match or Map usage which implicitly handles success
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is InvocationExpressionSyntax invocation)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var name = memberAccess.Name.Identifier.Text;
                    if (name == "Match" || name == "Map" || name == "Bind")
                    {
                        return true;
                    }
                }
            }

            // Check if we're inside an if statement that checks IsSuccess
            if (parent is IfStatementSyntax ifStatement)
            {
                if (IsSuccessCheckCondition(ifStatement.Condition, semanticModel))
                {
                    return true;
                }
            }

            // Check if we're inside a switch expression with IsSuccess pattern
            if (parent is SwitchExpressionSyntax switchExpr)
            {
                if (HasSuccessPatternInSwitch(switchExpr, semanticModel))
                {
                    return true;
                }
            }

            parent = parent.Parent;
        }

        // Check for early return patterns and Shouldly assertions
        if (HasEarlyReturnAfterFailureCheck(node, semanticModel) ||
            HasShouldlySuccessAssertion(node, semanticModel))
        {
            return true;
        }

        return false;
    }

    private static bool IsSuccessCheckCondition(ExpressionSyntax condition, SemanticModel semanticModel)
    {
        // Handle simple case: result.IsSuccess
        if (condition is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "IsSuccess")
        {
            var type = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
            return IsResultType(type);
        }

        // Handle parenthesized expressions: (result.IsSuccess)
        if (condition is ParenthesizedExpressionSyntax parenthesized)
        {
            return IsSuccessCheckCondition(parenthesized.Expression, semanticModel);
        }

        // Handle binary expressions with IsSuccess checks
        if (condition is BinaryExpressionSyntax binary)
        {
            return IsSuccessCheckCondition(binary.Left, semanticModel) ||
                   IsSuccessCheckCondition(binary.Right, semanticModel);
        }

        return false;
    }

    private static bool HasSuccessPatternInSwitch(SwitchExpressionSyntax switchExpr, SemanticModel semanticModel)
    {
        foreach (var arm in switchExpr.Arms)
        {
            if (IsSuccessPattern(arm.Pattern, semanticModel))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsSuccessPattern(PatternSyntax pattern, SemanticModel semanticModel)
    {
        // Handle property patterns like { IsSuccess: true }
        if (pattern is RecursivePatternSyntax recursive)
        {
            if (recursive.PropertyPatternClause != null)
            {
                foreach (var subpattern in recursive.PropertyPatternClause.Subpatterns)
                {
                    if (subpattern.NameColon?.Name.Identifier.Text == "IsSuccess")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool HasEarlyReturnAfterFailureCheck(SyntaxNode node, SemanticModel semanticModel)
    {
        // Find the containing method
        var containingMethod = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod?.Body == null)
            return false;

        // Get the member access expression for result.Value
        if (node is not MemberAccessExpressionSyntax memberAccess)
            return false;

        // Get the result variable identifier
        var resultIdentifier = GetResultIdentifier(memberAccess.Expression);
        if (resultIdentifier == null)
            return false;

        // Check all statements in the method for IsFailure early return
        return HasIsFailureEarlyReturn(containingMethod.Body, resultIdentifier, node, semanticModel);
    }

    private static string? GetResultIdentifier(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    private static bool HasIsFailureEarlyReturn(BlockSyntax block, string resultIdentifier, SyntaxNode valueAccessNode, SemanticModel semanticModel)
    {
        foreach (var statement in block.Statements)
        {
            // If we've reached or passed the value access, stop looking
            if (statement.Span.End >= valueAccessNode.SpanStart)
                break;

            // Check for if (result.IsFailure) return;
            if (statement is IfStatementSyntax ifStatement)
            {
                if (IsFailureCheckForIdentifier(ifStatement.Condition, resultIdentifier, semanticModel) &&
                    ContainsReturn(ifStatement.Statement))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsFailureCheckForIdentifier(ExpressionSyntax condition, string identifier, SemanticModel semanticModel)
    {
        // Handle simple case: result.IsFailure
        if (condition is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "IsFailure")
        {
            if (memberAccess.Expression is IdentifierNameSyntax ident &&
                ident.Identifier.Text == identifier)
            {
                var type = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
                return IsResultType(type);
            }
        }

        // Handle parenthesized expressions: (result.IsFailure)
        if (condition is ParenthesizedExpressionSyntax parenthesized)
        {
            return IsFailureCheckForIdentifier(parenthesized.Expression, identifier, semanticModel);
        }

        return false;
    }

    private static bool IsFailureCheckCondition(ExpressionSyntax condition, SemanticModel semanticModel)
    {
        // Handle simple case: result.IsFailure
        if (condition is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "IsFailure")
        {
            var type = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
            return IsResultType(type);
        }

        // Handle parenthesized expressions: (result.IsFailure)
        if (condition is ParenthesizedExpressionSyntax parenthesized)
        {
            return IsFailureCheckCondition(parenthesized.Expression, semanticModel);
        }

        // Handle binary expressions with IsFailure checks
        if (condition is BinaryExpressionSyntax binary)
        {
            return IsFailureCheckCondition(binary.Left, semanticModel) ||
                   IsFailureCheckCondition(binary.Right, semanticModel);
        }

        return false;
    }

    private static bool ContainsReturn(StatementSyntax statement)
    {
        if (statement is ReturnStatementSyntax)
            return true;

        if (statement is BlockSyntax block)
        {
            return block.Statements.Any(s => s is ReturnStatementSyntax);
        }

        return false;
    }

    private static bool HasShouldlySuccessAssertion(SyntaxNode node, SemanticModel semanticModel)
    {
        // Find the containing method
        var containingMethod = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod?.Body == null)
            return false;

        // Get the member access expression for result.Value
        if (node is not MemberAccessExpressionSyntax memberAccess)
            return false;

        // Get the result variable identifier
        var resultIdentifier = GetResultIdentifier(memberAccess.Expression);
        if (resultIdentifier == null)
            return false;

        // Check all statements in the method for Shouldly success assertion
        return HasShouldlyIsSuccessAssertion(containingMethod.Body, resultIdentifier, node, semanticModel);
    }

    private static bool HasShouldlyIsSuccessAssertion(BlockSyntax block, string resultIdentifier, SyntaxNode valueAccessNode, SemanticModel semanticModel)
    {
        foreach (var statement in block.Statements)
        {
            // If we've reached or passed the value access, stop looking
            if (statement.Span.End >= valueAccessNode.SpanStart)
                break;

            // Check for result.IsSuccess.ShouldBeTrue()
            if (statement is ExpressionStatementSyntax expressionStatement)
            {
                if (IsShouldlyIsSuccessAssertionForIdentifier(expressionStatement.Expression, resultIdentifier, semanticModel))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsShouldlyIsSuccessAssertionForIdentifier(ExpressionSyntax expression, string identifier, SemanticModel semanticModel)
    {
        // Pattern: result.IsSuccess.ShouldBeTrue()
        if (expression is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax shouldlyCall)
            {
                // Check if calling ShouldBeTrue
                if (shouldlyCall.Name.Identifier.Text == "ShouldBeTrue")
                {
                    // Check if the expression is result.IsSuccess
                    if (shouldlyCall.Expression is MemberAccessExpressionSyntax isSuccessAccess)
                    {
                        if (isSuccessAccess.Name.Identifier.Text == "IsSuccess" &&
                            isSuccessAccess.Expression is IdentifierNameSyntax ident &&
                            ident.Identifier.Text == identifier)
                        {
                            var type = semanticModel.GetTypeInfo(isSuccessAccess.Expression).Type;
                            return IsResultType(type);
                        }
                    }
                }
            }
        }

        return false;
    }
}