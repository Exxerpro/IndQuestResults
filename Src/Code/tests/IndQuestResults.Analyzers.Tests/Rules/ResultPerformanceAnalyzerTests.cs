using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using IndQuestResults.Analyzers.Rules;
using Xunit;
using IndQuestResults.Operations;

namespace IndQuestResults.Analyzers.Tests.Rules;

/// <summary>
/// Tests for ResultPerformanceAnalyzer to ensure performance best practices are enforced.
/// </summary>
public class ResultPerformanceAnalyzerTests
{
    /// <summary>
    /// Verifies that multiple Value accesses on a Result within the same scope trigger the info diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task MultipleValueAccess_ReportsInfo()
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
            var value2 = result.Value; // Second access - should trigger IQR101
            ProcessValues(value1, value2);
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private void ProcessValues(int v1, int v2) { }
}";

        var expected = new DiagnosticResult(ResultPerformanceAnalyzer.MultipleValueAccessId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
            .WithSpan(12, 26, 12, 38);

        await VerifyAnalyzerAsync(testCode, expected);
    }

    /// <summary>
    /// Ensures that a single Value access does not produce diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task SingleValueAccess_NoWarning()
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
            var value = result.Value; // Single access - should NOT trigger warning
            ProcessValue(value);
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private void ProcessValue(int value) { }
}";

        await VerifyAnalyzerAsync(testCode);
    }

    /// <summary>
    /// Verifies that creating a List from Errors triggers the performance warning diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task UnnecessaryToList_OnErrors_ReportsWarning()
    {
        const string testCode = @"
using IndQuestResults;
using System.Linq;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();
        if (result.IsFailure)
        {
            var errorList = result.Errors.ToList(); // Should trigger IQR102
            ProcessErrors(errorList);
        }
    }

    private Result<int> GetResult() => Result<int>.WithFailure(""Error"");
    private void ProcessErrors(System.Collections.Generic.List<string> errors) { }
}";

        var expected = new DiagnosticResult(ResultPerformanceAnalyzer.UnnecessaryToListId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithSpan(12, 29, 12, 51);

        await VerifyAnalyzerAsync(testCode, expected);
    }

    /// <summary>
    /// Ensures ToList usage on non-Result collections does not trigger diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ToListOnOtherCollection_NoWarning()
    {
        const string testCode = @"
using System.Collections.Generic;
using System.Linq;

public class TestClass
{
    public void TestMethod()
    {
        var enumerable = GetEnumerable();
        var list = enumerable.ToList(); // Should NOT trigger warning
        ProcessList(list);
    }

    private IEnumerable<string> GetEnumerable() => new[] { ""item1"", ""item2"" };
    private void ProcessList(List<string> list) { }
}";

        await VerifyAnalyzerAsync(testCode);
    }

    /// <summary>
    /// Verifies that repeated error formatting in a property getter is reported.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task RepeatedErrorFormatting_InProperty_ReportsInfo()
    {
        const string testCode = @"
using IndQuestResults;
using System;

public class TestClass
{
    private Result<int> _result = Result<int>.WithFailure(""Error"");

    public string ErrorMessage
    {
        get
        {
            return string.Join("", "", _result.Errors); // Should trigger IQR103
        }
    }
}";

        var expected = new DiagnosticResult(ResultPerformanceAnalyzer.RepeatedErrorFormattingId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
            .WithSpan(13, 20, 13, 55);

        await VerifyAnalyzerAsync(testCode, expected);
    }

    /// <summary>
    /// Verifies boxing in a hot path (loop) is reported as an info diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BoxingInLoop_ReportsInfo()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = Result<int>.Success(i); // Should trigger IQR104 (boxing int in hot path)
            ProcessResult(result);
        }
    }

    private void ProcessResult(Result<int> result) { }
}";

        var expected = new DiagnosticResult(ResultPerformanceAnalyzer.BoxingInHotPathId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
            .WithSpan(10, 26, 10, 48)
            .WithArguments("Int32");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    /// <summary>
    /// Ensures boxing outside loops does not produce diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BoxingOutsideLoop_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = Result<int>.Success(42); // Should NOT trigger warning (not in loop)
        ProcessResult(result);
    }

    private void ProcessResult(Result<int> result) { }
}";

        await VerifyAnalyzerAsync(testCode);
    }

    /// <summary>
    /// Verifies boxing in a foreach loop is reported.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BoxingInForeachLoop_ReportsInfo()
    {
        const string testCode = @"
using IndQuestResults;
using System.Collections.Generic;

public class TestClass
{
    public void TestMethod()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        foreach (var number in numbers)
        {
            var result = Result<int>.Success(number); // Should trigger IQR104
            ProcessResult(result);
        }
    }

    private void ProcessResult(Result<int> result) { }
}";

        var expected = new DiagnosticResult(ResultPerformanceAnalyzer.BoxingInHotPathId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
            .WithSpan(12, 26, 12, 53)
            .WithArguments("Int32");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    /// <summary>
    /// Verifies boxing in a while loop is reported.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BoxingInWhileLoop_ReportsInfo()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        int i = 0;
        while (i < 10)
        {
            var result = Result<bool>.Success(true); // Should trigger IQR104
            ProcessResult(result);
            i++;
        }
    }

    private void ProcessResult(Result<bool> result) { }
}";

        var expected = new DiagnosticResult(ResultPerformanceAnalyzer.BoxingInHotPathId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
            .WithSpan(11, 26, 11, 52)
            .WithArguments("Boolean");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    /// <summary>
    /// Verifies boxing inside a LINQ query is reported.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BoxingInLinqQuery_ReportsInfo()
    {
        const string testCode = @"
using IndQuestResults;
using System.Collections.Generic;
using System.Linq;

public class TestClass
{
    public void TestMethod()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var results = numbers.Select(n => Result<int>.Success(n)).ToList(); // Should trigger IQR104
        ProcessResults(results);
    }

    private void ProcessResults(List<Result<int>> results) { }
}";

        var expected = new DiagnosticResult(ResultPerformanceAnalyzer.BoxingInHotPathId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
            .WithSpan(11, 39, 11, 61)
            .WithArguments("Int32");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    /// <summary>
    /// Ensures that reference type Results in loops do not produce boxing diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ReferenceTypeInLoop_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = Result<string>.Success(""test""); // Should NOT trigger warning (string is reference type)
            ProcessResult(result);
        }
    }

    private void ProcessResult(Result<string> result) { }
}";

        await VerifyAnalyzerAsync(testCode);
    }

    /// <summary>
    /// Verifies multiple distinct performance issues are reported simultaneously.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task MultiplePerformanceIssues_ReportsMultipleDiagnostics()
    {
        const string testCode = @"
using IndQuestResults;
using System.Linq;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();
        if (result.IsSuccess)
        {
            var value1 = result.Value; // First access - OK
            var value2 = result.Value; // Second access - IQR101
            ProcessValues(value1, value2);
        }

        if (result.IsFailure)
        {
            var errorList = result.Errors.ToList(); // IQR102
            ProcessErrors(errorList);
        }

        for (int i = 0; i < 10; i++)
        {
            var loopResult = Result<int>.Success(i); // IQR104
            ProcessLoopResult(loopResult);
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private void ProcessValues(int v1, int v2) { }
    private void ProcessErrors(System.Collections.Generic.List<string> errors) { }
    private void ProcessLoopResult(Result<int> result) { }
}";

        var expected = new[]
        {
            new DiagnosticResult(ResultPerformanceAnalyzer.MultipleValueAccessId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                .WithSpan(13, 26, 13, 38),
            new DiagnosticResult(ResultPerformanceAnalyzer.UnnecessaryToListId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithSpan(19, 29, 19, 51),
            new DiagnosticResult(ResultPerformanceAnalyzer.BoxingInHotPathId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                .WithSpan(25, 30, 25, 52)
                .WithArguments("Int32")
        };

        await VerifyAnalyzerAsync(testCode, expected);
    }

    /// <summary>
    /// Ensures usage of a cached value avoids multiple access diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CachedValueAccess_NoWarning()
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
            var value = result.Value; // Single access
            ProcessValue(value);
            ProcessValueAgain(value); // Using cached value - should NOT trigger warning
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private void ProcessValue(int value) { }
    private void ProcessValueAgain(int value) { }
}";

        await VerifyAnalyzerAsync(testCode);
    }

    /// <summary>
    /// Verifies the analyzer with provided source and expected diagnostics.
    /// </summary>
    /// <param name="source">C# source code to analyze.</param>
    /// <param name="expected">Expected diagnostics.</param>
    /// <returns>A task representing the asynchronous test execution.</returns>
    private static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ResultPerformanceAnalyzer, DefaultVerifier>
        {
            TestCode = source,
        };

        // Add reference to IndQuestResults
        test.TestState.AdditionalReferences.Add(typeof(Result).Assembly);

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
