namespace IndQuestResults.Tests.Unit.Operations;

public class ResultOneShotEnumerableTests
{
    private sealed class OneShotStrings : IEnumerable<string>
    {
        private bool _iterated;
        private readonly string _value;
        public OneShotStrings(string value) => _value = value;
        public IEnumerator<string> GetEnumerator()
        {
            if (_iterated) throw new InvalidOperationException("Enumerated more than once");
            _iterated = true;
            yield return _value;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void WithFailure_OneShotEnumerable_NormalizesErrorsAndIsStable()
    {
        var source = new OneShotStrings("E1");
        var result = Result.WithFailure(source);

        // Behavior: failure, non-empty errors, contains expected
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull();
        result.Errors.Any().ShouldBeTrue();
        result.Errors.First().ShouldBe("E1");

        // Calling ToString (and reading Errors again) must not re-enumerate the source
        var s1 = result.ToString();
        var _ = result.Errors.ToArray();
        s1.ShouldContain("E1");
    }
}

