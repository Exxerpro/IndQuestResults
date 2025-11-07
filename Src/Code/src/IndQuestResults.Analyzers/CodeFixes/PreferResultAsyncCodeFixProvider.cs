using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IndQuestResults.Analyzers.CodeFixes;

/// <summary>
/// Code fix provider for IQR0001: Prefer ResultAsync over ResultExtensions.ThenAsync.
/// Rewrites: expr.ThenAsync(next) -> global::IndQuestResults.Async.ResultAsync.BindAsync(expr, next)
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferResultAsyncCodeFixProvider)), Shared]
public sealed class PreferResultAsyncCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Analyzers.PreferResultAsyncAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
            return;

        var span = diagnostic.Location.SourceSpan;
        var node = root.FindNode(span);
        if (node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>() is not MemberAccessExpressionSyntax memberAccess)
            return;
        if (memberAccess.Parent is not InvocationExpressionSyntax invocation)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use ResultAsync.BindAsync",
                createChangedDocument: c => UseResultAsyncBindAsync(context.Document, root, invocation, memberAccess, c),
                equivalenceKey: "UseResultAsyncBindAsync"),
            context.Diagnostics);
    }

    private static Task<Document> UseResultAsyncBindAsync(Document document, SyntaxNode root, InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess, CancellationToken cancellationToken)
    {
        // Build: ResultAsync.BindAsync(<expr>, <arg>)
        // First, ensure we have the using statement for IndQuestResults.Async
        var expr = memberAccess.Expression;
        var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault();

        // Create the ResultAsync.BindAsync call
        var resultAsyncQualified = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("IndQuestResults"),
                SyntaxFactory.IdentifierName("Async")),
            SyntaxFactory.IdentifierName("ResultAsync"))
            .WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation);

        var bindAsyncCall = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            resultAsyncQualified,
            SyntaxFactory.IdentifierName("BindAsync"));

        var newInvocation = SyntaxFactory.InvocationExpression(
            bindAsyncCall,
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(expr.WithoutTrivia()),
                    firstArg ?? SyntaxFactory.Argument(SyntaxFactory.IdentifierName("/* next */"))
                })));

        // Preserve trivia from original invocation
        newInvocation = newInvocation.WithTriviaFrom(invocation);

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        
        // Add using statement if not present
        if (newRoot is CompilationUnitSyntax compilationUnit)
        {
            var hasAsyncUsing = compilationUnit.Usings.Any(u => 
            {
                var name = u.Name?.ToString();
                return name == "IndQuestResults.Async" || 
                       name?.StartsWith("IndQuestResults.Async", StringComparison.OrdinalIgnoreCase) == true;
            });
            
            if (!hasAsyncUsing)
            {
                var asyncUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("IndQuestResults.Async"))
                    .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));
                var newUsings = compilationUnit.Usings.Add(asyncUsing);
                newRoot = compilationUnit.WithUsings(newUsings);
            }
        }

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
