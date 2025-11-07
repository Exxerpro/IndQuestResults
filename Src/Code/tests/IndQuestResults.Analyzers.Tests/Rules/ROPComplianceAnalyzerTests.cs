using IndQuestResults.Analyzers.Rules;
using IndQuestResults.Analyzers.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;
using static IndQuestResults.Analyzers.Tests.Helpers.AnalyzerTestHelper;

namespace IndQuestResults.Analyzers.Tests.Rules;

/// <summary>
/// Tests for ROPComplianceAnalyzer to ensure ROP compliance violations are detected.
/// </summary>
public class ROPComplianceAnalyzerTests
{
    /// <summary>
    /// Verifies that ArgumentNullException.ThrowIfNull in extension method is reported.
    /// </summary>
    [Fact]
    public async Task ThrowIfNull_InExtensionMethod_ReportsWarning()
    {
        const string testCode = @"
using System;
using IndQuestResults;

public static class TestExtensions
{
    public static Result<int> Map(this Result<int> result, Func<int, int> selector)
    {
        ArgumentNullException.ThrowIfNull(result); // Should trigger IQR201
        ArgumentNullException.ThrowIfNull(selector); // Should trigger IQR201
        return result.IsSuccess
            ? Result<int>.Success(selector(result.Value!))
            : Result<int>.WithFailure(result.Errors);
    }
}";

        var expected = new[]
        {
            Diagnostic(ROPComplianceAnalyzer.ROPViolationThrowIfNullId, DiagnosticSeverity.Warning, 9, 9),
            Diagnostic(ROPComplianceAnalyzer.ROPViolationThrowIfNullId, DiagnosticSeverity.Warning, 10, 9)
        };

        await VerifyAnalyzerAsync<ROPComplianceAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Verifies that throw statement in Result-returning method is reported.
    /// </summary>
    [Fact]
    public async Task ThrowStatement_InResultMethod_ReportsWarning()
    {
        const string testCode = @"
using IndQuestResults;

public class TestClass
{
    public Result<int> GetValue(int input)
    {
        if (input < 0)
        {
            throw new ArgumentException(""Input must be positive""); // Should trigger IQR202
        }
        return Result<int>.Success(input);
    }
}";

        var expected = Diagnostic(ROPComplianceAnalyzer.ROPViolationThrowStatementId, DiagnosticSeverity.Warning, 10, 13);

        await VerifyAnalyzerAsync<ROPComplianceAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Verifies that throw in catch block (rethrowing) is not reported.
    /// </summary>
    [Fact]
    public async Task ThrowInCatchBlock_NotReported()
    {
        const string testCode = @"
using System;
using IndQuestResults;

public class TestClass
{
    public Result<int> GetValue()
    {
        try
        {
            return Result<int>.Success(42);
        }
        catch (Exception ex)
        {
            throw; // Rethrowing is acceptable - should NOT trigger IQR202
        }
    }
}";

        await VerifyNoDiagnosticsAsync<ROPComplianceAnalyzer>(testCode);
    }

    /// <summary>
    /// Verifies that non-extension methods are not analyzed.
    /// </summary>
    [Fact]
    public async Task ThrowIfNull_InNonExtensionMethod_NotReported()
    {
        const string testCode = @"
using System;
using IndQuestResults;

public class TestClass
{
    public void Process(Result<int> result)
    {
        ArgumentNullException.ThrowIfNull(result); // Not an extension method - should NOT trigger
    }
}";

        await VerifyNoDiagnosticsAsync<ROPComplianceAnalyzer>(testCode);
    }

    /// <summary>
    /// Verifies that methods not returning Result are not analyzed.
    /// </summary>
    [Fact]
    public async Task ThrowIfNull_InNonResultMethod_NotReported()
    {
        const string testCode = @"
using System;

public static class TestExtensions
{
    public static int Map(this int value, Func<int, int> selector)
    {
        ArgumentNullException.ThrowIfNull(selector); // Not returning Result - should NOT trigger
        return selector(value);
    }
}";

        await VerifyNoDiagnosticsAsync<ROPComplianceAnalyzer>(testCode);
    }
}

