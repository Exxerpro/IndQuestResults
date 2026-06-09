namespace IndQuestResults.Tests.Mutation.Operations;

public class ResultGenericAdditionalCoverageTests
{
    [Fact]
    public void WithWarnings_ClampsMetadata_AndSetsHasWarnings()
    {
        var r = Result<double>.WithWarnings(["w1"], 3.14, confidence: 2.5, missingDataRatio: -1);
        r.IsRecoverable.ShouldBeTrue();
        r.HasWarnings.ShouldBeTrue();
        r.Confidence.ShouldBe(1.0);
        r.MissingDataRatio.ShouldBe(0.0);
    }

    [Fact]
    public void WithWarnings_DefaultWarningMessage_WhenNullOrEmpty()
    {
        var r1 = Result<string>.WithWarnings(null!, "x");
        r1.HasWarnings.ShouldBeTrue();
        r1.Warnings.ShouldContain(ResultConstants.DefaultWarningMessage);

        var r2 = Result<string>.WithWarnings(Array.Empty<string>(), "x");
        r2.Warnings.ShouldContain(ResultConstants.DefaultWarningMessage);
    }

    [Fact]
    public void Deconstruct_And_ImplicitConversions_Work()
    {
        Result<int> r = Result<int>.Success(7);
        Result nongeneric = r; // implicit to non-generic
        nongeneric.IsSuccess.ShouldBeTrue();

        var (ok, value, errs) = r; // deconstruct
        ok.ShouldBeTrue();
        value.ShouldBe(7);
        errs.ShouldBeEmpty();
    }

    [Fact]
    public void ToString_SuccessAndFailure_Format()
    {
        Result<string>.Success("ok").ToString().ShouldStartWith(ResultConstants.SuccessPrefix);
        Result<string>.WithFailure("e").ToString().ShouldStartWith(ResultConstants.FailurePrefix);
    }

    [Fact]
    public void WithSuccess_Alias_Works()
    {
        var r = Result<string>.WithSuccess("x");
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe("x");
    }

    [Fact]
    public void Combine_VariousPaths_Coverage()
    {
        // No input
        var baseR = Result<string>.Success("v");
        baseR.Combine().IsSuccess.ShouldBeTrue();

        // All success -> returns current
        var res = baseR.Combine(Result.Success(), Result.Success());
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("v");

        // Failures aggregate
        var res2 = baseR.Combine(Result.WithFailure("e1"), Result.WithFailure("e2"));
        res2.IsFailure.ShouldBeTrue();
        res2.Errors.ShouldContain("e1");
        res2.Errors.ShouldContain("e2");
    }

    [Fact]
    public void OnFailure_Invokes_AndTap_Executes()
    {
        var captured = new List<string>();
        var executed = false;
        var fail = Result<int>.WithFailure("err", 0);
        fail.OnFailure(es => captured.AddRange(es));
        captured.ShouldContain("err");

        var tapRes = Result<string>.Success("a").Tap(_ => executed = true);
        executed.ShouldBeTrue();
        tapRes.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Combine_AggregatesErrors_AndReturnsCurrentOnSuccess()
    {
        var baseSuccess = Result<string>.Success("v");
        var combined = baseSuccess.Combine(Result.WithFailure("e1"), Result.Success());
        combined.IsFailure.ShouldBeTrue();
        combined.Errors.ShouldContain("e1");

        var baseFail = Result<string>.WithFailure("e0");
        var combined2 = baseFail.Combine(Result.WithFailure("e1"));
        combined2.IsFailure.ShouldBeTrue();
        combined2.Errors.ShouldContain("e0");
        combined2.Errors.ShouldContain("e1");
    }

    [Fact]
    public void OnSuccess_Tap_Ensure_Map_Bind_Paths()
    {
        var executed = false;
        var mapped = Result<string>.Success("a")
            .OnSuccess(_ => executed = true)
            .Tap(_ => executed = true)
            .Ensure(s => s == "a", "err")
            .Map(s => s + "b")
            .Bind(s => Result<string>.Success(s + "c"));

        executed.ShouldBeTrue();
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe("abc");
    }

    [Fact]
    public void Recover_And_Match_FailurePath()
    {
        var failed = Result<string>.WithFailure("e");
        var recovered = failed.Recover(() => Result<string>.Success("r"));
        recovered.IsSuccess.ShouldBeTrue();

        var matched = failed.Match(s => s.Length, errs => -1);
        matched.IsSuccess.ShouldBeTrue();
        matched.Value.ShouldBe(-1);
    }
}
