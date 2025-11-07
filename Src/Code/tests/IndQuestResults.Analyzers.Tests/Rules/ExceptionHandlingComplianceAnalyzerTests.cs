using IndQuestResults.Analyzers.Rules;
using IndQuestResults.Analyzers.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;
using static IndQuestResults.Analyzers.Tests.Helpers.AnalyzerTestHelper;

namespace IndQuestResults.Analyzers.Tests.Rules;

/// <summary>
/// Tests for ExceptionHandlingComplianceAnalyzer to ensure exception handling compliance.
/// </summary>
public class ExceptionHandlingComplianceAnalyzerTests
{
    /// <summary>
    /// Verifies that missing exception parameter in WithFailure call is reported.
    /// </summary>
    [Fact]
    public async Task MissingExceptionParameter_InCatchBlock_ReportsWarning()
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
            return Result<int>.WithFailure(""Operation failed: "" + ex.Message); // Should trigger IQR301
        }
    }
}";

        var expected = Diagnostic(ExceptionHandlingComplianceAnalyzer.MissingExceptionParameterId, DiagnosticSeverity.Warning, 14, 20);

        await VerifyAnalyzerAsync<ExceptionHandlingComplianceAnalyzer>(testCode, expected);
    }

    /// <summary>
    /// Verifies that exception parameter included in WithFailure call is not reported.
    /// </summary>
    [Fact]
    public async Task ExceptionParameter_Included_NotReported()
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
            return Result<int>.WithFailure(""Operation failed: "" + ex.Message, default, ex); // Should NOT trigger
        }
    }
}";

        await VerifyNoDiagnosticsAsync<ExceptionHandlingComplianceAnalyzer>(testCode);
    }

    /// <summary>
    /// Verifies that catch block without exception variable is not analyzed.
    /// </summary>
    [Fact]
    public async Task CatchBlock_WithoutExceptionVariable_NotReported()
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
        catch (Exception) // No variable name
        {
            return Result<int>.WithFailure(""Operation failed""); // Should NOT trigger
        }
    }
}";

        await VerifyNoDiagnosticsAsync<ExceptionHandlingComplianceAnalyzer>(testCode);
    }

    /// <summary>
    /// Verifies that catch block in non-Result method is not analyzed.
    /// </summary>
    [Fact]
    public async Task CatchBlock_InNonResultMethod_NotReported()
    {
        const string testCode = @"
using System;

public class TestClass
{
    public int GetValue()
    {
        try
        {
            return 42;
        }
        catch (Exception ex)
        {
            return Result<int>.WithFailure(""Operation failed""); // Not returning Result - should NOT trigger
        }
    }
}";

        await VerifyNoDiagnosticsAsync<ExceptionHandlingComplianceAnalyzer>(testCode);
    }

    /// <summary>
    /// Verifies that exception used but not in WithFailure is reported.
    /// </summary>
    [Fact]
    public async Task ExceptionUsed_ButNotInWithFailure_ReportsWarning()
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
            var message = ex.Message; // Exception used
            return Result<int>.WithFailure(""Operation failed""); // But not passed to WithFailure - should trigger IQR302
        }
    }
}";

        var expected = Diagnostic(ExceptionHandlingComplianceAnalyzer.ExceptionNotPreservedId, DiagnosticSeverity.Warning, 9, 9);

        await VerifyAnalyzerAsync<ExceptionHandlingComplianceAnalyzer>(testCode, expected);
    }
}

