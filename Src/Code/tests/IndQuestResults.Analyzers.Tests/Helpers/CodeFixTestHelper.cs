using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IndQuestResults.Analyzers.Tests.Helpers;

internal static class CodeFixTestHelper
{
    public static async Task<string> ApplyFirstCodeFixAsync<TAnalyzer, TCodeFix>(string source, ILogger? logger = null)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        using var workspace = new AdhocWorkspace();

        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name: "TestAssembly",
            assemblyName: "TestAssembly",
            language: LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        var project = workspace.AddProject(projectInfo)
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Result).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location));

        var document = workspace.AddDocument(project.Id, "Test.cs", SourceText.From(source));

        // Run analyzer to get diagnostics
        var analyzer = new TAnalyzer();
        var compilation = await document.Project.GetCompilationAsync();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
        var diagnostics = await compilation!.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

        var allDiagnostics = diagnostics.ToArray();
#pragma warning disable CA1848 // Use LoggerMessage delegates - temporary debugging code
        logger?.LogInformation("Total diagnostics found: {Count}", allDiagnostics.Length);
        foreach (var diag in allDiagnostics)
        {
            var lineSpan = diag.Location.GetLineSpan();
            logger?.LogInformation("  Diagnostic: {Id} at line {Line}, column {Column} - {Message}", 
                diag.Id, lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1, diag.GetMessage());
        }

        var target = diagnostics.FirstOrDefault(d => d.Id.StartsWith("IQR", StringComparison.Ordinal));
        if (target == default)
        {
            logger?.LogWarning("No IQR diagnostic found - code fix cannot be applied");
            return source; // nothing to fix
        }

        logger?.LogInformation("Found IQR diagnostic: {Id} at line {Line}, column {Column}", 
            target.Id, target.Location.GetLineSpan().StartLinePosition.Line + 1, 
            target.Location.GetLineSpan().StartLinePosition.Character + 1);
#pragma warning restore CA1848

        // Apply code fix
        var codeFix = new TCodeFix();
        List<CodeAction> actions = [];
        var context = new CodeFixContext(
            document,
            target,
            (a, _) => actions.Add(a),
            CancellationToken.None);

        await codeFix.RegisterCodeFixesAsync(context).ConfigureAwait(false);
#pragma warning disable CA1848 // Use LoggerMessage delegates - temporary debugging code
        logger?.LogInformation("Code fix registered {Count} actions", actions.Count);
        if (actions.Count == 0)
        {
            logger?.LogWarning("No code fix actions registered");
            return source;
        }

        var op = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        logger?.LogInformation("Code fix action has {Count} operations", op.Length);
        foreach (var operation in op)
        {
            if (operation is ApplyChangesOperation apply)
            {
                logger?.LogInformation("Applying code fix changes");
                workspace.TryApplyChanges(apply.ChangedSolution);
                break;
            }
        }
#pragma warning restore CA1848

        var changedDoc = workspace.CurrentSolution.GetDocument(document.Id)!;
        var newText = await changedDoc.GetTextAsync().ConfigureAwait(false);
        return newText.ToString();
    }
}
