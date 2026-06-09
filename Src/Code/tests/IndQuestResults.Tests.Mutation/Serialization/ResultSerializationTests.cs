namespace IndQuestResults.Tests.Mutation.Serialization;

public class ResultSerializationTests
{
    [Fact]
    public void RoundTrip_Success_WithWarningsAndMetadata_PreservesState()
    {
        var original = Result<int>.WithWarnings(["approximate"], 42, confidence: 0.85, missingDataRatio: 0.15);

        var json = JsonSerializer.Serialize(original);
        var roundTrip = JsonSerializer.Deserialize<Result<int>>(json)!;

        roundTrip.IsRecoverable.ShouldBeTrue();
        roundTrip.IsFailure.ShouldBeFalse();
        roundTrip.Value.ShouldBe(42);
    }

    [Fact]
    public void RoundTrip_Failure_WithMultipleErrors_PreservesErrors()
    {
        var original = Result<string>.WithFailure(["e1", "e2"], value: null);

        var json = JsonSerializer.Serialize(original);
        var roundTrip = JsonSerializer.Deserialize<Result<string>>(json)!;

        roundTrip.IsFailure.ShouldBeTrue();
        roundTrip.HasErrors.ShouldBeTrue();
        roundTrip.Errors.ShouldNotBeNull();
        roundTrip.Errors.Count().ShouldBe(2);
        roundTrip.Errors.ShouldContain("e1");
        roundTrip.Errors.ShouldContain("e2");
    }

    [Fact]
    public void Deserialize_MalformedJson_ThrowsJsonException()
    {
        const string malformed = "{ \"isSuccess\": true, \"errors\": [ 1, 2, 3 ], \"value\": 5"; // missing closing }
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int>>(malformed));
    }

    [Fact]
    public void JsonConstructor_StateValidation_AdjustsHasErrors()
    {
        // Directly use JsonConstructor path to trigger ValidateInternalState
        // errors null => HasErrors should be normalized to false
        var constructed = new Result<int>(isSuccess: true, errors: null, value: 7);
        constructed.IsRecoverable.ShouldBeTrue();
        constructed.HasErrors.ShouldBeFalse();

        // errors non-empty => HasErrors should be true
        var constructedWithErrors = new Result<int>(isSuccess: false, errors: new[] { "x" }, value: default);
        constructedWithErrors.IsFailure.ShouldBeTrue();
        constructedWithErrors.HasErrors.ShouldBeTrue();
        constructedWithErrors.Errors.ShouldNotBeNull();
        constructedWithErrors.Errors.Any().ShouldBeTrue();
    }

    [Fact]
    public void RoundTrip_ComplexGeneric_ValueSurvives()
    {
        var value = new Dictionary<string, List<int>>
        {
            ["a"] = new() { 1, 2, 3 },
            ["b"] = new() { 4 }
        };
        var original = Result<Dictionary<string, List<int>>>.Success(value);

        var json = JsonSerializer.Serialize(original);
        var roundTrip = JsonSerializer.Deserialize<Result<Dictionary<string, List<int>>>>(json)!;

        roundTrip.IsRecoverable.ShouldBeTrue();
        roundTrip.Value.ShouldNotBeNull();
        roundTrip.Value!["a"].Count.ShouldBe(3);
        roundTrip.Value!["b"].Single().ShouldBe(4);
    }
}
