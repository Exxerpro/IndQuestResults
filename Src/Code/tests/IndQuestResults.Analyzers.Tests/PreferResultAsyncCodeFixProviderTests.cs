using System.Threading.Tasks;
using IndQuestResults.Analyzers.CodeFixes;
using IndQuestResults.Analyzers.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Meziantou.Extensions.Logging.Xunit;
using Xunit;
using Xunit.Abstractions;
using Shouldly;

namespace IndQuestResults.Analyzers.Tests;

/// <summary>
/// Code fix tests for IQR0001: Prefer ResultAsync over ResultExtensions.ThenAsync.
/// </summary>
public sealed class PreferResultAsyncCodeFixProviderTests
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferResultAsyncCodeFixProviderTests"/> class.
    /// </summary>
    /// <param name="output"></param>
    public PreferResultAsyncCodeFixProviderTests(ITestOutputHelper output)
    {
        _logger = XUnitLogger.CreateLogger<PreferResultAsyncCodeFixProviderTests>(output);
    }

    /// <summary>
    /// Applies the code fix and verifies the rewrite to ResultAsync.BindAsync.
    /// </summary>
    [Fact]
    public async Task CodeFix_Rewrites_ThenAsync_To_ResultAsync_BindAsync()
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

#pragma warning disable CA1848 // Use LoggerMessage delegates - temporary debugging code
        _logger.LogInformation("Applying code fix to source code");
        _logger.LogDebug("Source code:\n{Source}", src);

        var fixedCode = await CodeFixTestHelper.ApplyFirstCodeFixAsync<Analyzers.PreferResultAsyncAnalyzer, PreferResultAsyncCodeFixProvider>(src, _logger);

        _logger.LogInformation("Fixed code:\n{FixedCode}", fixedCode);
        _logger.LogInformation("Checking if fixed code contains 'IndQuestResults.Async.ResultAsync.BindAsync': {Contains}",
            fixedCode.Contains("IndQuestResults.Async.ResultAsync.BindAsync", StringComparison.OrdinalIgnoreCase));
        _logger.LogInformation("Checking if fixed code contains 'ThenAsync(': {Contains}",
            fixedCode.Contains("ThenAsync(", StringComparison.OrdinalIgnoreCase));
#pragma warning restore CA1848

        fixedCode.ShouldContain("IndQuestResults.Async.ResultAsync.BindAsync");
        fixedCode.ShouldNotContain("ThenAsync(");
    }
}
