using IndQuestResults.Analyzers.Rules;
using IndQuestResults.Analyzers.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;
using static IndQuestResults.Analyzers.Tests.Helpers.AnalyzerTestHelper;

namespace IndQuestResults.Analyzers.Tests.Rules;

/// <summary>
/// Tests for ResultPatternAnalyzer to ensure proper Result&lt;T&gt; pattern enforcement.
///
/// TODO v2.0: Add test cases for modern C# collection patterns:
/// - Collection expressions: Result&lt;int[]&gt;.Success([1, 2, 3])
/// - Target-typed new: Result&lt;List&lt;int&gt;&gt;.Success(new() { 1, 2, 3 })
/// - List patterns: if (result.Value is [var first, .., var last])
/// - Spread operator: Result&lt;int[]&gt;.Success([..array1, ..array2])
///
/// TODO v2.0: Add test cases for enhanced pattern matching:
/// - Extended property patterns with Value access
/// - Relational patterns in switch expressions
/// - Record types with positional patterns
/// </summary>
public class ResultPatternAnalyzerTests
{
    /// <summary>
    /// Verifies that an unhandled Result return value is reported with the expected diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task UnhandledResult_ReportsWarning()
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
    /// Verifies that direct Value access without checking success is reported as an error.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DirectValueAccess_WithoutSuccessCheck_ReportsError()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();
        var value = result.Value; // This should trigger IQR002
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        var expected = Diagnostic(ResultPatternAnalyzer.DirectValueAccessId, DiagnosticSeverity.Error, 9, 21);

        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Ensures that Value access after a success check does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DirectValueAccess_WithSuccessCheck_ReportsNoWarning()
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
            var value = result.Value; // This should NOT trigger IQR002
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that Value access with a success and null check does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DirectValueAccess_WithSuccessAndNullCheck_ReportsNoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();
        if (result.IsSuccess && result.Value is not null)
        {
            var value = result.Value; // This should NOT trigger IQR002
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that Value access with a null check and early return does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DirectValueAccess_WithEarlyReturn_ReportsNoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();
        if (result.IsFailure )
        {
            return;
        }

        if (result.Value is null)
        {
            return;
        }

        var value = result.Value; // This should NOT trigger IQR002
    }

    private Result<int[]> GetResult() => Result<int[]>.Success(new[] { 42, 23 });
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that Value access with a ShouldlyAsertion success check does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DirectValueAccess_WithAShouldlyAsertionSuccessCheck_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();

            result.IsSuccess.ShouldBeTrue();
            var value = result.Value; // This should NOT trigger IQR002
        }
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that Value access with a ShouldlyAsertion success check does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DirectValueAccess_WithAShouldlyAsertionSuccessAndNotNullNullabeCheck_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();

            result.IsSuccess.ShouldBeTrue();
            var value = result.Value; // This should NOT trigger IQR002
        }
    }

    private Result<int?> GetResult() => Result<int?>.Success(42);
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that Value access with a ShouldlyAsertion success and not null for nullable types check does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DirectValueAccess_WithAShouldlyAsertionSuccessAndNotNullCheck_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;
using System.Collections.Generic;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();

            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldNotBeNull();
            var value = result.Value; // This should NOT trigger IQR002
        }
    }

    private Result<List<int>> GetResult() => Result<List<int>>.Success(new List<int> { 42, 23 });
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// TODO Phase 2: Ensures that Value access for reference types without null check reports a warning.
    /// Currently, IsSuccess check via Shouldly is sufficient to suppress the diagnostic.
    /// Future enhancement: Detect reference types and require additional null checks.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DirectValueAccess_WithAShouldlyAsertionButNonNullCheck_ReportWarning()
    {
        const string testCode = @"
using IndQuestResults;
using System.Collections.Generic;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult();

            result.IsSuccess.ShouldBeTrue();
            var value = result.Value; // TODO Phase 2: Should trigger IQR002 Warning for reference types
        }
    }

    private Result<List<int>> GetResult() => Result<List<int>>.Success(new List<int> { 42, 35, 25 });
}";

        var expected = Diagnostic(ResultPatternAnalyzer.DirectValueAccessId, DiagnosticSeverity.Warning, 11, 25);

        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Verifies that throwing directly inside a Result-returning method is reported.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ThrowInResultMethod_ReportsWarning()
    {
        const string testCode = @"
using IndQuestResults;
using System;

public class TestClass
{
    public Result<int> TestMethod()
    {
        throw new InvalidOperationException(""Error""); // This should trigger IQR003
    }
}";

        var expected = Diagnostic(ResultPatternAnalyzer.ThrowingInResultId, DiagnosticSeverity.Error, 9, 9);

        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Ensures that exceptions handled via try/catch in Result-returning methods are not reported.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ThrowInTryCatch_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;
using System;

public class TestClass
{
    public Result<int> TestMethod()
    {
        try
        {
            throw new InvalidOperationException(""Error""); // This should NOT trigger IQR003
        }
        catch
        {
            return Result<int>.WithFailure(""Handled"");
        }
    }
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Verifies that returning null from a Result-returning method is reported as an error.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task NullResultReturn_ReportsError()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public Result<int> TestMethod()
    {
        return null; // This should trigger IQR004
    }
}";

        var expected = Diagnostic(ResultPatternAnalyzer.NullResultId, DiagnosticSeverity.Error, 8, 16);

        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Verifies that returning default from a Result-returning method is reported as an error.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DefaultResultReturn_ReportsError()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public Result<int> TestMethod()
    {
        return default; // This should trigger IQR004
    }
}";

        var expected = Diagnostic(ResultPatternAnalyzer.NullResultId, DiagnosticSeverity.Error, 8, 16);

        await VerifyAnalyzerAsync<ResultPatternAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Ensures that using the Match pattern on Result does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task MatchPattern_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        GetResult().Match(
            onSuccess: value => ProcessValue(value),
            onFailure: errors => HandleErrors(errors));
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private void ProcessValue(int value) { }
    private void HandleErrors(System.Collections.Generic.IEnumerable<string> errors) { }
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that using the Map pattern on Result does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task MapPattern_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult().Map(x => x.ToString());
        // Using result is acceptable
    }

    private Result<int> GetResult() => Result<int>.Success(42);
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that using the Bind pattern on Result does not report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BindPattern_NoWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetResult().Bind(x => ProcessValue(x));
        // Using result is acceptable
    }

    private Result<int> GetResult() => Result<int>.Success(42);
    private Result<string> ProcessValue(int value) => Result<string>.Success(value.ToString());
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that non-Result methods and exceptions outside Result-returning methods are not reported.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task NonResultMethod_NoWarning()
    {
        const string testCode = @"
using System;

public class TestClass
{
    public void TestMethod()
    {
        GetNonResult(); // This should NOT trigger any warnings
        throw new InvalidOperationException(); // This should NOT trigger IQR003
    }

    public string TestMethod2()
    {
        return null; // This should NOT trigger IQR004
    }

    private int GetNonResult() => 42;
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Verifies that multiple distinct issues are all reported with the expected diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task MultipleIssues_ReportsMultipleDiagnostics()
    {
        const string testCode = @"
using IndQuestResults;
using System;

public class TestClass
{
    public Result<int> TestMethod()
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
    /// Ensures that generic Result usage is handled correctly and does not produce false positives.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task GenericResult_HandledCorrectly()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetStringResult();
        if (result.IsSuccess)
        {
            var value = result.Value; // Should NOT trigger warning
            ProcessString(value);
        }
    }

    private Result<string> GetStringResult() => Result<string>.Success(""test"");
    private void ProcessString(string value) { }
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }

    /// <summary>
    /// Ensures that non-generic Result usage is handled correctly and does not produce false positives.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task NonGenericResult_HandledCorrectly()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public void TestMethod()
    {
        var result = GetNonGenericResult();
        if (result.IsSuccess)
        {
            ProcessSuccess();
        }
    }

    private Result GetNonGenericResult() => Result.Success();
    private void ProcessSuccess() { }
}";

        await VerifyNoDiagnosticsAsync<ResultPatternAnalyzer>(testCode);
    }
}
