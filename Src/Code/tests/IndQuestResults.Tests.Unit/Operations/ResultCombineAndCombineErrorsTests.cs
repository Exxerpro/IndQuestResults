namespace IndQuestResults.Tests.Unit.Operations;

public class ResultCombineAndCombineErrorsTests
{
    [Fact]
    public void Combine_WithNullArray_ReturnsThis()
    {
        var original = Result.Success();
        var combined = original.Combine((Result[]?)null!);
        combined.ShouldBeSameAs(original);
        combined.IsSuccess.ShouldBeTrue();
    }

    private sealed class OneShotEnumerable : IEnumerable<string>
    {
        private bool _consumed;
        private readonly string _value;
        public OneShotEnumerable(string value) => _value = value;
        public IEnumerator<string> GetEnumerator()
        {
            if (_consumed)
            {
                yield break;
            }
            _consumed = true;
            yield return _value;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void CombineErrors_FallbackWithDepletedEnumerables_ReturnsNoErrorsFoundMessage()
    {
        // Arrange: primary empty to force evaluating secondary.Any(); secondary yields once then depletes
        IEnumerable<string> primary = [];
        IEnumerable<string> secondary = new OneShotEnumerable("S1");

        // Act: First Any() on each will be true, but fallback AddRange gets zero items
        var res = Result.CombineErrors(primary, secondary);

        // Assert: Must choose NoErrorsFoundMessage when combined enumeration yields no items
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultConstants.NoErrorsFoundMessage);
    }

    [Fact]
    public void Combine_WithNoArguments_ReturnsThis()
    {
        var original = Result.Success();
        var combined = original.Combine();
        combined.ShouldBeSameAs(original);
        combined.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Combine_AggregatesErrors_FromThisAndOthers()
    {
        var thisFailure = Result.WithFailure(["E1"]);
        var otherFailure = Result.WithFailure(["E2", "E3"]);
        var success = Result.Success();

        var combined = thisFailure.Combine(success, otherFailure);

        combined.IsFailure.ShouldBeTrue();
        combined.Errors.ShouldBe(["E1", "E2", "E3"]);
    }

    [Fact]
    public void Combine_WithOnlySuccesses_RemainsSuccess()
    {
        var original = Result.Success();
        var combined = original.Combine(Result.Success(), Result.Success());
        combined.IsSuccess.ShouldBeTrue();
        combined.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void CombineErrors_BothNull_ReturnsDefaultMessage()
    {
        var res = Result.CombineErrors(null, null);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultConstants.NoErrorsFoundMessage);
    }

    [Fact]
    public void CombineErrors_PrimaryEmpty_SecondaryNonEmpty_ReturnsSecondary()
    {
        var primary = Array.Empty<string>();
        var secondary = new[] { "S1", "S2" };
        var res = Result.CombineErrors(primary, secondary);

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldBe(secondary);
    }

    [Fact]
    public void CombineErrors_BothEmpty_ReturnsDefaultMessage()
    {
        var primary = Array.Empty<string>();
        var secondary = Array.Empty<string>();
        var res = Result.CombineErrors(primary, secondary);

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultConstants.NoErrorsFoundMessage);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    public void FormatErrorsString_ICollectionBoundary_ShouldBeCorrect(int count)
    {
        // Use List to trigger ICollection<T> path (not array fast path)
        var list = Enumerable.Range(1, count).Select(i => $"E{i}").ToList();
        var s = Result.FormatErrorsString(list, "P");
        s.ShouldStartWith("P: E1");
        s.Count(c => c == ',').ShouldBe(Math.Max(0, count - 1));
        s.ShouldEndWith(count == 1 ? "E1" : $"E{count}");
    }
}
