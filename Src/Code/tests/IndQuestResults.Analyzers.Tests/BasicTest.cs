using Xunit;
using IndQuestResults.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;

namespace IndQuestResults.Analyzers.Tests;

/// <summary>
/// Basic tests to verify analyzers are working without external test framework dependencies.
/// </summary>
public class BasicAnalyzerTests
{
    [Fact]
    public void ResultPatternAnalyzer_CanBeInstantiated()
    {
        // Simple test to verify the analyzer can be created
        var analyzer = new ResultPatternAnalyzer();
        analyzer.ShouldNotBeNull();
        analyzer.SupportedDiagnostics.ShouldNotBeEmpty();
    }

    [Fact]
    public void ResultPerformanceAnalyzer_CanBeInstantiated()
    {
        // Simple test to verify the analyzer can be created
        var analyzer = new ResultPerformanceAnalyzer();
        analyzer.ShouldNotBeNull();
        analyzer.SupportedDiagnostics.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task BasicCompilation_WithAnalyzers_Succeeds()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void ValidMethod()
    {
        var result = GetResult();
        if (result.IsSuccess)
        {
            var value = result.Value;
            System.Console.WriteLine(value);
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        var compilation = CreateCompilation(testCode);
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new ResultPatternAnalyzer(),
            new ResultPerformanceAnalyzer());

        var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
        
        // This test should pass without any diagnostics since it uses proper patterns
        var analyzerDiagnostics = diagnostics.Where(d => d.Id.StartsWith("IQR")).ToArray();
        analyzerDiagnostics.ShouldBeEmpty("valid Result usage should not produce diagnostics");
    }

    [Fact]
    public async Task BasicCompilation_DetectsUnhandledResult()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void ProblematicMethod()
    {
        GetResult(); // This should be flagged as unhandled
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        var compilation = CreateCompilation(testCode);
        var analyzer = new ResultPatternAnalyzer();

        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync();
        
        var unhandledResultDiagnostics = diagnostics
            .Where(d => d.Id == ResultPatternAnalyzer.UnhandledResultId)
            .ToArray();
            
        unhandledResultDiagnostics.ShouldNotBeEmpty("unhandled Result should be detected");
        unhandledResultDiagnostics.Length.ShouldBe(1);
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Result).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}