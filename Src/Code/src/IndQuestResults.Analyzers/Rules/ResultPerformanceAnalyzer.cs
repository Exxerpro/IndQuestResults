using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IndQuestResults.Analyzers.Rules;

/// <summary>
/// Analyzer for Result&lt;T&gt; performance anti-patterns and optimization opportunities.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ResultPerformanceAnalyzer : DiagnosticAnalyzer
{
    public const string MultipleValueAccessId = "IQR101";
    public const string UnnecessaryToListId = "IQR102";
    public const string RepeatedErrorFormattingId = "IQR103";
    public const string BoxingInHotPathId = "IQR104";

    private static readonly DiagnosticDescriptor MultipleValueAccessRule = new(
        MultipleValueAccessId,
        "Multiple Result.Value accesses",
        "Result.Value is accessed multiple times. Cache it in a local variable",
        "Performance",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Accessing Result.Value multiple times may cause redundant null checks. Cache the value.");

    private static readonly DiagnosticDescriptor UnnecessaryToListRule = new(
        UnnecessaryToListId,
        "Unnecessary ToList() on Result.Errors",
        "Result.Errors.ToList() is unnecessary. Errors is already a materialized collection",
        "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Result.Errors is already an IEnumerable that's materialized. ToList() creates unnecessary allocations.");

    private static readonly DiagnosticDescriptor RepeatedErrorFormattingRule = new(
        RepeatedErrorFormattingId,
        "Repeated error string formatting",
        "Error string is being formatted multiple times. Consider caching the formatted result",
        "Performance",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Formatting error strings repeatedly can be expensive. Cache formatted strings for reuse.");

    private static readonly DiagnosticDescriptor BoxingInHotPathRule = new(
        BoxingInHotPathId,
        "Value type boxing in Result operations",
        "Value type '{0}' is being boxed in Result operation. Consider using struct-specific overloads",
        "Performance",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Boxing value types in hot paths can cause performance issues and GC pressure.");

    /// <summary>
    /// Gets the set of diagnostics that this analyzer can produce.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MultipleValueAccessRule,
            UnnecessaryToListRule,
            RepeatedErrorFormattingRule,
            BoxingInHotPathRule);

    /// <summary>
    /// Initializes analyzer actions and registers syntax node handlers.
    /// </summary>
    /// <param name="context">The analysis context used to register actions.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
        if (IsIndQuestResultsLibrary(context))
            return;
            
        var method = (MethodDeclarationSyntax)context.Node;
        if (method.Body == null && method.ExpressionBody == null)
            return;

        var semanticModel = context.SemanticModel;

        // Look for multiple .Value accesses on the same Result
        var valueAccesses = method.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Name.Identifier.Text == "Value")
            .ToList();

        if (valueAccesses.Count < 2)
            return;

        // Group by the Result instance being accessed
        var groups = valueAccesses
            .Select(access => new
            {
                Access = access,
                Symbol = GetResultSymbol(access.Expression, semanticModel)
            })
            .Where(x => x.Symbol != null && IsResultType(x.Symbol))
            .GroupBy(x => x.Symbol, SymbolEqualityComparer.Default);

        foreach (var group in groups)
        {
            var accesses = group.ToList();
            if (accesses.Count >= 2)
            {
                // Report on the second access
                var diagnostic = Diagnostic.Create(
                    MultipleValueAccessRule,
                    accesses[1].Access.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        // Skip analysis for IndQuestResults library itself
        if (IsIndQuestResultsLibrary(context))
            return;
            
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        // Check for ToList calls on Result.Errors
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "ToList")
        {
            // Check if calling ToList on Result.Errors
            if (memberAccess.Expression is MemberAccessExpressionSyntax errorAccess &&
                errorAccess.Name.Identifier.Text == "Errors")
            {
                var type = context.SemanticModel.GetTypeInfo(errorAccess.Expression).Type;
                if (IsResultType(type))
                {
                    var diagnostic = Diagnostic.Create(
                        UnnecessaryToListRule,
                        memberAccess.Name.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
        
        // Check for error formatting calls
        if (IsErrorFormattingCall(invocation, context.SemanticModel))
        {
            var diagnostic = Diagnostic.Create(
                RepeatedErrorFormattingRule,
                invocation.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
        
        // Check for boxing in Result creation calls
        if (IsResultCreation(invocation, context.SemanticModel))
        {
            var typeArg = GetResultTypeArgument(invocation, context.SemanticModel);
            if (typeArg?.IsValueType == true && invocation.ArgumentList?.Arguments.Count > 0)
            {
                // Check if we're in a hot path (loop or LINQ query)
                if (IsInHotPath(invocation))
                {
                    // Report location at the entire invocation for better visibility
                    var location = invocation.GetLocation();
                    
                    var diagnostic = Diagnostic.Create(
                        BoxingInHotPathRule,
                        location,
                        typeArg.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }


    private static ISymbol? GetResultSymbol(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(expression);
        return symbolInfo.Symbol ?? 
               semanticModel.GetTypeInfo(expression).Type;
    }

    private static bool IsResultType(ISymbol? symbol)
    {
        ITypeSymbol? type = symbol as ITypeSymbol;
        if (type == null && symbol is IPropertySymbol property)
            type = property.Type;
        if (type == null && symbol is IFieldSymbol field)
            type = field.Type;
        if (type == null && symbol is ILocalSymbol local)
            type = local.Type;
        
        return type?.Name == "Result" && 
               type.ContainingNamespace?.ToString()?.Contains("IndQuestResults") == true;
    }

    private static bool IsErrorFormattingCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        // Check for string.Join calls
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "Join")
        {
            // Much simpler check - just look for any argument that contains ".Errors"
            var invocationText = invocation.ToString();
            if (invocationText.Contains(".Errors"))
            {
                return true;
            }
        }
        
        // Also check for other error formatting patterns like ToString() on errors
        var invocationText2 = invocation.ToString();
        if (invocationText2.Contains(".Errors") && 
            (invocationText2.Contains("ToString") || invocationText2.Contains("Join")))
        {
            return true;
        }
        
        return false;
    }

    private static bool IsIndQuestResultsLibrary(SyntaxNodeAnalysisContext context)
    {
        // Check if we're analyzing the IndQuestResults library itself
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        return assemblyName?.Contains("IndQuestResults") == true;
    }
    
    private static bool ContainsErrorsAccess(SyntaxNode node)
    {
        // Check if this node or any descendant accesses ".Errors"
        if (node is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "Errors")
        {
            return true;
        }
        
        // Recursively check child nodes
        return node.ChildNodes().Any(ContainsErrorsAccess);
    }

    private static bool IsResultCreation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol?.Name == "Success" && 
            symbol.ContainingType?.Name == "Result" &&
            symbol.ContainingType.ContainingNamespace?.ToString()?.Contains("IndQuestResults") == true)
        {
            return true;
        }

        // Also check syntactically for Result<T>.Success() pattern
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "Success")
        {
            var expression = memberAccess.Expression;
            
            // Check for Result<T> pattern
            if (expression is GenericNameSyntax genericName &&
                genericName.Identifier.Text == "Result")
            {
                return true;
            }
            
            // Check for Result pattern (non-generic)
            if (expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.Text == "Result")
            {
                return true;
            }
        }

        return false;
    }

    private static ITypeSymbol? GetResultTypeArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol?.ContainingType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return namedType.TypeArguments.FirstOrDefault();
        }

        // Fall back to syntactic analysis for Result<T>.Success() pattern
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "Success" &&
            memberAccess.Expression is GenericNameSyntax genericName &&
            genericName.Identifier.Text == "Result" &&
            genericName.TypeArgumentList?.Arguments.Count > 0)
        {
            var typeArgument = genericName.TypeArgumentList.Arguments[0];
            return semanticModel.GetTypeInfo(typeArgument).Type;
        }

        return null;
    }

    private static bool IsInHotPath(SyntaxNode node)
    {
        // Simple heuristic: check if in a loop
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is ForStatementSyntax ||
                parent is ForEachStatementSyntax ||
                parent is WhileStatementSyntax ||
                parent is DoStatementSyntax)
            {
                return true;
            }
            
            // Also check if in a LINQ query
            if (parent is QueryExpressionSyntax ||
                parent is LambdaExpressionSyntax)
            {
                return true;
            }
            
            parent = parent.Parent;
        }
        
        return false;
    }

}