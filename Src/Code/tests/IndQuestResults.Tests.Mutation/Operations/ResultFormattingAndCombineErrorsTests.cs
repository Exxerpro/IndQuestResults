namespace IndQuestResults.Tests.Mutation.Operations;

public class ResultFormattingAndCombineErrorsTests
{
    [Fact]
    public void FormatErrorsString_Empty_ReturnsPrefixOnly()
    {
        var s1 = Result.FormatErrorsString(Array.Empty<string>(), ResultConstants.FailurePrefix);
        var s2 = Result.FormatErrorsString((IEnumerable<string>)new List<string>(), ResultConstants.FailurePrefix);
        s1.ShouldBe("WithFailure");
        s2.ShouldBe("WithFailure");
    }

    [Fact]
    public void FormatErrorsString_SmallArray_UsesSpanBuild()
    {
        var s = Result.FormatErrorsString(new[] { "a", "b", "c" }, ResultConstants.FailurePrefix);
        s.ShouldBe("WithFailure: a, b, c");
    }

    [Fact]
    public void FormatErrorsString_LargeCollection_UsesFallback()
    {
        var list = Enumerable.Range(0, 20).Select(i => $"e{i}").ToList();
        var s = Result.FormatErrorsString(list, ResultConstants.FailurePrefix);
        s.ShouldContain("WithFailure: e0, e1, e2");
    }

    [Fact]
    public void CombineErrors_VariousBranches()
    {
        // both null
        Result.CombineErrors(null, null).ToString().ShouldBe($"WithFailure: {ResultConstants.NoErrorsFoundMessage}");

        // one null
        var r2 = Result.CombineErrors(new[] { "a" }, null);
        r2.IsFailure.ShouldBeTrue();
        r2.Error.ShouldBe("a");

        // both empty
        Result.CombineErrors(Array.Empty<string>(), Array.Empty<string>()).ToString().ShouldBe($"WithFailure: {ResultConstants.NoErrorsFoundMessage}");

        // small collections (Span path)
        var r3 = Result.CombineErrors(new[] { "a" }, new[] { "b" });
        r3.ToString().ShouldBe("WithFailure: a, b");

        // large collections (fallback)
        var many = Enumerable.Range(0, 20).Select(i => $"e{i}").ToList();
        var r4 = Result.CombineErrors(many, many);
        r4.IsFailure.ShouldBeTrue();
        r4.Errors.Count().ShouldBe(40);
    }

    [Fact]
    public void ResultT_WithWarnings_ClampsMetadata_AndDefaultsWarningMessage()
    {
        var r = Result<string>.WithWarnings(Array.Empty<string>(), "v", confidence: 2.0, missingDataRatio: -1.0);
        r.IsSuccess.ShouldBeTrue();
        r.Warnings.ShouldBe(new[] { ResultConstants.DefaultWarningMessage });
        r.Confidence.ShouldBe(1.0);
        r.MissingDataRatio.ShouldBe(0.0);
    }
}


