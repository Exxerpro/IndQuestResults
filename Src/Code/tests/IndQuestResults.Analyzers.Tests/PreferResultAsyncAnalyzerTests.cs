using System.Threading.Tasks;
using IndQuestResults.Analyzers.Analyzers;
using IndQuestResults.Analyzers.Tests.Helpers;
using IndQuestResults.Analyzers.Tests.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using Meziantou.Extensions.Logging.Xunit;

namespace IndQuestResults.Analyzers.Tests;

/// <summary>
/// Analyzer tests for IQR0001: prefer ResultAsync over ResultExtensions.ThenAsync.
/// </summary>
public sealed class PreferResultAsyncAnalyzerTests
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferResultAsyncAnalyzerTests"/> class.
    /// </summary>
    /// <param name="output"></param>
    public PreferResultAsyncAnalyzerTests(ITestOutputHelper output)
    {
        _logger = XUnitLogger.CreateLogger<PreferResultAsyncAnalyzerTests>(output);
    }

    /// <summary>
    /// Verifies that calling ThenAsync on Task&lt;Result&lt;T&gt;&gt; produces IQR0001.
    /// </summary>
    [Fact]
    public async Task ThenAsync_OnTaskResult_ProducesDiagnostic()
    {
        var src = @"using System.Threading.Tasks;
using IndQuestResults;
using IndQuestResults.Operations;
class C {
    async Task M(){
        var t = Task.FromResult(Result<int>.Success(1));
        var x = await t.ThenAsync(v => Task.FromResult(Result<string>.Success(v.ToString())));
    }
}";

        await AnalyzerTestHelper.VerifyAnalyzerAsync<PreferResultAsyncAnalyzer>(
            src,
            _logger,
            AnalyzerTestHelper.Diagnostic(PreferResultAsyncAnalyzer.DiagnosticId, DiagnosticSeverity.Info, line: 7, column: 25));
    }

    /// <summary>
    /// Verifies that using ResultAsync.BindAsync does not trigger the analyzer.
    /// </summary>
    [Fact]
    public async Task BindAsync_FromResultAsync_DoesNotProduceDiagnostic()
    {
        var src = @"using System.Threading.Tasks;
using IndQuestResults;
using IndQuestResults.Async;
class C {
    async Task M(){
        var t = Task.FromResult(Result<int>.Success(1));
        var x = await ResultAsync.BindAsync(t, v => Task.FromResult(Result<string>.Success(v.ToString())));
    }
}";

        await AnalyzerTestHelper.VerifyNoDiagnosticsAsync<PreferResultAsyncAnalyzer>(src);
    }
}
