using System.Threading.Tasks;
using IndQuestResults.Analyzers.CodeFixes;
using IndQuestResults.Analyzers.Tests.Helpers;
using Xunit;
using Shouldly;

namespace IndQuestResults.Analyzers.Tests;

/// <summary>
/// Code fix tests for IQR0001: Prefer ResultAsync over ResultExtensions.ThenAsync.
/// </summary>
public sealed class PreferResultAsyncCodeFixProviderTests
{
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

        var fixedCode = await CodeFixTestHelper.ApplyFirstCodeFixAsync<Analyzers.PreferResultAsyncAnalyzer, PreferResultAsyncCodeFixProvider>(src);

        fixedCode.ShouldContain("IndQuestResults.Async.ResultAsync.BindAsync");
        fixedCode.ShouldNotContain("ThenAsync(");
    }
}


