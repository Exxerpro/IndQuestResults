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
}