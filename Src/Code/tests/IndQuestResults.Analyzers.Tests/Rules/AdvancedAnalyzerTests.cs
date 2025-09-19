using IndQuestResults.Analyzers.Rules;
using IndQuestResults.Analyzers.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using static IndQuestResults.Analyzers.Tests.Helpers.AnalyzerTestHelper;

namespace IndQuestResults.Analyzers.Tests.Rules;

/// <summary>
/// Advanced test examples demonstrating enhanced testing capabilities for .NET 10.
/// </summary>
public class AdvancedAnalyzerTests
{
    /// <summary>
    /// Example using Shouldly for more expressive test assertions.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task Shouldly_Example_ProvidesBetterErrorMessages()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        GetResult(); // This should trigger IQR001
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        var expected = Diagnostic(ResultPatternAnalyzer.UnhandledResultId, DiagnosticSeverity.Warning, 8, 9);

        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Example of performance testing for analyzers.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task PerformanceTest_AnalyzerCompletesWithinTimeLimit()
    {
        // Generate a larger codebase for performance testing
        var largeCodeBase = GenerateLargeValidCodeBase();

        // Verify analyzer performance - should complete without errors
        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(largeCodeBase);
    }

    /// <summary>
    /// Example demonstrating comprehensive analyzer testing without external dependencies.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ComprehensiveTest_CapturesAnalyzerOutput()
    {
        const string testCode = @"
using IndQuestResults;
using System;

public class TestClass
{
    public Result<int> ProblematicMethod()
    {
        GetResult(); // IQR001
        var result = GetResult();
        var value = result.Value; // IQR002
        throw new InvalidOperationException(); // IQR003
        return null; // IQR004
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        var expected = new[]
        {
            Diagnostic(ResultPatternAnalyzer.UnhandledResultId, DiagnosticSeverity.Warning, 9, 9),
            Diagnostic(ResultPatternAnalyzer.DirectValueAccessId, DiagnosticSeverity.Error, 11, 21),
            Diagnostic(ResultPatternAnalyzer.ThrowingInResultId, DiagnosticSeverity.Warning, 12, 9),
            Diagnostic(ResultPatternAnalyzer.NullResultId, DiagnosticSeverity.Error, 13, 16)
        };

        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Example testing .NET 10 specific language features.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task Net10Features_AnalyzerHandlesNewSyntax()
    {
        const string testCode = @"
using IndQuestResults;
using System;

public class TestClass
{
    // Test with .NET 10 features like required members, etc.
    public required string RequiredProperty { get; init; }

    public void TestMethod()
    {
        // Using pattern matching enhancements
        var result = GetResult();
        var processed = result switch
        {
            { IsSuccess: true } => ProcessSuccess(result.Value),
            { IsFailure: true } => ProcessFailure(result.Errors),
            _ => throw new InvalidOperationException()
        };
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private string ProcessSuccess(int value) => value.ToString();
    private string ProcessFailure(System.Collections.Generic.IEnumerable<string> errors) => string.Join("", "", errors);
}";

        // This should not trigger any diagnostics as it's using proper Result patterns
        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Example testing async Result patterns.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AsyncPatterns_AnalyzerHandlesTaskResults()
    {
        const string testCode = @"
using IndQuestResults;
using System.Threading.Tasks;

public class TestClass
{
    public async Task TestMethod()
    {
        await GetAsyncResult(); // This should trigger IQR001
    }

    private async Task<Result<int>> GetAsyncResult()
    {
        await Task.Delay(1);
        return Result<int>.Success(42);
    }
}";

        var expected = Diagnostic(ResultPatternAnalyzer.UnhandledResultId, DiagnosticSeverity.Warning, 9, 9);

        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Example testing Result pattern with proper error handling.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ProperErrorHandling_NoWarnings()
    {
        const string testCode = @"
using IndQuestResults;
using System;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();
        result.Match(
            onSuccess: value => 
            {
                Console.WriteLine($""Success: {value}"");
            },
            onFailure: errors => 
            {
                foreach (var error in errors)
                {
                    Console.WriteLine($""Error: {error}"");
                }
            });
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Example testing Result chaining operations.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ResultChaining_NoWarnings()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult()
            .Map(x => x * 2)
            .Bind(x => x > 50 ? Result<int>.Success(x) : Result<int>.WithFailure(""Too small""))
            .Map(x => x.ToString());

        if (result.IsSuccess)
        {
            ProcessString(result.Value);
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private void ProcessString(string value) { }
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Example testing both Result and ResultPerformance analyzers together.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task MultipleAnalyzers_BothPatternAndPerformance()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();
        if (result.IsSuccess)
        {
            var value1 = result.Value; // First access - OK
            var value2 = result.Value; // Second access - should trigger performance warning
            ProcessValues(value1, value2);
        }
        
        GetResult(); // Should trigger pattern warning
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private void ProcessValues(int v1, int v2) { }
}";

        // Test with ResultPatternAnalyzer
        var patternExpected = Diagnostic(ResultPatternAnalyzer.UnhandledResultId, DiagnosticSeverity.Warning, 16, 9);
        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, patternExpected);

        // Test with ResultPerformanceAnalyzer
        var performanceExpected = Diagnostic(ResultPerformanceAnalyzer.MultipleValueAccessId, DiagnosticSeverity.Info, 12, 26);
        await VerifyAnalyzerAsync<ResultPerformanceAnalyzer>(testCode, performanceExpected);
    }

    private static string GenerateLargeValidCodeBase()
    {
        var codeBuilder = new System.Text.StringBuilder();
        codeBuilder.AppendLine("using IndQuestResults;");
        codeBuilder.AppendLine("namespace TestProject {");

        // Generate multiple classes with valid Result usage
        for (int i = 0; i < 50; i++)
        {
            codeBuilder.AppendLine($@"
    public class Service{i}
    {{
        public void ProcessData{i}()
        {{
            var result = GetResult{i}();
            if (result.IsSuccess)
            {{
                var value = result.Value;
                ProcessValue{i}(value);
            }}
        }}

        private Result<int> GetResult{i}() => Result<int>.Success({i});
        private void ProcessValue{i}(int value) {{ }}
    }}");
        }

        codeBuilder.AppendLine("}");
        return codeBuilder.ToString();
    }
}