using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shouldly;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IndQuestResults.Analyzers.Tests.Helpers;

/// <summary>
/// Helper class for testing analyzers without external testing framework dependencies.
/// </summary>
public static class AnalyzerTestHelper
{
    /// <summary>
    /// Verifies that an analyzer produces expected diagnostics for the given source code.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer type to test.</typeparam>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="expectedDiagnostics">Expected diagnostic results.</param>
    /// <returns>A task representing the asynchronous test execution.</returns>
    public static async Task VerifyAnalyzerAsync<TAnalyzer>(
        string source, 
        params ExpectedDiagnostic[] expectedDiagnostics)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var compilation = CreateCompilation(source);
        var analyzer = new TAnalyzer();

        var diagnostics = await compilation.WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
        
        var analyzerDiagnostics = diagnostics
            .Where(d => d.Id.StartsWith("IQR", StringComparison.Ordinal))
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToArray();

        analyzerDiagnostics.Length.ShouldBe(expectedDiagnostics.Length, 
            $"Expected {expectedDiagnostics.Length} diagnostics but got {analyzerDiagnostics.Length}");

        for (int i = 0; i < expectedDiagnostics.Length; i++)
        {
            var expected = expectedDiagnostics[i];
            var actual = analyzerDiagnostics[i];

            actual.Id.ShouldBe(expected.Id, $"Diagnostic {i} has wrong ID");
            actual.Severity.ShouldBe(expected.Severity, $"Diagnostic {i} has wrong severity");
            
            var lineSpan = actual.Location.GetLineSpan();
            var actualLine = lineSpan.StartLinePosition.Line + 1; // Convert to 1-based
            var actualColumn = lineSpan.StartLinePosition.Character + 1; // Convert to 1-based
            
            actualLine.ShouldBe(expected.Line, $"Diagnostic {i} is on wrong line");
            actualColumn.ShouldBe(expected.Column, $"Diagnostic {i} is on wrong column");
        }
    }

    /// <summary>
    /// Verifies that no diagnostics are produced for the given source code.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer type to test.</typeparam>
    /// <param name="source">The source code to analyze.</param>
    /// <returns>A task representing the asynchronous test execution.</returns>
    public static async Task VerifyNoDiagnosticsAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var compilation = CreateCompilation(source);
        var analyzer = new TAnalyzer();

        var diagnostics = await compilation.WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
        
        var analyzerDiagnostics = diagnostics
            .Where(d => d.Id.StartsWith("IQR", StringComparison.Ordinal))
            .ToArray();

        analyzerDiagnostics.ShouldBeEmpty($"Expected no diagnostics but got: {string.Join(", ", analyzerDiagnostics.Select(d => d.Id))}");
    }

    /// <summary>
    /// Creates an expected diagnostic result.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    /// <returns>An expected diagnostic result.</returns>
    public static ExpectedDiagnostic Diagnostic(string diagnosticId, DiagnosticSeverity severity, int line, int column)
    {
        return new ExpectedDiagnostic(diagnosticId, severity, line, column);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Result).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

/// <summary>
/// Represents an expected diagnostic result for testing.
/// </summary>
/// <param name="Id">The diagnostic ID.</param>
/// <param name="Severity">The diagnostic severity.</param>
/// <param name="Line">The line number (1-based).</param>
/// <param name="Column">The column number (1-based).</param>
public record ExpectedDiagnostic(string Id, DiagnosticSeverity Severity, int Line, int Column);