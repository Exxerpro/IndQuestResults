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
using Microsoft.CodeAnalysis.Editing;
using IndQuestResults.Analyzers.Rules;

namespace IndQuestResults.Analyzers.CodeFixes;

/// <summary>
/// Code fix provider for Result&lt;T&gt; pattern violations.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ResultPatternCodeFixProvider)), Shared]
public class ResultPatternCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// Gets the diagnostic IDs that this code fix provider can address.
    /// </summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            ResultPatternAnalyzer.UnhandledResultId,
            ResultPatternAnalyzer.DirectValueAccessId,
            ResultPatternAnalyzer.ThrowingInResultId,
            ResultPatternAnalyzer.NullResultId);

    /// <summary>
    /// Gets the Fix All provider for this code fix provider.
    /// </summary>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers code fixes for the specified diagnostics.
    /// </summary>
    /// <param name="context">The code fix context.</param>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        switch (diagnostic.Id)
        {
            case ResultPatternAnalyzer.UnhandledResultId:
                await RegisterUnhandledResultFixes(context, root, diagnosticSpan);
                break;

            case ResultPatternAnalyzer.DirectValueAccessId:
                await RegisterDirectValueAccessFixes(context, root, diagnosticSpan);
                break;

            case ResultPatternAnalyzer.ThrowingInResultId:
                await RegisterThrowingInResultFixes(context, root, diagnosticSpan);
                break;

            case ResultPatternAnalyzer.NullResultId:
                await RegisterNullResultFixes(context, root, diagnosticSpan);
                break;
        }
    }

    /// <summary>
    /// Registers code fixes for unhandled Result<T> diagnostics.
    /// </summary>
    private async Task RegisterUnhandledResultFixes(CodeFixContext context, SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var invocation = root.FindNode(span).FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation == null)
            return;

        // Fix 1: Add IsSuccess check
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add IsSuccess check",
                createChangedDocument: c => AddSuccessCheck(context.Document, invocation, c),
                equivalenceKey: "AddSuccessCheck"),
            context.Diagnostics);

        // Fix 2: Use Match pattern
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use Match pattern",
                createChangedDocument: c => UseMatchPattern(context.Document, invocation, c),
                equivalenceKey: "UseMatchPattern"),
            context.Diagnostics);
    }

    /// <summary>
    /// Registers code fixes for direct Value access diagnostics.
    /// </summary>
    private async Task RegisterDirectValueAccessFixes(CodeFixContext context, SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var memberAccess = root.FindNode(span).FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
        if (memberAccess == null)
            return;

        // Fix: Wrap in IsSuccess check
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add IsSuccess check before Value access",
                createChangedDocument: c => WrapInSuccessCheck(context.Document, memberAccess, c),
                equivalenceKey: "WrapInSuccessCheck"),
            context.Diagnostics);
    }

    /// <summary>
    /// Registers code fixes for throw statements inside Result-returning methods.
    /// </summary>
    private async Task RegisterThrowingInResultFixes(CodeFixContext context, SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var throwStatement = root.FindNode(span).FirstAncestorOrSelf<ThrowStatementSyntax>();
        if (throwStatement == null)
            return;

        // Fix: Replace with Result.WithFailure
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with Result.WithFailure",
                createChangedDocument: c => ReplaceThrowWithFailure(context.Document, throwStatement, c),
                equivalenceKey: "ReplaceThrowWithFailure"),
            context.Diagnostics);
    }

    /// <summary>
    /// Registers code fixes for null returns in Result-returning methods.
    /// </summary>
    private async Task RegisterNullResultFixes(CodeFixContext context, SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var returnStatement = root.FindNode(span).FirstAncestorOrSelf<ReturnStatementSyntax>();
        if (returnStatement == null)
            return;

        // Fix: Replace null with Result.WithFailure
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with Result.WithFailure",
                createChangedDocument: c => ReplaceNullWithFailure(context.Document, returnStatement, c),
                equivalenceKey: "ReplaceNullWithFailure"),
            context.Diagnostics);
    }

    /// <summary>
    /// Adds an IsSuccess check around a Result-returning invocation.
    /// </summary>
    private async Task<Document> AddSuccessCheck(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        // Create: var result = MethodCall();
        var resultVariable = generator.IdentifierName("result");
        var variableDeclaration = generator.LocalDeclarationStatement(
            generator.IdentifierName("var"),
            "result",
            invocation);

        // Create: if (result.IsSuccess) { /* use result */ }
        var successCheck = generator.IfStatement(
            generator.MemberAccessExpression(resultVariable, "IsSuccess"),
            new[] { generator.ExpressionStatement(generator.IdentifierName("// TODO: Handle success")) });

        // Replace the expression statement with our new code
        var statement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (statement != null)
        {
            // Insert the variable declaration before the statement
            editor.InsertBefore(statement, variableDeclaration);
            // Replace the statement with the success check
            editor.ReplaceNode(statement, successCheck);
        }

        return editor.GetChangedDocument();
    }

    /// <summary>
    /// Replaces a bare invocation with a Match pattern call.
    /// </summary>
    private async Task<Document> UseMatchPattern(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        // Create Match pattern call
        var matchExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(invocation, "Match"),
            generator.Argument("onSuccess", RefKind.None,
                generator.ValueReturningLambdaExpression("value",
                    generator.IdentifierName("// TODO: Handle success"))),
            generator.Argument("onFailure", RefKind.None,
                generator.ValueReturningLambdaExpression("errors",
                    generator.IdentifierName("// TODO: Handle failure"))));

        var statement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (statement != null)
        {
            editor.ReplaceNode(statement, generator.ExpressionStatement(matchExpression));
        }

        return editor.GetChangedDocument();
    }

    /// <summary>
    /// Wraps direct Value access in an IsSuccess guard.
    /// </summary>
    private async Task<Document> WrapInSuccessCheck(Document document, MemberAccessExpressionSyntax memberAccess, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        // Find the statement containing this expression
        var statement = memberAccess.FirstAncestorOrSelf<StatementSyntax>();
        if (statement == null)
            return document;

        // Create if statement with IsSuccess check
        var resultExpression = memberAccess.Expression;
        var ifStatement = generator.IfStatement(
            generator.MemberAccessExpression(resultExpression, "IsSuccess"),
            new[] { statement });

        // Replace the original statement
        editor.ReplaceNode(statement, ifStatement);

        return editor.GetChangedDocument();
    }

    /// <summary>
    /// Replaces a throw statement with a corresponding Result.WithFailure return.
    /// </summary>
    private async Task<Document> ReplaceThrowWithFailure(Document document, ThrowStatementSyntax throwStatement, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Extract exception message if possible
        var errorMessage = "Operation failed";
        if (throwStatement.Expression is ObjectCreationExpressionSyntax creation &&
            creation.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = creation.ArgumentList.Arguments[0].Expression;
            if (firstArg is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                errorMessage = literal.Token.ValueText;
            }
        }

        // Determine if generic or non-generic Result
        var method = throwStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        var isGenericResult = false;
        if (method != null && semanticModel != null)
        {
            var returnType = semanticModel.GetTypeInfo(method.ReturnType).Type;
            isGenericResult = returnType?.TypeKind == TypeKind.Struct && returnType.Name.Contains("Result");
        }

        // Create Result.WithFailure call
        SyntaxNode failureCall;
        if (isGenericResult)
        {
            // For Result<T>, need to provide default value
            failureCall = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName("Result"),
                    generator.GenericName("WithFailure",
                        generator.IdentifierName("T"))), // Will need to be replaced with actual type
                generator.LiteralExpression(errorMessage),
                generator.Argument("defaultValue", RefKind.None, 
                    generator.DefaultExpression(generator.IdentifierName("T"))));
        }
        else
        {
            failureCall = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName("Result"),
                    "WithFailure"),
                generator.LiteralExpression(errorMessage));
        }

        // Create return statement
        var returnStatement = generator.ReturnStatement(failureCall);

        editor.ReplaceNode(throwStatement, returnStatement);

        return editor.GetChangedDocument();
    }

    /// <summary>
    /// Replaces a null return expression with a Result.WithFailure return.
    /// </summary>
    private async Task<Document> ReplaceNullWithFailure(Document document, ReturnStatementSyntax returnStatement, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Determine if generic or non-generic Result
        var method = returnStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        var isGenericResult = false;
        if (method != null && semanticModel != null)
        {
            var returnType = semanticModel.GetTypeInfo(method.ReturnType).Type;
            isGenericResult = returnType?.TypeKind == TypeKind.Struct && returnType.Name.Contains("Result");
        }

        // Create appropriate Result.WithFailure call
        SyntaxNode failureCall;
        if (isGenericResult)
        {
            failureCall = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName("Result"),
                    generator.GenericName("WithFailure",
                        generator.IdentifierName("T"))),
                generator.LiteralExpression("Operation returned null"),
                generator.Argument("defaultValue", RefKind.None,
                    generator.DefaultExpression(generator.IdentifierName("T"))));
        }
        else
        {
            failureCall = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName("Result"),
                    "WithFailure"),
                generator.LiteralExpression("Operation returned null"));
        }

        var newReturn = generator.ReturnStatement(failureCall);
        editor.ReplaceNode(returnStatement, newReturn);

        return editor.GetChangedDocument();
    }
}