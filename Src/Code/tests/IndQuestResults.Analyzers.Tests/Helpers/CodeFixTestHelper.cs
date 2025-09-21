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

namespace IndQuestResults.Analyzers.Tests.Helpers;

internal static class CodeFixTestHelper
{
    public static async Task<string> ApplyFirstCodeFixAsync<TAnalyzer, TCodeFix>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        using var workspace = new AdhocWorkspace();

        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name: "TestProject",
            assemblyName: "TestProject",
            language: LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        var project = workspace.AddProject(projectInfo)
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(IndQuestResults.Result).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location));

        var document = workspace.AddDocument(project.Id, "Test.cs", SourceText.From(source));

        // Run analyzer to get diagnostics
        var analyzer = new TAnalyzer();
        var compilation = await document.Project.GetCompilationAsync();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
        var diagnostics = await compilation!.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

        var target = diagnostics.FirstOrDefault(d => d.Id.StartsWith("IQR", StringComparison.Ordinal));
        if (target == default)
        {
            return source; // nothing to fix
        }

        // Apply code fix
        var codeFix = new TCodeFix();
        List<CodeAction> actions = [];
        var context = new CodeFixContext(
            document,
            target,
            (a, _) => actions.Add(a),
            CancellationToken.None);

        await codeFix.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        if (actions.Count == 0)
        {
            return source;
        }

        var op = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        foreach (var operation in op)
        {
            if (operation is ApplyChangesOperation apply)
            {
                workspace.TryApplyChanges(apply.ChangedSolution);
                break;
            }
        }

        var changedDoc = workspace.CurrentSolution.GetDocument(document.Id)!;
        var newText = await changedDoc.GetTextAsync().ConfigureAwait(false);
        return newText.ToString();
    }
}



