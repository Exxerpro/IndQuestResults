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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MultipleValueAccessRule,
            UnnecessaryToListRule,
            RepeatedErrorFormattingRule,
            BoxingInHotPathRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzePropertyBody, SyntaxKind.PropertyDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeToListCall, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
    {
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

        // Check for boxing in hot paths
        CheckForBoxing(method, context);
    }

    private static void AnalyzePropertyBody(SyntaxNodeAnalysisContext context)
    {
        var property = (PropertyDeclarationSyntax)context.Node;
        
        // Check getter for performance issues
        if (property.AccessorList != null)
        {
            var getter = property.AccessorList.Accessors
                .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            
            if (getter?.Body != null || getter?.ExpressionBody != null)
            {
                // Check for repeated error formatting in property getters
                var stringJoins = getter.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(inv => IsStringJoinOnErrors(inv, context.SemanticModel))
                    .ToList();

                if (stringJoins.Count > 0)
                {
                    var diagnostic = Diagnostic.Create(
                        RepeatedErrorFormattingRule,
                        stringJoins[0].GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static void AnalyzeToListCall(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.Text != "ToList")
            return;

        // Check if calling ToList on Result.Errors
        if (memberAccess.Expression is MemberAccessExpressionSyntax errorAccess &&
            errorAccess.Name.Identifier.Text == "Errors")
        {
            var type = context.SemanticModel.GetTypeInfo(errorAccess.Expression).Type;
            if (IsResultType(type))
            {
                var diagnostic = Diagnostic.Create(
                    UnnecessaryToListRule,
                    invocation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static void CheckForBoxing(MethodDeclarationSyntax method, SyntaxNodeAnalysisContext context)
    {
        // Look for Result<T> creation with value types
        var resultCreations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => IsResultCreation(inv, context.SemanticModel));

        foreach (var creation in resultCreations)
        {
            var typeArg = GetResultTypeArgument(creation, context.SemanticModel);
            if (typeArg?.IsValueType == true && creation.ArgumentList?.Arguments.Count > 0)
            {
                var firstArg = creation.ArgumentList.Arguments[0];
                var argType = context.SemanticModel.GetTypeInfo(firstArg.Expression).Type;
                
                // Check if we're in a loop or frequently called method
                if (IsInHotPath(creation))
                {
                    var diagnostic = Diagnostic.Create(
                        BoxingInHotPathRule,
                        creation.GetLocation(),
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

    private static bool IsStringJoinOnErrors(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol?.Name != "Join" || symbol.ContainingType?.Name != "String")
            return false;

        // Check if any argument is Result.Errors
        return invocation.ArgumentList?.Arguments.Any(arg =>
        {
            if (arg.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "Errors")
            {
                var type = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
                return IsResultType(type);
            }
            return false;
        }) == true;
    }

    private static bool IsResultCreation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        return symbol?.Name == "Success" && 
               symbol.ContainingType?.Name == "Result" &&
               symbol.ContainingType.ContainingNamespace?.ToString()?.Contains("IndQuestResults") == true;
    }

    private static ITypeSymbol? GetResultTypeArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol?.ContainingType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return namedType.TypeArguments.FirstOrDefault();
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